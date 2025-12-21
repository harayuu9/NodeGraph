using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NodeGraph.Generator;

/// <summary>
/// [Node]属性を持つクラスのコード生成
/// </summary>
public static class NodeEmitter
{
    public static void Emit(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        TypeDeclarationSyntax typeNode,
        ReferenceSymbols reference,
        bool hasExecIn,
        bool hasExecOut,
        string[] execOutNames)
    {
        if (!IsPartial(typeNode))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MustBePartial, typeNode.Identifier.GetLocation(), typeSymbol.Name));
            return;
        }

        if (IsNested(typeNode))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NestedNotAllowed, typeNode.Identifier.GetLocation(), typeSymbol.Name));
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

        var execInCount = hasExecIn ? 1 : 0;
        var execOutCount = hasExecOut ? execOutNames.Length : 0;
        var execOutFields = hasExecOut ? execOutNames.Select(name => new ExecPortData(name)).ToArray() : Array.Empty<ExecPortData>();

        var codeGen = new CSharpCodeGenerator(ns);
        codeGen.WriteLine($"partial class {typeSymbol.Name} : global::NodeGraph.Model.Node");

        using (codeGen.Scope())
        {
            // デフォルトコンストラクタ
            codeGen.WriteLine($"public {typeSymbol.Name}() : base({inputFields.Length}, {outputFields.Length}, {execInCount}, {execOutCount})");
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

                for (var i = 0; i < execInCount; i++)
                {
                    codeGen.WriteLine($"ExecInPorts[{i}] = new global::NodeGraph.Model.ExecInPort(this);");
                }

                for (var i = 0; i < execOutCount; i++)
                {
                    codeGen.WriteLine($"ExecOutPorts[{i}] = new global::NodeGraph.Model.ExecOutPort(this);");
                }
            }

            codeGen.WriteLine();

            // デシリアライズ用コンストラクタ
            codeGen.WriteLine($"public {typeSymbol.Name}(global::NodeGraph.Model.NodeId nodeId, global::NodeGraph.Model.PortId[] inputPortIds, global::NodeGraph.Model.PortId[] outputPortIds, global::NodeGraph.Model.PortId[] execInPortIds, global::NodeGraph.Model.PortId[] execOutPortIds) : base(nodeId, inputPortIds, outputPortIds, execInPortIds, execOutPortIds)");
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

                for (var i = 0; i < execInCount; i++)
                {
                    codeGen.WriteLine($"ExecInPorts[{i}] = new global::NodeGraph.Model.ExecInPort(this, execInPortIds[{i}]);");
                }

                for (var i = 0; i < execOutCount; i++)
                {
                    codeGen.WriteLine($"ExecOutPorts[{i}] = new global::NodeGraph.Model.ExecOutPort(this, execOutPortIds[{i}]);");
                }
            }

            // BeforeExecute
            codeGen.WriteLine("protected override void BeforeExecute()");
            using (codeGen.Scope())
            {
                for (var i = 0; i < inputFields.Length; i++)
                {
                    var x = inputFields[i];
                    codeGen.WriteLine($"this.{x.RawName} = (({x.PortType})InputPorts[{i}]).Value;");
                }
            }

            // AfterExecute
            codeGen.WriteLine("protected override void AfterExecute()");
            using (codeGen.Scope())
            {
                for (var i = 0; i < outputFields.Length; i++)
                {
                    var x = outputFields[i];
                    codeGen.WriteLine($"(({x.PortType})OutputPorts[{i}]).Value = this.{x.RawName};");
                }
            }

            // GetInputPortName
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

            // GetOutputPortName
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

            // GetExecInPortName
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

            // GetExecOutPortName
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

            // GetProperties
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
        var attributes = new List<string>();

        foreach (var attr in fieldSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, reference.PropertyAttribute))
            {
                var displayName = GetNamedArgument(attr, "DisplayName");
                var category = GetNamedArgument(attr, "Category");
                var tooltip = GetNamedArgument(attr, "Tooltip");

                var parts = new List<string>();
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
    }

    private static string? GetNamedArgument(AttributeData attr, string name)
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

    private static bool IsPartial(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static bool IsNested(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Parent is TypeDeclarationSyntax;
    }
}
