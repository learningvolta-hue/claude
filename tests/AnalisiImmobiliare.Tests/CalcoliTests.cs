using AnalisiImmobiliare.Core.Calcolo;
using AnalisiImmobiliare.Core.Modelli;
using AnalisiImmobiliare.Core.Servizi;
using Xunit;

namespace AnalisiImmobiliare.Tests;

public class CalcoloMutuoTests
{
    [Fact]
    public void RataMensile_CorrispondeAllaFormulaFrancese()
    {
        // 100.000 € al 3% per 20 anni: rata nota di 554,60 €
        Assert.Equal(554.60, CalcoloMutuo.CalcolaRata(100_000, 3, 20), 2);
    }

    [Fact]
    public void RataMensile_TassoZero_DivideIlCapitale()
    {
        Assert.Equal(100_000 / 240.0, CalcoloMutuo.CalcolaRata(100_000, 0, 20), 6);
    }

    [Fact]
    public void Piano_RestituisceTuttoIlCapitale()
    {
        var mutuo = new CalcoloMutuo(120_000, 3.5, 25);
        Assert.Equal(25, mutuo.Piano.Count);
        Assert.Equal(120_000, mutuo.Piano.Sum(a => a.QuotaCapitale), 2);
        Assert.Equal(0, mutuo.DebitoResiduoFineAnno(25), 2);
        Assert.Equal(0, mutuo.DebitoResiduoFineAnno(30));
    }
}

public class CalcoloImposteTests
{
    [Fact]
    public void Registro_DaPrivato_SecondaCasa_9PercentoSulValoreCatastale()
    {
        var costi = CalcoloImposte.CostiAcquisto(
            new DatiImmobile { RenditaCatastale = 600 },
            new DatiAcquisto { Prezzo = 150_000, ProvvigioneAgenziaPerc = 0, SpeseNotaio = 0 });

        Assert.Equal(75_600, costi.BaseImponibile, 2);        // 600 × 1,05 × 120
        Assert.Equal(6_804, costi.ImpostaRegistro, 2);
        Assert.Equal(6_904, costi.TotaleImposte, 2);           // + 50 + 50
    }

    [Fact]
    public void Registro_PrimaCasa_RispettaIlMinimoDi1000Euro()
    {
        var costi = CalcoloImposte.CostiAcquisto(
            new DatiImmobile { RenditaCatastale = 300 },
            new DatiAcquisto { Prezzo = 80_000, PrimaCasa = true, ProvvigioneAgenziaPerc = 0, SpeseNotaio = 0 });

        Assert.Equal(1_000, costi.ImpostaRegistro, 2);         // 693 € < minimo
    }

    [Fact]
    public void DaImpresa_IvaAl10PercentoEImposteFisse()
    {
        var costi = CalcoloImposte.CostiAcquisto(
            new DatiImmobile { RenditaCatastale = 600 },
            new DatiAcquisto { Prezzo = 200_000, Venditore = TipoVenditore.ImpresaConIva, ProvvigioneAgenziaPerc = 3, SpeseNotaio = 0 });

        Assert.Equal(20_000, costi.Iva, 2);
        Assert.Equal(20_600, costi.TotaleImposte, 2);
        Assert.Equal(7_320, costi.Agenzia, 2);                 // 6.000 + IVA 22%
    }

    [Fact]
    public void Imu_ConCanoneConcordatoRidottaDel25Percento()
    {
        Assert.Equal(1_068.48, CalcoloImposte.Imu(600, 10.6, false), 2);
        Assert.Equal(801.36, CalcoloImposte.Imu(600, 10.6, true), 2);
    }

    [Fact]
    public void TasseSulCanone_CedolareEIrpef()
    {
        Assert.Equal(2_100, CalcoloImposte.TasseSulCanone(10_000, new DatiFiscali { AliquotaCedolarePerc = 21 }), 2);

        var irpef = new DatiFiscali { Regime = RegimeFiscale.Irpef, AliquotaMarginaleIrpefPerc = 33, AddizionaliIrpefPerc = 2 };
        // 10.000 × 95% × 35% + 1% di registro
        Assert.Equal(3_425, CalcoloImposte.TasseSulCanone(10_000, irpef), 2);
    }

    [Fact]
    public void Plusvalenza_TassataSoloEntroCinqueAnni()
    {
        Assert.Equal(2_600, CalcoloImposte.ImpostaPlusvalenza(110_000, 100_000, 3), 2);
        Assert.Equal(0, CalcoloImposte.ImpostaPlusvalenza(110_000, 100_000, 5));
    }
}

public class IndicatoriFinanziariTests
{
    [Fact]
    public void Tir_DiUnObbligazioneAlDieciPercento()
    {
        var tir = IndicatoriFinanziari.Tir(new double[] { -1_000, 100, 100, 1_100 });
        Assert.NotNull(tir);
        Assert.Equal(10, tir!.Value, 4);
    }

    [Fact]
    public void Tir_NonEsisteSeIFlussiSonoTuttiPositivi()
    {
        Assert.Null(IndicatoriFinanziari.Tir(new double[] { 100, 100 }));
    }

    [Fact]
    public void Van_AlTassoDelTirEZero()
    {
        Assert.Equal(0, IndicatoriFinanziari.Van(new double[] { -1_000, 100, 100, 1_100 }, 10), 6);
    }
}

