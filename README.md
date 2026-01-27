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

## 🌸 進化計画: 癒し系AIコンパニオン

### ビジョン

「Chill with You: Lo-Fi Story」にインスピレーションを受け、AiAssistantを**癒し系AIコンパニオン**へと進化させます。

**コンセプト**: 作業を手伝うアシスタント ＋ 一緒にいるだけで癒されるペット

ユーザーとの会話を通じて関係性が深まり、より親密な存在へと成長していきます。

### 進化ロードマップ

```
Phase 1 (現在)          Phase 2                Phase 3 (最終目標)
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ ペットシステム   │ → │ Live2D/3D対応   │ → │ ペット＋人型    │
│ 感情・関係性    │    │ 表情・動作強化   │    │ キャラ選択可能  │
│ テキストベース   │    │ 音声合成連携    │    │ ストーリー要素  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

---

## Phase 1: 感情・関係性システム（現在の目標）

### 1.1 関係性（絆）システム

#### 概要
ユーザーとペットの間に「絆レベル」を導入。会話を重ねることで絆が深まります。

#### 絆レベル
| レベル | 名称 | 必要ポイント | 解放される要素 |
|--------|------|-------------|---------------|
| 1 | 出会い | 0 | 基本会話 |
| 2 | 知り合い | 100 | 挨拶バリエーション増加 |
| 3 | 友達 | 300 | 励まし・心配の言葉 |
| 4 | 親友 | 600 | 特別な反応、秘密の話 |
| 5 | 家族 | 1000 | 最大限の親密さ、特別演出 |

#### 絆ポイント獲得条件
| アクション | ポイント |
|-----------|---------|
| チャットメッセージ送信 | +1 |
| 長めの会話（5往復以上） | +5 ボーナス |
| 毎日のログイン | +10 |
| ポモドーロ完了 | +3 |
| デイリーゴール達成 | +5 |

### 1.2 感情システム

#### ペットの感情状態
```
😊 嬉しい   - ユーザーとの会話中、目標達成時
😌 穏やか   - 通常状態、作業見守り中
😢 寂しい   - 長時間放置後
😴 眠い     - 深夜帯
🎉 興奮     - 特別なイベント時
```

#### 感情に影響する要素
- 時間帯（朝・昼・夜・深夜）
- 最後の会話からの経過時間
- 天気（Weather API連携済み）
- ユーザーの活動状況

### 1.3 LLM駆動の会話システム

#### 設計方針
セリフはハードコードせず、**設定済みのLLM（Ollama、ChatGPT、Claude、LM Studio）** を使用して動的に生成します。これにより：
- 自然で多様な会話が可能
- 絆レベル・感情に応じた応答の変化
- ユーザーとの実際の会話（チャット）機能

#### システムプロンプト構築
LLMへのシステムプロンプトは、現在の状態に基づいて動的に構築されます：

```csharp
public class CompanionPromptBuilder
{
    /// <summary>
    /// 現在の状態に基づいてシステムプロンプトを構築
    /// </summary>
    public string BuildSystemPrompt(CompanionContext context)
    {
        return $"""
        あなたは癒し系のペットコンパニオンです。

        【キャラクター設定】
        - 種類: {context.PetType}（例: Dragon, Cat, Frog等）
        - 性格: 優しく、穏やかで、ユーザーを癒す存在
        - 口調: 丁寧だが親しみやすい、絵文字は控えめ

        【現在の状態】
        - 絆レベル: {context.BondLevel}/5（{context.BondLevelName}）
        - 感情: {context.Emotion}
        - 時間帯: {context.TimeOfDay}
        - 天気: {context.Weather}
        - 最後の会話: {context.TimeSinceLastChat}

        【応答ガイドライン】
        - 絆レベルが高いほど親密な話し方をする
        - 感情状態を反映した応答をする
        - 短めの応答（1-3文）を心がける
        - ユーザーを励まし、癒す言葉を選ぶ
        - アシスタントとしての機能も果たす（質問への回答、タスク支援等）

        【絆レベル別の親密度】
        Lv1: 丁寧語、距離感あり
        Lv2: 少しフレンドリー
        Lv3: 友達のような関係
        Lv4: とても親しい、心配や甘え表現
        Lv5: 家族のような絆、深い信頼
        """;
    }
}
```

#### 会話カテゴリ（トリガー）
```csharp
public enum DialogueTrigger
{
    UserMessage,        // ユーザーからのチャット入力
    Greeting,           // 起動時・時間帯変化時の挨拶
    Encouragement,      // ポモドーロ完了、ゴール達成時
    IdleComment,        // 一定時間経過後の独り言
    WeatherChange,      // 天気変化時のコメント
    BondLevelUp,        // 絆レベルアップ時
    ReturnGreeting      // 長時間放置後の復帰
}
```

### 1.4 インタラクションシステム

#### 設計思想
**癒し＋アシスタント**の2層構造。ペットは作業の邪魔をせず、バックグラウンドで癒しの存在として振る舞います。

```
┌─────────────────────────────────────────────────────┐
│                 インタラクション2層構造               │
├─────────────────────────────────────────────────────┤
│                                                     │
│  【Layer 1: 癒しペット層】                           │
│   - 動物タイプ: 音 + アニメーションのみ（非言語）     │
│   - 作業中は邪魔しない、そっと見守る                 │
│   - 感情・絆は視覚的に表現                          │
│                                                     │
│  【Layer 2: AIアシスタント層】                       │
│   - ユーザーが明示的に呼び出した時のみ応答           │
│   - 従来のチャット機能（質問応答、タスク支援）       │
│   - LLMによる知的サポート                           │
│                                                     │
└─────────────────────────────────────────────────────┘
```

#### 動物タイプ vs 人型タイプ

| 要素 | 動物タイプ（Phase 1） | 人型タイプ（Phase 3） |
|------|----------------------|---------------------|
| コミュニケーション | 音・アニメーション | テキスト会話 |
| 感情表現 | 鳴き声、動作 | 言葉、表情 |
| アシスタント機能 | 別レイヤー（AI） | キャラクターと統合可能 |
| 邪魔しない設計 | ✅ 完全非言語 | ✅ 控えめな発話 |

#### 動物ペットの表現方法

**アニメーション（視覚）**
```csharp
public enum PetAnimation
{
    Idle,           // 待機（通常）
    IdleHappy,      // 待機（嬉しい）
    IdleSleepy,     // 待機（眠い）
    IdleSad,        // 待機（寂しい）
    Greeting,       // 挨拶モーション
    Celebrate,      // お祝い（目標達成時）
    Comfort,        // 慰めモーション
    Playing         // 一人遊び（ユーザー作業中）
}
```

**サウンド（聴覚）**
```csharp
public enum PetSound
{
    // 汎用
    Purr,           // ゴロゴロ（満足）
    Chirp,          // 短い鳴き声（注目）
    Yawn,           // あくび（眠い）
    Whimper,        // 寂しい鳴き声

