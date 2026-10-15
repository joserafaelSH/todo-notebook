using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using TodoNotebook.Shell;

namespace TodoNotebook;

/// <summary>The whole UI: a borderless window with nothing but the list of todo cards.</summary>
public sealed class MainWindow : Window
{
    private static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    private readonly Border _root;
    private readonly StackPanel _list = new() { Margin = new Thickness(16, 4, 16, 16) };
    private readonly TextBlock _empty;
    private readonly Image _titleIcon = new() { Width = 18, Height = 18, Margin = new Thickness(0, 0, 10, 0) };

    public MainWindow()
    {
        Title = "Todo Notebook";
        MinWidth = 380; MinHeight = 360;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        Cursor = AppCursors.Arrow;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SetResourceReference(BackgroundProperty, "ShellBgBrush");
        SetResourceReference(FontFamilyProperty, "AppFont");
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 40, ResizeBorderThickness = new Thickness(6), CornerRadius = new CornerRadius(0),
            GlassFrameThickness = new Thickness(0), UseAeroCaptionButtons = false,
        });

        ApplySavedBounds();

        // Title bar: icon + name on the left, add / minimize / hide on the right.
        var titleLeft = new StackPanel
        {
            Orientation = Orientation.Horizontal, Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false,
        };
        titleLeft.Children.Add(_titleIcon);
        titleLeft.Children.Add(Ui.Text("Todo Notebook", "ShellMutedBrush", 12, FontWeights.SemiBold));

        var add = ChromeButton("", "Add todo");
        var min = ChromeButton("", "Minimize");
        var close = ChromeButton("", "Hide to tray");
        add.Click += (_, _) => ShellHost.ShowEditor();
        min.Click += (_, _) => WindowState = WindowState.Minimized;
        close.Click += (_, _) => HideToTray();
        var titleRight = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
        };
        titleRight.Children.Add(add);
        titleRight.Children.Add(min);
        titleRight.Children.Add(close);
        var titleBar = new Grid();
        titleBar.Children.Add(titleLeft);
        titleBar.Children.Add(titleRight);

        _empty = Ui.Text("Nothing to do.\nPress Ctrl+Alt+A anywhere to add a todo.", "MutedTextBrush", 13);
        _empty.TextAlignment = TextAlignment.Center;
        _empty.HorizontalAlignment = HorizontalAlignment.Center;
        _empty.VerticalAlignment = VerticalAlignment.Center;
        _empty.Visibility = Visibility.Collapsed;

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _list };
        var body = new Grid();
        body.Children.Add(scroll);
        body.Children.Add(_empty);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(titleBar);
        Grid.SetRow(body, 1);
        grid.Children.Add(body);

        _root = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = grid };
        _root.SetResourceReference(Border.BackgroundProperty, "ShellBgBrush");
        _root.SetResourceReference(Border.BorderBrushProperty, "ShellBorderBrush");
        Content = _root;

        StateChanged += (_, _) => _root.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        KeyDown += OnWindowKeyDown;
        Loaded += (_, _) => TryRoundCorners();
        SourceInitialized += (_, _) => TryRoundCorners();
        Store.Changed += () => Dispatcher.BeginInvoke(Rebuild);
        _titleIcon.Source = AppIcon.Create();
        Icon = AppIcon.Create();
        Rebuild();
    }

    private static Button ChromeButton(string glyph, string tooltip)
    {
        var b = new Button
        {
            Content = glyph, ToolTip = tooltip, Width = 44, Height = 32, FontSize = 10, FontFamily = IconFont,
            Cursor = AppCursors.Pointer,
        };
        b.SetResourceReference(Control.ForegroundProperty, "ShellMutedBrush");
        WindowChrome.SetIsHitTestVisibleInChrome(b, true);
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border), "bd");
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.MarginProperty, new Thickness(2));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("ShellHoverBrush"), "bd"));
        hover.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("ShellTextBrush")));
        template.Triggers.Add(hover);
        b.Template = template;
        return b;
    }

    // ---------- list ----------

    private void Rebuild()
    {
        _list.Children.Clear();
        var items = Store.Sorted();
        _empty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var item in items) _list.Children.Add(BuildCard(item, today));
    }

    private FrameworkElement BuildCard(TodoItem item, DateOnly today)
    {
        var check = Ui.Styled(new CheckBox
        {
            IsChecked = item.Done, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 12, 0),
            Cursor = AppCursors.Pointer,
        }, "TaskCheckBox");
        check.Click += (_, _) => Store.SetDone(item, check.IsChecked == true);

        var title = Ui.Text(item.Title, item.Done ? "MutedTextBrush" : "TextBrush", 14.5, FontWeights.SemiBold);
        title.TextWrapping = TextWrapping.Wrap;
        if (item.Done) title.TextDecorations = TextDecorations.Strikethrough;

        var body = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        body.Children.Add(title);
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            var desc = Ui.Text(item.Description, "MutedTextBrush", 12.5);
            desc.TextWrapping = TextWrapping.Wrap;
            desc.Margin = new Thickness(0, 4, 0, 0);
            body.Children.Add(desc);
        }
        if (item.Date is DateOnly d)
        {
            var color = item.Done ? null : d < today ? "DangerColor" : d == today ? "AccentColor" : null;
            body.Children.Add(color != null ? Ui.Chip(Ui.FormatDate(d, today), color) : MutedChip(Ui.FormatDate(d, today)));
        }

        var delete = Ui.IconButton("", "Delete");
        delete.VerticalAlignment = VerticalAlignment.Top;
        delete.Click += (_, _) => Store.Remove(item);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(check);
        Grid.SetColumn(body, 1);
        row.Children.Add(body);
        Grid.SetColumn(delete, 2);
        row.Children.Add(delete);

        var card = Ui.Styled(new Border
        {
            Child = row, Margin = new Thickness(0, 0, 0, 10), Cursor = AppCursors.Pointer,
            Opacity = item.Done ? 0.6 : 1,
        }, "CardBorder");
        card.MouseLeftButtonUp += (_, e) =>
        {
            if (e.OriginalSource is DependencyObject src && IsInside<System.Windows.Controls.Primitives.ButtonBase>(src, card)) return;
            ShellHost.ShowEditor(item);
        };
        card.MouseEnter += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, "BorderStrongBrush");
        card.MouseLeave += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        return card;
    }

    private static Border MutedChip(string text)
    {
        var chip = Ui.Chip(text, "MutedTextColor");
        chip.Background = Ui.Tint("SurfaceAltColor", 0xFF);
        return chip;
    }

    private static bool IsInside<T>(DependencyObject node, DependencyObject stopAt) where T : DependencyObject
    {
        for (var cur = node; cur != null && cur != stopAt; cur = (cur is Visual or System.Windows.Media.Media3D.Visual3D)
                 ? VisualTreeHelper.GetParent(cur) : LogicalTreeHelper.GetParent(cur))
            if (cur is T) return true;
        return false;
    }

    // ---------- window behaviour ----------

    private void ApplySavedBounds()
    {
        var s = Store.Settings;
        Width = s.WindowWidth is > 300 ? s.WindowWidth.Value : 520;
        Height = s.WindowHeight is > 300 ? s.WindowHeight.Value : 720;
        if (s.WindowLeft is double l && s.WindowTop is double t
            && l > SystemParameters.VirtualScreenLeft - Width + 80
            && l < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80
            && t > SystemParameters.VirtualScreenTop - 10
            && t < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 80)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = l;
            Top = t;
        }
    }

    private void SaveBounds()
    {
        try
        {
            var b = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            if (b.IsEmpty || double.IsNaN(b.Left)) return;
            var s = Store.Settings;
            s.WindowLeft = b.Left; s.WindowTop = b.Top; s.WindowWidth = b.Width; s.WindowHeight = b.Height;
            Store.SaveSettings();
        }
        catch { }
    }

    private void HideToTray()
    {
        SaveBounds();
        Hide();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        SaveBounds();
        if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; ShellHost.ShowEditor(); }
        else if (e.Key == Key.Escape) { e.Handled = true; HideToTray(); }
    }

    private void TryRoundCorners()
    {
        try
        {
            var h = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (h == IntPtr.Zero) return;
            int pref = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(h, 33, ref pref, sizeof(int));
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
}
