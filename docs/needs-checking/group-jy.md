# Needs checking — Group JY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group JY - lining added to, changed on and taken off ducts and their fittings: `SET_DUCT_LINING` (2026-11-01)

Asked for in the modeller's words: *"add 25 mm lining to these ducts"*, *"line this supply duct and its
fittings"*, *"remove the lining from this duct"*, *"change the lining thickness to 50"*. Nothing in Heron
put lining on, changed it or took it off - `SELECT_BY_INSULATION` and `CHECK_INSULATION_CLEARANCE` only
read it, and `SET_MEP_INSULATION` and `REMOVE_MEP_INSULATION` act on insulation alone.
`brain/heron_retrieve.py` on each sentence offered those, `CREATE_DUCT` and `FIT_MEP_JOINTS`.

| What | Where | What it does |
|---|---|---|
| `SET_DUCT_LINING` (MODIFY) | [`brain/fragments/set-duct-lining`](../../brain/fragments/set-duct-lining/fragment.yaml) | Revit's Add, Edit and Remove Lining as one job on the selected ducts, duct fittings and duct accessories: a thickness above 0 lines an unlined element or changes a lined one in place, 0 takes the lining off. Per element, what was added, changed or removed and what was skipped and why; pipes, flex duct and insulation named and left |

**Why one card and not three.** The three buttons set one value of a duct - its lining thickness - and
the modeller asks for that value. The insulation pair is two cards because `SET_MEP_INSULATION` is PROVEN,
insulation-only and replaces by delete-and-create; nothing here touches it.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). `DuctLining.Create`, `DuctLiningType`,
`InsulationLiningBase.GetLiningIds`, `HostElementId`, `Thickness` and `IsValidThickness`,
`Element.GetTypeId` and `ChangeTypeId`, `ElementType.FamilyName`, `Document.Delete`,
`Element.GetDependentElements` and `SubTransaction` were read off the shipped assemblies of all eight with
`tools/api-surface --members`. **Nothing here has run in Revit.** The card is DRAFT. Its risk is MODIFY; a
thickness of 0 deletes lining elements, and the confirmation Article 7 asks for a delete is Revit's own
box ([D-142](../DECISIONS.md)), so the card takes no `confirm`.

**Not known from the assemblies, and recorded here rather than guessed** (JY9 to JY11 measure them):
whether `DuctLining.Create` accepts a duct ACCESSORY as Revit's ribbon does (JY9); whether Revit lines a
flex duct at all - the card names flex duct and leaves it, which is safe and may be narrower than Revit
(JY10); and whether `ChangeTypeId` on a lining element changes its type in place, keeping its id (JY11).

