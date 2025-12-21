using NodeGraph.Model;

namespace NodeGraph.UnitTest;

/// <summary>
/// 文字列結果を取得するテスト用ノード
/// </summary>
[Node(HasExecIn = false, HasExecOut = false)]
public partial class TestStringResultNode
{
    [Input] private string _value = string.Empty;
    public string Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 浮動小数点結果を取得するテスト用ノード
/// </summary>
[Node(HasExecIn = false, HasExecOut = false)]
public partial class TestFloatResultNode
{
    [Input] private float _value;
    public float Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 真偽値結果を取得するテスト用ノード
/// </summary>
[Node(HasExecIn = false, HasExecOut = false)]
public partial class TestBoolResultNode
{
    [Input] private bool _value;
    public bool Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}

public class ParameterTests
{
    [Fact]
    public void NodeExecutionContext_GetParameter_ReturnsValue()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["intValue"] = 42,
            ["stringValue"] = "hello",
            ["floatValue"] = 3.14f,
            ["boolValue"] = true
        };

        var context = new NodeExecutionContext(CancellationToken.None, parameters);

        Assert.Equal(42, context.GetParameter<int>("intValue"));
        Assert.Equal("hello", context.GetParameter<string>("stringValue"));
        Assert.Equal(3.14f, context.GetParameter<float>("floatValue"));
        Assert.True(context.GetParameter<bool>("boolValue"));
    }

    [Fact]
    public void NodeExecutionContext_GetParameter_ReturnsDefaultForMissing()
    {
        var parameters = new Dictionary<string, object?>();
        var context = new NodeExecutionContext(CancellationToken.None, parameters);

        Assert.Equal(0, context.GetParameter<int>("missing"));
        Assert.Null(context.GetParameter<string>("missing"));
        Assert.Equal(0f, context.GetParameter<float>("missing"));
        Assert.False(context.GetParameter<bool>("missing"));
    }

    [Fact]
    public void NodeExecutionContext_TryGetParameter_ReturnsTrueWhenExists()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["value"] = 42
        };
        var context = new NodeExecutionContext(CancellationToken.None, parameters);

        var result = context.TryGetParameter<int>("value", out var value);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void NodeExecutionContext_TryGetParameter_ReturnsFalseWhenMissing()
    {
        var parameters = new Dictionary<string, object?>();
        var context = new NodeExecutionContext(CancellationToken.None, parameters);

        var result = context.TryGetParameter<int>("missing", out var value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void NodeExecutionContext_HasParameter_ReturnsCorrectly()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["exists"] = 42
        };
        var context = new NodeExecutionContext(CancellationToken.None, parameters);

        Assert.True(context.HasParameter("exists"));
        Assert.False(context.HasParameter("missing"));
    }

    [Fact]
    public void NodeExecutionContext_GetParameter_ConvertsTypes()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["intAsString"] = "123",
            ["floatAsDouble"] = 3.14
        };
        var context = new NodeExecutionContext(CancellationToken.None, parameters);

        // string -> int変換
        Assert.Equal(123, context.GetParameter<int>("intAsString"));
        // double -> float変換
        Assert.Equal(3.14f, context.GetParameter<float>("floatAsDouble"), 0.01f);
    }

    [Fact]
    public async Task StringParameterNode_ReadsFromContext()
    {
        var graph = new Graph();

        var param = graph.CreateNode<StringParameterNode>();
        param.SetParameterName("greeting");

        var result = graph.CreateNode<TestStringResultNode>();
        result.ConnectInput(0, param, 0);

        var parameters = new Dictionary<string, object?>
        {
            ["greeting"] = "Hello, World!"
        };

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(parameters);

        Assert.Equal("Hello, World!", result.Value);
    }

    [Fact]
    public async Task IntParameterNode_ReadsFromContext()
    {
        var graph = new Graph();

        var param = graph.CreateNode<IntParameterNode>();
        param.SetParameterName("count");

        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, param, 0);

        var parameters = new Dictionary<string, object?>
        {
            ["count"] = 42
        };

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(parameters);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task IntParameterNode_UsesDefaultWhenMissing()
    {
        var graph = new Graph();

        var param = graph.CreateNode<IntParameterNode>();
        param.SetParameterName("count");
        param.SetDefaultValue(99);

        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, param, 0);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(); // パラメータなし

        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task FloatParameterNode_ReadsFromContext()
    {
        var graph = new Graph();

        var param = graph.CreateNode<FloatParameterNode>();
        param.SetParameterName("rate");

        var result = graph.CreateNode<TestFloatResultNode>();
        result.ConnectInput(0, param, 0);

        var parameters = new Dictionary<string, object?>
        {
            ["rate"] = 3.14f
        };

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(parameters);

        Assert.Equal(3.14f, result.Value, 0.001f);
    }

    [Fact]
    public async Task BoolParameterNode_ReadsFromContext()
    {
        var graph = new Graph();

        var param = graph.CreateNode<BoolParameterNode>();
        param.SetParameterName("enabled");

        var result = graph.CreateNode<TestBoolResultNode>();
        result.ConnectInput(0, param, 0);

        var parameters = new Dictionary<string, object?>
        {
            ["enabled"] = true
        };

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(parameters);

        Assert.True(result.Value);
    }

    [Fact]
    public async Task ParameterNode_EmptyName_KeepsDefaultValue()
    {
        var graph = new Graph();

        var param = graph.CreateNode<IntParameterNode>();
        // パラメータ名を設定しない
        param.SetDefaultValue(100);

        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, param, 0);

        var parameters = new Dictionary<string, object?>
        {
            ["anyName"] = 42
        };

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(parameters);

        // パラメータ名が空なのでデフォルト値が使用される
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task MultipleParameterNodes_ReadDifferentValues()
    {
        var graph = new Graph();

        var paramA = graph.CreateNode<IntParameterNode>();
        paramA.SetParameterName("a");

        var paramB = graph.CreateNode<IntParameterNode>();
        paramB.SetParameterName("b");

        var add = graph.CreateNode<AddNode>();
        add.ConnectInput(0, paramA, 0);
        add.ConnectInput(1, paramB, 0);

        var result = graph.CreateNode<ResultNode>();
        result.ConnectInput(0, add, 0);

        var parameters = new Dictionary<string, object?>
        {
            ["a"] = 100,
            ["b"] = 200
        };

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync(parameters);

        Assert.Equal(300, result.Value);
    }
}
