using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using NodeGraph.Web;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<WebApp>();
}