    // 感情別
    HappySound,     // 嬉しい時の音
    SadSound,       // 寂しい時の音
    ExcitedSound,   // 興奮時の音

    // イベント
    WelcomeSound,   // 起動時・復帰時
    CongratSound    // 目標達成時
}
```

#### 非邪魔（Non-Intrusive）設計

```csharp
public class PetBehaviorService
{
    /// <summary>
    /// ペットの自律行動（ユーザーの作業を邪魔しない）
    /// </summary>
    public async Task UpdateBehaviorAsync(CompanionContext context)
    {
        // ユーザーが作業中の場合
        if (context.UserIsWorking)
        {
            // 静かなアニメーション（一人遊び、居眠り等）
            await PlayQuietAnimation();

            // 音は最小限（設定で完全オフ可能）
            if (_settings.AmbientSoundsEnabled)
            {
                await PlayAmbientSound(); // 控えめな環境音のみ
            }
        }
        else
        {
            // アイドル時は少し積極的に反応
            await PlayActiveAnimation();
        }
    }
}
```

#### AIアシスタント機能（別レイヤー）

ペットの存在とは独立して、ユーザーが明示的に呼び出した時のみAIが応答：

```
┌──────────────────────────────────────────┐
│  ユーザーの作業画面                        │
│                                          │
│  ┌────────────────┐                      │
│  │ 🐉 (アニメ)     │  ← ペット: 静かに見守る │
│  │    ♥♥♥♡♡      │                      │
│  └────────────────┘                      │
│                                          │
│  💬 ボタンクリック → AIアシスタント起動     │
│                                          │
│  ┌────────────────────────────────────┐  │
│  │ 🤖 何かお手伝いしましょうか？        │  │
│  │ [入力欄]                   [送信]  │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
```

**💬ボタンの動作**:
- クリック → AIアシスタントチャットを開く
- ペットのキャラクター性とは分離
- 従来の`IAiService`をそのまま使用

### 1.5 技術実装計画

#### 新規クラス
```
AiAssistant/
├── Companionship/
│   ├── # コア
│   ├── CompanionContext.cs          # 状態コンテキスト
│   ├── BondLevel.cs                 # 絆レベル定義
│   ├── EmotionState.cs              # 感情状態定義
│   │
│   ├── # 絆・感情サービス
│   ├── IBondService.cs              # 絆システムインターフェース
│   ├── BondService.cs               # 絆レベル管理・ポイント計算
│   ├── IEmotionService.cs           # 感情システムインターフェース
│   ├── EmotionService.cs            # 感情状態管理・遷移ロジック
│   │
│   ├── # ペット行動サービス（動物タイプ用）
│   ├── IPetBehaviorService.cs       # ペット行動インターフェース
│   ├── PetBehaviorService.cs        # 自律行動・アニメーション制御
│   ├── IPetSoundService.cs          # ペット音声インターフェース
│   ├── PetSoundService.cs           # 鳴き声・環境音再生
│   ├── PetAnimation.cs              # アニメーション種別定義
│   └── PetSound.cs                  # サウンド種別定義
│
├── # 既存（AIアシスタント層 - 変更なし）
├── IAiService.cs
├── AiServiceFactory.cs
├── OllamaAiService.cs / ChatGptService.cs / etc.
```

#### アーキテクチャ図
```
┌─────────────────────────────────────────────────────────────┐
│                      AiAssistant                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            Layer 1: 癒しペット層                      │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │ BondService │  │ Emotion     │  │ PetBehavior │  │   │
│  │  │ (絆管理)    │  │ Service     │  │ Service     │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │   │
│  │         │                │                │          │   │
│  │         └────────────────┼────────────────┘          │   │
│  │                          ↓                           │   │
│  │              ┌───────────────────────┐               │   │
│  │              │ CharacterAnimation    │               │   │
│  │              │ Controller            │               │   │
│  │              │ + PetSoundService     │               │   │
│  │              └───────────────────────┘               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            Layer 2: AIアシスタント層                  │   │
│  │  ┌─────────────────────────────────────────────┐    │   │
│  │  │ IAiService (Ollama/ChatGPT/Claude/LM Studio) │    │   │
│  │  └─────────────────────────────────────────────┘    │   │
│  │                          ↑                          │   │
│  │              💬 ユーザーが明示的に呼び出し            │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            共有サービス                              │   │
│  │  WeatherService │ PomodoroService │ DailyGoals etc. │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

