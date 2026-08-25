namespace SQL_prototipo.Controls;

/// <summary>
/// Provides IntelliSense-like completion for a <see cref="RichTextBox"/> SQL
/// editor. Suggestions combine SQL keywords with tables/columns pulled from a
/// <see cref="SchemaCache"/>. The popup opens automatically while typing and on
/// Ctrl+Space, and is context aware: after FROM/JOIN it favours tables, and
/// after a known table name/alias it offers that table's columns.
/// </summary>
public sealed class SqlAutoComplete : IDisposable
{
    private static readonly string[] Keywords =
    {
        "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE", "SET",
        "DELETE", "CREATE", "TABLE", "ALTER", "DROP", "JOIN", "INNER", "LEFT",
        "RIGHT", "FULL", "OUTER", "ON", "GROUP", "BY", "ORDER", "HAVING",
        "DISTINCT", "TOP", "LIMIT", "OFFSET", "AS", "AND", "OR", "NOT", "NULL",
        "IN", "LIKE", "BETWEEN", "IS", "EXISTS", "UNION", "ALL", "CASE", "WHEN",
        "THEN", "ELSE", "END", "ASC", "DESC", "COUNT", "SUM", "AVG", "MIN",
        "MAX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "INDEX", "VIEW"
    };

    private readonly RichTextBox _editor;
    private readonly SchemaCache _schema;
    private readonly ListBox _list;
    private int _replaceStart;
    private bool _suppress;
    private bool _disposed;

    public SqlAutoComplete(RichTextBox editor, SchemaCache schema)
    {
        _editor = editor;
        _schema = schema;

        _list = new ListBox
        {
            Visible = false,
            IntegralHeight = false,
            Height = 140,
            Width = 220,
            Font = editor.Font
        };

        _editor.KeyDown += Editor_KeyDown;
        _editor.KeyUp += Editor_KeyUp;
        _editor.LostFocus += (_, _) => Hide();
        _editor.HandleCreated += (_, _) => EnsureListParented();
        EnsureListParented();

        _list.Click += (_, _) => CommitSelection();
        _list.KeyDown += List_KeyDown;
    }

    private void EnsureListParented()
    {
        var host = _editor.FindForm();
        if (host != null && _list.Parent == null)
        {
            host.Controls.Add(_list);
            _list.BringToFront();
        }
    }

    private void Editor_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+Space forces the popup open.
        if (e.Control && e.KeyCode == Keys.Space)
        {
            ShowSuggestions(force: true);
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }

