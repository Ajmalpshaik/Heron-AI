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
  const most = rows.length ? Math.max(1, rows[0][1]) : 1;
  list.replaceChildren(...rows.map(([name, n]) => {
    const li = document.createElement("li");
    const a = document.createElement("span");
    const b = document.createElement("span");
    a.textContent = name;
    b.textContent = n;
    // A bar showing each category's share of the largest - drawn only.
    const share = document.createElement("span");
    share.className = "share";
    const bar = document.createElement("i");
    bar.style.setProperty("--share", Math.round(100 * Number(n) / most) + "%");
    share.append(bar);
    li.append(a, b, share);
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
      ];
      for (const [cls, text] of cells) {
        const span = document.createElement("span");
        span.className = cls;
        span.textContent = text;
        li.append(span);
      }
      // How it ended, as a chip that says it in words, then how long it took.
      const end = document.createElement("span");
      end.className = "end " + item.outcome;
      const chip = document.createElement("span");
      chip.className = "chip " + item.outcome;
      chip.textContent = WORDS[item.outcome] || item.outcome;
      end.append(chip, item.seconds + " s");
      li.append(end);
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
  button.className = "btn";
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

// ---------------------------------------------------------------- Phase 3
// The element table (docs/40 section 8): one row per element, one column per
// parameter the chat asked for. An editable cell is a text box; a cell Heron
// marked not editable is plain text with the reason on hover. Only changed
// cells are sent, and the old value is taken from Heron's own copy, never
// from this page - so nothing here can claim a value Revit did not show.

let tableShown = null;     // "<id>/<last.at>" of what is on screen

function tableCell(row, name, cell, edits, count) {
  const td = document.createElement("td");
  if (!cell) return td;
  if (!cell.editable) {
    td.className = "fixed";
    td.textContent = cell.value == null ? "" : cell.value;
    td.title = cell.why || "";
    return td;
  }
  const input = document.createElement("input");
  input.type = "text";
  input.value = cell.value == null ? "" : cell.value;
  const key = row.id + "/" + name;
  input.addEventListener("input", () => {
    if (input.value === (cell.value == null ? "" : cell.value)) edits.delete(key);
    else edits.set(key, { id: row.id, name: name, value: input.value });
    td.classList.toggle("edited", edits.has(key));
    count();
  });
  td.append(input);
  return td;
}

function renderTable(t) {
  const box = $("t-box");
  box.replaceChildren();
  $("t-empty").hidden = !!t;
  if (!t) return;

  const head = document.createElement("p");
  head.className = "where";
  [t.rows.length + " element(s) in " + (t.document || ""), "opened " + t.at]
    .concat(t.truncated ? [t.truncated + " more were left out"] : [])
    .forEach(text => {
      const span = document.createElement("span");
      span.textContent = text;
      head.append(span);
    });
  box.append(head);

  const stale = new Map((t.last && t.last.stale || []).map(s => [s.id + "/" + s.name, s.why]));
  const edits = new Map();
  const button = document.createElement("button");
  button.className = "btn";
  const count = () => {
    button.textContent = edits.size ? "Apply " + edits.size + " change(s)" : "Apply";
    button.disabled = edits.size === 0;
  };

  const wrap = document.createElement("div");
  wrap.className = "grid";
  // How a column is drawn: right-aligned when every value in it is a number,
  // marked as editable when any of its cells is. Presentation only.
  const NUM = /^ *-?[0-9]+(?:[.,][0-9]+)? *$/;
  const cellsOf = name => t.rows.map(r => (r.cells || {})[name]).filter(c => c);
  const numeric = {}, editable = {};
  for (const name of t.columns) {
    const cells = cellsOf(name);
    const values = cells.map(c => c.value).filter(v => v != null && v !== "");
    numeric[name] = values.length > 0 && values.every(v => NUM.test(String(v)));
    editable[name] = cells.some(c => c.editable);
  }

  const table = document.createElement("table");
  const thead = document.createElement("thead");
  const hr = document.createElement("tr");
  ["Element", "Category", "Type"].concat(t.columns).forEach((c, i) => {
    const th = document.createElement("th");
    th.scope = "col";
    th.textContent = c;
    if (i === 0) th.className = "num";
    if (i >= 3) {
      const name = t.columns[i - 3];
      if (numeric[name]) th.classList.add("num");
      if (editable[name]) { th.classList.add("edit"); th.title = "You can edit this column"; }
    }
    hr.append(th);
  });
  thead.append(hr);
  const tbody = document.createElement("tbody");
  for (const row of t.rows) {
    const tr = document.createElement("tr");
    [row.id, row.category, row.type].forEach((v, i) => {
      const td = document.createElement("td");
      td.className = i === 0 ? "fixed id num" : "fixed";
      td.textContent = v == null ? "" : v;
      tr.append(td);
    });
    for (const name of t.columns) {
      const td = tableCell(row, name, (row.cells || {})[name], edits, count);
      if (numeric[name]) td.classList.add("num");
      const why = stale.get(row.id + "/" + name);
      if (why) { td.classList.add("stale"); td.title = why; }
      tr.append(td);
    }
    tbody.append(tr);
  }
  table.append(thead, tbody);
  wrap.append(table);
  box.append(wrap);

  const legend = document.createElement("p");
  legend.className = "legend";
  [["l-edit", "You can edit"], ["l-fixed", "Read-only - point at it for why"],
   ["l-edited", "Edited, not applied yet"]].concat(stale.size ? [["l-stale", "Changed in Revit since it was read"]] : [])
    .forEach(([cls, text]) => {
      const span = document.createElement("span");
      span.className = cls;
      span.textContent = text;
      legend.append(span);
    });
  box.append(legend);

  const foot = document.createElement("footer");
  foot.className = "actions";
  const result = document.createElement("span");
  result.className = "result";
  if (t.last) {
    result.className = "result " + t.last.outcome;
    result.textContent = t.last.at + " · " + (t.last.outcome === "ok"
      ? "Applied - the values shown are what Revit now holds. To take it back, press Ctrl+Z once in Revit."
      : String(t.last.reply).split("\n").slice(0, 4).join("\n"));
  }
  count();
  button.addEventListener("click", async () => {
    button.disabled = true;
    result.className = "result";
    result.textContent = "Sending to Revit - it waits if the chat is using Revit right now…";
    try {
      const res = await fetch("/api/table/apply", {
        method: "POST",
        headers: Object.assign({ "Content-Type": "application/json" }, HEADER),
        credentials: "same-origin",
        body: JSON.stringify({ id: t.id, changes: Array.from(edits.values()) }),
      });
      const body = await res.json();
      if (!body.ok) { result.className = "result failed"; result.textContent = body.error || "Not applied."; count(); return; }
      tableShown = null;   // re-read: Heron's copy now holds what Revit holds
      await table_();
    } catch (e) {
      result.className = "result failed";
      result.textContent = "The page could not reach the chat. Nothing was sent to Revit.";
      count();
    }
  });
  foot.append(button, result);
  box.append(foot);
}

async function table_() {
  try {
    const res = await fetch("/api/table", { headers: HEADER, credentials: "same-origin" });
    if (!res.ok) return;
    const t = (await res.json()).table;
    const key = t ? t.id + "/" + (t.last ? t.last.at + t.last.outcome : "") : "none";
    if (key === tableShown) return;
    tableShown = key;
    renderTable(t);
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
    table_();
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
