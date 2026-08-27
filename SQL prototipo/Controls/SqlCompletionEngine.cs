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
/// UI-agnostic engine that produces IntelliSense-like SQL completion candidates.
/// Suggestions combine SQL keywords with tables/columns pulled from a
/// <see cref="SchemaCache"/>. It is context aware: after FROM/JOIN it favours
/// tables, and after a known table name/alias it offers that table's columns.
/// The engine operates purely on text + caret position so it can be reused by
/// any editor host (WinForms, AvalonEdit, etc.).
/// </summary>
public sealed class SqlCompletionEngine
{
    private static readonly string[] Keywords =
    {
        "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE", "SET",
        "DELETE", "CREATE", "TABLE", "ALTER", "DROP", "JOIN", "INNER", "LEFT",
        "RIGHT", "FULL", "OUTER", "ON", "GROUP", "BY", "ORDER", "HAVING",
        "DISTINCT", "TOP", "LIMIT", "OFFSET", "AS", "AND", "OR", "NOT", "NULL",
        "IN", "LIKE", "BETWEEN", "IS", "EXISTS", "UNION", "ALL", "CASE", "WHEN",
        "THEN", "ELSE", "END", "ASC", "DESC", "COUNT", "SUM", "AVG", "MIN",
        "MAX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "INDEX", "VIEW"
    };

    private readonly SchemaCache _schema;

    public SqlCompletionEngine(SchemaCache schema)
    {
        _schema = schema;
    }

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

        return BuildCandidates(text, prefix, replaceStart);
    }

    private List<CompletionItem> BuildCandidates(string text, string prefix, int replaceStart)
    {
        var results = new List<CompletionItem>();
        string preceding = GetPrecedingKeyword(text, replaceStart);

        bool afterFrom = preceding is "FROM" or "JOIN" or "INTO" or "UPDATE";

        // Context: "alias." or "table." -> only that table's columns.
        string qualifier = GetQualifierBeforeDot(text, replaceStart);
        if (qualifier.Length > 0)
        {
            string resolved = ResolveTableName(text, qualifier);
            _schema.EnsureColumns(resolved);
            var cols = _schema.GetColumnsIfLoaded(resolved);
            if (cols != null)
            {
                AddMatching(results, cols, prefix, CompletionKind.Column);
            }
            return results;
        }

        if (afterFrom)
        {
            AddMatching(results, _schema.TableNames, prefix, CompletionKind.Table);
            return results;
        }

        // General context: tables, columns of referenced tables, and keywords.
        AddMatching(results, _schema.TableNames, prefix, CompletionKind.Table);

        foreach (var tableName in GetReferencedTables(text))
        {
            _schema.EnsureColumns(tableName);
            var cols = _schema.GetColumnsIfLoaded(tableName);
            if (cols != null)
            {
                AddMatching(results, cols, prefix, CompletionKind.Column);
            }
        }

        AddMatching(results, Keywords, prefix, CompletionKind.Keyword);
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

    // --- Token / context parsing helpers ---

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

    /// <summary>Returns the identifier immediately before a '.' preceding the token, or "".</summary>
    private static string GetQualifierBeforeDot(string text, int tokenStart)
    {
        int i = tokenStart;
        if (i <= 0 || text[i - 1] != '.')
        {
            return string.Empty;
        }
        i--; // skip the dot
        int end = i;
        while (i > 0 && IsWordChar(text[i - 1]))
        {
            i--;
        }
        return text.Substring(i, end - i);
    }

    private static string GetPrecedingKeyword(string text, int tokenStart)
    {
        int i = tokenStart;
        while (i > 0 && char.IsWhiteSpace(text[i - 1]))
        {
            i--;
        }
        int end = i;
        while (i > 0 && IsWordChar(text[i - 1]))
        {
            i--;
        }
        return text.Substring(i, end - i).ToUpperInvariant();
    }

    /// <summary>Finds table names referenced in FROM/JOIN clauses of the text.</summary>
    private IEnumerable<string> GetReferencedTables(string text)
    {
        var known = new HashSet<string>(_schema.TableNames, StringComparer.OrdinalIgnoreCase);
        var tokens = Tokenize(text);
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            string kw = tokens[i].ToUpperInvariant();
            if (kw is "FROM" or "JOIN")
            {
                // The table may be schema-qualified (e.g. [dbo].[Customer] ->
                // tokens "dbo","Customer"), so scan a small window after the
                // keyword for the first token that matches a known table.
                for (int j = i + 1; j < tokens.Count && j <= i + 3; j++)
                {
                    if (known.Contains(tokens[j]))
                    {
                        yield return tokens[j];
                        break;
                    }
                }
            }
        }
    }

    /// <summary>Resolves an alias or table name to an actual table name.</summary>
    private string ResolveTableName(string text, string qualifier)
    {
        var known = new HashSet<string>(_schema.TableNames, StringComparer.OrdinalIgnoreCase);
        if (known.Contains(qualifier))
        {
            return qualifier;
        }

        // Look for "table alias" or "table AS alias" patterns.
        var tokens = Tokenize(text);
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (!known.Contains(tokens[i]))
            {
                continue;
            }
            string next = tokens[i + 1];
            if (string.Equals(next, qualifier, StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i];
            }
            if (string.Equals(next, "AS", StringComparison.OrdinalIgnoreCase)
                && i + 2 < tokens.Count
                && string.Equals(tokens[i + 2], qualifier, StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i];
            }
        }
        return qualifier;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            if (IsWordChar(text[i]))
            {
                int start = i;
                while (i < text.Length && IsWordChar(text[i]))
                {
                    i++;
                }
                tokens.Add(text.Substring(start, i - start));
            }
            else
            {
                i++;
            }
        }
        return tokens;
    }
}
