namespace NodeGraph.Model;

public abstract class SingleConnectPort : Port
{
    protected SingleConnectPort(Node parent) : base(parent) {}
    protected SingleConnectPort(Node parent, PortId id) : base(parent, id) {}

    private Port? _connectedPort = null;
    public Port? ConnectedPort => _connectedPort;

    protected override void ConnectPort(Port other)
    {
        if (_connectedPort != null)
        {
            _connectedPort.Disconnect(this);
            Disconnect(_connectedPort);
        }
        
        _connectedPort = other;
    }

    public override void Disconnect(Port other)
    {
        if (_connectedPort != other)
        {
            return;
        }
        
        _connectedPort = null;
    }

    public override void DisconnectAll() => _connectedPort = null;
}