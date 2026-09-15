# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-SKL-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Skill Documentation Agent - what a person can ask for, generated.

    python tools/generate-skill-catalog.py
    HERON_SKILL_CATALOG_OUT=somewhere.html python tools/generate-skill-catalog.py

docs/28: `HERON-DOC-SKL-003` Skill Documentation Agent - "Generated from
skill metadata." This is that, and the parallel to
`generate-fragment-catalog.py` one layer up.

A SKILL IS ONLY AS PROVEN AS THE WEAKEST FRAGMENT UNDER IT
------------------------------------------------------------
This is the whole reason the page exists and the one thing a list of
names cannot show. Every skill in the library sits at DRAFT. Some of
them rest entirely on PROVEN fragments and are waiting for nothing but
somebody to look; one rests entirely on DRAFT fragments and cannot move
until those do. In a list of names they are identical.

So each skill carries an EFFECTIVE status - the lowest on docs/09's
ladder among the fragments serving its capabilities - beside the status
its own card declares. A card can say anything; the chain underneath is
the fact.

REACHABLE PER RELEASE, NEVER OVERALL
--------------------------------------
A skill declaring 2020 to 2027 whose fragments cover 2024 and 2025 works
on two releases and claims eight. Reported per release, the same as
HERON-SKL-PRF-006 reports performance and for the same reason: an
overall figure is exactly what hides it.

IT CONCLUDES, SO IT HAS A TEST
--------------------------------
tests/test_catalog.py states the rule this file inherits: a generator
that only draws needs no test, one that CONCLUDES does. This one decides
a skill's effective status and its reachability, and both are judgements
that can be wrong while the page still renders.

WHAT IT DOES NOT CLAIM
------------------------
That any of this has been run against a model. Being in the catalogue is
not evidence, and the page says so in its own footer rather than leaving
a reader to assume it.
"""

import datetime
import io
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_skill as SKILL                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

OUT = os.environ.get("HERON_SKILL_CATALOG_OUT", "skill-catalog.html")

# Worse than any status on the ladder. A capability nobody provides is
# not a weak link, it is a missing one, and sorting it as "below
# DISCOVERED" is how it sorts to the top of the page.
UNSERVED = "UNSERVED"


def providers():
    """capability -> [{fragment, status, revit}], straight off disk."""
    found, problems = FRAG.load_all()
    book = {}
    for frag in found.values():
        capability = str(frag.data.get("capability") or "").strip()
        if not capability:
            continue
        book.setdefault(capability, []).append({
            "fragment": frag.id,
            "status": frag.status or "DISCOVERED",
            "revit": list(frag.supported)})
    return book, problems


def weakest(statuses):
    """The lowest status on docs/09's ladder, or UNSERVED."""
    if UNSERVED in statuses:
        return UNSERVED
    known = [one for one in statuses if one in FRAG.STATUSES]
    if not known:
        return UNSERVED
    return min(known, key=FRAG.STATUSES.index)


def unreachable(under, declared):
    """
    Which declared releases the skill cannot run on, and what is missing.

    PER RELEASE, NEVER OVERALL. A skill working on two of the eight it
    claims has a fine headline and six broken releases, and the headline
    is exactly what hides them.
    """
    dead = []
    for release in declared:
        short = [one["capability"] for one in under
                 if release not in one["revit"]]
        if short:
            dead.append({"revit": release, "missing": short})
    return dead


def collect():
    book, broken = providers()
    found, problems = SKILL.load_all()
    rows = []

    for skill in sorted(found.values(), key=lambda one: one.id):
        card = skill.data
        wants = [str(one).strip() for one in skill.needs() if str(one).strip()]
        declared = [str(one) for one in (card.get("revit") or [])]

        under, statuses = [], []
        for capability in wants:
            serving = book.get(capability) or []
            if not serving:
                under.append({"capability": capability, "by": [],
                              "status": UNSERVED, "revit": []})
                statuses.append(UNSERVED)
                continue
            covered = set()
            for one in serving:
                covered |= set(one["revit"])
            here = weakest([one["status"] for one in serving])
            under.append({"capability": capability,
                          "by": [one["fragment"] for one in serving],
                          "status": here, "revit": sorted(covered)})
            statuses.append(here)

        dead = unreachable(under, declared)

        rows.append({
            "id": skill.id, "name": card.get("name") or skill.id,
            "domain": card.get("domain") or "",
            "purpose": " ".join(str(card.get("purpose") or "").split()),
            "risk": card.get("risk") or "",
            "source": card.get("source") or "",
            "declared_status": skill.status or "",
            "effective": weakest(statuses) if statuses else UNSERVED,
            "utterances": skill.utterances(),
            "preconditions": [str(one) for one in
                              (card.get("preconditions") or [])],
            "revit": declared, "under": under, "unreachable_on": dead,
        })
    return rows, problems + broken


