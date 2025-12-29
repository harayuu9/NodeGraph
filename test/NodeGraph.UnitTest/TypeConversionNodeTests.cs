using NodeGraph.Model;

namespace NodeGraph.UnitTest;

/// <summary>
/// 型変換ノードのテスト
/// </summary>
public class TypeConversionNodeTests
{
    #region IntToStringNode Tests

    [Theory]
    [InlineData(42, "", "42")]
    [InlineData(42, "D5", "00042")]
    [InlineData(255, "X", "FF")]
    [InlineData(1234, "N0", "1,234")]
    public async Task IntToStringNode_FormatsCorrectly(int input, string format, string expected)
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<IntConstantNode>();
        inputNode.SetValue(input);

        var convert = graph.CreateNode<IntToStringNode>();
        convert.SetFormat(format);
        convert.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(convert.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)convert.OutputPorts[0]).Value;
        Assert.Equal(expected, result);
    }

    #endregion

    #region FloatToStringNode Tests

    [Fact]
    public async Task FloatToStringNode_FormatsWithPrecision()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<FloatConstantNode>();
        inputNode.SetValue(3.14159f);

        var convert = graph.CreateNode<FloatToStringNode>();
        convert.SetFormat("F2");
        convert.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(convert.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)convert.OutputPorts[0]).Value;
        Assert.Equal("3.14", result);
    }

    [Fact]
    public async Task FloatToStringNode_FormatsWithNoDecimal()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<FloatConstantNode>();
        inputNode.SetValue(3.99f);

        var convert = graph.CreateNode<FloatToStringNode>();
        convert.SetFormat("F0");
        convert.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(convert.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)convert.OutputPorts[0]).Value;
        Assert.Equal("4", result);
    }

    #endregion

    #region StringToIntNode Tests

    [Theory]
    [InlineData("42", 0, 42, true)]
    [InlineData("invalid", 0, 0, false)]
    [InlineData("invalid", 99, 99, false)]
    [InlineData("-123", 0, -123, true)]
    public async Task StringToIntNode_ParsesOrReturnsDefault(string input, int defaultValue, int expected, bool expectedSuccess)
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue(input);

        var convert = graph.CreateNode<StringToIntNode>();
        convert.SetDefaultValue(defaultValue);
        convert.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(convert.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<int>)convert.OutputPorts[0]).Value;
        var successResult = ((OutputPort<bool>)convert.OutputPorts[1]).Value;

        Assert.Equal(expected, result);
        Assert.Equal(expectedSuccess, successResult);
    }

    #endregion

    #region StringToFloatNode Tests

    [Fact]
    public async Task StringToFloatNode_ParsesValidFloat()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("3.14");

        var convert = graph.CreateNode<StringToFloatNode>();
        convert.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(convert.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<float>)convert.OutputPorts[0]).Value;
        var successResult = ((OutputPort<bool>)convert.OutputPorts[1]).Value;

        Assert.Equal(3.14f, result, 0.001f);
        Assert.True(successResult);
    }

    [Fact]
    public async Task StringToFloatNode_ReturnsDefaultForInvalid()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("not a number");

        var convert = graph.CreateNode<StringToFloatNode>();
        convert.SetDefaultValue(1.5f);
        convert.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(convert.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<float>)convert.OutputPorts[0]).Value;
        var successResult = ((OutputPort<bool>)convert.OutputPorts[1]).Value;

        Assert.Equal(1.5f, result, 0.001f);
        Assert.False(successResult);
    }

    #endregion
}
