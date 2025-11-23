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
    /// LoopNodeでIndex + Indexを計算して0, 2, 4を出力するテスト
    ///
    /// GraphExecutorのデータノードリセット機能により、通常のデータノード（[Node]属性）も
    /// ループ内で再実行されるようになりました。
    ///
    /// 動作：
    /// - Index=0のとき: 0+0=0 → "[PrintNode] 0"
    /// - Index=1のとき: 1+1=2 → "[PrintNode] 2"
    /// - Index=2のとき: 2+2=4 → "[PrintNode] 4"
    /// - ループ完了後: "[PrintNode] Loop completed!"
    ///
    /// FloatAddNodeはExecutionNodeが再実行される際に自動的にリセットされ、
    /// 最新のLoopNode.Indexの値で再計算されます。
    /// </summary>
    [Fact]
    public async Task LoopWithDataNode_WorksCorrectly()
    {
        // Console.Outをリダイレクトして出力をキャプチャ
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var graph = new Graph();

            // StartNode
            var start = graph.CreateNode<StartNode>();

            // LoopNode (count=3)
            var loop = graph.CreateNode<LoopNode>();
            loop.SetCount(3);
            start.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

            // FloatAddNode (Index + Index) - 通常のデータノード
            var add = graph.CreateNode<FloatAddNode>();
            add.ConnectInput(0, loop, 0); // Index -> A
            add.ConnectInput(1, loop, 0); // Index -> B

            // PrintNode (ループ本体で実行)
            var printBody = graph.CreateNode<PrintNode>();
            printBody.ConnectInput(0, add, 0); // Result -> Message (float→string自動変換)
            loop.ExecOutPorts[0].Connect(printBody.ExecInPorts[0]); // LoopBody -> PrintNode

            // ループバック接続
            printBody.ExecOutPorts[0].Connect(loop.ExecInPorts[0]);

            // ループ完了後のPrintNode
            var printCompleted = graph.CreateNode<PrintNode>();
            var messageNode = graph.CreateNode<StringConstantNode>();
            messageNode.SetValue("Loop completed!");
            printCompleted.ConnectInput(0, messageNode, 0);
            loop.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]); // Completed -> Print

            // グラフ実行
            var executor = graph.CreateExecutor();
            var executedNodes = new List<string>();
            await executor.ExecuteAsync(
                onExecute: node =>
                {
                    var nodeName = node.GetType().Name;
                    executedNodes.Add(nodeName);
                    _testOutputHelper.WriteLine($"Executing: {nodeName}");
                },
                onExecuted: node =>
                {
                    var nodeName = node.GetType().Name;
                    _testOutputHelper.WriteLine($"Completed: {nodeName}");
                }
            );

            _testOutputHelper.WriteLine($"\nTotal executions: {executedNodes.Count}");
            _testOutputHelper.WriteLine($"FloatAddNode executions: {executedNodes.Count(n => n == "FloatAddNode")}");
            _testOutputHelper.WriteLine($"PrintNode executions: {executedNodes.Count(n => n == "PrintNode")}");

            // 出力を取得
            var output = stringWriter.ToString();
            _testOutputHelper.WriteLine("Captured output:");
            _testOutputHelper.WriteLine(output);

            // 出力を行ごとに分割
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var printLines = lines.Where(l => l.StartsWith("[PrintNode]")).ToArray();

            // PrintNodeの出力を確認
            Assert.Equal(4, printLines.Length); // ループ本体3回 + 完了メッセージ1回

            // ループ本体の出力を確認（0, 2, 4）
            // FloatAddNodeがループ内で再実行され、正しい値が計算される
            Assert.Contains("0", printLines[0]); // Index=0: 0+0=0
            Assert.Contains("2", printLines[1]); // Index=1: 1+1=2
            Assert.Contains("4", printLines[2]); // Index=2: 2+2=4

            // 完了メッセージを確認
            Assert.Contains("Loop completed!", printLines[3]);
        }
        finally
        {
            // Console.Outを元に戻す
            Console.SetOut(originalOut);
        }
    }
}
