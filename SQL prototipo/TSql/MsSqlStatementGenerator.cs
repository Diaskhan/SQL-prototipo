using System.Text;
using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo.TSql;

/// <summary>
/// Generates MS SQL Server (T-SQL) DDL scripts (e.g. CREATE TABLE) from table metadata.
/// All T-SQL specific script generation lives under the <c>Services.TSql</c> folder/namespace.
/// </summary>
public sealed class MsSqlStatementGenerator
{
    private readonly DatabaseService _dbService;

    public MsSqlStatementGenerator(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Builds a <c>CREATE TABLE</c> script for the given table and its columns.
    /// A guard that creates the target schema (when it is missing) is prepended
    /// so the script can run against a database where the schema does not exist yet.
    /// </summary>
    public string BuildCreateTableScript(TableRef table, IReadOnlyList<TableColumnDefinition> columns)
    {
        var qualified = _dbService.QualifyTableName(table.Schema, table.Name);

        var builder = new StringBuilder();

        // Ensure the target schema exists before creating the table, otherwise
        // SQL Server fails with "The specified schema name ... does not exist".
        if (!string.IsNullOrWhiteSpace(table.Schema) &&
            !string.Equals(table.Schema, "dbo", StringComparison.OrdinalIgnoreCase))
        {
            var schemaLiteral = table.Schema.Replace("'", "''");
            builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schemaLiteral}')");
            builder.AppendLine($"    EXEC('CREATE SCHEMA [{table.Schema}]');");
            builder.AppendLine();
        }

        builder.AppendLine($"CREATE TABLE {qualified} (");

        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var line = $"    [{column.Name}] {column.DataType} {(column.IsNullable ? "NULL" : "NOT NULL")}";
            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
            {
                line += $" DEFAULT {column.DefaultValue}";
            }
            if (i < columns.Count - 1)
            {
                line += ",";
            }
            builder.AppendLine(line);
        }

        builder.AppendLine(");");
        return builder.ToString();
    }
}
