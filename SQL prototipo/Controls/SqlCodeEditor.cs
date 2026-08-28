using System.ComponentModel;
using System.Windows.Forms.Integration;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Ob.Common;
using Ob.Sqlite;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfKey = System.Windows.Input.Key;
using WpfKeyboard = System.Windows.Input.Keyboard;
using WpfModifierKeys = System.Windows.Input.ModifierKeys;
namespace SQL_prototipo.Controls;

/// <summary>
/// A WinForms SQL code editor that hosts the WPF AvalonEdit
/// <see cref="TextEditor"/> via an <see cref="ElementHost"/>. It provides SQL
/// syntax highlighting and IntelliSense-like completion (keywords, tables and
/// columns) powered by <see cref="SqlCompletionEngine"/>. The control exposes a
/// familiar <see cref="Text"/> property so it can be used as a drop-in
/// replacement for the previous <c>RichTextBox</c> based editor.
/// </summary>
public sealed class SqlCodeEditor : UserControl
{
    private readonly TextEditor _editor;
    private SqlCompletionEngine? _engine;
    private CompletionWindow? _completionWindow;
    private SchemaCache? _schema;

    public event EventHandler? CloseTabRequested;
    public event EventHandler? ExitRequested;

    public SqlCodeEditor()
    {
        _editor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new WpfFontFamily("Consolas"),
            FontSize = 13,
            WordWrap = false,
            SyntaxHighlighting = LoadSqlHighlighting()
        };
        _editor.Options.EnableHyperlinks = false;
        _editor.Options.EnableEmailHyperlinks = false;

        var host = new ElementHost
        {
            Dock = DockStyle.Fill,
            Child = _editor
        };
        Controls.Add(host);

