using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SQL_prototipo.Services.Providers;

public class SqlServerProvider : IDbProvider
{
    public string DatabaseType => "SqlServer";

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;";

    public string GetListColumnsSql(string tableName) =>
        "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
        $"WHERE TABLE_NAME = '{tableName.Replace("'", "''")}' ORDER BY ORDINAL_POSITION;";
}
