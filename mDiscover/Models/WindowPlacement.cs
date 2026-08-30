namespace mDiscover.Models;

/// <summary>
/// Represents the persistent window state and bounding dimensions in device-independent pixels (DIPs).
/// </summary>
public sealed class WindowPlacement
{
    /// <summary>
    /// Gets or sets the X position on the screen.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y position on the screen.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the window width.
    /// </summary>
    public int Width { get; set; } = 1100;

    /// <summary>
    /// Gets or sets the window height.
    /// </summary>
    public int Height { get; set; } = 750;

    /// <summary>
    /// Gets or sets a value indicating whether the window is maximized.
    /// </summary>
    public bool IsMaximized { get; set; }
}

