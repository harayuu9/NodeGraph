using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

/// <summary>
/// C#型からJSONスキーマ型への変換を行うマッパー
/// </summary>
public static class JsonTypeMapper
{
    /// <summary>
    /// C#型をJSONスキーマ型に変換
    /// </summary>
    public static JsonSchemaType? MapType(ITypeSymbol typeSymbol, Compilation compilation)
    {
        // Nullable<T>の場合は内部の型を取得
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var innerType = namedType.TypeArguments[0];
            var innerResult = MapType(innerType, compilation);
            if (innerResult != null)
            {
                return innerResult with { IsNullable = true };
            }
            return null;
        }

        // 参照型のnullable（T?）
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            var nonNullableType = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            var innerResult = MapType(nonNullableType, compilation);
            if (innerResult != null)
            {
                return innerResult with { IsNullable = true };
            }
            return null;
        }

        // プリミティブ型のマッピング
        var result = MapPrimitiveType(typeSymbol);
        if (result != null) return result;

        // 配列型
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            var elementResult = MapType(arrayType.ElementType, compilation);
            if (elementResult != null)
            {
                return new JsonSchemaType("array", Items: elementResult);
            }
            return null;
        }

        // ジェネリック型（List<T>, IList<T>, IEnumerable<T>, etc.）
        if (typeSymbol is INamedTypeSymbol genericType && genericType.IsGenericType)
        {
            var genericDef = genericType.OriginalDefinition.ToDisplayString();

            // List<T>, IList<T>, ICollection<T>, IEnumerable<T>
            if (IsListLikeType(genericDef))
            {
                var elementType = genericType.TypeArguments[0];
                var elementResult = MapType(elementType, compilation);
                if (elementResult != null)
                {
                    return new JsonSchemaType("array", Items: elementResult);
                }
                return null;
            }

            // Dictionary<string, T>
            if (IsDictionaryType(genericDef) && genericType.TypeArguments.Length == 2)
            {
                var keyType = genericType.TypeArguments[0];
                if (keyType.SpecialType == SpecialType.System_String)
                {
                    var valueType = genericType.TypeArguments[1];
                    var valueResult = MapType(valueType, compilation);
                    if (valueResult != null)
                    {
                        return new JsonSchemaType("object", AdditionalProperties: valueResult);
                    }
                }
                return null;
            }
        }

        // enum型
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var enumMembers = typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .Select(f => f.Name)
                .ToArray();

            return new JsonSchemaType("string", EnumValues: enumMembers);
        }

        // [JsonNode]属性を持つクラス
        var jsonNodeAttr = compilation.GetTypeByMetadataName("NodeGraph.Model.JsonNodeAttribute");
        if (jsonNodeAttr != null && typeSymbol.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, jsonNodeAttr)))
        {
            return new JsonSchemaType("object", ObjectTypeSymbol: typeSymbol);
        }

        // publicプロパティを持つクラス/構造体（[JsonNode]なしでも対応）
        if (typeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct &&
            typeSymbol.SpecialType == SpecialType.None)
        {
            var hasPublicProperties = typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.DeclaredAccessibility == Accessibility.Public &&
                          p.GetMethod != null && p.SetMethod != null);

            if (hasPublicProperties)
            {
                return new JsonSchemaType("object", ObjectTypeSymbol: typeSymbol);
            }
        }

        // サポートされていない型
        return null;
    }

    private static JsonSchemaType? MapPrimitiveType(ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType switch
        {
            SpecialType.System_String => new JsonSchemaType("string"),
            SpecialType.System_Boolean => new JsonSchemaType("boolean"),
            SpecialType.System_Byte => new JsonSchemaType("integer"),
            SpecialType.System_SByte => new JsonSchemaType("integer"),
            SpecialType.System_Int16 => new JsonSchemaType("integer"),
            SpecialType.System_UInt16 => new JsonSchemaType("integer"),
            SpecialType.System_Int32 => new JsonSchemaType("integer"),
            SpecialType.System_UInt32 => new JsonSchemaType("integer"),
            SpecialType.System_Int64 => new JsonSchemaType("integer"),
            SpecialType.System_UInt64 => new JsonSchemaType("integer"),
            SpecialType.System_Single => new JsonSchemaType("number"),
            SpecialType.System_Double => new JsonSchemaType("number"),
            SpecialType.System_Decimal => new JsonSchemaType("number"),
            _ => MapWellKnownType(typeSymbol)
        };
    }

    private static JsonSchemaType? MapWellKnownType(ITypeSymbol typeSymbol)
    {
        var fullName = typeSymbol.ToDisplayString();
        return fullName switch
        {
            "System.DateTime" => new JsonSchemaType("string", Format: "date-time"),
            "System.DateTimeOffset" => new JsonSchemaType("string", Format: "date-time"),
            "System.DateOnly" => new JsonSchemaType("string", Format: "date"),
            "System.TimeOnly" => new JsonSchemaType("string", Format: "time"),
            "System.TimeSpan" => new JsonSchemaType("string", Format: "duration"),
            "System.Guid" => new JsonSchemaType("string", Format: "uuid"),
            "System.Uri" => new JsonSchemaType("string", Format: "uri"),
            _ => null
        };
    }

    private static bool IsListLikeType(string genericDef)
    {
        return genericDef is
            "System.Collections.Generic.List<T>" or
            "System.Collections.Generic.IList<T>" or
            "System.Collections.Generic.ICollection<T>" or
            "System.Collections.Generic.IEnumerable<T>" or
            "System.Collections.Generic.IReadOnlyList<T>" or
            "System.Collections.Generic.IReadOnlyCollection<T>";
    }

    private static bool IsDictionaryType(string genericDef)
    {
        return genericDef is
            "System.Collections.Generic.Dictionary<TKey, TValue>" or
            "System.Collections.Generic.IDictionary<TKey, TValue>" or
            "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>";
    }

    /// <summary>
    /// 型がサポートされているかどうかを確認
    /// </summary>
    public static bool IsSupported(ITypeSymbol typeSymbol, Compilation compilation)
    {
        return MapType(typeSymbol, compilation) != null;
    }

    /// <summary>
    /// サポートされていない型の理由を返す
    /// </summary>
    public static string? GetUnsupportedReason(ITypeSymbol typeSymbol)
    {
        var fullName = typeSymbol.ToDisplayString();

        if (typeSymbol.SpecialType == SpecialType.System_Object)
            return "object型はJSONスキーマで表現できません";

        if (fullName == "dynamic")
            return "dynamic型はJSONスキーマで表現できません";

        if (typeSymbol.TypeKind == TypeKind.Delegate)
            return "デリゲート型はJSONスキーマで表現できません";

        if (typeSymbol.TypeKind == TypeKind.Pointer)
            return "ポインタ型はJSONスキーマで表現できません";

        if (fullName.StartsWith("System.Span<") || fullName.StartsWith("System.ReadOnlySpan<"))
            return "Span型はJSONスキーマで表現できません";

        if (fullName.StartsWith("System.Memory<") || fullName.StartsWith("System.ReadOnlyMemory<"))
            return "Memory型はJSONスキーマで表現できません";

        return $"型 '{fullName}' はJSONスキーマでサポートされていません";
    }
}

/// <summary>
/// JSONスキーマ型情報
/// </summary>
public record JsonSchemaType(
    string Type,
    string? Format = null,
    string[]? EnumValues = null,
    JsonSchemaType? Items = null,
    JsonSchemaType? AdditionalProperties = null,
    ITypeSymbol? ObjectTypeSymbol = null,
    bool IsNullable = false
);
