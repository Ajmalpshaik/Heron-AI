# One Revit sitting - everything left that needs the owner's Revit (written 2026-11-14)

> **Active.** Written for the owner (Ajmal PS), who works in Revit himself and wants every Revit-needing row
> done in ONE sitting, segregated, so his Revit is free the rest of the time. **Closure:** every row below
> run and recorded in its group's file, or recorded NOT RUN with the reason; then this note is retired.
> Rows and their "Look for" live in each group's file - this note only orders them. Branch
> `claude/pc-proofs-2026-11-12` (PR #869).

## Before Revit (no Revit, about 5 minutes)

| # | What | Who |
|---|---|---|
| 0a | **DONE 2026-11-14** - PR #870 merged (0b4ea659), then `main` merged into this branch - the batches below test #870's repairs, and without the merge they would test the old code | the session (merge on the owner's word, given 2026-11-14) |
| 0b | Restart the Claude app, so this chat's Heron runs the code it has now (`revit_health` says the chat's code is old; until the restart Heron keeps no change asked in a chat - NP4, NJ, NR and the outcome rows need it) | owner |
| 0c | One Revit at a time (16 GB): Revit 2020 first, closed before Revit 2024 starts | owner |

## Part 1 - Revit 2020 (about 60 minutes; the owner's hands for about 10 of them)

Open `%TEMP%\heron-work\prove-1102\P1102 MEP.rvt`; the session opens `P1102 STR.rvt` beside it. Heron connected
from the ribbon - **the owner presses it, or says "you press it"; never both** (the add-in log is read in the
same step as any press).

| Order | Rows | What | Owner's hands |
|---|---|---|---|
| 1 | [NX1-NX5](../../needs-checking/group-nx.md) | `python tools/batch-prove.py tools/jobs/defect-repairs-nx-revit2020-2026-11-14.yaml --session <pid>`, then NX5 (b), NX6's 2020 half and NX4's demand-factor reading by hand | none |
| 2 | further DRAFT cards on P1102 MEP / STR | rolled-back proofs, one card at a time, as on 2026-11-13 | none |
| 3 | [NP1, NP2](../../needs-checking/group-np.md) | Ask to Keep ON, then off with *Stop asking* - `revit_health` read after each | **two clicks** in Heron > AI Bridge > Settings |
| 4 | NP4 | Ask to Keep ON: a level *NP On* asked in the chat - **press Discard**; Ask to Keep off: a level *NP Off*, kept | **two clicks + Discard** |
| 5 | NP5, NP6 | a second chat holding the model; the app's copy of `heron.config` read | none |
| 6 | NP7 | Ask to Keep ON: two walls deleted through the chat - **press Delete in Revit's Delete box**; the ducts moved 200 mm, preview then apply | **one switch, one Delete** |
| 7 | [NR1-NR12](../../needs-checking/group-nr.md) | the three new methods (annotate a view, system colours, ...) on P1102 MEP, asked in a chat, Ask to Keep ON | Keep or Discard as each row says |
| 8 | NM3 (optional) | two Revit 2020s open at once on two small saved models - **only if the owner allows it on 16 GB**; else NOT RUN | the yes |

Close Revit 2020 **without saving** the scratch copies.

## Part 2 - Revit 2024 (about 60 minutes; the owner's hands for most of NH)

First start of Revit 2024 after the new add-in is deployed (`tools\deploy-addin.ps1 -RevitVersion 2024`, the
session, with Revit closed) stops at *Security - Unsigned Add-In*: **the owner answers it**. Open Group NK's
scratch files (`%TEMP%\heron-work\nk`: *Heron Reads Test*, *Heron Flag Test*) and the Snowdon Towers scratch copy.

| Order | Rows | What | Owner's hands |
|---|---|---|---|
| 1 | [NX6-NX10](../../needs-checking/group-nx.md) | `python tools/batch-prove.py tools/jobs/defect-repairs-nx-revit2024-2026-11-14.yaml --session <pid>`, then NX4 on 2024, NX7's second half and NX8 by hand | none |
| 2 | [NI5, NI6](../../needs-checking/group-ni.md) | on *Snowdon HVAC - schematic proof* | none |
| 3 | [NK8 (a)](../../needs-checking/group-nk.md) | the doubled "Keywords" project parameter deleted - **Delete box** | **one Delete** |
| 4 | [DQ3d](../../needs-checking/group-dq.md) | one division KEPT through `try-once` | **Keep** |
| 5 | [NH0-NH11](../../needs-checking/group-nh.md) | `PICK_ON_SCREEN` - a new scratch project *Heron Pick Test* | **a click in Revit for every row** |
| 6 | [NJ1-NJ12](../../needs-checking/group-nj.md) | parameters out to a spreadsheet and back, asked in a chat, Ask to Keep ON (NJ12 off); NJ9 NOT RUN without a workshared copy and a second user | Keep or Discard; the switch for NJ12 |
| 7 | NK9 | the second routes, by eye | **your eye** |

