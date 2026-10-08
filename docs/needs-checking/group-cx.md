# Needs checking — Group CX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CX - every material a project or family holds, listed by name, class and id (2026-10-09)

On 2026-10-09 a modeller with a Duct Accessories family open in Revit 2024 asked *"list the materials
in this family"* and was handed `TRANSFER_MATERIALS_BETWEEN_DOCUMENTS` - a write - on the lexical
backend, and `FIND_UNUSED_MATERIALS`, which lists only what nothing uses, on the trained one
([row 5b-376](../fragment-issues/section-5b-rows-176-200.md)). No capability listed the materials a
document holds; [section 3d](../fragment-issues/section-3d.md) had recorded the want on 2026-09-08 and
[section 3i](../fragment-issues/section-3i.md) had asked for it as `list-materials`.

**What was built.** New `LIST_MATERIALS` (FRG-ELE-077, DRAFT, READ): one collector over the document's
materials - the project, or the family open in the Family Editor - and nothing else. It leaves
`materialCount`, `materialList` (every material on one line, sorted by name ignoring case, as
*Name (class, id N)*), `materialIds` in the same order, and `findings` with the count by class. It does
not say whether anything uses a material: that is `FIND_UNUSED_MATERIALS`' walk over every element,
type and painted face, and its purpose says so rather than repeating it. `Material.MaterialClass` and
`Element.Name` read the same 2020 to 2027 (`tools/api-surface`), and the id is printed, never read as a
number.

Compiled for Revit 2020 to 2027 on 2026-10-09 (`tools/check-fragments-compile.py`). **Nothing below has
run in Revit.** The arrangement and the commands are in
[`list-materials-2026-10-09.yaml`](../../tools/jobs/list-materials-2026-10-09.yaml), which
`batch-prove --dry-run` reads as 1 WOULD RUN.

With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`
on every line.

| # | Run | Look for |
|---|---|---|
| **CX1** | Arrange the three scratch documents the job file names - **Heron Materials Project** (metric template, then `CREATE_MATERIAL` makes *Heron List Probe*), **Heron Materials Annotation** (new from Metric Generic Annotation.rft) and **Heron Materials Accessory** (a scratch copy of a Duct Accessories family) - saved once, left open together | Never one of the owner's working models |
| **CX2** | Manage > Materials in **Heron Materials Annotation**, before anything else | **Not measured: whether that template holds any material.** None - the job's negative is a true empty and CX3 is the whole proof. Any - CX3's verdict is NEG NOT EMPTY whatever the fragment does, and CX4 carries the second leg |
| **CX3** | `python tools/batch-prove.py tools/jobs/list-materials-2026-10-09.yaml` | positive, in the project: `materialCount` equal to its Material Browser's count (Project Materials: All), and *Heron List Probe* in `materialList` with its class and id. negative, in the annotation family with the project open beside it: `materialCount` 0, *No materials at all in this family*, and **no Heron List Probe** - a version reading the wrong document, or every open one, shows it there |
| **CX4** | `fragment --in "<each of the three>" list-materials`, one after another | D-53 tracking, by hand: three counts, each equal to its own Material Browser's, and *Heron List Probe* in the project's list alone. **No tool records this yet** - `prove-tracking.py` and `validate`'s tracking vary a request value, and this fragment's only input is the document |
| **CX5** | The second route: each document's Material Browser, compared name by name with `materialList`, and the class read off the Identity tab of two or three | the same names, the same order, the same classes. A material in the browser that the list lacks, or the reverse, is the finding - the browser may hide something the collector returns, and nobody has looked |
| **CX6** | Through the chat, with **Heron Materials Accessory** in front: *list the materials in this family*; then *what material is that* with one duct accessory selected in a project | the first reaches `LIST_MATERIALS`; the second still reaches `READ_ELEMENT_MATERIAL`. Needs the shared knowledge store rebuilt from main after merging - until then `heron_resolve` answers *Nothing provides LIST_MATERIALS* |
| **CX7** | The signature | `python brain/heron_validate.py accept list-materials --by "Ajmal PS"`, on CX3's record, on the owner's word only, after CX5 |
| **CX8** | CX3 and CX4 on **Revit 2020** and **2027** | the same answers. Compiled and api-surface-checked on all eight releases; what Revit DOES was measured on none |

**Routing, measured 2026-10-09 on scratch stores - not proof.** 149 sentences, both search backends
(`lexical`, and `model2vec` `potion-base-8M` in a venv under `%TEMP%\heron-work`), against main and
against main with [#447](https://github.com/Ajmalpshaik/Heron-AI/pull/447)'s row-375 card edits:
22 change winner, **every one to `LIST_MATERIALS`** - the row's two sentences, *what materials are in this
project*, *show me all the materials*, *list the materials*, *how many materials are there* and sixteen
more - and none moves anywhere else. Of the 149, those answered by a write fall from 39 to 29 (lexical)
and 33 to 29 (trained) on main, and from 41 to 30 and 33 to 30 with #447. `READ_ELEMENT_MATERIAL` keeps *what material is that* and *what material are these*
on both backends, words #1 and nearness #1 on the trained one. `check-risk-crossings` and `score-routing`
are identical before and after on both backends; `check-routing` and `check-intrusion` name FRG-ELE-077
nowhere. **The first wording of the purpose said "material" six times**, and on the trained backend it
made the two `READ_ELEMENT_MATERIAL` questions an exact tie that only PROVEN settled; reworded, they are
not close. **One thing moves the wrong way and is recorded, not tuned:** the purpose's *how many there
are* tips *how much space does this take up* - claimed only in a comment on `REPORT_BOUNDING_BOX` - to
`REPORT_SPACE_AIRFLOW` on the lexical backend, two reads 0.00003 apart; worded without "how", that
sentence stays and *how many materials are there* goes, with #447, to the transfer.
