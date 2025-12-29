using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Serialization;
using NodeGraph.Editor.Services;
using NodeGraph.Model;
using NodeGraph.Model.Serialization;
using NodeGraph.Web.Storage;

namespace NodeGraph.Web.ViewModels;

/// <summary>
/// グラフエディタのViewModel
/// </summary>
public partial class GraphEditorViewModel : ViewModelBase
{
    private readonly IStorageProvider _storage;
    private readonly NodeTypeService _nodeTypeService;
    private readonly SelectionManager _selectionManager;

    private const string GraphPrefix = "nodegraph:graph:";
    private const string LayoutPrefix = "nodegraph:layout:";
    private const string MetaPrefix = "nodegraph:meta:";

    /// <summary>
    /// パラメータパネルのViewModel
    /// </summary>
    public ParametersPanelViewModel ParametersPanel { get; }

    [ObservableProperty]
    private EditorGraph? _graph;

    [ObservableProperty]
    private string _graphName = string.Empty;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _executionStatus = "Ready";

    [ObservableProperty]
    private bool _hasChanges;

    public ObservableCollection<string> ExecutionLogs { get; } = new();

    public event EventHandler? BackToDashboardRequested;

    public GraphEditorViewModel(
        IStorageProvider storage,
        NodeTypeService nodeTypeService,
        SelectionManager selectionManager,
        ParametersPanelViewModel parametersPanel)
    {
        _storage = storage;
        _nodeTypeService = nodeTypeService;
        _selectionManager = selectionManager;
        ParametersPanel = parametersPanel;
    }

    /// <summary>
    /// 新規グラフを作成
    /// </summary>
    public void CreateNewGraph(string name)
    {
        GraphName = name;
        var graph = new Graph();
        Graph = new EditorGraph(graph, _selectionManager);
        HasChanges = true;
    }

    /// <summary>
    /// グラフを読み込む
    /// </summary>
    public async Task LoadGraphAsync(string name)
    {
        GraphName = name;

        var yaml = await _storage.ReadTextAsync($"{GraphPrefix}{name}");
        if (string.IsNullOrEmpty(yaml))
        {
            CreateNewGraph(name);
            return;
        }

        try
        {
            var graph = GraphSerializer.Deserialize(yaml);
            Graph = new EditorGraph(graph, _selectionManager);

            // レイアウトを読み込む
            var layoutYaml = await _storage.ReadTextAsync($"{LayoutPrefix}{name}");
            if (!string.IsNullOrEmpty(layoutYaml))
            {
                EditorLayoutSerializer.LoadLayout(layoutYaml, Graph);
            }

            HasChanges = false;
        }
        catch (Exception ex)
        {
            ExecutionLogs.Add($"Error loading graph: {ex.Message}");
            CreateNewGraph(name);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Graph == null) return;

        try
        {
            // グラフを保存
            var yaml = GraphSerializer.Serialize(Graph.Graph);
            await _storage.WriteTextAsync($"{GraphPrefix}{GraphName}", yaml);

            // レイアウトを保存
            var layoutYaml = EditorLayoutSerializer.SaveLayout(Graph);
            await _storage.WriteTextAsync($"{LayoutPrefix}{GraphName}", layoutYaml);

            // メタデータを保存
            var meta = new GraphMeta
            {
                LastModified = DateTime.Now,
                NodeCount = Graph.Nodes.Count,
                ConnectionCount = Graph.Connections.Count
            };
            var metaJson = System.Text.Json.JsonSerializer.Serialize(meta);
            await _storage.WriteTextAsync($"{MetaPrefix}{GraphName}", metaJson);

            HasChanges = false;
            ExecutionLogs.Add($"Saved: {GraphName}");
        }
        catch (Exception ex)
        {
            ExecutionLogs.Add($"Error saving: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (Graph == null) return;

        var yaml = GraphSerializer.Serialize(Graph.Graph);
        await _storage.DownloadFileAsync($"{GraphName}.graph.yml", yaml);
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (Graph == null) return;

        IsExecuting = true;
        ExecutionStatus = "Executing...";
        ExecutionLogs.Clear();

        var startTime = DateTime.Now;

        try
        {
            await Graph.ExecuteAsync(null, null, CancellationToken.None);

            var elapsed = DateTime.Now - startTime;
            ExecutionStatus = $"Completed ({elapsed.TotalMilliseconds:F0}ms)";
            ExecutionLogs.Add($"Execution completed successfully in {elapsed.TotalMilliseconds:F0}ms");
        }
        catch (Exception ex)
        {
            ExecutionStatus = "Error";
            ExecutionLogs.Add($"Error: {ex.Message}");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearLogs()
    {
        ExecutionLogs.Clear();
    }
}
