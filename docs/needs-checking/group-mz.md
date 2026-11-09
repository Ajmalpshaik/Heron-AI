# Needs checking — Group MZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MZ - rebar from a spec: the placed bars checked against the spec's confirmed values, each as meets, does not meet or not checked: `CHECK_REBAR_AGAINST_SPEC` and the `reinforce-concrete` method extended (2026-11-09)

The owner's ask: [docs/48](../48-studying-bimlogiq.md) section 5 item 8, *"Rebar from a spec - read the bar sizes
and covers from a given spec, then place, then check anchorage and bar length"*, started 2026-11-09 (*"yes start
item 8"*). What the modeller gets: *"reinforce these beams from this spec"* (pasted text, or a PDF page the chat
can read) - the host writes back what the spec gives per element type, the modeller confirms it, the existing
cards place it, and the new card checks it. Made NEW, at DRAFT, READ. It overlaps `REPORT_HOST_REBAR` (PROVEN),
which reads the same sets and compares nothing - not edited; widening a PROVEN card sends it back to DRAFT and
out of the chat's reach ([D-117](../DECISIONS.md)). The overlap and what is left are
[FRAGMENT-ISSUES row 5b-1489](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `CHECK_REBAR_AGAINST_SPEC` (READ) | [`brain/fragments/check-rebar-against-spec`](../../brain/fragments/check-rebar-against-spec/fragment.yaml) | Every spec value optional; one not given is not checked. Bar words by a loaded type's exact name or a nominal diameter in mm - anything else refuses the call with the loaded types (D-34). In a straight beam a set spread up the depth more than across it is side face, and any other bar goes by the middle of its longest straight leg in the beam's own frame: lowest third of the depth bottom, highest third top, between side face (`sideBars` is per face, checked x 2); in a column every bar that is not a tie is a main bar; footings, slabs and walls are not sorted by position. Links: bar type, the set's own spacing against the largest allowed (ties placed one by one NOT CHECKED), hook angle at both ends. Cover: the host's own Rebar Cover parameters face by face, the same distance within 0.5 mm, more or less said. Longest bar: Revit's Maximum Bar Length or Bar Length of every set and area/path bar group. Laps: straight legs of two sets in one host, parallel within 1 degree, centrelines within the two radii and 1 mm, ending past each other; a "50d" lap between two sizes NOT CHECKED; none found NOT CHECKED. Anchorage ALWAYS NOT CHECKED, with each main set's hooks and its first bar's length outside the host's concrete beside the spec's figure. Answers meets / doesNotMeet / notChecked counts and `report` host by host; never "compliant" |
| `reinforce-concrete` (method, extended) | [`brain/skills/reinforce-concrete.yaml`](../../brain/skills/reinforce-concrete.yaml) | `heron_method`: save; read the spec and write it back per element type, every missing value asked, the modeller confirms; match its words to LIST_REBAR_TYPES; make missing types; the hosts; SET_REBAR_COVER; place with the existing cards (PROVEN through `revit_change`; DRAFT by the command-line preview and the one `try-once`); CHECK_REBAR_AGAINST_SPEC through `revit_read`; REPORT_HOST_REBAR; Keep or Discard |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-09, a Linux container). Every Revit member it uses is in the card's
compatibility note, looked up with `tools/api-surface --members`. Two version branches: a bar's place is read with
`GetTransformedCenterlineCurves` from 2022 and `GetCenterlineCurves(..., position)` on 2020-2021, and the
diameters with `BarDiameter` on 2020-2021 and `BarNominalDiameter` / `BarModelDiameter` from 2022.
**Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (the rows below measure them): whether
`GetCenterlineCurves(..., position)` on 2020-2021 gives the bar AT that position or the set's reference bar - the
card moves every bar by `GetBarPositionTransform` only when all positions come back on top of the first (MZ2);
whether `GetTransformedCenterlineCurves` from 2022 places each bar where Revit draws it (MZ10); whether
`REBAR_ELEM_BAR_SPACING` holds the spacing of a Fixed Number set as well as a Maximum Spacing one (MZ8); whether
Revit's Maximum Bar Length is filled for a set whose bars do not vary (MZ3, MZ9); how close the length outside the
host comes to what Revit's own dimensions show (MZ7); and how long the lap search takes on a slab's sets (MZ12).

