# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-AGT-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""Generate the Heron agent map artifact directly from docs/28-agent-registry.md."""
import io, re, json, sys, os

SRC = 'docs/28-agent-registry.md'
OUT = os.environ.get('HERON_MAP_OUT', 'agent-map.html')

s = io.open(SRC, encoding='utf-8').read()

# ---- parse departments and agents ----
LAYER = {
    'Orchestration & Communication': 'brain',
    'Knowledge & RAG': 'brain',
    'Fragment Lifecycle': 'brain',
    'Skill Lifecycle': 'brain',
    'Import & Migration': 'brain',
    'Standards & BIM QA': 'brain',
    'Revit Engineering': 'revit',
    'MCP / Bridge': 'revit',
    'Session & Bridge Management': 'revit',
    'Development': 'build',
    'Agent Lifecycle & HR': 'build',
    'Documentation': 'build',
    'Reporting & Output': 'brain',
    'User & Personalization': 'brain',
    'Learning & Self-Growth': 'brain',
    'GitHub': 'build',
    'Kernel & Platform': 'platform',
    'Workspace & Folder Architecture': 'platform',
    'Naming & Taxonomy': 'platform',
    'Installation & Update': 'platform',
    'Operations, Health & Resilience': 'platform',
}
LAYER_LABEL = {
    'brain': 'Brain — knowledge, skills, standards',
    'revit': 'Revit & Bridge — the execution surface',
    'build': 'Engineering — builds and governs Heron itself',
    'platform': 'Platform — the spine everything runs on',
}

agents, depts, cur = [], [], None
for line in s.split('\n'):
    m = re.match(r'^## [0-9a-z]+\. (.+?) — (\d+)', line)
    if m:
        cur = m.group(1).strip()
        depts.append(cur)
        continue
    if line.startswith('| `HERON-') and cur:
        c = [x.strip() for x in line.split('|')]
        if len(c) < 7:
            continue
        name = re.sub(r'\*\*|↗', '', c[2]).strip()
        does = re.sub(r'\*\*', '', c[3])
        does = re.sub(r'\[([^\]]+)\]\([^)]+\)', r'\1', does)
        does = re.sub(r'\*([^*]+)\*', r'\1', does).replace('↗', '').strip()
        step = c[6].replace('*', '').strip()
        agents.append({
            'id': c[1].strip('`'),
            'n': name,
            'd': does,
            't': c[4].strip(),
            'r': c[5].strip() or '—',
            's': '' if step in ('—', '-', '') else step,
            'dept': cur,
            'layer': LAYER.get(cur, 'platform'),
            'new': '↗' in c[2] or '↗' in c[3],
        })

t1 = sum(1 for a in agents if a['t'] == 'T1')
t2 = sum(1 for a in agents if a['t'] == 'T2')
t3 = sum(1 for a in agents if a['t'] == 'T3')
phase01 = sum(1 for a in agents if a['s'])
sys.stdout.write('parsed %d agents, %d departments (T1=%d T2=%d T3=%d, phase0/1=%d)\n'
                 % (len(agents), len(depts), t1, t2, t3, phase01))

order = []
for lay in ('revit', 'brain', 'build', 'platform'):
    for d in depts:
        if LAYER.get(d, 'platform') == lay and d not in order:
            order.append(d)

DATA = json.dumps({'agents': agents, 'order': order, 'layerLabel': LAYER_LABEL},
                  ensure_ascii=False, separators=(',', ':'))

