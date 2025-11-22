using System;
using System.Management;
using System.Windows;

namespace XqueezeOS
{
    public partial class AboutDeviceWindow : Window
    {
        public AboutDeviceWindow()
        {
            InitializeComponent();
            LoadDeviceInfo();
        }

        private void LoadDeviceInfo()
        {
            // Display core OS and system device details
            OSVersionText.Text = $"OS Version: {Environment.OSVersion}";
            DeviceNameText.Text = $"Device Name: {Environment.MachineName}";
            UserCountText.Text = $"Active User: {Environment.UserName}";

            MemoryText.Text = $"Memory: {GetTotalMemory()} MB";
            StorageText.Text = $"Storage: {GetStorageInfo()}";
        }

        private string GetTotalMemory()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return (Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024).ToString("F2");
                }
            }
            catch
            {
                return "Unknown";
            }
            return "Unknown";
        }

        private string GetStorageInfo()
        {
            try
            {
                var drive = System.IO.DriveInfo.GetDrives()[0];
                return $"{drive.TotalSize / (1024 * 1024 * 1024)} GB";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
