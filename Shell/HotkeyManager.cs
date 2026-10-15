using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using TodoNotebook;

namespace TodoNotebook.Shell;

/// <summary>Registers the global hotkeys (show/hide window, quick add) on a window handle.</summary>
public sealed class HotkeyManager : IDisposable
{
    private const int ToggleId = 0x4E47;
    private const int QuickAddId = 0x4E48;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private readonly HashSet<int> _registered = new();

    public event Action? Pressed;
    public event Action? QuickAddPressed;

    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd)!;
        _source.AddHook(WndProc);
    }

    /// <summary>Returns null on success, otherwise a user-facing error message.</summary>
    public string? Register(AppSettings s) =>
        RegisterCore(ToggleId, s.HotkeyCtrl, s.HotkeyAlt, s.HotkeyShift, s.HotkeyKey, Describe(s));

    /// <summary>Quick-add hotkey. Returns null on success, otherwise a user-facing error message.</summary>
    public string? RegisterQuickAdd(AppSettings s) =>
        RegisterCore(QuickAddId, s.QuickAddCtrl, s.QuickAddAlt, s.QuickAddShift, s.QuickAddKey, DescribeQuickAdd(s));

    private string? RegisterCore(int id, bool ctrl, bool alt, bool shift, string keyName, string description)
    {
        Unregister(id);
        if (!Enum.TryParse<Key>(keyName, out var key) || key == Key.None)
            return "No hotkey key is set.";
        uint mods = MOD_NOREPEAT;
        if (ctrl) mods |= MOD_CONTROL;
        if (alt) mods |= MOD_ALT;
        if (shift) mods |= MOD_SHIFT;
        if ((mods & ~MOD_NOREPEAT) == 0)
            return "The hotkey needs at least one modifier (Ctrl, Alt or Shift).";
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (RegisterHotKey(_hwnd, id, mods, vk))
        {
            _registered.Add(id);
            return null;
        }
        return $"Could not register {description}. Another app is probably using it. Choose a different combination.";
    }

    public static string Describe(AppSettings s) => Join(s.HotkeyCtrl, s.HotkeyAlt, s.HotkeyShift, s.HotkeyKey);
    public static string DescribeQuickAdd(AppSettings s) => Join(s.QuickAddCtrl, s.QuickAddAlt, s.QuickAddShift, s.QuickAddKey);

    private static string Join(bool ctrl, bool alt, bool shift, string key)
    {
        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        parts.Add(key);
        return string.Join("+", parts);
    }

    private void Unregister(int id)
    {
        if (_registered.Remove(id)) UnregisterHotKey(_hwnd, id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (id == ToggleId) { handled = true; Pressed?.Invoke(); }
            else if (id == QuickAddId) { handled = true; QuickAddPressed?.Invoke(); }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister(ToggleId);
        Unregister(QuickAddId);
        _source.RemoveHook(WndProc);
    }
}
