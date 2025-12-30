using System;
using Avalonia;

namespace NodeGraph.App.Primitives;

/// <summary>
/// 2点間の線分を表す構造体（コネクタ描画用）
/// </summary>
public readonly record struct ConnectorLine(Point Start, Point End)
{
    public ConnectorLine(double startX, double startY, double endX, double endY)
        : this(new Point(startX, startY), new Point(endX, endY))
    {
    }

    public double StartX => Start.X;
    public double StartY => Start.Y;
    public double EndX => End.X;
    public double EndY => End.Y;

    public static ConnectorLine Zero => new(0, 0, 0, 0);

    public double Length => Math.Sqrt(Math.Pow(EndX - StartX, 2) + Math.Pow(EndY - StartY, 2));

    public Vector Direction
    {
        get
        {
            var dx = EndX - StartX;
            var dy = EndY - StartY;
            var length = Length;
            return length > 0 ? new Vector(dx / length, dy / length) : default;
        }
    }

    public static ConnectorLine FromPoints(Point start, Point end)
    {
        return new ConnectorLine(start, end);
    }
}