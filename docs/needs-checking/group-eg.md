# Needs checking — Group EG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-15 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EG - a tag turned as well as moved: `OFFSET_TAGS_FROM_HOST` version 2 (2026-10-15)

Asked for on 2026-10-15 on a real project (Revit 2020, a BIM 360 model that is never written to for a proof):
the cut-out tags in a builders-work plan were to sit inside the building, with no leader, and *"same
orientation as the opening"* - Horizontal for openings in east-west walls, Vertical for the one in a
north-south wall. [`OFFSET_TAGS_FROM_HOST`](../../brain/fragments/offset-tags-from-host/fragment.yaml)
(FRG-VIEW-113) moved all five and took the leaders off, but nothing in the library set a tag's orientation -
only `COPY_TAGS_TO_REPLACEMENTS` copies one - so the owner turned the last tag by hand.
[Row 5b-484](../fragment-issues/section-5b-rows-401-425.md) has the finding.

**What changed.** Version 2 takes five optional switches, and with none of them it is version 1:
`orientHorizontal`, `orientVertical`, `orientModel` or `orientToHost` - one choice, two at once refused - and
`keepHead`, which leaves each head where it is so a tag can be turned without moving it. `orientToHost` reads
the way the tag's element runs in the view: a family's hand direction (along the wall for a door or an opening
hosted in one), else its facing turned a quarter, else the line of a duct, pipe or wall, else the wall an
opening is cut in. Each tag is turned before its head is placed, and its orientation is read back; one Revit
keeps the other way is put back whole. The card stays **DRAFT** - it was never proved, so no proof went stale,
and Group AY's rows still decide version 1's behaviour.

**What each release allows**, read off the reference assemblies one release at a time on 2026-10-15
(`tools/api-surface --members`): an element tag's `TagOrientation` takes Horizontal and Vertical on every
release 2020 to 2027, and **Model only from 2022** - the enum has no such value on 2020 and 2021, so asking
an element tag for Model there is refused and the tag left as it was. **A room, space or area tag's own
orientation takes Horizontal, Vertical and Model on every release 2020 to 2027.** Whether Revit then
accepts a value for a given tag family is in no assembly; it is what the read-back and these rows find out.

**Compiled for every release 2020 to 2027, not yet run in Revit.** The job file's dry run reads all three
jobs runnable, and Group AY's version 1 job still reads runnable with none of the new switches in it.

**THE TEST PROJECT IS A NEW ONE, NEVER THE REAL PROJECT.** The header of
[`tools/jobs/offset-tags-from-host-orientation-2026-10-15.yaml`](../../tools/jobs/offset-tags-from-host-orientation-2026-10-15.yaml)
says how to make it: four walls as a rectangle on an unrotated Level 1 plan, a door in the middle of each,
the four door tags Horizontal with no leader, and one tagged room. **Ask the owner before opening it** in a
Revit that has the real project open, and have him Synchronize with Central first.

| # | Run | Look for |
|---|---|---|
| **EG1** | The job file's first job: the four door tags, `orientVertical` and `keepHead`; the negative gives `orientVertical` and `orientHorizontal` together | `turned` 4, `report` "0 Horizontal, 4 Vertical" and every head within 0 mm of where it was; the negative refused before any tag is touched, naming both switches, `moved` 0 |
| **EG2** | The second job: the room tag, `orientVertical` and `keepHead`; the negative asks it to follow its room (`orientToHost`) | `turned` 1, "1 Vertical" - a room tag turned on Revit 2020; the negative `noDirection` 1 and `moved` 0 |
| **EG3** | The third job, which writes the proof draft: the four door tags, `orientToHost` and `keepHead`; the negative hands the same four in with `tagCategories=Room Tags` | `turned` 2 and `alreadyThere` 2, "2 Horizontal, 2 Vertical", `offAxis` 0, `noDirection` 0; the negative `otherCategory` 4, nothing turned |
| **EG4** | The second route, THEN the signature: in a chat run of EG3's positive on the same new project (kept, not rolled back), look at the plan and select each door tag | the WEST and EAST walls' door tags read up the sheet and Properties says Orientation **Vertical**; the SOUTH and NORTH ones read across and say **Horizontal** - **read on screen, not inferred from `turned`**. Write what was seen into the draft's `second_route`, then `python brain/heron_validate.py accept offset-tags-from-host --by "Ajmal PS"` and set `heron-status: PROVEN` |
| **EG5** | The real job's shape, a chat run on the same new project: the door tags with `offsetRightMm=0`, `offsetUpMm=-8`, `addLeader=false` and `orientToHost` | every tag moved 8 mm below its door on paper AND turned the way its wall runs, no leader, every head within 0 mm of its target - the head is placed after the turn |
| **EG6** | `orientModel` on the door tags, `keepHead` - a chat run on Revit 2020 | every one in `refused`, the reason saying an element tag has no Model orientation before 2022, nothing moved or turned. On a Revit 2022 or later the same run must read back **Model** instead - owed until one is on this PC |
| **EG7** | A duct drawn at 30 degrees in a plan, tagged; its tag with `orientToHost` and `keepHead` | turned Horizontal, the nearer, and named in `offAxis` with "30 degrees off the view's right" |
