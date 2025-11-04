namespace NodeGraph.Model;

public abstract class Node : IWithId<NodeId>
{
    public NodeId Id { get; } = new(Guid.NewGuid());
    protected internal InputPort[] InputPorts { get; }
    protected internal OutputPort[] OutputPorts { get; }

    protected Node(int inputPortCount, int outputPortCount)
    {
        InputPorts = new InputPort[inputPortCount];
        OutputPorts = new OutputPort[outputPortCount];
    }
    
    protected abstract void BeforeExecute();
    protected abstract void AfterExecute();
    protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken);
    public void ConnectInput(int inputIndex, Node node, int outputIndex)
    {
        if (inputIndex < 0 || inputIndex >= InputPorts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(inputIndex));
        }
        if (outputIndex < 0 || outputIndex >= node.OutputPorts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(outputIndex));
        }
        
        if (!InputPorts[inputIndex].CanConnect(node.OutputPorts[outputIndex]))
        {
            return;
        }
        
        InputPorts[inputIndex].Connect(node.OutputPorts[outputIndex]);
    }

    public abstract string GetInputPortName(int index);
    public abstract string GetOutputPortName(int index);
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        BeforeExecute();
        await ExecuteCoreAsync(cancellationToken);
        AfterExecute();
    }

    /// <summary>
    /// このノードが持つプロパティ記述子の配列を取得します。
    /// ソースジェネレータによってオーバーライドされます。
    /// </summary>
    /// <returns>プロパティ記述子の配列</returns>
    public virtual PropertyDescriptor[] GetProperties()
    {
        return Array.Empty<PropertyDescriptor>();
    }

    /// <summary>
    /// 指定した名前のプロパティ値を取得します。
    /// </summary>
    /// <param name="name">プロパティ名</param>
    /// <returns>プロパティ値、存在しない場合はnull</returns>
    public object? GetPropertyValue(string name)
    {
        var property = Array.Find(GetProperties(), p => p.Name == name);
        return property?.Getter(this);
    }

    /// <summary>
    /// 指定した名前のプロパティ値を設定します。
    /// </summary>
    /// <param name="name">プロパティ名</param>
    /// <param name="value">設定する値</param>
    public void SetPropertyValue(string name, object? value)
    {
        var property = Array.Find(GetProperties(), p => p.Name == name);
        property?.Setter(this, value);
    }
}