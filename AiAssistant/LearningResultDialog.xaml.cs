using System;
using System.Windows;

namespace AiAssistant
{
    /// <summary>
    /// 学習結果表示用の大型ダイアログ
    /// ノート保存機能付き
    /// </summary>
    public partial class LearningResultDialog : Window
    {
        private readonly Action<string>? _saveNoteAction;
        private bool _noteSaved = false;

        /// <summary>
        /// 表示中のコンテンツ
        /// </summary>
        public string ResultContent { get; private set; } = string.Empty;

        public LearningResultDialog(string title, string content, Action<string>? saveNoteAction = null)
        {
            InitializeComponent();

            Title = title;
            TitleText.Text = title;
            ContentTextBox.Text = content;
            ResultContent = content;
            _saveNoteAction = saveNoteAction;

            // ノート保存機能がない場合はボタンを非表示
            if (saveNoteAction == null)
            {
                SaveNoteButton.Visibility = Visibility.Collapsed;
            }

            // Escキーで閉じる
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                }
            };
        }

        private void SaveNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_saveNoteAction != null && !_noteSaved)
            {
                _saveNoteAction(ResultContent);
                _noteSaved = true;
                SaveNoteButton.Content = "✓ 保存済み";
                SaveNoteButton.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x38, 0x8E, 0x3C));
                SaveNoteButton.IsEnabled = false;
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ResultContent);
                CopyButton.Content = "✓ コピー済み";

                // 2秒後に元に戻す
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    CopyButton.Content = "📋 コピー";
                    timer.Stop();
                };
                timer.Start();
            }
            catch
            {
                // クリップボードアクセスエラーは無視
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 学習結果ダイアログを表示
        /// </summary>
        /// <param name="title">ダイアログタイトル</param>
        /// <param name="content">表示するコンテンツ</param>
        /// <param name="saveNoteAction">ノート保存時のアクション（nullの場合はボタン非表示）</param>
        /// <param name="owner">親ウィンドウ</param>
        public static void Show(string title, string content, Action<string>? saveNoteAction = null, Window? owner = null)
        {
            var dialog = new LearningResultDialog(title, content, saveNoteAction);
            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            dialog.ShowDialog();
        }
    }
}
