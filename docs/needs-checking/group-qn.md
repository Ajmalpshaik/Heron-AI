# Needs checking — Group QN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QN - the Changes question opens in front of Settings, and a press that changed nothing is said - BUILT, NOT RUN IN REVIT (2026-11-24)

**Tracked as GitHub issue [#1370](https://github.com/Ajmalpshaik/Heron-AI/issues/1370) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-2019](../fragment-issues/row-5b-2019.md), issue [#1343](https://github.com/Ajmalpshaik/Heron-AI/issues/1343).
On Revit 2024 (Tower 171) Changes did not stay on twice. Every Heron window was owned by Revit's main window, so
the *Turn Changes on?* question was a sibling of the Settings window, not its child, and could open behind it; an
Esc pressed at a window that would not answer then answered the hidden question *leave it off*. Changed in
`revit/Heron.Revit.Addin`, compiled for Revit 2020 to 2027 (`tools/check-compile.py`), **not tested in Revit**:

- `HeronWindowStyle.ShowModal` - a Heron window opened while another is up is owned by it, centred on it, and
  brought to the front once drawn (never Topmost). The Settings window and every switch question use it.
- `HeronSettingsWindow.Press` - a press after which the switch reads as it did before, with nothing failed, shows
  an amber line: *"Changes is still OFF - nothing changed. To change it, press OFF again and answer the question
  that opens in front of this window."*
- The refusal and `revit_health`'s line say where the switch is inside Settings - *Changes at the top, then Admin,
  then Publish; the button beside it reads OFF* (kernel test and `tests/test_config_and_health.py`).

Run on any model - nothing in the model changes; restart Revit first so all three switches are off.

| ID | Do this | Pass looks like |
|---|---|---|
| **QN1** | Heron > AI Bridge > Settings; press **OFF** beside Changes. | *Turn Changes on?* opens **on top of and centred on** the Settings window. Click on Revit behind, then on Settings: the question stays in front of Settings. |
| **QN2** | With the question open, press **Esc**. | The question closes; Settings shows the amber line *"Changes is still OFF - nothing changed. ..."*; the row still reads OFF. |
| **QN3** | Press OFF beside Changes again, choose **Turn on**. | The row reads ON (red); no amber line. `revit_health` says *Changes ON, Admin off, Publish off*. |
| **QN4** | Press OFF beside Admin, choose **Leave off**. | Amber line *"Admin is still OFF - nothing changed. ..."*; `revit_health` still says *Admin off*. |
| **QN5** | Ask a chat to change something with Changes off. | The refusal says *"Changes at the top, then Admin, then Publish ... reads OFF: press it"*. |
| **QN6** | *Admin and Publish stay off* - row 5b-2019's finding. Restart Revit; in Settings press OFF beside Changes, then press **Esc** at the question. Then `revit_health`. | Changes, Admin and Publish all read OFF in Settings and in `revit_health`. The code turns Admin or Publish on only from its own row's question; ON here would be a defect - record it as a new row. |
