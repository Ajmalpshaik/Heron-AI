// Heron Companion page - Phase 1: shows what Revit is doing. Changes nothing.
// Model text (names of models, views, categories) is DATA: it is only ever
// put on the page with textContent, never as HTML (Golden Rule 19).
"use strict";

const HEADER = { "X-Heron-Companion": "1" };

// SWITCHED OFF by the owner (Ajmal PS, 2026-10-04), KEPT for later (docs/40
// s21.6): the Companion shows the HVAC Load Calculation. What is selected in
// Revit, and the Changes tables with the colour books that paint them, are
// kept below and in index.html's templates - nothing here calls them while
// these are false.
const SHOW_SELECTION = false;
const SHOW_CHANGES = false;
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
  ["f-version", "f-model", "f-view", "f-updated"].forEach(id => set(id, null));
  if (!SHOW_SELECTION) return;
  set("s-count", null);
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
  if (SHOW_SELECTION) showSelection(r);
}

// What is selected in Revit - switched off (SHOW_SELECTION), kept for later.
function showSelection(r) {
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

// The activity list sits in a small button in the corner, like a chat
// bubble, and opens into the full list when clicked (the owner's idea,
// 2026-09-29). Closed, it shows the newest action and how many arrived since
// it was last open. Open or closed is remembered in this browser only.
let fresh = false;          // false while the first batch is being drawn
let unseen = 0;

function dockOpen() { return !$("a-panel").hidden; }

function dockLatest(item, counts) {
  const last = $("a-last");
  const chip = document.createElement("span");
  chip.className = "chip " + item.outcome;
  chip.textContent = WORDS[item.outcome] || item.outcome;
  const tool = document.createElement("span");
  tool.className = "dock-tool";
  tool.textContent = item.tool;
  last.replaceChildren(chip, tool);
  if (counts && !dockOpen()) {
    unseen += 1;
    const badge = $("a-new");
    badge.hidden = false;
    badge.textContent = unseen + " new";
  }
}

function dockSet(open) {
  $("a-panel").hidden = !open;
  $("activity").classList.toggle("open", open);
  const toggle = $("a-toggle");
  toggle.setAttribute("aria-expanded", String(open));
  toggle.title = open ? "Make it small again" : "What the chat asked Heron to do - click to open";
  if (open) { unseen = 0; $("a-new").hidden = true; }
  try { localStorage.setItem("heron-companion-activity", open ? "open" : "small"); } catch (e) { /* not kept */ }
}

function dock() {
  let saved = null;
  try { saved = localStorage.getItem("heron-companion-activity"); } catch (e) { /* not kept */ }
  dockSet(saved === "open");
  $("a-toggle").addEventListener("click", () => dockSet(!dockOpen()));
  $("a-close").addEventListener("click", () => { dockSet(false); $("a-toggle").focus(); });
  document.addEventListener("keydown", e => {
    if (e.key === "Escape" && dockOpen()) { dockSet(false); $("a-toggle").focus(); }
  });
}

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
      dockLatest(item, fresh);
    }
    fresh = true;
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
const shown = new Map();   // card id -> { el, key }

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

// Drag one colour onto another to copy it (the owner's idea, 2026-09-29).
// A press that moves more than a few pixels is a drag; one that does not is
// a click and opens the picker as before. Dropping only fills in the other
// row's box - nothing is sent until Apply again is pressed.
function colourUnder(x, y, from) {
  const el = document.elementFromPoint(x, y);
  const pair = el && el.closest(".change .pair");
  const pick = pair && pair.querySelector("input[type=color]");
  return pick && pick !== from ? pick : null;
}

function dragColour(pick) {
  let start = null, ghost = null, over = null, dragged = false;
  const mark = target => {
    if (over) over.closest(".pair").classList.remove("drop-here");
    over = target;
    if (over) over.closest(".pair").classList.add("drop-here");
  };
  const end = () => {
    if (ghost) ghost.remove();
    mark(null);
    document.body.classList.remove("dragging-colour");
    start = ghost = null;
  };
  pick.addEventListener("pointerdown", e => {
    if (e.button !== 0) return;
    start = { x: e.clientX, y: e.clientY };
    dragged = false;
    try { pick.setPointerCapture(e.pointerId); } catch (err) { /* the drag still works without it */ }
  });
  pick.addEventListener("pointermove", e => {
    if (!start) return;
    if (!ghost) {
      if (Math.abs(e.clientX - start.x) + Math.abs(e.clientY - start.y) < 6) return;
      dragged = true;
      ghost = document.createElement("div");
      ghost.className = "colour-ghost";
      ghost.style.setProperty("background-color", pick.value);
      document.body.append(ghost);
      document.body.classList.add("dragging-colour");
    }
    ghost.style.setProperty("left", e.clientX + "px");
    ghost.style.setProperty("top", e.clientY + "px");
    mark(colourUnder(e.clientX, e.clientY, pick));
  });
  pick.addEventListener("pointerup", e => {
    if (!start) return;
    const target = ghost ? colourUnder(e.clientX, e.clientY, pick) : null;
    end();
    if (target) {
      target.value = pick.value;
      target.dispatchEvent(new Event("input"));
      const pair = target.closest(".pair");
      pair.classList.add("dropped");
      setTimeout(() => pair.classList.remove("dropped"), 900);
    }
  });
  pick.addEventListener("pointercancel", end);
  // A drag is not a click: keep the picker closed after one.
  pick.addEventListener("click", e => { if (dragged) { e.preventDefault(); dragged = false; } });
}

// The colour books and the paint button (the owner's idea, 2026-09-29). Five
// fixed books of seven colours, a column of colours picked from the screen or
// mixed by hand, and a round paint button in the top-right corner that holds
// the colour chosen. Dragging the button onto a colour in a change paints it;
// let go, the button springs back to its corner. Like any drop, it only fills
// in the box - nothing is sent until Apply again.
const BOOKS = [
  ["Basic",  ["255,0,0", "255,128,0", "255,255,0", "0,128,0", "0,0,255", "128,0,128", "0,0,0"]],
  ["Neon",   ["255,20,147", "255,95,31", "224,255,0", "57,255,20", "0,255,255", "31,81,255", "188,19,254"]],
  ["Pastel", ["255,179,186", "255,223,186", "255,255,186", "186,255,201", "186,225,255", "204,186,255", "255,186,243"]],
  ["Light",  ["255,204,204", "255,229,204", "255,255,204", "204,255,204", "204,229,255", "229,204,255", "224,224,224"]],
  ["Dark",   ["139,0,0", "139,69,0", "128,128,0", "0,100,0", "0,0,139", "75,0,130", "64,64,64"]],
];
const PICKED_MAX = 7;
let paintRgb = "30,79,184";
let picked = [];

function paintSave() {
  try { localStorage.setItem("heron-companion-paint", JSON.stringify({ now: paintRgb, picked })); } catch (e) { /* not kept */ }
}

