using System.Data;
using System.Diagnostics;
using SQL_prototipo.Models;

namespace SQL_prototipo.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly IDbProvider _provider;

    public DatabaseService(string connectionString, string databaseType = "SQLite")
    {
        _connectionString = connectionString;
        _provider = DbProviderFactory.Create(databaseType);
    }

    public DatabaseService(ConnectionInfo connection)
        : this(connection.ConnectionString, connection.DatabaseType)
    {
    }

    private System.Data.Common.DbConnection CreateConnection() => _provider.CreateConnection(_connectionString);

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return true;
    }

    public async Task<List<string>> GetAllTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = new List<string>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = _provider.GetListTablesSql();

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    public async Task<List<(string Name, string Type)>> GetColumnsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var columns = new List<(string, string)>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = _provider.GetListColumnsSql(tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
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
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (reader.FieldCount > 0)
        {
            var dataTable = new DataTable();
            dataTable.Load(reader);
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
}

