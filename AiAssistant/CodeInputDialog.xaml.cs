using System.Windows;

namespace AiAssistant
{
    /// <summary>
    /// コード入力用の大型ダイアログ
    /// シンタックスハイライトなしのシンプルなコードエディタ
    /// </summary>
    public partial class CodeInputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public CodeInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();

            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;

            // フォーカスを入力欄に
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    InputTextBox.SelectAll();
                }
            };

            // Ctrl+Enterで確定
            InputTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter &&
                    System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                {
                    OkButton_Click(s, e);
                }
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardText = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // 既存のテキストがある場合は追加、なければ置換
                        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
                        {
                            InputTextBox.Text = clipboardText;
                        }
                        else
                        {
                            // カーソル位置に挿入
                            var caretIndex = InputTextBox.CaretIndex;
                            InputTextBox.Text = InputTextBox.Text.Insert(caretIndex, clipboardText);
                            InputTextBox.CaretIndex = caretIndex + clipboardText.Length;
                        }
                        InputTextBox.Focus();
                    }
                }
            }
            catch
            {
                // クリップボードアクセスエラーは無視
            }
        }

        /// <summary>
        /// コード入力ダイアログを表示して結果を取得
        /// </summary>
        public static string? Show(string title, string prompt, string defaultValue = "", Window? owner = null)
        {
            var dialog = new CodeInputDialog(title, prompt, defaultValue);
            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            if (dialog.ShowDialog() == true)
            {
                return dialog.InputText;
            }

            return null;
        }
    }
}
