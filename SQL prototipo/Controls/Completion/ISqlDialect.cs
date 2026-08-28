using Antlr4.Runtime;

namespace SQL_prototipo.Controls.Completion;

/// <summary>
/// Encapsulates everything that is specific to a concrete SQL grammar/dialect
/// (lexer, parser, entry rule, preferred rules, ignored tokens and how a fired
/// rule maps to a suggestion kind). This keeps the generic
/// <see cref="Antlr4C3.CodeCompletionCore"/> and <see cref="SqlCompletionEngine"/>
/// free of any dialect-specific knowledge.
/// </summary>
public interface ISqlDialect
{
    /// <summary>Lexes and prepares a parser and its filled token stream for the given text.</summary>
    (Parser Parser, CommonTokenStream Tokens) Parse(string text);

    /// <summary>Builds the parse tree from the grammar entry rule (e.g. <c>parser.parse()</c>).</summary>
    ParserRuleContext CreateParseTree(Parser parser);

    /// <summary>
    /// Collects the table references (name + optional schema/alias) that appear
    /// in the <c>FROM</c>/<c>JOIN</c> clauses of the parsed statement. This is the
    /// semantic scope used to resolve which columns are valid at the caret.
    /// </summary>
    IReadOnlyList<TableReference> CollectTableReferences(ParserRuleContext tree);

    /// <summary>Rules the completion core should surface (tables, columns, aliases, ...).</summary>
    ISet<int> PreferredRules { get; }

    /// <summary>Tokens the completion core should ignore when collecting candidates.</summary>
    ISet<int> IgnoredTokens { get; }

    /// <summary>
    /// Maps a fired grammar rule index to a suggestion kind, or <c>null</c> when
    /// the rule should not produce an identifier suggestion.
    /// </summary>
    CompletionKind? MapRule(int ruleIndex);

    /// <summary>
    /// Converts a vocabulary literal name (e.g. <c>'SELECT'</c>) into a bare keyword,
    /// or returns <c>null</c> for operators/punctuation that are not keywords.
    /// </summary>
    string? ToKeyword(string? literalName);
}
