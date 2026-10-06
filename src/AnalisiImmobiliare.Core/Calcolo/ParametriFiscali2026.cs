namespace AnalisiImmobiliare.Core.Calcolo;

/// <summary>
/// Costanti fiscali italiane in vigore nel 2026.
/// Le aliquote sono raccolte qui, in un unico posto, così è facile aggiornarle
/// quando cambia la legge di bilancio.
/// </summary>
public static class ParametriFiscali2026
{
    // --- Acquisto da privato (imposta di registro) ---
    public const double RegistroPrimaCasa = 0.02;
    public const double RegistroAltriImmobili = 0.09;
    public const double RegistroMinimo = 1_000;
    public const double IpotecariaCatastaleDaPrivato = 50;   // ciascuna

    // --- Acquisto da impresa con IVA ---
    public const double IvaPrimaCasa = 0.04;
    public const double IvaAltriImmobili = 0.10;
    public const double ImposteFisseDaImpresa = 200;          // registro, ipotecaria, catastale: ciascuna

    // --- Valore catastale = rendita × 1,05 × moltiplicatore ---
    public const double RivalutazioneRendita = 1.05;
    public const double MoltiplicatorePrimaCasa = 110;
    public const double MoltiplicatoreAltriImmobili = 120;
    public const double MoltiplicatoreImu = 160;

    public const double IvaServizi = 0.22;                    // su provvigione agenzia

    // --- Mutuo: imposta sostitutiva sull'importo erogato ---
    public const double SostitutivaMutuoPrimaCasa = 0.0025;
    public const double SostitutivaMutuoAltri = 0.02;

    // --- Affitti ---
    public const double CedolareOrdinaria = 21;
    public const double CedolareConcordato = 10;
    public const double RiduzioneImuConcordato = 0.25;
    /// <summary>Con IRPEF si tassa il 95% del canone (deduzione forfettaria del 5%).</summary>
    public const double QuotaImponibileIrpef = 0.95;
    /// <summary>Registro annuo del contratto: 2% del canone, metà a carico del proprietario.</summary>
    public const double RegistroContrattoProprietario = 0.01;

    // --- Rivendita entro 5 anni: imposta sostitutiva sulla plusvalenza ---
    public const int AnniPlusvalenzaTassata = 5;
    public const double SostitutivaPlusvalenza = 0.26;
}
