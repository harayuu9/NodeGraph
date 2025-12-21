using NodeGraph.Model;
using Xunit.Abstractions;

namespace NodeGraph.UnitTest;

public class StartNodeTypeTest
{
    private readonly ITestOutputHelper _output;

    public StartNodeTypeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StartNode_IsNode()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();

        _output.WriteLine($"StartNode type: {start.GetType().FullName}");
        _output.WriteLine($"Is Node: {start is Node}");
        _output.WriteLine($"Base type: {start.GetType().BaseType?.FullName}");
        _output.WriteLine($"ExecOutPorts count: {start.ExecOutPorts.Length}");
        _output.WriteLine($"HasExecIn: {start.HasExecIn}");

        Assert.True(start is Node);
        Assert.False(start.HasExecIn); // StartNodeはエントリーポイント
        Assert.Single(start.ExecOutPorts);
    }

    [Fact]
    public void StartNode_ExecOut_ConnectsAndReturnsTarget()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var loop = graph.CreateNode<LoopNode>();

        _output.WriteLine($"Before connect - start.ExecOutPorts[0].ConnectedPort: {start.ExecOutPorts[0].ConnectedPort}");

        var connected = start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);
        _output.WriteLine($"Connect result: {connected}");
        _output.WriteLine($"After connect - start.ExecOutPorts[0].ConnectedPort: {start.ExecOutPorts[0].ConnectedPort}");

        var target = start.ExecOutPorts[0].GetExecutionTarget();
        _output.WriteLine($"Execution target: {target?.GetType().Name}");

        Assert.True(connected);
        Assert.NotNull(start.ExecOutPorts[0].ConnectedPort);
        Assert.Equal(loop, target);
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