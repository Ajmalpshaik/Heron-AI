# Needs checking — Group NL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NL - cards that were PROVEN, change the model and could report a wrong result as a success, repaired in code and back to DRAFT: `SET_ROUTING_PREFERENCE` version 2, `ADD_PROJECT_PARAMETER` version 3 and `CREATE_ASSEMBLY` version 2 (2026-11-11)

A triage on 2026-11-11 named PROVEN cards that change the model and can do it wrongly while the answer
reads as a success. The owner's instruction that day: *"if you are finding any fragment's mistake, even
though it is proven, you can change that one."* Each card below was repaired in code the same day, in a
cloud session with no Revit, **and changed again on 2026-11-12 after a review of the three repairs**. Each
is **DRAFT again** - its C# moved, so its proof no longer describes it and `revit_change` refuses it until
it is proved again ([D-30](../decisions/D-30.md), [D-117](../decisions/D-117.md)). Each card keeps its old
proof block as the record of what was signed.

| Card | Where | Rows | What changed | Held without Revit by |
|---|---|---|---|---|
| `SET_ROUTING_PREFERENCE` version 2 (MODIFY) | [`brain/fragments/set-routing-preference`](../../brain/fragments/set-routing-preference/fragment.yaml) | [5b-1120](../fragment-issues/section-5b-rows-401-425.md), [5b-1549](../fragment-issues/section-5b-rows-401-425.md) | A fitting is named "Family: Type", or by its type name alone when one loaded family has it; a type name two families share is refused and listed, twelve at most. The rule swap sits in a SubTransaction, kept only when the rule points at the part AND the group holds as many rules as before; a swap that is rolled back is read again before it is called put back, and the card throws - the whole job rolled back - if it does not read as before | The name rule and the keep-a-swap rule: [`tests/test_binding_note.py`](../../tests/test_binding_note.py) compiles the fragment's own lines and asks them - seen to fail on version 1, and on the first cut of the repair. **Not** which parts a project holds, and **not** that the SubTransaction puts a removed rule back |
| `ADD_PROJECT_PARAMETER` version 3 (ADMIN) | [`brain/fragments/add-project-parameter`](../../brain/fragments/add-project-parameter/fragment.yaml) | [5b-1127](../fragment-issues/section-5b-rows-401-425.md) | A shared parameter file that is not at the path, or a parameter the file's group does not hold, is refused naming what was not found, and nothing is written. Two separate yeses: `createMissing` makes a parameter (and its group) inside a file that is there and never a file; `createFile` makes a new file | Nothing but the compiler. A file on disk and Revit's reading of it need the PC |
| `CREATE_ASSEMBLY` version 2 (MODIFY) | [`brain/fragments/create-assembly`](../../brain/fragments/create-assembly/fragment.yaml) | [5b-1117](../fragment-issues/section-5b-rows-401-425.md), [5b-1553](../fragment-issues/section-5b-rows-401-425.md) | Rebuilt on two transactions inside the add-in's one group ([D-112](../decisions/D-112.md)'s `editScopeFailures`, as a stair, under [D-115](../decisions/D-115.md) item 1): the assembly is made and committed, then named and read back. `assembly` and `memberIds` are filled only then. Anything that fails after the first commit THROWS with the reason, so the add-in rolls the whole call back - no undo entry, nothing touched. A name is never taken from other assemblies | Nothing but the compiler. **Whether it works at all rests on one thing nobody has measured - NL9** |

**What is done and what is not.** Every card compiles on each release it declares, 2020 to 2027, with the
whole library ([`tools/check-fragments-compile.py`](../../tools/check-fragments-compile.py), 2026-11-12,
a Linux container), and every Revit member added is in its card's compatibility note, read with
`tools/api-surface --members`. **Nothing here has run in Revit.**

**Two things the owner should know before the run, said plainly.**

- **`SET_ROUTING_PREFERENCE` with several types is not all-or-nothing.** Each type's swap is its own, as
  each column is in `ATTACH_COLUMNS` and each duct in `CONVERT_TO_FLEX_DUCT`: a type whose swap Revit
  refuses is put back and named in `unverified`, and the types already changed STAY changed. It is one
  Undo entry, and `findings` says so in words when both happen. Only a put-back that does not read as
  before stops the whole job.
