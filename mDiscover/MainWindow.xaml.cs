using mDiscover.Services;
using mDiscover.ViewModels;
using mDiscover.Views;
using Microsoft.UI.Xaml;

namespace mDiscover;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel, WindowPlacementService windowPlacementService)
    {
        Content = new MainWindowContent(this, viewModel);
        InitializeComponent();

        var iconPath = Path.Combine("Assets", "app.ico");
        AppWindow.SetIcon(iconPath);

        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode;

        // Restore and track window placement (position, size, maximized state)
        windowPlacementService.TrackWindow(this);
    }
}
