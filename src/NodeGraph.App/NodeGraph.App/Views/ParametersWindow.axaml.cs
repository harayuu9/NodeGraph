using Avalonia.Controls;
using Avalonia.Interactivity;
using NodeGraph.App.Services;
using NodeGraph.App.ViewModels;

namespace NodeGraph.App.Views;

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
