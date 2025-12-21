using System.Collections.Generic;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

/// <summary>
/// ノード整列操作のUndo/Redoアクション
/// </summary>
public class ArrangeNodesAction : IUndoableAction
{
    private readonly List<(EditorNode Node, double OldX, double OldY, double NewX, double NewY)> _nodePositions;

    public ArrangeNodesAction(List<(EditorNode Node, double OldX, double OldY, double NewX, double NewY)> nodePositions)
    {
        _nodePositions = nodePositions;
    }

    public void Execute()
    {
        foreach (var (node, _, _, newX, newY) in _nodePositions)
        {
            node.X = newX;
            node.Y = newY;
        }
    }

    public void Undo()
    {
        foreach (var (node, oldX, oldY, _, _) in _nodePositions)
        {
            node.X = oldX;
            node.Y = oldY;
        }
    }
}