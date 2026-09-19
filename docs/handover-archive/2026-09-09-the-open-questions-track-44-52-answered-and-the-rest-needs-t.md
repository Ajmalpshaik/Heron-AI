# HANDOVER — 2026-09-09 (the open-questions track): 44 → 52 answered, and the rest needs the PC *(superseded — the live count is in [OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md))*

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Nine questions were open at the start of this track and one is open now.** `Q-51` stays open on
purpose. Everything else became [D-59](../DECISIONS.md) to [D-66](../DECISIONS.md).

**The owner answered two himself and handed the other six over** with *"this you can decide and when
you decide do the best only."* So six of the eight are my decisions, and every one of them is written
up with what it rejected and why — read the decision, not this summary, before changing any of them.

### WHAT IS LEFT, AND ALL OF IT NEEDS A WINDOWS PC WITH REVIT AND `dotnet`

**This container has no `dotnet` and no Revit.** Every rule below is CHECKED by a gate that runs here;
none of the edits was guessed. **Writing 62 collector rewrites nobody could compile would have been the
opposite of production-ready**, and that is the reason the lists are lists.

**The owner said the link work waits for the PC. It is item 1 and it is the biggest.**

| # | What | How many | Where the list is |
|---|---|---|---|
| **1** | **The link contracts** ([D-59](../DECISIONS.md)). Each reading fragment gains `includeLinks` in `needs` (`source: request`) and `linksSearched` in `provides`, and its collector spans `RevitLinkInstance` when asked. **Absent means host only**, so nothing already proved changes | **62** | `python tools/check-revit-gate.py --list links` |
| **2** | **The dropped-counts** ([D-64](../DECISIONS.md)). Each declares a field in `provides` naming what it dropped — the rule is that ONE EXISTS, not that a particular word does. The marker rides **only on the empty answer**, capped | **59** | `python tools/check-revit-gate.py --list reporting` |
| **3** | **The preview selection** ([D-60](../DECISIONS.md)) in [`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs). Select `preview.Ids` and `preview.Skipped`, **500 per set** as a setting's default, save the modeller's own selection first and put it back — except on accept | one file | D-60 |
| **4** | **The workflow id across the seam** ([D-61](../DECISIONS.md), [D-62](../DECISIONS.md)). The add-in mints one per request; the brain never sees one | one seam | below |
| **5** | **The project knowledge scope.** `heron_context` now carries the pinned project's NAME, but `_Open` still always opens the GLOBAL store, so `knowledge scope: global` on every packet and project-specific knowledge can never take part. Changing it touches every brain tool, so it wants a real project store | one class | `_Open` in [`heron_brain.py`](../../mcp/server/heron_brain.py) |

**Item 4 is small and it unlocks three things at once**, which is why it is worth doing early rather
than last:

- the utterance cache **fills** — `remember()` already refuses anything but a completed run, and the
  only reason nothing calls it is that no run result reaches the brain
- [`measure-routes.py`](../../tools/measure-routes.py) gets its **LIVE** half instead of only the
  structural one
- [`brain/heron_audit.py`](../../brain/heron_audit.py) can finally claim `HERON-MCP-LOG-010`, whose row
  says *keyed by Workflow ID* and is the only reason its header still says `Heron-Agent: none`

### HOW THIS TRACK ENDED, AND WHAT A FRESH SESSION SHOULD DO FIRST

**PR #44 was merged on 2026-09-09** (`fd1df12`) after a Codex review. There is no open branch. **Start
from `main`.**

**The first five minutes of the next session, in order:**

1. `python tools/check-gaps.py` — it is computed from disk and this file is typed by hand. **Believe
   the tool.**
2. `python tools/check-docs.py` — it derives every count claimed in prose and fails on drift. It caught
   four stale counts during this track, one of them written in the same commit that broke it.
3. Read the five queued items below. **Not one of them is blocked on a decision** — every rule they need
   is written and already checked by a gate.

**What this track did NOT touch, so nothing here changes the proving work:** no fragment status moved,
`D-30` is untouched, no Golden Rule changed, `write.enabled` is as it was, and nothing built here has
been near Revit. The PROVING track entries below are unaffected and still supersede this one on method.

### AN AUTOMATED REVIEW FOUND NINE THINGS AND ALL NINE WERE REAL

PR #44 was reviewed by Codex on 2026-09-09. **Nine findings, nine confirmed against the code, all
fixed.** Worth reading as a class rather than a list, because seven of the nine are the same mistake:
**a thing declared and then not finished** — and every one of them passed all three gates and the whole
test suite.

| | What it was |
|---|---|
| **The worst one** | **`.claude/skills/heron-guard/bin/heron_guard.py` was not in the repository at all.** `.gitignore`'s `bin/` rule — there for .NET build output — swallowed it. The SKILL.md was committed, the executable it advertises was not, so on any fresh clone the hook would have failed before making a permission decision. **It passed here because the file was on disk.** `.gitignore` now re-includes `.claude/skills/*/bin/*.py`, and the directory has to be re-included before the file or git will not look inside it |
| **A crash nobody hit** | `measure-graph.py` returned a bare `[]` where the caller unpacks `ids, known`. It never fired in the recorded run — but `--revit 2019` empties the library at the version wall, so **the one setting most worth measuring was the one that raised `ValueError`** |
| **A measurement that would have lied** | The same tool's answer key was not filtered by `--revit`, so a release-specific re-run would score a fragment's correct absence as a miss. **The recorded Q-52 numbers are unaffected** — they were run with no `--revit` and therefore no wall — but a re-run would not have been |
| **A hole in a brand-new gate** | `check-licence.py` counted a source-less **skill** as clean, from a restriction left over from when `.claude/skills` was still scanned. All ten `brain/skills/*.yaml` declared no source, so an imported skill with no licence would have passed the checker whose whole promise is that unmarked and clean are different findings. The ten now declare `source: OFFICIAL` |
| **Two of four unwired** | `heron_audit.context()` and a catalogue recorder were written and never called, so `heron_context` and `heron_capabilities` left no trace. *"The brain side of the trail"* was half the brain |
| **Correct refusals counted as failures** | Every `ok: false` went out with **no error code**, and `heron_gaps.analyse()` files those under `(none)` in `unclassified`. That is `heron_gaps.py`'s own founding mistake repeated. Three codes now exist and classify as correct refusals |
| **A claimed metric that was a different number** | `measure-routes.py`'s LIVE section counted **cache rows** — inventory, not route frequency — while D-62 existed to make the live share readable. It reads the trail now |
| **The packet was quietly less true than it could be** | Every context packet said `project: none named` while the add-in had known the pinned document's name since the first `count_elements` |
| **The generator lost its imports exactly when it needed them** | `heron_context` refused the API surface along with the neighbour when nothing matched — but `_api_surface()` takes no arguments and reads the executor's import list. **The novel request, which most needs telling what it may assume, was the one told nothing** |

**One half is deliberately not fixed and is queued instead.** The same finding asked for the project
**knowledge scope** to be passed as well as the name. `_Open` always opens the GLOBAL store, so that
changes how *every* brain tool resolves knowledge rather than one — and it wants a real project store to
test against. **Item 5 on the PC list.**

**The lesson, and it is not "run a reviewer".** Every one of the nine passed `check-docs`,
`check-metadata`, `check-structure` and every test suite that runs in this container. **A gate
checks what somebody thought to check**, and seven of these were written by the same person who
wrote the gate an hour earlier.

### Two things that must be re-checked on the PC before item 1 is called done

**Nested links.** [D-59](../DECISIONS.md) does not say whether `linksSearched` counts a link inside a
link. It is a real Revit case and **the first implementation answers it against a real federated
model**, not from here. Write the answer back into D-59 when you have it.

**Item 2 moves house when it is finished.** The rule lives in
[`check-revit-gate.py`](../../tools/check-revit-gate.py) today because putting it in
[`heron_fragment.validate()`](../../brain/heron_fragment.py) now would make 59 fragments invalid and fail
every gate in the repository on a library that is not broken. **Moving it into `validate()` is how the
worklist is declared finished** — do that, do not just close the list.

### What is new in the tree, and what each is for

| | |
|---|---|
| [`tools/check-licence.py`](../../tools/check-licence.py) | **Exits 1 on a finding**, unlike the other reports. Reads the FILES, not the landing page ([D-66](../DECISIONS.md)). 370 units, all clean today |
| [`tests/test_licence_check.py`](../../tests/test_licence_check.py) | Proves that checker **fires** — a clean run on a clean library proves nothing |
| [`brain/heron_audit.py`](../../brain/heron_audit.py) | The brain's half of the trail ([D-62](../DECISIONS.md)). Writes `audit-brain-YYYYMM.jsonl` beside the add-in's file; `heron_gaps.read()` already merged by `at`, so **no C# and no reader changed**. Never writes the user's sentence |
| [`tests/test_carried_sources.py`](../../tests/test_carried_sources.py) | The `Q-51` tripwire. Fails the day Heron first carries text it did not write |
| [`tools/measure-graph.py`](../../tools/measure-graph.py) | The `Q-52` measurement — six settings, six losses |
| [`.claude/skills/heron-guard/`](../../.claude/skills/heron-guard/) | A PreToolUse hook. Deny-tier, fails closed, `HERON_GUARD=off` to disable |
| [`.claude/skills/heron-ship/`](../../.claude/skills/heron-ship/) | What to run before pushing, and which failures are the machine rather than the change |

### The three test failures are the machine, and they have TWO causes not one

`tests/test_mcp_serves.py` and `tests/test_served_claims.py` need the **MCP SDK**.
`tests/test_bridge_roundtrip.py` needs a **built .NET test host**. **37 of 40 pass here**, and on a
machine with both, all 40 should. Do not "fix" them by editing the tests.

Six checks also need tools this container has not got: `check-compile`, `check-fragments-compile` and
`check-api-surface` need `dotnet`; `check-routing` and `check-intrusion` need `HERON_KNOWLEDGE` set.

### One measurement worth carrying, because the slow version looked fine

`remember()` first called `FRAG.load_all()` — reading all 360 fragment files to use one — putting
**1,522 ms** on a path a modeller waits on. The store already knew the folder. It is **5.5 ms** now.
**Nothing about the slow version looked wrong**: `load_all()` is what `index()` calls, it was already
imported, and the cost only appears if somebody times it. Time the thing on the waiting path.

### Still needing one line from the owner

**§10.15 of [33](../33-external-repository-research.md), "Claude CEO"** — the repository was never
identified. A link, or drop the row. It is the only entry in that document that names nothing.

---
