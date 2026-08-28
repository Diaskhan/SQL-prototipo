using System.Data.Common;
using Microsoft.Data.Sqlite;
using Ob.Common;

namespace Ob.Sqlite;

/// <summary>
/// <see cref="IDbProvider"/> implementation for SQLite databases.
/// </summary>
public sealed class SqliteProvider : IDbProvider
{
    public string DatabaseType => "SQLite";

    public bool SupportsSchemas => false;

    public DbConnection CreateConnection(string connectionString)
        => new SqliteConnection(connectionString);

    public string GetListTablesSql()
        => "SELECT '' AS schema_name, name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";

    public string GetListColumnsSql(string schema, string tableName)
        => $"SELECT name, type FROM pragma_table_info('{tableName.Replace("'", "''")}');";

    public string QualifyTableName(string schema, string tableName)
        => $"\"{tableName}\"";

    public string BuildSelectTopQuery(string qualifiedTableName, int rowCount)
        => $"SELECT * FROM {qualifiedTableName} LIMIT {rowCount}";

    public string GetNewQueryTemplate() => "SELECT ";
}
