import { GraphQLError } from 'graphql';
import { getAnalyticsByRegion, getAnalyticsSummary } from './repositories/analyticsRepository.js';
import { getDeviceById, getDevices } from './repositories/deviceRepository.js';
import { createReading, getReadingById, getReadings } from './repositories/readingRepository.js';

export const resolvers = {
  Query: {
    status: () => 'ok',
    devices: () => getDevices(),
    device: (_, { id }) => {
      assertUuid(id, 'id');
      return getDeviceById(id);
    },
    readings: (_, args) => {
      const limit = normalizeLimit(args.limit);
      const from = parseOptionalTimestamp(args.from, 'from');
      const to = parseOptionalTimestamp(args.to, 'to');
      validateDateRange(from, to);

      if (args.deviceId) {
        assertUuid(args.deviceId, 'deviceId');
      }

      return getReadings({
        deviceId: args.deviceId ?? null,
        from,
        to,
        limit
      });
    },
    reading: (_, { id }) => {
      assertUuid(id, 'id');
      return getReadingById(id);
    },
    analyticsSummary: (_, args) => {
      const from = parseOptionalTimestamp(args.from, 'from');
      const to = parseOptionalTimestamp(args.to, 'to');
      validateDateRange(from, to);

      return getAnalyticsSummary({
        from,
        to,
        region: emptyToNull(args.region),
        cropType: emptyToNull(args.cropType)
      });
    },
    analyticsByRegion: () => getAnalyticsByRegion()
  },
  Mutation: {
    createReading: (_, { input }) => {
      if (!input.sensorId?.trim()) {
        throw badUserInput('sensorId is required.');
      }

      const timestamp = parseRequiredTimestamp(input.timestamp, 'timestamp');
      const sowingDate = parseOptionalDate(input.sowingDate, 'sowingDate');
      const harvestDate = parseOptionalDate(input.harvestDate, 'harvestDate');

      return createReading({
        ...input,
        sensorId: input.sensorId.trim(),
        timestamp,
        sowingDate,
        harvestDate
      });
    }
  }
};

function normalizeLimit(limit) {
  const value = limit ?? 100;
  if (!Number.isInteger(value) || value < 1 || value > 1000) {
    throw badUserInput('limit must be between 1 and 1000.');
  }

  return value;
}

function assertUuid(value, fieldName) {
  const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  if (!uuidPattern.test(value)) {
    throw badUserInput(`${fieldName} must be a valid UUID.`);
  }
}

function parseRequiredTimestamp(value, fieldName) {
  if (!value?.trim()) {
    throw badUserInput(`${fieldName} is required.`);
  }

  return parseTimestamp(value, fieldName);
}

function parseOptionalTimestamp(value, fieldName) {
  if (!value?.trim()) {
    return null;
  }

  return parseTimestamp(value, fieldName);
}

function parseTimestamp(value, fieldName) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    throw badUserInput(`${fieldName} must be a valid ISO 8601 timestamp.`);
  }

  return date;
}

function parseOptionalDate(value, fieldName) {
  if (!value?.trim()) {
    return null;
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || Number.isNaN(new Date(`${value}T00:00:00Z`).getTime())) {
    throw badUserInput(`${fieldName} must be a valid ISO date, for example 2024-05-10.`);
  }

  return value;
}

function validateDateRange(from, to) {
  if (from && to && from > to) {
    throw badUserInput('from must be earlier than or equal to to.');
  }
}

function emptyToNull(value) {
  return value?.trim() ? value.trim() : null;
}

function badUserInput(message) {
  return new GraphQLError(message, {
    extensions: { code: 'BAD_USER_INPUT' }
  });
}
