# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NodeGraph is a visual node-based programming system built with C# and .NET 9. It provides a cross-platform desktop application for creating and executing computational graphs through a node-and-wire interface.

**Tech Stack:**
- C# 12 with preview features (.NET 9.0)
- Avalonia 11.3.8 (cross-platform UI framework)
- MVVM pattern with CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Roslyn source generation for code generation

## Architecture

The project follows a **three-layer architecture**:

1. **NodeGraph.Model** (.NET 9.0 / .NET Standard 2.1) - Core graph engine with no UI dependencies
   - Graph representation and node management
   - Port system with type-safe connections
   - Async graph execution engine with topological sorting
   - Object pooling utilities for memory optimization

2. **NodeGraph.Editor** (.NET 9.0) - Avalonia-based UI application
   - MVVM architecture with ViewModels and custom Controls
   - GraphControl for pan/zoom visualization
   - NodeControl and PortControl for rendering
   - Selection management system

3. **NodeGraph.Generator** (.NET Standard 2.0) - Roslyn source generator
   - Generates boilerplate code for nodes decorated with `[Node]` attribute
   - Automatically creates port arrays and property accessors from `[Input]` and `[Output]` fields

## Key Architectural Patterns

### Source Code Generation
Nodes are defined using attributes:
```csharp
[Node]
public partial class MyNode : Node
{
    [Input] private InputPort<float> _input;
    [Output] private OutputPort<float> _output;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        // Implementation
    }
}
```
The source generator creates the necessary port arrays and properties.

### Graph Execution
- `GraphExecutor` performs topological sorting to determine execution order
- Nodes execute asynchronously respecting dependencies
- Cycle detection prevents infinite loops
- All node exceptions are aggregated and thrown together

### Object Pooling
The codebase uses thread-safe object pooling for performance:
- `ListPool<T>` - Pool of List<T> instances
- `DictionaryPool<TKey, TValue>` - Pool of Dictionary instances
- `HashSetPool<T>` - Pool of HashSet instances

All pools use `ConcurrentBag<T>` for thread-safety and enforce maximum capacity limits to prevent memory leaks.

### Port System
- Generic `InputPort<T>` and `OutputPort<T>` with compile-time type safety
- Runtime type checking when connecting ports
- Bidirectional connection tracking
- Value propagation through setters

## Development Commands

### Build
```bash
dotnet build NodeGraph.slnx
```

### Run Tests
```bash
# Run all tests
dotnet test NodeGraph.slnx

# Run tests in a specific project
dotnet test test/NodeGraph.UnitTest/NodeGraph.UnitTest.csproj

# Run with coverage
dotnet test NodeGraph.slnx --collect:"XPlat Code Coverage"
```

### Run the Editor Application
```bash
dotnet run --project src/NodeGraph.Editor/NodeGraph.Editor.csproj
```

### Clean Build Artifacts
```bash
dotnet clean NodeGraph.slnx
```

## Project Structure Details

### NodeGraph.Model/
Core graph engine components:
- `Graph.cs` - Main graph container, manages nodes and creates executors
- `GraphExecutor.cs` - Async execution engine with topological sorting
- `Node.cs` - Abstract base class for all nodes
- `InputPort.cs` / `OutputPort.cs` - Generic port implementations
- `Node/` - Concrete node implementations (FloatAddNode, FloatConstantNode, etc.)
- `Pool/` - Object pooling utilities with thread-safe implementations
- `Structure.cs` - Core record structs (PortId, NodeId)
- `DisposableBag.cs` - Resource management utility

### NodeGraph.Editor/
UI layer components:
- `Controls/GraphControl.cs` - Main canvas with pan/zoom (uses MatrixTransform)
- `Controls/NodeControl.cs` - Visual representation of nodes
- `Selection/SelectionManager.cs` - Manages selected items in the editor
- `Models/EditorGraph.cs` - Wraps Model.Graph with editor-specific state
- `ViewModels/` - MVVM ViewModels using CommunityToolkit.Mvvm

### NodeGraph.Generator/
Source generation:
- `SourceGenerator.cs` - IIncrementalGenerator implementation
- `CSharpCodeGenerator.cs` - Emits C# code for nodes

## Important Configuration

### InternalsVisibleTo
NodeGraph.Model exposes internals to:
- `NodeGraph.UnitTest` - For testing internal APIs
- `NodeGraph.Editor` - For UI binding to internal state

### Multi-Targeting
NodeGraph.Model targets both `net9.0` and `netstandard2.1` for compatibility with different consumers.

### SDK Requirements
The project requires .NET 9.0 SDK (see `global.json`). It will roll forward to latest minor versions.

## Testing Approach

Tests use xUnit with the following patterns:
- Create a `Graph` instance
- Add and configure nodes using `graph.CreateNode<T>()`
- Connect ports between nodes
- Create executor: `var executor = graph.CreateExecutor()`
- Execute: `await executor.ExecuteAsync()`
- Assert results from output nodes

Example:
```csharp
[Fact]
public async Task Test1()
{
    var graph = new Graph();
    var constant = graph.CreateNode<FloatConstantNode>();
    constant.SetValue(100);

    var result = graph.CreateNode<FloatResultNode>();
    result.Input.ConnectFrom(constant.Output);

    var executor = graph.CreateExecutor();
    await executor.ExecuteAsync();

    Assert.Equal(100, result.Value);
}
```

## Working with Nodes

### Creating New Node Types
1. Create a class inheriting from `Node`
2. Add `[Node]` attribute to the class (make it `partial`)
3. Add `[Input]` fields for input ports
4. Add `[Output]` fields for output ports
5. Implement `ExecuteAsync(CancellationToken ct)` method
6. The source generator will create the port arrays and properties

### Node Execution Order
Nodes execute based on topological sort of their dependencies. A node only executes after all its input dependencies have completed.

## Memory Management

When working with collections in performance-critical code, use the pooling utilities:
```csharp
var list = ListPool<int>.Rent();
try
{
    // Use list
}
finally
{
    ListPool<int>.Return(list);
}
```

Or use `DisposableBag` for automatic cleanup of multiple resources.

## UI Development with Avalonia

The editor uses Avalonia XAML with MVVM:
- ViewModels inherit from `ViewModelBase` (CommunityToolkit.Mvvm)
- Use `ObservableProperty` for data binding
- Controls are in `NodeGraph.Editor/Controls/`
- ViewLocator maps ViewModels to Views automatically

## Solution Format

This project uses the new `.slnx` solution format (XML-based) instead of the traditional `.sln` format. Both Visual Studio 2022+ and Rider support this format.
