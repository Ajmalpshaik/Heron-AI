# Needs checking — Group PS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PS - a quoted value in a `name=value; name=value` list keeps its semicolon - BUILT, NOT RUN IN REVIT (2026-11-23)

**Why:** [row 5b-1993](../fragment-issues/row-5b-1993.md). On 2026-11-23 (Revit 2020) `set-family-type-values`
refused a filter family's text value because it held a `;` - the binder split every request-sourced table on
every semicolon. The split is now `HeronBindingNote.NamedEntries`: a value that begins with a double quote is read
to its closing quote, keeping its `;` and spaces, and `\"` inside it is a quote; an unquoted list splits as before.
Built in a cloud container with no Revit: the add-in compiles for Revit 2020 to 2027 (`tools/check-compile.py`),
and `tests/test_binding_note.py` proves the rule on its own. **None of it has met a Revit.** Rebuild the add-in and
restart Revit first (`python tools/heron-ready.py`). That needs Revit closed, so it is the owner's step. No
fragment's code changed, so `set-family-type-values` stays PROVEN; this checks the binder in front of it.

| Change | Files | Checks |
|---|---|---|
| A quoted value keeps its `;` and spaces; `\"` is a quote; an unquoted list is unchanged | `HeronBindingNote.cs` (`NamedEntries`), `RevitFragment.cs` (`NamedValues`) | PS1, PS2, PS3, PS4 |
| A quote never closed is refused, naming the entry, nothing written | `HeronBindingNote.cs` (`NamedEntries`) | PS2 |
| Text holding a `;` into EVERY type - the way with no type made current writes it as a formula string | `set-family-type-values` (unchanged), fed by `NamedEntries` | PS5 |

**The model: a scratch family, never a working one.** A new Generic Model family from the default template, saved
in `%TEMP%\heron-work\ps`, with two text type parameters added by hand - `Filter_Efficiency` and `Filter_Cell` -
a length type parameter `Width`, and two types, `Type 1` and `Type 2`. Changes on. Run each through the bridge CLI,
as the row was seen: `python mcp/client/heron_bridge_client.py fragment set-family-type-values --write --set ...`.

| ID | Do this | Pass looks like |
|---|---|---|
| **PS1** | *The row's own text, quoted.* `--set "typeName=Type 1" --set "values=Filter_Efficiency=\"92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; design 81 % / 91 %\"; Filter_Cell=Bag, G4"`. Open Family Types. | Kept. `written` reads back `Filter_Efficiency` as `92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; design 81 % / 91 %` - the semicolon kept, no quote marks - and `Filter_Cell` as `Bag, G4`. Family Types shows the same. One Ctrl+Z takes both back. |
| **PS2** | *Negative: the same text unquoted, then a quote never closed.* `--set "values=Filter_Efficiency=70 % ASHRAE 52/76; design 81 % / 91 %"`, then `--set "values=Filter_Efficiency=\"92 %; design 81 %; Filter_Cell=G4"`. | Both refused `[bad_request_value]`, nothing written, the type unchanged. The first: *"design 81 % / 91 %" is not an entry for 'values' ... A value that holds a semicolon goes in double quotes*. The second: *'Filter_Efficiency' in 'values' opens a double quote and never closes it*. |
| **PS3** | *An unquoted list is as it was.* `--set "values=Width=600; Filter_Cell=G4"`. | Kept, exactly as before this change: `Width = 600` mm and `Filter_Cell = G4`. |
| **PS4** | *Spaces and a quote inside.* `--set "values=Filter_Cell=\" G4 - \\\"bag\\\" \""` (the value as Heron receives it: `Filter_Cell=" G4 - \"bag\" "`). | `Filter_Cell` reads back ` G4 - "bag" ` - the leading and trailing space kept, the two inner quote marks written as quote marks. |
| **PS5** | *Every type, text holding a `;`.* `--set "typeName=all" --set "values=Filter_Efficiency=\"a; b\""`. | Kept in one change, and both types read back `a; b`. The answer's `route` says which way was taken. If it is *"no type made current"*, Revit took a `;` inside a formula's string - record that; if it is *"each type made current in turn"*, record the reason the answer gives. Either is a pass if both types read `a; b`. |