        _editor.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        _editor.TextArea.TextEntered += TextArea_TextEntered;
        _editor.TextArea.TextEntering += TextArea_TextEntering;
        _editor.KeyDown += Editor_KeyDown;
    }

    /// <summary>Gets or sets the SQL text of the editor.</summary>
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public override string Text
    {
        get => _editor.Document?.Text ?? string.Empty;
        set
        {
            if (_editor.Document != null)
            {
                _editor.Document.Text = value ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Attaches the schema cache that drives table and column suggestions.
    /// Completion is produced by <see cref="SqlCompletionEngine"/> from the grammar
    /// (which identifiers are valid) combined with this schema (their real names).
    /// </summary>
    public void AttachSchema(SchemaCache schema)
    {
        if (_schema != null)
        {
            _schema.Updated -= OnSchemaUpdated;
        }

        _schema = schema;
        _engine = new SqlCompletionEngine(new SqliteDialect(), schema);

        if (_schema != null)
        {
            _schema.Updated += OnSchemaUpdated;
        }
    }

    /// <summary>
    /// Re-runs completion when the schema cache changes (e.g. a table's columns
    /// finished loading asynchronously) so newly available items appear without
    /// requiring the user to retype.
    /// </summary>
    private void OnSchemaUpdated()
    {
        if (_editor.Dispatcher.CheckAccess())
        {
            RefreshOpenCompletion();
        }
        else
        {
            _editor.Dispatcher.BeginInvoke(new Action(RefreshOpenCompletion));
        }
    }

    private void RefreshOpenCompletion()
    {
        if (_completionWindow != null)
        {
            ShowCompletion(force: true);
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (Font != null)
        {
            _editor.FontFamily = new WpfFontFamily(Font.Name);
            // WinForms font size is in points; AvalonEdit expects device-independent pixels.
            _editor.FontSize = Font.SizeInPoints * 96.0 / 72.0;
        }
    }

    /// <summary>
    /// Loads the SQL syntax highlighting from the embedded <c>Sql.xshd</c>
    /// resource. AvalonEdit's built-in <see cref="HighlightingManager"/> does
    /// not register a SQL definition by default, so we ship and load our own.
    /// </summary>
    private static IHighlightingDefinition LoadSqlHighlighting()
    {
        using var stream = typeof(SqlCodeEditor).Assembly.GetManifestResourceStream("Sql.xshd")
            ?? throw new InvalidOperationException("Embedded resource 'Sql.xshd' was not found.");
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    private void Editor_KeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((WpfKeyboard.Modifiers & WpfModifierKeys.Control) == WpfModifierKeys.Control)
        {
            if (e.Key == WpfKey.W)
            {
                CloseTabRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                return;
            }

            if (e.Key == WpfKey.Q)
            {
                ExitRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                return;
            }
        }

        // Ctrl+Space forces the completion popup open.
        if (e.Key == WpfKey.Space && (WpfKeyboard.Modifiers & WpfModifierKeys.Control) == WpfModifierKeys.Control)
        {
            ShowCompletion(force: true);
            e.Handled = true;
        }
    }

    private void TextArea_TextEntered(object? sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (e.Text.Length == 0)
        {
            return;
        }

        char c = e.Text[0];
        if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
        {
            ShowCompletion(force: false);
        }
    }

    private void TextArea_TextEntering(object? sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        // Commit the selected suggestion when a non-identifier character is typed.
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            char c = e.Text[0];
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
    }

    private void ShowCompletion(bool force)
    {
        if (_engine == null)
        {
            return;
        }

        string text = _editor.Document.Text;
        int caret = _editor.CaretOffset;

        var items = _engine.GetSuggestions(text, caret, force, out int replaceStart);
        if (items.Count == 0)
        {
            _completionWindow?.Close();
            return;
        }

        _completionWindow = new CompletionWindow(_editor.TextArea)
        {
            StartOffset = replaceStart,
            EndOffset = caret
        };

        var data = _completionWindow.CompletionList.CompletionData;
        foreach (var item in items)
        {
            data.Add(new SqlCompletionData(item));
        }

        // Size the popup to fit the widest suggestion text.
        _completionWindow.Width = ComputeCompletionWidth(items);

        _completionWindow.Closed += (_, _) => _completionWindow = null;
        _completionWindow.Show();
    }

    /// <summary>
    /// Computes a popup width (in device-independent pixels) that fits the widest
    /// suggestion text, plus room for the icon, padding and scrollbar.
    /// </summary>
    private double ComputeCompletionWidth(IReadOnlyList<CompletionItem> items)
    {
        // Icon + item padding + potential vertical scrollbar.
        const double Chrome = 44;
        const double MinWidth = 120;
        const double MaxWidth = 600;

        var typeface = new System.Windows.Media.Typeface(
            _editor.FontFamily,
            System.Windows.FontStyles.Normal,
            System.Windows.FontWeights.Normal,
            System.Windows.FontStretches.Normal);

        double dpi = System.Windows.Media.VisualTreeHelper.GetDpi(_editor).PixelsPerDip;

        double widest = 0;
        foreach (var item in items)
        {
            var formatted = new System.Windows.Media.FormattedText(
                item.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                _editor.FontSize,
                System.Windows.Media.Brushes.Black,
                dpi);
            if (formatted.Width > widest)
            {
                widest = formatted.Width;
            }
        }

        return Math.Clamp(widest + Chrome, MinWidth, MaxWidth);
    }

    /// <summary>
    /// Provides small vector icons (rendered once and frozen) for each
    /// <see cref="CompletionKind"/> shown in the completion popup, so no external
    /// image assets are required.
    /// </summary>
    private static class CompletionIcons
    {
        private static readonly System.Windows.Media.ImageSource TableIcon =
            Create(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32), "T");
        private static readonly System.Windows.Media.ImageSource ColumnIcon =
            Create(System.Windows.Media.Color.FromRgb(0x15, 0x65, 0xC0), "C");
        private static readonly System.Windows.Media.ImageSource KeywordIcon =
            Create(System.Windows.Media.Color.FromRgb(0x6A, 0x1B, 0x9A), "K");

        public static System.Windows.Media.ImageSource? For(CompletionKind kind) => kind switch
        {
            CompletionKind.Table => TableIcon,
            CompletionKind.Column => ColumnIcon,
            CompletionKind.Keyword => KeywordIcon,
            _ => null
        };

        private static System.Windows.Media.ImageSource Create(System.Windows.Media.Color color, string glyph)
        {
            const int size = 16;

            var visual = new System.Windows.Media.DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var background = new System.Windows.Media.SolidColorBrush(color);
                dc.DrawRoundedRectangle(
                    background,
                    null,
                    new System.Windows.Rect(0, 0, size, size),
                    3, 3);

                var typeface = new System.Windows.Media.Typeface(
                    new WpfFontFamily("Segoe UI"),
                    System.Windows.FontStyles.Normal,
                    System.Windows.FontWeights.Bold,
                    System.Windows.FontStretches.Normal);

                var text = new System.Windows.Media.FormattedText(
                    glyph,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    typeface,
                    10,
                    System.Windows.Media.Brushes.White,
                    1.0);

                dc.DrawText(
                    text,
                    new System.Windows.Point((size - text.Width) / 2, (size - text.Height) / 2));
            }

            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                size, size, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }
    }

    /// <summary>Adapts a <see cref="CompletionItem"/> to AvalonEdit's completion list.</summary>
    private sealed class SqlCompletionData : ICompletionData
    {
        private readonly CompletionItem _item;

        public SqlCompletionData(CompletionItem item)
        {
            _item = item;
        }

        public System.Windows.Media.ImageSource? Image => CompletionIcons.For(_item.Kind);

        public string Text => _item.Text;

        public object Content => _item.Text;

        public object Description => _item.Kind.ToString();

        public double Priority => _item.Kind switch
        {
            CompletionKind.Column => 2,
            CompletionKind.Table => 1,
            _ => 0
        };

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, _item.Text);
        }
    }
}
