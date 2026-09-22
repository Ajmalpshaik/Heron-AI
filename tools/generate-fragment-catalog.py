# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-FRG-004
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Fragment Documentation Agent - the catalogue, generated from the library.

    python tools/generate-fragment-catalog.py
    HERON_CATALOG_OUT=somewhere.html python tools/generate-fragment-catalog.py

docs/28: `HERON-DOC-FRG-004` Fragment Documentation Agent - *"Generated from
fragment metadata."* This is that, and the parallel to
`generate-agent-map.py`, which is the same idea one layer up.

WHY IT WAS WORTH BUILDING
-------------------------
The library had 349 fragments the day this was written and no way to read
them - `ls brain/fragments/*/fragment.yaml | wc -l` says what it holds now,
and this page prints the figure it actually read rather than a second copy
of it. `heron_capabilities` answers *what can
Heron do* at the level of jobs and capabilities, which is the right answer to
that question and not this one. Nothing showed the library itself - what each
fragment is, what it needs, what it leaves behind, what it is allowed to
touch, and whether anybody has ever watched it work.

The demand was measured rather than assumed: over one session, "what have we
got" was answered five times by writing throwaway Python over the yaml files.
A question asked five times with a script each time is a missing page.

IT IS GENERATED, WHICH IS THE POINT
-----------------------------------
tools/README states the rule this file exists under: *a generated artefact
cannot lie about its source.* Every number and every row here comes off disk
at the moment it runs. There is nothing to keep in step, and re-running it is
the only way to change it - the same discipline as the agent map, and the
reason neither is committed.

WHAT IT SHOWS THAT A LIST OF NAMES WOULD NOT
--------------------------------------------
Status against evidence. A fragment sitting at DRAFT with a full set of
declared cases is waiting for a machine; one at DRAFT with no negative case
that can run is waiting for a DECISION, and they look identical in a list of
names. The catalogue separates them, because that difference is what the proof
queue is actually made of.

It states what it does NOT know, in the same breath. A fragment being in here
is not a claim that it works - most of them have never met a model, and the
line this page prints when it runs says how many there are and how many carry
a proof.

