# NuGet パッケージ公開設定ガイド

このドキュメントでは、NodeGraph パッケージを NuGet.org に公開するための設定手順を説明します。

## 1. NuGet API キーの取得

### 1.1 NuGet.org アカウントの作成
1. [nuget.org](https://www.nuget.org/) にアクセス
2. 右上の「Sign in」からサインイン（Microsoft アカウントまたは NuGet アカウント）

### 1.2 API キーの作成
1. サインイン後、右上のユーザー名をクリック
2. 「API Keys」を選択、または直接 https://www.nuget.org/account/apikeys にアクセス
3. 「Create」ボタンをクリック

### 1.3 API キーの設定
以下の設定で API キーを作成します：

| 項目 | 設定値 |
|------|--------|
| **Key Name** | `NodeGraph-GitHub-Actions` (任意の識別名) |
| **Expiration** | 365 days (推奨) |
| **Scopes** | `Push` |
| **Select Packages** | `Glob Pattern` を選択し、`NodeGraph.*` を入力 |

4. 「Create」をクリック
5. **重要**: 表示された API キーをコピーして安全な場所に保存（再表示不可）

## 2. GitHub Secrets の設定

### 2.1 リポジトリ設定画面へのアクセス
1. GitHub リポジトリ (https://github.com/harayuu9/NodeGraph) を開く
2. 「Settings」タブをクリック
3. 左メニューから「Secrets and variables」→「Actions」を選択

### 2.2 シークレットの追加
1. 「New repository secret」ボタンをクリック
2. 以下の値を入力：

| 項目 | 値 |
|------|-----|
| **Name** | `NUGET_API_KEY` |
| **Secret** | 手順1で取得した API キー |

3. 「Add secret」をクリック

## 3. パッケージの公開

設定が完了したら、以下の手順でパッケージを公開できます。

### 3.1 タグの作成とプッシュ
```bash
# プレリリース版のタグを作成（プレリリース依存がある場合）
git tag v0.1.0-preview.1

# または安定版のタグを作成
git tag v0.1.0

# タグをリモートにプッシュ
git push origin v0.1.0-preview.1
```

> **注意**: `Microsoft.Extensions.AI.OpenAI` などのプレリリース依存がある場合は、
> パッケージ自体もプレリリース版（`-preview`, `-alpha`, `-beta` など）としてリリースしてください。

### 3.2 公開の確認
1. GitHub リポジトリの「Actions」タブで実行状況を確認
2. 成功したら https://www.nuget.org/packages?q=NodeGraph で公開を確認

## 4. トラブルシューティング

### API キーが無効な場合
- API キーの有効期限が切れていないか確認
- スコープが正しく設定されているか確認（`Push` 権限が必要）
- パッケージ名パターンが一致しているか確認（`NodeGraph.*`）

### ビルドが失敗する場合
- `dotnet build` がローカルで成功するか確認
- .NET SDK のバージョンを確認（10.0.x が必要）

### 同じバージョンが既に存在する場合
- `--skip-duplicate` オプションにより、既存バージョンはスキップされます
- 新しいバージョン番号でタグを作成してください

## 5. バージョニングルール

セマンティックバージョニング (SemVer) に従います：
- **MAJOR** (1.0.0): 破壊的変更がある場合
- **MINOR** (0.1.0): 後方互換性のある機能追加
- **PATCH** (0.0.1): 後方互換性のあるバグ修正
- **PRERELEASE** (0.1.0-preview.1): プレリリース版

### プレリリース版について
プレリリース依存（`-preview`, `-alpha`, `-beta` など）がある場合、
NuGet ではパッケージ自体もプレリリース版としてリリースする必要があります。

```bash
# 例: プレリリース版のタグ
v0.1.0-preview.1
v0.1.0-alpha
v0.1.0-beta.2
```

## 6. 関連リンク

- [NuGet API Keys](https://www.nuget.org/account/apikeys)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Semantic Versioning](https://semver.org/)
