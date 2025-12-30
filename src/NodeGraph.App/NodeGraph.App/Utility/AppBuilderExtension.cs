using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace NodeGraph.App.Utility;

public static class AppBuilderExtension
{
    public static AppBuilder WithNotoSansJPFont(this AppBuilder appBuilder)
    {
        appBuilder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:Noto Sans JP", UriKind.Absolute),
                new Uri("avares://NodeGraph.App/Assets/Fonts", UriKind.Absolute)));
        });

        appBuilder.With(new FontManagerOptions
        {
            DefaultFamilyName = "Noto Sans JP",
        });

        return appBuilder;
    }
}