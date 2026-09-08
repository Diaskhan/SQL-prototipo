using OB.DataAccess.Providers;
using OB.DataAccess.Providers.mssql;
using OB.DataAccess.Providers.sqlite;

namespace OB.DataAccess.Providers;

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
            _ => new SqliteProvider()
        };
    }
}
