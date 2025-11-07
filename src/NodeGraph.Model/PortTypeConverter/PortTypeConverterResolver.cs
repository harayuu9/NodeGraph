using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NodeGraph.Model;

public static class PortTypeConverterProvider
{
    private static readonly ConcurrentDictionary<(Type From, Type To), object> Converters = new();

    static PortTypeConverterProvider()
    {
        RegisterWellKnownTypeConverters();
    }

    public static void Register<TFrom, TTo>(IPortTypeConverter<TFrom, TTo> converter)
    {
        Converters[(typeof(TFrom), typeof(TTo))] = converter;
        Cache<TFrom, TTo>.Converter = converter;
    }

    public static void Register<TFrom, TTo>(Func<TFrom, TTo> converterFunc)
    {
        Register(new FuncConverter<TFrom, TTo>(converterFunc));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IPortTypeConverter<TFrom, TTo>? GetConverter<TFrom, TTo>()
    {
        return Cache<TFrom, TTo>.Converter;
    }

    private static object? GetConverter(Type fromType, Type toType)
    {
        if (Converters.TryGetValue((fromType, toType), out var converter))
        {
            return converter;
        }

        // Try to create converter dynamically
        converter = TryCreateConverter(fromType, toType);
        if (converter != null)
        {
            Converters[(fromType, toType)] = converter;
        }

        return converter;
    }

    public static bool CanConvert<TFrom, TTo>()
    {
        return GetConverter<TFrom, TTo>() != null;
    }

    public static bool CanConvert(Type fromType, Type toType)
    {
        if (fromType == toType) return true;
        if (toType.IsAssignableFrom(fromType)) return true;

        return GetConverter(fromType, toType) != null;
    }

    private static object? TryCreateConverter(Type fromType, Type toType)
    {
        // Try identity converter
        if (fromType == toType)
        {
            var identityType = typeof(IdentityConverter<>).MakeGenericType(fromType);
            return Activator.CreateInstance(identityType);
        }

        // Try assignable converter (inheritance)
        if (toType.IsAssignableFrom(fromType))
        {
            var assignableType = typeof(AssignableConverter<,>).MakeGenericType(fromType, toType);
            return Activator.CreateInstance(assignableType);
        }

        // Try nullable converter
        if (toType.IsGenericType && toType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(toType)!;
            if (fromType == underlyingType)
            {
                var nullableType = typeof(NullableConverter<>).MakeGenericType(underlyingType);
                return Activator.CreateInstance(nullableType);
            }
        }

        return null;
    }

    private static void RegisterWellKnownTypeConverters()
    {
        // Numeric conversions (following C# implicit conversion rules)

        // byte conversions
        Register<byte, short>(v => v);
        Register<byte, int>(v => v);
        Register<byte, long>(v => v);
        Register<byte, float>(v => v);
        Register<byte, double>(v => v);
        Register<byte, decimal>(v => v);

        // short conversions
        Register<short, int>(v => v);
        Register<short, long>(v => v);
        Register<short, float>(v => v);
        Register<short, double>(v => v);
        Register<short, decimal>(v => v);

        // int conversions
        Register<int, long>(v => v);
        Register<int, float>(v => v);
        Register<int, double>(v => v);
        Register<int, decimal>(v => v);

        // long conversions
        Register<long, float>(v => v);
        Register<long, double>(v => v);
        Register<long, decimal>(v => v);

        // float conversions
        Register<float, double>(v => v);

        // sbyte conversions
        Register<sbyte, short>(v => v);
        Register<sbyte, int>(v => v);
        Register<sbyte, long>(v => v);
        Register<sbyte, float>(v => v);
        Register<sbyte, double>(v => v);
        Register<sbyte, decimal>(v => v);

        // ushort conversions
        Register<ushort, int>(v => v);
        Register<ushort, long>(v => v);
        Register<ushort, float>(v => v);
        Register<ushort, double>(v => v);
        Register<ushort, decimal>(v => v);

        // uint conversions
        Register<uint, long>(v => v);
        Register<uint, float>(v => v);
        Register<uint, double>(v => v);
        Register<uint, decimal>(v => v);

        // ulong conversions
        Register<ulong, float>(v => v);
        Register<ulong, double>(v => v);
        Register<ulong, decimal>(v => v);

        // char conversions
        Register<char, int>(v => v);
        Register<char, long>(v => v);
        Register<char, float>(v => v);
        Register<char, double>(v => v);
        Register<char, decimal>(v => v);
    }

    private static class Cache<TFrom, TTo>
    {
        public static IPortTypeConverter<TFrom, TTo>? Converter;

        static Cache()
        {
            try
            {
                var fromType = typeof(TFrom);
                var toType = typeof(TTo);

                // Try to create converter
                var converter = TryCreateConverter(fromType, toType);
                if (converter is IPortTypeConverter<TFrom, TTo> typedConverter)
                {
                    Converter = typedConverter;
                    Converters[(fromType, toType)] = typedConverter;
                }
            }
            catch
            {
                // If failed, leave Converter as null
            }
        }
    }
}