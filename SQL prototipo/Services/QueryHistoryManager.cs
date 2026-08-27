using System.IO;
using System.Text.Json;
using SQL_prototipo.Models;

namespace SQL_prototipo.Services;

/// <summary>
/// Persists recently executed queries to a JSON file in the user's AppData folder.
/// </summary>
public class QueryHistoryManager
{
    private const int MaxEntries = 100;
    private List<QueryHistoryEntry> _entries;

    public QueryHistoryManager()
    {
        _entries = Load();
    }

    public IReadOnlyList<QueryHistoryEntry> Entries => _entries.AsReadOnly();

    public void Add(string query, string connectionName, bool success)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        _entries.Insert(0, new QueryHistoryEntry(query, connectionName, success));
        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        }
        Save();
    }

    public IReadOnlyList<QueryHistoryEntry> GetRecent() => _entries.AsReadOnly();

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    private List<QueryHistoryEntry> Load()
    {
        try
        {
            var path = GetHistoryPath();
            if (!File.Exists(path)) return new List<QueryHistoryEntry>();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new List<QueryHistoryEntry>();

            return JsonSerializer.Deserialize<List<QueryHistoryEntry>>(json) ?? new List<QueryHistoryEntry>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading query history: {ex.Message}");
            return new List<QueryHistoryEntry>();
        }
    }

    private void Save()
    {
        try
        {
            var path = GetHistoryPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving query history: {ex.Message}");
        }
    }

    private static string GetHistoryPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "SQL_prototipo", "SQL_prototipo", "query_history.json");
    }
}
