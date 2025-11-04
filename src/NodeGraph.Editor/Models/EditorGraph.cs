using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

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
}