using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace WeekNumberTray;

/// <summary>
/// Owns the tray icon and the calendar: keeps the icon on the current week and opens the
/// calendar when the icon is clicked.
/// </summary>
internal sealed class TrayController : IDisposable
{
    // How often to check whether the date has changed. Waking from sleep and changing the clock
    // are also picked up straight away through SystemEvents.
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly Forms.NotifyIcon _trayIcon;
    private readonly DispatcherTimer _timer;
    private readonly CalendarWindow _calendar = new();
    private readonly UpdateChecker _updates = new();
    private AboutWindow? _about;
    private DateTime _shownDay;

    public TrayController()
    {
        _trayIcon = new Forms.NotifyIcon();
        _trayIcon.MouseUp += OnTrayMouseUp;
        UpdateTrayIcon();
        _trayIcon.Visible = true;

        _calendar.CheckForUpdatesRequested += OnCheckForUpdates;
        _calendar.AboutRequested += OnAbout;
        _calendar.ExitRequested += (_, _) => Application.Current.Shutdown();

        _timer = new DispatcherTimer { Interval = CheckInterval };
        _timer.Tick += (_, _) => RefreshIfDayChanged();
        _timer.Start();

        SystemEvents.TimeChanged += OnTimeChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnTrayMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button is Forms.MouseButtons.Left or Forms.MouseButtons.Right)
            _calendar.Toggle();
    }

    private void RefreshIfDayChanged()
    {
        if (DateTime.Today == _shownDay)
            return;
        UpdateTrayIcon();
        _calendar.RefreshToday();
    }

    private void UpdateTrayIcon()
    {
        DateTime today = DateTime.Today;
        int week = ISOWeek.GetWeekOfYear(today);

        // NotifyIcon keeps using the handle, so the old icon can only be freed after the swap
        var old = _trayIcon.Icon;
        _trayIcon.Icon = WeekIcon.Create(week);
        WeekIcon.Destroy(old);

        _trayIcon.Text = $"Week {week} ({ISOWeek.GetYear(today)})\n"
                       + today.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);
        _shownDay = today;
    }

    // SystemEvents may fire on its own thread
    private void OnTimeChanged(object? sender, EventArgs e) =>
        _timer.Dispatcher.BeginInvoke(RefreshIfDayChanged);

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            _timer.Dispatcher.BeginInvoke(RefreshIfDayChanged);
    }

    // The tray icon's size in pixels may differ now, so redraw it
    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        _timer.Dispatcher.BeginInvoke(UpdateTrayIcon);

    private async void OnCheckForUpdates(object? sender, EventArgs e)
    {
        await _updates.CheckAsync(beforeRestart: () => _trayIcon.Visible = false,
                                  afterFailedRestart: () => _trayIcon.Visible = true);
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        if (_about == null)
        {
            _about = new AboutWindow();
            _about.Closed += (_, _) => _about = null;
            _about.Show();
        }
        _about.Activate();
    }

    public void Dispose()
    {
        SystemEvents.TimeChanged -= OnTimeChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _timer.Stop();
        _trayIcon.Visible = false;
        WeekIcon.Destroy(_trayIcon.Icon);
        _trayIcon.Dispose();
    }
}
