using System.ComponentModel;
using System.Windows.Forms.Integration;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
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
    /// Retained for API compatibility. Completion is now driven purely by the
    /// grammar via <see cref="SqlCompletionEngine"/> and no longer uses schema data.
    /// </summary>
    public void AttachSchema(SchemaCache schema)
    {
        _engine = new SqlCompletionEngine(new Completion.SqliteDialect());
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

    /// <summary>Adapts a <see cref="CompletionItem"/> to AvalonEdit's completion list.</summary>
    private sealed class SqlCompletionData : ICompletionData
    {
        private readonly CompletionItem _item;

        public SqlCompletionData(CompletionItem item)
        {
            _item = item;
        }

        public System.Windows.Media.ImageSource? Image => null;

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
