namespace NodeGraph.Model.Services;

/// <summary>
/// サービスインスタンスを取得するためのインターフェース。
/// </summary>
public interface INodeServiceProvider
{
    /// <summary>
    /// 指定した型のサービスを取得します。登録されていない場合はnullを返します。
    /// </summary>
    T? GetService<T>() where T : class;

    /// <summary>
    /// 指定した型のサービスの取得を試みます。
    /// </summary>
    bool TryGetService<T>(out T? service) where T : class;

    /// <summary>
    /// 指定した型のサービスを取得します。登録されていない場合は例外をスローします。
    /// </summary>
    /// <exception cref="ServiceNotFoundException">サービスが見つからない場合</exception>
    T GetRequiredService<T>() where T : class;
}
