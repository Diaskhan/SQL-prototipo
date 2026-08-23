using SQL_prototipo.Models;
using SQL_prototipo.Services;

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

        // Cancel any running query when a tab is closed via its close button.
        tabControl1.TabClosing += (_, e) =>
        {
            // Keep the Connections tab alive so it can be reopened from the menu.
            if (e.TabPage == tabPage2)
            {
                e.Cancel = true;
                tabControl1.TabPages.Remove(tabPage2);
                return;
            }

            if (e.TabPage.Tag is QueryTabContext ctx)
            {
                ctx.Cts?.Cancel();
            }
        };

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

        // If clicked on a table (child of Tables node), open a new query tab
        if (e.Node.Parent != null && e.Node.Parent.Text == "Tables")
        {
            OpenNewQueryTab($"SELECT * FROM {e.Node.Text};", e.Node.Text);
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

        // Only handle table nodes (direct children of a "Tables" node)
        if (node.Parent == null || node.Parent.Text != "Tables") return;

        // Already loaded (no placeholder) -> nothing to do
        if (node.Nodes.Count != 1 || node.Nodes[0].Name != "__placeholder__") return;

        try
        {
            var columns = await _dbService.GetColumnsAsync(node.Text);

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
                            foreach (var table in tables)
                            {
                                var tableNode = new TreeNode(table)
                                {
                                    ImageKey = "table",
                                    SelectedImageKey = "table"
                                };
                                // Placeholder so the node shows an expand [+] glyph
                                if (_settingsManager.Settings.ShowTableColumnsInTree)
                                {
                                    tableNode.Nodes.Add(new TreeNode("Loading...") { Name = "__placeholder__" });
                                }
                                tablesNode.Nodes.Add(tableNode);
                            }
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

            // Also update the Connections tab combobox
            if (cmbConnections.Items.Count > 0)
            {
                cmbConnections.SelectedItem = connection;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            SetStatus($"{result.RowCount} row(s) returned in {result.ElapsedMilliseconds} ms.");
        }
        else
        {
            grid.DataSource = null;
            SetStatus($"{result.RecordsAffected} row(s) affected in {result.ElapsedMilliseconds} ms.");
        }
        RecordHistory(query, true);
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
                foreach (var table in tables)
                {
                    var tableNode = new TreeNode(table)
                    {
                        ImageKey = "table",
                        SelectedImageKey = "table"
                    };
                    if (_settingsManager.Settings.ShowTableColumnsInTree)
                    {
                        tableNode.Nodes.Add(new TreeNode("Loading...") { Name = "__placeholder__" });
                    }
                    rootNode.Nodes.Add(tableNode);
                }
                treeView1.Nodes.Add(rootNode);
                treeView1.ExpandAll();
            }
            finally
            {
                treeView1.EndUpdate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        MessageBox.Show(
            "SQL prototipo\nA simple multi-database SQL query tool.",
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
        var dgv = new DataGridView
        {
            Dock = DockStyle.Bottom,
            Height = 250,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            ReadOnly = true,
            AllowUserToAddRows = false
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

        newTab.Controls.Add(dgv);
        newTab.Controls.Add(rtb);
        newTab.Controls.Add(toolbar);

        tabControl1.TabPages.Add(newTab);
        tabControl1.SelectedTab = newTab;

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
