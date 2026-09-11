using Antlr4.Runtime;
using Antlr4C3;
using SqlGrammar = Antlr4C3.Grammars;

namespace SQL_prototipo.TSql;

/// <summary>
/// Provides SQL auto-completion suggestions by driving the ANTLR
/// <see cref="CodeCompletionCore"/> over the generated T-SQL grammar. All
/// grammar-specific logic (parsing, ignored tokens, preferred rules) is supplied
/// by a <see cref="TSqlGrammarProvider"/>. In addition to grammar-valid keywords,
/// it offers table and column names from a <see cref="SqlSchemaSnapshot"/> when the
/// caret sits in a table- or column-name position.
/// </summary>
public sealed class SqlCompletionProvider(TSqlGrammarProvider? grammar = null)
{
    // Grammar rules that indicate the caret is where a table name is expected.
    private static readonly HashSet<int> TableNameRules =
    [
        SqlGrammar.TSqlParser.RULE_table_name,
        SqlGrammar.TSqlParser.RULE_full_table_name,
    ];

    // Grammar rules that indicate the caret is where a column name is expected.
    private static readonly HashSet<int> ColumnNameRules =
    [
        SqlGrammar.TSqlParser.RULE_full_column_name,
        SqlGrammar.TSqlParser.RULE_column_name_list,
        SqlGrammar.TSqlParser.RULE_insert_column_id,
        SqlGrammar.TSqlParser.RULE_column_alias,
        SqlGrammar.TSqlParser.RULE_as_column_alias,
    ];

    private readonly TSqlGrammarProvider _grammar = grammar ?? new TSqlGrammarProvider();

    /// <summary>
    /// The database schema used to suggest table and column names. Replace it as
    /// the active connection changes; may be assigned from any thread.
    /// </summary>
    public SqlSchemaSnapshot Schema { get; set; } = SqlSchemaSnapshot.Empty;

    /// <summary>
    /// Maximum number of completion items returned (top N). Values less than or
    /// equal to zero mean "no limit". Defaults to 25.
    /// </summary>
    public int MaxSuggestions { get; set; } = 25;

    /// <summary>
    /// Returns the distinct, sorted list of completions valid at the given caret
    /// character offset within <paramref name="sql"/>. Includes grammar keywords
    /// plus table/column names when the caret is in an object-name position.
    /// </summary>
    public IReadOnlyList<SqlCompletionItem> GetCompletions(string sql, int caretOffset)
    {
        sql ??= string.Empty;

        (Parser parser, CommonTokenStream tokenStream) = _grammar.Parse(sql);
        tokenStream.Fill();

        int caretTokenIndex = ComputeCaretTokenIndex(tokenStream, caretOffset);

        // The partial word already typed at the caret (e.g. "W"). Used to filter
        // the suggestions so only matching items (e.g. "WHERE") are returned.
        string prefix = ComputeCaretPrefix(sql, caretOffset);

        // A table/alias qualifier before a trailing dot (e.g. "e" in "e.|"), used
        // to scope column completions to a single table.
        string? qualifier = ComputeCaretQualifier(sql, caretOffset, prefix);

        // Tables referenced in the query's FROM / JOIN clauses, so column
        // completions (e.g. in WHERE) come from those tables only.
        IReadOnlyList<TableReference> referencedTables = _grammar.CollectReferencedTables(sql);

        var core = new CodeCompletionCore(parser)
        {
            ignoredTokens = _grammar.IgnoredTokens,
            preferredRules = _grammar.PreferredRules,
        };

        CodeCompletionCore.CandidatesCollection candidates =
            core.CollectCandidates(caretTokenIndex, null);

        var vocabulary = parser.Vocabulary;

        var keywords = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (int tokenType in candidates.Tokens.Keys)
        {
            string? keyword = ToKeyword(vocabulary, tokenType);
            if (keyword != null)
            {
                keywords.Add(keyword);
            }
        }

        // Table/column names are listed first so they surface above keywords, then
        // keywords fill the rest. A shared set prevents duplicates across groups.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<SqlCompletionItem>();

        // A value <= 0 means "no limit"; otherwise cap the result at the top N items.
        int limit = MaxSuggestions > 0 ? MaxSuggestions : int.MaxValue;

        foreach (SqlCompletionItem item in GetSchemaCandidates(candidates, referencedTables, qualifier))
        {
            if (ordered.Count >= limit)
            {
                return ordered;
            }

            if (MatchesPrefix(item.Text, prefix) && seen.Add(item.Text))
            {
                ordered.Add(item);
            }
        }

        foreach (string keyword in keywords)
        {
            if (ordered.Count >= limit)
            {
                break;
            }

            if (MatchesPrefix(keyword, prefix) && seen.Add(keyword))
            {
                ordered.Add(new SqlCompletionItem(keyword, SqlCompletionKind.Keyword));
            }
        }

        return ordered;
    }

