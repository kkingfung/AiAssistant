# AiAssistant

WPFと.NET 8.0で構築されたフローティングデスクトップAIアシスタント

<img src="/Result.PNG" width="200"><img src="/Result2.PNG" width="200">

## 概要

AiAssistantは、かつてのMicrosoft Office助手（Clippy）にインスピレーションを受けた、現代的なデスクトップAIアシスタントアプリケーションです。透明な背景のフローティングウィンドウとして常にデスクトップ上に表示され、ユーザーの作業を邪魔することなく利用できます。

複数のAIサービス（Ollama、ChatGPT、Claude、LM Studio）に対応し、自然な会話を通じてユーザーをサポートします。さらに、カレンダー、メール、天気、為替など多彩な統合機能を搭載しています。

**注意**: これは個人プロジェクトであり、販売目的ではありません。

## 主な機能

### AIサービス統合 🤖

- **ローカルLLM（Ollama）**
  - 完全無料、オフラインで動作
  - Phi-3 Mini、Mistral、Llama 3.1などのモデルをサポート
  - ストリーミングレスポンスによるリアルタイム表示
  - 会話コンテキストの保持

- **ChatGPT（OpenAI）**
  - GPT-4、GPT-4-turbo、GPT-3.5-turboをサポート
  - 会話履歴の管理
  - ストリーミングレスポンス対応

- **Claude（Anthropic）**
  - Claude Sonnet 4などの最新モデルをサポート
  - 組織APIによる使用量確認機能

- **LM Studio**
  - OpenAI互換ローカルAPI
  - カスタムモデルのサポート

- **スマートサービス選択**
  - 自動的に最適なAIサービスを選択
  - 優先順位: ローカルLLM → Claude → ChatGPT → デモモード
  - 設定で優先度を変更可能

### 生産性向上機能 📊

- **Google Calendar統合**
  - 複数カレンダーの同時表示
  - 週表示・月表示対応
  - 予定の確認と管理

- **Gmail統合**
  - メール一覧の表示
  - 既読/未読の切り替え

- **天気情報**
  - 現在地の天気表示
  - Open-Meteo APIを使用（無料）

- **為替レート**
  - JPY/HKD/KRW対応
  - リアルタイムレート表示

- **ファンド価格チェック**
  - MUFGファンド価格監視
  - 複数ファンドの同時追跡

- **GitHub統合**
  - リポジトリ通知
  - PR/Issue確認

- **ポモドーロタイマー**
  - 作業/休憩の時間管理

- **デイリーゴール**
  - 日々の目標設定と追跡

- **クイックノート**
  - 素早いメモ作成

- **クリップボード履歴**
  - コピー履歴の管理

### コミュニケーション連携 💬

- **Discord統合**
  - Webhookによるステータス更新
  - 通知機能

- **Slack統合**
  - ステータス更新
  - ワークスペース連携

- **翻訳サービス**
  - AIを使用した即時翻訳
  - ホットキー対応（Ctrl+Shift+T）
  - 自動言語検出

### 音声機能 🎤

- **音声入力**
  - Windows Speech Recognition使用
  - マイクからのテキスト入力

- **音声出力**
  - テキスト読み上げ機能
  - System.Speech使用

### 教育機能 📚

- **言語学習サポート**
  - 🇺🇸 English（英語）
  - 🇯🇵 日本語（Japanese）
  - 🇰🇷 한국어（Korean）
  - 文法チェックと修正提案
  - 会話練習パートナー
  - 単語・フレーズの解説

- **プログラミング学習**
  - 💻 **C#** - .NET開発、Unity
  - 🐍 **Python** - データサイエンス、AI/ML
  - 📜 **JavaScript/TypeScript** - Web開発
  - ☕ **Java** - エンタープライズ、Android
  - 🦀 **Rust** - システムプログラミング
  - 🐹 **Go** - バックエンド、クラウド
  - 💎 **Ruby** - Web開発
  - 🐘 **PHP** - Web開発
  - ⚡ **C/C++** - システム、組み込み
  - 🎯 **Swift/Kotlin** - モバイル開発

