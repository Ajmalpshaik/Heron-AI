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

AND THE CHAIN IS ONLY HALF THE FACT
-------------------------------------
The paragraph above was the whole of this page until 2026-09-19, and it
has the shape FRAGMENT-ISSUES row 132 names: **a statement about the
FRAGMENTS that reads as a statement about the SKILL**. `heron_brain
.catalogue()` makes the same move with `ready`, computed from whether a
capability has any provider at all.

Measured: seven of the ten skills rest on a chain that is PROVEN all the
way down, and of those seven exactly ONE has every sentence it declares
reaching a capability it declares. Six do not, and one of the three
"weaker" skills answers two ordinary questions with a write. On a page
that showed only the chain, `count-elements` and `mep-grayout` were the
same card.

So each skill now carries its WORDS beside its chain - how many of its
own utterances reach a capability it declares, and loudly when one
reaches something that changes the model.

IT IS A RECORDING, AND THE PAGE SAYS SO
-----------------------------------------
That measurement asks `heron_brain.lookup` once per utterance and takes
about twenty-five minutes for forty-three sentences, which is not a page
render. It is READ from a file `tools/prove-skill.py --routing-to`
wrote, with the date and the index fingerprint it was taken against
printed beside it - row 116's rule, because the store is one file for
every worktree and two measurements are comparable only when that block
matches.

**No recording is NOT the same as no crossings.** A card with none says
so in those words rather than showing nothing, which would read as a
clean sweep.

AND A RECORDING OUTLIVES THE SENTENCES IT COUNTED
---------------------------------------------------
The fingerprint was printed and never compared, so `words 1 of 4 reach`
went on being shown after the four had been edited. The comparison is
made against the PHRASES rather than the store: `global.db` is one file
every worktree writes, so its md5 differs between two renders for
reasons that change nothing, and a rule hung on it would cry STALE on
every page - noise a reader learns to ignore, which is worse than
silence. The phrases are in git, and a difference in them is a real one.

A card whose words have moved reads **OUT OF DATE**, names how many were
never measured and how many are no longer said, and is counted as
NEITHER whole nor short. **A crossing is never suppressed by it**: one
found on a sentence the skill still says stays loud, and one found on a
sentence nobody says any more is not a live danger.

