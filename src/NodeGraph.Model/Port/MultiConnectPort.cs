namespace NodeGraph.Model;

public abstract class MultiConnectPort : Port
{
    protected MultiConnectPort(Node parent) : base(parent) {}
    protected MultiConnectPort(Node parent, PortId id) : base(parent, id) {}
    
    private readonly List<Port> _connectedPorts = [];
    public IReadOnlyList<Port> ConnectedPorts => _connectedPorts;
    
    protected override void ConnectPort(Port other)
    {
        _connectedPorts.Add(other);
    }

    public override void Disconnect(Port other)
    {
        _connectedPorts.Remove(other);
    }
    public override void DisconnectAll() => _connectedPorts.Clear();
}