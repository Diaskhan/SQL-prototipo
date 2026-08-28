using System.Data.Common;
using Microsoft.Data.SqlClient;
using Ob.Common;

namespace Ob.SqlServer;

/// <summary>
/// <see cref="IDbProvider"/> implementation for SQL Server LocalDB.
/// Shares the T-SQL dialect and <see cref="SqlConnection"/> client with
/// <see cref="SqlServerProvider"/>; only the connection string differs
/// (e.g. <c>Server=(localdb)\MSSQLLocalDB;Database=...;Integrated Security=true</c>).
/// </summary>
public sealed class LocalDbProvider : IDbProvider
{
    public string DatabaseType => "LocalDB";

    public bool SupportsSchemas => true;

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME;";

    public string GetListColumnsSql(string schema, string tableName)
    {
        var sql = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                  $"WHERE TABLE_NAME = '{tableName.Replace("'", "''")}'";
        if (!string.IsNullOrEmpty(schema))
        {
            sql += $" AND TABLE_SCHEMA = '{schema.Replace("'", "''")}'";
        }
        return sql + " ORDER BY ORDINAL_POSITION;";
    }

    public string QualifyTableName(string schema, string tableName) =>
        string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{tableName}]";

    public string BuildSelectTopQuery(string qualifiedTableName, int rowCount) =>
        $"SELECT TOP {rowCount} * FROM {qualifiedTableName}";

    public string GetNewQueryTemplate() => "SELECT ";
}
