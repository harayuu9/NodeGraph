namespace NodeGraph.Model.Services;

/// <summary>
/// IInitializerContextの実装。パラメータ辞書とServiceContainerを保持します。
/// </summary>
public sealed class InitializerContext : IInitializerContext
{
    private readonly IReadOnlyDictionary<string, object?> _parameters;
    private readonly ServiceContainer _serviceContainer;

    public InitializerContext(
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken,
        ServiceContainer serviceContainer)
    {
        _parameters = parameters ?? new Dictionary<string, object?>();
        CancellationToken = cancellationToken;
        _serviceContainer = serviceContainer;
    }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public void Register<T>(T instance) where T : class
    {
        _serviceContainer.Register(instance);
    }

    /// <inheritdoc />
    public void Register<TInterface, TImplementation>(TImplementation instance)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _serviceContainer.Register<TInterface, TImplementation>(instance);
    }

    /// <inheritdoc />
    public bool TryRegister<T>(T instance) where T : class
    {
        return _serviceContainer.TryRegister(instance);
    }

    /// <inheritdoc />
    public bool IsRegistered<T>() where T : class
    {
        return _serviceContainer.IsRegistered<T>();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool HasParameter(string name) => _parameters.ContainsKey(name);
}
