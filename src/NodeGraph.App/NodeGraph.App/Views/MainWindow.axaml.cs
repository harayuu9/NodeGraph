using Avalonia.Controls;
using NodeGraph.App.ViewModels;

namespace NodeGraph.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // ViewModelにWindowを設定
        Opened += (sender, args) =>
        {
            if (DataContext is MainWindowViewModel viewModel) viewModel.SetMainWindow(this);
        };
    }
}