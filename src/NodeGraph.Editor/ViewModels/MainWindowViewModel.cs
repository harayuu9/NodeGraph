using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NodeGraph.Editor.Models;
using NodeGraph.Editor.Selection;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.Undo;
using NodeGraph.Editor.Views;
using NodeGraph.Model;

namespace NodeGraph.Editor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SelectionManager _selectionManager;
    private readonly CommonParameterService _commonParameterService;

    [ObservableProperty] private string _currentTheme = "Default";

    [ObservableProperty] private bool _isDarkTheme;

    [ObservableProperty] private bool _isDefaultTheme = true;

    [ObservableProperty] private bool _isLightTheme;
    private Window? _mainWindow;

    [ObservableProperty] private EditorGraph _testGraph;

#if DEBUG
    public MainWindowViewModel() : this(new SelectionManager(), new UndoRedoManager(), new CommonParameterService())
    {
    }
#endif

    public MainWindowViewModel(SelectionManager selectionManager, UndoRedoManager undoRedoManager, CommonParameterService commonParameterService)
    {
        _selectionManager = selectionManager;
        _commonParameterService = commonParameterService;
        UndoRedoManager = undoRedoManager;

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

        var str1 = graph.CreateNode<StringConstantNode>();
        str1.SetValue("The result is:");

        var int1 = graph.CreateNode<IntConstantNode>();
        int1.SetValue(42);

        // Execution flow example: StartNode → LoopNode → PrintNode (loop back)
        var start = graph.CreateNode<StartNode>();

        var loopNode = graph.CreateNode<LoopNode>();
        loopNode.SetCount(3); // 3回ループ

        start.ExecOutPorts[0].Connect(loopNode.ExecInPorts[0]);

        // ループボディのPrintNode
        var printLoop = graph.CreateNode<PrintNode>();
        var loopMessage = graph.CreateNode<StringConstantNode>();
        loopMessage.SetValue("Loop iteration");
        printLoop.ConnectInput(0, loopMessage, 0);
        loopNode.ExecOutPorts[0].Connect(printLoop.ExecInPorts[0]);

        // PrintNodeからLoopNodeへのループバック
        printLoop.ExecOutPorts[0].Connect(loopNode.ExecInPorts[0]);

        // ループ完了後のPrintNode
        var printCompleted = graph.CreateNode<PrintNode>();
        var completedMessage = graph.CreateNode<StringConstantNode>();
        completedMessage.SetValue("Loop completed!");
        printCompleted.ConnectInput(0, completedMessage, 0);
        loopNode.ExecOutPorts[1].Connect(printCompleted.ExecInPorts[0]);

        // EditorGraphでラップ（SelectionManagerを注入）
        _testGraph = new EditorGraph(graph, selectionManager);

        // ノードの位置を設定（データフローノード）
        TestGraph.Nodes[0].X = 100;
        TestGraph.Nodes[0].Y = 100;

        TestGraph.Nodes[1].X = 350;
        TestGraph.Nodes[1].Y = 50;

        TestGraph.Nodes[2].X = 350;
        TestGraph.Nodes[2].Y = 200;

        TestGraph.Nodes[3].X = 600;
        TestGraph.Nodes[3].Y = 120;

        TestGraph.Nodes[4].X = 600;
        TestGraph.Nodes[4].Y = 300;

        TestGraph.Nodes[5].X = 600;
        TestGraph.Nodes[5].Y = 400;

        // 実行フローノードの位置を設定
        TestGraph.Nodes[6].X = 100; // start
        TestGraph.Nodes[6].Y = 500;

        TestGraph.Nodes[7].X = 350; // loopNode
        TestGraph.Nodes[7].Y = 500;

        TestGraph.Nodes[8].X = 600; // printLoop
        TestGraph.Nodes[8].Y = 550;

        TestGraph.Nodes[9].X = 350; // loopMessage
        TestGraph.Nodes[9].Y = 650;

        TestGraph.Nodes[10].X = 600; // printCompleted
        TestGraph.Nodes[10].Y = 750;

        TestGraph.Nodes[11].X = 350; // completedMessage
        TestGraph.Nodes[11].Y = 800;

        // 現在のテーマを取得
        UpdateCurrentTheme();
    }

    public bool CanUndo => UndoRedoManager.CanUndo();
    public bool CanRedo => UndoRedoManager.CanRedo();

    public UndoRedoManager UndoRedoManager { get; }

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
        await TestGraph.ExecuteAsync(_commonParameterService);
    }

    [RelayCommand]
    private async Task OpenParametersAsync()
    {
        if (_mainWindow == null) return;

        var window = new ParametersWindow(_commonParameterService);
        await window.ShowDialog(_mainWindow);
    }

    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    [RelayCommand]
    private void New()
    {
        // 新しい空のグラフを作成
        var graph = new Graph();
        TestGraph = new EditorGraph(graph, _selectionManager);

        // Undo履歴をクリア
        UndoRedoManager.Clear();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (_mainWindow == null) return;

        var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Graph File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("NodeGraph Files")
                {
                    Patterns = ["*.graph.yml"]
                }
            ]
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            // 拡張子を除いたベース名を取得
            var basePath = Path.ChangeExtension(filePath, null);

            TestGraph = EditorGraph.Load(basePath, _selectionManager);

            // Undo履歴をクリア
            UndoRedoManager.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(TestGraph.CurrentFilePath))
            await SaveAsAsync();
        else
            TestGraph.Save();
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (_mainWindow == null) return;

        var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Graph File",
            FileTypeChoices =
            [
                new FilePickerFileType("NodeGraph Files")
                {
                    Patterns = ["*.graph.yml"]
                }
            ],
            DefaultExtension = "graph.yml",
            SuggestedFileName = "untitled"
        });

        if (file != null)
        {
            var filePath = file.Path.LocalPath;
            // 拡張子を除いたベース名を取得
            var basePath = Path.ChangeExtension(filePath, null);

            TestGraph.Save(basePath);
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (_mainWindow != null) _mainWindow.Close();
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        UndoRedoManager.Undo();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        UndoRedoManager.Redo();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand]
    private async Task CopyMermaidToClipboardAsync()
    {
        if (_mainWindow == null) return;

        var mermaidGraph = TestGraph.Graph.ToMermaid();
        var clipboard = _mainWindow.Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync(mermaidGraph);
    }

    /// <summary>
    /// Undo/Redoの状態変更を通知します
    /// </summary>
    public void NotifyUndoRedoCanExecuteChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }
}