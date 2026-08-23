using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SQL_prototipo.Services.Providers;

/// <summary>
/// Provider for SQL Server LocalDB. It shares the SQL Server T-SQL dialect and
/// the <see cref="SqlConnection"/> client; only the connection string differs
/// (e.g. "Server=(localdb)\\MSSQLLocalDB;Database=...;Integrated Security=true").
/// </summary>
public class LocalDbProvider : IDbProvider
{
    public string DatabaseType => "LocalDB";

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;";

    public string GetListColumnsSql(string tableName) =>
        "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
        $"WHERE TABLE_NAME = '{tableName.Replace("'", "''")}' ORDER BY ORDINAL_POSITION;";

    public string GetNewQueryTemplate() => "SELECT ";
}
