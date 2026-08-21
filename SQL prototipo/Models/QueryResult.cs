using System.Data;

namespace SQL_prototipo.Models;

/// <summary>
/// Represents the outcome of executing a SQL statement, whether it returned
/// a result set (SELECT) or was a non-query (INSERT/UPDATE/DELETE/DDL).
/// </summary>
public class QueryResult
{
    /// <summary>Result set for queries that return rows; null for non-queries.</summary>
    public DataTable? Data { get; init; }

    /// <summary>Number of rows affected for non-query statements.</summary>
    public int RecordsAffected { get; init; }

    /// <summary>True when the statement returned a result set.</summary>
    public bool HasResultSet { get; init; }

    /// <summary>Execution time in milliseconds.</summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>Number of rows in the result set (0 for non-queries).</summary>
    public int RowCount => Data?.Rows.Count ?? 0;
}
