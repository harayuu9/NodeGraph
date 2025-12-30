using Avalonia.Controls;
using NodeGraph.App.Services;
using NodeGraph.App.ViewModels;

namespace NodeGraph.App.Views;

public partial class ExecutionHistoryWindow : Window
{
    public ExecutionHistoryWindow()
    {
        InitializeComponent();
    }

    public ExecutionHistoryWindow(ExecutionHistoryService historyService)
    {
        InitializeComponent();
        DataContext = new ExecutionHistoryWindowViewModel(historyService);
    }
}
