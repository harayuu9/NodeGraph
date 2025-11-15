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
        using var _5 = HashSetPool<Node>.Shared.Rent(_nodeCount, out var execFlowReady); // Exec flowが到達したノード
        using var _6 = HashSetPool<Node>.Shared.Rent(_nodeCount, out var currentlyExecuting); // 現在実行中のノード

        // 入次数0のノードを起動
        // ExecutionNodeでExecInを持つノードは、フロー到達まで待機
        var initialReadyCount = 0;
        for (var i = 0; i < _nodeCount; i++)
        {
            var node = _nodes[i];
            if (remainingDeps[node] == 0)
            {
                // ExecInを持たないノード、またはExecutionNodeでないノードは即座に実行
                if (node is not ExecutionNode execNode || !execNode.HasExecIn)
                {
                    Start(node);
                    initialReadyCount++;
                }
                else
                {
                    // ExecInを持つノードは、まだ実行しない（フロー待ち）
                    // 初期実行ノードにはカウントしない
                }
            }
        }

        if (_nodeCount > 0 && initialReadyCount == 0 && !HasAnyExecFlowEntry())
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
                // 実行完了したノードを現在実行中リストから削除
                currentlyExecuting.Remove(finishedNode);

                // データ依存の後続ノードを処理
                foreach (var succ in _successors[finishedNode])
                {
                    remainingDeps[succ]--;
                    if (remainingDeps[succ] == 0 && !started.Contains(succ))
                    {
                        // ExecInを持たないノードは即座に実行
                        if (succ is not ExecutionNode execSucc || !execSucc.HasExecIn)
                        {
                            Start(succ);
                        }
                        else if (execFlowReady.Contains(succ))
                        {
                            // ExecInを持つが、既にフローが到達している場合は実行
                            Start(succ);
                        }
                    }
                }

                // Exec flowの後続ノードを処理
                if (finishedNode is ExecutionNode execNode)
                {
                    var triggeredIndices = execNode.GetTriggeredExecOutIndices().ToArray();

                    // トリガーされたExecOutのみを処理
                    foreach (var execOutIndex in triggeredIndices)
                    {
                        var execOutPort = execNode.ExecOutPorts[execOutIndex];
                        var targets = execOutPort.GetExecutionTargets().ToArray();

                        foreach (var target in targets)
                        {
                            execFlowReady.Add(target);

                            // ExecutionNodeの再実行を許可（ループサポート）
                            // ただし、現在実行中でないことを確認
                            if (!currentlyExecuting.Contains(target))
                            {
                                // ExecutionNodeでExecInを持つ場合は、Exec flowのみで制御（データ依存無視）
                                if (target is ExecutionNode targetExecNode && targetExecNode.HasExecIn)
                                {
                                    // Exec flowで制御されるので、データ依存に関係なく実行
                                    Start(target);
                                }
                                // ExecutionNodeだがExecInを持たない場合は、データ依存をチェック
                                else if (target is ExecutionNode)
                                {
                                    if (remainingDeps[target] == 0)
                                    {
                                        Start(target);
                                    }
                                }
                                // 通常のNodeの場合
                                else if (remainingDeps[target] == 0 && !started.Contains(target))
                                {
                                    Start(target);
                                }
                            }
                        }
                    }

                    // 次回の実行のためにトリガー状態をリセット
                    execNode.ResetTriggers();
                }
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("ノードの実行中にエラーが発生しました。", exceptions);
        }

        // ExecutionNodeでExecInを持つノードは、実行されない可能性があるため、
        // completedCount != _nodeCountのチェックは行わない（または調整が必要）
        // ここでは簡易的に、未完了のノードがあってもエラーとしない
        // （実際には、ExecInを持つノードで到達不可能なものは無視する）

        return;

        bool HasAnyExecFlowEntry()
        {
            // ExecInを持たないExecutionNodeや、ExecInを持つがremainingDeps==0のノードがあるか
            for (var i = 0; i < _nodeCount; i++)
            {
                var node = _nodes[i];
                if (node is ExecutionNode en && en.HasExecIn && remainingDeps[node] == 0)
                {
                    // ExecInを持つノードがエントリーポイントとしてある可能性
                    // （通常は別途Entry nodeなどを用意するが、ここでは簡易判定）
                    return true;
                }
            }
            return false;
        }

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
            currentlyExecuting.Add(n);
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