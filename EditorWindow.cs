using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace TodoNotebook;

/// <summary>Floating window to add a todo without opening the list (global quick-add hotkey) or to edit an existing one.</summary>
public sealed class EditorWindow : Window
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly TextBlock _heading;
    private readonly TextBox _title;
    private readonly TextBox _description;
    private readonly DatePicker _date;
    private readonly TextBlock _hint;
    private TodoItem? _editing;

    public EditorWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;
        Width = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Cursor = Shell.AppCursors.Arrow;

        _heading = Ui.Text("Quick add todo", "MutedTextBrush", 12, FontWeights.SemiBold);
        _title = Ui.Styled(new TextBox { FontSize = 15, Padding = new Thickness(10, 8, 10, 8), MaxLength = 200, Margin = new Thickness(0, 8, 0, 8) }, "RoundedTextBox");
        _description = Ui.Styled(new TextBox
        {
            FontSize = 13, Padding = new Thickness(10, 8, 10, 8), MaxLength = 2000, Height = 84,
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 0, 8),
        }, "RoundedTextBox");
        _date = Ui.Styled(new DatePicker { SelectedDate = DateTime.Today }, "RoundedDatePicker");
        _hint = Ui.Text("", "MutedTextBrush", 11.5);
        _hint.Margin = new Thickness(0, 10, 0, 0);

        var stack = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };
        stack.Children.Add(_heading);
        stack.Children.Add(_title);
        stack.Children.Add(_description);
        stack.Children.Add(_date);
        stack.Children.Add(_hint);

        var card = Ui.Styled(new Border { CornerRadius = new CornerRadius(14), Child = stack, Margin = new Thickness(12) }, "CardBorder");
        card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 2, Opacity = 0.5 };
        Content = card;

        _title.KeyDown += (_, e) => { if (e.Key == Key.Enter) { e.Handled = true; Commit(); } };
        _description.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; Commit(); }
        };
        KeyDown += (_, e) => { if (e.Key == Key.Escape && !e.Handled) { e.Handled = true; Hide(); } };
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.OriginalSource is not TextBox and not DatePicker) { try { DragMove(); } catch { } }
        };
    }

    /// <summary>Opens empty for a new todo, or prefilled when <paramref name="item"/> is given.</summary>
    public void Summon(TodoItem? item = null)
    {
        _editing = item;
        _heading.Text = item == null ? "Quick add todo" : "Edit todo";
        _title.Text = item?.Title ?? "";
        _description.Text = item?.Description ?? "";
        _date.SelectedDate = item == null ? DateTime.Today : item.Date?.ToDateTime(TimeOnly.MinValue);
        _hint.Text = item == null ? "Enter to add · Esc to close" : "Enter to save · Ctrl+Enter in description · Esc to cancel";
        Show();
        Activate();
        Topmost = true;
        var h = new WindowInteropHelper(this).Handle;
        if (h != IntPtr.Zero) SetForegroundWindow(h);
        _title.Focus();
        Keyboard.Focus(_title);
        _title.SelectAll();
    }

    private void Commit()
    {
        var title = _title.Text.Trim();
        if (title.Length == 0) return;
        var description = _description.Text.Trim();
        DateOnly? date = _date.SelectedDate is DateTime d ? DateOnly.FromDateTime(d) : null;
        if (_editing != null)
        {
            Store.Update(_editing, title, description, date);
            _editing = null;
            Hide();
            return;
        }
        Store.Add(title, description, date);
        _title.Text = "";
        _description.Text = "";
        _hint.Text = $"Added “{(title.Length > 40 ? title[..40] + "…" : title)}”. Add another or press Esc.";
        _title.Focus();
    }
}
