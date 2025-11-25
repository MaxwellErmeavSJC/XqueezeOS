using System;
using System.IO;
using System.Windows;

namespace XqueezeOS
{
    /// <summary>
    /// Main Desktop Window with navigation to all mini-apps
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _username;

        public MainWindow(string username)
        {
            InitializeComponent();

            _username = string.IsNullOrWhiteSpace(username) ? Environment.UserName : username;
            ProfileUsername.Text = _username;
            DeviceNameText.Text = $"Device: {Environment.MachineName}";

            // Create XqueezeOS application data folder
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XqueezeOS");
            Directory.CreateDirectory(appData);
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenAbout_Click(sender, e);
        }

        /// <summary>
        /// Opens About Device window as a dialog
        /// </summary>
        private void OpenAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var about = new AboutDeviceWindow();
                about.Owner = this; // Set this as parent window
                about.ShowDialog(); // Show as modal dialog
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening About Device: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens File Manager window
        /// </summary>
        private void OpenFileManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileManager = new FileManagerWindow();
                fileManager.Owner = this; // Set parent for better window management
                fileManager.Show(); // Show as non-modal window

                // Optional: Bring to front if already open
                fileManager.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening File Manager: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens Camera window
        /// </summary>
        private void OpenCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var camera = new CameraWindow();
                camera.Owner = this;
                camera.Show();
                camera.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Camera: {ex.Message}\n\n" +
                    "Make sure AForge.Video packages are installed:\n" +
                    "- AForge.Video\n" +
                    "- AForge.Video.DirectShow",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens Gallery window
        /// </summary>
        private void OpenGallery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gallery = new GalleryWindow();
                gallery.Owner = this;
                gallery.Show();
                gallery.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Gallery: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Placeholder for Contacts app
        /// </summary>
        private void OpenContacts_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ContactsPage();
        }

        /// <summary>
        /// Placeholder for Calculator app
        /// </summary>
        private void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Calculator app not implemented yet.\n\nThis is assigned to another team member.",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Optional: Handle window closing
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Ask for confirmation before closing
            var result = MessageBox.Show("Are you sure you want to exit XqueezeOS?",
                "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }
    }
}
