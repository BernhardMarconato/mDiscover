namespace mDiscover.Core.Interfaces;

/// <summary>
/// Abstraction for system clipboard interactions, facilitating testability and UI decoupling.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Places the specified plain text onto the Windows clipboard.
    /// </summary>
    /// <param name="text">The text string to copy.</param>
    void SetText(string text);
}
