# VPM Unity Package Workflow

このリポジトリには、Unity Package Manager (VPM) 対応のUnityパッケージの自動リリース機能を提供するGitHubワークフローのテンプレートです。

## 📋 ワークフローの概要

### 1. 🏷️ UpdatePackageVersion.yml
**タグベースでのpackage.jsonバージョン自動更新**

- **トリガー**: `v*` パターンのタグ（例：`v1.0.0`, `v2.1.3`）がプッシュされたとき
- **機能**:
  - タグからバージョン番号を抽出（`v`接頭辞を自動削除）
  - package.jsonの現在のバージョンと比較
  - 異なる場合のみpackage.jsonのversionフィールドを更新
  - 変更をコミットしてmainブランチにプッシュ

```bash
# 使用例
git tag v1.2.3
git push origin v1.2.3
# → package.jsonのversionが"1.2.3"に自動更新される
```

### 2. 📦 CreateReleaseDraft.yml
**UnityPackageとZipファイルのリリースドラフト自動作成**

- **トリガー**: 
  - `workflow_call` - 他のワークフローから呼び出し可能
  - `workflow_dispatch` - 手動実行可能
- **機能**:
  1. package.jsonから`displayName`と`version`を取得
  2. `.git`と`.github`フォルダを削除
  3. metaファイルリストを作成
  4. UnityPackageファイルを生成（`{displayName}_v{version}.unitypackage`）
  5. Zipアーカイブを作成（`{displayName}_v{version}.zip`）
  6. リリースドラフトを作成し、以下をアセットとして添付:
     - UnityPackageファイル
     - Zipファイル
     - package.json

### 3. 🚀 DispatchAddVPMRepository.yml
**VPMリポジトリへの自動通知**

- **トリガー**: リリースが公開されたとき（`published`）
- **機能**:
  - 外部のVPMリポジトリに新しいバージョンが公開されたことを通知
  - Repository Dispatchイベントを送信
- **VPMリポジトリテンプレート**: [VPMRepositoryAutoUpdate](https://github.com/kurotori4423/VPMRepositoryAutoUpdate)

## 🔧 セットアップ

### 必要な設定

1. **package.json**: プロジェクトルートに以下の形式で配置
```json
{
  "displayName": "Your Package Name",
  "version": "1.0.0",
  "name": "com.yourname.packagename"
}
```

2. **シークレット設定**:
   - `VPM_REPO_TOKEN`: VPMリポジトリへの通知用トークン（DispatchAddVPMRepository.ymlで使用）

3. **プロジェクト構造**:
   - `Assets/` フォルダにUnityアセットとmetaファイルを配置
   - または、プロジェクトルートにmetaファイルを配置

### VPMリポジトリ設定

`DispatchAddVPMRepository.yml`で以下を適切に設定してください：
- `repository`: あなたのVPMリポジトリ名
- `client-payload`の`repository`: このパッケージのリポジトリ名

VPMリポジトリの自動更新機能については、テンプレートリポジトリを参照してください：  
📄 **[VPMRepositoryAutoUpdate](https://github.com/kurotori4423/VPMRepositoryAutoUpdate)** - VPMリポジトリの自動更新テンプレート

## 🔄 ワークフローの流れ

```mermaid
graph TD
    A[タグをプッシュ] --> B[UpdatePackageVersion実行]
    B --> C[package.json更新]
    C --> D[手動でCreateReleaseDraft実行]
    D --> E[UnityPackage生成]
    E --> F[リリースドラフト作成]
    F --> G[リリース公開]
    G --> H[DispatchAddVPMRepository実行]
    H --> I[VPMリポジトリに通知]
```

## 📝 使用方法

### 1. 新しいバージョンをリリースする場合

```bash
# 1. バージョンタグを作成・プッシュ
git tag v1.2.3
git push origin v1.2.3

# 2. package.jsonが自動更新される

# 3. 手動またはワークフロー呼び出しでリリースドラフトを作成
# GitHubのActionsページから "Create Unity Package Release Draft" を実行

# 4. 作成されたドラフトリリースを確認・編集して公開
```

### 2. 手動でリリースドラフトを作成する場合

1. GitHubのActionsページを開く
2. "Create Unity Package Release Draft"を選択
3. "Run workflow"をクリック
4. 必要に応じてタグ名を指定して実行

## ⚠️ 注意事項

- package.jsonは必須です。存在しない場合、CreateReleaseDraftワークフローは失敗します
- `displayName`または`name`、`version`フィールドが必要です
- metaファイルが見つからない場合でも処理は続行されます
- VPMリポジトリへの通知にはappropriateな権限を持つトークンが必要です

## 🔧 カスタマイズ

各ワークフローは、プロジェクトの要件に応じてカスタマイズ可能です：
- ファイル名の形式変更
- 除外するファイル・フォルダの調整
- リリースノートの自動生成設定
- 通知先の変更
