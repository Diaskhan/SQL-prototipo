using Microsoft.Data.SqlClient;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }


    private void button1_Click_1(object sender, EventArgs e)
    {
        var dbService = new DatabaseService("chinook.sqlite");
        dbService.OpenLocalDBConnection();
        var tables = dbService.GetAllTables();
        MessageBox.Show("Tables: " + string.Join(", ", tables));
    }

}
