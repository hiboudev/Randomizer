using Randomizer.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using WindowPlacementExample;

namespace Randomizer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Random rand;
        private readonly Color[] colors;
        private int colorIndex;

        public MainWindow()
        {
            InitializeComponent();

            colorIndex = -1;
            rand = new Random();
            colors = new Color[5] {
                Color.FromRgb(90, 139, 238),
                Color.FromRgb(185, 96, 240),
                Color.FromRgb(105, 190, 80),
                Color.FromRgb(204, 172, 90),
                Color.FromRgb(222, 89, 95),
            };

            textField.Text = "";
            MouseEnter += DrawNumber;
            MouseDown += HandleMouseDown;
            KeyDown += HandleKeyDown;
            menuItemKeepOnTop.Checked += HandleMenuItemKeepOnTopChanged;
            menuItemKeepOnTop.Unchecked += HandleMenuItemKeepOnTopChanged;
            menuItemShowInTaskbar.Checked += HandleMenuItemShowInTaskbarChanged;
            menuItemShowInTaskbar.Unchecked += HandleMenuItemShowInTaskbarChanged;
            Closing += HandleWindowClosing;
            SourceInitialized += HandleSourceInitialized;
        }

        private void HandleSourceInitialized(object sender, EventArgs e)
        {
            this.SetPlacement(Settings.Default.MainWindowPlacement);
            Topmost = menuItemKeepOnTop.IsChecked = Settings.Default.KeepOnTop;
            ShowInTaskbar = menuItemShowInTaskbar.IsChecked = Settings.Default.ShowInTaskbar;
        }

        private void HandleWindowClosing(object sender, EventArgs e)
        {
            Settings.Default.MainWindowPlacement = this.GetPlacement();
            Settings.Default.KeepOnTop = menuItemKeepOnTop.IsChecked;
            Settings.Default.ShowInTaskbar = ShowInTaskbar;
            Settings.Default.Save();
        }

        private void HandleMenuItemKeepOnTopChanged(object sender, RoutedEventArgs e)
        {
            Topmost = menuItemKeepOnTop.IsChecked == true;
        }

        private void HandleMenuItemShowInTaskbarChanged(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = menuItemShowInTaskbar.IsChecked == true;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                System.Windows.Application.Current.Shutdown();
        }

        private void HandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void DrawNumber(object sender, MouseEventArgs e)
        {
            textField.Foreground = NextColor();
            textField.Text = rand.Next(1, 101).ToString();
        }

        private Brush NextColor()
        {
            if (colorIndex == colors.Length - 1)
                colorIndex = 0;
            else colorIndex++;

            return new SolidColorBrush(colors[colorIndex]);
        }
    }
}

namespace WindowPlacementExample
{
    // RECT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }
    }

    // POINT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    // WINDOWPLACEMENT stores the position, size, and state of a window
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;
    }

    public static class WindowPlacement
    {
        private static Encoding encoding = new UTF8Encoding();
        private static XmlSerializer serializer = new XmlSerializer(typeof(WINDOWPLACEMENT));

        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;

        public static void SetPlacement(IntPtr windowHandle, string placementXml)
        {
            if (string.IsNullOrEmpty(placementXml))
            {
                return;
            }

            WINDOWPLACEMENT placement;
            byte[] xmlBytes = encoding.GetBytes(placementXml);

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                {
                    placement = (WINDOWPLACEMENT)serializer.Deserialize(memoryStream);
                }

                placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                placement.flags = 0;
                placement.showCmd = (placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd);

                SetWindowPlacement(windowHandle, ref placement);
            }
            catch (InvalidOperationException)
            {
                // Parsing placement XML failed. Fail silently.
            }
        }

        public static string GetPlacement(IntPtr windowHandle)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(windowHandle, out placement);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
                {
                    serializer.Serialize(xmlTextWriter, placement);
                    byte[] xmlBytes = memoryStream.ToArray();
                    return encoding.GetString(xmlBytes);
                }
            }
        }

        public static void SetPlacement(this Window window, string placementXml)
        {
            SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
        }

        public static string GetPlacement(this Window window)
        {
            return GetPlacement(new WindowInteropHelper(window).Handle);
        }
    }
}