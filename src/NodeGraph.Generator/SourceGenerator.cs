using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NodeGraph.Generator;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nodeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NodeGraph.Model.NodeAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (cont, _) => cont);
        var executionProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NodeGraph.Model.ExecutionNodeAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (cont, _) => cont);
        
        context.RegisterSourceOutput(nodeProvider, Emit);
        context.RegisterSourceOutput(executionProvider, EmitExecution);
    }

    private static void Emit(SourceProductionContext spc, GeneratorAttributeSyntaxContext source)
    {
        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;
        var reference = new ReferenceSymbols(source.SemanticModel.Compilation);
        Emitter.Emit(spc, typeSymbol, typeNode, reference, false, false, Array.Empty<string>());
    }

    private static void EmitExecution(SourceProductionContext spc, GeneratorAttributeSyntaxContext source)
    {
        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;
        var reference = new ReferenceSymbols(source.SemanticModel.Compilation);

        // ExecutionNodeAttributeから情報を取得
        var executionAttr = source.Attributes.FirstOrDefault();
        bool hasExecIn = true;
        string[] execOutNames = Array.Empty<string>();

        if (executionAttr != null)
        {
            // HasExecInプロパティを取得（名前付き引数）
            var hasExecInArg = executionAttr.NamedArguments.FirstOrDefault(x => x.Key == "HasExecIn");
            if (hasExecInArg.Value.Value is bool hasExecInValue)
            {
                hasExecIn = hasExecInValue;
            }

            // ExecOutNamesを取得（コンストラクタ引数の3番目以降）
            if (executionAttr.ConstructorArguments.Length > 2)
            {
                var execOutNamesArg = executionAttr.ConstructorArguments[2];
                if (!execOutNamesArg.IsNull && execOutNamesArg.Kind == TypedConstantKind.Array)
                {
                    execOutNames = execOutNamesArg.Values.Select(v => v.Value?.ToString() ?? "").ToArray();
                }
            }
        }

        Emitter.Emit(spc, typeSymbol, typeNode, reference, true, hasExecIn, execOutNames);
    }
}

public readonly struct PortData
{
    public readonly string Type;
    public readonly string PortType;
    public readonly string RawName;
    public readonly string Name;

    public PortData(IFieldSymbol fieldSymbol, bool isInput)
    {
        Type = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (isInput)
        {
            PortType = $"global::NodeGraph.Model.InputPort<{Type}>";
        }
        else
        {
            PortType = $"global::NodeGraph.Model.OutputPort<{Type}>";
        }
        RawName = fieldSymbol.Name;
        Name = StringCaseConverter.ToUpperCamelCase(fieldSymbol.Name);
    }

}

public readonly struct ExecPortData
{
    public readonly string Name;

    public ExecPortData(string name)
    {
        Name = name;
    }
}

public readonly struct PropertyData
{
    public readonly string Type;
    public readonly string RawName;
    public readonly string Name;
    public readonly IFieldSymbol FieldSymbol;

    public PropertyData(IFieldSymbol fieldSymbol)
    {
        Type = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        RawName = fieldSymbol.Name;
        Name = StringCaseConverter.ToUpperCamelCase(fieldSymbol.Name);
        FieldSymbol = fieldSymbol;
    }
}

