using System.Globalization;
using System.Net;
using System.Text;
using AnalisiImmobiliare.Core.Calcolo;
using AnalisiImmobiliare.Core.Modelli;

namespace AnalisiImmobiliare.Core.Servizi;

/// <summary>
/// Esporta i risultati in CSV (per Excel) e in un report HTML
/// che si apre nel browser e si può stampare in PDF.
/// </summary>
public static class Esportatore
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    private static string E(double v) => v.ToString("N0", It) + " €";
    private static string P(double v) => v.ToString("N2", It) + "%";
    private static string P(double? v) => v.HasValue ? P(v.Value) : "n.d.";
    private static string H(string s) => WebUtility.HtmlEncode(s);

    /// <summary>CSV con il punto e virgola, il separatore che Excel in italiano apre direttamente.</summary>
    public static string ProiezioneCsv(RisultatoAnalisi r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Anno;Canone incassato;Spese operative;NOI;Tasse;Rata mutuo;Interessi;Cash flow;Cash flow cumulato;Valore immobile;Debito residuo;Patrimonio netto");
        foreach (var a in r.Proiezione)
        {
            sb.AppendLine(string.Join(";", new[]
            {
                a.Anno.ToString(It), N(a.CanoneIncassato), N(a.SpeseOperative), N(a.Noi), N(a.Tasse),
                N(a.RataMutuo), N(a.QuotaInteressi), N(a.CashFlow), N(a.CashFlowCumulato),
                N(a.ValoreImmobile), N(a.DebitoResiduo), N(a.PatrimonioNetto)
            }));
        }
        return sb.ToString();

        static string N(double v) => v.ToString("F2", It);
    }

    public static string ReportHtml(ScenarioInvestimento s, RisultatoAnalisi r)
    {
        var m = AnalisiMercato.Analizza(s);
        var sb = new StringBuilder();
        sb.Append($$"""
            <!doctype html>
            <html lang="it"><head><meta charset="utf-8">
            <title>Report investimento - {{H(s.Immobile.Nome)}}</title>
            <style>
              body{font-family:Segoe UI,Arial,sans-serif;max-width:900px;margin:24px auto;padding:0 16px;color:#222}
              h1{color:#1f4e79;margin-bottom:0} h2{color:#1f4e79;border-bottom:2px solid #1f4e79;padding-bottom:4px;margin-top:28px}
              table{border-collapse:collapse;width:100%;font-size:14px} td,th{border:1px solid #ccc;padding:5px 8px}
              th{background:#eaf1f8;text-align:left} td.n{text-align:right}
              .kpi{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}
              .kpi div{background:#eaf1f8;border-radius:6px;padding:10px} .kpi b{display:block;font-size:20px;color:#1f4e79}
              .avviso{background:#fff4ce;border-left:4px solid #e0a800;padding:6px 10px;margin:6px 0}
              footer{margin-top:32px;font-size:12px;color:#777}
            </style></head><body>
            <h1>{{H(s.Immobile.Nome)}}</h1>
            <p>{{H(s.Immobile.Indirizzo)}} {{H(s.Immobile.Citta)}} · {{s.Immobile.SuperficieMq.ToString("N0", It)}} m² · Report del {{DateTime.Now.ToString("d MMMM yyyy", It)}}</p>
            <div class="kpi">
              <div>Rendimento lordo<b>{{P(r.RendimentoLordoPerc)}}</b></div>
              <div>Rendimento netto<b>{{P(r.RendimentoNettoPerc)}}</b></div>
              <div>Cash on cash<b>{{P(r.CashOnCashPerc)}}</b></div>
              <div>TIR ({{s.Proiezione.AnniDetenzione}} anni)<b>{{P(r.TirPerc)}}</b></div>
            </div>
            """);

        sb.Append("<h2>Acquisto</h2><table>");
        Riga(sb, "Prezzo", E(s.Acquisto.Prezzo));
        Riga(sb, "Prezzo al m²", E(r.PrezzoAlMq));
        Riga(sb, "Imposte (registro/IVA, ipotecaria, catastale)", E(r.CostiAcquisto.TotaleImposte));
        Riga(sb, "Agenzia (IVA inclusa)", E(r.CostiAcquisto.Agenzia));
        Riga(sb, "Notaio", E(r.CostiAcquisto.Notaio));
        Riga(sb, "Ristrutturazione e arredo", E(s.Acquisto.CostiRistrutturazione + s.Acquisto.ArredamentoEAltro));
        Riga(sb, "Costo complessivo", E(r.CostoComplessivo));
        Riga(sb, "Mutuo erogato", E(r.ImportoMutuo));
        Riga(sb, "Costi del mutuo", E(r.CostiMutuo));
        Riga(sb, "Capitale proprio investito", E(r.CapitaleProprio));
        sb.Append("</table>");

        sb.Append("<h2>Primo anno di gestione</h2><table>");
        Riga(sb, "Canone incassato (al netto dello sfitto)", E(r.CanoneIncassato));
        Riga(sb, "IMU", E(r.Imu));
        Riga(sb, "Spese operative totali", E(r.SpeseOperative));
        Riga(sb, "Reddito operativo netto (NOI)", E(r.Noi));
        Riga(sb, "Tasse sul canone", E(r.Tasse));
        Riga(sb, "Rata annua del mutuo", E(r.RataAnnua));
        Riga(sb, "Cash flow annuo", E(r.CashFlowAnnuo));
        Riga(sb, "DSCR", r.Dscr?.ToString("N2", It) ?? "n.d.");
        Riga(sb, "Anni per rientrare del capitale", r.AnniRientro?.ToString("N1", It) ?? "n.d.");
        sb.Append("</table>");

        sb.Append($"<h2>Uscita dopo {s.Proiezione.AnniDetenzione} anni</h2><table>");
        Riga(sb, "Valore di rivendita stimato", E(r.PrezzoVenditaFinale));
        Riga(sb, "Imposta sulla plusvalenza", E(r.ImpostaPlusvalenza));
        Riga(sb, "Incasso netto dalla vendita (dopo il debito)", E(r.IncassoNettoVendita));
        Riga(sb, "Guadagno totale", E(r.GuadagnoTotale));
        Riga(sb, "Multiplo del capitale", r.MultiploCapitale.ToString("N2", It) + "x");
        Riga(sb, $"VAN al {P(s.Proiezione.RendimentoObiettivoPerc)}", E(r.Van));
        Riga(sb, "Prezzo massimo d'offerta per il rendimento obiettivo", r.PrezzoMassimoOfferta is { } pm ? E(pm) : "n.d.");
        sb.Append("</table>");

        if (m.Vendite.Numero > 0 || m.Affitti.Numero > 0)
        {
            sb.Append("<h2>Confronto con il mercato</h2><table>");
            Riga(sb, $"Mediana vendite ({m.Vendite.Numero} comparabili)", E(m.Vendite.Mediana) + "/m²");
            Riga(sb, "Valore stimato dell'immobile", E(m.ValoreStimato));
            Riga(sb, "Scostamento del prezzo richiesto", P(m.ScostamentoPrezzoPerc));
            Riga(sb, $"Mediana affitti ({m.Affitti.Numero} comparabili)", m.Affitti.Mediana.ToString("N2", It) + " €/m² al mese");
            Riga(sb, "Canone di mercato stimato", E(m.CanoneStimato));
            Riga(sb, "Rendimento lordo medio della zona", P(m.RendimentoLordoZonaPerc));
            sb.Append("</table>");
        }

        sb.Append("<h2>Proiezione anno per anno</h2><table><tr><th>Anno</th><th>Canone</th><th>Spese</th><th>Tasse</th><th>Rata</th><th>Cash flow</th><th>Cumulato</th><th>Valore</th><th>Patrimonio netto</th></tr>");
        foreach (var a in r.Proiezione)
            sb.Append($"<tr><td>{a.Anno}</td><td class=n>{E(a.CanoneIncassato)}</td><td class=n>{E(a.SpeseOperative)}</td><td class=n>{E(a.Tasse)}</td><td class=n>{E(a.RataMutuo)}</td><td class=n>{E(a.CashFlow)}</td><td class=n>{E(a.CashFlowCumulato)}</td><td class=n>{E(a.ValoreImmobile)}</td><td class=n>{E(a.PatrimonioNetto)}</td></tr>");
        sb.Append("</table>");

        if (r.Avvisi.Count > 0)
        {
            sb.Append("<h2>Punti di attenzione</h2>");
            foreach (var a in r.Avvisi) sb.Append($"<div class=avviso>{H(a)}</div>");
        }

        sb.Append("<footer>Analisi Investimenti Immobiliari · Marco Consiglio · Parametri fiscali 2026. " +
                  "Le stime hanno scopo didattico e non sostituiscono il parere di un professionista.</footer></body></html>");
        return sb.ToString();
    }

    private static void Riga(StringBuilder sb, string etichetta, string valore) =>
        sb.Append($"<tr><th>{H(etichetta)}</th><td class=n>{H(valore)}</td></tr>");
}
