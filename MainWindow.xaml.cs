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
            MessageBox.Show("File Manager not implemented yet.", "Placeholder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenCamera_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Camera not implemented yet.", "Placeholder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenGallery_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gallery not implemented yet.", "Placeholder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenContacts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Contacts not implemented yet.", "Placeholder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Calculator not implemented yet.", "Placeholder", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