HTML = u"""<title>Heron Skill Catalogue</title>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<style>
:root{
  --ink:#16232B; --muted:#66767C; --ground:#EFF2F1; --card:#FAFBFA;
  --rule:#C6D0D1; --accent:#1D5C68; --warn:#A9660F; --proven:#3D7A55;
  --bad:#9E3B33;
}
@media (prefers-color-scheme:dark){:root{
  --ink:#DFE7E8; --muted:#84969B; --ground:#0F1719; --card:#161F22;
  --rule:#2B3A3E; --accent:#63BAC5; --warn:#DFA246; --proven:#71B98A;
  --bad:#E0796F;}}
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
     grid-template-columns:repeat(auto-fill,minmax(340px,1fr))}
.s{border:1px solid var(--rule);border-radius:4px;background:var(--card);
   padding:11px 13px}
.s h2{margin:0;font-size:14px;letter-spacing:-.005em}
.cap{font:11px ui-monospace,Consolas,monospace;color:var(--accent);margin:2px 0 6px}
.says{color:var(--muted);font-size:13px;margin:0 0 8px}
.tags{display:flex;flex-wrap:wrap;gap:5px;margin-bottom:7px}
.t{font:10px ui-monospace,Consolas,monospace;letter-spacing:.06em;
   text-transform:uppercase;border:1px solid currentColor;border-radius:2px;
   padding:1px 5px;color:var(--muted)}
.t.proven{color:var(--proven)} .t.warn{color:var(--warn)} .t.bad{color:var(--bad)}
.t.read{color:var(--accent)}
.words{margin:0 0 7px;padding-left:17px;font-size:13px}
.words li{margin:1px 0}
details{margin-top:6px;font-size:12.5px}
summary{cursor:pointer;color:var(--muted)}
dl{margin:6px 0 0;display:grid;grid-template-columns:auto 1fr;gap:2px 10px}
dt{font:10px ui-monospace,Consolas,monospace;letter-spacing:.06em;
   text-transform:uppercase;color:var(--muted);padding-top:2px}
dd{margin:0}
ul{margin:3px 0;padding-left:17px} li{margin:1px 0}
code{font:11px ui-monospace,Consolas,monospace}
footer{padding:16px 20px;border-top:1px solid var(--rule);color:var(--muted);
       font-size:12.5px;background:var(--card)}
.none{color:var(--muted);padding:24px 20px}
</style>
<header>
  <h1>Heron Skill Catalogue</h1>
  <div class="sub" id="sub">loading</div>
</header>
<div class="bar">
  <input id="q" placeholder="search a skill, a word somebody says, a capability">
  <select id="chain">
    <option value="">any chain</option>
    <option value="UNSERVED">nothing serves it</option>
    <option value="weak">weaker than PROVEN</option>
    <option value="PROVEN">PROVEN all the way down</option>
  </select>
  <select id="dom"><option value="">any domain</option></select>
</div>
<main id="out"></main>
<footer id="foot"></footer>
<script>
const DATA = __DATA__;
const rows = DATA.rows;
document.getElementById("sub").textContent = DATA.subtitle;
document.getElementById("foot").textContent = DATA.footer;

const dom = document.getElementById("dom");
[...new Set(rows.map(r => r.domain).filter(Boolean))].sort()
  .forEach(d => dom.add(new Option(d, d)));

function esc(s){ const e = document.createElement("i");
  e.textContent = s == null ? "" : String(s); return e.innerHTML; }

function card(r){
  const chain = r.effective === "UNSERVED" ? "bad"
              : r.effective === "PROVEN" ? "proven" : "warn";
  const dead = r.unreachable_on.length
    ? `<div class="t bad">unreachable on ${
        r.unreachable_on.map(d => esc(d.revit)).join(", ")}</div>` : "";
  return `<div class="s">
    <h2>${esc(r.name)}</h2>
    <div class="cap">${esc(r.id)}${r.domain ? " &middot; " + esc(r.domain) : ""}</div>
    <p class="says">${esc(r.purpose)}</p>
    <div class="tags">
      <span class="t read">${esc(r.risk)}</span>
      <span class="t">card says ${esc(r.declared_status)}</span>
      <span class="t ${chain}">chain is ${esc(r.effective)}</span>
      ${dead}
    </div>
    <ul class="words">${r.utterances.map(u => `<li>${esc(u)}</li>`).join("")}</ul>
    <details><summary>what it stands on</summary><dl>
      <dt>needs</dt><dd><ul>${r.under.map(u =>
        `<li><code>${esc(u.capability)}</code> &mdash; ${
          u.by.length ? esc(u.by.join(", ")) + " (" + esc(u.status) + ")"
                      : "<b>nothing provides this</b>"}</li>`).join("")}</ul></dd>
      <dt>revit</dt><dd>${esc(r.revit.join(", ")) || "not declared"}</dd>
      ${r.preconditions.length ? `<dt>before</dt><dd><ul>${
        r.preconditions.map(p => `<li>${esc(p)}</li>`).join("")}</ul></dd>` : ""}
      ${r.unreachable_on.length ? `<dt>dead on</dt><dd><ul>${
        r.unreachable_on.map(d => `<li>${esc(d.revit)}: ${
          esc(d.missing.join(", "))}</li>`).join("")}</ul></dd>` : ""}
    </dl></details></div>`;
}

function draw(){
  const q = document.getElementById("q").value.toLowerCase().trim();
  const want = document.getElementById("chain").value;
  const where = dom.value;
  const shown = rows.filter(r => {
    if (where && r.domain !== where) return false;
    if (want === "UNSERVED" && r.effective !== "UNSERVED") return false;
    if (want === "PROVEN" && r.effective !== "PROVEN") return false;
    if (want === "weak" && (r.effective === "PROVEN")) return false;
    if (!q) return true;
    return (r.id + " " + r.name + " " + r.purpose + " " +
            r.utterances.join(" ") + " " +
            r.under.map(u => u.capability).join(" ")).toLowerCase().includes(q);
  });
  document.getElementById("out").innerHTML = shown.length
    ? shown.map(card).join("")
    : `<div class="none">Nothing matches.</div>`;
}
["q", "chain", "dom"].forEach(id =>
  document.getElementById(id).addEventListener("input", draw));
draw();
</script>
"""


