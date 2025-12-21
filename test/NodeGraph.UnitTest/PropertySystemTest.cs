using NodeGraph.Model;

namespace NodeGraph.UnitTest;

public class PropertySystemTest
{
    [Fact]
    public void FloatConstantNode_Should_Have_Property()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();

        // Act
        var properties = node.GetProperties();

        // Assert
        Assert.NotEmpty(properties);
        Assert.Single(properties);
        Assert.Equal("Value", properties[0].Name);
        Assert.Equal(typeof(float), properties[0].Type);
    }

    [Fact]
    public void Property_Should_Have_Range_Attribute()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();
        var properties = node.GetProperties();
        var property = properties[0];

        // Act
        var rangeAttr = property.GetAttribute<RangeAttribute>();

        // Assert
        Assert.NotNull(rangeAttr);
        Assert.Equal(0, rangeAttr.Min);
        Assert.Equal(100, rangeAttr.Max);
    }

    [Fact]
    public void Property_Should_Get_And_Set_Value()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();
        var properties = node.GetProperties();
        var property = properties[0];

        // Act
        property.Setter(node, 42.5f);
        var value = property.Getter(node);

        // Assert
        Assert.Equal(42.5f, value);
    }

    [Fact]
    public void Node_Should_Get_Property_Value_By_Name()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();
        node.SetValue(99.9f);

        // Act
        var value = node.GetPropertyValue("Value");

        // Assert
        Assert.Equal(99.9f, value);
    }

    [Fact]
    public void Node_Should_Set_Property_Value_By_Name()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();

        // Act
        node.SetPropertyValue("Value", 12.34f);

        // Assert
        Assert.Equal(12.34f, node.GetPropertyValue("Value"));
    }

    [Fact]
    public void Property_DisplayName_Should_Use_Attribute_Value()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();
        var properties = node.GetProperties();
        var property = properties[0];

        // Act & Assert
        Assert.Equal("Value", property.DisplayName);
    }

    [Fact]
    public void Property_Should_Have_Tooltip()
    {
        // Arrange
        var graph = new Graph();
        var node = graph.CreateNode<FloatConstantNode>();
        var properties = node.GetProperties();
        var property = properties[0];

        // Act & Assert
        Assert.Equal("定数値", property.Tooltip);
    }
}