WHAT IT DOES NOT CLAIM
------------------------
That any of this has been run against a model. Being in the catalogue is
not evidence, and the page says so in its own footer rather than leaving
a reader to assume it. **Neither half is a proof**: a chain of PROVEN
fragments is not a proven skill, and words that reach are understanding
rather than evidence - the other half is a model (D-30).
"""

import datetime
import importlib.util
import io
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_skill as SKILL                                   # noqa: E402
import heron_fragment as FRAG                                 # noqa: E402

# THE FILENAME HAS A HYPHEN IN IT, so `import check-skill-routing` is not a
# sentence Python will read - that is the whole of these four lines. It owns
# the rules ABOUT a routing measurement, `classify()` and `words_moved()`
# both, and `tools/prove-skill.py` reads them from the same place. A second
# copy of either is how two tools start disagreeing about the same skill on
# the same day.
_spec = importlib.util.spec_from_file_location(
    "heron_tool_check_skill_routing",
    os.path.join(ROOT, "tools", "check-skill-routing.py"))
ROUTING = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(ROUTING)

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


RECORDINGS = os.path.join(ROOT, "tools", "jobs", "skills")

NOT_MEASURED = "NOT MEASURED"


def routing(folder=None):
    """(meta, {skill id: [row]}) from the newest saved routing measurement.

    Written by `tools/prove-skill.py --routing-to`. Read rather than
    re-computed for the reason in the header: one lookup per utterance is
    twenty-five minutes, and a page nobody waits for is a page nobody runs.

    A MISSING OR UNREADABLE FILE RETURNS EMPTY AND SAYS WHY. It never returns
    a zero dressed as a measurement (D-52) - the caller marks every card
    NOT MEASURED, which is a different sentence from "no crossings".
    """
    folder = folder or RECORDINGS
    meta = {"taken": None, "index": None, "revit": None, "file": None,
            "why": None}
    if not os.path.isdir(folder):
        meta["why"] = "no %s" % os.path.relpath(folder, ROOT)
        return meta, {}

    saved = sorted(name for name in os.listdir(folder)
                   if name.startswith("routing-") and name.endswith(".json"))
    if not saved:
        meta["why"] = ("no routing-*.json in %s - run `python "
                       "tools/prove-skill.py --routing-to %s/routing-<date>"
                       ".json`" % (os.path.relpath(folder, ROOT),
                                   os.path.relpath(folder, ROOT)))
        return meta, {}

    # NEWEST BY NAME, and the names carry the date for exactly this reason.
    path = os.path.join(folder, saved[-1])
    try:
        doc = json.loads(io.open(path, encoding="utf-8").read())
    except (IOError, OSError, ValueError) as why:
        meta["why"] = "%s could not be read - %s" % (saved[-1], why)
        return meta, {}

    meta["file"] = saved[-1]
    meta["taken"] = doc.get("taken")
    meta["revit"] = doc.get("revit")
    meta["index"] = (doc.get("index") or {}).get("md5")
    return meta, doc.get("skills") or {}


def said(rows, declared):
    """What a skill's own words did, as a card can show it.

    `where` is `check-skill-routing.classify`'s word, recorded at measuring
    time - this file does not re-decide it, because a second opinion about
    what a crossing is would be a second opinion.
    """
    out = {"total": len(rows), "reach": 0, "crossing": [], "elsewhere": [],
           "landed": [], "unsettled": [], "reach_unsettled": []}
    for row in rows:
        # SIX FIELDS WHERE THERE ARE SIX. A recording taken before
        # `prove-skill.py` recorded what the RETRIEVER said has five, and
        # padding is what lets both read - an old recording reports no
        # complaint, which is the absence of one and not a clean answer.
        phrase, capability, risk, route, where, told = (
            list(row) + [None] * 6)[:6]
        out["landed"].append({"phrase": phrase, "capability": capability,
                              "risk": risk, "route": route, "where": where,
                              "told": list(told or ())})
        if told:
            out["unsettled"].append(phrase)
            if where == "reach":
                out["reach_unsettled"].append(phrase)
        if where == "reach":
            out["reach"] += 1
        elif where == "crossing":
            out["crossing"].append(phrase)
        else:
            out["elsewhere"].append(phrase)
    out["declared"] = list(declared)
    return out


def collect(recordings=None):
    """`recordings` names the folder to read the measurement from.

    It exists so the WHOLE path can be exercised against a recording written
    for a test, rather than only the pieces. Nothing in this file may edit
    `brain/skills` to make a case appear - row 142 is what that costs, and a
    checkout several sessions share is the thing being protected.
    """
    book, broken = providers()
    where_meta, recorded = routing(recordings)
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

        # THE RECORDING, AND WHETHER IT IS STILL ABOUT THESE SENTENCES.
        spoke = moved = None
        if skill.id in recorded:
            spoke = said(recorded[skill.id], wants)
            gone, fresh = ROUTING.words_moved(recorded[skill.id], skill.utterances())
            moved = {"gone": gone, "fresh": fresh}
            # A CROSSING THE SKILL NO LONGER SAYS IS NOT A LIVE CROSSING, and
            # one it still says is, stale recording or not - so the danger is
            # counted off today's words while the record keeps what it found.
            spoke["crossing_live"] = [one for one in spoke["crossing"]
                                      if one not in gone]

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
            "words": spoke, "words_moved": moved,
        })
    return rows, problems + broken, where_meta


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
  <select id="words">
    <option value="">any words</option>
    <option value="crossing">a question answered by a write</option>
    <option value="short">not every word reaches</option>
    <option value="all">every word reaches</option>
    <option value="shaky">reached, and the retriever complained</option>
    <option value="stale">the recording is out of date</option>
    <option value="none">not measured</option>
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

  // THE WORDS, BESIDE THE CHAIN. A card with no recording says so in those
  // words: an absent measurement is not a clean one, and the difference is
  // the whole reason this half was added (FRAGMENT-ISSUES row 132).
  // A RECORDING OUTLIVES THE SENTENCES IT COUNTED. When the skill's words
  // have moved since it was taken, the count is about a different set, so it
  // is replaced rather than shown - but a crossing among the words the skill
  // STILL says is live either way, and is never suppressed.
  const w = r.words;
  const m = r.words_moved;
  const stale = !!(m && (m.gone.length || m.fresh.length));
  const cross = w ? (w.crossing_live || w.crossing) : [];
  const count = !w
    ? `<span class="t">words NOT MEASURED</span>`
    : stale
    ? `<span class="t warn">words OUT OF DATE &mdash; ${
        m.fresh.length} never measured, ${m.gone.length} no longer said</span>`
    : `<span class="t ${w.reach === w.total ? "proven" : "warn"}">words ${
        w.reach} of ${w.total} reach</span>`;
  // A REACH THE RETRIEVER COMPLAINED ABOUT IS NOT A CLEAN REACH. The right
  // capability, found by a coin toss, or by a words route that ranked the
  // library rather than selecting from it - and the page reported it as a
  // plain success (row 157). THE TWO COMPLAINTS ARE NOT THE SAME STRENGTH:
  // a coin toss says NEITHER route preferred the winner; the words one says
  // only that THAT route had no claim, and nearness may still have. So the
  // card says the retriever complained, never that the answer is wrong.
  const shaky = w ? (w.reach_unsettled || []) : [];
  const words = count + (cross.length
    ? `<span class="t bad">${cross.length} answered by a write</span>` : "")
    + (shaky.length
    ? `<span class="t warn">${shaky.length} reached, retriever complained</span>`
    : "");
  return `<div class="s">
    <h2>${esc(r.name)}</h2>
    <div class="cap">${esc(r.id)}${r.domain ? " &middot; " + esc(r.domain) : ""}</div>
    <p class="says">${esc(r.purpose)}</p>
    <div class="tags">
      <span class="t read">${esc(r.risk)}</span>
      <span class="t">card says ${esc(r.declared_status)}</span>
      <span class="t ${chain}">chain is ${esc(r.effective)}</span>
      ${words}
      ${dead}
    </div>
    <ul class="words">${r.utterances.map(u => {
      const hit = w && w.landed.find(l => l.phrase === u);
      if (!hit) return `<li>${esc(u)}</li>`;
      const mark = hit.where === "reach" ? "proven"
                 : hit.where === "crossing" ? "bad" : "warn";
      return `<li>${esc(u)} <span class="t ${mark}">${esc(hit.where)} &rarr; ${
        esc(hit.capability)} ${esc(hit.risk)}</span>${
        (hit.told || []).map(t =>
          `<span class="t warn">${esc(t)}</span>`).join("")}</li>`;
    }).join("")}</ul>
    <details><summary>what it stands on</summary><dl>
      <dt>needs</dt><dd><ul>${r.under.map(u =>
        `<li><code>${esc(u.capability)}</code> &mdash; ${
          u.by.length ? esc(u.by.join(", ")) + " (" + esc(u.status) + ")"
                      : "<b>nothing provides this</b>"}</li>`).join("")}</ul></dd>
      <dt>revit</dt><dd>${esc(r.revit.join(", ")) || "not declared"}</dd>
      ${stale ? `<dt>words moved</dt><dd><ul>${
        m.fresh.map(p => `<li>${esc(p)} &mdash; never measured</li>`).join("")
      }${m.gone.map(p => `<li>${esc(p)} &mdash; measured, no longer said</li>`
        ).join("")}</ul>re-take with <code>python tools/prove-skill.py --routing-to tools/jobs/skills/routing-&lt;date&gt;.json</code></dd>` : ""}
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
  const say = document.getElementById("words").value;
  const shown = rows.filter(r => {
    if (where && r.domain !== where) return false;
    const rm = r.words_moved, old = !!(rm && (rm.gone.length || rm.fresh.length));
    const rc = r.words ? (r.words.crossing_live || r.words.crossing) : [];
    if (say === "none" && r.words) return false;
    if (say === "crossing" && !rc.length) return false;
    if (say === "stale" && !old) return false;
    if (say === "shaky"
        && !(r.words && (r.words.reach_unsettled || []).length)) return false;
    // OUT OF DATE IS NEITHER, so it answers no to both of the last two.
    if (say === "short" && !(r.words && !old && r.words.reach < r.words.total)) return false;
    if (say === "all" && !(r.words && !old && r.words.reach === r.words.total)) return false;
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
["q", "chain", "dom", "words"].forEach(id =>
  document.getElementById(id).addEventListener("input", draw));
draw();
</script>
"""


