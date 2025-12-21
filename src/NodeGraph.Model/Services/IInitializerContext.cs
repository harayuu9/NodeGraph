namespace NodeGraph.Model.Services;

/// <summary>
/// Initializerに渡されるコンテキスト。サービス登録とパラメータ取得の両方を提供します。
/// </summary>
public interface IInitializerContext
{
    /// <summary>
    /// サービスインスタンスを登録します。既存の登録がある場合は上書きします。
    /// </summary>
    void Register<T>(T instance) where T : class;

    /// <summary>
    /// インターフェース型に対して実装インスタンスを登録します。
    /// </summary>
    void Register<TInterface, TImplementation>(TImplementation instance)
        where TInterface : class
        where TImplementation : class, TInterface;

    /// <summary>
    /// サービスインスタンスの登録を試みます。既存の登録がある場合は登録しません。
    /// </summary>
    /// <returns>登録に成功した場合はtrue</returns>
    bool TryRegister<T>(T instance) where T : class;

    /// <summary>
    /// 指定した型のサービスが登録されているかを確認します。
    /// </summary>
    bool IsRegistered<T>() where T : class;

    /// <summary>
    /// 指定された名前のパラメータを取得します。
    /// パラメータが存在しないか変換できない場合はdefault(T)を返します。
    /// </summary>
    T? GetParameter<T>(string name);

    /// <summary>
    /// 指定された名前のパラメータの取得を試みます。
    /// </summary>
    bool TryGetParameter<T>(string name, out T? value);

    /// <summary>
    /// 指定された名前のパラメータが存在するかを確認します。
    /// </summary>
    bool HasParameter(string name);

    /// <summary>
    /// キャンセルトークン。
    /// </summary>
    CancellationToken CancellationToken { get; }
}
