# Needs checking — Group PT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PT - a quoted value in a `name=value; name=value` list keeps its semicolon - BUILT, NOT RUN IN REVIT (2026-11-23)

**Tracked as GitHub issue [#1285](https://github.com/Ajmalpshaik/Heron-AI/issues/1285) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-1994](../fragment-issues/row-5b-1994.md). On 2026-11-23 (Revit 2020) `set-family-type-values`
refused a filter family's text value because it held a `;` - the binder split every request-sourced table on
every semicolon. The split is now `HeronBindingNote.NamedEntries`: a value that begins with a double quote is read
to its closing quote, keeping its `;` and spaces, and `\"` inside it is a quote; an unquoted list splits as before.
Built in a cloud container with no Revit: the add-in compiles for Revit 2020 to 2027 (`tools/check-compile.py`),
and `tests/test_binding_note.py` proves the rule on its own. **None of it has met a Revit.** Rebuild the add-in and
restart Revit first (`python tools/heron-ready.py`). That needs Revit closed, so it is the owner's step. No
fragment's code changed, so `set-family-type-values` stays PROVEN; this checks the binder in front of it.

| Change | Files | Checks |
|---|---|---|
| A quoted value keeps its `;` and spaces; `\"` is a quote; an unquoted list is unchanged | `HeronBindingNote.cs` (`NamedEntries`), `RevitFragment.cs` (`NamedValues`) | PT1, PT2, PT3, PT4 |
| A quote never closed is refused, naming the entry, nothing written | `HeronBindingNote.cs` (`NamedEntries`) | PT2 |
| Text holding a `;` into EVERY type - the way with no type made current writes it as a formula string | `set-family-type-values` (unchanged), fed by `NamedEntries` | PT5 |

**The model: a scratch family, never a working one.** A new Generic Model family from the default template, saved
in `%TEMP%\heron-work\ps`, with two text type parameters added by hand - `Filter_Efficiency` and `Filter_Cell` -
a length type parameter `Width`, and two types, `Type 1` and `Type 2`. Changes on. Run each through the bridge CLI,
as the row was seen: `python mcp/client/heron_bridge_client.py fragment set-family-type-values --write --set ...`.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**PT1**~~ | *The row's own text, quoted.* `--set "typeName=Type 1" --set "values=Filter_Efficiency=\"92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; design 81 % / 91 %\"; Filter_Cell=Bag, G4"`. Open Family Types. | Kept. `written` reads back `Filter_Efficiency` as `92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; design 81 % / 91 %` - the semicolon kept, no quote marks - and `Filter_Cell` as `Bag, G4`. Family Types shows the same. One Ctrl+Z takes both back. |
| ~~**PT2**~~ | *Negative: the same text unquoted, then a quote never closed.* `--set "values=Filter_Efficiency=70 % ASHRAE 52/76; design 81 % / 91 %"`, then `--set "values=Filter_Efficiency=\"92 %; design 81 %; Filter_Cell=G4"`. | Both refused `[bad_request_value]`, nothing written, the type unchanged. The first: *"design 81 % / 91 %" is not an entry for 'values' ... A value that holds a semicolon goes in double quotes*. The second: *'Filter_Efficiency' in 'values' opens a double quote and never closes it*. |
| ~~**PT3**~~ | *An unquoted list is as it was.* `--set "values=Width=600; Filter_Cell=G4"`. | Kept, exactly as before this change: `Width = 600` mm and `Filter_Cell = G4`. |
| **PT4** | *Spaces and a quote inside.* `--set "values=Filter_Cell=\" G4 - \\\"bag\\\" \""` (the value as Heron receives it: `Filter_Cell=" G4 - \"bag\" "`). | `Filter_Cell` reads back ` G4 - "bag" ` - the leading and trailing space kept, the two inner quote marks written as quote marks. |
| **PT5** | *Every type, text holding a `;`.* `--set "typeName=all" --set "values=Filter_Efficiency=\"a; b\""`. | Kept in one change, and both types read back `a; b`. The answer's `route` says which way was taken. If it is *"no type made current"*, Revit took a `;` inside a formula's string - record that; if it is *"each type made current in turn"*, record the reason the answer gives. Either is a pass if both types read `a; b`. |

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`, built from main `e8c3be150`)

**Arranged:** `pt-ahu` - a scratch COPY of Ajmal PS's AHU family *TRG_Mechanical Equipment_Air Handling Unit_DX_SKM
MAH-0202S2M.rfa*, saved in `%TEMP%\heron-work\pt` (the library file untouched). It has ONE type and no
*Filter_Efficiency* / *Filter_Cell*, so its own blank text type parameters stood in: *Motor_Efficiency_Class* for
Filter_Efficiency, *Keynote* for Filter_Cell, and the length *Unit_Width* for Width. Every call ran through
`heron_bridge_client.py fragment set-family-type-values --write` with no `--apply` - **rolled back**, read back by the
card, Family Types not looked at by hand. Run from branch `claude/revit-runs-1126`.

| Row | Result |
|---|---|
| PT1, run 2026-11-26 | **PASS** (rolled back) - `written` *Motor_Efficiency_Class = "92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; design 81 % / 91 %" (was "")* and *Keynote = "Bag, G4"*: the semicolon kept, no quote marks, both entries written. |
| PT2, run 2026-11-26 | **PASS** - both `[bad_request_value]`, nothing sent: *""design 81 % / 91 %" is not an entry for 'values' ... A value that holds a semicolon goes in double quotes: Note="a; b".*; and *"'Motor_Efficiency_Class' in 'values' opens a double quote and never closes it, so where its value ends cannot be told and nothing was taken from the list."* |
| PT3, run 2026-11-26 | **PASS** (rolled back) - *Unit_Width = 600 mm (was 975 mm); Keynote = "G4"*. |
| PT4, run 2026-11-26 | **FAILED, half** - `Keynote=" G4 - \"bag\" "` read back *Keynote = "G4 - "bag""*: the two inner quote marks written as quote marks (pass), but the **leading and trailing spaces were dropped**. The binder keeps them (`HeronBindingNote.NamedEntries` takes the quoted text whole); `set-family-type-values` then trims every value itself, `pair.Value.Trim()` at `impl/any/fragment.cs:451`. Whether Revit would keep the spaces is not known until that trim goes. Recorded as row [5b-2050](../fragment-issues/row-5b-2050.md), added to issue [#1285](https://github.com/Ajmalpshaik/Heron-AI/issues/1285). |
| PT5, run 2026-11-26 | **PASS on ONE type** (rolled back) - `typeName=all`, `Motor_Efficiency_Class="a; b"`: read back *"a; b"*, `route` **no type made current** - *"this text was set as a formula on every type at once and the formula cleared again"*, so Revit 2020 took the `;` inside a formula's string. The family has one type, so "both types" was not seen - still owed on a family with two. |

**Not signed, on the owner's word 2026-11-26:** `set-family-type-values` version 6 (also row 5b-2029, issue #1372) stays DRAFT until row 5b-2050's fix lands, so it is proved once, on the fixed code. Its passing draft from today is left in the worktree only.
