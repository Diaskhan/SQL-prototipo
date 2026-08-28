using Antlr4.Runtime;
using Antlr4C3.Grammars;

namespace SQL_prototipo.Controls.Completion;

/// <summary>
/// SQLite-specific <see cref="ISqlDialect"/> implementation. All references to
/// the generated <see cref="SQLiteLexer"/>/<see cref="SQLiteParser"/> and the
/// SQLite grammar rules live here.
/// </summary>
public sealed class SqliteDialect : ISqlDialect
{
    /// <summary>
    /// The most commonly needed SQLite grammar rules for completion. These tell
    /// the completion core to surface schema/identifier candidates (tables,
    /// columns, functions, aliases, etc.) in addition to bare keyword tokens.
    /// </summary>
    public ISet<int> PreferredRules { get; } = new HashSet<int>
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
        SQLiteParser.RULE_indexed_column,
        SQLiteParser.RULE_column_def,
        SQLiteParser.RULE_type_name,
        SQLiteParser.RULE_literal_value,
        SQLiteParser.RULE_alias,
        SQLiteParser.RULE_name,

        // Disabled rules: these wrap an expression (rule -> expr -> ... -> column_name),
        // so as outermost preferred rules they would shadow column_name and prevent
        // column suggestions in SELECT / ORDER BY / WHERE.
        //SQLiteParser.RULE_expr,
        //SQLiteParser.RULE_result_column,
        //SQLiteParser.RULE_ordering_term,
    };

    public ISet<int> IgnoredTokens { get; } = new HashSet<int>();

    public (Parser Parser, CommonTokenStream Tokens) Parse(string text)
    {
        var lexer = new SQLiteLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();

        var tokens = new CommonTokenStream(lexer);
        tokens.Fill();

        var parser = new SQLiteParser(tokens);
        parser.RemoveErrorListeners();

        return (parser, tokens);
    }

    public ParserRuleContext CreateParseTree(Parser parser)
        => ((SQLiteParser)parser).parse();

    public CompletionKind? MapRule(int ruleIndex) => ruleIndex switch
    {
        SQLiteParser.RULE_table_name or
        SQLiteParser.RULE_table_or_index_name or
        SQLiteParser.RULE_table_or_subquery or
        SQLiteParser.RULE_qualified_table_name or
        SQLiteParser.RULE_foreign_table or
        SQLiteParser.RULE_cte_table_name => CompletionKind.Table,

        SQLiteParser.RULE_column_name or
        SQLiteParser.RULE_column_name_excluding_string or
        SQLiteParser.RULE_indexed_column or
        SQLiteParser.RULE_column_def => CompletionKind.Column,

        _ => null,
    };

    public string? ToKeyword(string? literalName)
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
}
