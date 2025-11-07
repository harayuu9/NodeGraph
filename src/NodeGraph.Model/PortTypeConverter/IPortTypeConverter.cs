namespace NodeGraph.Model;

public interface IPortTypeConverter<in TFrom, out TTo>
{
    TTo Convert(TFrom value);
}