namespace NodeGraph.Model;

public class OutputPort<T> : OutputPort
{
    public OutputPort(Node parent, T value) : base(parent)
    {
        Value = value;
    }

    public OutputPort(Node parent, PortId id, T value) : base(parent, id)
    {
        Value = value;
    }

    public T Value
    {
        set
        {
            foreach (var port1 in ConnectedPorts)
            {
                var port = (InputPort<T>)port1;
                port.Value = value;
            }
        }
    }

    public override string ValueString => "None";

    public override bool CanConnect(Port other)
    {
        if (Parent == other.Parent) return false;
        return other is InputPort<T>;
    }
}

public abstract class OutputPort : MultiConnectPort
{
    protected OutputPort(Node parent) : base(parent) { }
    protected OutputPort(Node parent, PortId id) : base(parent, id) {}

}