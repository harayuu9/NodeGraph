namespace NodeGraph.Model;

internal sealed class FuncConverter<TFrom, TTo> : IPortTypeConverter<TFrom, TTo>
{
    private readonly Func<TFrom, TTo> _func;

    public FuncConverter(Func<TFrom, TTo> func)
    {
        _func = func;
    }

    public TTo Convert(TFrom value)
    {
        return _func(value);
    }
}