## After Revit (no Revit)

Signatures for what the sitting drafted, on the owner's *"sign"*; decisions D-153 and D-154 read to him one at a
time; the owner's word on rows 5b-1635 and 5b-1641; PRs merged on his word.

## Update - 2026-11-14 evening: what is left, ready for ONE sitting

**Done since this plan was written** (each in its group's run record): Part 1's rows NX1-NX5, NX6's 2020 half,
NX4's dialog figure, NP1, NP2, NP4, NP5 (half - row 5b-1645), NP6, NP7, NP8 (NOT RUN, said why), and about
forty more DRAFT cards proved on Revit 2020 and signed on the owner's word. **Prepared without Revit:** the
add-in rebuilt from this branch (main's PR #868 brought the Activity pane, D-163, and the pipe hardening, D-162)
and **deployed to Revit 2024** - its next start asks *Security - Unsigned Add-In*, the owner's to answer; the
same build for Revit 2020 is in `revit\Heron.Revit.Addin\bin\x64\Debug\2020`, to deploy
(`tools\deploy-addin.ps1 -RevitVersion 2020`) once Revit 2020 is closed. `P1102 MEP NR.rvt`, a fresh copy of
the MEP sample, is in `%TEMP%\heron-work\prove-1102` for Group NR.

**The order that works, because of what each part needs:**

| Step | Revit | What | The owner's hands |
|---|---|---|---|
| 1 | 2024 (2020 closed) | Start Revit 2024; answer *Security - Unsigned Add-In* with **Always Load**; press Heron > AI Bridge > Heron, or say "you press it" | the security answer |
| 2 | 2024 | `defect-repairs-nx-revit2024-2026-11-14.yaml` - NX6, NX7, NX9, NX10 - then NX4 on 2024, NX7's second half and NX8 by hand; NI5, NI6 | none |
| 3 | 2024 | [Group NW](../../needs-checking/group-nw.md) NW1-NW16 - the new add-in: an empty keep, a late answer, stopping a job, the Activity pane, the pipe | a few clicks in the Activity pane, as each row says |
| 4 | 2024 | NK8 (a) one **Delete**; DQ3d one **Keep**; NK9 by eye | Delete, Keep, look |
| 5 | 2024 | Group NH - `PICK_ON_SCREEN`: a click in Revit for each row | a click each row |
| 6 | 2024 | Group NJ - parameters to a spreadsheet and back, Ask to Keep ON | Keep or Discard |
| 7 | 2020 (2024 closed) | deploy the 2020 add-in, start Revit 2020 on `P1102 MEP NR.rvt` | the security answer |
| 8 | 2020 | Group NR in **new chats**, Ask to Keep ON - after step 2 has signed `reset-graphic-overrides` (NR6 uses it); P1102 ARCH linked into the NR copy for NR7-NR11 | Discard at each box unless the row says Keep |

**Group NR, the sentences to type** - each in a NEW chat, Ask to Keep ON, on `P1102 MEP NR`:

| Row | Open first | Type in the new chat |
|---|---|---|
| NR1 | a duct plan at 1:100 | *tag everything in this view for issue* - then answer: Ducts and Air Terminals, the default tags, no leader, 2 mm overlap, 2 mm gap |
| NR4 | a duct plan with three systems | *give every system its own colour and put the same on the other drawings* - answer: System Classification, prefix `NR - `, Heron's colours, one other plan |
| NR7 | the plan of Level 1, ARCH linked, Room Bounding off | *set up the spaces and zones from the architect's model* |
| NR11 | after renaming, deleting and adding a room in ARCH and reloading the link | *the architect's rooms have changed - bring the spaces up to date* |
| NR12 | an untagged duct plan, Ask to Keep **off** | *tag everything in this view for issue* - ducts only, the default tag |

NR2, NR3, NR5, NR6, NR8, NR9 and NR10 are calls the session makes on the same plans, as the rows say.

**The owner's order, 2026-11-14 (it replaces the table's order above):** Revit 2020 FIRST - step 7, then step 8
for NR1-NR5 and NR7-NR12; then Revit 2024 - steps 1 to 6; then **NR6 last**, back on Revit 2020, once step 2 has
signed `reset-graphic-overrides`. Before step 7 the owner closes his own Revit 2020 work, so the 2020 add-in can
be deployed; the session links `P1102 ARCH.rvt` into `P1102 MEP NR.rvt` (`LINK_FILES`, now PROVEN) before NR7.

## Update - 2026-11-14 night: the run book for everything left, so the session can run it alone

**Done on Revit 2020 since the evening update** (each in its group's run record): Group NR's NR1 to NR5 and NR7 to
NR12 - NR7 to NR11 on two scratch hosts the session made, *NR Spaces Host* and *NR Spaces Host 2*, and NR11 on a copy
of the architect's file, `P1102 ARCH NR11.rvt`; rows 5b-1651 to 5b-1655. **Left on Revit 2020:** NR6 (after NX8 signs
`reset-graphic-overrides` on 2024), NR2 (c) (a project with flex ducts and no flex duct tag), NM3 (two Revit 2020s
at once - NOT RUN on 16 GB unless the owner allows it).

**The owner's word, 2026-11-14:** prepare everything left so that, when he says go, the session runs it on its own.
Every model below is a SAVED SCRATCH COPY under `%TEMP%\heron-work`; each is closed WITHOUT saving at the end, so a
change kept during a run needs no undo. The rules that stand: no `revit_undo_last`, no dry run, no Groups NS-NV until
the owner says #867 is merged; a delete only in Revit's own Delete box; nothing saved, synced or exported unasked
beyond the scratch files named here; signatures only on his "sign".

### The owner's hands - once at the start of Revit 2024 (about 3 minutes)

| # | Step |
|---|---|
| H1 | Close Revit 2020 - **No** to saving *NR Spaces Host 2* |
| H2 | Start Revit 2024; *Security - Unsigned Add-In*: **Always Load** (the add-in from this branch is deployed) |
| H3 | Open `%TEMP%\heron-work\nk\Heron Reads Test.rvt` (Heron opens the rest itself with `ACTIVATE_DOCUMENT`) |
| H4 | Heron > Settings: **Ask to Keep OFF** for the alone part (A1-A8); press Heron > AI Bridge > Heron, or say "you press it" |
| H5 | One answer: may the session press Revit's Keep / Discard boxes and click in Revit itself (the computer-use tool, one approval for Revit)? **Yes** moves the B rows into the alone part; **no** keeps them for the hands block |

### A - the session alone, Revit 2024, Ask to Keep off (no box opens; nothing needs undoing)

| # | Rows | How |
|---|---|---|
| A1 | open *Heron Flag Test.rfa* and `nk\Snowdon Towers Sample Architectural.rvt` | `ACTIVATE_DOCUMENT`, then back to *Heron Reads Test* |
| A2 | [NX6, NX7 first half, NX9, NX10](../../needs-checking/group-nx.md) | `python tools/batch-prove.py tools/jobs/defect-repairs-nx-revit2024-2026-11-14.yaml --session <pid>` - every run rolled back |
| A3 | NX4 on 2024 | `fragment report-electrical-settings --in "Heron Reads Test"`, read only |
| A4 | NX7 second half | `SET_FAMILY_TYPE_VALUES` Count = 0 in *Heron Flag Test* (kept - scratch), then `report-family-parameters` again |
| A5 | NX8 | four walls given ONE override each with `OVERRIDE_GRAPHICS_IN_VIEW` (kept), `READ_GRAPHIC_OVERRIDES`, `reset-graphic-overrides` on each through `validate` (rolled back) - its D-30 pair - and the negative wall |
| A6 | [NI5, NI6](../../needs-checking/group-ni.md) | open `draw-mep-schematic\Snowdon HVAC - schematic proof.rvt`; the two CLI commands as the rows give them - rolled back, no box |
| A7 | [NW1-NW3, NW7-NW9](../../needs-checking/group-nw.md) | the empty rename and the two-view rename asked as a chat would (`revit_change`), the add-in log read; `Get-Acl` on the pipe and the bridge's `.json`. NW10 NOT RUN - no second PC |
| A8 | NW4-NW6 | only if `revit.operationTimeoutSeconds` is read per call (the add-in log says); otherwise moved to the hands block - the setting is put back to 60 afterwards |
| A9 | sign? | the drafts A2-A6 leave are listed for the owner's "sign" - nothing is signed without it |

### B - Keep, Discard, Delete or a click in Revit: the computer-use tool on his H5 yes, else his hands

| # | Rows | What the box or click is |
|---|---|---|
| B1 | [NJ1-NJ12](../../needs-checking/group-nj.md) on `nk\Snowdon Towers Sample Architectural.rvt` | Ask to Keep ON; Discard at each box unless the row keeps; NJ7 opens the CSV in Notepad; NJ12 with Ask to Keep off. NJ9 NOT RUN (no workshared central, no second user) |
| B2 | [NW11-NW16](../../needs-checking/group-nw.md) | the Activity pane's Select, Zoom and Undo buttons |
| B3 | [DQ3d](../../needs-checking/group-dq.md) | one Keep on `dqdr\Heron Repairs Test.rvt`, Parts shown |
| B4 | [NH0-NH11](../../needs-checking/group-nh.md) | a new *Heron Pick Test* (Heron makes it with `CREATE_FAMILY_DOCUMENT` from the 2024 metric architectural template and draws the 5000 mm model line) - then a click in the view for every row, Esc for NH2, the slow clicks of NH8 and NH9 |
| B5 | [NK8 (a)](../../needs-checking/group-nk.md) | the doubled *Keywords* parameter deleted - **Revit's Delete box is his alone** unless he says the session may press it, for that one delete |
| B6 | NK9 | **his eye**: the seven second routes the row lists |

### Revit 2020 at the end (Revit 2024 closed first; the 2020 add-in is deployed)

| # | Rows | How |
|---|---|---|
| C1 | NR6 | after A5 has signed `reset-graphic-overrides`: `cp "P1102 MEP.rvt" "P1102 MEP NR6.rvt"` under `prove-1102`, opened by Heron; a plan showing insulated ducts; ONE duct overridden first (`OVERRIDE_GRAPHICS_IN_VIEW`, a card for the row's "by hand" - said in the record); `COLOR_BY_PARAMETER`, then the two resets, each as the row gives it |
| C2 | NR2 (c) | a new project from `US Metric\DefaultMetric.rte` (Heron makes and saves it, `prove-1102\NR2c Host.rvt`); flex ducts copied in from *P1102 MEP NR6* (`COPY_ELEMENTS_BETWEEN_DOCUMENTS`); the filter, then `TAG_ELEMENTS_IN_VIEW` - `notTagged` and `notTaggedWhy` if no flex duct tag is loaded; if the template has one, NOT RUN again and why |
| C3 | close | every scratch project closed without saving - his clicks (Heron cannot close without saving) |

**After the runs:** the signatures, on his "sign"; D-153 and D-154 read to him one at a time; his word on rows 5b-1635
and 5b-1641; PR #869 merged on his word. This update is retired when A to C are recorded.

## Update - 2026-11-14 night: what the session did alone on Revit 2024, and the morning list

Revit 2024, session 3592, add-in 0.2.0.0 from `main` e6c7cb28, *Ask to Keep* off; the owner asleep from about 22:15.
Branch `claude/pc-proofs-2024-2026-11-14`. Every result is in its group's run record and every defect in a file of
its own; this section only points at them.

**Done alone.** Part A of the run book: [NX](../../needs-checking/group-nx.md) NX4 (on a Heron-made electrical
project), NX6's 2024 half, NX7, NX8, NX9, NX10 - all PASS; [NW](../../needs-checking/group-nw.md) NW2, NW7, NW8, NW9
PASS, NW1 FAIL as written; [NI](../../needs-checking/group-ni.md) NI6 PASS. Then on fresh scratch projects Heron made
itself (`CREATE_FAMILY_DOCUMENT` from the 2024 metric templates, under `%TEMP%\heron-work`):
[MI](../../needs-checking/group-mi.md) MI4 PASS, [LI](../../needs-checking/group-li.md),
[MG](../../needs-checking/group-mg.md) MG2 and MG4's panel PASS, [MF](../../needs-checking/group-mf.md) MF2 recorded,
[MM](../../needs-checking/group-mm.md) MM4 PASS, [JX](../../needs-checking/group-jx.md) JX2 and JX3 PASS, and
`delete-revision-numbering-sequence` in [BA](../../needs-checking/group-ba.md) PASS.

**New defects, each its own file:** [5b-1656](../../fragment-issues/row-5b-1656.md) (batch-prove's hand-back locks
its own next job out - export `HERON_CLIENT_ID=heron-batch-prove` until it is repaired),
[5b-1657](../../fragment-issues/row-5b-1657.md) (an empty keep reads KEPT), [5b-1658](../../fragment-issues/row-5b-1658.md)
(columns and floors routed away on the re-ranker), [5b-1659](../../fragment-issues/row-5b-1659.md) (load areas need
Area Based Load Boundary lines), [5b-1660](../../fragment-issues/row-5b-1660.md) (the naming card's *circuit
number*), [5b-1661](../../fragment-issues/row-5b-1661.md) (`join-geometry` joins what does not touch),
[5b-1662](../../fragment-issues/row-5b-1662.md) (a refused call still takes the lease).

**Why the night stopped early.** Revit's private memory grew from 6.3 GB to 14.1 GB - every new card compiled stays
loaded until Revit restarts - with under 500 MB of the PC's memory free; Revit 2020 put up its *Virtual Memory -
High Usage* box near 16 GB, and the owner's word was not to close Revit. Once, Revit's own *Project Not Saved
Recently* box blocked every call for about ten minutes; it was answered without saving (the file's time did not move),
not by the session. A saved model is left in front to keep it away.

**Open in Revit 2024 now** (none of it the owner's work): *Heron JX Ducts* (saved, in front), *Heron Reads Test*
(unsaved: four overrides in Level 1, floor plans renamed *Floor 1* and *Floor 2* - **close without saving**, the job
files name *Level 1*), *Heron Flag Test* (unsaved: Count 0 - close without saving), *Project1* (the blank project
Revit opened with). Closed and saved: *electrical-mi*, *Heron MG Analytical*, *Snowdon Towers Sample Architectural*.

### The morning list - the owner's hands, one at a time

| # | Step |
|---|---|
| M1 | Read the drafts, then say "sign" or not: `report-family-parameters`, `compare-elements`, `set-railing-type`, `reset-graphic-overrides`, `read-space-loads@2024` (`accept --release 2024`), `add-electrical-analytical-node`, `create-analytical-elements`, `report-energy-settings`, `remove-mep-insulation`, `delete-revision-numbering-sequence` - each with one gap, *no second route* |
| M2 | Two figures by eye: Analyze > Energy Settings in *Heron MG Analytical* - is Target Percentage Glazing 0 %? (MM4); Manage > Structural Settings there - cutbacks 2.5 / 2.5 / 1.5 mm? (MF2) |
| M3 | Restart Revit 2024 when convenient (memory), with the security box his |
| M4 | NW4-NW6: `revit.operationTimeoutSeconds` 10, restart Revit, then the rows; put it back to 60 |
| M5 | MI3: in *electrical-mi*, Analyze > Area Based Load Boundary round each room; then MI3 again |
| M6 | The B part of this run book (NJ, NW11-NW16, DQ3d, NH, NK8 (a), NK9) - Keep, Discard, clicks; the H5 question still unanswered |
| M7 | MI5, MG4's link, MG5, LI3 - each needs a DRAFT card's change KEPT, which is his `try-once` Keep |
| M8 | NI5 on a Revit with only *Snowdon HVAC - schematic proof* open; then Revit 2020's C1-C3 |
| M9 | Decisions: whether an un-witnessed empty keep should read as nothing changed (5b-1657, D-160) |

**M1 done 2026-11-15:** all ten signed on his "Yes, sign all ten" (commit 6c3c49d3) - nine cards PROVEN, `read-space-loads` accepted on 2024.

**M9 and the questions after the runs, answered 2026-11-15** - each put to him alone, in plain words: [D-153](../../decisions/D-153.md) and [D-154](../../decisions/D-154.md) ACCEPTED (*"Yes, accept it"* each); [row 5b-1657](../../fragment-issues/row-5b-1657.md) - an empty keep reads as nothing changed; [row 5b-1635](../../fragment-issues/row-5b-1635.md) - a card's own refusal reads REFUSED; [row 5b-1641](../../fragment-issues/row-5b-1641.md) - wording B, *"revit_change with previewOnly=true"*. The three rows stay OPEN until their repairs are made. **His hold of 2026-11-12 is lifted** - *"Yes, lift the hold"*, told that #867 merged on 2026-11-12: `revit_undo_last`, `revit_change` with `dry_run`, and Groups NS, NT, NU and NV may be run again, on scratch models and under every other rule of the sitting.
