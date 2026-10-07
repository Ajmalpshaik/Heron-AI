# Needs checking — Group CW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CW - a family's forms picked by id, kind or material, to select or delete (2026-10-08)

The form tools (`CREATE_FAMILY_EXTRUSION`, `_REVOLUTION`, `_BLEND`, `_SWEEP` and the rest) answer with each
form's id as `formId`, and on 2026-10-08 the owner, building a family in Revit 2024, needed several of those
forms SELECTED to delete or edit them - and Heron had no way ([row 5b-369](../fragment-issues/section-5b-rows-176-200.md)).
`SELECT_BY_MATERIAL` with `categories=Generic Models` in a Generic Model family answered *0 of 0 scanned*;
`SET_SELECTION` and `DELETE_ELEMENTS` take what is selected; `FILTER_ELEMENTS_BY_ID` refuses typed ids by
design.

**What was built.** New `SELECT_FAMILY_FORMS` (FRG-SEL-034, DRAFT, READ): forms of the family open in the
Family Editor, by the ids the form tools gave back, or by kind and side as `REPORT_FAMILY_FORMS` narrows
("all", "void", "solid, revolve"); an id that is not a form of this family refuses the WHOLE call, every bad
one named, and nothing is carried. Its forms go on to `SET_SELECTION` or `DELETE_ELEMENTS` with
`expect_from select-family-forms` - both PROVEN and NOT touched. `SELECT_BY_MATERIAL` becomes version 3
(still DRAFT): in a family it scans every form whatever the category list says.

**Why a form never passed a category filter, measured 2026-10-08 (Revit 2024, a scratch probe kept outside
the repository, read only):** a form with no subcategory has `Category` **null**; a form on a subcategory
has the SUBCATEGORY as its category ("Ring", id 2944, parent Generic Models). Neither passes a filter for
`Generic Models`. A form whose Material field is <By Category> answers `GetMaterialIds` with nothing.

Compiled for Revit 2020 to 2027 on 2026-10-08. **Every run below was on Revit 2024, session 9240, in
`FormSelectProof`** - a family made for this group by `CREATE_FAMILY_DOCUMENT` from *Metric Generic Model*,
saved to `%TEMP%\heron-work\form-select-18b06da1\FormSelectProof.rfa`, never one of the owner's open
families. Kept in it, unsaved: a solid extrusion (`...-00000b64`, Glass), a void extrusion (`...-00000b6b`)
and a solid revolve (`...-00000b70`, subcategory Ring) - every id starts
`f1a0a03f-f7c5-41b6-ac99-cf8e6dd840de`. The job file is
[`family-forms-by-id-2026-10-08.yaml`](../../tools/jobs/family-forms-by-id-2026-10-08.yaml).

With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`
on every line.

| # | Run | Look for |
|---|---|---|
| **CW1** | `validate --in "FormSelectProof" select-family-forms --set "forms=<b64>, <b70>" --negative-set "forms=<b64>, da0cea1e-82fa-11d3-a7db-00105aa73639-00000030"` | positive: `elements` 2, *solid extrusion ...b64 \|\| solid revolve ...b70*, `scanned` 3. negative: refused, *"...00000030" is "Center (Left/Right)" (Reference Planes), not a form*, `elements` 0 - the good id NOT carried. **RAN 2026-10-08:** exactly that; record `brain/proof-drafts/runs/select-family-forms.json`, draft fingerprint `7a661d39878bef87`, UNSIGNED |
| **CW2** | `validate --in "FormSelectProof" select-by-material --set materialName=Glass --set includePaint=false --set "categories=Generic Models"` and `--negative-set materialName=Default Wall` with the same two | positive: `elements` 1 (the extrusion), *1 of 3 scanned*, and the family line - all 3 forms scanned, 1 form <By Category> (the revolve; the void is not counted). negative: 0 of the same 3. **RAN 2026-10-08:** exactly that; record `brain/proof-drafts/runs/select-by-material.json`, UNSIGNED. **The same call on main's code, the same family: *0 of 0 scanned*** - the owner's report, reproduced |
| **CW3** | `fragment --in "FormSelectProof" select-family-forms --set forms=void`; then `forms=revolve, void` | `elements` 1, the void alone; then 0 with nothing refused - 3 looked at, none a void revolve. **RAN 2026-10-08:** both |
| **CW4** | `forms=cylinder`; then `forms=void, <b6b>` | refused - *neither an id in this family nor a kind or side*; then refused - *ids OR kind and side, not both*. **RAN 2026-10-08:** both |
| **CW5** | `fragment --in "Project2" select-family-forms --set forms=all` | `notAFamily` true, refused, `elements` 0. **RAN 2026-10-08** |
| **CW6** | the job it exists for: CW1's positive, then `fragment --in "FormSelectProof" set-selection --expect-from select-family-forms`, then `read-selection` | `selectedCount` 2, and the read-back the same two - element numbers 2916 and 2928, which are b64 and b70. **RAN 2026-10-08.** The window in front was another family, so it was reached by name; **OWED:** the owner to bring FormSelectProof in front and see the two highlighted |
| **CW7** | `forms=<b6b>`, then `fragment --in "FormSelectProof" delete-elements --write --expect-from "select-family-forms where forms=<b6b>"` (no `--apply`) | ROLLED BACK; `askedFor` 1, `deleted` 5, `alsoWent` 4 - the void and its sketch. **RAN 2026-10-08**, and `forms=all` afterwards still read all three forms |
| **CW8** | `SELECT_BY_MATERIAL` on a PROJECT, main's code and this branch's side by side | the same answer, no family line. **RAN 2026-10-08 on Project2** (8 categories, 3 scanned, 0 of 'Default'): identical. A project with real materials is **OWED** |
| **CW9** | the second route: Manage > Select by ID in FormSelectProof with 2916 and 2928, compared with what CW6 highlighted | the same two forms. **OWED - the owner's eyes** |
| **CW10** | the signatures | `python brain/heron_validate.py accept select-family-forms --by "Ajmal PS"` and the same for `select-by-material`, on the CW1/CW2 records, on the owner's word only, after CW6 and CW9 |
| **CW11** | CW1 to CW7 on **Revit 2020** and **2027** | The same answers. Every API member it calls reads the same 2020 to 2027 (api-surface) and both compile on all eight releases; what Revit DOES was measured on 2024 only - including the null category, which is the reason for version 3 |

**Through the chat** (`revit_read` / `revit_change`) the new capability needs its row in the shared
knowledge store, which the rebuild from main after merging gives it; until then `heron_resolve` answers
*Nothing provides SELECT_FAMILY_FORMS*.
