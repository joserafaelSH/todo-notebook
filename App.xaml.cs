using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using TodoNotebook.Shell;

namespace TodoNotebook;

public partial class App : Application
{
    private const string MutexName = "TodoNotebook.SingleInstance.Mutex";
    private const string ShowEventName = "TodoNotebook.SingleInstance.Show";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private TaskbarIcon? _tray;
    private ReminderService? _reminders;

    protected override void OnStartup(StartupEventArgs e)
    {
        var appFont = (FontFamily)FindResource("AppFont");
        System.Windows.Documents.TextElement.FontFamilyProperty.OverrideMetadata(typeof(System.Windows.Documents.TextElement), new FrameworkPropertyMetadata(appFont));
        TextBlock.FontFamilyProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(appFont));
        base.OnStartup(e);

        bool background = e.Args.Contains("--background");

        _mutex = new Mutex(false, MutexName);
        bool owned;
        try { owned = _mutex.WaitOne(TimeSpan.Zero); }
        catch (AbandonedMutexException) { owned = true; }

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        if (!owned)
        {
            _showEvent.Set();
            Environment.Exit(0);
            return;
        }

        Store.Load();
        Themes.Dark.Apply();
        AppCursors.Register(Resources);

        var window = new MainWindow();
        ShellHost.MainWindow = window;

        CreateTray();
        StartShowListener();

        // Create the handle so the hotkeys can be registered while the window is hidden.
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        ShellHost.Hotkey = new HotkeyManager(hwnd);
        ShellHost.Hotkey.Pressed += () => Dispatcher.BeginInvoke(ShellHost.ToggleMainWindow);
        ShellHost.Hotkey.QuickAddPressed += () => Dispatcher.BeginInvoke(() => ShellHost.ShowEditor());
        ShellHost.ApplySettings();

        if (!background) ShellHost.ShowMainWindow();

        if (_tray != null)
        {
            _reminders = new ReminderService(_tray);
            _tray.TrayBalloonTipClicked += (_, _) => ShellHost.ShowMainWindow();
            _reminders.Start();
        }
    }

    private static TextBlock MenuIcon(string glyph) => new()
    {
        Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 13,
    };

    private void CreateTray()
    {
        var menu = new ContextMenu();
        var open = new MenuItem { Header = "Open", Icon = MenuIcon("") };
        open.Click += (_, _) => ShellHost.ShowMainWindow();
        var quick = new MenuItem { Header = "Quick add todo", Icon = MenuIcon("") };
        quick.Click += (_, _) => ShellHost.ShowEditor();
        var quit = new MenuItem { Header = "Quit", Icon = MenuIcon("") };
        quit.Click += (_, _) => ShellHost.Quit();
        menu.Items.Add(open);
        menu.Items.Add(quick);
        menu.Items.Add(new Separator());
        menu.Items.Add(quit);

        _tray = new TaskbarIcon
        {
            IconSource = AppIcon.Create(),
            ToolTipText = "Todo Notebook",
            ContextMenu = menu,
            MenuActivation = PopupActivationMode.RightClick,
            DoubleClickCommand = null,
        };
        _tray.TrayLeftMouseUp += (_, _) => ShellHost.ShowMainWindow();
    }

    /// <summary>A second launch signals this instance to show its window instead of starting another.</summary>
    private void StartShowListener()
    {
        var t = new Thread(() =>
        {
            while (true)
            {
                try { _showEvent!.WaitOne(); }
                catch { return; }
                Dispatcher.BeginInvoke(ShellHost.ShowMainWindow);
            }
        }) { IsBackground = true };
        t.Start();
    }

    public void DisposeTray()
    {
        _reminders?.Dispose();
        _reminders = null;
        _tray?.Dispose();
        _tray = null;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DisposeTray();
        try { _mutex?.ReleaseMutex(); } catch { }
        base.OnExit(e);
    }
}
