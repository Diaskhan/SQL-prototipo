using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Data.SqlClient;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void OpenLocalDBConnection()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
        }
    }

    public List<string> GetAllTables()
    {
        var tables = new List<string>();
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sys.tables WHERE name NOT LIKE 'sys%'";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
        }
        return tables;
    }

    public List<string> ExecuteQuery(string query, bool includeColumnNames = false)
    {
        var results = new List<string>();
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = query;

            using (var reader = command.ExecuteReader())
            {
                if (includeColumnNames)
                {
                    var columnNames = string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)));
                    results.Add(columnNames);
                }

                while (reader.Read())
                {
                    var row = string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString()));
                    results.Add(row);
                }
            }
        }
        return results;
    }

    public DataTable ExecuteQueryAsDataTable(string query)
    {
        var dataTable = new DataTable();
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = command.ExecuteReader())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                    }
                    while (reader.Read())
                    {
                        var row = dataTable.NewRow();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[i] = reader.GetValue(i);
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
        }
        return dataTable;
    }

    public async Task<List<string>> ExecuteQueryAsync(string query)
    {
        var results = new List<string>();
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => $"{reader.GetName(i)}: {reader.GetValue(i)}")
                    .ToList();
                results.Add(string.Join(", ", row));
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при выполнении запроса: {ex.Message}", ex);
        }
        return results;
    }

    public async Task<DataTable> GetDataTableAsync(string query)
    {
        var dataTable = new DataTable();
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при получении данных: {ex.Message}", ex);
        }
        return dataTable;
    }

    public async Task<List<string>> ExecuteQueryToListAsync(string query)
    {
        var results = new List<string>();
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new List<object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                values.Add(reader.GetValue(i));
            }
            results.Add(string.Join(", ", values));
        }
        return results;
    }
}