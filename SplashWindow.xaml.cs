using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace XqueezeOS
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer _timer;
        private int _progress = 0;
        private string _username;

        // Default constructor
        public SplashWindow() : this(Environment.UserName)
        {
        }

        // Constructor with username
        public SplashWindow(string username)
        {
            InitializeComponent();
            _username = username;
            StartBootSequence();
        }

        private void StartBootSequence()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _progress += 2;

            if (_progress <= 100)
            {
                // Try to update progressBar if it exists
                try
                {
                    var progressBarField = this.GetType().GetField("progressBar",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (progressBarField != null)
                    {
                        var progressBar = progressBarField.GetValue(this) as System.Windows.Controls.ProgressBar;
                        if (progressBar != null)
                            progressBar.Value = _progress;
                    }
                }
                catch { }

                // Try to update txtProgress if it exists
                try
                {
                    var txtProgressField = this.GetType().GetField("txtProgress",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (txtProgressField != null)
                    {
                        var txtProgress = txtProgressField.GetValue(this) as System.Windows.Controls.TextBlock;
                        if (txtProgress != null)
                            txtProgress.Text = $"{_progress}%";
                    }
                }
                catch { }

                // Try to update txtStatus if it exists
                try
                {
                    var txtStatusField = this.GetType().GetField("txtStatus",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (txtStatusField != null)
                    {
                        var txtStatus = txtStatusField.GetValue(this) as System.Windows.Controls.TextBlock;
                        if (txtStatus != null)
                        {
                            if (_progress < 20)
                                txtStatus.Text = "Initializing system...";
                            else if (_progress < 40)
                                txtStatus.Text = "Loading kernel modules...";
                            else if (_progress < 60)
                                txtStatus.Text = "Mounting file systems...";
                            else if (_progress < 80)
                                txtStatus.Text = "Starting services...";
                            else if (_progress < 95)
                                txtStatus.Text = $"Loading user profile: {_username}...";
                            else
                                txtStatus.Text = "Welcome to XqueezeOS!";
                        }
                    }
                }
                catch { }
            }
            else
            {
                _timer.Stop();
                CompleteBootSequence();
            }
        }

        private async void CompleteBootSequence()
        {
            await Task.Delay(500);

            var mainWindow = new MainWindow(_username);
            mainWindow.Show();

            this.Close();
        }
    }
}