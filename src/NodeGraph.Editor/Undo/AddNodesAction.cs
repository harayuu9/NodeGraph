using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

/// <summary>
/// 複数ノード追加操作のUndo/Redoアクション（Paste、Duplicate用）
/// </summary>
public class AddNodesAction : IUndoableAction
{
    private readonly EditorGraph _graph;
    private readonly EditorNode[] _nodes;

    public AddNodesAction(EditorGraph graph, EditorNode[] nodes)
    {
        _graph = graph;
        _nodes = nodes;
    }

    public void Execute()
    {
        _graph.AddNode(_nodes);
    }

    public void Undo()
    {
        // ノードを削除（接続も削除される）
        foreach (var node in _nodes) _graph.RemoveNode(node);
    }
}