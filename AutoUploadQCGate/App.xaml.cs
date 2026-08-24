using DefaultNS.Common;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace AutoUploadQCGate
{
    public partial class App : System.Windows.Application
    {
        private static Mutex _mutex;
        private static bool _ownsMutex;
        private NotifyIcon _notifyIcon;
        private bool _isExit = false;
        private AppSettings _appSetting = new AppSettings();
        private void LoadSetting()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDirectory = System.IO.Path.GetDirectoryName(exePath);
                string settingsFile = System.IO.Path.Combine(exeDirectory, "settings.xml");

                if (File.Exists(settingsFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (TextReader reader = new StreamReader(settingsFile))
                    {
                        _appSetting = (AppSettings)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Global.WriteLog($"Settings load failed: {ex}");
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            LoadSetting();
            const string appName = "AutoUploadQCGate_SingleInstance";
            bool createdNew;
            _mutex = new Mutex(true, appName, out createdNew);
            _ownsMutex = createdNew;

            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Ứng dụng đã được mở, không thể mở thêm lần nữa!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                Current.Shutdown();
                return;
            }

            // Khởi tạo NotifyIcon
            System.Drawing.Icon appIcon;
            try
            {
                var streamInfo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/_icon.ico"));
                if (streamInfo != null)
                {
                    appIcon = new System.Drawing.Icon(streamInfo.Stream);
                }
                else if (System.IO.File.Exists("_icon.ico"))
                {
                    appIcon = new System.Drawing.Icon("_icon.ico");
                }
                else
                {
                    appIcon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            }
            catch
            {
                appIcon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Visible = true,
                Text = "AutoUploadQCGate"
            };

            var contextMenu = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem("Show");
            showItem.Click += (s, ev) => ShowMainWindow();
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, ev) => ExitApplication();
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, ev) => ShowMainWindow();

            
            Current.Activated += (s, ev) =>
            {
                if (Current.MainWindow != null && !_eventAttached)
                {
                    Current.MainWindow.Closing += MainWindow_Closing;
                    _eventAttached = true;
                }
            };
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Global.WriteLog($"Unhandled UI exception: {e.Exception}");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Global.WriteLog($"Unhandled application exception: {e.ExceptionObject}");
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Global.WriteLog($"Unobserved task exception: {e.Exception}");
        }
        private bool _eventAttached = false;


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true; // hủy đóng window
                var window = sender as Window;
                window?.Hide(); // ẩn window về tray
            }
        }

        private void ShowMainWindow()
        {
            if (Current.MainWindow == null)
                return;

            Current.MainWindow.Show();
            Current.MainWindow.WindowState = WindowState.Normal;
            Current.MainWindow.Activate();
        }

        private void ExitApplication()
        {
            LoadSetting();
            string appPassword = _appSetting.ApplicationPassword;

            var passwordDialog = new PasswordInputWindow(appPassword)
            {
                Owner = Current.MainWindow
            };

            bool? result = passwordDialog.ShowDialog();

            if (result == true && passwordDialog.IsAuthenticated)
            {
                _isExit = true;
                Current.Shutdown();
            }
            else
            {
                System.Windows.MessageBox.Show("Access denied!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
           
        }

        public void MinimizeToTray(Window window)
        {
            window.WindowState = WindowState.Minimized;
            window.Hide();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            if (_ownsMutex)
            {
                _mutex?.ReleaseMutex();
                _ownsMutex = false;
            }
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
