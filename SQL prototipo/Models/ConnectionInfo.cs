namespace SQL_prototipo.Models;

public class ConnectionInfo
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "SqlServer"; // SqlServer, LocalDB, etc.
    public string Group { get; set; } = "Default"; // Folder/Group for categorizing connections

    public ConnectionInfo()
    {
    }

    public ConnectionInfo(string name, string connectionString, string databaseType = "SqlServer", string group = "Default")
    {
        Name = name;
        ConnectionString = connectionString;
        DatabaseType = databaseType;
        Group = string.IsNullOrWhiteSpace(group) ? "Default" : group;
    }

    public override string ToString() => Name;
}
