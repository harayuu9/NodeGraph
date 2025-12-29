using NodeGraph.Model;
using Xunit.Abstractions;

namespace NodeGraph.UnitTest;

/// <summary>
/// 実行フロー制御のテスト
/// </summary>
[Collection("Sequential")]
public class ExecutionFlowTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ExecutionFlowTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task StartNode_ExecutesSuccessfully()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();

        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // StartNodeが実行されることを確認（例外が発生しないことで確認）
        Assert.True(true);
    }

    [Fact]
    public async Task IFNode_ExecutesTrueBranch()
    {
        var graph = new Graph();

        // テスト用にStringConstantNodeとPrintNodeを使用
        // Condition: true
        var conditionNode = graph.CreateNode<BoolConstantNode>();
        conditionNode.SetValue(true);

        var ifNode = graph.CreateNode<IFNode>();
        ifNode.ConnectInput(0, conditionNode, 0);

        var start = graph.CreateNode<StartNode>();

        // StartからIFにExec接続
        start.ExecOutPorts[0].Connect(ifNode.ExecInPorts[0]);

        // IFのTrueブランチにPrintを接続
        var printTrue = graph.CreateNode<PrintNode>();
        var messageTrueNode = graph.CreateNode<StringConstantNode>();
        messageTrueNode.SetValue("True branch executed");
        printTrue.ConnectInput(0, messageTrueNode, 0);
        ifNode.ExecOutPorts[0].Connect(printTrue.ExecInPorts[0]);

        // IFのFalseブランチにPrintを接続
        var printFalse = graph.CreateNode<PrintNode>();
        var messageFalseNode = graph.CreateNode<StringConstantNode>();
        messageFalseNode.SetValue("False branch executed");
        printFalse.ConnectInput(0, messageFalseNode, 0);
        ifNode.ExecOutPorts[1].Connect(printFalse.ExecInPorts[0]);

        var executor = graph.CreateExecutor();

        // 実行されたノードを追跡（実行フローノードのみカウント）
        var execFlowNodes = new List<string>();
        await executor.ExecuteAsync(node =>
            {
                if (node.HasExec) execFlowNodes.Add(node.GetType().Name);
            }
        );

        // StartNodeとIFNodeとprintTrueが実行されることを確認
        Assert.Contains("StartNode", execFlowNodes);
        Assert.Contains("IFNode", execFlowNodes);
        Assert.Contains("PrintNode", execFlowNodes);

        // printTrueが1回だけ実行されることを確認（printFalseは実行されない）
        var printNodeCount = execFlowNodes.Count(n => n == "PrintNode");
        Assert.Equal(1, printNodeCount);
    }

    [Fact]
    public async Task IFNode_ExecutesFalseBranch()
    {
        var graph = new Graph();

        // Condition: false
        var conditionNode = graph.CreateNode<BoolConstantNode>();
        conditionNode.SetValue(false);

        var ifNode = graph.CreateNode<IFNode>();
        ifNode.ConnectInput(0, conditionNode, 0);

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(ifNode.ExecInPorts[0]);

        var printTrue = graph.CreateNode<PrintNode>();
        var messageTrueNode = graph.CreateNode<StringConstantNode>();
        messageTrueNode.SetValue("True branch");
        printTrue.ConnectInput(0, messageTrueNode, 0);
        ifNode.ExecOutPorts[0].Connect(printTrue.ExecInPorts[0]);

        var printFalse = graph.CreateNode<PrintNode>();
        var messageFalseNode = graph.CreateNode<StringConstantNode>();
        messageFalseNode.SetValue("False branch");
        printFalse.ConnectInput(0, messageFalseNode, 0);
        ifNode.ExecOutPorts[1].Connect(printFalse.ExecInPorts[0]);

        var executor = graph.CreateExecutor();

        // 実行フローノードのみカウント
        var execFlowNodes = new List<string>();
        await executor.ExecuteAsync(node =>
            {
                if (node.HasExec) execFlowNodes.Add(node.GetType().Name);
            }
        );

        // FalseブランチのPrintNodeだけが実行されることを確認
        var printNodeCount = execFlowNodes.Count(n => n == "PrintNode");
        Assert.Equal(1, printNodeCount);
    }

    [Fact]
    public async Task LoopNode_ExecutesMultipleTimes()
    {
        var graph = new Graph();

        var loopNode = graph.CreateNode<LoopNode>();
        loopNode.SetCount(3); // 3回ループ

        var start = graph.CreateNode<StartNode>();
        start.ExecOutPorts[0].Connect(loopNode.ExecInPorts[0]);

        // ループボディのPrintNode
        var printBody = graph.CreateNode<PrintNode>();
        var bodyMessage = graph.CreateNode<StringConstantNode>();
        bodyMessage.SetValue("Loop body");
        printBody.ConnectInput(0, bodyMessage, 0);
        loopNode.ExecOutPorts[0].Connect(printBody.ExecInPorts[0]);

        // ループ完了後のノード
        var printCompleted = graph.CreateNode<PrintNode>();
        var messageNode = graph.CreateNode<StringConstantNode>();
        messageNode.SetValue("Loop completed");
        printCompleted.ConnectInput(0, messageNode, 0);
        loopNode.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]);

        var executor = graph.CreateExecutor();

        // 実行フローノードのみカウント
        var execFlowNodes = new List<string>();
        await executor.ExecuteAsync(
            node =>
            {
                if (node.HasExec)
                {
                    var nodeName = node.GetType().Name;
                    execFlowNodes.Add(nodeName);
                    _testOutputHelper.WriteLine($"Executing: {nodeName}");
                }
            },
            node =>
            {
                if (node.HasExec) _testOutputHelper.WriteLine($"Completed: {node.GetType().Name}");
            }
        );

        _testOutputHelper.WriteLine($"Total executions: {execFlowNodes.Count}");
        _testOutputHelper.WriteLine($"LoopNode executions: {execFlowNodes.Count(n => n == "LoopNode")}");
        _testOutputHelper.WriteLine($"PrintNode executions: {execFlowNodes.Count(n => n == "PrintNode")}");

        // LoopNodeは1回だけ実行される（内部でforループを回す）
        var loopNodeCount = execFlowNodes.Count(n => n == "LoopNode");
        Assert.Equal(1, loopNodeCount);

        // PrintNodeが4回実行されることを確認（ループ本体3回 + 完了後1回）
        var printNodeCount = execFlowNodes.Count(n => n == "PrintNode");
        Assert.Equal(4, printNodeCount);
    }

    /// <summary>
    /// LoopNodeのCountが異なる値の場合のテスト。
    /// SetCountで直接指定した回数ループすることを確認します。
    /// </summary>
    [Fact]
    public async Task LoopNode_ExecutesWithDifferentCount()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var loop = graph.CreateNode<LoopNode>();
        loop.SetCount(5); // 5回ループ
        start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

        // ループ本体のPrintNode
        var printBody = graph.CreateNode<PrintNode>();
        printBody.ConnectInput(0, loop, 0); // Index -> Message
        loop.ExecOutPorts[0].Connect(printBody.ExecInPorts[0]);

        // ループ完了後のPrintNode
        var printCompleted = graph.CreateNode<PrintNode>();
        var messageNode = graph.CreateNode<StringConstantNode>();
        messageNode.SetValue("Loop completed!");
        printCompleted.ConnectInput(0, messageNode, 0);
        loop.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        var executedNodes = new List<string>();
        await executor.ExecuteAsync(node =>
            {
                if (node.HasExec) executedNodes.Add(node.GetType().Name);
            }
        );

        // PrintNodeが6回実行される（ループ本体5回 + 完了後1回）
        var printNodeCount = executedNodes.Count(n => n == "PrintNode");
        Assert.Equal(6, printNodeCount);
    }
}

/// <summary>
/// テスト用のBool定数ノード
/// </summary>
[Node("Bool Constant", "Test", HasExecIn = false, HasExecOut = false)]
public partial class BoolConstantNode
{
    [Property] [Output] private bool _value;

    public void SetValue(bool value)
    {
        _value = value;
    }

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}