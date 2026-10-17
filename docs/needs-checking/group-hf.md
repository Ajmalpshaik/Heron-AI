# Needs checking — Group HF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-18 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HF - Heron's data out of a Store Claude's copy, and the Companion's note where Revit looks: D-135 on a real PC (2026-10-18)

Found from [Q-63](../open-questions/tier-2.md) and rows [5b-416, 5b-418 and 5b-419](../fragment-issues/section-5b-rows-401-425.md);
decided and built as [D-135](../decisions/D-135.md). **What changed.** On Windows Heron's Python side keeps its
knowledge store, every project's memory and the brain's audit in `%USERPROFILE%\.heron` (`HERON_DATA`
overrides), and the first process to ask carries in, copied and never moved, what each Store Claude's copy
and `%APPDATA%\Heron` hold, leaving `carried.json` to say so. The Companion's notes are in
`%TEMP%\Heron\companion` on both sides. The add-in's settings and trail do not move.

**What is measured, tested and not run.** Measured on the owner's laptop on 2026-10-17 and 18, from Claude's
shell and from his Terminal panel: where writes from inside the app land (D-135's table); and two trial
carries of his real data into scratch folders, one from each side - 39 files copied and 24,964 audit lines
from inside, 39 files and 25,004 lines from outside (the 40 more are a brain file only the real disk held,
which the copy hides from inside), and the copy's 41 files the same, byte for byte, before and after both.
`tests/test_data_home.py` and `tests/Heron.Kernel.TestHost` section 2 FAILED on the code before and pass on
it. **Not run:** the real carry, into the real `%USERPROFILE%\.heron`, by a chat on this code; and the
Companion button, which needs the add-in rebuilt and deployed from this change.

| # | Run | Look for |
|---|---|---|
| **HF1** | *The real carry, once, on the laptop, after this change is on `main` and the Claude app restarted.* Before: `python brain/heron_paths.py --where` from Claude's shell and from the owner's own terminal - both name `C:\Users\ajmal\.heron`, which does not exist yet. Then any chat that asks Heron anything. Then from the owner's terminal: `dir %USERPROFILE%\.heron` and `type %USERPROFILE%\.heron\carried.json` | `carried.json` names both old places, about 39 files copied and the brain's audit lines added, nothing failed; `knowledge\projects` holds the loads project's memory. **The D-30 negative:** the copy's `LocalCache\Roaming\Heron` holds as many files as before, the same - nothing moved, nothing deleted; and a second chat leaves no second run in `carried.json` |
| **HF2** | *The 40 lines the copy hides.* From the owner's terminal, outside the app: `python brain/heron_paths.py --carry` | About 40 audit lines added - the brain file only the real `%APPDATA%\Heron\audit` held - and the store named as LEFT, the home's own kept. A second `--carry` adds nothing |
| **HF3** | *New writes land outside the copy.* After HF1, ask a chat for a loads report on a project, and anything else Heron answers | From the owner's terminal: the project's new run under `%USERPROFILE%\.heron\knowledge\projects` and new lines in `%USERPROFILE%\.heron\audit\audit-brain-<month>.jsonl`. **Negative:** the copy's `Heron\knowledge` and `Heron\audit` unchanged in count and size |
| **HF4** | *The Companion button, on the new folder - this is [BK14](group-bk.md) again.* The add-in rebuilt and deployed from this change; a Revit the OWNER starts (never one started from Claude's shell); a chat open with the Companion on; press **Companion** on the Heron tab | The chat's page opens in the browser. `%TEMP%\Heron\companion` holds the chat's `chat-<pid>.json`. **Negative:** with no chat running, *"No Claude chat with Heron is running on this PC"*, and nothing opens |
| **HF5** | *A PC whose Claude is NOT the Store build* - the office PC - after the update and a Claude restart | The first chat carries `%APPDATA%\Heron\knowledge` and the brain's audit into `%USERPROFILE%\.heron`; `carried.json` names one old place; the old folder unchanged |
| **HF6** | *Removing the old copies - only on the owner's word, never as a check.* | Not a check to run: the owner decides whether to delete the copy's `Heron\knowledge` and `Heron\audit` (or Reset the app) once HF1 to HF3 pass |
