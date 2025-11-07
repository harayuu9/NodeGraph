namespace NodeGraph.Model;

[Node]
public partial class FloatResultNode
{
    [Input] private float _value;
    public float Value => _value;
}

[Node]
public partial class PreviewNode
{
    [Input] private object? _value;
}