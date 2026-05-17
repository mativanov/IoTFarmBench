namespace IoTFarmBench.RestService.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public string Host { get; } = Get("DB_HOST", "localhost");
    public string Port { get; } = Get("DB_PORT", "5432");
    public string Database { get; } = Get("DB_NAME", "iotfarmbench");
    public string User { get; } = Get("DB_USER", "postgres");
    public string Password { get; } = Get("DB_PASSWORD", "postgres");

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={User};Password={Password}";

    private static string Get(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) ?? fallback;
}
