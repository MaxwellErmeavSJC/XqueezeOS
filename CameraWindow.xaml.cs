using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

namespace XqueezeOS
{
    /// <summary>
    /// Camera Window - Captures photos and screenshots using Windows APIs
    /// NOTE: This implementation uses AForge.NET library for camera access
    /// You'll need to install: AForge.Video and AForge.Video.DirectShow NuGet packages
    /// </summary>
    public partial class CameraWindow : Window
    {
        #region Windows API Declarations

        // Windows API for taking screenshots
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth,
            int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        // System metrics constants
        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;
        const int SRCCOPY = 0x00CC0020;

        // Windows API for memory information
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        #endregion

        private VideoCaptureDevice _videoSource;
        private FilterInfoCollection _videoDevices;
        private string _photoStoragePath;
        private int _captureCount = 0;
        private Bitmap _currentFrame;

        public CameraWindow()
        {
            InitializeComponent();
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            // Create photo storage directory
            _photoStoragePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XqueezeOS",
                "Photos"
            );

            if (!Directory.Exists(_photoStoragePath))
            {
                Directory.CreateDirectory(_photoStoragePath);
            }

            txtStoragePath.Text = _photoStoragePath;

            // Enumerate video devices using DirectShow API (wrapped by AForge)
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                {
                    UpdateStatus("No camera devices found");
                    btnStartCamera.IsEnabled = false;
                }
                else
                {
                    UpdateStatus($"Found {_videoDevices.Count} camera device(s)");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing camera: {ex.Message}\n\n" +
                    "Note: Please install AForge.Video and AForge.Video.DirectShow NuGet packages.",
                    "Camera Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStartCamera.IsEnabled = false;
            }
        }

