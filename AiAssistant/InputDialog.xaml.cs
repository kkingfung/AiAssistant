using System.Windows;

namespace AiAssistant
{
    /// <summary>
    /// シンプルな入力ダイアログ
    /// </summary>
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();

            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;

            // フォーカスを入力欄に
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };

            // Enterキーで確定
            InputTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter &&
                    !System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                {
                    OkButton_Click(s, e);
                }
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 入力ダイアログを表示して結果を取得
        /// </summary>
        public static string? Show(string title, string prompt, string defaultValue = "", Window? owner = null)
        {
            var dialog = new InputDialog(title, prompt, defaultValue);
            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                return dialog.InputText;
            }

            return null;
        }
    }
}
