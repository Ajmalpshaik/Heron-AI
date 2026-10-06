# Needs checking — Group CJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CJ - the project's site, True North and units: set behind the Admin switch, and read (2026-10-06)

**Four tools, built 2026-10-06 for the setup a building-loads run reads** ([row
5b-322](../fragment-issues/section-5b-rows-176-200.md)): `set-project-location` and `set-project-units`
(both **ADMIN**, [D-106](../decisions/D-106.md)), `report-project-units`, and `report-location`
**version 4**. All four compile on every release 2020 to 2027 and are **DRAFT**.

**What has run, and where.** Scratch *Project2* (a new project from a metric template, never saved),
Revit 2024, session 51820, every change rolled back or put back - *Heron loads test* untouched:

- **Both setters, rolled back, positive and negative** - Boston to Doha by coordinates with UTC+03:00
  and True North 30 East; W and L/s to Btu/h and CFM; each value read back. Negatives: a city Revit's
  list lacks, a unit Revit does not offer for Cooling Load - refused, nothing changed. Edge cases, all
  rolled back: a city from Revit's list (Riyadh), latitude alone, True North with no side, daylight
  saving asked for (read-only in the API), an unknown key, a name three disciplines share, a new unit
  with no symbol named, a rounding change, the decimal symbol.
- **The second route, by the owner:** after the same values were kept, Manage > Location showed Doha's
  coordinates, UTC+03:00 and *Angle from Project North to True North: 30.00 deg East*, and Manage >
  Project Units showed Btu/h and CFM. Both put back with the tools and read back identical.
- **Both readers, four states each** - as the template left it, the Doha/Btu/h state, a third (Riyadh,
  12.5 West; kW, CFM, metres), and put back - each reading what was set, the units table changing in
  exactly the rows set and no other. REPORT_LOCATION was fed Project2's two levels (`--setup
  list-levels --keep-chain`), because the add-in Revit had loaded still refuses it with nothing
  selected.

Run every row on a scratch project with `--in`, `HERON_CLIENT_ID=ajmal-pc` and stdin closed
(`</dev/null`). The setters need `--allow-publish --write`, and the owner's Admin and Changes switches
on; `batch-prove` sends nothing declared ADMIN.

| # | Run | Look for |
|---|---|---|
| **CJ1** | The owner reads the drafts `brain/proof-drafts/set-project-location.yaml` and `set-project-units.yaml` (on his PC; drafts are not committed) and, if he agrees, `python brain/heron_validate.py accept <name> --by "Ajmal PS"` | Each proof names Project2, Revit 2024, session 51820, a rolled-back positive and a refused negative, and the dialog as the second route. **Signing is the owner's**, never an agent's |
| **CJ2** | **After the add-in from this change is deployed and Revit restarted**: `validate report-location --in "<scratch>"` with NOTHING selected and no setup | `bound` says *elements not given - none (optional)*; `locations` empty; `site` the whole site and True North. On an add-in older than this change the same run is refused - *elements was never supplied* - which is what it did on 2026-10-06 |
| **CJ3** | CJ2 with the site changed and put back (`fragment --write --apply set-project-location` on the scratch, then the original values), as on 2026-10-06 | The site follows each state; put back, it reads back identical. With CJ2, the evidence REPORT_LOCATION v4 is signed on |
| **CJ4** | REPORT_PROJECT_UNITS signed on the 2026-10-06 states, or the same three states again on another scratch | The table changing in exactly the rows set, and identical when put back. Second route: Manage > Project Units |
| **CJ5** | Both units tools on **Revit 2020** - `validate report-project-units` and `set-project-units --allow-publish --write --set "settings=Cooling Load=Btu/h"` | `findings` says *Revit 2020's UnitType names* - the enum route, which no Revit has run. Labels in Revit 2020's own words; the discipline from UnitGroup |
| **CJ6** | All four on **Revit 2027** (.NET 10) | The same answers as 2024. Revit 2027 added `SiteLocation.SetLatitudeAndLongitude`; the tool does not use it |
| **CJ7** | `true north=30 East` on a scratch whose **project base point is moved away** from the internal origin | The base point's shared E/N/elevation unchanged (the turn is about it), the survey point's shared position moved, both read back by REPORT_LOCATION. A turn that moved the base point throws and rolls back |
| **CJ8** | The setters on a **workshared** scratch | Either the change read back, or Revit's own refusal in the reply and nothing changed |
| **CJ9** | The Admin switch **OFF**, then `revit_change` asking to set the site | Refused by name - the Admin switch is off - and nothing sent. **The owner's switch, turned by him only** |
| **CJ10** | **The loads' first uses**, on a scratch or a copy - never *Heron loads test* itself: **CC23** with the template's Boston site (the loads must FAIL naming `doha-0.4`), then `set-project-location` to Doha and the loads again, then put back; **CC12** with `set-project-units` Btu/h and CFM, Finalize, then put back; **CC4** with `true north=30 East` - only after [row 5b-345](../fragment-issues/section-5b-rows-176-200.md)'s sign is fixed in `azimuth_deg` | CC23's FAIL and then pass; CC12's symbols exactly *Btu/h* and *CFM*, as REPORT_PROJECT_UNITS prints them; CC4's wall facing project north at bearing **330** for *30 East* |
