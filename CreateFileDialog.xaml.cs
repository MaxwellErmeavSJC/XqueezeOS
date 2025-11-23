using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace XqueezeOS
{
    public partial class CreateFileDialog : Window
    {
        public string FileName { get; private set; }
        public string FileContent { get; private set; }
        public string FileExtension { get; private set; }
        public string FullFileName => $"{FileName}{FileExtension}";

        public CreateFileDialog()
        {
            InitializeComponent();
            cmbFileType.SelectedIndex = 0;

            // Clear placeholder text when focused
            txtFileContent.GotFocus += (s, e) =>
            {
                if (txtFileContent.Text == "Enter file content here...")
                    txtFileContent.Text = "";
            };

            // Restore placeholder if empty
            txtFileContent.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFileContent.Text))
                    txtFileContent.Text = "Enter file content here...";
            };
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // Validate file name
            if (string.IsNullOrWhiteSpace(txtFileName.Text))
            {
                MessageBox.Show("Please enter a file name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFileName.Focus();
                return;
            }

            // Validate file name characters
            if (HasInvalidFileNameChars(txtFileName.Text))
            {
                MessageBox.Show("File name contains invalid characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFileName.Focus();
                return;
            }

            // Set properties
            FileName = txtFileName.Text.Trim();
            FileContent = txtFileContent.Text == "Enter file content here..." ? "" : txtFileContent.Text;
            FileExtension = cmbFileType.SelectedIndex switch
            {
                0 => ".txt",
                1 => ".json",
                2 => ".xml",
                3 => ".html",
                4 => ".csv",
                _ => ".txt"
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool HasInvalidFileNameChars(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return fileName.IndexOfAny(invalidChars) >= 0;
        }

        // Optional: Handle Enter key to create file
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && !txtFileContent.IsFocused)
            {
                BtnCreate_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                BtnCancel_Click(this, new RoutedEventArgs());
            }
            base.OnKeyDown(e);
        }
    }
}