using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ScreenshotApp;

public static class PdfExporter
{
    static PdfExporter()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public static string Export(Bitmap bmp, string imagePath)
    {
        string pdfPath = Path.ChangeExtension(imagePath, ".pdf");

        string tmp = Path.Combine(Path.GetTempPath(), $"ss_{Guid.NewGuid()}.png");
        bmp.Save(tmp, ImageFormat.Png);
        try
        {
            using var doc = new PdfDocument();
            var page = doc.AddPage();

            double dpi = bmp.HorizontalResolution > 0 ? bmp.HorizontalResolution : 96.0;
            double wPt = bmp.Width * 72.0 / dpi;
            double hPt = bmp.Height * 72.0 / dpi;

            // Fit within A4 (595 × 842 pt) keeping aspect ratio
            const double a4W = 595.28, a4H = 841.89;
            // Use landscape A4 if screenshot is wider than tall
            double maxW = wPt >= hPt ? a4H : a4W;
            double maxH = wPt >= hPt ? a4W : a4H;
            double scale = Math.Min(maxW / wPt, maxH / hPt);
            if (scale < 1.0) { wPt *= scale; hPt *= scale; }

            page.Width = XUnit.FromPoint(wPt);
            page.Height = XUnit.FromPoint(hPt);

            using var gfx = XGraphics.FromPdfPage(page);
            using var img = XImage.FromFile(tmp);
            gfx.DrawImage(img, 0, 0, page.Width.Point, page.Height.Point);

            doc.Save(pdfPath);
        }
        finally
        {
            File.Delete(tmp);
        }

        return pdfPath;
    }
}
