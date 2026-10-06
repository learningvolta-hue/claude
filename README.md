# Analisi Investimenti Immobiliari

**Autore:** Marco Consiglio · **Versione:** 1.0 (2026) · **Piattaforma:** Windows 10/11

Programma per Windows che aiuta a capire **se un immobile da mettere in affitto è un buon investimento**.
Inserisci i dati dell'annuncio. Il programma calcola costi d'acquisto, tasse, mutuo, rendimenti e
proiezione negli anni, e confronta il prezzo con il mercato della zona.

È pensato anche come **strumento didattico**: ogni campo ha una spiegazione (passa il mouse sopra) e la
scheda *Guida agli indicatori* spiega in parole semplici cosa significa ogni numero.

---

## Come scaricarlo e avviarlo

1. Apri la scheda **Actions** del repository su GitHub e scegli l'ultima esecuzione riuscita di
   *Build Analisi Immobiliare*.
2. In fondo alla pagina, sotto **Artifacts**, scarica `AnalisiImmobiliare-win-x64` (è un file .zip).
3. Estrai lo zip e fai doppio clic su `AnalisiImmobiliare.exe`.
   Non serve installare nulla: il file contiene già tutto quello che serve, .NET compreso.
4. Dal menu **File → Apri** carica `Bilocale-esempio.immo` per partire da un esempio completo.

> Al primo avvio Windows potrebbe mostrare l'avviso "Windows ha protetto il PC", perché il programma
> non è firmato digitalmente. Clicca su **Ulteriori informazioni → Esegui comunque**.

Quando viene creato un tag di versione (per esempio `v1.0.0`), l'eseguibile viene pubblicato anche nella
sezione **Releases**.

---

## Cosa fa

| Area | Cosa calcola |
|---|---|
| **Acquisto** | Imposta di registro o IVA, ipotecaria e catastale, agenzia con IVA, notaio, lavori: il **costo complessivo** e il **capitale proprio** da avere |
| **Affitto** | Canone incassato al netto dello sfitto, IMU, condominio, manutenzione, assicurazione, gestione: il **reddito operativo netto (NOI)** |
| **Tasse** | Cedolare secca (21% o 10%) oppure IRPEF con addizionali, per confrontare i due regimi |
| **Mutuo** | Rata con ammortamento alla francese, piano anno per anno, interessi totali, imposta sostitutiva |
| **Indicatori** | Rendimento lordo e netto, cash flow, cash on cash, DSCR, anni di rientro |
| **Futuro** | Proiezione fino a 40 anni con crescita di canone e valore, rivendita finale, plusvalenza, **TIR** e **VAN** |
| **Trattativa** | **Prezzo massimo d'offerta** per raggiungere il rendimento netto che vuoi |
| **Mercato** | Analisi dei **comparabili**: €/m² minimo, medio e mediano, valore stimato, canone di mercato, rendimento tipico della zona |
| **Confronto** | Tabella con più immobili affiancati, per scegliere il migliore |
| **Esportazione** | Report HTML (stampabile in PDF dal browser) e proiezione in CSV per Excel |

Gli scenari si salvano in file `.immo`. Sono file di testo in formato JSON, leggibili anche con il Blocco note.

---

## Esercizio guidato: il bilocale di esempio

Apri `esempi/Bilocale-esempio.immo`: un bilocale di 60 m² a 195.000 €, affittato a 950 € al mese, con
mutuo al 70%. Ecco cosa mostra il programma e come leggerlo.

| Indicatore | Valore | Come leggerlo |
|---|---|---|
| Rendimento lordo | 5,85% | Sembra buono, ma non considera spese e tasse |
| Costo complessivo | 226.068 € | Ai 195.000 € si aggiungono circa 31.000 € tra imposte, agenzia, notaio, lavori e arredo |
| Capitale proprio | 93.598 € | I soldi che devi avere tu, anche se la banca presta il 70% del prezzo |
| Rendimento netto | 2,81% | Il rendimento reale dell'immobile, come se lo pagassi in contanti |
| Cash flow mensile | −140 € | Ogni mese devi aggiungere soldi tuoi per pagare la rata |
| DSCR | 1,08 | Il reddito copre a malapena la rata |
| TIR a 10 anni | 2,87% | Considerando anche la rivendita, l'investimento rende meno del 5% obiettivo |
| Prezzo massimo d'offerta | ≈ 99.000 € | Il prezzo da pagare per ottenere il 5% netto con questo affitto |

