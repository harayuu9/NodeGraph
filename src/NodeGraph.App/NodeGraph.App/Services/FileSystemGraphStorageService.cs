using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Serialization;
using NodeGraph.Model.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NodeGraph.App.Services;

/// <summary>
/// インデックスファイルのシリアライズ用データ構造
/// </summary>
internal class GraphIndexData
{
    public string Version { get; set; } = "1.0.0";
    public List<GraphIndexEntry> Graphs { get; set; } = [];
}

internal class GraphIndexEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// ファイルシステムを使用したグラフ保存サービス実装。
/// Desktop、Android、iOSで使用される。
/// </summary>
public class FileSystemGraphStorageService : IGraphStorageService
{
    private const string IndexFileName = "graphs-index.yml";
    private readonly string _storageDirectory;
    private readonly string _indexFilePath;

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public FileSystemGraphStorageService()
    {
        // %AppData%/NodeGraph/graphs/ に保存
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "NodeGraph", "graphs");
        Directory.CreateDirectory(_storageDirectory);
        _indexFilePath = Path.Combine(_storageDirectory, IndexFileName);
    }

    public Task<IReadOnlyList<GraphMetadata>> GetSavedGraphsAsync()
    {
        var index = LoadIndex();
        var result = index.Graphs
            .Select(g => new GraphMetadata(g.Id, g.Name, g.CreatedAt, g.ModifiedAt))
            .OrderByDescending(g => g.ModifiedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<GraphMetadata>>(result);
    }

    public Task<string> SaveGraphAsync(EditorGraph graph, string name, string? existingId = null)
    {
        var id = existingId ?? Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        // グラフファイルを保存
        var graphPath = GetGraphPath(id);
        var layoutPath = GetLayoutPath(id);

        GraphSerializer.SaveToYaml(graph.Graph, graphPath);
        EditorLayoutSerializer.SaveLayoutToFile(graph, layoutPath);

        // インデックスを更新
        var index = LoadIndex();
        var existingEntry = index.Graphs.FirstOrDefault(g => g.Id == id);

        if (existingEntry != null)
        {
            existingEntry.Name = name;
            existingEntry.ModifiedAt = now;
        }
        else
        {
            index.Graphs.Add(new GraphIndexEntry
            {
                Id = id,
                Name = name,
                CreatedAt = now,
                ModifiedAt = now
            });
        }

        SaveIndex(index);
        return Task.FromResult(id);
    }

    public Task<EditorGraph?> LoadGraphAsync(string graphId, SelectionManager selectionManager)
    {
        var graphPath = GetGraphPath(graphId);
        var layoutPath = GetLayoutPath(graphId);

        if (!File.Exists(graphPath))
        {
            return Task.FromResult<EditorGraph?>(null);
        }

        try
        {
            var graph = GraphSerializer.LoadFromYaml(graphPath);
            var editorGraph = new EditorGraph(graph, selectionManager);

            if (File.Exists(layoutPath))
            {
                EditorLayoutSerializer.LoadLayoutFromFile(layoutPath, editorGraph);
            }

            return Task.FromResult<EditorGraph?>(editorGraph);
        }
        catch
        {
            return Task.FromResult<EditorGraph?>(null);
        }
    }

    public Task<bool> DeleteGraphAsync(string graphId)
    {
        try
        {
            // ファイルを削除
            var graphPath = GetGraphPath(graphId);
            var layoutPath = GetLayoutPath(graphId);

            if (File.Exists(graphPath)) File.Delete(graphPath);
            if (File.Exists(layoutPath)) File.Delete(layoutPath);

            // インデックスを更新
            var index = LoadIndex();
            var entry = index.Graphs.FirstOrDefault(g => g.Id == graphId);
            if (entry != null)
            {
                index.Graphs.Remove(entry);
                SaveIndex(index);
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> RenameGraphAsync(string graphId, string newName)
    {
        try
        {
            var index = LoadIndex();
            var entry = index.Graphs.FirstOrDefault(g => g.Id == graphId);

            if (entry == null)
            {
                return Task.FromResult(false);
            }

            entry.Name = newName;
            entry.ModifiedAt = DateTime.UtcNow;
            SaveIndex(index);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string GetGraphPath(string id) => Path.Combine(_storageDirectory, $"{id}.graph.yml");
    private string GetLayoutPath(string id) => Path.Combine(_storageDirectory, $"{id}.layout.yml");

    private GraphIndexData LoadIndex()
    {
        if (!File.Exists(_indexFilePath))
        {
            return new GraphIndexData();
        }

        try
        {
            var yaml = File.ReadAllText(_indexFilePath);
            return YamlDeserializer.Deserialize<GraphIndexData>(yaml) ?? new GraphIndexData();
        }
        catch
        {
            return new GraphIndexData();
        }
    }

    private void SaveIndex(GraphIndexData index)
    {
        var yaml = YamlSerializer.Serialize(index);
        File.WriteAllText(_indexFilePath, yaml);
    }
}
