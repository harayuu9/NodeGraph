using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NodeGraph.Editor.Services;
using NodeGraph.Editor.ViewModels;

namespace NodeGraph.Editor.Views;

public partial class AddNodeWindow : Window
{
    private AddNodeWindowViewModel? ViewModel => DataContext as AddNodeWindowViewModel;

    public AddNodeWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 指定された位置にウィンドウを表示してノードタイプを取得
    /// </summary>
    public static async Task<NodeTypeInfo?> ShowDialog(Window owner, PixelPoint position,
        NodeTypeService nodeTypeService)
    {
        var window = new AddNodeWindow
        {
            DataContext = new AddNodeWindowViewModel(nodeTypeService)
        };

        // ウィンドウの位置を設定
        window.Position = position;

        window.Show();

        // ウィンドウが閉じられるまで待機
        while (window.ViewModel?.SelectedNodeType == null)
        {
            await Task.Delay(100);
        }

        return window.ViewModel.SelectedNodeType;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // 検索ボックスにフォーカスを設定
        var searchBox = this.FindControl<TextBox>("SearchBox");
        searchBox?.Focus();

        // TreeViewでのダブルクリックを処理
        var treeView = this.FindControl<TreeView>("NodeTreeView");
        if (treeView != null)
        {
            treeView.DoubleTapped += OnTreeViewDoubleTapped;
        }

        // Enterキーでの確定を処理
        KeyDown += OnWindowKeyDown;

        // フォーカスを失った時に閉じる（Window外クリック含む）
        Deactivated += OnDeactivated;

        // ボタンイベントを設定
        var cancelButton = this.FindControl<Button>("CancelButton");
        if (cancelButton != null)
        {
            cancelButton.Click += (_, _) => Close();
        }

        var confirmButton = this.FindControl<Button>("ConfirmButton");
        if (confirmButton != null)
        {
            confirmButton.Click += (_, _) =>
            {
                ViewModel?.ConfirmCommand.Execute(null);
                if (ViewModel?.SelectedNodeType != null)
                {
                    Close();
                }
            };
        }
    }

    private void OnDeactivated(object? sender, System.EventArgs e)
    {
        Close();
    }

    private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedItem?.NodeTypeInfo != null)
        {
            ViewModel.ConfirmCommand.Execute(null);
            if (ViewModel.SelectedNodeType != null)
            {
                Close();
            }
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Enterキーが押されたら選択を確定
            if (ViewModel?.SelectedItem?.NodeTypeInfo != null)
            {
                ViewModel.ConfirmCommand.Execute(null);
                if (ViewModel.SelectedNodeType != null)
                {
                    Close();
                }
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Escapeキーでキャンセル
            Close();
            e.Handled = true;
        }
    }
}