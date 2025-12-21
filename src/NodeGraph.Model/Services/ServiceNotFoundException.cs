namespace NodeGraph.Model.Services;

/// <summary>
/// 要求されたサービスが見つからない場合にスローされる例外。
/// </summary>
public class ServiceNotFoundException : Exception
{
    /// <summary>
    /// 見つからなかったサービスの型。
    /// </summary>
    public Type ServiceType { get; }

    public ServiceNotFoundException(Type serviceType)
        : base($"Service of type '{serviceType.FullName}' is not registered.")
    {
        ServiceType = serviceType;
    }

    public ServiceNotFoundException(Type serviceType, string message)
        : base(message)
    {
        ServiceType = serviceType;
    }

    public ServiceNotFoundException(Type serviceType, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceType = serviceType;
    }
}
