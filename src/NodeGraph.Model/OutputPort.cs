namespace NodeGraph.Model;

public class OutputPort<T> : OutputPort
{
    public OutputPort(Node parent, T value) : base(parent)
    {
        Value = value;
    }

    public T Value
    {
        set
        {
            foreach (var port in ConnectedPortsRaw)
            {
                port.Value = value;
            }
        }
    }

    public List<InputPort<T>> ConnectedPortsRaw { get; } = [];
    internal override IReadOnlyList<InputPort> ConnectedPorts => ConnectedPortsRaw;
    public override bool CanConnect(InputPort other) => other is InputPort<T>;
    public override void Connect(InputPort other)
    {
        other.Disconnect();
        
        var x = (InputPort<T>)other;
        ConnectedPortsRaw.Add(x);
        x.ConnectedPortRaw = this;
    }

    public override void Disconnect()
    {
        foreach (var x in ConnectedPortsRaw)
        {
            x.ConnectedPortRaw = null;
        }
        ConnectedPortsRaw.Clear();  
    }

    public override void Disconnect(InputPort inputPort)
    {
        var x = (InputPort<T>)inputPort;
        x.ConnectedPortRaw = null;
        ConnectedPortsRaw.Remove(x);
    }
}

public abstract class OutputPort : IWithId<PortId>
{
    protected OutputPort(Node parent)
    {
        Parent = parent;
        Id = new PortId(Guid.NewGuid());
    }

    public PortId Id { get; }
    public Node Parent { get; }

    internal abstract IReadOnlyList<InputPort> ConnectedPorts { get; }
    public abstract bool CanConnect(InputPort other);
    public abstract void Connect(InputPort other);
    public abstract void Disconnect();
    public abstract void Disconnect(InputPort inputPort);
}