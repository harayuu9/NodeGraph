namespace NodeGraph.Model;

public interface IWithId<out T>
    where T : IId
{
    T Id { get; }
}