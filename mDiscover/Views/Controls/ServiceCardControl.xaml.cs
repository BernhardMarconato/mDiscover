using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using mDiscover.ViewModels;

namespace mDiscover.Views.Controls;

public sealed partial class ServiceCardControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(DiscoveredServiceViewModel),
            typeof(ServiceCardControl),
            new PropertyMetadata(null));

    public DiscoveredServiceViewModel? ViewModel
    {
        get => (DiscoveredServiceViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ServiceCardControl()
    {
        InitializeComponent();
    }
}
