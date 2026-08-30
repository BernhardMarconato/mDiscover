namespace mDiscover.Core.Interfaces;

/// <summary>
/// Provides application storage, configuration, and diagnostic folder paths.
/// </summary>
public interface IAppPathService
{
    /// <summary>
    /// Gets the absolute path to the local application data directory (e.g. `%LocalAppData%\mDiscover`).
    /// </summary>
    string AppDataFolderPath { get; }

    /// <summary>
    /// Gets the absolute path to the application log directory (e.g. `%LocalAppData%\mDiscover\Logs`).
    /// </summary>
    string LogFolderPath { get; }
}