#### データ永続化
```json
// companionship.json
{
  "bondPoints": 350,
  "bondLevel": 3,
  "currentEmotion": "peaceful",
  "lastInteraction": "2026-01-27T10:30:00",
  "totalConversations": 128,
  "statistics": {
    "pomodorosCompleted": 45,
    "goalsAchieved": 23,
    "daysActive": 30
  }
}
```

### 1.5 UI拡張

#### 絆表示（メインウィンドウ）
```
┌────────────────────────┐
│  🐉 [♥♥♥♡♡] Lv.3      │  ← 絆レベル表示
│  「今日も頑張ってるね」  │  ← セリフバルーン
│                        │
│    [ペットアニメ]       │
│                        │
│  😌 穏やか              │  ← 現在の感情
└────────────────────────┘
```

---

## Phase 2: ビジュアル・音声強化（次期目標）

### 2.1 Live2D / 3Dモデル対応

#### 技術選択肢の検討

| 方式 | 長所 | 短所 | 推奨度 |
|------|------|------|--------|
| **WebView2 + pixi-live2d** | WPF統合が容易、Web技術活用 | パフォーマンス、複雑な通信 | ⭐⭐⭐ |
| **Unity Embedded** | 豊富な3Dサポート、アセット活用 | 複雑、リソース重い | ⭐⭐ |
| **Live2D Cubism SDK (Native)** | 公式SDK、最高性能 | C++、WPF統合が困難 | ⭐⭐ |
| **Vts-Sharp (VTube Studio)** | 既存プロトコル、実績あり | VTube Studio依存 | ⭐⭐⭐ |

