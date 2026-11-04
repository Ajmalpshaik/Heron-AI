# Needs checking — Group LE

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LE - a steel connection of a named type between beams and columns: `PLACE_STRUCTURAL_CONNECTIONS` (2026-11-01)

Asked for in the modeller's words: *"add a base plate connection to these column bases"* and *"put a shear
plate connection between this beam and column"* - Revit's Steel > Connection (Structure > Connection on 2020).
No card placed one (a grep of every fragment's C# that day for `StructuralConnection`: nothing).
`ATTACH_COLUMNS` attaches a column base to a footing, `COPE_FRAMING` cuts a beam end round a column, and
`CONNECT_OPEN_ENDS` and `CONNECT_AIR_TERMINALS` join pipes and ducts - look-alikes. *"change these
connections to Clip angle"* is `CHANGE_ELEMENT_TYPE`'s, which already changes any element's type - see
[Group LD](group-ld.md).

| What | Where | What it does |
|---|---|---|
| `PLACE_STRUCTURAL_CONNECTIONS` (MODIFY) | [`brain/fragments/place-structural-connections`](../../brain/fragments/place-structural-connections/fragment.yaml) | Places a **connection of a named connection type** - `layout` together (the default): ONE connection joining every member handed in, which must touch one another (boxes within 50 mm), else nothing is placed and the member touching none is named; `layout` each: one connection on **each member alone**, such as a base plate on each column. The column is put first, as the primary member. **The type is never guessed** - none named, nothing placed and the loaded types listed. A grid or anything that is not structural framing, a column, wall, floor or foundation is named and never handed to Revit; a connection handed in is left alone; members a connection already joins are skipped and named (`alreadyConnected`); Revit's refusal is passed on in its words. Each connection is read back - in the model, of the type asked, joining what was asked - before it is kept (`created`, `report`). **Never deletes** a connection or a member; plates, bolts and welds are the type's own |

**One of a pair.** `REPORT_STRUCTURAL_CONNECTIONS` ([Group LD](group-ld.md)) answers every question about
connections and their types, so none reaches this write.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `StructuralConnectionHandler.Create(Document, IList<ElementId>, ElementId)` is
present 2020-2027 and is the only overload called. The one without a type is deprecated in 2021 and gone from
2022; the one with input points is 2021 on; and **the one taking a type name as a string makes a custom type
and deletes the elements handed in** (RevitAPI.xml 2020 and 2027) - never called. **Nothing here has run in
Revit.** The card is DRAFT. **Only Revit can settle** which member each connection type wants as primary (the
column is put first here), which end a single-member connection lands on, and whether 50 mm finds a beam
framing into a column face with Revit's cutback - LE3 to LE5 check them.

**Routing.** Measured through `heron_retrieve.find` on the same scratch stores as Group LD. *"add a base plate connection to these column bases"* and *"put a shear plate connection between this beam and column"* reach this card without the flag (`ATTACH_COLUMNS`, and `PLACE_BOUNDARY_CONDITIONS` or `PLACE_STRUCTURAL_LOADS`, before) and `REPORT_STRUCTURAL_CONNECTIONS` with it. No sentence any other card declares or claims moved to this card. The probes *"add a connection between this beam and column"* and *"connect these beams to the column with a shear plate"* reach it too (a write before each); *"put base plates on these columns"* still reaches `ATTACH_COLUMNS` and *"is this connection approved"*, a question, reaches this card without the flag - nobody's words, so not declared ([row 5b-1386](../fragment-issues/section-5b-rows-401-425.md)).

**The model - Group LD's copy saved before any connection was drawn, `steel-le`** (LD1), never a working model.
Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/place-structural-connections-le-2026-11-01.yaml`](../../tools/jobs/place-structural-connections-le-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LE1** | *Arrange.* Open `steel-le`; `REPORT_STRUCTURAL_CONNECTIONS` with `show` types; load a base plate and a shear plate type in Connection Settings if missing, and save. Where the types are called otherwise, the job file's `connectionType` changes | No connection in the model; the loaded types written down |
| **LE2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, the grids, `layout` each: `created` none, each grid named as not a structural member, refused *Nothing handed in is a structural framing, column, wall, floor or foundation*. Positive, the columns C1 and C2, "Base plate", `layout` each: `created` 2, each "joins 1" with its end, read back of that type |
| **LE3** | *Together, and the primary.* B1 and C1 selected, the shear plate type, `layout` left out | One connection, "joins 2", C1 first as primary - compare with Revit's own Steel > Connection on the same pair in a 3D view (the second route) |
| **LE4** | *Apart.* B1 and C2 selected, the same type | `created` none; refused names C2 as touching none of B1 and points at `layout` each |
| **LE5** | *Never doubled, never guessed.* LE3 kept once with `try-once`, then LE3 again; then B1 and C1 with `connectionType` left out; then "Moment end plate" | `alreadyConnected` 1 naming the kept connection; nothing placed with no type, every type listed; no type of that name, with the list |
| **LE6** | *Revit 2024 or later.* LE2 and LE3 on a project made the same way in that release | The same answers |
