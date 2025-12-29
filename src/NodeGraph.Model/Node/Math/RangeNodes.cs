namespace NodeGraph.Model;

[Node("Abs", "Math")]
public partial class AbsNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Abs(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Min", "Math")]
public partial class MinNode
{
    [Input] private float _a;
    [Input] private float _b;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Min(_a, _b);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Max", "Math")]
public partial class MaxNode
{
    [Input] private float _a;
    [Input] private float _b;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Max(_a, _b);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Clamp", "Math")]
public partial class ClampNode
{
    [Input] private float _value;
    [Input] private float _min;
    [Input] private float _max = 1f;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = Math.Clamp(_value, _min, _max);
        await context.ExecuteOutAsync(0);
    }
}
