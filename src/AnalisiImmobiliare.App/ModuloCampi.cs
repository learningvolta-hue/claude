namespace AnalisiImmobiliare.App;

/// <summary>
/// Pannello che costruisce un modulo "etichetta + campo" collegato al modello dati.
/// Ogni campo legge e scrive il valore tramite due funzioni (get/set), così il
/// codice dell'interfaccia resta corto e non serve il designer di Visual Studio.
/// </summary>
internal sealed class ModuloCampi : TableLayoutPanel
{
    private readonly List<Action> _aggiornamenti = new();
    private readonly ToolTip _suggerimenti;
    private bool _inAggiornamento;

    /// <summary>Scatta ogni volta che l'utente modifica un valore.</summary>
    public event EventHandler? Modificato;

    public ModuloCampi(ToolTip suggerimenti)
    {
        _suggerimenti = suggerimenti;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        ColumnCount = 3;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Padding = new Padding(8, 4, 16, 8);
    }

    /// <summary>Rilegge tutti i valori dal modello (es. dopo aver aperto un file).</summary>
    public void AggiornaDaModello()
    {
        _inAggiornamento = true;
        try { _aggiornamenti.ForEach(a => a()); }
        finally { _inAggiornamento = false; }
    }

    private void Notifica()
    {
        if (!_inAggiornamento) Modificato?.Invoke(this, EventArgs.Empty);
    }

    public void AggiungiTitolo(string testo)
    {
        var l = new Label
        {
            Text = testo,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
            ForeColor = Tema.Primario,
            Margin = new Padding(0, RowCount == 0 ? 4 : 14, 0, 4)
        };
        Controls.Add(l, 0, RowCount);
        SetColumnSpan(l, 3);
        RowCount++;
    }

    public void AggiungiNumero(string etichetta, string unita, Func<double> leggi, Action<double> scrivi,
        decimal min, decimal max, int decimali = 0, decimal incremento = 1, string? aiuto = null)
    {
        var campo = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimali,
            Increment = incremento,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right,
            Dock = DockStyle.Fill
        };
        campo.ValueChanged += (_, _) =>
        {
            if (_inAggiornamento) return;
            scrivi((double)campo.Value);
            Notifica();
        };
        _aggiornamenti.Add(() => campo.Value = Math.Clamp((decimal)Math.Round(leggi(), decimali), min, max));
        AggiungiRiga(etichetta, campo, unita, aiuto);
    }

    public void AggiungiTesto(string etichetta, Func<string> leggi, Action<string> scrivi, string? aiuto = null)
    {
        var campo = new TextBox { Dock = DockStyle.Fill };
        campo.TextChanged += (_, _) =>
        {
            if (_inAggiornamento) return;
            scrivi(campo.Text);
            Notifica();
        };
        _aggiornamenti.Add(() => campo.Text = leggi());
        AggiungiRiga(etichetta, campo, "", aiuto);
    }

    public void AggiungiSpunta(string etichetta, Func<bool> leggi, Action<bool> scrivi, string? aiuto = null)
    {
        var campo = new CheckBox { Text = etichetta, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
        campo.CheckedChanged += (_, _) =>
        {
            if (_inAggiornamento) return;
            scrivi(campo.Checked);
            Notifica();
        };
        _aggiornamenti.Add(() => campo.Checked = leggi());
        if (aiuto != null) _suggerimenti.SetToolTip(campo, aiuto);
        Controls.Add(campo, 0, RowCount);
        SetColumnSpan(campo, 3);
        RowCount++;
    }

    public void AggiungiScelta<T>(string etichetta, (T Valore, string Testo)[] opzioni,
        Func<T> leggi, Action<T> scrivi, string? aiuto = null) where T : struct, Enum
    {
        var campo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        foreach (var o in opzioni) campo.Items.Add(o.Testo);
        campo.SelectedIndexChanged += (_, _) =>
        {
            if (_inAggiornamento || campo.SelectedIndex < 0) return;
            scrivi(opzioni[campo.SelectedIndex].Valore);
            Notifica();
        };
        _aggiornamenti.Add(() =>
            campo.SelectedIndex = Array.FindIndex(opzioni, o => EqualityComparer<T>.Default.Equals(o.Valore, leggi())));
        AggiungiRiga(etichetta, campo, "", aiuto);
    }

    private void AggiungiRiga(string etichetta, Control campo, string unita, string? aiuto)
    {
        var l = new Label { Text = etichetta, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) };
        var u = new Label { Text = unita, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Tema.TestoSecondario, Margin = new Padding(3, 6, 3, 3) };
        if (aiuto != null)
        {
            _suggerimenti.SetToolTip(l, aiuto);
            _suggerimenti.SetToolTip(campo, aiuto);
        }
        Controls.Add(l, 0, RowCount);
        Controls.Add(campo, 1, RowCount);
        Controls.Add(u, 2, RowCount);
        RowCount++;
    }
}
