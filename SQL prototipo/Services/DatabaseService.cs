using Microsoft.Data.Sqlite;

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

    public List<string> ExecuteQuery(string query)
    {
        var results = new List<string>();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = query;

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString()));
                    results.Add(row);
                }
            }
        }
        return results;
    }
}

