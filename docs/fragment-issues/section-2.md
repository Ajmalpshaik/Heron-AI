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

---
