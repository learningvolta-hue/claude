using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenshotApp;

public class PreviewForm : Form
{
    private readonly Bitmap _bmp;
    private readonly string _path;

    public PreviewForm(Bitmap bmp, string path)
    {
        _bmp = bmp;
        _path = path;

        Text = $"Screenshot — {Path.GetFileName(path)}";
        Size = new Size(780, 560);
        MinimumSize = new Size(420, 300);
        StartPosition = FormStartPosition.CenterScreen;

        var pic = new PictureBox
        {
            Image = bmp,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = Color.FromArgb(245, 245, 245)
        };

        var btnClose = new Button { Text = "Chiudi", Width = 80, Height = 30 };
        var btnFolder = new Button { Text = "Apri cartella", Width = 110, Height = 30 };
        var btnCopy = new Button { Text = "Copia negli appunti", Width = 145, Height = 30 };
        var btnPdf = new Button { Text = "Esporta PDF", Width = 105, Height = 30 };

        var lblPath = new Label
        {
            Text = path,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f)
        };

        btnClose.Click += (_, _) => Close();
        btnFolder.Click += (_, _) => Process.Start("explorer.exe", $"/select,\"{_path}\"");
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetImage(_bmp);
            btnCopy.Text = "Copiato!";
            var t = new System.Windows.Forms.Timer { Interval = 1500 };
            t.Tick += (_, _) => { btnCopy.Text = "Copia negli appunti"; t.Stop(); t.Dispose(); };
            t.Start();
        };
        btnPdf.Click += (_, _) =>
        {
            btnPdf.Enabled = false;
            btnPdf.Text = "Generando...";
            try
            {
                string pdfPath = PdfExporter.Export(_bmp, _path);
                btnPdf.Text = "PDF salvato!";
                var t = new System.Windows.Forms.Timer { Interval = 2000 };
                t.Tick += (_, _) =>
                {
                    btnPdf.Text = "Esporta PDF";
                    btnPdf.Enabled = true;
                    t.Stop(); t.Dispose();
                };
                t.Start();
                Process.Start("explorer.exe", $"/select,\"{pdfPath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore esportazione PDF:\n{ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnPdf.Text = "Esporta PDF";
                btnPdf.Enabled = true;
            }
        };

        btnPanel.Controls.Add(btnClose);
        btnPanel.Controls.Add(btnFolder);
        btnPanel.Controls.Add(btnCopy);
        btnPanel.Controls.Add(btnPdf);

        var statusBar = new StatusStrip();
        statusBar.Items.Add(new ToolStripStatusLabel(path) { Spring = true, TextAlign = ContentAlignment.MiddleLeft });
        statusBar.Items.Add(new ToolStripStatusLabel($"{bmp.Width} × {bmp.Height} px"));

        Controls.Add(pic);
        Controls.Add(btnPanel);
        Controls.Add(statusBar);
    }
}
