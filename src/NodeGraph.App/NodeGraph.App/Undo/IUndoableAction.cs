namespace NodeGraph.App.Undo;

public interface IUndoableAction
{
    void Execute();
    void Undo();
}