using AnalisiImmobiliare.Core.Modelli;
using static AnalisiImmobiliare.Core.Calcolo.ParametriFiscali2026;

namespace AnalisiImmobiliare.Core.Calcolo;

/// <summary>Dettaglio dei costi da sostenere al rogito, oltre al prezzo.</summary>
public record CostiAcquisto(
    double BaseImponibile,
    double ImpostaRegistro,
    double ImpostaIpotecaria,
    double ImpostaCatastale,
    double Iva,
    double Agenzia,
    double Notaio)
{
    public double TotaleImposte => ImpostaRegistro + ImpostaIpotecaria + ImpostaCatastale + Iva;
    public double Totale => TotaleImposte + Agenzia + Notaio;
}

/// <summary>Imposte su acquisto, possesso (IMU) e affitto (cedolare o IRPEF).</summary>
public static class CalcoloImposte
{
    /// <summary>
    /// Valore catastale = rendita × 1,05 × moltiplicatore.
    /// È la base su cui si paga il registro con il sistema "prezzo-valore".
    /// </summary>
    public static double ValoreCatastale(double rendita, bool primaCasa) =>
        rendita * RivalutazioneRendita * (primaCasa ? MoltiplicatorePrimaCasa : MoltiplicatoreAltriImmobili);

    public static CostiAcquisto CostiAcquisto(DatiImmobile immobile, DatiAcquisto acquisto)
    {
        double agenzia = acquisto.Prezzo * acquisto.ProvvigioneAgenziaPerc / 100 * (1 + IvaServizi);

        if (acquisto.Venditore == TipoVenditore.ImpresaConIva)
        {
            // Da impresa con IVA: IVA sul prezzo + tre imposte fisse da 200 €.
            double iva = acquisto.Prezzo * (acquisto.PrimaCasa ? IvaPrimaCasa : IvaAltriImmobili);
            return new CostiAcquisto(acquisto.Prezzo, ImposteFisseDaImpresa, ImposteFisseDaImpresa,
                ImposteFisseDaImpresa, iva, agenzia, acquisto.SpeseNotaio);
        }

        // Da privato: registro sul valore catastale (se la rendita è nota), altrimenti sul prezzo.
        double baseImponibile = immobile.RenditaCatastale > 0
            ? ValoreCatastale(immobile.RenditaCatastale, acquisto.PrimaCasa)
            : acquisto.Prezzo;
        double aliquota = acquisto.PrimaCasa ? RegistroPrimaCasa : RegistroAltriImmobili;
        double registro = Math.Max(RegistroMinimo, baseImponibile * aliquota);

        return new CostiAcquisto(baseImponibile, registro, IpotecariaCatastaleDaPrivato,
            IpotecariaCatastaleDaPrivato, 0, agenzia, acquisto.SpeseNotaio);
    }

    /// <summary>IMU annua: rendita × 1,05 × 160 × aliquota. Ridotta del 25% con canone concordato.</summary>
    public static double Imu(double rendita, double aliquotaPerMille, bool canoneConcordato)
    {
        double imu = rendita * RivalutazioneRendita * MoltiplicatoreImu * aliquotaPerMille / 1000;
        return canoneConcordato ? imu * (1 - RiduzioneImuConcordato) : imu;
    }

    public static double ImpostaSostitutivaMutuo(double importo, bool primaCasa) =>
        importo * (primaCasa ? SostitutivaMutuoPrimaCasa : SostitutivaMutuoAltri);

    /// <summary>Tasse dovute in un anno sul canone incassato, secondo il regime scelto.</summary>
    public static double TasseSulCanone(double canoneIncassato, DatiFiscali fisco)
    {
        if (canoneIncassato <= 0) return 0;
        return fisco.Regime switch
        {
            RegimeFiscale.CedolareSecca => canoneIncassato * fisco.AliquotaCedolarePerc / 100,
            _ => canoneIncassato * QuotaImponibileIrpef
                     * (fisco.AliquotaMarginaleIrpefPerc + fisco.AddizionaliIrpefPerc) / 100
                 + canoneIncassato * RegistroContrattoProprietario
        };
    }

    /// <summary>
    /// Imposta sulla plusvalenza in caso di rivendita entro 5 anni dall'acquisto
    /// (imposta sostitutiva del 26% applicata dal notaio).
    /// </summary>
    public static double ImpostaPlusvalenza(double prezzoVendita, double costoComplessivo, int anniDetenzione)
    {
        if (anniDetenzione >= AnniPlusvalenzaTassata) return 0;
        double plusvalenza = prezzoVendita - costoComplessivo;
        return plusvalenza > 0 ? plusvalenza * SostitutivaPlusvalenza : 0;
    }
}