**Cosa insegna questo esempio?**

- **Il rendimento lordo inganna.** Dal 5,85% lordo si scende al 2,81% netto.
- **La leva negativa.** Il rendimento netto (2,81%) è più basso del tasso del mutuo (3,3%). Per questo
  prendere soldi in prestito *peggiora* il risultato e il cash flow diventa negativo. Prova ad abbassare
  la percentuale finanziata e osserva come cambia il *cash on cash*.
- **Il prezzo è in linea con il mercato** (−0,4% rispetto alla mediana dei comparabili), ma il mercato
  della zona è caro: questo immobile rende soprattutto se si rivaluta nel tempo.
- **Cedolare o IRPEF?** Passa a IRPEF con aliquota 33%: le tasse salgono da 2.294 € a 3.742 € all'anno.

---

## Le regole fiscali usate (2026)

Tutte le aliquote sono raccolte nel file
[`ParametriFiscali2026.cs`](src/AnalisiImmobiliare.Core/Calcolo/ParametriFiscali2026.cs), così è facile
aggiornarle quando cambia la legge.

- **Acquisto da privato:** registro del 9% sul valore catastale (rendita × 1,05 × 120), con un minimo
  di 1.000 €, più ipotecaria e catastale da 50 € ciascuna. Con le agevolazioni prima casa il registro
  è al 2% e il moltiplicatore è 110.
- **Acquisto da impresa con IVA:** IVA al 10% (4% prima casa) più registro, ipotecaria e catastale da
  200 € ciascuna.
- **IMU:** rendita × 1,05 × 160 × aliquota del Comune (fino a 10,6‰). Con il canone concordato è
  ridotta del 25%.
- **Cedolare secca:** 21%, oppure 10% con canone concordato.
- **IRPEF:** si tassa il 95% del canone all'aliquota marginale (scaglioni 2026: 23%, 33%, 43%), più le
  addizionali e l'1% di registro del contratto a carico del proprietario.
- **Mutuo:** imposta sostitutiva del 2% sull'importo (0,25% per la prima casa).
- **Rivendita entro 5 anni:** imposta sostitutiva del 26% sulla plusvalenza.

> Le stime hanno **scopo didattico**. Aliquote IMU, agevolazioni e regole sugli affitti brevi cambiano
> da Comune a Comune e da un anno all'altro: prima di decidere confrontati con notaio, commercialista
> e banca.

---

## Struttura del progetto

```
AnalisiImmobiliare.sln
├── src/
│   ├── AnalisiImmobiliare.Core/      ← logica di calcolo (nessuna interfaccia grafica)
│   │   ├── Modelli/                  ← i dati di uno scenario
│   │   ├── Calcolo/                  ← imposte, mutuo, indicatori, analisi di mercato
│   │   └── Servizi/                  ← salvataggio .immo, report HTML, CSV
│   └── AnalisiImmobiliare.App/       ← interfaccia Windows (WinForms)
├── tests/AnalisiImmobiliare.Tests/   ← test automatici dei calcoli (xUnit)
├── esempi/                           ← scenari di esempio
└── .github/workflows/build.yml       ← compila e pubblica l'eseguibile
```

**Perché due progetti separati?** I calcoli stanno in una libreria che non sa nulla delle finestre.
Così si possono verificare con test automatici su qualsiasi computer, e un domani si possono riusare
per una versione web o mobile senza riscriverli.

### Compilare da soli

Serve il [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test                                   # esegue i test dei calcoli
dotnet run --project src/AnalisiImmobiliare.App   # avvia il programma (solo su Windows)

# crea l'eseguibile unico
dotnet publish src/AnalisiImmobiliare.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

---

## Idee per le prossime versioni

- Scenario per **affitti brevi** (tariffa a notte, tasso di occupazione, pulizie, piattaforme)
- **Analisi di sensibilità**: come cambia il TIR se il tasso del mutuo o il canone variano
- Importazione delle **quotazioni OMI** dell'Agenzia delle Entrate
- Grafico di confronto tra più scenari

---

© 2026 Marco Consiglio
