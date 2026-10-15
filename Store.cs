using System.IO;
using System.Text.Json;

namespace TodoNotebook;

public sealed class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly? Date { get; set; }
    /// <summary>Optional time of day on <see cref="Date"/>; the tray reminder for this todo fires then.</summary>
    public TimeOnly? Time { get; set; }
    /// <summary>True once the reminder for the current Date/Time was shown (or skipped because it was already past).</summary>
    public bool Notified { get; set; }
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class TodoItemExtensions
{
    /// <summary>The moment the reminder should fire, or null when the todo has no time.</summary>
    public static DateTime? DueAt(this TodoItem t) =>
        t.Date is DateOnly d && t.Time is TimeOnly time ? d.ToDateTime(time) : null;
}

public sealed class AppSettings
{
    public bool HotkeyCtrl { get; set; } = true;
    public bool HotkeyAlt { get; set; } = true;
    public bool HotkeyShift { get; set; }
    public string HotkeyKey { get; set; } = "T";
    public bool QuickAddCtrl { get; set; } = true;
    public bool QuickAddAlt { get; set; } = true;
    public bool QuickAddShift { get; set; }
    public string QuickAddKey { get; set; } = "A";
    public bool StartWithWindows { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    /// <summary>Notification slots already shown, as "yyyy-MM-dd|HH".</summary>
    public List<string> RemindersFired { get; set; } = new();
}

/// <summary>Todos and settings as two small JSON files in %AppData%\TodoNotebook.</summary>
public static class Store
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TodoNotebook");
    private static readonly string TodosPath = Path.Combine(Folder, "todos.json");
    private static readonly string SettingsPath = Path.Combine(Folder, "settings.json");

    public static AppSettings Settings { get; private set; } = new();
    public static List<TodoItem> Todos { get; private set; } = new();
    public static event Action? Changed;

    public static void Load()
    {
        Settings = Read<AppSettings>(SettingsPath) ?? new AppSettings();
        Todos = Read<List<TodoItem>>(TodosPath) ?? new List<TodoItem>();
        if (!File.Exists(SettingsPath)) SaveSettings();
    }

    /// <summary>Open items first (dated ones by date, then undated), done items at the bottom.</summary>
    public static IReadOnlyList<TodoItem> Sorted() => Todos
        .OrderBy(t => t.Done)
        .ThenBy(t => t.Date ?? DateOnly.MaxValue)
        .ThenBy(t => t.CreatedAt)
        .ToList();

    public static void Add(string title, string description, DateOnly? date, TimeOnly? time)
    {
        var item = new TodoItem { Title = title, Description = description };
        Apply(item, date, time);
        Todos.Add(item);
        SaveTodos();
    }

    public static void Update(TodoItem item, string title, string description, DateOnly? date, TimeOnly? time)
    {
        item.Title = title; item.Description = description;
        Apply(item, date, time);
        SaveTodos();
    }

    /// <summary>A time needs a date. A reminder whose moment has already passed is not fired retroactively.</summary>
    private static void Apply(TodoItem item, DateOnly? date, TimeOnly? time)
    {
        time = date == null ? null : time;
        var changed = item.Date != date || item.Time != time;
        item.Date = date;
        item.Time = time;
        if (changed) item.Notified = item.DueAt() is not DateTime due || due <= DateTime.Now;
    }

    public static void SetDone(TodoItem item, bool done) { item.Done = done; SaveTodos(); }

    public static void Remove(TodoItem item) { Todos.Remove(item); SaveTodos(); }

    public static void MarkNotified(IEnumerable<TodoItem> items)
    {
        foreach (var t in items) t.Notified = true;
        Write(TodosPath, Todos);
    }

    public static void SaveSettings() => Write(SettingsPath, Settings);

    private static void SaveTodos()
    {
        Write(TodosPath, Todos);
        Changed?.Invoke();
    }

    private static T? Read<T>(string path)
    {
        try { if (File.Exists(path)) return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options); }
        catch (Exception ex) when (ex is JsonException or IOException) { }
        return default;
    }

    private static void Write(string path, object value)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(value, value.GetType(), Options));
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
