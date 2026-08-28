using Antlr4.Runtime;
using Antlr4C3;
using Antlr4C3.Grammars;

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
/// UI-agnostic SQL completion engine driven purely by the antlr4-c3
/// <see cref="CodeCompletionCore"/> over the generated SQLite grammar.
/// For a given text and caret it lexes/parses the input, asks the core which
/// tokens are valid at the caret and returns the matching SQL keywords.
/// </summary>
public sealed class SqlCompletionEngine
{
    /// <summary>
    /// The 30 most commonly needed SQLite grammar rules for completion. These
    /// tell <see cref="CodeCompletionCore"/> to surface schema/identifier
    /// candidates (tables, columns, functions, aliases, etc.) in addition to
    /// bare keyword tokens.
    /// </summary>
    private static readonly HashSet<int> PreferredRules = new()
    {
        SQLiteParser.RULE_table_name,
        SQLiteParser.RULE_column_name,
        SQLiteParser.RULE_column_name_excluding_string,
        SQLiteParser.RULE_schema_name,
        SQLiteParser.RULE_function_name,
        SQLiteParser.RULE_table_alias,
        SQLiteParser.RULE_column_alias,
        SQLiteParser.RULE_table_or_index_name,
        SQLiteParser.RULE_table_or_subquery,
        SQLiteParser.RULE_index_name,
        SQLiteParser.RULE_view_name,
        SQLiteParser.RULE_trigger_name,
        SQLiteParser.RULE_collation_name,
        SQLiteParser.RULE_foreign_table,
        SQLiteParser.RULE_pragma_name,
        SQLiteParser.RULE_module_name,
        SQLiteParser.RULE_savepoint_name,
        SQLiteParser.RULE_window_name,
        SQLiteParser.RULE_table_function_name,
        SQLiteParser.RULE_cte_table_name,
        SQLiteParser.RULE_qualified_table_name,
        SQLiteParser.RULE_result_column,
        SQLiteParser.RULE_ordering_term,
        SQLiteParser.RULE_indexed_column,
        SQLiteParser.RULE_column_def,
        SQLiteParser.RULE_type_name,
        SQLiteParser.RULE_literal_value,
        // RULE_expr is disabled: as the outermost preferred rule it shadows the
        // nested rules, so column_name/column_name_excluding_string never surface.
        //SQLiteParser.RULE_expr,
        SQLiteParser.RULE_alias,
        SQLiteParser.RULE_name,

    };

    /// <summary>
    /// Returns keyword completion candidates for the given text and caret.
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

        return CollectKeywords(text ?? string.Empty, caret)
            .Where(k => prefix.Length == 0 || k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .Select(k => new CompletionItem(k, CompletionKind.Keyword))
            .ToList();
    }

    /// <summary>Runs CodeCompletionCore and returns the valid SQL keyword literals.</summary>
    private static IReadOnlyList<string> CollectKeywords(string text, int caret)
    {
        try
        {
            var lexer = new SQLiteLexer(new AntlrInputStream(text));
            lexer.RemoveErrorListeners();

            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();

            var parser = new SQLiteParser(tokens);
            parser.RemoveErrorListeners();
            var tree = parser.parse();

            var core = new CodeCompletionCore(parser)
            {
                preferredRules = PreferredRules,
            };
            var candidates = core.CollectCandidates(ComputeTokenIndex(tokens, caret), tree);

            var vocabulary = parser.Vocabulary;
            var ruleNames = parser.RuleNames;
            var keywords = new List<string>();

            // Emit candidate rules first (tables, columns, aliases, ...)...
            foreach (var ruleIndex in candidates.Rules.Keys)
            {
                if (ruleIndex >= 0 && ruleIndex < ruleNames.Length)
                {
                    keywords.Add(ruleNames[ruleIndex]);
                }
            }

            // ...then the candidate keyword tokens.
            foreach (var tokenType in candidates.Tokens.Keys)
            {
                string? keyword = ToKeyword(vocabulary.GetLiteralName(tokenType));
                if (keyword != null)
                {
                    keywords.Add(keyword);
                }
            }

            return keywords;
        }
        catch
        {
            // Incomplete/invalid SQL can throw during analysis.
            return Array.Empty<string>();
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

    /// <summary>
    /// Converts a vocabulary literal name (e.g. <c>'SELECT'</c>) to a bare keyword,
    /// or returns <c>null</c> for operators/punctuation that are not keywords.
    /// </summary>
    private static string? ToKeyword(string? literalName)
    {
        if (string.IsNullOrEmpty(literalName))
        {
            return null;
        }

        string value = literalName!.Trim('\'');
        if (value.Length == 0 || !value.All(c => char.IsLetter(c) || c == '_'))
        {
            return null;
        }

        return value.ToUpperInvariant();
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
}
