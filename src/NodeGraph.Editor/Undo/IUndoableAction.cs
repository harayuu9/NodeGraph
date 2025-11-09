namespace NodeGraph.Editor.Undo;

public interface IUndoableAction
{
    void Execute();
    void Undo();
}