using System;
using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

/// <summary>
/// コンパイル時に必要な型シンボルへの参照を保持
/// </summary>
public class ReferenceSymbols
{
    public ReferenceSymbols(Compilation compilation)
    {
        _compilation = compilation;

        // Node関連
        NodeAttribute = GetTypeByMetadataName("NodeGraph.Model.NodeAttribute");
        InputAttribute = GetTypeByMetadataName("NodeGraph.Model.InputAttribute");
        OutputAttribute = GetTypeByMetadataName("NodeGraph.Model.OutputAttribute");
        PropertyAttribute = GetTypeByMetadataName("NodeGraph.Model.PropertyAttribute");
        RangeAttribute = GetTypeByMetadataName("NodeGraph.Model.RangeAttribute");
        MultilineAttribute = GetTypeByMetadataName("NodeGraph.Model.MultilineAttribute");
        ReadOnlyAttribute = GetTypeByMetadataName("NodeGraph.Model.ReadOnlyAttribute");

        // JsonNode関連
        JsonNodeAttribute = GetTypeByMetadataNameOrNull("NodeGraph.Model.JsonNodeAttribute");
        JsonPropertyAttribute = GetTypeByMetadataNameOrNull("NodeGraph.Model.JsonPropertyAttribute");
    }

    private readonly Compilation _compilation;

    // Node関連
    public INamedTypeSymbol NodeAttribute { get; }
    public INamedTypeSymbol InputAttribute { get; }
    public INamedTypeSymbol OutputAttribute { get; }
    public INamedTypeSymbol PropertyAttribute { get; }
    public INamedTypeSymbol RangeAttribute { get; }
    public INamedTypeSymbol MultilineAttribute { get; }
    public INamedTypeSymbol ReadOnlyAttribute { get; }

    // JsonNode関連
    public INamedTypeSymbol? JsonNodeAttribute { get; }
    public INamedTypeSymbol? JsonPropertyAttribute { get; }

    public Compilation Compilation => _compilation;

    private INamedTypeSymbol GetTypeByMetadataName(string metadataName)
    {
        var symbol = _compilation.GetTypeByMetadataName(metadataName);
        if (symbol == null)
        {
            throw new InvalidOperationException($"Type {metadataName} is not found in compilation.");
        }
        return symbol;
    }

    private INamedTypeSymbol? GetTypeByMetadataNameOrNull(string metadataName)
    {
        return _compilation.GetTypeByMetadataName(metadataName);
    }
}
