using Antlr4.Runtime;
using Antlr4C3;
using SQL_prototipo.Controls.Completion;

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
/// <see cref="CodeCompletionCore"/>. All grammar/dialect specifics (lexer,
/// parser, preferred rules, keyword mapping) are provided by an
/// <see cref="ISqlDialect"/>, so this engine itself is dialect-independent.
/// For a given text and caret it lexes/parses the input, asks the core which
/// tokens and rules are valid at the caret and returns the matching suggestions.
/// </summary>
public sealed class SqlCompletionEngine
{
    private readonly ISqlDialect _dialect;

    /// <summary>Creates an engine for the given SQL dialect (e.g. SQLite).</summary>
    public SqlCompletionEngine(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
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

        return CollectCandidates(text ?? string.Empty, caret)
            .Where(c => prefix.Length == 0 || c.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(c => c.Text, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(50)
            .ToList();
    }

    /// <summary>Runs CodeCompletionCore and returns the valid rule/keyword candidates.</summary>
    private IReadOnlyList<CompletionItem> CollectCandidates(string text, int caret)
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

            // Emit candidate rules first (tables, columns, aliases, ...)...
            foreach (var ruleIndex in candidates.Rules.Keys)
            {
                if (ruleIndex < 0 || ruleIndex >= ruleNames.Length)
                {
                    continue;
                }

                var kind = _dialect.MapRule(ruleIndex);
                if (kind is CompletionKind k)
                {
                    items.Add(new CompletionItem(ruleNames[ruleIndex], k));
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
}
