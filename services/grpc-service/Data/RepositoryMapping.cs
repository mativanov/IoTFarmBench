using System.Globalization;
using Npgsql;

namespace IoTFarmBench.GrpcService.Data;

internal static class RepositoryMapping
{
    public static object DbValue<T>(T? value) => value is null ? DBNull.Value : value;

    public static object DbString(string value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public static DateTime ToUtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    public static string ReadString(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    public static double ReadDouble(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetDouble(ordinal);
    }

    public static int ReadInt32(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    public static long ReadInt64(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt64(ordinal);
    }

    public static string ReadTimestamp(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        var value = DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
        return new DateTimeOffset(value).ToString("O", CultureInfo.InvariantCulture);
    }

    public static string ReadDate(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        return reader.GetFieldValue<DateOnly>(ordinal).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
