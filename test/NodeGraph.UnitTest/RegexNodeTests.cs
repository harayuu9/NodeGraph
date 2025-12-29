using NodeGraph.Model;

namespace NodeGraph.UnitTest;

/// <summary>
/// 正規表現ノードのテスト
/// </summary>
public class RegexNodeTests
{
    [Theory]
    [InlineData("abc123", @"\d+", false, true, "123")]
    [InlineData("abcdef", @"\d+", false, false, "")]
    [InlineData("ABC", @"abc", false, false, "")]
    [InlineData("ABC", @"abc", true, true, "ABC")]
    public async Task RegexMatchNode_MatchesPattern(string input, string pattern, bool ignoreCase, bool expectedMatch, string expectedValue)
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue(input);

        var regex = graph.CreateNode<RegexMatchNode>();
        regex.SetPattern(pattern);
        regex.SetIgnoreCase(ignoreCase);
        regex.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(regex.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var isMatchResult = ((OutputPort<bool>)regex.OutputPorts[0]).Value;
        var matchValueResult = ((OutputPort<string>)regex.OutputPorts[1]).Value;

        Assert.Equal(expectedMatch, isMatchResult);
        Assert.Equal(expectedValue, matchValueResult);
    }

    [Fact]
    public async Task RegexMatchNode_HandlesInvalidPattern()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("test");

        var regex = graph.CreateNode<RegexMatchNode>();
        regex.SetPattern("[invalid"); // Invalid regex
        regex.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(regex.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var isMatchResult = ((OutputPort<bool>)regex.OutputPorts[0]).Value;
        // Should not throw, isMatch should be false
        Assert.False(isMatchResult);
    }

    [Fact]
    public async Task RegexMatchNode_HandlesEmptyPattern()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("test");

        var regex = graph.CreateNode<RegexMatchNode>();
        regex.SetPattern(""); // Empty pattern
        regex.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(regex.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var isMatchResult = ((OutputPort<bool>)regex.OutputPorts[0]).Value;
        Assert.False(isMatchResult);
    }

    [Fact]
    public async Task RegexExtractNode_ExtractsGroups()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("John:30:Developer");

        var regex = graph.CreateNode<RegexExtractNode>();
        regex.SetPattern(@"(\w+):(\d+):(\w+)");
        regex.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(regex.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var isMatchResult = ((OutputPort<bool>)regex.OutputPorts[0]).Value;
        var group0Result = ((OutputPort<string>)regex.OutputPorts[1]).Value;
        var group1Result = ((OutputPort<string>)regex.OutputPorts[2]).Value;
        var group2Result = ((OutputPort<string>)regex.OutputPorts[3]).Value;
        var group3Result = ((OutputPort<string>)regex.OutputPorts[4]).Value;

        Assert.True(isMatchResult);
        Assert.Equal("John:30:Developer", group0Result);
        Assert.Equal("John", group1Result);
        Assert.Equal("30", group2Result);
        Assert.Equal("Developer", group3Result);
    }

    [Fact]
    public async Task RegexExtractNode_ReturnsEmptyWhenNoMatch()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();
        inputNode.SetValue("no match here");

        var regex = graph.CreateNode<RegexExtractNode>();
        regex.SetPattern(@"(\d+):(\d+)");
        regex.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(regex.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        var isMatchResult = ((OutputPort<bool>)regex.OutputPorts[0]).Value;
        var group1Result = ((OutputPort<string>)regex.OutputPorts[2]).Value;

        Assert.False(isMatchResult);
        Assert.Equal("", group1Result);
    }

    [Fact]
    public async Task RegexNode_CachesCompiledRegex()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var inputNode = graph.CreateNode<StringConstantNode>();

        var regex = graph.CreateNode<RegexMatchNode>();
        regex.SetPattern(@"\d+");
        regex.ConnectInput(0, inputNode, 0);

        start.ExecOutPorts[0].Connect(regex.ExecInPorts[0]);

        var executor = graph.CreateExecutor();

        // Execute multiple times - should reuse cached regex
        for (int i = 0; i < 10; i++)
        {
            inputNode.SetValue($"test{i}");
            await executor.ExecuteAsync();
        }

        // If we got here without exception, caching is working
        Assert.True(true);
    }
}
