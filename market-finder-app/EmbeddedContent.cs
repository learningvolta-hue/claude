using System.Reflection;

namespace MarketFinderPro;

/// <summary>
/// Legge i file del corso (index.html, assets/*) incorporati nell'EXE come risorse.
/// In questo modo l'applicazione è un unico file, senza cartelle da distribuire.
/// </summary>
static class EmbeddedContent
{
    private const string Prefix = "wwwroot/";

    private static readonly Assembly Asm = typeof(EmbeddedContent).Assembly;

    // Nome risorsa normalizzato ("assets/app.js") -> nome reale nel manifest.
    // Su Windows MSBuild può usare "\" come separatore: normalizziamo sempre a "/".
    private static readonly Dictionary<string, string> Index = Asm
        .GetManifestResourceNames()
        .Select(n => (Key: n.Replace('\\', '/'), Name: n))
        .Where(r => r.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(r => r.Key[Prefix.Length..], r => r.Name, StringComparer.OrdinalIgnoreCase);

    public static Stream? Open(string relativePath)
    {
        var key = relativePath.TrimStart('/');
        if (key.Length == 0) key = "index.html";
        return Index.TryGetValue(key, out var name) ? Asm.GetManifestResourceStream(name) : null;
    }

    public static Stream? OpenIcon() => Asm.GetManifestResourceStream("app.ico");

    public static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" or ".htm" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".ico" => "image/x-icon",
        ".woff2" => "font/woff2",
        _ => "application/octet-stream",
    };
}
