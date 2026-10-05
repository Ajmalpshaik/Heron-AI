# Session note — A SPRINKLER SYSTEM'S HYDRAULICS NOW COME FROM THE MODEL

> **Session note** from 2026-10-04, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-04 — A SPRINKLER SYSTEM'S HYDRAULICS NOW COME FROM THE MODEL

Cloud session, no Revit. The owner asked for firefighting what the Loads panel did for HVAC, sprinklers
first and plumbing later: *"write the sprinkler panel design ... make the good plan and read that plan
check that plan update that plan and finalize that plan and do it"*.

**What was done, in that order.**

1. **The design**, [docs/46](../46-sprinkler-hydraulics-from-the-model.md). The fire engine already
   solved a network ([42](../42-fire-protection-design.md)); what was missing was reading the network
   from Revit and a place to see it - so this is a reader and a panel, no new physics.
2. **The plan**, [`work-notes/plans/sprinkler-panel-2026-10-04.md`](../work-notes/plans/sprinkler-panel-2026-10-04.md).
3. **An independent review of the plan against the code** found 20 defects, every one with evidence.
   The ones that mattered most: the plan read Revit's *calculated* coefficient instead of the K-factor
   (`AssignedKCoefficient`); it walked only the pipe network, which holds no sprinklers; it had no answer
   for taps joining a pipe part-way; it would have refused most real floors at the solver's 400-point
   limit; it lost every fitting's centre-to-end length, which errs small; and it forgot the tool
   registry row, without which the chat labels a new tool destructive. All taken; the plan's review log
   says what each changed. **The plan was finalized only after that.**
4. **The build**: REPORT_SPRINKLER_NETWORK (DRAFT, compiled on 2020, 2024 and 2027), four brain modules,
   `heron_fire.hydraulic` now also giving its numbers as data, the `revit_sprinkler_hydraulics` tool,
   and the Companion's Sprinkler panel with its own 3D view. **It writes nothing to Revit.**

**Worth knowing next time.**

- **Leaving the dry branches out of the solve is exact,** not an approximation - a dead end with no open
  head carries no water. `tests/test_sprinkler_run.py` holds it to the unpruned answer.
- **The fittings chart holds very few cells,** so a first run asks for most fittings' equivalent length.
  That is D-33 working, and the page says it is expected.
- **The page was looked at** in headless Chromium on the test network, asking and solved, with no
  script error. It has not been run against a Revit model: [group CE](../needs-checking/group-ce.md).
