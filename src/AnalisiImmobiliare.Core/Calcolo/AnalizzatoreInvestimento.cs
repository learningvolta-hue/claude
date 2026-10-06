using AnalisiImmobiliare.Core.Modelli;

namespace AnalisiImmobiliare.Core.Calcolo;

/// <summary>Una riga della proiezione pluriennale.</summary>
public record AnnoProiezione(
    int Anno,
    double CanoneIncassato,
    double SpeseOperative,
    double Noi,
    double Tasse,
    double RataMutuo,
    double QuotaInteressi,
    double CashFlow,
    double CashFlowCumulato,
    double ValoreImmobile,
    double DebitoResiduo,
    double PatrimonioNetto);

/// <summary>Tutti i risultati dell'analisi di uno scenario.</summary>
public class RisultatoAnalisi
{
    // Acquisto
    public required CostiAcquisto CostiAcquisto { get; init; }
    public double CostoComplessivo { get; init; }      // prezzo + costi + lavori + arredo
    public double ImportoMutuo { get; init; }
    public double CostiMutuo { get; init; }            // sostitutiva + istruttoria + perizia
    public double CapitaleProprio { get; init; }       // soldi che escono dalle tue tasche
    public required CalcoloMutuo Mutuo { get; init; }

    // Primo anno
    public double CanoneLordoAnnuo { get; init; }
    public double CanoneIncassato { get; init; }
    public double Imu { get; init; }
    public double SpeseOperative { get; init; }
    public double Noi { get; init; }
    public double Tasse { get; init; }
    public double RataAnnua { get; init; }
    public double CashFlowAnnuo { get; init; }
    public double CashFlowMensile => CashFlowAnnuo / 12;

    // Indicatori
    public double PrezzoAlMq { get; init; }
    public double RendimentoLordoPerc { get; init; }
    public double RendimentoNettoPerc { get; init; }
    public double CashOnCashPerc { get; init; }
    public double? Dscr { get; init; }
    public double? AnniRientro { get; init; }

    // Proiezione e uscita
    public required IReadOnlyList<AnnoProiezione> Proiezione { get; init; }
    public double PrezzoVenditaFinale { get; init; }
    public double ImpostaPlusvalenza { get; init; }
    public double IncassoNettoVendita { get; init; }
    public double GuadagnoTotale { get; init; }
    public double MultiploCapitale { get; init; }
    public double? TirPerc { get; init; }
    public double Van { get; init; }
    public double? PrezzoMassimoOfferta { get; init; }

    public required IReadOnlyList<string> Avvisi { get; init; }
}

