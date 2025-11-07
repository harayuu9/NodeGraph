using System.Buffers;
using NodeGraph.Model.Pool;

namespace NodeGraph.Model;

public class GraphExecutor : IDisposable
{
    private readonly int _nodeCount;
    private readonly Node[] _nodes;
    private readonly Dictionary<Node, HashSet<Node>> _predecessors;
    private readonly Dictionary<Node, HashSet<Node>> _successors;
    private DisposableBag _disposableBag;

    internal GraphExecutor(Graph graph)
    {
        _nodeCount = graph.Nodes.Count;
        _disposableBag = new DisposableBag(_nodeCount * 2 + 2);

        // ノードを配列にコピー
        _nodes = ArrayPool<Node>.Shared.Rent(_nodeCount);
        for (var i = 0; i < _nodeCount; i++)
        {
            _nodes[i] = graph.Nodes[i];
        }

        // 依存関係の辞書を初期容量付きで作成
        DictionaryPool<Node, HashSet<Node>>.Shared.Rent(_nodeCount, out _predecessors).AddTo(ref _disposableBag);
        DictionaryPool<Node, HashSet<Node>>.Shared.Rent(_nodeCount, out _successors).AddTo(ref _disposableBag);
        
        // 全ノードのエントリを初期化
        for (var i = 0; i < _nodeCount; i++)
        {
            HashSetPool<Node>.Shared.Rent(out var predecessors).AddTo(ref _disposableBag);
            HashSetPool<Node>.Shared.Rent(out var successors).AddTo(ref _disposableBag);
            _predecessors[_nodes[i]] = predecessors;
            _successors[_nodes[i]] = successors;
        }

        // 前段（predecessors）を構築
        for (var i = 0; i < _nodeCount; i++)
        {
            var node = _nodes[i];
            var inputPorts = node.InputPorts;
            var portCount = inputPorts.Length;

            for (var j = 0; j < portCount; j++)
            {
                var connectedPort = inputPorts[j].ConnectedPort;
                if (connectedPort != null)
                {
                    var pred = connectedPort.Parent;
                    if (pred != node)
                    {
                        _predecessors[node].Add(pred);
                    }
                }
            }
        }

        // 後段（successors）を構築
        foreach (var (node, preds) in _predecessors)
        {
            foreach (var p in preds)
            {
                _successors[p].Add(node);
            }
        }
    }

    public async Task ExecuteAsync(Action<Node>? onExecute = null, Action<Node>? onExecuted = null, Action<Node, Exception>? onExcepted = null, CancellationToken cancellationToken = default)
    {
        // 各ノードの残り依存数（実行ごとに新規作成）
        using var _1 = DictionaryPool<Node, int>.Shared.Rent(_nodeCount, out var remainingDeps);
        for (var i = 0; i < _nodeCount; i++)
        {
            remainingDeps[_nodes[i]] = _predecessors[_nodes[i]].Count;
        }

        // 実行管理用の変数
        using var _2 = HashSetPool<Task>.Shared.Rent(out var running);
        using var _3 = DictionaryPool<Task, Node>.Shared.Rent(_nodeCount, out var taskToNode);
        using var _4 = HashSetPool<Node>.Shared.Rent(_nodeCount, out var started);
        
        // 入次数0のノードを起動
        var initialReadyCount = 0;
        for (var i = 0; i < _nodeCount; i++)
        {
            if (remainingDeps[_nodes[i]] == 0)
            {
                Start(_nodes[i]);
                initialReadyCount++;
            }
        }

        if (_nodeCount > 0 && initialReadyCount == 0)
        {
            throw new InvalidOperationException("実行可能なノードがありません。グラフに循環があるか、依存関係が未解決の可能性があります。");
        }

        // 実行ループ：完了次第、後続を解放
        List<Exception>? exceptions = null;  // 遅延初期化
        var completedCount = 0;

        while (running.Count > 0)
        {
            var finished = await Task.WhenAny(running);
            running.Remove(finished);
            var finishedNode = taskToNode[finished];

            try
            {
                await finished;
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }

            completedCount++;

            if (exceptions == null)
            {
                foreach (var succ in _successors[finishedNode])
                {
                    remainingDeps[succ]--;
                    if (remainingDeps[succ] == 0 && !started.Contains(succ))
                    {
                        Start(succ);
                    }
                }
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("ノードの実行中にエラーが発生しました。", exceptions);
        }

        if (completedCount != _nodeCount)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < _nodeCount; i++)
            {
                if (remainingDeps[_nodes[i]] > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(_nodes[i].Id.Value.ToString());
                }
            }
            throw new InvalidOperationException($"循環または未解決の依存関係を検出しました: {sb}");
        }

        return;

        void Start(Node n)
        {
            var t = Task.Run(async () =>
            {
                onExecute?.Invoke(n);
                try
                {
                    await n.ExecuteAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    onExcepted?.Invoke(n, e);
                    throw;
                }

                onExecuted?.Invoke(n);
            }, cancellationToken);
            started.Add(n);
            running.Add(t);
            taskToNode[t] = n;
        }
    }

    public void Dispose()
    {
        _disposableBag.Dispose();
        ArrayPool<Node>.Shared.Return(_nodes);
        GC.SuppressFinalize(this);
    }
}