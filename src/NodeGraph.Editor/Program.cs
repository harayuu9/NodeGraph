using Avalonia;

namespace NodeGraph.Editor;

/// <summary>
/// NodeGraph.Editorのエントリポイントを提供するクラス
/// </summary>
public static class EditorEntryPoint
{
    /// <summary>
    /// NodeGraph.Editorアプリケーションを起動します
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    public static void Run(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Avaloniaアプリケーションの構成を行います
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}