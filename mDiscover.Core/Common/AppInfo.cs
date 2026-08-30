using System.Diagnostics;
using System.Reflection;

namespace mDiscover.Core.Common;

/// <summary>
/// Provides application version metadata, licensing paths, and service type normalization helpers.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// Gets the formatted display string for the current application version including the full commit hash if available (e.g. "1.0.0+b82099f...").
    /// </summary>
    public static string DisplayVersionString => GetDisplayVersionString();

    /// <summary>
    /// Gets the formatted display string for the current application version (e.g. "1.0.0").
    /// </summary>
    public static string VersionString => GetAppVersionString();

    /// <summary>
    /// Gets the full git commit hash if available in the application metadata.
    /// </summary>
    public static string? CommitHash => GetCommitHash();

    /// <summary>
    /// Gets the 7-character short git commit hash if available.
    /// </summary>
    public static string? ShortCommitHash
    {
        get
        {
            var hash = CommitHash;
            if (string.IsNullOrWhiteSpace(hash))
                return null;
            return hash.Length > 7 ? hash[..7] : hash;
        }
    }

    /// <summary>
    /// Gets the semantic application version.
    /// </summary>
    public static Version Version => GetAppVersion();

    /// <summary>
    /// Gets the full informational version string, including the complete git commit hash if available (e.g. "1.0.0+b82099fa1b2c3d4e5f...").
    /// </summary>
    public static string GetDisplayVersionString()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                var productVer = versionInfo.ProductVersion;
                if (!string.IsNullOrWhiteSpace(productVer))
                {
                    return productVer.Trim();
                }
            }

            var entryAssembly = Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly;
            var infoVerAttr = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (!string.IsNullOrWhiteSpace(infoVerAttr?.InformationalVersion))
            {
                return infoVerAttr.InformationalVersion.Trim();
            }
        }
        catch
        {
        }

        return VersionString;
    }

    /// <summary>
    /// Extracts the full commit hash from the informational version string if present.
    /// </summary>
    public static string? GetCommitHash()
    {
        var displayVer = GetDisplayVersionString();
        var plusIndex = displayVer.IndexOf('+');
        if (plusIndex >= 0 && plusIndex < displayVer.Length - 1)
        {
            return displayVer[(plusIndex + 1)..].Trim();
        }

        return null;
    }

    /// <summary>
    /// Gets the formatted display string for the current application version.
    /// </summary>
    public static string GetAppVersionString()
    {
        var version = GetAppVersion();
        return version.Build >= 0 ? version.ToString(3) : version.ToString(2);
    }

    /// <summary>
    /// Resolves the application version from the running process executable or assembly metadata.
    /// </summary>
    public static Version GetAppVersion()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(processPath);

                if (Version.TryParse(versionInfo.FileVersion, out var parsedVer))
                {
                    return parsedVer;
                }

                return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, Math.Max(0, versionInfo.FileBuildPart));
            }
        }
        catch
        {
        }

        return new Version(1, 0, 0);
    }

    /// <summary>
    /// Normalizes a user-entered service type name by ensuring leading underscore and transport protocol suffix.
    /// </summary>
    public static string NormalizeServiceType(string raw)
    {
        var serviceType = raw.Trim();
        if (string.IsNullOrWhiteSpace(serviceType))
            return string.Empty;

        if (!serviceType.StartsWith('_'))
            serviceType = $"_{serviceType}";
        if (!serviceType.Contains("._tcp", StringComparison.OrdinalIgnoreCase) &&
            !serviceType.Contains("._udp", StringComparison.OrdinalIgnoreCase))
        {
            serviceType = $"{serviceType}._tcp";
        }

        return serviceType;
    }

    /// <summary>
    /// Locates the bundled third-party licenses directory.
    /// </summary>
    public static string? GetLicensesFolderPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var licensesPath = Path.Combine(baseDir, "Licenses");

        if (Directory.Exists(licensesPath))
            return licensesPath;

        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Licenses");
        if (Directory.Exists(fallback))
            return fallback;

        return null;
    }
}

