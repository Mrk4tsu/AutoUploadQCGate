using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Input;

namespace AutoUploadQCGate
{
    public partial class SettingsWindow : Window
    {
        private bool isPasswordVisible = false;
        private bool isConfirmPasswordVisible = false;
        private TextBox passwordTextBox;
        private TextBox confirmPasswordTextBox;

        public AppSettings Settings { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            Settings = LoadSettings();
            InitializeControls();
            UpdateStartupStatus();
        }

        private void InitializeControls()
        {
            PasswordValidationText.Text = Settings.ApplicationPassword;
            CombineLogPathTextBox.Text = Settings.CombineLogPath;
            ScanIntervalTextBox.Text = Settings.ScanInterval.ToString();
            RunWithWindowsCheckBox.IsChecked = Settings.RunWithWindows;
            AutoStartScanCheckBox.IsChecked = Settings.AutoStartScan;
            MinimizeToTrayCheckBox.IsChecked = Settings.MinimizeToTray;
            CheckUseProxyCheckBox.IsChecked = Settings.CheckUseProxy;
            DbPasswordBox.Password = Settings.DbPassword;
            ServerTextBox.Text = Settings.DbServer;
            PortTextBox.Text = Settings.DbPort.ToString();
            DatabaseNameTextBox.Text = Settings.DbName;
            UsernameTextBox.Text = Settings.DbUsername;
            CombineLogPathServerTextBox.Text = Settings.CombineLogPathServer;
            PrimaryKeyFilePathTextBox.Text = Settings.PrimaryKeyFilePath;
            ProxyHost.Text = Settings.ProxyHost;
            ProxyPort.Text = Settings.ProxyPort;

        }

        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
          //  TogglePasswordVisibility(PasswordBox, passwordTextBox, ref isPasswordVisible, sender);
        }

     

        private void TogglePasswordVisibility(PasswordBox passwordBox, TextBox textBox, ref bool isVisible, object sender)
        {
            if (!isVisible)
            {
                // Show password
                textBox.Text = passwordBox.Password;
                textBox.Visibility = Visibility.Visible;
                passwordBox.Visibility = Visibility.Collapsed;
                ((Button)sender).Content = "Hide";
                isVisible = true;
            }
            else
            {
                // Hide password
                passwordBox.Password = textBox.Text;
                passwordBox.Visibility = Visibility.Visible;
                textBox.Visibility = Visibility.Collapsed;
                ((Button)sender).Content = "Show";
                isVisible = false;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            //if (!ValidateSettings())
            //    return;

            // Update settings object
            Settings.ApplicationPassword = PasswordValidationText.Text;
            Settings.ScanInterval = int.Parse(ScanIntervalTextBox.Text);
            Settings.RunWithWindows = RunWithWindowsCheckBox.IsChecked ?? false;
            Settings.AutoStartScan = AutoStartScanCheckBox.IsChecked ?? false;
            Settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
            Settings.CheckUseProxy = CheckUseProxyCheckBox.IsChecked ?? false;
            Settings.CombineLogPath = CombineLogPathTextBox.Text;
            Settings.PrimaryKeyFilePath = PrimaryKeyFilePathTextBox.Text;
            Settings.CombineLogPathServer = CombineLogPathServerTextBox.Text;
            Settings.ProxyHost = ProxyHost.Text;
            Settings.ProxyPort = ProxyPort.Text;

            Settings.DbServer = ServerTextBox.Text;
            Settings.DbPort = int.Parse(PortTextBox.Text);
            Settings.DbUsername = UsernameTextBox.Text;
            Settings.DbPassword =  DbPasswordBox.Password;
            Settings.DbName = DatabaseNameTextBox.Text;


            // Save settings
            SaveSettings(Settings);

            // Apply startup settings
            SetRunWithWindows(Settings.RunWithWindows);

            MessageBox.Show("Settings saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void BrowseCombineLogPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CombineLogPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }
        //private bool ValidateSettings()
        //{
        //    string password = isPasswordVisible ? passwordTextBox.Text : PasswordBox.Password;

        //    if (string.IsNullOrWhiteSpace(password))
        //    {
        //        PasswordValidationText.Text = "Password cannot be empty!";
        //        PasswordValidationText.Visibility = Visibility.Visible;
        //        return false;
        //    }

        //    if (!int.TryParse(ScanIntervalTextBox.Text, out int interval) || interval <= 0)
        //    {
        //        MessageBox.Show("Please enter a valid scan interval (positive number).",
        //            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return false;
        //    }

        //    PasswordValidationText.Visibility = Visibility.Collapsed;
        //    return true;
        //}

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Only allow numeric input
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    break;
                }
            }
        }

        #region Settings Management

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                // Lấy đường dẫn thư mục chứa file exe
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDirectory = Path.GetDirectoryName(exePath);
                string settingsFile = Path.Combine(exeDirectory, "settings.xml");

                // Serialize settings to XML
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (TextWriter writer = new StreamWriter(settingsFile))
                {
                    serializer.Serialize(writer, settings);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public AppSettings LoadSettings()
        {
            try
            {
                // Lấy đường dẫn thư mục chứa file exe
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDirectory = Path.GetDirectoryName(exePath);
                string settingsFile = Path.Combine(exeDirectory, "settings.xml");

                if (File.Exists(settingsFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (TextReader reader = new StreamReader(settingsFile))
                    {
                        return (AppSettings)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Return default settings if file doesn't exist or error occurs
            return new AppSettings();
        }

        #endregion

        #region Windows Startup Management

        public void SetRunWithWindows(bool enable)
        {
            try
            {
                const string appName = "AutoUploadQCGate";
                string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (enable)
                    {
                        key.SetValue(appName, executablePath);
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting startup option: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool IsRunWithWindowsEnabled()
        {
            try
            {
                const string appName = "AutoUploadQCGate";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    return key?.GetValue(appName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateStartupStatus()
        {
            bool isEnabled = IsRunWithWindowsEnabled();
            StartupStatusText.Text = isEnabled ?
                "✓ Currently set to run with Windows startup" :
                "✗ Not set to run with Windows startup";
        }

        #endregion

        private void BrowseCombineLogServerPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CombineLogPathServerTextBox.Text = dialog.SelectedPath;
                }
            }
        }
        private void BrowsePrimaryKeyFilePathPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Title = "Select Private Key File";
            dialog.Filter = "Key files (*.key;*.pem;*.ppk)|*.key;*.pem;*.ppk|All files (*.*)|*.*";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == true)
            {
                PrimaryKeyFilePathTextBox.Text = dialog.FileName;
            }
        }

    }

    [Serializable]
    public class AppSettings
    {
        public string ApplicationPassword { get; set; } = "default123";
        public int ScanInterval { get; set; } = 30;
        public bool RunWithWindows { get; set; } = false;
        public bool AutoStartScan { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool CheckUseProxy { get; set; } = true;
        public string DbServer { get; set; } = "localhost";
        public int DbPort { get; set; } = 1433;
        public string DbUsername { get; set; } = "sa";
        public string DbPassword { get; set; } = "";
        public string DbName { get; set; } = "QCGateDB";
        public string CombineLogPath { get; set; } =
            @"C:\Users\Administrator\Documents\ProjectData\QCGateEmap\Archive";
        public string CombineLogPathServer { get; set; } =
            @"C:\Users\Administrator\Documents\ProjectData\QCGateEmap\Destination";
        public string PrimaryKeyFilePath { get; set; } = "aaaa";
        public string ProxyHost { get; set; } = "aaaa";
        public string ProxyPort { get; set; } = "aaaa";

        public AppSettings() { }
    }
}
