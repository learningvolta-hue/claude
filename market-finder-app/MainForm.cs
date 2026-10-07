using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MarketFinderPro;

/// <summary>
/// Finestra principale: ospita il corso HTML dentro un controllo WebView2
/// (il motore di Microsoft Edge già presente in Windows 10/11).
/// </summary>
public class MainForm : Form
{
    // Host "virtuale": le richieste a questo indirizzo vengono servite dalle risorse incorporate.
    private const string AppHost = "https://marketfinder.app/";

    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private FormWindowState _stateBeforeFullscreen = FormWindowState.Normal;
    private bool _fullscreen;

    public MainForm()
    {
        Text = "Market Finder Pro — Marco Consiglio";
        MinimumSize = new Size(900, 600);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(0x0f, 0x0f, 0x1a);

        Icon = LoadAppIcon();

        _web.DefaultBackgroundColor = BackColor;
        Controls.Add(_web);

        Load += async (_, _) => await InitWebViewAsync();
    }

    private async Task InitWebViewAsync()
    {
        try
        {
            // Dati di WebView2 (localStorage = progressi del corso) in %LOCALAPPDATA%\MarketFinderPro
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarketFinderPro");
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            await _web.EnsureCoreWebView2Async(env);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            var answer = MessageBox.Show(
                "Per funzionare, Market Finder Pro richiede «Microsoft Edge WebView2 Runtime».\n\n" +
                "È già incluso in Windows 11 e nella maggior parte dei PC con Windows 10.\n" +
                "Vuoi aprire la pagina di download ufficiale Microsoft?",
                "Componente mancante", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer == DialogResult.Yes)
                OpenExternal("https://developer.microsoft.com/microsoft-edge/webview2/");
            Close();
            return;
        }

        var core = _web.CoreWebView2;
        core.Settings.AreDevToolsEnabled = Debugger.IsAttached;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = true;

        // 1) Servi i file del corso dalle risorse incorporate nell'EXE
        core.AddWebResourceRequestedFilter(AppHost + "*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += OnWebResourceRequested;

        // 2) I link esterni (strumenti, siti) si aprono nel browser predefinito
        core.NewWindowRequested += (_, e) => { e.Handled = true; OpenExternal(e.Uri); };
        core.NavigationStarting += (_, e) =>
        {
            if (!e.Uri.StartsWith(AppHost, StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                OpenExternal(e.Uri);
            }
        };

        // 3) Messaggi dalla pagina (es. "Salva checklist" con finestra di dialogo Windows)
        core.WebMessageReceived += OnWebMessageReceived;

        core.Navigate(AppHost + "index.html");
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var path = new Uri(e.Request.Uri).AbsolutePath;
        var stream = EmbeddedContent.Open(Uri.UnescapeDataString(path));
        var env = _web.CoreWebView2.Environment;

        e.Response = stream is null
            ? env.CreateWebResourceResponse(null, 404, "Not Found", "")
            : env.CreateWebResourceResponse(stream, 200, "OK",
                "Content-Type: " + EmbeddedContent.ContentType(path));
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            if (root.GetProperty("type").GetString() == "saveChecklist")
                SaveTextFile(root.GetProperty("fileName").GetString() ?? "checklist.txt",
                             root.GetProperty("content").GetString() ?? "");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Messaggio non riconosciuto: lo ignoriamo
        }
    }

    private void SaveTextFile(string fileName, string content)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Salva la checklist",
            FileName = fileName,
            Filter = "File di testo (*.txt)|*.txt|Tutti i file (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        File.WriteAllText(dlg.FileName, content, new UTF8Encoding(true));
        MessageBox.Show(this, "Checklist salvata in:\n" + dlg.FileName, "Market Finder Pro",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // WebView2 inoltra qui i tasti "acceleratori" (F1–F12, Ctrl+…): F11 = schermo intero
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F11 || (keyData == Keys.Escape && _fullscreen))
        {
            ToggleFullscreen();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _stateBeforeFullscreen = WindowState;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal; // necessario per coprire anche la barra delle applicazioni
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = _stateBeforeFullscreen;
        }
        _fullscreen = !_fullscreen;
    }

    // Icona di finestra e barra delle applicazioni: prima quella incorporata,
    // in alternativa quella dell'EXE stesso
    private static Icon? LoadAppIcon()
    {
        try
        {
            using var stream = EmbeddedContent.OpenIcon();
            if (stream != null) return new Icon(stream);
        }
        catch (ArgumentException) { }

        return Environment.ProcessPath is { } exe ? Icon.ExtractAssociatedIcon(exe) : null;
    }

    private static void OpenExternal(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme is not ("http" or "https" or "mailto")) return;
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
