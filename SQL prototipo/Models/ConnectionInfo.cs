namespace SQL_prototipo.Models;

public class ConnectionInfo
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "SQLite"; // SQLite, SqlServer, MySQL, PostgreSQL, etc.

    public ConnectionInfo()
    {
    }

    public ConnectionInfo(string name, string connectionString, string databaseType = "SQLite")
    {
        Name = name;
        ConnectionString = connectionString;
        DatabaseType = databaseType;
    }

    public override string ToString() => Name;
}