HTML = u"""<title>Heron Agent Map</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Archivo:wght@400;500;600;700&family=IBM+Plex+Mono:wght@400;500;600&display=swap">
<style>
:root{
  --ground:#F2F4F7; --sheet:#FFFFFF; --ink:#151C27; --ink-2:#4A5567; --ink-3:#78849A;
  --rule:#D8DEE7; --rule-2:#E9EDF2; --accent:#12468F; --accent-soft:#E3ECF9;
  --t1:#6B7A90; --t1-bg:#EAEEF4;
  --t2:#9A6512; --t2-bg:#F8EEDC;
  --t3:#A33A32; --t3-bg:#F9E6E4;
  --phase:#1C6B4A; --phase-bg:#DFF0E7;
  --shadow:0 1px 2px rgba(21,28,39,.05),0 6px 20px -10px rgba(21,28,39,.18);
}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){
  --ground:#0D1117; --sheet:#151A22; --ink:#E6EAF1; --ink-2:#A3AEC0; --ink-3:#79859A;
  --rule:#262E3B; --rule-2:#1D242E; --accent:#79A8F0; --accent-soft:#182739;
  --t1:#93A1B6; --t1-bg:#1C232E;
  --t2:#D6A24E; --t2-bg:#2A2216;
  --t3:#E4756A; --t3-bg:#2E1B19;
  --phase:#5FC095; --phase-bg:#132A21;
  --shadow:0 1px 2px rgba(0,0,0,.4),0 6px 20px -10px rgba(0,0,0,.6);
}}
:root[data-theme="dark"]{
  --ground:#0D1117; --sheet:#151A22; --ink:#E6EAF1; --ink-2:#A3AEC0; --ink-3:#79859A;
  --rule:#262E3B; --rule-2:#1D242E; --accent:#79A8F0; --accent-soft:#182739;
  --t1:#93A1B6; --t1-bg:#1C232E;
  --t2:#D6A24E; --t2-bg:#2A2216;
  --t3:#E4756A; --t3-bg:#2E1B19;
  --phase:#5FC095; --phase-bg:#132A21;
  --shadow:0 1px 2px rgba(0,0,0,.4),0 6px 20px -10px rgba(0,0,0,.6);
}
*{box-sizing:border-box}
body{
  margin:0;background:var(--ground);color:var(--ink);
  font-family:Archivo,'Segoe UI',system-ui,sans-serif;
  font-size:15px;line-height:1.5;-webkit-font-smoothing:antialiased;
}
.wrap{max-width:1180px;margin:0 auto;padding:40px 22px 90px}
.mono{font-family:'IBM Plex Mono',ui-monospace,Menlo,monospace}

/* ---------- masthead ---------- */
header{border-bottom:2px solid var(--ink);padding-bottom:18px;margin-bottom:26px}
.eyebrow{
  font-family:'IBM Plex Mono',monospace;font-size:11px;letter-spacing:.16em;
  text-transform:uppercase;color:var(--ink-3);margin:0 0 10px
}
h1{font-size:clamp(28px,5vw,44px);font-weight:700;letter-spacing:-.022em;margin:0;text-wrap:balance}
.sub{color:var(--ink-2);margin:10px 0 0;max-width:64ch}

/* ---------- the reframe ---------- */
.figures{display:grid;grid-template-columns:repeat(auto-fit,minmax(148px,1fr));gap:1px;
  background:var(--rule);border:1px solid var(--rule);margin:26px 0 30px;border-radius:3px;overflow:hidden}
.fig{background:var(--sheet);padding:16px 18px}
.fig b{display:block;font-size:30px;font-weight:700;letter-spacing:-.02em;font-variant-numeric:tabular-nums;line-height:1.1}
.fig span{display:block;font-family:'IBM Plex Mono',monospace;font-size:10.5px;letter-spacing:.1em;
  text-transform:uppercase;color:var(--ink-3);margin-top:6px}
.fig.k1 b{color:var(--t1)} .fig.k2 b{color:var(--t2)} .fig.k3 b{color:var(--t3)} .fig.kp b{color:var(--phase)}

.callout{border-left:3px solid var(--accent);background:var(--accent-soft);
  padding:14px 18px;border-radius:0 3px 3px 0;margin:0 0 34px;color:var(--ink);max-width:74ch}
.callout strong{font-weight:600}

/* ---------- stack diagram ---------- */
h2{font-size:12px;font-family:'IBM Plex Mono',monospace;letter-spacing:.16em;text-transform:uppercase;
  color:var(--ink-3);margin:0 0 14px;font-weight:500}
.stack{display:flex;flex-direction:column;gap:10px;margin-bottom:38px}
.layer{background:var(--sheet);border:1px solid var(--rule);border-radius:4px;padding:14px 16px;box-shadow:var(--shadow)}
.layer-h{display:flex;align-items:baseline;gap:10px;margin-bottom:11px;flex-wrap:wrap}
.layer-h b{font-size:14px;font-weight:600}
.layer-h em{font-style:normal;color:var(--ink-3);font-size:12.5px}
.layer-h .ct{margin-left:auto;font-family:'IBM Plex Mono',monospace;font-size:11px;color:var(--ink-3);
  font-variant-numeric:tabular-nums}
.chips{display:flex;flex-wrap:wrap;gap:6px}
.chip{border:1px solid var(--rule);border-radius:3px;padding:5px 9px;font-size:12.5px;
  background:var(--ground);display:flex;align-items:center;gap:7px;cursor:pointer;color:var(--ink);
  font-family:inherit;text-align:left}
.chip:hover{border-color:var(--accent);color:var(--accent)}
.chip:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
.chip .n{font-family:'IBM Plex Mono',monospace;font-size:10.5px;color:var(--ink-3);font-variant-numeric:tabular-nums}
.flowline{display:flex;align-items:center;justify-content:center;gap:8px;color:var(--ink-3);
  font-family:'IBM Plex Mono',monospace;font-size:10.5px;letter-spacing:.1em;text-transform:uppercase}
.flowline::before,.flowline::after{content:"";height:1px;background:var(--rule);flex:1;max-width:120px}

/* ---------- controls ---------- */
.controls{display:flex;gap:9px;flex-wrap:wrap;align-items:center;margin-bottom:20px;
  position:sticky;top:0;background:var(--ground);padding:12px 0;z-index:5;border-bottom:1px solid var(--rule)}
input[type=search]{
  flex:1;min-width:190px;padding:8px 11px;border:1px solid var(--rule);border-radius:3px;
  background:var(--sheet);color:var(--ink);font:inherit;font-size:13.5px}
input[type=search]:focus{outline:2px solid var(--accent);outline-offset:-1px;border-color:var(--accent)}
.filt{border:1px solid var(--rule);background:var(--sheet);color:var(--ink-2);border-radius:3px;
  padding:7px 12px;font:inherit;font-size:12.5px;cursor:pointer}
.filt:hover{border-color:var(--accent)}
.filt:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
.filt[aria-pressed="true"]{background:var(--ink);color:var(--ground);border-color:var(--ink)}
.filt.t1[aria-pressed="true"]{background:var(--t1);border-color:var(--t1);color:#fff}
.filt.t2[aria-pressed="true"]{background:var(--t2);border-color:var(--t2);color:#fff}
.filt.t3[aria-pressed="true"]{background:var(--t3);border-color:var(--t3);color:#fff}
.filt.ph[aria-pressed="true"]{background:var(--phase);border-color:var(--phase);color:#fff}
.count{font-family:'IBM Plex Mono',monospace;font-size:11.5px;color:var(--ink-3);
  font-variant-numeric:tabular-nums;margin-left:auto}

/* ---------- registry ---------- */
.dept{background:var(--sheet);border:1px solid var(--rule);border-radius:4px;margin-bottom:14px;
  overflow:hidden;box-shadow:var(--shadow)}
.dept > h3{margin:0;padding:12px 16px;border-bottom:1px solid var(--rule-2);display:flex;
  align-items:baseline;gap:10px;font-size:14.5px;font-weight:600;background:var(--sheet)}
.dept > h3 .lay{font-family:'IBM Plex Mono',monospace;font-size:10px;letter-spacing:.1em;
  text-transform:uppercase;color:var(--ink-3);font-weight:400}
.dept > h3 .ct{margin-left:auto;font-family:'IBM Plex Mono',monospace;font-size:11px;
  color:var(--ink-3);font-weight:400;font-variant-numeric:tabular-nums}
.row{display:grid;grid-template-columns:210px 1fr auto;gap:14px;padding:9px 16px;
  border-top:1px solid var(--rule-2);align-items:baseline}
.row:first-of-type{border-top:none}
.row .nm{font-weight:500;font-size:13.5px}
.row .nm .id{display:block;font-family:'IBM Plex Mono',monospace;font-size:10px;color:var(--ink-3);
  font-weight:400;margin-top:2px;letter-spacing:.01em}
.row .ds{color:var(--ink-2);font-size:13px;line-height:1.45}
.row .mk{display:flex;gap:5px;align-items:center;white-space:nowrap}
.t{font-family:'IBM Plex Mono',monospace;font-size:10px;font-weight:600;padding:2px 6px;border-radius:2px}
.t.T1{color:var(--t1);background:var(--t1-bg)}
.t.T2{color:var(--t2);background:var(--t2-bg)}
.t.T3{color:var(--t3);background:var(--t3-bg)}
.rk{font-family:'IBM Plex Mono',monospace;font-size:9.5px;letter-spacing:.06em;color:var(--ink-3);
  border:1px solid var(--rule);padding:2px 5px;border-radius:2px}
.st{font-family:'IBM Plex Mono',monospace;font-size:9.5px;font-weight:600;color:var(--phase);
  background:var(--phase-bg);padding:2px 6px;border-radius:2px}
.new{color:var(--accent);font-size:11px;font-weight:600}
.empty{padding:28px 16px;color:var(--ink-3);text-align:center;font-size:13.5px}
footer{margin-top:44px;padding-top:18px;border-top:1px solid var(--rule);color:var(--ink-3);font-size:12.5px}
@media (max-width:720px){
  .row{grid-template-columns:1fr;gap:5px}
  .row .mk{margin-top:3px}
  .controls{position:static}
}
@media (prefers-reduced-motion:reduce){*{transition:none!important;animation:none!important}}
</style>

<div class="wrap">
<header>
  <p class="eyebrow">Heron AI &middot; Agent Registry</p>
  <h1>Every agent, and what it actually costs</h1>
  <p class="sub">244 agents across 21 departments. The number looks frightening until you separate
  what is ordinary code from what actually calls a model.</p>
</header>

<div class="figures">
  <div class="fig"><b id="fTot">217</b><span>Agents total</span></div>
  <div class="fig k1"><b id="fT1">147</b><span>T1 &middot; plain code</span></div>
  <div class="fig k2"><b id="fT2">52</b><span>T2 &middot; one model call</span></div>
  <div class="fig k3"><b id="fT3">18</b><span>T3 &middot; agentic loop</span></div>
  <div class="fig kp"><b id="fPh">20</b><span>Built in Phase 0&ndash;1</span></div>
</div>

<p class="callout"><strong>147 of the 217 never call a model at all.</strong> They are ordinary classes
with a method or two. Read that way, Heron is a normal application with about 147 services, 52 narrow
model calls and 18 genuine agentic workflows. Only 42 of them &mdash; a fifth &mdash; exist at all
before the first model change.</p>

<h2>How the layers stack</h2>
<div class="stack" id="stack"></div>

<h2>The full registry</h2>
<div class="controls">
  <input type="search" id="q" placeholder="Search agent, ID or description&hellip;" aria-label="Search agents">
  <button class="filt t1" data-t="T1" aria-pressed="false">T1</button>
  <button class="filt t2" data-t="T2" aria-pressed="false">T2</button>
  <button class="filt t3" data-t="T3" aria-pressed="false">T3</button>
  <button class="filt ph" id="phBtn" aria-pressed="false">Phase 0&ndash;1 only</button>
  <button class="filt" id="clr">Reset</button>
  <span class="count" id="cnt"></span>
</div>
<div id="reg"></div>

<footer>
  Generated from <span class="mono">docs/28-agent-registry.md</span>. Entries marked
  <span class="new">&#8599;</span> have responsibilities written during review rather than quoted from a
  specification &mdash; proposals, to confirm as each agent is built.
</footer>
</div>

<script>
const D = __DATA__;
const state = {q:'', tiers:new Set(), phase:false};
const byDept = {};
D.agents.forEach(a=>{ (byDept[a.dept] = byDept[a.dept] || []).push(a); });

/* ---- layer stack ---- */
const layers = ['revit','brain','build','platform'];
const flow = {revit:'user request enters here', brain:'decides what to do',
              build:'builds and governs Heron', platform:'runs underneath all of it'};
const stack = document.getElementById('stack');
layers.forEach((lay,i)=>{
  const ds = D.order.filter(d => (byDept[d]||[]).length && byDept[d][0].layer===lay);
  if(!ds.length) return;
  const n = ds.reduce((s,d)=>s+byDept[d].length,0);
  const el = document.createElement('div');
  el.className='layer';
  const head = document.createElement('div');
  head.className='layer-h';
  head.innerHTML = '<b>'+D.layerLabel[lay].split(' — ')[0]+'</b><em>'+
    (D.layerLabel[lay].split(' — ')[1]||'')+'</em><span class="ct">'+n+' agents</span>';
  el.appendChild(head);
  const chips = document.createElement('div');
  chips.className='chips';
  ds.forEach(d=>{
    const b=document.createElement('button');
    b.className='chip'; b.type='button';
    b.innerHTML = d+' <span class="n">'+byDept[d].length+'</span>';
    b.addEventListener('click',()=>{
      document.getElementById('q').value = '';
      state.q=''; render();
      const t=document.getElementById('dept-'+slug(d));
      if(t) t.scrollIntoView({behavior:'smooth',block:'start'});
    });
    chips.appendChild(b);
  });
  el.appendChild(chips);
  stack.appendChild(el);
  if(i<layers.length-1){
    const f=document.createElement('div'); f.className='flowline'; f.textContent=flow[lay]||'';
    stack.appendChild(f);
  }
});

function slug(s){return s.toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');}
function esc(s){return String(s).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));}

function match(a){
  if(state.tiers.size && !state.tiers.has(a.t)) return false;
  if(state.phase && !a.s) return false;
  if(state.q){
    const q=state.q.toLowerCase();
    if(!(a.n.toLowerCase().includes(q)||a.d.toLowerCase().includes(q)||a.id.toLowerCase().includes(q)||a.dept.toLowerCase().includes(q))) return false;
  }
  return true;
}

function render(){
  const reg=document.getElementById('reg');
  reg.textContent='';
  let shown=0;
  D.order.forEach(dept=>{
    const list=(byDept[dept]||[]).filter(match);
    if(!list.length) return;
    shown+=list.length;
    const sec=document.createElement('section');
    sec.className='dept'; sec.id='dept-'+slug(dept);
    const h=document.createElement('h3');
    h.innerHTML='<span>'+esc(dept)+'</span><span class="lay">'+
      esc(D.layerLabel[list[0].layer].split(' — ')[0])+'</span><span class="ct">'+list.length+'</span>';
    sec.appendChild(h);
    list.forEach(a=>{
      const r=document.createElement('div');
      r.className='row';
      r.innerHTML =
        '<div class="nm">'+esc(a.n)+(a.new?' <span class="new">&#8599;</span>':'')+
          '<span class="id">'+esc(a.id)+'</span></div>'+
        '<div class="ds">'+esc(a.d)+'</div>'+
        '<div class="mk"><span class="t '+a.t+'">'+a.t+'</span>'+
          (a.r&&a.r!=='—'?'<span class="rk">'+esc(a.r)+'</span>':'')+
          (a.s?'<span class="st">STEP '+esc(a.s)+'</span>':'')+'</div>';
      sec.appendChild(r);
    });
    reg.appendChild(sec);
  });
  if(!shown){
    const e=document.createElement('div');
    e.className='empty'; e.textContent='No agents match that.';
    reg.appendChild(e);
  }
  document.getElementById('cnt').textContent=shown+' of '+D.agents.length+' shown';
}

document.getElementById('q').addEventListener('input',e=>{state.q=e.target.value;render();});
document.querySelectorAll('.filt[data-t]').forEach(b=>{
  b.addEventListener('click',()=>{
    const t=b.dataset.t;
    if(state.tiers.has(t)) state.tiers.delete(t); else state.tiers.add(t);
    b.setAttribute('aria-pressed',state.tiers.has(t));
    render();
  });
});
document.getElementById('phBtn').addEventListener('click',function(){
  state.phase=!state.phase;
  this.setAttribute('aria-pressed',state.phase);
  render();
});
document.getElementById('clr').addEventListener('click',()=>{
  state.q=''; state.tiers.clear(); state.phase=false;
  document.getElementById('q').value='';
  document.querySelectorAll('.filt').forEach(b=>b.setAttribute('aria-pressed','false'));
  render();
});

const c=(t)=>D.agents.filter(a=>a.t===t).length;
document.getElementById('fTot').textContent=D.agents.length;
document.getElementById('fT1').textContent=c('T1');
document.getElementById('fT2').textContent=c('T2');
document.getElementById('fT3').textContent=c('T3');
document.getElementById('fPh').textContent=D.agents.filter(a=>a.s).length;
render();
</script>
"""

HTML = HTML.replace('__DATA__', DATA)
io.open(OUT, 'w', encoding='utf-8').write(HTML)
sys.stdout.write('wrote %s (%d KB)\n' % (OUT, len(HTML) // 1024))
