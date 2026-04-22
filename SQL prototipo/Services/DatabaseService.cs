using Microsoft.Data.Sqlite;
using System.Data;

namespace SQL_prototipo.Services;

public class DatabaseService
{
    private readonly string _сonnectionString;

    public DatabaseService(string connectionString)
    {
        _сonnectionString = connectionString;
    }

    private SqliteConnection CreateConnection() => new SqliteConnection(_сonnectionString);

    public async Task<List<string>> GetAllTablesAsync()
    {
        var tables = new List<string>();
        using var connection = CreateConnection();
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    public async Task<DataTable> ExecuteQueryAsync(string query)
    {
        var dataTable = new DataTable();
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = await command.ExecuteReaderAsync();
        dataTable.Load(reader);
        return dataTable;
    }
}

