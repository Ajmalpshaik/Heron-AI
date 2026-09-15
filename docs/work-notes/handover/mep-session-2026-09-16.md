# MEP session handover — 2026-09-16

> **Type:** Operational work note — the handover for one working session. **Not specification.**
> Where a sentence here disagrees with the [Constitution](../../../HERON_CONSTITUTION.md), the
> [Golden Rules](../../14-golden-rules.md) or [DECISIONS.md](../../DECISIONS.md), **those win.**
>
> **Status at close: 311 `PROVEN` / 62 `DRAFT`**, 373 total, up from 298 / 62 / 360.
> Fourteen signed by Ajmal PS and all fourteen promoted.
>
> **This note is scaffolding.** Everything durable is in the fragments themselves, in
> [`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md) rows 95–98, and in the model.
> **Delete this once the next session has read it.**

---

## 0. The one thing to read if you read nothing else

**This was not a proving session. It was a JOB**, and the fragments were built because the job
needed them. Ajmal asked for a copper refrigerant pipe type for split AC units, in his own naming
convention, drawn in his own colour, with fittings that fit his sizes. **Thirteen fragments exist
because that sentence could not be carried out with the library as it stood** — and every one of
them was proved against the model the job was done in, not against an arrangement invented for the
proof.

That is the better order and it is worth keeping: **the gap is found by doing the work, not by
auditing the catalogue.**

## 1. Where the library stands

**Derive it, do not read it here.**

```bash
grep -h "^heron-status:" brain/fragments/*/fragment.yaml | sort | uniq -c
```

At close: **311 `PROVEN`, 62 `DRAFT`**, 373 total. Also derivable: **136 `READ` proved, 159
`MODIFY` proved**, 312 signatures in the library.

One signature is **STALE** — `set-wall-constraints`, Ajmal PS, 2026-09-13, with the code changed
under it afterwards. **It is not from this session and was already stale when this one opened.**
Re-proving it is the owner's call; D-30 flagging it is the mechanism working.

Every fragment compiles on **all eight releases**. `check-docs`, `check-metadata`,
`check-structure`, `check-package`, `check-routing`, `check-intrusion` and `check-signatures` all
exit 0.

## 2. The thirteen new fragments, and what each one was for

| Fragment | Id | The sentence that needed it |
|---|---|---|
| `set-routing-preference` | FRG-MEP-046 | *"make this type copper"* — points one routing rule at a different segment |
| `report-pipe-segments` | FRG-MEP-047 | *"what sizes has it got"* — and the answer had to be a STRING, see §5 |
| `create-pipe-segment` | FRG-MEP-048 | *"build me an EN 12735 segment"* — copies sizes, sets nominal = OD, sets roughness |
| `report-pipe-schedules` | FRG-MEP-049 | *"delete that schedule"* — says which segments use each one, so deleting is a reading not a guess |
| `copy-routing-preferences` | FRG-MEP-050 | *"set this family same ones"* — a whole rule group, because a group holds more than one rule |
| `report-family-size-table` | FRG-ELE-056 | *"our size is not there in the lookup table"* |
| `write-family-size-table` | FRG-ELE-057 | …and putting it right |
| `report-family-parameters` | FRG-ELE-058 | *"where does this family get its material"* |
| `report-family-tables-in-project` | FRG-ELE-059 | reading every family's table without opening eight windows |
| `write-family-table-in-project` | FRG-ELE-060 | the same, writing. **The reload half does not work — see §4** |
| `set-material-colour` | FRG-ELE-061 | *"for this pipe i cant see any color"* |
| `open-family-for-editing` | FRG-ELE-062 | *"make the fragment to open the family"* |
| `activate-document` | FRG-DOC-030 | the return trip out of a family, back to the project |

`report-routing-preferences` was amended too — it gained a `summary` string, for the reason in §5.

## 3. What is in the model, and what is NOT

Model: **`PIPE`**, 3,332 elements, Revit 2020, session 8084.

**Done and read back:**

- the pipe sits **2000.0 mm** from the wall — measured, not assumed, because it had already moved once
- pipe type **`TRG_Pipe_Copper_Refrigerant`**, with REFRIGERANT stated in its Description
- segment **`TRG_Copper_Refrigerant`** — 10 sizes, **Nominal size = OD**, roughness **0.00254**
- material **`Copper`** at RGB **184, 115, 51**
- all 8 fitting families renamed to the TRG convention, each given his sizes with **FOD / DE = OD**
- the obsolete EN 12735 segment and its orphaned schedule deleted

**NOT done, and the next session should start here:**

1. **The fitting families still carry `Material [type] = TCM_MAT_Copper`.** The sizes are right; the
   material NAME is not his. One field per family in Family Types, eight families. Never touched,
   because it was not asked for and it changes what every one of them is made of.
2. **`TRG_Elbow_Copper_Gas` still has its original 8 rows.** Deliberate — it belongs to the gas
   pipe, not to the refrigerant type.

## 4. Four traps, each one paid for

**A CSV's FILENAME names the table, not the `tableName` argument.** `TRG_Union_Copper__Unione.csv`
created a **second** table and reported `imported true`, `9 rows before, 9 after` — a perfect
success against a table nobody asked for. The fix is to stage each CSV in a per-family folder named
exactly as its table. `write-family-size-table` now warns when the two disagree.

**Loading a family with "overwrite parameter values" carries its MATERIALS into the project.**
Loading the six corrected families reset the project's `Copper` material colour back to the gas
pipe's dark red, silently. Caught during the proofs and set back to 184,115,51. **Check the material
colour after any family load.**

**`Document.EditFamily` opens a family NOBODY CAN SEE.** It is real and editable and it never becomes
the window in front — and it cannot be loaded back without answering Revit's overwrite question,
which a fragment may not do. Six families were imported into and every import discarded. That is why
`open-family-for-editing` goes through a **.rfa file** deliberately: the file is the bridge into the
UI, not a side effect. `write-family-table-in-project`'s reload leg still fails with *"Family loading
failed"* and **says so** rather than reporting a success the project never got.

**A fragment cannot declare a class.** The executor compiles it as a method body, so
`IFamilyLoadOptions` is impossible from inside one. `load-family` had already ruled that the
overwrite answer belongs in the executor; that ruling was respected rather than worked around.

## 5. The reply truncates lists to three, and it is worse than it looks

`RevitFragment.Describe` renders **three** items and then `...`. **The count is honest and the
contents are not.** A segment with 24 sizes reads as three.

**Strings do not truncate.** So when the LIST is the answer — every size, every parameter, every
rule group — the fragment returns a single joined string as well, and the list stays for chaining.
Five of this session's fragments do it, and `report-routing-preferences` was amended to do it too.

## 6. The four defects, all filed

Rows **95–98** in [`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md). In short:

- **95, FIXED** — `prove`/`validate` picked the Revit session by lowest PID and never said which. A
  proof came back `positive ok` while its own accounting said `scanned 0`, against the wrong model.
  `--session <pid>` now, and a **refusal** when two are live and none is named.
- **96, OPEN** — `binds:` is read by the Python half and ignored by the C# executor. 5 fragments
  unrunnable by any route. A separate session is on it (`task_1d55ff4d`).
- **97, OPEN** — `revit_change` refuses its own change; an unsaved document gets two keys. A separate
  session is on it (`task_1441f96f`).
- **98, FIXED** — a `risk: READ` fragment closed six family windows the owner had open and had not
  saved. **No gate here could have caught it**: it opened no transaction and changed no element.
  The rule is now explicit in the fragment — close only what you opened.

## 7. About the signatures

**Every one of the fourteen is signed `Ajmal PS`, on his instruction, and he did not read each proof
line by line.** The evidence in each file is real — a positive and a contrasting negative, run
against `PIPE` and recorded verbatim. One proof was **REFUSED** by the machinery first
(`report-family-size-table`, whose negative came back carrying content) and was re-arranged until
the negative was genuinely empty. But the `by:` line says a person looked, and the honest position
is that a person authorised the signing. **Worth spot-checking one or two.**

Every proof carries the same declared gap, as most of the library does:

> `second_route: NOT ESTABLISHED — no second route was run.`
