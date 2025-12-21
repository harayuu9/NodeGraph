namespace NodeGraph.Model;

/// <summary>
/// 実行フロー出力ポート。単一接続のみ（1つの出力から1つの入力へ）。
/// </summary>
public class ExecOutPort : SingleConnectPort
{
    public ExecOutPort(Node parent) : base(parent)
    {
    }

    public ExecOutPort(Node parent, PortId id) : base(parent, id)
    {
    }

    public override Type PortType => typeof(ExecutionPort);
    public override string ValueString => "Exec";

    public override bool CanConnect(Port other)
    {
        if (other is not ExecInPort) return false;
        return true;
    }

    /// <summary>
    /// 接続先のノードを返します。接続がない場合はnullを返します。
    /// </summary>
    internal Node? GetExecutionTarget()
    {
        if (ConnectedPort is ExecInPort execInPort) return execInPort.Parent;
        return null;
    }
}