public static class Emitter
{
    public static void Emit(SourceProductionContext context, INamedTypeSymbol typeSymbol, TypeDeclarationSyntax typeNode, ReferenceSymbols reference, bool isExecutionNode, bool hasExecIn, string[] execOutNames)
    {
        if (!IsPartial(typeNode))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, typeNode.Identifier.GetLocation(), typeSymbol.Name));
            return;
        }

        // nested is not allowed
        if (IsNested(typeNode))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NestedNotAllow, typeNode.Identifier.GetLocation(), typeSymbol.Name));
            return;
        }
        
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"{typeSymbol.ContainingNamespace}";
        
        var fullType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace("<", "_")
            .Replace(">", "_");
        
        var inputFields = typeSymbol.GetMembers().OfType<IFieldSymbol>()
            .Where(field => !field.IsStatic && field.ContainsAttribute(reference.InputAttribute))
            .Select(field => new PortData(field, true))
            .ToArray();
        var outputFields = typeSymbol.GetMembers().OfType<IFieldSymbol>()
            .Where(field => !field.IsStatic && field.ContainsAttribute(reference.OutputAttribute))
            .Select(field => new PortData(field, false))
            .ToArray();
        var propertyFields = typeSymbol.GetMembers().OfType<IFieldSymbol>()
            .Where(field => !field.IsStatic && field.ContainsAttribute(reference.PropertyAttribute))
            .Select(field => new PropertyData(field))
            .ToArray();

        // ExecutionNodeの場合、Execポートはパラメータから取得
        var execInCount = isExecutionNode && hasExecIn ? 1 : 0;
        var execOutCount = isExecutionNode ? execOutNames.Length : 0;
        var execOutFields = execOutNames.Select(name => new ExecPortData(name)).ToArray();
        
        var codeGen = new CSharpCodeGenerator(ns);
        if (isExecutionNode)
        {
            codeGen.WriteLine($"partial class {typeSymbol.Name} : global::NodeGraph.Model.ExecutionNode");
        }
        else
        {
            codeGen.WriteLine($"partial class {typeSymbol.Name} : global::NodeGraph.Model.Node");
        }
        
        using (codeGen.Scope())
        {
            // デフォルトコンストラクタ（新規作成用）
            if (isExecutionNode)
            {
                codeGen.WriteLine($"public {typeSymbol.Name}() : base({inputFields.Length}, {outputFields.Length}, {execInCount}, {execOutCount})");
            }
            else
            {
                codeGen.WriteLine($"public {typeSymbol.Name}() : base({inputFields.Length}, {outputFields.Length})");
            }
            using (codeGen.Scope())
            {
                for (var i = 0; i < inputFields.Length; i++)
                {
                    var inputField = inputFields[i];
                    codeGen.WriteLine($"InputPorts[{i}] = new {inputField.PortType}(this, {inputField.RawName});");
                }

                for (var i = 0; i < outputFields.Length; i++)
                {
                    var outputField = outputFields[i];
                    codeGen.WriteLine($"OutputPorts[{i}] = new {outputField.PortType}(this, {outputField.RawName});");
                }

                if (isExecutionNode)
                {
                    for (var i = 0; i < execInCount; i++)
                    {
                        codeGen.WriteLine($"ExecInPorts[{i}] = new global::NodeGraph.Model.ExecInPort(this);");
                    }

                    for (var i = 0; i < execOutCount; i++)
                    {
                        codeGen.WriteLine($"ExecOutPorts[{i}] = new global::NodeGraph.Model.ExecOutPort(this);");
                    }
                }
            }

            codeGen.WriteLine();

            // デシリアライズ用コンストラクタ（NodeIdとPortIdを受け取る）
            if (isExecutionNode)
            {
                codeGen.WriteLine($"public {typeSymbol.Name}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds, global::NodeGraph.Model.PortId[] execInPortIds, global::NodeGraph.Model.PortId[] execOutPortIds) : base(nodeId, inputPortIds, outputPortIds, execInPortIds, execOutPortIds)");
            }
            else
            {
                codeGen.WriteLine($"public {typeSymbol.Name}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds) : base(nodeId, inputPortIds, outputPortIds)");
            }
            using (codeGen.Scope())
            {
                for (var i = 0; i < inputFields.Length; i++)
                {
                    var inputField = inputFields[i];
                    codeGen.WriteLine($"InputPorts[{i}] = new {inputField.PortType}(this, inputPortIds[{i}], {inputField.RawName});");
                }

                for (var i = 0; i < outputFields.Length; i++)
                {
                    var outputField = outputFields[i];
                    codeGen.WriteLine($"OutputPorts[{i}] = new {outputField.PortType}(this, outputPortIds[{i}], {outputField.RawName});");
                }

                if (isExecutionNode)
                {
                    for (var i = 0; i < execInCount; i++)
                    {
                        codeGen.WriteLine($"ExecInPorts[{i}] = new global::NodeGraph.Model.ExecInPort(this, execInPortIds[{i}]);");
                    }

                    for (var i = 0; i < execOutCount; i++)
                    {
                        codeGen.WriteLine($"ExecOutPorts[{i}] = new global::NodeGraph.Model.ExecOutPort(this, execOutPortIds[{i}]);");
                    }
                }
            }
            
            codeGen.WriteLine("protected override void BeforeExecute()");
            using (codeGen.Scope())
            {
                for (var i = 0; i < inputFields.Length; i++)
                {
                    var x = inputFields[i];
                    codeGen.WriteLine($"this.{x.RawName} = (({x.PortType})InputPorts[{i}]).Value;");
                }
            }
            
            codeGen.WriteLine("protected override void AfterExecute()");
            using (codeGen.Scope())
            {
                for (var i = 0; i < outputFields.Length; i++)
                {
                    var x = outputFields[i];
                    codeGen.WriteLine($"(({x.PortType})OutputPorts[{i}]).Value = this.{x.RawName};");
                }
            }
            
            codeGen.WriteLine("public override string GetInputPortName(int index)");
            using (codeGen.Scope())
            {
                codeGen.WriteLine("switch(index)");
                using (codeGen.Scope())
                {
                    for (var i = 0; i < inputFields.Length; i++)
                    {
                        var inputField = inputFields[i];
                        codeGen.WriteLine($"case {i}: return \"{inputField.Name}\";");
                    }
                    codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid input port index\");");
                }
            }
            
            codeGen.WriteLine("public override string GetOutputPortName(int index)");
            using (codeGen.Scope())
            {
                codeGen.WriteLine("switch(index)");
                using (codeGen.Scope())
                {
                    for (var i = 0; i < outputFields.Length; i++)
                    {
                        var outputField = outputFields[i];
                        codeGen.WriteLine($"case {i}: return \"{outputField.Name}\";");
                    }
                    codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid output port index\");");
                }
            }

            if (isExecutionNode)
            {
                codeGen.WriteLine("public override string GetExecInPortName(int index)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("switch(index)");
                    using (codeGen.Scope())
                    {
                        if (execInCount > 0)
                        {
                            codeGen.WriteLine($"case 0: return \"ExecIn\";");
                        }
                        codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid exec in port index\");");
                    }
                }

                codeGen.WriteLine("public override string GetExecOutPortName(int index)");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("switch(index)");
                    using (codeGen.Scope())
                    {
                        for (var i = 0; i < execOutFields.Length; i++)
                        {
                            var execOutField = execOutFields[i];
                            codeGen.WriteLine($"case {i}: return \"{execOutField.Name}\";");
                        }
                        codeGen.WriteLine("default: throw new global::System.InvalidOperationException(\"Invalid exec out port index\");");
                    }
                }
            }

            // Generate GetProperties override
            if (propertyFields.Length > 0)
            {
                codeGen.WriteLine("public override global::NodeGraph.Model.PropertyDescriptor[] GetProperties()");
                using (codeGen.Scope())
                {
                    codeGen.WriteLine("return new global::NodeGraph.Model.PropertyDescriptor[]");
                    using (codeGen.Scope(isFinishSemicolon: true))
                    {
                        for (var i = 0; i < propertyFields.Length; i++)
                        {
                            var prop = propertyFields[i];
                            var comma = i < propertyFields.Length - 1 ? "," : "";

                            codeGen.WriteLine("new global::NodeGraph.Model.PropertyDescriptor");
                            using (codeGen.Scope(isFinishSemicolon: false))
                            {
                                codeGen.WriteLine($"Name = \"{prop.Name}\",");
                                codeGen.WriteLine($"Type = typeof({prop.Type}),");
                                codeGen.WriteLine($"Getter = node => (({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})node).{prop.RawName},");
                                codeGen.WriteLine($"Setter = (node, value) => (({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})node).{prop.RawName} = ({prop.Type})value,");

                                // Generate attributes array
                                var attributes = GenerateAttributeInstances(prop.FieldSymbol, reference);
                                codeGen.WriteLine($"Attributes = new global::System.Attribute[] {{ {string.Join(", ", attributes)} }}");
                            }
                            codeGen.WriteLine(comma);
                        }
                    }
                }
            }

        }
        context.AddSource($"{fullType}.NodeGraphGenerator.g.cs", codeGen.GetResult());
    }

    private static string[] GenerateAttributeInstances(IFieldSymbol fieldSymbol, ReferenceSymbols reference)
    {
        var attributes = new System.Collections.Generic.List<string>();

        foreach (var attr in fieldSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, reference.PropertyAttribute))
            {
                var displayName = GetNamedArgument(attr, "DisplayName");
                var category = GetNamedArgument(attr, "Category");
                var tooltip = GetNamedArgument(attr, "Tooltip");

                var parts = new System.Collections.Generic.List<string>();
                if (displayName != null) parts.Add($"DisplayName = {displayName}");
                if (category != null) parts.Add($"Category = {category}");
                if (tooltip != null) parts.Add($"Tooltip = {tooltip}");

                if (parts.Count > 0)
                    attributes.Add($"new global::NodeGraph.Model.PropertyAttribute {{ {string.Join(", ", parts)} }}");
                else
                    attributes.Add("new global::NodeGraph.Model.PropertyAttribute()");
            }
            else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, reference.RangeAttribute))
            {
                var min = attr.ConstructorArguments[0].Value;
                var max = attr.ConstructorArguments[1].Value;
                attributes.Add($"new global::NodeGraph.Model.RangeAttribute({min}, {max})");
            }
            else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, reference.MultilineAttribute))
            {
                var lines = GetNamedArgument(attr, "Lines");
                if (lines != null)
                    attributes.Add($"new global::NodeGraph.Model.MultilineAttribute {{ Lines = {lines} }}");
                else
                    attributes.Add("new global::NodeGraph.Model.MultilineAttribute()");
            }
            else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, reference.ReadOnlyAttribute))
            {
                attributes.Add("new global::NodeGraph.Model.ReadOnlyAttribute()");
            }
        }

        return attributes.ToArray();

        static string? GetNamedArgument(AttributeData attr, string name)
        {
            foreach (var pair in attr.NamedArguments)
            {
                if (pair.Key == name && pair.Value.Value != null)
                {
                    if (pair.Value.Value is string s)
                        return $"\"{s}\"";
                    else
                        return pair.Value.Value.ToString();
                }
            }
            return null;
        }
    }

    private static bool IsPartial(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static bool IsNested(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Parent is TypeDeclarationSyntax;
    }
}

