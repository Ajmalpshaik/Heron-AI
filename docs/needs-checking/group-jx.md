# Needs checking — Group JX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group JX - insulation taken off ducts, pipes and their fittings: `REMOVE_MEP_INSULATION` (2026-11-01)

Asked for in the modeller's words: *"remove the insulation from these ducts"*, *"take the insulation off
these pipes and fittings"*, *"strip lagging from this run"*. Nothing in Heron removed insulation -
`SET_MEP_INSULATION` deletes the old layer only to put a new one on, and takes ducts and pipes alone;
`SELECT_BY_INSULATION` only reads; and `brain/heron_retrieve.py` on each sentence offered those two, the
clearance check, an offset and a category select.

| What | Where | What it does |
|---|---|---|
| `REMOVE_MEP_INSULATION` (MODIFY) | [`brain/fragments/remove-mep-insulation`](../../brain/fragments/remove-mep-insulation/fragment.yaml) | Revit's Remove Insulation on the selected ducts, pipes, fittings, accessories and flex - the insulation elements only, never the host and never lining; per host, how many went or that it had none |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). `InsulationLiningBase.GetInsulationIds`
and `HostElementId`, `Document.Delete(ICollection<ElementId>)`, `Element.GetDependentElements`,
`ViewSpecific`, `SubTransaction` and the eight duct and pipe categories were read off the shipped
assemblies of all eight with `tools/api-surface --members`. **Nothing here has run in Revit.** The card is
DRAFT. Its risk is MODIFY, as every delete in the library is graded; the confirmation Article 7 asks for
a delete is Revit's own box ([D-142](../DECISIONS.md)), so the card takes no `confirm`.

**Not known from the assemblies, and recorded here rather than guessed** (JX8 and JX9 measure them):
whether `GetDependentElements` on an insulation element names the tags on it, so that a tagged layer is
removed rather than put back (JX8); and whether `Document.Delete` on insulation ever hands back anything
beyond the insulation and its tags - the fragment puts that host back and names it if so (JX9).

**The model - Snowdon-scratch_ajmal.al in Revit 2024**, the owner's scratch copy of the sample HVAC
model `REMOVE_TAGS` was proved on. **Whether its ducts carry insulation is not known here.** Every run
rolled back. The job file is
[`tools/jobs/remove-mep-insulation-jx-2026-11-01.yaml`](../../tools/jobs/remove-mep-insulation-jx-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its room is left for JX1 to read** and is marked in the
file; until it is filled in, no room answers to it and the setup should stop there, before the remove
runs - expected, NOT RUN. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when
two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JX0** | *Open.* Snowdon-scratch_ajmal.al open and in front in Revit 2024, **nothing selected**, Changes on at the ribbon | The title bar reads *Snowdon-scratch_ajmal.al* |
| **JX1** | *Read the arrangement.* `select-by-insulation` Ducts, `wantInsulated` true and then false, `countLiningAsInsulated` false | How many ducts are insulated and how many bare. Find a room holding **a few of each** - under twenty in all - and write its name into the job file. **If the model has no insulated ducts, say so here and stop**: arranging one by editing the model is not this group's to do |
| **JX2** | *Remove, the D-30 positive.* The job file's job | `removed` equal to the insulation on the insulated ducts of that room; `report` a line per duct *N insulation element(s) removed*; `select-by-insulation` in the same rolled-back group finds them bare |
| **JX3** | *Its negative - THE FIRST CASE TO RUN.* The bare ducts of the same room | `removed` 0, *None of the N ... had insulation on it*, each named as having none; the insulated ducts of that room still insulated |
| **JX4** | *Fittings.* Three insulated ducts and the insulated fittings between them, selected by hand, `validate remove-mep-insulation --write` | Each fitting a line of its own with its insulation removed. If the fittings carry none, record that |
| **JX5** | *Lining is never touched.* A lined duct with no insulation, selected by hand. If the model has none, record that - do not add lining to arrange it | `removed` 0, the duct named as having no insulation, and its lining still on it |
| **JX6** | *Insulation picked directly.* One piece of duct insulation Tab-selected on its own | `removed` 1, the line naming the duct it wrapped; the duct still there |
| **JX7** | *A wrong selection.* Four walls and an air terminal | `removed` 0, each named *cannot carry insulation, left alone* |
| **JX8** | *A tagged layer.* An insulated duct whose insulation carries a tag, if the model has one | **Record** whether the layer is removed with the tag counted, or put back as *Revit would also have deleted 1 other element(s)*. The second is safe and wrong, and is a row of its own |
| **JX9** | *What else Delete takes.* Read the run record of JX2 | **Record** any host put back with *would also have deleted*, and what Revit named |
| **JX10** | *Kept, through `try-once`, on the modeller's yes.* Remove, as JX2, on a saved copy | Keep: Revit's Delete box first ([D-142](../DECISIONS.md)), then Keep or Discard; afterwards a section through one duct shows no wrap - the second route |
| **JX11** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The three sentences reach this card by identity, and *"remove these"* reaches `DELETE_ELEMENTS`. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept remove-mep-insulation --by
"Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here. The three sentences above reach this card
by identity. *"remove these"*, which `DELETE_ELEMENTS`' routing table claims, reached `REMOVE_TAGS` ranked
before this card existed and this card once it did; it is declared on `DELETE_ELEMENTS` now and reaches it
by identity ([row 5b-1358](../fragment-issues/section-5b-rows-401-425.md)). *"remove insulation"*, *"strip
the insulation"* and *"remove the pipe insulation"* reach this card ranked; without it they reached
`SET_MEP_INSULATION`, and *"delete the insulation"* `DELETE_ELEMENTS`. **Two sentences nobody has
declared now reach this card ranked and would do the wrong thing here**, recorded rather than answered by
inventing utterances: *"remove the lining from this duct"* (before: `SELECT_BY_INSULATION`) - this card
never removes lining, so on a lined duct it removes nothing, but on a duct with both it would take the
insulation; and *"remove these fittings"* (before: `MEASURE_FITTING_AREA`) - it would take the fittings'
insulation, not the fittings. A DRAFT is refused by `revit_change` ([D-117](../DECISIONS.md)), and either
would be asked in Revit's Delete box first. `DELETE_ELEMENTS` and `SET_MEP_INSULATION` are PROVEN; the
edit to each is outside `impl/`, so no fingerprint moved.
