using System.Reflection;

namespace SQL_prototipo;

/// <summary>
/// Exposes build metadata (version, commit hash, build date) that is embedded
/// into the assembly's informational version at publish time.
/// </summary>
public static class BuildInfo
{
    /// <summary>
    /// The full informational version, e.g. "1.0.0+2024.06.01.abc1234".
    /// Falls back to the assembly version when no metadata is present.
    /// </summary>
    public static string InformationalVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>The base version part (before the '+' metadata separator).</summary>
    public static string Version
    {
        get
        {
            var full = InformationalVersion;
            var idx = full.IndexOf('+');
            return idx >= 0 ? full[..idx] : full;
        }
    }

    /// <summary>The metadata part after '+', typically "buildDate.commitHash".</summary>
    public static string Metadata
    {
        get
        {
            var full = InformationalVersion;
            var idx = full.IndexOf('+');
            return idx >= 0 ? full[(idx + 1)..] : string.Empty;
        }
    }
}
