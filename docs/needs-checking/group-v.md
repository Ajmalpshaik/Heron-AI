# Needs checking — Group V

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group V — all 66 unproven fragments, sorted by WHY, read off disk 2026-09-19

**NOT ONE REVIT CALL WAS MADE TO PRODUCE THIS TABLE.** Every fragment that has
ever been run leaves a record in `brain/proof-drafts/runs/`, and the record
holds both phases with what each one bound and what each one returned. Sorting
them is reading, not proving — and it turns *"66 left"* into six lists with a
different owner each. The same method turned 23 agents into 6-13-4 on the same
day, and it costs a minute.

**`runs/` IS GITIGNORED, SO THIS IS A MEASUREMENT OF ONE CHECKOUT.** 326 run
records existed here when this was taken. **THE NUMBERS BELOW CANNOT BE
REPRODUCED FROM A FRESH CLONE AND NO GATE CAN CATCH THEM GOING STALE** - said
plainly because a committed table of derived counts is normally a thing this
repository refuses. It is kept because the SORT is the value and the sort does
not go stale: which bucket a fragment belongs in is a property of the fragment.
Re-derive the counts where you will actually prove, with:

```
python - <<'EOF'
import json, io, os, glob, yaml, re, collections
def empty(v):
    s = str(v).strip()
    if s in ("", "0", "(null)", "None", "False", "false", "[]", "{}"): return True
    return bool(re.match(r"^0 (item|entry|entrie)", s))
b = collections.defaultdict(list)
for f in sorted(glob.glob("brain/fragments/*/fragment.yaml")):
    slug = os.path.basename(os.path.dirname(f))
    text = io.open(f, encoding="utf-8").read()
    if "heron-status: DRAFT" not in text: continue
    rec = os.path.join("brain/proof-drafts/runs", slug + ".json")
    if not os.path.isfile(rec): b["no run record"].append(slug); continue
    d = json.load(io.open(rec, encoding="utf-8"))
    ph = {p.get("phase"): p for p in d.get("phases", [])}
    if "positive" not in ph or not ph["positive"].get("ok"):
        b["positive refused"].append(slug); continue
    if "negative" not in ph: b["no negative"].append(slug); continue
    y = yaml.safe_load(text)
    res = [p["name"] for p in ((y.get("contract") or {}).get("provides") or [])
           if p.get("role") == "result"]
    pv, nv = ph["positive"].get("provides") or {}, ph["negative"].get("provides") or {}
    pe = all(empty(pv.get(r)) for r in res) if res else None
    nf = any(not empty(nv.get(r)) for r in res) if res else None
    b["positive empty" if pe else ("negative not empty" if nf else "re-run")].append(slug)
for k in sorted(b, key=lambda k: -len(b[k])):
    print("%-22s %d  %s" % (k, len(b[k]), " ".join(b[k])))
EOF
``` A different worktree will sort the
same 66 differently — the DRAFT/PROVEN split is committed and shared, the
records are not. Re-derive it where you will actually work.

| | | |
|---|---|---|
| **26** | **no run record here** | Nothing has tried them in this checkout. `generate-jobs.py` offers **4** and marks **22** unarrangeable with a reason each |
| **19** | **POSITIVE EMPTY — it ran and found nothing** | So the arrangement was wrong rather than the fragment. **17 need MODEL CONTENT** — CAD imports, groups, design options, openings, electrical circuits, areas, annotation. **2 need a PERMISSION PHASE THAT DOES NOT EXIST** and were miscounted here first: `export-families` and `export-schedule-to-csv` are `risk: PUBLISH`, which `HeronPermissions` puts out of reach for Phase 0 and Phase 1, so nothing is sent to Revit at all. No model will ever unblock those two — checked by reading `risk:` on all nineteen rather than assuming the bucket was uniform |
| **13** | **POSITIVE never ran or was refused** | The binder said no before the fragment started. A refusal to start is not an answer, and it is the cheapest of these to read: the message names the need |
| **7** | **NEG NOT EMPTY — the negative was arranged wrong** | Listed below, because this is the group a session can fix at a desk |
| **1** | **judged by nothing at all** | `describe-blank-parameters` |

### The 7 whose negative found something, and what is actually wrong with each

**None of these is a fragment defect.** Every one is a proof that was arranged
badly, and three of them are the same mistake: **the negative changed something
the answer does not depend on.**

