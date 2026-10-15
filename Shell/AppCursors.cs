using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TodoNotebook.Shell;

/// <summary>
/// The app uses one cursor everywhere: a compact arrow drawn from the Lucide "mouse-pointer-2" icon (lucide.dev, ISC licence),
/// white with a thin dark outline. Clickable and draggable things are told apart by hover effects, not by the cursor.
/// The arrow is rendered once at ~3/4 of the system cursor size (DPI aware) and built as an in-memory 32-bit .cur.
/// </summary>
public static class AppCursors
{
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    private const int SM_CXCURSOR = 13;

    private const string ArrowPath =
        "M4.037 4.688a.495.495 0 0 1 .651-.651l16 6.5a.5.5 0 0 1-.063.947l-6.124 1.58a2 2 0 0 0-1.438 1.435l-1.579 6.126a.5.5 0 0 1-.947.063z";

    private static Cursor? _arrow;

    public static Cursor Arrow => _arrow ??= Build();
    /// <summary>Same arrow: kept so existing call sites and XAML resources stay valid.</summary>
    public static Cursor Pointer => Arrow;
    public static Cursor Grab => Arrow;

    /// <summary>Drag-and-drop feedback: the arrow while dragging, the system "no" cursor where dropping is not allowed.</summary>
    public static void UseGrabbingFeedback(UIElement source)
    {
        source.GiveFeedback += (_, e) =>
        {
            e.UseDefaultCursors = false;
            Mouse.SetCursor(e.Effects == DragDropEffects.None ? Cursors.No : Arrow);
            e.Handled = true;
        };
    }

    /// <summary>Makes the cursor available to XAML as DynamicResource PointerCursor / GrabCursor / ArrowCursor.</summary>
    public static void Register(ResourceDictionary resources)
    {
        resources["ArrowCursor"] = Arrow;
        resources["PointerCursor"] = Arrow;
        resources["GrabCursor"] = Arrow;
    }

    private static Cursor Build()
    {
        try
        {
            int size = Math.Clamp((int)Math.Round(GetSystemMetrics(SM_CXCURSOR) * 0.75), 20, 96);
            double k = size / 24.0;
            const double hot = 4.5;   // the arrow tip on the 24x24 grid

            var geometry = Geometry.Parse(ArrowPath);
            var outline = new Pen(new SolidColorBrush(Color.FromRgb(0x14, 0x16, 0x1C)), 1.7)
            {
                StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round,
            };
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(k, k));
                dc.DrawGeometry(Brushes.White, outline, geometry);
                dc.Pop();
            }
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var bgra = new FormatConvertedBitmap(rtb, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[size * size * 4];
            bgra.CopyPixels(pixels, size * 4, 0);

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            int maskRow = ((size + 31) / 32) * 4;
            int imageBytes = 40 + size * size * 4 + maskRow * size;
            w.Write((short)0); w.Write((short)2); w.Write((short)1);                   // ICONDIR: type 2 = cursor
            w.Write((byte)size); w.Write((byte)size);
            w.Write((byte)0); w.Write((byte)0);
            w.Write((short)Math.Clamp((int)Math.Round(hot * k), 0, size - 1));
            w.Write((short)Math.Clamp((int)Math.Round(hot * k), 0, size - 1));
            w.Write(imageBytes); w.Write(6 + 16);                                       // size, offset
            w.Write(40); w.Write(size); w.Write(size * 2); w.Write((short)1); w.Write((short)32);
            w.Write(0); w.Write(size * size * 4 + maskRow * size); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            for (int y = size - 1; y >= 0; y--) w.Write(pixels, y * size * 4, size * 4);   // bottom-up rows
            w.Write(new byte[maskRow * size]);                                         // AND mask: alpha does the work
            w.Flush();
            ms.Position = 0;
            return new Cursor(ms);
        }
        catch
        {
            return Cursors.Arrow;   // a cosmetic feature must never break the app
        }
    }
}
