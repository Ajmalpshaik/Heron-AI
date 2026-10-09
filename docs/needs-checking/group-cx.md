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

**What was built.** New `LIST_MATERIALS` (FRG-ELE-077, DRAFT, READ): one collector over the materials of
the document it is handed - a project, or a family open in the Family Editor - and nothing else. It
leaves `materialCount`, `materialList` (every material on one line, sorted by name ignoring case, as
*Name (class, id N)*), `materialClasses` (the count per class, *Concrete 4, Metal 9, no class 1*),
`materialIds` in the same order, and two short `findings`. The list and the tally are strings because
the add-in reports a collection as its count and first three items, each cut at 60 characters. It does
not say whether anything uses a material: that is `FIND_UNUSED_MATERIALS`' walk over every element,
type and painted face, and its purpose says so rather than repeating it. `Material.MaterialClass` and
`Element.Name` read the same 2020 to 2027 (`tools/api-surface`), and the id is printed, never read as a
number.

**Through a chat it reads the model that chat is pinned to**, which is not always the one in front: a
chat that touched a project first answers *list the materials in this family* about the project, and
the reply then says the model is not the one in front. The purpose says so; CX7 checks it.

Compiled for Revit 2020 to 2027 on 2026-10-09 (`tools/check-fragments-compile.py`). **Nothing below has
run in Revit.** The arrangement and the commands are in
[`list-materials-2026-10-09.yaml`](../../tools/jobs/list-materials-2026-10-09.yaml), which
`batch-prove --dry-run` reads as 1 WOULD RUN. **The Material Browser shows no total** (Revit knowledge,
not measured), so every "count" below is the rows of *Project Materials: All*, counted by hand.

