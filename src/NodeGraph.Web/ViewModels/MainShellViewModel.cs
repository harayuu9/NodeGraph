using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace NodeGraph.Web.ViewModels;

/// <summary>
/// メインシェルのViewModel。ナビゲーションを管理する。
/// </summary>
public partial class MainShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentViewName = "Dashboard";

    [ObservableProperty]
    private bool _isDashboardView = true;

    public MainShellViewModel(IServiceProvider services)
    {
        _services = services;
        NavigateToDashboard();
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        var dashboard = _services.GetRequiredService<DashboardViewModel>();
        dashboard.EditGraphRequested += OnEditGraphRequested;
        CurrentView = dashboard;
        CurrentViewName = "Dashboard";
        IsDashboardView = true;
    }

    [RelayCommand]
    private void NavigateToEditor()
    {
        var editor = _services.GetRequiredService<GraphEditorViewModel>();
        editor.BackToDashboardRequested += OnBackToDashboardRequested;
        CurrentView = editor;
        CurrentViewName = "Editor";
        IsDashboardView = false;
    }

    private void OnEditGraphRequested(object? sender, EditGraphEventArgs e)
    {
        var editor = _services.GetRequiredService<GraphEditorViewModel>();
        editor.BackToDashboardRequested += OnBackToDashboardRequested;

        if (e.IsNew)
        {
            editor.CreateNewGraph(e.GraphName);
        }
        else
        {
            _ = editor.LoadGraphAsync(e.GraphName);
        }

        CurrentView = editor;
        CurrentViewName = "Editor";
        IsDashboardView = false;
    }

    private void OnBackToDashboardRequested(object? sender, EventArgs e)
    {
        NavigateToDashboard();
    }
}

/// <summary>
/// グラフ編集リクエストのイベント引数
/// </summary>
public class EditGraphEventArgs : EventArgs
{
    public string GraphName { get; }
    public bool IsNew { get; }

    public EditGraphEventArgs(string graphName, bool isNew)
    {
        GraphName = graphName;
        IsNew = isNew;
    }
}
