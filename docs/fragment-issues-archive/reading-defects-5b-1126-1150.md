# FRAGMENT-ISSUES archive — section 5b, rows 1126 to 1150

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1126 to 1150 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1126

*Moved from the register on 2026-11-17.*

**Defect.** **[`MOVE_ELEMENTS`](../../brain/fragments/move-elements/fragment.yaml) MOVED A REVIT LINK INSTANCE 5000 MM AND ANSWERED `moved 0`, `unverified 1` - IT READS A MOVE BACK FROM THE ELEMENT'S `Location`, AND A LINK INSTANCE'S PLACE IS ITS TRANSFORM. THE CARD IS PROVEN.** Measured 2026-10-18, Revit 2020 session 30308, scratch Project1: `SELECT_BY_CATEGORY_NAME` *RVT Links* in a 3D view handed in instance 345450 (`HeronLinkSource.rvt : 2`, linked by hand), `offset=5000,0,0`, kept -> `moved 0, attempted 1, unverified 1 [345450]`, no refusal. A scratch read then found the instance's `GetTotalTransform().Origin` at 5000, 0 mm - it had moved. A rolled-back run answered the same. A modeller told *moved 0* moves it again and ends 10 m out.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Read a `RevitLinkInstance` (and an `ImportInstance`) back by its transform origin before and after, and count it.

---

### Row 5b-1130

*Moved from the register on 2026-11-17.*

**Defect.** **THE HVAC AND FIRE DESIGN CONTRACTS DECLARE THREE REFUSALS THE CODE NEVER PRODUCES BY THOSE NAMES: `HERON-MEP-HVD-001` AND `HERON-MEP-FPD-002` LIST `NEEDS_REQUEST_VALUES`, `UNUSABLE_REQUEST_VALUES` AND `UNKNOWN_CALCULATION`, AND `heron_hvac.run` / `heron_fire.run` ANSWER WITH A `status` OF `missing`, `refused` OR `unknown`.** Found 2026-10-18: `python tools/generate-contract-reference.py` prints six `UNREACHABLE` lines for the two agents (`brain/agents/HERON-MEP-HVD-001.yaml` and `brain/agents/HERON-MEP-FPD-002.yaml`, each under `failures:`). Row 95 is the other direction (refusals produced and never declared); this is a caller written to the contract waiting for three words it is never given, while the code's own three - which read as the same three cases - come back undeclared.

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Either the contracts take the code's words or the code reports the contract's; row 95's open question - what a contract's `version` means when it was written wrong from birth - decides which costs a MAJOR version. The owner's call, with row 95.

---

### Row 5b-1132

*Moved from the register on 2026-11-17.*

**Defect.** **THE "WHICH REVIT?" LIST SHOWS EACH REVIT'S PROCESS NUMBER AND NO PROJECT NAME, AGAINST `mcp/README.md` RULE 4 - *"THE USER NEVER SEES A PROCESS NUMBER. VERSION, PROJECT, AVAILABILITY"* - AND [docs/25](../25-multi-session-and-binding.md) SECTION 2a.** Found 2026-10-18 by the docs review of docs/25. `SessionBinding.describe` in `mcp/server/heron_session.py` writes each line as `"  %d) Revit %s   (session %s)  %s%s"` with the process id as the session, so two Revit 2024s read `1) Revit 2024 (session 63712) FREE` and `2) Revit 2024 (session 24688) FREE`, and the modeller has to know which process holds which project to choose. The choice is by list number, as the rule wants; the project is what is missing.

**State.** **MOVED TO ISSUE [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** docs/25 section 2a puts the list's changing facts at ask-time (`info` is lease-exempt, as `describe` says); the project name is one of them, and the process number leaves the line. Whether a short session number stays as a tie-breaker is the owner's call.
