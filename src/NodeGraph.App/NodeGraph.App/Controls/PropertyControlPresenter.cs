using Avalonia;
using Avalonia.Controls;
using NodeGraph.App.ViewModels;

namespace NodeGraph.App.Controls;

/// <summary>
/// PropertyViewModelから適切なコントロールを生成して表示するプレゼンター。
/// </summary>
public class PropertyControlPresenter : ContentControl
{
    public static readonly StyledProperty<PropertyViewModel?> PropertyViewModelProperty =
        AvaloniaProperty.Register<PropertyControlPresenter, PropertyViewModel?>(nameof(PropertyViewModel));

    static PropertyControlPresenter()
    {
        PropertyViewModelProperty.Changed.AddClassHandler<PropertyControlPresenter>(OnPropertyViewModelChanged);
    }

    public PropertyViewModel? PropertyViewModel
    {
        get => GetValue(PropertyViewModelProperty);
        set => SetValue(PropertyViewModelProperty, value);
    }

    private static void OnPropertyViewModelChanged(PropertyControlPresenter sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is PropertyViewModel propertyViewModel)
            sender.Content = PropertyControlFactory.CreateControl(propertyViewModel);
        else
            sender.Content = null;
    }
}