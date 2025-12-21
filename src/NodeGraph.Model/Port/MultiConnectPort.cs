namespace NodeGraph.Model;

public abstract class MultiConnectPort : Port
{
    private readonly List<Port> _connectedPorts = [];

    protected MultiConnectPort(Node parent) : base(parent)
    {
    }

    protected MultiConnectPort(Node parent, PortId id) : base(parent, id)
    {
    }

    public IReadOnlyList<Port> ConnectedPorts => _connectedPorts;

    protected override void ConnectPort(Port other)
    {
        if (_connectedPorts.Contains(other)) return;
        _connectedPorts.Add(other);
    }

    public override void Disconnect(Port other)
    {
        _connectedPorts.Remove(other);
    }

    public override void DisconnectAll()
    {
        _connectedPorts.Clear();
    }
}