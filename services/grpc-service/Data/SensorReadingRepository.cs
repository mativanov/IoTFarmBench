using IoTFarmBench.GrpcService.Protos;
using Npgsql;

namespace IoTFarmBench.GrpcService.Data;

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

    public static IReadOnlySet<string> AllowedSelectiveFields { get; } =
        SelectiveFields.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<SensorReadingMessage>> GetLatestAsync(
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
        var readings = new List<SensorReadingMessage>();
        while (await reader.ReadAsync(cancellationToken))
        {
            readings.Add(MapReading(reader));
        }

        return readings;
    }

    public async Task<SensorReadingMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
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

    public async Task<SensorReadingMessage> CreateAsync(
        CreateSensorReadingRequest request,
        DateTimeOffset timestamp,
        DateOnly? sowingDate,
        DateOnly? harvestDate,
        DeviceRepository deviceRepository,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var deviceId = await deviceRepository.GetOrCreateDeviceIdAsync(
            request.SensorId,
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

        command.Parameters.AddWithValue("deviceId", deviceId);
        command.Parameters.AddWithValue("timestamp", RepositoryMapping.ToUtcDateTime(timestamp));
        command.Parameters.AddWithValue("cropType", RepositoryMapping.DbString(request.CropType));
        command.Parameters.AddWithValue("soilMoisturePercent", request.SoilMoisturePercent);
        command.Parameters.AddWithValue("soilPh", request.SoilPh);
        command.Parameters.AddWithValue("temperatureC", request.TemperatureC);
        command.Parameters.AddWithValue("rainfallMm", request.RainfallMm);
        command.Parameters.AddWithValue("humidityPercent", request.HumidityPercent);
        command.Parameters.AddWithValue("sunlightHours", request.SunlightHours);
        command.Parameters.AddWithValue("irrigationType", RepositoryMapping.DbString(request.IrrigationType));
        command.Parameters.AddWithValue("fertilizerType", RepositoryMapping.DbString(request.FertilizerType));
        command.Parameters.AddWithValue("pesticideUsageMl", request.PesticideUsageMl);
        command.Parameters.AddWithValue("sowingDate", RepositoryMapping.DbValue(sowingDate));
        command.Parameters.AddWithValue("harvestDate", RepositoryMapping.DbValue(harvestDate));
        command.Parameters.AddWithValue("totalDays", request.TotalDays);
        command.Parameters.AddWithValue("yieldKgPerHectare", request.YieldKgPerHectare);
        command.Parameters.AddWithValue("ndviIndex", request.NdviIndex);
        command.Parameters.AddWithValue("cropDiseaseStatus", RepositoryMapping.DbString(request.CropDiseaseStatus));

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

    public async Task<IReadOnlyList<SelectiveReadingMessage>> GetSelectiveAsync(
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
        var readings = new List<SelectiveReadingMessage>();
        while (await reader.ReadAsync(cancellationToken))
        {
            readings.Add(MapSelectiveReading(reader, fields));
        }

        return readings;
    }

    private static SensorReadingMessage MapReading(NpgsqlDataReader reader)
    {
        return new SensorReadingMessage
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
            DeviceId = reader.GetGuid(reader.GetOrdinal("device_id")).ToString(),
            Timestamp = RepositoryMapping.ReadTimestamp(reader, "timestamp"),
            CropType = RepositoryMapping.ReadString(reader, "crop_type"),
            SoilMoisturePercent = RepositoryMapping.ReadDouble(reader, "soil_moisture_percent"),
            SoilPh = RepositoryMapping.ReadDouble(reader, "soil_ph"),
            TemperatureC = RepositoryMapping.ReadDouble(reader, "temperature_c"),
            RainfallMm = RepositoryMapping.ReadDouble(reader, "rainfall_mm"),
            HumidityPercent = RepositoryMapping.ReadDouble(reader, "humidity_percent"),
            SunlightHours = RepositoryMapping.ReadDouble(reader, "sunlight_hours"),
            IrrigationType = RepositoryMapping.ReadString(reader, "irrigation_type"),
            FertilizerType = RepositoryMapping.ReadString(reader, "fertilizer_type"),
            PesticideUsageMl = RepositoryMapping.ReadDouble(reader, "pesticide_usage_ml"),
            SowingDate = RepositoryMapping.ReadDate(reader, "sowing_date"),
            HarvestDate = RepositoryMapping.ReadDate(reader, "harvest_date"),
            TotalDays = RepositoryMapping.ReadInt32(reader, "total_days"),
            YieldKgPerHectare = RepositoryMapping.ReadDouble(reader, "yield_kg_per_hectare"),
            NdviIndex = RepositoryMapping.ReadDouble(reader, "ndvi_index"),
            CropDiseaseStatus = RepositoryMapping.ReadString(reader, "crop_disease_status")
        };
    }

    private static SelectiveReadingMessage MapSelectiveReading(NpgsqlDataReader reader, IReadOnlyList<string> fields)
    {
        var message = new SelectiveReadingMessage();
        foreach (var field in fields)
        {
            switch (field)
            {
                case "timestamp":
                    message.Timestamp = RepositoryMapping.ReadTimestamp(reader, field);
                    break;
                case "temperatureC":
                    message.TemperatureC = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "humidityPercent":
                    message.HumidityPercent = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "soilMoisturePercent":
                    message.SoilMoisturePercent = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "soilPh":
                    message.SoilPh = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "rainfallMm":
                    message.RainfallMm = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "sunlightHours":
                    message.SunlightHours = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "ndviIndex":
                    message.NdviIndex = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "yieldKgPerHectare":
                    message.YieldKgPerHectare = RepositoryMapping.ReadDouble(reader, field);
                    break;
                case "cropDiseaseStatus":
                    message.CropDiseaseStatus = RepositoryMapping.ReadString(reader, field);
                    break;
            }
        }

        return message;
    }
}
