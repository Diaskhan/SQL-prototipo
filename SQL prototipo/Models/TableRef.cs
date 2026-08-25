namespace SQL_prototipo.Models;

/// <summary>
/// Identifies a database table together with its (optional) schema.
/// For databases that do not support schemas (e.g. SQLite, MySQL) the
/// <see cref="Schema"/> is an empty string.
/// </summary>
public sealed class TableRef
{
    public TableRef(string schema, string name)
    {
        Schema = schema ?? string.Empty;
        Name = name ?? string.Empty;
    }

    /// <summary>The schema the table belongs to, or empty when not applicable.</summary>
    public string Schema { get; }

    /// <summary>The table name (unqualified).</summary>
    public string Name { get; }

    /// <summary>True when a non-empty schema is associated with this table.</summary>
    public bool HasSchema => !string.IsNullOrEmpty(Schema);
}
