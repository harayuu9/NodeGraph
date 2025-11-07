namespace NodeGraph.Model;

[AttributeUsage(AttributeTargets.Class)]
public class NodeAttribute(string? displayName = null, string? directory = null) : Attribute
{
    public string? DisplayName => displayName;
    public string? Directory => directory;
}

[AttributeUsage(AttributeTargets.Field)]
public class InputAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public class OutputAttribute : Attribute;