/// <summary>
/// Il "cuore" del programma: prende uno scenario e calcola tutti gli indicatori.
/// Non conosce l'interfaccia grafica, quindi si può verificare con test automatici.
/// </summary>
public static class AnalizzatoreInvestimento
{
    public static RisultatoAnalisi Analizza(ScenarioInvestimento s)
    {
        var acq = s.Acquisto;
        var costi = CalcoloImposte.CostiAcquisto(s.Immobile, acq);
        double costoComplessivo = acq.Prezzo + costi.Totale + acq.CostiRistrutturazione + acq.ArredamentoEAltro;

        // --- Finanziamento ---
        double importoMutuo = s.Mutuo.Attivo ? acq.Prezzo * s.Mutuo.PercentualeFinanziata / 100 : 0;
        var mutuo = new CalcoloMutuo(importoMutuo, s.Mutuo.TassoAnnuoPerc, s.Mutuo.Attivo ? s.Mutuo.DurataAnni : 0);
        double costiMutuo = importoMutuo > 0
            ? CalcoloImposte.ImpostaSostitutivaMutuo(importoMutuo, acq.PrimaCasa) + s.Mutuo.SpeseIstruttoria + s.Mutuo.SpesePerizia
            : 0;
        double capitaleProprio = costoComplessivo + costiMutuo - importoMutuo;

        // --- Proiezione anno per anno ---
        double imu = CalcoloImposte.Imu(s.Immobile.RenditaCatastale, s.Spese.AliquotaImuPerMille, s.Affitto.CanoneConcordato);
        int anni = Math.Max(1, s.Proiezione.AnniDetenzione);
        var proiezione = new List<AnnoProiezione>();
        double cumulato = 0;

        for (int anno = 1; anno <= anni; anno++)
        {
            double crescita = Math.Pow(1 + s.Proiezione.CrescitaCanoneAnnuaPerc / 100, anno - 1);
            double inflazione = Math.Pow(1 + s.Proiezione.InflazioneSpeseAnnuaPerc / 100, anno - 1);

            double canone = s.Affitto.CanoneMensile * Math.Clamp(12 - s.Affitto.MesiSfittoAnnui, 0, 12) * crescita;
            double spese = SpeseOperative(s.Spese, imu, canone, inflazione);
            double noi = canone - spese;
            double tasse = CalcoloImposte.TasseSulCanone(canone, s.Fisco);
            var rata = mutuo.Anno(anno);
            double cashFlow = noi - tasse - rata.Rate;
            cumulato += cashFlow;

            double valore = acq.Prezzo * Math.Pow(1 + s.Proiezione.RivalutazioneAnnuaPerc / 100, anno);
            double debito = mutuo.DebitoResiduoFineAnno(anno);

            proiezione.Add(new AnnoProiezione(anno, canone, spese, noi, tasse, rata.Rate, rata.QuotaInteressi,
                cashFlow, cumulato, valore, debito, valore - debito));
        }

        // --- Rivendita alla fine del periodo ---
        var ultimo = proiezione[^1];
        double costiVendita = ultimo.ValoreImmobile * s.Proiezione.CostiVenditaPerc / 100;
        double impostaPlus = CalcoloImposte.ImpostaPlusvalenza(ultimo.ValoreImmobile - costiVendita, costoComplessivo, anni);
        double incassoNetto = ultimo.ValoreImmobile - costiVendita - impostaPlus - ultimo.DebitoResiduo;

        var flussi = new List<double> { -capitaleProprio };
        flussi.AddRange(proiezione.Select(p => p.CashFlow));
        flussi[^1] += incassoNetto;

        double guadagno = flussi.Sum();
        var primo = proiezione[0];
        var avvisi = new List<string>();

        var risultato = new RisultatoAnalisi
        {
            CostiAcquisto = costi,
            CostoComplessivo = costoComplessivo,
            ImportoMutuo = importoMutuo,
            CostiMutuo = costiMutuo,
            CapitaleProprio = capitaleProprio,
            Mutuo = mutuo,

            CanoneLordoAnnuo = s.Affitto.CanoneMensile * 12,
            CanoneIncassato = primo.CanoneIncassato,
            Imu = imu,
            SpeseOperative = primo.SpeseOperative,
            Noi = primo.Noi,
            Tasse = primo.Tasse,
            RataAnnua = primo.RataMutuo,
            CashFlowAnnuo = primo.CashFlow,

            PrezzoAlMq = s.Immobile.SuperficieMq > 0 ? acq.Prezzo / s.Immobile.SuperficieMq : 0,
            RendimentoLordoPerc = Percentuale(s.Affitto.CanoneMensile * 12, acq.Prezzo),
            RendimentoNettoPerc = Percentuale(primo.Noi - primo.Tasse, costoComplessivo),
            CashOnCashPerc = Percentuale(primo.CashFlow, capitaleProprio),
            Dscr = primo.RataMutuo > 0 ? primo.Noi / primo.RataMutuo : null,
            AnniRientro = primo.CashFlow > 0 && capitaleProprio > 0 ? capitaleProprio / primo.CashFlow : null,

            Proiezione = proiezione,
            PrezzoVenditaFinale = ultimo.ValoreImmobile,
            ImpostaPlusvalenza = impostaPlus,
            IncassoNettoVendita = incassoNetto,
            GuadagnoTotale = guadagno,
            MultiploCapitale = capitaleProprio > 0 ? (guadagno + capitaleProprio) / capitaleProprio : 0,
            TirPerc = IndicatoriFinanziari.Tir(flussi),
            Van = IndicatoriFinanziari.Van(flussi, s.Proiezione.RendimentoObiettivoPerc),
            PrezzoMassimoOfferta = PrezzoMassimoOfferta(s),
            Avvisi = avvisi
        };

        AggiungiAvvisi(s, risultato, avvisi);
        return risultato;
    }

