using Avalonia.Controls;
using Avalonia.Interactivity;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.ViewModels;

namespace NodeGraph.Editor.Views;

public partial class ParametersWindow : Window
{
    public ParametersWindow()
    {
        InitializeComponent();
    }

    public ParametersWindow(CommonParameterService commonParameterService)
    {
        InitializeComponent();
        DataContext = new ParametersWindowViewModel(commonParameterService);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
