using System.Runtime.CompilerServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT.Interop;

namespace mDiscover.Helpers;

/// <summary>
/// Provides extension methods and properties for WinUI 3 Window and AppWindow, including DPI conversions,
/// display centering, and dynamic DPI-aware minimum/maximum window sizing via OverlappedPresenter.
/// </summary>
public static class WindowExtensions
{
    public const double DefaultWidthDips = 1100.0;
    public const double DefaultHeightDips = 750.0;
    public const double MinimumWidthDips = 640.0;
    public const double MinimumHeightDips = 480.0;

    private static readonly ConditionalWeakTable<AppWindow, WindowLimitsTracker> _trackers = new();

    extension(Window window)
    {
        /// <summary>
        /// Gets the native HWND handle for the WinUI Window.
        /// </summary>
        internal HWND WindowHandle
        {
            get
            {
                var raw = WindowNative.GetWindowHandle(window);
                return (HWND)raw;
            }
        }

        /// <summary>
        /// Gets the DPI for the window, defaulting to 96 if unable to query.
        /// </summary>
        public uint Dpi
        {
            get
            {
                var hwnd = window.WindowHandle;
                if (hwnd == HWND.Null)
                {
                    return 96;
                }

                var dpi = PInvoke.GetDpiForWindow(hwnd);
                return dpi == 0 ? 96 : dpi;
            }
        }

        /// <summary>
        /// Gets the DPI scaling factor for the window (1.0 = 100% scaling, 1.25 = 125%, etc.).
        /// </summary>
        public double DpiScale => window.Dpi / 96.0;

        /// <summary>
        /// Converts device-independent pixels (DIPs) to physical screen pixels for this window's DPI scale.
        /// </summary>
        public int DipsToPixels(double dips) =>
            (int)Math.Round(dips * window.DpiScale);

        /// <summary>
        /// Converts physical screen pixels to device-independent pixels (DIPs) for this window's DPI scale.
        /// </summary>
        public double PixelsToDips(int pixels) =>
            pixels / window.DpiScale;

        /// <summary>
        /// Enforces preferred minimum and optional maximum window dimensions, automatically recalculating
        /// physical pixel thresholds whenever the window moves across monitors with different DPI scalings.
        /// </summary>
        public void SetMinMaxSize(
            double minWidthDips = MinimumWidthDips,
            double minHeightDips = MinimumHeightDips,
            double? maxWidthDips = null,
            double? maxHeightDips = null)
        {
            var appWindow = window.AppWindow;
            var tracker = _trackers.GetValue(appWindow, _ => new WindowLimitsTracker(
                window, minWidthDips, minHeightDips, maxWidthDips, maxHeightDips));

            tracker.UpdateLimits(minWidthDips, minHeightDips, maxWidthDips, maxHeightDips);
        }

        /// <summary>
        /// Centers the window on the active display work area using DIP dimensions.
        /// </summary>
        public void CenterOnDisplay(
            double widthDips = DefaultWidthDips,
            double heightDips = DefaultHeightDips)
        {
            var widthPx = window.DipsToPixels(widthDips);
            var heightPx = window.DipsToPixels(heightDips);

            var displayArea = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                var workArea = displayArea.WorkArea;
                var x = workArea.X + (workArea.Width - widthPx) / 2;
                var y = workArea.Y + (workArea.Height - heightPx) / 2;
                window.AppWindow.MoveAndResize(new RectInt32(x, y, widthPx, heightPx));
            }
            else
            {
                window.AppWindow.Resize(new SizeInt32(widthPx, heightPx));
            }
        }
    }

    private sealed class WindowLimitsTracker
    {
        private readonly Window _window;
        private double _minWidthDips;
        private double _minHeightDips;
        private double? _maxWidthDips;
        private double? _maxHeightDips;
        private uint _lastDpi;

        public WindowLimitsTracker(
            Window window,
            double minWidthDips,
            double minHeightDips,
            double? maxWidthDips,
            double? maxHeightDips)
        {
            _window = window;
            _minWidthDips = minWidthDips;
            _minHeightDips = minHeightDips;
            _maxWidthDips = maxWidthDips;
            _maxHeightDips = maxHeightDips;
            _lastDpi = window.Dpi;

            Apply();
            _window.AppWindow.Changed += OnAppWindowChanged;
        }

        public void UpdateLimits(
            double minWidthDips,
            double minHeightDips,
            double? maxWidthDips,
            double? maxHeightDips)
        {
            _minWidthDips = minWidthDips;
            _minHeightDips = minHeightDips;
            _maxWidthDips = maxWidthDips;
            _maxHeightDips = maxHeightDips;
            Apply();
        }

        public void Apply()
        {
            if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = _window.DipsToPixels(_minWidthDips);
                presenter.PreferredMinimumHeight = _window.DipsToPixels(_minHeightDips);

                if (_maxWidthDips.HasValue)
                {
                    presenter.PreferredMaximumWidth = _window.DipsToPixels(_maxWidthDips.Value);
                }

                if (_maxHeightDips.HasValue)
                {
                    presenter.PreferredMaximumHeight = _window.DipsToPixels(_maxHeightDips.Value);
                }
            }
        }

        private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
        {
            var currentDpi = _window.Dpi;
            if (currentDpi != _lastDpi || args.DidPresenterChange)
            {
                _lastDpi = currentDpi;
                Apply();
            }
        }
    }
}
