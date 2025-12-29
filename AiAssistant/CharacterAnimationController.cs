using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace AiAssistant
{
    /// <summary>
    /// キャラクターアニメーションを管理するコントローラー
    /// ランダムにアニメーションを切り替えます
    /// </summary>
    public class CharacterAnimationController : IDisposable
    {
        private readonly Image _imageControl;
        private readonly List<string> _animationPaths;
        private readonly Random _random;
        private readonly DispatcherTimer _switchTimer;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _currentAnimationIndex = 0;
        private string _selectedPetType = "Dragon";

        /// <summary>
        /// アニメーション切り替え間隔（ミリ秒）
        /// </summary>
        public int SwitchIntervalMs { get; set; } = 15000; // 15秒ごと

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="imageControl">アニメーションを表示するImageコントロール</param>
        /// <param name="animationsFolder">アニメーションファイルが格納されているフォルダ</param>
        /// <param name="petType">表示するペットの種類 (Cat, Crab, Dragon, Frog, Shark, Snake, Random)</param>
        public CharacterAnimationController(Image imageControl, string animationsFolder, string petType = "Dragon")
        {
            _imageControl = imageControl ?? throw new ArgumentNullException(nameof(imageControl));
            _random = new Random();
            _animationPaths = new List<string>();
            _selectedPetType = petType;

            // アニメーションファイルを検索（GIF, PNG, WebM対応）
            if (Directory.Exists(animationsFolder))
            {
                var extensions = new[] { "*.gif", "*.png", "*.webm", "*.mp4" };
                var allFiles = new List<string>();

                foreach (var ext in extensions)
                {
                    allFiles.AddRange(Directory.GetFiles(animationsFolder, ext));
                }

                // ペットタイプでフィルタリング
                if (petType.Equals("Random", StringComparison.OrdinalIgnoreCase))
                {
                    // Randomの場合は全てのアニメーションを使用
                    _animationPaths.AddRange(allFiles);
                    Console.WriteLine($"[CharacterAnim] ランダムモード: {_animationPaths.Count}個のアニメーションファイルを検出");
                }
                else
                {
                    // 指定されたペットのアニメーションのみを抽出
                    _animationPaths.AddRange(allFiles.Where(f =>
                        Path.GetFileName(f).StartsWith(petType, StringComparison.OrdinalIgnoreCase)));

                    Console.WriteLine($"[CharacterAnim] {petType}: {_animationPaths.Count}個のアニメーションファイルを検出");
                }
            }
            else
            {
                Console.WriteLine($"[CharacterAnim] フォルダが見つかりません: {animationsFolder}");
            }

            // タイマーを設定
            _switchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SwitchIntervalMs)
            };
            _switchTimer.Tick += OnSwitchTimerTick;
        }

        /// <summary>
        /// アニメーション再生を開始します
        /// </summary>
        public void Start()
        {
            if (_animationPaths.Count == 0)
            {
                Console.WriteLine("[CharacterAnim] アニメーションファイルがありません");
                return;
            }

            // 初回アニメーションを読み込み
            LoadRandomAnimation();

            // タイマー開始
            _switchTimer.Start();
            Console.WriteLine("[CharacterAnim] アニメーション再生開始");
        }

        /// <summary>
        /// アニメーション再生を停止します
        /// </summary>
        public void Stop()
        {
            _switchTimer.Stop();
            _cancellationTokenSource?.Cancel();
            Console.WriteLine("[CharacterAnim] アニメーション再生停止");
        }

        /// <summary>
        /// ランダムなアニメーションを読み込みます
        /// </summary>
        private void LoadRandomAnimation()
        {
            if (_animationPaths.Count == 0) return;

            // ランダムなインデックスを選択（現在と同じものは避ける）
            int newIndex;
            if (_animationPaths.Count > 1)
            {
                do
                {
                    newIndex = _random.Next(_animationPaths.Count);
                } while (newIndex == _currentAnimationIndex);
            }
            else
            {
                newIndex = 0;
            }

            _currentAnimationIndex = newIndex;
            LoadAnimation(_animationPaths[newIndex]);
        }

        /// <summary>
        /// 指定されたアニメーションを読み込みます
        /// </summary>
        private void LoadAnimation(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".gif")
                {
                    // 既存のアニメーションをクリア（キャッシュ問題を防ぐ）
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(_imageControl, null);
                    _imageControl.Source = null;

                    // GIFアニメーション - WpfAnimatedGifライブラリを使用
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // キャッシュを無視
                    bitmap.EndInit();

                    // WpfAnimatedGifを使用してGIFアニメーションを再生
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(_imageControl, bitmap);
                    WpfAnimatedGif.ImageBehavior.SetRepeatBehavior(_imageControl, System.Windows.Media.Animation.RepeatBehavior.Forever);
                }
                else if (extension == ".png")
                {
                    // 静止画
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // クロマキー処理を適用（マゼンタを透明に）
                    var chromaKeyApplied = ChromaKeyHelper.ApplyChromaKey(bitmap);

                    _imageControl.Source = chromaKeyApplied;
                }
                else if (extension == ".webm" || extension == ".mp4")
                {
                    // WebM/MP4 動画
                    // 注意: WPFのImageコントロールではWebMを直接表示できないため、
                    // MediaElementを使用する必要があります
                    Console.WriteLine($"[CharacterAnim] WebM/MP4はMediaElementが必要です: {Path.GetFileName(filePath)}");

                    // とりあえずプレースホルダーを表示
                    _imageControl.Source = null;
                }

                Console.WriteLine($"[CharacterAnim] アニメーション切り替え: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterAnim] アニメーション読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// タイマーイベント：アニメーションを切り替え
        /// </summary>
        private void OnSwitchTimerTick(object? sender, EventArgs e)
        {
            LoadRandomAnimation();
        }

        public void Dispose()
        {
            Stop();
            _switchTimer?.Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}
