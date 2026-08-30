using mDiscover.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace mDiscover.Views;

public sealed partial class MainWindowContent : Grid
{
    public MainViewModel ViewModel { get; }

    public MainWindowContent(MainWindow mainWindow, MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        mainWindow.ExtendsContentIntoTitleBar = true;
        mainWindow.SetTitleBar(AppTitleBar);

        // Initial navigation
        ContentFrame.Navigate(typeof(MainPage));

        // Listen for Settings navigation state changes
        ViewModel.Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsSettingsOpen))
            {
                UpdateNavigation();
            }
        };
    }

    private void UpdateNavigation()
    {
        if (ViewModel.Settings.IsSettingsOpen)
        {
            if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
            {
                ContentFrame.Navigate(typeof(SettingsPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }
        else
        {
            if (ContentFrame.CurrentSourcePageType != typeof(MainPage))
            {
                ContentFrame.Navigate(typeof(MainPage), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
        }
    }

    private void OnTitleBarBackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        ViewModel.Settings.CloseSettingsCommand.Execute(null);
    }

    public bool IsSearchVisible(bool isSettingsOpen) => !isSettingsOpen;
}
