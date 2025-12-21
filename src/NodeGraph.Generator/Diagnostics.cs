using Microsoft.CodeAnalysis;

namespace NodeGraph.Generator;

/// <summary>
/// Diagnostic messages for the source generator
/// </summary>
public static class Diagnostics
{
    private const string Category = "NodeGraph.Generator";

    // ===== Node (NG00xx) =====

    public static readonly DiagnosticDescriptor MustBePartial = new(
        "NG0001",
        "Must be partial",
        "Type '{0}' must be declared as partial",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ===== Shared (NG00xx) =====

    public static readonly DiagnosticDescriptor NestedNotAllowed = new(
        "NG0002",
        "Nested type not allowed",
        "Type '{0}' is a nested class. Only top-level classes are supported",
        Category,
        DiagnosticSeverity.Error,
        true);

    // ===== JsonNode (NG10xx) =====

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        "NG1001",
        "Unsupported JSON type",
        "Property '{0}' has type '{1}' which is not supported in JSON schema",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor CircularReference = new(
        "NG1002",
        "Circular reference detected",
        "Type '{0}' has a circular reference",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor MissingParameterlessConstructor = new(
        "NG1003",
        "Missing parameterless constructor",
        "Type '{0}' must have a public parameterless constructor",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor NoPublicProperties = new(
        "NG1004",
        "No public properties",
        "Type '{0}' has no public properties",
        Category,
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor NestingTooDeep = new(
        "NG1005",
        "Nesting too deep",
        "Type '{0}' has nesting depth exceeding the maximum (10 levels)",
        Category,
        DiagnosticSeverity.Error,
        true);
}
