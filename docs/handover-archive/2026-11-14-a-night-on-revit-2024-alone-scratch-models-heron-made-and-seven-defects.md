# Session note — A NIGHT ON REVIT 2024 ALONE: SCRATCH MODELS HERON MADE, TEN DRAFTS AND SEVEN DEFECTS

> **Session note** from 2026-11-14, written straight into this folder as its README asks. Nothing here is
> specification: where it disagrees with [DECISIONS.md](../DECISIONS.md), the
> [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those win**. A
> note records what was true on its own day.

---

### 2026-11-14 night — A NIGHT ON REVIT 2024 ALONE: SCRATCH MODELS HERON MADE, TEN DRAFTS AND SEVEN DEFECTS

Written to the rule in [section 11](../HANDOVER.md#11-handover--2026-09-12): what is NEW, a MISTAKE worth not repeating,
what is still TO DO. Branch `claude/pc-proofs-2024-2026-11-14`, cut from `main` at e6c7cb28 after PR #869 merged on
the owner's admin-bypass yes. Revit 2024 on the owner's laptop, session 3592, add-in 0.2.0.0, *Ask to Keep* off; the
owner asleep, his word: *test continuously, do not close Revit, keep anything needing a delete box or a click for the
morning, draw on fresh scratch models what a test needs*. The run book and the morning list are in
[the sitting plan](../work-notes/plans/revit-sitting-2026-11-14.md), last section.

**NEW.**

- **Part A of the run book ran alone.** [NX](../needs-checking/group-nx.md) NX4, NX6 (2024 half), NX7, NX8, NX9, NX10 PASS;
  [NW](../needs-checking/group-nw.md) NW2, NW7, NW8, NW9 PASS and NW1 FAIL as written; [NI](../needs-checking/group-ni.md) NI6 PASS.
- **Heron made its own scratch projects** from the 2024 metric templates (`CREATE_FAMILY_DOCUMENT`, Admin switch the
  owner's, already on) and drew what each test needed with PROVEN cards: walls and spaces in *electrical-mi*; columns,
  a beam, floors and walls in *Heron MG Analytical*; ducts with and without insulation in *Heron JX Ducts*. On them:
  [MI](../needs-checking/group-mi.md) MI4, [MG](../needs-checking/group-mg.md) MG2 and MG4's panel, [MM](../needs-checking/group-mm.md) MM4,
  [JX](../needs-checking/group-jx.md) JX2 and JX3, `delete-revision-numbering-sequence` ([BA](../needs-checking/group-ba.md)) PASS;
  [MF](../needs-checking/group-mf.md) MF2 recorded; [LI](../needs-checking/group-li.md) LI2 FAIL.
- **Ten drafts wait for the owner's "sign"**, each with the one gap *no second route*: `report-family-parameters`,
  `compare-elements`, `set-railing-type`, `reset-graphic-overrides`, `read-space-loads@2024`, `add-electrical-analytical-node`,
  `create-analytical-elements`, `report-energy-settings`, `remove-mep-insulation`, `delete-revision-numbering-sequence`.
- **Seven defects, each a file:** [1656](../fragment-issues/row-5b-1656.md), [1657](../fragment-issues/row-5b-1657.md),
  [1658](../fragment-issues/row-5b-1658.md), [1659](../fragment-issues/row-5b-1659.md), [1660](../fragment-issues/row-5b-1660.md),
  [1661](../fragment-issues/row-5b-1661.md), [1662](../fragment-issues/row-5b-1662.md). Three were settled by rolled-back
  scratch probes under a card's contract, Revit's own `RevitAPI.xml` notes beside them: Revit 2024 makes electrical
  load areas only inside Area Based Load Boundary lines (1659), a circuit naming scheme refuses
  `RBS_ELEC_CIRCUIT_NUMBER` and takes `RBS_ELEC_CIRCUIT_NAMING_INDEX` (1660), and `JoinGeometry` joins elements
  that do not touch (1661). From Revit 2023 a physical element gets no analytical element by itself, so Group MG's
  MG1 reading cannot hold on 2024.
- **Revit's Undo list can be read without pressing an item** - the drop-down opened by UI Automation, read, closed -
  and each entry shows twice in that tree (a ListItem and its Button). NW1 and NW2 used it.

**MISTAKES worth not repeating.**

- **One Claude session must keep to ONE door for a stretch.** The chat and the command line each claim Revit's
  five-minute lease, and a call the model hold refuses still takes it (5b-1662); every switch between them cost a
  wait. `batch-prove` itself locked its own next job out the same way until `HERON_CLIENT_ID=heron-batch-prove` was
  exported (5b-1656). Give every command-line step of a night ONE `HERON_CONVERSATION_ID`, too.
- **Revit's *Project Not Saved Recently* box blocks every call** - nothing in Heron's answer says why but *busy*;
  read the window list by process id. It was answered without saving, not by the session (a save prompt is the
  owner's). Keep a SAVED model in front during a long run: save the scratch with `SAVE_DOCUMENT` from another window.
- **Memory is the night's real limit.** Revit 2024's private bytes went from 6.3 GB to 14.1 GB in under two hours of
  new cards - each compile stays loaded - and the run stopped there with under 500 MB free rather than risk the
  *Virtual Memory - High Usage* box or a crash in a Revit the owner said not to close. Plan a night as a budget of
  NEW compiles, not of hours; repeated cards cost little.
- **`Get-Acl \\.\pipe\<name>` does not work on Windows PowerShell 5.1** - *"Cannot find path"*. A
  `NamedPipeClientStream` opened with `ReadData, ReadPermissions` and `GetAccessControl()` reads it.
- **Two phrasings found routed away on the owner's backend** (5b-1658): measure on all four backends before
  blaming a card - the column miss was the re-ranker's alone.

**TO DO.** The morning list in [the sitting plan](../work-notes/plans/revit-sitting-2026-11-14.md) (M1 to M9): the
signatures; two dialog figures by eye; a Revit restart; NW4-NW6; MI3 with boundary lines; Part B's boxes and clicks;
the `try-once` keeps MI5, MG5 and LI3 need; NI5; then Revit 2020's C1-C3. *Heron Reads Test* and *Heron Flag Test*
are to be closed WITHOUT saving. #867's rule still stands: no `revit_undo_last`, no dry run, no Groups NS to NV.
