using SQL_prototipo.Models;
using SQL_prototipo.Services;
using SQL_prototipo.UI;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    private DatabaseService _dbService;
    private ConnectionManager _connectionManager;
    private QueryHistoryManager _historyManager;
    private SettingsManager _settingsManager;
    private string _currentConnectionString = "Data Source=chinook.sqlite";
    private string _currentDatabaseType = "SQLite";
    private ConnectionInfo? _activeConnection;
    private bool _isLoadingUI = false;

    // Additional UI elements created programmatically
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private Button _btnTestConnection = null!;
    private Button _btnUpdateConnection = null!;
    private ListBox _listBoxHistory = null!;

    public MainForm()
    {
        InitializeComponent();
        _connectionManager = new ConnectionManager();
        _historyManager = new QueryHistoryManager();
        _settingsManager = new SettingsManager();
        _dbService = new DatabaseService(_currentConnectionString, _currentDatabaseType);
        treeView1.NodeMouseDoubleClick += TreeView1_NodeMouseDoubleClick;
        treeView1.BeforeExpand += TreeView1_BeforeExpand;
        txtTableFilter.TextChanged += (_, _) => ApplyTableFilter(txtTableFilter.Text);

        // F5 shortcut to execute queries
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;

        InitializeImageList();
        InitializeAdditionalUi();
        LoadConnectionsToUI();
        LoadHistoryToUI();

        // Remove design-time placeholder tabs so the right panel starts empty.
        tabControl1.TabPages.Clear();
    }

    private void InitializeAdditionalUi()
    {
        // --- Status bar ---
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_statusLabel);
        this.Controls.Add(_statusStrip);

        // --- Test Connection button (Connections tab) ---
        _btnTestConnection = new Button
        {
            Text = "Test Connection",
            Location = new Point(10, 295),
            Size = new Size(165, 40),
            UseVisualStyleBackColor = true
        };
        _btnTestConnection.Click += btnTestConnection_Click;

        // --- Update Connection button (Connections tab) ---
        _btnUpdateConnection = new Button
        {
            Text = "Update Connection",
            Location = new Point(186, 295),
            Size = new Size(165, 40),
            UseVisualStyleBackColor = true
        };
        _btnUpdateConnection.Click += btnUpdateConnection_Click;

        panel4.Controls.Add(_btnTestConnection);
        panel4.Controls.Add(_btnUpdateConnection);

        // --- Query history list (Queries tab) ---
        var historyLabel = new Label
        {
            Text = "Query History (double-click to load)",
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _listBoxHistory = new ListBox
        {
            Dock = DockStyle.Fill
        };
        _listBoxHistory.DoubleClick += listBoxHistory_DoubleClick;
        tabPageQueries.Controls.Add(_listBoxHistory);
        tabPageQueries.Controls.Add(historyLabel);

        // --- "Manage Connections" menu item ---
        var manageConnectionsMenuItem = new ToolStripMenuItem("&Manage Connections");
        manageConnectionsMenuItem.Click += (_, _) => OpenConnectionsTab();
        menuStrip1.Items.Insert(menuStrip1.Items.Count - 1, manageConnectionsMenuItem);
    }

    private void OpenConnectionsTab()
    {
        if (!tabControl1.TabPages.Contains(tabPage2))
        {
            tabControl1.TabPages.Add(tabPage2);
        }
        tabControl1.SelectedTab = tabPage2;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void LoadHistoryToUI()
    {
        _listBoxHistory.Items.Clear();
        foreach (var entry in _historyManager.GetRecent())
        {
            _listBoxHistory.Items.Add(entry);
        }
    }

    private void LoadConnectionsToUI()
    {
        // Load connections into treeView1
        treeView1.BeginUpdate();
        try
        {
            treeView1.Nodes.Clear();

            // Group connections by Group property
            var groups = _connectionManager.Connections
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Group) ? "Default" : c.Group)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                TreeNode groupNode = new TreeNode(group.Key)
                {
                    ImageKey = "folder",
                    SelectedImageKey = "folder"
                };

                foreach (var connection in group)
                {
                    bool isActive = _activeConnection != null && _activeConnection.Name == connection.Name;
                    string dbIconKey = TreeIconProvider.GetDatabaseIconKey(connection.DatabaseType, isActive);
                    TreeNode connectionNode = new TreeNode(connection.Name)
                    {
                        Tag = connection,
                        ImageKey = dbIconKey,
                        SelectedImageKey = dbIconKey
                    };
                    groupNode.Nodes.Add(connectionNode);
                }

                treeView1.Nodes.Add(groupNode);
            }

            treeView1.ExpandAll();
        }
        finally
        {
            treeView1.EndUpdate();
        }

        CaptureTreeBackupAndFilter();

        // Populate the Connections tab tree (folders -> connections) and the combobox
        PopulateConnectionsTree();
        cmbConnections.Items.Clear();

        foreach (var connection in _connectionManager.Connections)
        {
            cmbConnections.Items.Add(connection);
        }

        // If there are connections, select the first one
        // Use _isLoadingUI flag to prevent SelectedIndexChanged from triggering a table load
        _isLoadingUI = true;
        try
        {
            if (cmbConnections.Items.Count > 0)
            {
                cmbConnections.SelectedIndex = 0;
            }
        }
        finally
        {
            _isLoadingUI = false;
        }
    }

    /// <summary>
    /// Populates the Connections tab tree with a strict two-level hierarchy:
    /// folders at the root and their connections as children.
    /// </summary>
    private void PopulateConnectionsTree()
    {
        _isLoadingUI = true;
        try
        {
            treeViewConnections.BeginUpdate();
            treeViewConnections.Nodes.Clear();

            var connectionsByGroup = _connectionManager.Connections
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Group) ? "Default" : c.Group);

            foreach (var folderName in _connectionManager.Folders)
            {
                TreeNode folderNode = new TreeNode(folderName)
                {
                    Name = folderName,
                    Tag = folderName,
                    ImageKey = "folder",
                    SelectedImageKey = "folder"
                };

                var group = connectionsByGroup
                    .FirstOrDefault(g => string.Equals(g.Key, folderName, StringComparison.OrdinalIgnoreCase));

                if (group != null)
                {
                    foreach (var connection in group)
                    {
                        bool isActive = _activeConnection != null && _activeConnection.Name == connection.Name;
                        string dbIconKey = TreeIconProvider.GetDatabaseIconKey(connection.DatabaseType, isActive);
                        folderNode.Nodes.Add(new TreeNode(connection.Name)
                        {
                            Tag = connection,
                            ImageKey = dbIconKey,
                            SelectedImageKey = dbIconKey
                        });
                    }
                }

                treeViewConnections.Nodes.Add(folderNode);
            }

            treeViewConnections.ExpandAll();
        }
        finally
        {
            treeViewConnections.EndUpdate();
            _isLoadingUI = false;
        }
    }

    private void RefreshConnectionsList()
    {
        _connectionManager.Refresh();
        LoadConnectionsToUI();
    }

    private async void TreeView1_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node == null) return;

        // If clicked on a table node, open a new query tab (schema-qualified, limited to 1000 rows)
        if (e.Node.Tag is TableRef table)
        {
            var query = _dbService.BuildSelectTopQuery(table.Schema, table.Name, 1000);
            OpenNewQueryTab(query, table.Name);
            return;
        }

        // If clicked on a connection node, load its tables
        if (e.Node.Tag is ConnectionInfo connection)
        {
            await LoadTablesForConnection(connection);
            return;
        }
    }

    private async void TreeView1_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        var node = e.Node;
        if (node == null) return;

        // Only handle table nodes (identified by a TableRef tag)
        if (node.Tag is not TableRef table) return;

        // Already loaded (no placeholder) -> nothing to do
        if (node.Nodes.Count != 1 || node.Nodes[0].Name != "__placeholder__") return;

        try
        {
            var columns = await _dbService.GetColumnsAsync(table.Schema, table.Name);

            node.Nodes.Clear();
            foreach (var (name, type) in columns)
            {
                var label = string.IsNullOrEmpty(type) ? name : $"{name} : {type}";
                node.Nodes.Add(new TreeNode(label)
                {
                    ImageKey = "table",
                    SelectedImageKey = "table"
                });
            }

            if (node.Nodes.Count == 0)
            {
                node.Nodes.Add(new TreeNode("(no columns)"));
            }
        }
        catch (Exception ex)
        {
            node.Nodes.Clear();
            node.Nodes.Add(new TreeNode($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Populates the given "Tables" node with table nodes. When the active
    /// provider supports schemas, tables are grouped under schema folder nodes.
    /// </summary>
    private void PopulateTablesNode(TreeNode tablesNode, IEnumerable<TableRef> tables)
    {
        bool useSchemas = _dbService.SupportsSchemas && tables.Any(t => t.HasSchema);

        if (useSchemas)
        {
            var schemaGroups = tables
                .GroupBy(t => t.HasSchema ? t.Schema : "(default)")
                .OrderBy(g => g.Key);

            foreach (var schemaGroup in schemaGroups)
            {
                var schemaNode = new TreeNode(schemaGroup.Key)
                {
                    ImageKey = "folder",
                    SelectedImageKey = "folder"
                };
                foreach (var table in schemaGroup.OrderBy(t => t.Name))
                {
                    schemaNode.Nodes.Add(CreateTableNode(table));
                }
                tablesNode.Nodes.Add(schemaNode);
            }
        }
        else
        {
            foreach (var table in tables)
            {
                tablesNode.Nodes.Add(CreateTableNode(table));
            }
        }
    }

    private TreeNode CreateTableNode(TableRef table)
    {
        var tableNode = new TreeNode(table.Name)
        {
            Tag = table,
            ImageKey = "table",
            SelectedImageKey = "table"
        };
        if (_settingsManager.Settings.ShowTableColumnsInTree)
        {
            tableNode.Nodes.Add(new TreeNode("Loading...") { Name = "__placeholder__" });
        }
        return tableNode;
    }

    // Holds an unfiltered snapshot of the Database tree so the table filter
    // can be applied and cleared without reloading from the database.
    private List<TreeNode> _treeBackup = new();

    /// <summary>
    /// Takes a snapshot of the freshly built Database tree and re-applies the
    /// current table filter (if any). Call this after (re)building treeView1.
    /// </summary>
    private void CaptureTreeBackupAndFilter()
    {
        _treeBackup = treeView1.Nodes.Cast<TreeNode>()
            .Select(n => (TreeNode)n.Clone())
            .ToList();

        if (!string.IsNullOrWhiteSpace(txtTableFilter.Text))
        {
            ApplyTableFilter(txtTableFilter.Text);
        }
    }

    /// <summary>
    /// Filters the Database tree by table name. An empty filter restores the
    /// full tree. A '*'/'?' pattern is matched as a wildcard; otherwise the
    /// filter is treated as a "contains" (i.e. *filter*) match.
    /// </summary>
    private void ApplyTableFilter(string? filter)
    {
        filter = filter?.Trim() ?? string.Empty;

        treeView1.BeginUpdate();
        try
        {
            treeView1.Nodes.Clear();

            if (string.IsNullOrEmpty(filter))
            {
                foreach (var node in _treeBackup)
                {
                    treeView1.Nodes.Add((TreeNode)node.Clone());
                }
            }
            else
            {
                foreach (var node in _treeBackup)
                {
                    var filtered = FilterNode(node, filter);
                    if (filtered != null)
                    {
                        treeView1.Nodes.Add(filtered);
                    }
                }
            }

            treeView1.ExpandAll();
        }
        finally
        {
            treeView1.EndUpdate();
        }
    }

    /// <summary>
    /// Returns a clone of <paramref name="source"/> if it is a matching table
    /// node or a container that has at least one matching descendant; otherwise null.
    /// </summary>
    private TreeNode? FilterNode(TreeNode source, string filter)
    {
        if (source.Tag is TableRef)
        {
            if (!MatchesFilter(source.Text, filter))
            {
                return null;
            }

            var tableClone = CloneShallow(source);
            foreach (TreeNode child in source.Nodes)
            {
                tableClone.Nodes.Add((TreeNode)child.Clone());
            }
            return tableClone;
        }

        var matchingChildren = new List<TreeNode>();
        foreach (TreeNode child in source.Nodes)
        {
            var filteredChild = FilterNode(child, filter);
            if (filteredChild != null)
            {
                matchingChildren.Add(filteredChild);
            }
        }

        if (matchingChildren.Count == 0)
        {
            return null;
        }

        var clone = CloneShallow(source);
        foreach (var child in matchingChildren)
        {
            clone.Nodes.Add(child);
        }
        return clone;
    }

    private static TreeNode CloneShallow(TreeNode source) => new TreeNode(source.Text)
    {
        Name = source.Name,
        Tag = source.Tag,
        ImageKey = source.ImageKey,
        SelectedImageKey = source.SelectedImageKey
    };

    private static bool MatchesFilter(string text, string filter)
    {
        if (filter.Contains('*') || filter.Contains('?'))
        {
            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(filter)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(
                text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Flattens an exception chain (including InnerException) into a readable
    /// message so the real root cause is not hidden behind a wrapper exception.
    /// </summary>
    private static string DescribeException(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        var current = ex;
        while (current != null)
        {
            sb.AppendLine($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }
        return sb.ToString().TrimEnd();
    }

    private async Task LoadTablesForConnection(ConnectionInfo connection)
    {
        try
        {
            ToggleUiState(false);

            // Switch to the selected connection
            _currentConnectionString = connection.ConnectionString;
            _currentDatabaseType = connection.DatabaseType;
            _dbService = new DatabaseService(connection);

            // Load tables from this connection
            var tables = await _dbService.GetAllTablesAsync();

            _activeConnection = connection;

            // Update treeView1 to show connection with its tables
            treeView1.BeginUpdate();
            try
            {
                treeView1.Nodes.Clear();

                // Group connections by Group property
                var groups = _connectionManager.Connections
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.Group) ? "Default" : c.Group)
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    TreeNode groupNode = new TreeNode(group.Key)
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folder"
                    };

                    foreach (var conn in group)
                    {
                        bool isActive = _activeConnection != null && _activeConnection.Name == conn.Name;
                        string dbIconKey = TreeIconProvider.GetDatabaseIconKey(conn.DatabaseType, isActive);
                        TreeNode connectionNode = new TreeNode(conn.Name)
                        {
                            Tag = conn,
                            ImageKey = dbIconKey,
                            SelectedImageKey = dbIconKey
                        };

                        // If this is the current connection, add its tables
                        if (conn.Name == connection.Name)
                        {
                            TreeNode tablesNode = new TreeNode("Tables")
                            {
                                ImageKey = "folder",
                                SelectedImageKey = "folder"
                            };
                            PopulateTablesNode(tablesNode, tables);
                            connectionNode.Nodes.Add(tablesNode);
                        }

                        groupNode.Nodes.Add(connectionNode);
                    }

                    treeView1.Nodes.Add(groupNode);
                }

                treeView1.ExpandAll();
            }
            finally
            {
                treeView1.EndUpdate();
            }

            CaptureTreeBackupAndFilter();

            // Also update the Connections tab combobox
            if (cmbConnections.Items.Count > 0)
            {
                cmbConnections.SelectedItem = connection;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tables:\n{DescribeException(ex)}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUiState(true);
        }
    }

    private async void btnExecuteQuery_Click(object sender, EventArgs e)
    {
        try
        {
            ToggleUiState(false);
            var query = richTextBox1.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            SetStatus("Executing query...");
            var result = await _dbService.ExecuteQueryAsync(query);
            ApplyQueryResult(result, dataGridView1, query);
        }
        catch (Exception ex)
        {
            RecordHistory(richTextBox1.Text.Trim(), false);
            SetStatus("Query failed.");
            MessageBox.Show($"Error executing query: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUiState(true);
        }
    }

    private void ApplyQueryResult(Models.QueryResult result, DataGridView grid, string query)
    {
        if (result.HasResultSet)
        {
            grid.DataSource = result.Data;
            ConfigureBinaryColumns(grid, result.Data);
            SetStatus($"{result.RowCount} row(s) returned in {result.ElapsedMilliseconds} ms.");
        }
        else
        {
            grid.DataSource = null;
            SetStatus($"{result.RecordsAffected} row(s) affected in {result.ElapsedMilliseconds} ms.");
        }
        RecordHistory(query, true);
    }

    // Replaces auto-generated image columns (for byte[] data) with text columns
    // showing a "binary data" placeholder. Otherwise DataGridView tries to render
    // raw bytes as an image and throws "Parameter is not valid" (GDI+ ArgumentException).
    private void ConfigureBinaryColumns(DataGridView grid, System.Data.DataTable? data)
    {
        if (data == null) return;

        var binaryColumns = data.Columns.Cast<System.Data.DataColumn>()
            .Where(c => c.DataType == typeof(byte[]))
            .Select(c => c.ColumnName)
            .ToHashSet(StringComparer.Ordinal);

        if (binaryColumns.Count == 0) return;

        foreach (var colName in binaryColumns)
        {
            var existing = grid.Columns[colName];
            if (existing == null) continue;

            int index = existing.Index;
            var textColumn = new DataGridViewTextBoxColumn
            {
                Name = existing.Name,
                HeaderText = existing.HeaderText,
                DataPropertyName = existing.DataPropertyName,
                ReadOnly = true
            };

            grid.Columns.RemoveAt(index);
            grid.Columns.Insert(index, textColumn);
        }

        // Detach any previous handler to avoid stacking on re-execution.
        grid.CellFormatting -= BinaryCellFormatting;
        grid.CellFormatting += BinaryCellFormatting;

        void BinaryCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            var colName = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrEmpty(colName)) colName = grid.Columns[e.ColumnIndex].Name;
            if (!binaryColumns.Contains(colName)) return;

            if (e.Value is byte[] bytes)
            {
                e.Value = $"binary data ({bytes.Length} bytes)";
                e.FormattingApplied = true;
            }
        }
    }

    private void RecordHistory(string query, bool success)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        var connName = _activeConnection?.Name ?? _currentDatabaseType;
        _historyManager.Add(query, connName, success);
        LoadHistoryToUI();
    }

    private void listBoxHistory_DoubleClick(object? sender, EventArgs e)
    {
        if (_listBoxHistory.SelectedItem is Models.QueryHistoryEntry entry)
        {
            OpenNewQueryTab(entry.Query, "History");
        }
    }

    private async void btnLoadTables_Click(object sender, EventArgs e)
    {
        try
        {
            ToggleUiState(false);
            var tables = await _dbService.GetAllTablesAsync();

            treeView1.BeginUpdate();
            try
            {
                treeView1.Nodes.Clear();
                TreeNode rootNode = new TreeNode("Tables")
                {
                    ImageKey = "folder",
                    SelectedImageKey = "folder"
                };
                PopulateTablesNode(rootNode, tables);
                treeView1.Nodes.Add(rootNode);
                treeView1.ExpandAll();
            }
            finally
            {
                treeView1.EndUpdate();
            }

            CaptureTreeBackupAndFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tables:\n{DescribeException(ex)}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUiState(true);
        }
    }

    private void ToggleUiState(bool enabled)
    {
        button2.Enabled = enabled;
        button1.Enabled = enabled;

        // Also toggle the active query tab's Execute/Cancel buttons if present
        var activeTab = tabControl1.SelectedTab;
        if (activeTab?.Tag is QueryTabContext ctx)
        {
            ctx.ExecuteButton.Enabled = enabled;
            ctx.CancelButton.Enabled = !enabled;
        }

        this.Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;
    }

    // Connection Management Event Handlers
    private void btnAddConnection_Click(object sender, EventArgs e)
    {
        try
        {
            var name = txtConnectionName.Text.Trim();
            var connectionString = txtConnectionString.Text.Trim();
            var databaseType = cmbConnectionType.SelectedItem?.ToString() ?? "SQLite";
            var group = txtGroup.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a connection name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Please enter a connection string.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _connectionManager.AddConnection(name, connectionString, databaseType, group);
            MessageBox.Show("Connection added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clear inputs and refresh list
            txtConnectionName.Clear();
            txtConnectionString.Clear();
            txtGroup.Clear();
            cmbConnectionType.SelectedIndex = 0;
            RefreshConnectionsList();
            LoadConnectionsToUI();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Duplicate Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnAddFolder_Click(object sender, EventArgs e)
    {
        using var dialog = new Form
        {
            Text = "Add Folder",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(320, 110)
        };

        var label = new Label { Text = "Folder name:", Location = new Point(12, 15), AutoSize = true };
        var textBox = new TextBox { Location = new Point(12, 38), Size = new Size(296, 23) };
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(152, 72), Size = new Size(75, 26) };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(233, 72), Size = new Size(75, 26) };

        dialog.Controls.AddRange(new Control[] { label, textBox, btnOk, btnCancel });
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var folderName = textBox.Text.Trim();
        if (string.IsNullOrEmpty(folderName))
        {
            MessageBox.Show("Please enter a folder name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _connectionManager.AddFolder(folderName);
            LoadConnectionsToUI();
            SetStatus($"Folder '{folderName}' added.");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Duplicate Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnDeleteConnection_Click(object sender, EventArgs e)
    {
        try
        {
            if (treeViewConnections.SelectedNode?.Tag is not ConnectionInfo selectedConnection)
            {
                MessageBox.Show("Please select a connection to delete.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete '{selectedConnection.Name}'?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _connectionManager.DeleteConnection(selectedConnection.Name);
                MessageBox.Show("Connection deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_activeConnection != null && _activeConnection.Name == selectedConnection.Name)
                {
                    _activeConnection = null;
                }
                RefreshConnectionsList();
                LoadConnectionsToUI();

                // Clear inputs
                txtConnectionName.Clear();
                txtConnectionString.Clear();
                txtGroup.Clear();
                cmbConnectionType.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnTestConnection_Click(object sender, EventArgs e)
    {
        var name = txtConnectionName.Text.Trim();
        var connectionString = txtConnectionString.Text.Trim();
        var databaseType = cmbConnectionType.SelectedItem?.ToString() ?? "SQLite";

        if (string.IsNullOrEmpty(connectionString))
        {
            MessageBox.Show("Please enter a connection string to test.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SetStatus("Testing connection...");
            this.Cursor = Cursors.WaitCursor;

            var service = new DatabaseService(connectionString, databaseType);
            await service.TestConnectionAsync();

            SetStatus("Connection test succeeded.");
            MessageBox.Show($"Connection '{(string.IsNullOrEmpty(name) ? databaseType : name)}' succeeded.", "Test Connection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus("Connection test failed.");
            MessageBox.Show($"Connection failed: {ex.Message}", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }

    private void btnUpdateConnection_Click(object sender, EventArgs e)
    {
        try
        {
            var name = txtConnectionName.Text.Trim();
            var connectionString = txtConnectionString.Text.Trim();
            var databaseType = cmbConnectionType.SelectedItem?.ToString() ?? "SQLite";
            var group = txtGroup.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please select or enter a connection name to update.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Please enter a connection string.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _connectionManager.UpdateConnection(name, connectionString, databaseType, group);
            MessageBox.Show("Connection updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshConnectionsList();
            LoadConnectionsToUI();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Update Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void treeViewConnections_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_isLoadingUI) return;

        if (e.Node?.Tag is ConnectionInfo connection)
        {
            txtConnectionName.Text = connection.Name;
            txtConnectionString.Text = connection.ConnectionString;
            cmbConnectionType.SelectedItem = connection.DatabaseType;
            txtGroup.Text = connection.Group;
        }
        else if (e.Node?.Tag is string folderName)
        {
            // A folder is selected: prefill the group so a new connection lands here.
            txtGroup.Text = folderName;
        }
    }

    private async void treeViewConnections_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        // Double-clicking a connection switches to it and loads its tables.
        if (e.Node?.Tag is ConnectionInfo connection)
        {
            try
            {
                _currentConnectionString = connection.ConnectionString;
                _currentDatabaseType = connection.DatabaseType;
                _dbService = new DatabaseService(connection);
                await LoadTablesForConnection(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void cmbConnections_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isLoadingUI) return;
        if (cmbConnections.SelectedItem is ConnectionInfo connection)
        {
            try
            {
                _currentConnectionString = connection.ConnectionString;
                _currentDatabaseType = connection.DatabaseType;
                _dbService = new DatabaseService(connection);

                // Load tables for this connection
                await LoadTablesForConnection(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void InitializeImageList()
    {
        ImageList imageList = TreeIconProvider.CreateImageList();
        treeView1.ImageList = imageList;
        treeViewConnections.ImageList = imageList;
    }

    private void newQueryMenuItem_Click(object? sender, EventArgs e)
    {
        OpenNewQueryTab(_dbService.GetNewQueryTemplate(), "Query", autoExecute: false);
    }

    private void executeQueryMenuItem_Click(object? sender, EventArgs e)
    {
        var activeTab = tabControl1.SelectedTab;
        if (activeTab?.Tag is QueryTabContext ctx)
        {
            ExecuteQueryInTab(ctx);
        }
        else
        {
            btnExecuteQuery_Click(this, EventArgs.Empty);
        }
    }

    private void exitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private async void settingsMenuItem_Click(object? sender, EventArgs e)
    {
        bool previousShowColumns = _settingsManager.Settings.ShowTableColumnsInTree;

        using var dialog = new SettingsForm(_settingsManager.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _settingsManager.Save();

        // Rebuild the object tree if the column visibility setting changed
        if (previousShowColumns != _settingsManager.Settings.ShowTableColumnsInTree
            && _activeConnection != null)
        {
            await LoadTablesForConnection(_activeConnection);
        }
    }

    private void aboutMenuItem_Click(object? sender, EventArgs e)
    {
        var version = BuildInfo.Version;
        var metadata = BuildInfo.Metadata;
        var buildLine = string.IsNullOrEmpty(metadata)
            ? $"Version: {version}"
            : $"Version: {version}\nBuild: {metadata}";

        MessageBox.Show(
            $"SQL prototipo\nA simple multi-database SQL query tool.\n\n{buildLine}",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            e.Handled = true;
            // Execute query in the currently active query tab
            var activeTab = tabControl1.SelectedTab;
            if (activeTab?.Tag is QueryTabContext ctx)
            {
                ExecuteQueryInTab(ctx);
            }
            else
            {
                // Fallback to the static first tab
                btnExecuteQuery_Click(this, EventArgs.Empty);
            }
        }
    }

    private void OpenNewQueryTab(string query, string tableName, bool autoExecute = true)
    {
        // Build a unique tab title
        int tabCount = tabControl1.TabPages.Cast<TabPage>()
            .Count(tp => tp.Tag is QueryTabContext);
        string title = $"{tableName} ({tabCount + 1})";

        // --- RichTextBox (query editor) ---
        var rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Text = query,
            Font = richTextBox1.Font,
            ScrollBars = RichTextBoxScrollBars.Both
        };

        // --- DataGridView (results) ---
        var dgv = new BufferedDataGridView
        {
            Dock = DockStyle.Bottom,
            Height = 250,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            ReadOnly = true,
            AllowUserToAddRows = false
        };

        // --- Splitter between query editor and results grid ---
        var splitter = new Splitter
        {
            Dock = DockStyle.Bottom,
            Height = 4,
            MinExtra = 100,
            MinSize = 80,
            TabStop = false
        };

        // --- Execute button ---
        var btnExec = new Button
        {
            Text = "Execute Query",
            Dock = DockStyle.Left,
            Width = 142,
            Height = 50
        };

        // --- Cancel button ---
        var btnCancel = new Button
        {
            Text = "Cancel",
            Dock = DockStyle.Left,
            Width = 100,
            Height = 50,
            Enabled = false
        };

        // --- Close tab button ---
        var btnClose = new Button
        {
            Text = "✕ Close Tab",
            Dock = DockStyle.Right,
            Width = 100,
            Height = 50
        };

        // --- Toolbar panel ---
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 50 };
        toolbar.Controls.Add(btnCancel);
        toolbar.Controls.Add(btnExec);
        toolbar.Controls.Add(btnClose);

        // --- New TabPage ---
        var newTab = new TabPage(title)
        {
            Padding = new Padding(3),
            UseVisualStyleBackColor = true
        };

        var ctx = new QueryTabContext(rtb, dgv, btnExec, btnCancel);
        // Store references so F5 and ToggleUiState can find the right context
        newTab.Tag = ctx;

        newTab.Controls.Add(rtb);
        newTab.Controls.Add(splitter);
        newTab.Controls.Add(dgv);
        newTab.Controls.Add(toolbar);

        tabControl1.TabPages.Add(newTab);
        tabControl1.SelectedTab = newTab;

        // Make the query editor and the results grid share the height 50/50.
        void SizeGridToHalf()
        {
            int available = newTab.ClientSize.Height - toolbar.Height - splitter.Height;
            if (available > 0)
            {
                dgv.Height = available / 2;
            }
        }

        // Apply the initial 50/50 split once the tab has a real size, then stop
        // so the user's manual splitter drags are preserved on later resizes.
        void OnFirstSize(object? sender, EventArgs e)
        {
            if (newTab.ClientSize.Height <= 0) return;
            SizeGridToHalf();
            newTab.SizeChanged -= OnFirstSize;
        }
        newTab.SizeChanged += OnFirstSize;
        SizeGridToHalf();

        // Wire up events
        btnExec.Click += (_, _) => ExecuteQueryInTab(ctx);
        btnCancel.Click += (_, _) => ctx.Cts?.Cancel();
        btnClose.Click += (_, _) =>
        {
            ctx.Cts?.Cancel();
            tabControl1.TabPages.Remove(newTab);
            newTab.Dispose();
        };

        // Auto-execute on open
        if (autoExecute)
        {
            ExecuteQueryInTab(ctx);
        }
    }

    private async void ExecuteQueryInTab(QueryTabContext ctx)
    {
        var query = ctx.Editor.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        using var cts = new CancellationTokenSource();
        ctx.Cts = cts;
        try
        {
            ToggleUiState(false);
            SetStatus("Executing query...");

            var result = await _dbService.ExecuteQueryAsync(query, cts.Token);
            ApplyQueryResult(result, ctx.Grid, query);
        }
        catch (OperationCanceledException)
        {
            RecordHistory(query, false);
            SetStatus("Query canceled.");
        }
        catch (Exception ex)
        {
            RecordHistory(query, false);
            SetStatus("Query failed.");
            MessageBox.Show($"Error executing query: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ctx.Cts = null;
            ToggleUiState(true);
        }
    }

    private sealed class QueryTabContext
    {
        public RichTextBox Editor { get; }
        public DataGridView Grid { get; }
        public Button ExecuteButton { get; }
        public Button CancelButton { get; }
        public CancellationTokenSource? Cts { get; set; }

        public QueryTabContext(RichTextBox editor, DataGridView grid, Button executeButton, Button cancelButton)
        {
            Editor = editor;
            Grid = grid;
            ExecuteButton = executeButton;
            CancelButton = cancelButton;
        }
    }
}
