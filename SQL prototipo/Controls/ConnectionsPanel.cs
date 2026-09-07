using SQL_prototipo.Models;
using SQL_prototipo.Services;

namespace SQL_prototipo.Controls;

/// <summary>
/// Self-contained UI for managing saved connections and folders (CRUD + trees).
/// Owns its own controls and CRUD logic against a shared <see cref="ConnectionManager"/>,
/// and communicates back to the host form through events.
/// </summary>
public partial class ConnectionsPanel : UserControl
{
    private ConnectionManager _connectionManager = null!;
    private ConnectionInfo? _activeConnection;
    private bool _isLoadingUI;

    /// <summary>Raised when the user chooses a connection to switch to (double-click or combo select).</summary>
    public event EventHandler<ConnectionInfo>? ConnectionActivated;

    /// <summary>Raised after the underlying connection/folder list changes (add/update/delete).</summary>
    public event EventHandler? ConnectionsChanged;

    /// <summary>Raised to report a short status message to the host.</summary>
    public event EventHandler<string>? StatusChanged;

    public ConnectionsPanel()
    {
        InitializeComponent();
    }

    /// <summary>Wires the panel to the shared connection manager and performs the first load.</summary>
    public void Initialize(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        treeViewConnections.ImageList = TreeIconProvider.CreateImageList();
        Reload();
    }

    /// <summary>The currently active connection, used to highlight the matching node/combo item.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public ConnectionInfo? ActiveConnection
    {
        get => _activeConnection;
        set
        {
            _activeConnection = value;
            Reload();
        }
    }

    /// <summary>Rebuilds the connection tree and the "switch connection" combo from the manager.</summary>
    public void Reload()
    {
        if (_connectionManager == null) return;
        PopulateConnectionsTree();
        PopulateConnectionsCombo();
    }

    private void RefreshFromStorage()
    {
        _connectionManager.Refresh();
        Reload();
    }

    /// <summary>
    /// Populates the tree with a strict two-level hierarchy: folders at the root
    /// and their connections as children.
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
                TreeNode folderNode = new(folderName)
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
                        folderNode.Nodes.Add(CreateConnectionNode(connection));
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

    private TreeNode CreateConnectionNode(ConnectionInfo connection)
    {
        bool isActive = _activeConnection?.Name == connection.Name;
        string iconKey = TreeIconProvider.GetDatabaseIconKey(connection.DatabaseType, isActive);
        return new TreeNode(connection.Name)
        {
            Tag = connection,
            ImageKey = iconKey,
            SelectedImageKey = iconKey
        };
    }

    private void PopulateConnectionsCombo()
    {
        _isLoadingUI = true;
        try
        {
            cmbConnections.Items.Clear();
            foreach (var connection in _connectionManager.Connections)
            {
                cmbConnections.Items.Add(connection);
            }

            if (_activeConnection != null)
            {
                var match = _connectionManager.Connections
                    .FirstOrDefault(c => c.Name == _activeConnection.Name);
                if (match != null)
                {
                    cmbConnections.SelectedItem = match;
                }
            }
            else if (cmbConnections.Items.Count > 0)
            {
                cmbConnections.SelectedIndex = 0;
            }
        }
        finally
        {
            _isLoadingUI = false;
        }
    }

    private void SetStatus(string message) => StatusChanged?.Invoke(this, message);

    // Connection Management Event Handlers
    private void BtnAddConnection_Click(object? sender, EventArgs e)
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
            RefreshFromStorage();
            ConnectionsChanged?.Invoke(this, EventArgs.Empty);
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

    private void BtnAddFolder_Click(object? sender, EventArgs e)
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

        dialog.Controls.AddRange([label, textBox, btnOk, btnCancel]);
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
            Reload();
            ConnectionsChanged?.Invoke(this, EventArgs.Empty);
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

    private void BtnDeleteConnection_Click(object? sender, EventArgs e)
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
                RefreshFromStorage();
                ConnectionsChanged?.Invoke(this, EventArgs.Empty);

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

    private async void BtnTestConnection_Click(object? sender, EventArgs e)
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
            Cursor = Cursors.WaitCursor;

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
            Cursor = Cursors.Default;
        }
    }

    private void BtnUpdateConnection_Click(object? sender, EventArgs e)
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

            RefreshFromStorage();
            ConnectionsChanged?.Invoke(this, EventArgs.Empty);
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

    private void TreeViewConnections_AfterSelect(object? sender, TreeViewEventArgs e)
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

    private void TreeViewConnections_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        // Double-clicking a connection switches to it and loads its tables.
        if (e.Node?.Tag is ConnectionInfo connection)
        {
            ConnectionActivated?.Invoke(this, connection);
        }
    }

    private void CmbConnections_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isLoadingUI) return;
        if (cmbConnections.SelectedItem is ConnectionInfo connection)
        {
            ConnectionActivated?.Invoke(this, connection);
        }
    }
}
