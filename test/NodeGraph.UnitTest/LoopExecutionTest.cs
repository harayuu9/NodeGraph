using NodeGraph.Model;
using Xunit.Abstractions;

namespace NodeGraph.UnitTest;

[Collection("Sequential")]
/// <summary>
/// LoopNodeの実行テスト
/// </summary>
public class LoopExecutionTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LoopExecutionTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// LoopNodeがノード内でループを完結し、正しい回数実行されることをテスト。
    /// 新しい設計では、LoopNodeは内部でforループを持ち、
    /// 外部ループバック接続は不要になりました。
    /// </summary>
    [Fact]
    public async Task LoopNode_ExecutesCorrectly()
    {
        var graph = new Graph();

        // StartNode
        var start = graph.CreateNode<StartNode>();

        // LoopNode (count=3)
        var loop = graph.CreateNode<LoopNode>();
        loop.SetCount(3);
        start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

        // PrintNode (ループ本体で実行)
        var printBody = graph.CreateNode<PrintNode>();
        printBody.ConnectInput(0, loop, 0); // Index -> Message
        loop.ExecOutPorts[0].Connect(printBody.ExecInPorts[0]); // LoopBody -> PrintNode

        // ループ完了後のPrintNode
        var printCompleted = graph.CreateNode<PrintNode>();
        var messageNode = graph.CreateNode<StringConstantNode>();
        messageNode.SetValue("Loop completed!");
        printCompleted.ConnectInput(0, messageNode, 0);
        loop.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]); // Completed -> Print

        // グラフ実行
        var executor = graph.CreateExecutor();
        var executedNodes = new List<string>();
        await executor.ExecuteAsync(node =>
            {
                var nodeName = node.GetType().Name;
                executedNodes.Add(nodeName);
                _testOutputHelper.WriteLine($"Executing: {nodeName}");
            }
        );

        // 実行順序の検証
        // PrintNodeが4回実行される（ループ本体3回 + 完了後1回）
        var printNodeCount = executedNodes.Count(n => n == "PrintNode");
        Assert.Equal(4, printNodeCount);

        // LoopNodeは1回だけ実行される
        var loopNodeCount = executedNodes.Count(n => n == "LoopNode");
        Assert.Equal(1, loopNodeCount);

        // StartNodeは1回だけ実行される
        var startNodeCount = executedNodes.Count(n => n == "StartNode");
        Assert.Equal(1, startNodeCount);
    }

    /// <summary>
    /// LoopNodeのCountが異なる値の場合のテスト。
    /// SetCountで直接指定した回数ループすることを確認します。
    /// </summary>
    [Fact]
    public async Task LoopWithDifferentCount_WorksCorrectly()
    {
        var graph = new Graph();

        // StartNode
        var start = graph.CreateNode<StartNode>();

        // LoopNode (Count=5)
        var loop = graph.CreateNode<LoopNode>();
        loop.SetCount(5);
        start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

        // PrintNode (ループ本体)
        var printBody = graph.CreateNode<PrintNode>();
        printBody.ConnectInput(0, loop, 0); // Index -> Message
        loop.ExecOutPorts[0].Connect(printBody.ExecInPorts[0]); // LoopBody -> Print

        // 完了メッセージ
        var printCompleted = graph.CreateNode<PrintNode>();
        var messageNode = graph.CreateNode<StringConstantNode>();
        messageNode.SetValue("Loop completed!");
        printCompleted.ConnectInput(0, messageNode, 0);
        loop.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]);

        // グラフ実行
        var executor = graph.CreateExecutor();
        var executedNodes = new List<string>();
        await executor.ExecuteAsync(node =>
            {
                var nodeName = node.GetType().Name;
                executedNodes.Add(nodeName);
                _testOutputHelper.WriteLine($"Executing: {nodeName}");
            }
        );

        // PrintNodeが6回実行される（ループ本体5回 + 完了後1回）
        var printNodeCount = executedNodes.Count(n => n == "PrintNode");
        Assert.Equal(6, printNodeCount);
    }
}