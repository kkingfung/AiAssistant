using System;
using System.Windows;
using System.Windows.Controls;

namespace AiAssistant
{
    /// <summary>
    /// 設定ウィンドウ
    /// アプリケーションの各種設定をGUIで変更できます
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly IChatHistoryService? _chatHistoryService;

        /// <summary>
        /// 設定が保存されたかどうか
        /// </summary>
        public bool SettingsSaved { get; private set; }

        public SettingsWindow(IChatHistoryService? chatHistoryService = null)
        {
            InitializeComponent();
            _settings = AppSettings.Instance;
            _chatHistoryService = chatHistoryService;

            LoadSettings();
        }

        /// <summary>
        /// 現在の設定をUIに読み込みます
        /// </summary>
        private void LoadSettings()
        {
            // アシスタント設定
            SelectComboBoxItem(ThemeComboBox, _settings.Assistant.Theme);
            AnimationIntervalTextBox.Text = _settings.Assistant.AnimationSwitchIntervalSeconds.ToString();

            // ローカルLLM設定
            LocalLlmEnabledCheckBox.IsChecked = _settings.LocalLlm.Enabled;
            OllamaEndpointTextBox.Text = _settings.LocalLlm.Endpoint;
            OllamaModelTextBox.Text = _settings.LocalLlm.Model;
            PreferLocalCheckBox.IsChecked = _settings.LocalLlm.PreferLocal;

            // OpenAI設定
            OpenAiApiKeyBox.Password = _settings.OpenAI.ApiKey;
            SelectComboBoxItemText(OpenAiModelComboBox, _settings.OpenAI.Model);

            // Google設定
            GoogleClientIdTextBox.Text = _settings.Google.ClientId;
            GoogleClientSecretBox.Password = _settings.Google.ClientSecret;

            // Anthropic設定
            AnthropicAdminKeyBox.Password = _settings.Anthropic.AdminApiKey;
            AnthropicOrgIdTextBox.Text = _settings.Anthropic.OrganizationId;

            // 天気設定
            WeatherCityTextBox.Text = _settings.Weather.City;
            WeatherLatTextBox.Text = _settings.Weather.Latitude.ToString();
            WeatherLonTextBox.Text = _settings.Weather.Longitude.ToString();

            // 会話履歴設定
            var historySettings = ChatHistorySettings.Load();
            SaveHistoryCheckBox.IsChecked = historySettings.SaveHistory;
            MaxHistoryTextBox.Text = historySettings.MaxMessages.ToString();
        }

        /// <summary>
        /// UIの設定を保存します
        /// </summary>
        private void SaveSettings()
        {
            // アシスタント設定
            _settings.Assistant.Theme = GetComboBoxSelectedText(ThemeComboBox);
            if (int.TryParse(AnimationIntervalTextBox.Text, out int interval) && interval > 0)
            {
                _settings.Assistant.AnimationSwitchIntervalSeconds = interval;
            }

            // ローカルLLM設定
            _settings.LocalLlm.Enabled = LocalLlmEnabledCheckBox.IsChecked ?? true;
            _settings.LocalLlm.Endpoint = OllamaEndpointTextBox.Text.Trim();
            _settings.LocalLlm.Model = OllamaModelTextBox.Text.Trim();
            _settings.LocalLlm.PreferLocal = PreferLocalCheckBox.IsChecked ?? true;

            // OpenAI設定
            _settings.OpenAI.ApiKey = OpenAiApiKeyBox.Password;
            _settings.OpenAI.Model = GetComboBoxSelectedText(OpenAiModelComboBox);

            // Google設定
            _settings.Google.ClientId = GoogleClientIdTextBox.Text.Trim();
            _settings.Google.ClientSecret = GoogleClientSecretBox.Password;

            // Anthropic設定
            _settings.Anthropic.AdminApiKey = AnthropicAdminKeyBox.Password;
            _settings.Anthropic.OrganizationId = AnthropicOrgIdTextBox.Text.Trim();

            // 天気設定
            _settings.Weather.City = WeatherCityTextBox.Text.Trim();
            if (double.TryParse(WeatherLatTextBox.Text, out double lat))
            {
                _settings.Weather.Latitude = lat;
            }
            if (double.TryParse(WeatherLonTextBox.Text, out double lon))
            {
                _settings.Weather.Longitude = lon;
            }

            // 設定ファイルを保存
            _settings.Save();

            // 会話履歴設定を保存（既存の設定を読み込んで更新）
            var historySettings = ChatHistorySettings.Load();
            historySettings.SaveHistory = SaveHistoryCheckBox.IsChecked ?? true;
            if (int.TryParse(MaxHistoryTextBox.Text, out int max) && max > 0)
            {
                historySettings.MaxMessages = max;
            }
            historySettings.Save();

            Console.WriteLine("[Settings] 設定を保存しました");
        }

        /// <summary>
        /// コンボボックスの選択テキストを取得します
        /// </summary>
        private static string GetComboBoxSelectedText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "";
            }
            return comboBox.Text;
        }

        /// <summary>
        /// コンボボックスのアイテムを選択します（部分一致）
        /// </summary>
        private static void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item)
                {
                    var text = item.Content?.ToString() ?? "";
                    if (text.Contains(value, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// コンボボックスのアイテムを選択します（完全一致）
        /// </summary>
        private static void SelectComboBoxItemText(ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item)
                {
                    if (item.Content?.ToString() == value)
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
            }
            // 一致するものがなければテキストを設定
            comboBox.Text = value;
        }

        /// <summary>
        /// 保存ボタンクリック
        /// </summary>
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                SettingsSaved = true;
                MessageBox.Show(
                    "設定を保存しました。\n一部の設定は再起動後に反映されます。",
                    "保存完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"設定の保存に失敗しました。\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// キャンセルボタンクリック
        /// </summary>
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 履歴クリアボタンクリック
        /// </summary>
        private void OnClearHistoryClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "会話履歴をすべて削除しますか？\nこの操作は元に戻せません。",
                "履歴の削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _chatHistoryService?.ClearHistory();
                    MessageBox.Show("履歴を削除しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"履歴の削除に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
