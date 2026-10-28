# Needs checking — Group JF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JF - a parameter named by its GUID when two shared parameters carry one name: `SET_PARAMETER_VALUES_BY_ID` version 6 (2026-10-28)

From [row 5b-1344](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020 (session 54600), in a real
BIM 360 project, six volume control dampers each carried TWO writable shared parameters called *Equipment
Tag*, and `key=Mark|in=Duct Accessories;MOD070|Equipment Tag=VCD042;...` refused every row, because which one
was meant would be a guess. No Heron tool could name either.

**What changed.** [`impl/any/fragment.cs`](../../brain/fragments/set-parameter-values-by-id/impl/any/fragment.cs)
looks a parameter up by its name as spelt first, exactly as version 5 did. Only when nothing on the element is
called that, and the name ends in a GUID in braces - *JF Tag {9a6f71be-7b4f-4c3f-98dc-6301efab92db}* - is the
one parameter with that GUID taken, and only while it is still called what comes before the braces. Id rows,
a key row's `parameter=value`, the key and a narrowing all take it.

**What is done and what is not.** It compiles for all eight releases (`tools/check-fragments-compile.py`,
2026-10-28). **Nothing here has run in Revit.** The card is DRAFT until JF3 and JF4 pass on one release and
the owner signs; version 5's proof is kept on the card as a record and is stale.

**The model - scratch, never a working project.** A copy of
`C:\Program Files\Autodesk\Revit 2020\Samples\rac_basic_sample_project.rvt` saved as
`%TEMP%\heron-work\group-jf\JF Two Tags.rvt`. **The two parameters**, one shared parameter file each, so no
file ever holds one name twice:

| File | Group | Name | Type | GUID |
|---|---|---|---|---|
| `%TEMP%\heron-work\group-jf\jf-tag-a.txt` | JF | JF Tag | TEXT | 9a6f71be-7b4f-4c3f-98dc-6301efab92db (*A*) |
| `%TEMP%\heron-work\group-jf\jf-tag-b.txt` | JF | JF Tag | TEXT | 8481d09b-11b5-4293-8dbc-b79b846fb5e5 (*B*) |

The job file is [`tools/jobs/set-parameter-values-by-id-jf-2026-10-28.yaml`](../../tools/jobs/set-parameter-values-by-id-jf-2026-10-28.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JF0** | *Open.* The copy open and in front in Revit 2020, nothing else of the owner's in it | The title bar reads *JF Two Tags* |
| **JF1** | *Arrange, by hand, kept in the copy.* Manage > Shared Parameters, point at `jf-tag-a.txt`; Manage > Project Parameters > Add, Shared parameter, *JF Tag*, Instance, category Walls. The same again with `jf-tag-b.txt`. Put Revit's shared parameter file back to what it was. Give one wall the Mark *JF-WALL* | Project Parameters lists *JF Tag* twice. **If Revit refuses the second one by name, JF1 FAILS as an arrangement** - nothing below can mean anything, and the group needs a different one |
| **JF2** | *Check the arrangement.* `REPORT_PARAMETER_INVENTORY` on the JF-WALL wall's id, `parameterFilter=jf tag`, `fullRecord=true` | Two *JF Tag*, both String, shared, writable, GUIDs *A* and *B*, both blank; *"A NAME CARRIED TWICE"* names them |
| **JF3** | *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/set-parameter-values-by-id-jf-2026-10-28.yaml --session <pid>`, its positive leg: `key=Mark\|in=Walls;JF-WALL\|JF Tag {A}=ONE` | **`written` 1**; readBack name *JF Tag {9a6f71be-...}*, value *ONE*; `refused` empty; rolled back. **FAIL:** version 5's *"two parameters on it are called JF Tag"* |
| **JF4** | *The negative - the D-30 negative.* Its negative leg: `key=Mark\|in=Walls;JF-WALL\|JF Tag=ONE`, no GUID | `written` 0; `refused` names JF-WALL - two parameters on it are called JF Tag, and says to name the one meant as *'JF Tag {its GUID}'* |
| **JF5** | *Nothing kept.* JF2 again | Both *JF Tag* still blank |
| **JF6** | *The other one, and the wrong ones - by hand through the bridge client, each `--write` without `--apply` (rolled back).* `JF Tag {B}=TWO`; `Comments {A}=ONE`; `JF Tag {00000000-0000-0000-0000-000000000001}=ONE` | *B* written 1; the other two `written` 0, each refused as *"it has no parameter called ..."* |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept set-parameter-values-by-id --by
"Ajmal PS"` once JF3 and JF4 pass, and `heron-status` set to PROVEN in the same change. A row that fails
reopens 5b-1344; nothing in a row changes to make it pass.

### Results - Revit 2020, 2026-10-28

**Not run on the scratch copy.** The owner asked for the tool to be tried on his own model instead (*"try here, I
will save now the model"*), so the proof is D-120's: one run through try-once ([D-119](../decisions/D-119.md)) on
the saved *4355-BHVD-3D-50S10-BL001A* (a real BIM 360 project, Revit 2020, session 27048), kept by the owner,
and the negative rolled back beside it. Code run: fingerprint `623bf673ccc9f3f4`, version 6.

| # | Result | What it read |
|---|---|---|
| JF0 | NOT RUN | The scratch copy was made but never opened with the parameters added |
| JF1 | NOT RUN | The owner did not arrange the two *JF Tag* parameters; the file was unchanged |
| JF2 | PASS, on the owner's model | `REPORT_PARAMETER_INVENTORY` on dampers 7761415 and 8395771: two *Equipment Tag*, both String, shared, writable - *e133774c-3f62-45f1-a0c6-7e6c6238380b* (holds VCD001 on 7761415) and *cb0704fc-6be3-4f8a-85cf-84525659cbbd* (blank); both blank on 8395771 |
| JF3 | PASS, kept | `try-once`: `key=Mark\|in=Duct Accessories;530\|Equipment Tag {e133774c-...}=VCD020;531\|...=VCD021;532\|...=VCD022` - `written` 3, `refused` empty, readBack *VCD020*, *VCD021*, *VCD022* under *Equipment Tag {e133774c-...}*; the owner pressed Keep |
| JF4 | PASS | `validate --write --kept`, negative `key=Mark\|in=Duct Accessories;530\|Equipment Tag=VCD020`: `written` 0, refused *"two parameters on it are called Equipment Tag and more than one can be written - name the one meant as 'Equipment Tag {its GUID}' - not written (id 8395771)"*; rolled back |
| JF5 | PASS | The inventory again: *e133774c* reads VCD020-VCD022 on 8395771, 8395926, 8395994, *cb0704fc* still blank on all three |
| JF6 | NOT RUN | The other GUID, a GUID the element lacks and *Comments {GUID}* need the scratch copy |

**Signed by Ajmal PS on 2026-10-28** - `accept set-parameter-values-by-id --by "Ajmal PS"`, run in his own
terminal on his word *"now you can make it proven and promote"* - and the card set PROVEN in the same change.
JF0, JF1 and JF6 are still owed.

**What this group does not settle.** Whether Revit lets a PROJECT hold two project parameters of one name is
JF1's own question; the owner's project holds two *Equipment Tag* on every duct accessory, and where each
came from - the family or the project's parameters - is not read (no Heron tool reads the project's
parameter bindings).
