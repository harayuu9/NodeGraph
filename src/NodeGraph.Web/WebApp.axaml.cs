using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Services;
using NodeGraph.Web.Services;
using NodeGraph.Web.Storage;
using NodeGraph.Web.ViewModels;
using NodeGraph.Web.Views;

namespace NodeGraph.Web;

public class WebApp : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            _services = ConfigureServices();
            singleView.MainView = new MainShellView
            {
                DataContext = _services.GetRequiredService<MainShellViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register browser-specific storage
        services.AddSingleton<IStorageProvider, BrowserStorageProvider>();

        // Register shared services
        services.AddSingleton<NodeTypeService>();
        services.AddSingleton<SelectionManager>();
        services.AddSingleton<BrowserParameterService>();

        // Register ViewModels
        services.AddTransient<MainShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<GraphEditorViewModel>();
        services.AddTransient<ParametersPanelViewModel>();

        return services.BuildServiceProvider();
    }
}
