using System.Collections.Generic;
using SqlGrammar = Antlr4C3.Grammars;

namespace SQL_prototipo.TSql;

/// <summary>
/// Walks a parsed T-SQL tree and records every table referenced in a
/// FROM / JOIN clause (via <c>table_source_item</c>), together with its schema
/// and alias. antlr4-c3 can tell us that a column is expected, but not which
/// tables are in scope; this listener supplies that missing information so
/// column suggestions (e.g. in WHERE) can be limited to the query's tables.
/// </summary>
public sealed class TSqlTableCollector : SqlGrammar.TSqlParserBaseListener
{
    private readonly List<TableReference> _tables = new();

    /// <summary>The tables discovered so far, in source order.</summary>
    public IReadOnlyList<TableReference> Tables => _tables;

    public override void EnterTable_source_item(
        SqlGrammar.TSqlParser.Table_source_itemContext context)
    {
        SqlGrammar.TSqlParser.Full_table_nameContext? fullTableName = context.full_table_name();
        if (fullTableName is null)
        {
            // Derived tables, function calls, OPENJSON, etc. have no plain name.
            return;
        }

        SqlGrammar.TSqlParser.Id_Context? tableId = fullTableName.table;
        if (tableId is null)
        {
            SqlGrammar.TSqlParser.Id_Context[] ids = fullTableName.id_();
            if (ids.Length == 0)
            {
                return;
            }

            tableId = ids[^1];
        }

        string name = Unquote(tableId.GetText());
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string schema = fullTableName.schema is { } schemaId
            ? Unquote(schemaId.GetText())
            : string.Empty;

        string? alias = context.as_table_alias()?.table_alias()?.GetText();
        alias = string.IsNullOrEmpty(alias) ? null : Unquote(alias);

        _tables.Add(new TableReference(schema, name, alias));
    }

    // Identifiers may be delimited with [ ], " " or ` `; strip those so names
    // match the plain names stored in the schema snapshot.
    private static string Unquote(string value) => value.Trim('[', ']', '"', '`');
}
