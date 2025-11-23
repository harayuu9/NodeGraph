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
    public void StartNode_IsExecutionNode()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();

        _output.WriteLine($"StartNode type: {start.GetType().FullName}");
        _output.WriteLine($"Is ExecutionNode: {start is ExecutionNode}");
        _output.WriteLine($"Is Node: {start is Node}");
        _output.WriteLine($"Base type: {start.GetType().BaseType?.FullName}");
        _output.WriteLine($"ExecOutPorts count: {start.ExecOutPorts.Length}");

        Assert.True(start is ExecutionNode);
        Assert.Single(start.ExecOutPorts);
    }

    [Fact]
    public void StartNode_ExecOut_ConnectsAndReturnsTargets()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var loop = graph.CreateNode<LoopNode>();

        _output.WriteLine($"Before connect - start.ExecOutPorts[0].ConnectedPort: {start.ExecOutPorts[0].ConnectedPort}");

        var connected = start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);
        _output.WriteLine($"Connect result: {connected}");
        _output.WriteLine($"After connect - start.ExecOutPorts[0].ConnectedPort: {start.ExecOutPorts[0].ConnectedPort}");

        var targets = start.ExecOutPorts[0].GetExecutionTargets().ToArray();
        _output.WriteLine($"Execution targets count: {targets.Length}");
        if (targets.Length > 0)
        {
            _output.WriteLine($"Target[0] type: {targets[0].GetType().Name}");
        }

        Assert.True(connected);
        Assert.NotNull(start.ExecOutPorts[0].ConnectedPort);
        Assert.Single(targets);
        Assert.Equal(loop, targets[0]);
    }
}

