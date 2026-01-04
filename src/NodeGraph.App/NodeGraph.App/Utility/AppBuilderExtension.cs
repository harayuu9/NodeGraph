using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace NodeGraph.App.Utility;

public static class AppBuilderExtension
{
    public static AppBuilder WithNotoSansJPFont(this AppBuilder appBuilder)
    {
        // Interフォントをデフォルトとして使用（WASM環境での互換性のため）
        // Noto Sans JPはフォールバックとして設定
        appBuilder.With(new FontManagerOptions
        {
            DefaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter",
            FontFallbacks =
            [
                new FontFallback { FontFamily = new FontFamily("avares://NodeGraph.App/Assets/Fonts#Noto Sans JP") }
            ]
        });

        return appBuilder;
    }
}