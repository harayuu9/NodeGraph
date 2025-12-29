using NodeGraph.Editor.Services;
using Xunit;

namespace NodeGraph.UnitTest.Plugin;

public class PluginServiceTest
{
    [Fact]
    public void LoadPlugins_WithEmptyDirectory_ReturnsNoResults()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new PluginService();

            // Act
            service.LoadPlugins(tempDir);

            // Assert
            Assert.Empty(service.LoadResults);
            Assert.Empty(service.PluginNodeTypes);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadPlugins_WithNonExistentDirectory_ReturnsNoResults()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var service = new PluginService();

        // Act
        service.LoadPlugins(nonExistentDir);

        // Assert
        Assert.Empty(service.LoadResults);
        Assert.Empty(service.PluginNodeTypes);
    }

    [Fact]
    public void LoadPlugins_WithInvalidDll_ReturnsFailureResult()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 無効なDLLファイルを作成
            File.WriteAllText(Path.Combine(tempDir, "invalid.dll"), "not a dll");

            var service = new PluginService();

            // Act
            service.LoadPlugins(tempDir);

            // Assert
            Assert.Single(service.LoadResults);
            Assert.False(service.LoadResults[0].Success);
            Assert.Empty(service.PluginNodeTypes);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadResults_InitialState_IsEmpty()
    {
        // Arrange & Act
        var service = new PluginService();

        // Assert
        Assert.Empty(service.LoadResults);
        Assert.Empty(service.PluginNodeTypes);
    }
}
