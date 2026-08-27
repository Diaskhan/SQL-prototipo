namespace SQL_prototipo.Controls;

/// <summary>The kind of a completion suggestion, used to pick its icon.</summary>
public enum CompletionKind
{
    Keyword,
    Table,
    Column
}

/// <summary>A single suggestion shown in the autocomplete popup.</summary>
public sealed class CompletionItem
{
    public CompletionItem(string text, CompletionKind kind)
    {
        Text = text;
        Kind = kind;
    }

    public string Text { get; }
    public CompletionKind Kind { get; }

    public override string ToString() => Text;
}

/// <summary>
/// Thin, UI-agnostic wrapper over the antlr4-c3 <see cref="CodeCompletionCore"/>
/// (through <see cref="AntlrSqlAnalyzer"/>). It exposes only the grammar-derived
/// keyword candidates for a given text + caret position; it does not merge in any
/// schema data or apply hand-written completion heuristics.
/// </summary>
public sealed class SqlCompletionEngine
{
    private readonly AntlrSqlAnalyzer _analyzer = new();

    /// <summary>
    /// Returns completion candidates for the given text and caret position.
    /// <paramref name="replaceStart"/> is the offset where the current token
    /// begins (i.e. where a chosen suggestion should replace text up to caret).
    /// When <paramref name="force"/> is false and there is no current token,
    /// an empty list is returned.
    /// </summary>
    public IReadOnlyList<CompletionItem> GetSuggestions(string text, int caret, bool force, out int replaceStart)
    {
        string prefix = GetCurrentToken(text, caret, out replaceStart);
        if (!force && prefix.Length == 0)
        {
            return Array.Empty<CompletionItem>();
        }

        var analysis = _analyzer.Analyze(text, caret);
        if (analysis == null)
        {
            return Array.Empty<CompletionItem>();
        }

        var results = new List<CompletionItem>();
        AddMatching(results, analysis.Keywords, prefix, CompletionKind.Keyword);

        return results
            .GroupBy(r => r.Text, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(50)
            .ToList();
    }

    private static void AddMatching(List<CompletionItem> target, IEnumerable<string> source, string prefix, CompletionKind kind)
    {
        foreach (var item in source)
        {
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }
            if (prefix.Length == 0 || item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(new CompletionItem(item, kind));
            }
        }
    }

    // --- Caret / current-token helpers (editor positioning only) ---

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string GetCurrentToken(string text, int caret, out int start)
    {
        int i = caret;
        while (i > 0 && IsWordChar(text[i - 1]))
        {
            i--;
        }
        start = i;
        return text.Substring(i, caret - i);
    }
}
