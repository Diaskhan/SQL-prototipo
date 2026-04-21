using Microsoft.Data.Sqlite;
using System.Data;

namespace SQL_prototipo.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void OpenLocalDBConnection()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            // Connection opened successfully
        }
    }

    public List<string> GetAllTables()
    {
        var tables = new List<string>();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";

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
        using (var connection = new SqliteConnection(_connectionString))
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
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = command.ExecuteReader())
                {
                    // Добавить колонки
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                    }
                    // Добавить строки
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
}

