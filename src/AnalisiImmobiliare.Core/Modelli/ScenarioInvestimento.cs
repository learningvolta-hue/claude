namespace AnalisiImmobiliare.Core.Modelli;

/// <summary>Chi vende l'immobile: cambia il modo in cui si pagano le imposte d'acquisto.</summary>
public enum TipoVenditore
{
    /// <summary>Privato (o impresa esente IVA): imposta di registro.</summary>
    Privato,
    /// <summary>Impresa costruttrice con vendita soggetta a IVA.</summary>
    ImpresaConIva
}

/// <summary>Regime con cui vengono tassati i canoni d'affitto.</summary>
public enum RegimeFiscale
{
    /// <summary>Imposta sostitutiva ad aliquota fissa (21% ordinaria, 10% canone concordato).</summary>
    CedolareSecca,
    /// <summary>Tassazione ordinaria: il canone si somma agli altri redditi.</summary>
    Irpef
}

/// <summary>
/// Raccoglie tutti i dati di un'ipotesi di investimento immobiliare.
/// I dati sono divisi in sezioni, così come appaiono nell'interfaccia.
/// </summary>
public class ScenarioInvestimento
{
    public DatiImmobile Immobile { get; set; } = new();
    public DatiAcquisto Acquisto { get; set; } = new();
    public DatiAffitto Affitto { get; set; } = new();
    public DatiSpese Spese { get; set; } = new();
    public DatiMutuo Mutuo { get; set; } = new();
    public DatiFiscali Fisco { get; set; } = new();
    public DatiProiezione Proiezione { get; set; } = new();
    public List<Comparabile> Comparabili { get; set; } = new();
}

public class DatiImmobile
{
    public string Nome { get; set; } = "Nuovo immobile";
    public string Indirizzo { get; set; } = "";
    public string Citta { get; set; } = "";
    public double SuperficieMq { get; set; } = 70;
    /// <summary>Rendita catastale (dalla visura). Serve per registro e IMU.</summary>
    public double RenditaCatastale { get; set; } = 600;
    public string Note { get; set; } = "";
}

public class DatiAcquisto
{
    public double Prezzo { get; set; } = 150_000;
    public TipoVenditore Venditore { get; set; } = TipoVenditore.Privato;
    /// <summary>Le agevolazioni "prima casa" valgono solo se è la propria abitazione principale.</summary>
    public bool PrimaCasa { get; set; }
    /// <summary>Provvigione dell'agenzia in percentuale sul prezzo (IVA esclusa).</summary>
    public double ProvvigioneAgenziaPerc { get; set; } = 3;
    public double SpeseNotaio { get; set; } = 2_500;
    public double CostiRistrutturazione { get; set; }
    public double ArredamentoEAltro { get; set; }
}

public class DatiAffitto
{
    public double CanoneMensile { get; set; } = 750;
    /// <summary>Mesi all'anno in cui l'immobile resta vuoto (cambio inquilino, morosità).</summary>
    public double MesiSfittoAnnui { get; set; } = 1;
    /// <summary>Contratto a canone concordato (3+2): cedolare al 10% e IMU ridotta del 25%.</summary>
    public bool CanoneConcordato { get; set; }
}

public class DatiSpese
{
    /// <summary>Aliquota IMU in per mille: 8,6‰ è la base, i Comuni possono arrivare a 10,6‰.</summary>
    public double AliquotaImuPerMille { get; set; } = 10.6;
    /// <summary>Spese condominiali a carico del proprietario (non ripetibili all'inquilino).</summary>
    public double CondominioProprietarioAnnuo { get; set; } = 400;
    /// <summary>Accantonamento per manutenzione, in percentuale sul canone incassato.</summary>
    public double ManutenzionePercCanone { get; set; } = 5;
    public double AssicurazioneAnnua { get; set; } = 200;
    /// <summary>Costo di un'eventuale agenzia di gestione, in percentuale sul canone incassato.</summary>
    public double GestionePercCanone { get; set; }
}

public class DatiMutuo
{
    public bool Attivo { get; set; } = true;
    /// <summary>Loan To Value: percentuale del prezzo finanziata dalla banca.</summary>
    public double PercentualeFinanziata { get; set; } = 70;
    public double TassoAnnuoPerc { get; set; } = 3.3;
    public int DurataAnni { get; set; } = 25;
    public double SpeseIstruttoria { get; set; } = 1_000;
    public double SpesePerizia { get; set; } = 300;
}

public class DatiFiscali
{
    public RegimeFiscale Regime { get; set; } = RegimeFiscale.CedolareSecca;
    public double AliquotaCedolarePerc { get; set; } = 21;
    /// <summary>Aliquota IRPEF più alta raggiunta dal proprietario (23%, 33% o 43% nel 2026).</summary>
    public double AliquotaMarginaleIrpefPerc { get; set; } = 33;
    /// <summary>Addizionali regionale e comunale, in media circa 2%.</summary>
    public double AddizionaliIrpefPerc { get; set; } = 2;
}

public class DatiProiezione
{
    public int AnniDetenzione { get; set; } = 10;
    public double RivalutazioneAnnuaPerc { get; set; } = 2;
    public double CrescitaCanoneAnnuaPerc { get; set; } = 2;
    public double InflazioneSpeseAnnuaPerc { get; set; } = 2;
    /// <summary>Costi della rivendita finale (agenzia, certificazioni), in percentuale sul valore.</summary>
    public double CostiVenditaPerc { get; set; } = 3;
    /// <summary>Rendimento minimo desiderato: usato per il VAN e per il prezzo massimo d'offerta.</summary>
    public double RendimentoObiettivoPerc { get; set; } = 5;
}

public enum TipoComparabile { Vendita, Affitto }

/// <summary>
/// Un annuncio o una compravendita di un immobile simile nella stessa zona.
/// Per la vendita il prezzo è quello totale; per l'affitto è il canone mensile.
/// </summary>
public class Comparabile
{
    public string Descrizione { get; set; } = "";
    public TipoComparabile Tipo { get; set; }
    public double Prezzo { get; set; }
    public double SuperficieMq { get; set; }

    public double PrezzoAlMq => SuperficieMq > 0 ? Prezzo / SuperficieMq : 0;
}
