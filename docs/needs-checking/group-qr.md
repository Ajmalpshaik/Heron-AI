# Needs checking — Group QR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QR - PROPOSE_LEVELS_FROM_POINT_CLOUD version 1: candidate levels from a linked point cloud - DRAFT until proved (2026-11-25)

**Tracked as GitHub issue [#1399](https://github.com/Ajmalpshaik/Heron-AI/issues/1399) since 2026-11-25** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** a modeller learning scan to BIM linked a public street scan (Union Station, converted with ReCap to
`UnionStation.rcp`) origin to origin in Revit 2024 and asked for the floor levels to be found from the cloud
rather than measured by hand. No card read a point cloud's points. `PROPOSE_LEVELS_FROM_POINT_CLOUD` is new, at
DRAFT, a READ: it creates nothing. Its `tests/cases.yaml` holds every case; these rows are the run, and
[`tools/jobs/qr-pointcloud-levels-revit2024-2026-11-25.yaml`](../../tools/jobs/qr-pointcloud-levels-revit2024-2026-11-25.yaml)
the job file.

**What is done and what is not.** Compiles for 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-25).
The point cloud namespace was added to the fragment imports in both halves - the compile gate's list and the
add-in's `HeronFragmentImports` - so the executor in Revit must be rebuilt before the card can run. **Never run
in Revit.** RevitAPI.xml says the filter is in model coordinates but not which space the points come back in;
the card carries them through the cloud's transform and says when they fall outside the cloud's extent. QR3
settles which reading is right.

**The models: scratch, never a working one** - Revit 2024. `scan-levels`: new from the metric template, the
`.rcp` linked Origin to Origin and pinned. `no-cloud`: the same template, nothing linked. Before the run, cut a
section through the cloud and place spot elevations on the road and on one roof line; write both down here.

| ID | Do this | Pass looks like |
|---|---|---|
| **QR1** | **Negative, first.** On `no-cloud`, the card with every value left out | Refused: *"No point cloud is linked in this model, so there are no points to read levels from."* No candidate, no number |
| **QR2** | On `scan-levels`, every value left out | `pointsRead` above 1,000; the lowest candidate within 100 mm of the road's spot elevation; each line with mm, project units, share, ratio and HIGH/MEDIUM/LOW, and the nearest of Level 1 / Level 2; the closing lines *"candidates, not levels"* and *"outside only"*; the element count unchanged |
| **QR3** | The same, after the cloud is moved 3000 mm up (unpin, Move, saved) | Every candidate 3000 mm higher than in QR2, and NO "CHECK: ... outside the cloud's own extent" line. A CHECK line, or candidates unmoved, means the points come back in the scan's own coordinates - the card's transform step is then wrong, and a row is opened |
| **QR4** | **Negative.** `boxMm` drawn tight round one facade's face (about 500 mm deep, read off a plan) | At most the pavement and the parapet or eaves - NOTHING HIGH or MEDIUM between them unless a section shows a ledge or canopy at that height |
| **QR5** | **Negative.** `boxMm` "900000,900000,901000,901000"; then `pointCloudName` "nothing-like-this"; then `bandMm` 2 | *"NO POINTS were read inside that box"*; refused *"No point cloud matches"* with the cloud's name; refused *"a band between 5 mm and 1000 mm"* |
