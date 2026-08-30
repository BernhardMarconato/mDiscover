using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using mDiscover.ViewModels;

namespace mDiscover.Views.Controls;

public sealed partial class ServiceInspectorControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(DiscoveredServiceViewModel),
            typeof(ServiceInspectorControl),
            new PropertyMetadata(null));

    public DiscoveredServiceViewModel? ViewModel
    {
        get => (DiscoveredServiceViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ServiceInspectorControl()
    {
        InitializeComponent();
    }

    private void OnHeroSummaryPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width <= 0)
        {
            return;
        }

        var isNarrow = e.NewSize.Width < 500;
        VisualStateManager.GoToState(this, isNarrow ? nameof(NarrowLayout) : nameof(WideLayout), true);
    }
}
