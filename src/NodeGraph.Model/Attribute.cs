namespace NodeGraph.Model;

[AttributeUsage(AttributeTargets.Class)]
public class NodeAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public class InputAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public class OutputAttribute : Attribute;