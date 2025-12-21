namespace NodeGraph.Model;

/// <summary>
/// ノード実行時のコンテキスト。下流ノードの実行を制御します。
/// </summary>
public class NodeExecutionContext(CancellationToken cancellationToken)
{
    public readonly CancellationToken CancellationToken = cancellationToken;

    /// <summary>
    /// 下流実行デリゲート（GraphExecutorが設定）
    /// </summary>
    internal Func<Node, int, Task>? ExecuteOutDelegate { get; set; }

    /// <summary>
    /// 現在実行中のノード
    /// </summary>
    internal Node? CurrentNode { get; set; }

    /// <summary>
    /// 指定されたExecOutポートの接続先ノードを実行し、完了を待機します。
    /// 呼び出し時に自動的にFlushOutputs()が呼ばれ、出力値が下流に伝播されます。
    /// </summary>
    /// <param name="index">ExecOutポートのインデックス</param>
    public async Task ExecuteOutAsync(int index)
    {
        if (CurrentNode == null) return;

        // 自動フラッシュ: フィールド値をOutputPortへコピー
        CurrentNode.FlushOutputs();

        // 下流を実行
        if (ExecuteOutDelegate != null) await ExecuteOutDelegate(CurrentNode, index);
    }
}