**Routing.** Measured 2026-11-09 with `tools/score-routing.py --sentences` (62 sentences frozen before the card was
written: 8 for it, 5 of the owner's words for the job, 12 of `REPORT_HOST_REBAR`'s own, 6 of `LIST_REBAR_TYPES`'s, 21
of the placing and type cards', 10 questions; lexical and model - the re-ranked backends NOT ASKED) against main
with no card. Row 5b-1489 has the result: none of `REPORT_HOST_REBAR`'s twelve sentences and none of the four
questions it answered on main moved; no question moved to a write. **Before promotion,** the frozen set is asked
again on the owner's PC with the re-ranked backend (model+rerank), which a cloud session cannot run - MZ11
records it.

**The model - a SAVED scratch copy of the rebar-hy scratch project, Revit 2020** (Group HY's: the metric structural
template, saved under `%TEMP%\heron-work\rebar-hy`). Never a working model. Every run below is a read except
MZ1's arrangement and the bars placed by hand in MZ6 to MZ9, all kept on the copy. The job file is
[`tools/jobs/check-rebar-against-spec-mz-2026-11-09.yaml`](../../tools/jobs/check-rebar-against-spec-mz-2026-11-09.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MZ0** | *Open.* Revit 2020, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MZ1** | *Arrange, and KEEP it.* `LIST_REBAR_TYPES`: record the names of the 8, 10, 12 and 16 mm bar types and the 135 degree stirrup/tie hook. `CREATE_REBAR_COVER_TYPE` "MZ cover 30", 30 mm; `SET_REBAR_COVER` top, bottom and other to it on the Level 2 beam; `PLACE_BEAM_REBAR` on that beam - bottomBars 3 x 16mm, topBars 2 x 12mm, stirrups 8mm @ 200, the hook recorded, cover from the host - all PROVEN, through `revit_change`, kept. `REPORT_HOST_REBAR` on the beam: record each set and the longest bar. Save | The names and the longest bar recorded; three sets in the beam (bottom, top, stirrups) as `REPORT_HOST_REBAR` reads them |
| **MZ2** | *Each bar in its place - Revit 2020.* A second straight beam on Level 2, 600 mm deep or more, given side face bars too (`PLACE_BEAM_REBAR` with sideBars 3 x 12mm, kept). The card with bottomBars, topBars and sideBars as placed | bottom "3 x", top "2 x", side face "6 x" (3 on each face) - each meets. **Record** whether the card moved the bars by their position transform. If the bars of a side face set came back on one point, its lowest bar lies in the bottom third and bottom reads 9 - the 2020 curves were NOT placed and the card's test for it is wrong |
| **MZ3** | *The D-30 negative - THE NEGATIVE FIRST - then the positive.* The job file | Negative: meets 0, doesNotMeet 8 or more, each in findings with both values. Positive: meets 9, doesNotMeet 0, notChecked 2 (laps "no lap found", anchorage never passed - "both ends inside this host", 40d = 640 mm). **Record** whether the longest bar came from Maximum Bar Length or Bar Length |
| **MZ4** | *Refused, nothing checked.* bottomBars "3 x T16" (no type named T16 in the metric template); then no value at all | Refused naming 'T16' and listing the loaded types; then "No spec value was given". meets, doesNotMeet and notChecked all 0 |
| **MZ5** | *Not a host.* A steel column and a wall of a non-structural type, cover 30 | Both in notHosts; nothing checked |
| **MZ6** | *Laps.* In a wall of the copy, sketch two 16 mm bars by hand (Structure > Rebar), touching, overlapping 600 mm, kept; then a 12 mm bar lapping a 16 mm one. The card with lap 800, then 500, then 50d | 800: DOES NOT MEET, shortest 600 mm. 500: meets. 50d: 800 mm asked of the two 16 mm bars - DOES NOT MEET; the 12/16 pair NOT CHECKED with 50d, checked with 500. **Record** whether a lap Revit draws side by side is found, and whether bars laid on one line are |
| **MZ7** | *Anchorage, read and never passed.* A beam bar extended by hand 300 mm into the column at its end, kept. The card with anchorage 40d | NOT CHECKED, always. Its line names that set with "300 mm outside this host's concrete" - **record** the figure against Revit's own dimension - and 40d = 640 mm |
| **MZ8** | *A column.* A concrete column given 4 x 16mm main bars and 8mm ties at 150 with the 135 degree hook (HY6's arrangement, kept by `try-once` or by hand). The card with mainBars 4 x 16mm, links 8mm @ 150, linkHook 135; then bottomBars 3 x 16mm | meets for main bars, link type, spacing and hooks; bottomBars NOT CHECKED "a column's bars are given as main bars". **Record** whether a Fixed Number tie set gives its spacing (Revit's Spacing in Properties) |
| **MZ9** | *A slab and a footing.* HY10's area reinforcement and HY's footing bars, kept. The card with cover "top 25, bottom 40", maxBarLength below the slab's longest bar, bottomBars 3 x 16mm | cover per face as the hosts hold it; the longest bar DOES NOT MEET naming the area bar groups over it; bottomBars NOT CHECKED "sorted by position in beams and columns only" |
| **MZ10** | *Revit 2025 or later.* The copy upgraded in 2025, 2026 or 2027: MZ2 and MZ3 | As on 2020 - each bar in its place through `GetTransformedCenterlineCurves`; on 2026+ the hooks read through `GetHookTypeId` as on 2020 |
| **MZ12** | *Time.* A slab with area reinforcement both ways top and bottom, and a beam with 300 stirrups, lap 50d and every other value. Through `revit_read` | **Record the seconds.** Against the 60-second run clock; a host over 3,000 straight legs says "too many bars in one host to compare in one call" rather than running on |
| **MZ11** | *The whole method in a chat.* *"reinforce these beams from this spec"* with a short spec pasted (bottom 3T16, top 2T12, links T8@200, cover 30, lap 50d, anchorage 40d, max bar length 12 m, one value left out) | The host calls `heron_method` for `reinforce-concrete`; writes the table back per element type and ASKS the value left out; matches T16 to the model's type with the modeller; places with `PLACE_BEAM_REBAR` through `revit_change`; runs `CHECK_REBAR_AGAINST_SPEC` through `revit_read` and reads each line back as meets, does not meet or not checked; **never says compliant or OK to build**; a DRAFT card named only by its command-line preview and `try-once`. Then, on the owner's PC (model with the re-ranker), `heron_lookup` of *"what anchorage length do these bars have"*, *"what lap length do these column bars have"* and *"what bar sizes does the spec call for"*: **record** where each lands - the first two must stay `REPORT_HOST_REBAR` |
