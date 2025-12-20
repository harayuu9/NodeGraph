using NodeGraph.Model.Pool;

namespace NodeGraph.Model;

public class GraphExecutor : IDisposable
{
    private readonly Graph _graph;
    private readonly List<Node> _canParallelNodes;
    private readonly StartNode? _startNode;

    internal GraphExecutor(Graph graph)
    {
        _graph = graph;
        _canParallelNodes = new List<Node>();

        foreach (var node in graph.Nodes)
        {
            if (!node.HasExec)
            {
                _canParallelNodes.Add(node);
            }
        }

        _startNode = graph.Nodes.OfType<StartNode>().FirstOrDefault();
    }

    public async Task ExecuteAsync(
        Action<Node>? onExecute = null,
        Action<Node>? onExecuted = null,
        Action<Node, Exception>? onExcepted = null,
        CancellationToken cancellationToken = default)
    {
        using var tasksRental = ListPool<Task>.Shared.Rent(_canParallelNodes.Count, out var tasks);
        var context = new NodeExecutionContext(cancellationToken);

        // デリゲートを設定: 指定されたExecOutの接続先を実行
        context.ExecuteOutDelegate = async (node, index) =>
        {
            if (index < 0 || index >= node.ExecOutPorts.Length) return;

            var execOutPort = node.ExecOutPorts[index];
            var target = execOutPort.GetExecutionTarget();
            if (target != null)
            {
                await ExecuteNodeAsync(target, context, onExecute, onExecuted, onExcepted);
            }
        };

        // Phase 1: 並列実行（HasExec = false のノード）
        foreach (var node in _canParallelNodes)
        {
            tasks.Add(ExecuteNodeAsync(node, context, onExecute, onExecuted, onExcepted));
        }

        await Task.WhenAll(tasks);

        // Phase 2: StartNode から実行フロー開始
        if (_startNode != null)
        {
            await ExecuteNodeAsync(_startNode, context, onExecute, onExecuted, onExcepted);
        }
    }

    private async Task ExecuteNodeAsync(
        Node node,
        NodeExecutionContext context,
        Action<Node>? onExecute,
        Action<Node>? onExecuted,
        Action<Node, Exception>? onExcepted)
    {
        var previousNode = context.CurrentNode;
        context.CurrentNode = node;
        onExecute?.Invoke(node);
        try
        {
            await node.ExecuteAsync(context);
            onExecuted?.Invoke(node);
        }
        catch (Exception ex)
        {
            onExcepted?.Invoke(node, ex);
            throw;
        }
        finally
        {
            context.CurrentNode = previousNode;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
