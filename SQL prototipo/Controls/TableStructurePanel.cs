using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo.Controls;

public sealed class TableStructurePanel : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly Button _addButton = new() { Text = "Add column" };
    private readonly Button _deleteButton = new() { Text = "Delete column" };
    private readonly Button _saveButton = new() { Text = "Save" };
    private readonly Button _refreshButton = new() { Text = "Refresh" };
    private readonly Label _infoLabel = new() { AutoSize = true, Dock = DockStyle.Fill };
    private DatabaseService? _service;
    private string _schema = string.Empty;
    private string _tableName = string.Empty;

    public event EventHandler? StructureSaved;

    public TableStructurePanel()
    {
        Dock = DockStyle.Fill;
        BuildUi();
    }

    public async Task LoadTableAsync(DatabaseService service, TableRef table)
    {
        _service = service;
        _schema = table.Schema;
        _tableName = table.Name;
        _infoLabel.Text = $"Table structure: {(_schema.Length == 0 ? table.Name : $"{_schema}.{table.Name}")}";
        await ReloadAsync();
    }

    private void BuildUi()
    {
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(6),
            WrapContents = false
        };
        buttons.Controls.AddRange([_addButton, _deleteButton, _saveButton, _refreshButton]);

        var header = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 4, 8, 0) };
        header.Controls.Add(_infoLabel);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 24 });
        var dataTypeColumn = new DataGridViewComboBoxColumn
        {
            Name = "DataType",
            HeaderText = "SQL type",
            FillWeight = 24,
            FlatStyle = FlatStyle.Flat
        };
        dataTypeColumn.Items.AddRange(CreateDataTypeList().ToArray());
        _grid.Columns.Add(dataTypeColumn);
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Nullable", HeaderText = "Nullable", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Default", HeaderText = "Default (new columns)", FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OriginalName", Visible = false });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsNew", Visible = false });

        _addButton.Click += (_, _) => AddColumn();
        _deleteButton.Click += (_, _) => DeleteColumn();
        _saveButton.Click += async (_, _) => await SaveAsync();
        _refreshButton.Click += async (_, _) => await ReloadAsync();

        Controls.Add(_grid);
        Controls.Add(header);
        Controls.Add(buttons);
    }

    private async Task ReloadAsync()
    {
        if (_service == null || string.IsNullOrEmpty(_tableName)) return;

        try
        {
            SetBusy(true);
            var columns = await _service.GetTableDefinitionAsync(_schema, _tableName);
            _grid.Rows.Clear();
            foreach (var column in columns)
            {
                AddDataTypeIfMissing(column.DataType);
                _grid.Rows.Add(column.Name, column.DataType, column.IsNullable, column.DefaultValue,
                    column.OriginalName, column.IsNew);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void AddColumn()
    {
        var rowIndex = _grid.Rows.Add("NewColumn", "nvarchar(100)", true, string.Empty, string.Empty, true);
        _grid.CurrentCell = _grid.Rows[rowIndex].Cells[0];
        _grid.BeginEdit(true);
    }

    private void DeleteColumn()
    {
        if (_grid.CurrentRow is { IsNewRow: false })
        {
            _grid.Rows.Remove(_grid.CurrentRow);
        }
    }

    private async Task SaveAsync()
    {
        if (_service == null) return;

        try
        {
            SetBusy(true);
            _grid.EndEdit();
            var columns = new List<TableColumnDefinition>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                columns.Add(new TableColumnDefinition
                {
                    Name = Convert.ToString(row.Cells["Name"].Value)?.Trim() ?? string.Empty,
                    DataType = Convert.ToString(row.Cells["DataType"].Value)?.Trim() ?? string.Empty,
                    IsNullable = Convert.ToBoolean(row.Cells["Nullable"].Value ?? false),
                    DefaultValue = Convert.ToString(row.Cells["Default"].Value)?.Trim() ?? string.Empty,
                    OriginalName = Convert.ToString(row.Cells["OriginalName"].Value) ?? string.Empty,
                    IsNew = Convert.ToBoolean(row.Cells["IsNew"].Value ?? false)
                });
            }

            await _service.ApplyTableDefinitionAsync(_schema, _tableName, columns);
            MessageBox.Show("Table structure saved.", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await ReloadAsync();
            StructureSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _grid.Enabled = !busy;
        _addButton.Enabled = !busy;
        _deleteButton.Enabled = !busy;
        _saveButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private static void ShowError(Exception ex) =>
        MessageBox.Show(ex.Message, "Table structure error", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void AddDataTypeIfMissing(string dataType)
    {
        if (_grid.Columns["DataType"] is DataGridViewComboBoxColumn column &&
            !column.Items.Contains(dataType))
        {
            column.Items.Add(dataType);
        }
    }

    private static List<string> CreateDataTypeList() =>
    [
        "bigint", "int", "smallint", "tinyint", "bit",
        "decimal(18,2)", "numeric(18,2)", "money", "smallmoney",
        "float", "real", "date", "datetime", "datetime2", "smalldatetime", "time",
        "uniqueidentifier", "char(10)", "varchar(50)", "varchar(255)", "varchar(MAX)",
        "nchar(10)", "nvarchar(50)", "nvarchar(100)", "nvarchar(255)", "nvarchar(MAX)",
        "binary(16)", "varbinary(MAX)", "xml"
    ];
}
