namespace AnalisiImmobiliare.Core.Calcolo;

/// <summary>Formule finanziarie generiche: VAN e TIR.</summary>
public static class IndicatoriFinanziari
{
    /// <summary>
    /// Valore Attuale Netto: somma dei flussi di cassa "riportati a oggi".
    /// Un euro tra un anno vale meno di un euro oggi: si divide per (1 + r)^anno.
    /// flussi[0] è l'esborso iniziale (negativo), flussi[1..] i flussi degli anni successivi.
    /// </summary>
    public static double Van(IReadOnlyList<double> flussi, double tassoPerc)
    {
        double r = tassoPerc / 100;
        double van = 0;
        for (int t = 0; t < flussi.Count; t++)
            van += flussi[t] / Math.Pow(1 + r, t);
        return van;
    }

    /// <summary>
    /// Tasso Interno di Rendimento: il tasso che rende il VAN uguale a zero.
    /// Si trova per bisezione: si dimezza l'intervallo finché il VAN non si annulla.
    /// Restituisce null se il TIR non esiste (es. flussi tutti positivi o tutti negativi).
    /// </summary>
    public static double? Tir(IReadOnlyList<double> flussi)
    {
        if (flussi.Count < 2 || !flussi.Any(f => f < 0) || !flussi.Any(f => f > 0))
            return null;

        double basso = -99.0, alto = 1_000.0;   // percentuali
        double vanBasso = Van(flussi, basso);
        double vanAlto = Van(flussi, alto);
        if (Math.Sign(vanBasso) == Math.Sign(vanAlto)) return null;

        for (int i = 0; i < 200; i++)
        {
            double medio = (basso + alto) / 2;
            double vanMedio = Van(flussi, medio);
            if (Math.Abs(vanMedio) < 1e-7) return medio;
            if (Math.Sign(vanMedio) == Math.Sign(vanBasso))
            {
                basso = medio;
                vanBasso = vanMedio;
            }
            else
            {
                alto = medio;
            }
        }
        return (basso + alto) / 2;
    }
}
