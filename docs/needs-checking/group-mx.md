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
picture-to-model question are [FRAGMENT-ISSUES row 5b-1481](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `DRAW_MEP_FROM_CAD` (MODIFY) | [`brain/fragments/draw-mep-from-cad`](../../brain/fragments/draw-mep-from-cad/fragment.yaml) | The CAD link or import selected, or named (`cadName`); every Line and PolyLine segment on the layers named (exactly, case and all - one missing refuses the call and lists the layers); arcs, splines, lines not flat, pieces under `minLengthMm` and blocks counted and skipped. DOUBLE (auto for a duct): two parallel lines `minWidthMm`-`maxWidthMm` apart, overlapping, with no third parallel line between, are one duct on their centreline, the spacing its width - a line with one possible partner paired first. SINGLE (auto for a pipe): one line, one run. Pieces in one line, one width, under `bridgeMm` apart are one run, except across a point on another run. Joints, nearest first: two ends whose lines cross within `bridgeMm` (or half a width) - elbow; an end on the side of a run within half its width - tee (two at a point, cross); two in-line ends of different widths - transition; a small overshoot trimmed; never carried across another run; runs crossing in their middles listed as clashes. Sizes: each `size@x,y` hint to the one run nearest it within `hintReachMm`; carried across a two-way joint; else the default, listed; in double mode the width is the spacing rounded to `sizeStepMm`, a hint winning and the disagreement listed; a pipe's spacing never used as a size. `previewOnly` draws nothing and lists every run (`runsFound`). Then each run as one duct or pipe at the level's elevation + `middleElevationMm`, sized and read back; each joint's ROOM checked first - every leg against the fittings at both its ends on a safe estimate (elbow 1.5 x the largest size there, tee, cross, transition 1 x, tap 1 x the branch, plus a margin), a joint without room skipped and listed in `failedJoins` before Revit is asked; then the fitting by the type's routing preferences in its own SubTransaction - a TAP on an uncut main where the type prefers taps - a fitting Revit refuses by throwing taken back alone; a joint where a run was not drawn gets none. A failure Revit only POSTS is handled at the executor's commit and rolls the WHOLE call back, one undo (`allOrNothing` true: any unfitted joint does). ONE CAD per call; more selected, a Revit link selected, or a name found only inside a Revit link - refused in words; a layer missing from a view-only import - "may be turned off in that view". Above `maxRuns` (500) refused before anything is made, and more than 40 pieces per run allowed before any search; every search in a grid. One undo |
| `cad-to-mep` (method) | [`brain/skills/cad-to-mep.yaml`](../../brain/skills/cad-to-mep.yaml) | `heron_method`: find the CAD, survey its layers (`EXTRACT_CAD_CURVES`, no layer named), look at the plan (`revit_view_image`), decide each layer's meaning WITH the modeller, preview, place the tags read off the picture, draw, look again, Keep or Discard |

**The review of 2026-11-09 (d16d2c8) and what changed.** An independent review found that the per-fitting
SubTransaction does not keep the rest when Revit POSTS a failure - the executor's commit rolls the whole call back
- so the room each fitting needs is now checked before Revit is asked; it found the Taps routing preference
ignored (now a tap on an uncut main), a crossing drawn with a break joined, a drawn centreline pairing as two
half-width ducts, neighbouring ducts able to pair as false ducts, the cost unbounded by `maxRuns`, a losing hint
counted as used, an incomplete joint given a wrong fitting, sizes carried across transitions, and the method
naming the wrong tool for the CAD list and an impossible look while Keep or Discard is open. Each is fixed or
said; row 5b-1481 has the list.

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-09, a Linux container). Every Revit member it uses is in the card's
compatibility note, looked up with `tools/api-surface --members`; there is no version branch. Its geometry - the
part between reading the CAD and calling Revit - was lifted unchanged into a scratch console program with a
stand-in point type and run on four hand-made layouts: a double-line main with a tee branch and a square corner;
two ducts side by side 200 mm apart; a width change drawn with tapered lines; single-line pipes with a tag break,
a bend drawn as an arc, a branch stopping 10 mm short of the main, a parallel pipe 150 mm away, a line crossing
the main and a plain X; a cross and a T drawn as touching lines. Each gave the runs, joints and sizes expected,
after four defects that run found were fixed (a branch end joined as a far corner instead of a tee; an X
trimmed into corners; a cross merged into one line through the main; one tag sizing two runs). After the
review, eight more: four double-line ducts side by side at gaps of 150, 300 and 120 mm with edges broken in
three places (four ducts, none doubtful); a duct with its centreline drawn on the layer (dropped, full width);
a single-line and a double-line crossing drawn with a break (left unjoined, listed); the first layout with a
Taps type (the main uncut, one tap); two hints near one run (the nearer used, the other listed); a transition
with one side tagged (not carried); a duct with a stray parallel line (paired, marked DOUBTFUL); and 20,000
pieces - 100 ducts with broken edges - paired, joined and listed in 0.66 s. The room check, the taps, the
fittings and everything else that calls Revit was NOT exercised there. That is the arithmetic on made-up lines,
not Revit: **nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (the rows below measure them): whether
`GetInstanceGeometry` of a linked DWG hands back each layer's Lines and PolyLines as EXTRACT_CAD_CURVES found on
its own proof, and whether a real drawing's ducts are plain lines or blocks (MX2, MX12); whether
`NewElbowFitting` and `NewTeeFitting` place fittings on ducts drawn to the joint point with no room left for
the fitting - Revit trims the ducts when it can, and a leg shorter than the fitting is refused (MX5, MX6);
whether a fitting refused inside its SubTransaction leaves a warning Revit raises only when the outer
transaction commits, which would undo the whole call however `allOrNothing` is set (MX6); how long a floor of
linework takes (MX8).

