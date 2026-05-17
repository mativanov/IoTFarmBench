export const typeDefs = `#graphql
  type Device {
    id: ID!
    sensorId: String!
    farmId: String
    region: String
    latitude: Float
    longitude: Float
  }

  type SensorReading {
    id: ID!
    deviceId: ID!
    timestamp: String!
    cropType: String
    soilMoisturePercent: Float
    soilPh: Float
    temperatureC: Float
    rainfallMm: Float
    humidityPercent: Float
    sunlightHours: Float
    irrigationType: String
    fertilizerType: String
    pesticideUsageMl: Float
    sowingDate: String
    harvestDate: String
    totalDays: Int
    yieldKgPerHectare: Float
    ndviIndex: Float
    cropDiseaseStatus: String
  }

  type AnalyticsSummary {
    count: Int!
    avgTemperatureC: Float
    avgHumidityPercent: Float
    avgSoilMoisturePercent: Float
    avgSoilPh: Float
    avgRainfallMm: Float
    avgSunlightHours: Float
    avgNdviIndex: Float
    avgYieldKgPerHectare: Float
    minTimestamp: String
    maxTimestamp: String
  }

  type AnalyticsByRegion {
    region: String
    count: Int!
    avgTemperatureC: Float
    avgHumidityPercent: Float
    avgSoilMoisturePercent: Float
    avgNdviIndex: Float
    avgYieldKgPerHectare: Float
  }

  input CreateSensorReadingInput {
    sensorId: String!
    farmId: String
    region: String
    latitude: Float
    longitude: Float
    timestamp: String!
    cropType: String
    soilMoisturePercent: Float
    soilPh: Float
    temperatureC: Float
    rainfallMm: Float
    humidityPercent: Float
    sunlightHours: Float
    irrigationType: String
    fertilizerType: String
    pesticideUsageMl: Float
    sowingDate: String
    harvestDate: String
    totalDays: Int
    yieldKgPerHectare: Float
    ndviIndex: Float
    cropDiseaseStatus: String
  }

  type Query {
    status: String!

    devices: [Device!]!
    device(id: ID!): Device

    readings(deviceId: ID, from: String, to: String, limit: Int = 100): [SensorReading!]!
    reading(id: ID!): SensorReading

    analyticsSummary(from: String, to: String, region: String, cropType: String): AnalyticsSummary!
    analyticsByRegion: [AnalyticsByRegion!]!
  }

  type Mutation {
    createReading(input: CreateSensorReadingInput!): SensorReading!
  }
`;
