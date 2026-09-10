namespace SQL_prototipo.Models;

/// <summary>
/// Describes a table column as shown by the table structure editor.
/// </summary>
public sealed class TableColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}