**The picture and the model.** Until 2026-11-09 `revit_view_image` gave the picture and the view's name, never
its crop extents or scale, so a tag's pixel position could not be turned into model coordinates exactly; the
method gave each tag the middle of the run it labels, from the preview's `runsFound`. **Since 2026-11-09, on the
owner's word, the capture's reply carries a NAMED PLAN's scale, crop state, annotation crop state and offsets,
crop shape and the crop's four corners in internal mm** (`RevitCapture.PlanFrame`, read only, compiled 2020-2027,
NOT RUN), and `revit_view_image` turns them into ONE line - the picture's size, its top-left corner and the
formula from a pixel to model x and y - only for a plan whose crop is on, a plain rectangle, with the annotation
crop off, and only when the picture's own shape is the crop's. Anything else is one line saying why; the method
falls back to the middle of the run. **Whether Revit's picture fills the crop edge to edge is not known** - the
API says only that FitToPage fits "the whole view" - so MX19 measures it. Row 5b-1481 has the detail.

**Routing.** Measured 2026-11-09 with `tools/score-routing.py --sentences` (54 sentences frozen before the card
was written: 8 for it, 34 of the neighbouring cards' own words, 12 questions about a drawing; lexical and model -
the re-ranked backends NOT ASKED) and `tools/check-routing.py`, on this tree against the same tree without the
card, and again after the review's changes. Row 5b-1481 has the result. **A CONDITION FOR PROMOTION TO PROVEN:**
*"what size are the ducts on this cad drawing"* - a question - reaches this write on both backends, and is settled
before the card is signed: a read that answers it, or the owner's recorded decision that it may stay.

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
| **MX6** | *No room, and a failure Revit posts.* In a further copy: (a) shorten one leg of the square corner in the DWG well below 1.5 x its width; (b) a second corner whose leg is JUST longer than the card's estimate (1.5 x the width plus 50 mm at each end) but shorter than the type's real elbow, if the type's elbow is longer - else NOT COVERED. Then the positive, rolled back; then (a) with allOrNothing true | (a): failedJoins "the elbow - no room - run N is X mm long and would need about Y mm ... left unfitted", **no fitting asked of Revit there**, the other fittings placed, the call kept. (b): **record what Revit does** - an elbow placed, or the WHOLE call rolled back with Revit's words ("modified to be in the opposite direction") - the card says the second is what happens when the estimate is short; if it happens on ordinary drawings, the estimate is raised. allOrNothing: the whole call rolled back, the message naming the corner |
| **MX7** | *Single-line pipes.* layerNames M-PIPE-CHWS, drawAs pipe, systemType "Hydronic Supply", runType "Standard", middleElevationMm 3000, defaultSize d25, sizes "Ø100@<the tag's run middle>", rolled back | Supply and return drawn as **two pipes, never paired**; the tagged run 100, the run after the arc bend 100 ("carried"), the rest 25 and listed; an elbow at the bend, a tee at the tee; a size the pipe segment lacks named "asked ..., Revit made it ..." |
| **MX8** | *A layer with arcs, and a large layer, and its time.* A layer of the real background with many radius elbows drawn as arcs and several hundred lines - previewOnly true, then drawn, rolled back | skippedWhy counting the arcs; each straight pair either side joined by an elbow, not left open. **Record the time** and the run count; above a minute, the default maxRuns of 500 is for the owner to lower |
| **MX9** | *Kept, through `try-once`, on the modeller's yes.* MX5's call on the saved copy, the Level 1 plan in front | Keep or Discard. Look: every duct over its CAD lines, none where the drawing has none, fittings at the corners and the tee, sizes as tagged. One Ctrl+Z (before Keep) takes everything away |
| **MX10** | *Each refusal.* cadName "ZZZ No Such.dwg"; nothing selected and cadName left out; layerNames in the wrong case; one real layer and one not; drawAs pipe with the duct type; drawAs duct with "Hydronic Supply"; drawAs "cable tray"; defaultSize "d200" on the rectangular type; sizes without @; maxRuns 2 - each rolled back | Each refused in its own words, **nothing drawn**; the wrong-case layer and the half-real list name the real layers |
| **MX11** | *Mode obeyed.* mode single on the duct layer, previewOnly; mode double on the pipe layer, previewOnly | Single: two runs per duct, unpaired, at the default. Double on pipes: supply and return paired as one - the reason auto never pairs a pipe layer - and findings saying how much paired |
| **MX12** | *What a real drawing may hold.* A block on a named layer; a curve with its ends at different heights; a CAD imported "current view only"; the same DWG placed twice and named; a layer holding only text - each where a real background has one, rolled back | Blocks counted "not opened"; the curve counted; the view-only import read through its own view; placed twice - refused, select one; text only - refused, no straight line, nothing drawn. Each a drawing has not got: **NOT COVERED** |
| **MX13** | *Revit 2025 or later.* The copy upgraded on a further copy in Revit 2025, 2026 or 2027: MX2's two legs, rolled back; MX9's look on a kept copy | As MX2 and MX9 - the same runs, joints and sizes as on 2020 |
| **MX14** | *The whole method in a chat.* "understand from the CAD and draw the ducts" in a chat with Heron | The host calls `heron_method` for `cad-to-mep`, lists the CAD with `revit_imports`, surveys the layers, takes the picture, **asks** which layer is which before drawing anything, and - while the card is DRAFT - says that no chat tool runs it: the preview on the command line (`fragment ... --write`, rolled back), the draw through `try-once` on the modeller's ask; never sent through another tool. After Keep, a picture of the PLAN view |
| **MX15** | *Taps.* MX5's call with runType a duct type whose routing preference is Taps ("Mitered Elbows / Taps" where the project has it - record which), rolled back; then the same layout drawn with a Tees type | Taps: the main ONE duct through the branch point, **never cut**, the branch joined by a tap (fittings "1 tap(s)"), findings saying the preference is Taps. Tees: the main split, a tee. Select the tap: its family is the type's tap |
| **MX16** | *A crossing drawn with a break.* In a further copy of the DWG: one chilled water pipe drawn stopping 150 mm either side of another; one supply duct drawn stopping 100 mm clear of either edge of another duct; previewOnly, then drawn, rolled back | Each listed "crossing(s) drawn with a break ... left unjoined"; **no union, tee or cross through the other run**; the stopping ends open |
| **MX17** | *One CAD, and where it is.* Two CADs selected; a Revit link selected; in a further copy, a Revit model holding a CAD linked in, its CAD's name as cadName; a CAD imported "Current view only" with the duct layer turned off in that view - each rolled back | Refused: two selected, both named; the Revit link - a CAD inside a Revit link is not read; the CAD inside the link - named as inside that link; the view-only import - the layer "may be in the drawing and turned off in that view", never "not in the CAD drawing" |
| **MX18** | *A drawn centreline and doubtful pairs.* In a further copy of the DWG: a centreline on the duct layer along the main; a stray line 250 mm beside one duct edge; four ducts side by side 100-300 mm apart - previewOnly, rolled back | The main at full width, skippedWhy naming the centreline; the duct beside the stray line marked "DOUBTFUL PAIR - check" in runsFound and findings; the four ducts paired as four. Record any wrong pairing the picture shows |
| **MX19** | *The picture against the model - two known points.* Rebuild and deploy the add-in, restart Revit and the chat. In the scratch copy, on a DUPLICATE of the Level 1 plan: crop ON, crop region visible, annotation crop OFF, not rotated; take two run ends far apart inside the crop from MX3's `runsFound` - internal mm, the numbers the line gives - and note where each is on the DWG. `revit_view_image` of that plan at 1280 and again at 2400 px wide; then the same with the crop region turned by 30 degrees (Rotate the crop in the plan); then with a text note placed OUTSIDE the crop, annotation crop still off; then with the annotation crop ON; then the crop OFF | The answer has a **Model position** line naming the picture's size and formula. Find each point's pixel in the PNG (any picture viewer that shows the cursor's pixel), put it through the formula, and **record the difference** in mm from `runsFound`'s numbers at both widths - within one pixel's worth of mm on both, the picture fills the crop edge to edge; a steady offset at the edges is the margin, recorded in pixels. The rotated crop: the same check. **The text note outside the crop: record whether the picture grew** - if it did and the line was still given, the shape check missed it and the server must refuse that case. Annotation crop ON and crop OFF: a *No model position* line, never numbers. Negative (D-30): a 3D view by name - *No model position ... not a plan* |
