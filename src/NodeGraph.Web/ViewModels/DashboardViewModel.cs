using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Web.Storage;

namespace NodeGraph.Web.ViewModels;

/// <summary>
/// ダッシュボードのViewModel。グラフ一覧の管理を行う。
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IStorageProvider _storage;
    private const string GraphPrefix = "nodegraph:graph:";
    private const string MetaPrefix = "nodegraph:meta:";

    public ObservableCollection<GraphItemViewModel> Graphs { get; } = new();

    [ObservableProperty]
    private GraphItemViewModel? _selectedGraph;

    [ObservableProperty]
    private bool _isLoading;

    public event EventHandler<EditGraphEventArgs>? EditGraphRequested;

    public DashboardViewModel(IStorageProvider storage)
    {
        _storage = storage;
        _ = LoadGraphsAsync();
    }

    [RelayCommand]
    private async Task LoadGraphsAsync()
    {
        IsLoading = true;
        try
        {
            Graphs.Clear();

            var keys = await _storage.ListKeysAsync(GraphPrefix);
            foreach (var key in keys)
            {
                var graphName = key[GraphPrefix.Length..];
                var meta = await LoadMetaAsync(graphName);

                Graphs.Add(new GraphItemViewModel
                {
                    Name = graphName,
                    LastModified = meta?.LastModified ?? DateTime.Now,
                    NodeCount = meta?.NodeCount ?? 0,
                    ConnectionCount = meta?.ConnectionCount ?? 0
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NewGraph()
    {
        var graphName = $"Graph_{DateTime.Now:yyyyMMdd_HHmmss}";
        EditGraphRequested?.Invoke(this, new EditGraphEventArgs(graphName, isNew: true));
    }

    [RelayCommand]
    private async Task UploadGraphAsync()
    {
        var result = await _storage.UploadFileAsync();
        if (result == null) return;

        var (filename, content) = result.Value;

        // ファイル名からグラフ名を生成
        var graphName = Path.GetFileNameWithoutExtension(filename);
        if (graphName.EndsWith(".graph"))
        {
            graphName = graphName[..^6]; // ".graph" を除去
        }

        // 保存
        await _storage.WriteTextAsync($"{GraphPrefix}{graphName}", content);

        // メタデータを更新
        await SaveMetaAsync(graphName, new GraphMeta
        {
            LastModified = DateTime.Now,
            NodeCount = 0,
            ConnectionCount = 0
        });

        // 一覧を更新
        await LoadGraphsAsync();
    }

    [RelayCommand]
    private void EditGraph(GraphItemViewModel? item)
    {
        if (item == null) return;
        EditGraphRequested?.Invoke(this, new EditGraphEventArgs(item.Name, isNew: false));
    }

    [RelayCommand]
    private async Task DownloadGraphAsync(GraphItemViewModel? item)
    {
        if (item == null) return;

        var content = await _storage.ReadTextAsync($"{GraphPrefix}{item.Name}");
        if (content == null) return;

        await _storage.DownloadFileAsync($"{item.Name}.graph.yml", content);
    }

    [RelayCommand]
    private async Task DeleteGraphAsync(GraphItemViewModel? item)
    {
        if (item == null) return;

        await _storage.DeleteAsync($"{GraphPrefix}{item.Name}");
        await _storage.DeleteAsync($"{MetaPrefix}{item.Name}");
        await _storage.DeleteAsync($"nodegraph:layout:{item.Name}");

        Graphs.Remove(item);
    }

    private async Task<GraphMeta?> LoadMetaAsync(string graphName)
    {
        var json = await _storage.ReadTextAsync($"{MetaPrefix}{graphName}");
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<GraphMeta>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveMetaAsync(string graphName, GraphMeta meta)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(meta);
        await _storage.WriteTextAsync($"{MetaPrefix}{graphName}", json);
    }
}

/// <summary>
/// グラフアイテムのViewModel
/// </summary>
public partial class GraphItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DateTime _lastModified;

    [ObservableProperty]
    private int _nodeCount;

    [ObservableProperty]
    private int _connectionCount;
}

/// <summary>
/// グラフのメタデータ
/// </summary>
public class GraphMeta
{
    public DateTime LastModified { get; set; }
    public int NodeCount { get; set; }
    public int ConnectionCount { get; set; }
}
