using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScreenshotApp;

public static class WindowCapture
{
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    public static Bitmap? Capture(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out RECT r)) return null;
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return null;

        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(150);

        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        IntPtr hdc = g.GetHdc();
        bool ok = PrintWindow(hWnd, hdc, PW_RENDERFULLCONTENT);
        g.ReleaseHdc(hdc);

        if (!ok)
        {
            bmp.Dispose();
            return null;
        }
        return bmp;
    }
}
