using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace mDiscover.Behaviors;

/// <summary>
/// A XAML behavior that enables text selection on the message <see cref="TextBlock"/> of an <see cref="InfoBar"/>.
/// </summary>
public partial class SelectableInfoBarMessageBehavior : Behavior<InfoBar>
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(SelectableInfoBarMessageBehavior),
            new PropertyMetadata(true, OnIsEnabledChanged));

    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            if (AssociatedObject.IsLoaded)
            {
                UpdateTextSelection();
            }

            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.SizeChanged += OnSizeChanged;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateTextSelection();
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateTextSelection();

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SelectableInfoBarMessageBehavior behavior)
        {
            behavior.UpdateTextSelection();
        }
    }

    private void UpdateTextSelection()
    {
        if (AssociatedObject?.FindDescendant<TextBlock>(tb => tb.Name == "Message") is TextBlock textBlock)
        {
            textBlock.IsTextSelectionEnabled = IsEnabled;
        }
    }
}
