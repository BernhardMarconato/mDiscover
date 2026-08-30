using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using mDiscover.ViewModels;

namespace mDiscover.Views;

public sealed partial class MainPage : Page
{
    private readonly HashSet<ListView> _groupListViews = [];
    private bool _isSynchronizingSelection;

    public MainViewModel ViewModel { get; }

    public MainPage() : this(App.Current.Services.GetRequiredService<MainViewModel>())
    {
    }

    public MainPage(MainViewModel viewModel)
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Auto-start discovery scan only on initial startup if idle
        if (ViewModel.DiscoveryState == Core.Models.DiscoveryState.Idle)
        {
            _ = ViewModel.StartDiscoveryAsync();
        }
    }

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        const double minLeftWidth = 260.0;
        const double minRightWidth = 320.0;
        const double sizerWidth = 8.0;

        var availableWidth = e.NewSize.Width;
        if (availableWidth <= 0)
        {
            return;
        }

        var maxAllowedLeft = Math.Max(minLeftWidth, availableWidth - minRightWidth - sizerWidth);
        LeftPaneGrid.MaxWidth = Math.Min(600.0, maxAllowedLeft);

        if (LeftPaneGrid.Width > LeftPaneGrid.MaxWidth)
        {
            ViewModel.Settings.SidebarWidth = LeftPaneGrid.MaxWidth;
        }
    }

    private void OnServiceListViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView lv)
        {
            _groupListViews.Add(lv);
            SyncListViewSelection(lv);
        }
    }

    private void OnServiceListViewUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView lv)
        {
            _groupListViews.Remove(lv);
        }
    }

    private void OnServiceListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSynchronizingSelection)
        {
            return;
        }

        if (sender is ListView activeListView && activeListView.SelectedItem is DiscoveredServiceViewModel item)
        {
            _isSynchronizingSelection = true;
            try
            {
                ViewModel.SelectedService = item;

                foreach (var lv in _groupListViews)
                {
                    if (lv != activeListView && lv.SelectedItem != null)
                    {
                        lv.SelectedItem = null;
                    }
                }
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedService))
        {
            if (_isSynchronizingSelection)
            {
                return;
            }

            _isSynchronizingSelection = true;
            try
            {
                foreach (var lv in _groupListViews)
                {
                    SyncListViewSelection(lv);
                }
            }
            finally
            {
                _isSynchronizingSelection = false;
            }
        }
    }

    private void SyncListViewSelection(ListView lv)
    {
        var target = ViewModel.SelectedService;
        if (target == null)
        {
            if (lv.SelectedItem != null)
            {
                lv.SelectedItem = null;
            }
            return;
        }

        if (lv.ItemsSource is IEnumerable<DiscoveredServiceViewModel> items)
        {
            var contains = false;
            foreach (var item in items)
            {
                if (ReferenceEquals(item, target) || item.Model.Id == target.Model.Id)
                {
                    contains = true;
                    if (lv.SelectedItem != item)
                    {
                        lv.SelectedItem = item;
                    }
                    break;
                }
            }

            if (!contains && lv.SelectedItem != null)
            {
                lv.SelectedItem = null;
            }
        }
    }
}
