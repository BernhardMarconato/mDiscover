using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using mDiscover.ViewModels;

namespace mDiscover.Views.Controls;

public sealed partial class MainToolbarControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(MainViewModel),
            typeof(MainToolbarControl),
            new PropertyMetadata(null));

    public MainViewModel? ViewModel
    {
        get => (MainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public MainToolbarControl()
    {
        InitializeComponent();
    }
}
