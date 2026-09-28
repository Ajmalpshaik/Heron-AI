# Needs checking — Group BD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BD - `link-family-parameter`: an element already in a family linked to a family parameter, or unlinked, and read back (2026-09-28)

**One new general tool, built on 2026-09-28 because nothing in the library could do the job and widening the
nearest one would have made it a different job.**
[`link-family-parameter`](../../brain/fragments/link-family-parameter/fragment.yaml) - LINK_FAMILY_PARAMETER,
FRG-PAR-025, PROVEN since 2026-09-29 - is the Family Editor's "Associate Family Parameter" button for every element a call
names: a `category` as Revit or the Properties palette names it ("Duct Connector", "Extrusion", a nested
family's name) or `all`, a `system` classification for connectors or `all`, and `links` as
`ElementParameter=FamilyParameter` pairs, `none` to unlink. A family parameter that does not exist, or holds
another kind of value, is refused naming both parameters and both kinds before anything changes; every link
is read back, before and after, on one line; one already as asked is "already linked" and not counted. It
compiles on every release from 2020 to 2027. Why a new tool and not ADD_FAMILY_CONNECTOR widened: [row
5b-251](../FRAGMENT-ISSUES.md).

**The first use was already done by hand.** The owner moved the fan coil unit's two duct connectors from
`Designed_Supply_Air_Flow` / `Designed_Return_Air_Flow` to `Actual_Supply_Air_Flow` /
`Actual_Return_Air_Flow` in the Family Editor on 2026-09-28, before the tool existed. So the tool READ that
state rather than making it, and every write below was rolled back.

**The working run, 2026-09-28 - the evidence behind BD2's draft, not a proof.** The family
`TRG_MECH_EQP_Fan Coil Unit_FCU_R0`, open in the Family Editor from "heron ai bulding", Revit 2024, Heron
session 27288, reached with `validate --in` so no run could land on the project:

1. **Read with no transaction open** - Revit refuses any change, so the tool can only report: the supply
   connector (id 930423) *"Flow already linked to Actual_Supply_Air_Flow (reads 0.0 L/s)"*, the return
   connector (id 930424) *"Flow already linked to Actual_Return_Air_Flow (reads 0.0 L/s)"*, changed 0.
2. **The D-30 legs, rolled back:** `Flow=Designed_Supply_Air_Flow` on the supply connector - *"Flow
   Actual_Supply_Air_Flow -> Designed_Supply_Air_Flow (reads 250.0 L/s)"*, changed 1; `Flow=Unit_Length` -
   refused, *"\"Unit_Length\" (Length) cannot drive \"Flow\" (Air Flow) ... The ones that could drive it:
   Actual_Return_Air_Flow, Actual_Supply_Air_Flow, Designed_Fresh_Air_Flow, Designed_Return_Air_Flow,
   Designed_Supply_Air_Flow."*, changed 0.
3. **Unlink, rolled back:** `category=Duct Connectors` (a plural), `system=all`, `Flow=none` - both
   connectors *"-> not linked"*, changed 2. Its negative, `Flow=Chilled_Water_Flow`, a pipe flow of the same
   storage type, refused as *"(Flow) cannot drive \"Flow\" (Air Flow)"*.
4. **A solid, rolled back:** `category=Extrusion`, `Material=Casing_Material` - 8 matched, 3 already linked,
   5 changed, each reading `MECH_EQP_FCU_Galvanised Steel Sheet`; `Material=Unit_Length` refused ONCE for
   all 8, naming them.
5. **Refused before anything changed, no transaction open:** `system=Exhaust Air`, with what the family has
   (*"1 Duct Connector (Return Air), 1 Duct Connector (Supply Air), 1 Electrical Connector (Power Balanced),
   8 Extrusion, 1 Pipe Connector (Return Hydronic), 1 Pipe Connector (Sanitary), 1 Pipe Connector (Supply
   Hydronic)"*); `system=Suply Air`; `Flow=Actual_Supply_Air_Flw`; `Flow` on the extrusions (*"What can be
   linked there: Extrusion End, Extrusion Start, Material, Visible."*); and the project, `--in "heron ai
   bulding"`, `notAFamily` true. `system=Hydronic Supply` found the supply pipe connector (id 930466),
   *"Flow already linked to Chilled_Water_Flow (reads 0.9 L/s)"* - Revit's word order, where its enum says
   SupplyHydronic.
6. **Read back after everything:** both connectors still on the Actual parameters, and the eight
   extrusions' Material links exactly as read before the first write - Casing_Material on 930353, 930391 and
   930402, none on 930439, CHWS, Drain, CHWR and Electrical on the other four. Nothing was saved, loaded or
   kept.

**2026-09-29, after Codex's review of [PR #356](https://github.com/Ajmalpshaik/Heron-AI/pull/356).** Four points,
all fixed before signing: a refusal from Revit part-way now says the call FAILED and is rolled back, not that
nothing was kept - the rollback is the host's and cannot be seen from the fragment; a category name is matched
exactly first, and a loose match that reaches two different sets of elements is refused; the same parameter
named twice in another case (`Flow=...; flow=...`) is refused; and the README's totals carry the date they
were derived. The code changed, so the proof was run again on the same family, session 27288: the positive
changed 1, `Unit_Length` refused, the unlink, the pipe-flow negative and the extrusion runs as on 2026-09-28,
`Flow=...; flow=...` refused, and the family read back exactly as found. Codex's second pass found two more,
also fixed: a blank `system` is refused rather than read as `all`, which on "Duct Connector" would have
relinked every connector; and a link that cannot be READ, before the change or after it, fails the call
instead of passing as "not linked". Run a third time on the family - the same answers, a blank `system`
refused, the family read back exactly as found - and **signed on the owner's word, as Ajmal PS, fingerprint
`ec38de660815c7c6`, with his own by-hand setting of the two links as the second route (BD1).** Codex's
second review also asked that the card not be PROVEN before that route was recorded; it was recorded first.

**Routing, measured rather than assumed.** The real `find` was asked 52 sentences on two scratch stores, one
built from the main checkout at `1e50db60`, one from this branch. The owner's two phrasings reach
LINK_FAMILY_PARAMETER by identity; on main the first reached SET_AIR_TERMINAL_FLOW, which writes a VALUE in a
project, and the second REPORT_CONNECTORS. No sentence that reached a read on main reaches this writer, and
none that reached ADD_FAMILY_CONNECTOR, SET_AIR_TERMINAL_FLOW or REPORT_CONNECTOR_LOADS by identity moved.
**Still wrong, and not made worse:** "which family parameter drives this connector's flow" and "which
parameter is the connector flow associated with" are questions, and reach this writer - on main they reached
SET_AIR_TERMINAL_FLOW and ADD_FAMILY_CONNECTOR, also writers. No read in the library answers them.

| # | Check | Expected |
|---|---|---|
| ~~**BD1**~~ | **By hand, the second route.** In the Family Editor, select the supply duct connector, press the button at the end of Flow's row in Properties, and read which parameter the Associate Family Parameter dialog has selected; then the return connector | **DONE 2026-09-29, by the owner's own hand-setting rather than a second look.** He re-linked both connectors with that dialog on 2026-09-28, before the tool existed - supply to `Actual_Supply_Air_Flow`, return to `Actual_Return_Air_Flow` - and confirmed it in the chat on 2026-09-29; the tool read exactly those two before any proof ran and after every proof. Written into the proof's `second_route`, which also says what it cannot see: a rolled-back change leaves nothing in the dialog to look at. Codex's second review asked for this route before PROVEN |
| ~~**BD2**~~ | The two legs for `link-family-parameter` ([D-30](../DECISIONS.md)) - **signed 2026-09-29.** With the family open in the Family Editor and `HERON_CLIENT_ID=ajmal-pc` set: `python tools/batch-prove.py tools/jobs/link-family-parameter-fcu-2026-09-28.yaml --dry-run`, then without `--dry-run`, then `python brain/heron_validate.py draft link-family-parameter --from brain/proof-drafts/runs/link-family-parameter.json`. Write BD1 into the draft's `second_route`, sign with `python brain/heron_validate.py accept link-family-parameter --by "Ajmal PS"` and set `heron-status: PROVEN` | `PASS` on `changed`: 1 moving the supply connector's Flow to `Designed_Supply_Air_Flow`, 0 and refused by kind for `Unit_Length`; Revit reports the group ROLLED BACK both times, and BD1's dialog still shows `Actual_Supply_Air_Flow` afterwards. **DONE 2026-09-29** - run again after both of Codex's review passes, signed as Ajmal PS on his word, fingerprint `ec38de660815c7c6`. The job file re-runs it any time; a run whose fingerprint differs is about other code |
| **BD3** | **From a chat, once merged.** The card is NEW, so the shared store needs `python brain/heron_scope.py --rebuild` run from the main checkout once it is fast-forwarded - a refresh never adds a row. Then with the family in front: `heron_lookup` "link the supply connector flow to Actual_Supply_Air_Flow", and `revit_change LINK_FAMILY_PARAMETER` with `category=Duct Connector`, `system=Supply Air`, `links=Flow=Actual_Supply_Air_Flow` | The lookup names LINK_FAMILY_PARAMETER by identity. The change answers `changed 0`, `alreadyLinked 1` and prints `linkReport` WHOLE - *"Duct Connector (Supply Air) id 930423: Flow already linked to Actual_Supply_Air_Flow ..."* - and adds nothing that changes the family to Revit's undo list |
| **BD4** | **Revit 2020 and Revit 2027.** A parameter's kind is read through `Definition.ParameterType` and `LabelUtils.GetLabelFor` on 2020 and 2021, and `GetDataType` and `GetLabelForSpec` from 2022 - only 2024 has run. On a copy of any family with a duct connector, in each release: the no-transaction read, and `Flow=` a length parameter. **NEEDS REAL REVIT 2020 and 2027** | The read answers "already linked" or "not linked" per connector; the length refused naming Length and the connector's flow kind in that release's words. A refusal that names a kind that could not be read means the reflection missed that release |
