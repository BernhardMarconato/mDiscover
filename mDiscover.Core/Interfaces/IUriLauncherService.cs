namespace mDiscover.Core.Interfaces;

/// <summary>
/// Provides capabilities to launch URIs and local folder paths using the host OS shell.
/// </summary>
public interface IUriLauncherService
{
    /// <summary>
    /// Launches the default system application registered for the specified URI scheme (e.g. default browser for http/https).
    /// </summary>
    /// <param name="uri">The absolute URI to launch.</param>
    /// <returns><see langword="true"/> if successfully launched; otherwise <see langword="false"/>.</returns>
    Task<bool> LaunchUriAsync(Uri uri);

    /// <summary>
    /// Opens the specified local filesystem directory in the default file manager (e.g. Windows File Explorer).
    /// </summary>
    /// <param name="folderPath">The absolute path to the directory.</param>
    /// <returns><see langword="true"/> if successfully opened; otherwise <see langword="false"/>.</returns>
    Task<bool> LaunchFolderPathAsync(string folderPath);
}