- **`CREATE_ASSEMBLY` now opens its own transactions, on D-112's switch.** That use is decided:
  [D-115](../decisions/D-115.md) item 1, Accepted - *"A fragment that Revit refuses inside a transaction
  declares `editScopeFailures` too, whatever the reason"* - and the card names the refusal that made it
  need the switch, as item 3 asks. What is NOT known is whether it works: NL9.
- **When `CREATE_ASSEMBLY` refuses after Revit has made the assembly, the answer is not a tidy refusal.**
  It reads *"'create-assembly' threw while running: <why, in Revit's words>. The call failed, and Heron
  rolls the whole call back - no assembly is kept."* That is on purpose: only the add-in's rollback
  leaves the model exactly as it was, with no undo entry. The reason is in the sentence.

**The model - the scratch copies the 2026-11-02 rounds used, never a working project.** Revit 2020, with
`P1102 MEP` open and in front: the scratch copy of `Samples\rme_advanced_sample_project.rvt` under
`%TEMP%\heron-work\prove-1102`. Every line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>`
when two Revits are connected, and `< /dev/null`.

**One job file, one sitting:** [`tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml`](../../tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml).

```bash
python tools/batch-prove.py tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml --dry-run
python tools/batch-prove.py tools/jobs/silent-damage-repairs-nl-2026-11-11.yaml --session <pid>
```

The dry run was read on 2026-11-12 with no Revit connected, so it says only that the file is runnable as
written. **It does not catch a wrong name** - the job file's header says which THREE values are an
assumption of what the sample holds.

**NL0's reads, as command lines** - written here and in the job file's header, not run. One conversation
id for the series, so the third line can take what the second left; `--in` stands before the card's name.

```bash
export HERON_CLIENT_ID=ajmal-pc HERON_CONVERSATION_ID=nl0
C="python mcp/client/heron_bridge_client.py fragment --in"

# every duct type, as "Family : Type"
$C "P1102 MEP" select-types --set categories=Ducts --set familyNameContains= --set typeNameContains=

# the one type the job names, and what its rules point at now
$C "P1102 MEP" select-types --set categories=Ducts --set "familyNameContains=Rectangular Duct" --set "typeNameContains=Mitered Elbows / Tees"
$C "P1102 MEP" report-routing-preferences --expect-from select-types

# every loaded duct fitting, as "Family : Type"
$C "P1102 MEP" select-types --set "categories=Duct Fittings" --set familyNameContains= --set typeNameContains=

