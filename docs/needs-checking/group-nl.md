# Needs checking — Group NL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NL - cards that were PROVEN, change the model and could report a wrong result as a success, repaired in code and back to DRAFT: `SET_ROUTING_PREFERENCE` version 2 and `ADD_PROJECT_PARAMETER` version 3 (2026-11-11)

A triage on 2026-11-11 named PROVEN cards that change the model and can do it wrongly while the answer
reads as a success. The owner's instruction that day: *"if you are finding any fragment's mistake, even
though it is proven, you can change that one."* Each card below was repaired in code the same day, in a
cloud session with no Revit, and is **DRAFT again** - its C# moved, so its proof no longer describes it
and `revit_change` refuses it until it is proved again ([D-30](../decisions/D-30.md),
[D-117](../decisions/D-117.md)). Each card keeps its old proof block as the record of what was signed.

| Card | Where | Rows | What changed | Held without Revit by |
|---|---|---|---|---|
| `SET_ROUTING_PREFERENCE` version 2 (MODIFY) | [`brain/fragments/set-routing-preference`](../../brain/fragments/set-routing-preference/fragment.yaml) | [5b-1120](../fragment-issues/section-5b-rows-401-425.md), [5b-1549](../fragment-issues/section-5b-rows-401-425.md) | A fitting is named "Family: Type", or by its type name alone when one loaded family has it; a type name two families share is refused and each is listed. The rule swap sits in a SubTransaction, rolled back when Revit refuses it or it does not read back | The name rule: [`tests/test_binding_note.py`](../../tests/test_binding_note.py) compiles the fragment's own lines and asks them the row's names - seen to fail on version 1's rule. **Not** which parts a project holds, and **not** that the SubTransaction puts a removed rule back |
| `ADD_PROJECT_PARAMETER` version 3 (ADMIN) | [`brain/fragments/add-project-parameter`](../../brain/fragments/add-project-parameter/fragment.yaml) | [5b-1127](../fragment-issues/section-5b-rows-401-425.md) | A shared parameter file that is not at the path, or a parameter the file's group does not hold, is refused naming what was not found, and nothing is written - unless the new input `createMissing` is true | Nothing but the compiler. A file on disk and Revit's reading of it need the PC |

**What is done and what is not.** Every card compiles on each release it declares, 2020 to 2027, with the
whole library ([`tools/check-fragments-compile.py`](../../tools/check-fragments-compile.py), 2026-11-11,
a Linux container), and every Revit member added is in its card's compatibility note, read with
`tools/api-surface --members`. **Nothing here has run in Revit.**

