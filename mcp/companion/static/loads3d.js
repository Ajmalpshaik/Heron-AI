// Heron Companion - the Loads panel's 3D view (docs/44 section 12).
//
// Draws the faces a building's loads were worked out from, exactly as the
// brain described them (brain/heron_loads_view.py): every colour, legend,
// category and sentence arrives in the data, and this file only projects and
// paints them (mcp/companion README rule 4). Nothing is fetched from anywhere
// but this page's own server, and model text reaches the screen as text only -
// through textContent, or drawn on the canvas (Golden Rule 19).
"use strict";

(function () {
  const HEADER = { "X-Heron-Companion": "1" };
  const FOV = 40 * Math.PI / 180;

  let root = null;          // the container in the Loads card
  let canvas = null, ctx = null;
  let view = null;          // what the brain sent
  let polys = [];           // faces prepared for drawing
  let drawn = [];           // what the last frame drew, nearest last - for picking
  let shownKey = null;
  let selected = null;
  let focusSpace = null;
  const cam = { yaw: 35, pitch: 32, dist: 30, target: [0, 0, 0] };
  const show = { levels: {}, roofs: true, floors: true, partitions: false, openings: true,
                 xray: false, labels: true };
  let mode = "type";
  let pending = false;

  function el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = String(text);
    return e;
  }

  // ---------------------------------------------------------------- vectors
  const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
  const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
  const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
  function unit(v) { const n = Math.hypot(v[0], v[1], v[2]) || 1; return [v[0] / n, v[1] / n, v[2] / n]; }

  function newell(loop) {
    const n = [0, 0, 0];
    for (let i = 0; i < loop.length; i++) {
      const a = loop[i], b = loop[(i + 1) % loop.length];
      n[0] += (a[1] - b[1]) * (a[2] + b[2]);
      n[1] += (a[2] - b[2]) * (a[0] + b[0]);
      n[2] += (a[0] - b[0]) * (a[1] + b[1]);
    }
    return unit(n);
  }

  function hexRgb(h) {
    return [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];
  }

  // ---------------------------------------------------------------- camera
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
    const u = cross(r, f);
    return { e, f, r, u };
  }

  function fit() {
    if (!polys.length) return;
    const lo = [Infinity, Infinity, Infinity], hi = [-Infinity, -Infinity, -Infinity];
    for (const p of polys) for (const lp of p.loops) for (const v of lp) for (let k = 0; k < 3; k++) {
      lo[k] = Math.min(lo[k], v[k]); hi[k] = Math.max(hi[k], v[k]);
    }
    cam.target = [(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, (lo[2] + hi[2]) / 2];
    const size = Math.hypot(hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2]) || 10;
    cam.dist = size / (2 * Math.tan(FOV / 2)) * 1.25;
  }

  // ---------------------------------------------------------------- data
  function prepare() {
    polys = [];
    for (const f of view.faces) {
      const loops = f.loops.filter(lp => lp.length >= 3);
      if (!loops.length) continue;
      const all = loops[0];
      const c = [0, 0, 0];
      for (const v of all) { c[0] += v[0]; c[1] += v[1]; c[2] += v[2]; }
      polys.push({ face: f, loops, centre: [c[0] / all.length, c[1] / all.length, c[2] / all.length],
                   normal: newell(all) });
    }
  }

  function visible(p) {
    const f = p.face;
    if (show.levels[f.level || "(no level)"] === false) return false;
    if (focusSpace && f.space !== focusSpace) return "ghost";
    if (f.opening) return show.openings;
    if (f.side === "top") return show.roofs;
    if (f.side === "bottom") return show.floors;
    if (f.beyond === "space") return show.partitions;
    return true;
  }

  // ---------------------------------------------------------------- drawing
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
    const light = unit([B.e[0] - cam.target[0] + cam.dist * 0.3, B.e[1] - cam.target[1], B.e[2] - cam.target[2] + cam.dist]);
    const project = v => {
      const d = sub(v, B.e);
      const z = dot(d, B.f);
      if (z <= 0.05) return null;
      return [w / 2 + dot(d, B.r) / z * focal, h / 2 - dot(d, B.u) / z * focal, z];
    };
    const list = [];
    for (const p of polys) {
      const vis = visible(p);
      if (!vis) continue;
      const screen = [];
      let ok = true, depth = 0, n = 0;
      for (const lp of p.loops) {
        const pts = [];
        for (const v of lp) { const s = project(v); if (!s) { ok = false; break; } pts.push(s); depth += s[2]; n++; }
        if (!ok) break;
        screen.push(pts);
      }
      if (!ok || !n) continue;
      // An opening is drawn after the wall it sits in.
      list.push({ p, screen, depth: depth / n - (p.face.opening ? 0.05 : 0), ghost: vis === "ghost" });
    }
    list.sort((a, b) => b.depth - a.depth);
    for (const item of list) {
      const f = item.p.face;
      const rgb = hexRgb(f.colours[mode] || "#bbbbbb");
      const shade = 0.62 + 0.38 * Math.abs(dot(item.p.normal, light));
      const alpha = item.ghost ? 0.08 : (show.xray && !f.opening ? 0.28 : (f.opening ? 0.85 : 1));
      ctx.beginPath();
      for (const pts of item.screen) {
        ctx.moveTo(pts[0][0], pts[0][1]);
        for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i][0], pts[i][1]);
        ctx.closePath();
      }
      ctx.fillStyle = "rgba(" + Math.round(rgb[0] * shade) + "," + Math.round(rgb[1] * shade) + "," +
        Math.round(rgb[2] * shade) + "," + alpha + ")";
      ctx.fill("evenodd");
      ctx.lineWidth = 1;
      ctx.strokeStyle = item.ghost ? "rgba(90,90,90,0.12)" : "rgba(40,40,40,0.35)";
      ctx.stroke();
    }
    if (selected) {
      const item = list.find(i => i.p.face.id === selected);
      if (item) {
        ctx.beginPath();
        for (const pts of item.screen) {
          ctx.moveTo(pts[0][0], pts[0][1]);
          for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i][0], pts[i][1]);
          ctx.closePath();
        }
        ctx.lineWidth = 3; ctx.strokeStyle = "#ff2d95"; ctx.stroke();
      }
    }
    drawn = list;
    if (show.labels) labels(project);
    compass(w, B);
  }

  function labels(project) {
    ctx.font = "11px system-ui, Segoe UI, sans-serif";
    ctx.textAlign = "center"; ctx.textBaseline = "middle";
    for (const [id, sp] of Object.entries(view.spaces)) {
      if (!sp.label_at || show.levels[sp.level || "(no level)"] === false) continue;
      if (focusSpace && id !== focusSpace) continue;
      const s = project(sp.label_at);
      if (!s) continue;
      const text = sp.name || id;
      const width = ctx.measureText(text).width + 8;
      ctx.fillStyle = "rgba(255,255,255,0.85)";
      ctx.fillRect(s[0] - width / 2, s[1] - 8, width, 16);
      ctx.fillStyle = "#1f2328";
      ctx.fillText(text, s[0], s[1]);
    }
  }

  // True north on the screen: the direction the brain worked out with the
  // same rule as every facing (heron_takeoff.azimuth_deg) - never a second formula.
  function compass(w, B) {
    const north = [view.north_xy[0], view.north_xy[1], 0];
    let x = dot(north, B.r), y = dot(north, B.u);
    const n = Math.hypot(x, y) || 1; x /= n; y /= n;
    const cx = w - 34, cy = 34, r = 22;
    ctx.beginPath(); ctx.arc(cx, cy, r + 4, 0, 2 * Math.PI);
    ctx.fillStyle = "rgba(255,255,255,0.8)"; ctx.fill();
    ctx.strokeStyle = "rgba(0,0,0,0.25)"; ctx.lineWidth = 1; ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(cx + x * r, cy - y * r);
    ctx.lineTo(cx - y * 6, cy - x * 6);
    ctx.lineTo(cx + y * 6, cy + x * 6);
    ctx.closePath(); ctx.fillStyle = "#c0392b"; ctx.fill();
    ctx.fillStyle = "#1f2328"; ctx.font = "bold 10px system-ui, sans-serif";
    ctx.textAlign = "center"; ctx.textBaseline = "middle";
    ctx.fillText("N", cx + x * (r + 9), cy - y * (r + 9));
  }

  function redraw() {
    if (pending) return;
    pending = true;
    requestAnimationFrame(frame);
  }

  // ---------------------------------------------------------------- picking
  function inside(pts, x, y) {
    let hit = false;
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
      const a = pts[i], b = pts[j];
      if ((a[1] > y) !== (b[1] > y) && x < (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0]) hit = !hit;
    }
    return hit;
  }

  function pick(x, y) {
    for (let i = drawn.length - 1; i >= 0; i--) {
      const item = drawn[i];
      if (item.ghost) continue;
      let hit = false;
      for (const pts of item.screen) if (inside(pts, x, y)) hit = !hit;
      if (hit) return item.p.face;
    }
    return null;
  }

  function fixed(v, d) { return v == null || v === "" ? "-" : Number(v).toFixed(d); }

  function describe(f) {
    const info = root.querySelector(".l3d-info");
    info.replaceChildren();
    if (!f) { info.append(el("p", "muted small", "Click a face to see what it counted as.")); return; }
    const sp = view.spaces[f.space] || {};
    const rows = [
      ["Face", f.label], ["Counted as", f.used_as], ["Type", f.type || "-"],
      ["Element", f.element || "-"], ["From link", f.link || "-"],
      ["Area", fixed(f.area_m2, 2) + " m²"], ["U-value", f.u_w_m2k == null ? "none in the model" : fixed(f.u_w_m2k, 3) + " W/m²K"],
    ];
    if (f.shgc !== undefined) rows.push(["SHGC", f.shgc == null ? "none in the model" : fixed(f.shgc, 2)]);
    if (f.facing_deg != null) rows.push(["Faces", fixed(f.facing_deg, 0) + "° (" + f.facing + ")"]);
    rows.push(["Beyond it", f.beyond_space ? "Space " + f.beyond_space : (f.beyond || "-")]);
    rows.push(["Space", sp.name || f.space], ["Level", sp.level || "-"], ["Status", sp.status || "-"]);
    if (sp.total_w != null) {
      rows.push(["Cooling at its peak", fixed(sp.total_w, 0) + " W (" + fixed(sp.w_per_m2, 1) + " W/m²) " + (sp.peak || "")]);
      rows.push(["Heating", fixed(sp.heating_w, 0) + " W"], ["Supply air", fixed(sp.supply_ls, 1) + " L/s"]);
    }
    if ((sp.why || []).length) rows.push(["Why", sp.why.join(" · ")]);
    const dl = el("dl");
    for (const [k, v] of rows) dl.append(el("dt", null, k), el("dd", null, v));
    info.append(dl);
  }

  // ---------------------------------------------------------------- controls
  function legend() {
    const box = root.querySelector(".l3d-legend");
    box.replaceChildren();
    const L = view.legends[mode];
    if (Array.isArray(L)) {
      for (const row of L) {
        const line = el("div", "l3d-key");
        const sw = el("i"); sw.style.background = row.colour;
        line.append(sw, el("span", null, row.label));
        box.append(line);
      }
      return;
    }
    const bar = el("div", "l3d-ramp");
    bar.style.background = "linear-gradient(90deg," + L.ramp.join(",") + ")";
    const ends = el("div", "l3d-ends");
    ends.append(el("span", null, fixed(L.low, 2)), el("span", null, fixed(L.high, 2) + " " + L.unit));
    box.append(bar, ends);
    for (const row of L.extra || []) {
      const line = el("div", "l3d-key");
      const sw = el("i"); sw.style.background = row.colour;
      line.append(sw, el("span", null, row.label));
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
    const pickMode = el("select");
    pickMode.setAttribute("aria-label", "Colour by");
    for (const m of view.modes) { const o = el("option", null, m.label); o.value = m.key; pickMode.append(o); }
    pickMode.value = mode;
    pickMode.addEventListener("change", () => { mode = pickMode.value; legend(); redraw(); });
    const colour = el("label", "l3d-row"); colour.append(el("span", null, "Colour by"), pickMode);
    side.append(colour);

    const onlySpace = el("select");
    onlySpace.setAttribute("aria-label", "Show one Space");
    const all = el("option", null, "Every Space"); all.value = ""; onlySpace.append(all);
    for (const [id, sp] of Object.entries(view.spaces)) { const o = el("option", null, sp.name || id); o.value = id; onlySpace.append(o); }
    onlySpace.value = focusSpace || "";
    onlySpace.addEventListener("change", () => { focusSpace = onlySpace.value || null; redraw(); });
    const one = el("label", "l3d-row"); one.append(el("span", null, "Space"), onlySpace);
    side.append(one);

    const ticks = el("div", "l3d-ticks");
    for (const lv of view.levels) {
      if (!(lv in show.levels)) show.levels[lv] = true;
      ticks.append(tick(lv, () => show.levels[lv] !== false, v => { show.levels[lv] = v; }));
    }
    ticks.append(tick("Roofs and ceilings", () => show.roofs, v => { show.roofs = v; }));
    ticks.append(tick("Floors", () => show.floors, v => { show.floors = v; }));
    ticks.append(tick("Walls between Spaces", () => show.partitions, v => { show.partitions = v; }));
    ticks.append(tick("Windows and doors", () => show.openings, v => { show.openings = v; }));
    ticks.append(tick("See-through", () => show.xray, v => { show.xray = v; }));
    ticks.append(tick("Space names", () => show.labels, v => { show.labels = v; }));
    side.append(ticks);

    const buttons = el("div", "l3d-buttons");
    const button = (text, title, go) => { const b = el("button", null, text); b.type = "button"; b.title = title; b.addEventListener("click", go); buttons.append(b); };
    button("Top", "Look straight down - plan, project north up", () => { cam.pitch = 89.5; cam.yaw = 0; redraw(); });
    button("3D", "Back to the corner view", () => { cam.pitch = 32; cam.yaw = 35; redraw(); });
    button("Fit", "Fit the whole building", () => { fit(); redraw(); });
    button("+", "Closer", () => { cam.dist *= 0.8; redraw(); });
    button("−", "Further", () => { cam.dist *= 1.25; redraw(); });
    side.append(buttons);

    side.append(el("div", "l3d-legend"));
    side.append(el("div", "l3d-info"));
    for (const n of view.notes || []) side.append(el("p", "small warn", n));
    legend();
    describe(null);
  }

  // ---------------------------------------------------------------- input
  function wire() {
    const pointers = new Map();
    let moved = 0, pinch = null;
    canvas.addEventListener("pointerdown", e => {
      // A pointer the browser no longer tracks (a touch already cancelled)
      // cannot be captured; the drag still works without it.
      try { canvas.setPointerCapture(e.pointerId); } catch (err) { /* not capturable */ }
      pointers.set(e.pointerId, [e.clientX, e.clientY, e.button, e.shiftKey || e.ctrlKey]);
      moved = 0;
      if (pointers.size === 2) { const [a, b] = [...pointers.values()]; pinch = Math.hypot(a[0] - b[0], a[1] - b[1]); }
    });
    canvas.addEventListener("pointermove", e => {
      const was = pointers.get(e.pointerId);
      if (!was) return;
      const dx = e.clientX - was[0], dy = e.clientY - was[1];
      moved += Math.abs(dx) + Math.abs(dy);
      pointers.set(e.pointerId, [e.clientX, e.clientY, was[2], was[3]]);
      if (pointers.size === 2) {
        const [a, b] = [...pointers.values()];
        const d = Math.hypot(a[0] - b[0], a[1] - b[1]);
        if (pinch) cam.dist *= pinch / (d || pinch);
        pinch = d;
      } else if (was[2] === 2 || was[3]) {
        const B = basis();
        const k = cam.dist * 0.0015;
        for (let i = 0; i < 3; i++) cam.target[i] += (-B.r[i] * dx + B.u[i] * dy) * k;
      } else {
        // As Revit's orbit: the building turns the way the mouse moves - drag
        // left and it turns left (Ajmal, 2026-10-04). Up and down already did.
        cam.yaw -= dx * 0.4;
        cam.pitch = Math.max(2, Math.min(89.5, cam.pitch + dy * 0.4));
      }
      redraw();
    });
    const end = e => {
      const was = pointers.get(e.pointerId);
      pointers.delete(e.pointerId);
      if (pointers.size < 2) pinch = null;
      if (was && moved < 5 && e.type === "pointerup") {
        const r = canvas.getBoundingClientRect();
        const f = pick(e.clientX - r.left, e.clientY - r.top);
        selected = f ? f.id : null;
        describe(f);
        redraw();
      }
    };
    canvas.addEventListener("pointerup", end);
    canvas.addEventListener("pointercancel", end);
    canvas.addEventListener("contextmenu", e => e.preventDefault());
    canvas.addEventListener("wheel", e => {
      e.preventDefault();
      cam.dist *= Math.exp(e.deltaY * 0.0012);
      redraw();
    }, { passive: false });
    window.addEventListener("resize", redraw);
  }

  function mount() {
    root.replaceChildren();
    const stage = el("div", "l3d-stage");
    canvas = el("canvas");
    canvas.setAttribute("aria-label", "3D view of the faces the loads were worked out from");
    stage.append(canvas);
    const side = el("div", "l3d-side");
    root.append(stage, side);
    ctx = canvas.getContext("2d");
    wire();
  }

  async function load() {
    try {
      const res = await fetch("/api/loads/view", { headers: HEADER, credentials: "same-origin" });
      if (!res.ok) return;
      const got = (await res.json()).view;
      if (!got) { root.hidden = true; return; }
      const first = !view;
      view = got;
      if (!canvas) mount();
      prepare();
      if (first) fit();
      controls();
      root.hidden = false;
      redraw();
    } catch (e) { /* the state poll reports a closed page */ }
  }

  window.HeronLoads3D = {
    // Called by companion.js whenever the Loads panel shows a different run.
    update(container, key) {
      root = container;
      if (key === shownKey) return;
      shownKey = key;
      if (!key) { root.hidden = true; return; }
      load();
    },
    // From the results table: show one Space, every other ghosted.
    focus(id) {
      focusSpace = id || null;
      const pickSpace = root && root.querySelector(".l3d-side select[aria-label='Show one Space']");
      if (pickSpace) pickSpace.value = focusSpace || "";
      if (root) root.scrollIntoView({ behavior: "smooth", block: "nearest" });
      redraw();
    },
  };
})();
