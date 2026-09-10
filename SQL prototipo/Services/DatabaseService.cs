using SQL_prototipo.Models;
using System.Data;
using System.Diagnostics;
using OB.DataAccess.Providers;

namespace SQL_prototipo.Services;

public class DatabaseService(string connectionString, string databaseType = "SqlServer")
{
    private readonly string _connectionString = connectionString;
    private readonly IDbProvider _provider = DbProviderFactory.Create(databaseType);

    public DatabaseService(ConnectionInfo connection)
        : this(connection.ConnectionString, connection.DatabaseType)
    {
    }

    private System.Data.Common.DbConnection CreateConnection() => _provider.CreateConnection(_connectionString);

    /// <summary>
    /// Returns the starting text used when opening a new, empty query tab,
    /// as defined by the active database provider.
    /// </summary>
    public string GetNewQueryTemplate() => _provider.GetNewQueryTemplate();

    /// <summary>
    /// Indicates whether the active database provider organizes tables into schemas.
    /// </summary>
    public bool SupportsSchemas => _provider.SupportsSchemas;

    /// <summary>
    /// Returns the schema-qualified, quoted identifier for a table, ready to be
    /// embedded in a query for the active provider.
    /// </summary>
    public string QualifyTableName(string schema, string tableName) =>
        _provider.QualifyTableName(schema, tableName);