        if (!_list.Visible)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Down:
                _list.SelectedIndex = Math.Min(_list.SelectedIndex + 1, _list.Items.Count - 1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Up:
                _list.SelectedIndex = Math.Max(_list.SelectedIndex - 1, 0);
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Enter:
            case Keys.Tab:
                CommitSelection();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Escape:
                Hide();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
        }
    }

    private void List_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
        {
            CommitSelection();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Hide();
            _editor.Focus();
        }
    }

    private void Editor_KeyUp(object? sender, KeyEventArgs e)
    {
        if (_suppress)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Left:
            case Keys.Right:
            case Keys.Home:
            case Keys.End:
            case Keys.Enter:
            case Keys.Tab:
            case Keys.Escape:
            case Keys.Up:
            case Keys.Down:
            case Keys.ControlKey:
            case Keys.ShiftKey:
                return;
        }

        ShowSuggestions(force: false);
    }

    private void ShowSuggestions(bool force)
    {
        int caret = _editor.SelectionStart;
        string text = _editor.Text;

        string prefix = GetCurrentToken(text, caret, out _replaceStart);
        if (!force && prefix.Length == 0)
        {
            Hide();
            return;
        }

        var items = BuildCandidates(text, caret, prefix);
        if (items.Count == 0)
        {
            Hide();
            return;
        }

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in items)
        {
            _list.Items.Add(item);
        }
        _list.EndUpdate();
        _list.SelectedIndex = 0;

        PositionListAtCaret();
        if (!_list.Visible)
        {
            _list.Visible = true;
        }
        _list.BringToFront();
        _editor.Focus();
    }

    private List<string> BuildCandidates(string text, int caret, string prefix)
    {
        var results = new List<string>();
        string preceding = GetPrecedingKeyword(text, _replaceStart);

        bool afterFrom = preceding is "FROM" or "JOIN" or "INTO" or "UPDATE";

        // Context: "alias." or "table." -> only that table's columns.
        string qualifier = GetQualifierBeforeDot(text, _replaceStart);
        if (qualifier.Length > 0)
        {
            string resolved = ResolveTableName(text, qualifier);
            _schema.EnsureColumns(resolved);
            var cols = _schema.GetColumnsIfLoaded(resolved);
            if (cols != null)
            {
                AddMatching(results, cols, prefix);
            }
            return results;
        }

        if (afterFrom)
        {
            AddMatching(results, _schema.TableNames, prefix);
            return results;
        }

        // General context: tables, columns of referenced tables, and keywords.
        AddMatching(results, _schema.TableNames, prefix);

        foreach (var tableName in GetReferencedTables(text))
        {
            _schema.EnsureColumns(tableName);
            var cols = _schema.GetColumnsIfLoaded(tableName);
            if (cols != null)
            {
                AddMatching(results, cols, prefix);
            }
        }

        AddMatching(results, Keywords, prefix);
        return results.Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList();
    }

    private static void AddMatching(List<string> target, IEnumerable<string> source, string prefix)
    {
        foreach (var item in source)
        {
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }
            if (prefix.Length == 0 || item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(item);
            }
        }
    }

    private void CommitSelection()
    {
        if (_list.SelectedItem is not string chosen)
        {
            Hide();
            return;
        }

        _suppress = true;
        try
        {
            int caret = _editor.SelectionStart;
            int length = caret - _replaceStart;
            if (length < 0)
            {
                length = 0;
            }

            _editor.SelectionStart = _replaceStart;
            _editor.SelectionLength = length;
            _editor.SelectedText = chosen;
            _editor.SelectionStart = _replaceStart + chosen.Length;
            _editor.SelectionLength = 0;
        }
        finally
        {
            _suppress = false;
        }

        Hide();
        _editor.Focus();
    }

    private void PositionListAtCaret()
    {
        Point caretPos = _editor.GetPositionFromCharIndex(_editor.SelectionStart);
        Point screen = _editor.PointToScreen(new Point(caretPos.X, caretPos.Y + (_editor.Font.Height)));
        var host = _list.Parent ?? _editor.FindForm();
        if (host != null)
        {
            _list.Location = host.PointToClient(screen);
        }
    }

    private void Hide()
    {
        if (_list.Visible)
        {
            _list.Visible = false;
        }
    }

    // --- Token / context parsing helpers ---

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string GetCurrentToken(string text, int caret, out int start)
    {
        int i = caret;
        while (i > 0 && IsWordChar(text[i - 1]))
        {
            i--;
        }
        start = i;
        return text.Substring(i, caret - i);
    }

    /// <summary>Returns the identifier immediately before a '.' preceding the token, or "".</summary>
    private static string GetQualifierBeforeDot(string text, int tokenStart)
    {
        int i = tokenStart;
        if (i <= 0 || text[i - 1] != '.')
        {
            return string.Empty;
        }
        i--; // skip the dot
        int end = i;
        while (i > 0 && IsWordChar(text[i - 1]))
        {
            i--;
        }
        return text.Substring(i, end - i);
    }

    private static string GetPrecedingKeyword(string text, int tokenStart)
    {
        int i = tokenStart;
        while (i > 0 && char.IsWhiteSpace(text[i - 1]))
        {
            i--;
        }
        int end = i;
        while (i > 0 && IsWordChar(text[i - 1]))
        {
            i--;
        }
        return text.Substring(i, end - i).ToUpperInvariant();
    }

    /// <summary>Finds table names referenced in FROM/JOIN clauses of the text.</summary>
    private IEnumerable<string> GetReferencedTables(string text)
    {
        var known = new HashSet<string>(_schema.TableNames, StringComparer.OrdinalIgnoreCase);
        var tokens = Tokenize(text);
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            string kw = tokens[i].ToUpperInvariant();
            if (kw is "FROM" or "JOIN")
            {
                // The table may be schema-qualified (e.g. [dbo].[Customer] ->
                // tokens "dbo","Customer"), so scan a small window after the
                // keyword for the first token that matches a known table.
                for (int j = i + 1; j < tokens.Count && j <= i + 3; j++)
                {
                    if (known.Contains(tokens[j]))
                    {
                        yield return tokens[j];
                        break;
                    }
                }
            }
        }
    }

    /// <summary>Resolves an alias or table name to an actual table name.</summary>
    private string ResolveTableName(string text, string qualifier)
    {
        var known = new HashSet<string>(_schema.TableNames, StringComparer.OrdinalIgnoreCase);
        if (known.Contains(qualifier))
        {
            return qualifier;
        }

        // Look for "table alias" or "table AS alias" patterns.
        var tokens = Tokenize(text);
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (!known.Contains(tokens[i]))
            {
                continue;
            }
            string next = tokens[i + 1];
            if (string.Equals(next, qualifier, StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i];
            }
            if (string.Equals(next, "AS", StringComparison.OrdinalIgnoreCase)
                && i + 2 < tokens.Count
                && string.Equals(tokens[i + 2], qualifier, StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i];
            }
        }
        return qualifier;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            if (IsWordChar(text[i]))
            {
                int start = i;
                while (i < text.Length && IsWordChar(text[i]))
                {
                    i++;
                }
                tokens.Add(text.Substring(start, i - start));
            }
            else
            {
                i++;
            }
        }
        return tokens;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _editor.KeyDown -= Editor_KeyDown;
        _editor.KeyUp -= Editor_KeyUp;
        _list.Dispose();
    }
}
