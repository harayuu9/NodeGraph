using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

/// <summary>
/// ポート情報
/// </summary>
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
            PortType = $"global::NodeGraph.Model.InputPort<{Type}>";
        else
            PortType = $"global::NodeGraph.Model.OutputPort<{Type}>";
        RawName = fieldSymbol.Name;
        Name = StringCaseConverter.ToUpperCamelCase(fieldSymbol.Name);
    }
}

/// <summary>
/// 実行ポート情報
/// </summary>
public readonly struct ExecPortData
{
    public readonly string Name;

    public ExecPortData(string name)
    {
        Name = name;
    }
}

/// <summary>
/// プロパティ情報
/// </summary>
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