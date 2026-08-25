using System.Data.Common;
using MySqlConnector;

namespace SQL_prototipo.Services.Providers;

public class MySqlProvider : IDbProvider
{
    public string DatabaseType => "MySQL";

    public bool SupportsSchemas => false;

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public string GetListTablesSql() =>
        "SELECT '' AS schema_name, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
        "WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME;";

    public string GetListColumnsSql(string schema, string tableName) =>
        "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
        $"WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName.Replace("'", "''")}' ORDER BY ORDINAL_POSITION;";

    public string QualifyTableName(string schema, string tableName) => $"`{tableName}`";

    public string BuildSelectTopQuery(string qualifiedTableName, int rowCount) =>
        $"SELECT * FROM {qualifiedTableName} LIMIT {rowCount}";

    public string GetNewQueryTemplate() => "SELECT ";
}
