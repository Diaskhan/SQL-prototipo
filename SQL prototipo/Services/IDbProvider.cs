using System.Data.Common;

namespace SQL_prototipo.Services;

/// <summary>
/// Abstracts database-specific behavior so the application can work with
/// SQLite, SQL Server, MySQL and PostgreSQL through a common interface.
/// </summary>
public interface IDbProvider
{
    /// <summary>
    /// The database type identifier this provider handles (e.g. "SQLite").
    /// </summary>
    string DatabaseType { get; }

    /// <summary>
    /// Creates a new (unopened) ADO.NET connection for this provider.
    /// </summary>
    DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Returns SQL that lists all user table names in a single column.
    /// </summary>
    string GetListTablesSql();

    /// <summary>
    /// Returns SQL that lists the columns (name, type) for the given table.
    /// The <paramref name="tableName"/> is expected to be a plain identifier.
    /// </summary>
    string GetListColumnsSql(string tableName);

    /// <summary>
    /// Returns the starting text used when opening a new, empty query tab.
    /// </summary>
    string GetNewQueryTemplate();
}