    // True when the candidate starts with the typed prefix (case-insensitive).
    // An empty prefix matches everything.
    private static bool MatchesPrefix(string candidate, string prefix)
    {
        return prefix.Length == 0
            || candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    // Extracts the identifier/word characters immediately preceding the caret so
    // completions can be filtered by what the user has already typed.
    private static string ComputeCaretPrefix(string sql, int caretOffset)
    {
        int start = Math.Clamp(caretOffset, 0, sql.Length);
        int i = start;
        while (i > 0)
        {
            char c = sql[i - 1];
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                break;
            }

            i--;
        }

        return sql.Substring(i, start - i);
    }

    // Extracts a table/alias qualifier when the caret word is preceded by a dot,
    // e.g. returns "e" for "... WHERE e.co|". Returns null when unqualified.
    private static string? ComputeCaretQualifier(string sql, int caretOffset, string prefix)
    {
        int caret = Math.Clamp(caretOffset, 0, sql.Length);
        int dotIndex = caret - prefix.Length - 1;
        if (dotIndex < 0 || sql[dotIndex] != '.')
        {
            return null;
        }

        int i = dotIndex - 1;
        while (i >= 0 && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_'))
        {
            i--;
        }

        int start = i + 1;
        int length = dotIndex - start;
        if (length <= 0)
        {
            return null;
        }

        return sql.Substring(start, length).Trim('[', ']', '"', '`');
    }

    // Yields the table and/or column names (sorted) when the collected rule
    // candidates indicate the caret is at a table- or column-name position.
    // Column suggestions are scoped to the tables referenced by the query (and
    // to a single table when the caret is qualified with an alias, e.g. "e.").
    private IEnumerable<SqlCompletionItem> GetSchemaCandidates(
        CodeCompletionCore.CandidatesCollection candidates,
        IReadOnlyList<TableReference> referencedTables,
        string? qualifier)
    {
        SqlSchemaSnapshot schema = Schema;
        if (schema == null || schema.IsEmpty)
        {
            yield break;
        }

        bool wantsTables = candidates.Rules.Keys.Any(TableNameRules.Contains);
        bool wantsColumns = candidates.Rules.Keys.Any(ColumnNameRules.Contains);

        if (wantsTables)
        {
            foreach (string table in schema.Tables)
            {
                yield return new SqlCompletionItem(table, SqlCompletionKind.Table);
            }
        }

        if (wantsColumns)
        {
            foreach (string column in GetColumnCandidates(schema, referencedTables, qualifier))
            {
                yield return new SqlCompletionItem(column, SqlCompletionKind.Column);
            }
        }
    }

    // Determines which columns to offer at a column position: the columns of the
    // query's referenced tables (optionally narrowed to a qualifying alias/name).
    // Falls back to every known column when the query has no parseable tables or
    // no per-table column information is available.
    private static IEnumerable<string> GetColumnCandidates(
        SqlSchemaSnapshot schema,
        IReadOnlyList<TableReference> referencedTables,
        string? qualifier)
    {
        IEnumerable<TableReference> scope = referencedTables;
        if (!string.IsNullOrEmpty(qualifier))
        {
            scope = referencedTables.Where(t =>
                string.Equals(t.Alias, qualifier, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.Name, qualifier, StringComparison.OrdinalIgnoreCase));
        }

        var tableNames = scope.Select(t => t.Name).ToList();
        if (tableNames.Count > 0)
        {
            IReadOnlyList<string> scopedColumns = schema.ColumnsFor(tableNames);
            if (scopedColumns.Count > 0)
            {
                return scopedColumns;
            }
        }

        // No query tables (yet) or no per-table info: offer all known columns.
        return string.IsNullOrEmpty(qualifier) ? schema.Columns : [];
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

/// <summary>The category of a completion suggestion, used to pick its icon.</summary>
public enum SqlCompletionKind
{
    Keyword,
    Table,
    Column,
}

/// <summary>A single completion suggestion together with its category.</summary>
public readonly record struct SqlCompletionItem(string Text, SqlCompletionKind Kind);


/// <summary>
/// An immutable snapshot of the table and column names available for
/// auto-completion. Build one from the active connection's schema and assign it
/// to <see cref="SqlCompletionProvider.Schema"/>. When per-table column
/// information is supplied, column completions can be scoped to the tables that
/// actually appear in the query (see <see cref="ColumnsFor"/>).
/// </summary>
public sealed class SqlSchemaSnapshot
{
    /// <summary>An empty snapshot that yields no table/column suggestions.</summary>
    public static readonly SqlSchemaSnapshot Empty = new([], []);

    private readonly Dictionary<string, IReadOnlyList<string>> _columnsByTable;

    /// <summary>
    /// Creates a snapshot with a flat list of columns (not associated with any
    /// particular table). Column completions cannot be scoped to query tables.
    /// </summary>
    public SqlSchemaSnapshot(IEnumerable<string> tables, IEnumerable<string> columns)
    {
        Tables = Distinct(tables);
        Columns = Distinct(columns);
        _columnsByTable = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a snapshot that maps each table name to its columns, enabling
    /// column completions to be scoped to the tables used in a query.
    /// </summary>
    public SqlSchemaSnapshot(IReadOnlyDictionary<string, IEnumerable<string>> columnsByTable)
    {
        columnsByTable ??= new Dictionary<string, IEnumerable<string>>();

        _columnsByTable = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in columnsByTable)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                _columnsByTable[pair.Key] = Distinct(pair.Value);
            }
        }

        Tables = Distinct(_columnsByTable.Keys);
        Columns = Distinct(_columnsByTable.Values.SelectMany(v => v));
    }

    /// <summary>Distinct table names (unqualified).</summary>
    public IReadOnlyList<string> Tables { get; }

    /// <summary>Distinct column names across all known tables.</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>True when there is nothing to suggest.</summary>
    public bool IsEmpty => Tables.Count == 0 && Columns.Count == 0;

    /// <summary>
    /// Returns the distinct, sorted columns belonging to the given tables. Names
    /// are matched case-insensitively against the known tables; unknown names are
    /// ignored. When no per-table information is available (flat snapshot) or no
    /// name matches, an empty list is returned so callers can fall back.
    /// </summary>
    public IReadOnlyList<string> ColumnsFor(IEnumerable<string> tableNames)
    {
        if (tableNames == null || _columnsByTable.Count == 0)
        {
            return [];
        }

        var result = new List<string>();
        foreach (string name in tableNames)
        {
            if (!string.IsNullOrWhiteSpace(name) && _columnsByTable.TryGetValue(name, out var columns))
            {
                result.AddRange(columns);
            }
        }

        return Distinct(result);
    }

    private static IReadOnlyList<string> Distinct(IEnumerable<string>? values)
    {
        if (values == null)
        {
            return [];
        }

        return
        [
            .. values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase),
        ];

    }
}

