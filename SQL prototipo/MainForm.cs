using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    private DatabaseService _dbService;
    private ConnectionManager _connectionManager;
    private QueryHistoryManager _historyManager;
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

        // --- Query history list (Connections tab) ---
        var historyLabel = new Label
        {
            Text = "Query History (double-click to load)",
            AutoSize = true,
            Location = new Point(10, 345)
        };
        _listBoxHistory = new ListBox
        {
            Location = new Point(10, 365),
            Size = new Size(341, 150)
        };
        _listBoxHistory.DoubleClick += listBoxHistory_DoubleClick;
        panel4.Controls.Add(historyLabel);
        panel4.Controls.Add(_listBoxHistory);
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
                string dbIconKey = GetDatabaseIconKey(connection.DatabaseType, isActive);
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

        // Also load into listbox and combobox for the Connections tab
        listBoxConnections.Items.Clear();
        cmbConnections.Items.Clear();

        foreach (var connection in _connectionManager.Connections)
        {
            listBoxConnections.Items.Add(connection);
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
                if (listBoxConnections.Items.Count > 0)
                    listBoxConnections.SelectedIndex = 0;
            }
        }
        finally
        {
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
                    string dbIconKey = GetDatabaseIconKey(conn.DatabaseType, isActive);
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
                            tableNode.Nodes.Add(new TreeNode("Loading...") { Name = "__placeholder__" });
                            tablesNode.Nodes.Add(tableNode);
                        }
                        connectionNode.Nodes.Add(tablesNode);
                    }

                    groupNode.Nodes.Add(connectionNode);
                }

                treeView1.Nodes.Add(groupNode);
            }

            treeView1.ExpandAll();

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
                tableNode.Nodes.Add(new TreeNode("Loading...") { Name = "__placeholder__" });
                rootNode.Nodes.Add(tableNode);
            }
            treeView1.Nodes.Add(rootNode);
            treeView1.ExpandAll();
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

    private void btnDeleteConnection_Click(object sender, EventArgs e)
    {
        try
        {
            if (listBoxConnections.SelectedItem is not ConnectionInfo selectedConnection)
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

    private void listBoxConnections_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isLoadingUI) return;
        if (listBoxConnections.SelectedItem is ConnectionInfo connection)
        {
            txtConnectionName.Text = connection.Name;
            txtConnectionString.Text = connection.ConnectionString;
            cmbConnectionType.SelectedItem = connection.DatabaseType;
            txtGroup.Text = connection.Group;
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
        ImageList imageList = new ImageList();
        imageList.ImageSize = new Size(16, 16);
        imageList.ColorDepth = ColorDepth.Depth32Bit;

        imageList.Images.Add("server", CreateServerIcon());
        
        // Active Icons
        imageList.Images.Add("database_active", CreateDatabaseIcon(Color.FromArgb(43, 87, 151), true));
        imageList.Images.Add("database_sqlite_active", CreateDatabaseIcon(Color.FromArgb(0, 100, 150), true));
        imageList.Images.Add("database_sqlserver_active", CreateDatabaseIcon(Color.FromArgb(186, 12, 47), true));
        imageList.Images.Add("database_mysql_active", CreateDatabaseIcon(Color.FromArgb(242, 145, 17), true));
        imageList.Images.Add("database_postgresql_active", CreateDatabaseIcon(Color.FromArgb(51, 102, 153), true));

        // Inactive Icons
        imageList.Images.Add("database_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_sqlite_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_sqlserver_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_mysql_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_postgresql_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));

        imageList.Images.Add("folder", CreateFolderIcon());
        imageList.Images.Add("table", CreateTableIcon());

        treeView1.ImageList = imageList;
    }

    private string GetDatabaseIconKey(string databaseType, bool isActive)
    {
        string suffix = isActive ? "_active" : "_inactive";
        return databaseType.ToLower() switch
        {
            "sqlite" => "database_sqlite" + suffix,
            "sqlserver" => "database_sqlserver" + suffix,
            "mysql" => "database_mysql" + suffix,
            "postgresql" => "database_postgresql" + suffix,
            _ => "database" + suffix
        };
    }

    private Image CreateServerIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color frameColor = Color.FromArgb(70, 80, 95);
            Color faceColor = Color.FromArgb(230, 235, 240);
            Color ledColor = Color.FromArgb(0, 200, 100);

            // First blade
            using (Brush brush = new SolidBrush(faceColor))
            using (Pen pen = new Pen(frameColor, 1f))
            {
                g.FillRectangle(brush, 1, 3, 14, 4);
                g.DrawRectangle(pen, 1, 3, 14, 4);

                g.FillRectangle(brush, 1, 9, 14, 4);
                g.DrawRectangle(pen, 1, 9, 14, 4);
            }

            // LED indicators
            using (Brush ledBrush = new SolidBrush(ledColor))
            {
                g.FillEllipse(ledBrush, 3, 4, 2, 2);
                g.FillEllipse(ledBrush, 3, 10, 2, 2);
            }

            // Vents
            using (Pen linePen = new Pen(Color.FromArgb(120, 130, 140), 1))
            {
                g.DrawLine(linePen, 7, 5, 12, 5);
                g.DrawLine(linePen, 7, 11, 12, 11);
            }
        }
        return bmp;
    }

    private Image CreateDatabaseIcon(Color color, bool isActive)
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int x = 2, w = 12;
            int h = 4; // Height of the ellipse

            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 30),
                Math.Max(0, color.G - 30),
                Math.Max(0, color.B - 30)
            );
            Color lightColor = Color.FromArgb(
                Math.Min(255, color.R + 40),
                Math.Min(255, color.G + 40),
                Math.Min(255, color.B + 40)
            );

            // Draw stacked segments
            using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                new Rectangle(x, 1, w, 14), darkColor, lightColor, LinearGradientMode.Horizontal))
            {
                // Bottom cylinder section
                g.FillRectangle(bodyBrush, x, 9, w, 4);
                g.FillEllipse(bodyBrush, x, 11, w, h);

                // Middle cylinder section
                g.FillRectangle(bodyBrush, x, 5, w, 4);
                g.FillEllipse(bodyBrush, x, 7, w, h);

                // Top cylinder section body
                g.FillRectangle(bodyBrush, x, 1, w, 4);
                g.FillEllipse(bodyBrush, x, 3, w, h);
            }

            // Top lid
            using (LinearGradientBrush lidBrush = new LinearGradientBrush(
                new Rectangle(x, 1, w, h), lightColor, color, LinearGradientMode.Vertical))
            {
                g.FillEllipse(lidBrush, x, 1, w, h);
            }

            // Outlines
            Color outlineColor = Color.FromArgb(120, 255, 255, 255);
            using (Pen outlinePen = new Pen(outlineColor, 1f))
            {
                g.DrawEllipse(outlinePen, x, 1, w, h);
                g.DrawEllipse(outlinePen, x, 5, w, h);
                g.DrawEllipse(outlinePen, x, 9, w, h);
            }

            // Side borders
            using (Pen borderPen = new Pen(darkColor, 1f))
            {
                g.DrawLine(borderPen, x, 3, x, 13);
                g.DrawLine(borderPen, x + w, 3, x + w, 13);
            }

            // Status indicator badge in bottom-right corner
            if (isActive)
            {
                // Draw white background circle for contrast
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, 10, 10, 6, 6);
                }
                // Fill with green color
                using (Brush greenBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.FillEllipse(greenBrush, 11, 11, 4, 4);
                }
            }
            else
            {
                // Draw white background circle for contrast
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, 10, 10, 6, 6);
                }
                // Fill with muted gray color
                using (Brush grayBrush = new SolidBrush(Color.FromArgb(180, 185, 190)))
                {
                    g.FillEllipse(grayBrush, 11, 11, 4, 4);
                }
            }
        }
        return bmp;
    }

    private Image CreateFolderIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color baseColor = Color.FromArgb(240, 173, 78);
            Color lightColor = Color.FromArgb(252, 218, 141);
            Color shadowColor = Color.FromArgb(200, 130, 30);

            using (LinearGradientBrush brush = new LinearGradientBrush(
                new Rectangle(2, 2, 12, 12), lightColor, baseColor, LinearGradientMode.ForwardDiagonal))
            {
                GraphicsPath path = new GraphicsPath();
                path.AddLine(2, 4, 2, 13);
                path.AddLine(2, 13, 14, 13);
                path.AddLine(14, 13, 14, 4);
                path.AddLine(14, 4, 8, 4);
                path.AddLine(7, 2, 2, 2);
                path.CloseFigure();

                g.FillPath(brush, path);
                using (Pen borderPen = new Pen(shadowColor, 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            using (LinearGradientBrush flapBrush = new LinearGradientBrush(
                new Rectangle(2, 5, 12, 8), Color.FromArgb(255, 230, 170), baseColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle(flapBrush, 2, 5, 12, 8);
                using (Pen borderPen = new Pen(shadowColor, 1f))
                {
                    g.DrawRectangle(borderPen, 2, 5, 12, 8);
                }
            }
        }
        return bmp;
    }

    private Image CreateTableIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color headerColor = Color.FromArgb(41, 128, 185);
            Color gridColor = Color.FromArgb(200, 210, 220);
            Color rowColor2 = Color.FromArgb(240, 244, 248);

            // Table body
            g.FillRectangle(Brushes.White, 2, 2, 12, 12);

            // Header
            using (Brush hb = new SolidBrush(headerColor))
            {
                g.FillRectangle(hb, 2, 2, 12, 4);
            }

            // Alternate row
            using (Brush r2 = new SolidBrush(rowColor2))
            {
                g.FillRectangle(r2, 2, 9, 12, 2);
            }

            // Grid border
            using (Pen borderPen = new Pen(Color.FromArgb(100, 110, 120), 1f))
            {
                g.DrawRectangle(borderPen, 2, 2, 12, 12);
            }

            // Grid lines (horizontal)
            using (Pen gridPen = new Pen(gridColor, 1f))
            {
                g.DrawLine(gridPen, 2, 6, 14, 6);
                g.DrawLine(gridPen, 2, 9, 14, 9);
                g.DrawLine(gridPen, 2, 11, 14, 11);

                // Vertical column dividers
                g.DrawLine(gridPen, 6, 6, 6, 14);
                g.DrawLine(gridPen, 10, 6, 10, 14);
            }
        }
        return bmp;
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

    private void OpenNewQueryTab(string query, string tableName)
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
        ExecuteQueryInTab(ctx);
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
