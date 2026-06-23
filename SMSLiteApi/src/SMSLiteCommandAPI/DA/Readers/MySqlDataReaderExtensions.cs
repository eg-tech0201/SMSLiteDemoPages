using MySqlConnector;

namespace SMSLiteCommandAPI.DA.Readers;

internal static class MySqlDataReaderExtensions
{
    public static int? NullableInt32(this MySqlDataReader reader, string name)
        => reader.TryOrdinal(name, out var ordinal) && !reader.IsDBNull(ordinal)
            ? Convert.ToInt32(reader.GetValue(ordinal))
            : null;

    public static short? NullableInt16(this MySqlDataReader reader, string name)
        => reader.TryOrdinal(name, out var ordinal) && !reader.IsDBNull(ordinal)
            ? Convert.ToInt16(reader.GetValue(ordinal))
            : null;

    public static DateTime? NullableDateTime(this MySqlDataReader reader, string name)
        => reader.TryOrdinal(name, out var ordinal) && !reader.IsDBNull(ordinal)
            ? reader.GetDateTime(ordinal)
            : null;

    public static string? NullableString(this MySqlDataReader reader, string name)
        => reader.TryOrdinal(name, out var ordinal) && !reader.IsDBNull(ordinal)
            ? Convert.ToString(reader.GetValue(ordinal))
            : null;

    private static bool TryOrdinal(this MySqlDataReader reader, string name, out int ordinal)
    {
        for (var index = 0; index < reader.FieldCount; index++)
        {
            if (string.Equals(reader.GetName(index), name, StringComparison.OrdinalIgnoreCase))
            {
                ordinal = index;
                return true;
            }
        }

        ordinal = -1;
        return false;
    }
}
