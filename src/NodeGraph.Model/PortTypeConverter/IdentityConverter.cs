namespace NodeGraph.Model;

internal sealed class IdentityConverter<T> : IPortTypeConverter<T, T>
{
    public T Convert(T value) => value;
}