# NEEDS-CHECKING archive — Group EC

> **Checks that were done, moved out of the live register.** These are rows of Group EC of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the gap pass of 2026-10-14: what the library could not do, and the tools written for it (2026-10-14)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EC3

*Moved from the register on 2026-11-17.*

**Run.** Part 2: `python tools/batch-prove.py tools/jobs/create-panel-schedules-2026-10-14.yaml`, then read [`brain/proof-drafts/create-panel-schedules.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept create-panel-schedules --by "Ajmal PS"` and set `heron-status: PROVEN`

**Look for.** `CREATE_PANEL_SCHEDULES` (FRG-MEP-067), new - the library read a panel's circuits and made no view a sheet could carry. Positive (PB-1): one schedule view made and named; negative (PB-2, already scheduled): nothing made, PB-2 and its view in `alreadyScheduled`. Second route: the positive kept by hand, Project Browser > Panel Schedules showing one new view that opens on PB-1. **And before PROVEN**, on the owner's live backend: *"open the panel schedule for DB-1"* - on the lexical route it reaches this write (the card's routing comment; on `main` it reached another write); the read sentences are declared on `REPORT_PANEL_CIRCUITS` and reach it **Struck 2026-11-13:** signed - `REPORT_PANEL_CIRCUITS` PROVEN at version 1, `proof:` dated 2026-10-21 by Ajmal PS, fingerprint `8738c9955e37ed57`; `CREATE_PANEL_SCHEDULES` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `0af8d3532c46c449`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---

### Row EC7

*Moved from the register on 2026-11-17.*

**Run.** Part 6: `python tools/batch-prove.py tools/jobs/offset-mep-section-2026-10-14.yaml`, then read [`brain/proof-drafts/offset-mep-section.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept offset-mep-section --by "Ajmal PS"` and set `heron-status: PROVEN`

**Look for.** `OFFSET_MEP_SECTION` (FRG-MEP-069), new - nothing took a stretch of a run over or under an obstruction. Positive (2000 to 3500 mm, up 400, 90 degrees): five pieces in `runs`; negative (2000 to 7000 mm, past the end): nothing changed. Second route: kept by hand, `FIT_MEP_JOINTS` fits four elbows with nothing rolled back, the far ends unmoved, the stretch at 3400 mm, and `TRACE_CONNECTIVITY` reaching end to end. **Also try a 400x200 duct at 45 degrees** and look at the legs in a section - a rectangular leg may come out turned, which the card warns of and nobody has seen yet. The method that strings the clash job together is [`clash-reroute`](../../brain/skills/clash-reroute.yaml), DRAFT **Struck 2026-11-13:** signed - `OFFSET_MEP_SECTION` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `77d94e41d20c35d7`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---

### Row EC8

*Moved from the register on 2026-11-17.*

**Run.** Part 7: `python tools/batch-prove.py tools/jobs/place-model-groups-2026-10-14.yaml`, then read [`brain/proof-drafts/place-model-groups.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept place-model-groups --by "Ajmal PS"` and set `heron-status: PROVEN`

**Look for.** `PLACE_MODEL_GROUPS` (FRG-ELE-099), new - `GROUP_ELEMENTS` makes a group and nothing placed another instance of one. Positive: two instances placed, each read back on Level 2; negative (a name that matches none): nothing placed, the model groups that exist listed. Second route: kept by hand, three instances in the Project Browser and `SELECT_GROUP_MEMBERS` on a new one returning three walls - which also exercises EC1's group half **Struck 2026-11-13:** signed - `PLACE_MODEL_GROUPS` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `7878835c2598c996`; `GROUP_ELEMENTS` PROVEN at version 2, `proof:` dated 2026-10-21 by Ajmal PS, fingerprint `b50dc00b7157022c`; `SELECT_GROUP_MEMBERS` PROVEN at version 3, `proof:` dated 2026-11-02 by Ajmal PS, fingerprint `fccc522b1624d8ee`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---

### Row EC9

*Moved from the register on 2026-11-17.*

**Run.** Part 8, with the owner's **Admin** and **Changes** switches on, `HERON_CLIENT_ID=ajmal-pc` and stdin closed: `python brain/heron_validate.py validate acquire-coordinates --in "Heron Gap Pass Test" --allow-publish --write --set "linkInstance=<link A's name>" --negative-set "linkInstance=<link B's name>" </dev/null` - `batch-prove` sends nothing ADMIN ([D-106](../decisions/D-106.md)). Then `accept` and PROVEN as for the others

**Look for.** `ACQUIRE_COORDINATES` (FRG-DOC-049), new, **ADMIN** - `CHECK_LINK_COORDINATES` read the links' coordinates and nothing could take them. Positive (link A): `acquired` true, `before` and `after` different, `after` reading link A's system (E 10000, N 20000, 30 degrees, measured at its origin), no element moved; negative (link B, unloaded): nothing acquired, refused by name. Rolled back by the proof. Second route: by hand, Report Shared Coordinates on a point before and after **Struck 2026-11-13:** signed - `CHECK_LINK_COORDINATES` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `114f813233b5d769`; `ACQUIRE_COORDINATES` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `72d0e08226c642cf`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---

### Row EC11

*Moved from the register on 2026-11-17.*

**Run.** Part 1's wall and part 6's pipe: `python tools/batch-prove.py tools/jobs/create-wall-elevation-2026-10-14.yaml`, then read [`brain/proof-drafts/create-wall-elevation.yaml`](../../brain/proof-drafts/README.md), `accept` and PROVEN as for the others

**Look for.** `CREATE_WALL_ELEVATION` (FRG-VIEW-139), new - *"make an elevation of this wall"* reached `CREATE_ROOM_ELEVATIONS`, whose views look in the marker's default directions. Positive (part 1's wall at 30 degrees, exterior, 3000 mm): one elevation, the read-back saying it faces the wall square; negative (part 6's pipe handed in as the wall): refused as not a wall, nothing made. Second route: open the view - the wall square on screen, its two ends level - and the marker's arrow pointing at the wall in plan **Struck 2026-11-13:** signed - `CREATE_WALL_ELEVATION` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `076ebf6a0697303a`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---

### Row EC13

*Moved from the register on 2026-11-17.*

**Run.** Part 10: `python tools/batch-prove.py tools/jobs/report-project-information-2026-10-14.yaml`, then read [`brain/proof-drafts/report-project-information.yaml`](../../brain/proof-drafts/README.md), `accept` and PROVEN as for the others. Then, by hand, change Project Status to `Issued for construction` and ask again through `revit_read`

**Look for.** `REPORT_PROJECT_INFORMATION` (FRG-DOC-052), new - nothing read Project Information, and *"what is the project number"* reached `SET_REVISION_SETTINGS`, a write, on the trained search ([row 5b-477](../fragment-issues/section-5b-rows-476-500.md)). Positive (the project): `projectInfo` with the ten rows in the dialog's order, `Project Number = HERON-2026-014` and `Client Name = Heron Test Client` among them, `Building Name = (empty)`, and a finding naming Building Name as empty; negative (the family): `projectInfo` empty, `notAProject` true. Tracking: after the hand change only the Project Status row differs. Second route: Manage > Project Information, and the title block on any sheet, whose labels print the same values **Struck 2026-11-13:** signed - `REPORT_PROJECT_INFORMATION` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `105cf1038c186628`; `SET_REVISION_SETTINGS` PROVEN at version 1, `proof:` dated 2026-10-18 by Ajmal PS, fingerprint `785608cc1ba45c43`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
