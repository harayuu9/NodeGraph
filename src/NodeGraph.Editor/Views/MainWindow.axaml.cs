using Avalonia.Controls;
using NodeGraph.Editor.ViewModels;

namespace NodeGraph.Editor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // ViewModelにWindowを設定
        this.Opened += (sender, args) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SetMainWindow(this);
            }
        };
    }
}