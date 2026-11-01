# Fragment issues — section 2

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 2. CANNOT PROVE — Revit itself declines

None of these looks like a fragment defect. Each asked Revit to do something and Revit said no, for a
reason that reads as correct. **They stay `DRAFT` because a fragment that will not act cannot be shown
to act** — the positive case and the negative case come back identical, and D-30 exists to catch
exactly that.

| Fragment | What Revit said | Worth trying |
|---|---|---|
| `delete-revision` | *"Revit would not delete revision 1 — a revision cloud still uses it"* | A revision with **no** cloud on it. There is only one revision in Snowdon |
| `edit-revision` | *"Nothing changed on revision 1"* | Why. The values passed differed from what was there |
| `remove-view-template` | *"'L2' — Revit refused to detach 'Mechanical Plan': A managed …"* (message truncated in the record) | Read the full message. It may be the workshared model, or a template that is in use |

### Since then — all three PROVEN, noted 2026-11-01

*"They stay `DRAFT`"* was true when it was written and is not now. Each was later proved with a
positive and a negative case; each proof is in the fragment's own `fragment.yaml`, under `proof:`.

| Fragment | Proved | Model |
|---|---|---|
| `remove-view-template` | 2026-09-09 | Snowdon-scratch_ajmal.al (9,628 elements), Revit 2024, session 32940 |
| `edit-revision` | 2026-10-17 | Project1 (3,247 elements), Revit 2020, session 51568 |
| `delete-revision` | 2026-10-18 | Project2 (3,035 elements), Revit 2020, session 13520 |

`edit-revision` is at version 3 and `delete-revision` at version 2 now, so the fragment that was
proved is not always the one that was refused here. Read the fragment's own history before reading
the refusal above as still true. The rows stay as they were seen, because this register is
append-only.

Found because an outside review of the repository read this section as current and asked for code
fixes to all three.

---
