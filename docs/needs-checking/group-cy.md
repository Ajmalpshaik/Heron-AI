# Needs checking — Group CY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CY - every distinct date typed on the sheets, all sheets, nothing selected (2026-10-10)

On 2026-10-10 a modeller in Revit 2020, with model *4355-BHVD-3D-50C10-BL001A* (19 sheets) open, asked
to *"check all sheets, take all the text on the sheets, take the dates in that text from all sheets,
and keep only unique dates"*, and confirmed that only a note typed on the sheet itself counts - not text
inside a plan or section placed on it. Two PROVEN cards came close and neither answered.
`EXTRACT_DATES_FROM_TEXT` reads the sheets it is handed and nothing was selected; its dates are a
dictionary, which reaches the reply as its count alone. `FIND_DATES_IN_VIEWS` ran - 2,621 notes, 57
views, 7 unique dates in 15 views - but it reads every view, not the sheets, and its dates are a list,
which reaches the reply as its count and first three items: **five of the seven dates never reached the
chat** (`RevitFragment.Describe`).

**What was built.** New `LIST_DATES_ON_SHEETS` (FRG-SHT-028, DRAFT, READ). It takes the document and
nothing else: every sheet (sheet templates left out), then every text note in the model, kept only when
its **owner view is a sheet**. The date rules are `FIND_DATES_IN_VIEWS`' parser copied character for
character - a day, a written month of three or more letters, a 2 or 4 digit year, a calendar check, a
numeric month refused. It leaves `uniqueDateCount`; `dateList`, every distinct date on one line, oldest
first, *"09-SEP-2026 - 2 note(s) on 2 sheet(s): M-101, M-102"* with `  ||  ` between - a **string**, so
every date reaches the reply; `sheetsWithDateCount`; and as accounting `sheetsRead`, `notesOnSheets`,
`notesInViewsNotRead`, `sheetsWithNoDate` (sheet numbers, placeholders marked) and a `summary` that says
every time what was not read - notes inside views, title block labels and parameters, numeric months.

**New rather than widened, and that was weighed.** Widening either PROVEN card would move code under its
fingerprint and send it back to DRAFT until re-proved: `EXTRACT_DATES_FROM_TEXT` would have to read
"nothing selected" as "every sheet", the silent fall-back a negative case exists to catch, and
`FIND_DATES_IN_VIEWS` would need a sheets-only switch and a string answer. If the owner would rather
widen `FIND_DATES_IN_VIEWS` with a switch, this card is the one to retire. Neither PROVEN card's code was
touched; each gained one routing comment naming this card.

Compiled for Revit 2020 to 2027 on 2026-10-10 (`tools/check-fragments-compile.py`, every release ok).
`tools/api-surface` **could not run** on that PC - Windows Application Control blocked the freshly built
`ApiSurface.dll` - so the members were read instead from the `RevitAPI.xml` of every release's reference
package: all documented 2020 to 2027 (`Text` on `TextElement`). **Nothing below has run in Revit.** The
arrangement and commands are in
[`list-dates-on-sheets-2026-10-10.yaml`](../../tools/jobs/list-dates-on-sheets-2026-10-10.yaml), which
`batch-prove --dry-run` reads as 1 WOULD RUN.

