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
    /// Indicates whether this database engine organizes tables into schemas
    /// (e.g. SQL Server "dbo", PostgreSQL "public"). SQLite and MySQL do not.
    /// </summary>
    bool SupportsSchemas { get; }

    /// <summary>
    /// Creates a new (unopened) ADO.NET connection for this provider.
    /// </summary>
    DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Returns SQL that lists all user tables. The first column is the schema
    /// name (empty when not applicable) and the second column is the table name.
    /// </summary>
    string GetListTablesSql();

    /// <summary>
    /// Returns SQL that lists the columns (name, type) for the given table.
    /// The <paramref name="schema"/> may be empty for schema-less databases.
    /// </summary>
    string GetListColumnsSql(string schema, string tableName);

    /// <summary>
    /// Returns the identifier used to reference a table inside a query,
    /// schema-qualified and quoted appropriately for this provider.
    /// </summary>
    string QualifyTableName(string schema, string tableName);

    /// <summary>
    /// Returns the starting text used when opening a new, empty query tab.
    /// </summary>
    string GetNewQueryTemplate();
}
