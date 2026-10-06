/* ============================================
   Market Finder Pro — app.js
   Marco Consiglio · marcocreazioni.it · 2026
   ============================================ */

// ── Stato moduli completati ──────────────────
const completed = new Set();

// ── Versione Windows (WebView2) ──────────────
// Se la pagina gira dentro l'app desktop, window.chrome.webview è disponibile
const isDesktopApp = !!(window.chrome && window.chrome.webview);

// ── Salvataggio progressi (localStorage) ─────
const STORAGE_KEY = 'mfp-progress-2026';

function saveState() {
  try {
    const checks = {};
    document.querySelectorAll('.checklist-item input[type=checkbox]')
      .forEach(cb => { checks[cb.id] = cb.checked; });
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ completed: [...completed], checks }));
  } catch (e) { /* storage non disponibile: si continua senza salvare */ }
}

function loadState() {
  try {
    const data = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null');
    if (!data) return;
    (data.completed || []).forEach(id => {
      completed.add(id);
      const nav = document.querySelector('[data-nav="' + id + '"]');
      if (nav) nav.classList.add('done');
    });
    Object.entries(data.checks || {}).forEach(([id, val]) => {
      const cb = document.getElementById(id);
      if (cb) cb.checked = !!val;
    });
  } catch (e) { /* dati corrotti o storage non disponibile */ }
}

// ── Navigazione sezioni ──────────────────────
function goTo(id) {
  document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));

  const sec = document.getElementById('sec-' + id);
  if (sec) sec.classList.add('active');

  const nav = document.querySelector('[data-nav="' + id + '"]');
  if (nav) nav.classList.add('active');

  window.scrollTo({ top: 0, behavior: 'smooth' });
}

// ── Segna modulo completato e naviga ─────────
function completeAndGo(done, next) {
  completed.add(done);
  const navItem = document.querySelector('[data-nav="' + done + '"]');
  if (navItem) navItem.classList.add('done');
  updateProgress();
  saveState();
  goTo(next);
}

// ── Barra di avanzamento ─────────────────────
function updateProgress() {
  const total = 4; // moduli totali
  const pct   = Math.round((completed.size / total) * 100);
  const fill  = document.getElementById('prog-fill');
  const label = document.getElementById('prog-label');
  if (fill)  fill.style.width  = pct + '%';
  if (label) label.textContent = pct + '%';
}

// ── Validazione quiz ─────────────────────────
function checkQuiz(el, qid, correct) {
  const container = el.closest('.quiz-card');
  if (!container) return;

  // Evita risposte multiple
  if (container.dataset.answered) return;
  container.dataset.answered = '1';

  const opts     = container.querySelectorAll('.quiz-opt');
  const feedback = container.querySelector('#fb-' + qid);

  opts.forEach(o => {
    o.style.pointerEvents = 'none';
    if (o.dataset.ans === correct) o.classList.add('correct');
  });

  if (el.dataset.ans === correct) {
    el.classList.add('correct');
    if (feedback) {
      feedback.textContent = '✅ Esatto! Ottima risposta.';
      feedback.className   = 'quiz-feedback show ok';
    }
  } else {
    el.classList.add('wrong');
    if (feedback) {
      feedback.textContent = '❌ Non esatto. La risposta corretta è evidenziata in verde.';
      feedback.className   = 'quiz-feedback show ko';
    }
  }
}

// ── Copia prompt AI ──────────────────────────
function copyPrompt(id, btn) {
  const el = document.getElementById(id);
  if (!el) return;

  navigator.clipboard.writeText(el.textContent.trim()).then(() => {
    btn.classList.add('copied');
    btn.innerHTML = '✓ Copiato!';
    setTimeout(() => {
      btn.classList.remove('copied');
      btn.innerHTML = '📋 Copia Prompt';
    }, 2000);
  }).catch(() => {
    // fallback per browser senza clipboard API
    const ta = document.createElement('textarea');
    ta.value = el.textContent.trim();
    document.body.appendChild(ta);
    ta.select();
    document.execCommand('copy');
    document.body.removeChild(ta);
    btn.classList.add('copied');
    btn.innerHTML = '✓ Copiato!';
    setTimeout(() => {
      btn.classList.remove('copied');
      btn.innerHTML = '📋 Copia Prompt';
    }, 2000);
  });
}

// ── Download checklist ───────────────────────
function downloadChecklist() {
  const items   = document.querySelectorAll('.checklist-item');
  let   content = 'MARKET FINDER PRO — CHECKLIST OPERATIVA\n';
  content += 'Marco Consiglio · marcocreazioni.it · 2026\n';
  content += '='.repeat(45) + '\n\n';

  let phase = '';
  items.forEach(item => {
    const h2 = item.previousElementSibling;
    if (h2 && (h2.tagName === 'H2' || h2.tagName === 'H3')) {
      if (h2.textContent !== phase) {
        phase = h2.textContent;
        content += '\n▶ ' + phase.toUpperCase() + '\n';
      }
    }
    const cb    = item.querySelector('input[type=checkbox]');
    const label = item.querySelector('label');
    if (cb && label) {
      const checked = cb.checked ? '[✓]' : '[ ]';
      const text    = label.childNodes[0]?.textContent?.trim() || '';
      const sub     = label.querySelector('span')?.textContent?.trim() || '';
      content += checked + ' ' + text + (sub ? '\n      → ' + sub : '') + '\n';
    }
  });

  content += '\n' + '='.repeat(45) + '\n';
  content += 'Scaricato il: ' + new Date().toLocaleDateString('it-IT') + '\n';

  // Nell'app Windows: finestra «Salva con nome» nativa
  if (isDesktopApp) {
    window.chrome.webview.postMessage({
      type: 'saveChecklist',
      fileName: 'market-finder-pro-checklist.txt',
      content: content
    });
    return;
  }

  const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
  const url  = URL.createObjectURL(blob);
  const a    = document.createElement('a');
  a.href     = url;
  a.download = 'market-finder-pro-checklist.txt';
  a.click();
  URL.revokeObjectURL(url);
}

// ── Reset checklist ──────────────────────────
function resetChecklist() {
  document.querySelectorAll('.checklist-item input[type=checkbox]')
    .forEach(cb => { cb.checked = false; });
  saveState();
}

// ── Init ─────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  loadState();
  document.querySelectorAll('.checklist-item input[type=checkbox]')
    .forEach(cb => cb.addEventListener('change', saveState));
  goTo('home');
  updateProgress();
});
