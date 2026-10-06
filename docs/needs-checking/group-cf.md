# Needs checking — Group CF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CF - Area and Volume Computations: the whole dialog read, and set behind the Admin switch (2026-10-06)

**NOTHING HERE HAS RUN IN REVIT.** Both tools were written on 2026-10-06 after a lookup found nothing
that reads or sets the dialog - the model *Heron loads test* needed Areas and Volumes before its Spaces
had volumes, and the owner set it by hand (row 5b-327). They compile on every release 2020 to 2027 and
are **DRAFT**. The owner said the same day not to test them yet: *"keep it there, it is not like
tested"*.

`report-area-volume-computations` reads both tabs and asks for nothing. `set-area-volume-computations`
is **ADMIN** ([D-106](../decisions/D-106.md)) - it runs only while the owner's Admin switch and Changes
are on. So its proof goes through `validate --allow-publish --write`, never `batch-prove`, which sends
nothing declared ADMIN.

**What the proof needs, and why it is not any model:** a test project - **never** *Heron loads test* -
whose Volume Computations still reads **Areas only**, with a few placed, bounded Rooms or Spaces, and the
Gross Building and Rentable schemes; and any family open beside it for the negative. Run with `--in`
naming the project, `HERON_CLIENT_ID=ajmal-pc`, stdin closed (`</dev/null`). Read the settings with
CF1 first: a project already at Areas and Volumes cannot show CF2's positive.

| # | Run | Look for |
|---|---|---|
| **CF1** | `validate report-area-volume-computations --in "<project>" --negative-in "<family>"` - read only | Volume Computations and Room Area Computation in the dialog's words, every area scheme including one with no areas, each with its description, Gross Building marked, its area and area-plan counts; the counts of placed rooms and spaces with a volume. Negative: the family - `notAProject`, nothing else. Second route: the dialog itself |
| **CF2** | `validate set-area-volume-computations --in "<project>" --allow-publish --write --set "settings=Volume Computations=Areas and Volumes" --negative-set "settings=Room Area Computation=<what CF1 read>"` | Positive: `changed 1`, *Areas only -> Areas and Volumes* read back, and **a Space's volume above zero** - `spacesWithVolume` above the finding's "before" count. Negative: the setting already as asked - `changed 0`, `alreadyAsAsked 1`, `settingReport` empty, nothing written |
| **CF3** | CF1 again, straight after CF2 | **The model exactly as CF1 read it** - Areas only still. The rollback is checked by reading, never assumed - a rollback reported as done once left a rename in place ([row 19](../fragment-issues/section-5-rows-001-025.md)) |
| **CF4** | CF2's positive with `Room Area Computation=At wall center` added | The room setting read back moved, and what Revit reports for **Spaces** after it - do Spaces follow the one setting the dialog shows, or keep their own? Unmeasured on every release |
| **CF5** | `settings=New Area Scheme=Heron Proof : made by a proof` | **The first time `ElementTransformUtils.CopyElement` meets an area scheme in front of Heron.** The copy must come back ordinary (not Gross Building), named, described, with **no area plan and no area** brought along - the fragment refuses and rolls back if any came. Unmeasured: the API declares no way to create a scheme, and the copy route is the Revit API forum's, not seen here |
| **CF6** | `Rename Area Scheme=Rentable -> Rentable Heron` and `Area Scheme Description=Gross Building : <text>` | Both read back. **Which parameter holds a scheme's description is a guess** - `ALL_MODEL_DESCRIPTION` - and CF1 says *not read* beside every scheme if the guess is wrong. Fix the one lookup in both fragments before anything else is proved |
| **CF7** | the Admin switch OFF, then `revit_change` asking for Areas and Volumes | Refused by name - the Admin switch is off - and nothing sent. **The owner's switch, turned by him only** |
| **CF8** | CF1 and CF2 on **Revit 2020** and **2027** | The same answers. The members were looked up in each release's reference assembly; nothing was run there |
