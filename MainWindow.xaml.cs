
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace KonataClock
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private DispatcherTimer _timer2;
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppKeyPath = @"SOFTWARE\TimerClockKonata";
        private const string AppName = "TimerKonata";
        private bool switchh = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closing += OnClosing;
            SendToBottom(new WindowInteropHelper(this).Handle);
            double step = 0.1;
            _timer2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.05) };
            _timer2.Tick += (_, __) => { ClockText_hours.Text = DateTime.Now.ToString("HH"); ClockText_minutes.Text = DateTime.Now.ToString("mm"); if (seperator.Opacity < 0.1) { switchh = true; } else { if (seperator.Opacity > 0.9) { switchh = false; } } if (switchh == false) { seperator.Opacity = seperator.Opacity - step; } else { if (switchh == true) { seperator.Opacity = seperator.Opacity + step; } } };
            _timer2.Start();

            SetLaunchAtStartup(true);
            var imageData = Resource1.img;
            using (var bmp = new Bitmap(imageData))
            using (var mem2 = new MemoryStream())
            {
                bmp.Save(mem2, ImageFormat.Png);
                mem2.Position = 0;
                var bmpImg = new BitmapImage();
                bmpImg.BeginInit();
                bmpImg.CacheOption = BitmapCacheOption.OnLoad;
                bmpImg.StreamSource = mem2;
                bmpImg.EndInit();
                MyImageControl.Source = bmpImg;
            }


            SourceInitialized += MainWindow_SourceInitialized;
        }


        public static void SetLaunchAtStartup(bool enable)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
            {
                if (enable)
                {
                    var execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{execPath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
        }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AppKeyPath, true) ??
                              Registry.CurrentUser.CreateSubKey(AppKeyPath))
            {
                if (double.TryParse(key.GetValue("Left")?.ToString(), out var left) &&
                    double.TryParse(key.GetValue("Top")?.ToString(), out var top))
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = left;
                    Top = top;
                }
            }

            using (var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                runKey.SetValue(AppName,
                    $"\"{System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe")}\"");
        }
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_SHOWWINDOW = 0x0040;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        public static void SendToBottom(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AppKeyPath, true) ??
                              Registry.CurrentUser.CreateSubKey(AppKeyPath))
            {
                key.SetValue("Left", Left);
                key.SetValue("Top", Top);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AppKeyPath, true) ??
                              Registry.CurrentUser.CreateSubKey(AppKeyPath))
            {
                key.SetValue("Left", Left);
                key.SetValue("Top", Top);
            }
            DragMove();
            SendToBottom(new WindowInteropHelper(this).Handle);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle).AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var hwndMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hwndMonitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(hwndMonitor, ref monitorInfo);

                var rcWorkArea = monitorInfo.rcWork;
                var rcMonitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = (int)Width;
                mmi.ptMaxSize.y = (int)Height;
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        //charlie kirk
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;
    }
}
