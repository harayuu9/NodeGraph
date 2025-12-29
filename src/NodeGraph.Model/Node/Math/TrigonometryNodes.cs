namespace NodeGraph.Model;

[Node("Sin", "Math")]
public partial class SinNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Sin(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Cos", "Math")]
public partial class CosNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Cos(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Tan", "Math")]
public partial class TanNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Tan(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Asin", "Math")]
public partial class AsinNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Asin(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Acos", "Math")]
public partial class AcosNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Acos(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Atan2", "Math")]
public partial class Atan2Node
{
    [Input] private float _y;
    [Input] private float _x;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Atan2(_y, _x);
        await context.ExecuteOutAsync(0);
    }
}
