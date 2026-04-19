using Microsoft.Data.SqlClient;

namespace SQL_prototipo;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }


    private void OpenLocalDBConnection()
    {
        string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=YourDatabaseName;Trusted_Connection=True;";
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                MessageBox.Show("Соединение открыто!");
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка при открытии соединения: {ex.Message}");
            }
        }
    }
}
