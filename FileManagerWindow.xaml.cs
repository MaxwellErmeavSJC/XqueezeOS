using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace XqueezeOS
{
    /// <summary>
    /// File Manager Window - Manages file operations using Windows APIs
    /// </summary>
    public partial class FileManagerWindow : Window
    {
        #region Windows API Declarations

        // Windows API for getting file information
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetFileInformationByHandle(
            IntPtr hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        // Windows API for getting disk space
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetDiskFreeSpaceEx(
            string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        // Windows API for file operations
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CopyFile(
            string lpExistingFileName,
            string lpNewFileName,
            bool bFailIfExists);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool MoveFile(
            string lpExistingFileName,
            string lpNewFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeleteFile(string lpFileName);

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        // Constants
        const uint GENERIC_READ = 0x80000000;
        const uint OPEN_EXISTING = 3;
        const uint FILE_SHARE_READ = 0x00000001;
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        #endregion

        private ObservableCollection<FileItem> _files;
        private string _storageDirectory;
        private long _totalStorageUsed;

        public FileManagerWindow()
        {
            InitializeComponent();
            InitializeFileManager();
        }

        private void InitializeFileManager()
        {
            // Create XqueezeOS storage directory
            _storageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XqueezeOS",
                "FileStorage"
            );

            // Use Windows API to create directory
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
                UpdateStatus($"Created storage directory using System.IO API");
            }

            _files = new ObservableCollection<FileItem>();
            lstFiles.ItemsSource = _files;

            LoadFiles();
        }

        private void LoadFiles()
        {
            try
            {
                _files.Clear();
                _totalStorageUsed = 0;

                if (!Directory.Exists(_storageDirectory))
                {
                    UpdateStatus("Storage directory not found");
                    return;
                }

                // Get all files using Directory.GetFiles (which internally uses Windows FindFirstFile/FindNextFile APIs)
                string[] files = Directory.GetFiles(_storageDirectory);

                foreach (string filePath in files)
                {
                    try
                    {
                        // Use Windows API to get file information
                        FileInfo fileInfo = new FileInfo(filePath);
                        BY_HANDLE_FILE_INFORMATION fileHandleInfo = GetFileHandleInformation(filePath);

                        long fileSize = fileInfo.Length;
                        _totalStorageUsed += fileSize;

                        string extension = fileInfo.Extension.ToLower();
                        string icon = GetFileIcon(extension);

                        _files.Add(new FileItem
                        {
                            FileName = fileInfo.Name,
                            FilePath = filePath,
                            FileSize = fileSize,
                            FileInfo = $"{FormatFileSize(fileSize)} • {fileInfo.LastWriteTime:MM/dd/yyyy}",
                            Icon = icon,
                            Extension = extension,
                            CreatedTime = fileInfo.CreationTime,
                            ModifiedTime = fileInfo.LastWriteTime
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading file {filePath}: {ex.Message}");
                    }
                }

                UpdateFileCounters();
                UpdateStatus($"Loaded {_files.Count} files from storage");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets file information using Windows API CreateFile and GetFileInformationByHandle
        /// </summary>
        private BY_HANDLE_FILE_INFORMATION GetFileHandleInformation(string filePath)
        {
            IntPtr fileHandle = CreateFile(
                filePath,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero
            );

            BY_HANDLE_FILE_INFORMATION fileInfo = new BY_HANDLE_FILE_INFORMATION();

            if (fileHandle != INVALID_HANDLE_VALUE)
            {
                GetFileInformationByHandle(fileHandle, out fileInfo);
                CloseHandle(fileHandle);
            }

            return fileInfo;
        }

        private string GetFileIcon(string extension)
        {
            return extension switch
            {
                ".txt" => "📄",
                ".pdf" => "📕",
                ".doc" or ".docx" => "📘",
                ".xls" or ".xlsx" => "📗",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".mp3" or ".wav" or ".wma" => "🎵",
                ".mp4" or ".avi" or ".mkv" => "🎬",
                ".zip" or ".rar" or ".7z" => "📦",
                ".exe" => "⚙️",
                ".html" or ".htm" => "🌐",
                ".json" => "📋",
                ".xml" => "📋",
                ".cs" => "💻",
                _ => "📄"
            };
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

        private void BtnNewFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateFileDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = dialog.FileName;
                    string content = dialog.FileContent;
                    string extension = dialog.FileExtension;

                    string filePath = Path.Combine(_storageDirectory, $"{fileName}{extension}");

                    // Check if file exists
                    if (File.Exists(filePath))
                    {
                        var result = MessageBox.Show(
                            "File already exists. Overwrite?",
                            "Confirm",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    // Write file using System.IO (which uses Windows WriteFile API internally)
                    File.WriteAllText(filePath, content);

                    UpdateStatus($"Created file: {fileName}{extension}");
                    LoadFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem is FileItem selectedFile)
            {
                try
                {
                    // Use Windows Shell API to open file with default application
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = selectedFile.FilePath,
                        UseShellExecute = true
                    });

                    UpdateStatus($"Opened: {selectedFile.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a file to open.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnRenameFile_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem is FileItem selectedFile)
            {
                var dialog = new RenameFileDialog(selectedFile.FileName);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        string newFileName = dialog.NewFileName;
                        string newPath = Path.Combine(_storageDirectory, newFileName);

                        if (File.Exists(newPath))
                        {
                            MessageBox.Show("A file with this name already exists.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Use Windows API MoveFile to rename
                        bool success = MoveFile(selectedFile.FilePath, newPath);

                        if (success)
                        {
                            UpdateStatus($"Renamed: {selectedFile.FileName} → {newFileName}");
                            LoadFiles();
                        }
                        else
                        {
                            // Fallback to .NET method if API fails
                            File.Move(selectedFile.FilePath, newPath);
                            UpdateStatus($"Renamed (using .NET API): {selectedFile.FileName} → {newFileName}");
                            LoadFiles();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming file: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem is FileItem selectedFile)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{selectedFile.FileName}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Use Windows API DeleteFile
                        bool success = DeleteFile(selectedFile.FilePath);

                        if (success)
                        {
                            UpdateStatus($"Deleted: {selectedFile.FileName}");
                            LoadFiles();
                        }
                        else
                        {
                            // Fallback to .NET method
                            File.Delete(selectedFile.FilePath);
                            UpdateStatus($"Deleted (using .NET API): {selectedFile.FileName}");
                            LoadFiles();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFiles();
        }

        private void BtnShowInfo_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem is FileItem selectedFile)
            {
                try
                {
                    // Get disk space information using Windows API
                    ulong freeBytesAvailable, totalBytes, totalFreeBytes;
                    bool diskSpaceSuccess = GetDiskFreeSpaceEx(
                        _storageDirectory,
                        out freeBytesAvailable,
                        out totalBytes,
                        out totalFreeBytes
                    );

                    string diskInfo = diskSpaceSuccess
                        ? $"\n\nDisk Space:\nFree: {FormatFileSize((long)freeBytesAvailable)}\nTotal: {FormatFileSize((long)totalBytes)}"
                        : "";

                    MessageBox.Show(
                        $"File: {selectedFile.FileName}\n" +
                        $"Size: {FormatFileSize(selectedFile.FileSize)}\n" +
                        $"Type: {selectedFile.Extension}\n" +
                        $"Created: {selectedFile.CreatedTime}\n" +
                        $"Modified: {selectedFile.ModifiedTime}\n" +
                        $"Path: {selectedFile.FilePath}" +
                        diskInfo,
                        "File Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error getting file info: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = lstFiles.SelectedItem != null;

            btnRenameFile.IsEnabled = hasSelection;
            btnDeleteFile.IsEnabled = hasSelection;
            btnShowInfo.IsEnabled = hasSelection;

            if (lstFiles.SelectedItem is FileItem selectedFile)
            {
                // Update details panel
                txtFileName.Text = selectedFile.FileName;
                txtFileSize.Text = FormatFileSize(selectedFile.FileSize);
                txtCreated.Text = selectedFile.CreatedTime.ToString("F");
                txtModified.Text = selectedFile.ModifiedTime.ToString("F");
                txtFileType.Text = $"{selectedFile.Extension.TrimStart('.').ToUpper()} File";
                txtFullPath.Text = selectedFile.FilePath;
                pnlFileDetails.Visibility = Visibility.Visible;
            }
            else
            {
                txtFileName.Text = "No file selected";
                pnlFileDetails.Visibility = Visibility.Collapsed;
            }
        }

        private void LstFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnOpenFile_Click(sender, e);
        }

        private void UpdateFileCounters()
        {
            txtFileCount.Text = _files.Count.ToString();
            txtStorageUsed.Text = FormatFileSize(_totalStorageUsed);
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
            Debug.WriteLine($"[FileManager] {message}");
        }
    }

    #region Helper Classes

    /// <summary>
    /// Represents a file item in the file manager
    /// </summary>
    public class FileItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileInfo { get; set; }
        public string Icon { get; set; }
        public string Extension { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    #endregion
}
