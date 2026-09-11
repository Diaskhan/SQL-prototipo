using SQL_prototipo.Controls;
using SQL_prototipo.Models;
using SQL_prototipo.Services;
using SQL_prototipo.TSql;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    private DatabaseService _dbService;
    private readonly ConnectionManager _connectionManager;
    private readonly QueryHistoryManager _historyManager;
    private readonly SettingsManager _settingsManager;
    private string _currentConnectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true";
    private string _currentDatabaseType = "SqlServer";
    private ConnectionInfo? _activeConnection;

    // Additional UI elements created programmatically
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private HistoryPanel _historyPanel = null!;

    public MainForm()
    {
        InitializeComponent();
        _connectionManager = new ConnectionManager();
        _historyManager = new QueryHistoryManager();
        _settingsManager = new SettingsManager();
        _dbService = new DatabaseService(_currentConnectionString, _currentDatabaseType);
        treeView1.NodeMouseDoubleClick += TreeView1_NodeMouseDoubleClick;
        treeView1.NodeMouseClick += TreeView1_NodeMouseClick;
        treeView1.BeforeExpand += TreeView1_BeforeExpand;
        txtTableFilter.TextChanged += (_, _) => ApplyTableFilter(txtTableFilter.Text);

        // Connection management is delegated to the ConnectionsPanel user control.
        connectionsPanel.Initialize(_connectionManager);
        connectionsPanel.ConnectionActivated += ConnectionsPanel_ConnectionActivated;
        connectionsPanel.ConnectionsChanged += (_, _) => OnConnectionsChanged();
        connectionsPanel.StatusChanged += (_, message) => SetStatus(message);

        // F5 shortcut to execute queries
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;

        InitializeImageList();
        QueryTabPanel.ApplySqlHighlighting(richTextBox1);
        InitializeAdditionalUi();
        RefreshDatabaseTree();

        // Remove design-time placeholder tabs so the right panel starts empty.
        tabControl1.TabPages.Clear();
    }

    private void TreeView1_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.Node?.Tag is not TableRef table)
        {
            return;
        }

        treeView1.SelectedNode = e.Node;
        var menu = new ContextMenuStrip();
        var structureItem = new ToolStripMenuItem("View and edit table structure");
        structureItem.Click += (_, _) => OpenTableStructureTab(table);
        menu.Items.Add(structureItem);

        var createStatementItem = new ToolStripMenuItem("SQL Create statement");
        createStatementItem.Click += (_, _) => _ = OpenCreateStatementTabAsync(table);
        menu.Items.Add(createStatementItem);

        menu.Show(treeView1, e.Location);
    }

    private async Task OpenCreateStatementTabAsync(TableRef table)
    {
        if (_dbService == null)
        {
            return;
        }

        try
        {
            var columns = await _dbService.GetTableDefinitionAsync(table.Schema, table.Name);
            var script = new MsSqlStatementGenerator(_dbService).BuildCreateTableScript(table, columns);

            QueryTabPanel.Open(tabControl1, script, $"Create: {table.Name}",
                () => _dbService, SetStatus, busy => ToggleUiState(!busy), RecordHistory,
                autoExecute: false,
                maxAutocompleteSuggestions: _settingsManager.Settings.MaxAutocompleteSuggestions);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to generate CREATE statement: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenTableStructureTab(TableRef table)
    {
        var tabKey = $"{_activeConnection?.Name}|{table.Schema}|{table.Name}";
        var existing = tabControl1.TabPages.Cast<TabPage>()
            .FirstOrDefault(page => string.Equals(page.Name, tabKey, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            tabControl1.SelectedTab = existing;
            return;
        }

        var panel = new TableStructurePanel();
        var tab = new TabPage($"Structure: {table.Name}")
        {
            Name = tabKey,
            Padding = new Padding(3)
        };
        tab.Controls.Add(panel);
        panel.StructureSaved += async (_, _) =>
        {
            if (_activeConnection != null)
            {
                await LoadTablesForConnection(_activeConnection);
            }
        };
        tabControl1.TabPages.Add(tab);
        tabControl1.SelectedTab = tab;
        _ = LoadTableStructureAsync(panel, table);
    }

    private async Task LoadTableStructureAsync(TableStructurePanel panel, TableRef table)
    {
        try
        {
            await panel.LoadTableAsync(_dbService, table);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to load table structure: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InitializeAdditionalUi()
    {
        // --- Status bar ---
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_statusLabel);
        this.Controls.Add(_statusStrip);



        // --- Query history list (Queries tab) ---
        _historyPanel = new HistoryPanel { Dock = DockStyle.Fill };
        _historyPanel.Initialize(_historyManager);
        _historyPanel.EntryActivated += (_, entry) =>
        {
            QueryTabPanel.Open(tabControl1, entry.Query, "History",
                () => _dbService, SetStatus, busy => ToggleUiState(!busy), RecordHistory,
                maxAutocompleteSuggestions: _settingsManager.Settings.MaxAutocompleteSuggestions);
        };
        tabPageQueries.Controls.Add(_historyPanel);

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

    private void RefreshDatabaseTree()
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
                TreeNode groupNode = new(group.Key)
                {
                    ImageKey = "folder",
                    SelectedImageKey = "folder"
                };

                foreach (var connection in group)
                {
                    groupNode.Nodes.Add(CreateConnectionNode(connection));
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
    }

    private TreeNode CreateConnectionNode(ConnectionInfo connection)
    {
        bool isActive = _activeConnection?.Name == connection.Name;
        string dbIconKey = TreeIconProvider.GetDatabaseIconKey(connection.DatabaseType, isActive);
        return new TreeNode(connection.Name)
        {
            Tag = connection,
            ImageKey = dbIconKey,
            SelectedImageKey = dbIconKey
        };
    }

    /// <summary>
    /// Reacts to add/update/delete/folder changes raised by the connections panel:
    /// clears a stale active connection and rebuilds the database object tree.
    /// </summary>
    private void OnConnectionsChanged()
    {
        if (_activeConnection != null && _connectionManager.GetConnection(_activeConnection.Name) == null)
        {
            _activeConnection = null;
        }
        RefreshDatabaseTree();
        connectionsPanel.ActiveConnection = _activeConnection;
    }

    private async void ConnectionsPanel_ConnectionActivated(object? sender, ConnectionInfo connection)
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

    private async void TreeView1_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node == null) return;

        // If clicked on a table node, open a new query tab (schema-qualified, limited to 1000 rows)
        if (e.Node.Tag is TableRef table)
        {
            var query = _dbService.BuildSelectTopQuery(table.Schema, table.Name, 1000);
            QueryTabPanel.Open(tabControl1, query, table.Name,
                () => _dbService, SetStatus, busy => ToggleUiState(!busy), RecordHistory,
                maxAutocompleteSuggestions: _settingsManager.Settings.MaxAutocompleteSuggestions);
            return;
        }

        // If clicked on a connection node, load its tables
        if (e.Node.Tag is ConnectionInfo connection)
        {
            if (e.Node.Nodes.Count > 0)
            {
                return;
            }

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
    private List<TreeNode> _treeBackup = [];

    /// <summary>
    /// Takes a snapshot of the freshly built Database tree and re-applies the
    /// current table filter (if any). Call this after (re)building treeView1.
    /// </summary>
    private void CaptureTreeBackupAndFilter()
    {
        _treeBackup =
        [
            .. treeView1.Nodes.Cast<TreeNode>().Select(n => (TreeNode)n.Clone()),
        ];

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
    private static TreeNode? FilterNode(TreeNode source, string filter)
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

    private static TreeNode CloneShallow(TreeNode source) => new(source.Text)
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

        return text.Contains(filter, StringComparison.OrdinalIgnoreCase);
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
                    TreeNode groupNode = new(group.Key)
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folder"
                    };

                    foreach (var conn in group)
                    {
                        bool isActive = _activeConnection != null && _activeConnection.Name == conn.Name;
                        string dbIconKey = TreeIconProvider.GetDatabaseIconKey(conn.DatabaseType, isActive);
                        TreeNode connectionNode = new(conn.Name)
                        {
                            Tag = conn,
                            ImageKey = dbIconKey,
                            SelectedImageKey = dbIconKey
                        };

                        // If this is the current connection, add its tables
                        if (conn.Name == connection.Name)
                        {
                            TreeNode tablesNode = new("Tables")
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

            // Reflect the active connection in the connections panel.
            connectionsPanel.ActiveConnection = connection;
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

    private async void BtnExecuteQuery_Click(object sender, EventArgs e)
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

    private void ApplyQueryResult(Models.QueryResult result, BufferedDataGridView grid, string query)
    {
        if (result.HasResultSet)
        {
            grid.PrepareForDataRefresh();
            grid.DataSource = result.Data;
            grid.ConfigureBinaryColumns(result.Data);
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
        _historyPanel.Add(query, connName, success);
    }

    private async void BtnLoadTables_Click(object sender, EventArgs e)
    {
        try
        {
            ToggleUiState(false);
            var tables = await _dbService.GetAllTablesAsync();

            treeView1.BeginUpdate();
            try
            {
                treeView1.Nodes.Clear();
                TreeNode rootNode = new("Tables")
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
        if (activeTab?.Tag is QueryTabPanel panel)
        {
            panel.SetExecutionEnabled(enabled);
        }

        this.Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;
    }

    private void InitializeImageList()
    {
        treeView1.ImageList = TreeIconProvider.CreateImageList();
    }

    private void NewQueryMenuItem_Click(object? sender, EventArgs e)
    {
        QueryTabPanel.Open(tabControl1, _dbService.GetNewQueryTemplate(), "Query",
            () => _dbService, SetStatus, busy => ToggleUiState(!busy), RecordHistory,
            autoExecute: false,
            maxAutocompleteSuggestions: _settingsManager.Settings.MaxAutocompleteSuggestions);
    }

    private void ExecuteQueryMenuItem_Click(object? sender, EventArgs e)
    {
        var activeTab = tabControl1.SelectedTab;
        if (activeTab?.Tag is QueryTabPanel panel)
        {
            panel.ExecuteQuery();
        }
        else
        {
            BtnExecuteQuery_Click(this, EventArgs.Empty);
        }
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private async void SettingsMenuItem_Click(object? sender, EventArgs e)
    {
        bool previousShowColumns = _settingsManager.Settings.ShowTableColumnsInTree;

        using var dialog = new SettingsForm(_settingsManager.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _settingsManager.Save();

        // Apply the (possibly updated) autocomplete limit to all open query tabs.
        foreach (TabPage tab in tabControl1.TabPages)
        {
            if (tab.Tag is QueryTabPanel openPanel)
            {
                openPanel.MaxAutocompleteSuggestions = _settingsManager.Settings.MaxAutocompleteSuggestions;
            }
        }

        // Rebuild the object tree if the column visibility setting changed
        if (previousShowColumns != _settingsManager.Settings.ShowTableColumnsInTree
            && _activeConnection != null)
        {
            await LoadTablesForConnection(_activeConnection);
        }
    }

    private void AboutMenuItem_Click(object? sender, EventArgs e)
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
            if (activeTab?.Tag is QueryTabPanel panel)
            {
                panel.ExecuteQuery();
            }
            else
            {
                // Fallback to the static first tab
                BtnExecuteQuery_Click(this, EventArgs.Empty);
            }
        }
    }
}
