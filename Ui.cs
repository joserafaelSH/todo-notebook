using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TodoNotebook;

internal static class Ui
{
    public static T Styled<T>(T el, string styleKey) where T : FrameworkElement
    {
        el.SetResourceReference(FrameworkElement.StyleProperty, styleKey);
        return el;
    }

    public static TextBlock Text(string text, string? brushKey = "TextBrush", double size = 13, FontWeight? weight = null)
    {
        var t = new TextBlock { Text = text, FontSize = size, VerticalAlignment = VerticalAlignment.Center };
        if (weight != null) t.FontWeight = weight.Value;
        if (brushKey != null) t.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        return t;
    }

    public static Button IconButton(string glyph, string tooltip)
    {
        var b = new Button
        {
            Content = new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 12 },
            ToolTip = tooltip,
            Padding = new Thickness(6),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Shell.AppCursors.Pointer,
        };
        return Styled(b, "IconButton");
    }

    public static Brush Tint(string colorKey, byte alpha)
    {
        var c = (Color)Application.Current.FindResource(colorKey);
        var b = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
        b.Freeze();
        return b;
    }

    /// <summary>Small rounded label tinted with a theme colour ("AccentColor", "DangerColor", ...).</summary>
    public static Border Chip(string text, string colorKey)
    {
        var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold };
        tb.SetResourceReference(TextBlock.ForegroundProperty, colorKey.Replace("Color", "Brush"));
        return new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 3),
            Background = Tint(colorKey, 0x2E),
            Child = tb,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0),
        };
    }

    public static string FormatDate(DateOnly d, DateOnly today, TimeOnly? time = null)
    {
        int diff = d.DayNumber - today.DayNumber;
        var text = d.ToString("MMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture);
        if (time is TimeOnly t) text += " · " + t.ToString("HH:mm");
        return diff switch
        {
            0 => "Today · " + text,
            1 => "Tomorrow · " + text,
            < 0 => "Overdue · " + text,
            _ => text,
        };
    }
}
