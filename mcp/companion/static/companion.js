// Heron Companion page - Phase 1: shows what Revit is doing. Changes nothing.
// Model text (names of models, views, categories) is DATA: it is only ever
// put on the page with textContent, never as HTML (Golden Rule 19).
"use strict";

const HEADER = { "X-Heron-Companion": "1" };
let paired = false;
let failures = 0;

function $(id) { return document.getElementById(id); }

function set(id, text) { $(id).textContent = text == null || text === "" ? "—" : String(text); }

function pill(text, kind) {
  const el = $("link");
  el.textContent = text;
  el.className = "pill " + kind;
}

function notice(text, bad) {
  const el = $("notice");
  if (!text) { el.hidden = true; return; }
  el.hidden = false;
  el.className = "notice" + (bad ? " bad" : "");
  el.textContent = text;
}

function ago(seconds) {
  if (seconds == null) return "—";
  if (seconds < 5) return "just now";
  if (seconds < 60) return seconds + " seconds ago";
  const m = Math.round(seconds / 60);
  return m + (m === 1 ? " minute ago" : " minutes ago");
}

function clearRevit() {
  ["f-version", "f-model", "f-view", "f-updated", "s-count"].forEach(id => set(id, null));
  $("s-categories").replaceChildren();
  $("s-note").textContent = "";
}

function show(state) {
  const r = state.revit;
  if (!r) {
    clearRevit();
    if (state.choice === "several") {
      notice(state.others + " Revits are connected and this chat has not chosen one yet. " +
             "Tell the chat which one to use, and this page will follow it.");
    } else if (state.choice === "bound-gone") {
      notice("The Revit this chat was using is no longer connected. " +
             "Press the Heron button in that Revit to connect it again.", true);
    } else {
      notice("Revit is not connected to Heron. Press the Heron button in Revit.");
    }
    pill("Waiting for Revit", "wait");
    return;
  }

  notice(state.choice === "only" && state.others === 0
    ? "" : (state.others > 0 ? "Other Revits are connected too; this page shows the one this chat uses." : ""));
  pill("Connected", "ok");

  set("f-version", "Revit " + r.revitVersion);
  set("f-model", r.document ? r.document.title : "No model open");
  set("f-view", r.view ? r.view.name + "  (" + r.view.type + ")" : null);
  set("f-updated", ago(r.updatedSecondsAgo));

  const sel = r.selection || { count: 0, categories: {} };
  set("s-count", sel.count);
  const list = $("s-categories");
  const rows = Object.entries(sel.categories || {}).sort((a, b) => b[1] - a[1]);
  list.replaceChildren(...rows.map(([name, n]) => {
    const li = document.createElement("li");
    const a = document.createElement("span");
    const b = document.createElement("span");
    a.textContent = name;
    b.textContent = n;
    li.append(a, b);
    return li;
  }));
  $("s-note").textContent = sel.count === 0 ? "Nothing is selected in Revit."
    : (sel.countCapped ? "Only the first 5,000 were sorted into categories." : "");
}

let lastSeq = 0;
const WORDS = { ok: "OK", refused: "refused", failed: "failed" };

async function activity() {
  try {
    const res = await fetch("/api/activity?since=" + lastSeq, { headers: HEADER, credentials: "same-origin" });
    if (!res.ok) return;
    const body = await res.json();
    const list = $("a-list");
    for (const item of body.items) {
      lastSeq = Math.max(lastSeq, item.seq);
      const li = document.createElement("li");
      const cells = [
        ["when", item.at],
        ["tool", item.tool],
        ["what", item.summary],
        ["end " + item.outcome, (WORDS[item.outcome] || item.outcome) + " · " + item.seconds + " s"],
      ];
      for (const [cls, text] of cells) {
        const span = document.createElement("span");
        span.className = cls;
        span.textContent = text;
        li.append(span);
      }
      list.prepend(li);
    }
    $("a-empty").hidden = list.children.length > 0;
  } catch (e) { /* the state poll reports a closed page */ }
}

// ---------------------------------------------------------------- Phase 3
// The after-change tables (docs/40 section 21.1). A value is edited in place;
// a colour - three whole numbers 0-255, the form every Heron colour takes -
// also gets a colour picker; an override string such as
// "projection-line-colour=255,0,0; surface-foreground-colour=255,0,0" is split
// into one row per setting and put back together on Apply. Model text only
// ever reaches the page as text.

const RGB = /^ *([0-9]{1,3}) *, *([0-9]{1,3}) *, *([0-9]{1,3}) *$/;
const shown = new Map();   // card id -> { el, lastAt }

function isRgb(v) {
  const m = RGB.exec(v);
  return !!m && [m[1], m[2], m[3]].every(n => Number(n) <= 255);
}

function toHex(v) {
  const m = RGB.exec(v);
  return "#" + [m[1], m[2], m[3]].map(n => Number(n).toString(16).padStart(2, "0")).join("");
}

function fromHex(h) {
  return [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16)).join(",");
}

function isOverrides(v) {
  return v.includes("=") && v.split(";").every(p => p.trim() === "" || p.includes("="));
}

function valueEditor(value) {
  const box = document.createElement("div");
  box.className = "pair";
  const text = document.createElement("input");
  text.type = "text";
  text.value = value;
  if (isRgb(value)) {
    const pick = document.createElement("input");
    pick.type = "color";
    pick.value = toHex(value);
    pick.addEventListener("input", () => { text.value = fromHex(pick.value); });
    text.addEventListener("input", () => { if (isRgb(text.value)) pick.value = toHex(text.value); });
    box.append(pick);
  }
  box.append(text);
  return { box, read: () => text.value.trim() };
}

