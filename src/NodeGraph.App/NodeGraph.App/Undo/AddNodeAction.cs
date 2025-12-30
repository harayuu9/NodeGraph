using NodeGraph.App.Models;

namespace NodeGraph.App.Undo;

/// <summary>
/// ノード追加操作のUndo/Redoアクション
/// </summary>
public class AddNodeAction : IUndoableAction
{
    private readonly EditorGraph _graph;
    private readonly EditorNode _node;

    public AddNodeAction(EditorGraph graph, EditorNode node)
    {
        _graph = graph;
        _node = node;
    }

    public void Execute()
    {
        // ノードを追加
        _graph.Graph.AddNode(_node.Node);
        _graph.Nodes.Add(_node);
    }

    public void Undo()
    {
        // ノードを削除（接続も削除される）
        _graph.RemoveNode(_node);
    }
}