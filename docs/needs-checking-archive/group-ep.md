# NEEDS-CHECKING archive — Group EP

> **Checks that were done, moved out of the live register.** These are rows of Group EP of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *one install does everything: Heron's Python side after the add-in, and the Store-Claude refusal (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EP1

*Moved from the register on 2026-11-17.*

**Run.** **RUN 2026-10-17 on the owner's laptop - PASS on every look-for, with all four Revits ticked and the add-in fetched from v0.1.0** - [the run](../needs-checking/group-ep.md#the-run-of-2026-10-17-on-the-owners-laptop), below. A PC with Python 3.12 and **none** of Heron's packages (`pip list --user` shows no `mcp`). Close Revit, double-click `HeronInstaller.exe` in the new folder from File Explorer, tick a Revit and the AI Bridge connector, press Install

**Look for.** Before pressing: the *Heron's Python side* paragraph above Install. After: the add-in line per Revit, then each step said in turn while it runs, then four lines ending *heron-ready says READY*; `%TEMP%\Heron\python-setup.log` holds the pip and heron-ready output; `pip list --user` shows `mcp 2.2.0`, `cryptography 50.0.1`, `cffi 2.1.1`, `PyYAML 6.0.3`; `python tools/heron-ready.py --check` in the folder says READY; the Heron tab appears when Revit opens. Write down how long the Python half took - **Struck 2026-10-18:** PASS 2026-10-17, as recorded in this row and in the run below.

---

### Row EP3

*Moved from the register on 2026-11-17.*

**Run.** **RUN 2026-10-17 - PASS for plain `heron-install` and for `--no-python`** - [below](../needs-checking/group-ep.md#the-run-of-2026-10-17-on-the-owners-laptop). **RUN 2026-10-18 - PASS for Python 3.10 and for 3.15**, python.org's embeddable 3.10.11 and 3.15.0 each put first on PATH for one run, never installed - [the second run](../needs-checking/group-ep.md#the-run-of-2026-10-18-on-the-owners-laptop), below. `heron-install` from a terminal opened outside Claude, in the new folder; then `heron-install --no-python`; then on a user whose `python` is 3.10, plain `heron-install`; and once more where `python` is 3.15

**Look for.** The listing prints what the Python side will do before anything happens; exit 0 and READY; with `--no-python` the add-in only, no Python lines, exit 0; with 3.10, and again with 3.15, the Python line names the version found and the measured range 3.11 to 3.14, exit 2, add-in installed, pip never run - **Struck 2026-10-18:** PASS for all four shapes, 2026-10-17 and 2026-10-18, as recorded in this row and in the runs below.

---

### Row EP4

*Moved from the register on 2026-11-17.*

**Run.** **RUN 2026-10-17 - PASS, the D-30 negative** - [below](../needs-checking/group-ep.md#the-run-of-2026-10-17-on-the-owners-laptop). **The owner's laptop, Store-built Claude.** From Claude's own shell: `heron-install --list`, then `heron-install`

**Look for.** `--list` lists and ends with the refusal; plain `heron-install` exits 1 with *Heron was not installed. This is running inside the Claude app...*, and nothing changed - no new file under `%APPDATA%\Autodesk\Revit\Addins` seen from the owner's own terminal, no new package in the copy's `LocalCache\Roaming\Python` - **the D-30 negative** - **Struck 2026-10-18:** PASS 2026-10-17, as recorded in this row and in the run below.

---
