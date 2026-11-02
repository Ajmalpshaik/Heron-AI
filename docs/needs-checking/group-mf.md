# Needs checking — Group MF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MF - Manage > Structural Settings and Structure > Reinforcement Settings - read both dialogs, and change either for the whole model, read back before and after: `REPORT_STRUCTURAL_SETTINGS`, `SET_STRUCTURAL_SETTINGS`, `SET_REINFORCEMENT_SETTINGS` (2026-11-02)

Asked for in the modeller's words: *"what are the structural settings"*, *"set the beam symbolic cutback
distance to 50 mm"*, *"set the brace symbol to show above and below"*, *"change the column connection symbol
plan offset"*, *"what are the reinforcement settings"*, *"turn on rebar shape defines hooks"*, *"set the rebar
rounding to 5 mm"*, *"include hooks in rebar shape definition"*. Nothing in Heron read or changed either
dialog: `brain/heron_retrieve.py` on a scratch store sent the two questions to `SET_VIEW_UNDERLAY` and
`PLACE_AREA_REINFORCEMENT` (writes) without the host's question flag, and the changes to
`SET_FRAMING_END_EXTENSION`, `SET_VIEW_UNDERLAY`, `PLACE_STRUCTURAL_CONNECTIONS`, `LIST_REBAR_TYPES` and
`CREATE_REBAR_BAR_TYPE`. `LIST_REBAR_TYPES` (PROVEN) reads one Reinforcement Setting beside the rebar types
and was left untouched; `SET_FRAMING_END_EXTENSION` shortens chosen beams, which is not the symbolic cutback.

| What | Where | What it does |
|---|---|---|
| `REPORT_STRUCTURAL_SETTINGS` (READ) | [`brain/fragments/report-structural-settings`](../../brain/fragments/report-structural-settings/fragment.yaml) | Reads **both dialogs** in the names the two writes take: symbolic cutback distance (brace, beam/truss, column), brace parallel line offset, show brace above / below with their symbols, kicker brace symbol, boundary condition symbol spacing and the four boundary condition symbols, loads display scaling (2024+), steel boundary setback (2026+); hooks and end treatments in the rebar shape definition, host structural rebar, rebar presentation in view and section, rebar and fabric rounding with methods and the unit Revit holds them in, varying length suffix, numbering. Says the Connection Symbols are not readable through the API |
| `SET_STRUCTURAL_SETTINGS` (ADMIN) | [`brain/fragments/set-structural-settings`](../../brain/fragments/set-structural-settings/fragment.yaml) | A table of `name=value` - cutbacks, parallel line offset and symbol spacing in millimetres, show brace above / below / both, the brace, kicker and boundary condition symbol types by name, loads display scaling (2024+). A **connection symbol** is refused by name - no API on any release - and sent to the dialog |
| `SET_REINFORCEMENT_SETTINGS` (ADMIN) | [`brain/fragments/set-reinforcement-settings`](../../brain/fragments/set-reinforcement-settings/fragment.yaml) | A table of `name=value` - hooks and end treatments in the rebar shape definition, host structural rebar, rebar presentation in view / section, rebar and fabric total / segment length rounding in millimetres (`rebar rounding` = both) and their methods, the varying length suffix |

Both writes: everything parsed before anything is written, so a bad entry changes nothing; a setting **Revit**
refuses is answered with Revit's own words, every setting already written in that call put back, `refused`
says NOTHING WAS CHANGED; a put-back that fails throws and the caller's transaction rolls back. `before` and
`after` read every setting back; one that reads back different throws.

**Why ADMIN.** Graded as `SET_PROJECT_UNITS` and `SET_AREA_VOLUME_COMPUTATIONS` are
([D-106](../DECISIONS.md)): both dialogs are how the project is set up for everybody - every plan view's
framing symbols, which Rebar Shape every new bar is matched to, the length every rebar schedule prints. So
neither write can be sent by `batch-prove` or `try-once`; each is proved by name with the owner's Admin
switch on.

**What is done and what is not.** All three compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member was read off the
shipped assemblies of all eight with `tools/api-surface --members`; the per-release members are named in
each card's `compatibility-note`. **Nothing here has run in Revit.** All three are DRAFT. **Only Revit can
settle**, and MF2-MF4 record: whether the cutback distances and parallel line offset are model lengths in
feet (as converted) rather than paper distances; which unit the rounding manager names and whether the
rounding values are held in it; Revit's words when it refuses *include hooks in rebar shape definition* in a
model with rebar; and what a family document gives the read.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `65062d4` and of
this change, Revit 2024, with and without the host's question flag (the `READ` ceiling), over every declared
utterance and routing-table sentence of a live card and fourteen probes (4,459 sentences). Every sentence the
three cards declare reaches its card by identity; the two questions reach `REPORT_STRUCTURAL_SETTINGS` with
and without the flag. No other card's declared sentence lost the card it reached. Four made-up question
probes reach the writes without the flag (the read with it), and *"put it back"* flipped again between two
cards that do not claim it - [row 5b-1433](../fragment-issues/section-5b-rows-401-425.md), OPEN.

**The model - a scratch project from the metric structural template, any release.** Never a working model.
Every run below is rolled back except MF1's arrangement. The job file is
[`tools/jobs/structural-settings-mf-2026-11-02.yaml`](../../tools/jobs/structural-settings-mf-2026-11-02.yaml)
(the read only; `--dry-run` reads it back and sends nothing). Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MF0** | *Open.* Any release 2020 to 2027, a new project from the metric structural template saved as "Heron MF Structure"; a beam family saved as "Heron MF Beam Family"; Changes and Admin on at the ribbon | The title bar names the scratch project |
| **MF1** | *Arrange, and KEEP it.* One beam, one brace and one column on Level 2. Save. A copy "Heron MF Rebar" with one bar in a concrete beam (`PLACE_BEAM_REBAR`). `REPORT_STRUCTURAL_SETTINGS` in each - write down every value | The values recorded, and the same in the two dialogs |
| **MF2** | *The read's D-30 pair.* The job file | Positive: both strings read as the dialogs show. Negative (the family): **record** what Revit gives |
| **MF3** | *SET_STRUCTURAL_SETTINGS, NEGATIVE FIRST.* `validate set-structural-settings --in "Heron MF Structure" --allow-publish --write --set "settings=beam/truss cutback=50" --negative-set "settings=beam/truss cutback=50 ft"`; then `show brace=above and below`, `column connection symbol plan offset=10`, a name twice, `loads display scaling=yes` | Negative: changed 0, *not a distance in millimetres*, settings as MF1. Positive: changed 1, `after` *beam/truss cutback 50 mm*; the beam's line at Coarse stops 50 mm short. The connection symbol refused by name. **Record** whether 50 mm reads back as 50 mm |
| **MF4** | *SET_REINFORCEMENT_SETTINGS, NEGATIVE FIRST.* `validate set-reinforcement-settings --in "Heron MF Structure" --allow-publish --write --set "settings=rebar rounding=5" --negative-set "settings=rebar rounding=5 in"`; then in "Heron MF Rebar" `include hooks in rebar shape definition=<the opposite of MF1's>` | Negative: changed 0, nothing changed. Positive: both rebar roundings *5 mm*, and Structure > Reinforcement Settings > Rounding shows 5 mm - **record** the unit the read names. The rebar model: **record** Revit's refusal, and that nothing changed |
| **MF5** | *The read's tracking negative.* MF3's and MF4's positives as a rolled-back setup, then `REPORT_STRUCTURAL_SETTINGS` | Exactly beam/truss cutback 50 mm and the two rebar roundings 5 mm changed, every other value as MF1 |
