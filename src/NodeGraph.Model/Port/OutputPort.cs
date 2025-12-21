namespace NodeGraph.Model;

public class OutputPort<T> : OutputPort
{
    private T _value;

    public OutputPort(Node parent, T value) : base(parent)
    {
        _value = value;
    }

    public OutputPort(Node parent, PortId id, T value) : base(parent, id)
    {
        _value = value;
    }

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            foreach (var port in ConnectedPorts)
            {
                if (port is not InputPort inputPort)
                    continue;

                inputPort.SetValueFrom(value);
            }
        }
    }

    public override Type PortType => typeof(T);
    public override string ValueString => "None";

    public override bool CanConnect(Port other)
    {
        if (Parent == other.Parent) return false;
        if (other is not InputPort) return false;

        // Check if this output port's type can be converted to the input port's type
        return PortTypeConverterProvider.CanConvert(typeof(T), other.PortType);
    }
}

public abstract class OutputPort : MultiConnectPort
{
    protected OutputPort(Node parent) : base(parent)
    {
    }

    protected OutputPort(Node parent, PortId id) : base(parent, id)
    {
    }
}