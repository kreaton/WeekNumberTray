using System.Windows;
using Velopack;
using Velopack.Sources;

namespace WeekNumberTray;

/// <summary>"Check for updates": asks GitHub for a newer release through Velopack and installs it.</summary>
internal sealed class UpdateChecker
{
    // Public GitHub repo whose releases BuildRelease.ps1 uploads to
    private const string RepoUrl = "https://github.com/kreaton/WeekNumberTray";

    private readonly UpdateManager _manager = new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    private bool _running;

    public async Task CheckAsync(Action beforeRestart, Action afterFailedRestart)
    {
        // Clicking the menu item twice mustn't start a second check
        if (_running)
            return;
        _running = true;
        try
        {
            if (!_manager.IsInstalled)
            {
                Message("Updates are only available in the installed app.", "Update Check", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var update = await _manager.CheckForUpdatesAsync();
            if (update == null)
            {
                Message("No updates available.", "Update Check", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var answer = Message(
                $"A new version ({update.TargetFullRelease.Version}) is available. Do you want to download and install it now?",
                "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            await _manager.DownloadUpdatesAsync(update);

            // The restart ends the process without the normal shutdown, so the icon must go now
            beforeRestart();
            _manager.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            afterFailedRestart();
            Message($"Update failed: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _running = false;
        }
    }

    // This app usually has no window in front, so the box gets an invisible topmost owner.
    // Without one, it could open behind whatever the user is working in.
    private static MessageBoxResult Message(string text, string caption, MessageBoxButton buttons, MessageBoxImage image)
    {
        var owner = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        owner.Show();
        owner.Activate();
        try
        {
            return MessageBox.Show(owner, text, caption, buttons, image);
        }
        finally
        {
            owner.Close();
        }
    }
}
