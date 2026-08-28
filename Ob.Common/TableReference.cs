namespace Ob.Common;

/// <summary>
/// A single table reference found in a SQL statement's <c>FROM</c>/<c>JOIN</c>
/// clauses, together with its optional schema and alias.
/// </summary>
public sealed class TableReference
{
    public TableReference(string table, string? schema, string? alias)
    {
        Table = table;
        Schema = schema;
        Alias = alias;
    }

    /// <summary>The table name as written in the query (unquoted).</summary>
    public string Table { get; }

    /// <summary>The schema/database qualifier (e.g. <c>main</c>), or null.</summary>
    public string? Schema { get; }

    /// <summary>The table alias (e.g. <c>u</c> in <c>users u</c>), or null.</summary>
    public string? Alias { get; }

    /// <summary>The identifier used to qualify columns: the alias if present, otherwise the table name.</summary>
    public string QualifyingName => string.IsNullOrEmpty(Alias) ? Table : Alias!;

    public override string ToString()
        => Alias is { Length: > 0 } ? $"{Table} AS {Alias}" : Table;
}
