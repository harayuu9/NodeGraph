using Avalonia.Controls;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.ViewModels;

namespace NodeGraph.Editor.Views;

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
