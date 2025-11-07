namespace NodeGraph.Model;

internal sealed class NullableConverter<T> : IPortTypeConverter<T, T?>
    where T : struct
{
    public T? Convert(T value) => value;
}