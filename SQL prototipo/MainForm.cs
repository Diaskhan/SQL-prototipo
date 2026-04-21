using Microsoft.Data.SqlClient;
using SQL_prototipo.Services;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    private const string ConnectionString = "Data Source=chinook.sqlite";
    private readonly DatabaseService _dbService;

    public MainForm()
    {
        InitializeComponent();
        treeView1.NodeMouseDoubleClick += treeView1_NodeMouseDoubleClick;
        _dbService = new DatabaseService(ConnectionString);
    }

    private void treeView1_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node != null)
        {
            richTextBox1.Text = $"Select * from {e.Node.Text}";
        }
    }

    private void button2_Click(object sender, EventArgs e)
    {
            _dbService.OpenLocalDBConnection();
            var results = _dbService.ExecuteQueryAsDataTable(richTextBox1.Text);
            dataGridView1.DataSource = results;
        
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
        try
        {
            _dbService.OpenLocalDBConnection();
            var tables = _dbService.GetAllTables();

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
    }
}
