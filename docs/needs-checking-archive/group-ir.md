# NEEDS-CHECKING archive — Group IR

> **Checks that were done, moved out of the live register.** These are rows of Group IR of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a connector's tick boxes, above all Allow Slope Adjustments: `SET_FAMILY_CONNECTOR_ROLES` version 4 and `REPORT_FAMILY_CONNECTORS` version 2 (2026-10-22)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IR1

*Moved from the register on 2026-11-17.*

**Run.** Arrange on **Revit 2020**: File, New, Family, Metric Mechanical Equipment.rft, saved as **Heron Connector Ticks Test**. `CREATE_REFERENCE_PLANES` "Outlet Face" at -300 (left), "Inlet Face" at 300 (right), "Duct Face" at 600 (height); `CREATE_FAMILY_FORMS` one box "kind=extrusion workPlane=Ref. Level profile=rect -300,-150 300,150 startMm=0 endMm=600 solid=true"; `ADD_FAMILY_CONNECTOR` Sanitary pipe connector on "Outlet Face" and on "Inlet Face", a Supply Air duct connector on "Duct Face"; `ADD_FAMILY_PARAMETERS` "Width", Length, type

**Look for.** one box, two Sanitary pipe connectors, one duct connector. `REPORT_FAMILY_CONNECTORS` reads **"no slope adjustments"** on each pipe row and nothing of slope on the duct row - the read of the box before any write **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 94928*, which records it run and passed.

---

### Row IR14

*Moved from the register on 2026-11-17.*

**Run.** The signatures, on the owner's word only, after IR2, IR3, IR6 and IR12

**Look for.** `python brain/heron_validate.py accept set-family-connector-roles --by "Ajmal PS"` and `python brain/heron_validate.py accept report-family-connectors --by "Ajmal PS"` **Struck 2026-11-13:** signed - `REPORT_FAMILY_CONNECTORS` PROVEN at version 2, `proof:` dated 2026-11-02 by Ajmal PS, fingerprint `63cf527f0fa05f81`; `SET_FAMILY_CONNECTOR_ROLES` PROVEN at version 5, `proof:` dated 2026-11-12 by Ajmal PS, fingerprint `334167c5b046a931`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
