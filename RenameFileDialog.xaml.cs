using System.Windows;

namespace XqueezeOS
{
    public partial class RenameFileDialog : Window
    {
        public string NewFileName { get; private set; }

        public RenameFileDialog(string currentFileName)
        {
            InitializeComponent();
            txtNewFileName.Text = currentFileName;
            txtNewFileName.SelectAll();
            txtNewFileName.Focus();
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewFileName.Text))
            {
                MessageBox.Show("Please enter a file name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewFileName = txtNewFileName.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}