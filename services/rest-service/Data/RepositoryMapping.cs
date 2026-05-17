using Npgsql;

namespace IoTFarmBench.RestService.Data;

internal static class RepositoryMapping
{
    public static object DbValue<T>(T? value) => value is null ? DBNull.Value : value;

    public static DateTime ToUtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    public static DateTimeOffset ReadTimestamp(NpgsqlDataReader reader, string name)
    {
        var value = reader.GetDateTime(reader.GetOrdinal(name));
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public static DateTimeOffset? ReadNullableTimestamp(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public static DateOnly? ReadNullableDate(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateOnly>(ordinal);
    }

    public static T? ReadNullable<T>(NpgsqlDataReader reader, string name) where T : struct
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);
    }

    public static string? ReadNullableString(NpgsqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
