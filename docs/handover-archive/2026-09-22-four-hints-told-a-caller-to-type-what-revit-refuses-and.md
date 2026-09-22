# Session note — FOUR HINTS TOLD A CALLER TO TYPE WHAT REVIT REFUSES, AND FOURTEEN SAID NOTHING

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — FOUR HINTS TOLD A CALLER TO TYPE WHAT REVIT REFUSES, AND FOURTEEN SAID NOTHING

A Windows session with Revit 2024 open on `Project1`. It started as modelling work — Walls in `1 - Mech`
turned green, orange, blue, then projection green with a solid red cut — and it became a measurement
of what a capability's FIRST use costs.

#### What was measured

The first colour change took **twelve** tool calls. The next two took **one** each. Four of the twelve
were refusals from `SET_CATEGORY_GRAPHICS`, each giving away one layer of one fixed fact: the
parameter names, then a view's exact spelling, then the separator, then the nine override settings.
`SET_CATEGORY_SOLID_FILL` then cost two more refusals for the identical three values. **The cost is
not the work; it is learning how to ask for it, once per capability per conversation.**

#### PR #284 had already fixed the discovery, and this session built it a second time

`heron_resolve` now prints `YOU SUPPLY THESE` from `how_to_type` in
[`brain/heron_fragment.py`](../../brain/heron_fragment.py) — one copy, read by the server and by
`tools/generate-jobs.py`. It merged at 19:34 while this session was building the same thing as PR #289
from a branch cut at `2e1abd1`, **without fetching `main` once in between.**

**PR #289 was closed, not merged, and the reason is the one to carry forward: GitHub called it
MERGEABLE.** It had no textual conflict. `git merge-tree --write-tree origin/main HEAD` showed the
result would print TWO "what to type" blocks from two vocabularies — #284's own commit message warns
against exactly that: *"a second copy would drift, and the drift would be invisible."* **Mergeable is
a fact about text, not about meaning.** Fetch `main` before building, and again before merging.

#### What this change adds, measured across the whole library

Every caller-supplied value, read through `heron_bridge_client.fragment_needs` — **765 values, 58
distinct types**. `how_to_type` as #284 left it:

| | types | now |
|---|---|---|
| a WRONG hint - "comma separated" for a shape Revit refuses or splits on semicolons | **4**: `IList<Reference>`, `IList<Color>`, both request dictionaries | corrected; the dictionaries carry `NamedValues`' OWN example strings |
| NO hint, where a modeller will type it wrong or cannot type it at all | **11**, led by `double` (102 values) and `int` (27) - "250mm" is refused | hinted in `FromRequest`'s own words; `Arc`, `IFCVersion` and `Reference` say **CANNOT BE TYPED** |
| no hint, and none needed | **3**: `string`, `Material`, `RevitLinkInstance` | named in `NO_HINT_ON_PURPOSE`, each with its reason |

The other 40 types already had a right hint. Four were name-rule misses — `View3D`,
`ViewDuplicateOption`, `SpatialElement` and `ElementId` carry no word break after `View` or around
`Element`, so the `\b` patterns walked past all four.

```bash
python tests/test_how_to_type.py      # all pass; against main's hints before this change, 21 fail
```

Seen to fail against `main`'s `heron_fragment.py` — **21 clean failures, no traceback**, each naming
a fragment that asks for the type. `NO_HINT_ON_PURPOSE` is read through `getattr`, which is why it
fails rather than crashes.

**Closing #289 lost nothing.** Its code stays reachable at `refs/pull/289/head` (`51f6ccb`). The two
checks it had that nothing on `main` held are carried into the same suite: the override hint must name
every setting the add-in's own refusal lists, read out of `RevitFragment.cs` rather than retyped; and
`heron_resolve` must report an unreadable contract rather than print it as nothing to type. Both were
seen to go red on a trimmed hint and a changed wording.

#### Two traps worth knowing before touching `mcp/`

1. **A session's Heron server runs that session's OWN tree.** `.mcp.json` launches
   `mcp/server/heron_mcp_server.py` by a relative path. After a Claude app restart, a worktree serves
   its branch's code, not `main`'s — which is how #289 was measured live, and how a server change can
   be tested before merge. It is also why a session on an old branch does not see a fix `main` has.
2. **`brain/` shadows `mcp/server/` on `sys.path`.** `heron_brain.py` inserts `brain/` at position 0,
   so a server module sharing a name with a brain module is silently replaced: the import succeeds and
   fails one attribute later. It cost this session a debugging round, on a module called
   `heron_contract`. `tests/test_how_to_type.py` now asserts the three import roots share no name.

#### Proven, and not

- **Proven live:** `SET_CATEGORY_VISIBILITY`, never used in the session, ran on its **first attempt**
  against `1 - Mech` on `Project1` once its contract was printed — **0 refusals, against 4 and 2**.
  Structural Trusses was hidden and restored. The restore reported `changed 1`, which reads back that the
  hide landed. Net change to the model: none. That ran on #289's block. Of the three types it used,
  `View` and `IList<Category>` print the same facts from #284's copy; `bool`'s "true or false" is one
  this change adds.
- **Not proven:** no hint is verified TRUE beyond the strings the suite reads out of `RevitFragment.cs`.
  `FromRequest` is the authority and needs Revit.

#### What is left

- **`RevitLinkInstance` is unhinted on purpose.** Nobody has measured how a link instance's name reads
  to `OneOfClass`. Measure it on a model with a link loaded, then hint it and drop the entry.
- **Clause 3 of the suite names its semicolon types by hand.** A new semicolon-shaped type would pass
  on a wrong "comma separated". Deriving the separator from the C# was rejected — the split hides behind
  five helpers, and a parser misreading one would be a confident false green.
- **The four suites that failed on this PC during the sweep were already fixed on `main`** by PRs
  #285, #286 (merged 19:36) and #288 (merged 20:12). This session then started two side sessions on
  two of them — the relpath crash and the U+2705 tick — **after** both had merged, for the same reason
  it built #289: it had not fetched `main`. #288 covers the tick entirely. #286 fixed
  `tools/api-changes.py` only, and the relpath brief also named the other `os.path.relpath` calls in
  `tools/`, so part of that session's work may be new. Check anything either opens against those two
  PRs before merging it.
