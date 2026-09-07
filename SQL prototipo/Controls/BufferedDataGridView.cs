using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SQL_prototipo.Controls;

/// <summary>
/// A <see cref="DataGridView"/> with double buffering enabled to eliminate the
/// flicker that occurs while scrolling, resizing, or refreshing large result
/// sets. <see cref="DataGridView.DoubleBuffered"/> is protected, so it can only
/// be turned on from a derived type.
/// </summary>
public sealed class BufferedDataGridView : DataGridView
{
    private bool _copyInitialized = false;
    private bool _freezeMenuInitialized = false;
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

        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            SetupFrozenColumnsMenu();
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

    public void ConfigureBinaryColumns(System.Data.DataTable? data)
    {
        if (data == null) return;

        var binaryColumns = data.Columns.Cast<System.Data.DataColumn>()
            .Where(c => c.DataType == typeof(byte[]))
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.Ordinal);

        if (binaryColumns.Count == 0) return;

        foreach (var colName in binaryColumns)
        {
            var existing = Columns[colName];
            if (existing == null) continue;

            int index = existing.Index;
            var textColumn = new DataGridViewTextBoxColumn
            {
                Name = existing.Name,
                HeaderText = existing.HeaderText,
                DataPropertyName = existing.DataPropertyName,
                ReadOnly = true
            };

            Columns.RemoveAt(index);
            Columns.Insert(index, textColumn);
        }

        CellFormatting -= BinaryCellFormatting;
        CellFormatting += BinaryCellFormatting;

        void BinaryCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            var colName = Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(colName)) colName = Columns[e.ColumnIndex].Name;
            if (!binaryColumns.Contains(colName)) return;

            if (e.Value is byte[] bytes)
            {
                e.Value = $"binary data ({bytes.Length} bytes)";
                e.FormattingApplied = true;
            }
        }
    }

    private void SetupFrozenColumnsMenu()
    {
        if (_freezeMenuInitialized) return;
        _freezeMenuInitialized = true;

        var menu = ContextMenuStrip ?? new ContextMenuStrip();
        var freezeItem = new ToolStripMenuItem("Freeze column");
        var unfreezeItem = new ToolStripMenuItem("Unfreeze column");
        var unfreezeAllItem = new ToolStripMenuItem("Unfreeze all columns");
        int selectedColumnIndex = -1;

        if (menu.Items.Count > 0)
        {
            menu.Items.Add(new ToolStripSeparator());
        }

        freezeItem.Click += (_, _) => FreezeColumn(selectedColumnIndex);
        unfreezeItem.Click += (_, _) => UnfreezeColumn(selectedColumnIndex);
        unfreezeAllItem.Click += (_, _) => UnfreezeAllColumns();

        menu.Items.Add(freezeItem);
        menu.Items.Add(unfreezeItem);
        menu.Items.Add(unfreezeAllItem);
        menu.Opening += (_, e) =>
        {
            Point clientPoint = PointToClient(Cursor.Position);
            var hit = HitTest(clientPoint.X, clientPoint.Y);
            selectedColumnIndex = hit.RowIndex >= 0 && hit.ColumnIndex >= 0
                ? hit.ColumnIndex
                : -1;

            if (selectedColumnIndex < 0)
            {
                freezeItem.Enabled = false;
                unfreezeItem.Enabled = false;
                return;
            }

            bool isFrozen = Columns[selectedColumnIndex].Frozen;
            freezeItem.Enabled = !isFrozen;
            unfreezeItem.Enabled = isFrozen;
        };

        ContextMenuStrip = menu;
    }

    private void FreezeColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Columns.Count) return;

        var column = Columns[columnIndex];
        if (column.Frozen) return;

        var columnsToFreeze = Columns
            .Cast<DataGridViewColumn>()
            .Where(c => c.Frozen)
            .OrderBy(c => c.DisplayIndex)
            .Append(column)
            .ToArray();

        UnfreezeAllColumns();

        var columnsInDisplayOrder = Columns
            .Cast<DataGridViewColumn>()
            .OrderBy(c => c.DisplayIndex)
            .ToList();

        columnsInDisplayOrder.Remove(column);
        columnsInDisplayOrder.Insert(columnsToFreeze.Length - 1, column);

        for (int i = 0; i < columnsInDisplayOrder.Count; i++)
        {
            columnsInDisplayOrder[i].DisplayIndex = i;
        }

        foreach (var frozenColumn in columnsToFreeze)
        {
            frozenColumn.Frozen = true;
        }

        Invalidate();
    }

    private void UnfreezeColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Columns.Count) return;

        var columnsToKeepFrozen = Columns
            .Cast<DataGridViewColumn>()
            .Where(c => c.Frozen && c.Index != columnIndex)
            .OrderBy(c => c.DisplayIndex)
            .ToArray();

        UnfreezeAllColumns();

        foreach (var column in columnsToKeepFrozen)
        {
            FreezeColumn(column.Index);
        }
    }

    private void UnfreezeAllColumns()
    {
        foreach (var column in Columns
            .Cast<DataGridViewColumn>()
            .Where(c => c.Frozen)
            .OrderByDescending(c => c.DisplayIndex))
        {
            column.Frozen = false;
        }

        Invalidate();
    }

    public void PrepareForDataRefresh()
    {
        UnfreezeAllColumns();
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
