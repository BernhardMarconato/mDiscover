using System.Text.Json;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Helpers;
using mDiscover.Models;
using mDiscover.Serialization;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace mDiscover.Services;

/// <summary>
/// Manages window position, sizing, DPI scaling, and maximized state persistence across application sessions.
/// Uses CsWin32 P/Invoke generators for native Win32 window placement querying.
/// Designed with a placement abstraction that easily transitions to Microsoft.UI.Windowing.AppWindow
/// placement APIs when finalized in stable Windows App SDK releases.
/// </summary>
public sealed class WindowPlacementService(ISettingsService settingsService)
{
    private readonly ISettingsService _settingsService = settingsService;

    /// <summary>
    /// Enforces min/max window bounds, restores persisted placement, and registers lifecycle events to persist on close.
    /// </summary>
    public void TrackWindow(
        Window window,
        double defaultWidthDips = WindowExtensions.DefaultWidthDips,
        double defaultHeightDips = WindowExtensions.DefaultHeightDips,
        double minWidthDips = WindowExtensions.MinimumWidthDips,
        double minHeightDips = WindowExtensions.MinimumHeightDips)
    {
        // Enforce minimum window size via subclassing
        window.SetMinMaxSize(minWidthDips, minHeightDips);

        // Restore placement
        RestorePlacement(window, defaultWidthDips, defaultHeightDips, minWidthDips, minHeightDips);

        window.Closed += (s, e) => SavePlacement(window);
        window.AppWindow.Closing += (s, e) => SavePlacement(window);
    }

    /// <summary>
    /// Retrieves current native window placement via CsWin32, converts to DPI-independent coordinates, and saves to settings.
    /// </summary>
    public unsafe void SavePlacement(Window window)
    {
        var hwnd = window.WindowHandle;
        if (hwnd == HWND.Null)
        {
            return;
        }

        var wp = new WINDOWPLACEMENT { length = (uint)sizeof(WINDOWPLACEMENT) };
        if (PInvoke.GetWindowPlacement(hwnd, ref wp))
        {
            var isMaximized = wp.showCmd == SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED;
            var scale = window.DpiScale;

            // Convert physical screen pixels to DPI-independent units (DIPs)
            var x = (int)Math.Round(wp.rcNormalPosition.left / scale);
            var y = (int)Math.Round(wp.rcNormalPosition.top / scale);
            var width = (int)Math.Round((wp.rcNormalPosition.right - wp.rcNormalPosition.left) / scale);
            var height = (int)Math.Round((wp.rcNormalPosition.bottom - wp.rcNormalPosition.top) / scale);

            if (width >= WindowExtensions.MinimumWidthDips && height >= WindowExtensions.MinimumHeightDips)
            {
                var placement = new WindowPlacement
                {
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    IsMaximized = isMaximized
                };

                var json = JsonSerializer.Serialize(placement, UiJsonSerializerContext.Default.WindowPlacement);
                _settingsService.SaveSetting(SettingDefinitions.WindowPlacement, json);
            }
        }
    }

    /// <summary>
    /// Restores window bounds and state from settings, validating visibility against current display monitors via CsWin32.
    /// </summary>
    public void RestorePlacement(
        Window window,
        double defaultWidthDips = WindowExtensions.DefaultWidthDips,
        double defaultHeightDips = WindowExtensions.DefaultHeightDips,
        double minWidthDips = WindowExtensions.MinimumWidthDips,
        double minHeightDips = WindowExtensions.MinimumHeightDips)
    {
        var hwnd = window.WindowHandle;
        if (hwnd == HWND.Null)
        {
            return;
        }

        var json = _settingsService.ReadSetting(SettingDefinitions.WindowPlacement);
        WindowPlacement? placement = null;

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                placement = JsonSerializer.Deserialize(json, UiJsonSerializerContext.Default.WindowPlacement);
            }
            catch
            {
                placement = null;
            }
        }

        if (placement != null && placement.Width >= minWidthDips && placement.Height >= minHeightDips)
        {
            var scale = window.DpiScale;
            var pixelX = (int)Math.Round(placement.X * scale);
            var pixelY = (int)Math.Round(placement.Y * scale);
            var pixelWidth = (int)Math.Round(placement.Width * scale);
            var pixelHeight = (int)Math.Round(placement.Height * scale);

            // Verify that the restored normal position intersects a currently connected active monitor
            var rect = new RECT
            {
                left = pixelX,
                top = pixelY,
                right = pixelX + pixelWidth,
                bottom = pixelY + pixelHeight
            };

            var monitor = PInvoke.MonitorFromRect(rect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);
            if (monitor != nint.Zero)
            {
                window.AppWindow.MoveAndResize(new RectInt32(pixelX, pixelY, pixelWidth, pixelHeight));

                if (placement.IsMaximized && window.AppWindow.Presenter is OverlappedPresenter overlapped)
                {
                    overlapped.Maximize();
                }
                return;
            }
        }

        // Fallback: Center default dimensions on the primary display
        window.CenterOnDisplay(defaultWidthDips, defaultHeightDips);
    }
}
