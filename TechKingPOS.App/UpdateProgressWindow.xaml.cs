using System.ComponentModel;
using System.Windows;

namespace TechKingPOS.App
{
    public partial class UpdateProgressWindow : Window
    {
        private bool _allowClose = false;

        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        // Block close unless explicitly allowed
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
            }
        }

        // Call this when update is fully finished
        public void AllowClose()
        {
            _allowClose = true;
        }

        // Update status text
        public void SetStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }

        // Update progress (0–100)
        public void SetProgress(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percent;
                PercentText.Text = $"{percent}%";
            });
        }
    }
}
