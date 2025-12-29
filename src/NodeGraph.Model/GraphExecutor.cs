using NodeGraph.Model.Pool;
using NodeGraph.Model.Services;

namespace NodeGraph.Model;

public class GraphExecutor : IDisposable
{
    private readonly List<List<Node>> _executionLayers;
    private readonly Graph _graph;
    private readonly StartNode? _startNode;

    // Initializer自動検出用のキャッシュ
    private static List<INodeContextInitializer>? _cachedInitializers;
    private static readonly object _initializerLock = new();

    internal GraphExecutor(Graph graph)
    {
        _graph = graph;
        _executionLayers = TopologicalSort(graph);
        _startNode = graph.Nodes.OfType<StartNode>().FirstOrDefault();
    }

    /// <summary>
    /// HasExec=false のノードをトポロジカルソートして実行レイヤーを構築します。
    /// 各レイヤー内のノードは並列実行可能で、依存関係のあるノードは後のレイヤーに配置されます。
    /// </summary>
    private static List<List<Node>> TopologicalSort(Graph graph)
    {
        var layers = new List<List<Node>>();
        var dataNodes = new List<Node>();

        foreach (var node in graph.Nodes)
            if (!node.HasExec)
                dataNodes.Add(node);

        if (dataNodes.Count == 0)
            return layers;

        // 依存関係を構築: ノード -> そのノードが依存するノードのセット
        var dependencies = new Dictionary<Node, HashSet<Node>>();
        foreach (var node in dataNodes)
        {
            dependencies[node] = new HashSet<Node>();
            foreach (var inputPort in node.InputPorts)
            {
                if (inputPort.ConnectedPort?.Parent is Node sourceNode && !sourceNode.HasExec)
                {
                    dependencies[node].Add(sourceNode);
                }
            }
        }

        // レイヤー分け: 依存関係がないノードから順に処理
        var remaining = new HashSet<Node>(dataNodes);
        var processed = new HashSet<Node>();

        while (remaining.Count > 0)
        {
            var currentLayer = new List<Node>();

            foreach (var node in remaining)
            {
                // このノードの全ての依存先が処理済みか確認
                var deps = dependencies[node];
                if (deps.All(d => processed.Contains(d)))
                {
                    currentLayer.Add(node);
                }
            }

            if (currentLayer.Count == 0)
            {
                // サイクル検出: 残りのノードを全て現在のレイヤーに追加（エラー回避）
                currentLayer.AddRange(remaining);
            }

            foreach (var node in currentLayer)
            {
                remaining.Remove(node);
                processed.Add(node);
            }

            layers.Add(currentLayer);
        }

        return layers;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// グラフを実行します（後方互換性のためのオーバーロード）。
    /// </summary>
    public Task ExecuteAsync(
        Action<Node>? onExecute = null,
        Action<Node>? onExecuted = null,
        Action<Node, Exception>? onExcepted = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(null, onExecute, onExecuted, null, onExcepted, cancellationToken);
    }

    /// <summary>
    /// 外部パラメータを渡してグラフを実行します。
    /// </summary>
    /// <param name="parameters">外部パラメータの辞書。ParameterNodeから参照されます。</param>
    /// <param name="onExecute">ノード実行開始時のコールバック</param>
    /// <param name="onExecuted">ノード実行完了時のコールバック</param>
    /// <param name="onExecOut">ExecuteOutAsync呼び出し時のコールバック（制御フローノード用）</param>
    /// <param name="onExcepted">ノード実行例外時のコールバック</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters,
        Action<Node>? onExecute = null,
        Action<Node>? onExecuted = null,
        Action<Node>? onExecOut = null,
        Action<Node, Exception>? onExcepted = null,
        CancellationToken cancellationToken = default)
    {
        // サービスコンテナの初期化
        var serviceContainer = new ServiceContainer();
        var initializerContext = new InitializerContext(parameters, cancellationToken, serviceContainer);

        // Initializer自動検出・実行
        foreach (var initializer in GetInitializers())
            initializer.Initialize(initializerContext);

        var context = new NodeExecutionContext(cancellationToken, parameters, serviceContainer);

        // デリゲートを設定: 指定されたExecOutの接続先を実行
        context.ExecuteOutDelegate = async (node, index) =>
        {
            // ExecuteOutAsync呼び出し時にコールバック（制御フローノードの履歴記録用）
            onExecOut?.Invoke(node);

            if (index < 0 || index >= node.ExecOutPorts.Length) return;

            var execOutPort = node.ExecOutPorts[index];
            var target = execOutPort.GetExecutionTarget();
            if (target != null) await ExecuteNodeAsync(target, context, onExecute, onExecuted, onExecOut, onExcepted);
        };

        // Phase 1: レイヤー順に実行（HasExec = false のノード）
        // 各レイヤー内のノードは並列実行可能、レイヤー間は依存関係を尊重
        foreach (var layer in _executionLayers)
        {
            if (layer.Count == 1)
            {
                // 単一ノードの場合は直接実行
                await ExecuteNodeAsync(layer[0], context, onExecute, onExecuted, onExecOut, onExcepted);
            }
            else
            {
                // 複数ノードの場合は並列実行
                using var tasksRental = ListPool<Task>.Shared.Rent(layer.Count, out var tasks);
                foreach (var node in layer)
                    tasks.Add(ExecuteNodeAsync(node, context, onExecute, onExecuted, onExecOut, onExcepted));
                await Task.WhenAll(tasks);
            }
        }

        // Phase 2: StartNode から実行フロー開始
        if (_startNode != null) await ExecuteNodeAsync(_startNode, context, onExecute, onExecuted, onExecOut, onExcepted);
    }

    private async Task ExecuteNodeAsync(
        Node node,
        NodeExecutionContext context,
        Action<Node>? onExecute,
        Action<Node>? onExecuted,
        Action<Node>? onExecOut,
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

    /// <summary>
    /// キャッシュされたInitializerを取得します。
    /// </summary>
    private static IReadOnlyList<INodeContextInitializer> GetInitializers()
    {
        if (_cachedInitializers != null)
            return _cachedInitializers;

        lock (_initializerLock)
        {
            if (_cachedInitializers != null)
                return _cachedInitializers;

            _cachedInitializers = DiscoverInitializers();
            return _cachedInitializers;
        }
    }

    /// <summary>
    /// INodeContextInitializerを実装するクラスを自動検出します。
    /// </summary>
    private static List<INodeContextInitializer> DiscoverInitializers()
    {
        var initializers = new List<INodeContextInitializer>();
        var interfaceType = typeof(INodeContextInitializer);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && interfaceType.IsAssignableFrom(type))
                    {
                        try
                        {
                            if (Activator.CreateInstance(type) is INodeContextInitializer initializer)
                                initializers.Add(initializer);
                        }
                        catch
                        {
                            // インスタンス作成エラーは無視
                        }
                    }
                }
            }
            catch
            {
                // アセンブリ読み込みエラーは無視
            }
        }

        // Order順にソート
        initializers.Sort((a, b) => a.Order.CompareTo(b.Order));
        return initializers;
    }

    /// <summary>
    /// Initializerキャッシュをクリアします（テスト用）。
    /// </summary>
    internal static void ClearInitializerCache()
    {
        lock (_initializerLock)
        {
            _cachedInitializers = null;
        }
    }
}