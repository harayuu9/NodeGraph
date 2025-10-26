namespace NodeGraph.Model;

public class GraphExecutor
{
    private readonly Node[] _nodes;
    private readonly Dictionary<Node, HashSet<Node>> _predecessors;
    private readonly Dictionary<Node, HashSet<Node>> _successors;

    internal GraphExecutor(Graph graph)
    {
        var nodeCount = graph.Nodes.Count;

        // ノードを配列にコピー（LINQを使わずに最適化）
        _nodes = new Node[nodeCount];
        for (var i = 0; i < nodeCount; i++)
        {
            _nodes[i] = graph.Nodes[i];
        }

        // 依存関係の辞書を初期容量付きで作成
        _predecessors = new Dictionary<Node, HashSet<Node>>(nodeCount);
        _successors = new Dictionary<Node, HashSet<Node>>(nodeCount);

        // 全ノードのエントリを初期化
        for (var i = 0; i < nodeCount; i++)
        {
            _predecessors[_nodes[i]] = new HashSet<Node>();
            _successors[_nodes[i]] = new HashSet<Node>();
        }

        // 前段（predecessors）を構築
        for (var i = 0; i < nodeCount; i++)
        {
            var node = _nodes[i];
            var inputPorts = node.InputPorts;
            var portCount = inputPorts.Count;

            for (var j = 0; j < portCount; j++)
            {
                var connected = inputPorts[j].ConnectedPort;
                var pred = connected?.Parent;
                if (pred != null && pred != node)
                {
                    _predecessors[node].Add(pred);
                }
            }
        }

        // 後段（successors）を構築
        foreach (var kvp in _predecessors)
        {
            var node = kvp.Key;
            var preds = kvp.Value;
            foreach (var p in preds)
            {
                _successors[p].Add(node);
            }
        }
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var nodeCount = _nodes.Length;

        // 各ノードの残り依存数（実行ごとに新規作成）
        var remainingDeps = new Dictionary<Node, int>(nodeCount);
        for (var i = 0; i < nodeCount; i++)
        {
            remainingDeps[_nodes[i]] = _predecessors[_nodes[i]].Count;
        }

        // 実行管理用の変数
        var running = new HashSet<Task>();  // List→HashSetでRemoveをO(1)に
        var taskToNode = new Dictionary<Task, Node>(nodeCount);
        var started = new HashSet<Node>(nodeCount);

        // 入次数0のノードを起動
        var initialReadyCount = 0;
        for (var i = 0; i < nodeCount; i++)
        {
            if (remainingDeps[_nodes[i]] == 0)
            {
                Start(_nodes[i]);
                initialReadyCount++;
            }
        }

        if (nodeCount > 0 && initialReadyCount == 0)
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
                exceptions ??= new List<Exception>();
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

        if (exceptions != null && exceptions.Count > 0)
        {
            throw new AggregateException("ノードの実行中にエラーが発生しました。", exceptions);
        }

        if (completedCount != nodeCount)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < nodeCount; i++)
            {
                if (remainingDeps[_nodes[i]] > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(_nodes[i].Id.Value.ToString());
                }
            }
            throw new InvalidOperationException(
                $"循環または未解決の依存関係を検出しました: {sb}");
        }

        void Start(Node n)
        {
            var t = Task.Run(() => n.ExecuteAsync(cancellationToken), cancellationToken);
            started.Add(n);
            running.Add(t);
            taskToNode[t] = n;
        }
    }
}