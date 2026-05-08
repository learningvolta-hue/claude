using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenshotApp;

public class RegionSelector : Form
{
    public Rectangle SelectedRegion { get; private set; }
    public Bitmap Background => _background;

    private readonly Bitmap _background;
    private Point _start;
    private Rectangle _rect;
    private bool _drawing;

    public RegionSelector()
    {
        var bounds = Screen.PrimaryScreen!.Bounds;
        _background = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(_background))
            g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

        FormBorderStyle = FormBorderStyle.None;
        Bounds = bounds;
        Cursor = Cursors.Cross;
        TopMost = true;
        DoubleBuffered = true;
        ShowInTaskbar = false;
        KeyPreview = true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _start = e.Location;
            _drawing = true;
        }
        else if (e.Button == MouseButtons.Right)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_drawing)
        {
            _rect = BuildRect(_start, e.Location);
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _drawing)
        {
            _drawing = false;
            SelectedRegion = BuildRect(_start, e.Location);
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImage(_background, 0, 0);

        using var dim = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        e.Graphics.FillRectangle(dim, ClientRectangle);

        if (_rect.Width > 1 && _rect.Height > 1)
        {
            e.Graphics.DrawImage(_background, _rect, _rect, GraphicsUnit.Pixel);

            using var pen = new Pen(Color.White, 2);
            e.Graphics.DrawRectangle(pen, _rect);

            string label = $"{_rect.Width} × {_rect.Height}";
            var font = SystemFonts.DefaultFont;
            var size = e.Graphics.MeasureString(label, font);
            float lx = Math.Min(_rect.Right - size.Width - 4, ClientSize.Width - size.Width - 4);
            float ly = _rect.Bottom + 4;
            if (ly + size.Height > ClientSize.Height) ly = _rect.Top - size.Height - 4;
            e.Graphics.DrawString(label, font, Brushes.White, lx, ly);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _background.Dispose();
        base.Dispose(disposing);
    }

    private static Rectangle BuildRect(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