#### 推奨アプローチ: WebView2 + pixi-live2d

```
┌─────────────────────────────────────────────────────────┐
│                   MainWindow (WPF)                       │
│  ┌─────────────────────────────────────────────────┐   │
│  │              WebView2 Control                     │   │
│  │  ┌─────────────────────────────────────────┐    │   │
│  │  │         HTML Canvas                       │    │   │
│  │  │  ┌─────────────────────────────────┐    │    │   │
│  │  │  │    pixi-live2d-display           │    │    │   │
│  │  │  │    (Live2D Model Rendering)      │    │    │   │
│  │  │  └─────────────────────────────────┘    │    │   │
│  │  └─────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────┘   │
│                          ↑↓                             │
│              JavaScript Interop (PostMessage)            │
│                          ↑↓                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │           Live2DService (C#)                      │   │
│  │  - SetExpression(emotion)                        │   │
│  │  - SetMotion(animation)                          │   │
│  │  - SetLipSync(audioLevel)                        │   │
│  │  - SetLookAt(x, y)                               │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

#### 実装ステップ

**Step 2.1.1: WebView2統合基盤**
```csharp
// Live2D/ILive2DService.cs
public interface ILive2DService
{
    Task InitializeAsync(string modelPath);
    Task SetExpressionAsync(EmotionType emotion);
    Task SetMotionAsync(string motionName, MotionPriority priority);
    Task SetLipSyncAsync(float volume);
    Task SetLookAtAsync(double x, double y);
    Task SetParameterAsync(string paramName, float value);

    event EventHandler<Live2DEventArgs>? MotionFinished;
    event EventHandler? ModelLoaded;
}
```

**Step 2.1.2: HTML/JS レンダラー**
```javascript
// live2d-renderer.js
class Live2DRenderer {
    constructor(canvasId) {
        this.app = new PIXI.Application({...});
        this.model = null;
    }

    async loadModel(modelPath) {
        this.model = await PIXI.live2d.Live2DModel.from(modelPath);
        this.app.stage.addChild(this.model);
    }

    setExpression(expression) {
        this.model.expression(expression);
    }

    setMotion(group, index) {
        this.model.motion(group, index);
    }

    setLipSync(volume) {
        this.model.internalModel.coreModel.setParameterValueById(
            'ParamMouthOpenY', volume
        );
    }
}
```

**Step 2.1.3: 表情・感情マッピング**
```csharp
// Live2D/Live2DExpressionMapper.cs
public static class Live2DExpressionMapper
{
    public static string GetExpression(EmotionType emotion) => emotion switch
    {
        EmotionType.Happy => "happy",
        EmotionType.Peaceful => "neutral",
        EmotionType.Lonely => "sad",
        EmotionType.Sleepy => "sleepy",
        EmotionType.Excited => "excited",
        _ => "neutral"
    };

