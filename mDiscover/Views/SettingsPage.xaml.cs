using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using mDiscover.ViewModels;

namespace mDiscover.Views;

public sealed partial class SettingsPage : Page
{
    public MainViewModel ViewModel { get; }

    public SettingsPage() : this(App.Current.Services.GetRequiredService<MainViewModel>())
    {
    }

    public SettingsPage(MainViewModel viewModel)
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }
}
