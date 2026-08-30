namespace mDiscover.Core.Interfaces;

/// <summary>
/// Provides application lifecycle management, including programmatic process restarts and shutdown routines.
/// </summary>
public interface IAppLifecycleService
{
    /// <summary>
    /// Programmatically terminates the current application instance and starts a new instance of the application.
    /// </summary>
    void Restart();
}
