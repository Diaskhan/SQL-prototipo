using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SqlGrammar = Antlr4C3.Grammars;

namespace SQL_prototipo.Services;

/// <summary>
/// Encapsulates everything needed to run antlr4-c3 autocomplete against the
/// generated Microsoft SQL Server (T-SQL) grammar: how to build the parser and
/// run the entry rule, plus the ignored tokens and preferred rules used to tune
/// the candidate collection.
/// </summary>
public sealed class TSqlGrammarProvider
{
    /// <summary>Tokens the completion core should never surface as candidates.</summary>
    public ISet<int> IgnoredTokens { get; } = BuildIgnoredTokens();

    /// <summary>Object-name / identifier rules the completion core should prefer.</summary>
    public ISet<int> PreferredRules { get; } = new HashSet<int>
    {
        // Object-name rules we DO want to collect completions for.
        SqlGrammar.TSqlParser.RULE_table_name,
        SqlGrammar.TSqlParser.RULE_full_table_name,
        SqlGrammar.TSqlParser.RULE_simple_name,
        SqlGrammar.TSqlParser.RULE_full_column_name,
        SqlGrammar.TSqlParser.RULE_column_name_list,
        SqlGrammar.TSqlParser.RULE_insert_column_id,
        SqlGrammar.TSqlParser.RULE_cursor_name,
        SqlGrammar.TSqlParser.RULE_scalar_function_name,
        SqlGrammar.TSqlParser.RULE_func_proc_name_schema,
        SqlGrammar.TSqlParser.RULE_func_proc_name_database_schema,
        SqlGrammar.TSqlParser.RULE_func_proc_name_server_database_schema,
        SqlGrammar.TSqlParser.RULE_as_column_alias,
        SqlGrammar.TSqlParser.RULE_column_alias,

        // Generic identifier / keyword rules: visited so their tokens resolve
        // through these rules instead of being offered as raw keyword candidates.
        SqlGrammar.TSqlParser.RULE_id_,
        SqlGrammar.TSqlParser.RULE_simple_id,
        SqlGrammar.TSqlParser.RULE_id_or_string,
        SqlGrammar.TSqlParser.RULE_keyword,
    };

    /// <summary>Builds a parser, runs the entry rule and returns it with its token stream.</summary>
    public (Parser Parser, CommonTokenStream Tokens) Parse(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new SqlGrammar.TSqlLexer(inputStream);
        lexer.RemoveErrorListeners();

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SqlGrammar.TSqlParser(tokenStream);
        parser.RemoveErrorListeners();

        parser.tsql_file();

        return (parser, tokenStream);
    }

    /// <summary>
    /// Parses <paramref name="code"/> and returns the tables referenced in its
    /// FROM / JOIN clauses (with schema and alias). Used to scope column
    /// completions to the tables actually present in the query.
    /// </summary>
    public IReadOnlyList<TableReference> CollectReferencedTables(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new SqlGrammar.TSqlLexer(inputStream);
        lexer.RemoveErrorListeners();

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SqlGrammar.TSqlParser(tokenStream);
        parser.RemoveErrorListeners();

        var tree = parser.tsql_file();

        var collector = new TSqlTableCollector();
        ParseTreeWalker.Default.Walk(collector, tree);

        return collector.Tables;
    }

    // Ignore whitespace/comments, EOF, every operator token (except STAR, still
    // wanted for "SELECT *") and every built-in function-name token.
    private static HashSet<int> BuildIgnoredTokens()
    {
        var tokens = new HashSet<int>
        {
            TokenConstants.EOF,
            SqlGrammar.TSqlLexer.SPACE,
            SqlGrammar.TSqlLexer.COMMENT,
            SqlGrammar.TSqlLexer.LINE_COMMENT,
        };

        // Operators: EQUAL .. PLACEHOLDER form a contiguous block in the lexer.
        for (int token = SqlGrammar.TSqlLexer.EQUAL; token <= SqlGrammar.TSqlLexer.PLACEHOLDER; token++)
        {
            if (token != SqlGrammar.TSqlLexer.STAR)
            {
                tokens.Add(token);
            }
        }

        // Built-in functions: ABS .. SP_EXECUTESQL form a contiguous block.
        for (int token = SqlGrammar.TSqlLexer.ABS; token <= SqlGrammar.TSqlLexer.SP_EXECUTESQL; token++)
        {
            tokens.Add(token);
        }

        return tokens;
    }
}
