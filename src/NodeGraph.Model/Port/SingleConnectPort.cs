namespace NodeGraph.Model;

public abstract class SingleConnectPort : Port
{
    protected SingleConnectPort(Node parent) : base(parent)
    {
    }

    protected SingleConnectPort(Node parent, PortId id) : base(parent, id)
    {
    }

    public Port? ConnectedPort { get; private set; }

    protected override void ConnectPort(Port other)
    {
        if (ConnectedPort != null)
        {
            ConnectedPort.Disconnect(this);
            Disconnect(ConnectedPort);
        }

        ConnectedPort = other;
    }

    public override void Disconnect(Port other)
    {
        if (ConnectedPort != other) return;

        ConnectedPort = null;
    }

    public override void DisconnectAll()
    {
        ConnectedPort = null;
    }
}