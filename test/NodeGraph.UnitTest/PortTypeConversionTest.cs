using NodeGraph.Model;

namespace NodeGraph.UnitTest;

public class PortTypeConversionTest
{
    [Fact]
    public void Converter_Should_Convert_Int_To_Float()
    {
        // Arrange & Act
        var canConvert = PortTypeConverterProvider.CanConvert(typeof(int), typeof(float));

        // Assert
        Assert.True(canConvert);
    }

    [Fact]
    public void Converter_Should_Convert_Int_Value_To_Float()
    {
        // Arrange
        const int intValue = 42;

        // Act - Use generic converter (no boxing, no reflection)
        var converter = PortTypeConverterProvider.GetConverter<int, float>();
        var result = converter!.Convert(intValue);

        // Assert
        Assert.Equal(42.0f, result);
    }

    [Fact]
    public void Converter_Should_Convert_Float_To_Double()
    {
        // Arrange
        const float floatValue = 3.14f;

        // Act - Use generic converter (no boxing, no reflection)
        var converter = PortTypeConverterProvider.GetConverter<float, double>();
        var result = converter!.Convert(floatValue);

        // Assert
        Assert.Equal(3.14, result, 5);
    }

    [Fact]
    public void Converter_Should_Support_Identity_Conversion()
    {
        // Arrange & Act
        var canConvert = PortTypeConverterProvider.CanConvert(typeof(int), typeof(int));

        // Assert
        Assert.True(canConvert);
    }

    [Fact]
    public void Converter_Should_Support_Assignable_Types()
    {
        // Arrange & Act
        var canConvert = PortTypeConverterProvider.CanConvert(typeof(string), typeof(object));

        // Assert
        Assert.True(canConvert);
    }

    [Fact]
    public void Custom_Converter_Should_Be_Registered()
    {
        // Arrange
        PortTypeConverterProvider.Register<string, int>(int.Parse);

        // Act
        var canConvert = PortTypeConverterProvider.CanConvert<string, int>();

        // Assert
        Assert.True(canConvert);
    }

    [Fact]
    public void Custom_Converter_Should_Convert_Value()
    {
        // Arrange
        PortTypeConverterProvider.Register<string, int>(int.Parse);

        // Act - Use generic converter (no boxing, no reflection)
        var converter = PortTypeConverterProvider.GetConverter<string, int>();
        var result = converter!.Convert("123");

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void InputPort_Should_Accept_Compatible_OutputPort()
    {
        // Arrange
        var graph = new Graph();
        var intNode = graph.CreateNode<IntConstantNode>();
        var floatNode = graph.CreateNode<FloatResultNode>();

        // Get ports via InputPorts and OutputPorts arrays
        var intOutput = intNode.OutputPorts[0];
        var floatInput = floatNode.InputPorts[0];

        // Act
        var canConnect = floatInput.CanConnect(intOutput);

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public void OutputPort_Should_Connect_To_Compatible_InputPort()
    {
        // Arrange
        var graph = new Graph();
        var intNode = graph.CreateNode<IntConstantNode>();
        var floatNode = graph.CreateNode<FloatResultNode>();

        var intOutput = intNode.OutputPorts[0];
        var floatInput = floatNode.InputPorts[0];

        // Act
        var canConnect = intOutput.CanConnect(floatInput);

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task OutputPort_Should_Convert_And_Propagate_Value()
    {
        // Arrange
        var graph = new Graph();
        var intNode = graph.CreateNode<IntConstantNode>();
        var floatNode = graph.CreateNode<FloatResultNode>();

        // Set value and connect
        intNode.SetValue(42);

        var intOutput = intNode.OutputPorts[0];
        var floatInput = floatNode.InputPorts[0];
        intOutput.Connect(floatInput);

        // Execute to propagate values
        var executor = graph.CreateExecutor();
        await executor.ExecuteAsync();

        // Assert
        Assert.Equal(42.0f, floatNode.Value);
    }

    [Fact]
    public void OutputPort_Should_Reject_Incompatible_InputPort()
    {
        // Arrange
        var graph = new Graph();
        var floatNode = graph.CreateNode<FloatConstantNode>();
        var intNode = graph.CreateNode<IntResultNode>();

        var floatOutput = floatNode.OutputPorts[0];
        var intInput = intNode.InputPorts[0];

        // Act
        var canConnect = floatOutput.CanConnect(intInput);

        // Assert
        Assert.False(canConnect, "Float to Int should not be allowed (no implicit conversion)");
    }
}

// Test helper node

[Node]
public partial class IntResultNode
{
    [Input] private int _value;
    public int Value => _value;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
