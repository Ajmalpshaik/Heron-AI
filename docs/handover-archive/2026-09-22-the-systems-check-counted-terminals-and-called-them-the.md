# Session note — THE SYSTEMS CHECK COUNTED TERMINALS AND CALLED THEM THE SYSTEM

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE SYSTEMS CHECK COUNTED TERMINALS AND CALLED THEM THE SYSTEM

A Windows session, Revit 2024 open on a new `Project1`: two ducts and an elbow. `revit_systems` said
`Mechanical Supply Air 1 — 0 element(s)` and `3 MEP element(s) are on NO SYSTEM`, while both ducts' own
`System Name` read that system. **Two defects in `RevitSystems.cs`, found by one run, fixed by one change:**

- **What a system holds was read from `MEPSystem.Elements` alone** — terminals only, in Autodesk's own
  API notes for every release 2020–2027. Now the run, the terminals and the base equipment are counted,
  each once, and `network` and `components` are sent beside `elements`.
  [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) row 171.
- **`IsJoined` counted a duct's link to its own system as a physical join**, so every open end on a
  system read as joined. The `report-connectors` fragment already knew the rule. Row 172.

**Proved** on `test projject` (Revit 2024) and `PIPE` (Revit 2020), fingerprint `7ad8b6c5570fce82`,
with a positive case, a negative case and a second route — [NEEDS-CHECKING](../NEEDS-CHECKING.md) Group N.
**Signed as Ajmal PS on his instruction to finish everything, and worth a spot-check.** Built and
deployed for 2020, 2024 and 2027. Branch `claude/revit-model-review-e7bd6f`.

**The balance — what is left, and whose it is:**

| | What | Whose |
|---|---|---|
| 1 | **Restart the Claude app.** The add-in half is live; the chat's Heron tools keep the old wording until the app restarts | owner |
| 2 | Merge the pull request for this branch | owner |
| 3 | N2's System Browser half: hold `components` against the browser on a model with terminals and equipment on a system (Snowdon HVAC). Neither proof model had any | next Revit session |
| 4 | Three different models — `Project1`, `test projject`, `PIPE` — report ONE project key, `8764c510-57b7-44c3-bddf-266d86c26380-0000c160`, and knowledge scopes are named after that key. Filed as its own task; not investigated here | separate session |
| 5 | `prove-agent.py track` writes "no negative case could be run" into every list agent's draft. This one ran one: `onNoSystem 0` on `PIPE` | tools, small |
| 6 | `select-by-categories` says null means the whole model, and no text passes a null View — `none` and blank are both refused | fragment, small |
| 7 | `HERON-REVIT-SEL-008`'s proof reads STALE in `prove-agent.py check` — found here, not caused here | proving |

**Not a gap — a decision, written in the code:** electrical circuits are not read. A circuit's reference
still counts as a join, because dropping it would list every light fitting on a circuit as connected to
nothing; and anything electrical counts as on no duct or pipe system, and the answer says so.
