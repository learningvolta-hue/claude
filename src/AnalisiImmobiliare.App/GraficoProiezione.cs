using System.Drawing.Drawing2D;
using AnalisiImmobiliare.Core.Calcolo;

namespace AnalisiImmobiliare.App;

/// <summary>
/// Grafico disegnato a mano con GDI+: mostra come crescono nel tempo
/// il patrimonio netto (valore − debito) e il cash flow cumulato,
/// confrontati con il capitale investito all'inizio.
/// </summary>
internal sealed class GraficoProiezione : Control
{
    private IReadOnlyList<AnnoProiezione> _anni = Array.Empty<AnnoProiezione>();
    private double _capitaleProprio;

    public GraficoProiezione()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    public void ImpostaDati(IReadOnlyList<AnnoProiezione> anni, double capitaleProprio)
    {
        _anni = anni;
        _capitaleProprio = capitaleProprio;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (_anni.Count == 0) return;

        var area = new Rectangle(80, 34, Width - 100, Height - 70);
        if (area.Width < 50 || area.Height < 50) return;

        // Scala verticale: comprende zero, tutte le serie e il capitale investito.
        var valori = _anni.SelectMany(a => new[] { a.PatrimonioNetto, a.CashFlowCumulato })
                          .Append(0).Append(_capitaleProprio).ToList();
        double max = valori.Max(), min = valori.Min();
        if (max - min < 1) max = min + 1;
        double margine = (max - min) * 0.08;
        max += margine;
        if (min < 0) min -= margine;

        float Y(double v) => (float)(area.Bottom - (v - min) / (max - min) * area.Height);
        float X(int anno) => _anni.Count == 1
            ? area.Left + area.Width / 2f
            : area.Left + (anno - 1) * area.Width / (float)(_anni.Count - 1);

        using var font = new Font(Font.FontFamily, 8);
        using var testo = new SolidBrush(Tema.TestoSecondario);
        using var griglia = new Pen(Color.FromArgb(225, 225, 225));

        // Griglia orizzontale con 5 tacche
        for (int i = 0; i <= 5; i++)
        {
            double v = min + (max - min) * i / 5;
            float y = Y(v);
            g.DrawLine(griglia, area.Left, y, area.Right, y);
            var etichetta = (v / 1000).ToString("N0") + "k €";
            var dim = g.MeasureString(etichetta, font);
            g.DrawString(etichetta, font, testo, area.Left - dim.Width - 4, y - dim.Height / 2);
        }

        // Asse degli anni
        int passo = Math.Max(1, _anni.Count / 10);
        foreach (var a in _anni.Where(a => a.Anno % passo == 0 || a.Anno == 1))
            g.DrawString(a.Anno.ToString(), font, testo, X(a.Anno) - 6, area.Bottom + 4);

        // Linea dello zero e del capitale investito
        using (var zero = new Pen(Color.Gray, 1))
            g.DrawLine(zero, area.Left, Y(0), area.Right, Y(0));
        using (var capitale = new Pen(Color.Gray, 1.5f) { DashStyle = DashStyle.Dash })
            g.DrawLine(capitale, area.Left, Y(_capitaleProprio), area.Right, Y(_capitaleProprio));

        DisegnaSerie(g, _anni.Select(a => a.PatrimonioNetto), Tema.Primario, X, Y);
        DisegnaSerie(g, _anni.Select(a => a.CashFlowCumulato), Tema.Linea2, X, Y);

        // Legenda
        float lx = area.Left;
        lx = Legenda(g, font, lx, "Patrimonio netto (valore − debito)", Tema.Primario, false);
        lx = Legenda(g, font, lx, "Cash flow cumulato", Tema.Linea2, false);
        Legenda(g, font, lx, "Capitale investito", Color.Gray, true);
    }

    private void DisegnaSerie(Graphics g, IEnumerable<double> serie, Color colore, Func<int, float> x, Func<double, float> y)
    {
        var punti = serie.Select((v, i) => new PointF(x(i + 1), y(v))).ToArray();
        using var penna = new Pen(colore, 2.5f);
        if (punti.Length > 1) g.DrawLines(penna, punti);
        using var pennello = new SolidBrush(colore);
        foreach (var p in punti) g.FillEllipse(pennello, p.X - 3, p.Y - 3, 6, 6);
    }

    private static float Legenda(Graphics g, Font font, float x, string testo, Color colore, bool tratteggio)
    {
        using var penna = new Pen(colore, 2.5f) { DashStyle = tratteggio ? DashStyle.Dash : DashStyle.Solid };
        g.DrawLine(penna, x, 14, x + 22, 14);
        using var pennello = new SolidBrush(Color.Black);
        g.DrawString(testo, font, pennello, x + 26, 7);
        return x + 26 + g.MeasureString(testo, font).Width + 18;
    }
}