        private void BtnStartCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_videoDevices == null || _videoDevices.Count == 0)
                {
                    MessageBox.Show("No camera device available.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Use first available camera (DirectShow API via AForge)
                _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);

                // Set camera resolution if available
                if (_videoSource.VideoCapabilities.Length > 0)
                {
                    var capability = _videoSource.VideoCapabilities[0];
                    _videoSource.VideoResolution = capability;
                    txtResolution.Text = $"Resolution: {capability.FrameSize.Width}x{capability.FrameSize.Height}";
                }

                // Subscribe to new frame event
                _videoSource.NewFrame += VideoSource_NewFrame;

                // Start camera
                _videoSource.Start();

                // Update UI
                btnStartCamera.IsEnabled = false;
                btnStopCamera.IsEnabled = true;
                btnCapture.IsEnabled = true;
                pnlCameraPlaceholder.Visibility = Visibility.Collapsed;

                txtCameraStatus.Text = "Camera: Active";
                UpdateStatus("Camera started successfully");

                LogMemoryUsage("Camera Started");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting camera: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStopCamera_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void StopCamera()
        {
            try
            {
                if (_videoSource != null && _videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                    _videoSource.WaitForStop();
                    _videoSource.NewFrame -= VideoSource_NewFrame;
                }

                // Update UI
                btnStartCamera.IsEnabled = true;
                btnStopCamera.IsEnabled = false;
                btnCapture.IsEnabled = false;
                pnlCameraPlaceholder.Visibility = Visibility.Visible;
                imgPreview.Visibility = Visibility.Collapsed;

                txtCameraStatus.Text = "Camera: Inactive";
                UpdateStatus("Camera stopped");

                LogMemoryUsage("Camera Stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping camera: {ex.Message}");
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // This event is called on a background thread
            // We need to clone the bitmap as it will be disposed after this method returns
            try
            {
                if (_currentFrame != null)
                {
                    _currentFrame.Dispose();
                }

                _currentFrame = (Bitmap)eventArgs.Frame.Clone();

                // Update preview on UI thread
                Dispatcher.Invoke(() =>
                {
                    // Convert bitmap to WPF image source
                    using (var memory = new MemoryStream())
                    {
                        _currentFrame.Save(memory, ImageFormat.Bmp);
                        memory.Position = 0;

                        var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        imgPreview.Source = bitmapImage;
                        imgPreview.Visibility = Visibility.Visible;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing frame: {ex.Message}");
            }
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentFrame == null)
                {
                    MessageBox.Show("No frame available to capture.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show flash animation
                ShowCaptureFlash();

                // Generate filename with timestamp
                string filename = $"Photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string filepath = Path.Combine(_photoStoragePath, filename);

                // Save photo using GDI+ (Windows Graphics API)
                _currentFrame.Save(filepath, ImageFormat.Jpeg);

                _captureCount++;
                txtCaptureCount.Text = $"Photos Captured: {_captureCount}";

                UpdateStatus($"Photo saved: {filename}");

                // Log file size
                FileInfo fileInfo = new FileInfo(filepath);
                Debug.WriteLine($"Photo saved: {filename} ({FormatFileSize(fileInfo.Length)})");

                LogMemoryUsage("Photo Captured");

                // Show confirmation
                MessageBox.Show($"Photo saved successfully!\n\nLocation: {filepath}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing photo: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Minimize window before taking screenshot
                this.WindowState = WindowState.Minimized;

                // Wait for window to minimize
                System.Threading.Thread.Sleep(500);

                // Capture screenshot using Windows GDI API
                Bitmap screenshot = CaptureScreenUsingWindowsAPI();

                // Restore window
                this.WindowState = WindowState.Normal;

                if (screenshot != null)
                {
                    // Generate filename
                    string filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string filepath = Path.Combine(_photoStoragePath, filename);

                    // Save screenshot
                    screenshot.Save(filepath, ImageFormat.Png);
                    screenshot.Dispose();

                    _captureCount++;
                    txtCaptureCount.Text = $"Photos Captured: {_captureCount}";

                    UpdateStatus($"Screenshot saved: {filename}");

                    LogMemoryUsage("Screenshot Captured");

                    MessageBox.Show($"Screenshot saved successfully!\n\nLocation: {filepath}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error taking screenshot: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Captures the screen using Windows GDI API (BitBlt)
        /// </summary>
        private Bitmap CaptureScreenUsingWindowsAPI()
        {
            try
            {
                // Get screen dimensions using Windows API
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                // Get desktop window handle
                IntPtr desktopHandle = GetDesktopWindow();

                // Get device context for desktop
                IntPtr desktopDC = GetWindowDC(desktopHandle);

                // Create compatible DC and bitmap
                IntPtr memoryDC = CreateCompatibleDC(desktopDC);
                IntPtr bitmap = CreateCompatibleBitmap(desktopDC, screenWidth, screenHeight);

                // Select bitmap into DC
                IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

                // Copy screen to bitmap using BitBlt
                BitBlt(memoryDC, 0, 0, screenWidth, screenHeight, desktopDC, 0, 0, SRCCOPY);

                // Create managed Bitmap from handle
                Bitmap screenshot = System.Drawing.Image.FromHbitmap(bitmap);

                // Cleanup
                SelectObject(memoryDC, oldBitmap);
                DeleteObject(bitmap);
                DeleteDC(memoryDC);
                ReleaseDC(desktopHandle, desktopDC);

                return screenshot;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CaptureScreenUsingWindowsAPI: {ex.Message}");
                return null;
            }
        }

        private void BtnOpenGallery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var galleryWindow = new GalleryWindow();
                galleryWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening gallery: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowCaptureFlash()
        {
            flashOverlay.Visibility = Visibility.Visible;

            var animation = new DoubleAnimation
            {
                From = 0.8,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            animation.Completed += (s, e) =>
            {
                flashOverlay.Visibility = Visibility.Collapsed;
            };

            flashOverlay.BeginAnimation(OpacityProperty, animation);
        }

        private void LogMemoryUsage(string operation)
        {
            try
            {
                // Get memory information using Windows API
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    long usedMemory = (long)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
                    Debug.WriteLine($"[{operation}] Memory Used: {FormatFileSize(usedMemory)} / {FormatFileSize((long)memStatus.ullTotalPhys)} ({memStatus.dwMemoryLoad}%)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting memory status: {ex.Message}");
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
            Debug.WriteLine($"[Camera] {message}");
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
            _currentFrame?.Dispose();
            base.OnClosing(e);
        }
    }
}