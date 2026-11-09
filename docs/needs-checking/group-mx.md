# Needs checking — Group MX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MX - the lines on named layers of a CAD drawing drawn as real ducts or pipes with their fittings, sized from tags read off a picture: `DRAW_MEP_FROM_CAD` and the `cad-to-mep` method (2026-11-09)

The owner's ask: [docs/48](../48-studying-bimlogiq.md) section 5 item 6, *"CAD lines to ducts and pipes - read
the CAD layer, ask size and type, draw the run"*, and his words of 2026-11-09: *"NOT ONLY FROM THE LAYERS. IF I
SAY UNDERSTAND FROM THE CAD, IT WILL TAKE THE SCREENSHOT AND IT WILL UNDERSTAND LIKE THAT ALSO"*. Made NEW, at
DRAFT, with a method for the whole job. It overlaps `EXTRACT_CAD_CURVES`, `CREATE_MEP_RUNS` and
`FIT_MEP_JOINTS` (all PROVEN) - none was edited; widening a PROVEN one would send it back to DRAFT and out of
the chat's reach ([D-117](../DECISIONS.md)). The overlap, the merge planned once this is proved and the
picture-to-model question are [FRAGMENT-ISSUES row 5b-1480](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `DRAW_MEP_FROM_CAD` (MODIFY) | [`brain/fragments/draw-mep-from-cad`](../../brain/fragments/draw-mep-from-cad/fragment.yaml) | The CAD link or import selected, or named (`cadName`); every Line and PolyLine segment on the layers named (exactly, case and all - one missing refuses the call and lists the layers); arcs, splines, lines not flat, pieces under `minLengthMm` and blocks counted and skipped. DOUBLE (auto for a duct): two parallel lines `minWidthMm`-`maxWidthMm` apart, overlapping, with no third parallel line between, are one duct on their centreline, the spacing its width - a line with one possible partner paired first. SINGLE (auto for a pipe): one line, one run. Pieces in one line, one width, under `bridgeMm` apart are one run, except across a point on another run. Joints, nearest first: two ends whose lines cross within `bridgeMm` (or half a width) - elbow; an end on the side of a run within half its width - tee (two at a point, cross); two in-line ends of different widths - transition; a small overshoot trimmed; never carried across another run; runs crossing in their middles listed as clashes. Sizes: each `size@x,y` hint to the one run nearest it within `hintReachMm`; carried across a two-way joint; else the default, listed; in double mode the width is the spacing rounded to `sizeStepMm`, a hint winning and the disagreement listed; a pipe's spacing never used as a size. `previewOnly` draws nothing and lists every run (`runsFound`). Then each run as one duct or pipe at the level's elevation + `middleElevationMm`, sized and read back; each joint's fitting by the type's routing preferences in its own SubTransaction - a refused one taken back alone and listed in `failedJoins` (`allOrNothing` true: the whole call rolled back). Above `maxRuns` (500) refused before anything is made. One undo |
| `cad-to-mep` (method) | [`brain/skills/cad-to-mep.yaml`](../../brain/skills/cad-to-mep.yaml) | `heron_method`: find the CAD, survey its layers (`EXTRACT_CAD_CURVES`, no layer named), look at the plan (`revit_view_image`), decide each layer's meaning WITH the modeller, preview, place the tags read off the picture, draw, look again, Keep or Discard |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-09, a Linux container). Every Revit member it uses is in the card's
compatibility note, looked up with `tools/api-surface --members`; there is no version branch. Its geometry - the
part between reading the CAD and calling Revit - was lifted unchanged into a scratch console program with a
stand-in point type and run on four hand-made layouts: a double-line main with a tee branch and a square corner;
two ducts side by side 200 mm apart; a width change drawn with tapered lines; single-line pipes with a tag break,
a bend drawn as an arc, a branch stopping 10 mm short of the main, a parallel pipe 150 mm away, a line crossing
the main and a plain X; a cross and a T drawn as touching lines. Each gave the runs, joints and sizes expected,
after four defects that run found were fixed (a branch end joined as a far corner instead of a tee; an X
trimmed into corners; a cross merged into one line through the main; one tag sizing two runs). That is the
arithmetic on made-up lines, not Revit: **nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (the rows below measure them): whether
`GetInstanceGeometry` of a linked DWG hands back each layer's Lines and PolyLines as EXTRACT_CAD_CURVES found on
its own proof, and whether a real drawing's ducts are plain lines or blocks (MX2, MX12); whether
`NewElbowFitting` and `NewTeeFitting` place fittings on ducts drawn to the joint point with no room left for
the fitting - Revit trims the ducts when it can, and a leg shorter than the fitting is refused (MX5, MX6);
whether a fitting refused inside its SubTransaction leaves a warning Revit raises only when the outer
transaction commits, which would undo the whole call however `allOrNothing` is set (MX6); how long a floor of
linework takes (MX8).

**The picture and the model.** `revit_view_image` gives the picture and the view's name, never its crop extents
or scale, so a tag's pixel position cannot be turned into model coordinates exactly. The method works round it:
the preview's `runsFound` gives each run's coordinates and the span of the linework, and a hint needs only to be
nearer its own run than any other - the host gives a tag the middle of the run it labels. A small addition to
the capture's reply that would make it exact is described in row 5b-1480 and NOT made; the owner decides.

**Routing.** Measured 2026-11-09 with `tools/score-routing.py --sentences` (54 sentences frozen before the card
was written: 8 for it, 34 of the neighbouring cards' own words, 12 questions about a drawing; lexical and model -
the re-ranked backends NOT ASKED) and `tools/check-routing.py`, on this tree against the same tree without the
card. Row 5b-1480 has the result.

**The model - a SAVED scratch copy of Project1, Revit 2020** (the new project `CREATE_MEP_RUNS` was re-proved
on), with ONE test DWG linked. Never a working model. Every run below is rolled back except the arrangement
(MX1) and MX9's one run. The job file is
[`tools/jobs/draw-mep-from-cad-mx-2026-11-09.yaml`](../../tools/jobs/draw-mep-from-cad-mx-2026-11-09.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MX0** | *Open.* Revit 2020, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MX1** | *Arrange, and KEEP it.* Make or take a small test DWG - a copy of a real Gulf-project background cut to one area is best - holding: on ONE layer a DOUBLE-LINE supply duct layout (a main about 600 wide, one tee branch about 400, one square corner, one radius elbow drawn as an ARC, the main's line broken for a tag, end caps), with its size tags as TEXT; on ANOTHER layer a SINGLE-LINE chilled water layout (supply and return side by side, one tee, one bend drawn as an arc, a tag "Ø100"). Link it into the copy on Level 1 (Insert > Link CAD, Auto - origin to origin). **Record** the file name and both layer names exactly, and write them into the job file if they differ from MX-TEST.dwg, M-DUCT-SA, M-PIPE-CHWS. Save | The DWG showing in the Level 1 plan; the names recorded |
| **MX2** | *The survey, and the D-30 negative - THE NEGATIVE FIRST.* `EXTRACT_CAD_CURVES` with the DWG selected and no layer named (a READ in effect - it creates nothing); then the job file | The survey lists both layers with their counts. Negative: refused naming ZZZ-NO-SUCH-LAYER and listing the layers, **nothing drawn**. Positive: created one duct per run, fittings the elbows (2) and the tee (1), failedJoins empty, layersRead "taken as double lines" with 1 arc skipped, skippedWhy counting the end caps |
| **MX3** | *The preview.* The positive's call with previewOnly true | **Nothing new in the model.** runsFound: the span, then each run - the main split at the branch, the branch, the leg after the square corner, the leg after the arc - with "width from the spacing, height default"; findings saying PREVIEW - NOTHING WAS DRAWN. Record the run count against what the plan shows |
| **MX4** | *Size tags off the picture - the AI half.* In a chat: `revit_view_image` of the Level 1 plan; read every tag off the picture; turn each into size@x,y as the `cad-to-mep` method's step 5 says (the middle of the run it labels, from MX3's runsFound). **Record** the hints line, and which tags could not be read at that picture width | Every tag in the DWG read correctly, or named as unreadable. A preview with the hints: **every hint used** (findings "N of N size hint(s) used"), each run the tag beside it names, the run after the arc carried from its tagged neighbour |
| **MX5** | *A T and corners, drawn.* The positive with MX4's hints, rolled back - look before it rolls back if the run allows, else in MX9 | fittings 2 elbows and 1 tee, Revit's own from "Mitered Elbows / Tees"; every duct the size its tag says (Properties: Width, Height); sizeConflicts naming any tag that disagrees with its lines; runsWithoutSize only the untagged runs no tagged run continues into |
| **MX6** | *A fitting Revit refuses.* In a further copy, shorten one leg of the corner in the DWG (or choose a corner whose leg is shorter than the elbow), then the positive, rolled back; then the same with allOrNothing true | First: failedJoins naming that corner with Revit's words, the other fittings placed, **the run kept**. Second: the whole call rolled back, nothing drawn, the message naming the corner. **Record whether the first run ever undoes itself at the end** (a warning raised when the outer transaction commits) - if it does, the per-fitting SubTransaction is not enough and the card is corrected |
| **MX7** | *Single-line pipes.* layerNames M-PIPE-CHWS, drawAs pipe, systemType "Hydronic Supply", runType "Standard", middleElevationMm 3000, defaultSize d25, sizes "Ø100@<the tag's run middle>", rolled back | Supply and return drawn as **two pipes, never paired**; the tagged run 100, the run after the arc bend 100 ("carried"), the rest 25 and listed; an elbow at the bend, a tee at the tee; a size the pipe segment lacks named "asked ..., Revit made it ..." |
| **MX8** | *A layer with arcs, and a large layer, and its time.* A layer of the real background with many radius elbows drawn as arcs and several hundred lines - previewOnly true, then drawn, rolled back | skippedWhy counting the arcs; each straight pair either side joined by an elbow, not left open. **Record the time** and the run count; above a minute, the default maxRuns of 500 is for the owner to lower |
| **MX9** | *Kept, through `try-once`, on the modeller's yes.* MX5's call on the saved copy, the Level 1 plan in front | Keep or Discard. Look: every duct over its CAD lines, none where the drawing has none, fittings at the corners and the tee, sizes as tagged. One Ctrl+Z (before Keep) takes everything away |
| **MX10** | *Each refusal.* cadName "ZZZ No Such.dwg"; nothing selected and cadName left out; layerNames in the wrong case; one real layer and one not; drawAs pipe with the duct type; drawAs duct with "Hydronic Supply"; drawAs "cable tray"; defaultSize "d200" on the rectangular type; sizes without @; maxRuns 2 - each rolled back | Each refused in its own words, **nothing drawn**; the wrong-case layer and the half-real list name the real layers |
| **MX11** | *Mode obeyed.* mode single on the duct layer, previewOnly; mode double on the pipe layer, previewOnly | Single: two runs per duct, unpaired, at the default. Double on pipes: supply and return paired as one - the reason auto never pairs a pipe layer - and findings saying how much paired |
| **MX12** | *What a real drawing may hold.* A block on a named layer; a curve with its ends at different heights; a CAD imported "current view only"; the same DWG placed twice and named; a layer holding only text - each where a real background has one, rolled back | Blocks counted "not opened"; the curve counted; the view-only import read through its own view; placed twice - refused, select one; text only - refused, no straight line, nothing drawn. Each a drawing has not got: **NOT COVERED** |
| **MX13** | *Revit 2025 or later.* The copy upgraded on a further copy in Revit 2025, 2026 or 2027: MX2's two legs, rolled back; MX9's look on a kept copy | As MX2 and MX9 - the same runs, joints and sizes as on 2020 |
| **MX14** | *The whole method in a chat.* "understand from the CAD and draw the ducts" in a chat with Heron | The host calls `heron_method` for `cad-to-mep`, surveys the layers, takes the picture, **asks** which layer is which before drawing anything, previews, then draws - never drawing before the modeller has named the layers |
