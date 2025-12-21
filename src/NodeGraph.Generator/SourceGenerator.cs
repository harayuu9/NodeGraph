using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NodeGraph.Generator;

/// <summary>
/// NodeGraphのソースジェネレータ
/// [Node]属性と[JsonNode]属性を処理
/// </summary>
[Generator(LanguageNames.CSharp)]
public class SourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [Node]属性を処理
        var nodeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NodeGraph.Model.NodeAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (cont, _) => cont);

        context.RegisterSourceOutput(nodeProvider, EmitNode);

        // [JsonNode]属性を処理
        var jsonNodeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NodeGraph.Model.JsonNodeAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (cont, _) => cont);

        context.RegisterSourceOutput(jsonNodeProvider, EmitJsonNode);
    }

    /// <summary>
    /// [Node]属性の処理
    /// </summary>
    private static void EmitNode(SourceProductionContext spc, GeneratorAttributeSyntaxContext source)
    {
        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;
        var reference = new ReferenceSymbols(source.SemanticModel.Compilation);

        // NodeAttributeから情報を取得
        var nodeAttr = source.Attributes.FirstOrDefault();
        bool hasExecIn = true;
        bool hasExecOut = true;
        string[] execOutNames = ["Out"];

        // NodeAttributeからdisplayNameとdirectoryを取得
        string? displayName = null;
        string? directory = null;

        if (nodeAttr != null)
        {
            // コンストラクタ引数からdisplayNameとdirectoryを取得
            if (nodeAttr.ConstructorArguments.Length > 0 && nodeAttr.ConstructorArguments[0].Value is string dn)
            {
                displayName = dn;
            }
            if (nodeAttr.ConstructorArguments.Length > 1 && nodeAttr.ConstructorArguments[1].Value is string dir)
            {
                directory = dir;
            }

            var hasExecInArg = nodeAttr.NamedArguments.FirstOrDefault(x => x.Key == "HasExecIn");
            if (hasExecInArg.Value.Value is bool hasExecInValue)
            {
                hasExecIn = hasExecInValue;
            }

            var hasExecOutArg = nodeAttr.NamedArguments.FirstOrDefault(x => x.Key == "HasExecOut");
            if (hasExecOutArg.Value.Value is bool hasExecOutValue)
            {
                hasExecOut = hasExecOutValue;
            }

            if (nodeAttr.ConstructorArguments.Length > 2)
            {
                var execOutNamesArg = nodeAttr.ConstructorArguments[2];
                if (!execOutNamesArg.IsNull && execOutNamesArg.Kind == TypedConstantKind.Array)
                {
                    var names = execOutNamesArg.Values.Select(v => v.Value?.ToString() ?? "").ToArray();
                    if (names.Length > 0)
                    {
                        execOutNames = names;
                    }
                }
            }
        }

        NodeEmitter.Emit(spc, typeSymbol, typeNode, reference, hasExecIn, hasExecOut, execOutNames, displayName, directory);
    }

    /// <summary>
    /// [JsonNode]属性の処理
    /// </summary>
    private static void EmitJsonNode(SourceProductionContext spc, GeneratorAttributeSyntaxContext source)
    {
        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;
        var compilation = source.SemanticModel.Compilation;

        // ネストされたクラスは非対応
        if (typeNode.Parent is TypeDeclarationSyntax)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.NestedNotAllowed,
                typeNode.Identifier.GetLocation(),
                typeSymbol.Name));
            return;
        }

        // パラメータなしコンストラクタのチェック
        var hasParameterlessConstructor = typeSymbol.InstanceConstructors
            .Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);

        if (!hasParameterlessConstructor && !typeSymbol.IsValueType)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MissingParameterlessConstructor,
                typeNode.Identifier.GetLocation(),
                typeSymbol.Name));
            return;
        }

        // 属性情報を取得
        var nodeAttr = source.Attributes.FirstOrDefault();
        string? displayName = null;
        string directory = "Json";
        bool strictSchema = true;

        if (nodeAttr != null)
        {
            foreach (var arg in nodeAttr.NamedArguments)
            {
                switch (arg.Key)
                {
                    case "DisplayName" when arg.Value.Value is string dn:
                        displayName = dn;
                        break;
                    case "Directory" when arg.Value.Value is string dir:
                        directory = dir;
                        break;
                    case "StrictSchema" when arg.Value.Value is bool ss:
                        strictSchema = ss;
                        break;
                }
            }
        }

        displayName ??= typeSymbol.Name;

        // プロパティ情報を抽出
        var properties = JsonSchemaBuilder.ExtractProperties(typeSymbol, compilation);

        if (properties.Count == 0)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.NoPublicProperties,
                typeNode.Identifier.GetLocation(),
                typeSymbol.Name));
            return;
        }

        // 全プロパティの型をチェック
        foreach (var prop in properties)
        {
            if (!JsonTypeMapper.IsSupported(prop.TypeSymbol, compilation))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.UnsupportedType,
                    prop.Location,
                    prop.Name,
                    prop.TypeSymbol.ToDisplayString()));
                return;
            }
        }

        // スキーマを生成
        var schemaResult = JsonSchemaBuilder.Build(spc, typeSymbol, properties, compilation, strictSchema);
        if (schemaResult == null) return;

        // 3種類のノードを生成
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? "" : typeSymbol.ContainingNamespace.ToDisplayString();
        var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        JsonNodeEmitter.EmitDeserializeNode(spc, typeSymbol, ns, displayName, directory, properties, fullTypeName);
        JsonNodeEmitter.EmitSerializeNode(spc, typeSymbol, ns, displayName, directory, properties, fullTypeName);
        JsonNodeEmitter.EmitSchemaNode(spc, typeSymbol, ns, displayName, directory, schemaResult.SchemaJson);
    }
}
