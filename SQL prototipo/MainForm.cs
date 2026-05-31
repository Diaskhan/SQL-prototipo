using SQL_prototipo.Services;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    private const string ConnectionString = "Data Source=chinook.sqlite";
    private readonly DatabaseService _dbService;

    public MainForm()
    {
        InitializeComponent();
        treeView1.NodeMouseDoubleClick += TreeView1_NodeMouseDoubleClick;
        _dbService = new DatabaseService(ConnectionString);
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
}
