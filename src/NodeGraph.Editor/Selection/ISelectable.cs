using Avalonia;

namespace NodeGraph.Editor.Selection;

/// <summary>
/// 選択可能なオブジェクトを表すインターフェース
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// このオブジェクトの一意な識別子
    /// </summary>
    object SelectionId { get; }
}

public interface IPositionable
{
    public double X { get; set; }
    public double Y { get; set; }
}

public static class PositionExtensions
{
    public static Point Point(this IPositionable position)
    {
        return new Point(position.X, position.Y);
    }
}

public interface IRectangular : IPositionable
{
    public double Width { get; set; }
    public double Height { get; set; }
}