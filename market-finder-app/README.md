# Market Finder Pro — Applicazione Windows

**Corso interattivo per l'analisi di mercato e la ricerca di prodotti da vendere online.**
Autore: Marco Consiglio · marcocreazioni.it · 2026

Questa è la versione **desktop per Windows** del corso web *Market Finder Pro*.
Un unico file `.exe`: niente installazione, niente cartelle da copiare.

---

## ▶ Come si avvia

1. Scarica `release/MarketFinderPro.exe` (oppure l'artifact *MarketFinderPro-win-x64* da GitHub Actions)
2. Fai doppio clic sul file
3. La prima volta Windows SmartScreen potrebbe mostrare «PC protetto»: clicca **Ulteriori informazioni → Esegui comunque**
   (succede con tutti i programmi non firmati digitalmente)

### Requisiti
- Windows 10 o 11, 64 bit
- **Microsoft Edge WebView2 Runtime**: già presente in Windows 11 e in quasi tutti i PC con Windows 10.
  Se manca, l'app lo segnala e apre la pagina di download ufficiale Microsoft.
- Non serve installare .NET: è già incluso nell'EXE.

---

## ✨ Cosa aggiunge la versione Windows

| Funzione | Nel browser | Nell'app Windows |
|----------|-------------|------------------|
| Moduli, quiz e prompt AI | ✅ | ✅ identici |
| Progressi del corso | si perdono chiudendo la pagina | **vengono ricordati** a ogni avvio |
| Checklist spuntata | si perde | **viene ricordata** |
| Scarica checklist (.txt) | finisce nella cartella Download | finestra **«Salva con nome»** di Windows |
| Link agli strumenti esterni | nuova scheda | si aprono nel **browser predefinito** |
| Schermo intero | — | tasto **F11** (Esc per uscire) |

I progressi sono salvati in `%LOCALAPPDATA%\MarketFinderPro`.

---

## 🧠 Come funziona (spiegazione didattica)

L'app è costruita in **C# con .NET 8 (Windows Forms)** e usa il controllo **WebView2**,
cioè il motore di Microsoft Edge "incastonato" dentro una finestra Windows.

```
┌──────────────── MarketFinderPro.exe ────────────────┐
│  MainForm (finestra Windows)                         │
│   └─ WebView2  ──richiede──▶ https://marketfinder.app/index.html
│                     ▲                                │
│                     └── EmbeddedContent: risponde con i file  │
│                         incorporati (wwwroot/*)       │
└──────────────────────────────────────────────────────┘
```

1. **I file del corso** (`wwwroot/index.html`, `assets/style.css`, `assets/app.js`) sono incorporati
   nell'EXE come *risorse* (vedi `MarketFinderPro.csproj`).
2. La finestra apre un indirizzo "virtuale" `https://marketfinder.app/`: ogni richiesta viene
   intercettata (`WebResourceRequested`) e servita leggendo la risorsa incorporata. Nessun server, nessuna rete.
3. **Comunicazione pagina → Windows**: in `app.js`, se esiste `window.chrome.webview`
   (cioè siamo dentro l'app) il pulsante «Scarica Checklist» invia un messaggio con `postMessage`;
   `MainForm` lo riceve e mostra la finestra «Salva con nome».
4. Gli stessi file funzionano **ancora nel browser**: aprendo `wwwroot/index.html` il corso si comporta come la versione web.

### Struttura

```
market-finder-app/
├── MarketFinderPro.csproj   ← progetto .NET 8 (risorse incorporate, icona, WebView2)
├── Program.cs               ← punto di avvio
├── MainForm.cs              ← finestra + WebView2 + salvataggio + link esterni + F11
├── EmbeddedContent.cs       ← lettura dei file del corso dall'EXE
├── app.ico                  ← icona dell'applicazione
├── wwwroot/                 ← il corso web (HTML/CSS/JS)
│   ├── index.html
│   └── assets/ (style.css, app.js)
└── release/MarketFinderPro.exe  ← eseguibile pronto
```

---

## 🔧 Compilare da sorgente

Serve il **.NET 8 SDK** (funziona anche da Linux/macOS grazie a `EnableWindowsTargeting`):

```bash
dotnet publish market-finder-app/MarketFinderPro.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o ./dist
```

In alternativa, ad ogni push su `market-finder-app/**` il workflow
`.github/workflows/build-market-finder.yml` compila l'EXE su GitHub Actions
(scaricabile come artifact *MarketFinderPro-win-x64*).

### Modificare i contenuti del corso
Modifica i file in `wwwroot/` (ogni modulo è una `<section id="sec-…">` in `index.html`;
per un nuovo modulo aggiorna anche `total` in `updateProgress()` di `app.js`) e ricompila:
i nuovi contenuti vengono incorporati automaticamente nell'EXE.

---

© 2026 Marco Consiglio · marcocreazioni.it · learningvolta@gmail.com
