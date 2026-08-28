using Antlr4.Runtime.Tree;
using Antlr4C3.Grammars;

namespace Ob.Sqlite;

/// <summary>
/// A single table reference found in a SQL statement's <c>FROM</c>/<c>JOIN</c>
/// clauses, together with its optional schema and alias.
/// </summary>
public sealed class TableReference
{
    public TableReference(string table, string? schema, string? alias)
    {
        Table = table;
        Schema = schema;
        Alias = alias;
    }

    /// <summary>The table name as written in the query (unquoted).</summary>
    public string Table { get; }

    /// <summary>The schema/database qualifier (e.g. <c>main</c>), or null.</summary>
    public string? Schema { get; }

    /// <summary>The table alias (e.g. <c>u</c> in <c>users u</c>), or null.</summary>
    public string? Alias { get; }

    /// <summary>The identifier used to qualify columns: the alias if present, otherwise the table name.</summary>
    public string QualifyingName => string.IsNullOrEmpty(Alias) ? Table : Alias!;

    public override string ToString()
        => Alias is { Length: > 0 } ? $"{Table} AS {Alias}" : Table;
}

/// <summary>
/// Walks a SQLite parse tree and collects every table reference (name + alias)
/// that appears in <c>table_or_subquery</c> nodes, i.e. the tables in the
/// <c>FROM</c>/<c>JOIN</c> clauses. This provides the semantic "which tables are
/// in scope" information that <see cref="Antlr4C3.CodeCompletionCore"/> does not.
/// </summary>
public sealed class SqliteTableReferenceCollector : SQLiteParserBaseListener
{
    private readonly List<TableReference> _references = new();

    /// <summary>The collected table references, in the order they were parsed.</summary>
    public IReadOnlyList<TableReference> References => _references;

    /// <summary>Walks <paramref name="tree"/> and returns all table references found in it.</summary>
    public static IReadOnlyList<TableReference> Collect(IParseTree tree)
    {
        if (tree == null)
        {
            return Array.Empty<TableReference>();
        }

        var collector = new SqliteTableReferenceCollector();
        ParseTreeWalker.Default.Walk(collector, tree);
        return collector._references;
    }

    public override void EnterTable_or_subquery(SQLiteParser.Table_or_subqueryContext context)
    {
        // Only plain table references have a table_name; subqueries/table-valued
        // functions are skipped (their columns aren't resolved from the schema).
        var tableNameCtx = context.table_name();
        if (tableNameCtx == null)
        {
            return;
        }

        string table = Unquote(tableNameCtx.GetText());
        if (table.Length == 0)
        {
            return;
        }

        string? schema = context.schema_name() is { } s ? Unquote(s.GetText()) : null;

        string? alias = context.table_alias()?.GetText()
                        ?? context.table_alias_excluding_joins()?.GetText();
        alias = string.IsNullOrEmpty(alias) ? null : Unquote(alias!);

        _references.Add(new TableReference(table, schema, alias));
    }

    /// <summary>Strips SQLite identifier quoting: double quotes, backticks or square brackets.</summary>
    private static string Unquote(string identifier)
    {
        if (string.IsNullOrEmpty(identifier) || identifier.Length < 2)
        {
            return identifier ?? string.Empty;
        }

        char first = identifier[0];
        char last = identifier[identifier.Length - 1];

        bool quoted =
            (first == '"' && last == '"') ||
            (first == '`' && last == '`') ||
            (first == '[' && last == ']');

        if (!quoted)
        {
            return identifier;
        }

        string inner = identifier.Substring(1, identifier.Length - 2);

        // Un-escape doubled quotes/backticks ("" -> ", `` -> `).
        if (first == '"')
        {
            inner = inner.Replace("\"\"", "\"");
        }
        else if (first == '`')
        {
            inner = inner.Replace("``", "`");
        }

        return inner;
    }
}
