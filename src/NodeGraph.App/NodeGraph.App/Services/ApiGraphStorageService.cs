using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NodeGraph.App.Models;
using NodeGraph.App.Selection;
using NodeGraph.App.Serialization;
using NodeGraph.Model.Serialization;

namespace NodeGraph.App.Services;

/// <summary>
/// API経由でグラフを保存・読み込みするサービス実装。
/// ブラウザ環境でバックエンドAPIと通信します。
/// </summary>
public class ApiGraphStorageService : IGraphStorageService
{
    private readonly HttpClient _httpClient;

    public ApiGraphStorageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<GraphMetadata>> GetSavedGraphsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<GraphMetadataDto>>("api/graphs");
            if (response == null)
            {
                return Array.Empty<GraphMetadata>();
            }

            return response
                .Select(dto => new GraphMetadata(dto.Id, dto.Name, dto.CreatedAt, dto.ModifiedAt))
                .ToList();
        }
        catch
        {
            return Array.Empty<GraphMetadata>();
        }
    }

    public async Task<string> SaveGraphAsync(EditorGraph graph, string name, string? existingId = null)
    {
        var graphYaml = GraphSerializer.Serialize(graph.Graph);
        var layoutYaml = EditorLayoutSerializer.SaveLayout(graph);

        var request = new SaveGraphRequest(name, graphYaml, layoutYaml);

        if (existingId != null)
        {
            // 既存グラフを更新
            var response = await _httpClient.PutAsJsonAsync($"api/graphs/{existingId}", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SaveGraphResponse>();
            return existingId;
        }
        else
        {
            // 新規作成
            var response = await _httpClient.PostAsJsonAsync("api/graphs", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SaveGraphResponse>();
            return result?.Id ?? throw new InvalidOperationException("Failed to get graph ID from response");
        }
    }

    public async Task<EditorGraph?> LoadGraphAsync(string graphId, SelectionManager selectionManager)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<GraphDataDto>($"api/graphs/{graphId}");
            if (response == null)
            {
                return null;
            }

            var graph = GraphSerializer.Deserialize(response.GraphYaml);
            var editorGraph = new EditorGraph(graph, selectionManager);

            if (!string.IsNullOrEmpty(response.LayoutYaml))
            {
                EditorLayoutSerializer.LoadLayout(response.LayoutYaml, editorGraph);
            }

            return editorGraph;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteGraphAsync(string graphId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/graphs/{graphId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RenameGraphAsync(string graphId, string newName)
    {
        try
        {
            var request = new RenameGraphRequest(newName);
            var response = await _httpClient.PatchAsJsonAsync($"api/graphs/{graphId}/name", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // DTOs matching the API contracts
    private record GraphMetadataDto(string Id, string Name, DateTime CreatedAt, DateTime ModifiedAt);
    private record GraphDataDto(string Id, string Name, string GraphYaml, string LayoutYaml, DateTime CreatedAt, DateTime ModifiedAt);
    private record SaveGraphRequest(string Name, string GraphYaml, string LayoutYaml);
    private record SaveGraphResponse(string Id, DateTime CreatedAt, DateTime ModifiedAt);
    private record RenameGraphRequest(string NewName);
}
