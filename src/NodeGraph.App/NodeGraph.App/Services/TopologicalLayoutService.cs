using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using NodeGraph.App.Models;
using NodeGraph.App.Undo;
using NodeGraph.Model;
using NodeGraph.Model.Pool;

namespace NodeGraph.App.Services;

/// <summary>
/// ポート順制約と右詰めレイヤリングを備えたレイアウト
/// </summary>
public class TopologicalLayoutService : IGraphLayoutService
{
    public MoveNodesAction CreateArrangeAction(
        IEnumerable<EditorNode> nodes,
        EditorGraph graph,
        Func<EditorNode, (double width, double height)> getNodeSize)
    {
        var selected = nodes.Distinct().ToList();
        if (selected.Count == 0) return new MoveNodesAction([], []);

        var old = selected.Select(n => (n, n.X, n.Y)).ToList();

        // 層決定（右詰め）
        var layers = ComputeLayersRightPushed(selected, graph);
        if (layers.Count == 0) return new MoveNodesAction([], []);

        using var selectedSetRental = selected.ToHashSetFromPool(out var selectedSet);
        var edges = graph.Connections
            .Where(c => selectedSet.Contains(c.SourceNode) && selectedSet.Contains(c.TargetNode))
            .ToList();

        var nodeSize = selected.ToDictionary(n => n, getNodeSize);

        // ===== 2) 交差削減：ポート順制約 + バリセンタ =====
        // next層の順に対し、prev層を並べ替える（後ろ→前パス）
        for (var i = layers.Count - 2; i >= 0; --i)
            OrderPrevLayerWithPortConstraints(layers[i], layers[i + 1], edges);

        // ついでに前→後パスでバリセンタ微調整（親の並びに寄せる）
        for (var i = 1; i < layers.Count; ++i)
            OrderLayerByBarycenter(layers[i], layers[i - 1], edges, true);

        // ===== 3) X座標：依存に基づくコンパクション =====
        const double minHorizontal = 80.0;
        var baseX = selected.Min(n => n.X);
        var baseY = selected.Min(n => n.Y);

        // layer 0
        foreach (var n in layers[0]) n.X = baseX;

        // layer >=1
        for (var i = 1; i < layers.Count; ++i)
        {
            var x = baseX + minHorizontal; // 下限
            foreach (var v in layers[i])
            {
                // 親の右端 + 最小間隔 の最大値
                var parents = edges.Where(e => e.TargetNode == v).Select(e => e.SourceNode).Distinct().ToList();
                foreach (var p in parents) x = Math.Max(x, p.X + nodeSize[p].width + minHorizontal);
            }

            foreach (var v in layers[i]) v.X = x;
        }

        // ===== 4) Y座標：適応的な余白配分（重み付き単調回帰） =====
        const double baseGap = 24.0; // 最小基準ギャップ（以前の verticalSpacing よりやや広め）
        const double degGapPx = 3.0; // 次数による加算（px/edge）…混雑ほど広く
        const double bcGapPx = 8.0; // 次層バリセンタ差による加算（px/index差）

        // 事前集計
        var outMap = edges.GroupBy(e => e.SourceNode).ToDictionary(g => g.Key, g => g.ToList());
        var inMap = edges.GroupBy(e => e.TargetNode).ToDictionary(g => g.Key, g => g.ToList());

        // 右端（シンク側）から順に Y を決める。
        // 最終層はアンカーがないので「フォールバック＋可変ギャップ」で配置し、
        // それより左の層は「次層アンカー」を見て最小二乗でフィットする。
        for (var i = layers.Count - 1; i >= 0; --i)
        {
            var next = i + 1 < layers.Count ? layers[i + 1] : null;
            PlaceLayerByIsotonic(
                layers[i],
                next,
                baseY,
                nodeSize,
                outMap,
                inMap,
                baseGap,
                degGapPx,
                bcGapPx
            );
        }

        // ---- Undo/Redo アクションを生成 ----
        var nodePositions = old.Select(o => (o.n, o.X, o.Y, o.n.X, o.n.Y)).ToList();
        var targets = nodePositions.Select(p => p.n).ToArray();
        var newPositions = nodePositions.Select(p => new Point(p.Item4, p.Item5)).ToArray();
        var action = new MoveNodesAction(targets, newPositions);

        // Undoのため元に戻す
        foreach (var (n, ox, oy, _, _) in nodePositions)
        {
            n.X = ox;
            n.Y = oy;
        }

        return action;

        // ========== ヘルパ ==========
        static void PlaceLayerByIsotonic(
            List<EditorNode> curLayer,
            List<EditorNode>? nextLayer,
            double baseY,
            Dictionary<EditorNode, (double width, double height)> nodeSize,
            Dictionary<EditorNode, List<EditorConnection>> outMap,
            Dictionary<EditorNode, List<EditorConnection>> inMap,
            double baseGap,
            double degGapPx,
            double bcGapPx)
        {
            if (curLayer.Count == 0) return;

            var n = curLayer.Count;
            var centersStar = new double[n]; // m*_i (理想センター)
            var heights = new double[n]; // h_i
            var weights = new double[n]; // 回帰の重み（次数に応じて）
            var degs = new int[n]; // 次数
            var bc = new double[n]; // 次層に対するバリセンタ（なければ NaN）

            // 次層インデックス（バリセンタ計算用）
            var nextIndex = nextLayer == null
                ? new Dictionary<EditorNode, int>()
                : nextLayer.Select((n, i) => (n, i)).ToDictionary(t => t.n, t => t.i);

            for (var i = 0; i < n; i++)
            {
                var u = curLayer[i];
                var (uw, uh) = nodeSize[u];
                heights[i] = uh;

                var inCnt = inMap.TryGetValue(u, out var inE) ? inE.Count : 0;
                var outCnt = outMap.TryGetValue(u, out var outE) ? outE.Count : 0;
                degs[i] = inCnt + outCnt;

                // 次層に対するバリセンタ（ターゲットノードの平均インデックス）
                if (outMap.TryGetValue(u, out var outs))
                {
                    var idxList = outs.Where(e => nextIndex.ContainsKey(e.TargetNode))
                        .Select(e => nextIndex[e.TargetNode])
                        .ToList();
                    bc[i] = idxList.Count > 0 ? idxList.Average() : double.NaN;
                }
                else
                {
                    bc[i] = double.NaN;
                }

                // 理想センター m*_i：出力先（次層）と入力元（前層）の両方のアンカーを考慮
                using var anchorsRental = ListPool<double>.Shared.Rent(out var anchors);

                // 出力先（次層）のアンカー：既に配置済みなので正確
                if (outMap.TryGetValue(u, out var outEdges))
                    foreach (var e in outEdges)
                    {
                        if (!nextIndex.ContainsKey(e.TargetNode)) continue;
                        var v = e.TargetNode;
                        var vh = nodeSize[v].height;
                        var inCount = GetInputPortCount(v);
                        inCount = Math.Max(inCount, 1);
                        var pidx = GetTargetPortIndex(e);
                        pidx = Math.Clamp(pidx, 0, inCount - 1);
                        var gap = vh / (inCount + 1);
                        var anchor = v.Y + gap * (pidx + 1); // v の入力ポートアンカーY
                        anchors.Add(anchor);
                    }

                // 入力元（前層）のアンカー：暫定位置を使って推定（重み軽め）
                if (inMap.TryGetValue(u, out var inEdges))
                    foreach (var e in inEdges)
                    {
                        var src = e.SourceNode;
                        var srcH = nodeSize[src].height;
                        // 出力ポートは下部に配置されると仮定（簡易推定）
                        // より正確にしたい場合は GetOutputPortCount/GetSourcePortIndex を追加
                        var srcAnchor = src.Y + srcH * 0.75; // 暫定的に下から1/4の位置
                        anchors.Add(srcAnchor);
                    }

                if (anchors.Count == 0)
                    // フォールバック：単純な縦積みの目安（層の順序は既に交差削減で決まっている）
                    centersStar[i] = baseY + i * (uh + baseGap) + uh / 2.0;
                else
                    centersStar[i] = Median(anchors);

                // 回帰の重み：次数が大きいほど理想に寄せたい
                weights[i] = 1.0 + 0.25 * degs[i];
            }

            // 最小センター間隔 Δ_i を可変に（混雑/バリセンタ差で増やす）
            var delta = new double[Math.Max(0, n - 1)];
            for (var i = 0; i < delta.Length; i++)
            {
                var degAdd = degGapPx * 0.5 * (degs[i] + degs[i + 1]); // 片側平均
                var bcAdd = !double.IsNaN(bc[i]) && !double.IsNaN(bc[i + 1])
                    ? bcGapPx * Math.Abs(bc[i + 1] - bc[i])
                    : 0.0;
                // 必要なら上限を掛けて暴れを抑制
                var extra = degAdd + bcAdd;
                extra = Math.Min(extra, 120.0); // safety cap
                delta[i] = (heights[i] + heights[i + 1]) / 2.0 + baseGap + extra;
            }

            // --- 離隔制約つき単調回帰に変換： t_i = m_i − Σ_{k<i} Δ_k ---
            var offset = new double[n];
            for (var i = 1; i < n; i++) offset[i] = offset[i - 1] + delta[i - 1];
            var tStar = new double[n];
            for (var i = 0; i < n; i++) tStar[i] = centersStar[i] - offset[i];

            // PAV（Pool-Adjacent-Violators）で重み付き単調回帰
            WeightedIsotonicRegression(tStar, weights);

            // 復元して Y を適用
            for (var i = 0; i < n; i++)
            {
                var center = tStar[i] + offset[i];
                curLayer[i].Y = center - heights[i] / 2.0;
            }
        }

        // 中央値ユーティリティ
        static double Median(List<double> xs)
        {
            xs.Sort();
            var m = xs.Count / 2;
            return xs.Count % 2 == 1 ? xs[m] : 0.5 * (xs[m - 1] + xs[m]);
        }

        // 重み付き PAV（単調非減少）
        // y: 目標値（上書きして解を返す）, w: 各点の重み（>0）
        static void WeightedIsotonicRegression(double[] y, double[] w)
        {
            var n = y.Length;
            var v = new double[n];
            var vw = new double[n];
            var len = new int[n];
            var m = 0;
            for (var i = 0; i < n; i++)
            {
                v[m] = y[i];
                vw[m] = w[i] > 0 ? w[i] : 1.0;
                len[m] = 1;
                while (m > 0 && v[m - 1] > v[m])
                {
                    var s = vw[m - 1] + vw[m];
                    v[m - 1] = (vw[m - 1] * v[m - 1] + vw[m] * v[m]) / s;
                    vw[m - 1] = s;
                    len[m - 1] += len[m];
                    m--;
                }

                m++;
            }

            // 展開
            var k = 0;
            for (var i = 0; i < m; i++)
            for (var j = 0; j < len[i]; j++)
                y[k++] = v[i];
        }

        static void OrderPrevLayerWithPortConstraints(
            List<EditorNode> prev, List<EditorNode> next,
            List<EditorConnection> edges)
        {
            // nextのインデックス
            var nextPos = next.Select((n, i) => (n, i)).ToDictionary(t => t.n, t => t.i);

            // 順序制約：同一ターゲットの入力はポート番号昇順
            var before = prev.ToDictionary(n => n, _ => new HashSet<EditorNode>());
            var indeg = prev.ToDictionary(n => n, _ => 0);

            foreach (var t in next)
            {
                var inc = edges.Where(e => e.TargetNode == t && prev.Contains(e.SourceNode))
                    .OrderBy(GetTargetPortIndex)
                    .ToList();
                for (var j = 1; j < inc.Count; ++j)
                {
                    var a = inc[j - 1].SourceNode;
                    var b = inc[j].SourceNode;
                    if (before[a].Add(b)) indeg[b]++;
                }
            }

            // 優先度（barycenter with next）: つながるnextの平均位置
            var score = new Dictionary<EditorNode, double>();
            foreach (var u in prev)
            {
                var outs = edges.Where(e => e.SourceNode == u && nextPos.ContainsKey(e.TargetNode)).ToList();
                score[u] = outs.Count == 0
                    ? double.PositiveInfinity
                    : outs.Average(e => nextPos[e.TargetNode] + 0.001 * GetTargetPortIndex(e));
            }

            // 制約つきトポロジカル順（Kahn）＋ score最小優先
            using var readyRental = prev.Where(n => indeg[n] == 0).ToListFromPool(out var ready);
            using var orderRental = ListPool<EditorNode>.Shared.Rent(out var order);
            while (ready.Count > 0)
            {
                ready.Sort((a, b) => score[a].CompareTo(score[b]));
                var u = ready[0];
                ready.RemoveAt(0);
                order.Add(u);
                foreach (var v in before[u])
                    if (--indeg[v] == 0)
                        ready.Add(v);
            }

            if (order.Count == prev.Count)
            {
                prev.Clear();
                prev.AddRange(order);
            }
            else
            {
                // 制約に矛盾がある場合はスコア順のみ
                prev.Sort((a, b) => score[a].CompareTo(score[b]));
            }
        }

        static void OrderLayerByBarycenter(List<EditorNode> cur, List<EditorNode> other, List<EditorConnection> edges, bool previousLayer)
        {
            var otherIndex = other.Select((n, i) => (n, i)).ToDictionary(t => t.n, t => t.i);
            cur.Sort((a, b) => GetScore(a).CompareTo(GetScore(b)));
            return;

            double GetScore(EditorNode n)
            {
                var indices = previousLayer
                    ? edges.Where(e => e.TargetNode == n && otherIndex.ContainsKey(e.SourceNode)).Select(e => otherIndex[e.SourceNode])
                    : edges.Where(e => e.SourceNode == n && otherIndex.ContainsKey(e.TargetNode)).Select(e => otherIndex[e.TargetNode]);
                var list = indices.ToList();
                return list.Count == 0 ? double.PositiveInfinity : list.Average();
            }
        }

        static int GetTargetPortIndex(EditorConnection connection)
        {
            return connection.TargetNode.InputPorts.IndexOf(connection.TargetPort);
        }

        static int GetInputPortCount(EditorNode editorNode)
        {
            return editorNode.InputPorts.Count;
        }
    }

