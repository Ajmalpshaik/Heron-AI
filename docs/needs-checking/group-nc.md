# Needs checking — Group NC

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NC - a 2D family's lines and filled region edges locked to named planes, along and by their ends: `LOCK_FAMILY_LINES_TO_PLANES` (2026-11-11)

Asked 2026-11-11 for the owner's KDF square diffuser (Revit 2020): its plan flow arrows are a nested detail item
fixed at 600 mm, where Autodesk's sample supply diffuser carries arrows that grow with its size. Nothing in Heron
locked a line's END to a plane crossing it, or a filled region's edges at all, so a slanted arrow-head edge could
not follow a Length. Made NEW, at DRAFT - `LOCK_FORM_SKETCH_TO_PLANES` is PROVEN and untouched; why, and what is
not known, is [FRAGMENT-ISSUES row 5b-1493](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `LOCK_FAMILY_LINES_TO_PLANES` (MODIFY) | [`brain/fragments/lock-family-lines-to-planes`](../../brain/fragments/lock-family-lines-to-planes/fragment.yaml) | For detail lines, symbolic lines and each filled region's sketch, and each plane named: a straight line lying on it locked ALONG it; a line's END on a plane crossing it locked to it; a region's corner once per plane. In the line's own view. A plane nothing lies along or ends on refuses the whole call, naming the nearest ends. Every lock read back locked; every line's ends read back unmoved and returned in `ends` |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). **Nothing here has run in Revit.**

**The model - a SAVED scratch family, never the owner's own families** (not `Diffuser Flow Arrow_DRAFT.rfa`, not
the KDF diffuser). File, New, Family, the metric Detail Item template, saved as "Heron Line Lock Test" under
`%TEMP%\heron-work\line-lock`, Revit 2020. Every call with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>`
when two Revits are connected, and `< /dev/null`.

**The arrangement (NC0), made with Heron's own PROVEN tools and KEPT:** an instance length parameter "Length" =
600 (`ADD_FAMILY_PARAMETERS`); reference planes "Tip" at X 600, "Head Base" at X 300, "Head Top" at Y 80, "Head
Bottom" at Y -80, "Spare" at X 1000 (`CREATE_REFERENCE_PLANES`); "Center (Left/Right)" to "Tip" labelled Length,
"Center (Left/Right)" to "Head Base" labelled with a parameter "Head Base" = Length / 2 (`SET_FAMILY_FORMULA`,
`LABEL_FAMILY_DIMENSION`); the shaft `DRAW_FAMILY_DETAIL_LINES` "path 0,0; 300,0" in Ref. Level, lockToPlanes
false; the head `DRAW_FAMILY_FILLED_REGION` "polygon 300,80 600,0 300,-80", Solid Black. The CONTROL: a second detail line "path 0,-200; 300,-200", never locked - it shows that what follows, follows because of the lock. Record the line's and the
region's ids. Save. *The planes:* "Center (Left/Right), Center (Front/Back), Head Base, Tip, Head Top, Head Bottom".

| # | Run | Look for |
|---|---|---|
| **NC0** | *Open and arrange, KEEP it,* as above | The title bar names the scratch family; Length 600; nothing locked on the shaft or the head |
| **NC1** | *The D-30 negative FIRST* - lines = both ids, planes "Tip, Spare"; rolled back | Refused before anything is locked, naming the tip 400 mm off "Spare" - and NO lock on "Tip" either (all or nothing) |
| **NC2** | *The positive* - lines = both ids, the six planes; rolled back | Eight locks: the shaft ALONG "Center (Front/Back)", its tail to "Center (Left/Right)", its head end to "Head Base"; the head's base edge ALONG "Head Base", its corners to "Head Top" / "Head Bottom", the tip to "Tip" and "Center (Front/Back)". `ends` unchanged. **If Revit refuses a line's END reference, or gives no sketch for the region, that is the first thing this group learns** and the card is changed, not proved - the finding says which |
| **NC3** | *Planes it cannot lock to* - the shaft alone, planes "Ref. Level"; and lines = a reference plane's id; and both ids with planes "Tip, Tip Copy" (a second plane at X 600) | Refused as not seen edge on; refused as not a line or filled region; refused as two coinciding planes. Nothing locked |
| **NC4** | *FOLLOW, KEPT* - NC2's positive through `try-once` (or `revit_change` once PROVEN) and KEEP; then `SET_FAMILY_TYPE_VALUES` Length 900; then NC2's call once more | The second run: nothing new, every lock "already there"; `ends` reads the shaft (0, 0) to (450, 0) and the head's corners at (450, 80), (900, 0), (450, -80) - **the slanted edges re-drawn between them**. Then Length 300: (0, 0) to (150, 0), corners (150, 80), (300, 0), (150, -80). The CONTROL, read at Length 900 by a rolled-back call on it alone with planes "Center (Left/Right)": still (0, -200) to (300, -200) - it did NOT follow. LOOK at Edit Boundary: closed padlocks; no "lines are not in a closed loop" |
| **NC5** | *A region's corners joined?* In NC4, if one slanted edge's tip end did NOT follow while the other did | Then Revit does not keep a region's corners joined under a lock, and the card is changed to lock every edge's end, not once per corner - not proved |
| **NC6** | *Second route.* By hand on a copy: Align, the plane, the shaft or its end, the padlock; Edit Boundary, Align each corner, the padlock; both copies at Length 900 and 300 side by side | The two copies read back the same ends at each length |

**Before the owner's own families.** Only once NC is through. The first run on `Diffuser Flow Arrow_DRAFT.rfa` is
the owner's choice, through `try-once`, behind Revit's Keep or Discard; that arrow family is nested in the
diffuser, so it is saved and reloaded into the diffuser by the owner afterwards.

### Run 1 - 2026-11-11, Revit 2020 (session 37656), the scratch family above, main at 1279ce3c

NC0 arranged as written and KEPT (the shaft `...-00000814`, the head `...-0000081a`, the control `...-00000823`;
not yet saved by the owner). **Version 1: NC1 and NC2 both refused before anything was locked** - "The line
...-00000814 belongs to a form's sketch" - for a plain detail line. A read-only probe found why: a Detail Item
family keeps a `Sketch` of its own on "Ref. Level" (id 401, from the template, owned by no form or region) whose
profile lists every loose detail line, and version 1 counted every sketch. NC1's refusal was for that wrong
reason, so it tested nothing. [FRAGMENT-ISSUES row 5b-1495](../fragment-issues/section-5b-rows-401-425.md) has
the cause and the fix.

**Version 2, the same day, the same family, every run rolled back:**

| # | Result |
|---|---|
| **NC1** | **As expected.** Refused before anything was locked, naming the tip 400 mm off "Spare"; nothing locked, "Tip" included |
| **NC2** | **As expected.** Eight locks, exactly the list above; `ends` read back unchanged. Revit 2020 aligns a detail line's END reference to a plane through the API - the first thing this group set out to learn - and gives back the region's sketch from `GetDependentElements` |
| *`all`* | Planes "Center (Left/Right)": three targets (shaft, control, region), the region's three edges once each, both tail ends locked |
| **NC3-NC6** | **NOT RUN.** NC4 - KEPT and resized - is what proves the arrow FOLLOWS; no padlock has been looked at yet, as nothing was kept |

Run with `HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py validate lock-family-lines-to-planes
--write --set "lines=<shaft>, <head>" --set "planes=Center (Left/Right), Center (Front/Back), Head Base, Tip, Head
Top, Head Bottom" --negative-set "lines=<shaft>, <head>" --negative-set "planes=Tip, Spare" < /dev/null`. The card
stays DRAFT; next is NC3 on version 2.
