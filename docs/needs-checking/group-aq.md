# Needs checking — Group AQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-25 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AQ - `set-air-terminal-flow`: each air terminal's design flow, written through the parameter its duct connector names (2026-09-25)

**One fragment from one sitting on 2026-09-25, in Project1 on Revit 2024, and it is DRAFT.**
[`set-air-terminal-flow`](../../brain/fragments/set-air-terminal-flow/fragment.yaml) - SET_AIR_TERMINAL_FLOW,
FRG-MEP-054 - sets each air terminal's design flow, in litres per second, from a file of `Id,Flow` rows. It
compiles on every release from 2020 to 2027. It has run once, in a chat, on the owner's working model, and
**a chat run is not a proof**: no negative case was run and no fingerprint was taken
([D-30](../DECISIONS.md)), so no row below is closed by it.

**Why it exists.** An air terminal carries two parameters called "Flow" - the family's own, which the duct
connector takes its flow from, and a built-in one Revit works out from the connector. So since
[row 5b-203](../FRAGMENT-ISSUES.md) every writer that looks a parameter up by name refuses "Flow" on an air
terminal, as [D-54](../DECISIONS.md) §3 says it must. This fragment asks the terminal's one duct connector
which family parameter its flow is tied to, writes that parameter matched by id, reads every value back after
one regeneration, and puts back any terminal whose value did not hold.

**The working run, 2026-09-25 - a chat run, not a proof.** Project1, saved as "heron ai bulding", Revit 2024,
Changes on, with a file of 90 rows that splits each space's specified supply and return airflow over its
diffusers: 89 changed, 1 in `alreadyThatFlow` (id 927789), 0 refused, 0 put back, 0 in `builtInDisagrees`.
Then SELECT_BY_NUMERIC_PARAMETER on category `Spaces` found all 15 spaces' Actual Supply Airflow and Actual
Return Airflow equal to their specified values - supply 330 x4, 335 x8, 395, 400 and 455 L/s; return 295 x4,
300 x8, 355, 360 and 410 L/s. That is one Heron read checking a Heron write, not Revit's own Properties, so
AQ5's second route is still owed.

**The inputs are files, and three of them exist already.** They sit in `proof-aq\` in the scratchpad of the
session that did the run - the full path is in the job file. `aq-design-flows.csv` is that run's own file,
byte for byte; `aq-away.csv` is the same 90 ids with every flow 10 L/s higher except 927789's, which stays at
110; `aq-bad-rows.csv` is AQ3's. Save As keeps element ids, so these rows name a test copy's own diffusers.
**A test copy made today already holds the design flows**, so the design file on its own would change
nothing there - which is why AQ1's positive hands in `aq-away.csv`. The fourth file, `aq-walls.csv`, can only
be made at the sitting: AQ1 says how.

Every row runs on a TEST COPY of "heron ai bulding" - never the working model.

| # | Check | Expected |
|---|---|---|
| **AQ1** | The proof ([D-30](../DECISIONS.md)). First make `aq-walls.csv` beside the other three: the test copy's Level 1 walls, one row each - export their ids with EXPORT_PARAMETERS_TO_CSV after FILTER_ELEMENTS_BY_CATEGORY `category=Walls`, `levelId=Level 1`, keep its `ElementId` column, head the file `Id,Flow` and put 100 on every row. Then, with `HERON_CLIENT_ID=ajmal-pc` set and the test copy in front, `python tools/batch-prove.py tools/jobs/set-air-terminal-flow-project1-2026-09-25.yaml --dry-run`, then the same without `--dry-run`. Then read the draft, sign it with `python brain/heron_validate.py accept set-air-terminal-flow --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive - the Level 1 air terminals and `aq-away.csv`: `changed` 89, `alreadyThatFlow` 927789 alone, and `refused`, `noFlowParameter` and `builtInDisagrees` empty. The negative - the Level 1 walls and `aq-walls.csv`: `changed` 0, and every wall named in `notATerminal`. `NEG NOT EMPTY` means it wrote to something that is not an air terminal. **Read the negative's findings as well as its verdict:** a missing `aq-walls.csv` also comes back `changed` 0, with *"There is no file at"* in its findings, and that negative proved nothing |
| **AQ2** | The positive's check, KEPT, with Changes on, in a chat: `revit_read` FILTER_ELEMENTS_BY_CATEGORY with `category=Air Terminals` and `levelId=Level 1`, then `revit_change` SET_AIR_TERMINAL_FLOW with `csvPath` the full path of `aq-away.csv`; then the same two calls with `aq-design-flows.csv`; then the design file once more. After the first two, `revit_read` SELECT_BY_NUMERIC_PARAMETER on category `Spaces`, for Actual Supply Airflow and for Actual Return Airflow | The first call: 89 changed, 927789 in `alreadyThatFlow`, and no space's Actual Supply or Return Airflow equal to its specified value any more. The second: 89 changed, 927789 in `alreadyThatFlow`, and all 15 spaces equal again - supply 330 x4, 335 x8, 395, 400, 455 L/s; return 295 x4, 300 x8, 355, 360, 410 L/s - **checked by the spaces, never by the fragment's own count.** The third: 0 changed and all 90 in `alreadyThatFlow` - a re-run writes nothing. One Ctrl+Z takes back each kept call |
| **AQ3** | The bad rows, in the same chat: the same filter, then SET_AIR_TERMINAL_FLOW with `aq-bad-rows.csv` - 927790 at -50, 927791 at "abc", 927820 twice, and 927821 | `badRows` 3, each named in the findings by its line - the -50, the "abc", and the second 927820 row as "in the file twice - the first row stands" - with nothing written for them, so 927790 and 927791 keep their flows. The good rows are still written: `changed` 2, 927820 at its first row's flow and 927821 at its row's. One Ctrl+Z takes it back |
| **AQ4** | A terminal whose connector's Flow Configuration is Calculated. Arrange it on the test copy: Edit Family on one diffuser, select its duct connector, set Flow Configuration to Calculated, save the family under a NEW name and Load into Project, so that no family in the model is overwritten - and look at the project's materials afterwards, because loading families has reset them before. Change ONE diffuser to the new family's type with CHANGE_ELEMENT_TYPE, then run SET_AIR_TERMINAL_FLOW with a file holding one row, for that diffuser | `changed` 0, that diffuser named in `noFlowParameter`, and its Flow in Properties the same before and after: a Calculated flow is tied to no family parameter, so there is nothing to write. **If it is changed instead**, `GetAssociateFamilyParameterId` does name a parameter for a Calculated connector, and the fragment's purpose and its `tests/cases.yaml` are wrong about it - record that in [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) section 5b |
| **AQ5** | The second route - Revit's own Properties palette, after AQ2's second call. Select a diffuser and read Flow under Mechanical - Flow; then select its space and read Actual Supply Airflow, or Actual Return Airflow for a return diffuser | The diffuser's Flow is its row in `aq-design-flows.csv`, and the space's actual airflow is its specified value - Revit's own reading, beside Heron's |
