namespace SQL_prototipo.Models;

/// <summary>
/// Application-wide user settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// When true, table nodes in the object tree expose their columns as child nodes.
    /// </summary>
    public bool ShowTableColumnsInTree { get; set; } = true;
}
