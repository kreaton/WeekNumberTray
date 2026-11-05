using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace WeekNumberTray;

/// <summary>
/// The popup calendar with ISO 8601 week numbers. It opens next to the mouse pointer and closes
/// like a menu when it loses focus.
/// </summary>
public partial class CalendarWindow : Window
{
    // When the popup is open and you click the tray icon, the popup first loses focus and hides
    // itself, and only then does the click arrive. A click this soon after hiding means "close".
    private const int ReopenDelayMs = 300;

    private static readonly string[] DayNames = ["Wk", "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
    private const int WeekRows = 6;

    private DateTime _shownMonth = FirstOfMonth(DateTime.Today);
    private long _lastHideTicks = -ReopenDelayMs;

    public event EventHandler? CheckForUpdatesRequested;
    public event EventHandler? AboutRequested;
    public event EventHandler? ExitRequested;

    public CalendarWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => RoundCorners();
    }

    /// <summary>Opens the calendar on the current month, or closes it if it's already open.</summary>
    public void Toggle()
    {
        if (IsVisible)
        {
            HideCalendar();
            return;
        }
        if (Environment.TickCount64 - _lastHideTicks < ReopenDelayMs)
            return;

        _shownMonth = FirstOfMonth(DateTime.Today);
        Rebuild();
        AutoStartCheck.Visibility = AutoStartManager.IsEnabled() ? Visibility.Visible : Visibility.Hidden;

        PlaceNearPointer();
        Show();
        Activate();
    }

    /// <summary>Redraws an open calendar after the date has changed.</summary>
    public void RefreshToday()
    {
        if (IsVisible)
            Rebuild();
    }

    private void HideCalendar()
    {
        _lastHideTicks = Environment.TickCount64;
        Hide();
    }

    // ---------- Drawing ----------

    private void Rebuild()
    {
        DateTime today = DateTime.Today;
        MonthTitle.Text = _shownMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        TodayText.Text = $"Today: {today.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture)} · Week {ISOWeek.GetWeekOfYear(today)}";

        MonthGrid.Children.Clear();

        // Backgrounds first, so the text ends up on top
        var weekColumn = new Border { CornerRadius = new CornerRadius(4) };
        weekColumn.SetResourceReference(Border.BackgroundProperty, "SubtleFillColorSecondaryBrush");
        Grid.SetRowSpan(weekColumn, WeekRows + 1);
        MonthGrid.Children.Add(weekColumn);

        var underNames = new Border { Height = 1, VerticalAlignment = VerticalAlignment.Bottom };
        underNames.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
        Grid.SetColumnSpan(underNames, DayNames.Length);
        MonthGrid.Children.Add(underNames);

        for (int col = 0; col < DayNames.Length; col++)
        {
            string brush = col switch
            {
                0 => "AccentTextFillColorPrimaryBrush",
                6 or 7 => "TextFillColorTertiaryBrush",
                _ => "TextFillColorSecondaryBrush"
            };
            AddText(DayNames[col], 0, col, brush, fontSize: 12);
        }

        // The grid starts on the Monday on or before the 1st of the month
        int daysSinceMonday = ((int)_shownMonth.DayOfWeek + 6) % 7;
        DateTime monday = _shownMonth.AddDays(-daysSinceMonday);

        for (int week = 0; week < WeekRows; week++, monday = monday.AddDays(7))
        {
            int row = week + 1;
            AddText(ISOWeek.GetWeekOfYear(monday).ToString(CultureInfo.InvariantCulture), row, 0,
                    "AccentTextFillColorPrimaryBrush", FontWeights.SemiBold);

            bool isCurrentWeek = today >= monday && today < monday.AddDays(7);
            if (isCurrentWeek)
                AddCurrentWeekHighlight(row);

            for (int d = 0; d < 7; d++)
            {
                DateTime day = monday.AddDays(d);
                string label = day.Day.ToString(CultureInfo.InvariantCulture);
                if (day == today)
                {
                    AddTodayCircle(row, d + 1);
                    AddText(label, row, d + 1, "TextOnAccentFillColorPrimaryBrush", FontWeights.SemiBold);
                }
                else
                {
                    string brush = day.Month != _shownMonth.Month ? "TextFillColorDisabledBrush"
                                 : d >= 5 ? "TextFillColorSecondaryBrush"
                                 : "TextFillColorPrimaryBrush";
                    AddText(label, row, d + 1, brush);
                }
            }
        }
    }

