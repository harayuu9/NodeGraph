namespace NodeGraph.Model;

public interface IId
{
    Guid Value { get; }
}

public interface IWithId<out T>
    where T : IId
{
    T Id { get; }
}

public record struct PortId(Guid Value) : IId;
public record struct NodeId(Guid Value) : IId;

public class Port : IWithId<PortId>
{
    public Port(Node parent)
    {
        Parent = parent;
        Id = new PortId(Guid.NewGuid());
    }
    
    public PortId Id { get; } 
    public Node Parent { get; }
    
    public Port? ConnectedPort { get; set; }
}

public class Node : IWithId<NodeId>
{
    public NodeId Id { get; } = new(Guid.NewGuid());
    public List<Port> InputPorts { get; } = [];
    public List<Port> OutputPorts { get; } = [];
}

public class Graph
{
    public List<Node> Nodes { get; } = [];
}