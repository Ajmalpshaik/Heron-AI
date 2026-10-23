# Needs checking — Group IW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IW - a night with nobody at the keyboard: `batch-prove --unattended` waits out a held Revit and closes only Revit's memory warning (2026-10-23)

On 2026-10-21 and again on 2026-10-22 a session proving fragments while the owner slept wrote the same
private wrapper round [`tools/batch-prove.py`](../../tools/batch-prove.py). What it did is the tool's own now,
as `--unattended` ([`tools/README.md`](../../tools/README.md#batch-provepy--many-fragments-proved-in-one-pass-and-neither-half-taken-on-trust)):
a job refused because another chat held the Revit or the model is asked again after 30 s, 25 tries in all; a
job refused `revit_busy` has Revit's **"Virtual Memory - High Usage"** warning closed - by its exact title,
with `WM_CLOSE` - and is asked again after 10 s; the models go back after each fragment; one line per fragment
goes to `batch-prove.log` in Heron's log folder and `--report` is rewritten after each.

[`tests/test_batch_prove.py`](../../tests/test_batch_prove.py) holds the rules without a Revit. **What it cannot
hold is the closer meeting the real window.** The wrapper found the warning with UI Automation, among Revit's
children by process id; this finds it among the top-level windows Revit's process owns, by title, with no
package to install. A dialog Revit puts up is a top-level window it owns, and its title is what UI Automation
names it - **but nobody has watched this code close it.** None of the rows below has run.

**The owner's PC, Revit 2020, a scratch copy of a sample model - never his own file.** Every row runs the
job file of a real night, or one job of it, with `--session <pid>` and `--report`. Nothing here signs,
promotes or keeps a change: every run is rolled back as by day.

| # | Run | Look for |
|---|---|---|
| **IW1** | *The warning closed.* A night long enough for Revit 2020 to put up **"Virtual Memory - High Usage"** - about sixty jobs - with `--unattended`. Or, the moment it is up on any night, one job of that night's file with `--unattended` | The run prints *closed Revit's "Virtual Memory - High Usage" warning (Revit <pid>)*, **the warning is gone from the screen**, and the next try is taken - its verdict is about the fragment, not `revit_busy`. *"no ... warning is open"* while it is plainly up means it is not a top-level window of Revit's process: this code cannot see it - reopen this group and find it as the wrapper did |
| **IW2** | *Negative - nothing else is ever closed.* On the scratch model open Revit's own **Object Styles** (Manage tab) and leave it open; run one job of a night's file with `--unattended` | Every try prints *no "Virtual Memory - High Usage" warning is open - nothing was closed* and *left as it is, for a person: "Object Styles"*; **Object Styles is still open** after the last try; the job ends `DID NOT RUN` with Revit's busy sentence and `(after 25 tries)` in the log. Anything closed fails the row - stop using `--unattended` until it is fixed |
| **IW3** | *A held model waited out.* Use a Heron tool on the scratch model from a chat, and do not release it; start one job with `--unattended`; after a minute or two press **Free this model** on the ribbon | *held by another chat, asking again in 30 s* with the model-hold sentence, then the job runs once freed: a judged verdict, and `(after N tries)` on its log line |
| **IW4** | *The morning after.* Any `--unattended` night, its output sent to a file (`> night.txt`) | `%LOCALAPPDATA%\Heron\logs\batch-prove.log` has a line opening the night, one line per job and a `finished:` line; the `--report` file says `"finished": true` and holds every job; `night.txt` reads as UTF-8 and the run did not stop on a character it could not print |

**Then** the group closes on IW1 to IW4 - there is no card to sign: `--unattended` is a tool's flag, not a
fragment. A row that fails reopens it; nothing in a row changes to make it pass.
