using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Serialization;
using NodeGraph.Model;
using NodeGraph.Model.Serialization;

namespace NodeGraph.Editor.Models;

public partial class EditorGraph : ObservableObject
{
    private readonly Graph _graph;

    public EditorGraph(Graph graph, SelectionManager selectionManager)
    {
        _graph = graph;
        SelectionManager = selectionManager;
        foreach (var graphNode in _graph.Nodes)
        {
            Nodes.Add(new EditorNode(selectionManager, graphNode));
        }

        // 既存の接続を読み込む
        LoadConnections();
    }

    public SelectionManager SelectionManager { get; }

    [ObservableProperty] public partial bool IsExecuting { get; set; }
    [ObservableProperty] public partial string? CurrentFilePath { get; set; }

    public ObservableCollection<EditorNode> Nodes { get; } = [];
    public ObservableCollection<EditorConnection> Connections { get; } = [];

    public Graph Graph => _graph;

    private void LoadConnections()
    {
        foreach (var editorNode in Nodes)
        {
            foreach (var editorPort in editorNode.OutputPorts)
            {
                var outputPort = (OutputPort)editorPort.Port;
                var connectedPorts = outputPort.ConnectedPorts;
                foreach (var inputPort in connectedPorts)
                {
                    // 接続先のEditorNodeとEditorPortを検索
                    var targetNode = Nodes.FirstOrDefault(n => n.InputPorts.Any(p => p.Port == inputPort));

                    if (targetNode == null) continue;

                    var targetPort = targetNode.InputPorts.FirstOrDefault(p => p.Port == inputPort);
                    if (targetPort == null) continue;

                    // 接続を作成
                    var connection = new EditorConnection(editorNode, editorPort, targetNode, targetPort);
                    Connections.Add(connection);
                }
            }
        }
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsExecuting = true;

            // 既存の選択を全てクリア
            SelectionManager.ClearSelection();

            foreach (var editorNode in Nodes)
            {
                editorNode.ExecutionStatus = ExecutionStatus.Waiting;
            }
        
            var executor = _graph.CreateExecutor();
            try
            {
                await executor.ExecuteAsync(
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
                        var node = Nodes.FirstOrDefault(xx => xx.Node == x);
                        if (node == null) return;

                        Dispatcher.UIThread.Post(() => { node.ExecutionStatus = ExecutionStatus.Exception; });
                    }, cancellationToken);
            }
            catch (Exception)
            {
                // ignored
            }

            await Task.Delay(5000, cancellationToken);
        }
        finally
        {
            foreach (var editorNode in Nodes)
            {
                editorNode.ExecutionStatus = ExecutionStatus.None;
            }
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

        GraphSerializer.SaveToYaml(_graph, graphPath);
        EditorLayoutSerializer.SaveLayout(this, layoutPath);

        CurrentFilePath = filePath;
    }

    /// <summary>
    /// 現在のファイルパスに上書き保存します
    /// </summary>
    public void Save()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            throw new InvalidOperationException("CurrentFilePath is not set. Use Save(string filePath) instead.");
        }

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
        if (File.Exists(layoutPath))
        {
            EditorLayoutSerializer.LoadLayout(layoutPath, editorGraph);
        }

        editorGraph.CurrentFilePath = filePath;
        return editorGraph;
    }
}