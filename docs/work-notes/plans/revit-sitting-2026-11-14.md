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
