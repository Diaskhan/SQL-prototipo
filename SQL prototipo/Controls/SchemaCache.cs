using SQL_prototipo.Models;

namespace SQL_prototipo.Controls;

/// <summary>
/// Thread-safe cache of the active connection's tables and (lazily loaded)
/// columns, used to drive SQL auto-completion. Column lists are fetched on
/// demand through a loader delegate and cached; <see cref="Updated"/> is raised
/// whenever the tables change or a column list finishes loading.
/// </summary>
public sealed class SchemaCache
{
    private readonly object _sync = new();
    private List<TableRef> _tables = new();
    private readonly Dictionary<string, List<string>> _columns = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loading = new(StringComparer.OrdinalIgnoreCase);
    private Func<TableRef, Task<List<string>>>? _loader;

    /// <summary>Raised (possibly from a background thread) when cached data changes.</summary>
    public event Action? Updated;

    public void SetColumnLoader(Func<TableRef, Task<List<string>>> loader) => _loader = loader;

    public void SetTables(IEnumerable<TableRef> tables)
    {
        lock (_sync)
        {
            _tables = tables.ToList();
            _columns.Clear();
            _loading.Clear();
        }
        Updated?.Invoke();
    }

    public IReadOnlyList<string> TableNames
    {
        get
        {
            lock (_sync)
            {
                return _tables
                    .Select(t => t.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
    }

    /// <summary>Returns cached columns for a table, or null if not yet loaded.</summary>
    public List<string>? GetColumnsIfLoaded(string tableName)
    {
        lock (_sync)
        {
            return _columns.TryGetValue(tableName, out var cols) ? cols : null;
        }
    }

    /// <summary>Starts loading a table's columns in the background if needed.</summary>
    public void EnsureColumns(string tableName)
    {
        TableRef? tref;
        lock (_sync)
        {
            if (_loader == null || _columns.ContainsKey(tableName) || _loading.Contains(tableName))
            {
                return;
            }

            tref = _tables.FirstOrDefault(t => string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
            if (tref == null)
            {
                return;
            }
            _loading.Add(tableName);
        }

        _ = LoadColumnsAsync(tref);
    }

    private async Task LoadColumnsAsync(TableRef tref)
    {
        List<string> cols;
        try
        {
            cols = await _loader!(tref).ConfigureAwait(false);
        }
        catch
        {
            cols = new List<string>();
        }

        lock (_sync)
        {
            _columns[tref.Name] = cols;
            _loading.Remove(tref.Name);
        }
        Updated?.Invoke();
    }
}
