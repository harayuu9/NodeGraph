namespace NodeGraph.Model;

public class InputPort<T> : InputPort
{
    public InputPort(Node parent, T value) : base(parent)
    {
        Value = value;
    }
    
    public T Value { get; set; }
    public OutputPort<T>? ConnectedPortRaw { get; set; }
    internal override OutputPort? ConnectedPort => ConnectedPortRaw;
    public override bool CanConnect(OutputPort other) => other is OutputPort<T>;
    public override void Connect(OutputPort other)
    {
        Disconnect();
        
        var x = (OutputPort<T>)other;
        ConnectedPortRaw = x;
        x.ConnectedPortsRaw.Add(this);
    }

    public override void Disconnect()
    {
        ConnectedPort?.Disconnect();
    }
}

public abstract class InputPort : IWithId<PortId>
{
    protected InputPort(Node parent)
    {
        Parent = parent;
        Id = new PortId(Guid.NewGuid());
    }

    public PortId Id { get; }
    public Node Parent { get; }

    internal abstract OutputPort? ConnectedPort { get; }
    public abstract bool CanConnect(OutputPort other);
    public abstract void Connect(OutputPort other);
    public abstract void Disconnect();
}