using System.Drawing;
using System.Drawing.Drawing2D;
using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    private DatabaseService _dbService;
    private ConnectionManager _connectionManager;
    private string _currentConnectionString = "Data Source=chinook.sqlite";

    public MainForm()
    {
        InitializeComponent();
        _connectionManager = new ConnectionManager();
        _dbService = new DatabaseService(_currentConnectionString);
        treeView1.NodeMouseDoubleClick += TreeView1_NodeMouseDoubleClick;

        InitializeImageList();
        LoadConnectionsToUI();
    }

    private void LoadConnectionsToUI()
    {
        // Load connections into treeView1
        treeView1.Nodes.Clear();
        TreeNode connectionsRoot = new TreeNode("Connections")
        {
            ImageKey = "server",
            SelectedImageKey = "server"
        };

        foreach (var connection in _connectionManager.Connections)
        {
            string dbIconKey = GetDatabaseIconKey(connection.DatabaseType);
            TreeNode connectionNode = new TreeNode(connection.Name)
            {
                Tag = connection,
                ImageKey = dbIconKey,
                SelectedImageKey = dbIconKey
            };
            connectionsRoot.Nodes.Add(connectionNode);
        }

        treeView1.Nodes.Add(connectionsRoot);
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
        if (cmbConnections.Items.Count > 0)
        {
            cmbConnections.SelectedIndex = 0;
            if (listBoxConnections.Items.Count > 0)
                listBoxConnections.SelectedIndex = 0;
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

        // If clicked on a table (child of Tables node), generate SELECT query
        if (e.Node.Parent != null && e.Node.Parent.Text == "Tables")
        {
            richTextBox1.Text = $"SELECT * FROM {e.Node.Text};";
            return;
        }

        // If clicked on a connection node, load its tables
        if (e.Node.Tag is ConnectionInfo connection)
        {
            await LoadTablesForConnection(connection);
            return;
        }

        // If clicked on Connections root, do nothing
        if (e.Node.Text == "Connections")
        {
            return;
        }
    }

    private async Task LoadTablesForConnection(ConnectionInfo connection)
    {
        try
        {
            ToggleUiState(false);

            // Switch to the selected connection
            _currentConnectionString = connection.ConnectionString;
            _dbService = new DatabaseService(_currentConnectionString);

            // Load tables from this connection
            var tables = await _dbService.GetAllTablesAsync();

            // Update treeView1 to show connection with its tables
            treeView1.Nodes.Clear();
            TreeNode connectionsRoot = new TreeNode("Connections")
            {
                ImageKey = "server",
                SelectedImageKey = "server"
            };

            foreach (var conn in _connectionManager.Connections)
            {
                string dbIconKey = GetDatabaseIconKey(conn.DatabaseType);
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
                        tablesNode.Nodes.Add(new TreeNode(table)
                        {
                            ImageKey = "table",
                            SelectedImageKey = "table"
                        });
                    }
                    connectionNode.Nodes.Add(tablesNode);
                }

                connectionsRoot.Nodes.Add(connectionNode);
            }

            treeView1.Nodes.Add(connectionsRoot);
            treeView1.ExpandAll();

            // Also update the Connections tab combobox
            if (cmbConnections.Items.Count > 0)
            {
                cmbConnections.SelectedItem = connection;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            var results = await _dbService.ExecuteQueryAsync(query);
            dataGridView1.DataSource = results;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка выполнения запроса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUiState(true);
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
                rootNode.Nodes.Add(new TreeNode(table)
                {
                    ImageKey = "table",
                    SelectedImageKey = "table"
                });
            }
            treeView1.Nodes.Add(rootNode);
            treeView1.ExpandAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            _connectionManager.AddConnection(name, connectionString, databaseType);
            MessageBox.Show("Connection added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clear inputs and refresh list
            txtConnectionName.Clear();
            txtConnectionString.Clear();
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
                RefreshConnectionsList();
                LoadConnectionsToUI();

                // Clear inputs
                txtConnectionName.Clear();
                txtConnectionString.Clear();
                cmbConnectionType.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void listBoxConnections_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (listBoxConnections.SelectedItem is ConnectionInfo connection)
        {
            txtConnectionName.Text = connection.Name;
            txtConnectionString.Text = connection.ConnectionString;
            cmbConnectionType.SelectedItem = connection.DatabaseType;
        }
    }

    private async void cmbConnections_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbConnections.SelectedItem is ConnectionInfo connection)
        {
            try
            {
                _currentConnectionString = connection.ConnectionString;
                _dbService = new DatabaseService(_currentConnectionString);

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
        imageList.Images.Add("database", CreateDatabaseIcon(Color.FromArgb(43, 87, 151)));
        imageList.Images.Add("database_sqlite", CreateDatabaseIcon(Color.FromArgb(0, 100, 150)));
        imageList.Images.Add("database_sqlserver", CreateDatabaseIcon(Color.FromArgb(186, 12, 47)));
        imageList.Images.Add("database_mysql", CreateDatabaseIcon(Color.FromArgb(242, 145, 17)));
        imageList.Images.Add("database_postgresql", CreateDatabaseIcon(Color.FromArgb(51, 102, 153)));
        imageList.Images.Add("folder", CreateFolderIcon());
        imageList.Images.Add("table", CreateTableIcon());

        treeView1.ImageList = imageList;
    }

    private string GetDatabaseIconKey(string databaseType)
    {
        return databaseType.ToLower() switch
        {
            "sqlite" => "database_sqlite",
            "sqlserver" => "database_sqlserver",
            "mysql" => "database_mysql",
            "postgresql" => "database_postgresql",
            _ => "database"
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

    private Image CreateDatabaseIcon(Color color)
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
}
