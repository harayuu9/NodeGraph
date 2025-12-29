using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.Undo;
using NodeGraph.Editor.ViewModels;
using NodeGraph.Editor.Views;

namespace NodeGraph.Editor;

public class App : Application
{
    public static IServiceProvider? ServiceProvider => Current is App app ? app.Services : null;

    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // プラグインをロード
            var pluginService = new PluginService();
            var pluginsPath = GetPluginsDirectory();
            pluginService.LoadPlugins(pluginsPath);

            // DIコンテナのセットアップ
            Services = ConfigureServices(pluginService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// プラグインディレクトリのパスを取得する
    /// </summary>
    private static string GetPluginsDirectory()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, "Plugins");
    }

    private ServiceProvider ConfigureServices(PluginService pluginService)
    {
        var services = new ServiceCollection();

        // プラグインサービスを登録
        services.AddSingleton(pluginService);

        services.AddSingleton<ConfigService>();
        services.AddSingleton<ExecutionHistoryService>();
        services.AddSingleton<SelectionManager>();

        // NodeTypeServiceにプラグインノードを登録
        services.AddSingleton(sp =>
        {
            var nodeTypeService = new NodeTypeService();
            nodeTypeService.RegisterPluginTypes(pluginService.PluginNodeTypes);
            return nodeTypeService;
        });

        services.AddSingleton<UndoRedoManager>();
        services.AddSingleton<CommonParameterService>();

        // ViewModelsを登録
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}