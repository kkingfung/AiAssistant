using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AiAssistant
{
    // 簡單狀態列舉
    public enum AssistantState
    {
        Idle,
        Listening,
        Thinking,
        Speaking
    }

    /// <summary>
    /// 簡化的 ViewModel，示範非同步呼叫 IAiService（不阻塞 UI）並支援串流與取消。
    /// 可直接設定為 Window.DataContext。
    /// </summary>
    public sealed class AssistantViewModel : INotifyPropertyChanged
    {
        private readonly IAiService _aiService;
        private CancellationTokenSource? _cts;

        private AssistantState _state = AssistantState.Idle;
        public AssistantState State
        {
            get => _state;
            private set { if (_state == value) return; _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateText)); }
        }

        public string StateText => State.ToString();

        private string _responseText = string.Empty;
        public string ResponseText
        {
            get => _responseText;
            private set { if (_responseText == value) return; _responseText = value; OnPropertyChanged(); }
        }

        public ICommand SendPromptCommand { get; }
        public ICommand StreamPromptCommand { get; }
        public ICommand CancelCommand { get; }

        public AssistantViewModel(IAiService? aiService = null)
        {
            _aiService = aiService ?? new MockAiService();

            SendPromptCommand = new DelegateCommand(async p => await SendPromptAsync((p as string) ?? string.Empty),
                                                  _ => State == AssistantState.Idle);
            StreamPromptCommand = new DelegateCommand(async p => await StreamPromptAsync((p as string) ?? string.Empty),
                                                     _ => State == AssistantState.Idle);
            CancelCommand = new DelegateCommand(_ => Cancel(), _ => State == AssistantState.Thinking || State == AssistantState.Listening);
        }

        // 非同步一次性回應（UI thread-safe: await will resume on UI context）
        public async Task SendPromptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;
            Cancel(); // cancel any previous work
            _cts = new CancellationTokenSource();

            try
            {
                State = AssistantState.Listening;
                await Task.Delay(80, _cts.Token).ConfigureAwait(true); // small UX delay to show state
                State = AssistantState.Thinking;

                // 非同步取得完整回應（MockAiService 模擬延遲）
                var result = await _aiService.GetResponseAsync(prompt, _cts.Token).ConfigureAwait(false);

                // 切回 UI 執行續更新 UI 屬性
                await ApplicationCurrentInvokeAsync(() =>
                {
                    ResponseText = result;
                    State = AssistantState.Speaking;
                }).ConfigureAwait(false);

                // 稍後回到 Idle
                await Task.Delay(300, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 使用者已取消，保持或重設狀態
            }
            finally
            {
                // 回到 UI 執行續清理/設定 Idle
                await ApplicationCurrentInvokeAsync(() => State = AssistantState.Idle).ConfigureAwait(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        // 非同步串流回應（逐段顯示）
        public async Task StreamPromptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;
            Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                State = AssistantState.Listening;
                await Task.Delay(80, _cts.Token).ConfigureAwait(true);
                State = AssistantState.Thinking;

                ResponseText = string.Empty;

                await foreach (var chunk in _aiService.StreamResponseAsync(prompt, _cts.Token))
                {
                    // 每個 chunk 都以 UI 執行續更新
                    await ApplicationCurrentInvokeAsync(() =>
                    {
                        ResponseText += (ResponseText.Length == 0 ? "" : " ") + chunk;
                    }).ConfigureAwait(false);
                }

                // 完成後標示為 Speaking（或直接 Idle）
                await ApplicationCurrentInvokeAsync(() => State = AssistantState.Speaking).ConfigureAwait(false);
                await Task.Delay(300, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 取消
            }
            finally
            {
                await ApplicationCurrentInvokeAsync(() => State = AssistantState.Idle).ConfigureAwait(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void Cancel()
        {
            try
            {
                _cts?.Cancel();
            }
            catch { }
        }

        // 幫助函式：確保在 UI 執行續執行 action
        private static Task ApplicationCurrentInvokeAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object?>();
            var app = System.Windows.Application.Current;
            if (app == null || app.Dispatcher == null)
            {
                try { action(); tcs.SetResult(null); }
                catch (Exception ex) { tcs.SetException(ex); }
                return tcs.Task;
            }

            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                try { action(); tcs.SetResult(null); }
                catch (Exception ex) { tcs.SetException(ex); }
            }));
            return tcs.Task;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // 簡潔 DelegateCommand，保留最小實作
    internal sealed class DelegateCommand : ICommand
    {
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Action<object?>? _execute;
        private readonly Predicate<object?>? _canExecute;

        public DelegateCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter)
        {
            if (_executeAsync != null) await _executeAsync(parameter).ConfigureAwait(false);
            else _execute?.Invoke(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { } // minimal
            remove { }
        }
    }
}