def main():
    rows, problems, where_meta = collect()
    unserved = [one for one in rows if one["effective"] == UNSERVED]
    proven = [one for one in rows if one["effective"] == "PROVEN"]
    dead = [one for one in rows if one["unreachable_on"]]

    # THE OTHER HALF, COUNTED THE SAME WAY. `measured` is deliberately not
    # folded into `proven`: a skill can have a chain of PROVEN fragments and
    # a sentence that answers a question with a write, and a single number
    # that merged the two would hide exactly the case this half was added for.
    measured = [one for one in rows if one["words"]]
    # A RECORDING WHOSE SENTENCES HAVE MOVED IS NOT A CLEAN ONE. It stays in
    # `measured` - it did measure something - and is kept OUT of `whole`,
    # because a word added since cannot have reached anything.
    stale = [one for one in measured
             if one["words_moved"]["gone"] or one["words_moved"]["fresh"]]
    whole = [one for one in measured
             if one not in stale
             and one["words"]["reach"] == one["words"]["total"]]
    writes = [one for one in measured if one["words"]["crossing_live"]]
    # A REACH THE RETRIEVER COMPLAINED ABOUT. Counted beside `whole`
    # rather than subtracted from it: the sentence DID reach a declared
    # capability, and whether a complaint spends that is a reader's call,
    # not a number this page may quietly revise (row 157).
    shaky = [one for one in measured if one["words"]["reach_unsettled"]]
    shaky_words = sum(len(one["words"]["reach_unsettled"]) for one in shaky)

    # TWO CAVEATS, BUILT AS A VARIABLE RATHER THAN NESTED IN THE FORMAT.
    # The first version read `shaky_text + stale_text if stale else ""`, which
    # Python parses as `(shaky_text + stale_text) if stale else ""` - so with
    # no stale recording the SHAKY sentence vanished too. The page computed
    # the number and dropped it, which is the whole of what row 157 is about,
    # committed inside the change that added row 157. Found by reading the
    # rendered subtitle rather than the code.
    caveats = ""
    if shaky:
        caveats += (" %d sentence(s) across %d skill(s) reached a declared "
                    "capability AND THE RETRIEVER COMPLAINED - its own note "
                    "says a coin toss, where neither route preferred the "
                    "winner, or a words route that ranked the library rather "
                    "than selecting from it." % (shaky_words, len(shaky)))
    if stale:
        caveats += (" %d recording(s) are OUT OF DATE - the skill's words "
                    "changed since they were taken, so their counts are "
                    "about different sentences." % len(stale))

    payload = {
        "rows": rows,
        "subtitle": (
            "%d skill(s), generated from brain/skills at %s. CHAIN: %d rest "
            "on one that is PROVEN all the way down; %d on something weaker; "
            "%d on a capability nothing provides. WORDS: %s"
            % (len(rows), datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
               len(proven), len(rows) - len(proven) - len(unserved),
               len(unserved),
               ("NOT MEASURED - %s" % (where_meta.get("why") or "no recording"))
               if not measured else
               ("%d of %d measured, %d have every word reaching a capability "
                "they declare, and %d answer a question with something that "
                "CHANGES THE MODEL.%s Recorded %s against index %s - a "
                "recording, not a run."
                % (len(measured), len(rows), len(whole), len(writes),
                   caveats,
                   where_meta.get("taken") or "at an unrecorded time",
                   (where_meta.get("index") or "?")[:8])))),
        "footer": (
            "Generated from brain/skills and brain/fragments by "
            "tools/generate-skill-catalog.py. A skill's CHAIN is the lowest "
            "status on docs/09's ladder among the fragments serving it - a "
            "card can say anything, the chain underneath is the fact. Its "
            "WORDS are the other half: how many of its own utterances reach a "
            "capability it declares, read from %s and NOT re-measured here - "
            "one lookup per utterance is about twenty-five minutes. A card "
            "reading NOT MEASURED has no recording covering it, which is a "
            "different thing from having no crossings; one reading OUT OF "
            "DATE has a recording about sentences the skill no longer says, "
            "compared against the PHRASES, which are in git, and never "
            "against the store, which every worktree writes. NEITHER HALF IS A "
            "PROOF: being in this catalogue is NOT a claim that anything has "
            "been run against a model, and understanding is half a proof - "
            "the other half is a model (D-30). Re-run rather than edit."
            % (where_meta.get("file") or "no recording")),
    }

    text = HTML.replace("__DATA__", json.dumps(payload, sort_keys=True))
    io.open(OUT, "w", encoding="utf-8").write(text)

    def w(line):
        sys.stdout.write(line.encode("ascii", "replace").decode("ascii"))

    w("%d skill(s): %d PROVEN all the way down, %d weaker, %d unserved\n"
      % (len(rows), len(proven), len(rows) - len(proven) - len(unserved),
         len(unserved)))
    if not measured:
        w("  words NOT MEASURED - %s\n"
          % (where_meta.get("why") or "no recording"))
    else:
        w("  words: %d of %d skills measured (%s), %d reach throughout, "
          "%d answer a question with a write\n"
          % (len(measured), len(rows), where_meta.get("file"), len(whole),
             len(writes)))
        for one in writes:
            w("    %s answers %s with a write\n"
              % (one["id"], ", ".join(repr(each) for each in
                                      one["words"]["crossing_live"])))
        for one in shaky:
            w("    %s reached %d declared capability(ies) and the "
              "retriever complained - %s\n"
              % (one["id"], len(one["words"]["reach_unsettled"]),
                 ", ".join(repr(each)
                           for each in one["words"]["reach_unsettled"])))
        for one in stale:
            w("    %s OUT OF DATE - %d never measured, %d no longer said\n"
              % (one["id"], len(one["words_moved"]["fresh"]),
                 len(one["words_moved"]["gone"])))
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