public class ReferenceSymbols
{
    public ReferenceSymbols(Compilation compilation)
    {
        NodeAttribute = GetTypeByMetadataName("NodeGraph.Model.NodeAttribute");
        ExecutionNodeAttribute = GetTypeByMetadataName("NodeGraph.Model.ExecutionNodeAttribute");
        InputAttribute = GetTypeByMetadataName("NodeGraph.Model.InputAttribute");
        OutputAttribute = GetTypeByMetadataName("NodeGraph.Model.OutputAttribute");
        PropertyAttribute = GetTypeByMetadataName("NodeGraph.Model.PropertyAttribute");
        RangeAttribute = GetTypeByMetadataName("NodeGraph.Model.RangeAttribute");
        MultilineAttribute = GetTypeByMetadataName("NodeGraph.Model.MultilineAttribute");
        ReadOnlyAttribute = GetTypeByMetadataName("NodeGraph.Model.ReadOnlyAttribute");

        return;
        INamedTypeSymbol GetTypeByMetadataName(string metadataName)
        {
            var symbol = compilation.GetTypeByMetadataName(metadataName);
            if (symbol == null)
            {
                throw new InvalidOperationException($"Type {metadataName} is not found in compilation.");
            }
            return symbol;
        }
    }

    public INamedTypeSymbol NodeAttribute { get; }
    public INamedTypeSymbol ExecutionNodeAttribute { get; }
    public INamedTypeSymbol InputAttribute { get; }
    public INamedTypeSymbol OutputAttribute { get; }
    public INamedTypeSymbol PropertyAttribute { get; }
    public INamedTypeSymbol RangeAttribute { get; }
    public INamedTypeSymbol MultilineAttribute { get; }
    public INamedTypeSymbol ReadOnlyAttribute { get; }


}

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MustBePartial = new(
        "NG0001",
        "Must be partial",
        "Type '{0}' must be partial",
        "NodeGraph",
        DiagnosticSeverity.Error,
        true);
    
    public static readonly DiagnosticDescriptor NestedNotAllow = new(
        "NG0002",
        "Nested not allowed",
        "Type '{0}' cannot be nested",
        "NodeGraph",
        DiagnosticSeverity.Error,
        true);
}
