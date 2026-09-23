# Fragment issues — section 3e

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3e. THE SCHEDULE WALL HAS A DOOR — found 2026-09-09

**Ten fragments read or edit a schedule by looking for one in the selection**, and a schedule is a
VIEW — it cannot be selected as an element. HANDOVER.md hit this on 2026-09-07: *"both schedule
fragments refused the only object a person can select… a view cannot be selected as an element."*

**The way through was already in the library and nobody had written it down.**
`report-schedule-definition` was proved on 2026-09-08 against *"the 'Heat Recovery Unit Summary'
schedule, placed on a sheet and clicked"* — a `ScheduleSheetInstance`, which IS an element.

**And it can be done without clicking.** The category is called **`Schedule Graphics`**:

```
prove select-by-category-name set-selection   --set categoryName="Schedule Graphics"   --set inViewOnly="Notes, Symbols & Schedules"
```

Three instances on that sheet. `read-schedule-contents` — listed as *"fixed and UNPROVEN"* since
2026-09-07 — was proved through it immediately: 3 schedules, 144 body rows, against 307 non-schedules
skipped.

### …and behind the door, EIGHT fragments never got the fix

Tried through it, four of them, and every one did **nothing** — `added 0`, `changed 0`, `sorted 0`, and
tellingly `availableFields 0` and `presentFields 0`, meaning they never saw the schedule at all.

The reason is exact. **Only four fragments in the library understand `ScheduleSheetInstance`:**

| Understands it | Status |
|---|---|
| `report-schedule-definition` | PROVEN |
| `read-schedule-contents` | PROVEN 2026-09-09 |
| `place-schedule-on-sheet` | DRAFT |
| `find-unplaced-views` | — |

**Eight cast to `ViewSchedule` and nothing else**, so a schedule on a sheet slides straight past them:

`add-schedule-fields` · `add-schedule-combined-field` · `remove-schedule-fields` ·
`set-schedule-appearance` · `set-schedule-filters` · `set-schedule-sort-group` ·
`add-revision-cloud` · `export-schedule-to-csv`

The 2026-09-07 fix HANDOVER.md describes — *"they demanded a `ViewSchedule`; clicking a schedule on a
sheet gives a `ScheduleSheetInstance`"* — was applied to the two READ fragments and not to the eight
that edit.

**AND THEY FAIL SILENTLY.** Handed a schedule on a sheet they report `0 changed` with no refusal, which
reads as *"there was nothing to do"* rather than *"I could not see what you gave me"*. That is the
failure D-30's negative case exists to catch, and it is why none of them can be proved: both legs come
back identical because both legs did nothing.

**The fix is one line each and already written twice.** Do it once for all eight rather than per
fragment — a shared helper that resolves a selection to the `ViewSchedule` behind it, whichever form
arrived, is the shape the library wants.

---
