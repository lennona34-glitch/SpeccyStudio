using SpeccyStudio.Core;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SpeccyStudio.Controls;

public sealed class SpectrumCanvas : FrameworkElement
{
    private bool _painting;
    private bool _paper;
    private (int X, int Y) _last = (-1, -1);

    public SpectrumScreen? Screen { get; set; }
    public int SelectedColorIndex { get; set; } = 7;
    public bool ShowAttributeGrid { get; set; } = true;
    public bool FlashPhase { get; set; }

    public event EventHandler? EditStarted;
    public event EventHandler? PixelChanged;

    public SpectrumCanvas()
    {
        Focusable = true;
        Cursor = Cursors.Cross;
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsInfinity(availableSize.Width) ? 768 : availableSize.Width;
        double h = double.IsInfinity(availableSize.Height) ? 576 : availableSize.Height;
        double scale = Math.Min(w / SpectrumScreen.Width, h / SpectrumScreen.Height);
        return new Size(SpectrumScreen.Width * scale, SpectrumScreen.Height * scale);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(4, 6, 10)), null, new Rect(RenderSize));
        if (Screen is null) return;
        double scale = Math.Min(ActualWidth / SpectrumScreen.Width, ActualHeight / SpectrumScreen.Height);
        double drawWidth = SpectrumScreen.Width * scale, drawHeight = SpectrumScreen.Height * scale;
        double ox = (ActualWidth - drawWidth) / 2, oy = (ActualHeight - drawHeight) / 2;

        var bitmap = Screen.ToBitmapSource(FlashPhase);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        dc.DrawImage(bitmap, new Rect(ox, oy, drawWidth, drawHeight));

        if (ShowAttributeGrid && scale >= 2)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(75, 255, 255, 255)), 1);
            pen.Freeze();
            for (int x = 0; x <= 32; x++)
            {
                double px = ox + x * 8 * scale;
                dc.DrawLine(pen, new Point(px, oy), new Point(px, oy + drawHeight));
            }
            for (int y = 0; y <= 24; y++)
            {
                double py = oy + y * 8 * scale;
                dc.DrawLine(pen, new Point(ox, py), new Point(ox + drawWidth, py));
            }
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (Screen is null || e.ChangedButton is not (MouseButton.Left or MouseButton.Right)) return;
        Focus(); CaptureMouse();
        _painting = true;
        _paper = e.ChangedButton == MouseButton.Right;
        _last = (-1, -1);
        EditStarted?.Invoke(this, EventArgs.Empty);
        PaintAt(e.GetPosition(this));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_painting) PaintAt(e.GetPosition(this));
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_painting) return;
        _painting = false;
        _last = (-1, -1);
        ReleaseMouseCapture();
    }

    private void PaintAt(Point point)
    {
        if (Screen is null) return;
        double scale = Math.Min(ActualWidth / SpectrumScreen.Width, ActualHeight / SpectrumScreen.Height);
        double ox = (ActualWidth - SpectrumScreen.Width * scale) / 2;
        double oy = (ActualHeight - SpectrumScreen.Height * scale) / 2;
        int x = (int)((point.X - ox) / scale), y = (int)((point.Y - oy) / scale);
        if ((uint)x >= SpectrumScreen.Width || (uint)y >= SpectrumScreen.Height || _last == (x, y)) return;
        _last = (x, y);
        Screen.SetPixel(x, y, SelectedColorIndex, _paper);
        PixelChanged?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
    }
}