function paintUse(rgb) {
  paintRgb = rgb;
  const root = document.documentElement;
  root.style.setProperty("--paint", "rgb(" + rgb + ")");
  const [r, g, b] = rgb.split(",").map(Number);
  $("p-toggle").classList.toggle("light-paint", 0.299 * r + 0.587 * g + 0.114 * b > 170);
  $("p-rgb").textContent = rgb;
  document.querySelectorAll(".paint-chip").forEach(c => c.classList.toggle("on", c.dataset.rgb === rgb));
  paintSave();
}

function paintChip(rgb, name) {
  const chip = document.createElement("button");
  chip.type = "button";
  chip.className = "paint-chip";
  chip.dataset.rgb = rgb;
  chip.style.setProperty("background-color", "rgb(" + rgb + ")");
  chip.title = name + " " + rgb;
  chip.setAttribute("aria-label", name + " " + rgb);
  chip.addEventListener("click", () => paintUse(rgb));
  return chip;
}

function paintBook(name, colours, slots) {
  const book = document.createElement("div");
  book.className = "paint-book";
  const head = document.createElement("h3");
  head.textContent = name;
  const list = document.createElement("ol");
  for (let i = 0; i < slots; i++) {
    const li = document.createElement("li");
    if (colours[i]) li.append(paintChip(colours[i], name));
    else { const gap = document.createElement("div"); gap.className = "paint-empty"; li.append(gap); }
    list.append(li);
  }
  book.append(head, list);
  return book;
}

function paintBooks() {
  const box = $("p-books");
  box.replaceChildren(...BOOKS.map(([name, colours]) => paintBook(name, colours, 7)),
                      paintBook("Picked", picked, PICKED_MAX));
  paintUse(paintRgb);
}

function paintPicked(rgb) {
  picked = [rgb].concat(picked.filter(c => c !== rgb)).slice(0, PICKED_MAX);
  paintBooks();
  paintUse(rgb);
}

function paintOpen() { return !$("p-panel").hidden; }

function paintSet(open) {
  $("p-panel").hidden = !open;
  $("p-toggle").setAttribute("aria-expanded", String(open));
}

// The button follows the pointer while dragged; elementsFromPoint looks
// underneath it, since the button itself is what the pointer is over.
function paintTarget(x, y) {
  const under = document.elementsFromPoint(x, y).find(el => !$("paint").contains(el));
  const pair = under && under.closest(".change .pair");
  return pair && pair.querySelector("input[type=color]");
}

function paintDrag() {
  const button = $("p-toggle");
  let start = null, over = null, dragged = false;
  const mark = target => {
    if (over) over.closest(".pair").classList.remove("drop-here");
    over = target;
    if (over) over.closest(".pair").classList.add("drop-here");
  };
  const home = () => {
    mark(null);
    button.classList.remove("dragging");
    button.style.removeProperty("transform");
    document.body.classList.remove("dragging-colour");
    start = null;
  };
  button.addEventListener("pointerdown", e => {
    if (e.button !== 0) return;
    start = { x: e.clientX, y: e.clientY };
    dragged = false;
    try { button.setPointerCapture(e.pointerId); } catch (err) { /* the drag still works without it */ }
  });
  button.addEventListener("pointermove", e => {
    if (!start) return;
    const dx = e.clientX - start.x, dy = e.clientY - start.y;
    if (!dragged) {
      if (Math.abs(dx) + Math.abs(dy) < 6) return;
      dragged = true;
      button.classList.add("dragging");
      document.body.classList.add("dragging-colour");
    }
    button.style.setProperty("transform", "translate(" + dx + "px," + dy + "px)");
    mark(paintTarget(e.clientX, e.clientY));
  });
  button.addEventListener("pointerup", e => {
    if (!start) return;
    const target = dragged ? paintTarget(e.clientX, e.clientY) : null;
    home();
    if (target) {
      target.value = toHex(paintRgb);
      target.dispatchEvent(new Event("input"));
      const pair = target.closest(".pair");
      pair.classList.add("dropped");
      setTimeout(() => pair.classList.remove("dropped"), 900);
    }
  });
  button.addEventListener("pointercancel", home);
  button.addEventListener("click", e => {
    if (dragged) { e.preventDefault(); dragged = false; return; }
    paintSet(!paintOpen());
  });
}

function paint() {
  try {
    const saved = JSON.parse(localStorage.getItem("heron-companion-paint") || "null");
    if (saved && isRgb(saved.now)) paintRgb = saved.now;
    if (saved && Array.isArray(saved.picked)) picked = saved.picked.filter(isRgb).slice(0, PICKED_MAX);
  } catch (e) { /* start from the defaults */ }
  paintBooks();
  paintDrag();
  $("p-close").addEventListener("click", () => { paintSet(false); $("p-toggle").focus(); });
  document.addEventListener("keydown", e => {
    if (e.key === "Escape" && paintOpen()) { paintSet(false); $("p-toggle").focus(); }
  });
  $("p-own").addEventListener("change", e => paintPicked(fromHex(e.target.value)));
  // Picking from the screen is Chrome and Edge's EyeDropper; where the
  // browser has none, the button stays hidden and Mix your own still works.
  if ("EyeDropper" in window) {
    const eye = $("p-eye");
    eye.hidden = false;
    eye.addEventListener("click", async () => {
      try {
        const got = await new window.EyeDropper().open();
        paintPicked(fromHex(got.sRGBHex));
      } catch (e) { /* the modeller pressed Esc */ }
    });
  }
}

// A true / false value is a tick box (the owner's idea, 2026-09-29), with
// the word beside it. It sends back the same word in the same case it came
// with - "true" stays lower case, "True" stays capitalised.
const BOOL = /^(true|false)$/i;

function tickEditor(value) {
  const box = document.createElement("label");
  box.className = "pair tick";
  const tick = document.createElement("input");
  tick.type = "checkbox";
  tick.checked = value.toLowerCase() === "true";
  const word = document.createElement("span");
  const capital = value[0] === value[0].toUpperCase();
  const read = () => {
    const w = tick.checked ? "true" : "false";
    return capital ? w[0].toUpperCase() + w.slice(1) : w;
  };
  word.textContent = read();
  tick.addEventListener("change", () => { word.textContent = read(); });
  box.append(tick, word);
  return { box, read };
}

function valueEditor(value) {
  if (BOOL.test(value.trim())) return tickEditor(value.trim());
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
    pick.title = "Click to pick a colour, or drag it onto another colour to copy it there";
    dragColour(pick);
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
      result.textContent = "The page lost the answer, so it cannot say whether Revit made the " +
        "change. Look at the model, and at Revit's undo list, before pressing Apply again.";
    }
    button.disabled = false;
  });
  foot.append(button, result);
  box.append(head, table, foot);
  // Unsent edits: any box that no longer holds what the card was built with.
  const built = readers.map(r => r.read());
  box.values = () => readers.map(r => r.read());
  box.edited = () => box.values().some((v, i) => v !== built[i]);
  return box;
}

