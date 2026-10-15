using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TodoNotebook.Shell;

/// <summary>The "T" app icon, drawn in the current theme's accent colour (rebuilt only when that colour changes).</summary>
public static class AppIcon
{
    private static BitmapSource? _cached;
    private static (Color Top, Color Bottom, Color Glyph) _cachedFor;

    public static BitmapSource Create()
    {
        var key = (Resolve("AccentHoverColor", Color.FromRgb(0x8B, 0xA8, 0xFF)),
                   Resolve("AccentPressedColor", Color.FromRgb(0x5B, 0x6C, 0xF0)),
                   Resolve("OnAccentColor", Colors.White));
        if (_cached != null && _cachedFor == key) return _cached;

        const int size = 64;
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var brush = new LinearGradientBrush(key.Item1, key.Item2, 45);
            dc.DrawRoundedRectangle(brush, null, new Rect(2, 2, size - 4, size - 4), 16, 16);
            var text = new FormattedText("T", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                40, new SolidColorBrush(key.Item3), 1.0);
            dc.DrawText(text, new Point((size - text.Width) / 2, (size - text.Height) / 2));
        }
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        bmp.Freeze();
        _cached = bmp;
        _cachedFor = key;
        return bmp;
    }

    private static Color Resolve(string key, Color fallback) =>
        Application.Current?.TryFindResource(key) is Color c ? c : fallback;
}