| fragment | what the negative did | why it could never work |
|---|---|---|
| `add-project-parameter` | same `categories=Ducts` in both legs | not a contrast at all. `bound true` both times because nothing differed |
| `create-workset-3d-views` | changed the CATEGORY | it makes one view per WORKSET. A category cannot move that answer |
| `find-unused-materials` | changed the region box | *unused in the project* is a project-wide fact. `unusedMaterials 60` both times |
| `report-open-documents` | changed the category | it lists what is OPEN IN REVIT. No selection on earth changes that |
| `trace-connectivity` | changed the region, kept `start=selected` | `reached` is seeded with the START, so it can NEVER be empty. Read below — this entry was wrong first |
| `place-structural-family` | Text Notes in a Legend | already [row 128](../FRAGMENT-ISSUES.md) — it placed a column when asked for a beam |
| `report-findings` | nothing matched, honestly | the negative is **CORRECT** — it says *"NOTHING WAS CHECKED"*. `report` is PROSE, so a non-empty string reads as a find |

**FOUR OF THE SEVEN NEED TRACKING RATHER THAN A BETTER NEGATIVE. THREE ARE MODEL-INDEPENDENT FOR A SELECTION** — `create-workset-3d-views`,
`find-unused-materials`, `report-open-documents`. Handing them a different
selection is the wrong experiment, and no amount of re-arranging fixes it.
[D-53](../DECISIONS.md) is what they need: vary an INPUT the answer genuinely
depends on, across **three or more** values, and show the answer following it.
That is the same ruling `prove-agent.py track` already applies to agents that
can never return empty, and `refresh-view` and `save-document` are marked the
same way by `generate-jobs.py` today.

**`trace-connectivity` IS A FOURTH TRACKING CASE, AND THIS ENTRY SAID THE
OPPOSITE FOR AN HOUR.** It was written as *"the one that can be fixed with one
value — give it a different `start`"*, and then the code was read. Two things
make that wrong:

```
fragment.cs:54   queue.Enqueue(start);
fragment.cs:61   reached.Add(current);      <- the start, on the first pass
fragment.cs:95   foreach (var candidate in elements)   <- GEOMETRIC route only
```

**`reached` ALWAYS CONTAINS THE START, so it can never come back empty** while
anything binds at all — which is the exact shape [D-53](../DECISIONS.md) exists
for, and no choice of start changes it.

**And `elements` does not constrain the walk.** The declared-connector route
follows `Connector.IsConnected` wherever it goes; the pool is consulted only
when looking for a geometric neighbour within `tolerance`. So the old negative
— which moved the region and kept `start=selected` — was changing something
that can only move `joinedByGeometry`, and `reached 25` in both legs was the
fragment behaving exactly as written.

**`generate-jobs.py` CANNOT SEE THIS SHAPE.** It marks `refresh-view` and
`save-document` for tracking because they *take* nothing but the document; this
one takes three things and still cannot return empty, because it seeds its own
answer. Worth a rule there. **THE REST OF THE LIBRARY WAS SWEPT FOR THE SAME SHAPE AND
IT IS A SINGLETON** — every fragment's `role: result` fields were matched against
its own need names across all 395, looking for a result seeded directly from an
input, and `trace-connectivity` is the only one (`reached` and `joinedByGeometry`,
both from `start`). So this is one fragment to mark, not a class to design for.

### The singleton, and it was measured rather than assumed

`describe-blank-parameters` is **the only fragment of 395 that declares no
`role: result` at all** — checked across the whole library, DRAFT and PROVEN.
So `batch-prove` cannot judge it: its rule is *did a declared result come back
empty*, and there is no declared result to look at.

**ITS RUN RECORD IS ALREADY A TEXTBOOK D-53 PROOF and nothing can score it.**
On `Snowdon-scratch_ajmal.al`, 9,638 elements:

```
positive   blank 22, absent 0    "22 element(s) have Comments but it is empty"
negative   blank 0,  absent 22   "22 element(s) do NOT have ZZZ NO SUCH PARAMETER at all"
```

The inputs moved and the answer moved with them, **in opposite directions**.
That is exactly what tracking asks for. **What it needs is a way to RECORD
that, not another run** — and whoever builds it should check whether
`heron_validate` can accept a tracked proof for a fragment, the way
`prove-agent.py vary` already does for an agent.
