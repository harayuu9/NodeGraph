# CLAUDE.md

このファイルはこのリポジトリで作業する際のClaude Code (claude.ai/code) へのガイダンスを提供します。

## プロジェクト概要

NodeGraphは、C#と.NET 9で構築されたビジュアルノードベースプログラミングシステムです。ノード&ワイヤーインターフェースを通じて計算グラフを作成・実行するクロスプラットフォームのデスクトップアプリケーションを提供します。

**技術スタック:**
- C# 12 with preview features (.NET 9.0)
- Avalonia 11.3.8 (クロスプラットフォームUIフレームワーク)
- MVVMパターン with CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Roslynソースジェネレータによるコード生成
- YamlDotNet (シリアライゼーション)

## アーキテクチャ

プロジェクトは**3層アーキテクチャ**に従います:

1. **NodeGraph.Model** (.NET 9.0 / .NET Standard 2.1) - UI依存なしのコアグラフエンジン
   - グラフ表現とノード管理
   - 型安全な接続を持つポートシステム
   - トポロジカルソートによる非同期グラフ実行エンジン
   - メモリ最適化のためのオブジェクトプーリングユーティリティ
   - ポート型変換システム (異なる型間の自動変換)
   - プロパティシステム (ノードの編集可能なパラメータ)
   - YAML形式でのグラフシリアライゼーション

2. **NodeGraph.Editor** (.NET 9.0) - AvaloniaベースのUIアプリケーション
   - ViewModelsとカスタムControlsを使用したMVVMアーキテクチャ
   - パン/ズーム可能なビジュアライゼーション用のGraphControl
   - レンダリング用のNodeControlとPortControl
   - SelectionManagerによる選択管理システム
   - UIレイアウトの永続化 (.layout.yml)
   - プロパティUIの自動生成 (PropertyControlFactory)
   - 実行ステータスのビジュアルフィードバック

3. **NodeGraph.Generator** (.NET Standard 2.0) - Roslynソースジェネレータ
   - `[Node]`属性で装飾されたノード用のボイラープレートコードを生成
   - `[Input]`/`[Output]`フィールドからポート配列とプロパティアクセサを自動作成
   - `BeforeExecute()`/`AfterExecute()`メソッドの生成 (値のコピー処理)
   - `GetProperties()`メソッドの生成 (PropertyDescriptor配列)
   - デフォルトコンストラクタとデシリアライゼーションコンストラクタの生成

## 開発コマンド

### ビルド
```bash
dotnet build NodeGraph.slnx
```

### テスト実行
```bash
# 全テスト実行
dotnet test NodeGraph.slnx

# 特定プロジェクトのテスト実行
dotnet test test\NodeGraph.UnitTest\NodeGraph.UnitTest.csproj

# カバレッジ付きでテスト実行
dotnet test NodeGraph.slnx --collect:"XPlat Code Coverage"
```

### Editorアプリケーションの実行
```bash
dotnet run --project src\NodeGraph.Editor\NodeGraph.Editor.csproj
```

### ビルド成果物のクリーン
```bash
dotnet clean NodeGraph.slnx
```

## 主要なアーキテクチャパターン

### ソースコード生成
ノードは属性を使用して定義されます:
```csharp
[Node]
public partial class MyNode : Node
{
    [Input] private InputPort<float> _input;
    [Output] private OutputPort<float> _output;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        // 実装
    }
}
```
ソースジェネレータが必要なポート配列とプロパティを作成します。

### グラフ実行
- `GraphExecutor`がトポロジカルソートを実行して実行順序を決定
- ノードは依存関係を尊重して非同期に実行
- サイクル検出により無限ループを防止
- 全ノードの例外を集約して一度に投げる
- コールバック機構: `onExecute`, `onExecuted`, `onExcepted`
- `ArrayPool`使用によるメモリ効率化

### ポートシステム
- コンパイル時の型安全性を持つジェネリック`InputPort<T>`と`OutputPort<T>`
- ポート接続時のランタイム型チェック
- 双方向接続追跡
- セッター経由での値伝播
- **SingleConnectPort** - 単一接続のみ (主に入力ポート)
- **MultiConnectPort** - 複数接続可能 (主に出力ポート)

### ポート型変換システム
異なる型のポート間の接続を可能にする自動型変換:

- `PortTypeConverterProvider` - グローバルな型変換プロバイダ
  - `ConcurrentDictionary`によるコンバータキャッシュ
  - `Register<TFrom, TTo>()`でカスタムコンバータを登録可能

