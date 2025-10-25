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
    public OutputPort<T>? ConnectedPort { get; set; }
    public override bool CanConnect(OutputPort other) => other is OutputPort<T>;
    public override void Connect(OutputPort other)
    {
        Disconnect();
        other.Disconnect(this);
        
        var x = (OutputPort<T>)other;
        ConnectedPort = x;
        x.ConnectedPorts.Add(this);
    }

    public override void Disconnect()
    {
        ConnectedPort?.Disconnect();
        ConnectedPort = null;   
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
        Disconnect();
        other.Disconnect();
        
        var x = (InputPort<T>)other;
        ConnectedPorts.Add(x);
        x.ConnectedPort = this;
    }

    public override void Disconnect()
    {
        foreach (var x in ConnectedPorts)
        {
            x.ConnectedPort = null;
        }
        ConnectedPorts.Clear();  
    }

    public override void Disconnect(InputPort inputPort)
    {
        var x = (InputPort<T>)inputPort;
        ConnectedPorts.Remove(x);
    }
}

public abstract class Node : IWithId<NodeId>
{
    public NodeId Id { get; } = new(Guid.NewGuid());
    protected List<InputPort> InputPorts { get; } = [];
    protected List<OutputPort> OutputPorts { get; } = [];
    
    protected abstract void InitializePorts();
    protected abstract void BeforeExecute();
    protected abstract void AfterExecute();
    protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
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
    public async Task ExecuteNodeAsync(CancellationToken cancellationToken)
    {
        BeforeExecute();
        await ExecuteAsync(cancellationToken);
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
}