    public static (string group, int index) GetIdleMotion(EmotionType emotion) => emotion switch
    {
        EmotionType.Happy => ("Idle", 1),
        EmotionType.Sleepy => ("Idle", 2),
        _ => ("Idle", 0)
    };
}
```

**Step 2.1.4: マウス追従（視線追従）**
```csharp
// MainWindow.xaml.cs
private void OnMouseMove(object sender, MouseEventArgs e)
{
    if (_live2dService == null) return;

    var pos = e.GetPosition(this);
    var normalizedX = (pos.X / ActualWidth - 0.5) * 2;  // -1 to 1
    var normalizedY = (pos.Y / ActualHeight - 0.5) * 2; // -1 to 1

    _live2dService.SetLookAtAsync(normalizedX, normalizedY);
}
```

#### Live2Dモデル要件
- **フォーマット**: Cubism 4.x (.model3.json)
- **必須パラメータ**:
  - `ParamAngleX/Y/Z` - 頭の回転
  - `ParamEyeLOpen/ROpen` - まばたき
  - `ParamMouthOpenY` - 口パク
  - `ParamBodyAngleX/Y` - 体の揺れ
- **推奨表情**:
  - `neutral`, `happy`, `sad`, `angry`, `surprised`, `sleepy`
- **推奨モーション**:
  - `Idle` グループ（複数バリエーション）
  - `Greeting`, `Celebrate`, `Comfort`

---

### 2.2 音声合成システム

#### 技術選択肢

| 方式 | 特徴 | コスト | 品質 |
|------|------|--------|------|
| **VOICEVOX** | ローカル、無料、商用可 | 無料 | ⭐⭐⭐⭐ |
| **COEIROINK** | ローカル、無料 | 無料 | ⭐⭐⭐⭐ |
| **Style-Bert-VITS2** | 高品質、ローカル | 無料 | ⭐⭐⭐⭐⭐ |
| **Windows TTS** | 組み込み、簡単 | 無料 | ⭐⭐ |
| **Google Cloud TTS** | 高品質、多言語 | 有料 | ⭐⭐⭐⭐⭐ |
| **Azure TTS** | 高品質、感情表現 | 有料 | ⭐⭐⭐⭐⭐ |

#### 推奨: VOICEVOX統合

**アーキテクチャ**
```
┌─────────────────────────────────────────────────────┐
│                  VoiceSynthesisService               │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────┐ │
│  │ IVoiceSynth │    │ VOICEVOX    │    │ Fallback│ │
│  │ esizer      │ → │ Synthesizer │ or │ Windows │ │
│  └─────────────┘    └─────────────┘    │ TTS     │ │
│         ↓                               └─────────┘ │
│  ┌─────────────────────────────────────────────┐   │
│  │              AudioPlaybackService             │   │
│  │  - Play audio                                 │   │
│  │  - Generate lip sync data                     │   │
│  │  - Notify Live2D service                      │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**実装インターフェース**
```csharp
// Voice/IVoiceSynthesisService.cs
public interface IVoiceSynthesisService
{
    /// <summary>利用可能な音声一覧</summary>
    IReadOnlyList<VoiceInfo> AvailableVoices { get; }

    /// <summary>現在選択中の音声</summary>
    VoiceInfo? CurrentVoice { get; set; }

    /// <summary>テキストを音声に変換</summary>
    Task<VoiceSynthesisResult> SynthesizeAsync(string text, CancellationToken ct = default);

    /// <summary>音声を再生（リップシンク連携）</summary>
    Task SpeakAsync(string text, CancellationToken ct = default);

    /// <summary>再生中かどうか</summary>
    bool IsSpeaking { get; }

    /// <summary>再生停止</summary>
    void Stop();

    /// <summary>リップシンク用音量イベント</summary>
    event EventHandler<float>? LipSyncUpdate;
}

public record VoiceInfo(
    string Id,
    string Name,
    string? Description,
    VoiceSynthesizerType SynthesizerType
);

public record VoiceSynthesisResult(
    byte[] AudioData,
    TimeSpan Duration,
    float[]? LipSyncData  // 口パク用の音量データ
);
```

**VOICEVOX連携**
```csharp
// Voice/VoicevoxSynthesizer.cs
public class VoicevoxSynthesizer : IVoiceSynthesizer
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:50021";

    public async Task<byte[]> SynthesizeAsync(string text, int speakerId, CancellationToken ct)
    {
        // 1. 音声クエリを作成
        var queryResponse = await _httpClient.PostAsync(
            $"{BaseUrl}/audio_query?text={Uri.EscapeDataString(text)}&speaker={speakerId}",
            null, ct);
        var query = await queryResponse.Content.ReadAsStringAsync(ct);

        // 2. 音声合成
        var synthesisResponse = await _httpClient.PostAsync(
            $"{BaseUrl}/synthesis?speaker={speakerId}",
            new StringContent(query, Encoding.UTF8, "application/json"),
            ct);

        return await synthesisResponse.Content.ReadAsByteArrayAsync(ct);
    }
}
```

**リップシンク連携**
```csharp
// Voice/LipSyncAnalyzer.cs
public class LipSyncAnalyzer
{
    /// <summary>
    /// 音声データから口パク用の音量データを生成
    /// </summary>
    public float[] AnalyzeAudio(byte[] wavData, int sampleRate = 24000)
    {
        // WAVデータをサンプルに変換
        var samples = ConvertToSamples(wavData);

        // 50ms単位で音量を計算
        var frameSize = sampleRate / 20; // 50ms
        var frames = new List<float>();

        for (int i = 0; i < samples.Length; i += frameSize)
        {
            var frameEnd = Math.Min(i + frameSize, samples.Length);
            var rms = CalculateRms(samples, i, frameEnd);
            frames.Add(NormalizeVolume(rms));
        }

        return frames.ToArray();
    }

    private float NormalizeVolume(float rms)
    {
        // 0.0 - 1.0 の範囲に正規化
        return Math.Clamp(rms * 3.0f, 0f, 1f);
    }
}
```