- **学習サポート機能**
  - コードレビューと改善提案
  - エラー解説とデバッグヘルプ
  - ベストプラクティスの説明
  - アルゴリズムと設計パターン解説
  - 技術面接対策

### フローティングウィンドウUI 🖼️

- 常に最前面表示、透明な背景
- 画面位置の保存と復元
- カスタマイズ可能なサイズ
- タスクバーから非表示
- ダーク/ライトテーマ対応

### インタラクティブチャット 💭

- チャットバルーンによる会話表示
- リアルタイムストリーミングレスポンス
- Markdownフォーマット対応
- カスタマイズ可能なチャットバブルスタイル
- 会話履歴の永続化

### クリックスルーモード 👆

- 右クリックまたはCtrl+Alt+Tで切り替え
- マウスクリックがウィンドウを透過（作業の邪魔にならない）

### アニメーションキャラクター 🐉

- 6種類のSciFiペット（Cat、Crab、Dragon、Frog、Shark、Snake）
- 54個の透明背景アニメーション（320×320px）
- マゼンタクロマキー自動除去
- ランダムアニメーション自動切り替え（15秒ごと）
- ペット選択UI付き
- ランダムモード（全ペットをミックス表示）

### MVVMアーキテクチャ

- 関心事の明確な分離
- サービスベース設計
- 適切なキャンセルサポートを持つAsync/await

## 技術スタック

- **.NET 8.0** - ターゲットフレームワーク
- **WPF** - UIフレームワーク
- **C# 12** with nullable reference types
- **MVVM Pattern** - アーキテクチャ

### 主要パッケージ

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| OllamaSharp | 5.4.8 | Ollama統合 |
| OpenAI | 2.1.0 | ChatGPT統合 |
| Google.Apis.Calendar.v3 | 1.69.0 | Googleカレンダー |
| Google.Apis.Gmail.v1 | 1.69.0 | Gmail統合 |
| HtmlAgilityPack | 1.11.72 | Webスクレイピング |
| WpfAnimatedGif | 2.0.2 | GIFアニメーション |
| System.Speech | 8.0.0 | 音声入出力 |
| System.Drawing.Common | 8.0.0 | 画像処理 |

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
       "Model": "llama3.1:8b",
       "MaxTokens": 2000,
       "PreferLocal": true
     }
   }
   ```

### オプション2: ChatGPT（OpenAI）

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
       "PreferLocal": false
     }
   }
   ```

### オプション3: Claude（Anthropic）

1. **APIキーの取得**
   - https://console.anthropic.com/ でAPIキーを作成

2. **設定ファイルの編集**

   ```json
   {
     "Claude": {
       "ApiKey": "sk-ant-your-api-key-here",
       "Model": "claude-sonnet-4-20250514",
       "MaxTokens": 2000,
       "Temperature": 0.7
     }
   }
   ```

### オプション4: LM Studio（ローカルGUI）

1. **LM Studioのインストール**
   - https://lmstudio.ai/ からダウンロード
   - モデルをダウンロードしてローカルサーバーを起動

