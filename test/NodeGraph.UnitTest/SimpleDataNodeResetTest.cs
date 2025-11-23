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
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
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
            printBody.ExecOutPorts[0].Connect(loop.ExecInPorts[0]); // ループバック

            var printCompleted = graph.CreateNode<PrintNode>();
            var messageNode = graph.CreateNode<StringConstantNode>();
            messageNode.SetValue("Done");
            printCompleted.ConnectInput(0, messageNode, 0);
            loop.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]);

            var executor = graph.CreateExecutor();
            await executor.ExecuteAsync();

            var output = stringWriter.ToString();
            _testOutputHelper.WriteLine("Output:");
            _testOutputHelper.WriteLine(output);

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var printLines = lines.Where(l => l.StartsWith("[PrintNode]")).ToArray();

            // 期待：0, 1, 2, Done
            Assert.Equal(4, printLines.Length);
            Assert.Contains("0", printLines[0]);
            Assert.Contains("1", printLines[1]);
            Assert.Contains("2", printLines[2]);
            Assert.Contains("Done", printLines[3]);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