With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`
on every line.

| # | Run | Look for |
|---|---|---|
| **CX1** | Arrange the three scratch documents the job file names, saved once and left open together: **Heron Materials Project** (File > New > Project, template *<None>*, so its list is short), then with Changes on in the ribbon `fragment --in "Heron Materials Project" create-material --write --apply --set "materialNames=Heron List Probe" --set "colours=128,128,128" --set transparencies=0`; **Heron Materials Annotation** (new from Metric Generic Annotation.rft); **Heron Materials Accessory** (a scratch copy of a Duct Accessories family) | *Heron List Probe* created and kept. Never one of the owner's working models |
| **CX2** | Manage > Materials in **Heron Materials Annotation**, before anything else. **Not measured: whether that template holds any material, or whether Revit lets every one be deleted** | None listed, or every one deleted and the family saved - the job's negative is a true empty and CX3 can be signed. If Revit keeps one, CX3's verdict is NEG NOT EMPTY whatever the fragment does, and **nothing in this group can be signed** until a tool records tracking across documents: `heron_validate` refuses a record whose negative came back full unless it carries a tracking set, and CX4 writes none |
| **CX3** | `python tools/batch-prove.py tools/jobs/list-materials-2026-10-09.yaml` | positive, in the project: `materialCount` equal to its hand count, *Heron List Probe* in `materialList` with its class and id, and `materialClasses` adding up to `materialCount`. negative, in the emptied annotation family with the project open beside it: `materialCount` 0, `materialClasses` empty, *No materials at all in this family*, and **no Heron List Probe** - a version reading the wrong document, or every open one, shows it there |
| **CX4** | `fragment --in "<each of the three>" list-materials`, one after another | D-53 tracking, by hand: each count equal to its own document's hand count, and *Heron List Probe* in the project's list alone. **Evidence for a person, not a signature** - `prove-tracking.py` and `validate`'s tracking vary a request value, and this fragment's only input is the document |
| **CX5** | The second route, and the empty class: each document's Material Browser compared name by name with `materialList`; the class read off the Identity tab of two or three; *Heron List Probe*'s Identity tab read - if its class is empty, it must show as *(no class, id N)* and count under *no class* in `materialClasses`; if not, clear one material's class by hand in the project, run CX3's positive again, and look for the same | the same names, the same order, the same classes. A material in the browser that the list lacks, or the reverse, is the finding - the browser may hide something the collector returns, and nobody has looked |
| **CX6** | `fragment --in "<a large scratch model>" list-materials` - `Snowdon-scratch_ajmal.al`, or a scratch copy of an office template | `materialCount`, how long the call took, and **the reply's length**: `materialList` is not capped, so a thousand materials send one line of tens of kilobytes. Expected, not measured |
| **CX7** | Through the chat. In a **fresh** chat with **Heron Materials Accessory** in front: *list the materials in this family*. Then load the accessory into **Heron Materials Project**, place one, select it, re-pin with `revit_use_this_model`, and ask *what material is that* | the first reaches `LIST_MATERIALS` on the accessory family, with no *is not the model in front* line; the second still reaches `READ_ELEMENT_MATERIAL`. Needs the shared knowledge store rebuilt from main after merging - until then `heron_resolve` answers *Nothing provides LIST_MATERIALS* |
| **CX8** | The signature | `python brain/heron_validate.py accept list-materials --by "Ajmal PS"`, on CX3's record, on the owner's word only, after CX5 - and only on CX2's first branch |
| **CX9** | CX3 to CX5 on **Revit 2020** and **2027** | the three documents made again under the same titles from that release's own templates - a 2024 file does not open in 2020 - so "the same answers" means each count matching its own hand count, the probe in the project alone, and the annotation negative empty. Compiled and api-surface-checked on all eight releases; what Revit DOES was measured on none |

**Routing, measured 2026-10-09 on scratch stores - not proof.** 189 sentences - the 149 first measured,
13 about material use and 27 count and listing questions about other things, both sets added when PR
#450 was reviewed - on both search backends (`lexical`, and `model2vec` `potion-base-8M` in a venv under
`%TEMP%\heron-work`), main as it stood (`e21a3f5e`, with row 375's card edits) against main with this
card:

- **Of the 149, 22 change winner on the lexical backend and 19 on the trained one, every one to
  `LIST_MATERIALS`.** Four are this card's own declared sentences, which reach it by exact match, so the
  search itself moved 18 and 15: *list the materials*, *list all the materials*, *how many materials are
  there*, *what materials are in this family*, *give me a list of all the materials* and the rest.
  *materials in this model*, *which materials are in this model* and *what material does this family use*
  moved on the lexical backend only; on the trained one they stay with `FIND_UNUSED_MATERIALS` and
  `SELECT_BY_MATERIAL`, as on main. Of the 149, those answered by a write fall from 41 to 30 (lexical)
  and 33 to 30 (trained).
- **`READ_ELEMENT_MATERIAL` keeps its questions.** *what material are these* goes there by words #1 and
  nearness #1 on both backends, with this card not among the five shown; *what material is that* is
  declared there since row 5b-375. **The first wording of the purpose said "material" six times** and made
  the two an exact tie on the trained backend that only PROVEN settled.
- **One thin margin, beside a write, so the next card near materials is measured against it:** *what
  material does this family use* wins by 0.000032 over `CREATE_MATERIAL` on the lexical backend.
- **Questions about USE come here, because the purpose names "used" to disclaim it** - recorded, not
  tuned. *what is this material used on*, *list the materials that are used*, *which materials are used
  in this project* and *list the materials and where they are used* now reach this card, whose answer says
  which tool to use; before, they reached `FIND_UNUSED_MATERIALS`, `READ_ELEMENT_MATERIAL` or the
  transfer. *what uses this material* reached `SET_MATERIAL_COLOUR`, a write, on both backends, and now
  reaches this read. Without "used" and "uses" in the purpose they all go back, that write included.
- **Still answered by a write, the same on main, not caused here and not fixed:** *which materials are in
  use* (`CREATE_MATERIAL`, lexical), *which elements use this material* (`REPLACE_MATERIAL` lexical,
  `CREATE_MATERIAL` trained), and row 375's *what material does this use* and *which material are those*.
- **The count phrase costs two sentences, both on the lexical backend, both between reads - recorded, not
  tuned:** *how many rooms are there* goes from `COUNT_BY_SPATIAL_CONTAINER` to
  `REPORT_AREA_VOLUME_COMPUTATIONS`, and *show me all the families* - `SELECT_SUBCOMPONENTS` on main - comes
  to THIS card, a list of materials for a question about families. It also tipped *how much space does
  this take up* to `REPORT_SPACE_AIRFLOW` (0.000029 apart) while only a comment on `REPORT_BOUNDING_BOX`
  claimed it; [row 5b-385](../fragment-issues/section-5b-rows-176-200.md) declared it there, and with
  both merged it goes there by exact match on both backends. Without *how many there are* the two go back,
  and *how many materials are there* goes to the transfer, a write. No other of the 27 moves, and none
  moves on the trained backend - where *how many rooms are there* reaches `PLACE_ROOMS`, **a write, the
  same on main**, found in passing and not this card's.
- **The pin sentence was worded three times.** *which the answer names* and *the answer gives its title*
  put this card ahead of `DESCRIBE_ELEMENTS` for the owner's question #20 on the trained backend;
  *and the answer says which* sent *which family is the heavy one*, claimed by
  `REPORT_GEOMETRY_COMPLEXITY`, to `LOAD_FAMILY` - a write - on the lexical one. *and the answer says so*
  moves neither.
- **Against row 5b-396's held-out set** ([`material-questions.tsv`](../../tests/data/material-questions.tsv),
  238 sentences nobody tuned to), main `aebca7ad` against main with this card: its five listing sentences
  reach this card 5 of 5 on the lexical backend and 4 of 5 on the trained one, where main sent two and one
  of them to a write. Its 69 questions about what an element is made of reach a write 30 → 30 (lexical) and
  21 → 22 (trained): **the one new is *what material was used when these were created*, which goes from
  `SELECT_BY_MATERIAL` to `CREATE_PIPE_SEGMENT` - a write - on the trained backend**, two cards that sit
  0.000001 apart on main. On the trained backend 2 of the 10 questions about UNUSED materials that reached
  `FIND_UNUSED_MATERIALS` now reach this card, pulled by the purpose's *NOT WHAT IS USED*; on the lexical
  one none is lost. **Three rewordings without "used" were measured on 420 sentences**: each settles the
  trained crossing and the unused questions, and each sends a question to a write on the lexical backend
  (*which family is the heavy one* to `LOAD_FAMILY` among them) - and the lexical backend is the one the
  owner's PC answers on. So the wording stays: across the 420, on the lexical backend this card sends no
  read to a write and turns 15 writes into reads.
- **The house tools, main `aebca7ad` against main with this card:** `check-risk-crossings` gives the same
  crossings on both backends; `check-routing` and `check-intrusion` name FRG-ELE-077 nowhere and newly
  miss no routing-table claim. `score-routing` is the same on the trained backend; on the lexical one the
  owner's question #2, *How many VCDs are there, and what sizes are they?*, keeps a read - its winner goes
  from `COUNT_ELEMENTS` to `REPORT_NESTED_FAMILIES`, neither the answer key's - and the answer key's
  `GROUP_AND_COUNT` moves from 4th to 3rd. `check-skill-routing` changes no winner on the trained backend;
  on the lexical one two skill sentences change answer and both now reach a step of their skill where
  they missed on main (*build a parametric family from this picture*, *create a face based family so it
  hosts ...*). Measured on `e21a3f5e` earlier the same day, *which ducts have no system name* moved
  between two reads instead; on `aebca7ad` it does not move.
