using System.Windows;
using System.Windows.Input;

namespace FloatingAssistant
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // 滑鼠左鍵按下並拖曳即可移動視窗
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // DragMove 可能在某些情況拋例外，這裡忽略以維持簡潔
                }
            }
        }
    }
}