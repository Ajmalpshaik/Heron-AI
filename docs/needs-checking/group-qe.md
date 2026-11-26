# Needs checking — Group QE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group QE - LINK_FAMILY_PARAMETER version 7: a plain element id in `forms` names the element, as its unique id does - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1332](https://github.com/Ajmalpshaik/Heron-AI/issues/1332) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-2013](../fragment-issues/row-5b-2013.md). Version 6 read every id in `forms` as a unique id, so the
number the read tools print for an element (`ELEMENT id 9659268`) was refused as naming nothing; only the unique id
linked the nested fan copy. Version 7 ([`brain/fragments/link-family-parameter/`](../../brain/fragments/link-family-parameter/fragment.yaml))
reads digits alone as the element id, 64-bit from Revit 2024 on. Built in a cloud session; not yet run anywhere.

**The model: a scratch copy of a family, never the working one** - any family with a nested family's copy placed in
it and an Air Flow family parameter the copy's own Air Flow can be linked to (the inertial air filter family with
its nested bleed fan, saved under `%TEMP%\heron-work\qe\`, is the case that found it). The add-in rebuilt and
redeployed (`python tools/heron-ready.py`) and Revit restarted. **Revit 2020 first.**

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**QE1**~~ | *Positive - a nested copy by its plain id.* Read the copy's id with `REPORT_PARAMETER_INVENTORY` (the number after `ELEMENT id`), then `LINK_FAMILY_PARAMETER` with `forms=` that number and `links=` the copy's Air Flow parameter `=` the family's | `changed 1`, `matched 1`, the link read back in `linkReport`; select the copy and press the button at the end of that row in Properties - Associate Family Parameter shows the family parameter. A refusal *"No element in this family has the id"* is a FAIL |
| ~~**QE2**~~ | *Negative - a number that names nothing.* The same call with `forms=` a number no element in the family has (one above any id the read tools printed) | Refused *"No element in this family has the id ..."*, saying an element is named by its element id or its unique id; nothing linked, the copy's link as QE1 left it |

**Closing:** once QE1 and QE2 pass on Revit 2020, the card is signed again at version 7 (`fragment-proving`), row
5b-2013 is closed in its file with the release, model and date, and each id here struck at both ends.

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`), card version 7 from main `02780d9a4`

**Arranged:** `qs-scratch` ([Group QS](group-qs.md)'s scratch family, `%TEMP%\heron-work\qs`) with a type Length
parameter *QE Length* added (ADD_FAMILY_PARAMETERS, kept), reading 0. It holds no nested family - LOAD_FAMILY is
DRAFT - so a FORM stood in for the nested copy: the plain id is read the same way for any element named in `forms`
(the version 7 change, row 5b-2013). `Extrusion Start=QE Length` on extrusions whose start is already 0, so nothing
moved. Rolled back.

| Row | Result |
|---|---|
| QE1, run 2026-11-26, on a form | **PASS** - `forms=2684` (the plain id): *"Family 'qs-scratch': 1 link(s) or value(s) changed and 0 already as asked, on 1 element(s) named by id - read back from the family."*, `changed` 1. The same with the long id `8692d480-...-00000a83`: `changed` 1. A nested copy itself was not to hand. |
| QE2, run 2026-11-26 | **PASS** - `forms=99999999`: `refused` *"Nothing was changed. No element in this family has the id "99999999". Name an element by the id Revit prints for it (a number such as 9659268) or by its unique id - the read tools and the tools that made it give both."*, `changed` 0. |

The card is already PROVEN at version 7 (signed 2026-11-24 on a material link), so no new signature is owed for
these two.
