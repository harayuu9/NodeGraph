using Avalonia;
using Avalonia.Media;

namespace NodeGraph.App.Controls;

/// <summary>
/// GraphControlのステート管理部分
/// パン・ズーム・選択・ドラッグなどの一時的なUIステートを管理します
/// </summary>
public partial class GraphControl
{
    private readonly ScaleTransform _scaleTransform = new() { ScaleX = 1.0, ScaleY = 1.0 };
    private readonly TransformGroup _transformGroup;

    // トランスフォーム
    private readonly TranslateTransform _translateTransform = new();

    // コネクタ管理
    private bool _connectorUpdateScheduled;
    private PortControl? _currentHoverPort;
    private PortControl? _dragSourcePort;
    private bool _isDragging;

    // ポートドラッグ用
    private bool _isDraggingPort;
    private bool _isRightButtonDown;

    // 矩形選択用
    private bool _isSelecting;

    // ドラッグ状態
    private Point _lastDragPoint;
    private Point _rightButtonDownPoint;
    private Point _selectionStartPoint;

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