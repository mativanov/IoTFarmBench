using IoTFarmBench.RestService.Models;
using Npgsql;

namespace IoTFarmBench.RestService.Data;

public sealed class AnalyticsRepository(DbConnectionFactory connectionFactory)
{
    public async Task<AnalyticsSummaryDto> GetSummaryAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? region,
        string? cropType,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT
                COUNT(*) AS count,
                AVG(sr.temperature_c) AS avg_temperature_c,
                AVG(sr.humidity_percent) AS avg_humidity_percent,
                AVG(sr.soil_moisture_percent) AS avg_soil_moisture_percent,
                AVG(sr.soil_ph) AS avg_soil_ph,
                AVG(sr.rainfall_mm) AS avg_rainfall_mm,
                AVG(sr.sunlight_hours) AS avg_sunlight_hours,
                AVG(sr.ndvi_index) AS avg_ndvi_index,
                AVG(sr.yield_kg_per_hectare) AS avg_yield_kg_per_hectare,
                MIN(sr.timestamp) AS min_timestamp,
                MAX(sr.timestamp) AS max_timestamp
            FROM sensor_readings sr
            JOIN devices d ON d.id = sr.device_id
            WHERE (@from::timestamptz IS NULL OR sr.timestamp >= @from)
              AND (@to::timestamptz IS NULL OR sr.timestamp <= @to)
              AND (@region::text IS NULL OR d.region = @region)
              AND (@cropType::text IS NULL OR sr.crop_type = @cropType)
            """,
            connection);

        command.Parameters.AddWithValue("from", RepositoryMapping.DbValue(from?.UtcDateTime));
        command.Parameters.AddWithValue("to", RepositoryMapping.DbValue(to?.UtcDateTime));
        command.Parameters.AddWithValue("region", RepositoryMapping.DbValue(region));
        command.Parameters.AddWithValue("cropType", RepositoryMapping.DbValue(cropType));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new AnalyticsSummaryDto(
            reader.GetInt64(reader.GetOrdinal("count")),
            RepositoryMapping.ReadNullable<double>(reader, "avg_temperature_c"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_humidity_percent"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_soil_moisture_percent"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_soil_ph"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_rainfall_mm"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_sunlight_hours"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_ndvi_index"),
            RepositoryMapping.ReadNullable<double>(reader, "avg_yield_kg_per_hectare"),
            RepositoryMapping.ReadNullableTimestamp(reader, "min_timestamp"),
            RepositoryMapping.ReadNullableTimestamp(reader, "max_timestamp"));
    }

    public async Task<IReadOnlyList<AggregatedReadingDto>> GetByRegionAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT
                d.region,
                COUNT(*) AS count,
                AVG(sr.temperature_c) AS avg_temperature_c,
                AVG(sr.humidity_percent) AS avg_humidity_percent,
                AVG(sr.soil_moisture_percent) AS avg_soil_moisture_percent,
                AVG(sr.ndvi_index) AS avg_ndvi_index,
                AVG(sr.yield_kg_per_hectare) AS avg_yield_kg_per_hectare
            FROM sensor_readings sr
            JOIN devices d ON d.id = sr.device_id
            GROUP BY d.region
            ORDER BY d.region NULLS LAST
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<AggregatedReadingDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AggregatedReadingDto(
                RepositoryMapping.ReadNullableString(reader, "region"),
                reader.GetInt64(reader.GetOrdinal("count")),
                RepositoryMapping.ReadNullable<double>(reader, "avg_temperature_c"),
                RepositoryMapping.ReadNullable<double>(reader, "avg_humidity_percent"),
                RepositoryMapping.ReadNullable<double>(reader, "avg_soil_moisture_percent"),
                RepositoryMapping.ReadNullable<double>(reader, "avg_ndvi_index"),
                RepositoryMapping.ReadNullable<double>(reader, "avg_yield_kg_per_hectare")));
        }

        return rows;
    }
}
