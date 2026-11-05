using System.Reflection;
using System.Windows;

namespace WeekNumberTray;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {DescribeVersion()}, (c) Per Hejndorf";
    }

    /// <summary>The version and short commit, from the "1.0.3+abcdef..." the SDK stamps into the build.</summary>
    private static string DescribeVersion()
    {
        string full = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        string[] parts = full.Split('+');
        if (parts.Length < 2)
            return parts[0];
        return $"{parts[0]} · {parts[1][..Math.Min(7, parts[1].Length)]}";
    }

    private void OnOk(object sender, RoutedEventArgs e) => Close();
}