THAT SENTENCE USED TO TYPE THE NUMBER, AND THE NUMBER WENT STALE. It said
"349 are catalogued" in the present tense while the library held 396, in the
one file whose own argument is that *a generated artefact cannot lie about
its source*. The page never did; its docstring did. AGENTS.md has the rule
and the reason: three READMEs in this repository once carried a fragment
count wrong by more than a hundred.
"""

import io
import os
import re
import sys
import json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as HF                                   # noqa: E402

OUT = os.environ.get("HERON_CATALOG_OUT", "fragment-catalog.html")

# The arrangement that cannot be run, from the lesson of 2026-09-07. Kept in
# step with brain/heron_validate.py by being the same sentence, not by being
# the same constant - this tool must not import the validation agent to draw a
# page, and the alternative to a second copy here would be a shared module
# holding one regex, which is more machinery than the fact deserves.
DISPROVED = re.compile(
    r"(empty (element|selection) list|an empty selection|nothing selected"
    r"|no elements selected)", re.I)


def cases_of(frag):
    """Declared positive and negative cases, and whether the negative can run."""
    cases, problem = frag.cases()
    if problem:
        return [], [], problem

    def givens(section):
        rows = []
        for row in (cases.get(section) or []):
            if isinstance(row, dict) and row.get("given"):
                rows.append(" ".join(str(row["given"]).split()))
        return rows

    positive, negative = givens("positive"), givens("negative")
    stranded = [g for g in negative if DISPROVED.search(g)]
    workable = [g for g in negative if g not in stranded]

    # FLAG ANY STRANDED CASE, NOT ONLY A FRAGMENT MADE ENTIRELY OF THEM.
    #
    # The first version only spoke when EVERY negative case was unrunnable,
    # and reported 0 across the whole library - while eighteen fragments each
    # carry one. Every one of those eighteen also carries a workable case, so
    # "is it blocked" is no, and the honest answer is both halves at once: the
    # arrangement is dead, and the fragment is not stuck behind it. A page that
    # showed nothing was accurate about blocking and silent about the defect.
    note = None
    if stranded:
        note = ("%d declared negative case(s) cannot run - %r. Fed `elements`, "
                "so an empty list is refused before the fragment starts. %s"
                % (len(stranded), stranded[0],
                   ("Not blocking: %d workable case(s) remain."
                    % len(workable)) if workable else
                   "BLOCKING: no workable negative case remains."))
    return positive, negative, note


def proof_of(frag):
    """What the proof says, flattened for display, or None."""
    proof = frag.data.get("proof")
    if not isinstance(proof, dict):
        return None
    return {
        "date": str(proof.get("date") or ""),
        "by": str(proof.get("by") or ""),
        "model": str(proof.get("model") or ""),
        "positive": str(proof.get("positive_case") or ""),
        "negative": str(proof.get("negative_case") or ""),
        "second": str(proof.get("second_route") or ""),
    }


def contract_of(frag):
    contract = frag.data.get("contract") or {}

    def side(key):
        out = []
        for item in (contract.get(key) or []):
            if isinstance(item, dict):
                out.append("%s: %s" % (item.get("name", "?"), item.get("type", "?")))
        return out
    return side("needs"), side("provides")


def collect():
    found, problems = HF.load_all()
    rows = []
    for frag in sorted(found.values(), key=lambda f: f.slug):
        positive, negative, note = cases_of(frag)
        needs, provides = contract_of(frag)
        rows.append({
            "slug": frag.slug,
            "id": frag.id,
            "capability": frag.data.get("capability"),
            "says": frag.data.get("semantic-identity") or "",
            "kind": frag.data.get("kind"),
            "domain": frag.data.get("domain"),
            "risk": frag.data.get("risk"),
            "status": frag.status,
            "revit": [str(v) for v in (frag.data.get("revit") or [])],
            "needs": needs,
            "provides": provides,
            "utterances": [str(u) for u in (frag.data.get("utterances") or [])][:6],
            "positive": positive,
            "negative": negative,
            "note": note,
            "proof": proof_of(frag),
        })
    return rows, problems


HTML = u"""<title>Heron Fragment Catalogue</title>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<style>
:root{
  --ink:#16232B; --muted:#66767C; --ground:#EFF2F1; --card:#FAFBFA;
  --rule:#C6D0D1; --accent:#1D5C68; --write:#A9660F; --proven:#3D7A55;
}
@media (prefers-color-scheme:dark){:root{
  --ink:#DFE7E8; --muted:#84969B; --ground:#0F1719; --card:#161F22;
  --rule:#2B3A3E; --accent:#63BAC5; --write:#DFA246; --proven:#71B98A;}}
*{box-sizing:border-box}
body{margin:0;background:var(--ground);color:var(--ink);
     font:14px/1.55 -apple-system,"Segoe UI",system-ui,sans-serif}
header{padding:18px 20px;border-bottom:1px solid var(--rule);background:var(--card)}
h1{margin:0 0 4px;font-size:19px;letter-spacing:-.01em}
.sub{color:var(--muted);font-size:13px}
.bar{display:flex;flex-wrap:wrap;gap:8px;padding:12px 20px;
     border-bottom:1px solid var(--rule);background:var(--card);
     position:sticky;top:0;z-index:5}
input,select{font:inherit;padding:6px 9px;border:1px solid var(--rule);
     border-radius:3px;background:var(--ground);color:var(--ink)}
input{flex:1;min-width:200px}
main{padding:14px 20px 60px;display:grid;gap:10px;
     grid-template-columns:repeat(auto-fill,minmax(330px,1fr))}
.f{border:1px solid var(--rule);border-radius:4px;background:var(--card);padding:11px 13px}
.f h2{margin:0;font-size:14px;letter-spacing:-.005em}
.cap{font:11px ui-monospace,Consolas,monospace;color:var(--accent);margin:2px 0 6px}
.says{color:var(--muted);font-size:13px;margin:0 0 8px}
.tags{display:flex;flex-wrap:wrap;gap:5px;margin-bottom:7px}
.t{font:10px ui-monospace,Consolas,monospace;letter-spacing:.06em;
   text-transform:uppercase;border:1px solid currentColor;border-radius:2px;
   padding:1px 5px;color:var(--muted)}
.t.proven{color:var(--proven)} .t.write{color:var(--write)} .t.read{color:var(--accent)}
details{margin-top:6px;font-size:12.5px}
summary{cursor:pointer;color:var(--muted)}
dl{margin:6px 0 0;display:grid;grid-template-columns:auto 1fr;gap:2px 10px}
dt{font:10px ui-monospace,Consolas,monospace;letter-spacing:.06em;
   text-transform:uppercase;color:var(--muted);padding-top:2px}
dd{margin:0}
ul{margin:3px 0;padding-left:17px} li{margin:1px 0}
.warn{border-left:3px solid var(--write);padding-left:8px;color:var(--write);
      font-size:12.5px;margin-top:7px}
footer{padding:16px 20px;border-top:1px solid var(--rule);color:var(--muted);
       font-size:12.5px}
.none{padding:30px 20px;color:var(--muted)}
</style>
<header>
  <h1>Heron Fragment Catalogue</h1>
  <div class="sub" id="sub"></div>
</header>
<div class="bar">
  <input id="q" placeholder="Search name, capability, what it does, or a phrase somebody would say">
  <select id="status"><option value="">any status</option></select>
  <select id="risk"><option value="">any risk</option></select>
  <select id="domain"><option value="">any domain</option></select>
  <select id="proof">
    <option value="">proof: any</option>
    <option value="yes">has a proof</option>
    <option value="no">no proof yet</option>
    <option value="stuck">negative case cannot run</option>
  </select>
</div>
<main id="list"></main>
<footer id="foot"></footer>
<script>
var D = __DATA__;
var list = document.getElementById('list');

function fill(sel, values){
  values.sort().forEach(function(v){
    var o = document.createElement('option'); o.value = o.textContent = v;
    document.getElementById(sel).appendChild(o);
  });
}
function uniq(key){
  var s = {}; D.rows.forEach(function(r){ if(r[key]) s[r[key]] = 1; });
  return Object.keys(s);
}
fill('status', uniq('status')); fill('risk', uniq('risk')); fill('domain', uniq('domain'));

function esc(s){ return String(s == null ? '' : s)
  .replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

function ul(items){
  if(!items || !items.length) return '';
  return '<ul>' + items.map(function(i){ return '<li>' + esc(i) + '</li>'; }).join('') + '</ul>';
}

function card(r){
  var tags = '<span class="t">' + esc(r.kind) + '</span>' +
    '<span class="t ' + (r.risk === 'READ' ? 'read' : (r.risk === 'MODIFY' ? 'write' : '')) +
      '">' + esc(r.risk) + '</span>' +
    '<span class="t ' + (r.proof ? 'proven' : '') + '">' + esc(r.status) + '</span>' +
    '<span class="t">' + esc(r.revit[0] || '?') + '-' + esc(r.revit[r.revit.length-1] || '?') + '</span>';

  var body = '<h2>' + esc(r.slug) + '</h2>' +
    '<div class="cap">' + esc(r.capability) + '</div>' +
    '<p class="says">' + esc(r.says) + '</p>' +
    '<div class="tags">' + tags + '</div>';

  if(r.note) body += '<div class="warn">' + esc(r.note) + '</div>';

  body += '<details><summary>contract and cases</summary><dl>' +
    '<dt>needs</dt><dd>' + (r.needs.length ? esc(r.needs.join(', ')) : '&mdash;') + '</dd>' +
    '<dt>leaves</dt><dd>' + (r.provides.length ? esc(r.provides.join(', ')) : '&mdash;') + '</dd>' +
    '</dl>';
  if(r.utterances.length) body += '<dl><dt>said as</dt><dd>' + ul(r.utterances) + '</dd></dl>';
  if(r.positive.length) body += '<dl><dt>proves</dt><dd>' + ul(r.positive) + '</dd></dl>';
  if(r.negative.length) body += '<dl><dt>empty when</dt><dd>' + ul(r.negative) + '</dd></dl>';
  if(r.proof){
    body += '<dl><dt>proof</dt><dd>' + esc(r.proof.date) + ', ' + esc(r.proof.by) +
            '<br>' + esc(r.proof.model) + '</dd></dl>';
  }
  body += '</details>';

  var d = document.createElement('div');
  d.className = 'f';
  d.innerHTML = body;
  return d;
}

function draw(){
  var q = document.getElementById('q').value.toLowerCase().trim();
  var st = document.getElementById('status').value;
  var rk = document.getElementById('risk').value;
  var dm = document.getElementById('domain').value;
  var pf = document.getElementById('proof').value;

  var shown = D.rows.filter(function(r){
    if(st && r.status !== st) return false;
    if(rk && r.risk !== rk) return false;
    if(dm && r.domain !== dm) return false;
    if(pf === 'yes' && !r.proof) return false;
    if(pf === 'no' && r.proof) return false;
    if(pf === 'stuck' && !r.note) return false;
    if(!q) return true;
    var hay = [r.slug, r.capability, r.says, r.domain].concat(r.utterances).join(' ').toLowerCase();
    return hay.indexOf(q) >= 0;
  });

  list.innerHTML = '';
  if(!shown.length){
    list.innerHTML = '<div class="none">Nothing matches.</div>';
  } else {
    shown.forEach(function(r){ list.appendChild(card(r)); });
  }
  document.getElementById('sub').textContent =
    shown.length + ' of ' + D.rows.length + ' fragments \\u00b7 ' +
    D.proven + ' carry a proof \\u00b7 generated ' + D.at;
}

['q','status','risk','domain','proof'].forEach(function(id){
  document.getElementById(id).addEventListener('input', draw);
});
draw();
document.getElementById('foot').textContent = D.footer;
</script>
"""


def main():
    rows, problems = collect()
    proven = sum(1 for r in rows if r["proof"])
    stuck = sum(1 for r in rows if r["note"])

    import datetime
    payload = {
        "rows": rows,
        "proven": proven,
        "at": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "footer": (
            "Generated from brain/fragments by tools/generate-fragment-catalog.py. "
            "Being in this catalogue is NOT a claim that a fragment works: %d of "
            "%d carry a proof against a real model, and the rest have never met "
            "one. Re-run rather than edit - this page cannot be kept in step by "
            "hand, which is why it is generated." % (proven, len(rows))),
    }

    text = HTML.replace("__DATA__", json.dumps(payload, sort_keys=True))
    io.open(OUT, "w", encoding="utf-8").write(text)

    def w(s):
        sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))

    w("%d fragment(s), %d with a proof, %d whose negative case cannot run\n"
      % (len(rows), proven, stuck))
    for line in problems[:5]:
        w("  unreadable: %s\n" % line)
    w("wrote %s (%d KB)\n" % (OUT, len(text) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
