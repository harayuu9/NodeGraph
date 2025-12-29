using NodeGraph.Model;

namespace NodeGraph.UnitTest.Model;

public class MathNodesTest
{
    #region Trigonometry Tests

    [Fact]
    public async Task SinNode_Should_Calculate_Sin()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(MathF.PI / 2);

        var sin = graph.CreateNode<SinNode>();
        sin.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, sin, 0);

        // Exec flow: Start → Sin → Result
        start.ExecOutPorts[0].Connect(sin.ExecInPorts[0]);
        sin.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(1.0f, result.Value, 5);
    }

    [Fact]
    public async Task CosNode_Should_Calculate_Cos()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(0f);

        var cos = graph.CreateNode<CosNode>();
        cos.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, cos, 0);

        start.ExecOutPorts[0].Connect(cos.ExecInPorts[0]);
        cos.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(1.0f, result.Value, 5);
    }

    [Fact]
    public async Task TanNode_Should_Calculate_Tan()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(0f);

        var tan = graph.CreateNode<TanNode>();
        tan.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, tan, 0);

        start.ExecOutPorts[0].Connect(tan.ExecInPorts[0]);
        tan.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(0f, result.Value, 5);
    }

    [Fact]
    public async Task AsinNode_Should_Calculate_Asin()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(1f);

        var asin = graph.CreateNode<AsinNode>();
        asin.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, asin, 0);

        start.ExecOutPorts[0].Connect(asin.ExecInPorts[0]);
        asin.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(MathF.PI / 2, result.Value, 5);
    }

    [Fact]
    public async Task AcosNode_Should_Calculate_Acos()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(1f);

        var acos = graph.CreateNode<AcosNode>();
        acos.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, acos, 0);

        start.ExecOutPorts[0].Connect(acos.ExecInPorts[0]);
        acos.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(0f, result.Value, 5);
    }

    [Fact]
    public async Task Atan2Node_Should_Calculate_Atan2()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var inputY = graph.CreateNode<FloatConstantNode>();
        inputY.SetValue(1f);
        var inputX = graph.CreateNode<FloatConstantNode>();
        inputX.SetValue(1f);

        var atan2 = graph.CreateNode<Atan2Node>();
        atan2.ConnectInput(0, inputY, 0);
        atan2.ConnectInput(1, inputX, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, atan2, 0);

        start.ExecOutPorts[0].Connect(atan2.ExecInPorts[0]);
        atan2.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(MathF.PI / 4, result.Value, 5);
    }

    #endregion

    #region Exponential Tests

    [Fact]
    public async Task PowNode_Should_Calculate_Power()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var inputBase = graph.CreateNode<FloatConstantNode>();
        inputBase.SetValue(2f);
        var inputExp = graph.CreateNode<FloatConstantNode>();
        inputExp.SetValue(3f);

        var pow = graph.CreateNode<PowNode>();
        pow.ConnectInput(0, inputBase, 0);
        pow.ConnectInput(1, inputExp, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, pow, 0);

        start.ExecOutPorts[0].Connect(pow.ExecInPorts[0]);
        pow.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(8f, result.Value, 5);
    }

    [Fact]
    public async Task SqrtNode_Should_Calculate_SquareRoot()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(4f);

        var sqrt = graph.CreateNode<SqrtNode>();
        sqrt.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, sqrt, 0);

        start.ExecOutPorts[0].Connect(sqrt.ExecInPorts[0]);
        sqrt.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(2f, result.Value, 5);
    }

    [Fact]
    public async Task ExpNode_Should_Calculate_Exp()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(0f);

        var exp = graph.CreateNode<ExpNode>();
        exp.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, exp, 0);

        start.ExecOutPorts[0].Connect(exp.ExecInPorts[0]);
        exp.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(1f, result.Value, 5);
    }

    [Fact]
    public async Task LogNode_Should_Calculate_NaturalLog()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(MathF.E);

        var log = graph.CreateNode<LogNode>();
        log.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, log, 0);

        start.ExecOutPorts[0].Connect(log.ExecInPorts[0]);
        log.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(1f, result.Value, 5);
    }

    [Fact]
    public async Task Log10Node_Should_Calculate_Log10()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(100f);

        var log10 = graph.CreateNode<Log10Node>();
        log10.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, log10, 0);

        start.ExecOutPorts[0].Connect(log10.ExecInPorts[0]);
        log10.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(2f, result.Value, 5);
    }

    #endregion

    #region Range Tests

    [Fact]
    public async Task AbsNode_Should_Calculate_AbsoluteValue()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(-5f);

        var abs = graph.CreateNode<AbsNode>();
        abs.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, abs, 0);

        start.ExecOutPorts[0].Connect(abs.ExecInPorts[0]);
        abs.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(5f, result.Value, 5);
    }

    [Fact]
    public async Task MinNode_Should_Return_Minimum()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var inputA = graph.CreateNode<FloatConstantNode>();
        inputA.SetValue(3f);
        var inputB = graph.CreateNode<FloatConstantNode>();
        inputB.SetValue(7f);

        var min = graph.CreateNode<MinNode>();
        min.ConnectInput(0, inputA, 0);
        min.ConnectInput(1, inputB, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, min, 0);

        start.ExecOutPorts[0].Connect(min.ExecInPorts[0]);
        min.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(3f, result.Value, 5);
    }

    [Fact]
    public async Task MaxNode_Should_Return_Maximum()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var inputA = graph.CreateNode<FloatConstantNode>();
        inputA.SetValue(3f);
        var inputB = graph.CreateNode<FloatConstantNode>();
        inputB.SetValue(7f);

        var max = graph.CreateNode<MaxNode>();
        max.ConnectInput(0, inputA, 0);
        max.ConnectInput(1, inputB, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, max, 0);

        start.ExecOutPorts[0].Connect(max.ExecInPorts[0]);
        max.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(7f, result.Value, 5);
    }

    [Fact]
    public async Task ClampNode_Should_Clamp_Value()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var inputValue = graph.CreateNode<FloatConstantNode>();
        inputValue.SetValue(5f);
        var inputMin = graph.CreateNode<FloatConstantNode>();
        inputMin.SetValue(0f);
        var inputMax = graph.CreateNode<FloatConstantNode>();
        inputMax.SetValue(3f);

        var clamp = graph.CreateNode<ClampNode>();
        clamp.ConnectInput(0, inputValue, 0);
        clamp.ConnectInput(1, inputMin, 0);
        clamp.ConnectInput(2, inputMax, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, clamp, 0);

        start.ExecOutPorts[0].Connect(clamp.ExecInPorts[0]);
        clamp.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(3f, result.Value, 5);
    }

    #endregion

    #region Rounding Tests

    [Fact]
    public async Task FloorNode_Should_Floor_Value()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(2.7f);

        var floor = graph.CreateNode<FloorNode>();
        floor.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, floor, 0);

        start.ExecOutPorts[0].Connect(floor.ExecInPorts[0]);
        floor.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(2f, result.Value, 5);
    }

    [Fact]
    public async Task CeilNode_Should_Ceil_Value()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(2.3f);

        var ceil = graph.CreateNode<CeilNode>();
        ceil.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, ceil, 0);

        start.ExecOutPorts[0].Connect(ceil.ExecInPorts[0]);
        ceil.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(3f, result.Value, 5);
    }

    [Fact]
    public async Task RoundNode_Should_Round_Value()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<FloatConstantNode>();
        input.SetValue(2.5f);

        var round = graph.CreateNode<RoundNode>();
        round.ConnectInput(0, input, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, round, 0);

        start.ExecOutPorts[0].Connect(round.ExecInPorts[0]);
        round.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.Equal(2f, result.Value, 5); // MathF.Round uses banker's rounding
    }

    #endregion

    #region Random Tests

    [Fact]
    public async Task RandomNode_Should_Return_Value_Between_0_And_1()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var random = graph.CreateNode<RandomNode>();

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, random, 0);

        start.ExecOutPorts[0].Connect(random.ExecInPorts[0]);
        random.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.InRange(result.Value, 0f, 1f);
    }

    [Fact]
    public async Task RandomRangeNode_Should_Return_Value_In_Range()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var inputMin = graph.CreateNode<FloatConstantNode>();
        inputMin.SetValue(10f);
        var inputMax = graph.CreateNode<FloatConstantNode>();
        inputMax.SetValue(20f);

        var randomRange = graph.CreateNode<RandomRangeNode>();
        randomRange.ConnectInput(0, inputMin, 0);
        randomRange.ConnectInput(1, inputMax, 0);

        var result = graph.CreateNode<FloatResultNode>();
        result.ConnectInput(0, randomRange, 0);

        start.ExecOutPorts[0].Connect(randomRange.ExecInPorts[0]);
        randomRange.ExecOutPorts[0].Connect(result.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        Assert.InRange(result.Value, 10f, 20f);
    }

    #endregion
}
