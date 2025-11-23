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
            _predecessors[_nodes[i]].Clear();
            _successors[_nodes[i]].Clear();
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
        using var _7 = DictionaryPool<Task, NodeExecutionContext>.Shared.Rent(_nodeCount, out var taskToContext); // TaskとContextの紐づけ

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

        while (running.Count > 0)
        {
            var finished = await Task.WhenAny(running);
            running.Remove(finished);
            var finishedNode = taskToNode[finished];
            var context = taskToContext[finished];

            try
            {
                await finished;
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }

            if (exceptions == null)
            {
                // 実行完了したノードを現在実行中リストから削除
                currentlyExecuting.Remove(finishedNode);

                // データ依存の後続ノードを処理
                foreach (var succ in _successors[finishedNode])
                {
                    // startedに含まれていないノード、またはstartedに含まれているが実行中のノードのみremainingDepsを減算
                    // (startedから削除されたノードは、再実行のためにremainingDepsを維持する)
                    if (!started.Contains(succ) || currentlyExecuting.Contains(succ))
                    {
                        remainingDeps[succ]--;
                    }

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
                    var triggeredIndices = context.GetTriggeredExecOutIndices();

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
                                // ExecutionNodeでExecInを持つ場合でも、データ依存が解決済みの場合のみ実行
                                if (target is ExecutionNode targetExecNode && targetExecNode.HasExecIn)
                                {
                                    // 再実行の場合、先に依存データノードをリセット
                                    if (started.Contains(target))
                                    {
                                        // 自分自身をstartedから削除
                                        started.Remove(target);
                                        // 依存先のデータノードをリセット
                                        ResetDependentDataNodes(target);
                                    }

                                    // Exec flowが到達したが、データ依存が未解決の場合は待つ
                                    if (remainingDeps[target] == 0)
                                    {
                                        Start(target);
                                    }
                                    // データ依存が未解決の場合は、依存解決後に実行される
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
                                else
                                {
                                    if (remainingDeps[target] == 0 && !started.Contains(target))
                                    {
                                        Start(target);
                                    }
                                }
                            }
                        }
                    }
                }

                onExecuted?.Invoke(finishedNode);
            }
            else
            {
                onExcepted?.Invoke(finishedNode, exceptions[^1]);
            }
        }

        // すべてのタスクが完了した後、例外があれば投げる
        if (exceptions != null && exceptions.Count > 0)
        {
            throw new AggregateException("グラフの実行中に1つ以上のエラーが発生しました。", exceptions);
        }

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
            // リセット処理はExecフロー処理の中で行われるため、ここでは何もしない

            var context = new NodeExecutionContext(n, cancellationToken);
            var t = Task.Run(async () =>
            {
                onExecute?.Invoke(n);
                try
                {
                    await n.ExecuteAsync(context);
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
            taskToContext[t] = context;
        }

        void ResetDependentDataNodes(Node execNode)
        {
            // execNodeの入力ポートから遡って依存しているデータノードを収集
            using var _dataNodes = HashSetPool<Node>.Shared.Rent(out var dataNodes);
            CollectDependentDataNodes(execNode, dataNodes);

            // リセット処理：startedから削除して再実行可能にする
            // 実行中のノードはリセットしない（完了を待つ）
            foreach (var dataNode in dataNodes)
            {
                if (started.Contains(dataNode) && !currentlyExecuting.Contains(dataNode))
                {
                    started.Remove(dataNode);

                    // remainingDepsを再計算：完了済みの依存先を除外
                    var unfinishedDeps = 0;
                    foreach (var pred in _predecessors[dataNode])
                    {
                        // リセット対象のデータノードは未完了としてカウント
                        if (dataNodes.Contains(pred) && !currentlyExecuting.Contains(pred))
                        {
                            unfinishedDeps++;
                        }
                        // リセット対象外で未開始のノードは未完了
                        else if (!started.Contains(pred))
                        {
                            unfinishedDeps++;
                        }
                        // 実行中のノードは未完了
                        else if (currentlyExecuting.Contains(pred))
                        {
                            unfinishedDeps++;
                        }
                        // それ以外は完了済み
                    }
                    remainingDeps[dataNode] = unfinishedDeps;
                }
            }

            // リセット後、依存関係が解決済みのデータノードを再実行
            foreach (var dataNode in dataNodes)
            {
                if (remainingDeps[dataNode] == 0 && !started.Contains(dataNode) && !currentlyExecuting.Contains(dataNode))
                {
                    // データノードを再実行する際、そのsuccessorsのremainingDepsを増やす
                    // （再実行されたデータノードの完了を待つため）
                    foreach (var succ in _successors[dataNode])
                    {
                        remainingDeps[succ]++;
                    }

                    // データノードは即座に実行（ExecInを持たない）
                    Start(dataNode);
                }
            }
        }

        void CollectDependentDataNodes(Node node, HashSet<Node> visited)
        {
            // このノードの依存先（predecessors）を走査
            foreach (var pred in _predecessors[node])
            {
                // ExecutionNodeは含めない（データノードのみをリセット対象とする）
                if (pred is not ExecutionNode && visited.Add(pred))
                {
                    // 再帰的に依存先を収集
                    CollectDependentDataNodes(pred, visited);
                }
            }
        }
    }

    public void Dispose()
    {
        _disposableBag.Dispose();
        ArrayPool<Node>.Shared.Return(_nodes);
        GC.SuppressFinalize(this);
    }
}