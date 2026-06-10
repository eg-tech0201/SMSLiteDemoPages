window.smsScrollToEvent = (container, target) => {
  if (!container || !target) {
    return;
  }

  const containerRect = container.getBoundingClientRect();
  const targetRect = target.getBoundingClientRect();
  const offsetTop = targetRect.top - containerRect.top + container.scrollTop;
  const nextScroll = offsetTop - (container.clientHeight / 2) + (targetRect.height / 2);

  container.scrollTop = Math.max(nextScroll, 0);
};

window.smsScrollToHour = (container, hour) => {
  if (!container) {
    return;
  }

  const row = container.querySelector(`[data-hour="${hour}"]`);
  if (!row) {
    return;
  }

  const containerRect = container.getBoundingClientRect();
  const rowRect = row.getBoundingClientRect();
  const offsetTop = rowRect.top - containerRect.top + container.scrollTop;

  container.scrollTop = Math.max(offsetTop - 16, 0);
};

window.smsPositionPopover = (eventEl, pop) => {
  try {
    if (!eventEl || !pop) {
      return;
    }
    const pad = 8;
  // compensate for possible page scaling wrapper
  const scaled = document.querySelector('.scaled-root');
  let scale = 1;
  if (scaled) {
    const st = window.getComputedStyle(scaled);
    const m = st.transform.match(/matrix\(([^,]+),/);
    if (m && m[1]) scale = parseFloat(m[1]);
  }

  const eventRect = eventEl.getBoundingClientRect();

  pop.style.position = "fixed";
  pop.style.transform = "none";

  // position slightly overlapping the anchor to avoid hover gaps
  let left = eventRect.left;
  let top = eventRect.bottom - 4; // overlap by 4px

  // set initial placement
  pop.style.left = `${left}px`;
  pop.style.top = `${top}px`;

  const popRect = pop.getBoundingClientRect();

  if (popRect.right > window.innerWidth - pad) {
    left = Math.max(pad, window.innerWidth - pad - popRect.width);
  }
  if (left < pad) {
    left = pad;
  }
  if (popRect.bottom > window.innerHeight - pad) {
    top = eventRect.top - popRect.height + 4; // overlap when flipping
  }
  if (top < pad) {
    top = pad;
  }

    pop.style.left = `${Math.round(left)}px`;
      pop.style.top = `${Math.round(top)}px`;
  } catch (err) {
    try { window.smsLogDiag('error', 'smsPositionPopover failed', { message: err && err.message ? err.message : String(err) }); } catch {}
    try { console.error('[SMSDiag] smsPositionPopover failed', err); } catch {}
  }
};


window.smsFocusElement = (el) => {
  if (!el) {
    return;
  }

  el.focus({ preventScroll: true });
};

window.smsGetRect = (el) => {
  if (!el) {
    return null;
  }

  const rect = el.getBoundingClientRect();
  return {
    top: rect.top,
    bottom: rect.bottom,
    left: rect.left,
    right: rect.right,
    width: rect.width,
    height: rect.height,
    windowWidth: window.innerWidth,
    windowHeight: window.innerHeight
  };
};

window.smsSaveJson = (key, value) => {
  if (!key) {
    return;
  }

  window.localStorage.setItem(key, JSON.stringify(value));
};

window.smsLoadJson = (key) => {
  if (!key) {
    return null;
  }

  const raw = window.localStorage.getItem(key);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
};

window.smsRemoveStorage = (key) => {
  if (!key) {
    return;
  }

  window.localStorage.removeItem(key);
};

window.smsDownloadText = (filename, content, mimeType) => {
  const blob = new Blob([content ?? ""], { type: mimeType || "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename || "download.txt";
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
};

// --- Diagnostics: error capture, overflow detection, persistent client logs ---
(function () {
  const STORAGE_KEY = 'smsDiagLogs';
  const MAX_LOGS = 500;

  function saveLogEntry(entry) {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY);
      const arr = raw ? JSON.parse(raw) : [];
      arr.push(entry);
      if (arr.length > MAX_LOGS) arr.splice(0, arr.length - MAX_LOGS);
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(arr));
    } catch {
      // ignore storage errors
    }
  }

  window.smsLogDiag = (level, message, data) => {
    const entry = { ts: new Date().toISOString(), level: level || 'info', message: message || '', data: data || null };
    try {
      if (entry.level === 'error') console.error('[SMSDiag]', entry); else console.log('[SMSDiag]', entry);
    } catch {}
    saveLogEntry(entry);
  };

  window.addEventListener('error', (e) => {
    try {
      window.smsLogDiag('error', 'window.error', { message: e.message, filename: e.filename, lineno: e.lineno, colno: e.colno, stack: e.error && e.error.stack ? e.error.stack : null });
    } catch {}
  });

  window.addEventListener('unhandledrejection', (e) => {
    try {
      const reason = e.reason ? (e.reason.stack || (typeof e.reason === 'object' ? JSON.stringify(e.reason) : String(e.reason))) : null;
      window.smsLogDiag('error', 'unhandledrejection', { reason });
    } catch {}
  });

  window.smsGetDiagLogs = () => {
    try { return JSON.parse(window.localStorage.getItem(STORAGE_KEY) || '[]'); } catch { return []; }
  };

  window.smsDownloadDiagLogs = () => {
    const logs = window.smsGetDiagLogs();
    window.smsDownloadText('sms-diag-logs-' + new Date().toISOString().replace(/[:.]/g, '-') + '.json', JSON.stringify(logs, null, 2), 'application/json');
  };

  window.smsDetectOverflow = (limit = 20) => {
    const offenders = [];
    const winW = window.innerWidth;
    const winH = window.innerHeight;
    const nodes = Array.from(document.querySelectorAll('body *'));
    for (let el of nodes) {
      try {
        const style = getComputedStyle(el);
        if (!style || style.display === 'none' || style.visibility === 'hidden' || el.offsetParent === null) continue;
        const rect = el.getBoundingClientRect();
        if (rect.right > winW + 1 || rect.bottom > winH + 1 || (el.scrollWidth && el.scrollWidth > el.clientWidth + 1)) {
          offenders.push({ tag: el.tagName.toLowerCase(), class: el.className, rect: { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom, width: rect.width, height: rect.height }, scrollWidth: el.scrollWidth, clientWidth: el.clientWidth });
          if (offenders.length >= limit) break;
        }
      } catch {}
    }
    window.smsLogDiag('warn', 'overflow-detection', { count: offenders.length, sample: offenders.slice(0, limit) });
    try { console.warn('[SMSDiag] overflow offenders sample:', offenders.slice(0, limit)); } catch {}
    return offenders;
  };

  window.addEventListener('load', () => {
    try {
      const bodyOverflow = document.body && document.body.scrollWidth > window.innerWidth;
      if (bodyOverflow) {
        window.smsLogDiag('warn', 'body-overflow', { bodyScrollWidth: document.body.scrollWidth, windowInnerWidth: window.innerWidth });
        window.smsDetectOverflow(30);
      } else {
        window.smsLogDiag('info', 'body-no-overflow', { bodyScrollWidth: document.body.scrollWidth, windowInnerWidth: window.innerWidth });
      }
    } catch (e) { window.smsLogDiag('error', 'diagnostic-load-failed', { message: e && e.message ? e.message : String(e) }); }
  });

})();
