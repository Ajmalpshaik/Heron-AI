# The eighteenth session, 2026-09-02 — eight fragments, and the check that finally reads its own label

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **94 to 102**. All 102 compile on all eight releases; all 102 are
`DRAFT`. **Every signature was read off the reference assemblies BEFORE writing** — the lesson from last
session — and the batch compiled clean on the first run, which the previous two did not.

| | |
|---|---|
| `CREATE_REVISION` | **The input `ADD_REVISION_CLOUD` could not get.** That fragment needs a `revisionId` and refuses to invent one; nothing could produce one, so the whole revision job stopped at step one. It does **not** issue the revision: `Issued` is a one-way gate that locks the description and refuses any new cloud, so creating one already issued makes a revision nobody can cloud |
| `LIST_REVISIONS` | *"Which sheets go out with this issue"* — the print list. **A revision on NO sheets is the row worth finding**: either not clouded yet, or clouded on a view that is on no sheet, and the second is invisible in Revit until the drawing goes out unmarked |
| `SET_ELEMENT_PHASE` | *"Set the phase"* was answering `SET_ELEMENT_WORKSET` — **a write, to the wrong property, that reports success.** Created and demolished are asked for separately: a phase called "Existing" says nothing about which was meant, and putting an element ON the existing phase versus DEMOLISHING it there are opposite instructions |
| `READ_SPACE_LOADS` | Heating, cooling and airflow per Space, each figure saying whether Revit **calculated** it or somebody typed it — where they disagree it is either a decision or a stale analysis. A zero is reported as **NO LOAD**, never as `0 W`, because a number-shaped non-answer in front of somebody sizing a chiller is worse than none |
| `CREATE_DRAFTING_VIEW` | Shows no model and never changes — every standard detail in a set is one. The scale is required: a drafting view is empty and has nothing to take one from |
| `CREATE_CALLOUT` | Two things at once — a boundary on the parent and a linked enlarged view. The callout's type is taken from **what the parent is**, because a callout of a section must be a section |
| `FIND_VIEWS_WITHOUT_TEMPLATE` | **On a sheet or not is the whole difference.** A working view needs no template; the same view on a sheet is being *issued*. The working ones are counted, not listed — a flat list is hundreds of rows nobody reads |
| `FIND_UNUSED_FAMILIES` | *"Purge unused"*, answered as a **report**. It deletes nothing: purging is hard to reverse, Revit has the command, and doing it to a shared model from here would be the most damaging thing in this library |

### The units in `READ_SPACE_LOADS` are not all the same kind of certain

Worth reading before that fragment is trusted:

- **Airflow is exact.** Revit stores ft³/s; 1 ft = 0.3048 m *by definition*, so 1 ft³ = 28.316846592 L
  exactly. Same class as D-20's 304.8, provable on paper, needs no model.
- **Load is NOT.** The internal HVAC power unit is *assumed* to be BTU/s and converted at 1055.05585262 W.
  Reflection over an assembly shows a property type, never a unit.

> **If that assumption is wrong every load is out by a constant factor** — which is the most dangerous
> shape a unit error takes: each number looks plausible, every comparison between spaces still works, and
> only somebody sizing a real chiller finds out. **First thing to check against a model**, and it is
> written into the test cases as the first negative rather than asserted as settled.

### The claims check caught its own author, twice, within minutes

The audit added last session — *a routing table is a comment, and comments are not indexed* — fired on
this batch immediately:

- `"which phase is this on"` was claimed by `READ_ELEMENT_PHASE` and served by **`SET_ELEMENT_PHASE`**,
  the new write. The fragment declared *"**what** phase is this on"*. **A one-word difference put a read
  question on a fragment that changes the model.**
- Fixing that shifted the weights and took `"what is this"` and `"what fall is on this drain"` off
  `DESCRIBE_ELEMENTS` and `MEASURE_MEP_SLOPE` — both claimed in tables, neither declared. Stable after
  two rounds.

### The collision list misled two consecutive sessions, and now says so

`check-routing.py` prints **SENTENCES TWO FRAGMENTS BOTH WANT** from `SEARCH.keywords` — **one half of
the fusion, and not what the host calls.** The risk section right above it says loudly that it measures
through `find`; this one said nothing and read like a defect list.

Nine "new" collisions appeared this batch. **All nine resolve correctly through `find`** — identity
catches every one. The previous session edited fragments to chase several of the same kind before
checking; this session nearly did it again.

> The section now carries its own label and the one-line way to check:
> `heron_brain.lookup("the sentence")`. **A diagnostic that does not say which stage it measures will be
> read as a verdict.**

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **102** below `PROVEN`.

---
