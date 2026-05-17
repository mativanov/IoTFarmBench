using System.Globalization;
using Grpc.Core;
using IoTFarmBench.GrpcService.Data;
using IoTFarmBench.GrpcService.Protos;

namespace IoTFarmBench.GrpcService.Services;

public sealed class FarmBenchmarkGrpcService(
    DeviceRepository deviceRepository,
    SensorReadingRepository readingRepository,
    AnalyticsRepository analyticsRepository) : FarmBenchmarkService.FarmBenchmarkServiceBase
{
    public override async Task<GetDevicesResponse> GetDevices(GetDevicesRequest request, ServerCallContext context)
    {
        var devices = await deviceRepository.GetAllAsync(context.CancellationToken);
        var response = new GetDevicesResponse();
        response.Devices.AddRange(devices);
        return response;
    }

    public override async Task<DeviceMessage> GetDeviceById(GetDeviceByIdRequest request, ServerCallContext context)
    {
        var id = ParseRequiredGuid(request.Id, "id");
        var device = await deviceRepository.GetByIdAsync(id, context.CancellationToken);
        return device ?? throw new RpcException(new Status(StatusCode.NotFound, "Device was not found."));
    }

    public override async Task<GetReadingsResponse> GetReadings(GetReadingsRequest request, ServerCallContext context)
    {
        var deviceId = ParseOptionalGuid(request.DeviceId, "device_id");
        var from = ParseOptionalTimestamp(request.From, "from");
        var to = ParseOptionalTimestamp(request.To, "to");
        ValidateDateRange(from, to);
        var limit = NormalizeLimit(request.Limit);

        var readings = await readingRepository.GetLatestAsync(deviceId, from, to, limit, context.CancellationToken);
        var response = new GetReadingsResponse();
        response.Readings.AddRange(readings);
        return response;
    }

    public override async Task<SensorReadingMessage> GetReadingById(GetReadingByIdRequest request, ServerCallContext context)
    {
        var id = ParseRequiredGuid(request.Id, "id");
        var reading = await readingRepository.GetByIdAsync(id, context.CancellationToken);
        return reading ?? throw new RpcException(new Status(StatusCode.NotFound, "Reading was not found."));
    }

    public override async Task<SensorReadingMessage> CreateReading(
        CreateSensorReadingRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            throw InvalidArgument("sensor_id is required.");
        }

        var timestamp = ParseRequiredTimestamp(request.Timestamp, "timestamp");
        var sowingDate = ParseOptionalDate(request.SowingDate, "sowing_date");
        var harvestDate = ParseOptionalDate(request.HarvestDate, "harvest_date");

        return await readingRepository.CreateAsync(
            request,
            timestamp,
            sowingDate,
            harvestDate,
            deviceRepository,
            context.CancellationToken);
    }

    public override async Task<GetSelectiveReadingsResponse> GetSelectiveReadings(
        GetSelectiveReadingsRequest request,
        ServerCallContext context)
    {
        if (request.Fields.Count == 0)
        {
            throw InvalidArgument("fields is required.");
        }

        var fields = request.Fields
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (fields.Length == 0)
        {
            throw InvalidArgument("fields must include at least one non-empty field name.");
        }

        var invalidFields = fields
            .Where(field => !SensorReadingRepository.AllowedSelectiveFields.Contains(field))
            .ToArray();

        if (invalidFields.Length > 0)
        {
            throw InvalidArgument($"Invalid selective fields: {string.Join(", ", invalidFields)}.");
        }

        var limit = NormalizeLimit(request.Limit);
        var readings = await readingRepository.GetSelectiveAsync(fields, limit, context.CancellationToken);
        var response = new GetSelectiveReadingsResponse();
        response.Readings.AddRange(readings);
        return response;
    }

    public override async Task<AnalyticsSummaryMessage> GetAnalyticsSummary(
        GetAnalyticsSummaryRequest request,
        ServerCallContext context)
    {
        var from = ParseOptionalTimestamp(request.From, "from");
        var to = ParseOptionalTimestamp(request.To, "to");
        ValidateDateRange(from, to);

        return await analyticsRepository.GetSummaryAsync(
            from,
            to,
            request.Region,
            request.CropType,
            context.CancellationToken);
    }

    public override async Task<GetAnalyticsByRegionResponse> GetAnalyticsByRegion(
        GetAnalyticsByRegionRequest request,
        ServerCallContext context)
    {
        var regions = await analyticsRepository.GetByRegionAsync(context.CancellationToken);
        var response = new GetAnalyticsByRegionResponse();
        response.Regions.AddRange(regions);
        return response;
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return 100;
        }

        if (limit > 1000)
        {
            throw InvalidArgument("limit must be less than or equal to 1000.");
        }

        return limit;
    }

    private static Guid ParseRequiredGuid(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id))
        {
            throw InvalidArgument($"{fieldName} must be a valid UUID.");
        }

        return id;
    }

    private static Guid? ParseOptionalGuid(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var id)
            ? id
            : throw InvalidArgument($"{fieldName} must be a valid UUID.");
    }

    private static DateTimeOffset ParseRequiredTimestamp(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw InvalidArgument($"{fieldName} is required.");
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var timestamp)
            ? timestamp
            : throw InvalidArgument($"{fieldName} must be a valid ISO 8601 timestamp.");
    }

    private static DateTimeOffset? ParseOptionalTimestamp(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var timestamp)
            ? timestamp
            : throw InvalidArgument($"{fieldName} must be a valid ISO 8601 timestamp.");
    }

    private static DateOnly? ParseOptionalDate(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw InvalidArgument($"{fieldName} must be a valid ISO date, for example 2024-05-10.");
    }

    private static void ValidateDateRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is not null && to is not null && from > to)
        {
            throw InvalidArgument("from must be earlier than or equal to to.");
        }
    }

    private static RpcException InvalidArgument(string message) =>
        new(new Status(StatusCode.InvalidArgument, message));
}
