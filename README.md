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
  - 固定サイズ（360×400px）
  - タスクバーから非表示
  - 閉じるボタン（×）付き

- **インタラクティブチャット**
  - キャラクターをクリックしてチャットバルーンを表示
  - 大型チャットウィンドウ（340×380px）
  - テキスト入力とEnterキーで送信
  - リアルタイムストリーミングレスポンス
  - Markdownフォーマット対応
  - 送信中は「入力中...」表示と送信ボタン無効化

- **クリックスルーモード**
  - 右クリックまたはCtrl+Alt+Tで切り替え
  - マウスクリックをウィンドウを透過（作業の邪魔にならない）

- **アニメーションキャラクター** 🐉
  - 6種類のSciFiペット（Cat、Crab、Dragon、Frog、Shark、Snake）
  - 54個の透明背景GIFアニメーション（320×320px）
  - マゼンタクロマキー除去済み
  - ランダムアニメーション自動切り替え（15秒ごと）
  - UIからペットを即座に切り替え可能
  - 右下配置で統一されたサイズ表示

- **ペット選択UI** 🐾
  - ワンクリックで6種類のペットを切り替え
  - リアルタイムアニメーション更新
  - 設定の自動保存
  - ランダムモード（全ペットをミックス表示）

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
       "Model": "llama3.1:8b",
       "MaxTokens": 2000,
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
├── AiAssistant/
│   ├── MainWindow.xaml / .xaml.cs       # メインUIウィンドウ
│   ├── AssistantViewModel.cs            # ViewModel（状態管理）
│   ├── IAiService.cs                    # AIサービスインターフェース
│   ├── OllamaAiService.cs               # Ollama統合
│   ├── ChatGptService.cs                # ChatGPT統合
│   ├── MockAiService.cs                 # モックAI実装
│   ├── AiServiceFactory.cs              # サービス選択ロジック
│   ├── CharacterAnimationController.cs  # アニメーション管理
│   ├── ChromaKeyHelper.cs               # マゼンタ背景除去
│   ├── AppSettings.cs                   # 設定管理
│   ├── appsettings.json                 # 設定ファイル
│   └── CharacterAnimations/             # アニメーションファイル
│       ├── CatIdle01.webm
│       ├── DragonIdle01.webm
│       └── ... (全50個のアニメーション)
├── Sci-Fi Boss pack/                    # Unity 3Dアセット（gitignore対象）
├── OLLAMA_SETUP.md                      # Ollamaセットアップガイド
├── OLLAMA_INTEGRATION_SUMMARY.md        # 実装詳細
├── UNITY_ANIMATION_EXPORT_GUIDE.md      # Unity録画ガイド
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
- [x] アニメーションキャラクター表示（6種類のペット）
- [x] Unity Recorder連携（WebM録画）
- [x] マゼンタクロマキー自動除去
- [x] ペット選択UI
- [x] ランダムアニメーション切り替え
- [x] ダークテーマ対応

### 計画中 🔄

- [ ] 音声入力サポート
- [ ] ペットごとの音声エフェクト

### 追加実装済み ✅

- [x] Google Calendar統合（複数カレンダー、週/月表示）
- [x] Gmail統合（メール一覧、既読/未読切り替え）
- [x] 天気情報表示
- [x] ファンド価格チェック
- [x] 為替レート（JPY/HKD/KRW）
- [x] Claude使用量確認（組織API）
- [x] 設定UI（GUIベース）
- [x] 会話履歴の永続化

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
**最終更新**: 2025-12-29
**ステータス**: 実用可能（ローカルLLM、ChatGPT、キャラクターアニメーション統合済み）
