namespace NodeGraph.Model;

/// <summary>
/// 実行フロー出力ポート。単一接続のみ可能です（1つの出力から1つの入力へ）。
/// </summary>
public class ExecOutPort : SingleConnectPort
{
    public ExecOutPort(Node parent) : base(parent) { }
    public ExecOutPort(Node parent, PortId id) : base(parent, id) { }

    public override Type PortType => typeof(ExecutionPort);
    public override string ValueString => "Exec";

    public override bool CanConnect(Port other)
    {
        if (other is not ExecInPort) return false;
        return true;
    }

    /// <summary>
    /// 接続されているExecInPortのノードに実行フローを送信します。
    /// </summary>
    internal IEnumerable<Node> GetExecutionTargets()
    {
        if (ConnectedPort is ExecInPort execInPort)
        {
            yield return execInPort.Parent;
        }
    }
}
