using IoTFarmBench.GrpcService.Protos;
using Npgsql;

namespace IoTFarmBench.GrpcService.Data;

public sealed class DeviceRepository(DbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<DeviceMessage>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, sensor_id, farm_id, region, latitude, longitude
            FROM devices
            ORDER BY sensor_id
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var devices = new List<DeviceMessage>();
        while (await reader.ReadAsync(cancellationToken))
        {
            devices.Add(MapDevice(reader));
        }

        return devices;
    }

    public async Task<DeviceMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, sensor_id, farm_id, region, latitude, longitude
            FROM devices
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapDevice(reader) : null;
    }

    public async Task<Guid> GetOrCreateDeviceIdAsync(
        string sensorId,
        string farmId,
        string region,
        double latitude,
        double longitude,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO devices (sensor_id, farm_id, region, latitude, longitude)
            VALUES (@sensorId, @farmId, @region, @latitude, @longitude)
            ON CONFLICT (sensor_id) DO UPDATE SET
                farm_id = COALESCE(EXCLUDED.farm_id, devices.farm_id),
                region = COALESCE(EXCLUDED.region, devices.region),
                latitude = COALESCE(EXCLUDED.latitude, devices.latitude),
                longitude = COALESCE(EXCLUDED.longitude, devices.longitude)
            RETURNING id
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("sensorId", sensorId);
        command.Parameters.AddWithValue("farmId", RepositoryMapping.DbString(farmId));
        command.Parameters.AddWithValue("region", RepositoryMapping.DbString(region));
        command.Parameters.AddWithValue("latitude", latitude);
        command.Parameters.AddWithValue("longitude", longitude);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (Guid)(result ?? throw new InvalidOperationException("Device upsert did not return an id."));
    }

    private static DeviceMessage MapDevice(NpgsqlDataReader reader)
    {
        return new DeviceMessage
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")).ToString(),
            SensorId = reader.GetString(reader.GetOrdinal("sensor_id")),
            FarmId = RepositoryMapping.ReadString(reader, "farm_id"),
            Region = RepositoryMapping.ReadString(reader, "region"),
            Latitude = RepositoryMapping.ReadDouble(reader, "latitude"),
            Longitude = RepositoryMapping.ReadDouble(reader, "longitude")
        };
    }
}
