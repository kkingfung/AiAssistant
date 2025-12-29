# AiAssistant

WPFと.NET 8.0で構築されたフローティングデスクトップAIアシスタント

## 概要

AiAssistantは、かつてのMicrosoft Office助手（Clippy）にインスピレーションを受けた、現代的なデスクトップAIアシスタントアプリケーションです。透明な背景のフローティングウィンドウとして常にデスクトップ上に表示され、ユーザーの作業を邪魔することなく利用できます。

ローカルLLM（Ollama）またはChatGPT APIを使用して、自然な会話を通じてユーザーをサポートします。

**注意**: これは個人プロジェクトであり、販売目的ではありません。

## 主な機能

### 実装済み ✅

- **ローカルLLM統合（Ollama）**
  - 完全無料、オフラインで動作
  - Phi-3 Mini、Mistral、Llama 3.1などのモデルをサポート
  - ストリーミングレスポンスによるリアルタイム表示
  - 会話コンテキストの保持

- **ChatGPT統合**
  - OpenAI APIによるクラウドAI
  - 会話履歴の管理
  - ストリーミングレスポンス対応

- **スマートサービス選択**
  - 自動的に最適なAIサービスを選択
  - 優先順位: ローカルLLM → ChatGPT → デモモード
  - 設定で優先度を変更可能

- **フローティングウィンドウUI**
  - 常に最前面表示、透明な背景
  - 画面右下に自動配置
  - 固定サイズ（300×400px）
  - タスクバーから非表示
  - 閉じるボタン（×）付き

- **インタラクティブチャット**
  - キャラクターをクリックしてチャットバルーンを表示
  - テキスト入力とEnterキーで送信
  - リアルタイムストリーミングレスポンス
  - 送信中は「入力中...」表示と送信ボタン無効化

- **クリックスルーモード**
  - 右クリックまたはCtrl+Alt+Tで切り替え
  - マウスクリックをウィンドウを透過（作業の邪魔にならない）

- **MVVMアーキテクチャ**
  - 関心事の明確な分離
  - サービスベース設計
  - 適切なキャンセルサポートを持つAsync/await

## 技術スタック

- **.NET 8.0** - ターゲットフレームワーク
- **WPF** - UIフレームワーク
- **C# with nullable reference types**
- **MVVM Pattern** - アーキテクチャ
- **OllamaSharp** - ローカルLLM統合
- **OpenAI SDK** - ChatGPT統合

## セットアップ

### 前提条件

- Windows 10以降
- .NET 8.0 SDK以降

### オプション1: ローカルLLM（Ollama）- 推奨

1. **Ollamaのインストール**
   ```bash
   # https://ollama.com からインストーラーをダウンロード
   # インストール後、Ollamaは自動起動
   ```

2. **モデルのダウンロード**
   ```bash
   # 推奨: Phi-3 Mini（軽量、4GB RAM）
   ollama pull phi3:mini

   # またはより高品質: Mistral 7B（8GB RAM）
   ollama pull mistral
   ```

3. **設定ファイルの確認**

   `AiAssistant/appsettings.json`は既にローカルLLMが有効化されています：
   ```json
   {
     "LocalLlm": {
       "Enabled": true,
       "Provider": "Ollama",
       "Endpoint": "http://localhost:11434",
       "Model": "phi3:mini",
       "PreferLocal": true
     }
   }
   ```

### オプション2: ChatGPT（クラウド）

1. **APIキーの取得**
   - https://platform.openai.com/api-keys でAPIキーを作成

2. **設定ファイルの編集**

   `AiAssistant/appsettings.json`を編集：
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-your-api-key-here",
       "Model": "gpt-4",
       "MaxTokens": 2000,
       "Temperature": 0.7
     },
     "LocalLlm": {
       "PreferLocal": false  // クラウド優先に変更
     }
   }
   ```

### プロジェクトのビルドと実行

```bash
# リポジトリのクローン
git clone <repository-url>
cd AiAssistant

# ビルド
dotnet build

