using SQL_prototipo.Models;
using System.Data;
using System.Diagnostics;
using OB.DataAccess.Providers;

namespace SQL_prototipo.Services;

public class DatabaseService(string connectionString, string databaseType = "SqlServer")
{
    private readonly string _connectionString = connectionString;
    private readonly IDbProvider _provider = DbProviderFactory.Create(databaseType);

    public DatabaseService(ConnectionInfo connection)
        : this(connection.ConnectionString, connection.DatabaseType)
    {
    }

    private System.Data.Common.DbConnection CreateConnection() => _provider.CreateConnection(_connectionString);

    /// <summary>
    /// Returns the starting text used when opening a new, empty query tab,
    /// as defined by the active database provider.
    /// </summary>
    public string GetNewQueryTemplate() => _provider.GetNewQueryTemplate();

    /// <summary>
    /// Indicates whether the active database provider organizes tables into schemas.
    /// </summary>
    public bool SupportsSchemas => _provider.SupportsSchemas;

    /// <summary>
    /// Returns the schema-qualified, quoted identifier for a table, ready to be
    /// embedded in a query for the active provider.
    /// </summary>
    public string QualifyTableName(string schema, string tableName) =>
        _provider.QualifyTableName(schema, tableName);

    /// <summary>
    /// Builds a "select first N rows" query for the given table using the
    /// active provider's row-limiting syntax (TOP vs LIMIT).
    /// </summary>
    public string BuildSelectTopQuery(string schema, string tableName, int rowCount)
    {
        var qualified = _provider.QualifyTableName(schema, tableName);
        return _provider.BuildSelectTopQuery(qualified, rowCount);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<List<TableRef>> GetAllTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = new List<TableRef>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = _provider.GetListTablesSql();

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.FieldCount > 1 && !reader.IsDBNull(0)
                ? reader.GetValue(0)?.ToString() ?? string.Empty
                : string.Empty;
            var name = reader.FieldCount > 1
                ? (reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty)
                : (reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty);
            tables.Add(new TableRef(schema, name));
        }
        return tables;
    }

    public async Task<List<(string Name, string Type)>> GetColumnsAsync(string schema, string tableName, CancellationToken cancellationToken = default)
    {
        var columns = new List<(string, string)>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = _provider.GetListColumnsSql(schema, tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty;
            var type = reader.FieldCount > 1 && !reader.IsDBNull(1) ? reader.GetValue(1)?.ToString() ?? string.Empty : string.Empty;
            columns.Add((name, type));
        }
        return columns;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (reader.FieldCount > 0)
        {
            var dataTable = await LoadDataTableAsync(reader, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            return new QueryResult
            {
                Data = dataTable,
                HasResultSet = true,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }

        stopwatch.Stop();
        return new QueryResult
        {
            HasResultSet = false,
            RecordsAffected = reader.RecordsAffected,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Reads the entire result set of <paramref name="reader"/> into a
    /// <see cref="DataTable"/> using asynchronous row reads, so large result
    /// sets don't block while data is being pulled from the provider.
    /// </summary>
    private static async Task<DataTable> LoadDataTableAsync(
        System.Data.Common.DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var dataTable = new DataTable();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i) ?? typeof(object));
        }

        var values = new object[reader.FieldCount];
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            reader.GetValues(values);
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }
}