public class AnalisiMercatoTests
{
    [Fact]
    public void Statistiche_CalcolaLaMedianaConNumeroPari()
    {
        var st = AnalisiMercato.Statistiche(new double[] { 2_000, 1_000, 4_000, 3_000 });
        Assert.Equal(2_500, st.Mediana);
        Assert.Equal(1_000, st.Minimo);
        Assert.Equal(4_000, st.Massimo);
    }

    [Fact]
    public void Analizza_StimaValoreECanoneDaiComparabili()
    {
        var s = new ScenarioInvestimento
        {
            Immobile = { SuperficieMq = 100 },
            Acquisto = { Prezzo = 220_000 },
            Affitto = { CanoneMensile = 1_000 },
            Comparabili =
            {
                new Comparabile { Tipo = TipoComparabile.Vendita, Prezzo = 200_000, SuperficieMq = 100 },
                new Comparabile { Tipo = TipoComparabile.Affitto, Prezzo = 1_000, SuperficieMq = 100 }
            }
        };
        var m = AnalisiMercato.Analizza(s);

        Assert.Equal(200_000, m.ValoreStimato, 2);
        Assert.Equal(10, m.ScostamentoPrezzoPerc, 6);
        Assert.Equal(1_000, m.CanoneStimato, 2);
        Assert.Equal(6, m.RendimentoLordoZonaPerc, 6);
    }
}

public class AnalizzatoreInvestimentoTests
{
    [Fact]
    public void SenzaMutuo_CashOnCashUgualeAlRendimentoNetto()
    {
        var s = new ScenarioInvestimento { Mutuo = { Attivo = false } };
        var r = AnalizzatoreInvestimento.Analizza(s);

        Assert.Equal(r.CostoComplessivo, r.CapitaleProprio, 6);
        Assert.Equal(r.RendimentoNettoPerc, r.CashOnCashPerc, 6);
        Assert.Null(r.Dscr);
    }

    [Fact]
    public void ScenarioPredefinito_ProduceRisultatiCoerenti()
    {
        var s = new ScenarioInvestimento();
        var r = AnalizzatoreInvestimento.Analizza(s);

        Assert.Equal(6.0, r.RendimentoLordoPerc, 6);            // 9.000 / 150.000
        Assert.Equal(s.Proiezione.AnniDetenzione, r.Proiezione.Count);
        Assert.Equal(105_000, r.ImportoMutuo, 2);
        Assert.Equal(r.CostoComplessivo + r.CostiMutuo - r.ImportoMutuo, r.CapitaleProprio, 6);
        Assert.NotNull(r.TirPerc);
        Assert.Equal(r.Proiezione.Sum(p => p.CashFlow), r.Proiezione[^1].CashFlowCumulato, 6);
    }

    [Fact]
    public void PrezzoMassimoOfferta_RaggiungeIlRendimentoObiettivo()
    {
        var s = new ScenarioInvestimento { Proiezione = { RendimentoObiettivoPerc = 4 } };
        var prezzo = AnalizzatoreInvestimento.PrezzoMassimoOfferta(s);
        Assert.NotNull(prezzo);

        s.Acquisto.Prezzo = prezzo!.Value;
        var r = AnalizzatoreInvestimento.Analizza(s);
        Assert.Equal(4, r.RendimentoNettoPerc, 3);
    }

    [Fact]
    public void Avvisi_SegnalanoRenditaMancante()
    {
        var s = new ScenarioInvestimento { Immobile = { RenditaCatastale = 0 } };
        var r = AnalizzatoreInvestimento.Analizza(s);
        Assert.Contains(r.Avvisi, a => a.Contains("Rendita catastale"));
    }
}

public class ArchivioTests
{
    [Fact]
    public void Json_AndataERitornoConservaIDati()
    {
        var s = new ScenarioInvestimento
        {
            Immobile = { Nome = "Bilocale Centro", Citta = "Milano" },
            Fisco = { Regime = RegimeFiscale.Irpef },
            Comparabili = { new Comparabile { Descrizione = "Annuncio 1", Prezzo = 180_000, SuperficieMq = 60 } }
        };

        var copia = ArchivioScenari.DaJson(ArchivioScenari.InJson(s));

        Assert.Equal("Bilocale Centro", copia.Immobile.Nome);
        Assert.Equal(RegimeFiscale.Irpef, copia.Fisco.Regime);
        Assert.Single(copia.Comparabili);
        Assert.Equal(3_000, copia.Comparabili[0].PrezzoAlMq, 6);
    }

    [Fact]
    public void ReportHtml_ContieneAutoreENome()
    {
        var s = new ScenarioInvestimento { Immobile = { Nome = "Trilocale <Test>" } };
        var html = Esportatore.ReportHtml(s, AnalizzatoreInvestimento.Analizza(s));
        Assert.Contains("Marco Consiglio", html);
        Assert.Contains("Trilocale &lt;Test&gt;", html);
    }
}

public class EsempiTests
{
    [Fact]
    public void FileDiEsempio_SiApreESiAnalizza()
    {
        var percorso = Path.Combine(AppContext.BaseDirectory, "esempi", "Bilocale-esempio.immo");
        var s = ArchivioScenari.Apri(percorso);
        var r = AnalizzatoreInvestimento.Analizza(s);
        var m = AnalisiMercato.Analizza(s);

        Assert.Equal(7, s.Comparabili.Count);
        Assert.Equal(4, m.Vendite.Numero);
        Assert.Equal(3, m.Affitti.Numero);
        Assert.True(r.RendimentoLordoPerc > 0);
        Assert.NotNull(r.TirPerc);
    }
}
