using Avalonia;
using Avalonia.Media;

namespace NodeGraph.Editor.Controls;

/// <summary>
/// GraphControlのステート管理部分
/// パン・ズーム・選択・ドラッグなどの一時的なUIステートを管理します
/// </summary>
public partial class GraphControl
{
    // ドラッグ状態
    private Point _lastDragPoint;
    private bool _isDragging;
    private bool _isRightButtonDown;
    private Point _rightButtonDownPoint;

    // コネクタ管理
    private bool _connectorUpdateScheduled;

    // 矩形選択用
    private bool _isSelecting;
    private Point _selectionStartPoint;

    // ポートドラッグ用
    private bool _isDraggingPort;
    private PortControl? _dragSourcePort;
    private PortControl? _currentHoverPort;

    // トランスフォーム
    private readonly TranslateTransform _translateTransform = new();
    private readonly ScaleTransform _scaleTransform = new() { ScaleX = 1.0, ScaleY = 1.0 };
    private readonly TransformGroup _transformGroup;

    /// <summary>
    /// ズームを更新します
    /// </summary>
    private void UpdateZoom()
    {
        _scaleTransform.ScaleX = Zoom;
        _scaleTransform.ScaleY = Zoom;
    }

    /// <summary>
    /// パンオフセットを更新します
    /// </summary>
    private void UpdatePan()
    {
        _translateTransform.X = PanOffset.X;
        _translateTransform.Y = PanOffset.Y;
    }
}
