using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NodeGraph.App.Selection;
using NodeGraph.App.Services;
using NodeGraph.App.Undo;
using NodeGraph.App.ViewModels;
using NodeGraph.App.Views;

namespace NodeGraph.App;

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

            // DIコンテナのセットアップ
            Services = ConfigureServices();

            var dashboardViewModel = Services.GetRequiredService<DashboardViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = dashboardViewModel
            };

            // 保存済みグラフ一覧を非同期で読み込み
            _ = dashboardViewModel.LoadSavedGraphsAsync();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Browser/Mobile platform
            DisableAvaloniaDataAnnotationValidation();
            Services = ConfigureServices();

            var dashboardViewModel = Services.GetRequiredService<DashboardViewModel>();

            singleViewPlatform.MainView = new MainView
            {
                DataContext = dashboardViewModel
            };

            // 保存済みグラフ一覧を非同期で読み込み
            _ = dashboardViewModel.LoadSavedGraphsAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ConfigService>();
        services.AddSingleton<ExecutionHistoryService>();
        services.AddSingleton<SelectionManager>();
        services.AddSingleton<NodeTypeService>();
        services.AddSingleton<UndoRedoManager>();
        services.AddSingleton<CommonParameterService>();

        // プラットフォーム別のストレージサービスを登録
        if (OperatingSystem.IsBrowser())
        {
            // ブラウザ環境ではAPIを使用してデータを永続化
            // WASM環境では直接HttpClientを作成して登録
            var apiBaseUrl = "http://localhost:5000/";
            var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
            services.AddSingleton<IGraphStorageService>(new ApiGraphStorageService(httpClient));
        }
        else
        {
            services.AddSingleton<IGraphStorageService, FileSystemGraphStorageService>();
        }

        // ViewModelsを登録
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();

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