    private void AddText(string text, int row, int col, string brush, FontWeight? weight = null, double fontSize = 14)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        Grid.SetRow(block, row);
        Grid.SetColumn(block, col);
        MonthGrid.Children.Add(block);
    }

    private void AddCurrentWeekHighlight(int row)
    {
        var highlight = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 1, 0, 1), Opacity = 0.18 };
        highlight.SetResourceReference(Border.BackgroundProperty, "AccentFillColorDefaultBrush");
        Grid.SetRow(highlight, row);
        Grid.SetColumn(highlight, 1);
        Grid.SetColumnSpan(highlight, 7);
        MonthGrid.Children.Add(highlight);
    }

    private void AddTodayCircle(int row, int col)
    {
        var circle = new Ellipse { Width = 26, Height = 26 };
        circle.SetResourceReference(Shape.FillProperty, "AccentFillColorDefaultBrush");
        Grid.SetRow(circle, row);
        Grid.SetColumn(circle, col);
        MonthGrid.Children.Add(circle);
    }

    private static DateTime FirstOfMonth(DateTime d) => new(d.Year, d.Month, 1);

    // ---------- Input ----------

    private void ShiftMonth(int months)
    {
        _shownMonth = _shownMonth.AddMonths(months);
        Rebuild();
    }

    private void GoToToday()
    {
        _shownMonth = FirstOfMonth(DateTime.Today);
        Rebuild();
    }

    private void OnPreviousMonth(object sender, RoutedEventArgs e) => ShiftMonth(-1);
    private void OnNextMonth(object sender, RoutedEventArgs e) => ShiftMonth(1);
    private void OnToday(object sender, RoutedEventArgs e) => GoToToday();
    private void OnMouseWheel(object sender, MouseWheelEventArgs e) => ShiftMonth(e.Delta > 0 ? -1 : 1);

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                HideCalendar();
                break;
            case Key.Left or Key.Up or Key.PageUp:
                ShiftMonth(-1);
                break;
            case Key.Right or Key.Down or Key.PageDown:
                ShiftMonth(1);
                break;
            case Key.Home:
                GoToToday();
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (IsVisible)
            HideCalendar();
    }

    private void OnToggleAutoStart(object sender, RoutedEventArgs e)
    {
        bool enable = !AutoStartManager.IsEnabled();
        AutoStartManager.SetEnabled(enable);
        AutoStartCheck.Visibility = AutoStartManager.IsEnabled() ? Visibility.Visible : Visibility.Hidden;
    }

    private void OnCheckForUpdates(object sender, RoutedEventArgs e)
    {
        HideCalendar();
        CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        HideCalendar();
        AboutRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        HideCalendar();
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    // ---------- Window placement ----------

    /// <summary>
    /// Puts the hidden window centred above the pointer, kept inside the usable area of the
    /// pointer's monitor. This works in physical pixels through Win32, because WPF's Left and Top
    /// can't express a position on a monitor with different scaling from the window's current one.
    /// </summary>
    private void PlaceNearPointer()
    {
        IntPtr hwnd = new WindowInteropHelper(this).EnsureHandle();

        GetCursorPos(out POINT pointer);
        IntPtr monitor = MonitorFromPoint(pointer, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);
        GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpi, out _);
        double scale = dpi / 96.0;

        Root.Measure(new Size(Width, double.PositiveInfinity));
        Height = Root.DesiredSize.Height;

        int width = (int)Math.Ceiling(Width * scale);
        int height = (int)Math.Ceiling(Height * scale);
        int gap = (int)(8 * scale);
        RECT area = info.rcWork;
        int x = Math.Max(Math.Min(pointer.X - width / 2, area.Right - width - gap), area.Left + gap);
        int y = Math.Max(Math.Min(pointer.Y - height - gap, area.Bottom - height - gap), area.Top + gap);

        SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>Asks Windows 11 to round the corners. Windows 10 ignores this.</summary>
    private void RoundCorners()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        int preference = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int MDT_EFFECTIVE_DPI = 0;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
