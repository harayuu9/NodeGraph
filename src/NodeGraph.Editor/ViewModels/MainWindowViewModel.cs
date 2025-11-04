using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Model;

namespace NodeGraph.Editor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public EditorGraph TestGraph { get; }

    [ObservableProperty]
    private string _currentTheme = "Default";

    [ObservableProperty]
    private bool _isDefaultTheme = true;

    [ObservableProperty]
    private bool _isLightTheme = false;

    [ObservableProperty]
    private bool _isDarkTheme = false;

    public MainWindowViewModel(SelectionManager selectionManager)
    {
        // テスト用のグラフを作成
        var graph = new Graph();

        // テスト用のノードを作成
        var a1 = graph.CreateNode<FloatConstantNode>();
        a1.SetValue(10);
        
        var a2 = graph.CreateNode<FloatConstantNode>();
        a2.SetValue(5);
        
        var add = graph.CreateNode<FloatAddNode>();
        add.ConnectInput(0, a1, 0);
        add.ConnectInput(1, a2, 0);
        
        var res = graph.CreateNode<FloatResultNode>();
        res.ConnectInput(0, add, 0);
        
        // EditorGraphでラップ（SelectionManagerを注入）
        TestGraph = new EditorGraph(graph, selectionManager);

        // ノードの位置を設定
        TestGraph.Nodes[0].X = 100;
        TestGraph.Nodes[0].Y = 100;

        TestGraph.Nodes[1].X = 350;
        TestGraph.Nodes[1].Y = 50;

        TestGraph.Nodes[2].X = 350;
        TestGraph.Nodes[2].Y = 200;

        TestGraph.Nodes[3].X = 600;
        TestGraph.Nodes[3].Y = 120;

        // 現在のテーマを取得
        UpdateCurrentTheme();
    }

    [RelayCommand]
    private void SwitchTheme(string themeName)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = themeName switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        CurrentTheme = themeName;
        UpdateThemeFlags();
    }

    private void UpdateThemeFlags()
    {
        IsDefaultTheme = CurrentTheme == "Default";
        IsLightTheme = CurrentTheme == "Light";
        IsDarkTheme = CurrentTheme == "Dark";
    }

    private void UpdateCurrentTheme()
    {
        if (Application.Current is null)
            return;

        var variant = Application.Current.ActualThemeVariant;
        CurrentTheme = variant == ThemeVariant.Dark ? "Dark" :
                      variant == ThemeVariant.Light ? "Light" : "Default";
        UpdateThemeFlags();
    }

    [RelayCommand]
    private async Task ExecuteGraphAsync()
    {
        await TestGraph.ExecuteAsync();
    }
}