using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TodoNotebook.Shell;

/// <summary>Window visibility, hotkeys and quit logic shared by the tray and the hotkeys.</summary>
public static class ShellHost
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static Window? MainWindow { get; set; }
    public static HotkeyManager? Hotkey { get; set; }
    private static EditorWindow? _editor;

    /// <summary>Opens the quick-add window, or the same window prefilled to edit <paramref name="item"/>.</summary>
    public static void ShowEditor(TodoItem? item = null)
    {
        _editor ??= new EditorWindow();
        _editor.Summon(item);
    }

    public static void ApplySettings()
    {
        var s = Store.Settings;
        AutostartHelper.Apply(s.StartWithWindows);
        Hotkey?.Register(s);
        Hotkey?.RegisterQuickAdd(s);
    }

    public static void ShowMainWindow()
    {
        var w = MainWindow;
        if (w == null) return;
        if (!w.IsVisible) w.Show();
        if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
        w.Activate();
        w.Topmost = true;
        w.Topmost = false;
        var h = new WindowInteropHelper(w).Handle;
        if (h != IntPtr.Zero) SetForegroundWindow(h);
        w.Focus();
    }

    public static void ToggleMainWindow()
    {
        var w = MainWindow;
        if (w == null) return;
        if (w.IsVisible && w.IsActive && w.WindowState != WindowState.Minimized) w.Hide();
        else ShowMainWindow();
    }

    public static void Quit()
    {
        Hotkey?.Dispose();
        (Application.Current as App)?.DisposeTray();
        Application.Current.Shutdown();
    }
}
