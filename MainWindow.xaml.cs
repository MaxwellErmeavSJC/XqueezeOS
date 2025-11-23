using System;
using System.IO;
using System.Windows;

namespace XqueezeOS
{
    public partial class MainWindow : Window
    {
        private readonly string _username;

        public MainWindow(string username)
        {
            InitializeComponent();
            _username = string.IsNullOrWhiteSpace(username) ? Environment.UserName : username;
            ProfileUsername.Text = _username;
            DeviceNameText.Text = $"Device: {Environment.MachineName}";

            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XqueezeOS");
            Directory.CreateDirectory(appData);
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenAbout_Click(sender, e);
        }

        private void OpenAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutDeviceWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private void OpenFileManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileManager = new FileManagerWindow();
                fileManager.Owner = this;
                fileManager.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening File Manager: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var camera = new CameraWindow();
                camera.Owner = this;
                camera.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Camera: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenGallery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gallery = new GalleryWindow();
                gallery.Owner = this;
                gallery.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Gallery: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenContacts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Contacts feature coming soon!", "XqueezeOS",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Launch Windows Calculator
                System.Diagnostics.Process.Start("calc.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Calculator: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}