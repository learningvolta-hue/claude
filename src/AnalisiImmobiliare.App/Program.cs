using System.Globalization;

namespace AnalisiImmobiliare.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Numeri, date e valute sempre in formato italiano (virgola decimale, punto per le migliaia).
        var it = CultureInfo.GetCultureInfo("it-IT");
        CultureInfo.DefaultThreadCurrentCulture = it;
        CultureInfo.DefaultThreadCurrentUICulture = it;
        Thread.CurrentThread.CurrentCulture = it;
        Thread.CurrentThread.CurrentUICulture = it;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args.FirstOrDefault()));
    }
}
