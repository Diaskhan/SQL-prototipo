using SQL_prototipo.Models;
using System.Text.Json;

namespace SQL_prototipo.Services;

public class ConnectionManager
{
    private const string ConnectionsSettingKey = "SavedConnections";
    private List<ConnectionInfo> _connections;

    public IReadOnlyList<ConnectionInfo> Connections => _connections.AsReadOnly();

    public ConnectionManager()
    {
        _connections = LoadConnections();
    }

    /// <summary>
    /// Load connections from Application Settings (user.config)
    /// </summary>
    private List<ConnectionInfo> LoadConnections()
    {
        try
        {
            var configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                return new List<ConnectionInfo>();
            }

            var doc = System.Xml.Linq.XDocument.Load(configPath);
            var ns = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/2003/07/configuration");
            var userSettingsGroup = doc.Element(ns + "configuration")
                ?.Element(ns + "userSettings")
                ?.Element(ns + "SQL_prototipo.Properties.Settings");

            var settingElement = userSettingsGroup?.Elements(ns + "setting")
                .FirstOrDefault(e => e.Attribute("name")?.Value == ConnectionsSettingKey);

            if (settingElement?.Value == null)
            {
                return new List<ConnectionInfo>();
            }

            var json = settingElement.Value.Trim();
            if (string.IsNullOrEmpty(json))
            {
                return new List<ConnectionInfo>();
            }

            return JsonSerializer.Deserialize<List<ConnectionInfo>>(json) ?? new List<ConnectionInfo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading connections: {ex.Message}");
            return new List<ConnectionInfo>();
        }
    }

    /// <summary>
    /// Save connections to Application Settings (user.config)
    /// </summary>
    private void SaveConnections()
    {
        try
        {
            var json = JsonSerializer.Serialize(_connections);
            var configPath = GetConfigPath();

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

            System.Xml.Linq.XDocument doc;
            var ns = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/2003/07/configuration");

            if (File.Exists(configPath))
            {
                doc = System.Xml.Linq.XDocument.Load(configPath);
            }
            else
            {
                doc = new System.Xml.Linq.XDocument(
                    new System.Xml.Linq.XElement(ns + "configuration",
                        new System.Xml.Linq.XElement(ns + "userSettings")
                    )
                );
            }

            var userSettingsGroup = doc.Element(ns + "configuration")
                ?.Element(ns + "userSettings")
                ?.Element(ns + "SQL_prototipo.Properties.Settings");

            if (userSettingsGroup == null)
            {
                var userSettings = doc.Element(ns + "configuration")?.Element(ns + "userSettings");
                userSettingsGroup = new System.Xml.Linq.XElement(ns + "SQL_prototipo.Properties.Settings");
                userSettings?.Add(userSettingsGroup);
            }

            var settingElement = userSettingsGroup.Elements(ns + "setting")
                .FirstOrDefault(e => e.Attribute("name")?.Value == ConnectionsSettingKey);

            if (settingElement != null)
            {
                settingElement.Value = json;
            }
            else
            {
                var newSetting = new System.Xml.Linq.XElement(ns + "setting",
                    new System.Xml.Linq.XAttribute("name", ConnectionsSettingKey),
                    new System.Xml.Linq.XAttribute("serializeAs", "String"),
                    json
                );
                userSettingsGroup.Add(newSetting);
            }

            doc.Save(configPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving connections: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the path to user.config file
    /// </summary>
    private string GetConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var companyName = "SQL_prototipo";
        var appName = "SQL_prototipo";
        var version = "1.0.0.0";

        return Path.Combine(appData, companyName, appName, version, "user.config");
    }

    /// <summary>
    /// Add a new connection
    /// </summary>
    public void AddConnection(string name, string connectionString, string databaseType = "SQLite", string group = "Default")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Connection name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        if (_connections.Any(c => c.Name == name))
            throw new InvalidOperationException($"Connection with name '{name}' already exists.");

        _connections.Add(new ConnectionInfo(name, connectionString, databaseType, group));
        SaveConnections();
    }

    /// <summary>
    /// Update an existing connection
    /// </summary>
    public void UpdateConnection(string name, string connectionString, string databaseType = "SQLite", string group = "Default")
    {
        var connection = _connections.FirstOrDefault(c => c.Name == name);
        if (connection == null)
            throw new InvalidOperationException($"Connection '{name}' not found.");

        connection.ConnectionString = connectionString;
        connection.DatabaseType = databaseType;
        connection.Group = string.IsNullOrWhiteSpace(group) ? "Default" : group;
        SaveConnections();
    }

    /// <summary>
    /// Delete a connection
    /// </summary>
    public void DeleteConnection(string name)
    {
        var connection = _connections.FirstOrDefault(c => c.Name == name);
        if (connection != null)
        {
            _connections.Remove(connection);
            SaveConnections();
        }
    }

    /// <summary>
    /// Get a connection by name
    /// </summary>
    public ConnectionInfo? GetConnection(string name)
    {
        return _connections.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// Refresh connections from storage
    /// </summary>
    public void Refresh()
    {
        _connections = LoadConnections();
    }
}
