using IoTFarmBench.RestService.Infrastructure.Database;
using Npgsql;

namespace IoTFarmBench.RestService.Data;

public sealed class DbConnectionFactory(DatabaseOptions options)
{
    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
