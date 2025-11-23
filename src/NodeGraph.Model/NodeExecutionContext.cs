using System;
using System.Collections.Generic;
using System.Threading;

namespace NodeGraph.Model;

public class NodeExecutionContext
{
    public Node Node { get; }
    public CancellationToken CancellationToken { get; }

    private readonly HashSet<int> _triggeredExecOutIndices = [];

    public NodeExecutionContext(Node node, CancellationToken cancellationToken)
    {
        Node = node;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// 指定されたExecOutポートをトリガーします。
    /// </summary>
    public void TriggerExecOut(int index)
    {
        if (Node is not ExecutionNode execNode)
        {
            throw new InvalidOperationException("This node is not an ExecutionNode.");
        }

        if (index < 0 || index >= execNode.ExecOutPorts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        _triggeredExecOutIndices.Add(index);
    }

    /// <summary>
    /// トリガーされたExecOutポートのインデックスを取得します。
    /// TriggerExecOutが呼ばれていない場合は、全てのExecOutがトリガーされたとみなします。
    /// </summary>
    internal IEnumerable<int> GetTriggeredExecOutIndices()
    {
        if (Node is not ExecutionNode execNode)
        {
            yield break;
        }

        if (_triggeredExecOutIndices.Count == 0)
        {
            // デフォルトでは全てのExecOutをトリガー
            for (int i = 0; i < execNode.ExecOutPorts.Length; i++)
            {
                yield return i;
            }
        }
        else
        {
            foreach (var index in _triggeredExecOutIndices)
            {
                yield return index;
            }
        }
    }
}
