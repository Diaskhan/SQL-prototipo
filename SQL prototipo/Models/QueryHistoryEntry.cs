namespace SQL_prototipo.Models;

/// <summary>
/// A single entry in the query execution history.
/// </summary>
public class QueryHistoryEntry
{
    public string Query { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.Now;
    public bool Success { get; set; }

    public QueryHistoryEntry()
    {
    }

    public QueryHistoryEntry(string query, string connectionName, bool success)
    {
        Query = query;
        ConnectionName = connectionName;
        Success = success;
        ExecutedAt = DateTime.Now;
    }

    public override string ToString()
    {
        var status = Success ? "OK" : "ERR";
        var singleLine = Query.Replace("\r", " ").Replace("\n", " ").Trim();
        if (singleLine.Length > 60)
            singleLine = singleLine.Substring(0, 60) + "…";
        return $"[{ExecutedAt:HH:mm:ss}] ({status}) {singleLine}";
    }
}