// What a card holds on the server; when it changes - an Apply from another
// tab, or a newer change by the chat - the card on screen is redrawn.
function cardKey(card) {
  return card.id + "|" + JSON.stringify(card.rows) + "|" +
    (card.last ? card.last.at + "/" + card.last.outcome : "");
}

function changedElsewhere(el) {
  if (el.querySelector(".elsewhere")) return;
  const note = document.createElement("p");
  note.className = "elsewhere";
  note.textContent = "Changed elsewhere - press Apply again only after checking.";
  el.querySelector("header").after(note);
}

function replacedHere(el, gone) {
  const note = document.createElement("p");
  note.className = "elsewhere";
  note.textContent = "The chat has made a newer change to this - it is the card above. " +
    "Your unsent values stay here to copy across; this one can no longer be applied.";
  el.querySelector("header").after(note);
  el.querySelectorAll("button.btn").forEach(b => { b.disabled = true; });
  const dismiss = document.createElement("button");
  dismiss.className = "btn";
  dismiss.textContent = "Dismiss";
  dismiss.addEventListener("click", () => { el.remove(); gone(); });
  note.after(dismiss);
}

async function changes() {
  try {
    const res = await fetch("/api/changes", { headers: HEADER, credentials: "same-origin" });
    if (!res.ok) return;
    const body = await res.json();
    const list = $("c-list");
    const ids = new Set(body.cards.map(c => c.id));
    for (const [id, item] of shown) {
      if (ids.has(id) || item.replaced) continue;
      // A card the chat replaced while the modeller had unsent values in it
      // stays, saying so, so nothing typed is lost (Codex review of #362).
      if (item.el.edited()) { replacedHere(item.el, () => shown.delete(id)); item.replaced = true; continue; }
      item.el.remove(); shown.delete(id);
    }
    body.cards.slice().reverse().forEach(card => {
      const key = cardKey(card);
      const item = shown.get(card.id);
      if (!item) {
        const el = buildCard(card);
        list.prepend(el);
        shown.set(card.id, { el, key });
      } else if (item.key !== key) {
        // Never throw away what the modeller is typing: a card with unsent
        // edits keeps them and says it changed elsewhere; one being typed in
        // waits for the next poll.
        const same = item.el.values().every((v, i) => card.rows[i] && v === card.rows[i].value);
        if (item.el.edited() && !same) { changedElsewhere(item.el); return; }
        if (item.el.contains(document.activeElement)) return;
        const el = buildCard(card);
        item.el.replaceWith(el);
        shown.set(card.id, { el, key });
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
// Edits the modeller typed that Revit has NOT taken - kept when a stale row
// refuses the whole Apply, so the table redraws with them still in it and
// the stale cells marked (Codex review of #362). {id, map}, or null.
let keptEdits = null;

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
  if (edits.has(key)) { input.value = edits.get(key).value; td.classList.add("edited"); }
  input.addEventListener("input", () => {
    if (input.value === (cell.value == null ? "" : cell.value)) edits.delete(key);
    else edits.set(key, { id: row.id, name: name, value: input.value });
    td.classList.toggle("edited", edits.has(key));
    count();
  });
  td.append(input);
  if (!NO_COPY.test(name) && !cell.noCopy) {
    td.classList.add("copyable");
    input.title = "Drag this cell onto another cell in the column to copy its value there";
    input.addEventListener("dragstart", e => e.preventDefault());   // not the browser's text drag
    const handle = document.createElement("span");
    handle.className = "fill";
    handle.title = "Drag up or down to fill the cells you pass over with this value (like Excel)";
    td.append(handle);
  }
  return td;
}

// Drag a cell onto another cell in its column to copy its value there, the
// way a colour swatch is dragged (the owner's idea, 2026-09-29). A press
// that stays inside its own cell is ordinary typing and selecting; leaving
// the cell with the button held starts the drag. The cell dropped on becomes
// an edit like typing makes it - nothing is sent until Apply. Read-only
// cells take nothing. Mark and Type Mark are never copied (docs/40 section
// 21.1): a Mark copied onto another would stop being unique.
const NO_COPY = /^ *(type +)?mark *$/i;

function dragCells(tbody) {
  let from = null, start = null, ghost = null, over = null;
  const target = (x, y) => {
    const el = document.elementFromPoint(x, y);
    const td = el && el.closest("td");
    return td && td !== from && td.parentElement.parentElement === tbody &&
      td.cellIndex === from.cellIndex && td.classList.contains("copyable") ? td : null;
  };
  const mark = td => {
    if (over) over.classList.remove("drop-here");
    over = td;
    if (over) over.classList.add("drop-here");
  };
  const move = e => {
    if (!ghost) {
      const el = document.elementFromPoint(e.clientX, e.clientY);
      if ((el && el.closest("td")) === from ||
          Math.abs(e.clientX - start.x) + Math.abs(e.clientY - start.y) < 6) return;
      const input = from.querySelector("input");
      input.setSelectionRange(input.selectionStart, input.selectionStart);
      ghost = document.createElement("div");
      ghost.className = "cell-ghost";
      ghost.textContent = input.value === "" ? "(empty)" : input.value;
      document.body.append(ghost);
      document.body.classList.add("dragging-cell");
    }
    e.preventDefault();
    ghost.style.setProperty("left", e.clientX + "px");
    ghost.style.setProperty("top", e.clientY + "px");
    mark(target(e.clientX, e.clientY));
  };
  const up = e => {
    const td = ghost ? target(e.clientX, e.clientY) : null;
    if (td) {
      const input = td.querySelector("input");
      input.value = from.querySelector("input").value;
      input.dispatchEvent(new Event("input"));
      td.classList.add("dropped");
      setTimeout(() => td.classList.remove("dropped"), 900);
    }
    if (ghost) ghost.remove();
    mark(null);
    document.body.classList.remove("dragging-cell");
    from = start = ghost = null;
    document.removeEventListener("pointermove", move);
    document.removeEventListener("pointerup", up);
    document.removeEventListener("pointercancel", up);
  };
  tbody.addEventListener("pointerdown", e => {
    const td = e.button === 0 && e.target.closest && e.target.closest("td.copyable");
    if (!td || from || e.target.closest(".fill")) return;
    from = td;
    start = { x: e.clientX, y: e.clientY };
    document.addEventListener("pointermove", move);
    document.addEventListener("pointerup", up);
    document.addEventListener("pointercancel", up);
  });
}

// Fill like Excel: the square that shows only at a cell's bottom-right
// corner. Dragged up or down its column, every cell passed over takes the
// value - as edits, sent only by Apply. Read-only cells are passed over and
// left alone; Mark and Type Mark have no square (NO_COPY above).
function fillDown(tbody) {
  let from = null, range = [];
  const paint = td => {
    range.forEach(c => c.classList.remove("fill-range"));
    range = [];
    if (!td || td.parentElement.parentElement !== tbody || td.cellIndex !== from.cellIndex) return;
    const a = from.parentElement.sectionRowIndex, b = td.parentElement.sectionRowIndex;
    for (let i = Math.min(a, b); i <= Math.max(a, b); i++) {
      const c = tbody.rows[i].cells[from.cellIndex];
      c.classList.add("fill-range");
      range.push(c);
    }
  };
  const move = e => {
    e.preventDefault();
    const el = document.elementFromPoint(e.clientX, e.clientY);
    paint(el && el.closest("td"));
  };
  const up = () => {
    const value = from.querySelector("input").value;
    range.forEach(c => {
      const input = c !== from && c.classList.contains("copyable") && c.querySelector("input");
      if (!input || input.value === value) return;
      input.value = value;
      input.dispatchEvent(new Event("input"));
    });
    range.forEach(c => c.classList.remove("fill-range"));
    range = [];
    from = null;
    document.body.classList.remove("filling");
    document.removeEventListener("pointermove", move);
    document.removeEventListener("pointerup", up);
    document.removeEventListener("pointercancel", up);
  };
  tbody.addEventListener("pointerdown", e => {
    const handle = e.button === 0 && e.target.closest && e.target.closest(".fill");
    if (!handle || from) return;
    e.preventDefault();
    from = handle.closest("td");
    document.body.classList.add("filling");
    paint(from);
    document.addEventListener("pointermove", move);
    document.addEventListener("pointerup", up);
    document.addEventListener("pointercancel", up);
  });
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
  const edits = keptEdits && keptEdits.id === t.id ? new Map(keptEdits.map) : new Map();
  keptEdits = null;
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
  ["Element", "Category", "Family", "Type"].concat(t.columns).forEach((c, i) => {
    const th = document.createElement("th");
    th.scope = "col";
    th.textContent = c;
    if (i === 0) th.className = "num";
    if (i >= 4) {
      const name = t.columns[i - 4];
      if (numeric[name]) th.classList.add("num");
      if (editable[name]) { th.classList.add("edit"); th.title = "You can edit this column"; }
    }
    hr.append(th);
  });
  thead.append(hr);
  const tbody = document.createElement("tbody");
  for (const row of t.rows) {
    const tr = document.createElement("tr");
    [row.id, row.category, row.family, row.type].forEach((v, i) => {
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
  dragCells(tbody);
  fillDown(tbody);
  table.append(thead, tbody);
  wrap.append(table);
  box.append(wrap);

  const legend = document.createElement("p");
  legend.className = "legend";
  [["l-edit", "You can edit"], ["l-fixed", "Read-only - point at it for why"],
   ["l-edited", "Edited, not applied yet"],
   ["l-copy", "Drag a cell onto another in its column to copy it"],
   ["l-fill", "Or drag the square at a cell's bottom-right corner to fill up or down (not Mark or Type Mark)"]].concat(stale.size ? [["l-stale", "Changed in Revit since it was read"]] : [])
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
      // Not applied (a row changed in Revit): redraw with the edits kept.
      if (!body.applied) keptEdits = { id: t.id, map: new Map(edits) };
      tableShown = null;   // re-read: Heron's copy now holds what Revit holds
      await table_();
    } catch (e) {
      result.className = "result failed";
      result.textContent = "The page lost the answer, so it cannot say whether Revit made the " +
        "change. Look at the model, and at Revit's undo list, before pressing Apply again.";
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

// ---------------------------------------------------------------- loads
// The HVAC Load Calculation panel (docs/44 section 7). Every number on it was
// worked out by Heron's brain; the page only shows them, collects typed
// inputs, and posts them back. Model text goes on the page with textContent
// only. The look (2026-10-04): the four steps first, the building's figures,
// the checks and the 3D take-off, then the inputs above the results.

let loadsShown = null;
let loadsBusy = false;

// A Space's own values: its type's eight, and the set points one Space may
// have of its own (docs/44 s5.3). [name, what it is, its unit]
const SET_POINT_FIELDS = [
  ["room_dry_bulb_c", "Room, cooling", "°C"], ["room_rh_pct", "Room humidity", "% RH"],
  ["heating_room_dry_bulb_c", "Room, heating", "°C"], ["supply_dry_bulb_c", "Supply air", "°C"],
];
const PROFILE_FIELDS = [
  ["people_per_m2", "People", "per m²"], ["sensible_w_each", "Sensible heat", "W per person"],
  ["latent_w_each", "Latent heat", "W per person"], ["lighting_w_per_m2", "Lighting", "W/m²"],
  ["equipment_w_per_m2", "Equipment", "W/m²"], ["infiltration_ach", "Infiltration", "air changes/h"],
  ["outdoor_air_ls_per_person", "Outdoor air", "L/s per person"],
  ["outdoor_air_ls_per_m2", "Outdoor air", "L/s per m²"],
];
const SPACE_FIELDS = PROFILE_FIELDS.concat(SET_POINT_FIELDS);

function el(tag, cls, text) {
  const e = document.createElement(tag);
  if (cls) e.className = cls;
  if (text != null) e.textContent = String(text);
  return e;
}

function fixed(v, places) { return v == null || v === "" ? "—" : Number(v).toFixed(places); }

// A number as the brain gave it, its thousands grouped - formatting only, never arithmetic.
function num(v, places) {
  if (v == null || v === "" || isNaN(Number(v))) return "—";
  return Number(v).toLocaleString("en-US", { minimumFractionDigits: places, maximumFractionDigits: places });
}

function valueOf(entry) { return entry && typeof entry === "object" ? entry.value : entry; }
function sourceOf(entry) { return entry && typeof entry === "object" ? (entry.source || "instruction") : "instruction"; }

// Where a value came from, in one word; the whole source is in its tooltip.
function sourceWord(source) {
  const s = String(source || "instruction");
  if (s.startsWith("standard")) return "standard";
  if (s.startsWith("model")) return "model";
  if (s.startsWith("assum")) return "assumed";
  return "you";
}

// Icons are SVG made here, in the page's own SVG namespace - no emoji, no file.
const ICONS = {
  edit: ["M4 20h4L19 9l-4-4L4 16v4z", "M14 6l4 4"],
  check: ["M5 12.5l4.5 4.5L19 7"],
  refresh: ["M20 11a8 8 0 1 0-2.4 5.7", "M20 4v7h-7"],
  eye: ["M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z", "M12 9.2a2.8 2.8 0 1 0 0 5.6 2.8 2.8 0 0 0 0-5.6z"],
  file: ["M7 3h7l5 5v13H7z", "M14 3v5h5", "M10 13h6M10 17h6"],
  open: ["M14 4h6v6", "M20 4l-9 9", "M18 14v6H4V6h6"],
  revit: ["M12 15V4", "M7.5 8.5L12 4l4.5 4.5", "M4 15v5h16v-5"],
};

function icon(paths) {
  const ns = document.querySelector("svg.ico").namespaceURI;
  const svg = document.createElementNS(ns, "svg");
  svg.setAttribute("class", "ico");
  svg.setAttribute("viewBox", "0 0 24 24");
  svg.setAttribute("aria-hidden", "true");
  svg.setAttribute("focusable", "false");
  paths.forEach(d => {
    const p = document.createElementNS(ns, "path");
    p.setAttribute("d", d);
    svg.append(p);
  });
  return svg;
}

function button(label, cls, paths, title) {
  const b = el("button", cls);
  b.type = "button";
  if (paths) b.append(icon(paths));
  b.append(document.createTextNode(label));
  if (title) b.title = title;
  return b;
}

async function loadsPost(path, body, said) {
  loadsBusy = true;
  document.querySelectorAll("#loads button").forEach(b => { b.disabled = true; });
  said.className = "result";
  said.textContent = path.endsWith("finalize") ? "Writing into Revit - it waits if the chat is using Revit right now…"
    : (path.endsWith("report") && body && body.ask)
      ? "Choose where to save the report in the folder window that opened - look behind this page if you cannot see it…"
      : "Working…";
  try {
    const res = await fetch(path, {
      method: "POST",
      headers: Object.assign({ "Content-Type": "application/json" }, HEADER),
      credentials: "same-origin",
      body: JSON.stringify(body || {}),
    });
    const answer = await res.json();
    said.className = answer.ok ? "result ok" : "result failed";
    said.textContent = answer.said || answer.error || (answer.ok ? "Done." : "Not done.");
  } catch (e) {
    said.className = "result failed";
    said.textContent = path.endsWith("finalize")
      ? "The page lost the answer, so it cannot say whether Revit took the values. Look at the model, and at Revit's undo list, before pressing Finalize again."
      : "The page could not reach Heron. Nothing was changed.";
  }
  loadsBusy = false;
  loadsShown = null;
  await loads_();
  const after = $("l-said");
  if (after && said.textContent) { after.className = said.className; after.textContent = said.textContent; }
}

// Model, when it was read, the run - one quiet line under the title.
function loadsMeta(L) {
  const meta = el("div", "l-meta");
  const item = (label, value) => {
    const s = el("span", "l-meta-item");
    s.append(el("span", "l-meta-label", label), el("span", "l-meta-value", value));
    meta.append(s);
  };
  item("Model", L.document || "—");
  item("Read from Revit", L.read_at || L.at || "—");
  item("Calculated", L.run_id ? (L.at || "") + " · run " + L.run_id : "not yet");
  meta.append(el("span", "l-meta-note", "Recalculate reads nothing from Revit. Finalize reads the model again and stops if it changed."));
  return meta;
}

// The four steps, each with its own button: calculate, check the take-off,
// report, and write into Revit. Gate 1 (docs/44 s6): the report is a draft
// and Finalize stays shut until the modeller has checked the take-off.
function loadsSteps(L, project, profiles, overrides, said) {
  const spaces = L.spaces || [];
  const ok = spaces.filter(s => s.status === "ok");
  const terminals = ok.reduce((n, s) => n + (s.terminals || []).length, 0);
  const steps = el("ol", "l-steps");
  steps.setAttribute("aria-label", "The four steps");
  const step = (n, title, state, done, current, controls) => {
    const li = el("li", "l-step" + (done ? " done" : "") + (current ? " current" : ""));
    const mark = el("span", "l-step-n");
    if (done) mark.append(icon(ICONS.check)); else mark.textContent = String(n);
    const text = el("div", "l-step-text");
    text.append(el("span", "l-step-title", title), el("span", "l-step-state", state));
    const act = el("div", "l-step-act");
    controls.filter(Boolean).forEach(c => act.append(c));
    li.append(mark, text, act);
    steps.append(li);
  };

  const recalc = button("Recalculate", "btn-lite", ICONS.refresh,
    "Works out the loads again with the inputs below. Nothing in Revit changes.");
  recalc.addEventListener("click", () => loadsPost("/api/loads/recalculate",
    { project: project, profiles: profiles, overrides: overrides }, said));
  step(1, "Calculate",
       L.run_id ? ok.length + " of " + spaces.length + " Spaces calculated" : "Answer the questions, then Recalculate",
       !!L.run_id && ok.length > 0, !L.run_id || !ok.length, [recalc]);

  const sure = button(L.confirmed ? "Confirmed" : "The take-off is right", "btn-lite", ICONS.eye,
    "I have looked at the 3D view and the checks on the model, and the faces are right.");
  sure.disabled = !L.run_id || !!L.confirmed;
  sure.addEventListener("click", () => {
    if (confirm("Confirm that the faces in the 3D view - walls, roofs, windows, what is beyond each - " +
                "are the building's? The report stops being a draft and Finalize opens.")) {
      loadsPost("/api/loads/confirm", {}, said);
    }
  });
  step(2, "Check the take-off",
       L.confirmed ? "Confirmed - the report is final" : "Look at the 3D view and the checks, then confirm",
       !!L.confirmed, !!L.run_id && ok.length > 0 && !L.confirmed, [sure]);

  const report = button("Report…", "btn-lite", ICONS.file,
    "Asks where to save it, then writes the HVAC load calculation sheet - HTML, PDF and CSV. Nothing in Revit changes.");
  report.disabled = !L.run_id;
  report.addEventListener("click", () => loadsPost("/api/loads/report", { ask: true }, said));
  // Where the sheet went, and the sheet itself in a tab of its own - through a
  // key only this page holds (docs/44 s12.7).
  const opens = [];
  if (L.report && L.report.token) {
    [["html", "Open report"], ["pdf", "Open PDF"]].forEach(([kind, label]) => {
      if (!L.report[kind]) return;
      const a = el("a", "btn-lite");
      a.append(icon(ICONS.open), document.createTextNode(label));
      a.href = "/report/" + encodeURIComponent(L.report.token) + "/" + kind;
      a.target = "_blank";
      a.rel = "noopener";
      opens.push(a);
    });
  }
  step(3, "Report",
       L.report ? "Saved in " + (L.report.folder || "the usual place") + (L.report.pdf ? "" : " - no PDF, the page is the report")
                : (L.confirmed ? "Write the calculation sheet" : "A draft until the take-off is confirmed"),
       !!L.report, !!L.confirmed && !L.report, [report].concat(opens));

  const finalize = button("Finalize to Revit", "btn", ICONS.revit);
  finalize.disabled = !ok.length || !L.confirmed;
  finalize.title = L.confirmed
    ? "Reads the model again, then writes the loads and airflows into " + ok.length + " Spaces and " +
      terminals + " diffusers - one undo entry for the Spaces, and one more if diffusers are written."
    : "Confirm the take-off first - check the 3D view, then press 'The take-off is right'.";
  finalize.addEventListener("click", () => {
    if (confirm(finalize.title + " Go ahead?")) loadsPost("/api/loads/finalize", {}, said);
  });
  step(4, "Write into Revit",
       L.finalized ? "Written at " + L.finalized.at
                   : (L.confirmed ? ok.length + " Spaces and " + terminals + " diffusers" : "Opens once the take-off is confirmed"),
       !!L.finalized, !!L.confirmed && !!L.report && !L.finalized, [finalize]);
  return steps;
}

// The building's figures, as the brain added them up - the page sums nothing.
function loadsFigures(L) {
  const b = L.building || {};
  const spaces = L.spaces || [];
  const refused = spaces.filter(s => s.status === "refused").length;
  const wrap = el("div", "l-figures");
  const fig = (cls, label, value, unit, sub) => {
    const d = el("div", "l-fig " + cls);
    const v = el("div", "l-fig-value");
    v.append(el("span", "l-fig-num", value), el("span", "l-fig-unit", unit));
    d.append(el("div", "l-fig-label", label), v, el("div", "l-fig-sub", sub));
    wrap.append(d);
  };
  fig("cool", "Cooling (AC) load", num(b.block_w, 0), "W",
      num(b.block_tr, 2) + " TR · rooms' block · " + (b.block_when || "—"));
  fig("cool", "At the coil, with outdoor air", num(b.coil_block_w, 0), "W",
      num(b.coil_block_tr, 2) + " TR · " + (b.coil_block_when || "—"));
  fig("heat", "Heating load", num(b.heating_w, 0), "W", "the Spaces' heat losses");
  fig("air", "Supply air", num(b.supply_ls, 0), "L/s", "outdoor air " + num(b.outdoor_air_ls, 0) + " L/s");
  fig("plain", "Cooling per floor area", num(b.block_w_per_m2, 0), "W/m²",
      "over " + num(b.calculated_area_m2, 1) + " m² calculated");
  fig(b.calculated != null && b.calculated < spaces.length ? "bad" : "plain", "Spaces calculated",
      b.calculated == null ? "—" : String(b.calculated), "of " + spaces.length,
      refused ? refused + " refused - see why below" : "none refused");
  return wrap;
}

function loadsChecks(L) {
  const qa = L.qa || [];
  const wrap = el("section", "l-sec l-qa");
  const head = el("h3", null, "Checks on the model");
  ["FAIL", "WARN", "INFO"].forEach(level => {
    const n = qa.filter(f => f.level === level).length;
    if (n) head.append(el("span", "badge " + level.toLowerCase(), n + " " + level));
  });
  wrap.append(head);
  const list = el("ul", "l-qa-list");
  qa.forEach(f => {
    const level = String(f.level);
    const li = el("li", "qa-" + level.toLowerCase());
    li.append(el("span", "badge " + level.toLowerCase(), level), el("span", "qa-text", f.text));
    list.append(li);
  });
  if (!qa.length) list.append(el("li", "muted", "Nothing found."));
  wrap.append(list);
  return wrap;
}

// Asked - what Heron still needs; a standard's figure is OFFERED, never filled in.
function loadsAsked(L, project, profiles) {
  const ask = el("section", "l-sec l-asked");
  ask.append(el("h3", null, "Heron needs these before it calculates anything"));
  const scroll = el("div", "l-scroll");
  const tbl = el("table", "l-table");
  const head = el("tr");
  ["For", "Value", "Unit", "Why", "A standard offers", "Your answer"].forEach(h => head.append(el("th", null, h)));
  tbl.append(head);
  (L.asked || []).forEach(a => {
    const tr = el("tr");
    let input;
    if (a.input.startsWith("beyond:")) {
      // What is beyond a face Revit could not see past: one of four words,
      // which the brain lists in the question itself.
      input = el("select");
      const none = el("option", null, "choose…"); none.value = ""; input.append(none);
      String(a.unit).split(",").flatMap(part => part.split(" or ")).map(w => w.trim()).filter(Boolean)
        .forEach(word => { const o = el("option", null, word); o.value = word; input.append(o); });
      input.addEventListener("change", () => { project[a.input] = input.value || null; });
    } else {
      input = el("input");
      input.type = "text";
      input.addEventListener("input", () => {
        const raw = input.value.trim();
        const v = raw === "" ? null : (isNaN(Number(raw)) ? raw : Number(raw));
        if (a.for === "project") project[a.input] = v;
        else { profiles[a.for] = profiles[a.for] || {}; profiles[a.for][a.input] = v; }
      });
    }
    input.setAttribute("aria-label", a.input + " " + a.for);
    tr.append(el("td", null, a.for === "project" ? "project" : a.for),
              el("td", "id", a.input), el("td", "muted", a.unit),
              el("td", null, a.why), el("td", "small muted", a.offer || "no standard figure held"));
    const cell = el("td"); cell.append(input); tr.append(cell);
    tbl.append(tr);
  });
  scroll.append(tbl);
  ask.append(scroll, el("p", "small muted", "Answer, then press Recalculate in step 1."));
  return ask;
}

// Inputs - one row per Space type, each value editable, where it came from beside it.
function loadsInputs(L, profiles) {
  const wrap = el("section", "l-sec l-inputs");
  wrap.append(el("h3", null, "Inputs per Space type"));
  const scroll = el("div", "l-scroll");
  const tbl = el("table", "l-table l-input-table");
  const head = el("tr");
  head.append(el("th", null, "Space type"));
  PROFILE_FIELDS.forEach(([, label, unit]) => {
    const th = el("th", "num");
    th.append(el("span", "th-main", label), el("span", "th-unit", unit));
    head.append(th);
  });
  tbl.append(head);
  Object.keys(profiles).sort().forEach(key => {
    const tr = el("tr");
    tr.append(el("td", "l-type", key));
    PROFILE_FIELDS.forEach(([name, label, unit]) => {
      const td = el("td", "num");
      const word = sourceWord(sourceOf(profiles[key][name]));
      const input = el("input", "src-" + word);
      input.type = "text";
      input.inputMode = "decimal";
      input.value = valueOf(profiles[key][name]) == null ? "" : valueOf(profiles[key][name]);
      input.setAttribute("aria-label", key + " - " + label + " " + unit);
      input.title = "From: " + sourceOf(profiles[key][name]);
      input.addEventListener("input", () => {
        const raw = input.value.trim();
        profiles[key][name] = raw === "" ? null : (isNaN(Number(raw)) ? raw : Number(raw));
      });
      td.append(input, el("span", "tag src-" + word, word));
      tr.append(td);
    });
    tbl.append(tr);
  });
  scroll.append(tbl);
  const key = el("p", "l-key");
  key.append(el("span", "l-key-title", "Where each value comes from:"));
  [["you", "you said it"], ["standard", "a standard"], ["model", "the Revit model"], ["assumed", "an assumption"]]
    .forEach(([word, text]) => key.append(el("span", "l-key-item src-" + word, text)));
  wrap.append(scroll, key);
  return wrap;
}

// Results - per Space, then zones and the building: block AND sum of peaks.
// Which load each column is (Ajmal, 2026-10-04: "which load - AC load or
// heating load?"): the cooling (AC) load and its peak hour, the heating load,
// then the air it needs.
function loadsResults(L, project, profiles, overrides) {
  const results = el("section", "l-sec l-results");
  results.append(el("h3", null, "Results per Space"));
  const scroll = el("div", "l-scroll");
  const rtbl = el("table", "l-table l-result-table");
  const thead = el("thead");
  const groups = el("tr", "groups");
  [["", 3, ""], ["Cooling (AC) load", 6, "g cool"], ["Heating load", 1, "g heat"], ["Air", 3, "g air"], ["", 1, ""]]
    .forEach(([h, span, cls]) => {
      const th = el("th", cls || null, h);
      th.colSpan = span;
      groups.append(th);
    });
  const rh = el("tr", "cols");
  [["Space", ""], ["Zone", ""], ["m²", "num"], ["Sensible W", "num g"], ["Latent W", "num"], ["Total W", "num"],
   ["W/m²", "num"], ["TR", "num"], ["Peak at", ""], ["W", "num g"], ["Supply L/s", "num g"],
   ["Outdoor air L/s", "num"], ["ACH", "num"], ["Status", "g"]]
    .forEach(([h, cls]) => rh.append(el("th", cls || null, h)));
  thead.append(groups, rh);
  rtbl.append(thead);
  const tbody = el("tbody");
  (L.spaces || []).forEach(s => {
    const v = s.shown || {};
    const tr = el("tr", s.status === "ok" ? null : "off");
    // The Space's name opens it alone in the 3D view, every other ghosted.
    const name = el("td", "l-space");
    const go = el("button", "linkish", ((s.number || "") + " " + (s.name || "")).trim());
    go.type = "button";
    go.title = "Show this Space alone in the 3D view";
    go.addEventListener("click", () => { if (window.HeronLoads3D) window.HeronLoads3D.focus(String(s.id)); });
    // This Space's own values - its people, lights, equipment, outdoor air and
    // set points - beside its Space type's, kept as a change for this Space only.
    const sid = String(s.id);
    const edit = el("button", "icon-btn");
    edit.type = "button";
    edit.append(icon(ICONS.edit));
    edit.title = "Change this Space's own values - kept for this Space only";
    edit.setAttribute("aria-label", "Change the own values of " + ((s.number || "") + " " + (s.name || "")).trim());
    const own = el("tr", "own");
    own.hidden = !overrides[sid];
    const ownCell = el("td");
    ownCell.colSpan = 14;
    const ownBox = el("div", "own-box");
    SPACE_FIELDS.forEach(([field, label, unit]) => {
      const lab = el("label", "own-field");
      const input = el("input");
      input.type = "text";
      input.inputMode = "decimal";
      const mine = overrides[sid] && valueOf(overrides[sid][field]);
      input.value = mine == null ? "" : mine;
      const shared = (profiles[s.profile] && valueOf(profiles[s.profile][field])) ?? valueOf(project[field]);
      input.placeholder = shared == null ? "" : String(shared);
      input.setAttribute("aria-label", sid + " " + field);
      input.addEventListener("input", () => {
        const raw = input.value.trim();
        overrides[sid] = overrides[sid] || {};
        // Blank is sent as nothing, which CLEARS this Space's own value, so its
        // type's value applies again - never left out, which would keep the old one.
        overrides[sid][field] = raw === "" ? null : (isNaN(Number(raw)) ? raw : Number(raw));
      });
      lab.append(el("span", "small muted", label + " (" + unit + ")"), input);
      ownBox.append(lab);
    });
    ownCell.append(el("p", "small muted", "This Space only - blank keeps its Space type's value (shown faint). Press Recalculate."), ownBox);
    own.append(ownCell);
    edit.addEventListener("click", () => {
      own.hidden = !own.hidden;
      edit.setAttribute("aria-expanded", String(!own.hidden));
    });
    edit.setAttribute("aria-expanded", String(!own.hidden));
    name.append(go, edit);
    const status = el("td", "g");
    status.append(el("span", "badge " + (s.status === "ok" ? "ok" : s.status === "refused" ? "fail" : "warn"), s.status));
    if ((s.why || []).length) status.title = s.why.join("\n");
    tr.append(name,
              el("td", "muted", s.zone || "—"), el("td", "num", num(s.area_m2, 1)),
              el("td", "num g", num(v.sensible_w, 0)), el("td", "num", num(v.latent_w, 0)),
              el("td", "num strong", num(v.total_w, 0)), el("td", "num", num(v.w_per_m2, 1)),
              el("td", "num", num(v.tr, 2)), el("td", "muted", v.peak || "—"),
              el("td", "num g strong", num(v.heating_w, 0)),
              el("td", "num g", num(v.supply_ls, 1)), el("td", "num", num(v.outdoor_air_ls, 1)),
              el("td", "num", num(v.ach, 1)), status);
    tbody.append(tr, own);
    if (s.status !== "ok" && (s.why || []).length) {
      const why = el("tr", "why");
      const td = el("td", "small", s.why.join(" · "));
      td.colSpan = 14;
      why.append(td);
      tbody.append(why);
    }
  });
  rtbl.append(tbody);
  const tfoot = el("tfoot");
  const total = (label, z, cls) => {
    const tr = el("tr", "sum" + (cls ? " " + cls : ""));
    // The sum of peaks is not a sensible or a latent figure: it spans both
    // columns, beside the block load it is NOT (the Total W column).
    const peaks = el("td", "num g small muted", "sum of peaks " + num(z.sum_of_peaks_w, 0) + " W");
    peaks.colSpan = 2;
    tr.append(el("td", null, label), el("td", null, ""), el("td", "num", num(z.area_m2, 1)), peaks,
              el("td", "num strong", num(z.block_w, 0)), el("td", "num", num(z.block_w_per_m2, 1)),
              el("td", "num", num(z.block_tr, 2)), el("td", "muted", z.block_when || "—"),
              el("td", "num g strong", num(z.heating_w, 0)),
              el("td", "num g", num(z.supply_ls, 1)), el("td", "num", num(z.outdoor_air_ls, 1)),
              el("td", null, ""), el("td", "g small muted",
                "coil " + num(z.coil_block_w, 0) + " W, " + (z.coil_block_when || "—")));
    tfoot.append(tr);
  };
  (L.zones || []).forEach(z => total("Zone " + z.name, z));
  if (L.building) total("Building", L.building, "building");
  rtbl.append(tfoot);
  scroll.append(rtbl);
  results.append(scroll, el("p", "small muted",
    "Total W in a zone or building row is its block load - the largest hour of the Spaces added together, " +
    "not their peaks added up. Coil: with the outdoor air at the coil. Click a Space to see it alone in the 3D view; " +
    "the pencil gives it values of its own."));
  return results;
}

// Glass by the way it faces - added up by the brain from the take-off.
function loadsGlass(L) {
  const glass = el("section", "l-sec l-glass");
  glass.append(el("h3", null, "Glass to outside, by the way it faces"));
  const gt = el("table", "l-table");
  const gh = el("tr");
  ["Facing", "Outside wall m²", "Glass m²", "Glass % of wall"].forEach((h, i) => gh.append(el("th", i ? "num" : null, h)));
  gt.append(gh);
  ["N", "E", "S", "W"].forEach(q => {
    const g = L.summary.glass_by_facing[q] || {};
    const tr = el("tr");
    tr.append(el("td", null, q), el("td", "num", num(g.wall_m2, 1)), el("td", "num", num(g.glass_m2, 1)),
              el("td", "num", num(g.glass_pct_of_wall, 1)));
    gt.append(tr);
  });
  glass.append(gt, el("p", "small muted", "Glass is " + num(L.summary.glass_pct_of_floor, 1) +
    " % of the " + num(L.summary.floor_m2, 1) + " m² of floor placed."));
  return glass;
}

// What the run is and is not, what Revit holds after Finalize, earlier runs.
function loadsMore(L) {
  const more = el("section", "l-sec l-more");
  const notes = el("details", "l-notes");
  notes.append(el("summary", null, "What this calculation is, and what it is not"));
  (L.notes || []).forEach(n => notes.append(el("p", "small", n)));
  more.append(notes);
  if (L.finalized) {
    const d = el("details", "l-back");
    d.open = true;
    d.append(el("summary", null, "Written into Revit at " + L.finalized.at));
    d.append(el("p", "small", L.finalized.said || ""));
    // What Revit holds now, read back after the write - beside what was calculated.
    ((L.finalized.values && L.finalized.values.read_back_text) || []).forEach(t => d.append(el("pre", "small", t)));
    more.append(d);
  }
  // Runs - every Recalculate is kept; an earlier one opens read-only.
  const runs = el("details", "l-runs");
  runs.append(el("summary", null, "Earlier runs"));
  runs.addEventListener("toggle", async () => {
    if (!runs.open || runs.dataset.read) return;
    runs.dataset.read = "1";
    try {
      const res = await fetch("/api/loads/runs", { headers: HEADER, credentials: "same-origin" });
      const list = (await res.json()).runs || [];
      if (!list.length) runs.append(el("p", "small muted", "No run is kept for this project yet."));
      list.forEach(r => runs.append(el("p", "small", r.run_id + " · " + (r.when || "") +
        " · block " + num(r.block_w, 0) + " W" + (r.run_id === L.run_id ? " (this one)" : ""))));
    } catch (e) { runs.append(el("p", "small muted", "The runs could not be read.")); }
  });
  more.append(runs);
  return more;
}

function renderLoads(L) {
  const box = $("l-box");
  const below = $("l-box2");
  box.replaceChildren();
  below.replaceChildren();
  $("l-empty").hidden = !!L;
  if (!L) return;
  const project = Object.assign({}, L.project || {});
  const profiles = JSON.parse(JSON.stringify(L.profiles || {}));
  const overrides = JSON.parse(JSON.stringify(L.overrides || {}));
  const said = el("p", "result");
  said.id = "l-said";
  said.setAttribute("role", "status");
  said.setAttribute("aria-live", "polite");
  box.append(loadsMeta(L), loadsSteps(L, project, profiles, overrides, said), said);
  if ((L.asked || []).length) box.append(loadsAsked(L, project, profiles));
  box.append(loadsFigures(L), loadsChecks(L));
  if (L.has_view) box.append(el("h3", "l-3d-title", "3D take-off - the faces these loads were worked out from"));
  below.append(loadsInputs(L, profiles), loadsResults(L, project, profiles, overrides));
  const extra = el("div", "l-extra");
  if (L.summary && L.summary.glass_by_facing) extra.append(loadsGlass(L));
  extra.append(loadsMore(L));
  below.append(extra);
}

async function loads_() {
  if (loadsBusy) return;
  // An input being typed is never wiped by the poll.
  if (document.activeElement && document.activeElement.closest && document.activeElement.closest("#loads")) return;
  try {
    const res = await fetch("/api/loads", { headers: HEADER, credentials: "same-origin" });
    if (!res.ok) return;
    const L = (await res.json()).loads;
    const key = L ? (L.run_id || "") + "/" + L.at + "/" + (L.report ? "r" : "") + (L.confirmed ? "c" : "") +
      (L.finalized ? L.finalized.at : "") : "none";
    if (key === loadsShown) return;
    loadsShown = key;
    renderLoads(L);
    // The 3D view keeps its own camera across redraws; it reloads only for a new run.
    if (window.HeronLoads3D) window.HeronLoads3D.update($("l-3d"), L && L.has_view ? (L.run_id || "") + "/" + L.at : null);
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
    if (SHOW_CHANGES) changes();
    table_();
    loads_();
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

// The light / dark switch. It starts from Windows' own setting; a choice made
// with the button is remembered in this browser only.
function theme() {
  const system = window.matchMedia("(prefers-color-scheme: dark)");
  let saved = null;
  try { saved = localStorage.getItem("heron-companion-theme"); } catch (e) { /* not kept */ }
  const button = $("theme");
  const use = mode => {
    document.documentElement.dataset.theme = mode;
    const next = mode === "dark" ? "light" : "dark";
    button.title = "Switch to " + next;
    button.setAttribute("aria-label", "Switch to " + next);
  };
  use(saved === "dark" || saved === "light" ? saved : (system.matches ? "dark" : "light"));
  system.addEventListener("change", e => { if (!saved) use(e.matches ? "dark" : "light"); });
  button.addEventListener("click", () => {
    saved = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
    try { localStorage.setItem("heron-companion-theme", saved); } catch (e) { /* not kept */ }
    use(saved);
  });
}

async function start() {
  theme();
  dock();
  if (SHOW_CHANGES) paint();
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

// The Load and Clear buttons: the page asks Heron itself - never the chat -
// to READ the active view and put its filters or category colours here as
// tables, or to forget every table. Neither changes anything in Revit.
async function tableTool(button, path, body) {
  const said = $("c-said");
  document.querySelectorAll(".c-tools button").forEach(b => { b.disabled = true; });
  said.className = "result";
  said.textContent = path.endsWith("clear") ? "Clearing..." : "Reading Revit's active view...";
  try {
    const res = await fetch(path, {
      method: "POST",
      headers: Object.assign({ "Content-Type": "application/json" }, HEADER),
      credentials: "same-origin",
      body: JSON.stringify(body || {}),
    });
    const answer = await res.json();
    if (!answer.ok) { said.className = "result failed"; said.textContent = answer.error || "Not done."; }
    else said.textContent = answer.reply || "Every table was removed. Nothing in Revit changed.";
  } catch (e) {
    said.className = "result failed";
    said.textContent = "The page could not reach Heron. Nothing was read or changed.";
  }
  document.querySelectorAll(".c-tools button").forEach(b => { b.disabled = false; });
  changes();
}

if (SHOW_CHANGES) {
  $("c-load-filters").addEventListener("click", () => tableTool(null, "/api/changes/load", { kind: "filters" }));
  $("c-load-categories").addEventListener("click", () => tableTool(null, "/api/changes/load", { kind: "categories" }));
  $("c-clear").addEventListener("click", () => tableTool(null, "/api/changes/clear"));
}

start();
