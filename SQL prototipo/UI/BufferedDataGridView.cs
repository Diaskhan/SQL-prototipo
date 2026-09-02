using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SQL_prototipo.UI;

/// <summary>
/// A <see cref="DataGridView"/> with double buffering enabled to eliminate the
/// flicker that occurs while scrolling, resizing, or refreshing large result
/// sets. <see cref="DataGridView.DoubleBuffered"/> is protected, so it can only
/// be turned on from a derived type.
/// </summary>
public sealed class BufferedDataGridView : DataGridView
{
    private bool _copyInitialized = false;
    private bool _enableAlternating = true;
    private Color _alternatingBackColor = Color.FromArgb(245, 245, 245);

    /// <summary>
    /// Если true (по умолчанию), грид автоматически настраивает поведение копирования
    /// в конструкторе (за исключением режима дизайнера). Можно отключить перед вызовом SetupGridCopy вручную.
    /// Не сериализуется дизайнером.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EnableAutoCopy { get; set; } = true;

    /// <summary>
    /// Включить подсветку каждой второй строки (zebra). По умолчанию true.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    public bool EnableAlternatingRowHighlight
    {
        get => _enableAlternating;
        set
        {
            _enableAlternating = value;
            ApplyAlternatingRowStyle();
        }
    }

    /// <summary>
    /// Цвет подсветки для каждой второй строки по умолчанию.
    /// </summary>
    [Category("Appearance")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AlternatingRowBackColor
    {
        get => _alternatingBackColor;
        set
        {
            _alternatingBackColor = value;
            ApplyAlternatingRowStyle();
        }
    }

    public BufferedDataGridView()
    {
        DoubleBuffered = true;
        // Apply alternating row style by default (designer excluded)
        ApplyAlternatingRowStyle();

        // Don't initialize copy behavior at design time (Visual Studio designer)
        if (EnableAutoCopy && LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            SetupGridCopy();
        }
    }

    private void ApplyAlternatingRowStyle()
    {
        if (EnableAlternatingRowHighlight)
        {
            // Ensure default row color is white for contrast
            this.RowsDefaultCellStyle.BackColor = Color.White;
            this.AlternatingRowsDefaultCellStyle.BackColor = AlternatingRowBackColor;
        }
        else
        {
            // Reset to default cell style background
            this.AlternatingRowsDefaultCellStyle.BackColor = this.DefaultCellStyle.BackColor;
        }
    }

    /// <summary>
    /// Настройка поведения копирования аналогично SSMS: контекстное меню и горячие клавиши.
    /// Вызвать один раз для конкретного экземпляра грида.
    /// </summary>
    public void SetupGridCopy()
    {
        if (_copyInitialized) return;
        _copyInitialized = true;

        // default: copy without headers for Ctrl+C
        this.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

        var ctx = new ContextMenuStrip();
        var copyItem = new ToolStripMenuItem("Copy") { ShortcutKeys = Keys.Control | Keys.C };
        var copyWithHeadersItem = new ToolStripMenuItem("Copy with headers") { ShortcutKeys = Keys.Control | Keys.Shift | Keys.C };
        copyItem.Click += (_, _) => CopySelection(includeHeaders: false);
        copyWithHeadersItem.Click += (_, _) => CopySelection(includeHeaders: true);
        ctx.Items.Add(copyItem);
        ctx.Items.Add(copyWithHeadersItem);
        this.ContextMenuStrip = ctx;

        this.KeyDown += (s, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C && !e.Shift)
            {
                CopySelection(false);
                e.Handled = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.C)
            {
                CopySelection(true);
                e.Handled = true;
            }
        };
    }

    private void CopySelection(bool includeHeaders)
    {
        if (this.GetCellCount(DataGridViewElementStates.Selected) == 0) return;
        var previous = this.ClipboardCopyMode;
        this.ClipboardCopyMode = includeHeaders ? DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText : DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        try
        {
            var dataObj = this.GetClipboardContent();
            if (dataObj != null)
            {
                Clipboard.SetDataObject(dataObj);
            }
        }
        finally
        {
            this.ClipboardCopyMode = previous;
        }
    }
}
