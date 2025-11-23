using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace XqueezeOS
{
    /// <summary>
    /// Gallery Window - Displays and manages captured images
    /// </summary>
    public partial class GalleryWindow : Window
    {
        #region Windows API Declarations

        // Windows API for shell operations
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        const int SW_SHOW = 5;
        const uint SEE_MASK_INVOKEIDLIST = 12;

        #endregion

        private ObservableCollection<ImageItem> _allImages;
        private ObservableCollection<ImageItem> _filteredImages;
        private string _photoStoragePath;
        private ImageItem _selectedImage;

        public GalleryWindow()
        {
            InitializeComponent();
            InitializeGallery();
        }

        private void InitializeGallery()
        {
            _photoStoragePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XqueezeOS",
                "Photos"
            );

            if (!Directory.Exists(_photoStoragePath))
            {
                Directory.CreateDirectory(_photoStoragePath);
            }

            _allImages = new ObservableCollection<ImageItem>();
            _filteredImages = new ObservableCollection<ImageItem>();

            LoadImages();
        }

        private void LoadImages()
        {
            try
            {
                _allImages.Clear();

                if (!Directory.Exists(_photoStoragePath))
                {
                    ShowEmptyState(true);
                    return;
                }

                // Get all image files
                string[] extensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" };
                var imageFiles = extensions
                    .SelectMany(ext => Directory.GetFiles(_photoStoragePath, ext))
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                if (imageFiles.Count == 0)
                {
                    ShowEmptyState(true);
                    UpdateStatus("No images found");
                    return;
                }

                foreach (string filePath in imageFiles)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(filePath);

                        // Create thumbnail
                        string thumbnailPath = CreateThumbnail(filePath);

                        // Determine if it's a photo or screenshot
                        bool isScreenshot = fileInfo.Name.StartsWith("Screenshot_");

                        _allImages.Add(new ImageItem
                        {
                            FileName = fileInfo.Name,
                            FilePath = filePath,
                            ThumbnailPath = thumbnailPath ?? filePath,
                            FileSize = FormatFileSize(fileInfo.Length),
                            FileSizeBytes = fileInfo.Length,
                            DateTaken = fileInfo.CreationTime.ToString("MM/dd/yyyy hh:mm tt"),
                            DateTakenDateTime = fileInfo.CreationTime,
                            FileInfo = $"{FormatFileSize(fileInfo.Length)} • {fileInfo.CreationTime:MM/dd/yyyy}",
                            IsScreenshot = isScreenshot
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading image {filePath}: {ex.Message}");
                    }
                }

                ApplyFilter();
                ShowEmptyState(false);
                UpdateStatus($"Loaded {_allImages.Count} images");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading images: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CreateThumbnail(string imagePath)
        {
            try
            {
                string thumbnailDir = Path.Combine(_photoStoragePath, ".thumbnails");
                if (!Directory.Exists(thumbnailDir))
                {
                    Directory.CreateDirectory(thumbnailDir);
                }

                string thumbnailPath = Path.Combine(
                    thumbnailDir,
                    Path.GetFileNameWithoutExtension(imagePath) + "_thumb.jpg"
                );

                // Check if thumbnail already exists
                if (File.Exists(thumbnailPath))
                {
                    return thumbnailPath;
                }

                // Create thumbnail using System.Drawing
                using (var image = System.Drawing.Image.FromFile(imagePath))
                {
                    int thumbWidth = 200;
                    int thumbHeight = 200;

                    // Calculate aspect ratio
                    float ratio = Math.Min((float)thumbWidth / image.Width, (float)thumbHeight / image.Height);
                    int newWidth = (int)(image.Width * ratio);
                    int newHeight = (int)(image.Height * ratio);

                    using (var thumbnail = new Bitmap(newWidth, newHeight))
                    {
                        using (var graphics = Graphics.FromImage(thumbnail))
                        {
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                        }

                        thumbnail.Save(thumbnailPath, ImageFormat.Jpeg);
                    }
                }

                return thumbnailPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating thumbnail: {ex.Message}");
                return null;
            }
        }

        private void ApplyFilter()
        {
            _filteredImages.Clear();

            var filtered = _allImages.AsEnumerable();

            // Apply selected filter
            if (cmbFilter.SelectedIndex == 1) // Photos Only
            {
                filtered = filtered.Where(i => !i.IsScreenshot);
            }
            else if (cmbFilter.SelectedIndex == 2) // Screenshots Only
            {
                filtered = filtered.Where(i => i.IsScreenshot);
            }
            else if (cmbFilter.SelectedIndex == 3) // Today
            {
                filtered = filtered.Where(i => i.DateTakenDateTime.Date == DateTime.Today);
            }
            else if (cmbFilter.SelectedIndex == 4) // This Week
            {
                var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                filtered = filtered.Where(i => i.DateTakenDateTime >= weekStart);
            }

            foreach (var image in filtered)
            {
                _filteredImages.Add(image);
            }

            // Update display
            itemsGrid.ItemsSource = _filteredImages;
            lstImages.ItemsSource = _filteredImages;

            txtImageCount.Text = _filteredImages.Count.ToString();
            txtSelectedCount.Text = "0";

            if (_filteredImages.Count == 0)
            {
                ShowEmptyState(true);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadImages();
        }

        private void BtnViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedImage != null)
            {
                try
                {
                    // Open image with default viewer using Windows Shell API
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _selectedImage.FilePath,
                        UseShellExecute = true
                    });

                    UpdateStatus($"Opened: {_selectedImage.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening image: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedImage != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{_selectedImage.FileName}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Delete file
                        File.Delete(_selectedImage.FilePath);

                        // Delete thumbnail if exists
                        if (File.Exists(_selectedImage.ThumbnailPath) &&
                            _selectedImage.ThumbnailPath != _selectedImage.FilePath)
                        {
                            File.Delete(_selectedImage.ThumbnailPath);
                        }

                        UpdateStatus($"Deleted: {_selectedImage.FileName}");
                        LoadImages();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting image: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnImageInfo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedImage != null)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(_selectedImage.FilePath);

                    // Get image dimensions
                    string dimensions = "Unknown";
                    try
                    {
                        using (var image = System.Drawing.Image.FromFile(_selectedImage.FilePath))
                        {
                            dimensions = $"{image.Width} x {image.Height} pixels";
                        }
                    }
                    catch { }

                    string type = _selectedImage.IsScreenshot ? "Screenshot" : "Photo";

                    MessageBox.Show(
                        $"File: {_selectedImage.FileName}\n" +
                        $"Type: {type}\n" +
                        $"Size: {_selectedImage.FileSize}\n" +
                        $"Dimensions: {dimensions}\n" +
                        $"Taken: {_selectedImage.DateTaken}\n" +
                        $"Modified: {fileInfo.LastWriteTime:MM/dd/yyyy hh:mm tt}\n" +
                        $"Path: {_selectedImage.FilePath}",
                        "Image Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error getting image info: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open folder using Windows Shell API
                Process.Start(new ProcessStartInfo
                {
                    FileName = _photoStoragePath,
                    UseShellExecute = true,
                    Verb = "open"
                });

                UpdateStatus("Opened photo storage folder");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbViewMode.SelectedIndex == 0) // Grid View
            {
                scrollGridView.Visibility = Visibility.Visible;
                lstImages.Visibility = Visibility.Collapsed;
            }
            else // List View
            {
                scrollGridView.Visibility = Visibility.Collapsed;
                lstImages.Visibility = Visibility.Visible;
            }
        }

        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allImages != null)
            {
                ApplyFilter();
            }
        }

        private void ItemsGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get clicked item
            var element = e.OriginalSource as FrameworkElement;
            if (element != null)
            {
                var imageItem = element.DataContext as ImageItem;
                if (imageItem != null)
                {
                    _selectedImage = imageItem;
                    EnableImageActions(true);
                    txtSelectedCount.Text = "1";

                    if (e.ClickCount == 2) // Double click
                    {
                        BtnViewImage_Click(sender, e);
                    }
                }
            }
        }

        private void LstImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstImages.SelectedItem is ImageItem selectedImage)
            {
                _selectedImage = selectedImage;
                EnableImageActions(true);
                txtSelectedCount.Text = "1";
            }
            else
            {
                _selectedImage = null;
                EnableImageActions(false);
                txtSelectedCount.Text = "0";
            }
        }

        private void LstImages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnViewImage_Click(sender, e);
        }

        private void EnableImageActions(bool enabled)
        {
            btnViewImage.IsEnabled = enabled;
            btnDeleteImage.IsEnabled = enabled;
            btnImageInfo.IsEnabled = enabled;
        }

        private void ShowEmptyState(bool show)
        {
            pnlEmptyState.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            if (show)
            {
                txtImageCount.Text = "0";
                txtSelectedCount.Text = "0";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "bytes", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
            Debug.WriteLine($"[Gallery] {message}");
        }
    }

    #region Helper Classes

    public class ImageItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ThumbnailPath { get; set; }
        public string FileSize { get; set; }
        public long FileSizeBytes { get; set; }
        public string DateTaken { get; set; }
        public DateTime DateTakenDateTime { get; set; }
        public string FileInfo { get; set; }
        public bool IsScreenshot { get; set; }
    }

    #endregion
}