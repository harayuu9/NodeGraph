namespace NodeGraph.Model;

/// <summary>
/// 実行フロー入力ポート。複数接続を許可します（複数のExecOutから実行フローを受け取れます）。
/// </summary>
public class ExecInPort : MultiConnectPort
{
    public ExecInPort(Node parent) : base(parent) { }
    public ExecInPort(Node parent, PortId id) : base(parent, id) { }

    public override Type PortType => typeof(ExecutionPort);
    public override string ValueString => "Exec";

    public override bool CanConnect(Port other)
    {
        // Execポートは同じノード内の接続を許可（ループサポート）
        // if (Parent == other.Parent) return false;  // この制限を削除
        if (other is not ExecOutPort) return false;
        return true;
    }
}
