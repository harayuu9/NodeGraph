using System.Linq;
using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

public static class Extensions
{
    public static bool ContainsAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        return symbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribute));
    }
}