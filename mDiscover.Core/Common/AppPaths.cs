namespace mDiscover.Core.Common;

/// <summary>
/// Provides canonical application and logs folder paths for both packaged and unpackaged execution.
/// </summary>
public static class AppPaths
{
    public const string AppName = "mDiscover";
    public const string PublisherName = "BurnArc";
    public const string LogFolderName = "Logs";
    public const string LogFileName = "mdiscover.log";

    /// <summary>
    /// Initializes the global application paths with the runtime AppData folder path.
    /// </summary>
    public static void Initialize(string appDataFolder)
    {
        AppDataFolder = appDataFolder;
        LogFolder = Path.Combine(appDataFolder, LogFolderName);
    }

    /// <summary>
    /// Gets the application data folder path.
    /// </summary>
    public static string AppDataFolder { get => field ?? GetDefaultAppDataFolder(); private set; }

    /// <summary>
    /// Gets the diagnostic logs folder path.
    /// </summary>
    public static string LogFolder { get => field ?? Path.Combine(AppDataFolder, LogFolderName); private set; }

    private static string GetDefaultAppDataFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            PublisherName,
            AppName);
    }
}