function rowsFor(row, table) {
  // One table row per setting inside an override string; one row otherwise.
  if (isOverrides(row.value)) {
    const parts = row.value.split(";").map(p => p.trim()).filter(p => p);
    const readers = parts.map(part => {
      const at = part.indexOf("=");
      const key = part.slice(0, at).trim();
      const tr = document.createElement("tr");
      const name = document.createElement("td");
      name.className = "name";
      name.textContent = row.name + " › " + key;
      const cell = document.createElement("td");
      const editor = valueEditor(part.slice(at + 1).trim());
      cell.append(editor.box);
      tr.append(name, cell);
      table.append(tr);
      return () => key + "=" + editor.read();
    });
    return () => readers.map(r => r()).join("; ");
  }
  const tr = document.createElement("tr");
  const name = document.createElement("td");
  name.className = "name";
  name.textContent = row.name;
  const cell = document.createElement("td");
  const editor = valueEditor(row.value);
  cell.append(editor.box);
  tr.append(name, cell);
  table.append(tr);
  return editor.read;
}

function showResult(el, last) {
  el.className = "result " + last.outcome;
  const first = String(last.reply).split("\n").slice(0, 4).join("\n");
  el.textContent = last.at + " · " + (last.outcome === "ok" ? "Applied. " : "") + first +
    (last.outcome === "ok" ? "\nTo take it back, press Ctrl+Z once in Revit." : "");
}

function buildCard(card) {
  const box = document.createElement("div");
  box.className = "change";
  const head = document.createElement("header");
  const cap = document.createElement("span");
  cap.className = "cap";
  cap.textContent = card.capability;
  const where = document.createElement("span");
  where.className = "where";
  where.textContent = (card.document || "") + " · " + card.at;
  head.append(cap, where);

  const table = document.createElement("table");
  const readers = card.rows.map(row => ({ name: row.name, read: rowsFor(row, table) }));

  const foot = document.createElement("footer");
  const button = document.createElement("button");
  button.textContent = "Apply again";
  const result = document.createElement("span");
  result.className = "result";
  if (card.last) showResult(result, card.last);
  button.addEventListener("click", async () => {
    button.disabled = true;
    result.className = "result";
    result.textContent = "Sending to Revit - it waits if the chat is using Revit right now…";
    try {
      const res = await fetch("/api/change/apply", {
        method: "POST",
        headers: Object.assign({ "Content-Type": "application/json" }, HEADER),
        credentials: "same-origin",
        body: JSON.stringify({ id: card.id, values: readers.map(r => ({ name: r.name, value: r.read() })) }),
      });
      const body = await res.json();
      if (body.ok) showResult(result, { at: "now", outcome: body.outcome, reply: body.reply });
      else { result.className = "result failed"; result.textContent = body.error || "Not applied."; }
    } catch (e) {
      result.className = "result failed";
      result.textContent = "The page could not reach the chat. Nothing was sent to Revit.";
    }
    button.disabled = false;
  });
  foot.append(button, result);
  box.append(head, table, foot);
  return box;
}

async function changes() {
  try {
    const res = await fetch("/api/changes", { headers: HEADER, credentials: "same-origin" });
    if (!res.ok) return;
    const body = await res.json();
    const list = $("c-list");
    const ids = new Set(body.cards.map(c => c.id));
    for (const [id, item] of shown) {
      if (!ids.has(id)) { item.el.remove(); shown.delete(id); }
    }
    body.cards.slice().reverse().forEach(card => {
      if (!shown.has(card.id)) {
        const el = buildCard(card);
        list.prepend(el);
        shown.set(card.id, { el });
      }
    });
    $("c-empty").hidden = shown.size > 0;
  } catch (e) { /* the state poll reports a closed page */ }
}

async function pair(code) {
  const res = await fetch("/api/pair", {
    method: "POST",
    headers: Object.assign({ "Content-Type": "application/json" }, HEADER),
    body: JSON.stringify({ code: code }),
    credentials: "same-origin",
  });
  return res.ok;
}

async function poll() {
  try {
    const res = await fetch("/api/state", { headers: HEADER, credentials: "same-origin" });
    if (res.status === 403) {
      pill("Not opened from the chat", "bad");
      notice("Open this page with the Companion button in Revit, or ask the chat to \"open the Heron Companion\".", true);
      return;
    }
    const body = await res.json();
    failures = 0;
    document.body.classList.remove("closed");
    show(body.state);
    activity();
    changes();
  } catch (e) {
    failures += 1;
    if (failures >= 3) {
      document.body.classList.add("closed");
      pill("Closed", "bad");
      notice("This page has stopped: the Companion was switched off in Revit, or the chat " +
             "that opened it has closed. Heron itself is not affected. To open it again, " +
             "press the Companion button in Revit.", true);
    }
  }
  setTimeout(poll, 1000);
}

async function start() {
  const code = new URLSearchParams(location.search).get("pair");
  if (code) {
    // The one-time code leaves the address bar at once, whatever happens next.
    history.replaceState(null, "", "/");
    paired = await pair(code).catch(() => false);
    if (!paired) {
      pill("Link expired", "bad");
      notice("This link has already been used or is too old. Press the Companion button " +
             "in Revit to open a fresh one.", true);
    }
  }
  poll();
}

start();