#### VOICEVOX話者（キャラクター）例
| ID | 名前 | 特徴 |
|----|------|------|
| 0 | 四国めたん（あまあま） | 優しい、癒し系 |
| 2 | ずんだもん | 元気、かわいい |
| 8 | 春日部つむぎ | 明るい、友達感 |
| 13 | 青山龍星 | 落ち着いた男性声 |

---

### 2.3 BGM・環境音システム

#### Lo-Fi BGM再生
```csharp
// Audio/IBgmService.cs
public interface IBgmService
{
    /// <summary>BGM再生</summary>
    Task PlayAsync(BgmTrack track);

    /// <summary>停止</summary>
    void Stop();

    /// <summary>音量（0.0-1.0）</summary>
    float Volume { get; set; }

    /// <summary>現在再生中のトラック</summary>
    BgmTrack? CurrentTrack { get; }
}

public enum BgmTrack
{
    None,
    LoFiChill,      // 穏やかなLo-Fi
    LoFiStudy,      // 集中用Lo-Fi
    LoFiNight,      // 夜用Lo-Fi
    Ambient,        // 環境音のみ
    Rain,           // 雨音
    Fireplace       // 暖炉
}
```

#### YouTube/Web連携（オプション）
```csharp
// Audio/IWebAudioService.cs
public interface IWebAudioService
{
    /// <summary>YouTube/SoundCloud等のURLから再生</summary>
    Task PlayFromUrlAsync(string url);

    /// <summary>プレイリスト再生</summary>
    Task PlayPlaylistAsync(IEnumerable<string> urls);
}
```

---

### 2.4 ビジュアルカスタマイズ

#### 背景テーマ
```csharp
// Customization/IThemeService.cs
public interface IThemeService
{
    Theme CurrentTheme { get; set; }
    IReadOnlyList<Theme> AvailableThemes { get; }

    event EventHandler<Theme>? ThemeChanged;
}

public record Theme(
    string Id,
    string Name,
    string BackgroundImage,      // 背景画像パス
    string AccentColor,          // アクセントカラー
    bool IsDark,                 // ダークテーマか
    SeasonType? Season           // 季節（オプション）
);

public enum SeasonType { Spring, Summer, Autumn, Winter }
```

#### 季節イベント
```csharp
// Events/ISeasonalEventService.cs
public interface ISeasonalEventService
{
    SeasonalEvent? CurrentEvent { get; }

    /// <summary>現在の季節/イベントに合わせたデコレーションを取得</summary>
    IEnumerable<Decoration> GetDecorations();
}

public record SeasonalEvent(
    string Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string SpecialThemeId,
    string? SpecialModelPath  // 特別な衣装等
);
```

---

### 2.5 Phase 2 実装優先順位

#### 高優先度 🔴
1. **WebView2 + pixi-live2d 基盤** - Live2D表示の基本実装
2. **ILive2DService** - 表情・モーション制御インターフェース
3. **VOICEVOX統合** - 音声合成基本機能
4. **リップシンク連携** - Live2D + 音声の同期

#### 中優先度 🟡
5. **マウス追従（視線追従）** - インタラクティブ性向上
6. **BGMサービス** - Lo-Fi音楽再生
7. **背景テーマシステム** - ビジュアルカスタマイズ
8. **複数音声対応** - VOICEVOX話者選択

#### 低優先度 🟢
9. **環境音** - 雨音、暖炉等
10. **季節イベント** - 特別演出
11. **Web音源連携** - YouTube等
12. **アクセサリーシステム** - ペット装飾

---

### 2.6 必要なパッケージ・依存関係

