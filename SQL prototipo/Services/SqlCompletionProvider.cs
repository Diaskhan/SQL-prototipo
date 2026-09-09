using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4C3;
using Antlr4C3.Grammars;

namespace SQL_prototipo.Services;

/// <summary>
/// Provides SQL keyword auto-completion suggestions by driving the ANTLR
/// <see cref="CodeCompletionCore"/> over the generated T-SQL grammar. Given the
/// current editor text and caret position, it returns the set of grammar-valid
/// keywords that may appear at the caret.
/// </summary>
public sealed class SqlCompletionProvider
{
    // Grammar rules that represent identifier positions (table/column names).
    // Marking them as "preferred" makes the core stop descending into them so we
    // don't get flooded with generic identifier-token noise at those spots.
    private static readonly HashSet<int> PreferredRules = new()
    {
        TSqlParser.RULE_table_name,
        TSqlParser.RULE_full_table_name,
        TSqlParser.RULE_column_name_list,
        TSqlParser.RULE_full_column_name,
    };

    /// <summary>
    /// Returns the distinct, sorted list of keyword completions valid at the
    /// given caret character offset within <paramref name="sql"/>.
    /// </summary>
    public IReadOnlyList<string> GetCompletions(string sql, int caretOffset)
    {
        sql ??= string.Empty;

        var input = new AntlrInputStream(sql);
        var lexer = new TSqlLexer(input);
        lexer.RemoveErrorListeners();

        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();

        var parser = new TSqlParser(tokenStream);
        parser.RemoveErrorListeners();
        var tree = parser.tsql_file();

        int caretTokenIndex = ComputeCaretTokenIndex(tokenStream, caretOffset);

        var core = new CodeCompletionCore(parser)
        {
            preferredRules = PreferredRules,
        };

        CodeCompletionCore.CandidatesCollection candidates =
            core.CollectCandidates(caretTokenIndex, tree);

        var vocabulary = parser.Vocabulary;
        var results = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (int tokenType in candidates.Tokens.Keys)
        {
            string? keyword = ToKeyword(vocabulary, tokenType);
            if (keyword != null)
            {
                results.Add(keyword);
            }
        }

        return results.ToList();
    }

    // Locates the index of the token the caret sits in (or the following token /
    // EOF when the caret is on whitespace) so the core knows where to resolve.
    private static int ComputeCaretTokenIndex(CommonTokenStream stream, int caretOffset)
    {
        IList<IToken> tokens = stream.GetTokens();
        if (tokens.Count == 0)
        {
            return 0;
        }

        // Zero-based index of the character immediately before the caret.
        int caretCharIndex = caretOffset - 1;

        foreach (IToken token in tokens)
        {
            if (token.Type == TokenConstants.EOF)
            {
                return token.TokenIndex;
            }

            if (token.Channel != TokenConstants.DefaultChannel)
            {
                continue;
            }

            // Caret is within (or right at the end of) this token.
            if (caretCharIndex >= token.StartIndex && caretCharIndex <= token.StopIndex)
            {
                return token.TokenIndex;
            }

            // Caret is before this token starts (e.g. on whitespace).
            if (token.StartIndex >= caretOffset)
            {
                return token.TokenIndex;
            }
        }

        return tokens[tokens.Count - 1].TokenIndex;
    }

    // Converts a token type into a keyword string, or null when the token has no
    // literal name or is not an alphabetic keyword (operators, punctuation, etc.).
    private static string? ToKeyword(IVocabulary vocabulary, int tokenType)
    {
        string? literal = vocabulary.GetLiteralName(tokenType);
        if (string.IsNullOrEmpty(literal))
        {
            return null;
        }

        // Literal names are quoted, e.g. "'SELECT'".
        string word = literal!.Trim('\'');
        if (word.Length == 0)
        {
            return null;
        }

        foreach (char c in word)
        {
            if (!char.IsLetter(c) && c != '_')
            {
                return null;
            }
        }

        return word.ToUpperInvariant();
    }
}
