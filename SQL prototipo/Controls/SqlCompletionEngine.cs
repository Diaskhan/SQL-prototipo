using Antlr4.Runtime;
using Antlr4C3;
using Ob.Sqlite;

namespace SQL_prototipo.Controls;

/// <summary>
/// UI-agnostic SQL completion engine driven purely by the antlr4-c3
/// <see cref="CodeCompletionCore"/>. All grammar/dialect specifics (lexer,
/// parser, preferred rules, keyword mapping) are provided by an
/// <see cref="ISqlDialect"/>, so this engine itself is dialect-independent.
/// For a given text and caret it lexes/parses the input, asks the core which
/// tokens and rules are valid at the caret and returns the matching suggestions.
/// </summary>
public sealed class SqlCompletionEngine
{
    private readonly ISqlDialect _dialect;
    private readonly SchemaCache? _schema;

    /// <summary>Creates an engine for the given SQL dialect (e.g. SQLite).</summary>
    /// <param name="dialect">The grammar/dialect providing lexer, parser and rule mapping.</param>
    /// <param name="schema">
    /// Optional schema cache used to turn <see cref="CompletionKind.Table"/>/
    /// <see cref="CompletionKind.Column"/> candidates into real table and column
    /// names. When null, only keyword suggestions are produced.
    /// </param>
    public SqlCompletionEngine(ISqlDialect dialect, SchemaCache? schema = null)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _schema = schema;
    }

    /// <summary>
    /// Returns completion candidates for the given text and caret.
    /// <paramref name="replaceStart"/> is the offset where the current word begins
    /// (where a chosen suggestion replaces text up to the caret). When
    /// <paramref name="force"/> is false and there is no current word, an empty
    /// list is returned.
    /// </summary>
    public IReadOnlyList<CompletionItem> GetSuggestions(string text, int caret, bool force, out int replaceStart)
    {
        string prefix = GetCurrentWord(text, caret, out replaceStart);
        if (!force && prefix.Length == 0)
        {
            return Array.Empty<CompletionItem>();
        }

        string? qualifier = GetQualifier(text ?? string.Empty, replaceStart);

        return CollectCandidates(text ?? string.Empty, caret, qualifier)
            .Where(c => prefix.Length == 0 || c.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(c => c.Text, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(50)
            .ToList();
    }

    /// <summary>Runs CodeCompletionCore and returns the valid rule/keyword candidates.</summary>
    private IReadOnlyList<CompletionItem> CollectCandidates(string text, int caret, string? qualifier)
    {
        try
        {
            var (parser, tokens) = _dialect.Parse(text);
            var tree = _dialect.CreateParseTree(parser);

            var core = new CodeCompletionCore(parser)
            {
                preferredRules = _dialect.PreferredRules,
                ignoredTokens = _dialect.IgnoredTokens,
            };
            var candidates = core.CollectCandidates(ComputeTokenIndex(tokens, caret), tree);

            var vocabulary = parser.Vocabulary;
            var ruleNames = parser.RuleNames;
            var items = new List<CompletionItem>();

            // Determine which identifier kinds the grammar allows at the caret.
            bool wantTable = false;
            bool wantColumn = false;
            foreach (var ruleIndex in candidates.Rules.Keys)
            {
                if (ruleIndex < 0 || ruleIndex >= ruleNames.Length)
                {
                    continue;
                }

                switch (_dialect.MapRule(ruleIndex))
                {
                    case CompletionKind.Table:
                        wantTable = true;
                        break;
                    case CompletionKind.Column:
                        wantColumn = true;
                        break;
                }
            }

            // Emit real schema identifiers (tables first, then columns)...
            if (_schema != null)
            {
                if (wantTable && qualifier == null)
                {
                    AddTableSuggestions(items);
                }

                if (wantColumn)
                {
                    AddColumnSuggestions(items, _dialect.CollectTableReferences(tree), qualifier);
                }
            }

            // ...then the candidate keyword tokens.
            foreach (var tokenType in candidates.Tokens.Keys)
            {
                string? keyword = _dialect.ToKeyword(vocabulary.GetLiteralName(tokenType));
                if (keyword != null)
                {
                    items.Add(new CompletionItem(keyword, CompletionKind.Keyword));
                }
            }

            return items;
        }
        catch
        {
            // Incomplete/invalid SQL can throw during analysis.
            return Array.Empty<CompletionItem>();
        }
    }

    /// <summary>Adds all known table names from the schema cache.</summary>
    private void AddTableSuggestions(List<CompletionItem> items)
    {
        foreach (var table in _schema!.TableNames)
        {
            items.Add(new CompletionItem(table, CompletionKind.Table));
        }
    }

    /// <summary>
    /// Adds column suggestions for the tables in scope. When a
    /// <paramref name="qualifier"/> (alias or table name before a dot) is present,
    /// only that table's columns are offered; otherwise the union of columns from
    /// all tables in the <c>FROM</c>/<c>JOIN</c> clauses is used.
    /// </summary>
    private void AddColumnSuggestions(
        List<CompletionItem> items,
        IReadOnlyList<TableReference> tableRefs,
        string? qualifier)
    {
        IEnumerable<string> tables;
        if (qualifier != null)
        {
            // Resolve the qualifier against aliases and table names in scope.
            var resolved = tableRefs
                .Where(t =>
                    string.Equals(t.Alias, qualifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Table, qualifier, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Table)
                .ToList();

            // Fall back to treating the qualifier itself as a table name
            // (e.g. "users." before a FROM clause exists).
            tables = resolved.Count > 0 ? resolved : new[] { qualifier };
        }
        else if (tableRefs.Count > 0)
        {
            tables = tableRefs.Select(t => t.Table);
        }
        else
        {
            // No FROM yet: fall back to every table's columns.
            tables = _schema!.TableNames;
        }

        foreach (var table in tables.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _schema!.EnsureColumns(table);
            var columns = _schema.GetColumnsIfLoaded(table);
            if (columns == null)
            {
                continue;
            }

            foreach (var column in columns)
            {
                items.Add(new CompletionItem(column, CompletionKind.Column));
            }
        }
    }

    /// <summary>
    /// Maps a character caret offset to the token stream index expected by
    /// <see cref="CodeCompletionCore.CollectCandidates"/>: the token that contains
    /// the caret (partial word) or the token that follows it.
    /// </summary>
    private static int ComputeTokenIndex(CommonTokenStream tokenStream, int caret)
    {
        var tokens = tokenStream.GetTokens();
        foreach (var token in tokens)
        {
            if (token.Type == TokenConstants.EOF)
            {
                return token.TokenIndex;
            }
            if ((caret > token.StartIndex && caret <= token.StopIndex + 1) || token.StartIndex >= caret)
            {
                return token.TokenIndex;
            }
        }
        return tokens.Count > 0 ? tokens[tokens.Count - 1].TokenIndex : 0;
    }

    /// <summary>Returns the word currently under the caret and its start offset.</summary>
    private static string GetCurrentWord(string text, int caret, out int start)
    {
        int i = caret;
        while (i > 0 && (char.IsLetterOrDigit(text[i - 1]) || text[i - 1] == '_'))
        {
            i--;
        }
        start = i;
        return text.Substring(i, caret - i);
    }

    /// <summary>
    /// Returns the qualifier immediately preceding the current word when the text
    /// looks like <c>qualifier.word</c> (e.g. <c>u.</c> or <c>users.</c>), or null.
    /// The qualifier is an alias or table name used to scope column suggestions.
    /// </summary>
    private static string? GetQualifier(string text, int wordStart)
    {
        int i = wordStart;
        if (i <= 0 || text[i - 1] != '.')
        {
            return null;
        }

        i--; // skip the dot
        int end = i;
        while (i > 0 && (char.IsLetterOrDigit(text[i - 1]) || text[i - 1] == '_'))
        {
            i--;
        }

        return end > i ? text.Substring(i, end - i) : null;
    }
}
