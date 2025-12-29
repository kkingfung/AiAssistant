# セットアップガイド (Setup Guide)

## APIキーの設定

### 1. OpenAI APIキーの取得

1. [OpenAI Platform](https://platform.openai.com/)にアクセス
2. アカウントを作成またはログイン
3. [API Keys](https://platform.openai.com/api-keys)ページに移動
4. 「Create new secret key」をクリック
5. 生成されたAPIキーをコピー（**一度しか表示されません**）

### 2. 設定ファイルの編集

1. プロジェクトの`AiAssistant`フォルダ内にある`appsettings.json`を開く
2. `YOUR_OPENAI_API_KEY_HERE`を実際のAPIキーに置き換える

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",  // ここに実際のAPIキーを入力
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.7
  },
  ...
}
```

### 3. モデルの選択

使用するGPTモデルを選択できます：

- `gpt-4` - 最も高性能（推奨、コストが高い）
- `gpt-4-turbo` - GPT-4の高速版
- `gpt-3.5-turbo` - 高速で安価

**注意**: モデルによって料金が異なります。[OpenAI Pricing](https://openai.com/pricing)で確認してください。

### 4. その他の設定

#### MaxTokens
- レスポンスの最大トークン数
- デフォルト: 2000
- 範囲: 1 〜 4096（モデルによって異なる）

#### Temperature
- レスポンスのランダム性
- デフォルト: 0.7
- 範囲: 0.0（決定論的） 〜 1.0（ランダム）

## Google API設定（将来実装予定）

現在は未実装ですが、将来的にGoogle APIを統合する予定です。

1. [Google Cloud Console](https://console.cloud.google.com/)でプロジェクトを作成
2. 必要なAPI（Search, Calendar, Drive等）を有効化
3. 認証情報を作成
4. `appsettings.json`に認証情報を設定

```json
{
  "Google": {
    "ApiKey": "YOUR_GOOGLE_API_KEY",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

## セキュリティ上の注意

### ❗ 重要 ❗

- **APIキーを公開しないでください**
- `appsettings.json`はGitにコミットされません（`.gitignore`で除外済み）
- `appsettings.template.json`はテンプレートです（APIキーを含まない）

### APIキーの保護

1. **絶対にGitHubなどにアップロードしない**
   - `.gitignore`が正しく設定されているか確認
   - `git status`でappsettings.jsonが表示されないことを確認

2. **他人とAPIキーを共有しない**
   - APIキーは個人用です
   - 他の開発者は自分のAPIキーを取得する必要があります

3. **APIキーが漏洩した場合**
   - すぐに[OpenAI Platform](https://platform.openai.com/api-keys)で無効化
   - 新しいAPIキーを生成

## ビルドと実行

### 前提条件
- .NET 8.0 SDK以上
- Windows 10以上

### ビルド手順

```bash
# 依存関係の復元
dotnet restore

# ビルド
dotnet build

# 実行
dotnet run --project AiAssistant/AiAssistant.csproj
```

### Visual Studioでの実行

1. `AiAssistant.sln`をVisual Studioで開く
2. F5キーを押して実行

## トラブルシューティング

### 「APIキーが未設定です」と表示される

- `appsettings.json`のAPIキーが正しく設定されているか確認
- APIキーが`"YOUR_OPENAI_API_KEY_HERE"`のままになっていないか確認
- APIキーの前後にスペースがないか確認

### ChatGPTサービスの初期化に失敗する

- インターネット接続を確認
- OpenAI APIのステータスを確認: [status.openai.com](https://status.openai.com/)
- APIキーが有効か確認（課金設定が必要な場合があります）

### MockAiServiceが使用される

- APIキーが未設定、または無効な場合、自動的にMockAiServiceにフォールバック
- デモ/テスト用途ではMockAiServiceでも動作確認可能

## 料金について

OpenAI APIは使用量に応じて課金されます：

- **GPT-4**: 入力 $0.03/1K tokens, 出力 $0.06/1K tokens
- **GPT-3.5-turbo**: 入力 $0.0005/1K tokens, 出力 $0.0015/1K tokens

**推奨**:
- テスト時はgpt-3.5-turboを使用
- [Usage Dashboard](https://platform.openai.com/usage)で使用量を監視
- 必要に応じて使用制限を設定

## その他のリソース

- [OpenAI API Documentation](https://platform.openai.com/docs)
- [OpenAI Cookbook](https://github.com/openai/openai-cookbook)
- [プロジェクトREADME](README.md)
