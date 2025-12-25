using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Serialization;
using NodeGraph.Editor.Services;
using NodeGraph.Model;
using NodeGraph.Model.Pool;
using NodeGraph.Model.Serialization;

namespace NodeGraph.Editor.Models;

public partial class EditorGraph : ObservableObject
{
    private Dictionary<string, object?>? _runtimeParameters;
    public EditorGraph(Graph graph, SelectionManager selectionManager)
    {
        Graph = graph;
        SelectionManager = selectionManager;
        foreach (var graphNode in Graph.Nodes) Nodes.Add(new EditorNode(selectionManager, graphNode));

        // 既存の接続を読み込む
        LoadConnections(Nodes);
    }

    public SelectionManager SelectionManager { get; }

    [ObservableProperty] public partial bool IsExecuting { get; set; }
    [ObservableProperty] public partial string? CurrentFilePath { get; set; }

    public ObservableCollection<EditorNode> Nodes { get; } = [];
    public ObservableCollection<EditorConnection> Connections { get; } = [];

    public Graph Graph { get; }

    private void LoadConnections<T>(T nodes)
        where T : IEnumerable<EditorNode>
    {
        foreach (var editorNode in nodes)
        {
            // データポートの接続をロード
            foreach (var editorPort in editorNode.OutputPorts)
            {
                var outputPort = (OutputPort)editorPort.Port;
                var connectedPorts = outputPort.ConnectedPorts;
                foreach (var inputPort in connectedPorts)
                {
                    // 接続先のEditorNodeとEditorPortを検索
                    var targetNode = nodes.FirstOrDefault(n => n.InputPorts.Any(p => p.Port == inputPort));

                    var targetPort = targetNode?.InputPorts.FirstOrDefault(p => p.Port == inputPort);
                    if (targetPort == null) continue;

                    // 接続を作成
                    var connection = new EditorConnection(editorNode, editorPort, targetNode!, targetPort);
                    Connections.Add(connection);
                }
            }

            // Execポートの接続をロード（ExecOutPortは単一接続のみ）
            foreach (var editorPort in editorNode.ExecOutPorts)
            {
                var execOutPort = (ExecOutPort)editorPort.Port;
                if (execOutPort.ConnectedPort is ExecInPort execInPort)
                {
                    // 接続先のEditorNodeとEditorPortを検索
                    var targetNode = nodes.FirstOrDefault(n => n.ExecInPorts.Any(p => p.Port == execInPort));

                    var targetPort = targetNode?.ExecInPorts.FirstOrDefault(p => p.Port == execInPort);
                    if (targetPort == null) continue;

                    // 接続を作成
                    var connection = new EditorConnection(editorNode, editorPort, targetNode!, targetPort);
                    Connections.Add(connection);
                }
            }
        }
    }

    /// <summary>
    /// 次の実行に使用するランタイムパラメータを設定します。
    /// これらはCommonParameterを上書きします。
    /// </summary>
    public void SetRuntimeParameters(Dictionary<string, object?>? parameters)
    {
        _runtimeParameters = parameters;
    }

    /// <summary>
    /// ランタイムパラメータをクリアします。
    /// </summary>
    public void ClearRuntimeParameters()
    {
        _runtimeParameters = null;
    }

