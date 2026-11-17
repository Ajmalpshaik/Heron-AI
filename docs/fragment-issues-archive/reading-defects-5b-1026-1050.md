# FRAGMENT-ISSUES archive — section 5b, rows 1026 to 1050

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1026 to 1050 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1030

*Moved from the register on 2026-11-17.*

**Defect.** **[`tools/time-family-build.py`](../../tools/time-family-build.py) COUNTS NO FORM FROM A REAL `revit_change` REPLY. CLAUDE CODE STORES HERON'S REPLY AS THE JSON TEXT `{"result":"..."}`, WITH ITS NEWLINES WRITTEN AS `\n`, AND `FORM_COUNT` IS ANCHORED TO THE START OF A LINE.** Met 2026-10-17 on the tool's first run on a real build ([Group DA](../needs-checking/group-da.md)'s build timing, [docs/04 §8.7](../04-heron-mcp.md)). Five `CREATE_FAMILY_FORMS` replies printed `formCount` 4, 10, 10, 6 and 14. For each one the tool said *"prints no single formCount - revit_change does not print a value the last reply of the same capability showed"*, which is a different cause, and it left forms built and seconds a form NOT MEASURED. In the chat's transcript, the reply on line 241 is one string that begins `{"result":"CREATE_FAMILY_FORMS ran in ...` and holds `\n  formCount       4\n` with the newlines escaped. `_text_of` returns that string as it is, so `FORM_COUNT` (`^\s+formCount\s+(\d+)\s*$`, MULTILINE) has no line start to match. The synthetic replies in [`tests/test_time_family_build.py`](../../tests/test_time_family_build.py) are written with real newlines and no wrapper, which is why the suite passes. The tool's "revit_change hand-back" figure is taken from the same undecoded text, so its 28,698 characters count each escaped newline and quote as two.

**State.** **FIXED 2026-10-17 in the tool, [pull request #634](https://github.com/Ajmalpshaik/Heron-AI/pull/634) - not re-run on the owner's build.** `_text_of` reads a tool result whose text parses as a JSON object holding a string under `result` as that string, and any other text as it is, so `formCount` is matched line by line and the hand-back counts each newline and quote once. [`tests/test_time_family_build.py`](../../tests/test_time_family_build.py) section 9 gives it a `revit_change` reply in exactly that shape, as a string and as one text part: 8 checks fail on the old `_text_of` and pass on the new one, and a wrapped reply with no count is still NOT MEASURED. The re-run on the transcript and audit kept at `%TEMP%\heron-work\pump-1017\` is LOCAL, on the laptop. Re-run 2026-10-18 on those files at `e7c0301e`: forms built 44, seconds a form 10.9 s ([docs/04 §8.7](../04-heron-mcp.md)).

---

### Row 5b-1031

*Moved from the register on 2026-11-17.*

**Defect.** **[`tools/time-family-build.py`](../../tools/time-family-build.py) COUNTS THE OWNER'S TIME IN [D-126](../DECISIONS.md)'s KEEP BOX AS "REVIT'S OWN TIME": THE AUDIT'S `ms` RUNS ROUND THE WHOLE JOB, THE BOX INCLUDED.** Met 2026-10-17 in the same run. The tool gave Revit's own time as 101.6 s over 18 runs. Revit 2024's `journal.0016.txt` has ten *"Keep what Heron just did?"* boxes inside the window, each answered *"Keep it"*, 80.9 s in all from shown to answered. Two of them, 24.8 s and 24.0 s, fall inside the two runs the audit times at 26,570 ms (`create-material`) and 26,140 ms (the first `create-family-forms`), and the tool also lists both among its "first runs in a session". The tool's docstring says a dialog answered inside a call lands in the tools' time. It does not say the dialog also lands in Revit's own time, which is the figure docs/04 §8.7 compares across builds, and the 2026-10-09 and 2026-10-11 builds ran before D-126 had a box.

**State.** **FIXED 2026-10-17 in the tool, by the pull request that adds `--journal` - run once on the owner's real build.** `tools/time-family-build.py --journal journal.NNNN.txt --journal-utc-offset +03:00` reads each Keep box from Revit's journal - shown at its `TaskDialog` line, answered at its `"TaskDialogResult"` line and the button after it, each at the last stamp at or before it - and reports the boxes, their seconds, how many were not kept, and Revit's own time less the boxes in all, for the first runs and run by run, each figure with its journal and audit lines. Without `--journal` they are NOT MEASURED, and without the offset only the whole journal's boxes are counted. The other way, the add-in's audit writing the box's seconds apart, was not taken: it needs a C# change and a redeploy on every release, and cannot help a build already recorded. Held by [`tests/test_time_family_build.py`](../../tests/test_time_family_build.py) sections 10 to 13, on synthetic journals. **Run on this build, as Ajmal PS reported it in a comment on [pull request #635](https://github.com/Ajmalpshaik/Heron-AI/pull/635), 2026-10-17:** on the laptop at `4b85495c`, the transcript, `audit-202610.jsonl` and Revit 2024.3's `journal.0016.txt` from `%TEMP%\heron-work\pump-1017\`, `--journal-utc-offset +03:00`, the window `--since 2026-10-17T17:53:25.525Z --until 2026-10-17T18:01:24.569Z`. 10 Keep boxes, all 10 inside this chat's runs, 80.9 s shown to answered, every one *"Keep it"* - the journal's hand count. Revit's own time less the boxes 20.7 s of 101.6 s; the first runs less the boxes 16.3 s; every run joined to its audit line. Not said there: whether that journal's header names its zone, so the offset is still the flag's.

---
