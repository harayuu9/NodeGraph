using NodeGraph.Editor.Services;
using NodeGraph.Model;
using Xunit;

namespace NodeGraph.UnitTest.Plugin;

public class PluginScannerTest
{
    [Fact]
    public void ContainsNodeTypes_WithValidPluginDll_ReturnsTrue()
    {
        // Arrange: NodeGraph.Model.dllにはNode派生型が含まれている
        var scanner = new PluginScanner();
        var modelDllPath = typeof(Node).Assembly.Location;

        // Act
        var result = scanner.ContainsNodeTypes(modelDllPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsNodeTypes_WithNonPluginDll_ReturnsFalse()
    {
        // Arrange: xunit.core.dllにはNode型は含まれていない
        var scanner = new PluginScanner();
        var xunitDllPath = typeof(FactAttribute).Assembly.Location;

        // Act
        var result = scanner.ContainsNodeTypes(xunitDllPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsNodeTypes_WithInvalidPath_ReturnsFalse()
    {
        // Arrange
        var scanner = new PluginScanner();

        // Act
        var result = scanner.ContainsNodeTypes("nonexistent.dll");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnumerateDlls_WithNonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var scanner = new PluginScanner();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var dlls = scanner.EnumerateDlls(nonExistentDir).ToList();

        // Assert
        Assert.Empty(dlls);
    }

    [Fact]
    public void EnumerateDlls_WithValidDirectory_ReturnsDlls()
    {
        // Arrange
        var scanner = new PluginScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // ダミーDLLファイルを作成
            File.WriteAllText(Path.Combine(tempDir, "test1.dll"), "dummy");
            File.WriteAllText(Path.Combine(tempDir, "test2.dll"), "dummy");
            File.WriteAllText(Path.Combine(tempDir, "test.txt"), "dummy");

            // Act
            var dlls = scanner.EnumerateDlls(tempDir).ToList();

            // Assert
            Assert.Equal(2, dlls.Count);
            Assert.All(dlls, d => Assert.EndsWith(".dll", d));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnumerateDlls_WithSubdirectories_ReturnsDllsRecursively()
    {
        // Arrange
        var scanner = new PluginScanner();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "root.dll"), "dummy");
            File.WriteAllText(Path.Combine(subDir, "sub.dll"), "dummy");

            // Act
            var dlls = scanner.EnumerateDlls(tempDir).ToList();

            // Assert
            Assert.Equal(2, dlls.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