    /// <summary>
    /// Segnalazioni didattiche: evidenziano dati mancanti o situazioni da approfondire.
    /// </summary>
    private static void AggiungiAvvisi(ScenarioInvestimento s, RisultatoAnalisi r, List<string> avvisi)
    {
        if (s.Immobile.RenditaCatastale <= 0)
            avvisi.Add("Rendita catastale assente: l'IMU non è calcolata e il registro è stimato sul prezzo. Trovi la rendita nella visura catastale.");
        if (s.Acquisto.PrimaCasa)
            avvisi.Add("Hai scelto le agevolazioni 'prima casa': spettano solo se l'immobile diventa la tua abitazione principale, non per un investimento da affittare.");
        if (s.Mutuo.Attivo && s.Mutuo.PercentualeFinanziata > 80)
            avvisi.Add("Le banche difficilmente finanziano oltre l'80% del valore per un immobile da mettere a reddito.");
        if (r.CashFlowAnnuo < 0)
            avvisi.Add($"Il cash flow del primo anno è negativo ({r.CashFlowMensile:N0} € al mese): dovrai integrare la rata con altri soldi.");
        if (r.Dscr is < 1.2)
            avvisi.Add($"DSCR pari a {r.Dscr:N2}: il reddito netto copre a fatica la rata. Le banche preferiscono valori sopra 1,2.");
        if (s.Affitto.CanoneConcordato && s.Fisco.Regime == RegimeFiscale.CedolareSecca
            && s.Fisco.AliquotaCedolarePerc > ParametriFiscali2026.CedolareConcordato)
            avvisi.Add("Con il canone concordato la cedolare secca scende al 10%: controlla l'aliquota inserita.");
        if (s.Affitto.MesiSfittoAnnui <= 0)
            avvisi.Add("Nessun mese di sfitto previsto: è un'ipotesi ottimistica, meglio prevedere almeno mezzo mese all'anno.");

        var mercato = AnalisiMercato.Analizza(s);
        if (mercato.ValoreStimato > 0 && mercato.ScostamentoPrezzoPerc > 10)
            avvisi.Add($"Il prezzo è del {mercato.ScostamentoPrezzoPerc:N0}% sopra la mediana dei comparabili: c'è margine per trattare.");
        if (mercato.CanoneStimato > 0 && mercato.ScostamentoCanonePerc > 15)
            avvisi.Add($"Il canone ipotizzato è del {mercato.ScostamentoCanonePerc:N0}% sopra quello di mercato: potrebbe essere difficile da ottenere.");
    }

    /// <summary>
    /// Spese annue di gestione: IMU, condominio a carico del proprietario,
    /// manutenzione, assicurazione e gestione.
    /// </summary>
    private static double SpeseOperative(DatiSpese sp, double imu, double canone, double inflazione) =>
        imu
        + (sp.CondominioProprietarioAnnuo + sp.AssicurazioneAnnua) * inflazione
        + canone * (sp.ManutenzionePercCanone + sp.GestionePercCanone) / 100;

    private static double Percentuale(double valore, double totale) => totale > 0 ? valore / totale * 100 : 0;

    /// <summary>
    /// Prezzo più alto che si può pagare per ottenere il rendimento netto obiettivo.
    /// Il prezzo cambia anche le imposte d'acquisto, quindi si cerca per bisezione.
    /// </summary>
    public static double? PrezzoMassimoOfferta(ScenarioInvestimento s)
    {
        double obiettivo = s.Proiezione.RendimentoObiettivoPerc;
        if (obiettivo <= 0) return null;

        double imu = CalcoloImposte.Imu(s.Immobile.RenditaCatastale, s.Spese.AliquotaImuPerMille, s.Affitto.CanoneConcordato);
        double canone = s.Affitto.CanoneMensile * Math.Clamp(12 - s.Affitto.MesiSfittoAnnui, 0, 12);
        double redditoNetto = canone - SpeseOperative(s.Spese, imu, canone, 1) - CalcoloImposte.TasseSulCanone(canone, s.Fisco);
        if (redditoNetto <= 0) return null;

        double RendimentoA(double prezzo)
        {
            var acq = new DatiAcquisto
            {
                Prezzo = prezzo,
                Venditore = s.Acquisto.Venditore,
                PrimaCasa = s.Acquisto.PrimaCasa,
                ProvvigioneAgenziaPerc = s.Acquisto.ProvvigioneAgenziaPerc,
                SpeseNotaio = s.Acquisto.SpeseNotaio
            };
            double costo = prezzo + CalcoloImposte.CostiAcquisto(s.Immobile, acq).Totale
                           + s.Acquisto.CostiRistrutturazione + s.Acquisto.ArredamentoEAltro;
            return redditoNetto / costo * 100;
        }

        double basso = 0, alto = redditoNetto / (obiettivo / 100);   // oltre questo prezzo è impossibile
        if (RendimentoA(1) < obiettivo) return null;                 // i soli costi fissi bastano a sforare
        for (int i = 0; i < 100; i++)
        {
            double medio = (basso + alto) / 2;
            if (RendimentoA(medio) >= obiettivo) basso = medio; else alto = medio;
        }
        return basso;
    }
}
