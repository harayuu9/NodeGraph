namespace NodeGraph.Model;

public class InputPort<T> : InputPort
{
    public InputPort(Node parent, T value) : base(parent)
    {
        Value = value;
    }

    public InputPort(Node parent, PortId id, T value) : base(parent, id)
    {
        Value = value;
    }
    
    public T Value { get; set; }
    public override bool CanConnect(Port other)
    {
        if (Parent == other.Parent) return false;
        return other is OutputPort<T>;
    }
}

public abstract class InputPort : SingleConnectPort
{
    protected InputPort(Node parent) : base(parent) { }
    protected InputPort(Node parent, PortId id) : base(parent, id) {}
}