namespace NodeGraph.Model.Services;

/// <summary>
/// サービスコンテナの初期化を行うインターフェース。
/// 実装クラスは自動検出され、GraphExecutor.ExecuteAsync()時に呼び出されます。
/// </summary>
public interface INodeContextInitializer
{
    /// <summary>
    /// 初期化の優先順位。小さい値ほど先に実行されます。
    /// </summary>
    int Order => 0;

    /// <summary>
    /// サービスを登録します。
    /// </summary>
    /// <param name="context">初期化コンテキスト（サービス登録とパラメータ取得が可能）</param>
    void Initialize(IInitializerContext context);
}
