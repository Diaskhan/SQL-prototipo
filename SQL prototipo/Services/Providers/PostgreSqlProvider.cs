using System.Data.Common;
using Npgsql;

namespace SQL_prototipo.Services.Providers;

public class PostgreSqlProvider : IDbProvider
{
    public string DatabaseType => "PostgreSQL";

    public bool SupportsSchemas => true;

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT table_schema, table_name FROM information_schema.tables " +
        "WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema') " +
        "ORDER BY table_schema, table_name;";

    public string GetListColumnsSql(string schema, string tableName)
    {
        var sql = "SELECT column_name, data_type FROM information_schema.columns " +
                  $"WHERE table_name = '{tableName.Replace("'", "''")}'";
        if (!string.IsNullOrEmpty(schema))
        {
            sql += $" AND table_schema = '{schema.Replace("'", "''")}'";
        }
        return sql + " ORDER BY ordinal_position;";
    }

    public string QualifyTableName(string schema, string tableName) =>
        string.IsNullOrEmpty(schema)
            ? $"\"{tableName}\""
            : $"\"{schema}\".\"{tableName}\"";

    public string GetNewQueryTemplate() => "SELECT ";
}
