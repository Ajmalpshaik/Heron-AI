// Heron Companion - the Sprinkler panel's 3D view (docs/46 section 7).
//
// Draws the pipes and heads the hydraulic calculation solved, exactly as the
// brain described them (brain/heron_sprinkler_view.py): every colour, legend,
// mode and line of text arrives in the data, and this file only projects and
// paints them (mcp/companion README rule 4). Nothing is fetched from anywhere
// but this page's own server, and model text reaches the screen as text only -
// through textContent, or drawn on the canvas (Golden Rule 19).
"use strict";

(function () {
  const HEADER = { "X-Heron-Companion": "1" };
  const FOV = 40 * Math.PI / 180;

  let root = null, canvas = null, ctx = null;
  let view = null;
  let drawn = [];           // what the last frame drew, for picking
  let shownKey = null;
  let selected = null;
  let mode = "size";
  let pending = false;
  const cam = { yaw: 35, pitch: 32, dist: 30, target: [0, 0, 0] };
  const show = { levels: {}, heads: true, outlines: true };

  function el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = String(text);
    return e;
  }

  const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
  const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
  const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
  function unit(v) { const n = Math.hypot(v[0], v[1], v[2]) || 1; return [v[0] / n, v[1] / n, v[2] / n]; }

  function eye() {
    const y = cam.yaw * Math.PI / 180, p = cam.pitch * Math.PI / 180;
    const d = [Math.cos(p) * Math.sin(y), -Math.cos(p) * Math.cos(y), Math.sin(p)];
    return [cam.target[0] + d[0] * cam.dist, cam.target[1] + d[1] * cam.dist, cam.target[2] + d[2] * cam.dist];
  }

  function basis() {
    const e = eye();
    const f = unit(sub(cam.target, e));
    let r = cross(f, [0, 0, 1]);
    if (Math.hypot(r[0], r[1], r[2]) < 1e-6) r = [1, 0, 0];
    r = unit(r);
    return { e, f, r, u: cross(r, f) };
  }

  function points() {
    const out = [];
    for (const s of view.segments) { if (s.a) out.push(s.a); if (s.b) out.push(s.b); }
    for (const p of view.points) if (p.at) out.push(p.at);
    for (const o of view.outlines || []) for (const p of o.points) out.push(p);
    return out;
  }

  function fit() {
    const all = points();
    if (!all.length) return;
    const lo = [Infinity, Infinity, Infinity], hi = [-Infinity, -Infinity, -Infinity];
    for (const v of all) for (let k = 0; k < 3; k++) { lo[k] = Math.min(lo[k], v[k]); hi[k] = Math.max(hi[k], v[k]); }
    cam.target = [(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, (lo[2] + hi[2]) / 2];
    const size = Math.hypot(hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2]) || 10;
    cam.dist = size / (2 * Math.tan(FOV / 2)) * 1.2;
  }

  function onLevel(x) { return show.levels[x.level || "(no level)"] !== false; }

  function colour(x) { return (x.colour && (x.colour[mode] || x.colour.size)) || view.dry || "#999999"; }

  function frame() {
    pending = false;
    if (!canvas || !view) return;
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.clientWidth, h = canvas.clientHeight;
    if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
      canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
    }
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);
    const B = basis();
    const focal = (h / 2) / Math.tan(FOV / 2);
    const project = v => {
      const d = sub(v, B.e);
      const z = dot(d, B.f);
      if (z <= 0.05) return null;
      return [w / 2 + dot(d, B.r) / z * focal, h / 2 - dot(d, B.u) / z * focal, z];
    };
    // Each Space's outline at its heads' height - a thin dashed line, under the pipes.
    if (show.outlines) {
      ctx.setLineDash([6, 4]);
      for (const o of view.outlines || []) {
        if (!onLevel(o)) continue;
        const pts = o.points.map(project);
        if (pts.some(p => !p) || pts.length < 3) continue;
        ctx.beginPath(); ctx.moveTo(pts[0][0], pts[0][1]);
        for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i][0], pts[i][1]);
        ctx.closePath(); ctx.lineWidth = 1; ctx.strokeStyle = o.colour || "#7a8796"; ctx.stroke();
      }
      ctx.setLineDash([]);
    }
    const list = [];
    for (const s of view.segments) {
      if (!s.a || !s.b || !onLevel(s)) continue;
      const a = project(s.a), b = project(s.b);
      if (!a || !b) continue;
      list.push({ kind: "pipe", item: s, a, b, depth: (a[2] + b[2]) / 2 });
    }
    if (show.heads) {
      for (const p of view.points) {
        if (!p.at || !onLevel(p)) continue;
        const s = project(p.at);
        if (s) list.push({ kind: "head", item: p, s, depth: s[2] - 0.01 });
      }
    }
    list.sort((x, y) => y.depth - x.depth);
    for (const d of list) {
      const hit = d.item.id === selected;
      if (d.kind === "pipe") {
        ctx.beginPath(); ctx.moveTo(d.a[0], d.a[1]); ctx.lineTo(d.b[0], d.b[1]);
        ctx.lineWidth = hit ? 6 : 3; ctx.lineCap = "round";
        ctx.strokeStyle = hit ? "#ff2d95" : colour(d.item); ctx.stroke();
      } else {
        ctx.beginPath(); ctx.arc(d.s[0], d.s[1], hit ? 7 : 5, 0, 2 * Math.PI);
        ctx.fillStyle = colour(d.item); ctx.fill();
        ctx.lineWidth = hit ? 3 : 1; ctx.strokeStyle = hit ? "#ff2d95" : "rgba(0,0,0,0.5)"; ctx.stroke();
      }
    }
    if (view.source && view.source.at) {
      const s = project(view.source.at);
      if (s) {
        ctx.beginPath(); ctx.rect(s[0] - 6, s[1] - 6, 12, 12);
        ctx.fillStyle = view.source.colour || "#2d3748"; ctx.fill();
        ctx.font = "11px system-ui, Segoe UI, sans-serif"; ctx.textAlign = "left"; ctx.textBaseline = "middle";
        ctx.fillText("source", s[0] + 9, s[1]);
      }
    }
    drawn = list;
  }

  function redraw() { if (pending) return; pending = true; requestAnimationFrame(frame); }

  function near(px, py, a, b) {
    const dx = b[0] - a[0], dy = b[1] - a[1];
    const len = dx * dx + dy * dy || 1;
    const t = Math.max(0, Math.min(1, ((px - a[0]) * dx + (py - a[1]) * dy) / len));
    return Math.hypot(px - (a[0] + t * dx), py - (a[1] + t * dy));
  }

  function pick(x, y) {
    let best = null, gap = 8;
    for (let i = drawn.length - 1; i >= 0; i--) {
      const d = drawn[i];
      const g = d.kind === "head" ? Math.hypot(x - d.s[0], y - d.s[1]) - 2 : near(x, y, d.a, d.b);
      if (g < gap) { gap = g; best = d.item; }
    }
    return best;
  }

  function describe(item) {
    const info = root.querySelector(".l3d-info");
    info.replaceChildren();
    if (!item) { info.append(el("p", "muted small", "Click a pipe or a head to see its figures.")); return; }
    const dl = el("dl");
    for (const [k, v] of item.info || []) dl.append(el("dt", null, k), el("dd", null, v));
    info.append(dl);
  }

  function legend() {
    const box = root.querySelector(".l3d-legend");
    box.replaceChildren();
    const m = view.modes.find(x => x.key === mode);
    for (const row of (m && m.legend) || []) {
      const line = el("div", "l3d-key");
      const sw = el("i"); sw.style.background = row[0];
      line.append(sw, el("span", null, row[1]));
      box.append(line);
    }
  }

  function tick(label, get, set) {
    const lab = el("label", "l3d-tick");
    const box = el("input"); box.type = "checkbox"; box.checked = get();
    box.addEventListener("change", () => { set(box.checked); redraw(); });
    lab.append(box, el("span", null, label));
    return lab;
  }

  function controls() {
    const side = root.querySelector(".l3d-side");
    side.replaceChildren();
    if (!view.modes.find(x => x.key === mode)) mode = view.modes[0].key;
    const pickMode = el("select");
    pickMode.setAttribute("aria-label", "Colour by");
    for (const m of view.modes) { const o = el("option", null, m.label); o.value = m.key; pickMode.append(o); }
    pickMode.value = mode;
    pickMode.addEventListener("change", () => { mode = pickMode.value; legend(); redraw(); });
    const row = el("label", "l3d-row"); row.append(el("span", null, "Colour by"), pickMode);
    side.append(row);
    const ticks = el("div", "l3d-ticks");
    for (const lv of view.levels) {
      if (!(lv in show.levels)) show.levels[lv] = true;
      ticks.append(tick(lv, () => show.levels[lv] !== false, v => { show.levels[lv] = v; }));
    }
    ticks.append(tick("Sprinklers", () => show.heads, v => { show.heads = v; }));
    if ((view.outlines || []).length) ticks.append(tick("Space outlines", () => show.outlines, v => { show.outlines = v; }));
    side.append(ticks);
    const buttons = el("div", "l3d-buttons");
    const button = (text, title, go) => { const b = el("button", null, text); b.type = "button"; b.title = title; b.addEventListener("click", go); buttons.append(b); };
    button("Top", "Look straight down - plan view", () => { cam.pitch = 89.5; cam.yaw = 0; redraw(); });
    button("3D", "Back to the corner view", () => { cam.pitch = 32; cam.yaw = 35; redraw(); });
    button("Fit", "Fit the whole system", () => { fit(); redraw(); });
    button("+", "Closer", () => { cam.dist *= 0.8; redraw(); });
    button("−", "Further", () => { cam.dist *= 1.25; redraw(); });
    side.append(buttons, el("div", "l3d-legend"), el("div", "l3d-info"));
    legend();
    describe(null);
  }

  function wire() {
    const pointers = new Map();
    let moved = 0;
    canvas.addEventListener("pointerdown", e => {
      try { canvas.setPointerCapture(e.pointerId); } catch (err) { /* not capturable */ }
      pointers.set(e.pointerId, [e.clientX, e.clientY, e.button, e.shiftKey || e.ctrlKey]);
      moved = 0;
    });
    canvas.addEventListener("pointermove", e => {
      const was = pointers.get(e.pointerId);
      if (!was) return;
      const dx = e.clientX - was[0], dy = e.clientY - was[1];
      moved += Math.abs(dx) + Math.abs(dy);
      pointers.set(e.pointerId, [e.clientX, e.clientY, was[2], was[3]]);
      if (was[2] === 2 || was[3]) {
        const B = basis();
        const k = cam.dist * 0.0015;
        for (let i = 0; i < 3; i++) cam.target[i] += (-B.r[i] * dx + B.u[i] * dy) * k;
      } else {
        cam.yaw -= dx * 0.4;
        cam.pitch = Math.max(2, Math.min(89.5, cam.pitch + dy * 0.4));
      }
      redraw();
    });
    const end = e => {
      const was = pointers.get(e.pointerId);
      pointers.delete(e.pointerId);
      if (was && moved < 5 && e.type === "pointerup") {
        const r = canvas.getBoundingClientRect();
        const item = pick(e.clientX - r.left, e.clientY - r.top);
        selected = item ? item.id : null;
        describe(item);
        redraw();
      }
    };
    canvas.addEventListener("pointerup", end);
    canvas.addEventListener("pointercancel", end);
    canvas.addEventListener("contextmenu", e => e.preventDefault());
    canvas.addEventListener("wheel", e => { e.preventDefault(); cam.dist *= Math.exp(e.deltaY * 0.0012); redraw(); }, { passive: false });
    window.addEventListener("resize", redraw);
  }

  function mount() {
    root.replaceChildren();
    const stage = el("div", "l3d-stage");
    canvas = el("canvas");
    canvas.setAttribute("aria-label", "3D view of the pipes and sprinklers the hydraulic calculation solved");
    stage.append(canvas);
    root.append(stage, el("div", "l3d-side"));
    ctx = canvas.getContext("2d");
    wire();
  }

  async function load() {
    try {
      const res = await fetch("/api/sprinkler/view", { headers: HEADER, credentials: "same-origin" });
      if (!res.ok) return;
      const got = (await res.json()).view;
      if (!got) { root.hidden = true; return; }
      const first = !view;
      view = got;
      if (!canvas) mount();
      if (first) fit();
      controls();
      root.hidden = false;
      redraw();
    } catch (e) { /* the state poll reports a closed page */ }
  }

  window.HeronSprinkler3D = {
    // Called by companion.js whenever the Sprinkler panel shows a different run.
    update(container, key) {
      root = container;
      if (key === shownKey) return;
      shownKey = key;
      if (!key) { root.hidden = true; return; }
      load();
    },
  };
})();
