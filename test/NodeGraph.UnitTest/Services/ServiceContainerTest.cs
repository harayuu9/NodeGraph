using NodeGraph.Model;
using NodeGraph.Model.Services;

namespace NodeGraph.UnitTest.Services;

public class ServiceContainerTest
{
    #region ServiceContainer Tests

    [Fact]
    public void Register_GetService_ReturnsInstance()
    {
        var container = new ServiceContainer();
        var service = new TestService("test");

        container.Register(service);

        var retrieved = container.GetService<TestService>();
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void RegisterInterface_GetService_ReturnsImplementation()
    {
        var container = new ServiceContainer();
        var service = new TestServiceImpl("impl");

        container.Register<ITestService, TestServiceImpl>(service);

        var retrieved = container.GetService<ITestService>();
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void GetService_NotRegistered_ReturnsNull()
    {
        var container = new ServiceContainer();

        var retrieved = container.GetService<TestService>();

        Assert.Null(retrieved);
    }

    [Fact]
    public void GetRequiredService_NotRegistered_ThrowsException()
    {
        var container = new ServiceContainer();

        var ex = Assert.Throws<ServiceNotFoundException>(() =>
            container.GetRequiredService<TestService>());

        Assert.Equal(typeof(TestService), ex.ServiceType);
    }

    [Fact]
    public void TryGetService_Registered_ReturnsTrueAndService()
    {
        var container = new ServiceContainer();
        var service = new TestService("test");
        container.Register(service);

        var result = container.TryGetService<TestService>(out var retrieved);

        Assert.True(result);
        Assert.Same(service, retrieved);
    }

    [Fact]
    public void TryGetService_NotRegistered_ReturnsFalse()
    {
        var container = new ServiceContainer();

        var result = container.TryGetService<TestService>(out var retrieved);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    [Fact]
    public void TryRegister_AlreadyRegistered_ReturnsFalse()
    {
        var container = new ServiceContainer();
        container.Register(new TestService("first"));

        var result = container.TryRegister(new TestService("second"));

        Assert.False(result);
        Assert.Equal("first", container.GetService<TestService>()!.Name);
    }

    [Fact]
    public void TryRegister_NotRegistered_ReturnsTrue()
    {
        var container = new ServiceContainer();

        var result = container.TryRegister(new TestService("first"));

        Assert.True(result);
    }

    [Fact]
    public void IsRegistered_Registered_ReturnsTrue()
    {
        var container = new ServiceContainer();
        container.Register(new TestService("test"));

        Assert.True(container.IsRegistered<TestService>());
    }

    [Fact]
    public void IsRegistered_NotRegistered_ReturnsFalse()
    {
        var container = new ServiceContainer();

        Assert.False(container.IsRegistered<TestService>());
    }

    [Fact]
    public void Register_OverwritesExisting()
    {
        var container = new ServiceContainer();
        container.Register(new TestService("first"));
        container.Register(new TestService("second"));

        Assert.Equal("second", container.GetService<TestService>()!.Name);
    }

    #endregion

    #region InitializerContext Tests

    [Fact]
    public void InitializerContext_GetParameter_ReturnsValue()
    {
        var serviceContainer = new ServiceContainer();
        var parameters = new Dictionary<string, object?> { ["key"] = "value" };
        var context = new InitializerContext(parameters, CancellationToken.None, serviceContainer);

        var value = context.GetParameter<string>("key");

        Assert.Equal("value", value);
    }

    [Fact]
    public void InitializerContext_GetParameter_NotFound_ReturnsDefault()
    {
        var serviceContainer = new ServiceContainer();
        var context = new InitializerContext(null, CancellationToken.None, serviceContainer);

        var value = context.GetParameter<string>("key");

        Assert.Null(value);
    }

    [Fact]
    public void InitializerContext_Register_RegistersToServiceContainer()
    {
        var serviceContainer = new ServiceContainer();
        var context = new InitializerContext(null, CancellationToken.None, serviceContainer);

        context.Register(new TestService("test"));

        Assert.NotNull(serviceContainer.GetService<TestService>());
    }

    [Fact]
    public void InitializerContext_HasParameter_ReturnsTrue()
    {
        var serviceContainer = new ServiceContainer();
        var parameters = new Dictionary<string, object?> { ["key"] = "value" };
        var context = new InitializerContext(parameters, CancellationToken.None, serviceContainer);

        Assert.True(context.HasParameter("key"));
        Assert.False(context.HasParameter("nonexistent"));
    }

    #endregion

    #region NodeExecutionContext Service Tests

    [Fact]
    public void NodeExecutionContext_GetService_ReturnsRegisteredService()
    {
        var serviceContainer = new ServiceContainer();
        serviceContainer.Register(new TestService("test"));
        var context = new NodeExecutionContext(CancellationToken.None, null, serviceContainer);

        var service = context.GetService<TestService>();

        Assert.NotNull(service);
        Assert.Equal("test", service.Name);
    }

    [Fact]
    public void NodeExecutionContext_GetService_NoProvider_ReturnsNull()
    {
        var context = new NodeExecutionContext(CancellationToken.None);

        var service = context.GetService<TestService>();

        Assert.Null(service);
    }

    [Fact]
    public void NodeExecutionContext_GetRequiredService_NoProvider_ThrowsException()
    {
        var context = new NodeExecutionContext(CancellationToken.None);

        Assert.Throws<ServiceNotFoundException>(() =>
            context.GetRequiredService<TestService>());
    }

    [Fact]
    public void NodeExecutionContext_TryGetService_ReturnsCorrectResult()
    {
        var serviceContainer = new ServiceContainer();
        serviceContainer.Register(new TestService("test"));
        var context = new NodeExecutionContext(CancellationToken.None, null, serviceContainer);

        var result = context.TryGetService<TestService>(out var service);

        Assert.True(result);
        Assert.NotNull(service);
    }

    #endregion

    #region Test Helpers

    private class TestService
    {
        public string Name { get; }

        public TestService(string name)
        {
            Name = name;
        }
    }

    private interface ITestService
    {
        string Name { get; }
    }

    private class TestServiceImpl : ITestService
    {
        public string Name { get; }

        public TestServiceImpl(string name)
        {
            Name = name;
        }
    }

    #endregion
}
