using Microsoft.Data.SqlClient;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
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
