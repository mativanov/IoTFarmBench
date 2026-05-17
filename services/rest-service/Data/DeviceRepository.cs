using IoTFarmBench.RestService.Models;
using Npgsql;

namespace IoTFarmBench.RestService.Data;

public sealed class DeviceRepository(DbConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<DeviceDto>> GetAllAsync(CancellationToken cancellationToken)
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
        var devices = new List<DeviceDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            devices.Add(MapDevice(reader));
        }

        return devices;
    }

    public async Task<DeviceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
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
        string? farmId,
        string? region,
        double? latitude,
        double? longitude,
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
        command.Parameters.AddWithValue("farmId", RepositoryMapping.DbValue(farmId));
        command.Parameters.AddWithValue("region", RepositoryMapping.DbValue(region));
        command.Parameters.AddWithValue("latitude", RepositoryMapping.DbValue(latitude));
        command.Parameters.AddWithValue("longitude", RepositoryMapping.DbValue(longitude));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (Guid)(result ?? throw new InvalidOperationException("Device upsert did not return an id."));
    }

    private static DeviceDto MapDevice(NpgsqlDataReader reader)
    {
        return new DeviceDto(
            reader.GetGuid(reader.GetOrdinal("id")),
            reader.GetString(reader.GetOrdinal("sensor_id")),
            RepositoryMapping.ReadNullableString(reader, "farm_id"),
            RepositoryMapping.ReadNullableString(reader, "region"),
            RepositoryMapping.ReadNullable<double>(reader, "latitude"),
            RepositoryMapping.ReadNullable<double>(reader, "longitude"));
    }
}
