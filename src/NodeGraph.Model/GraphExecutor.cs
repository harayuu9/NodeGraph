namespace NodeGraph.Model;

public class GraphExecutor : IDisposable
{
    private readonly Graph _graph;

    internal GraphExecutor(Graph graph)
    {
        _graph = graph;
    }

    public async Task ExecuteAsync(
    Action<Node>? onExecute = null,
    Action<Node>? onExecuted = null,
    Action<Node, Exception>? onExcepted = null,
    CancellationToken cancellationToken = default)
    {
        // Step 1: ExecInを持たないノードを全て並列実行
        var noExecInNodes = new List<Node>();
        var contexts = new Dictionary<Node, NodeExecutionContext>();

        foreach (var node in _graph.Nodes)
        {
            if (!node.HasExecIn)
            {
                noExecInNodes.Add(node);
            }
        }

        var tasks = new List<Task>(noExecInNodes.Count);
        foreach (var node in noExecInNodes)
        {
            var context = new NodeExecutionContext(cancellationToken);
            contexts[node] = context;
            tasks.Add(ExecuteNodeAsync(node, context, onExecute, onExecuted, onExcepted));
        }

        await Task.WhenAll(tasks);

        // Step 2: StartNodeからExec flowを順次実行
        foreach (var node in noExecInNodes)
        {
            if (node is StartNode)
            {
                await ExecuteExecFlowAsync(node, contexts[node], onExecute, onExecuted, onExcepted, cancellationToken);
            }
        }
    }

    private async Task ExecuteExecFlowAsync(
    Node currentNode,
    NodeExecutionContext context,
    Action<Node>? onExecute,
    Action<Node>? onExecuted,
    Action<Node, Exception>? onExcepted,
    CancellationToken cancellationToken)
    {
        var execOutPorts = currentNode.ExecOutPorts;

        for (int i = 0; i < execOutPorts.Length; i++)
        {
            if (!context.IsTriggered(i)) continue;

            foreach (var target in execOutPorts[i].GetExecutionTargets())
            {
                var targetContext = new NodeExecutionContext(cancellationToken);
                await ExecuteNodeAsync(target, targetContext, onExecute, onExecuted, onExcepted);

                // 再帰的にExec flowを辿る（LoopNode対応）
                await ExecuteExecFlowAsync(target, targetContext, onExecute, onExecuted, onExcepted, cancellationToken);
            }
        }
    }

    private async Task ExecuteNodeAsync(
    Node node,
    NodeExecutionContext context,
    Action<Node>? onExecute,
    Action<Node>? onExecuted,
    Action<Node, Exception>? onExcepted)
    {
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
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
