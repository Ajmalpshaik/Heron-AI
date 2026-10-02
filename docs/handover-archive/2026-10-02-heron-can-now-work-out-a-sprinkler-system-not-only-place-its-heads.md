# Session note — HERON CAN NOW WORK OUT A SPRINKLER SYSTEM, NOT ONLY PLACE ITS HEADS

> **Session note** from 2026-10-02, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-02 — HERON CAN NOW WORK OUT A SPRINKLER SYSTEM, NOT ONLY PLACE ITS HEADS

Cloud session, no Revit, the same day as the HVAC engine and built on it. The owner's request, in their
own words: *"same like that we need to do the firefighting also ... sprinkler spacing minimum maximum
... number of sprinklers ... one sprinkler this much pipe size ... combined sprinkler above and below
ceiling ... where is the gap you can fill the gap."*

**NEW.**

- **A fire protection design engine**, [`brain/heron_fire.py`](../../brain/heron_fire.py), served as the
  READ-only `heron_fire` MCP tool and owned by `HERON-MEP-FPD-002`, the second row of *MEP Design
  Engineering*. Hazard classes looked up and never decided, sprinkler spacing and layout, a drawn layout
  checked as NFPA 13 measures S and L, the design area, pipe schedules including sprinklers above and
  below a ceiling, the beam and three-times rules, a Newton solver for a tree or a looped grid, the water
  supply, storage, pumps, standpipes, extinguishers, detectors, coverage.
  [`docs/42`](../42-fire-protection-design.md) is what it was built from and what it is not.
- **It shares the HVAC engine's answer machinery rather than copying it**: `run`, `catalogue`,
  `describe` and the reference helpers take an engine's own registry and tables, defaulting to HVAC's,
  and both engines go through one brain seam, `_design()`. The design basis keeps one file per project
  AND discipline - `<key>.fire.json` beside `<key>.hvac.json`.
- **A figure nobody could check is not held.** Every page fetch was refused by the network and the
  search budget ran out, so the tables hold only what a search extract, the owner's own firefighting
  file or the `fluids` package confirmed; the rest - equivalent lengths, C factors, temperature ratings,
  five pipe schedule cells - is asked for. Hazen-Williams is derived in the suite from its general form.
  [F43](../proposals/f43.md) asks the owner five things, two of them about his own file against NFPA.

**MISTAKE WORTH NOT REPEATING.**

- **An edit script truncated the uncommitted engine to nothing.** It opened the file for writing with a
  bad `newline=` argument; Python opens and truncates the file BEFORE it validates that argument, so
  the exception came after the damage. The engine was rebuilt from the session's own record. Since
  then every scripted edit writes a temporary file and moves it into place, and the work was committed
  locally at each step. Write through `os.replace`, and commit a restore point before a scripted edit.

**TO DO.**

- NEEDS-CHECKING [Group BU](../needs-checking/group-bu.md): the per-project fire record against Revit's
  own project key, the held figures against a copy of NFPA 13, and one system against a listed
  hydraulic program.
