using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Data;

public static class DataReaderExtensions
{
    public static string GetStringValue(this DbDataReader reader, string columnName) => reader[columnName].ToString()!;

    public static string GetNullableString(this DbDataReader reader, string columnName, string defaultValue = "") =>
        reader[columnName] is DBNull ? defaultValue : reader[columnName].ToString()!;

    public static byte[]? GetNullableBytes(this DbDataReader reader, string columnName) =>
        reader[columnName] is DBNull ? null : (byte[])reader[columnName];

    public static int GetInt32Value(this DbDataReader reader, string columnName) => Convert.ToInt32(reader[columnName]);

    public static DateTime GetDateTimeValue(this DbDataReader reader, string columnName) => Convert.ToDateTime(reader[columnName]);

    public static DateOnly GetDateOnlyValue(this DbDataReader reader, string columnName) =>
        DateOnly.FromDateTime(Convert.ToDateTime(reader[columnName]));
}
