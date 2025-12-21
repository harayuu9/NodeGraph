using NodeGraph.Model;
using Xunit.Abstractions;

namespace NodeGraph.UnitTest;

[Collection("Sequential")]
/// <summary>
/// データノードリセット機能の簡素なテスト
/// </summary>
public class SimpleDataNodeResetTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SimpleDataNodeResetTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// 最も単純なケース：LoopNode → PrintNode（データ依存なし）
    /// LoopNodeのIndexが正しく更新されるかを確認
    /// </summary>
    [Fact]
    public async Task SimpleLoop_PrintsIndexValues()
    {
        var graph = new Graph();

        var start = graph.CreateNode<StartNode>();
        var loop = graph.CreateNode<LoopNode>();
        loop.SetCount(3);
        start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

        // LoopNodeのIndexを直接PrintNodeに接続
        var printBody = graph.CreateNode<PrintNode>();
        printBody.ConnectInput(0, loop, 0); // Index -> Message (int→string自動変換)
        loop.ExecOutPorts[0].Connect(printBody.ExecInPorts[0]);

        var printCompleted = graph.CreateNode<PrintNode>();
        var messageNode = graph.CreateNode<StringConstantNode>();
        messageNode.SetValue("Done");
        printCompleted.ConnectInput(0, messageNode, 0);
        loop.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        var executedNodes = new List<string>();
        await executor.ExecuteAsync(node =>
            {
                executedNodes.Add(node.GetType().Name);
                _testOutputHelper.WriteLine($"Executing: {node.GetType().Name}");
            }
        );

        // PrintNodeが4回実行される（ループ本体3回 + 完了後1回）
        var printNodeCount = executedNodes.Count(n => n == "PrintNode");
        Assert.Equal(4, printNodeCount);
    }
}