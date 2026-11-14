# Session note — THE OWNER'S PC PROVES WHAT #855 BUILT, AND THE PROVING FINDS NEW DEFECTS ON THE WAY

> **Session note** from 2026-11-13, written straight into this folder as its README asks. Nothing here is
> specification: where it disagrees with [DECISIONS.md](../DECISIONS.md), the
> [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those win**. A
> note records what was true on its own day.

---

### 2026-11-13 — THE OWNER'S PC PROVES WHAT #855 BUILT, AND THE PROVING FINDS NEW DEFECTS ON THE WAY

Written to the rule in [section 11](../HANDOVER.md#11-handover--2026-09-12): what is NEW, a MISTAKE worth not repeating,
what is still TO DO. It covers branch `claude/pc-proofs-2026-11-12` (draft PR #869), cut from `main` at
10334996, on the owner's laptop: Revit 2024 in the morning, Revit 2020 from the afternoon - **one Revit at a time,
the owner's rule for a 16 GB PC**. Every run was on a saved scratch copy under `%TEMP%\heron-work`; each group's
own file carries its dated run record, and each defect is a file of its own under
[`fragment-issues/`](../fragment-issues/). Counts here are the day's; derive today's with the commands in
[HANDOVER](../HANDOVER.md).

**NEW.**

- **The handover's ordered list was worked through as far as one PC and one Revit allow.** [NK](../needs-checking/group-nk.md)
  on Revit 2024; [NL](../needs-checking/group-nl.md) on 2024 (NL5-NL7) and 2020 (NL0-NL4, NL9-NL12);
  [DQ](../needs-checking/group-dq.md) and [DR](../needs-checking/group-dr.md) on 2024; [NP](../needs-checking/group-np.md)
  NP3 on 2020 in two halves, old add-in then new; [NM](../needs-checking/group-nm.md) NM1, NM2, NM4 (SEL-008), NM6;
  [NI](../needs-checking/group-ni.md) NI1-NI4; [NG](../needs-checking/group-ng.md) NG5-NG7, the project card, on files made in
  2020 for it. Each group's run record says PASS / FAIL / NOT RUN / NEEDS THE OWNER row by row.
- **Signed by Ajmal PS on his word** (*"You can sign"*), each with `accept` and the status flipped by hand:
  `ADD_PROJECT_PARAMETER` (morning), then one commit (ccb8b063) with the nine NK reads, `set-routing-preference`,
  `create-assembly`, `divide-parts`, `copy-elements-between-documents`, `set-parameter-values-by-id` version 8,
  and the cards proved one by one on Revit 2020 in the afternoon on *P1102 MEP*, *P1102 STR* and *Heron Many
  Types Project 2020* - every one rolled back, a value- or selection-driven negative for each. Agent
  `HERON-REVIT-SEL-008` re-signed; `VIE-013` stays STALE until NM3. **Left for the owner:** `report-bounding-box`
  and `compare-elements`, whose rows passed with findings (5b-352 open, 5b-1631).
- **Item 9, `tests/test_routing_phrases.py`, on this PC's backend (model + re-ranker):** seven failures - rows
  5b-1513 and 5b-1514 **stand**. The branch was `claude/pc-proofs-2026-11-12`, i.e. `main` at 10334996 plus this
  sitting's records and no code.
- **Item 10, the `outcome:` line in a real chat** (Revit 2020, Ask to Keep off): a kept `CREATE_LEVELS` read
  `outcome: KEPT`; a DRAFT card sent through `revit_change` read `outcome: REFUSED`; **a card's own refusal - a
  level name already taken, nothing made - read `outcome: KEPT`** ([row 5b-1635](../fragment-issues/row-5b-1635.md),
  a question for the owner).
- **Defects found, each recorded and not repaired here:** [1627](../fragment-issues/row-5b-1627.md),
  [1628](../fragment-issues/row-5b-1628.md), [1629](../fragment-issues/row-5b-1629.md), [1630](../fragment-issues/row-5b-1630.md),
  [1631](../fragment-issues/row-5b-1631.md), [1632](../fragment-issues/row-5b-1632.md), [1633](../fragment-issues/row-5b-1633.md),
  [1634](../fragment-issues/row-5b-1634.md), [1635](../fragment-issues/row-5b-1635.md), [1636](../fragment-issues/row-5b-1636.md)
  (two electrical cards take every voltage 10.76 times too small on Revit 2020), [1637](../fragment-issues/row-5b-1637.md),
  [1638](../fragment-issues/row-5b-1638.md), [1639](../fragment-issues/row-5b-1639.md). The owner then said whatever can be done
  without him and without Revit is to be done: a separate session on its own branch, cut from `main`, is
  repairing the card defects among them (not 1627, 1632, 1634, 1635 - the owner's to decide), each to DRAFT, and
  writes the group of PC runs that proves the repairs.
- **The new add-in is deployed to Revit 2020 only** (built from 10334996, `Heron.Revit.Addin.dll` 2026-11-13 18:48).
  2024, 2026 and 2027 were built and not deployed: each first start asks the owner the *Security - Unsigned
  Add-In* question, which is his to answer.

**MISTAKES worth not repeating.**

- **A long proving run on one Revit 2020 session ends in Revit's own *"Virtual Memory - High Usage"* box** - this
  day after about two hours, Revit's private bytes at the PC's 16 GB. Until it is closed, every Heron call
  answers `revit_busy`; nothing says why but the box. Restart Revit before that, as the proving skill's sixty-job
  rule says, and read the window list (UI Automation, by process id) when a busy answer has no reason.
- **A chat stays pinned to the first model it used** (Article 12b), so `revit_change` in this chat put the
  *Outcome Test* level into *P1102 MEP* while another project was in front. Right by the rule; name the model
  before a kept change in a chat that has read more than one.
- **`fragment` does not check a card's `revit:` list** ([row 5b-1403](../fragment-issues/section-5b-rows-401-425.md)
  says so): `set-circuit-naming-scheme`, 2021 and later, was sent to Revit 2020 and spent a compile to fail. Read
  the card's `revit:` line before a single run.
- **A bare `|` inside a 5b row's cell turns CI red** in `tests/test_open_defects.py` (row 1634 quoted NG's
  `key=Type Name|in=...`); write it `\|`, and run that suite before pushing a new row - only the full sweep runs it.
- **NG6 and NG7 write `Comments` into types,** which have `Type Comments` ([row 5b-1634](../fragment-issues/row-5b-1634.md)) - a
  row's parameter can be the thing that is wrong, and is recorded, not changed to pass.
- **A `role: result` field that holds the input or a model-wide count** makes a negative impossible
  ([5b-1638](../fragment-issues/row-5b-1638.md), [5b-1639](../fragment-issues/row-5b-1639.md),
  `report-browser-organization`): the runner's *"the negative case returned content"* gap is the finding, not
  a bad arrangement.

**TO DO.**

- **The owner's hands:** NP1, NP2, NP4-NP8 (the Ask to Keep switch, Keep or Discard, a delete); NJ, NR (Ask to
  Keep ON); NH (clicks); DQ3d (one division kept); NK8(a) (a delete); NK9 (by eye); NL4a's kept half.
- **Two Revits at once, or a second release:** NM3 (two Revit 2020 sessions), then NM5; NI4's newer half,
  NI5 and NI6 (Revit 2024); NP3 both at once. Not NI7 before row 5b-1510's repair.
- **Not signable until repaired:** `read-space-loads`, `report-family-parameters`, `set-railing-type`,
  `report-electrical-settings`, `add-electrical-distribution`, `create-load-case`, `set-floor-span-direction`,
  `report-structural-loads`, `report-browser-organization`. `report-framing-justification`,
  `report-floor-span-direction` and `report-rebar-numbering` were drafted after ccb8b063 and signed later the same
  evening on the owner's *"whatever you need to do you can do it"*.
- **Decisions D-153 to D-159**: read to the owner one at a time, his yes or no recorded - not yet done.
- **Scratch state on the PC:** *P1102 MEP* holds an unsaved level *Outcome Test* and a loaded
  *M_Multi Category Tag*; close it without saving. The 2020 scratch families and project for NG are in
  `%TEMP%\heron-work\many-types-2020`. Nothing was deleted, as the owner asked.
- **#867's rule still stands until the owner says otherwise:** no `revit_undo_last`, no `revit_change` dry run,
  no Groups NS to NV.

**Continued the next morning, 2026-11-14** (Revit 2020 restarted, session 17824, *P1102 STR*, every run rolled
back): `place-structural-loads`, `place-boundary-conditions`, `create-opening`, `attach-columns`, `create-truss`,
`edit-rebar-numbering` and `place-rebar-couplers` proved and signed on the owner's *"you can sign"*.
`place-column-rebar` is not: hooked ties are refused by Revit ([row 5b-1644](../fragment-issues/row-5b-1644.md)).
`main` (#868) was merged into the branch; the balance-of-work file was regenerated to resolve it.