- 組み込みコンバータ:
  - `IdentityConverter<T>` - 同一型 (変換不要)
  - `AssignableConverter<TFrom, TTo>` - 継承/実装関係
  - `NullableConverter<T>` - T → T? 変換
  - `FuncConverter<TFrom, TTo>` - ラムダ式ベースのカスタム変換

- 事前登録された数値型変換 (C#の暗黙的型変換ルールに従う):
  - byte → short, int, long, float, double, decimal
  - short → int, long, float, double, decimal
  - int → long, float, double, decimal など

### プロパティシステム
ノードに編集可能なパラメータを追加:

**属性:**
- `[Property]` - フィールドを編集可能プロパティとしてマーク
  - `DisplayName` - UI表示名
  - `Category` - カテゴリ分類
  - `Tooltip` - ツールチップテキスト
- `[Range(min, max)]` - 数値プロパティの範囲指定
- `[Multiline(lines)]` - 文字列を複数行入力にする
- `[ReadOnly]` - 読み取り専用マーク

**PropertyDescriptor:**
- プロパティのメタデータとアクセサを提供
- Getter/Setterデリゲート
- 属性情報の取得

**PropertyViewModel:**
- プロパティの双方向データバインディング
- 自動型変換 (`Convert.ChangeType`)
- ノードへの値の自動反映

**PropertyControlFactory:**
- プロパティの型と属性に基づいてUIコントロールを自動生成
  - Range属性 + 数値型 → Slider + TextBox
  - Multiline属性 + string → 複数行TextBox
  - 数値型 → NumericUpDown
  - bool → CheckBox
  - enum → ComboBox

### シリアライゼーションシステム
グラフとレイアウトの永続化:

**GraphSerializer (NodeGraph.Model):**
- YAML形式でグラフ構造を保存/読み込み (`.graph.yml`)
- バージョン管理 (現在1.0.0)
- 2パス読み込み:
  1. 全ノード作成 (デシリアライズコンストラクタ使用)
  2. 接続確立
- ノードタイプ検索 (リフレクション)
- プロパティ値の自動型変換
- ポートIDとノードIDの保存による整合性維持

**GraphData:**
- Version - ファイルバージョン
- Nodes - NodeDataのリスト (Id, Type, Properties, Ports)
- Connections - ConnectionDataのリスト (Source, Target)

**EditorLayoutSerializer (NodeGraph.Editor):**
- UIレイアウトを別ファイルに保存 (`.layout.yml`)
- ノード位置情報 (X, Y)
- バージョン管理 (1.0.0)

**GraphMigrator:**
- 将来のバージョンマイグレーション用フレームワーク
- マイグレーション関数の登録機構 (現在は拡張ポイントのみ)

### オブジェクトプーリング
パフォーマンス重視のコードではプーリングユーティリティを使用:

スレッドセーフなオブジェクトプーリング (`ConcurrentBag<T>`ベース):
- `ListPool<T>` - List<T>インスタンスのプール
- `DictionaryPool<TKey, TValue>` - Dictionaryインスタンスのプール
- `HashSetPool<T>` - HashSetインスタンスのプール

全プールが最大容量制限を強制してメモリリークを防止します。

使用例:
```csharp
var list = ListPool<int>.Rent();
try
{
    // listを使用
}
finally
{
    ListPool<int>.Return(list);
}

// またはIDisposableハンドル使用
using var rental = ListPool<int>.RentDisposable();
var list = rental.Value;
// 自動的に返却される
```

**DisposableBag:**
複数のIDisposableリソースを一括管理:
```csharp
var bag = default(DisposableBag);
resource1.AddTo(ref bag);
resource2.AddTo(ref bag);
bag.Dispose(); // 全リソースを一括破棄
```

## プロジェクト構造の詳細

### NodeGraph.Model/
コアグラフエンジンコンポーネント:
- `Graph.cs` - メイングラフコンテナ、ノード管理、executorの作成
- `GraphExecutor.cs` - トポロジカルソート付き非同期実行エンジン
- `Node/Node.cs` - 全ノードの抽象基底クラス
- `Port/InputPort.cs` / `Port/OutputPort.cs` - ジェネリックポート実装
- `Port/SingleConnectPort.cs` / `Port/MultiConnectPort.cs` - 接続数制御
- `Node/` - 具体的なノード実装 (FloatAddNode, FloatConstantNode, etc.)
- `PortTypeConverter/` - 型変換システム
  - `PortTypeConverterProvider.cs` - グローバルプロバイダ
  - `IdentityConverter.cs`, `AssignableConverter.cs`, `NullableConverter.cs`, `FuncConverter.cs`
- `PropertyAttribute.cs` / `PropertyDescriptor.cs` - プロパティシステム
- `Serialization/` - グラフのシリアライゼーション
  - `GraphSerializer.cs` - YAML保存/読み込み
  - `GraphData.cs` - データ構造
  - `GraphMigrator.cs` - バージョンマイグレーション
- `Pool/` - スレッドセーフなオブジェクトプーリング実装
- `Structure.cs` - コア record structs (PortId, NodeId)
- `DisposableBag.cs` - リソース管理ユーティリティ
- `Attribute.cs` - ノード定義用の属性 ([Node], [Input], [Output])

### NodeGraph.Editor/
UIレイヤーコンポーネント:
- `Models/` - MVVMモデル層
  - `EditorGraph.cs` - Model.GraphのラッパーでUI状態を追加
  - `EditorNode.cs` - Model.NodeのラッパーでX/Y座標と実行ステータスを追加
  - `EditorPort.cs` - Model.Portのラッパー
  - `EditorConnection.cs` - 接続の視覚表現
  - `ExecutionStatus.cs` - 実行ステータス列挙型 (None, Waiting, Executing, Executed, Exception)
- `ViewModels/` - CommunityToolkit.MvvmのViewModels
  - `MainWindowViewModel.cs` - メインウィンドウ、ファイル操作、実行コマンド
  - `AddNodeWindowViewModel.cs` - ノード追加ウィンドウ、検索機能
  - `PropertyViewModel.cs` - プロパティの双方向バインディング
- `Controls/` - カスタムAvaloniaコントロール
  - `GraphControl.cs` - パン/ズーム付きメインキャンバス (MatrixTransform使用)
  - `NodeControl.cs` - ノードの視覚表現
  - `PortControl.cs` - ポートのレンダリングと接続操作
  - `ConnectorControl.cs` - ベジェ曲線による接続線描画
  - `ExecutionStatusBadgeControl.cs` - 実行ステータス表示バッジ
  - `PropertyControlPresenter.cs` - プロパティUIコントロールのプレゼンター
  - `PropertyControlFactory.cs` - 型と属性に基づくUIコントロール自動生成
  - `GridDecorator.cs` - グリッド背景描画
- `Primitives/ConnectorLine.cs` - 2点間の線分表現 (record struct)
- `Selection/` - 選択管理システム
  - `SelectionManager.cs` - 選択アイテムの一元管理
  - `ISelectable.cs` / `IPositionable.cs` / `IRectangular.cs` - インターフェース
- `Serialization/` - UIレイアウトのシリアライゼーション
  - `EditorLayoutSerializer.cs` - ノード位置の保存/読み込み
  - `LayoutData.cs` - レイアウトデータ構造
- `Services/NodeTypeService.cs` - ノードタイプの検出と検索
- `Converters/TypeToColorConverter.cs` - ポート型名から色への変換
- `ViewLocator.cs` - ViewModelからViewへの自動マッピング

### NodeGraph.Generator/
ソースジェネレーション:
- `SourceGenerator.cs` - IIncrementalGenerator実装
- `Emitter.cs` - ノード用のC#コード生成
  - デフォルトコンストラクタ
  - デシリアライゼーションコンストラクタ
  - BeforeExecute() / AfterExecute()
  - GetProperties()
  - ポート名取得メソッド
- `CSharpCodeGenerator.cs` - コード生成ヘルパー
- `StringCaseConverter.cs` - フィールド名変換

## 重要な設定

### InternalsVisibleTo
NodeGraph.Modelは内部APIを以下に公開:
- `NodeGraph.UnitTest` - 内部APIのテスト用
- `NodeGraph.Editor` - 内部状態へのUIバインディング用

### マルチターゲット
NodeGraph.Modelは`net9.0`と`netstandard2.1`の両方をターゲットにして、異なるコンシューマーとの互換性を確保しています。

### SDK要件
プロジェクトは.NET 9.0 SDKを必要とします (`global.json`参照)。最新のマイナーバージョンにロールフォワードします。

### ソリューション形式
このプロジェクトは従来の`.sln`形式ではなく、新しい`.slnx`ソリューション形式 (XMLベース) を使用しています。Visual Studio 2022+とRiderの両方がこの形式をサポートしています。

## テストアプローチ

テストはxUnitを使用し、以下のパターンに従います:
- `Graph`インスタンスを作成
- `graph.CreateNode<T>()`を使用してノードを追加・設定
- ノード間でポートを接続
- executorを作成: `var executor = graph.CreateExecutor()`
- 実行: `await executor.ExecuteAsync()`
- 出力ノードから結果をアサート

例:
```csharp
[Fact]
public async Task Test1()
{
    var graph = new Graph();
    var constant = graph.CreateNode<FloatConstantNode>();
    constant.SetValue(100);

    var result = graph.CreateNode<FloatResultNode>();
    result.Input.ConnectFrom(constant.Output);

    var executor = graph.CreateExecutor();
    await executor.ExecuteAsync();

    Assert.Equal(100, result.Value);
}
```

## ノードの操作

### 新しいノードタイプの作成
1. `Node`を継承するクラスを作成
2. クラスに`[Node]`属性を追加 (`partial`にする)
3. 入力ポート用に`[Input]`フィールドを追加
4. 出力ポート用に`[Output]`フィールドを追加
5. `ExecuteAsync(CancellationToken ct)`メソッドを実装
6. ソースジェネレータがポート配列とプロパティを作成します

例 (プロパティ付きノード):
```csharp
[Node(displayName: "定数", directory: "Math")]
public partial class FloatConstantNode : Node
{
    [Output] private OutputPort<float> _output;
    [Property(DisplayName = "値")] private float _value;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        _output.Value = _value;
        return Task.CompletedTask;
    }
}
```

### ノード実行順序
ノードは依存関係のトポロジカルソートに基づいて実行されます。ノードは全ての入力依存関係が完了した後にのみ実行されます。

### 実行フロー
```
GraphExecutor.ExecuteAsync()
  → トポロジカルソート
  → 各ノードをTask.Runで起動
    → BeforeExecute() (InputPort値をフィールドへコピー)
    → ExecuteAsync() (ユーザー実装)
    → AfterExecute() (フィールド値をOutputPortへコピー)
      → OutputPort.Value setter
        → 接続された全InputPortへ値を伝播
```

## UIエディタの機能

### ファイル操作
- **New** - 新規グラフ作成
- **Open** - `.graph.yml`と`.layout.yml`を読み込み
- **Save** - 両ファイルを保存
- **Save As** - 新しい場所に保存
- **Exit** - アプリケーション終了

### グラフ編集
- ノードのドラッグ&ドロップ
- ポート間の接続作成
- 矩形選択による複数選択
- ノードプロパティの編集 (PropertyControlPresenter経由)
- ノードの追加 (AddNodeWindow, NodeTypeService)

### グラフ実行
- `ExecuteAsync()`コマンド
- 実行中はUIをブロック (TouchGuard)
- ステータス可視化 (ExecutionStatusBadgeControl)
- 例外発生時の表示
- 完了5秒後に自動ステータスリセット

### ビジュアル機能
- パン/ズーム (GraphControl)
- グリッド背景 (GridDecorator)
- 型による色分け (TypeToColorConverter)
- ベジェ曲線接続線 (ConnectorControl)
- テーマ切り替え (Default/Light/Dark)

## 主要なデータフロー

### 値伝播
```
OutputPort<T>.Value = value
  → ConnectedPorts走査
    → InputPort.SetValueFrom<T>(value)
      → PortTypeConverter.Convert()
      → InputPort<T>.Value = convertedValue
```

### グラフ保存/読み込み
```
保存:
EditorGraph.Save()
  → GraphSerializer.SaveToYaml() → example.graph.yml
  → EditorLayoutSerializer.SaveLayout() → example.layout.yml

読み込み:
EditorGraph.Load()
  → GraphSerializer.LoadFromYaml() → ノード作成 + 接続
  → EditorLayoutSerializer.LoadLayout() → ノード位置復元
```

## 依存性の方向
- Model → 依存なし (完全に独立、UIフレームワークに依存しない)
- Editor → Model (Modelの上に構築されたUIレイヤー)
- Generator → Model (属性情報のみ参照)

## メモリ最適化戦略
- ArrayPool使用 (GraphExecutor, DisposableBag)
- オブジェクトプール (List, Dictionary, HashSet)
- DisposableBagによる一括リソース管理
- 構造体の活用 (ConnectorLine, PortId, NodeId)
- ConcurrentBagによるスレッドセーフなプーリング
