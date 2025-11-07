using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NodeGraph.Editor.Converters;

/// <summary>
/// Converts port type names to colors for visual differentiation.
/// Primitive types get predefined colors, other types get consistent hash-based colors.
/// </summary>
public class TypeToColorConverter : IValueConverter
{
    private static readonly Dictionary<string, Color> PrimitiveTypeColors = new()
    {
        { "Single", Color.Parse("#007ACC") },    // float - blue (current default)
        { "Int32", Color.Parse("#FF6B6B") },     // int - coral red
        { "String", Color.Parse("#4ECB71") },    // string - green
        { "Boolean", Color.Parse("#FFA500") },   // bool - orange
        { "Double", Color.Parse("#9B59B6") },    // double - purple
        { "Int64", Color.Parse("#E74C3C") },     // long - red
        { "Decimal", Color.Parse("#1ABC9C") },   // decimal - teal
        { "Byte", Color.Parse("#F39C12") },      // byte - yellow-orange
        { "Int16", Color.Parse("#E67E22") },     // short - orange-red
        { "UInt32", Color.Parse("#C0392B") },    // uint - dark red
        { "UInt64", Color.Parse("#8E44AD") },    // ulong - dark purple
        { "Char", Color.Parse("#16A085") },      // char - green-teal
        { "SByte", Color.Parse("#D68910") },     // sbyte - golden
        { "UInt16", Color.Parse("#D35400") },    // ushort - pumpkin
    };

    /// <summary>
    /// Converts a type name string to a SolidColorBrush.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string typeName)
            return new SolidColorBrush(Color.Parse("#007ACC")); // Default blue

        // Check if it's a primitive type with predefined color
        if (PrimitiveTypeColors.TryGetValue(typeName, out var color))
        {
            return new SolidColorBrush(color);
        }

        // Generate consistent color based on type name hash
        // This ensures the same type always gets the same color across sessions
        var hash = typeName.GetHashCode();
        var hue = Math.Abs(hash) % 360;

        // Use moderate saturation and brightness for readability
        var generatedColor = ColorFromHSV(hue, 0.7, 0.8);

        return new SolidColorBrush(generatedColor);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("TypeToColorConverter does not support ConvertBack.");
    }

    /// <summary>
    /// Converts HSV color values to RGB color.
    /// </summary>
    /// <param name="hue">Hue (0-360)</param>
    /// <param name="saturation">Saturation (0-1)</param>
    /// <param name="value">Value/Brightness (0-1)</param>
    private static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = System.Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        int v = System.Convert.ToInt32(value);
        int p = System.Convert.ToInt32(value * (1 - saturation));
        int q = System.Convert.ToInt32(value * (1 - f * saturation));
        int t = System.Convert.ToInt32(value * (1 - (1 - f) * saturation));

        return hi switch
        {
            0 => Color.FromRgb((byte)v, (byte)t, (byte)p),
            1 => Color.FromRgb((byte)q, (byte)v, (byte)p),
            2 => Color.FromRgb((byte)p, (byte)v, (byte)t),
            3 => Color.FromRgb((byte)p, (byte)q, (byte)v),
            4 => Color.FromRgb((byte)t, (byte)p, (byte)v),
            _ => Color.FromRgb((byte)v, (byte)p, (byte)q),
        };
    }
}
