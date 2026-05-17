using IoTFarmBench.RestService.Models;
using Npgsql;

namespace IoTFarmBench.RestService.Data;

public sealed class SensorReadingRepository(DbConnectionFactory connectionFactory)
{
    private static readonly IReadOnlyDictionary<string, string> SelectiveFields =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["timestamp"] = "sr.timestamp",
            ["temperatureC"] = "sr.temperature_c",
            ["humidityPercent"] = "sr.humidity_percent",
            ["soilMoisturePercent"] = "sr.soil_moisture_percent",
            ["soilPh"] = "sr.soil_ph",
            ["rainfallMm"] = "sr.rainfall_mm",
            ["sunlightHours"] = "sr.sunlight_hours",
            ["ndviIndex"] = "sr.ndvi_index",
            ["yieldKgPerHectare"] = "sr.yield_kg_per_hectare",
            ["cropDiseaseStatus"] = "sr.crop_disease_status"
        };

    private static readonly IReadOnlyDictionary<string, Func<NpgsqlDataReader, object?>> SelectiveReaders =
        new Dictionary<string, Func<NpgsqlDataReader, object?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["timestamp"] = reader => RepositoryMapping.ReadTimestamp(reader, "timestamp"),
            ["temperatureC"] = reader => RepositoryMapping.ReadNullable<double>(reader, "temperatureC"),
            ["humidityPercent"] = reader => RepositoryMapping.ReadNullable<double>(reader, "humidityPercent"),
            ["soilMoisturePercent"] = reader => RepositoryMapping.ReadNullable<double>(reader, "soilMoisturePercent"),
            ["soilPh"] = reader => RepositoryMapping.ReadNullable<double>(reader, "soilPh"),
            ["rainfallMm"] = reader => RepositoryMapping.ReadNullable<double>(reader, "rainfallMm"),
            ["sunlightHours"] = reader => RepositoryMapping.ReadNullable<double>(reader, "sunlightHours"),
            ["ndviIndex"] = reader => RepositoryMapping.ReadNullable<double>(reader, "ndviIndex"),
            ["yieldKgPerHectare"] = reader => RepositoryMapping.ReadNullable<double>(reader, "yieldKgPerHectare"),
            ["cropDiseaseStatus"] = reader => RepositoryMapping.ReadNullableString(reader, "cropDiseaseStatus")
        };

    public static IReadOnlySet<string> AllowedSelectiveFields { get; } =
        SelectiveFields.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<SensorReadingDto>> GetLatestAsync(
        Guid? deviceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, device_id, timestamp, crop_type, soil_moisture_percent, soil_ph,
                   temperature_c, rainfall_mm, humidity_percent, sunlight_hours,
                   irrigation_type, fertilizer_type, pesticide_usage_ml, sowing_date,
                   harvest_date, total_days, yield_kg_per_hectare, ndvi_index,
                   crop_disease_status
            FROM sensor_readings
            WHERE (@deviceId::uuid IS NULL OR device_id = @deviceId)
              AND (@from::timestamptz IS NULL OR timestamp >= @from)
              AND (@to::timestamptz IS NULL OR timestamp <= @to)
            ORDER BY timestamp DESC
            LIMIT @limit
            """,
            connection);

        command.Parameters.AddWithValue("deviceId", RepositoryMapping.DbValue(deviceId));
        command.Parameters.AddWithValue("from", RepositoryMapping.DbValue(from?.UtcDateTime));
        command.Parameters.AddWithValue("to", RepositoryMapping.DbValue(to?.UtcDateTime));
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var readings = new List<SensorReadingDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            readings.Add(MapReading(reader));
        }

        return readings;
    }

    public async Task<SensorReadingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, device_id, timestamp, crop_type, soil_moisture_percent, soil_ph,
                   temperature_c, rainfall_mm, humidity_percent, sunlight_hours,
                   irrigation_type, fertilizer_type, pesticide_usage_ml, sowing_date,
                   harvest_date, total_days, yield_kg_per_hectare, ndvi_index,
                   crop_disease_status
            FROM sensor_readings
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReading(reader) : null;
    }

    public async Task<SensorReadingDto> CreateAsync(
        CreateSensorReadingRequest request,
        DeviceRepository deviceRepository,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var deviceId = await deviceRepository.GetOrCreateDeviceIdAsync(
            request.SensorId!,
            request.FarmId,
            request.Region,
            request.Latitude,
            request.Longitude,
            connection,
            transaction,
            cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO sensor_readings (
                device_id, timestamp, crop_type, soil_moisture_percent, soil_ph,
                temperature_c, rainfall_mm, humidity_percent, sunlight_hours,
                irrigation_type, fertilizer_type, pesticide_usage_ml, sowing_date,
                harvest_date, total_days, yield_kg_per_hectare, ndvi_index,
                crop_disease_status
            )
            VALUES (
                @deviceId, @timestamp, @cropType, @soilMoisturePercent, @soilPh,
                @temperatureC, @rainfallMm, @humidityPercent, @sunlightHours,
                @irrigationType, @fertilizerType, @pesticideUsageMl, @sowingDate,
                @harvestDate, @totalDays, @yieldKgPerHectare, @ndviIndex,
                @cropDiseaseStatus
            )
            RETURNING id, device_id, timestamp, crop_type, soil_moisture_percent, soil_ph,
                      temperature_c, rainfall_mm, humidity_percent, sunlight_hours,
                      irrigation_type, fertilizer_type, pesticide_usage_ml, sowing_date,
                      harvest_date, total_days, yield_kg_per_hectare, ndvi_index,
                      crop_disease_status
            """,
            connection,
            transaction);

        AddCreateParameters(command, request, deviceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Reading insert did not return a row.");
        }

        var created = MapReading(reader);
        await reader.CloseAsync();
        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetSelectiveAsync(
        IReadOnlyList<string> fields,
        int limit,
        CancellationToken cancellationToken)
    {
        var selectedColumns = fields.Select(field => $"{SelectiveFields[field]} AS \"{field}\"");
        var sql =
            $"""
            SELECT {string.Join(", ", selectedColumns)}
            FROM sensor_readings sr
            ORDER BY sr.timestamp DESC
            LIMIT @limit
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields)
            {
                row[field] = SelectiveReaders[field](reader);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static void AddCreateParameters(NpgsqlCommand command, CreateSensorReadingRequest request, Guid deviceId)
    {
        command.Parameters.AddWithValue("deviceId", deviceId);
        command.Parameters.AddWithValue("timestamp", RepositoryMapping.ToUtcDateTime(request.Timestamp!.Value));
        command.Parameters.AddWithValue("cropType", RepositoryMapping.DbValue(request.CropType));
        command.Parameters.AddWithValue("soilMoisturePercent", RepositoryMapping.DbValue(request.SoilMoisturePercent));
        command.Parameters.AddWithValue("soilPh", RepositoryMapping.DbValue(request.SoilPh));
        command.Parameters.AddWithValue("temperatureC", RepositoryMapping.DbValue(request.TemperatureC));
        command.Parameters.AddWithValue("rainfallMm", RepositoryMapping.DbValue(request.RainfallMm));
        command.Parameters.AddWithValue("humidityPercent", RepositoryMapping.DbValue(request.HumidityPercent));
        command.Parameters.AddWithValue("sunlightHours", RepositoryMapping.DbValue(request.SunlightHours));
        command.Parameters.AddWithValue("irrigationType", RepositoryMapping.DbValue(request.IrrigationType));
        command.Parameters.AddWithValue("fertilizerType", RepositoryMapping.DbValue(request.FertilizerType));
        command.Parameters.AddWithValue("pesticideUsageMl", RepositoryMapping.DbValue(request.PesticideUsageMl));
        command.Parameters.AddWithValue("sowingDate", RepositoryMapping.DbValue(request.SowingDate));
        command.Parameters.AddWithValue("harvestDate", RepositoryMapping.DbValue(request.HarvestDate));
        command.Parameters.AddWithValue("totalDays", RepositoryMapping.DbValue(request.TotalDays));
        command.Parameters.AddWithValue("yieldKgPerHectare", RepositoryMapping.DbValue(request.YieldKgPerHectare));
        command.Parameters.AddWithValue("ndviIndex", RepositoryMapping.DbValue(request.NdviIndex));
        command.Parameters.AddWithValue("cropDiseaseStatus", RepositoryMapping.DbValue(request.CropDiseaseStatus));
    }

    private static SensorReadingDto MapReading(NpgsqlDataReader reader)
    {
        return new SensorReadingDto(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetGuid(reader.GetOrdinal("device_id")),
            RepositoryMapping.ReadTimestamp(reader, "timestamp"),
            RepositoryMapping.ReadNullableString(reader, "crop_type"),
            RepositoryMapping.ReadNullable<double>(reader, "soil_moisture_percent"),
            RepositoryMapping.ReadNullable<double>(reader, "soil_ph"),
            RepositoryMapping.ReadNullable<double>(reader, "temperature_c"),
            RepositoryMapping.ReadNullable<double>(reader, "rainfall_mm"),
            RepositoryMapping.ReadNullable<double>(reader, "humidity_percent"),
            RepositoryMapping.ReadNullable<double>(reader, "sunlight_hours"),
            RepositoryMapping.ReadNullableString(reader, "irrigation_type"),
            RepositoryMapping.ReadNullableString(reader, "fertilizer_type"),
            RepositoryMapping.ReadNullable<double>(reader, "pesticide_usage_ml"),
            RepositoryMapping.ReadNullableDate(reader, "sowing_date"),
            RepositoryMapping.ReadNullableDate(reader, "harvest_date"),
            RepositoryMapping.ReadNullable<int>(reader, "total_days"),
            RepositoryMapping.ReadNullable<double>(reader, "yield_kg_per_hectare"),
            RepositoryMapping.ReadNullable<double>(reader, "ndvi_index"),
            RepositoryMapping.ReadNullableString(reader, "crop_disease_status"));
    }
}
