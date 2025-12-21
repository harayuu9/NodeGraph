namespace NodeGraph.Model;

/// <summary>
/// ノード実行時のコンテキスト。下流ノードの実行を制御し、外部パラメータへのアクセスを提供します。
/// </summary>
public class NodeExecutionContext
{
    public readonly CancellationToken CancellationToken;

    private readonly IReadOnlyDictionary<string, object?> _parameters;

    public NodeExecutionContext(
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        CancellationToken = cancellationToken;
        _parameters = parameters ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// 指定された名前のパラメータを取得します。
    /// パラメータが存在しないか変換できない場合はdefault(T)を返します。
    /// </summary>
    public T? GetParameter<T>(string name)
    {
        if (_parameters.TryGetValue(name, out var value) && value != null)
        {
            if (value is T typed)
                return typed;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// 指定された名前のパラメータの取得を試みます。
    /// </summary>
    public bool TryGetParameter<T>(string name, out T? value)
    {
        if (_parameters.TryGetValue(name, out var objValue) && objValue != null)
        {
            if (objValue is T typed)
            {
                value = typed;
                return true;
            }

            try
            {
                value = (T)Convert.ChangeType(objValue, typeof(T));
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 指定された名前のパラメータが存在するかを確認します。
    /// </summary>
    public bool HasParameter(string name) => _parameters.ContainsKey(name);

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