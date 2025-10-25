namespace NodeGraph.Model;

public interface IId
{
    Guid Value { get; }
}

public interface IWithId<out T>
    where T : IId
{
    T Id { get; }
}

public record struct PortId(Guid Value) : IId;
public record struct NodeId(Guid Value) : IId;

public abstract class InputPort : IWithId<PortId>
{
    protected InputPort(Node parent)
    {
        Parent = parent;
        Id = new PortId(Guid.NewGuid());
    }
    
    public PortId Id { get; } 
    public Node Parent { get; }
    
    internal abstract OutputPort? ConnectedPort { get; }
    public abstract bool CanConnect(OutputPort other);
    public abstract void Connect(OutputPort other);
    public abstract void Disconnect();
}

public class InputPort<T> : InputPort
{
    public InputPort(Node parent, T value) : base(parent)
    {
        Value = value;
    }
    
    public T Value { get; set; }
    public OutputPort<T>? ConnectedPortRaw { get; set; }
    internal override OutputPort? ConnectedPort => ConnectedPortRaw;
    public override bool CanConnect(OutputPort other) => other is OutputPort<T>;
    public override void Connect(OutputPort other)
    {
        Disconnect();
        
        var x = (OutputPort<T>)other;
        ConnectedPortRaw = x;
        x.ConnectedPorts.Add(this);
    }

    public override void Disconnect()
    {
        ConnectedPort?.Disconnect();
    }
}

public abstract class OutputPort : IWithId<PortId>
{
    protected OutputPort(Node parent)
    {
        Parent = parent;
        Id = new PortId(Guid.NewGuid());
    }
    
    public PortId Id { get; } 
    public Node Parent { get; }
    
    public abstract bool CanConnect(InputPort other);
    public abstract void Connect(InputPort other);
    public abstract void Disconnect();
    public abstract void Disconnect(InputPort inputPort);
}

public class OutputPort<T> : OutputPort
{
    public OutputPort(Node parent, T value) : base(parent)
    {
        Value = value;
    }

    public T Value
    {
        set
        {
            foreach (var port in ConnectedPorts)
            {
                port.Value = value;
            }
        }
    }

    public List<InputPort<T>> ConnectedPorts { get; } = [];
    public override bool CanConnect(InputPort other) => other is InputPort<T>;
    public override void Connect(InputPort other)
    {
        other.Disconnect();
        
        var x = (InputPort<T>)other;
        ConnectedPorts.Add(x);
        x.ConnectedPortRaw = this;
    }

    public override void Disconnect()
    {
        foreach (var x in ConnectedPorts)
        {
            x.ConnectedPortRaw = null;
        }
        ConnectedPorts.Clear();  
    }

    public override void Disconnect(InputPort inputPort)
    {
        var x = (InputPort<T>)inputPort;
        x.ConnectedPortRaw = null;
        ConnectedPorts.Remove(x);
    }
}

public abstract class Node : IWithId<NodeId>
{
    public NodeId Id { get; } = new(Guid.NewGuid());
    protected internal List<InputPort> InputPorts { get; } = [];
    protected internal List<OutputPort> OutputPorts { get; } = [];
    
    protected abstract void InitializePorts();
    protected abstract void BeforeExecute();
    protected abstract void AfterExecute();
    protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken);
    public void ConnectInput(int inputIndex, Node node, int outputIndex)
    {
        if (inputIndex < 0 || inputIndex >= InputPorts.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(inputIndex));
        }
        if (outputIndex < 0 || outputIndex >= node.OutputPorts.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(outputIndex));
        }
        
        if (!InputPorts[inputIndex].CanConnect(node.OutputPorts[outputIndex]))
        {
            return;
        }
        
        InputPorts[inputIndex].Connect(node.OutputPorts[outputIndex]);
    }

    public void Initialize()
    {
        InitializePorts();
    }
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        BeforeExecute();
        await ExecuteCoreAsync(cancellationToken);
        AfterExecute();
    }
}

public class Graph
{
    public List<Node> Nodes { get; } = [];
    
    public T CreateNode<T>() where T : Node, new()
    {
        var node = new T();
        node.Initialize();
        Nodes.Add(node);
        return node;
    }
    
    public T[] GetNodes<T>() where T : Node
    {
        return Nodes.OfType<T>().ToArray();
    }

    public async Task Execute()
    {
        // --- 依存関係（前段/後段）を構築（※前段は重複排除） ---
        var predecessors = Nodes.ToDictionary(n => n, _ => new HashSet<Node>());
        var successors   = Nodes.ToDictionary(n => n, _ => new HashSet<Node>());

        foreach (var node in Nodes)
        {
            foreach (var ip in node.InputPorts)
            {
                var connected = ip.ConnectedPort;
                var pred = connected?.Parent;
                if (pred != null && pred != node)
                {
                    predecessors[node].Add(pred);
                }
            }
        }
        foreach (var (node, preds) in predecessors)
        {
            foreach (var p in preds)
            {
                successors[p].Add(node);
            }
        }

        // 各ノードの残り依存数（ユニーク前段数）
        var remainingDeps = predecessors.ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        // 実行開始関数
        var running = new List<Task>();
        var taskToNode = new Dictionary<Task, Node>();
        var started = new HashSet<Node>();

        // 入次数 0 を起動
        var initialReady = Nodes.Where(n => remainingDeps[n] == 0).ToList();
        if (Nodes.Count > 0 && initialReady.Count == 0)
        {
            throw new InvalidOperationException("実行可能なノードがありません。グラフに循環があるか、依存関係が未解決の可能性があります。");
        }
        foreach (var n in initialReady) Start(n);

        // 実行ループ：完了次第、後続を解放
        var exceptions = new List<Exception>();
        var completedCount = 0;

        while (running.Count > 0)
        {
            var finished = await Task.WhenAny(running);
            running.Remove(finished);
            var finishedNode = taskToNode[finished];

            try
            {
                // 例外はここで拾う（以降のスケジューリング方針は要件次第だが、
                // 本実装では新規起動は行わず、既に走っているものは待機して集約）
                await finished;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            completedCount++;

            if (exceptions.Count == 0)
            {
                foreach (var succ in successors[finishedNode])
                {
                    remainingDeps[succ]--;
                    if (remainingDeps[succ] == 0 && !started.Contains(succ))
                    {
                        Start(succ);
                    }
                }
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("ノードの実行中にエラーが発生しました。", exceptions);
        }

        if (completedCount != Nodes.Count)
        {
            var unresolved = Nodes.Where(n => remainingDeps[n] > 0)
                                  .Select(n => n.Id.Value.ToString());
            throw new InvalidOperationException(
                $"循環または未解決の依存関係を検出しました: {string.Join(", ", unresolved)}");
        }

        return;

        void Start(Node n)
        {
            var t = Task.Run(() => n.ExecuteAsync(CancellationToken.None));
            started.Add(n);
            running.Add(t);
            taskToNode[t] = n;
        }
    }
}