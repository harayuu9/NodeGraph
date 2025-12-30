using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NodeGraph.App.Selection;

namespace NodeGraph.App.Models;

/// <summary>
/// Port間の接続を表すエディタモデル
/// </summary>
public partial class EditorConnection : ObservableObject, ISelectable
{
    private readonly Guid _id = Guid.NewGuid();

    /// <summary>
    /// 接続の終了点X座標
    /// </summary>
    [ObservableProperty] private double _endX;

    /// <summary>
    /// 接続の終了点Y座標
    /// </summary>
    [ObservableProperty] private double _endY;

    /// <summary>
    /// 接続の開始点X座標
    /// </summary>
    [ObservableProperty] private double _startX;

    /// <summary>
    /// 接続の開始点Y座標
    /// </summary>
    [ObservableProperty] private double _startY;

    public EditorConnection(
        EditorNode sourceNode,
        EditorPort sourcePort,
        EditorNode targetNode,
        EditorPort targetPort)
    {
        if (!sourcePort.IsOutput)
            throw new ArgumentException("Source port must be an output port", nameof(sourcePort));
        if (!targetPort.IsInput)
            throw new ArgumentException("Target port must be an input port", nameof(targetPort));

        SourceNode = sourceNode;
        SourcePort = sourcePort;
        TargetNode = targetNode;
        TargetPort = targetPort;
    }

    /// <summary>
    /// 接続元のノード
    /// </summary>
    public EditorNode SourceNode { get; }

    /// <summary>
    /// 接続元のポート
    /// </summary>
    public EditorPort SourcePort { get; }

    /// <summary>
    /// 接続先のノード
    /// </summary>
    public EditorNode TargetNode { get; }

    /// <summary>
    /// 接続先のポート
    /// </summary>
    public EditorPort TargetPort { get; }

    /// <summary>
    /// この EditorConnection の一意な識別子
    /// </summary>
    public object SelectionId => _id;
}