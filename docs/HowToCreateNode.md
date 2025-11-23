# 新しいノードの作成方法

NodeGraph では、Source Generator のおかげで、非常に少ないコード量で新しいノードを作成できます。

## 基本ステップ

1. `Node` クラスを継承したクラスを作成します。
2. `[Node]` 属性をクラスに付与します。
3. クラスに `partial` キーワードを付けます。
4. 入出力ポートをフィールドとして定義し、属性を付けます。**フィールドの型はポートの値の型（例: `float`）を指定します。**
5. `ExecuteAsync` メソッドをオーバーライドしてロジックを実装します。

## 実装例：足し算ノード

以下は、2つの浮動小数点数を足し合わせるノードの完全な実装例です。

```csharp
using NodeGraph.Model;

namespace MyProject.Nodes;

// 1. Node属性でカテゴリと表示名を設定
[Node(DisplayName = "Add (Float)", Directory = "Math/Float")]
public partial class FloatAddNode : Node
{
    // 2. 入力ポートの定義 (型は float)
    [Input] private float _a;
    [Input] private float _b;

    // 3. 出力ポートの定義 (型は float)
    [Output] private float _result;

    // 4. 実行ロジックの実装
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        // 入力ポートの値は自動的にフィールドに代入されています
        // BeforeExecute() で InputPort.Value -> Field のコピーが行われます
        var sum = _a + _b;

        // 結果を出力ポート用のフィールドに設定
        _result = sum;

        // AfterExecute() で Field -> OutputPort.Value のコピーが行われます
        return Task.CompletedTask;
    }
}
```

## プロパティの追加

ノードに設定可能なパラメータ（プロパティ）を追加するには、`[Property]` 属性を使用します。

```csharp
[Node("Constant Value")]
public partial class FloatConstantNode : Node
{
    [Output] private float _output;

    // プロパティの定義
    [Property(DisplayName = "Value")]
    private float _value;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        _output = _value;
        return Task.CompletedTask;
    }
}
```

## 利用可能な属性

### クラス属性
- `[Node]`: ノードのメタデータを定義します。
    - `DisplayName`: UIに表示される名前
    - `Directory`: ノード検索メニューでのカテゴリパス（例: "Math/Basic"）

### フィールド属性
- `[Input]`: 入力ポートを定義します。フィールドの型がポートの型になります。
- `[Output]`: 出力ポートを定義します。
- `[Property]`: 編集可能なプロパティを定義します。
    - `DisplayName`: プロパティ名
    - `Tooltip`: ツールチップテキスト
- `[Range(min, max)]`: 数値プロパティの入力範囲を制限します（スライダーが表示されます）。
- `[Multiline]`: 文字列プロパティを複数行入力にします。

## 実行フロー制御ノード

`if` や `loop` のような制御フローを持つノードを作成する場合は、`ExecutionNode` を継承します。

```csharp
[ExecutionNode(HasExecIn = true, "True", "False")] // ExecInと2つのExecOutを持つ
public partial class IfNode : ExecutionNode
{
    [Input] private bool _condition;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        if (_condition)
        {
            TriggerExecOut(0); // "True" ポートを実行
        }
        else
        {
            TriggerExecOut(1); // "False" ポートを実行
        }
        return Task.CompletedTask;
    }
}
```
