namespace NodeGraph.Model;

public record struct PortId(Guid Value) : IId;
public record struct NodeId(Guid Value) : IId;