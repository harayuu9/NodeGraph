namespace NodeGraph.Model;

[Node("Random", "Math")]
public partial class RandomNode
{
#if NETSTANDARD2_1
    [ThreadStatic] private static Random? _random;
    private static Random SharedRandom => _random ??= new Random();
#endif

    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
#if NETSTANDARD2_1
        _result = (float)SharedRandom.NextDouble();
#else
        _result = Random.Shared.NextSingle();
#endif
        await context.ExecuteOutAsync(0);
    }
}

[Node("Random Range", "Math")]
public partial class RandomRangeNode
{
#if NETSTANDARD2_1
    [ThreadStatic] private static Random? _random;
    private static Random SharedRandom => _random ??= new Random();
#endif

    [Input] private float _min;
    [Input] private float _max = 1f;
    [Output] private float _result;

    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
#if NETSTANDARD2_1
        _result = _min + (float)SharedRandom.NextDouble() * (_max - _min);
#else
        _result = _min + Random.Shared.NextSingle() * (_max - _min);
#endif
        await context.ExecuteOutAsync(0);
    }
}
