// Heron Companion - the Model Dashboard's 3D view (docs/40 section 21.13).
//
// Draws the model's shapes exactly as the brain sent them
// (brain/heron_model_view.py): every colour, the legend and every sentence
// arrive in the data, and this file only projects and paints (mcp/companion
// README rule 4). Heron's own WebGL - no library, nothing fetched from
// anywhere but this page's own server. Model text reaches the screen through
// textContent only (Golden Rule 19).
//
// Revit is Z up, so this is too. Flat shading, one light from the viewer's
// side, and a second pass that paints each element in its own colour code
// for picking: the pixel under the click names the element.
//
// The view's own tools (docs/40 s21.17) are the page's alone and read nothing
// from Revit: Top, Front, Side and 3D cameras, zoom to what is lit, a cut
// height that hides everything above it, categories hidden and shown, a
// picture of the view saved to the PC, and full screen.
//
// Rooms and spaces read as volumes, and glass, are drawn see-through
// (docs/40 s21.18): a solid pass first, then a see-through pass that writes no
// depth, so what is inside a room still shows. Colour by category or by
// material - the brain sends both; the page only swaps which one is drawn.
"use strict";

(function () {
  const HEADER = { "X-Heron-Companion": "1" };
  const FOV = 45 * Math.PI / 180;

  let root = null, stage = null, canvas = null, gl = null, info = null;
  let draw = null, pick = null;            // the two programs
  let buf = null;                          // GL buffers
  let count = 0;                           // vertices
  let view = null;                         // what the brain sent
  let ranges = [];                         // per element: [first vertex, vertex count]
  let ids = [];                            // per element: its id, as text
  let cats = [];                           // per element: its category index
  let hi = null;                           // Float32Array, 1 where highlighted
  let vis = null;                          // Float32Array, 0 where its category is hidden
  let lit = false;                         // anything highlighted
  let hidden = new Set();                  // category indexes hidden by the modeller
  let zmin = 0, zmax = 1;                  // the drawing's lowest and highest point
  let cut = null;                          // the cut height, or null for no cut
  let onTools = null;                      // told when hidden categories or the cut change
  let mode = "category";                   // "category" or "material"
  let colBy = null, alpBy = null;          // per mode: Float32Array colours and alphas
  let radius = 10;
  let shownKey = null;
  let tools = null;                        // the toolbar over the canvas
  let onPick = null;
  let pending = false;
  const cam = { yaw: 35, pitch: 28, dist: 30, target: [0, 0, 0] };

  function el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = String(text);
    return e;
  }

  function rgb(h) {
    const ok = /^#[0-9a-fA-F]{6}$/.test(String(h || ""));
    const s = ok ? h : "#9e9e9e";
    return [parseInt(s.slice(1, 3), 16) / 255, parseInt(s.slice(3, 5), 16) / 255,
            parseInt(s.slice(5, 7), 16) / 255];
  }

  // ---------------------------------------------------------------- matrices
  // Column-major 4x4, as WebGL reads them.
  function perspective(fov, aspect, near, far) {
    const f = 1 / Math.tan(fov / 2), nf = 1 / (near - far);
    return new Float32Array([f / aspect, 0, 0, 0, 0, f, 0, 0, 0, 0, (far + near) * nf, -1,
                             0, 0, 2 * far * near * nf, 0]);
  }
  function lookAt(eye, at, up) {
    const z = norm([eye[0] - at[0], eye[1] - at[1], eye[2] - at[2]]);
    const x = norm(cross(up, z));
    const y = cross(z, x);
    return new Float32Array([x[0], y[0], z[0], 0, x[1], y[1], z[1], 0, x[2], y[2], z[2], 0,
                             -dot(x, eye), -dot(y, eye), -dot(z, eye), 1]);
  }
  function multiply(a, b) {
    const o = new Float32Array(16);
    for (let c = 0; c < 4; c++)
      for (let r = 0; r < 4; r++)
        o[c * 4 + r] = a[r] * b[c * 4] + a[4 + r] * b[c * 4 + 1] + a[8 + r] * b[c * 4 + 2] +
                       a[12 + r] * b[c * 4 + 3];
    return o;
  }
  const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
  const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
  function norm(v) { const n = Math.hypot(v[0], v[1], v[2]) || 1; return [v[0] / n, v[1] / n, v[2] / n]; }

  function eye() {
    const y = cam.yaw * Math.PI / 180, p = cam.pitch * Math.PI / 180;
    return [cam.target[0] + cam.dist * Math.cos(p) * Math.sin(y),
            cam.target[1] - cam.dist * Math.cos(p) * Math.cos(y),
            cam.target[2] + cam.dist * Math.sin(p)];
  }

  // ---------------------------------------------------------------- shaders
  const DRAW_VS = [
    "attribute vec3 aPos; attribute vec3 aNor; attribute vec3 aCol; attribute float aHi; attribute float aVis;",
    "attribute float aAlp;",
    "uniform mat4 uMvp; uniform vec3 uLight; uniform float uLit; uniform vec3 uHi; uniform vec3 uDim;",
    "varying vec3 vCol; varying float vZ; varying float vAlp;",
    "void main() {",
    "  vAlp = (uLit > 0.5 && aHi > 0.5) ? 1.0 : aAlp;",
    "  float l = 0.42 + 0.58 * abs(dot(normalize(aNor), uLight));",
    "  vec3 base = aCol;",
    "  if (uLit > 0.5) base = aHi > 0.5 ? uHi : uDim;",
    "  vCol = base * l;",
    "  vZ = aPos.z;",
    // A hidden category's corners go behind the far plane, so nothing of it is drawn or picked.
    "  gl_Position = aVis > 0.5 ? uMvp * vec4(aPos, 1.0) : vec4(0.0, 0.0, 2.0, 1.0);",
    "}"].join("\n");
  // uPass 0 draws only what is solid, uPass 1 only what is see-through.
  const DRAW_FS = "precision mediump float; varying vec3 vCol; varying float vZ; varying float vAlp;" +
    " uniform float uCut; uniform float uPass;" +
    " void main() { if (vZ > uCut) discard; bool solid = vAlp > 0.99;" +
    " if ((uPass < 0.5) != solid) discard; gl_FragColor = vec4(vCol, solid ? 1.0 : vAlp); }";
  const PICK_VS = [
    "attribute vec3 aPos; attribute vec3 aPick; attribute float aVis; attribute float aAlp;",
    "uniform mat4 uMvp; varying vec3 vPick; varying float vZ; varying float vAlp;",
    "void main() { vPick = aPick; vZ = aPos.z; vAlp = aAlp;",
    "  gl_Position = aVis > 0.5 ? uMvp * vec4(aPos, 1.0) : vec4(0.0, 0.0, 2.0, 1.0); }"].join("\n");
  const PICK_FS = "precision mediump float; varying vec3 vPick; varying float vZ; varying float vAlp;" +
    " uniform float uCut; uniform float uPass;" +
    " void main() { if (vZ > uCut) discard; if ((uPass < 0.5) != (vAlp > 0.99)) discard;" +
    " gl_FragColor = vec4(vPick, 1.0); }";

  function program(vs, fs) {
    const make = (kind, src) => {
      const s = gl.createShader(kind);
      gl.shaderSource(s, src);
      gl.compileShader(s);
      if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s));
      return s;
    };
    const p = gl.createProgram();
    gl.attachShader(p, make(gl.VERTEX_SHADER, vs));
    gl.attachShader(p, make(gl.FRAGMENT_SHADER, fs));
    gl.linkProgram(p);
    if (!gl.getProgramParameter(p, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(p));
    return p;
  }

  // ---------------------------------------------------------------- mount
  function mount() {
    root.replaceChildren();
    stage = el("div", "m3d-stage");
    canvas = el("canvas");
    canvas.setAttribute("aria-label", "3D view of the model - drag to turn, right-drag to pan, wheel to zoom, click an element");
    tools = toolbar();
    stage.append(canvas, tools);
    info = el("div", "m3d-info");
    root.append(stage, info);
    gl = canvas.getContext("webgl", { antialias: true, preserveDrawingBuffer: false });
    if (!gl) {
      stage.replaceChildren(el("p", "empty", "This browser cannot draw 3D here (WebGL is off). " +
        "Chrome or Edge with hardware acceleration on can."));
      return false;
    }
    draw = program(DRAW_VS, DRAW_FS);
    pick = program(PICK_VS, PICK_FS);
    interact();
    return true;
  }

  // Every triangle gets its own three corners, so each face is flat-shaded
  // with its own normal - the clearest way to read a model's edges.
  function prepare() {
    const els = view.elements || [];
    const palette = (view.categories || []).map(c => rgb(c.colour));
    const mats = (view.materials || []).map(m => ({ rgb: rgb(m.colour), alpha: Math.max(0.1, Math.min(1, Number(m.alpha) || 0)) }));
    const none = rgb(view.noMaterial);
    const volumes = new Set(view.volumes || []);
    const volumeAlpha = Math.max(0.05, Math.min(0.9, Number(view.volumeAlpha) || 0.18));
    let n = 0;
    els.forEach(e => { n += e.t.length; });
    count = n;
    const pos = new Float32Array(n * 3), nor = new Float32Array(n * 3);
    const col = new Float32Array(n * 3), pck = new Float32Array(n * 3);
    const colM = new Float32Array(n * 3), alpC = new Float32Array(n), alpM = new Float32Array(n);
    hi = new Float32Array(n);
    vis = new Float32Array(n).fill(1);
    ranges = []; ids = []; cats = [];
    let k = 0;
    let min = [Infinity, Infinity, Infinity], max = [-Infinity, -Infinity, -Infinity];
    els.forEach((e, index) => {
      const code = index + 1;
      const pc = [(code & 255) / 255, ((code >> 8) & 255) / 255, ((code >> 16) & 255) / 255];
      const c = palette[e.c] || [0.6, 0.6, 0.6];
      const isVolume = volumes.has(e.c);
      const m = Array.isArray(e.m) ? e.m : null;
      ranges.push([k, e.t.length]);
      ids.push(String(e.id));
      cats.push(e.c);
      for (let t = 0; t < e.t.length; t += 3) {
        const mi = m ? m[t / 3] : (typeof e.m === "number" ? e.m : -1);
        const mat = mi >= 0 ? mats[mi] : null;
        const mc = isVolume ? c : (mat ? mat.rgb : none);
        const ma = isVolume ? volumeAlpha : (mat ? mat.alpha : 1);
        const ca = isVolume ? volumeAlpha : 1;
        const a = e.t[t] * 3, b = e.t[t + 1] * 3, d = e.t[t + 2] * 3;
        const A = [e.v[a], e.v[a + 1], e.v[a + 2]], B = [e.v[b], e.v[b + 1], e.v[b + 2]];
        const D = [e.v[d], e.v[d + 1], e.v[d + 2]];
        const N = norm(cross([B[0] - A[0], B[1] - A[1], B[2] - A[2]], [D[0] - A[0], D[1] - A[1], D[2] - A[2]]));
        [A, B, D].forEach(P => {
          pos.set(P, k * 3); nor.set(N, k * 3); col.set(c, k * 3); pck.set(pc, k * 3);
          colM.set(mc, k * 3); alpC[k] = ca; alpM[k] = ma;
          for (let i = 0; i < 3; i++) { if (P[i] < min[i]) min[i] = P[i]; if (P[i] > max[i]) max[i] = P[i]; }
          k++;
        });
      }
    });
    if (!isFinite(min[0])) { min = [0, 0, 0]; max = [1, 1, 1]; }
    zmin = min[2]; zmax = max[2];
    hidden = new Set();
    cut = null;
    cam.target = [(min[0] + max[0]) / 2, (min[1] + max[1]) / 2, (min[2] + max[2]) / 2];
    radius = Math.max(1, Math.hypot(max[0] - min[0], max[1] - min[1], max[2] - min[2]) / 2);
    if (buf) Object.values(buf).forEach(b => gl.deleteBuffer(b));
    const upload = arr => { const b = gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER, b); gl.bufferData(gl.ARRAY_BUFFER, arr, gl.STATIC_DRAW); return b; };
    colBy = { category: col, material: colM };
    alpBy = { category: alpC, material: alpM };
    if (mode === "material" && !view.hasMaterials) mode = "category";
    buf = { pos: upload(pos), nor: upload(nor), col: upload(colBy[mode]), pck: upload(pck), hi: upload(hi),
            vis: upload(vis), alp: upload(alpBy[mode]) };
    lit = false;
    syncTools();
  }

  function fit() {
    cam.dist = radius / Math.sin(FOV / 2) * 1.05;
    redraw();
  }

  function attrib(p, name, b, size) {
    const at = gl.getAttribLocation(p, name);
    if (at < 0) return;
    gl.bindBuffer(gl.ARRAY_BUFFER, b);
    gl.enableVertexAttribArray(at);
    gl.vertexAttribPointer(at, size, gl.FLOAT, false, 0, 0);
  }

  function size() {
    const dpr = window.devicePixelRatio || 1;
    const w = canvas.clientWidth, h = canvas.clientHeight;
    if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
      canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
    }
    return [canvas.width, canvas.height];
  }

  function mvp(w, h) {
    // The near plane as far out as the model allows: depth resolution is spent
    // near the eye, and some browsers give only 16 bits of it, so a near plane
    // too close makes faces a few centimetres apart flicker against each other.
    const near = Math.max(cam.dist / 100, cam.dist - radius * 2.2, 0.01), far = cam.dist + radius * 4;
    return multiply(perspective(FOV, w / Math.max(h, 1), near, far), lookAt(eye(), cam.target, [0, 0, 1]));
  }

  function frame() {
    pending = false;
    if (!gl || !view) return;
    const [w, h] = size();
    gl.viewport(0, 0, w, h);
    const dark = document.documentElement.dataset.theme === "dark";
    gl.clearColor(dark ? 0.07 : 0.97, dark ? 0.09 : 0.976, dark ? 0.13 : 0.984, 1);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    gl.enable(gl.DEPTH_TEST);
    gl.useProgram(draw);
    attrib(draw, "aPos", buf.pos, 3); attrib(draw, "aNor", buf.nor, 3);
    attrib(draw, "aCol", buf.col, 3); attrib(draw, "aHi", buf.hi, 1); attrib(draw, "aVis", buf.vis, 1);
    attrib(draw, "aAlp", buf.alp, 1);
    gl.uniform1f(gl.getUniformLocation(draw, "uCut"), cutAt());
    gl.uniformMatrix4fv(gl.getUniformLocation(draw, "uMvp"), false, mvp(w, h));
    const e = eye();
    gl.uniform3fv(gl.getUniformLocation(draw, "uLight"), norm([e[0] - cam.target[0], e[1] - cam.target[1], e[2] - cam.target[2] + cam.dist * 0.6]));
    gl.uniform1f(gl.getUniformLocation(draw, "uLit"), lit ? 1 : 0);
    gl.uniform3fv(gl.getUniformLocation(draw, "uHi"), rgb(view.highlight));
    gl.uniform3fv(gl.getUniformLocation(draw, "uDim"), rgb(view.dimmed));
    // Solid first; then see-through on top, blended, writing no depth, so a
    // room never hides what stands inside it.
    gl.uniform1f(gl.getUniformLocation(draw, "uPass"), 0);
    gl.drawArrays(gl.TRIANGLES, 0, count);
    gl.enable(gl.BLEND);
    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
    gl.depthMask(false);
    // A room's face lies on the wall it is bounded by: pushed back a hair, the
    // wall wins cleanly instead of the two flickering in stripes.
    gl.enable(gl.POLYGON_OFFSET_FILL);
    gl.polygonOffset(1, 2);
    gl.uniform1f(gl.getUniformLocation(draw, "uPass"), 1);
    gl.drawArrays(gl.TRIANGLES, 0, count);
    gl.disable(gl.POLYGON_OFFSET_FILL);
    gl.depthMask(true);
    gl.disable(gl.BLEND);
  }

  function redraw() {
    if (pending) return;
    pending = true;
    requestAnimationFrame(frame);
  }

  // The element under a pixel: one draw in colour codes into an offscreen
  // target, one pixel read back.
  function pickAt(x, y) {
    const [w, h] = size();
    const fb = gl.createFramebuffer(), tex = gl.createTexture(), depth = gl.createRenderbuffer();
    gl.bindTexture(gl.TEXTURE_2D, tex);
    // NEAREST and CLAMP: a canvas is rarely a power of two, and WebGL 1 will
    // not render into such a texture with mipmap filtering or repeat.
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
    gl.bindRenderbuffer(gl.RENDERBUFFER, depth);
    gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT16, w, h);
    gl.bindFramebuffer(gl.FRAMEBUFFER, fb);
    gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
    gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT, gl.RENDERBUFFER, depth);
    if (gl.checkFramebufferStatus(gl.FRAMEBUFFER) !== gl.FRAMEBUFFER_COMPLETE) {
      gl.bindFramebuffer(gl.FRAMEBUFFER, null);
      gl.deleteFramebuffer(fb); gl.deleteTexture(tex); gl.deleteRenderbuffer(depth);
      return -1;
    }
    gl.viewport(0, 0, w, h);
    gl.clearColor(0, 0, 0, 1);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    gl.enable(gl.DEPTH_TEST);
    gl.useProgram(pick);
    attrib(pick, "aPos", buf.pos, 3); attrib(pick, "aPick", buf.pck, 3); attrib(pick, "aVis", buf.vis, 1);
    attrib(pick, "aAlp", buf.alp, 1);
    gl.uniform1f(gl.getUniformLocation(pick, "uCut"), cutAt());
    gl.uniformMatrix4fv(gl.getUniformLocation(pick, "uMvp"), false, mvp(w, h));
    // A solid element under the click wins; only where there is none does a
    // see-through one - a room, glass - answer.
    const px = new Uint8Array(4);
    const dpr = window.devicePixelRatio || 1;
    for (let pass = 0; pass < 2; pass++) {
      gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
      gl.uniform1f(gl.getUniformLocation(pick, "uPass"), pass);
      if (pass) { gl.enable(gl.POLYGON_OFFSET_FILL); gl.polygonOffset(1, 2); }
      gl.drawArrays(gl.TRIANGLES, 0, count);
      gl.disable(gl.POLYGON_OFFSET_FILL);
      gl.readPixels(Math.round(x * dpr), Math.round(h - y * dpr), 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, px);
      if (px[0] + (px[1] << 8) + (px[2] << 16) > 0) break;
    }
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    gl.deleteFramebuffer(fb); gl.deleteTexture(tex); gl.deleteRenderbuffer(depth);
    const code = px[0] + (px[1] << 8) + (px[2] << 16);
    redraw();
    return code > 0 && code <= ids.length ? code - 1 : -1;
  }

  function interact() {
    let start = null, last = null, moved = false, panning = false;
    canvas.addEventListener("contextmenu", e => e.preventDefault());
    canvas.addEventListener("pointerdown", e => {
      start = last = [e.clientX, e.clientY];
      moved = false;
      panning = e.button === 2 || e.shiftKey;
      try { canvas.setPointerCapture(e.pointerId); } catch (err) { /* not capturable */ }
    });
    canvas.addEventListener("pointermove", e => {
      if (!last) return;
      const dx = e.clientX - last[0], dy = e.clientY - last[1];
      if (Math.abs(e.clientX - start[0]) + Math.abs(e.clientY - start[1]) > 3) moved = true;
      last = [e.clientX, e.clientY];
      if (panning) {
        const y = cam.yaw * Math.PI / 180;
        const s = cam.dist * 0.0015;
        const right = [Math.cos(y), Math.sin(y), 0];
        cam.target[0] -= right[0] * dx * s; cam.target[1] -= right[1] * dx * s;
        cam.target[2] += dy * s;
      } else {
        cam.yaw = (cam.yaw - dx * 0.4) % 360;
        cam.pitch = Math.max(-89, Math.min(89, cam.pitch + dy * 0.4));
      }
      redraw();
    });
    canvas.addEventListener("pointerup", e => {
      if (start && !moved && e.button === 0 && onPick) {
        const r = canvas.getBoundingClientRect();
        const index = pickAt(e.clientX - r.left, e.clientY - r.top);
        onPick(index < 0 ? null : { id: ids[index], category: (view.categories[cats[index]] || {}).name || "" });
      }
      start = last = null;
    });
    canvas.addEventListener("wheel", e => {
      e.preventDefault();
      cam.dist = Math.max(radius * 0.02, Math.min(radius * 40, cam.dist * Math.exp(e.deltaY * 0.0012)));
      redraw();
    }, { passive: false });
    window.addEventListener("resize", redraw);
  }

  // ---------------------------------------------------------------- tools
  // The cut height in metres, or far above everything when there is no cut.
  function cutAt() { return cut == null ? zmax + radius * 10 + 1 : cut; }

  function button(text, title, act) {
    const b = el("button", "m3d-btn", text);
    b.type = "button";
    b.title = title;
    b.addEventListener("click", act);
    return b;
  }

  // Revit's own named views, as a modeller reads them: Top looks down, Front
  // looks north from the south, Side looks west from the east, 3D is the
  // usual corner view.
  const PRESETS = { top: [0, 89.9], front: [0, 0], side: [90, 0], iso: [35, 28] };
  function preset(name) {
    const p = PRESETS[name];
    if (!p || !view) return;
    cam.yaw = p[0]; cam.pitch = p[1];
    fit();
  }

  function toolbar() {
    const bar = el("div", "m3d-tools");
    bar.setAttribute("role", "toolbar");
    bar.setAttribute("aria-label", "3D view tools");
    bar.append(
      button("Fit", "Show the whole drawing", () => { if (view) fit(); }),
      button("Top", "Look down from above, like a plan", () => preset("top")),
      button("Front", "Look from the south, like an elevation", () => preset("front")),
      button("Side", "Look from the east, like a side elevation", () => preset("side")),
      button("3D", "The usual corner view", () => preset("iso")),
      button("Zoom to lit", "Zoom to the elements lit up from a row, a bar or a click", zoomLit));
    const cutBox = el("label", "m3d-cut");
    cutBox.title = "Hide everything above this height - look inside a storey. The height is from Revit's internal origin. Nothing in Revit changes";
    cutBox.append(el("span", null, "Cut"));
    const range = el("input");
    range.type = "range"; range.min = "0"; range.max = "1000"; range.value = "1000";
    range.setAttribute("aria-label", "Cut height - hide everything above it");
    const shown = el("span", "m3d-cut-value", "off");
    range.addEventListener("input", () => {
      const f = Number(range.value) / 1000;
      cut = f >= 1 ? null : zmin + (zmax - zmin) * f;
      shown.textContent = cut == null ? "off" : (cut + (view && view.offset ? Number(view.offset[2]) || 0 : 0)).toFixed(2) + " m";
      redraw();
      if (onTools) onTools();
    });
    cutBox.append(range, shown);
    bar.append(cutBox,
      button("Save picture", "Save this view as a PNG picture on this PC", savePicture),
      button("Full screen", "Fill the screen with the 3D view - Esc comes back", () => {
        if (document.fullscreenElement) document.exitFullscreen();
        else if (stage.requestFullscreen) stage.requestFullscreen().catch(() => {});
      }));
    bar._range = range; bar._shown = shown;
    return bar;
  }

  function syncTools() {
    if (!tools) return;
    tools._range.value = cut == null ? "1000" : String(Math.round(1000 * (cut - zmin) / Math.max(zmax - zmin, 1e-6)));
    tools._shown.textContent = "off";
  }

  // The camera framed on the lit elements' extent; nothing lit, the whole drawing.
  function zoomLit() {
    if (!view || !hi) return;
    const els = view.elements || [];
    let min = [Infinity, Infinity, Infinity], max = [-Infinity, -Infinity, -Infinity];
    ranges.forEach(([first], index) => {
      if (!hi[first] || !vis[first]) return;
      const v = els[index].v;
      for (let i = 0; i < v.length; i += 3)
        for (let a = 0; a < 3; a++) { if (v[i + a] < min[a]) min[a] = v[i + a]; if (v[i + a] > max[a]) max[a] = v[i + a]; }
    });
    if (!isFinite(min[0])) { fit(); return; }
    cam.target = [(min[0] + max[0]) / 2, (min[1] + max[1]) / 2, (min[2] + max[2]) / 2];
    const r = Math.max(0.5, Math.hypot(max[0] - min[0], max[1] - min[1], max[2] - min[2]) / 2);
    cam.dist = r / Math.sin(FOV / 2) * 1.2;
    redraw();
  }

  // A picture of exactly what is on screen: drawn and read back in the same
  // task, so the drawing buffer is still there to read.
  function savePicture() {
    if (!gl || !view) return;
    frame();
    canvas.toBlob(blob => {
      if (!blob) return;
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = "Heron 3D view.png";
      document.body.append(a);
      a.click();
      setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 1000);
    }, "image/png");
  }

  // Hide or show categories by name. Hidden ones are neither drawn nor picked.
  function setHidden(names) {
    if (!gl || !buf || !vis || !view) return;
    const index = new Map((view.categories || []).map((c, i) => [c.name, i]));
    hidden = new Set();
    (names || []).forEach(n => { if (index.has(n)) hidden.add(index.get(n)); });
    vis.fill(1);
    cats.forEach((c, i) => {
      if (!hidden.has(c)) return;
      const [first, n] = ranges[i];
      vis.fill(0, first, first + n);
    });
    gl.bindBuffer(gl.ARRAY_BUFFER, buf.vis);
    gl.bufferSubData(gl.ARRAY_BUFFER, 0, vis);
    redraw();
    if (onTools) onTools();
  }

  // Colour by "category" or "material": the brain made both, this swaps them.
  function setMode(next) {
    const want = next === "material" && view && view.hasMaterials ? "material" : "category";
    mode = want;
    if (!gl || !buf || !colBy) return mode;
    gl.bindBuffer(gl.ARRAY_BUFFER, buf.col);
    gl.bufferData(gl.ARRAY_BUFFER, colBy[mode], gl.STATIC_DRAW);
    gl.bindBuffer(gl.ARRAY_BUFFER, buf.alp);
    gl.bufferData(gl.ARRAY_BUFFER, alpBy[mode], gl.STATIC_DRAW);
    redraw();
    if (onTools) onTools();
    return mode;
  }

  function hiddenNames() {
    return [...hidden].map(i => ((view && view.categories) || [])[i]).filter(Boolean).map(c => c.name);
  }

  function highlight(list) {
    if (!gl || !buf || !hi) return 0;
    hi.fill(0);
    const wanted = new Set((list || []).map(String));
    let found = 0;
    ids.forEach((id, index) => {
      if (!wanted.has(id)) return;
      found++;
      const [first, n] = ranges[index];
      hi.fill(1, first, first + n);
    });
    lit = wanted.size > 0;
    gl.bindBuffer(gl.ARRAY_BUFFER, buf.hi);
    gl.bufferSubData(gl.ARRAY_BUFFER, 0, hi);
    redraw();
    return found;
  }

  async function load() {
    try {
      const res = await fetch("/api/dashboard/view", { headers: HEADER, credentials: "same-origin" });
      if (!res.ok) return;
      const got = (await res.json()).view;
      if (!got) { root.hidden = true; return; }
      view = got;
      root.hidden = false;
      if (!canvas && !mount()) return;
      prepare();
      fit();
    } catch (e) { /* the state poll reports a closed page */ }
  }

  window.HeronModel3D = {
    // Called by companion.js whenever the dashboard holds a different 3D read.
    update(container, key, picked) {
      root = container;
      onPick = picked || null;
      if (key === shownKey) return;
      shownKey = key;
      if (!key) { root.hidden = true; return; }
      load();
    },
    // Light up these elements, every other one faded; empty or null clears it.
    // Returns how many of them are in the view.
    highlight,
    fit() { if (view) fit(); },
    // Camera presets: "top", "front", "side", "iso".
    preset,
    zoomLit,
    // The categories hidden, by name; set it with a list of names.
    setHidden,
    hidden: hiddenNames,
    // Told whenever the hidden categories, the cut or the colouring change.
    onTools(fn) { onTools = fn || null; },
    // "category" or "material"; a read with no materials stays on category.
    setMode,
    mode() { return mode; },
  };
})();