**The model - Snowdon-scratch_ajmal.al in Revit 2024**, the owner's scratch copy of the sample HVAC
model `REMOVE_TAGS` was proved on. **Whether its ducts carry lining, and which Duct Lining types it holds,
are not known here.** Every run rolled back. The job file is
[`tools/jobs/set-duct-lining-jy-2026-11-01.yaml`](../../tools/jobs/set-duct-lining-jy-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its room and lining type are left for JY1 to read** and
are marked in the file; until they are filled in, no room answers to it and the setup should stop there,
before the lining runs - expected, NOT RUN. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JY0** | *Open.* Snowdon-scratch_ajmal.al open and in front in Revit 2024, **nothing selected**, Changes on at the ribbon | The title bar reads *Snowdon-scratch_ajmal.al* |
| **JY1** | *Read the arrangement.* `select-by-insulation` Ducts, `wantInsulated` false, `countLiningAsInsulated` true; the Duct Lining types read off Revit's Add Lining box | A room holding **a few bare ducts** - under twenty - and a lining type; write both into the job file. **If the model has no Duct Lining type, say so here and stop**: making one is not this group's to do |
| **JY2** | *Line, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the bare ducts at thickness 0 - `added` 0, `removed` 0, each named *had no lining*, no lined duct touched. Positive: `added` equal to the ducts, a line each *lining added, 25 mm of <type>*; `select-by-insulation` with lining counted finds them lined in the same rolled-back group |
| **JY3** | *Fittings.* Three bare ducts and the fittings between them, selected by hand, `validate set-duct-lining --write` at 25 | Each fitting a line of its own, lined |
| **JY4** | *Edit in place.* Lined ducts, selected by hand, at 50 with `liningType` left out | `changed` equal to them, *changed from 25 mm ... to 50 mm*, the type unchanged and the lining's element ids the same as before |
| **JY5** | *Remove.* Lined ducts, selected by hand, at 0. If the model has none, JY2's positive arranged inside the same rolled-back group first | `removed` equal to their lining elements; the ducts still there at the same size, and any insulation on them still on them |
| **JY6** | *Pipes.* A few pipes and a pipe fitting at 25 | `added` 0, each named *A pipe has no lining in Revit*; refused saying so |
| **JY7** | *Insulation is never touched.* One piece of duct insulation Tab-selected, at 0 | `removed` 0, named *Insulation, not lining - left alone*, still on the duct |
| **JY8** | *A type the model has not got.* Bare ducts at 25, `liningType` a name no lining type has | `added` 0, refused naming the model's lining types |
| **JY9** | *An accessory.* A duct accessory - a damper - at 25 | **Record** whether it is lined, or named *NOT lined - Revit refused* and why |
| **JY10** | *Flex duct.* One flex duct at 25 | Named *Flex duct - not lined here*. **Record** whether Revit's own ribbon offers Add Lining on a flex duct; if it does, the card is narrower than Revit and that is a row of its own |
| **JY11** | *A type change.* Lined ducts at their own thickness with another `liningType` | **Record** whether each reads back with the new type and the same element id |
| **JY12** | *Kept, through `try-once`, on the modeller's yes.* Line, as JY2, on a saved copy | Keep or Discard; afterwards a duct schedule with Lining Thickness shows 25 mm on exactly the ducts `report` names - the second route |
| **JY13** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The four sentences reach this card by identity; *"remove the insulation from these ducts"* still reaches `REMOVE_MEP_INSULATION`. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept set-duct-lining --by
"Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `heron_brain.lookup` on a scratch
store each, against this tree with and without the card. The four sentences above reach this card by
identity; without it they reached `SET_MEP_INSULATION` (*"add 25 mm lining to these ducts"*, *"change the
lining thickness to 50"*), `CREATE_DUCT` (*"line this supply duct and its fittings"*) and
`REMOVE_MEP_INSULATION` (*"remove the lining from this duct"* - the item [Group JX](group-jx.md) and
[row 5b-1358](../fragment-issues/section-5b-rows-401-425.md) recorded open, closed by this card). Ten
undeclared lining phrasings tried beside them - *"add lining"*, *"remove the lining"*, *"change the lining
type"*, *"make the lining 50 mm"* and six more - reach this card ranked; before it each reached an
insulation card, `CHANGE_ELEMENT_TYPE`, `READ_ELEMENT_TABLE` or `SET_MEP_SYSTEM_TYPE`. **No sentence that
is not about lining moved**: the insulation cards' declared and undeclared sentences, *"remove these"*,
*"add a duct"* and *"add a duct fitting"* reach the cards they reached before. `tools/check-routing.py`
on a fresh store: no question answered by a write, *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 65 before
and after, the same rows; four declared sentences now rank this card first on the keyword route alone -
*"add a duct"*, *"add a duct fitting"*, *"show only the supply ducts in it"* and *"add duct insulation and
lining to those filters"* - and each still reaches its own card by identity. `SET_MEP_INSULATION` (PROVEN)
and `REMOVE_MEP_INSULATION` name this card in their routing tables; the edit is outside `impl/`, and
`SET_MEP_INSULATION`'s fingerprint still equals its proof's (d5645b81e8af4f08).
