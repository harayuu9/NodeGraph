using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Services;
using NodeGraph.App.Undo;
using NodeGraph.Model;

namespace NodeGraph.App.ViewModels;

/// <summary>
/// ダッシュボードUI全体を管理するViewModel
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IGraphStorageService _storageService;
    private readonly SelectionManager _selectionManager;
    private readonly CommonParameterService _parameterService;
    private readonly ExecutionHistoryService _historyService;

    [ObservableProperty] private EditorGraph? _currentGraph;
    [ObservableProperty] private GraphListItemViewModel? _selectedGraphItem;
    [ObservableProperty] private bool _isExecuting;
    [ObservableProperty] private string _executionStatus = "Ready";
    [ObservableProperty] private bool _isLeftPanelVisible = true;
    [ObservableProperty] private bool _isOutputPanelVisible = true;
    [ObservableProperty] private string? _currentGraphId;
    [ObservableProperty] private string _currentGraphName = "Untitled";

    /// <summary>
    /// 保存済みグラフ一覧
    /// </summary>
    public ObservableCollection<GraphListItemViewModel> SavedGraphs { get; } = [];

    /// <summary>
    /// パラメータ一覧
    /// </summary>
    public ObservableCollection<ParameterItemViewModel> Parameters { get; } = [];

    /// <summary>
    /// 実行出力ログ
    /// </summary>
    public ObservableCollection<string> OutputLines { get; } = [];

    /// <summary>
    /// UndoRedoManager
    /// </summary>
    public UndoRedoManager UndoRedoManager { get; }

    /// <summary>
    /// InspectorViewModel
    /// </summary>
    public InspectorViewModel InspectorViewModel { get; }

    public DashboardViewModel(
        IGraphStorageService storageService,
        SelectionManager selectionManager,
        UndoRedoManager undoRedoManager,
        CommonParameterService parameterService,
        ExecutionHistoryService historyService)
    {
        _storageService = storageService;
        _selectionManager = selectionManager;
        UndoRedoManager = undoRedoManager;
        _parameterService = parameterService;
        _historyService = historyService;
        InspectorViewModel = new InspectorViewModel(selectionManager);

        // 初期グラフを作成
        CreateNewGraphInternal();

        // パラメータを読み込み
        LoadParameters();
    }

#if DEBUG
    /// <summary>
    /// デザイナ用コンストラクタ
    /// </summary>
    public DashboardViewModel() : this(null!, null!, null!, null!, null!)
    {
    }
