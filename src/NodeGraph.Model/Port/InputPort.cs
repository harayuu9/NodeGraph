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
    public override Type PortType => typeof(T);
    public override string ValueString => Value?.ToString() ?? "None";

    public override bool CanConnect(Port other)
    {
        if (Parent == other.Parent) return false;
        if (other is not OutputPort) return false;

        // Check if the output port's type can be converted to this input port's type
        return PortTypeConverterProvider.CanConvert(other.PortType, typeof(T));
    }

    internal override void SetValueFrom<TSource>(TSource value)
    {
        // If types match exactly, no conversion needed
        if (typeof(TSource) == typeof(T) && value is T typedValue)
        {
            Value = typedValue;
            return;
        }

        if (value == null)
        {
            Value = default!;
            return;
        }

        var converter = PortTypeConverterProvider.GetConverter<TSource, T>();
        if (converter != null)
        {
            Value = converter.Convert(value);
            return;
        }

        if (value is T directCast)
        {
            Value = directCast;
            return;
        }

        throw new InvalidOperationException($"Cannot set value of type {typeof(TSource).Name} to InputPort<{typeof(T).Name}>");
    }
}

public abstract class InputPort : SingleConnectPort
{
    protected InputPort(Node parent) : base(parent)
    {
    }

    protected InputPort(Node parent, PortId id) : base(parent, id)
    {
    }

    internal abstract void SetValueFrom<TSource>(TSource value);
}