2. **設定ファイルの編集**

   ```json
   {
     "LmStudio": {
       "Enabled": true,
       "Endpoint": "http://localhost:1234",
       "Model": "local-model",
       "MaxTokens": 2000,
       "Temperature": 0.7
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
2. **キャラクター表示**: 選択したペットのアニメーションが自動再生されます
3. **チャット開始**: 💬ボタンをクリックしてチャットバルーンを開きます
4. **メッセージ入力**: テキストボックスに入力してEnterキーまたは送信ボタン
5. **AIレスポンス**: リアルタイムでストリーミング表示されます
6. **ペット変更**: 🐾ボタンをクリックして好きなペットを選択
7. **閉じる**: 右上の×ボタンで終了

### UIボタン

- **🐾 ペット選択ボタン**: 6種類のペットを切り替え（Cat、Crab、Dragon、Frog、Shark、Snake、Random）
- **💬 チャットボタン**: チャットバルーンを開く/閉じる
- **× 閉じるボタン**: アプリケーションを終了

### ショートカット

- **Ctrl+Alt+T**: クリックスルーモード切り替え
- **Alt+F4**: アプリケーション終了
- **右クリック**: クリックスルーモード切り替え
- **左ドラッグ**: ウィンドウ移動

### ペット選択

1. 右上の **🐾 ボタン** をクリック
2. 表示されたリストから好きなペットを選択
3. アニメーションが即座に切り替わります
4. 設定は自動的に保存されます

利用可能なペット：
- 🐱 **Cat (HellCat)** - 8種類のアニメーション
- 🦀 **Crab (SciFi)** - 7種類のアニメーション
- 🐉 **Dragon (SciFi)** - 7種類のアニメーション（デフォルト）
- 🐸 **Frog (SciFi)** - 10種類のアニメーション
- 🦈 **Shark (SciFi)** - 10種類のアニメーション
- 🐍 **Snake (SciFi)** - 8種類のアニメーション
- 🎲 **Random** - すべてのペットをランダムに表示

### AIサービスの切り替え

起動時に画面上部に表示されるメッセージで現在のサービスを確認できます：

- ✅ 「ローカルLLM (Ollama phi3:mini) を使用しています」 - Ollama動作中
- ⚠️ 「ChatGPT (クラウド) を使用しています」 - OpenAI API使用中
- ⚠️ 「MockAiService (デモ) を使用しています」 - デモモード（AIなし）

## プロジェクト構造

```
AiAssistant/
├── AiAssistant/                         # メインプロジェクト
│   ├── MainWindow.xaml/.cs              # メインUIウィンドウ
│   ├── SettingsWindow.xaml/.cs          # 設定画面
│   ├── InputDialog.xaml/.cs             # 入力ダイアログ
│   ├── AssistantViewModel.cs            # メインViewModel
│   │
│   ├── # AIサービス
│   ├── IAiService.cs                    # AIサービスインターフェース
│   ├── AiServiceFactory.cs              # サービス選択ロジック
│   ├── OllamaAiService.cs               # Ollama統合
│   ├── ChatGptService.cs                # ChatGPT統合
│   ├── ClaudeAiService.cs               # Claude統合
│   ├── LmStudioAiService.cs             # LM Studio統合
│   ├── MockAiService.cs                 # モックAI実装
│   │
│   ├── # 生産性サービス
│   ├── ICalendarService.cs              # カレンダーインターフェース
│   ├── GoogleCalendarService.cs         # Googleカレンダー統合
│   ├── IGmailService.cs                 # Gmailインターフェース
│   ├── GmailService.cs                  # Gmail統合
│   ├── GoogleCredentialHelper.cs        # Google認証ヘルパー
│   ├── IWeatherService.cs               # 天気インターフェース
│   ├── WeatherService.cs                # 天気情報取得
│   ├── ICurrencyService.cs              # 為替インターフェース
│   ├── CurrencyService.cs               # 為替レート取得
│   ├── IFundService.cs                  # ファンドインターフェース
│   ├── MufgFundService.cs               # MUFGファンド価格取得
│   ├── IGitHubService.cs                # GitHubインターフェース
│   ├── GitHubService.cs                 # GitHub統合
│   ├── IClaudeUsageService.cs           # Claude使用量インターフェース
│   ├── ClaudeUsageService.cs            # Claude使用量確認
│   │
│   ├── # コミュニケーションサービス
│   ├── IStatusService.cs                # ステータスインターフェース
│   ├── DiscordStatusService.cs          # Discordステータス更新
│   ├── SlackStatusService.cs            # Slackステータス更新
│   ├── ITranslationService.cs           # 翻訳インターフェース
│   ├── TranslationService.cs            # AI翻訳サービス
│   │
│   ├── # ユーティリティサービス
│   ├── PomodoroService.cs               # ポモドーロタイマー
│   ├── DailyGoalsService.cs             # デイリーゴール管理
│   ├── QuickNotesService.cs             # クイックノート
│   ├── ClipboardHistoryService.cs       # クリップボード履歴
│   ├── MediaControlService.cs           # メディアコントロール
│   ├── ScreenshotOcrService.cs          # スクリーンショットOCR
│   ├── MiniGameService.cs               # ミニゲーム
│   │
│   ├── # 音声サービス
│   ├── VoiceInputService.cs             # 音声入力
│   ├── VoiceOutputService.cs            # 音声出力
│   │
│   ├── # チャット関連
│   ├── IChatHistoryService.cs           # チャット履歴インターフェース
│   ├── ChatHistoryService.cs            # チャット履歴永続化
│   ├── ChatBubbleStyle.cs               # チャットバブルスタイル
│   ├── MarkdownTextBlockHelper.cs       # Markdown表示ヘルパー
│   │
│   ├── # アニメーション
│   ├── CharacterAnimationController.cs  # アニメーション管理
│   ├── ChromaKeyHelper.cs               # マゼンタ背景除去
│   │
│   ├── # 設定・ユーティリティ
│   ├── AppSettings.cs                   # 設定管理クラス
│   ├── WindowInteropHelpers.cs          # Win32 API連携
│   ├── appsettings.json                 # 設定ファイル
│   │
│   └── CharacterAnimations/             # アニメーションファイル
│       ├── CatIdle01.gif
│       ├── DragonIdle01.gif
│       └── ... (全54個のアニメーション)
│
├── Build/                               # ビルド出力
├── Result.PNG                           # スクリーンショット
├── Result2.PNG                          # スクリーンショット
├── SETUP.md                             # セットアップガイド
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
    "CharacterAnimationsFolder": "CharacterAnimations",
    "SelectedPet": "Dragon",
    "AnimationSwitchIntervalSeconds": 15,
    "Theme": "Dark",
    "WindowWidth": 280,
    "WindowHeight": 400,
    "AspectRatio": 0.7,
    "SaveWindowPosition": true,
    "LastPositionX": 100,
    "LastPositionY": 100
  }
}
```

### 設定項目

#### AI設定
- **OpenAI.ApiKey**: OpenAI APIキー（ChatGPT使用時）
- **LocalLlm.Enabled**: ローカルLLMを有効化
- **LocalLlm.Model**: 使用するOllamaモデル名
- **LocalLlm.PreferLocal**: ローカルを優先（`true`でローカル優先）

#### キャラクター設定
- **Assistant.CharacterAnimationsFolder**: アニメーションファイルのフォルダ
- **Assistant.SelectedPet**: 表示するペット（"Cat", "Crab", "Dragon", "Frog", "Shark", "Snake", "Random"）
- **Assistant.AnimationSwitchIntervalSeconds**: アニメーション切り替え間隔（秒）
- **Assistant.Theme**: UIテーマ（"Light" または "Dark"）

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

### コア機能 ✅

- [x] フローティングウィンドウUI（透明背景、最前面表示）
- [x] MVVMアーキテクチャ
- [x] クリックスルーモード
- [x] ダーク/ライトテーマ対応
- [x] 設定ファイルシステム
- [x] 設定UI（GUIベース）

### AIサービス ✅

- [x] Ollama統合（ローカルLLM）
- [x] ChatGPT統合（OpenAI API）
- [x] Claude統合（Anthropic API）
- [x] LM Studio統合（ローカルAPI）
- [x] スマートサービス選択（自動フォールバック）
- [x] ストリーミングレスポンス
- [x] 会話履歴の永続化

### チャットUI ✅

- [x] チャットバルーンUI
- [x] Markdownフォーマット対応
- [x] カスタマイズ可能なバブルスタイル
- [x] 送信中表示と送信ボタン無効化

### キャラクターアニメーション ✅

- [x] 6種類のペット（Cat、Crab、Dragon、Frog、Shark、Snake）
- [x] 54個のアニメーション
- [x] マゼンタクロマキー自動除去
- [x] ペット選択UI
- [x] ランダムモード

### 生産性機能 ✅

- [x] Google Calendar統合
- [x] Gmail統合
- [x] 天気情報表示
- [x] 為替レート表示
- [x] ファンド価格チェック
- [x] GitHub統合
- [x] Claude使用量確認
- [x] ポモドーロタイマー
- [x] デイリーゴール
- [x] クイックノート
- [x] クリップボード履歴

### コミュニケーション ✅

- [x] Discord統合
- [x] Slack統合
- [x] AI翻訳サービス

### 音声機能 ✅

- [x] 音声入力（Windows Speech Recognition）
- [x] 音声出力（テキスト読み上げ）

### 教育機能 ✅

- [x] 言語学習サポート（英語、日本語、韓国語）
- [x] プログラミング学習（C#、Python、JavaScript等）
- [x] コードレビュー・デバッグヘルプ
- [x] アルゴリズム・設計パターン解説

### 計画中 🔄

- [ ] ペットごとの音声エフェクト
- [ ] より多くのペットアニメーション
- [ ] プラグインシステム
- [ ] その他の言語サポート（中国語、スペイン語等）

## パフォーマンス

### 推奨モデルとハードウェア要件

| モデル | RAM | レスポンス速度 | 品質 |
|--------|-----|---------------|------|
| TinyLlama | 2GB | ⚡⚡⚡⚡ 非常に速い | ⭐⭐ 基本的 |
| Phi-3 Mini | 4GB | ⚡⚡⚡⚡ 速い | ⭐⭐⭐ 良い |
| Mistral 7B | 8GB | ⚡⚡⚡ 普通 | ⭐⭐⭐⭐ とても良い |
| Llama 3.1 8B | 8GB | ⚡⚡⚡ 普通 | ⭐⭐⭐⭐ とても良い |

## キャラクターアニメーションシステム

### 概要

AiAssistantは、Unity Recorderで録画された透明背景のWebMアニメーションを使用して、6種類のSciFiペットを表示できます。

### アニメーション録画（Unity）

詳細な手順は `UNITY_ANIMATION_EXPORT_GUIDE.md` を参照してください。

**簡易手順**:
1. Unity Editor (2021.3 LTS以降) をインストール
2. Unity Recorder 5.1.3 をインストール
3. Sci-Fi Boss packアセットをインポート
4. カメラ背景を**マゼンタ RGB(255, 0, 255)**に設定
5. WebM VP8形式で録画（512x512、30 FPS推奨）
6. ファイルを `CharacterAnimations/` フォルダに配置

### クロマキーシステム

`ChromaKeyHelper.cs`が自動的にマゼンタ背景を除去：
- **検出色**: RGB(255, 0, 255) ± 30 の許容範囲
- **処理**: unsafe code による高速ピクセル操作
- **対応形式**: GIF、PNG、WebM（将来的にMP4も対応予定）

### カスタムアニメーション追加

1. Unity Recorderでアニメーションを録画（マゼンタ背景）
2. ファイル名を `{PetName}Idle##.webm` 形式で保存
   - 例: `DragonIdle01.webm`, `FrogIdle05.webm`
3. `CharacterAnimations/` フォルダに配置
4. アプリを再起動すると自動的に認識

### アニメーションファイル命名規則

```
{PetType}{AnimationName}{Number}.{ext}

PetType: Cat, Crab, Dragon, Frog, Shark, Snake
AnimationName: Idle, Flying, Roar, など
Number: 01-99
ext: webm, gif, png
```

## ライセンス

個人プロジェクト - 販売目的なし

### 使用アセット
- **Sci-Fi Boss Pack**: Unity Asset Store（3Dモデルとアニメーション）
- **OllamaSharp**: MIT License
- **OpenAI SDK**: MIT License

## 参考

- インスピレーション: Microsoft Office Clippy
- ローカルLLM: [Ollama](https://ollama.com)
- AI API: [OpenAI](https://openai.com)
- Unity Recorder: [Unity Package Manager](https://docs.unity3d.com/Packages/com.unity.recorder@latest)

---

**作成日**: 2025-12-29
**最終更新**: 2026-01-10
**ステータス**: 実用可能（Ollama、ChatGPT、Claude、LM Studio対応 / 多彩な統合機能搭載）
