namespace SQL_prototipo.Controls;

/// <summary>The kind of a completion suggestion, used to pick its icon.</summary>
public enum CompletionKind
{
    Keyword,
    Table,
    Column
}

/// <summary>A single suggestion shown in the autocomplete popup.</summary>
public sealed class CompletionItem
{
    public CompletionItem(string text, CompletionKind kind)
    {
        Text = text;
        Kind = kind;
    }

    public string Text { get; }
    public CompletionKind Kind { get; }

    public override string ToString() => Text;
}

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
            Font = editor.Font,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = Math.Max(18, editor.Font.Height + 4)
        };

        _editor.KeyDown += Editor_KeyDown;
        _editor.KeyUp += Editor_KeyUp;
        _editor.LostFocus += (_, _) => Hide();
        _editor.HandleCreated += (_, _) => EnsureListParented();
        EnsureListParented();

        _list.Click += (_, _) => CommitSelection();
        _list.KeyDown += List_KeyDown;
        _list.DrawItem += List_DrawItem;
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

    private List<CompletionItem> BuildCandidates(string text, int caret, string prefix)
    {
        var results = new List<CompletionItem>();
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
                AddMatching(results, cols, prefix, CompletionKind.Column);
            }
            return results;
        }

        if (afterFrom)
        {
            AddMatching(results, _schema.TableNames, prefix, CompletionKind.Table);
            return results;
        }

        // General context: tables, columns of referenced tables, and keywords.
        AddMatching(results, _schema.TableNames, prefix, CompletionKind.Table);

        foreach (var tableName in GetReferencedTables(text))
        {
            _schema.EnsureColumns(tableName);
            var cols = _schema.GetColumnsIfLoaded(tableName);
            if (cols != null)
            {
                AddMatching(results, cols, prefix, CompletionKind.Column);
            }
        }

        AddMatching(results, Keywords, prefix, CompletionKind.Keyword);
        return results
            .GroupBy(r => r.Text, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(50)
            .ToList();
    }

    private static void AddMatching(List<CompletionItem> target, IEnumerable<string> source, string prefix, CompletionKind kind)
    {
        foreach (var item in source)
        {
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }
            if (prefix.Length == 0 || item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(new CompletionItem(item, kind));
            }
        }
    }

    private void CommitSelection()
    {
        if (_list.SelectedItem is not CompletionItem selected)
        {
            Hide();
            return;
        }

        string chosen = selected.Text;
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

    private void List_DrawItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0 || e.Index >= _list.Items.Count)
        {
            e.DrawFocusRectangle();
            return;
        }

        var item = (CompletionItem)_list.Items[e.Index];
        var bounds = e.Bounds;

        // --- Draw a small type icon on the left ---
        int iconSize = Math.Min(bounds.Height - 4, 12);
        var iconRect = new Rectangle(bounds.Left + 3, bounds.Top + (bounds.Height - iconSize) / 2, iconSize, iconSize);
        DrawKindIcon(e.Graphics, iconRect, item.Kind);

        // --- Draw the text after the icon ---
        var textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
            ? SystemColors.HighlightText
            : SystemColors.WindowText;
        var textRect = new Rectangle(iconRect.Right + 5, bounds.Top, bounds.Width - iconRect.Right - 5, bounds.Height);
        TextRenderer.DrawText(e.Graphics, item.Text, _list.Font, textRect, textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        e.DrawFocusRectangle();
    }

    private static void DrawKindIcon(Graphics g, Rectangle rect, CompletionKind kind)
    {
        switch (kind)
        {
            case CompletionKind.Table:
                // Blue grid-like square representing a table.
                using (var brush = new SolidBrush(Color.FromArgb(52, 120, 210)))
                {
                    g.FillRectangle(brush, rect);
                }
                using (var pen = new Pen(Color.White))
                {
                    int midY = rect.Top + rect.Height / 2;
                    int midX = rect.Left + rect.Width / 2;
                    g.DrawLine(pen, rect.Left, midY, rect.Right, midY);
                    g.DrawLine(pen, midX, rect.Top, midX, rect.Bottom);
                }
                break;

            case CompletionKind.Column:
                // Green circle representing a column.
                using (var brush = new SolidBrush(Color.FromArgb(60, 160, 90)))
                {
                    g.FillEllipse(brush, rect);
                }
                break;

            default:
                // Grey marker for keywords.
                using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
                {
                    g.FillRectangle(brush, rect.Left, rect.Top + rect.Height / 4, rect.Width, rect.Height / 2);
                }
                break;
        }
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
