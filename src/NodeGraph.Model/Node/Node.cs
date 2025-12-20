using System.Reflection;

namespace NodeGraph.Model;

/// <summary>
/// 実行フローポートの型マーカー
/// </summary>
public readonly struct ExecutionPort;

/// <summary>
/// 全てのノードの基底クラス。データポートと実行フローポートを持ちます。
/// </summary>
public abstract class Node : IWithId<NodeId>
{
    public NodeId Id { get; } = new(Guid.NewGuid());
    protected internal InputPort[] InputPorts { get; }
    protected internal OutputPort[] OutputPorts { get; }
    protected internal ExecInPort[] ExecInPorts { get; }
    protected internal ExecOutPort[] ExecOutPorts { get; }

    public bool HasExecIn => ExecInPorts.Length > 0;
    public bool HasExecOut => ExecOutPorts.Length > 0;
    public bool HasExec => HasExecIn || HasExecOut;

    
    protected Node(int inputPortCount, int outputPortCount, int execInPortCount, int execOutPortCount)
    {
        InputPorts = new InputPort[inputPortCount];
        OutputPorts = new OutputPort[outputPortCount];
        ExecInPorts = new ExecInPort[execInPortCount];
        ExecOutPorts = new ExecOutPort[execOutPortCount];
    }

    protected Node(NodeId nodeId, PortId[] inputPortIds, PortId[] outputPortIds, PortId[] execInPortIds, PortId[] execOutPortIds)
    {
        Id = nodeId;
        InputPorts = new InputPort[inputPortIds.Length];
        OutputPorts = new OutputPort[outputPortIds.Length];
        ExecInPorts = new ExecInPort[execInPortIds.Length];
        ExecOutPorts = new ExecOutPort[execOutPortIds.Length];
    }

    public abstract string GetExecInPortName(int index);
    public abstract string GetExecOutPortName(int index);
    
    protected abstract void BeforeExecute();
    protected abstract void AfterExecute();
    protected virtual Task ExecuteCoreAsync(NodeExecutionContext context) => Task.CompletedTask;
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
    
    public async Task ExecuteAsync(NodeExecutionContext context)
    {
        BeforeExecute();
        await ExecuteCoreAsync(context);
        AfterExecute();
    }

    /// <summary>
    /// 出力ポートの値をフラッシュします。
    /// ExecuteOutAsync呼び出し時に自動的に呼ばれますが、手動で呼ぶこともできます。
    /// </summary>
    public void FlushOutputs() => AfterExecute();

    /// <summary>
    /// このノードが持つプロパティ記述子の配列を取得します。
    /// ソースジェネレータによってオーバーライドされます。
    /// </summary>
    /// <returns>プロパティ記述子の配列</returns>
    public virtual PropertyDescriptor[] GetProperties() => [];

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

    public string GetDisplayName()
    {
        var type = GetType();
        var nodeAttribute = type.GetCustomAttribute<NodeAttribute>();
        return nodeAttribute?.DisplayName ?? type.Name;
    }
    
    public string GetDirectory()
    {
        var type = GetType();
        var nodeAttribute = type.GetCustomAttribute<NodeAttribute>();
        return nodeAttribute?.Directory ?? "Other";
    }
}