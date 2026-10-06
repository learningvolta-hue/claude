using System.ComponentModel;
using System.Diagnostics;
using AnalisiImmobiliare.Core.Calcolo;
using AnalisiImmobiliare.Core.Modelli;
using AnalisiImmobiliare.Core.Servizi;

namespace AnalisiImmobiliare.App;

/// <summary>
/// Finestra principale: a sinistra si inseriscono i dati, a destra i risultati
/// si aggiornano in tempo reale a ogni modifica.
/// </summary>
internal sealed class MainForm : Form
{
    private const string TitoloApp = "Analisi Investimenti Immobiliari";
    private const string FiltroFile = "Scenario immobiliare (*.immo)|*.immo|Tutti i file (*.*)|*.*";

    private ScenarioInvestimento _scenario = new();
    private RisultatoAnalisi? _risultato;
    private string? _percorsoFile;
    private bool _modificato;

    private readonly ToolTip _aiuto = new() { AutoPopDelay = 30_000, InitialDelay = 300, ReshowDelay = 100 };
    private readonly List<ModuloCampi> _moduli = new();
    private ModuloCampi _moduloFisco = null!;

    // Riepilogo
    private readonly Dictionary<string, Label> _kpi = new();
    private readonly FlowLayoutPanel _pannelloAvvisi = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    private readonly ListView _dettaglio = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };

    // Altre schede dei risultati
    private readonly TabControl _schedeRisultati = new() { Dock = DockStyle.Fill };
    private readonly TabPage _schedaGuida = new("Guida agli indicatori");
    private readonly GraficoProiezione _grafico = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _gridProiezione = NuovaGriglia();
    private readonly DataGridView _gridMutuo = NuovaGriglia();
    private readonly DataGridView _gridComparabili = NuovaGriglia(modificabile: true);
    private readonly DataGridView _gridConfronto = NuovaGriglia();
    private readonly ListView _statMercato = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
    private BindingList<Comparabile> _comparabili = new();
    private readonly BindingList<RigaConfronto> _confronto = new();

    private readonly ToolStripStatusLabel _stato = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    public MainForm(string? fileDaAprire)
    {
        Text = TitoloApp;
        Size = new Size(1380, 880);
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var divisore = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 6
        };
        divisore.Panel1.Controls.Add(CreaPannelloDati());
        divisore.Panel2.Controls.Add(CreaPannelloRisultati());

        var stato = new StatusStrip();
        stato.Items.Add(_stato);
        stato.Items.Add(new ToolStripStatusLabel("Parametri fiscali 2026 · Marco Consiglio"));

        Controls.Add(divisore);
        Controls.Add(CreaMenu());
        Controls.Add(stato);
        Load += (_, _) => divisore.SplitterDistance = 460;
        FormClosing += (_, e) => e.Cancel = !ConfermaPerditaModifiche();

        CaricaScenario(new ScenarioInvestimento(), null);
        if (!string.IsNullOrEmpty(fileDaAprire) && File.Exists(fileDaAprire))
            ApriFile(fileDaAprire);
    }

    // =====================================================================
    //  Costruzione dell'interfaccia
    // =====================================================================

    private MenuStrip CreaMenu()
    {
        var menu = new MenuStrip();

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(new ToolStripMenuItem("&Nuovo", null, (_, _) => Nuovo(), Keys.Control | Keys.N));
        file.DropDownItems.Add(new ToolStripMenuItem("&Apri...", null, (_, _) => Apri(), Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripMenuItem("&Salva", null, (_, _) => Salva(false), Keys.Control | Keys.S));
        file.DropDownItems.Add(new ToolStripMenuItem("Salva &con nome...", null, (_, _) => Salva(true), Keys.Control | Keys.Shift | Keys.S));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("Esporta &report (HTML/PDF)...", null, (_, _) => EsportaReport(), Keys.Control | Keys.E));
        file.DropDownItems.Add(new ToolStripMenuItem("Esporta proiezione per &Excel (CSV)...", null, (_, _) => EsportaCsv()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("&Esci", null, (_, _) => Close()));

        var confronto = new ToolStripMenuItem("&Confronto");
        confronto.DropDownItems.Add(new ToolStripMenuItem("Aggiungi lo scenario &attuale", null, (_, _) => AggiungiAlConfronto(), Keys.Control | Keys.K));
        confronto.DropDownItems.Add(new ToolStripMenuItem("Aggiungi scenari da &file...", null, (_, _) => AggiungiFileAlConfronto()));
        confronto.DropDownItems.Add(new ToolStripMenuItem("&Svuota il confronto", null, (_, _) => _confronto.Clear()));

        var aiuto = new ToolStripMenuItem("&Aiuto");
        aiuto.DropDownItems.Add(new ToolStripMenuItem("&Guida agli indicatori", null, (_, _) => _schedeRisultati.SelectedTab = _schedaGuida, Keys.F1));
        aiuto.DropDownItems.Add(new ToolStripMenuItem("&Informazioni", null, (_, _) => MessageBox.Show(this,
            $"{TitoloApp}\nVersione 1.0 – 2026\n\nAutore: Marco Consiglio\n\n" +
            "Strumento didattico per valutare un investimento immobiliare da mettere a reddito.\n" +
            "Le stime non sostituiscono il parere di un notaio, di un commercialista o di un consulente.",
            "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information)));

        menu.Items.AddRange(new ToolStripItem[] { file, confronto, aiuto });
        return menu;
    }

    private Control CreaPannelloDati()
    {
        var schede = new TabControl { Dock = DockStyle.Fill };

        // --- Immobile e acquisto ---
        var m1 = NuovoModulo();
        m1.AggiungiTitolo("Dati dell'immobile");
        m1.AggiungiTesto("Nome / descrizione", () => _scenario.Immobile.Nome, v => { _scenario.Immobile.Nome = v; AggiornaTitolo(); });
        m1.AggiungiTesto("Indirizzo", () => _scenario.Immobile.Indirizzo, v => _scenario.Immobile.Indirizzo = v);
        m1.AggiungiTesto("Città", () => _scenario.Immobile.Citta, v => _scenario.Immobile.Citta = v);
        m1.AggiungiNumero("Superficie commerciale", "m²", () => _scenario.Immobile.SuperficieMq, v => _scenario.Immobile.SuperficieMq = v, 0, 10_000,
            aiuto: "Superficie usata negli annunci: comprende muri e, in parte, balconi e cantina.");
        m1.AggiungiNumero("Rendita catastale", "€", () => _scenario.Immobile.RenditaCatastale, v => _scenario.Immobile.RenditaCatastale = v, 0, 100_000, 2, 10,
            "Si legge nella visura catastale. Serve a calcolare l'imposta di registro e l'IMU.");
        m1.AggiungiTitolo("Acquisto");
        m1.AggiungiNumero("Prezzo d'acquisto", "€", () => _scenario.Acquisto.Prezzo, v => _scenario.Acquisto.Prezzo = v, 0, 50_000_000, 0, 1_000);
        m1.AggiungiScelta("Venditore", new[]
            {
                (TipoVenditore.Privato, "Privato (imposta di registro)"),
                (TipoVenditore.ImpresaConIva, "Impresa costruttrice (con IVA)")
            },
            () => _scenario.Acquisto.Venditore, v => _scenario.Acquisto.Venditore = v,
            "Da privato si paga il registro (9% sul valore catastale); da impresa si paga l'IVA (10%) sul prezzo.");
        m1.AggiungiSpunta("Agevolazioni prima casa", () => _scenario.Acquisto.PrimaCasa, v => _scenario.Acquisto.PrimaCasa = v,
            "Solo se l'immobile diventa la tua abitazione principale: registro al 2% o IVA al 4%.");
        m1.AggiungiNumero("Provvigione agenzia", "% + IVA", () => _scenario.Acquisto.ProvvigioneAgenziaPerc, v => _scenario.Acquisto.ProvvigioneAgenziaPerc = v, 0, 10, 2, 0.5m);
        m1.AggiungiNumero("Notaio (atto e mutuo)", "€", () => _scenario.Acquisto.SpeseNotaio, v => _scenario.Acquisto.SpeseNotaio = v, 0, 100_000, 0, 100);
        m1.AggiungiNumero("Ristrutturazione", "€", () => _scenario.Acquisto.CostiRistrutturazione, v => _scenario.Acquisto.CostiRistrutturazione = v, 0, 10_000_000, 0, 1_000);
        m1.AggiungiNumero("Arredamento e altro", "€", () => _scenario.Acquisto.ArredamentoEAltro, v => _scenario.Acquisto.ArredamentoEAltro = v, 0, 10_000_000, 0, 500);
        AggiungiScheda(schede, "Immobile", m1);

        // --- Affitto e spese ---
        var m2 = NuovoModulo();
        m2.AggiungiTitolo("Affitto");
        m2.AggiungiNumero("Canone mensile", "€", () => _scenario.Affitto.CanoneMensile, v => _scenario.Affitto.CanoneMensile = v, 0, 1_000_000, 0, 25);
        m2.AggiungiNumero("Mesi di sfitto all'anno", "mesi", () => _scenario.Affitto.MesiSfittoAnnui, v => _scenario.Affitto.MesiSfittoAnnui = v, 0, 12, 1, 0.5m,
            "Periodi senza inquilino o con affitto non pagato. Prudenza: almeno 0,5–1 mese all'anno.");
        m2.AggiungiSpunta("Contratto a canone concordato (3+2)", () => _scenario.Affitto.CanoneConcordato, v =>
            {
                _scenario.Affitto.CanoneConcordato = v;
                _scenario.Fisco.AliquotaCedolarePerc = v ? ParametriFiscali2026.CedolareConcordato : ParametriFiscali2026.CedolareOrdinaria;
                BeginInvoke(() => _moduloFisco.AggiornaDaModello());
            },
            "Canone calcolato secondo gli accordi locali: cedolare al 10% e IMU ridotta del 25%, ma affitto spesso più basso.");
        m2.AggiungiTitolo("Spese annue del proprietario");
        m2.AggiungiNumero("Aliquota IMU", "‰", () => _scenario.Spese.AliquotaImuPerMille, v => _scenario.Spese.AliquotaImuPerMille = v, 0, 20, 2, 0.1m,
            "Delibera del Comune: 8,6‰ di base, fino a 10,6‰ per le seconde case.");
        m2.AggiungiNumero("Condominio non ripetibile", "€/anno", () => _scenario.Spese.CondominioProprietarioAnnuo, v => _scenario.Spese.CondominioProprietarioAnnuo = v, 0, 100_000, 0, 50,
            "La parte di spese condominiali che resta al proprietario (es. amministratore, manutenzione straordinaria).");
        m2.AggiungiNumero("Manutenzione", "% canone", () => _scenario.Spese.ManutenzionePercCanone, v => _scenario.Spese.ManutenzionePercCanone = v, 0, 50, 1, 0.5m);
        m2.AggiungiNumero("Assicurazione", "€/anno", () => _scenario.Spese.AssicurazioneAnnua, v => _scenario.Spese.AssicurazioneAnnua = v, 0, 100_000, 0, 50);
        m2.AggiungiNumero("Gestione / property manager", "% canone", () => _scenario.Spese.GestionePercCanone, v => _scenario.Spese.GestionePercCanone = v, 0, 50, 1, 0.5m);
        AggiungiScheda(schede, "Affitto e spese", m2);

        // --- Mutuo ---
        var m3 = NuovoModulo();
        m3.AggiungiTitolo("Finanziamento");
        m3.AggiungiSpunta("Acquisto con mutuo", () => _scenario.Mutuo.Attivo, v => _scenario.Mutuo.Attivo = v);
        m3.AggiungiNumero("Percentuale finanziata (LTV)", "% prezzo", () => _scenario.Mutuo.PercentualeFinanziata, v => _scenario.Mutuo.PercentualeFinanziata = v, 0, 100, 0, 5,
            "Loan To Value: quota del prezzo che presta la banca. Il resto lo metti tu.");
        m3.AggiungiNumero("Tasso annuo (TAN)", "%", () => _scenario.Mutuo.TassoAnnuoPerc, v => _scenario.Mutuo.TassoAnnuoPerc = v, 0, 20, 2, 0.05m);
        m3.AggiungiNumero("Durata", "anni", () => _scenario.Mutuo.DurataAnni, v => _scenario.Mutuo.DurataAnni = (int)v, 1, 40);
        m3.AggiungiNumero("Spese di istruttoria", "€", () => _scenario.Mutuo.SpeseIstruttoria, v => _scenario.Mutuo.SpeseIstruttoria = v, 0, 100_000, 0, 100);
        m3.AggiungiNumero("Perizia", "€", () => _scenario.Mutuo.SpesePerizia, v => _scenario.Mutuo.SpesePerizia = v, 0, 100_000, 0, 50);
        AggiungiScheda(schede, "Mutuo", m3);

        // --- Fisco e proiezione ---
        var m4 = _moduloFisco = NuovoModulo();
        m4.AggiungiTitolo("Tassazione dell'affitto");
        m4.AggiungiScelta("Regime fiscale", new[]
            {
                (RegimeFiscale.CedolareSecca, "Cedolare secca"),
                (RegimeFiscale.Irpef, "IRPEF ordinaria")
            },
            () => _scenario.Fisco.Regime, v => _scenario.Fisco.Regime = v,
            "Cedolare: aliquota fissa, niente addizionali né registro. IRPEF: il canone si somma ai tuoi redditi.");
        m4.AggiungiNumero("Aliquota cedolare secca", "%", () => _scenario.Fisco.AliquotaCedolarePerc, v => _scenario.Fisco.AliquotaCedolarePerc = v, 0, 50, 1, 0.5m,
            "21% per i contratti liberi, 10% per il canone concordato.");
        m4.AggiungiNumero("Tua aliquota IRPEF marginale", "%", () => _scenario.Fisco.AliquotaMarginaleIrpefPerc, v => _scenario.Fisco.AliquotaMarginaleIrpefPerc = v, 0, 60, 0, 1,
            "Scaglioni 2026: 23% fino a 28.000 €, 33% fino a 50.000 €, 43% oltre.");
        m4.AggiungiNumero("Addizionali regionale e comunale", "%", () => _scenario.Fisco.AddizionaliIrpefPerc, v => _scenario.Fisco.AddizionaliIrpefPerc = v, 0, 5, 2, 0.1m);
        m4.AggiungiTitolo("Proiezione e rivendita");
        m4.AggiungiNumero("Anni di detenzione", "anni", () => _scenario.Proiezione.AnniDetenzione, v => _scenario.Proiezione.AnniDetenzione = (int)v, 1, 40,
            aiuto: "Dopo quanti anni immagini di rivendere. Se vendi prima di 5 anni paghi il 26% sulla plusvalenza.");
        m4.AggiungiNumero("Rivalutazione dell'immobile", "%/anno", () => _scenario.Proiezione.RivalutazioneAnnuaPerc, v => _scenario.Proiezione.RivalutazioneAnnuaPerc = v, -10, 20, 1, 0.5m);
        m4.AggiungiNumero("Crescita del canone", "%/anno", () => _scenario.Proiezione.CrescitaCanoneAnnuaPerc, v => _scenario.Proiezione.CrescitaCanoneAnnuaPerc = v, -10, 20, 1, 0.5m);
        m4.AggiungiNumero("Inflazione delle spese", "%/anno", () => _scenario.Proiezione.InflazioneSpeseAnnuaPerc, v => _scenario.Proiezione.InflazioneSpeseAnnuaPerc = v, -10, 20, 1, 0.5m);
        m4.AggiungiNumero("Costi di rivendita", "% valore", () => _scenario.Proiezione.CostiVenditaPerc, v => _scenario.Proiezione.CostiVenditaPerc = v, 0, 10, 1, 0.5m);
        m4.AggiungiNumero("Rendimento obiettivo", "%", () => _scenario.Proiezione.RendimentoObiettivoPerc, v => _scenario.Proiezione.RendimentoObiettivoPerc = v, 0, 30, 1, 0.5m,
            "Il rendimento minimo che vuoi ottenere: usato per il VAN e per il prezzo massimo d'offerta.");
        AggiungiScheda(schede, "Fisco e futuro", m4);

        return schede;
    }

    private ModuloCampi NuovoModulo()
    {
        var m = new ModuloCampi(_aiuto);
        m.Modificato += (_, _) => { _modificato = true; AggiornaTitolo(); Ricalcola(); };
        _moduli.Add(m);
        return m;
    }

    private static void AggiungiScheda(TabControl schede, string titolo, Control contenuto)
    {
        var pagina = new TabPage(titolo) { UseVisualStyleBackColor = true };
        pagina.Controls.Add(contenuto);
        schede.TabPages.Add(pagina);
    }

    private Control CreaPannelloRisultati()
    {
        _schedeRisultati.TabPages.Add(CreaSchedaRiepilogo());

        var proiezione = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        proiezione.Panel1.Controls.Add(_grafico);
        proiezione.Panel2.Controls.Add(_gridProiezione);
        ImpostaDivisione(proiezione, 300);
        AggiungiScheda(_schedeRisultati, "Proiezione", proiezione);

        AggiungiScheda(_schedeRisultati, "Piano del mutuo", _gridMutuo);
        AggiungiScheda(_schedeRisultati, "Mercato e comparabili", CreaSchedaMercato());
        AggiungiScheda(_schedeRisultati, "Confronto scenari", CreaSchedaConfronto());

        _schedaGuida.Controls.Add(CreaGuida());
        _schedeRisultati.TabPages.Add(_schedaGuida);

        ConfiguraGriglie();
        return _schedeRisultati;
    }

    /// <summary>
    /// Posiziona il divisore solo quando il pannello è abbastanza grande:
    /// prima della visualizzazione la sua altezza può essere ancora minima.
    /// </summary>
    private static void ImpostaDivisione(SplitContainer sc, int distanza)
    {
        void AlRidimensionamento(object? sender, EventArgs e)
        {
            if (sc.Height < distanza + sc.Panel2MinSize + sc.SplitterWidth + 20) return;
            sc.SplitterDistance = distanza;
            sc.SizeChanged -= AlRidimensionamento;
        }
        sc.SizeChanged += AlRidimensionamento;
    }

    private TabPage CreaSchedaRiepilogo()
    {
        var pagina = new TabPage("Riepilogo") { UseVisualStyleBackColor = true };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(6) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var schede = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        AggiungiKpi(schede, "lordo", "Rendimento lordo", "Canone annuo ÷ prezzo. Utile per un primo confronto veloce tra annunci.");
        AggiungiKpi(schede, "netto", "Rendimento netto", "(Reddito operativo − tasse) ÷ costo complessivo, come se pagassi tutto in contanti.");
        AggiungiKpi(schede, "cfm", "Cash flow mensile", "Quanto ti resta in tasca ogni mese dopo spese, tasse e rata del mutuo.");
        AggiungiKpi(schede, "coc", "Cash on cash", "Cash flow annuo ÷ capitale proprio: il rendimento dei soldi che metti tu.");
        AggiungiKpi(schede, "tir", "TIR", "Tasso Interno di Rendimento sull'intero periodo, compresa la rivendita finale.");
        AggiungiKpi(schede, "capitale", "Capitale proprio", "I soldi che devi avere: prezzo + costi − mutuo.");
        AggiungiKpi(schede, "dscr", "DSCR", "Reddito operativo ÷ rata del mutuo. Sotto 1 il reddito non basta a pagare la rata.");
        AggiungiKpi(schede, "max", "Prezzo max d'offerta", "Il prezzo più alto che puoi pagare per ottenere il rendimento netto obiettivo.");

        _dettaglio.Columns.Add("Voce", 340);
        _dettaglio.Columns.Add("Valore", 160, HorizontalAlignment.Right);

        layout.Controls.Add(schede, 0, 0);
        layout.Controls.Add(_pannelloAvvisi, 0, 1);
        layout.Controls.Add(_dettaglio, 0, 2);
        pagina.Controls.Add(layout);
        return pagina;
    }

    private void AggiungiKpi(FlowLayoutPanel contenitore, string chiave, string titolo, string aiuto)
    {
        var scheda = new Panel { Size = new Size(205, 66), BackColor = Tema.SfondoScheda, Margin = new Padding(4) };
        var t = new Label { Text = titolo, Location = new Point(8, 6), AutoSize = true, ForeColor = Tema.TestoSecondario };
        var v = new Label { Text = "–", Location = new Point(8, 26), AutoSize = true, Font = new Font(Font.FontFamily, 15, FontStyle.Bold), ForeColor = Tema.Primario };
        scheda.Controls.Add(t);
        scheda.Controls.Add(v);
        foreach (Control c in new Control[] { scheda, t, v }) _aiuto.SetToolTip(c, aiuto);
        contenitore.Controls.Add(scheda);
        _kpi[chiave] = v;
    }

    private Control CreaSchedaMercato()
    {
        var divisore = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        var istruzioni = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(4),
            Text = "Inserisci annunci o compravendite di immobili simili nella stessa zona (fonti: portali immobiliari, quotazioni OMI). " +
                   "Per le VENDITE indica il prezzo totale, per gli AFFITTI il canone mensile. Il programma calcola il valore al m² e lo confronta con il tuo immobile."
        };
        divisore.Panel1.Controls.Add(_gridComparabili);
        divisore.Panel1.Controls.Add(istruzioni);

        _statMercato.Columns.Add("Indicatore di mercato", 340);
        _statMercato.Columns.Add("Valore", 180, HorizontalAlignment.Right);
        divisore.Panel2.Controls.Add(_statMercato);
        ImpostaDivisione(divisore, 280);
        return divisore;
    }

    private Control CreaSchedaConfronto()
    {
        var pannello = new Panel { Dock = DockStyle.Fill };
        var pulsanti = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(2) };
        pulsanti.Controls.Add(Pulsante("Aggiungi scenario attuale", AggiungiAlConfronto));
        pulsanti.Controls.Add(Pulsante("Aggiungi da file...", AggiungiFileAlConfronto));
        pulsanti.Controls.Add(Pulsante("Rimuovi selezionati", () =>
        {
            foreach (DataGridViewRow r in _gridConfronto.SelectedRows)
                if (r.DataBoundItem is RigaConfronto rc) _confronto.Remove(rc);
        }));
        pulsanti.Controls.Add(Pulsante("Svuota", _confronto.Clear));
        pannello.Controls.Add(_gridConfronto);
        pannello.Controls.Add(pulsanti);
        return pannello;

        static Button Pulsante(string testo, Action azione)
        {
            var b = new Button { Text = testo, AutoSize = true, Height = 28 };
            b.Click += (_, _) => azione();
            return b;
        }
    }

    private static DataGridView NuovaGriglia(bool modificabile = false) => new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        ReadOnly = !modificabile,
        AllowUserToAddRows = modificabile,
        AllowUserToDeleteRows = modificabile,
        AllowUserToResizeRows = false,
        RowHeadersWidth = 28,
        SelectionMode = modificabile ? DataGridViewSelectionMode.CellSelect : DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = { BackColor = Tema.SfondoScheda, Font = new Font("Segoe UI", 9F, FontStyle.Bold) }
    };

    private static void Colonna(DataGridView g, string proprieta, string intestazione, string formato = "N0", bool sola = true)
    {
        g.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = proprieta,
            HeaderText = intestazione,
            ReadOnly = sola,
            DefaultCellStyle =
            {
                Format = formato,
                Alignment = formato == "" ? DataGridViewContentAlignment.MiddleLeft : DataGridViewContentAlignment.MiddleRight
            }
        });
    }

    private void ConfiguraGriglie()
    {
        Colonna(_gridProiezione, nameof(AnnoProiezione.Anno), "Anno", "0");
        Colonna(_gridProiezione, nameof(AnnoProiezione.CanoneIncassato), "Canone €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.SpeseOperative), "Spese €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.Noi), "NOI €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.Tasse), "Tasse €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.RataMutuo), "Rata €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.CashFlow), "Cash flow €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.CashFlowCumulato), "Cumulato €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.ValoreImmobile), "Valore €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.DebitoResiduo), "Debito €");
        Colonna(_gridProiezione, nameof(AnnoProiezione.PatrimonioNetto), "Patrimonio netto €");
        _gridProiezione.CellFormatting += ColoraNegativi;

        Colonna(_gridMutuo, nameof(AnnoAmmortamento.Anno), "Anno", "0");
        Colonna(_gridMutuo, nameof(AnnoAmmortamento.Rate), "Rate pagate nell'anno €");
        Colonna(_gridMutuo, nameof(AnnoAmmortamento.QuotaInteressi), "di cui interessi €");
        Colonna(_gridMutuo, nameof(AnnoAmmortamento.QuotaCapitale), "di cui capitale €");
        Colonna(_gridMutuo, nameof(AnnoAmmortamento.DebitoResiduo), "Debito residuo €");

        // Comparabili: griglia modificabile collegata direttamente alla lista dello scenario
        Colonna(_gridComparabili, nameof(Comparabile.Descrizione), "Descrizione / fonte", "", sola: false);
        _gridComparabili.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(Comparabile.Tipo),
            HeaderText = "Tipo",
            DataSource = Enum.GetValues(typeof(TipoComparabile)),
            ValueType = typeof(TipoComparabile),
            FlatStyle = FlatStyle.Flat
        });
        Colonna(_gridComparabili, nameof(Comparabile.Prezzo), "Prezzo totale / canone mensile €", "N0", sola: false);
        Colonna(_gridComparabili, nameof(Comparabile.SuperficieMq), "Superficie m²", "N0", sola: false);
        Colonna(_gridComparabili, nameof(Comparabile.PrezzoAlMq), "€/m²", "N2");
        _gridComparabili.Columns[0].FillWeight = 220;
        _gridComparabili.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_gridComparabili.IsCurrentCellDirty && _gridComparabili.CurrentCell is DataGridViewComboBoxCell)
                _gridComparabili.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _gridComparabili.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0) _gridComparabili.InvalidateRow(e.RowIndex);
            ComparabiliModificati();
        };
        _gridComparabili.UserDeletedRow += (_, _) => ComparabiliModificati();
        _gridComparabili.DataError += (_, e) =>
        {
            e.ThrowException = false;
            _stato.Text = "Valore non valido nella tabella dei comparabili: inserisci un numero.";
        };

        Colonna(_gridConfronto, nameof(RigaConfronto.Nome), "Scenario", "");
        Colonna(_gridConfronto, nameof(RigaConfronto.Prezzo), "Prezzo €");
        Colonna(_gridConfronto, nameof(RigaConfronto.CapitaleProprio), "Capitale proprio €");
        Colonna(_gridConfronto, nameof(RigaConfronto.RendimentoLordo), "Rend. lordo %", "N2");
        Colonna(_gridConfronto, nameof(RigaConfronto.RendimentoNetto), "Rend. netto %", "N2");
        Colonna(_gridConfronto, nameof(RigaConfronto.CashFlowMensile), "Cash flow/mese €");
        Colonna(_gridConfronto, nameof(RigaConfronto.CashOnCash), "Cash on cash %", "N2");
        Colonna(_gridConfronto, nameof(RigaConfronto.Tir), "TIR %", "N2");
        Colonna(_gridConfronto, nameof(RigaConfronto.Van), "VAN €");
        _gridConfronto.Columns[0].FillWeight = 200;
        _gridConfronto.DataSource = _confronto;
        _gridConfronto.CellFormatting += ColoraNegativi;
    }

    private static void ColoraNegativi(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value is double v && v < 0) e.CellStyle!.ForeColor = Tema.Negativo;
    }

    private Control CreaGuida()
    {
        var testo = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new Font("Segoe UI", 10F) };
        void Titolo(string t) { testo.SelectionFont = new Font("Segoe UI", 11.5F, FontStyle.Bold); testo.SelectionColor = Tema.Primario; testo.AppendText(t + "\n"); }
        void Par(string t) { testo.SelectionFont = new Font("Segoe UI", 10F); testo.SelectionColor = Color.Black; testo.AppendText(t + "\n\n"); }

        Titolo("Come si usa questo programma");
        Par("1) Compila le schede a sinistra partendo dall'annuncio. 2) Guarda il Riepilogo: i valori si aggiornano subito. " +
            "3) Inserisci alcuni comparabili per capire se il prezzo è in linea con la zona. 4) Salva lo scenario e aggiungilo al Confronto per valutare più immobili. " +
            "Passando il mouse su un campo compare una spiegazione.");
        Titolo("Costi d'acquisto (regole 2026)");
        Par("Da privato: imposta di registro del 9% sul valore catastale (rendita × 1,05 × 120), minimo 1.000 €, più 50 € di ipotecaria e 50 € di catastale. " +
            "Con le agevolazioni prima casa il registro scende al 2% (moltiplicatore 110). Da impresa costruttrice: IVA al 10% (4% prima casa) e tre imposte fisse da 200 €. " +
            "A questi si aggiungono notaio e agenzia (provvigione + IVA 22%).");
        Titolo("Rendimento lordo");
        Par("Canone annuo ÷ prezzo. È il numero che si legge di solito negli annunci, ma ignora spese e tasse: serve solo per una prima scrematura.");
        Titolo("NOI – Reddito operativo netto");
        Par("Canone effettivamente incassato (togliendo i mesi di sfitto) meno IMU, condominio, manutenzione, assicurazione e gestione. Non considera tasse e mutuo.");
        Titolo("Rendimento netto");
        Par("(NOI − tasse) ÷ costo complessivo. Indica quanto rende l'immobile in sé, come se lo pagassi tutto in contanti. È il dato giusto per confrontare l'immobile con altri investimenti.");
        Titolo("Cash flow e cash on cash");
        Par("Il cash flow è ciò che ti resta dopo aver pagato anche la rata del mutuo. Il cash on cash lo divide per i soldi che hai messo tu: " +
            "grazie alla leva del mutuo può superare il rendimento netto, ma il rischio aumenta.");
        Titolo("Leva finanziaria");
        Par("Se il rendimento netto dell'immobile è più alto del tasso del mutuo, prendere a prestito aumenta il rendimento del tuo capitale (leva positiva). " +
            "Se è più basso, il mutuo lo riduce (leva negativa). Prova a cambiare la percentuale finanziata e osserva il cash on cash.");
        Titolo("DSCR");
        Par("Debt Service Coverage Ratio = NOI ÷ rata annua. Sopra 1,2 il reddito copre la rata con un margine di sicurezza; sotto 1 la rata va pagata con altri soldi.");
        Titolo("TIR e VAN");
        Par("Il TIR (Tasso Interno di Rendimento) riassume in un solo tasso annuo tutto l'investimento: il capitale iniziale, i cash flow di ogni anno e l'incasso della rivendita finale. " +
            "Il VAN (Valore Attuale Netto) riporta tutti i flussi a oggi usando il tuo rendimento obiettivo: se è positivo l'investimento rende più del tuo obiettivo.");
        Titolo("Cedolare secca o IRPEF?");
        Par("La cedolare secca tassa il canone al 21% (10% con canone concordato) e non prevede addizionali né imposta di registro sul contratto. " +
            "Con l'IRPEF il 95% del canone si somma ai tuoi redditi e paghi la tua aliquota marginale (23%, 33% o 43% nel 2026) più le addizionali. " +
            "Per chi ha redditi medio-alti la cedolare conviene quasi sempre: prova a cambiare il regime e confronta.");
        Titolo("Analisi di mercato con i comparabili");
        Par("Il valore si stima con il prezzo al m² di immobili simili nella stessa zona. Si usa la mediana, cioè il valore centrale, perché un singolo annuncio fuori mercato la sposta poco. " +
            "Ricorda che i prezzi degli annunci sono richieste: le compravendite reali chiudono di solito qualche punto percentuale più in basso.");
        Titolo("Avvertenza");
        Par("Strumento didattico realizzato da Marco Consiglio. Le aliquote sono aggiornate al 2026 ma possono variare per Comune o per nuove leggi: " +
            "prima di decidere confrontati con notaio, commercialista e banca.");
        testo.SelectionStart = 0;
        return testo;
    }

    // =====================================================================
    //  Calcolo e visualizzazione dei risultati
    // =====================================================================

    private static string Euro(double v) => v.ToString("N0") + " €";
    private static string Perc(double v) => v.ToString("N2") + "%";

    private void Ricalcola()
    {
        try
        {
            _risultato = AnalizzatoreInvestimento.Analizza(_scenario);
            MostraRiepilogo(_risultato);
            MostraMercato();
            _gridProiezione.DataSource = _risultato.Proiezione.ToList();
            _gridMutuo.DataSource = _risultato.Mutuo.Piano.ToList();
            _grafico.ImpostaDati(_risultato.Proiezione, _risultato.CapitaleProprio);
            _stato.Text = _percorsoFile ?? "Scenario non ancora salvato";
        }
        catch (Exception ex)
        {
            _stato.Text = "Impossibile completare il calcolo: " + ex.Message;
        }
    }

    private void MostraRiepilogo(RisultatoAnalisi r)
    {
        double obiettivo = _scenario.Proiezione.RendimentoObiettivoPerc;
        Kpi("lordo", Perc(r.RendimentoLordoPerc), null);
        Kpi("netto", Perc(r.RendimentoNettoPerc), r.RendimentoNettoPerc >= obiettivo);
        Kpi("cfm", Euro(r.CashFlowMensile), r.CashFlowAnnuo >= 0);
        Kpi("coc", Perc(r.CashOnCashPerc), r.CashOnCashPerc >= 0);
        Kpi("tir", r.TirPerc is { } tir ? Perc(tir) : "n.d.", r.TirPerc >= obiettivo);
        Kpi("capitale", Euro(r.CapitaleProprio), null);
        Kpi("dscr", r.Dscr is { } d ? d.ToString("N2") : "senza mutuo", r.Dscr is null ? null : r.Dscr >= 1.2);
        Kpi("max", r.PrezzoMassimoOfferta is { } pm ? Euro(pm) : "n.d.",
            r.PrezzoMassimoOfferta is null ? null : r.PrezzoMassimoOfferta >= _scenario.Acquisto.Prezzo);

        _pannelloAvvisi.SuspendLayout();
        _pannelloAvvisi.Controls.Clear();
        foreach (var a in r.Avvisi)
        {
            _pannelloAvvisi.Controls.Add(new Label
            {
                Text = "⚠ " + a,
                AutoSize = true,
                MaximumSize = new Size(Math.Max(300, _pannelloAvvisi.Parent?.Width - 30 ?? 800), 0),
                BackColor = Tema.Attenzione,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(4, 2, 4, 2)
            });
        }
        _pannelloAvvisi.ResumeLayout();

        var c = r.CostiAcquisto;
        var a1 = new ListViewGroup("Acquisto");
        var a2 = new ListViewGroup("Primo anno di affitto");
        var a3 = new ListViewGroup($"Rivendita dopo {_scenario.Proiezione.AnniDetenzione} anni");
        _dettaglio.BeginUpdate();
        _dettaglio.Items.Clear();
        _dettaglio.Groups.Clear();
        _dettaglio.Groups.AddRange(new[] { a1, a2, a3 });

        Voce(a1, "Prezzo d'acquisto", Euro(_scenario.Acquisto.Prezzo));
        Voce(a1, "Prezzo al m²", Euro(r.PrezzoAlMq));
        if (c.Iva > 0) Voce(a1, "IVA", Euro(c.Iva));
        else Voce(a1, $"Imposta di registro (base imponibile {Euro(c.BaseImponibile)})", Euro(c.ImpostaRegistro));
        Voce(a1, "Imposte ipotecaria e catastale", Euro(c.ImpostaIpotecaria + c.ImpostaCatastale) + (c.Iva > 0 ? " + registro " + Euro(c.ImpostaRegistro) : ""));
        Voce(a1, "Agenzia (IVA inclusa)", Euro(c.Agenzia));
        Voce(a1, "Notaio", Euro(c.Notaio));
        Voce(a1, "Ristrutturazione e arredo", Euro(_scenario.Acquisto.CostiRistrutturazione + _scenario.Acquisto.ArredamentoEAltro));
        Voce(a1, "COSTO COMPLESSIVO", Euro(r.CostoComplessivo));
        Voce(a1, "Mutuo erogato", Euro(r.ImportoMutuo));
        Voce(a1, "Costi del mutuo (imposta sostitutiva 2%, istruttoria, perizia)", Euro(r.CostiMutuo));
        Voce(a1, "CAPITALE PROPRIO NECESSARIO", Euro(r.CapitaleProprio));

        Voce(a2, "Canone lordo annuo", Euro(r.CanoneLordoAnnuo));
        Voce(a2, "Canone incassato (meno lo sfitto)", Euro(r.CanoneIncassato));
        Voce(a2, "di cui IMU", "− " + Euro(r.Imu));
        Voce(a2, "Spese operative totali", "− " + Euro(r.SpeseOperative));
        Voce(a2, "Reddito operativo netto (NOI)", Euro(r.Noi));
        Voce(a2, _scenario.Fisco.Regime == RegimeFiscale.CedolareSecca ? "Cedolare secca" : "IRPEF, addizionali e registro", "− " + Euro(r.Tasse));
        Voce(a2, $"Rata del mutuo ({Euro(r.Mutuo.RataMensile)} al mese)", "− " + Euro(r.RataAnnua));
        Voce(a2, "CASH FLOW ANNUO", Euro(r.CashFlowAnnuo));
        Voce(a2, "Anni per recuperare il capitale con i soli affitti", r.AnniRientro?.ToString("N1") ?? "n.d.");
        if (r.ImportoMutuo > 0) Voce(a2, "Interessi totali pagati alla banca", Euro(r.Mutuo.TotaleInteressi));

        Voce(a3, "Valore di rivendita stimato", Euro(r.PrezzoVenditaFinale));
        Voce(a3, "Imposta sulla plusvalenza", Euro(r.ImpostaPlusvalenza));
        Voce(a3, "Incasso netto (dopo costi, imposte e debito residuo)", Euro(r.IncassoNettoVendita));
        Voce(a3, "GUADAGNO TOTALE (affitti + vendita − capitale)", Euro(r.GuadagnoTotale));
        Voce(a3, "Multiplo del capitale investito", r.MultiploCapitale.ToString("N2") + "x");
        Voce(a3, $"VAN al {Perc(_scenario.Proiezione.RendimentoObiettivoPerc)}", Euro(r.Van));
        _dettaglio.EndUpdate();

        void Voce(ListViewGroup g, string voce, string valore)
        {
            var item = new ListViewItem(new[] { voce, valore }, g);
            if (voce == voce.ToUpperInvariant()) item.Font = new Font(_dettaglio.Font, FontStyle.Bold);
            _dettaglio.Items.Add(item);
        }
    }

    private void Kpi(string chiave, string testo, bool? positivo)
    {
        var l = _kpi[chiave];
        l.Text = testo;
        l.ForeColor = positivo switch { true => Tema.Positivo, false => Tema.Negativo, _ => Tema.Primario };
    }

    private void MostraMercato()
    {
        var m = AnalisiMercato.Analizza(_scenario);
        _statMercato.BeginUpdate();
        _statMercato.Items.Clear();
        void Riga(string a, string b) => _statMercato.Items.Add(new ListViewItem(new[] { a, b }));

        Riga("Prezzo al m² del tuo immobile", Euro(m.PrezzoMqImmobile));
        if (m.Vendite.Numero > 0)
        {
            Riga($"Vendite: comparabili inseriti", m.Vendite.Numero.ToString());
            Riga("Vendite: €/m² minimo – massimo", $"{Euro(m.Vendite.Minimo)} – {Euro(m.Vendite.Massimo)}");
            Riga("Vendite: €/m² medio", Euro(m.Vendite.Media));
            Riga("Vendite: €/m² mediano", Euro(m.Vendite.Mediana));
            Riga("Valore stimato del tuo immobile (mediana × m²)", Euro(m.ValoreStimato));
            Riga("Prezzo richiesto rispetto al mercato", (m.ScostamentoPrezzoPerc >= 0 ? "+" : "") + Perc(m.ScostamentoPrezzoPerc));
        }
        else Riga("Vendite", "nessun comparabile inserito");

        if (m.Affitti.Numero > 0)
        {
            Riga("Affitti: comparabili inseriti", m.Affitti.Numero.ToString());
            Riga("Affitti: €/m² al mese mediano", m.Affitti.Mediana.ToString("N2") + " €");
            Riga("Canone di mercato stimato per il tuo immobile", Euro(m.CanoneStimato) + " al mese");
            Riga("Canone ipotizzato rispetto al mercato", (m.ScostamentoCanonePerc >= 0 ? "+" : "") + Perc(m.ScostamentoCanonePerc));
        }
        else Riga("Affitti", "nessun comparabile inserito");

        if (m.RendimentoLordoZonaPerc > 0)
            Riga("Rendimento lordo tipico della zona", Perc(m.RendimentoLordoZonaPerc));
        _statMercato.EndUpdate();
    }

    private void ComparabiliModificati()
    {
        _modificato = true;
        AggiornaTitolo();
        Ricalcola();
    }

    // =====================================================================
    //  File, esportazioni e confronto
    // =====================================================================

    private void CaricaScenario(ScenarioInvestimento scenario, string? percorso)
    {
        _scenario = scenario;
        _percorsoFile = percorso;
        _comparabili = new BindingList<Comparabile>(_scenario.Comparabili);
        _comparabili.ListChanged += (_, e) =>
        {
            if (e.ListChangedType is ListChangedType.ItemAdded or ListChangedType.ItemDeleted) ComparabiliModificati();
        };
        _gridComparabili.DataSource = _comparabili;
        _moduli.ForEach(m => m.AggiornaDaModello());
        _modificato = false;
        AggiornaTitolo();
        Ricalcola();
    }

    private void AggiornaTitolo() =>
        Text = $"{_scenario.Immobile.Nome}{(_modificato ? " *" : "")} – {TitoloApp}";

    private bool ConfermaPerditaModifiche()
    {
        if (!_modificato) return true;
        var risposta = MessageBox.Show(this, "Vuoi salvare le modifiche allo scenario attuale?", TitoloApp,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return risposta switch
        {
            DialogResult.Yes => Salva(false),
            DialogResult.No => true,
            _ => false
        };
    }

    private void Nuovo()
    {
        if (ConfermaPerditaModifiche()) CaricaScenario(new ScenarioInvestimento(), null);
    }

    private void Apri()
    {
        if (!ConfermaPerditaModifiche()) return;
        using var dlg = new OpenFileDialog { Filter = FiltroFile };
        if (dlg.ShowDialog(this) == DialogResult.OK) ApriFile(dlg.FileName);
    }

    private void ApriFile(string percorso)
    {
        try
        {
            CaricaScenario(ArchivioScenari.Apri(percorso), percorso);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossibile aprire il file:\n" + ex.Message, TitoloApp, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool Salva(bool chiediNome)
    {
        _gridComparabili.EndEdit();
        string? percorso = _percorsoFile;
        if (chiediNome || percorso == null)
        {
            using var dlg = new SaveFileDialog { Filter = FiltroFile, FileName = NomeFileSicuro(_scenario.Immobile.Nome) + ArchivioScenari.Estensione };
            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            percorso = dlg.FileName;
        }
        try
        {
            ArchivioScenari.Salva(_scenario, percorso);
            _percorsoFile = percorso;
            _modificato = false;
            AggiornaTitolo();
            _stato.Text = "Salvato in " + percorso;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossibile salvare:\n" + ex.Message, TitoloApp, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void EsportaReport()
    {
        if (_risultato == null) return;
        using var dlg = new SaveFileDialog { Filter = "Pagina web (*.html)|*.html", FileName = "Report " + NomeFileSicuro(_scenario.Immobile.Nome) + ".html" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dlg.FileName, Esportatore.ReportHtml(_scenario, _risultato));
        // Apre il report nel browser predefinito: da lì si può stampare o salvare in PDF.
        Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
    }

    private void EsportaCsv()
    {
        if (_risultato == null) return;
        using var dlg = new SaveFileDialog { Filter = "CSV per Excel (*.csv)|*.csv", FileName = "Proiezione " + NomeFileSicuro(_scenario.Immobile.Nome) + ".csv" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        // UTF-8 con BOM: Excel riconosce così le lettere accentate e il simbolo €.
        File.WriteAllText(dlg.FileName, Esportatore.ProiezioneCsv(_risultato), new System.Text.UTF8Encoding(true));
        _stato.Text = "Proiezione esportata in " + dlg.FileName;
    }

    private void AggiungiAlConfronto()
    {
        if (_risultato == null) return;
        _confronto.Add(RigaConfronto.Da(_scenario.Immobile.Nome, _scenario, _risultato));
        _schedeRisultati.SelectedIndex = 4;
    }

    private void AggiungiFileAlConfronto()
    {
        using var dlg = new OpenFileDialog { Filter = FiltroFile, Multiselect = true };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        foreach (var f in dlg.FileNames)
        {
            try
            {
                var s = ArchivioScenari.Apri(f);
                _confronto.Add(RigaConfronto.Da(s.Immobile.Nome, s, AnalizzatoreInvestimento.Analizza(s)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Impossibile leggere {Path.GetFileName(f)}:\n{ex.Message}", TitoloApp, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        _schedeRisultati.SelectedIndex = 4;
    }

    private static string NomeFileSicuro(string nome)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) nome = nome.Replace(c, '_');
        return string.IsNullOrWhiteSpace(nome) ? "scenario" : nome.Trim();
    }
}

/// <summary>Una riga della tabella di confronto tra più immobili.</summary>
internal sealed class RigaConfronto
{
    public string Nome { get; init; } = "";
    public double Prezzo { get; init; }
    public double CapitaleProprio { get; init; }
    public double RendimentoLordo { get; init; }
    public double RendimentoNetto { get; init; }
    public double CashFlowMensile { get; init; }
    public double CashOnCash { get; init; }
    public double? Tir { get; init; }
    public double Van { get; init; }

    public static RigaConfronto Da(string nome, ScenarioInvestimento s, RisultatoAnalisi r) => new()
    {
        Nome = nome,
        Prezzo = s.Acquisto.Prezzo,
        CapitaleProprio = r.CapitaleProprio,
        RendimentoLordo = r.RendimentoLordoPerc,
        RendimentoNetto = r.RendimentoNettoPerc,
        CashFlowMensile = r.CashFlowMensile,
        CashOnCash = r.CashOnCashPerc,
        Tir = r.TirPerc,
        Van = r.Van
    };
}
