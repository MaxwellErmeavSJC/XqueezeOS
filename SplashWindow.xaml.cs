using System;
using System.Windows;
using System.Windows.Threading;

namespace XqueezeOS
{
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private int _progress = 0;

        public SplashWindow()
        {
            InitializeComponent();

            // Timer simulates BIOS -> OS boot. Update progress bar and messages.
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(60);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _progress += 1;
            BootProgressBar.Value = _progress; // ensure ProgressBar is named BootProgressBar in XAML

            // Staged boot messages
            if (_progress < 20) StatusText.Text = "POST: Initializing hardware...";
            else if (_progress < 45) StatusText.Text = "Loading kernel modules...";
            else if (_progress < 70) StatusText.Text = "Starting system services...";
            else if (_progress < 95) StatusText.Text = "Preparing desktop environment...";
            else StatusText.Text = "Ready. Launching...";

            if (_progress >= 100)
            {
                _timer.Stop();

                // Pass logged-in username to MainWindow
                var user = Environment.UserName;
                var main = new MainWindow(user);
                main.Show();

                // Close splash screen
                this.Close();
            }
        }
    }
}
