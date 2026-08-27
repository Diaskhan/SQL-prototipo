using Antlr4.Runtime;
using Antlr4C3;
using Antlr4C3.Grammars;

namespace SQL_prototipo.Controls;

/// <summary>
/// Grammar-driven completion context computed with the antlr4-c3
/// <see cref="CodeCompletionCore"/> over the generated SQLite grammar.
/// It reports which keyword literals are valid at the caret and whether the
/// caret position expects a table or a column (mapped from preferred rules).
/// </summary>
public sealed class AntlrSqlAnalysis
{
    public AntlrSqlAnalysis(IReadOnlyList<string> keywords, bool suggestTables, bool suggestColumns)
    {
        Keywords = keywords;
        SuggestTables = suggestTables;
        SuggestColumns = suggestColumns;
    }

    /// <summary>SQL keyword literals that are grammatically valid at the caret.</summary>
    public IReadOnlyList<string> Keywords { get; }

    /// <summary>True when a table name is expected at the caret (FROM/JOIN/...).</summary>
    public bool SuggestTables { get; }

    /// <summary>True when a column name is expected at the caret.</summary>
    public bool SuggestColumns { get; }
}

/// <summary>
/// Uses <see cref="CodeCompletionCore"/> with the SQLite ANTLR grammar to
/// determine, for a given text and caret offset, the set of valid keywords and
/// the expected identifier category (table vs column). This replaces the
/// hand-written keyword/context heuristics with grammar-accurate results.
/// </summary>
public sealed class AntlrSqlAnalyzer
{
    // Rules that represent identifier positions we resolve against the schema.
    private static readonly HashSet<int> TableRules = new()
    {
        SQLiteParser.RULE_table_name,
        SQLiteParser.RULE_schema_name,
        SQLiteParser.RULE_table_or_index_name,
    };

    private static readonly HashSet<int> ColumnRules = new()
    {
        SQLiteParser.RULE_column_name,
        SQLiteParser.RULE_column_name_excluding_string,
    };

    private static readonly HashSet<int> PreferredRules = new(TableRules.Concat(ColumnRules));

    /// <summary>
    /// Analyzes <paramref name="text"/> at <paramref name="caret"/> and returns the
    /// grammar-derived completion context, or <c>null</c> if analysis fails.
    /// </summary>
    public AntlrSqlAnalysis? Analyze(string text, int caret)
    {
        try
        {
            var lexer = new SQLiteLexer(new AntlrInputStream(text ?? string.Empty));
            lexer.RemoveErrorListeners();

            var tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();

            var parser = new SQLiteParser(tokenStream);
            parser.RemoveErrorListeners();

            var tree = parser.parse();

            int caretTokenIndex = ComputeTokenIndex(tokenStream, caret);

            var core = new CodeCompletionCore(parser)
            {
                preferredRules = PreferredRules,
            };

            var candidates = core.CollectCandidates(caretTokenIndex, tree);

            var keywords = new List<string>();
            var vocabulary = parser.Vocabulary;
            foreach (var tokenType in candidates.Tokens.Keys)
            {
                string? keyword = ToKeyword(vocabulary.GetLiteralName(tokenType));
                if (keyword != null)
                {
                    keywords.Add(keyword);
                }
            }

            bool suggestTables = candidates.Rules.Keys.Any(TableRules.Contains);
            bool suggestColumns = candidates.Rules.Keys.Any(ColumnRules.Contains);

            return new AntlrSqlAnalysis(keywords, suggestTables, suggestColumns);
        }
        catch
        {
            // Incomplete/invalid SQL can throw during analysis; return null.
            return null;
        }
    }

    /// <summary>
    /// Maps a character caret offset to the token stream index expected by
    /// <see cref="CodeCompletionCore.CollectCandidates"/>. Returns the token that
    /// contains the caret (when typing a partial word) or the token that follows it.
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

            // Caret is inside or at the end of this token (partial identifier/keyword).
            if (caret > token.StartIndex && caret <= token.StopIndex + 1)
            {
                return token.TokenIndex;
            }

            // Caret is before this token (e.g. in trailing whitespace) -> complete here.
            if (token.StartIndex >= caret)
            {
                return token.TokenIndex;
            }
        }

        return tokens.Count > 0 ? tokens[tokens.Count - 1].TokenIndex : 0;
    }

    /// <summary>
    /// Converts a vocabulary literal name (e.g. <c>'SELECT'</c>) to a bare keyword
    /// string, or returns <c>null</c> for punctuation/operators that are not keywords.
    /// </summary>
    private static string? ToKeyword(string? literalName)
    {
        if (string.IsNullOrEmpty(literalName))
        {
            return null;
        }

        string value = literalName!.Trim('\'');
        if (value.Length == 0)
        {
            return null;
        }

        // Keep only alphabetic keyword literals (skip operators like '(', ',', '=').
        foreach (char c in value)
        {
            if (!char.IsLetter(c) && c != '_')
            {
                return null;
            }
        }

        return value.ToUpperInvariant();
    }
}