    /// <summary>
    /// Builds a "select first N rows" query for the given table using the
    /// active provider's row-limiting syntax (TOP vs LIMIT).
    /// </summary>
    public string BuildSelectTopQuery(string schema, string tableName, int rowCount)
    {
        var qualified = _provider.QualifyTableName(schema, tableName);
        return _provider.BuildSelectTopQuery(qualified, rowCount);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<List<TableRef>> GetAllTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = new List<TableRef>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = _provider.GetListTablesSql();

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var schema = reader.FieldCount > 1 && !reader.IsDBNull(0)
                ? reader.GetValue(0)?.ToString() ?? string.Empty
                : string.Empty;
            var name = reader.FieldCount > 1
                ? (reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty)
                : (reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty);
            tables.Add(new TableRef(schema, name));
        }
        return tables;
    }

    public async Task<List<(string Name, string Type)>> GetColumnsAsync(string schema, string tableName, CancellationToken cancellationToken = default)
    {
        var columns = new List<(string, string)>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = _provider.GetListColumnsSql(schema, tableName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty;
            var type = reader.FieldCount > 1 && !reader.IsDBNull(1) ? reader.GetValue(1)?.ToString() ?? string.Empty : string.Empty;
            columns.Add((name, type));
        }
        return columns;
    }

    public async Task<List<TableColumnDefinition>> GetTableDefinitionAsync(
        string schema,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var result = new List<TableColumnDefinition>();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT,
                   CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = @schema
            ORDER BY ORDINAL_POSITION;
            """;
        AddParameter(command, "@tableName", tableName);
        AddParameter(command, "@schema", string.IsNullOrEmpty(schema) ? "dbo" : schema);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(0);
            var dataType = reader.GetString(1);
            var length = reader.IsDBNull(4) ? null : reader.GetValue(4) as int?;
            byte? precision = reader.IsDBNull(5) ? null : Convert.ToByte(reader.GetValue(5));
            byte? scale = reader.IsDBNull(6) ? null : Convert.ToByte(reader.GetValue(6));
            result.Add(new TableColumnDefinition
            {
                Name = name,
                OriginalName = name,
                DataType = FormatDataType(dataType, length, precision, scale),
                IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                DefaultValue = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3)?.ToString() ?? string.Empty
            });
        }

        return result;
    }

    public async Task ApplyTableDefinitionAsync(
        string schema,
        string tableName,
        IReadOnlyCollection<TableColumnDefinition> columns,
        CancellationToken cancellationToken = default)
    {
        if (columns.Count == 0)
        {
            throw new InvalidOperationException("The table must contain at least one column.");
        }

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var existingNames = await GetExistingColumnNamesAsync(
                connection, transaction, schema, tableName, cancellationToken);
            var requestedNames = columns
                .Select(column => column.IsNew ? column.Name : column.OriginalName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var existingName in existingNames.Where(name => !requestedNames.Contains(name)))
            {
                await ExecuteDdlAsync(connection, transaction,
                    $"ALTER TABLE {QualifyTableName(schema, tableName)} DROP COLUMN {QuoteIdentifier(existingName)};",
                    cancellationToken);
            }

            foreach (var column in columns)
            {
                ValidateColumn(column);
                var quotedTable = QualifyTableName(schema, tableName);
                var quotedName = QuoteIdentifier(column.Name);
                var definition = $"{quotedName} {column.DataType} {(column.IsNullable ? "NULL" : "NOT NULL")}";

                if (column.IsNew)
                {
                    if (!column.IsNullable && string.IsNullOrWhiteSpace(column.DefaultValue))
                    {
                        throw new InvalidOperationException($"Specify a default value for the new NOT NULL column '{column.Name}' or allow NULL.");
                    }

                    var defaultClause = string.IsNullOrWhiteSpace(column.DefaultValue)
                        ? string.Empty
                        : $" DEFAULT {column.DefaultValue}";
                    await ExecuteDdlAsync(connection, transaction,
                        $"ALTER TABLE {quotedTable} ADD {definition}{defaultClause};", cancellationToken);
                    continue;
                }

                if (!string.Equals(column.OriginalName, column.Name, StringComparison.Ordinal))
                {
                    await ExecuteDdlAsync(connection, transaction,
                        $"EXEC sp_rename N'{EscapeLiteral(quotedTable)}.{EscapeLiteral(QuoteIdentifier(column.OriginalName))}', N'{EscapeLiteral(column.Name)}', 'COLUMN';",
                        cancellationToken);
                }

                await ExecuteDdlAsync(connection, transaction,
                    $"ALTER TABLE {quotedTable} ALTER COLUMN {definition};", cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task ExecuteDdlAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<string>> GetExistingColumnNamesAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string schema,
        string tableName,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = @schema;
            """;
        AddParameter(command, "@tableName", tableName);
        AddParameter(command, "@schema", string.IsNullOrEmpty(schema) ? "dbo" : schema);

        var names = new List<string>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string EscapeLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static void ValidateColumn(TableColumnDefinition column)
    {
        if (string.IsNullOrWhiteSpace(column.Name) || string.IsNullOrWhiteSpace(column.DataType))
        {
            throw new InvalidOperationException("Each column must have a name and a data type.");
        }
        if (column.DataType.Contains(';') || column.DataType.Contains("--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid data type '{column.DataType}'.");
        }
    }

    private static string FormatDataType(string type, int? length, byte? precision, byte? scale)
    {
        if (type is "varchar" or "nvarchar" or "char" or "nchar" or "varbinary" or "binary")
        {
            return $"{type}({(length == -1 ? "MAX" : length?.ToString() ?? "MAX")})";
        }
        if (type is "decimal" or "numeric")
        {
            return $"{type}({precision ?? 18},{scale ?? 0})";
        }
        return type;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (reader.FieldCount > 0)
        {
            var dataTable = await LoadDataTableAsync(reader, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            return new QueryResult
            {
                Data = dataTable,
                HasResultSet = true,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }

        stopwatch.Stop();
        return new QueryResult
        {
            HasResultSet = false,
            RecordsAffected = reader.RecordsAffected,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Reads the entire result set of <paramref name="reader"/> into a
    /// <see cref="DataTable"/> using asynchronous row reads, so large result
    /// sets don't block while data is being pulled from the provider.
    /// </summary>
    private static async Task<DataTable> LoadDataTableAsync(
        System.Data.Common.DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var dataTable = new DataTable();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            dataTable.Columns.Add(reader.GetName(i), reader.GetFieldType(i) ?? typeof(object));
        }

        var values = new object[reader.FieldCount];
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            reader.GetValues(values);
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }
}

