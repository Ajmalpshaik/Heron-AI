# Session note — THE SAFETY RAILS MET REVIT, AND HERON'S DATA LEFT THE STORE APP'S COPY

> **Archived session note** from 2026-10-18. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-10-18 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-10-18 — THE SAFETY RAILS MET REVIT, AND HERON'S DATA LEFT THE STORE APP'S COPY

Not one session's note: where `main` stood at the end of 2026-10-18, written by the documentation
refresh so the next session starts from today rather than from 2026-10-14, the last sitting before it.
The full list is `git log --first-parent --format='%cs %s' origin/main --since=2026-10-15`; every line
below names the register row or decision that holds its evidence. Everything run in Revit was run on the
owner's laptop, on scratch copies, with the owner pressing every box himself.

**Run in Revit:**

- **The move, measured.** `D1` to `D6` passed in Revit 2024 on 2026-10-17: +200.0000 mm on three ducts,
  read two ways, a pinned duct beside them unmoved, one undo entry
  ([Group D](../needs-checking/group-d.md)). Not run: any other release, a duct on a fitting, a failure
  forced mid-move (`G-12`).
- **The safety groups, in Revit 2024 and 2027 on 2026-10-18** (#650): the Keep or Discard box
  ([D-126](../DECISIONS.md)), a DRAFT's one run ([D-119](../DECISIONS.md)), the run guard, Revit's dialogs, the
  rollback check, the run clock and `stop`, pictures of a view, the show door. Each of Groups ET, EM, EX,
  EL, EO, EZ, ER, EK, ES, EV, EJ and CP carries a dated section with PASS, FAIL and NOT RUN kept apart;
  **six faults became rows 5b-1090 to 5b-1095** ([section 5b](../fragment-issues/section-5b-rows-401-425.md)).
  Nothing of it ran on Revit 2020.
- **Heron loads on Revit 2020, 2024, 2026 and 2027** ([Group EQ](../needs-checking/group-eq.md),
  2026-10-17). On 2026 no fragment could run ([row 5b-575](../fragment-issues/section-5b-rows-401-425.md));
  the repair is in code and not yet run in Revit ([Group GW](../needs-checking/group-gw.md)).
- **Signed and `PROVEN` by Ajmal PS:** `create-text-note`, kept once behind the Keep box on Revit 2024
  ([Group EM](../needs-checking/group-em.md), EM1 and EM3, #656); `EDIT_TEXT_VALUES` v5 and
  `READ_ELEMENT_PARAMETERS` v4 on Revit 2020 ([Group FD](../needs-checking/group-fd.md), #640).
- **The installer's Python half** ran on the laptop on 2026-10-17 and 2026-10-18 from a local build:
  READY, and Python 3.10 and 3.15 refused by name ([Group EP](../needs-checking/group-ep.md)).
- **The data carry of [D-135](../DECISIONS.md) passed; the Companion button failed**, because Revit runs
  with a temporary folder of its own ([Group HF](../needs-checking/group-hf.md),
  [row 5b-1100](../fragment-issues/section-5b-rows-401-425.md), #654).

**Decided:** [D-135](../DECISIONS.md) — the Python side keeps its data in `%USERPROFILE%\.heron`, out of a
Store Claude's private copy; [D-136](../DECISIONS.md) — the Companion's folder is found from inside Revit by
stepping out of a GUID-named one; [D-138](../DECISIONS.md) — an **Ask to Keep** switch on the ribbon,
**off by default**, and one Keep per job, superseding [D-126](../DECISIONS.md) item 5 in part.

**Built and compiled for 2020 to 2027, or tested in a cloud container — NOT run in Revit:** the Ask to
Keep switch (#660); D-136's folder rule (#657); the Keep box zooming to the change (ET14, #648); and
changes for five of the six faults — 5b-1090 (#655), 5b-1091 and 5b-1092 (#658), 5b-1093 (#653),
5b-1094 (#651). 5b-1095, answers lost under twenty connections at once, is recorded and not fixed.

**The office PC is retired** (#645, #647): the laptop is the owner's only PC, and its Claude is the
Microsoft Store build, so EP6 and HF5 are NOT RUN: each needs a Claude that is not the Store build, and
HF5 more than that — [Group HF](../needs-checking/group-hf.md) says what.

**Next, on the laptop with Revit open:** run each change above, starting with HF4 again (the Companion
button) and the Ask to Keep switch; then `G-12`, and [A16b](../needs-checking/group-a.md) — the rollback that
reported success and kept a rename — which nothing since has re-run. Everything else waiting on the owner
is `python tools/owner-queue.py`.
