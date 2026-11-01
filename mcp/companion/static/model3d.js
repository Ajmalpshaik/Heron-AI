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
  let lit = false;                         // anything highlighted
  let radius = 10;
  let shownKey = null;
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
    "attribute vec3 aPos; attribute vec3 aNor; attribute vec3 aCol; attribute float aHi;",
    "uniform mat4 uMvp; uniform vec3 uLight; uniform float uLit; uniform vec3 uHi; uniform vec3 uDim;",
    "varying vec3 vCol;",
    "void main() {",
    "  float l = 0.42 + 0.58 * abs(dot(normalize(aNor), uLight));",
    "  vec3 base = aCol;",
    "  if (uLit > 0.5) base = aHi > 0.5 ? uHi : uDim;",
    "  vCol = base * l;",
    "  gl_Position = uMvp * vec4(aPos, 1.0);",
    "}"].join("\n");
  const DRAW_FS = "precision mediump float; varying vec3 vCol; void main() { gl_FragColor = vec4(vCol, 1.0); }";
  const PICK_VS = [
    "attribute vec3 aPos; attribute vec3 aPick; uniform mat4 uMvp; varying vec3 vPick;",
    "void main() { vPick = aPick; gl_Position = uMvp * vec4(aPos, 1.0); }"].join("\n");
  const PICK_FS = "precision mediump float; varying vec3 vPick; void main() { gl_FragColor = vec4(vPick, 1.0); }";

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
    stage.append(canvas);
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
    let n = 0;
    els.forEach(e => { n += e.t.length; });
    count = n;
    const pos = new Float32Array(n * 3), nor = new Float32Array(n * 3);
    const col = new Float32Array(n * 3), pck = new Float32Array(n * 3);
    hi = new Float32Array(n);
    ranges = []; ids = []; cats = [];
    let k = 0;
    let min = [Infinity, Infinity, Infinity], max = [-Infinity, -Infinity, -Infinity];
    els.forEach((e, index) => {
      const code = index + 1;
      const pc = [(code & 255) / 255, ((code >> 8) & 255) / 255, ((code >> 16) & 255) / 255];
      const c = palette[e.c] || [0.6, 0.6, 0.6];
      ranges.push([k, e.t.length]);
      ids.push(String(e.id));
      cats.push(e.c);
      for (let t = 0; t < e.t.length; t += 3) {
        const a = e.t[t] * 3, b = e.t[t + 1] * 3, d = e.t[t + 2] * 3;
        const A = [e.v[a], e.v[a + 1], e.v[a + 2]], B = [e.v[b], e.v[b + 1], e.v[b + 2]];
        const D = [e.v[d], e.v[d + 1], e.v[d + 2]];
        const N = norm(cross([B[0] - A[0], B[1] - A[1], B[2] - A[2]], [D[0] - A[0], D[1] - A[1], D[2] - A[2]]));
        [A, B, D].forEach(P => {
          pos.set(P, k * 3); nor.set(N, k * 3); col.set(c, k * 3); pck.set(pc, k * 3);
          for (let i = 0; i < 3; i++) { if (P[i] < min[i]) min[i] = P[i]; if (P[i] > max[i]) max[i] = P[i]; }
          k++;
        });
      }
    });
    if (!isFinite(min[0])) { min = [0, 0, 0]; max = [1, 1, 1]; }
    cam.target = [(min[0] + max[0]) / 2, (min[1] + max[1]) / 2, (min[2] + max[2]) / 2];
    radius = Math.max(1, Math.hypot(max[0] - min[0], max[1] - min[1], max[2] - min[2]) / 2);
    if (buf) Object.values(buf).forEach(b => gl.deleteBuffer(b));
    const upload = arr => { const b = gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER, b); gl.bufferData(gl.ARRAY_BUFFER, arr, gl.STATIC_DRAW); return b; };
    buf = { pos: upload(pos), nor: upload(nor), col: upload(col), pck: upload(pck), hi: upload(hi) };
    lit = false;
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
    const near = Math.max(cam.dist / 1000, 0.01), far = cam.dist + radius * 4;
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
    attrib(draw, "aCol", buf.col, 3); attrib(draw, "aHi", buf.hi, 1);
    gl.uniformMatrix4fv(gl.getUniformLocation(draw, "uMvp"), false, mvp(w, h));
    const e = eye();
    gl.uniform3fv(gl.getUniformLocation(draw, "uLight"), norm([e[0] - cam.target[0], e[1] - cam.target[1], e[2] - cam.target[2] + cam.dist * 0.6]));
    gl.uniform1f(gl.getUniformLocation(draw, "uLit"), lit ? 1 : 0);
    gl.uniform3fv(gl.getUniformLocation(draw, "uHi"), rgb(view.highlight));
    gl.uniform3fv(gl.getUniformLocation(draw, "uDim"), rgb(view.dimmed));
    gl.drawArrays(gl.TRIANGLES, 0, count);
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
    attrib(pick, "aPos", buf.pos, 3); attrib(pick, "aPick", buf.pck, 3);
    gl.uniformMatrix4fv(gl.getUniformLocation(pick, "uMvp"), false, mvp(w, h));
    gl.drawArrays(gl.TRIANGLES, 0, count);
    const px = new Uint8Array(4);
    const dpr = window.devicePixelRatio || 1;
    gl.readPixels(Math.round(x * dpr), Math.round(h - y * dpr), 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, px);
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
  };
})();
