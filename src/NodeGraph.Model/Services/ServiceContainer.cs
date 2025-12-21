using System.Collections.Concurrent;

namespace NodeGraph.Model.Services;

/// <summary>
/// シンプルなサービスコンテナ実装。スレッドセーフです。
/// </summary>
public sealed class ServiceContainer : INodeServiceProvider
{
    private readonly ConcurrentDictionary<Type, object> _services = new();

    /// <summary>
    /// サービスインスタンスを登録します。既存の登録がある場合は上書きします。
    /// </summary>
    public void Register<T>(T instance) where T : class
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        _services[typeof(T)] = instance;
    }

    /// <summary>
    /// インターフェース型に対して実装インスタンスを登録します。
    /// </summary>
    public void Register<TInterface, TImplementation>(TImplementation instance)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        _services[typeof(TInterface)] = instance;
    }

    /// <summary>
    /// サービスインスタンスの登録を試みます。既存の登録がある場合は登録しません。
    /// </summary>
    /// <returns>登録に成功した場合はtrue</returns>
    public bool TryRegister<T>(T instance) where T : class
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        return _services.TryAdd(typeof(T), instance);
    }

    /// <summary>
    /// 指定した型のサービスが登録されているかを確認します。
    /// </summary>
    public bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <inheritdoc />
    public T? GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
    }

    /// <inheritdoc />
    public bool TryGetService<T>(out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            service = (T)obj;
            return true;
        }

        service = null;
        return false;
    }

    /// <inheritdoc />
    public T GetRequiredService<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }

        throw new ServiceNotFoundException(typeof(T));
    }
}
