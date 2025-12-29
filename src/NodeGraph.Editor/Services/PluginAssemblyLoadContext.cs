using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace NodeGraph.Editor.Services;

/// <summary>
/// プラグインアセンブリをロードするためのカスタムAssemblyLoadContext
/// プラグインの分離と依存関係の解決を行う
/// </summary>
public class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _pluginsDirectory;
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// 新しいPluginAssemblyLoadContextを作成する
    /// </summary>
    /// <param name="mainPluginPath">メインプラグインDLLのパス（依存解決の基点）</param>
    /// <param name="pluginsDirectory">プラグインディレクトリのパス</param>
    public PluginAssemblyLoadContext(string mainPluginPath, string pluginsDirectory)
        : base(isCollectible: false) // アンロード不要のためfalse
    {
        _pluginsDirectory = pluginsDirectory;
        _resolver = new AssemblyDependencyResolver(mainPluginPath);
    }

    /// <summary>
    /// アセンブリをロードする
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. deps.jsonベースの依存解決を試行
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        // 2. Pluginsディレクトリ内を再帰的に検索
        var dllName = assemblyName.Name + ".dll";
        try
        {
            var matchingFiles = Directory.GetFiles(_pluginsDirectory, dllName, SearchOption.AllDirectories);
            if (matchingFiles.Length > 0)
                return LoadFromAssemblyPath(matchingFiles[0]);
        }
        catch
        {
            // ディレクトリアクセスエラーは無視
        }

        // 3. デフォルトコンテキストにフォールバック（共有フレームワークアセンブリ用）
        return null;
    }

    /// <summary>
    /// アンマネージDLLをロードする
    /// </summary>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
