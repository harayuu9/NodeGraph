using NodeGraph.Model;

namespace NodeGraph.UnitTest;

/// <summary>
/// 整数結果を取得するテスト用ノード
/// </summary>
[Node(HasExecIn = false, HasExecOut = false)]
public partial class TestIntResultNode
{
    [Input] private int _value;
    public int Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 文字列操作ノードのテスト
/// </summary>
public class StringNodeTests
{
    #region StringConcatNode Tests

    [Fact]
    public async Task StringConcatNode_ConcatenatesTwoStrings()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var a = graph.CreateNode<StringConstantNode>();
        a.SetValue("Hello, ");
        var b = graph.CreateNode<StringConstantNode>();
        b.SetValue("World!");

        var concat = graph.CreateNode<StringConcatNode>();
        concat.ConnectInput(0, a, 0);
        concat.ConnectInput(1, b, 0);

        start.ExecOutPorts[0].Connect(concat.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)concat.OutputPorts[0]).Value;
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task StringConcatNode_HandlesEmptyStrings()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var a = graph.CreateNode<StringConstantNode>();
        a.SetValue("Test");

        var concat = graph.CreateNode<StringConcatNode>();
        concat.ConnectInput(0, a, 0);
        // b is not connected, defaults to empty

        start.ExecOutPorts[0].Connect(concat.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)concat.OutputPorts[0]).Value;
        Assert.Equal("Test", result);
    }

    #endregion

    #region StringLengthNode Tests

    [Fact]
    public async Task StringLengthNode_ReturnsCorrectLength()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<StringConstantNode>();
        input.SetValue("Hello");

        var length = graph.CreateNode<StringLengthNode>();
        length.ConnectInput(0, input, 0);

        start.ExecOutPorts[0].Connect(length.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<int>)length.OutputPorts[0]).Value;
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task StringLengthNode_ReturnsZeroForEmpty()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<StringConstantNode>();
        input.SetValue("");

        var length = graph.CreateNode<StringLengthNode>();
        length.ConnectInput(0, input, 0);

        start.ExecOutPorts[0].Connect(length.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var lengthResult = ((OutputPort<int>)length.OutputPorts[0]).Value;
        var isEmptyResult = ((OutputPort<bool>)length.OutputPorts[1]).Value;
        Assert.Equal(0, lengthResult);
        Assert.True(isEmptyResult);
    }

    #endregion

    #region ToUpperNode Tests

    [Fact]
    public async Task ToUpperNode_ConvertsToUppercase()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<StringConstantNode>();
        input.SetValue("hello world");

        var upper = graph.CreateNode<ToUpperNode>();
        upper.ConnectInput(0, input, 0);

        start.ExecOutPorts[0].Connect(upper.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)upper.OutputPorts[0]).Value;
        Assert.Equal("HELLO WORLD", result);
    }

    #endregion

    #region ToLowerNode Tests

    [Fact]
    public async Task ToLowerNode_ConvertsToLowercase()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<StringConstantNode>();
        input.SetValue("HELLO WORLD");

        var lower = graph.CreateNode<ToLowerNode>();
        lower.ConnectInput(0, input, 0);

        start.ExecOutPorts[0].Connect(lower.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)lower.OutputPorts[0]).Value;
        Assert.Equal("hello world", result);
    }

    #endregion

    #region TrimNode Tests

    [Theory]
    [InlineData("  hello  ", TrimMode.Both, "hello")]
    [InlineData("  hello  ", TrimMode.Start, "hello  ")]
    [InlineData("  hello  ", TrimMode.End, "  hello")]
    public async Task TrimNode_RemovesWhitespace(string input, TrimMode mode, string expected)
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue(input);

        var trim = graph.CreateNode<TrimNode>();
        trim.SetMode(mode);
        trim.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(trim.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)trim.OutputPorts[0]).Value;
        Assert.Equal(expected, result);
    }

    #endregion

    #region StringContainsNode Tests

    [Theory]
    [InlineData("Hello World", "World", false, true)]
    [InlineData("Hello World", "world", false, false)]
    [InlineData("Hello World", "world", true, true)]
    [InlineData("Hello World", "", false, true)]
    public async Task StringContainsNode_FindsSubstring(string input, string search, bool ignoreCase, bool expected)
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue(input);
        var searchNode = graph.CreateNode<StringConstantNode>();
        searchNode.SetValue(search);

        var contains = graph.CreateNode<StringContainsNode>();
        contains.SetIgnoreCase(ignoreCase);
        contains.ConnectInput(0, inputNode, 0);
        contains.ConnectInput(1, searchNode, 0);

        start.ExecOutPorts[0].Connect(contains.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<bool>)contains.OutputPorts[0]).Value;
        Assert.Equal(expected, result);
    }

    #endregion

    #region StringIndexOfNode Tests

    [Fact]
    public async Task StringIndexOfNode_FindsPosition()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("Hello World");
        var searchNode = graph.CreateNode<StringConstantNode>();
        searchNode.SetValue("World");

        var indexOf = graph.CreateNode<StringIndexOfNode>();
        indexOf.ConnectInput(0, inputNode, 0);
        indexOf.ConnectInput(1, searchNode, 0);

        start.ExecOutPorts[0].Connect(indexOf.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var indexResult = ((OutputPort<int>)indexOf.OutputPorts[0]).Value;
        var foundResult = ((OutputPort<bool>)indexOf.OutputPorts[1]).Value;
        Assert.Equal(6, indexResult);
        Assert.True(foundResult);
    }

    [Fact]
    public async Task StringIndexOfNode_ReturnsMinusOneWhenNotFound()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("Hello World");
        var searchNode = graph.CreateNode<StringConstantNode>();
        searchNode.SetValue("xyz");

        var indexOf = graph.CreateNode<StringIndexOfNode>();
        indexOf.ConnectInput(0, inputNode, 0);
        indexOf.ConnectInput(1, searchNode, 0);

        start.ExecOutPorts[0].Connect(indexOf.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var indexResult = ((OutputPort<int>)indexOf.OutputPorts[0]).Value;
        var foundResult = ((OutputPort<bool>)indexOf.OutputPorts[1]).Value;
        Assert.Equal(-1, indexResult);
        Assert.False(foundResult);
    }

    #endregion

    #region SubstringNode Tests

    [Theory]
    [InlineData("Hello World", 0, 5, "Hello")]
    [InlineData("Hello World", 6, -1, "World")]
    [InlineData("Hello World", 6, 100, "World")]
    [InlineData("Hello World", 100, 5, "")]
    public async Task SubstringNode_ExtractsCorrectly(string input, int startIndex, int length, string expected)
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue(input);
        var startIndexNode = graph.CreateNode<IntConstantNode>();
        startIndexNode.SetValue(startIndex);
        var lengthNode = graph.CreateNode<IntConstantNode>();
        lengthNode.SetValue(length);

        var substring = graph.CreateNode<SubstringNode>();
        substring.ConnectInput(0, inputNode, 0);
        substring.ConnectInput(1, startIndexNode, 0);
        substring.ConnectInput(2, lengthNode, 0);

        start.ExecOutPorts[0].Connect(substring.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)substring.OutputPorts[0]).Value;
        Assert.Equal(expected, result);
    }

    #endregion

    #region ReplaceNode Tests

    [Fact]
    public async Task ReplaceNode_ReplacesSubstring()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("Hello World");
        var oldValueNode = graph.CreateNode<StringConstantNode>();
        oldValueNode.SetValue("World");
        var newValueNode = graph.CreateNode<StringConstantNode>();
        newValueNode.SetValue("Universe");

        var replace = graph.CreateNode<ReplaceNode>();
        replace.ConnectInput(0, inputNode, 0);
        replace.ConnectInput(1, oldValueNode, 0);
        replace.ConnectInput(2, newValueNode, 0);

        start.ExecOutPorts[0].Connect(replace.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)replace.OutputPorts[0]).Value;
        Assert.Equal("Hello Universe", result);
    }

    [Fact]
    public async Task ReplaceNode_ReplacesAllOccurrences()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("a-b-c-d");
        var oldValueNode = graph.CreateNode<StringConstantNode>();
        oldValueNode.SetValue("-");
        var newValueNode = graph.CreateNode<StringConstantNode>();
        newValueNode.SetValue("_");

        var replace = graph.CreateNode<ReplaceNode>();
        replace.ConnectInput(0, inputNode, 0);
        replace.ConnectInput(1, oldValueNode, 0);
        replace.ConnectInput(2, newValueNode, 0);

        start.ExecOutPorts[0].Connect(replace.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)replace.OutputPorts[0]).Value;
        Assert.Equal("a_b_c_d", result);
    }

    #endregion

    #region StringJoinNode Tests

    [Fact]
    public async Task StringJoinNode_JoinsWithSeparator()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var a = graph.CreateNode<StringConstantNode>();
        a.SetValue("Hello");
        var b = graph.CreateNode<StringConstantNode>();
        b.SetValue("World");

        var join = graph.CreateNode<StringJoinNode>();
        join.SetSeparator(", ");
        join.ConnectInput(0, a, 0);
        join.ConnectInput(1, b, 0);

        start.ExecOutPorts[0].Connect(join.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)join.OutputPorts[0]).Value;
        Assert.Equal("Hello, World", result);
    }

    #endregion

    #region StringSplitNode Tests

    [Fact]
    public async Task StringSplitNode_SplitsBySeparator()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var input = graph.CreateNode<StringConstantNode>();
        input.SetValue("a,b,c");

        var split = graph.CreateNode<StringSplitNode>();
        split.SetSeparator(",");
        split.ConnectInput(0, input, 0);

        start.ExecOutPorts[0].Connect(split.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var firstResult = ((OutputPort<string>)split.OutputPorts[0]).Value;
        var secondResult = ((OutputPort<string>)split.OutputPorts[1]).Value;
        var countResult = ((OutputPort<int>)split.OutputPorts[2]).Value;

        Assert.Equal("a", firstResult);
        Assert.Equal("b", secondResult);
        Assert.Equal(3, countResult);
    }

    #endregion

    #region StringFormatNode Tests

    [Fact]
    public async Task StringFormatNode_FormatsString()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var arg0 = graph.CreateNode<StringConstantNode>();
        arg0.SetValue("World");

        var format = graph.CreateNode<StringFormatNode>();
        format.SetFormat("Hello, {0}!");
        format.ConnectInput(0, arg0, 0);

        start.ExecOutPorts[0].Connect(format.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)format.OutputPorts[0]).Value;
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task StringFormatNode_HandlesMultipleArgs()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var arg0 = graph.CreateNode<StringConstantNode>();
        arg0.SetValue("Alice");
        var arg1 = graph.CreateNode<StringConstantNode>();
        arg1.SetValue("30");

        var format = graph.CreateNode<StringFormatNode>();
        format.SetFormat("Name: {0}, Age: {1}");
        format.ConnectInput(0, arg0, 0);
        format.ConnectInput(1, arg1, 0);

        start.ExecOutPorts[0].Connect(format.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)format.OutputPorts[0]).Value;
        Assert.Equal("Name: Alice, Age: 30", result);
    }

    #endregion

    #region StringInterpolateNode Tests

    [Fact]
    public async Task StringInterpolateNode_InterpolatesTemplate()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var a = graph.CreateNode<StringConstantNode>();
        a.SetValue("Alice");
        var b = graph.CreateNode<StringConstantNode>();
        b.SetValue("30");

        var interpolate = graph.CreateNode<StringInterpolateNode>();
        interpolate.SetTemplate("Name: {A}, Age: {B}");
        interpolate.ConnectInput(0, a, 0);
        interpolate.ConnectInput(1, b, 0);

        start.ExecOutPorts[0].Connect(interpolate.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var result = ((OutputPort<string>)interpolate.OutputPorts[0]).Value;
        Assert.Equal("Name: Alice, Age: 30", result);
    }

    #endregion
}
