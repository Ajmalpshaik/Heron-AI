# NEEDS-CHECKING archive — Group HF

> **Checks that were done, moved out of the live register.** These are rows of Group HF of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *Heron's data out of a Store Claude's copy, and the Companion's note where Revit looks: D-135 on a real PC (2026-10-18)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row HF1

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-18 on the laptop** - one carry, 39 files copied and 25,118 audit lines, nothing failed; the copy's 41 files unchanged - [what was seen](../needs-checking/group-hf.md#2026-10-18-the-run-on-the-laptop), below. *The real carry, once, on the laptop, after this change is on `main` and the Claude app restarted.* Before: `python brain/heron_paths.py --where` from Claude's shell and from the owner's own terminal - both name `C:\Users\ajmal\.heron`, which holds no `carried.json` yet (a suite run can make the folder itself - [row 5b-1070](../fragment-issues/section-5b-rows-1051-1075.md)). Then any chat that asks Heron anything. Then from the owner's terminal: `dir %USERPROFILE%\.heron` and `type %USERPROFILE%\.heron\carried.json`

**Look for.** `carried.json` names both old places, about 39 files copied and the brain's audit lines added, nothing failed; `knowledge\projects` holds the loads project's memory. **The D-30 negative:** the copy's `LocalCache\Roaming\Heron` holds as many files as before, the same - nothing moved, nothing deleted; and a second chat leaves no second run in `carried.json`

---

### Row HF2

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-18** - 40 lines added, a second `--carry` added none - [what was seen](../needs-checking/group-hf.md#2026-10-18-the-run-on-the-laptop), below. *The 40 lines the copy hides.* From the owner's terminal, outside the app: `python brain/heron_paths.py --carry`

**Look for.** About 40 audit lines added - the brain file only the real `%APPDATA%\Heron\audit` held - and the store named as LEFT, the home's own kept. A second `--carry` adds nothing

---

### Row HF7

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-18** - 42 files from both folders, the drill byte for byte, neither folder changed - [what was seen](../needs-checking/group-hf.md#2026-10-18-the-run-on-the-laptop), below. *The default backup holds both folders - [row 5b-1069](../fragment-issues/section-5b-rows-1051-1075.md).* After HF1, from the owner's terminal outside the app, with neither `HERON_DATA` nor `HERON_BACKUP` set: `python tools/heron-backup.py backup`, then `python tools/heron-backup.py drill`, then `python tools/heron-backup.py list`

**Look for.** The backup lands in `%USERPROFILE%\.heron\backup\<stamp>` and names two folders - `%APPDATA%\Heron` (the add-in's config and audit) and `%USERPROFILE%\.heron` (knowledge, `knowledge\projects`, the brain's audit) - each `knowledge\*.db` skipped; the drill says both came back byte for byte; `list` still shows any backup taken in `%APPDATA%\Heron\backup` before. **The D-30 negative:** neither folder changed across the drill, and `%APPDATA%\Heron\backup` gained nothing

---
