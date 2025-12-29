namespace NodeGraph.Model;

[Node("Pow", "Math")]
public partial class PowNode
{
    [Input] private float _base = 1f;
    [Input] private float _exponent = 1f;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Pow(_base, _exponent);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Sqrt", "Math")]
public partial class SqrtNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Sqrt(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Exp", "Math")]
public partial class ExpNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Exp(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Log", "Math")]
public partial class LogNode
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Log(_value);
        await context.ExecuteOutAsync(0);
    }
}

[Node("Log10", "Math")]
public partial class Log10Node
{
    [Input] private float _value;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        _result = MathF.Log10(_value);
        await context.ExecuteOutAsync(0);
    }
}
