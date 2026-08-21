using System.Data.Common;
using Npgsql;

namespace SQL_prototipo.Services.Providers;

public class PostgreSqlProvider : IDbProvider
{
    public string DatabaseType => "PostgreSQL";

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT table_name FROM information_schema.tables " +
        "WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema') " +
        "ORDER BY table_name;";

    public string GetListColumnsSql(string tableName) =>
        "SELECT column_name, data_type FROM information_schema.columns " +
        $"WHERE table_name = '{tableName.Replace("'", "''")}' ORDER BY ordinal_position;";
}
