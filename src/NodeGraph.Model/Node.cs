namespace NodeGraph.Model;

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