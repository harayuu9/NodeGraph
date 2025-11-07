namespace NodeGraph.Model;

internal sealed class AssignableConverter<TFrom, TTo> : IPortTypeConverter<TFrom, TTo>
    where TFrom : TTo
{
    public TTo Convert(TFrom value) => value;
}