**The model - the scratch copies the 2026-11-02 rounds used, never a working project.** Revit 2020, with
`P1102 MEP` open and in front: the scratch copy of `Samples\rme_advanced_sample_project.rvt` under
`%TEMP%\heron-work\prove-1102`. Every line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>`
when two Revits are connected, and `< /dev/null`.

**One job file, one sitting:** [`tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml`](../../tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml).

```bash
python tools/batch-prove.py tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml --dry-run
python tools/batch-prove.py tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml --session <pid>
```

The dry run was read on 2026-11-11 with no Revit connected, so it says only that the file is runnable as
written. **It does not catch a wrong name** - the job file's header says which values are an assumption
of what the sample holds, and how to read the real ones first.

| # | Run | Look for |
|---|---|---|
| **NL0** | *Read first, change nothing.* In `P1102 MEP`: `SELECT_TYPES` with `categories=Ducts`, then `REPORT_ROUTING_PREFERENCES` on the duct type the job names (`Rectangular Duct: Mitered Elbows / Tees`); then `SELECT_TYPES` with `categories=Duct Fittings`. Write down what Elbows rule 0 points at and its size window; pick another loaded ELBOW; check that `Standard` is the type name of two or more loaded families. Put the real names in the job file | The job's `partName` is an elbow rule 0 does **not** point at, written `Family: Type`; the negative's name is one two families share |
| **NL1** | *The D-30 pair for [5b-1120](../fragment-issues/section-5b-rows-401-425.md), rolled back.* The job's first entry. Positive: `partName` the other elbow as `Family: Type`. Negative: the same type and rule, `partName=Standard` | Positive: `rulesChanged 1`, `findings` naming the old and the new elbow as `Family: Type` with the size windows carried across. Negative: `rulesChanged 0`, `typesTouched 0`, `findings` saying the name does not say which one is meant, and `nearMisses` holding each family that has it. **To the first, version 1 said "No Elbows part is called ..."; to the second, it bound whichever the collector met first.** Afterwards the type's Routing Preferences dialog reads what NL0 wrote down |
| **NL2** | *The other spellings, rolled back, by name.* `validate --write` with the same setup: `partName` with a space each side of the colon, as `SELECT_TYPES` prints it; then the elbow FAMILY's name alone | The first finds the same part. The second changes nothing and lists that family's types in `nearMisses`, each `Family: Type` |
| **NL3** | *The refused swap is PUT BACK - [5b-1549](../fragment-issues/section-5b-rows-401-425.md). A rolled-back run cannot show this: the rollback restores the rule whatever the card did.* On the scratch copy, saved, one duct of that type selected: `python mcp/client/heron_bridge_client.py try-once set-routing-preference --in "P1102 MEP" --set group=Elbows --set "partName=<a loaded Family: Type that is NOT an elbow - a damper>" --set ruleIndex=0`. If Revit asks, answer Keep. Then `REPORT_ROUTING_PREFERENCES` on the type, and its Routing Preferences dialog. Then Undo | `rulesChanged 0`, the rule in `unverified` ending *"The rule was put back as it was, still pointing at ..."*, and the read and the dialog showing **exactly** what NL0 wrote down - the same number of Elbows rules, the same part, the same window. **A missing rule there means the SubTransaction did not put it back: record it on row 5b-1549 and do not sign.** Whether Revit refused by throwing (*"could not be rebuilt: ..."*) or took the call and did nothing (*"did not read back as ..."*) goes on the row too. If Revit ACCEPTS a damper as an elbow (`rulesChanged 1`), that is a finding of its own: Undo, and record it |
| **NL4** | *A segment, as version 1 was proved - it must not have moved.* Rolled back: a pipe type in the same model, `group=Segments`, `partName` another segment that exists (read both with `REPORT_ROUTING_PREFERENCES` first), `ruleIndex=0`; then `partName=ZZZNOTASEGMENT` | `rulesChanged 1` with the size windows carried across; then `rulesChanged 0` and *"No Segments part is called 'ZZZNOTASEGMENT'"* |
| **NL5** | *Arrange, and look before anything runs.* The folder `%TEMP%\heron-work\prove-1102` holds `heron-p1102-shared.txt` with `HERON_P1102_TAG` in group `HERON` - read it in a text editor, and take a copy. If the file has gone, NL8's second run makes one. **`heron-p1102-shraed.txt` must NOT be in the folder.** Point Manage > Shared Parameters at a file that is neither, and note its path | The file is there with the parameter in it; the misspelt name is not; the noted path |
| **NL6** | *The D-30 pair for [5b-1127](../fragment-issues/section-5b-rows-401-425.md), rolled back.* ADMIN, so no batch sends it ([D-106](../DECISIONS.md)): the dry run prints the `validate --allow-publish ... --write` line to run by name, with **Admin** and **Changes** on in Heron's Settings - the owner's to turn on. Positive: the real file. Negative: the same call with one letter of the file name wrong. `createMissing` is left out of both | Positive: `bound true`, `boundCategories [Ducts]`, *"The definition already existed in group 'HERON' and was reused"*. Negative: `bound false`, `boundCategories` empty, `findings` naming the path and saying nothing was done. **Then look in the folder: no `heron-p1102-shraed.txt`.** That look is the proof - version 2 created the file there, bound the parameter and answered `bound true`. Manage > Shared Parameters shows the noted path after both |
| **NL7** | *A name the file has not got, rolled back, by name.* The NL6 line with the real file and `parameterName=HERON_P1102_TPYO`; then with `groupName=ZZZ NO SUCH GROUP` | Both `bound false`, `findings` naming the file, the name and the group and saying nothing was written. **Compare the file with NL5's copy: byte for byte the same** - no new line, no new group. Version 2 wrote the name into the file and bound it |
| **NL8** | *`createMissing=true` makes what was asked for, rolled back.* The NL6 line with `--set createMissing=true` and `parameterName=HERON_NL_NEW`; then with a path where no file is (`heron-nl-new.txt` in the same folder) | First: `bound true` and a finding that the parameter was created in the file as asked and that Undo does not take it out - the name is in the file afterwards, though the binding was rolled back. Second: *"Created a new shared parameter file at ..."* and `bound true`. **Neither file change is rolled back: delete `heron-nl-new.txt`, and put NL5's copy back over the real file** |

**Signing.** `SET_ROUTING_PREFERENCE` is signed from NL1's draft only after NL3 has been looked at: NL1
proves the name rule in Revit, NL3 is the only row that shows the swap is put back.
`ADD_PROJECT_PARAMETER` is signed from NL6's draft after the look in the folder, with NL7 run first.
