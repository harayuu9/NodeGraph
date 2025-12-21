using System.Text;
using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

/// <summary>
/// OpenAI Function Calling形式のJSONスキーマを構築
/// </summary>
public static class JsonSchemaBuilder
{
    /// <summary>
    /// 型からJSONスキーマ文字列を生成
    /// </summary>
    public static SchemaResult? Build(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        List<JsonPropertyInfo> properties,
        Compilation compilation,
        bool strictSchema)
    {
        var visitedTypes = new HashSet<string>();
        var schemaJson = BuildObjectSchema(context, typeSymbol, properties, compilation, visitedTypes, strictSchema, 0);

        if (schemaJson == null) return null;

        return new SchemaResult(schemaJson);
    }

    private static string? BuildObjectSchema(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        List<JsonPropertyInfo> properties,
        Compilation compilation,
        HashSet<string> visitedTypes,
        bool strictSchema,
        int depth)
    {
        // 循環参照チェック
        var typeName = typeSymbol.ToDisplayString();
        if (visitedTypes.Contains(typeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CircularReference,
                typeSymbol.Locations.FirstOrDefault(),
                typeName));
            return null;
        }

        // 深すぎるネストを防止
        if (depth > 10)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.NestingTooDeep,
                typeSymbol.Locations.FirstOrDefault(),
                typeName));
            return null;
        }

        visitedTypes.Add(typeName);

        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":\"object\",");
        sb.Append("\"properties\":{");

        var requiredProps = new List<string>();
        var isFirst = true;

        foreach (var prop in properties)
        {
            if (!isFirst) sb.Append(",");
            isFirst = false;

            var jsonPropName = prop.JsonName;
            sb.Append($"\"{jsonPropName}\":");

            var schemaType = JsonTypeMapper.MapType(prop.TypeSymbol, compilation);
            if (schemaType == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.UnsupportedType,
                    prop.Location,
                    prop.Name,
                    prop.TypeSymbol.ToDisplayString()));
                visitedTypes.Remove(typeName);
                return null;
            }

            var propSchema = BuildTypeSchema(context, schemaType, compilation, visitedTypes, strictSchema, depth + 1, prop.Description);
            if (propSchema == null)
            {
                visitedTypes.Remove(typeName);
                return null;
            }

            sb.Append(propSchema);

            // requiredに追加（nullableでない場合）
            if (prop.IsRequired && !schemaType.IsNullable)
            {
                requiredProps.Add(jsonPropName);
            }
        }

        sb.Append("}");

        // required配列
        if (requiredProps.Count > 0)
        {
            sb.Append(",\"required\":[");
            sb.Append(string.Join(",", requiredProps.Select(p => $"\"{p}\"")));
            sb.Append("]");
        }

        // additionalProperties
        if (strictSchema)
        {
            sb.Append(",\"additionalProperties\":false");
        }

        sb.Append("}");

        visitedTypes.Remove(typeName);
        return sb.ToString();
    }

    private static string? BuildTypeSchema(
        SourceProductionContext context,
        JsonSchemaType schemaType,
        Compilation compilation,
        HashSet<string> visitedTypes,
        bool strictSchema,
        int depth,
        string? description = null)
    {
        var sb = new StringBuilder();
        sb.Append("{");

        var hasContent = false;

        // description
        if (!string.IsNullOrEmpty(description))
        {
            sb.Append($"\"description\":\"{EscapeJsonString(description!)}\"");
            hasContent = true;
        }

        // type
        if (hasContent) sb.Append(",");
        sb.Append($"\"type\":\"{schemaType.Type}\"");

        // format
        if (!string.IsNullOrEmpty(schemaType.Format))
        {
            sb.Append($",\"format\":\"{schemaType.Format}\"");
        }

        // enum values
        if (schemaType.EnumValues is { Length: > 0 })
        {
            sb.Append(",\"enum\":[");
            sb.Append(string.Join(",", schemaType.EnumValues.Select(v => $"\"{v}\"")));
            sb.Append("]");
        }

        // array items
        if (schemaType.Items != null)
        {
            var itemsSchema = BuildTypeSchema(context, schemaType.Items, compilation, visitedTypes, strictSchema, depth, null);
            if (itemsSchema == null) return null;
            sb.Append($",\"items\":{itemsSchema}");
        }

        // additionalProperties (for Dictionary)
        if (schemaType.AdditionalProperties != null)
        {
            var additionalSchema = BuildTypeSchema(context, schemaType.AdditionalProperties, compilation, visitedTypes, strictSchema, depth, null);
            if (additionalSchema == null) return null;
            sb.Append($",\"additionalProperties\":{additionalSchema}");
        }

        // nested object
        if (schemaType.ObjectTypeSymbol != null)
        {
            var nestedType = schemaType.ObjectTypeSymbol as INamedTypeSymbol;
            if (nestedType != null)
            {
                var nestedProperties = ExtractProperties(nestedType, compilation);
                var nestedSchema = BuildObjectSchema(context, nestedType, nestedProperties, compilation, visitedTypes, strictSchema, depth);
                if (nestedSchema == null) return null;

                // typeを除いた残りの部分を追加
                // nestedSchemaは {"type":"object",...} の形式なので、properties以降を取り出す
                var propertiesStart = nestedSchema.IndexOf("\"properties\"");
                if (propertiesStart > 0)
                {
                    sb.Append(",");
                    sb.Append(nestedSchema.Substring(propertiesStart, nestedSchema.Length - propertiesStart - 1));
                }
            }
        }

        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// 型からプロパティ情報を抽出
    /// </summary>
    public static List<JsonPropertyInfo> ExtractProperties(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var jsonPropertyAttr = compilation.GetTypeByMetadataName("NodeGraph.Model.JsonPropertyAttribute");
        var properties = new List<JsonPropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop) continue;
            if (prop.DeclaredAccessibility != Accessibility.Public) continue;
            if (prop.GetMethod == null || prop.SetMethod == null) continue;
            if (prop.IsStatic || prop.IsIndexer) continue;

            // JsonPropertyAttributeから情報を取得
            string? jsonName = null;
            string? description = null;
            bool required = true;

            if (jsonPropertyAttr != null)
            {
                var attr = prop.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, jsonPropertyAttr));

                if (attr != null)
                {
                    foreach (var arg in attr.NamedArguments)
                    {
                        switch (arg.Key)
                        {
                            case "Name" when arg.Value.Value is string n:
                                jsonName = n;
                                break;
                            case "Description" when arg.Value.Value is string d:
                                description = d;
                                break;
                            case "Required" when arg.Value.Value is bool r:
                                required = r;
                                break;
                        }
                    }
                }
            }

            // デフォルトのJSON名はcamelCase
            jsonName ??= ToCamelCase(prop.Name);

            properties.Add(new JsonPropertyInfo(
                prop.Name,
                jsonName,
                prop.Type,
                description,
                required,
                prop.Locations.FirstOrDefault()));
        }

        return properties;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

/// <summary>
/// プロパティ情報
/// </summary>
public record JsonPropertyInfo(
    string Name,
    string JsonName,
    ITypeSymbol TypeSymbol,
    string? Description,
    bool IsRequired,
    Location? Location
);

/// <summary>
/// スキーマ生成結果
/// </summary>
public record SchemaResult(string SchemaJson);