```xml
<!-- AiAssistant.csproj に追加 -->

<!-- WebView2 (Live2D表示用) -->
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2420.22" />

<!-- NAudio (音声再生・分析) -->
<PackageReference Include="NAudio" Version="2.2.1" />

<!-- JSON通信 -->
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

#### 外部ソフトウェア要件
- **VOICEVOX** - https://voicevox.hiroshiba.jp/ （ローカルインストール）
- **Live2Dモデル** - Cubism 4.x形式 (.model3.json)

---

### 2.7 新規ファイル構造

```
AiAssistant/
├── Live2D/
│   ├── ILive2DService.cs           # インターフェース
│   ├── WebView2Live2DService.cs    # WebView2実装
│   ├── Live2DExpressionMapper.cs   # 感情→表情マッピング
│   ├── Live2DMotionMapper.cs       # 行動→モーションマッピング
│   └── Resources/
│       ├── live2d-renderer.html    # Live2D表示用HTML
│       └── live2d-renderer.js      # pixi-live2dラッパー
│
├── Voice/
│   ├── IVoiceSynthesisService.cs   # 音声合成インターフェース
│   ├── VoiceSynthesisService.cs    # 統合サービス
│   ├── VoicevoxSynthesizer.cs      # VOICEVOX実装
│   ├── WindowsTtsSynthesizer.cs    # Windows TTS実装（フォールバック）
│   ├── LipSyncAnalyzer.cs          # リップシンクデータ生成
│   └── VoiceInfo.cs                # 音声情報モデル
│
├── Audio/
│   ├── IBgmService.cs              # BGMインターフェース
│   ├── BgmService.cs               # BGM再生サービス
│   ├── IAmbientSoundService.cs     # 環境音インターフェース
│   └── AmbientSoundService.cs      # 環境音再生
│
├── Customization/
│   ├── IThemeService.cs            # テーマインターフェース
│   ├── ThemeService.cs             # テーマ管理
│   └── Theme.cs                    # テーマモデル
│
└── Events/
    ├── ISeasonalEventService.cs    # 季節イベントインターフェース
    └── SeasonalEventService.cs     # イベント管理
```

---

## Phase 3: 人型キャラクター対応（最終目標）

### 3.1 キャラクター選択システム
- ペット or 人型キャラクターを選択可能
- キャラクターごとの個性・ストーリー

### 3.2 ストーリー要素
- キャラクター背景ストーリー
- 絆レベルで解放されるエピソード
- 特別イベント

### 3.3 高度なインタラクション
- より自然な会話
- ユーザーの好みを学習
- パーソナライズされた体験

---

## 実装優先順位

### Phase 1: 動物ペットシステム

#### 高優先度 🔴
1. `CompanionContext.cs` - 状態コンテキストモデル
2. `BondLevel.cs` / `EmotionState.cs` - 定義クラス
3. `IBondService` / `BondService` - 絆システム基盤
4. `IEmotionService` / `EmotionService` - 感情システム基盤
5. `IPetBehaviorService` / `PetBehaviorService` - **ペット自律行動**
6. 絆・感情データの永続化（companionship.json）

#### 中優先度 🟡
7. `PetAnimation.cs` - アニメーション種別と感情のマッピング
8. `IPetSoundService` / `PetSoundService` - 鳴き声・環境音
9. 既存`CharacterAnimationController`との統合
10. UI表示（絆レベルバー、感情アイコン）
11. 時間帯・天気に応じた行動変化

#### 低優先度 🟢
12. 統計・実績システム
13. 特別イベント（絆レベルアップ演出等）
14. 音声ファイルの追加（各ペット用サウンド）
15. 設定UI（音量、行動頻度等）

### 技術的な注意点

**Layer 1（癒しペット層）**:
- 既存の`CharacterAnimationController`を拡張
- 感情状態に応じたアニメーション自動選択
- 非邪魔設計：ユーザー作業中は静かな行動のみ
- サウンドはオプション（設定でオフ可能）

**Layer 2（AIアシスタント層）**:
- 既存の`IAiService`をそのまま維持
- ペット層とは独立して動作
- 💬ボタンで明示的に呼び出し

**将来の拡張（Phase 3）**:
- 人型キャラクター追加時に`ICompanionChatService`を実装
- 人型のみテキスト会話対応
- 動物タイプは引き続き非言語コミュニケーション

---

**作成日**: 2025-12-29
**最終更新**: 2026-01-28
**ステータス**: Phase 1 完了（感情・絆システム実装済み）
**次期開発**: Phase 2 - Live2D/音声合成強化

### 実装履歴

| フェーズ | ステータス | 完了日 |
|---------|----------|--------|
| Phase 1: 感情・絆システム | ✅ 完了 | 2026-01-28 |
| Phase 2: Live2D/音声強化 | 🔄 計画済み | - |
| Phase 3: 人型キャラクター | 📝 設計中 | - |
