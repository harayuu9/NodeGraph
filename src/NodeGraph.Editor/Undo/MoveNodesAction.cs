using System.Linq;
using Avalonia;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Undo;

public class MoveNodesAction : IUndoableAction
{
    private readonly Point[] _newPositions;
    private readonly Point[] _previousPositions;
    private readonly EditorNode[] _targets;

    public MoveNodesAction(EditorNode[] targets, Point[] newPositions)
    {
        _targets = targets;
        _previousPositions = targets.Select(x => new Point(x.X, x.Y)).ToArray();
        _newPositions = newPositions;
    }

    public void Execute()
    {
        for (var i = 0; i < _targets.Length; i++)
        {
            _targets[i].X = _newPositions[i].X;
            _targets[i].Y = _newPositions[i].Y;
        }
    }

    public void Undo()
    {
        for (var i = 0; i < _targets.Length; i++)
        {
            _targets[i].X = _previousPositions[i].X;
            _targets[i].Y = _previousPositions[i].Y;
        }
    }
}