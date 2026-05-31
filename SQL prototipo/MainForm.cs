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

        LoadConnectionsToUI();
    }

    private void LoadConnectionsToUI()
    {
        // Load connections into listbox
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

    private void TreeView1_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node != null && e.Node.Parent != null)
        {
            richTextBox1.Text = $"SELECT * FROM {e.Node.Text};";
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
            TreeNode rootNode = new TreeNode("Tables");
            foreach (var table in tables)
            {
                rootNode.Nodes.Add(table);
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

    private void cmbConnections_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbConnections.SelectedItem is ConnectionInfo connection)
        {
            try
            {
                _currentConnectionString = connection.ConnectionString;
                _dbService = new DatabaseService(_currentConnectionString);
                treeView1.Nodes.Clear();
                MessageBox.Show($"Switched to connection: {connection.Name}", "Connection Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