# 実行
dotnet run --project AiAssistant/AiAssistant.csproj
```

## 使い方

### 基本操作

1. **起動時**: ウィンドウが画面右下に表示されます
2. **キャラクターをクリック**: チャットバルーンが開きます
3. **メッセージ入力**: テキストボックスに入力してEnterキーまたは送信ボタン
4. **AIレスポンス**: リアルタイムでストリーミング表示されます
5. **閉じる**: 右上の×ボタンで終了

### ショートカット

- **Ctrl+Alt+T**: クリックスルーモード切り替え
- **Alt+F4**: アプリケーション終了
- **右クリック**: クリックスルーモード切り替え
- **左ドラッグ**: ウィンドウ移動

### AIサービスの切り替え

起動時に画面上部に表示されるメッセージで現在のサービスを確認できます：

- ✅ 「ローカルLLM (Ollama phi3:mini) を使用しています」 - Ollama動作中
- ⚠️ 「ChatGPT (クラウド) を使用しています」 - OpenAI API使用中
- ⚠️ 「MockAiService (デモ) を使用しています」 - デモモード（AIなし）

## プロジェクト構造

```
AiAssistant/
├── AiAssistant/
│   ├── MainWindow.xaml / .xaml.cs       # メインUIウィンドウ
│   ├── AssistantViewModel.cs            # ViewModel（状態管理）
│   ├── IAiService.cs                    # AIサービスインターフェース
│   ├── OllamaAiService.cs               # Ollama統合
│   ├── ChatGptService.cs                # ChatGPT統合
│   ├── MockAiService.cs                 # モックAI実装
│   ├── AiServiceFactory.cs              # サービス選択ロジック
│   ├── AppSettings.cs                   # 設定管理
│   └── appsettings.json                 # 設定ファイル
├── OLLAMA_SETUP.md                      # Ollamaセットアップガイド
├── OLLAMA_INTEGRATION_SUMMARY.md        # 実装詳細
└── README.md                            # このファイル
```

## アーキテクチャ

### MVVMパターン

- **Model**: AIサービス（`OllamaAiService`, `ChatGptService`, etc.）
- **View**: `MainWindow.xaml`（UI定義）
- **ViewModel**: `AssistantViewModel`（UIロジックと状態管理）

### サービスレイヤー

すべてのAIサービスは`IAiService`インターフェースを実装：

```csharp
public interface IAiService
{
    // 完全なレスポンスを一度に取得
    Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken);

    // ストリーミングレスポンス（リアルタイム表示用）
    IAsyncEnumerable<string> StreamResponseAsync(string prompt, CancellationToken cancellationToken);
}
```

### サービス選択ロジック

`AiServiceFactory`が設定に基づいて自動的に最適なサービスを選択：

1. **ローカルLLM優先**（`PreferLocal: true`）
   - Ollamaが実行中か確認
   - モデルがダウンロード済みか確認
   - 利用可能なら`OllamaAiService`を使用

2. **クラウドフォールバック**
   - OpenAI APIキーが設定されていれば`ChatGptService`を使用

3. **デモモード**
   - すべて利用不可なら`MockAiService`を使用

## 設定

### appsettings.json

```json
{
  "OpenAI": {
    "ApiKey": "",
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.7
  },
  "LocalLlm": {
    "Enabled": true,
    "Provider": "Ollama",
    "Endpoint": "http://localhost:11434",
    "Model": "phi3:mini",
    "MaxTokens": 500,
    "PreferLocal": true
  },
  "Assistant": {
    "WindowWidth": 300,
    "WindowHeight": 400,
    "AspectRatio": 0.75,
    "SaveWindowPosition": true
  }
}
```

### 設定項目

- **OpenAI.ApiKey**: OpenAI APIキー（ChatGPT使用時）
- **LocalLlm.Enabled**: ローカルLLMを有効化
- **LocalLlm.Model**: 使用するOllamaモデル名
- **LocalLlm.PreferLocal**: ローカルを優先（`true`でローカル優先）

## トラブルシューティング

### 「MockAiService (デモ) を使用しています」と表示される

**原因**:
- Ollamaが起動していない
- モデルがダウンロードされていない
- OpenAI APIキーが設定されていない

**解決策**:
```bash
# Ollamaの状態確認
ollama list

# モデルのダウンロード
ollama pull phi3:mini

# Ollamaの再起動（必要な場合）
# Windowsの場合、タスクマネージャーからOllamaを再起動
```

### レスポンスが遅い

**ローカルLLMの場合**:
- より軽量なモデルに変更: `tinyllama`（1.1B）
- RAMとCPU/GPUの使用状況を確認

**ChatGPTの場合**:
- インターネット接続を確認
- APIレート制限を確認

## 開発状況

### 完了 ✅

- [x] 基本的なフローティングウィンドウUI
- [x] MVVMアーキテクチャ
- [x] Ollama統合（ローカルLLM）
- [x] ChatGPT統合
- [x] スマートサービス選択
- [x] ストリーミングレスポンス
- [x] チャットバルーンUI
- [x] 送信中表示と送信ボタン無効化
- [x] クリックスルーモード
- [x] 設定ファイルシステム

### 計画中 🔄

- [ ] 3Dキャラクター表示（Unity統合）
- [ ] アニメーション再生
- [ ] 音声入力サポート
- [ ] Google API統合
- [ ] 設定UI
- [ ] 会話履歴の永続化

## パフォーマンス

### 推奨モデルとハードウェア要件

| モデル | RAM | レスポンス速度 | 品質 |
|--------|-----|---------------|------|
| TinyLlama | 2GB | ⚡⚡⚡⚡ 非常に速い | ⭐⭐ 基本的 |
| Phi-3 Mini | 4GB | ⚡⚡⚡⚡ 速い | ⭐⭐⭐ 良い |
| Mistral 7B | 8GB | ⚡⚡⚡ 普通 | ⭐⭐⭐⭐ とても良い |
| Llama 3.1 8B | 8GB | ⚡⚡⚡ 普通 | ⭐⭐⭐⭐ とても良い |

## ライセンス

個人プロジェクト - 販売目的なし

## 参考

- インスピレーション: Microsoft Office Clippy
- ローカルLLM: [Ollama](https://ollama.com)
- AI API: [OpenAI](https://openai.com)

---

**作成日**: 2025-12-29
**ステータス**: 実用可能（ローカルLLMまたはChatGPT統合済み）
