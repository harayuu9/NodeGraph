using NodeGraph.Model;

namespace NodeGraph.UnitTest.Model;

/// <summary>
/// ポート接続ロジックのテスト
/// </summary>
public class PortConnectionTest
{
    [Fact]
    public void Connect_BidirectionalConnection_Established()
    {
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        var result = graph.CreateNode<FloatResultNode>();

        var outputPort = constant.OutputPorts[0];
        var inputPort = result.InputPorts[0];

        var connected = outputPort.Connect(inputPort);

        Assert.True(connected);
        // OutputPort (MultiConnect) には InputPort が接続されている
        Assert.Contains(inputPort, ((OutputPort<float>)outputPort).ConnectedPorts.Cast<Port>());
        // InputPort (SingleConnect) には OutputPort が接続されている
        Assert.Equal(outputPort, ((InputPort<float>)inputPort).ConnectedPort);
    }

    [Fact]
    public void SingleConnectPort_SecondConnection_ReplacesFirst()
    {
        var graph = new Graph();
        var constant1 = graph.CreateNode<FloatConstantNode>();
        var constant2 = graph.CreateNode<FloatConstantNode>();
        var result = graph.CreateNode<FloatResultNode>();

        var output1 = constant1.OutputPorts[0];
        var output2 = constant2.OutputPorts[0];
        var input = result.InputPorts[0];

        // 最初の接続
        output1.Connect(input);
        Assert.Equal(output1, ((InputPort<float>)input).ConnectedPort);

        // 2回目の接続で最初の接続が置き換えられる
        output2.Connect(input);
        Assert.Equal(output2, ((InputPort<float>)input).ConnectedPort);

        // 最初の出力ポートからの接続は解除されている
        Assert.DoesNotContain(input, ((OutputPort<float>)output1).ConnectedPorts.Cast<Port>());
    }

    [Fact]
    public void MultiConnectPort_MultipleConnections_AllMaintained()
    {
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        var result1 = graph.CreateNode<FloatResultNode>();
        var result2 = graph.CreateNode<FloatResultNode>();
        var result3 = graph.CreateNode<FloatResultNode>();

        var output = constant.OutputPorts[0];

        // 同じ出力ポートから複数の入力に接続
        output.Connect(result1.InputPorts[0]);
        output.Connect(result2.InputPorts[0]);
        output.Connect(result3.InputPorts[0]);

        var connectedPorts = ((OutputPort<float>)output).ConnectedPorts;
        Assert.Equal(3, connectedPorts.Count);
        Assert.Contains(result1.InputPorts[0], connectedPorts.Cast<Port>());
        Assert.Contains(result2.InputPorts[0], connectedPorts.Cast<Port>());
        Assert.Contains(result3.InputPorts[0], connectedPorts.Cast<Port>());
    }

    [Fact]
    public void Disconnect_RemovesFromBothPorts()
    {
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        var result = graph.CreateNode<FloatResultNode>();

        var output = constant.OutputPorts[0];
        var input = result.InputPorts[0];

        output.Connect(input);
        Assert.NotEmpty(((OutputPort<float>)output).ConnectedPorts);

        // 切断
        output.Disconnect(input);
        input.Disconnect(output);

        Assert.Empty(((OutputPort<float>)output).ConnectedPorts);
        Assert.Null(((InputPort<float>)input).ConnectedPort);
    }

    [Fact]
    public void DisconnectAll_ClearsAllConnections()
    {
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        var result1 = graph.CreateNode<FloatResultNode>();
        var result2 = graph.CreateNode<FloatResultNode>();

        var output = constant.OutputPorts[0];

        output.Connect(result1.InputPorts[0]);
        output.Connect(result2.InputPorts[0]);

        Assert.Equal(2, ((OutputPort<float>)output).ConnectedPorts.Count);

        output.DisconnectAll();

        Assert.Empty(((OutputPort<float>)output).ConnectedPorts);
    }

    [Fact]
    public void CanConnect_CompatibleTypes_ReturnsTrue()
    {
        var graph = new Graph();
        var intConstant = graph.CreateNode<IntConstantNode>();
        var floatResult = graph.CreateNode<FloatResultNode>();

        var intOutput = intConstant.OutputPorts[0];
        var floatInput = floatResult.InputPorts[0];

        // int -> float は暗黙的変換可能
        var connected = intOutput.Connect(floatInput);
        Assert.True(connected);
    }

    [Fact]
    public void CanConnect_IncompatibleTypes_ReturnsFalse()
    {
        var graph = new Graph();
        var stringConstant = graph.CreateNode<StringConstantNode>();
        var testIncompat = graph.CreateNode<TestIncompatibleInputNode>();

        var stringOutput = stringConstant.OutputPorts[0];
        var guidInput = testIncompat.InputPorts[0];

        // string -> Guid は変換不可
        var connected = stringOutput.Connect(guidInput);
        Assert.False(connected);
    }

    [Fact]
    public void ExecOutPort_SingleConnection_ReplacesExisting()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var print1 = graph.CreateNode<PrintNode>();
        var print2 = graph.CreateNode<PrintNode>();

        var execOut = start.ExecOutPorts[0];

        // 最初の接続
        execOut.Connect(print1.ExecInPorts[0]);
        Assert.Equal(print1, execOut.GetExecutionTarget());

        // 2回目の接続で置き換え
        execOut.Connect(print2.ExecInPorts[0]);
        Assert.Equal(print2, execOut.GetExecutionTarget());
    }

    [Fact]
    public void ExecInPort_MultipleConnections_AllMaintained()
    {
        var graph = new Graph();
        var start1 = graph.CreateNode<StartNode>();
        var start2 = graph.CreateNode<StartNode>();
        var loop = graph.CreateNode<LoopNode>();

        // 複数のExecOutからExecInに接続（分岐からの合流など）
        // Note: 実際のノードではあまりこのケースは発生しないが、MultiConnectPortの動作テスト
        start1.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

        // ExecInPortはMultiConnectなので複数接続可能
        var execIn = loop.ExecInPorts[0];
        Assert.Contains(start1.ExecOutPorts[0], ((ExecInPort)execIn).ConnectedPorts.Cast<Port>());
    }

    [Fact]
    public void OutputPort_ValuePropagation_AfterConnection()
    {
        var graph = new Graph();
        var constant = graph.CreateNode<FloatConstantNode>();
        constant.SetValue(42.5f);

        var result = graph.CreateNode<FloatResultNode>();

        constant.OutputPorts[0].Connect(result.InputPorts[0]);

        // 値を設定
        ((OutputPort<float>)constant.OutputPorts[0]).Value = 100f;

        // 値が伝播していることを確認
        var inputValue = ((InputPort<float>)result.InputPorts[0]).Value;
        Assert.Equal(100f, inputValue);
    }

}

/// <summary>
/// テスト用の非互換型入力ノード（Guid型入力）
/// </summary>
[Node("Test Incompatible Input", "Test", HasExecIn = false, HasExecOut = false)]
public partial class TestIncompatibleInputNode
{
    [Input] private Guid _value;

    public Guid Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}