#endif

    /// <summary>
    /// 保存済みグラフ一覧を読み込み
    /// </summary>
    public async Task LoadSavedGraphsAsync()
    {
        var graphs = await _storageService.GetSavedGraphsAsync();
        SavedGraphs.Clear();
        foreach (var graph in graphs)
        {
            var item = new GraphListItemViewModel(graph);
            item.RenameConfirmed += OnGraphRenameConfirmed;
            SavedGraphs.Add(item);
        }
    }

    /// <summary>
    /// 新規グラフ作成
    /// </summary>
    [RelayCommand]
    private void NewGraph()
    {
        CreateNewGraphInternal();
        UndoRedoManager.Clear();
        OutputLines.Clear();
        ExecutionStatus = "Ready";
    }

    private void CreateNewGraphInternal()
    {
        var graph = new Graph();
        CurrentGraph = new EditorGraph(graph, _selectionManager);
        CurrentGraphId = null;
        CurrentGraphName = "Untitled";
    }

    /// <summary>
    /// グラフを保存
    /// </summary>
    [RelayCommand]
    private async Task SaveGraphAsync()
    {
        if (CurrentGraph == null) return;

        CurrentGraphId = await _storageService.SaveGraphAsync(CurrentGraph, CurrentGraphName, CurrentGraphId);

        // グラフ一覧を更新
        await LoadSavedGraphsAsync();

        AddOutputLine($"Graph saved: {CurrentGraphName}");
    }

    /// <summary>
    /// 選択されたグラフを読み込み
    /// </summary>
    [RelayCommand]
    private async Task LoadSelectedGraphAsync()
    {
        if (SelectedGraphItem == null) return;

        var graph = await _storageService.LoadGraphAsync(SelectedGraphItem.Id, _selectionManager);
        if (graph != null)
        {
            CurrentGraph = graph;
            CurrentGraphId = SelectedGraphItem.Id;
            CurrentGraphName = SelectedGraphItem.Name;
            UndoRedoManager.Clear();
            OutputLines.Clear();
            ExecutionStatus = "Ready";
            AddOutputLine($"Graph loaded: {CurrentGraphName}");
        }
    }

    /// <summary>
    /// 選択されたグラフを削除
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedGraphAsync()
    {
        if (SelectedGraphItem == null) return;

        var graphName = SelectedGraphItem.Name;
        var result = await _storageService.DeleteGraphAsync(SelectedGraphItem.Id);

        if (result)
        {
            // 現在編集中のグラフが削除された場合は新規作成
            if (CurrentGraphId == SelectedGraphItem.Id)
            {
                NewGraph();
            }

            await LoadSavedGraphsAsync();
            AddOutputLine($"Graph deleted: {graphName}");
        }
    }

    /// <summary>
    /// グラフを実行
    /// </summary>
    [RelayCommand]
    private async Task ExecuteGraphAsync()
    {
        if (CurrentGraph == null || IsExecuting) return;

        try
        {
            IsExecuting = true;
            ExecutionStatus = "Executing...";
            AddOutputLine("Execution started");

            await CurrentGraph.ExecuteAsync(_parameterService, _historyService);

            ExecutionStatus = "Completed";
            AddOutputLine("Execution completed successfully");
        }
        catch (Exception ex)
        {
            ExecutionStatus = "Error";
            AddOutputLine($"Execution failed: {ex.Message}");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// 左パネルの表示/非表示を切り替え
    /// </summary>
    [RelayCommand]
    private void ToggleLeftPanel()
    {
        IsLeftPanelVisible = !IsLeftPanelVisible;
    }

    /// <summary>
    /// 出力パネルの表示/非表示を切り替え
    /// </summary>
    [RelayCommand]
    private void ToggleOutputPanel()
    {
        IsOutputPanelVisible = !IsOutputPanelVisible;
    }

    /// <summary>
    /// 出力ログをクリア
    /// </summary>
    [RelayCommand]
    private void ClearOutput()
    {
        OutputLines.Clear();
    }

    /// <summary>
    /// Undo
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        if (UndoRedoManager.CanUndo())
        {
            UndoRedoManager.Undo();
        }
    }

    /// <summary>
    /// Redo
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        if (UndoRedoManager.CanRedo())
        {
            UndoRedoManager.Redo();
        }
    }

    private void LoadParameters()
    {
        Parameters.Clear();
        var parameters = _parameterService.GetParameters();
        var fileParams = _parameterService.GetFileParameters();

        foreach (var kvp in parameters)
        {
            var item = new ParameterItemViewModel
            {
                Name = kvp.Key,
                Value = kvp.Value?.ToString() ?? "",
                IsFromEnvironment = !fileParams.ContainsKey(kvp.Key)
            };
            item.ValueChanged += OnParameterValueChanged;
            Parameters.Add(item);
        }
    }

    private void OnParameterValueChanged(ParameterItemViewModel item)
    {
        _parameterService.SetParameter(item.Name, item.Value);
    }

    private async void OnGraphRenameConfirmed(GraphListItemViewModel item)
    {
        await _storageService.RenameGraphAsync(item.Id, item.Name);

        // 現在編集中のグラフの場合は名前を更新
        if (CurrentGraphId == item.Id)
        {
            CurrentGraphName = item.Name;
        }
    }

    private void AddOutputLine(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        OutputLines.Add($"[{timestamp}] {message}");
    }
}
