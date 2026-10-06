namespace AnalisiImmobiliare.Core.Calcolo;

/// <summary>Riepilogo di un anno del piano di ammortamento.</summary>
public record AnnoAmmortamento(int Anno, double Rate, double QuotaInteressi, double QuotaCapitale, double DebitoResiduo);

/// <summary>
/// Mutuo con ammortamento "alla francese": la rata mensile è sempre uguale,
/// all'inizio contiene soprattutto interessi e poi soprattutto capitale.
/// </summary>
public class CalcoloMutuo
{
    public double Importo { get; }
    public double TassoAnnuoPerc { get; }
    public int DurataAnni { get; }
    public double RataMensile { get; }
    public IReadOnlyList<AnnoAmmortamento> Piano { get; }

    public CalcoloMutuo(double importo, double tassoAnnuoPerc, int durataAnni)
    {
        Importo = Math.Max(0, importo);
        TassoAnnuoPerc = tassoAnnuoPerc;
        DurataAnni = Math.Max(0, durataAnni);
        RataMensile = CalcolaRata(Importo, TassoAnnuoPerc, DurataAnni);
        Piano = CostruisciPiano();
    }

    /// <summary>
    /// Formula della rata costante: R = C · i / (1 − (1 + i)^−n),
    /// con C = capitale, i = tasso mensile, n = numero di rate.
    /// </summary>
    public static double CalcolaRata(double capitale, double tassoAnnuoPerc, int anni)
    {
        int n = anni * 12;
        if (capitale <= 0 || n <= 0) return 0;
        double i = tassoAnnuoPerc / 100 / 12;
        if (i == 0) return capitale / n;
        return capitale * i / (1 - Math.Pow(1 + i, -n));
    }

    public double TotaleInteressi => Piano.Sum(a => a.QuotaInteressi);

    /// <summary>Debito ancora da restituire alla fine dell'anno indicato.</summary>
    public double DebitoResiduoFineAnno(int anno)
    {
        if (anno <= 0) return Importo;
        if (anno > Piano.Count) return 0;
        return Piano[anno - 1].DebitoResiduo;
    }

    /// <summary>Dati dell'anno indicato; oltre la durata del mutuo restituisce un anno a zero.</summary>
    public AnnoAmmortamento Anno(int anno) =>
        anno >= 1 && anno <= Piano.Count ? Piano[anno - 1] : new AnnoAmmortamento(anno, 0, 0, 0, 0);

    private List<AnnoAmmortamento> CostruisciPiano()
    {
        var piano = new List<AnnoAmmortamento>();
        double debito = Importo;
        double i = TassoAnnuoPerc / 100 / 12;

        for (int anno = 1; anno <= DurataAnni && Importo > 0; anno++)
        {
            double interessiAnno = 0, capitaleAnno = 0;
            for (int mese = 0; mese < 12; mese++)
            {
                double interessi = debito * i;
                double capitale = Math.Min(RataMensile - interessi, debito);
                debito -= capitale;
                interessiAnno += interessi;
                capitaleAnno += capitale;
            }
            if (debito < 0.005) debito = 0;   // elimina residui di arrotondamento
            piano.Add(new AnnoAmmortamento(anno, interessiAnno + capitaleAnno, interessiAnno, capitaleAnno, debito));
        }
        return piano;
    }
}
