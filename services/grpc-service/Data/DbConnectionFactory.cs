using Npgsql;

namespace IoTFarmBench.GrpcService.Data;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory()
    {
        var host = Get("DB_HOST", "localhost");
        var port = Get("DB_PORT", "5432");
        var database = Get("DB_NAME", "iotfarmbench");
        var user = Get("DB_USER", "postgres");
        var password = Get("DB_PASSWORD", "postgres");
        var poolMax = Get("DB_POOL_MAX", "30");

        _connectionString =
            $"Host={host};Port={port};Database={database};Username={user};Password={password};Maximum Pool Size={poolMax}";
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string Get(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) ?? fallback;
}