With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`
on every line.

| # | Run | Look for |
|---|---|---|
| **CY1** | Arrange **Heron Sheet Dates**, a scratch project: four sheets S-01 to S-04 with a title block. With each SHEET as the open view, type S-01 *ISSUED 09 SEP 2026* and *REV A 22 Jul 2025*; S-02 *22/JUL/25* and *15 MAY 2026*; S-03 *15 May 2026*; S-04 *09-09-2026*, *31 SEP 2026* and *2 OFF 300*. Place one floor plan on S-04 and type *01 JAN 2026* INSIDE the plan (open the plan, not the sheet). Save | eight notes on sheets, one in the plan. Never one of the owner's working models |
| **CY2** | Arrange **Heron Sheet Dates Empty**, a second scratch project: three sheets S-01 to S-03, each with *GENERAL NOTES* and *09-09-2026* typed on it. Then the dates everywhere else: *09 SEP 2026* inside a floor plan placed on S-01, *22 JUL 2025* in a drafting view placed on no sheet, *15 May 2026* in S-02's Sheet Issue Date parameter. Save; leave both projects open | six notes on sheets, two in views, one date in a parameter |
| **CY3** | `python tools/batch-prove.py tools/jobs/list-dates-on-sheets-2026-10-10.yaml` | positive: `uniqueDateCount` 3, `dateList` *22-JUL-2025 - 2 note(s) on 2 sheet(s): S-01, S-02 \|\| 15-MAY-2026 - 2 note(s) on 2 sheet(s): S-02, S-03 \|\| 09-SEP-2026 - 1 note(s) on 1 sheet(s): S-01*, `sheetsRead` 4, `notesOnSheets` 8, `sheetsWithNoDate` *S-04*, **no 01-JAN-2026**. negative: `uniqueDateCount` 0, `dateList` empty, `sheetsRead` 3, `notesOnSheets` 6, `notesInViewsNotRead` at least 2. A version reading every view shows 09-SEP-2026 and 22-JUL-2025 there; one reading the other document shows all three |
| **CY4** | The second route: in **Heron Sheet Dates**, select all four sheets in the project browser and run `fragment --in "Heron Sheet Dates" extract-dates-from-text` | `datesFound 3 entry(ies)` and `sheetsWithNoDate 1 item(s)` - the same answer from the PROVEN card, which finds a sheet's notes with a collector scoped to the sheet where this one uses each note's owner view |
| **CY5** | CY3's positive again with S-01 selected | the same answer - the selection is not an input |
| **CY6** | `revit_read` with capability `LIST_DATES_ON_SHEETS` on *4355-BHVD-3D-50C10-BL001A*, Revit 2020 - read only, the observation that asked for this card, **not its proof** | `sheetsRead` 19, and every date in `dateList` one of the seven `FIND_DATES_IN_VIEWS` reported there on 2026-10-10. A date here it did not report is a finding against one of the two. The reply's length and how long it took |
| **CY7** | The signature | `python brain/heron_validate.py accept list-dates-on-sheets --by "Ajmal PS"`, on CY3's record, on the owner's word only, after CY4 |
| **CY8** | CY1 to CY4 on **Revit 2020** and **2027** | the two projects made again under the same titles from that release's own templates - a 2024 file does not open in 2020 - and the same answers. Compiled on all eight releases; what Revit DOES was measured on none |

**Routing, measured 2026-10-10 - not proof.** 46 sentences through `heron_retrieve.find`, the host's own
route, Revit 2020 filter, private scratch stores: the tree without this card (`489a6e63`, before the
card was written; the lexical store from a detached checkout of it) against the tree with it, on the
trained backend (`model`) and on the lexical one (the model imports blocked in the measuring script,
since a venv could not be made on that PC).

**Trained backend:**

- **12 change winner, every one to `LIST_DATES_ON_SHEETS`.** Three are the modeller's own words (the
  sentence, its clause *take the dates in that text from all sheets*, and *keep only unique dates*).
  Six are dates-on-all-sheets questions. Five went to `EXTRACT_DATES_FROM_TEXT`, which would have
  asked for a selection: *list all the dates on the sheets*, *what dates are on all the sheets*,
  *unique dates from every sheet*, *which issue dates are on our drawings*, *show me all dates written on
  sheets*. The sixth, *list every date typed on a sheet*, went to `CREATE_SHEET_LIST` - **a write**.
- **Recorded, not tuned:** *list all the text notes* (was `CREATE_TEXT_NOTE`) and *what text is on this
  sheet* (was `SET_SCHEDULE_APPEARANCE`) move from a **write** to this read, which answers only dates;
  *read the sheet issue date* moves from `EXTRACT_DATES_FROM_TEXT` to this card - both wrong for it,
  `READ_ELEMENT_PARAMETERS` reads a parameter, and this card's summary names it.
- **The first wording said "in the model"**, and took *find every typed date in the model* - claimed in
  `FIND_DATES_IN_VIEWS`' routing table, so `check-routing` reported it CLAIMED AND NOT REACHED - and
  *what dates are in the model* from that card. Without the phrase both go back, and every declared
  sentence of both PROVEN date cards reaches its own card by exact match.

**Lexical backend:**

- **8 change winner, every one to `LIST_DATES_ON_SHEETS`.** The three of the modeller's words; four
  dates-on-all-sheets questions (*list all the dates on the sheets* from `LIST_SHEETS`, *what dates are
  on all the sheets*, *unique dates from every sheet* and *show me all dates written on sheets* from
  `EXTRACT_DATES_FROM_TEXT`); and **one lost claim**: *what dates are on these sheets*, which
  `EXTRACT_DATES_FROM_TEXT`'s routing table claims - *these* means the selected sheets. By words this
  card leads by 0.162, by nearness it is second, 0.001 ahead of that card, and `find` puts it 1.2 fusion
  ranks clear. Two rewordings that cut *sheet* and *date* from the purpose and the identity narrowed it
  to a tie at best (*0.0 of one fusion rank apart*) and never sent it back, and one of them lost this
  card its own *take the dates in that text from all sheets*; the pull is the modeller's sentence and
  the capability's name, and neither is weakened to buy a rank.
  Declaring the sentence an utterance of `EXTRACT_DATES_FROM_TEXT` would settle it by exact match - an
  edit to a PROVEN card's words, outside `impl/` so its proof would not go stale, and left to the owner.
- No sentence moves to or from a write; *list all the text notes*, *what text is on this sheet* and
  *change the text in a note* reach `CREATE_TEXT_NOTE`, a write, **before and after alike** - not caused
  here and not fixed.

**The house tools, on the trained backend:** `check-routing --revit 2020` against its before-record: the
same three questions answered by a write, the same 95 routing-table claims not reached (the first wording
made it 96). Its keyword-route section - one half of the fusion, which the tool itself says is not what
the host calls - gained two lines, *the date is typed in a text note* (this card first by words) and
*every one of these in the model* (`PLACE_MEP_FITTING` first), lost one, and moved eleven others by a
rank or a few (*what are these* by 71); every one of them is a declared sentence, and the eight asked
through `find` - both new lines among them - reach their own cards by exact match, before and after
alike. `check-intrusion` does not put this card in its top 12.
