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
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "NodeGraph.Model.NodeAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (cont, _) => cont);
        
        context.RegisterSourceOutput(provider, Emit);
    }

    private static void Emit(SourceProductionContext spc, GeneratorAttributeSyntaxContext source)
    {
        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;
        var reference = new ReferenceSymbols(source.SemanticModel.Compilation);
        Emitter.Emit(spc, typeSymbol, typeNode, reference);
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

public static class Emitter
{
    public static void Emit(SourceProductionContext context, INamedTypeSymbol typeSymbol, TypeDeclarationSyntax typeNode, ReferenceSymbols reference)
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
        
        var codeGen = new CSharpCodeGenerator(ns);
        codeGen.WriteLine($"partial class {typeSymbol.Name} : global::NodeGraph.Model.Node");
        using (codeGen.Scope())
        {
            codeGen.WriteLine("protected override void InitializePorts()");
            using (codeGen.Scope())
            {
                foreach (var inputField in inputFields)
                {
                    codeGen.WriteLine($"InputPorts.Add(new {inputField.PortType}(this, {inputField.RawName}));");
                }
                foreach (var outputField in outputFields)
                {
                    codeGen.WriteLine($"OutputPorts.Add(new {outputField.PortType}(this, {outputField.RawName}));");
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
        }
        context.AddSource($"{fullType}.NodeGraphGenerator.g.cs", codeGen.GetResult());
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
        InputAttribute = GetTypeByMetadataName("NodeGraph.Model.InputAttribute");
        OutputAttribute = GetTypeByMetadataName("NodeGraph.Model.OutputAttribute");
        
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
    public INamedTypeSymbol InputAttribute { get; }
    public INamedTypeSymbol OutputAttribute { get; }


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
