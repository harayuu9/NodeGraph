using NodeGraph.Model;

namespace NodeGraph.UnitTest.Model;

/// <summary>
/// GraphExecutorのテスト
/// </summary>
public class GraphExecutorTest
{
    [Fact]
    public async Task ExecuteAsync_EmptyGraph_NoException()
    {
        var graph = new Graph();
        var executor = graph.CreateExecutor();

        // 空のグラフでも例外が発生しないことを確認
        await executor.ExecuteAsync();
    }

    [Fact]
    public async Task ExecuteAsync_OnExecuteCallback_CalledForParallelNodes()
    {
        var graph = new Graph();
        // HasExec=false のノードのみを使用（並列実行される）
        var constant1 = graph.CreateNode<FloatConstantNode>();
        var constant2 = graph.CreateNode<FloatConstantNode>();
        var constant3 = graph.CreateNode<FloatConstantNode>();

        var executor = graph.CreateExecutor();
        var executedNodes = new List<Node>();

        await executor.ExecuteAsync(
            onExecute: node => executedNodes.Add(node)
        );

        // HasExec=false のノードのみが並列実行フェーズで実行される
        Assert.Equal(3, executedNodes.Count);
        Assert.Contains(constant1, executedNodes);
        Assert.Contains(constant2, executedNodes);
        Assert.Contains(constant3, executedNodes);
    }

    [Fact]
    public async Task ExecuteAsync_OnExecutedCallback_CalledAfterExecution()
    {
        var graph = new Graph();
        // HasExec=false のノードのみを使用
        var constant = graph.CreateNode<FloatConstantNode>();

        var executor = graph.CreateExecutor();
        var executedNodes = new List<Node>();

        await executor.ExecuteAsync(
            onExecuted: node => executedNodes.Add(node)
        );

        Assert.Single(executedNodes);
        Assert.Same(constant, executedNodes[0]);
    }

    [Fact]
    public async Task ExecuteAsync_OnExceptedCallback_CalledOnError()
    {
        var graph = new Graph();
        var failingNode = graph.CreateNode<TestExceptionNode>();

        var executor = graph.CreateExecutor();
        var exceptionNodes = new List<(Node, Exception)>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await executor.ExecuteAsync(
                onExcepted: (node, ex) => exceptionNodes.Add((node, ex))
            );
        });

        Assert.Single(exceptionNodes);
        Assert.Same(failingNode, exceptionNodes[0].Item1);
        Assert.IsType<InvalidOperationException>(exceptionNodes[0].Item2);
    }

    [Fact]
    public async Task ExecuteAsync_Parameters_PassedToNodes()
    {
        var graph = new Graph();
        var paramNode = graph.CreateNode<IntParameterNode>();
        paramNode.SetParameterName("testParam");
        paramNode.SetDefaultValue(0);

        var result = graph.CreateNode<TestIntResultNode>();
        result.ConnectInput(0, paramNode, 0);

        var executor = graph.CreateExecutor();
        var parameters = new Dictionary<string, object?>
        {
            { "testParam", 42 }
        };

        await executor.ExecuteAsync(parameters);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithStartNode_ExecutesInOrder()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var print = graph.CreateNode<PrintNode>();
        var message = graph.CreateNode<StringConstantNode>();
        message.SetValue("Test");
        print.ConnectInput(0, message, 0);
        start.ExecOutPorts[0].Connect(print.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        var executedWithExec = new List<string>();

        await executor.ExecuteAsync(
            onExecute: node =>
            {
                if (node.HasExec)
                    executedWithExec.Add(node.GetType().Name);
            }
        );

        // StartNodeとPrintNodeが実行されることを確認
        Assert.Contains("StartNode", executedWithExec);
        Assert.Contains("PrintNode", executedWithExec);
    }

    [Fact]
    public async Task ExecuteAsync_ParallelNodes_AllExecuted()
    {
        var graph = new Graph();

        // 5つの独立したノードを作成（並列実行される）
        var nodes = new List<FloatConstantNode>();
        for (int i = 0; i < 5; i++)
        {
            var node = graph.CreateNode<FloatConstantNode>();
            node.SetValue(i);
            nodes.Add(node);
        }

        var executor = graph.CreateExecutor();
        var executedCount = 0;

        await executor.ExecuteAsync(
            onExecuted: _ => Interlocked.Increment(ref executedCount)
        );

        Assert.Equal(5, executedCount);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationToken_Respected()
    {
        var graph = new Graph();
        var start = graph.CreateNode<StartNode>();
        var delayNode = graph.CreateNode<TestDelayNode>();
        start.ExecOutPorts[0].Connect(delayNode.ExecInPorts[0]);

        var executor = graph.CreateExecutor();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // 即座にキャンセル

        // キャンセルされたトークンで実行
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await executor.ExecuteAsync(
                onExecute: null,
                onExecuted: null,
                onExcepted: null,
                cancellationToken: cts.Token
            );
        });
    }
}

/// <summary>
/// テスト用の例外を投げるノード
/// </summary>
[Node("Test Exception", "Test", HasExecIn = false, HasExecOut = false)]
public partial class TestExceptionNode
{
    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        throw new InvalidOperationException("Test exception");
    }
}

/// <summary>
/// テスト用の遅延ノード
/// </summary>
[Node("Test Delay", "Test", HasExecIn = true, HasExecOut = true)]
public partial class TestDelayNode
{
    protected override async Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1000, context.CancellationToken);
    }
}

/// <summary>
/// テスト用のint結果ノード（HasExec=false）
/// </summary>
[Node("Test Int Result", "Test", HasExecIn = false, HasExecOut = false)]
public partial class TestIntResultNode
{
    [Input] private int _value;

    public int Value => _value;

    protected override Task ExecuteCoreAsync(NodeExecutionContext context)
    {
        return Task.CompletedTask;
    }
}
