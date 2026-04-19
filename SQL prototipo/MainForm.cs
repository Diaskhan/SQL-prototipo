using Microsoft.Data.SqlClient;
using SQL_prototipo.Services;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        treeView1.NodeMouseDoubleClick += treeView1_NodeMouseDoubleClick;
    }

    private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        richTextBox1.Text = $"Select * from {e.Node.Text}";
    }

    private void button2_Click(object sender, EventArgs e)
    {
        var dbService = new DatabaseService("Data Source=chinook.sqlite");
        var results = dbService.ExecuteQuery(richTextBox1.Text);
        richTextBox2.Text = string.Join(Environment.NewLine, results);
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
        var dbService = new DatabaseService("Data Source=chinook.sqlite");
        dbService.OpenLocalDBConnection();
        var tables = dbService.GetAllTables();

        treeView1.Nodes.Clear();
        TreeNode rootNode = new TreeNode("Tables");
        foreach (var table in tables)
        {
            rootNode.Nodes.Add(table);
        }
        treeView1.Nodes.Add(rootNode);
        treeView1.ExpandAll();

    }
}
