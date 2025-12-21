using Avalonia;
using Avalonia.Controls.Primitives;
using NodeGraph.Editor.Models;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// ノードの実行ステータスを表示するバッジコントロール
/// </summary>
public class ExecutionStatusBadgeControl : TemplatedControl
{
    public static readonly StyledProperty<ExecutionStatus> ExecutionStatusProperty =
        AvaloniaProperty.Register<ExecutionStatusBadgeControl, ExecutionStatus>(nameof(ExecutionStatus));

    public ExecutionStatus ExecutionStatus
    {
        get => GetValue(ExecutionStatusProperty);
        set => SetValue(ExecutionStatusProperty, value);
    }
}