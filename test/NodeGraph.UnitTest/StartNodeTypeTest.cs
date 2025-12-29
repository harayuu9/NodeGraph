using NodeGraph.Model;
using Xunit.Abstractions;

namespace NodeGraph.UnitTest;

/// <summary>
/// ExecOutPortの単一接続動作テスト
/// </summary>
public class StartNodeTypeTest
{
    private readonly ITestOutputHelper _output;

    public StartNodeTypeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ExecOutPort_IsSingleConnection()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var loop1 = graph.CreateNode<LoopNode>();
        var loop2 = graph.CreateNode<LoopNode>();

        // 同じExecOutPortから2回接続すると、最後の接続のみが有効
        start.ExecOutPorts[0].Connect(loop1.ExecInPorts[0]);
        start.ExecOutPorts[0].Connect(loop2.ExecInPorts[0]);

        var target = start.ExecOutPorts[0].GetExecutionTarget();
        _output.WriteLine($"Execution target: {target?.GetType().Name}");

        // 単一接続なのでloop2のみが接続される
        Assert.Equal(loop2, target);
    }
}
