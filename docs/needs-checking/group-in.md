# Needs checking — Group IN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IN - a still sun's time through the PC's own time zone: `SET_SUN_SETTINGS` v2, `APPLY_VISUAL_PRESET` v3, `REPORT_VISUAL_PRESET` v3 (2026-10-21)

From [row 5b-1306](../fragment-issues/section-5b-rows-401-425.md). The three tools wrote a Still sun's time through
the site's time zone, and Revit holds the dialog's clock turned to UTC by the PC's own: on the owner's laptop
(UTC+3, site Boston) 11:00 asked became 19:00, the sun below the horizon and the render black. Fixed in the code;
DRAFT until these rows pass and the owner signs.

**The model.** `Cinematic ID Test.rvt` in `%TEMP%\heron-work\cinematic-id`, a scratch copy of Revit 2020's
`rac_basic_sample_project.rvt`, with the owner's three camera views `Cinematic Interior - Kitchen`,
`Cinematic Dining Table` and `Cinematic Living Room`. Revit 2020. **The check the clock cannot make** is the sun's
height: Boston, 21 October, 11:00 on the dialog stands about 37 degrees up.

| # | Run | Look for |
|---|---|---|
| IN0 | *Negatives, rolled back.* `SET_SUN_SETTINGS` time `25:99`; `APPLY_VISUAL_PRESET` and `REPORT_VISUAL_PRESET` preset `Spielberg` | Each refused naming what it takes, NOTHING WAS CHANGED / Nothing was read |
| IN1 | *`SET_SUN_SETTINGS` still 11:00 on `Cinematic Dining Table`, rolled back* | readBack 11:00, the sun about 37 degrees up |
| IN2 | *The same kept* - `try-once` on the saved model, the owner presses Keep and opens the view's Sun Settings | The dialog shows **11:00 AM**. The proof for `SET_SUN_SETTINGS` v2, with IN0's negative |
| IN3 | *`APPLY_VISUAL_PRESET` `Interior Day` on `Cinematic Dining Table`, rolled back* | Every setting as the preset, *"Sun still 10:00"* |
| IN4 | *`Interior Day` kept* on `Cinematic Living Room` - `try-once`, the owner's Keep; he opens Sun Settings and renders | The dialog shows **10:00 AM**, Interior: Sun and Artificial, exposure 10; the render is lit, not black. The proof for `APPLY_VISUAL_PRESET` v3 |
| IN5 | *`REPORT_VISUAL_PRESET` `Interior Day` on `Cinematic Interior - Kitchen`*, whose dialog the owner set to 10:00 AM | *"Sun still 10:00 (as the preset)"* - version 2 read 02:00. The proof for `REPORT_VISUAL_PRESET` v3, with IN0's negative |
| IN6 | *`Exterior Dusk` on `Cinematic Dining Table`, rolled back* | The sun half an hour before Revit's sunset for the site that day |

**Then sign** `set-sun-settings` on IN2, `apply-visual-preset` on IN4, `report-visual-preset` on IN5 - each with IN0's
negative for it.

### Results

Run 2026-10-21 on Revit 2020 (session 68704), every run rolled back unless it says kept.

| # | Result | What was seen |
|---|---|---|
| IN0 | PASS | *"'25:99' is not a time Heron can read - say 10:00, 14:30 or 10 am. NOTHING WAS CHANGED."*; *"No visual preset is called 'Spielberg'. The presets: Exterior Day, Exterior Dusk, Interior Day, Interior Night, Equipment Close-up, Clay, Presentation Elevation. NOTHING WAS CHANGED."* (apply) and *"... Nothing was read."* (report) |
| IN1 | PASS | *"'Cinematic Dining Table': Still study, 2026-10-21 11:00; the sun 36.69° up, bearing 171.18°"*, 2 of 2 read back. Version 1 would have written 19:00, the sun at 0 degrees (row 5b-1306's probe) |
| IN2 | PASS | The owner saved and pressed **Keep** (22:08): `try-once set-sun-settings` still 11:00 on `Cinematic Dining Table` - *"KEPT  You were asked in Revit and pressed KEEP after seeing the result"*, *"Still study, 2026-10-21 11:00; the sun 36.69° up, bearing 171.18°"*. Negative against the kept run (`--kept`, rolled back): time `25:99` refused. **Signed by Ajmal PS on 2026-10-21** - `SET_SUN_SETTINGS` v2 PROVEN. His look at the dialog's 11:00 AM was asked for and not reported in words |
| IN3 | PASS on the numbers | 6 settings changed, every one *"as the preset"*, *"Sun still 10:00"*, Interior: Sun and Artificial, exposure value 10 |
| IN4 | PASS | The owner saved and pressed **Keep** (22:27): `try-once apply-visual-preset` `Interior Day` on `Cinematic Living Room` - *"KEPT  You were asked in Revit and pressed KEEP after seeing the result"*; 6 settings changed, 7 already as the preset, 0 skipped; *"Sun still 10:00 (as the preset)"*. Negative against the kept run (`--kept`, rolled back): preset `Spielberg` refused. **Signed by Ajmal PS on 2026-10-21** - `APPLY_VISUAL_PRESET` v3 PROVEN. His render of the view is his to look at |
| IN5 | PASS | *"Sun still 10:00 (as the preset)"* on the kitchen view, the owner's own 10:00 AM - **signed by Ajmal PS on 2026-10-21**, `REPORT_VISUAL_PRESET` v3 PROVEN |
| IN6 | PASS on the numbers | *"Sun still 16:23"* - 30 minutes before Revit's sunset for Boston on 21 October (16:53 on the site's standard clock; about 16:56 by an almanac) |
