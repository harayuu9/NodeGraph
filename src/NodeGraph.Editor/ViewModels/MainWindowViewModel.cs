using System;
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
using NodeGraph.Editor.Undo;
using NodeGraph.Model;

namespace NodeGraph.Editor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SelectionManager _selectionManager;
    private readonly UndoRedoManager _undoRedoManager;
    private Window? _mainWindow;

    [ObservableProperty] private EditorGraph _testGraph;

    [ObservableProperty] private string _currentTheme = "Default";

    [ObservableProperty] private bool _isDefaultTheme = true;

    [ObservableProperty] private bool _isLightTheme = false;

    [ObservableProperty] private bool _isDarkTheme = false;

    public bool CanUndo => _undoRedoManager.CanUndo();
    public bool CanRedo => _undoRedoManager.CanRedo();

    public UndoRedoManager UndoRedoManager => _undoRedoManager;

#if DEBUG
    public MainWindowViewModel() : this(new SelectionManager(), new UndoRedoManager()) { }
#endif

    public MainWindowViewModel(SelectionManager selectionManager, UndoRedoManager undoRedoManager)
    {
        _selectionManager = selectionManager;
        _undoRedoManager = undoRedoManager;

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

        // EditorGraphでラップ（SelectionManagerを注入）
        _testGraph = new EditorGraph(graph, selectionManager);

        // ノードの位置を設定
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
        _undoRedoManager.Clear();
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
            FileTypeFilter = new[]
            {
                new FilePickerFileType("NodeGraph Files")
                {
                    Patterns = new[] { "*.graph.yml" }
                }
            }
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            // 拡張子を除いたベース名を取得
            var basePath = Path.ChangeExtension(filePath, null);

            TestGraph = EditorGraph.Load(basePath, _selectionManager);

            // Undo履歴をクリア
            _undoRedoManager.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(TestGraph.CurrentFilePath))
        {
            await SaveAsAsync();
        }
        else
        {
            TestGraph.Save();
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (_mainWindow == null) return;

        var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Graph File",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("NodeGraph Files")
                {
                    Patterns = new[] { "*.graph.yml" }
                }
            },
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
        if (_mainWindow != null)
        {
            _mainWindow.Close();
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _undoRedoManager.Undo();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _undoRedoManager.Redo();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
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