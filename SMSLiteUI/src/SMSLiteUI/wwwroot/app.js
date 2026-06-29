window.smsPositionFilterPopover = (anchorEl, pop) => {
  if (!anchorEl || !pop) {
    return;
  }

  const pad = 8;
  const gap = 8;
  const anchorRect = anchorEl.getBoundingClientRect();
  const availableWidth = Math.max(0, window.innerWidth - (pad * 2));
  const width = Math.min(anchorRect.width, availableWidth);
  const anchorCenter = anchorRect.left + (anchorRect.width / 2);
  const left = Math.min(
    Math.max(pad, anchorCenter - (width / 2)),
    window.innerWidth - pad - width
  );
  const top = anchorRect.bottom + gap;
  const availableHeight = Math.max(160, window.innerHeight - top - pad);

  pop.style.position = "fixed";
  pop.style.transform = "none";
  pop.style.right = "auto";
  pop.style.bottom = "auto";
  pop.style.margin = "0";
  pop.style.width = `${Math.round(width)}px`;
  pop.style.maxWidth = `${Math.round(availableWidth)}px`;
  pop.style.maxHeight = `${Math.round(availableHeight)}px`;
  pop.style.overflowY = "auto";
  pop.style.left = `${Math.round(left)}px`;
  pop.style.top = `${Math.round(top)}px`;
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