    /// <summary>
    /// グラフを実行します（後方互換性のためのオーバーロード）。
    /// </summary>
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(null, null, cancellationToken);
    }

    /// <summary>
    /// 共通パラメータを使用してグラフを実行します。
    /// </summary>
    /// <param name="commonParameterService">共通パラメータサービス（nullの場合はランタイムパラメータのみ使用）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task ExecuteAsync(
        CommonParameterService? commonParameterService,
        ExecutionHistoryService? historyService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsExecuting = true;

            // 既存の選択を全てクリア
            SelectionManager.ClearSelection();

            foreach (var editorNode in Nodes) editorNode.ExecutionStatus = ExecutionStatus.Waiting;

            // パラメータをマージ: runtimeParams > commonParams
            var commonParams = commonParameterService?.GetParameters();
            var mergedParams = ParameterMerger.Merge(commonParams, _runtimeParameters);
            var history = ExecutionHistory.Create(this);

            var executor = Graph.CreateExecutor();
            try
            {
                await executor.ExecuteAsync(
                    mergedParams,
                    x =>
                    {
                        var node = Nodes.FirstOrDefault(xx => xx.Node == x);
                        if (node == null) return;

                        Dispatcher.UIThread.Post(() =>
                        {
                            node.UpdatePortValues();
                            node.ExecutionStatus = ExecutionStatus.Executing;
                        });
                    },
                    x =>
                    {
                        history.Add(x);
                        var node = Nodes.FirstOrDefault(xx => xx.Node == x);
                        if (node == null) return;

                        Dispatcher.UIThread.Post(() =>
                        {
                            node.UpdatePortValues();
                            node.ExecutionStatus = ExecutionStatus.Executed;
                        });
                    },
                    (x, exception) =>
                    {
                        history.Add(x);
                        var node = Nodes.FirstOrDefault(xx => xx.Node == x);
                        if (node == null) return;

                        Dispatcher.UIThread.Post(() => { node.ExecutionStatus = ExecutionStatus.Exception; });
                    }, cancellationToken);
            }
            catch (Exception)
            {
                // ignored
            }

            historyService?.Add(history);
            await Task.Delay(5000, cancellationToken);
        }
        finally
        {
            foreach (var editorNode in Nodes) editorNode.ExecutionStatus = ExecutionStatus.None;
            IsExecuting = false;
        }
    }

    /// <summary>
    /// グラフとレイアウトを保存します
    /// </summary>
    public void Save(string filePath)
    {
        // .graph.yml と .layout.yml の両方を保存
        var graphPath = Path.ChangeExtension(filePath, ".graph.yml");
        var layoutPath = Path.ChangeExtension(filePath, ".layout.yml");

        GraphSerializer.SaveToYaml(Graph, graphPath);
        EditorLayoutSerializer.SaveLayoutToFile(this, layoutPath);

        CurrentFilePath = filePath;
    }

    /// <summary>
    /// 現在のファイルパスに上書き保存します
    /// </summary>
    public void Save()
    {
        if (string.IsNullOrEmpty(CurrentFilePath)) throw new InvalidOperationException("CurrentFilePath is not set. Use Save(string filePath) instead.");

        Save(CurrentFilePath);
    }

    /// <summary>
    /// グラフとレイアウトを読み込んで新しいEditorGraphを作成します
    /// </summary>
    public static EditorGraph Load(string filePath, SelectionManager selectionManager)
    {
        // .graph.yml と .layout.yml の両方を読み込む
        var graphPath = Path.ChangeExtension(filePath, ".graph.yml");
        var layoutPath = Path.ChangeExtension(filePath, ".layout.yml");

        var graph = GraphSerializer.LoadFromYaml(graphPath);
        var editorGraph = new EditorGraph(graph, selectionManager);

        // レイアウトファイルが存在する場合は読み込む
        if (File.Exists(layoutPath)) EditorLayoutSerializer.LoadLayoutFromFile(layoutPath, editorGraph);

        editorGraph.CurrentFilePath = filePath;
        return editorGraph;
    }

    public void AddNode(params ReadOnlySpan<EditorNode> editorNode)
    {
        using var _ = ListPool<EditorNode>.Shared.Rent(out var list);
        foreach (var node in editorNode)
            if (Graph.AddNode(node.Node))
            {
                Nodes.Add(node);
                list.Add(node);
            }

        LoadConnections(list);
    }

    /// <summary>
    /// ノードを削除します
    /// </summary>
    public void RemoveNode(EditorNode editorNode)
    {
        // 接続を削除
        var connectionsToRemove = Connections
            .Where(c => c.SourceNode == editorNode || c.TargetNode == editorNode)
            .ToList();

        foreach (var connection in connectionsToRemove) Connections.Remove(connection);

        // ノードを削除
        Nodes.Remove(editorNode);
        Graph.Nodes.Remove(editorNode.Node);
    }

    /// <summary>
    /// グラフ全体をクローンします
    /// </summary>
    public EditorGraph Clone()
    {
        return Clone(Nodes.ToArray());
    }

    /// <summary>
    /// 特定のEditorNodeのみをクローンして新しいEditorGraphを作成します
    /// 接続状態もクローン対象のノード内で完結しているものは複製されます
    /// </summary>
    public EditorGraph Clone(EditorNode[] editorNodes)
    {
        // Model層のNodeをクローン
        var nodes = editorNodes.Select(en => en.Node).ToArray();
        var clonedGraph = Graph.Clone(nodes);

        // 新しいEditorGraphを作成
        var clonedEditorGraph = new EditorGraph(clonedGraph, SelectionManager);

        // 位置情報をコピー（元のノードとクローンされたノードは同じ順序）
        for (var i = 0; i < editorNodes.Length && i < clonedEditorGraph.Nodes.Count; i++)
        {
            clonedEditorGraph.Nodes[i].X = editorNodes[i].X;
            clonedEditorGraph.Nodes[i].Y = editorNodes[i].Y;
        }

        return clonedEditorGraph;
    }
}