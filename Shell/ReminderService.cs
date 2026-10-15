using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace TodoNotebook.Shell;

/// <summary>
/// Tray notifications. A todo with a time notifies at that moment. The rest are summarised at 10:00 and 16:00.
/// Checks every minute, so a reminder missed while the PC slept (or the app was closed) still fires once, on the next check.
/// </summary>
public sealed class ReminderService : IDisposable
{
    private static readonly int[] SummaryHours = { 10, 16 };

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

    private static string Short(string t) => t.Length > 60 ? t[..60] + "…" : t;

    /// <summary>Summary of the open todos that have no time of their own, or null when there is nothing to say.</summary>
    internal static string? ComposeSummary(DateOnly today)
    {
        var open = Store.Todos.Where(t => !t.Done && t.Time == null).ToList();
        if (open.Count == 0) return null;
        var overdue = open.Count(t => t.Date < today);
        var dueToday = open.Count(t => t.Date == today);
        var parts = new List<string>();
        if (overdue > 0) parts.Add($"{overdue} overdue");
        if (dueToday > 0) parts.Add($"{dueToday} due today");
        var rest = open.Count - overdue - dueToday;
        if (rest > 0) parts.Add($"{rest} upcoming");
        var first = open.OrderBy(t => t.Date ?? DateOnly.MaxValue).First().Title;
        return string.Join(" · ", parts) + "\n" + Short(first);
    }

    private void Check()
    {
        try
        {
            // A todo's own reminder takes the balloon; the summary then waits for the next tick.
            if (CheckTodoReminders()) return;
            CheckSummary();
        }
        catch { /* a reminder must never take the app down */ }
    }

    private bool CheckTodoReminders()
    {
        var now = DateTime.Now;
        var hits = Store.Todos.Where(t => !t.Done && !t.Notified && t.DueAt() is DateTime due && due <= now).ToList();
        if (hits.Count == 0) return false;

        Store.MarkNotified(hits);
        var message = hits.Count == 1
            ? $"{Short(hits[0].Title)}\n{hits[0].Time:HH:mm}"
            : $"{hits.Count} todos are due: " + string.Join(", ", hits.Take(3).Select(h => Short(h.Title))) + (hits.Count > 3 ? "…" : "");
        _tray.ShowBalloonTip("Todo Notebook", message, BalloonIcon.Info);
        return true;
    }

    private void CheckSummary()
    {
        var s = Store.Settings;
        var now = DateTime.Now;
        var day = DateOnly.FromDateTime(now).ToString("yyyy-MM-dd");
        s.RemindersFired.RemoveAll(x => !x.StartsWith(day));      // old days are no longer needed

        var pending = SummaryHours.Where(h => now.Hour >= h && !s.RemindersFired.Contains($"{day}|{h}")).ToList();
        if (pending.Count == 0) return;

        foreach (var h in pending) s.RemindersFired.Add($"{day}|{h}");   // one balloon even if several slots were missed
        Store.SaveSettings();
        var message = ComposeSummary(DateOnly.FromDateTime(now));
        if (message != null) _tray.ShowBalloonTip("Todo Notebook", message, BalloonIcon.Info);
    }

    public void Dispose() => _timer.Stop();
}
