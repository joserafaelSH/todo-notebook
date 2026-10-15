using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace TodoNotebook.Shell;

/// <summary>
/// Tray notification at 10:00 and 16:00 summarising what is still open. Checks every minute, so a slot
/// missed while the PC slept (or the app was closed) still fires once, on the next check.
/// </summary>
public sealed class ReminderService : IDisposable
{
    private static readonly int[] Hours = { 10, 16 };

    private readonly TaskbarIcon _tray;
    private readonly DispatcherTimer _timer;

    public ReminderService(TaskbarIcon tray)
    {
        _tray = tray;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (_, _) => Check();
    }

    public void Start()
    {
        _timer.Start();
        Check();
    }

    internal static string? Compose(DateOnly today)
    {
        var open = Store.Todos.Where(t => !t.Done).ToList();
        if (open.Count == 0) return null;
        var overdue = open.Count(t => t.Date < today);
        var dueToday = open.Count(t => t.Date == today);
        var parts = new List<string>();
        if (overdue > 0) parts.Add($"{overdue} overdue");
        if (dueToday > 0) parts.Add($"{dueToday} due today");
        var rest = open.Count - overdue - dueToday;
        if (rest > 0) parts.Add($"{rest} upcoming");
        var first = open.OrderBy(t => t.Date ?? DateOnly.MaxValue).First().Title;
        return string.Join(" · ", parts) + "\n" + (first.Length > 60 ? first[..60] + "…" : first);
    }

    private void Check()
    {
        try
        {
            var s = Store.Settings;
            var now = DateTime.Now;
            var day = DateOnly.FromDateTime(now).ToString("yyyy-MM-dd");
            s.RemindersFired.RemoveAll(x => !x.StartsWith(day));      // old days are no longer needed

            var pending = Hours.Where(h => now.Hour >= h && !s.RemindersFired.Contains($"{day}|{h}")).ToList();
            if (pending.Count == 0) return;

            foreach (var h in pending) s.RemindersFired.Add($"{day}|{h}");   // one balloon even if several slots were missed
            Store.SaveSettings();
            var message = Compose(DateOnly.FromDateTime(now));
            if (message != null) _tray.ShowBalloonTip("Todo Notebook", message, BalloonIcon.Info);
        }
        catch { /* a reminder must never take the app down */ }
    }

    public void Dispose() => _timer.Stop();
}
