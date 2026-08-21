using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace SQL_prototipo.Services.Providers;

public class SqliteProvider : IDbProvider
{
    public string DatabaseType => "SQLite";

    public DbConnection CreateConnection(string connectionString) => new SqliteConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";

    public string GetListColumnsSql(string tableName) =>
        $"SELECT name, type FROM pragma_table_info('{tableName.Replace("'", "''")}');";
}