def main():
    rows, problems = collect()
    unserved = [one for one in rows if one["effective"] == UNSERVED]
    proven = [one for one in rows if one["effective"] == "PROVEN"]
    dead = [one for one in rows if one["unreachable_on"]]

    payload = {
        "rows": rows,
        "subtitle": (
            "%d skill(s), generated from brain/skills at %s. %d rest on a "
            "chain that is PROVEN all the way down; %d on something weaker; "
            "%d on a capability nothing provides."
            % (len(rows), datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
               len(proven), len(rows) - len(proven) - len(unserved),
               len(unserved))),
        "footer": (
            "Generated from brain/skills and brain/fragments by "
            "tools/generate-skill-catalog.py. A skill's CHAIN is the lowest "
            "status on docs/09's ladder among the fragments serving it - a "
            "card can say anything, the chain underneath is the fact. Being "
            "in this catalogue is NOT a claim that anything has been run "
            "against a model. Re-run rather than edit."),
    }

    text = HTML.replace("__DATA__", json.dumps(payload, sort_keys=True))
    io.open(OUT, "w", encoding="utf-8").write(text)

    def w(line):
        sys.stdout.write(line.encode("ascii", "replace").decode("ascii"))

    w("%d skill(s): %d PROVEN all the way down, %d weaker, %d unserved\n"
      % (len(rows), len(proven), len(rows) - len(proven) - len(unserved),
         len(unserved)))
    for one in dead:
        w("  %s is unreachable on %s\n"
          % (one["id"], ", ".join(each["revit"]
                                  for each in one["unreachable_on"])))
    for line in problems[:5]:
        w("  unreadable: %s\n" % line)
    w("wrote %s (%d KB)\n" % (OUT, len(text) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())
