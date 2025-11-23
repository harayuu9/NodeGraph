# NodeGraph

NodeGraph は、C# と .NET 9 で構築された、モダンで高性能なビジュアルノードベースプログラミングシステムです。
Avalonia UI を採用しており、クロスプラットフォーム（Windows, macOS, Linux）で動作します。

![NodeGraph Screenshot](docs/images/screenshot.png)
*(スクリーンショットはイメージです)*

## ✨ 主な機能

- **ハイパフォーマンス**: `ArrayPool` やオブジェクトプーリングを積極的に活用し、メモリ割り当てを最小限に抑えた設計。
- **非同期実行**: `Task` ベースの実行エンジンにより、UIをブロックすることなく複雑な計算フローを実行可能。
- **強力な型システム**: ジェネリクスを活用した型安全なポート接続と、柔軟な型変換システム。
- **拡張性**: Roslyn Source Generator により、属性 (`[Node]`, `[Input]`, `[Output]`) を付けるだけで簡単にカスタムノードを作成可能。
- **モダンなUI**: Avalonia UI によるスムーズなパン・ズーム操作と、MVVMパターンによるクリーンな設計。
- **永続化**: YAML形式でのグラフ保存・読み込みと、Undo/Redoの完全サポート。

## 🚀 クイックスタート

### 必要要件
- .NET 9.0 SDK

### ビルド
```bash
dotnet build NodeGraph.slnx
```

### エディタの実行
```bash
dotnet run --project src\NodeGraph.Editor\NodeGraph.Editor.csproj
```

### テストの実行
```bash
dotnet test NodeGraph.slnx
```

## 📖 ドキュメント

詳細なドキュメントは `docs/` ディレクトリにあります。

- [アーキテクチャ解説](docs/Architecture.md): システムの全体構造と設計思想について
- [ノードの作成方法](docs/HowToCreateNode.md): 新しいノードタイプを追加するチュートリアル

## 🛠️ 技術スタック

- **言語**: C# 12 (.NET 9.0)
- **UIフレームワーク**: Avalonia UI 11.3.8
- **MVVM**: CommunityToolkit.Mvvm
- **メタプログラミング**: Roslyn Source Generators
- **シリアライゼーション**: YamlDotNet

## 📄 ライセンス

このプロジェクトは MIT ライセンスの下で公開されています。
