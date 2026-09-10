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

    /// <summary>
    /// Maximum number of suggestions shown in the auto-completion dropdown (top N).
    /// </summary>
    public int MaxAutocompleteSuggestions { get; set; } = 25;
}
