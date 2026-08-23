using SQL_prototipo.Services.Providers;

namespace SQL_prototipo.Services;

/// <summary>
/// Resolves an <see cref="IDbProvider"/> from a database type identifier.
/// </summary>
public static class DbProviderFactory
{
    public static IDbProvider Create(string databaseType)
    {
        return (databaseType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "sqlite" => new SqliteProvider(),
            "sqlserver" => new SqlServerProvider(),
            "localdb" => new LocalDbProvider(),
            "mysql" => new MySqlProvider(),
            "postgresql" => new PostgreSqlProvider(),
            _ => new SqliteProvider()
        };
    }
}
