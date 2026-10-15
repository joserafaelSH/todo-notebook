using System.Windows;
using System.Windows.Media;

namespace TodoNotebook.Themes;

/// <summary>The Goal Notebook "Dark" palette. Each key K becomes the resources "KColor" and "KBrush" (the only theme for now).</summary>
public static class Dark
{
    private static readonly Dictionary<string, string> Colors = new()
    {
        ["Bg"] = "#0F1115", ["Surface"] = "#171A21", ["SurfaceAlt"] = "#1F232C", ["Border"] = "#2A2F3B",
        ["Text"] = "#E8EAEE", ["MutedText"] = "#8B93A3",
        ["Accent"] = "#6C8CFF", ["AccentHover"] = "#8AA3FF", ["AccentPressed"] = "#566FE0", ["OnAccent"] = "#FFFFFF",
        ["Danger"] = "#F0616D", ["Warn"] = "#FACC15",
        ["BorderStrong"] = "#3A4150", ["DisabledText"] = "#586070", ["Thumb"] = "#363C49", ["ThumbHover"] = "#4A5263",
        ["Input"] = "#12141A",
        ["ShellBg"] = "#16181D", ["ShellSidebar"] = "#1B1E25", ["ShellBorder"] = "#2A2E38", ["ShellText"] = "#E6E8EE",
        ["ShellMuted"] = "#8B91A0", ["ShellHover"] = "#242832", ["ShellSelected"] = "#2C3342",
    };

    private static readonly (string Key, string Source, byte Alpha)[] Derived =
    {
        ("AccentDim", "Accent", 0x26), ("AccentClear", "Accent", 0x00), ("AccentSelect", "Accent", 0x80),
        ("SurfaceAltClear", "SurfaceAlt", 0x00),
    };

    public static void Apply()
    {
        var res = Application.Current.Resources;
        foreach (var (key, hex) in Colors) Set(res, key, (Color)ColorConverter.ConvertFromString(hex));
        foreach (var (key, source, alpha) in Derived)
        {
            var c = (Color)ColorConverter.ConvertFromString(Colors[source]);
            Set(res, key, Color.FromArgb(alpha, c.R, c.G, c.B));
        }
    }

    private static void Set(ResourceDictionary res, string key, Color color)
    {
        res[key + "Color"] = color;
        if (res[key + "Brush"] is SolidColorBrush existing && !existing.IsFrozen && !existing.IsSealed)
            existing.Color = color;
        else
            res[key + "Brush"] = new SolidColorBrush(color);
    }
}