python mcp/client/heron_bridge_client.py release
```

| # | Run | Look for |
|---|---|---|
| **NL0** | *Read first, change nothing* - the five lines above, on `P1102 MEP`. Write down: whether exactly ONE duct type is `Rectangular Duct : Mitered Elbows / Tees` and nothing else holds both words; what its Elbows rule 0 points at, its size window, and how many Elbows rules it has; another loaded ELBOW; and how many loaded families have a type called `Standard`. Put the real names in the job file | The second line hands on 1 type. The job's `partName` is an elbow rule 0 does **not** point at, written `Family: Type`; the negative's name is one two or more families share |
| **NL1** | *The D-30 pair for [5b-1120](../fragment-issues/section-5b-rows-401-425.md), rolled back.* The job's first entry. Positive: `partName` the other elbow as `Family: Type`. Negative: the same type and rule, `partName=Standard` | Positive: `rulesChanged 1`, `findings` naming the old and the new elbow as `Family: Type` with the size windows carried across. Negative: `rulesChanged 0`, `typesTouched 0`, `findings` saying the name does not say which one is meant, and `nearMisses` holding the families that have it - twelve at most, duct and pipe fittings first, with *"and N more are not"* when there are more. **To the first, version 1 said "No Elbows part is called ..."; to the second, it bound whichever the collector met first.** Afterwards the type's Routing Preferences dialog reads what NL0 wrote down |
| **NL2** | *The other spellings, rolled back, by name.* `validate --write` with the same setup: `partName` with a space each side of the colon, as `SELECT_TYPES` prints it; then the elbow FAMILY's name alone | The first finds the same part. The second changes nothing and lists that family's types in `nearMisses`, each `Family: Type` |
| **NL3** | *The refused swap is PUT BACK - [5b-1549](../fragment-issues/section-5b-rows-401-425.md). A rolled-back run cannot show this: the rollback restores the rule whatever the card did.* On the scratch copy, saved, one duct of that type selected: `python mcp/client/heron_bridge_client.py try-once set-routing-preference --in "P1102 MEP" --set group=Elbows --set "partName=<a loaded Family: Type that is NOT an elbow - a damper>" --set ruleIndex=0`. If Revit asks, answer Keep. Then `REPORT_ROUTING_PREFERENCES` on the type, and its Routing Preferences dialog. Then Undo | `rulesChanged 0`, the rule in `unverified` ending *"The rule was put back and read again: N rule(s), M size window(s), still pointing at ..., as before"*, and the read and the dialog showing **exactly** what NL0 wrote down - the same number of Elbows rules, the same part, the same window. **If the answer is instead "threw while running ... does NOT read as it did before ... the whole job is rolled back", the SubTransaction did not put the rule back, the card caught it and the add-in undid everything: the dialog must still read as NL0 - record the words on row 5b-1549 and do not sign.** Whether Revit refused by throwing (*"could not be rebuilt: ..."*) or took the call and did nothing (*"did not read back as ..."*, or *"left the group with N rule(s) where it had M"*) goes on the row too - it has never been measured. If Revit ACCEPTS a damper as an elbow (`rulesChanged 1`), that is a finding of its own: Undo, and record it |
| **NL4** | *A segment, as version 1 was proved - it must not have moved.* Rolled back: a pipe type in the same model, `group=Segments`, `partName` another segment that exists (read both with `REPORT_ROUTING_PREFERENCES` first), `ruleIndex=0`; then `partName=ZZZNOTASEGMENT` | `rulesChanged 1` with the size windows carried across; then `rulesChanged 0` and *"No Segments part is called 'ZZZNOTASEGMENT'"* |
| **NL4a** | *Several types - NOT all-or-nothing, and said.* Two duct types selected (one duct of each), kept through `try-once`: a part one of them can take and the other already uses, or cannot | The one that can change is in `rulesChanged`; the other in `alreadyCorrect` or `unverified`. Where one changed and one was refused, `findings` says the job is not all-or-nothing. ONE undo entry; one Undo puts the changed type back |
| **NL5** | *Arrange, and look before anything runs.* The folder `%TEMP%\heron-work\prove-1102` holds `heron-p1102-shared.txt` with `HERON_P1102_TAG` in group `HERON` - read it in a text editor, and take a copy. If the file has gone, NL8's last run makes one. **`heron-p1102-shraed.txt` must NOT be in the folder.** Point Manage > Shared Parameters at a file that is neither, and note its path | The file is there with the parameter in it; the misspelt name is not; the noted path |
| **NL6** | *The D-30 pair for [5b-1127](../fragment-issues/section-5b-rows-401-425.md), rolled back.* ADMIN, so no batch sends it ([D-106](../DECISIONS.md)): the dry run prints the `validate --allow-publish ... --write` line to run by name, with **Admin** and **Changes** on in Heron's Settings - the owner's to turn on. Positive: the real file, `createMissing` and `createFile` left out. **Negative: one letter of the file name wrong AND `createMissing=true`** - what *"create a shared parameter"* sends | Positive: `bound true`, `boundCategories [Ducts]`, *"The definition already existed in group 'HERON' and was reused"*. Negative: `bound false`, `boundCategories` empty, `findings` naming the path and saying that asking for a new parameter does not make a new file. **Then look in the folder: no `heron-p1102-shraed.txt`.** That look is the proof - version 2 created the file there, bound the parameter and answered `bound true`, and so did the first cut of version 3 whenever `createMissing` was true. Manage > Shared Parameters shows the noted path after both |
| **NL7** | *A name the file has not got, rolled back, by name.* The NL6 positive line with `parameterName=HERON_P1102_TPYO`; then with `groupName=ZZZ NO SUCH GROUP` | Both `bound false`, `findings` naming the file, the name and the group and saying nothing was written. **Compare the file with NL5's copy: byte for byte the same** - no new line, no new group. Version 2 wrote the name into the file and bound it |
| **NL8** | *Each yes makes what it is for, rolled back.* (a) the NL6 positive line with `--set createMissing=true` and `parameterName=HERON_NL_NEW`; (b) the same with `groupName=HERON NL GROUP` and `parameterName=HERON_NL_NEW2`; (c) `--set createFile=true`, `createMissing` left out, and a path where no file is (`heron-nl-new.txt` in the same folder) | (a) `bound true` and a finding that the parameter was created in the file as asked and that Revit's Undo does not take it out. (b) the same, and the group named as NEW. (c) *"Created a new shared parameter file at ..., as asked. It is a file on disk: Revit's Undo does NOT remove it"*, the parameter made in it, `bound true`. **None of the three file changes is rolled back with the model: delete `heron-nl-new.txt`, and put NL5's copy back over the real file** |
| **NL9** | *THE RUN THAT DECIDES WHETHER `CREATE_ASSEMBLY` WORKS - [5b-1553](../fragment-issues/section-5b-rows-401-425.md), rolled back.* The job's last entry: the ducts in `Callout of Level 1 HVAC WSHP TYP`, `assemblyName=HERON NL A1` (a name no assembly has), `namingCategory=Ducts`; negative `namingCategory=Zz No Such Category` | **A pass can only mean an assembly existed with that name:** `assembly` not null, `memberIds` the ducts, `findings` *"Made assembly "HERON NL A1" with N member(s), read back"* - both are filled only after the assembly is made, committed, named, read back with a type and that name, and committed again. **If Revit does not assign the type at a commit inside the add-in's group**, the verdict is `DID NOT RUN` and the message reads *"'create-assembly' threw while running: Revit made the assembly but had not given it an assembly type after the transaction that made it was committed, so it could not be named or used. The call failed, and Heron rolls the whole call back - no assembly is kept."* - **not** *"Revit refused the name"*, and not the owner's mistake. Those words go on row 5b-1553 verbatim, the row stays open, and nothing is signed |
| **NL10** | *It is really there, and it is ONE undo - only if NL9 passed.* Count the entries under Assemblies in the Project Browser. Two of those ducts selected, the scratch copy saved: `python mcp/client/heron_bridge_client.py try-once create-assembly --in "P1102 MEP" --set "assemblyName=HERON NL A2" --set namingCategory=Ducts`; answer Keep | ONE new entry under Assemblies named exactly `HERON NL A2`; each duct's Properties names it; **ONE entry in Revit's undo list for the job**, though the card ran two transactions inside it; one Undo takes the assembly and its name away together and leaves the ducts - redo it, and leave it KEPT for NL11 and NL12 |
| **NL11** | *All or nothing - a name that is already another assembly's, [5b-1117](../fragment-issues/section-5b-rows-401-425.md), rolled back.* With `HERON NL A2` kept from NL10: the `validate` line the dry run prints for `create-assembly`, with `assemblyName=HERON NL A2` in its `--set` and its `--negative-set`. The two ducts already in A2 are left out and named; the callout's other ducts are assembled, and the name is then found taken. **Before and after, read the undo list and the Assemblies node** | The positive leg is `DID NOT RUN` on purpose, with *"'create-assembly' threw while running: Another assembly type in this model is already called "HERON NL A2", so the new assembly was not given that name. Ask again with a name no assembly has. The call failed, and Heron rolls the whole call back - no assembly is kept."* The Assemblies node holds A2 alone; **the undo list has no new entry**; no duct outside A2 reports an assembly. If the add-in adds *"THE ROLLBACK DID NOT HOLD"*, that is the add-in's own check speaking - stop, and record it on row 5b-1117 |
| **NL12** | *A name is never taken from another assembly - Revit's "all matching assembly instances share this name".* With A2 kept: copy A2's two ducts 5 m to one side (so the copies match it exactly), select the two copies, and through a rolled-back `validate --write` ask for `assemblyName=HERON NL A3`; then the same with `assemblyName=` empty | **Either answer is a finding for row 5b-1553, and both are safe.** If Revit gives the copies A2's own type: the first call stops - *"Revit matched the new assembly to the assembly type "HERON NL A2", which 1 assembly(ies) already in the model share. Naming it "HERON NL A3" would rename all of them"* - and the second succeeds, reporting `HERON NL A2`. If Revit gives them a type of their own: the first call succeeds as `HERON NL A3`. **In both, A2 is still called `HERON NL A2` in the Project Browser afterwards** |

**Run 2026-11-13, Revit 2024, session 22684, on "Heron Reads Test" - `ADD_PROJECT_PARAMETER` only, NL5 to NL7 (every run rolled back; records in `%TEMP%\heron-work\nl`).** **Not the model and release this group names:** the owner asked that the card be proved on the scratch project Group NK was arranging, because NK0 needed a project parameter added and no PROVEN card could add one. The values are the job file's own, with `--in "Heron Reads Test"`; the shared parameter file is the one under `prove-1102`. **NL5 PASS:** `heron-p1102-shared.txt` holds `HERON_P1102_TAG` in group `HERON` (and `HERON_P1102_NOT_IN_FILE`); `heron-p1102-shraed.txt` is not in the folder; a copy was taken to `%TEMP%\heron-work\nl\heron-p1102-shared.NL5-copy.txt` (SHA-256 `04d85b98...affcabc`); `REPORT_SHARED_PARAMETER_FILE` read Revit's setting as *"Revit has no shared parameter file set - Manage > Shared Parameters is empty"* - neither file. **NL6 PASS, both halves** (`validate --allow-publish --write`, record `brain/proof-drafts/runs/add-project-parameter.json`): positive `bound true`, `boundCategories [Ducts]`, *"The definition already existed in group 'HERON' and was reused - no second parameter of that name was made"*, *"Revit's shared parameter file setting was put back to none, as it was"*; negative (`heron-p1102-shraed.txt`, `createMissing=True`) `bound false`, `boundCategories` empty, *"There is no shared parameter file at '.../heron-p1102-shraed.txt', so nothing was done: no file was made, no parameter was added and the shared parameter file Revit has set was not touched ... Asking for a new parameter does not make a new file"*. **Looked in the folder afterwards: no `heron-p1102-shraed.txt`.** **NL7 PASS** (`fragment --write`, rolled back): `parameterName=HERON_P1102_TPYO` - `bound false`, *"has no parameter called 'HERON_P1102_TPYO' in group 'HERON', so nothing was done: nothing was written into the file and no parameter was added"*; `groupName=ZZZ NO SUCH GROUP` - `bound false`, *"has no group called 'ZZZ NO SUCH GROUP' to find 'HERON_P1102_TAG' in, so nothing was done"*; both put the setting back to none. **The file afterwards is byte for byte NL5's copy** (`cmp`, same SHA-256). **NL8 NOT RUN** - it writes into the file and makes a new one, which a rollback does not undo, and the signing below does not need it. Draft `brain/proof-drafts/add-project-parameter.yaml`, fingerprint `e7048aa7a2d7148e`, second route NOT ESTABLISHED. **Revit 2020, this group's release, is not run** - a further release entry when it is.

**Signing.** `SET_ROUTING_PREFERENCE` is signed from NL1's draft only after NL3 has been looked at: NL1
proves the name rule in Revit, NL3 is the only row that shows the swap is put back.
`ADD_PROJECT_PARAMETER` is signed from NL6's draft after the look in the folder, with NL7 run first.
`CREATE_ASSEMBLY` is signed from NL9's draft only if NL9 passed AND NL10 showed the assembly in the
Project Browser under the name asked for, as one undo entry; NL11 and NL12 are looked at first.

**What changed for other proofs.** `CREATE_ASSEMBLY` can no longer be a setup step: a setup step runs
inside the add-in's own step transaction, where a card cannot open its own. `validate --write` and a dry
run now refuse that before anything is sent ([row 5b-1556](../fragment-issues/section-5b-rows-401-425.md)).
`CREATE_ASSEMBLY_VIEWS` (DRAFT) was arranged that way in `tools/jobs/p1102-round9-2026-11-02.yaml` - a
record of 2026-11-02, left as it was, whose dry run now reads `REFUSED` for that entry; its proof needs
an assembly made and KEPT first, and NL10 leaves one.
