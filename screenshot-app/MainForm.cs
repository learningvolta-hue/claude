using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ScreenshotApp;

public class MainForm : Form
{
    private readonly HotkeyManager _hotkeys;
    private readonly NotifyIcon _tray;
    private bool _exitRequested;

    private const int ID_FULLSCREEN = 1;
    private const int ID_REGION = 2;
    private const int ID_WINDOW = 3;

    public MainForm()
    {
        InitializeComponent();

        _hotkeys = new HotkeyManager(Handle);
        bool f = _hotkeys.Register(ID_FULLSCREEN, Keys.F, ctrl: true, alt: true);
        bool r = _hotkeys.Register(ID_REGION, Keys.R, ctrl: true, alt: true);
        bool w = _hotkeys.Register(ID_WINDOW, Keys.W, ctrl: true, alt: true);

        if (!f || !r || !w)
            MessageBox.Show(
                "Alcune hotkey non sono state registrate (potrebbero essere già in uso da un'altra app).",
                "Avviso hotkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Apri", null, (_, _) => ShowMainWindow());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Schermata intera  (Ctrl+Alt+F)", null, (_, _) => CaptureFullScreen());
        trayMenu.Items.Add("Seleziona area  (Ctrl+Alt+R)", null, (_, _) => CaptureRegion());
        trayMenu.Items.Add("Finestra specifica  (Ctrl+Alt+W)", null, (_, _) => CaptureWindow());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Esci", null, (_, _) => ExitApp());

        _tray = new NotifyIcon
        {
            Text = "Screenshot App",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void InitializeComponent()
    {
        Text = "Screenshot App";
        Size = new Size(340, 240);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(12, 10, 12, 6)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        var btnFull = new Button { Text = "Schermata intera  (Ctrl+Alt+F)", Dock = DockStyle.Fill };
        var btnRegion = new Button { Text = "Seleziona area  (Ctrl+Alt+R)", Dock = DockStyle.Fill };
        var btnWindow = new Button { Text = "Finestra specifica  (Ctrl+Alt+W)", Dock = DockStyle.Fill };
        var lblInfo = new Label
        {
            Text = "Salvati in Immagini\\Screenshots  —  chiudi per minimizzare nel vassoio",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 7.5f)
        };

        btnFull.Click += (_, _) => CaptureFullScreen();
        btnRegion.Click += (_, _) => CaptureRegion();
        btnWindow.Click += (_, _) => CaptureWindow();

        layout.Controls.Add(btnFull, 0, 0);
        layout.Controls.Add(btnRegion, 0, 1);
        layout.Controls.Add(btnWindow, 0, 2);
        layout.Controls.Add(lblInfo, 0, 3);
        Controls.Add(layout);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == HotkeyManager.WM_HOTKEY)
        {
            switch (m.WParam.ToInt32())
            {
                case ID_FULLSCREEN: CaptureFullScreen(); break;
                case ID_REGION: CaptureRegion(); break;
                case ID_WINDOW: CaptureWindow(); break;
            }
        }
        base.WndProc(ref m);
    }

    private void CaptureFullScreen()
    {
        bool wasVisible = Visible;
        if (wasVisible) Hide();
        Thread.Sleep(300);

        var bounds = Screen.PrimaryScreen!.Bounds;
        var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

        if (wasVisible) Show();
        SaveAndPreview(bmp);
        bmp.Dispose();
    }

    private void CaptureRegion()
    {
        bool wasVisible = Visible;
        if (wasVisible) Hide();
        Thread.Sleep(300);

        using var sel = new RegionSelector();
        if (sel.ShowDialog() == DialogResult.OK && !sel.SelectedRegion.IsEmpty)
        {
            var r = sel.SelectedRegion;
            var bmp = sel.Background.Clone(r, sel.Background.PixelFormat);
            if (wasVisible) Show();
            SaveAndPreview(bmp);
            bmp.Dispose();
        }
        else
        {
            if (wasVisible) Show();
        }
    }

    private void CaptureWindow()
    {
        using var dlg = new WindowPickerDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedHandle != IntPtr.Zero)
        {
            var bmp = WindowCapture.Capture(dlg.SelectedHandle);
            if (bmp != null)
            {
                SaveAndPreview(bmp);
                bmp.Dispose();
            }
            else
            {
                MessageBox.Show("Impossibile catturare la finestra selezionata.",
                    "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void SaveAndPreview(Bitmap bmp)
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Screenshots");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        bmp.Save(path, ImageFormat.Png);

        using var prev = new PreviewForm(bmp, path);
        prev.ShowDialog(this);
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _exitRequested = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _tray.ShowBalloonTip(2000, "Screenshot App",
                "L'app è attiva nel vassoio di sistema.\nUsa Ctrl+Alt+F/R/W per gli screenshot rapidi.",
                ToolTipIcon.Info);
            return;
        }
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        base.OnFormClosing(e);
    }

    private static Icon CreateTrayIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.CornflowerBlue);
            g.FillRectangle(Brushes.White, 2, 3, 12, 9);
            g.FillEllipse(Brushes.CornflowerBlue, 5, 5, 6, 6);
            g.FillEllipse(Brushes.SteelBlue, 6, 6, 4, 4);
            g.FillRectangle(Brushes.CornflowerBlue, 10, 3, 4, 2);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
