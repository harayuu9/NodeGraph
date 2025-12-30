using System.Collections.Generic;

namespace NodeGraph.App.Undo;

/// <summary>
/// 複数のアクションを一つのアクションとしてまとめるクラス
/// </summary>
public class CompositeUndoableAction : IUndoableAction
{
    private readonly List<IUndoableAction> _actions = new();

    public bool HasActions => _actions.Count > 0;

    public void Execute()
    {
        foreach (var action in _actions) action.Execute();
    }

    public void Undo()
    {
        // 逆順でUndoを実行
        for (var i = _actions.Count - 1; i >= 0; i--) _actions[i].Undo();
    }

    public void AddAction(IUndoableAction action)
    {
        _actions.Add(action);
    }
}