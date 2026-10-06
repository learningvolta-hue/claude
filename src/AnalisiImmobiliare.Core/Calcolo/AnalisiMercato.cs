using AnalisiImmobiliare.Core.Modelli;

namespace AnalisiImmobiliare.Core.Calcolo;

/// <summary>Statistiche sul prezzo al metro quadro di un gruppo di comparabili.</summary>
public record StatisticheMq(int Numero, double Minimo, double Massimo, double Media, double Mediana)
{
    public static readonly StatisticheMq Vuote = new(0, 0, 0, 0, 0);
}

/// <summary>Confronto tra l'immobile analizzato e il mercato della zona.</summary>
public record RisultatoMercato(
    StatisticheMq Vendite,
    StatisticheMq Affitti,
    double PrezzoMqImmobile,
    double ValoreStimato,
    double ScostamentoPrezzoPerc,
    double CanoneStimato,
    double ScostamentoCanonePerc,
    double RendimentoLordoZonaPerc);

/// <summary>
/// Stima del valore con il metodo dei "comparabili": si guarda quanto costano
/// al metro quadro immobili simili nella stessa zona.
/// Si usa la mediana perché un singolo annuncio fuori mercato la sposta poco.
/// </summary>
public static class AnalisiMercato
{
    public static StatisticheMq Statistiche(IEnumerable<double> valoriMq)
    {
        var v = valoriMq.Where(x => x > 0).OrderBy(x => x).ToList();
        if (v.Count == 0) return StatisticheMq.Vuote;
        double mediana = v.Count % 2 == 1
            ? v[v.Count / 2]
            : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2;
        return new StatisticheMq(v.Count, v[0], v[^1], v.Average(), mediana);
    }

    public static RisultatoMercato Analizza(ScenarioInvestimento s)
    {
        var vendite = Statistiche(s.Comparabili.Where(c => c.Tipo == TipoComparabile.Vendita).Select(c => c.PrezzoAlMq));
        var affitti = Statistiche(s.Comparabili.Where(c => c.Tipo == TipoComparabile.Affitto).Select(c => c.PrezzoAlMq));

        double mq = s.Immobile.SuperficieMq;
        double prezzoMq = mq > 0 ? s.Acquisto.Prezzo / mq : 0;

        double valoreStimato = vendite.Mediana * mq;
        double scostPrezzo = valoreStimato > 0 ? (s.Acquisto.Prezzo / valoreStimato - 1) * 100 : 0;

        double canoneStimato = affitti.Mediana * mq;
        double scostCanone = canoneStimato > 0 ? (s.Affitto.CanoneMensile / canoneStimato - 1) * 100 : 0;

        double rendZona = vendite.Mediana > 0 ? affitti.Mediana * 12 / vendite.Mediana * 100 : 0;

        return new RisultatoMercato(vendite, affitti, prezzoMq, valoreStimato, scostPrezzo,
            canoneStimato, scostCanone, rendZona);
    }
}
