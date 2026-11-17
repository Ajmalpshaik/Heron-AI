# NEEDS-CHECKING archive — Group AI

> **Checks that were done, moved out of the live register.** These are rows of Group AI of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the development hooks, on the owner's PC ([PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308))* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row AI2

*Moved from the register on 2026-11-17.*

**Check.** ~~In a fresh session, without loading any skill, ask for a new file under `brain/` holding the Revit vendor namespace in a comment~~

**Expected.** **PASSED 2026-09-23 on the owner's PC** - the same session as AI1, with no skill loaded: a new `brain/_guard_check.py` whose one line was a comment naming the namespace. Refused before a byte was written: *"This edit would put the Revit vendor namespace in brain, and it may appear only inside revit/ or tools/ - docs/16 section 4, the adapter boundary that keeps the core testable without Revit."* The file was never on disk, `git status` stayed clean, `HERON_GUARD` was unset, and the diary logged `deny` at 17:02:10Z. *It was to look like:* Refused before the file is written, with the guard's reason. Written means the guard is not running in every session - row 5b-164 again

---

### Row AI3

*Moved from the register on 2026-11-17.*

**Check.** ~~In the same session, ask for a short note carrying Arabic text in a new file under `docs/`, then delete it~~

**Expected.** **PASSED 2026-09-23 on the owner's PC** - `docs/_arabic_check.md`, a heading and the Arabic word `مرحبا`, was written with no refusal: *"File created successfully"*. Its bytes on disk were that word's UTF-8, `d9 85 d8 b1 d8 ad d8 a8 d8 a7`, and the guard did read it rather than skip it - the diary logged `allow` at 17:02:28Z, the same session, 18 seconds after AI2's `deny`. The file was deleted and `git status` came back clean. *It was to look like:* Written, with no refusal. A refusal naming `UnicodeDecodeError` means the Windows code-page trap is back - row 5b-165

---
