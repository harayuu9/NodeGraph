using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Serialization;
using NodeGraph.Model.Serialization;

namespace NodeGraph.App.Services;

/// <summary>
/// ブラウザ向けのグラフ保存サービス実装。
/// 現在はメモリベースの実装（セッション間でデータは保持されない）。
/// 将来的にはlocalStorageを使用した永続化を実装予定。
/// </summary>
public class BrowserGraphStorageService : IGraphStorageService
{
    // メモリ内ストレージ（セッション内でのみ有効）
    private readonly Dictionary<string, (string graphYaml, string layoutYaml, GraphMetadata metadata)> _storage = new();

    public Task<IReadOnlyList<GraphMetadata>> GetSavedGraphsAsync()
    {
        var result = _storage.Values
            .Select(v => v.metadata)
            .OrderByDescending(g => g.ModifiedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<GraphMetadata>>(result);
    }

    public Task<string> SaveGraphAsync(EditorGraph graph, string name, string? existingId = null)
    {
        var id = existingId ?? Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        // グラフをYAML形式でシリアライズ
        var graphYaml = GraphSerializer.Serialize(graph.Graph);
        var layoutYaml = EditorLayoutSerializer.SaveLayout(graph);

        // 既存エントリの作成日時を保持
        var createdAt = now;
        if (_storage.TryGetValue(id, out var existing))
        {
            createdAt = existing.metadata.CreatedAt;
        }

        var metadata = new GraphMetadata(id, name, createdAt, now);
        _storage[id] = (graphYaml, layoutYaml, metadata);

        return Task.FromResult(id);
    }

    public Task<EditorGraph?> LoadGraphAsync(string graphId, SelectionManager selectionManager)
    {
        if (!_storage.TryGetValue(graphId, out var data))
        {
            return Task.FromResult<EditorGraph?>(null);
        }

        try
        {
            var graph = GraphSerializer.Deserialize(data.graphYaml);
            var editorGraph = new EditorGraph(graph, selectionManager);

            if (!string.IsNullOrEmpty(data.layoutYaml))
            {
                EditorLayoutSerializer.LoadLayout(data.layoutYaml, editorGraph);
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
        return Task.FromResult(_storage.Remove(graphId));
    }

    public Task<bool> RenameGraphAsync(string graphId, string newName)
    {
        if (!_storage.TryGetValue(graphId, out var data))
        {
            return Task.FromResult(false);
        }

        var newMetadata = new GraphMetadata(
            data.metadata.Id,
            newName,
            data.metadata.CreatedAt,
            DateTime.UtcNow
        );

        _storage[graphId] = (data.graphYaml, data.layoutYaml, newMetadata);
        return Task.FromResult(true);
    }
}