    private static List<List<EditorNode>> ComputeLayersRightPushed(IEnumerable<EditorNode> nodes, EditorGraph graph)
    {
        var nodeList = nodes.Distinct().ToList();
        if (nodeList.Count == 0) return [];

        using var selectedRental = nodeList.ToHashSetFromPool(out var selected);
        var edges = graph.Connections
            .Where(c => selected.Contains(c.SourceNode) && selected.Contains(c.TargetNode))
            .ToList();

        var inAdj = nodeList.ToDictionary(n => n, _ => new List<EditorNode>());
        var outAdj = nodeList.ToDictionary(n => n, _ => new List<EditorNode>());

        foreach (var e in edges)
        {
            inAdj[e.TargetNode].Add(e.SourceNode);
            outAdj[e.SourceNode].Add(e.TargetNode);
        }

        // Kahnでトポロジカル順（選択サブグラフ）
        var indeg = nodeList.ToDictionary(n => n, n => inAdj[n].Count);
        var q = new Queue<EditorNode>(nodeList.Where(n => indeg[n] == 0));
        using var topoRental = ListPool<EditorNode>.Shared.Rent(out var topo);
        while (q.Count > 0)
        {
            var u = q.Dequeue();
            topo.Add(u);
            foreach (var v in outAdj[u])
                if (--indeg[v] == 0)
                    q.Enqueue(v);
        }

        if (topo.Count != nodeList.Count)
            // サイクルがある場合は素直に左寄せ長径で層決め（簡易解）
            topo = nodeList; // 既存順のまま

        // (a) 通常の左→右長径で初期レイヤ
        var layer = nodeList.ToDictionary(n => n, _ => 0);
        foreach (var u in topo)
            if (inAdj[u].Count > 0)
                layer[u] = inAdj[u].Max(p => layer[p] + 1);

        // (b) 右詰め：子の最小レイヤ−1 まで押し出す（1パスで十分：トポロジ逆順）
        for (var i = topo.Count - 1; i >= 0; --i)
        {
            var u = topo[i];
            if (outAdj[u].Count == 0) continue;
            var allowed = outAdj[u].Min(v => layer[v] - 1);
            if (allowed > layer[u]) layer[u] = allowed;
        }

        // 層をグループ化
        var maxL = layer.Values.DefaultIfEmpty(0).Max();
        var layers = Enumerable.Range(0, maxL + 1).Select(_ => new List<EditorNode>()).ToList();
        foreach (var kv in layer) layers[kv.Value].Add(kv.Key);

        // 孤立ノードは0層（そのまま）
        return layers;
    }
}