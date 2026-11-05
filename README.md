<img src="WeekNumberTray/icon.png" alt="" width="96" align="right">

# Week Number Tray

Always know what week it is. Week Number Tray shows the current week number as a small icon in the Windows notification area, and one click opens a calendar with week numbers.

Week numbers follow the ISO 8601 standard used in most of Europe, where weeks start on Monday.

## Install

**[Download the installer](https://github.com/kreaton/WeekNumberTray/releases/latest/download/WeekNumberTray-win-Setup.exe)** and run it. If your PC doesn't have the .NET 10 Desktop Runtime yet, the installer adds it.

The installer isn't code-signed, so Windows may warn you about it. To continue, choose **More info → Run anyway**.

Windows may hide new tray icons behind the **^** arrow on the taskbar. Drag the icon out onto the taskbar to keep it in view.

To get a newer version later, click the icon and choose **Check for updates**. To remove the app, use **Settings → Apps → Installed apps** in Windows.

## How to use it

- **See the week:** the icon always shows the current week number. Hover over it for the full date.
- **Open the calendar:** left- or right-click the icon. Click the icon again, click somewhere else or press Esc to close it.
- **Change month:** use the arrows in the calendar, the mouse wheel or the keyboard.
- **Go back to today:** click the "Today" line in the calendar, or press Home.
- **Start with Windows:** choose **Start with Windows** in the calendar. A check mark shows that it's on.
- **Quit:** choose **Exit** at the bottom of the calendar.

| Key | Action |
|---|---|
| Left, Up or Page Up | Previous month |
| Right, Down or Page Down | Next month |
| Home | Back to the current month |
| Esc | Close the calendar |

The calendar follows the Windows light or dark setting and your accent color. The icon updates by itself when the date changes.

---

## For developers

This is a WPF version, on .NET 10 with the built-in Fluent theme, of the Rust app [WeekNumber](https://github.com/kreaton/WeekNumber). WPF has no tray icon of its own, so it uses the `NotifyIcon` from WinForms. Installs and updates use [Velopack](https://velopack.io).

### Build and run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). You can also open `WeekNumberTray.slnx` in a Visual Studio version that supports .NET 10 and press F5.

```bash
dotnet run --project WeekNumberTray
```

### Releases

Every push to `main` builds the app on GitHub Actions and publishes a new release, one patch version higher than the last (`.github/workflows/release.yml`). To push without releasing, add `[skip ci]` to the commit message. You can also start a release by hand from the repository's Actions tab, or run `WeekNumberTray\BuildRelease.ps1` locally. Add `-LocalOnly` to build without uploading.

The repository must stay public, because **Check for updates** reads its releases without logging in.

### Source files

All of these are in the `WeekNumberTray` folder.

- `Program.cs`: entry point, Velopack startup and the single-instance check
- `TrayController.cs`: tray icon, date tracking and the actions from the calendar menu
- `WeekIcon.cs`: draws the week number as an icon
- `CalendarWindow.xaml`: the calendar popup and its placement next to the pointer
- `AboutWindow.xaml`: about window
- `UpdateChecker.cs`: Check for updates
- `AutoStartManager.cs`: the "Start with Windows" option
