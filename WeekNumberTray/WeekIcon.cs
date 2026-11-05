using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace WeekNumberTray;

/// <summary>Draws the week number as a tray icon: white digits on a blue rounded square.</summary>
internal static class WeekIcon
{
    private static readonly Color Background = Color.FromArgb(0x00, 0x5A, 0xB4);

    // Height of Segoe UI's digits as a share of the font size
    private const float DigitHeightRatio = 0.7f;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(int week)
    {
        // The size of a small icon at the current display scaling: 16 at 100%, 24 at 150%, ...
        int size = Math.Max(16, Forms.SystemInformation.SmallIconSize.Width);
        string text = week.ToString(CultureInfo.InvariantCulture);

        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using (var background = new SolidBrush(Background))
            using (var shape = RoundedSquare(size, size / 5f))
                g.FillPath(background, shape);

            DrawDigits(g, text, size);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>Frees an icon from <see cref="Create"/>. Windows doesn't do this for us.</summary>
    public static void Destroy(Icon? icon)
    {
        if (icon == null)
            return;
        DestroyIcon(icon.Handle);
        icon.Dispose();
    }

    private static void DrawDigits(Graphics g, string text, int size)
    {
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();

        // The largest font, starting at 90% of the icon height, whose text fits the width
        float em = size * 0.9f;
        Font font = NewFont(em);
        while (em > 6 && g.MeasureString(text, font, int.MaxValue, format).Width > size - 1)
        {
            font.Dispose();
            em -= 0.5f;
            font = NewFont(em);
        }

        using (font)
        {
            // Centre the digits themselves. A font's line box has space above them and below
            // the baseline, so centring the box would put them too high.
            FontFamily family = font.FontFamily;
            float ascent = em * family.GetCellAscent(font.Style) / family.GetEmHeight(font.Style);
            float digitHeight = em * DigitHeightRatio;
            float width = g.MeasureString(text, font, int.MaxValue, format).Width;
            float x = (size - width) / 2f;
            float y = (size - digitHeight) / 2f - (ascent - digitHeight);
            g.DrawString(text, font, Brushes.White, x, y, format);
        }
    }

    private static Font NewFont(float em) => new("Segoe UI", em, FontStyle.Bold, GraphicsUnit.Pixel);

    private static GraphicsPath RoundedSquare(int size, float radius)
    {
        float d = radius * 2;
        float far = size - d;
        var path = new GraphicsPath();
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(far, 0, d, d, 270, 90);
        path.AddArc(far, far, d, d, 0, 90);
        path.AddArc(0, far, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
