using Velopack;

namespace WeekNumberTray;

internal static class Program
{
    // Kept in a field so the mutex lives as long as the app does
    private static Mutex? _singleInstance;

    [STAThread]
    private static void Main()
    {
        // Must be the first thing to run: handles Velopack's install/update/uninstall hooks
        // and may exit the process. Don't swallow exceptions here.
        VelopackApp.Build().Run();

        _singleInstance = new Mutex(initiallyOwned: true, @"Local\WeekNumberTray.Wpf.SingleInstance", out bool isFirst);
        if (!isFirst)
            return;

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
