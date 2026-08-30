namespace mDiscover.Core.Interfaces;

/// <summary>
/// Abstraction for UI thread dispatching, allowing ViewModels and services to marshal operations onto the main UI thread.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Enqueues the specified action to be executed on the UI dispatcher thread.
    /// </summary>
    /// <param name="action">The delegate action to execute.</param>
    void Enqueue(Action action);
}
