using SQL_prototipo.Models;
using System.Text.Json;

namespace SQL_prototipo.Services;

public class ConnectionManager
{
    private const string ConnectionsSettingKey = "SavedConnections";
    private const string FoldersSettingKey = "SavedFolders";
    private const string DefaultFolder = "Default";
    private List<ConnectionInfo> _connections;
    private List<string> _folders;

    public IReadOnlyList<ConnectionInfo> Connections => _connections.AsReadOnly();

    /// <summary>
    /// All folder names, including empty folders and folders referenced by connections.
    /// The "Default" folder is always present.
    /// </summary>
    public IReadOnlyList<string> Folders
    {
        get
        {
            var all = new List<string>(_folders);
            foreach (var conn in _connections)
            {
                var group = string.IsNullOrWhiteSpace(conn.Group) ? DefaultFolder : conn.Group;
                if (!all.Contains(group, StringComparer.OrdinalIgnoreCase))
                    all.Add(group);
            }
            if (!all.Contains(DefaultFolder, StringComparer.OrdinalIgnoreCase))
                all.Insert(0, DefaultFolder);
            return all.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }
    }

    public ConnectionManager()
    {
        _connections = LoadConnections();
        _folders = LoadFolders();
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
                return [];
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
                return [];
            }

            var json = settingElement.Value.Trim();
            if (string.IsNullOrEmpty(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<ConnectionInfo>>(json) ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading connections: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Load folder names from Application Settings (user.config)
    /// </summary>
    private List<string> LoadFolders()
    {
        try
        {
            var json = ReadSetting(FoldersSettingKey);
            if (string.IsNullOrEmpty(json))
            {
                return [DefaultFolder];
            }

            return JsonSerializer.Deserialize<List<string>>(json) ?? [DefaultFolder];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading folders: {ex.Message}");
            return [DefaultFolder];
        }
    }

    /// <summary>
    /// Read a raw string setting value from user.config
    /// </summary>
    private string? ReadSetting(string key)
    {
        var configPath = GetConfigPath();
        if (!File.Exists(configPath))
        {
            return null;
        }

        var doc = System.Xml.Linq.XDocument.Load(configPath);
        var ns = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/2003/07/configuration");
        var userSettingsGroup = doc.Element(ns + "configuration")
            ?.Element(ns + "userSettings")
            ?.Element(ns + "SQL_prototipo.Properties.Settings");

        var settingElement = userSettingsGroup?.Elements(ns + "setting")
            .FirstOrDefault(e => e.Attribute("name")?.Value == key);

        return settingElement?.Value?.Trim();
    }

    /// <summary>
    /// Save connections to Application Settings (user.config)
    /// </summary>
    private void SaveConnections()
    {
        WriteSetting(ConnectionsSettingKey, JsonSerializer.Serialize(_connections));
    }

    /// <summary>
    /// Save folder names to Application Settings (user.config)
    /// </summary>
    private void SaveFolders()
    {
        WriteSetting(FoldersSettingKey, JsonSerializer.Serialize(_folders));
    }

    /// <summary>
    /// Write a raw string setting value to user.config
    /// </summary>
    private void WriteSetting(string key, string json)
    {
        try
        {
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
                .FirstOrDefault(e => e.Attribute("name")?.Value == key);

            if (settingElement != null)
            {
                settingElement.Value = json;
            }
            else
            {
                var newSetting = new System.Xml.Linq.XElement(ns + "setting",
                    new System.Xml.Linq.XAttribute("name", key),
                    new System.Xml.Linq.XAttribute("serializeAs", "String"),
                    json
                );
                userSettingsGroup.Add(newSetting);
            }

            doc.Save(configPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving setting '{key}': {ex.Message}");
        }
    }

    /// <summary>
    /// Get the path to user.config file
    /// </summary>
    private static string GetConfigPath()
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
    public void UpdateConnection(string name, string connectionString, string databaseType = "SQLite", string group = "Default", string? newName = null)
    {
        var connection = _connections.FirstOrDefault(c => c.Name == name)
            ?? throw new InvalidOperationException($"Connection '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(newName) && newName != name)
        {
            if (_connections.Any(c => c != connection && c.Name == newName))
                throw new InvalidOperationException($"Connection with name '{newName}' already exists.");

            connection.Name = newName;
        }

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
    /// Add a new empty folder
    /// </summary>
    public void AddFolder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));

        name = name.Trim();

        if (_folders.Contains(name, StringComparer.OrdinalIgnoreCase) ||
            _connections.Any(c => string.Equals(
                string.IsNullOrWhiteSpace(c.Group) ? DefaultFolder : c.Group,
                name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Folder '{name}' already exists.");
        }

        _folders.Add(name);
        SaveFolders();
    }

    /// <summary>
    /// Delete an empty folder. Folders that still contain connections cannot be deleted.
    /// </summary>
    public void DeleteFolder(string name)
    {
        if (string.Equals(name, DefaultFolder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The 'Default' folder cannot be deleted.");

        if (_connections.Any(c => string.Equals(
                string.IsNullOrWhiteSpace(c.Group) ? DefaultFolder : c.Group,
                name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Folder '{name}' is not empty. Move or delete its connections first.");
        }

        _folders.RemoveAll(f => string.Equals(f, name, StringComparison.OrdinalIgnoreCase));
        SaveFolders();
    }

    /// <summary>
    /// Refresh connections from storage
    /// </summary>
    public void Refresh()
    {
        _connections = LoadConnections();
        _folders = LoadFolders();
    }
}
