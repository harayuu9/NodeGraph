namespace NodeGraph.Model;

[Node("Floor", "Math")]
public partial class FloorNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Floor(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Ceil", "Math")]
public partial class CeilNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Ceiling(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Round", "Math")]
public partial class RoundNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Round(_value);
        await context.ExecuteOutAsync(0);
    }
}
