namespace NodeGraph.Model;

public abstract class Port : IWithId<PortId>
{
    protected Port(Node parent)
    {
        Parent = parent;
        Id = new PortId(Guid.NewGuid());
    }
    protected Port(Node parent, PortId id)
    {
        Parent = parent;
        Id = id;
    }

    public PortId Id { get; }
    public Node Parent { get; }
    public abstract Type PortType { get; }
    public abstract string ValueString { get; }
    protected abstract void ConnectPort(Port other);
    
    public abstract bool CanConnect(Port port);
    public abstract void Disconnect(Port port);
    public abstract void DisconnectAll();

    public bool Connect(Port other)
    {
        if (CanConnect(other) && other.CanConnect(this))
        {
            ConnectPort(other);
            other.ConnectPort(this);
            return true;
        }

        return false;
    }
}