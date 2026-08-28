# 29 — The Heron Metadata Standard

> **Every artefact Heron creates carries metadata. No exceptions.**
>
> This is [Golden Rule 10](14-golden-rules.md) — *every important object must have identity, version and
> lifecycle* — made concrete enough to check with a script.
>
> Owner's instruction, 2026-08-27: *whatever you are creating, add metadata… create that standard policy
> checker.* Enforced by `HERON-STD-MET-014` and `tools/check-metadata.py`.

---

## 1. Why, beyond tidiness

Metadata here does one thing that matters more than documentation: it **ties the code to the registry**.

The [agent registry](28-agent-registry.md) says `HERON-MCP-SRV-001` exists and is built in Step 1.
Without metadata, nothing connects that claim to a file on disk. With it, a script can answer:

- Which file implements this agent?
- Which agents are claimed by code that does not exist in the registry?
- Which Step 1 agents have no implementation yet?

That is the difference between a registry that **describes** the system and one that **is** the system.

---

## 2. The five fields

Every Heron artefact carries these. Names are fixed; the comment syntax follows the file's language.

| Field | Meaning | Example |
|---|---|---|
| `Heron-Agent` | Which registry agent(s) this implements. Comma-separated | `HERON-MCP-SRV-001, HERON-MCP-CON-002` |
| `Heron-Step` | Build step that introduced it ([27](27-build-order.md)) | `1` |
| `Heron-Status` | Lifecycle stage ([24](24-trust-model.md)) | `DRAFT` |
| `Heron-Since` | Version it first appeared in | `0.1.0` |
| `Heron-Layer` | `bridge` · `revit` · `brain` · `platform` · `test` · `tool` | `bridge` |

`Heron-Agent` may be `none` for scaffolding that implements no agent — a build file, a test harness.
It may **not** be omitted; an absent field is a gap, `none` is a decision.

### C# and Python

```csharp
// Heron-Agent:  HERON-MCP-SRV-001, HERON-MCP-CON-002
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
```

### Fragments and skills — YAML front matter

The five fields plus what a fragment additionally needs ([09](09-skills-and-fragments.md)). **This is
what [`brain/heron_fragment.py`](../brain/heron_fragment.py) enforces**, and running
`python brain/heron_fragment.py` checks every fragment against it:

```yaml
heron-agent: HERON-REVIT-SEL-008
heron-step: 7
heron-status: DRAFT
heron-since: 0.1.0
heron-layer: brain

id: FRG-SEL-001
semantic-identity: put these elements in the current selection
kind: action                      # filter | action | recipe   (D-29)
domain: revit.selection
capability: SET_SELECTION
version: 1
source: OFFICIAL
risk: EXECUTE
purpose: Show the user what was found, on screen, where they can see it.

contract:                         # DATA, not prose            (D-29)
  needs:
    - name: uidoc
      type: UIDocument
    - name: elements
      type: IList<Element>
  provides:
    - name: selectedCount
      type: int

revit: ["2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027"]
runtime: [net472, net48, net8.0-windows, net10.0-windows]
tests: tests/

utterances:                       # what somebody actually types  (Step 9)
  - select these
  - highlight them on screen
  - OST_DuctCurves
```

**`utterances` is required, and Step 9 is why.** [09 §2](09-skills-and-fragments.md) recommended it —
*"utterances drive matching quality directly"* — and Step 7 built the shape without it. Step 9 then could
not find the category filter by typing `OST_DuctCurves`, because a capability named for the general case
contains none of the words a modeller uses for the specific one.

They are not documentation. They are **the input the exact-match short circuit actually runs on**: nobody
types *"all elements of one category in the active document"*, they type *"select all ducts"*. Revit's own
tokens belong here for the same reason — they are what gets pasted in, and they are precisely the class a
meaning-based search handles worst ([05 §4](05-heron-brain.md)).

**A fragment nobody can phrase a request for is unfindable**, and being findable is the whole point of
Steps 8 to 11 — so an empty list is refused rather than allowed as a to-do.

**Two fields changed when Step 7 built the validator**, and both changes are here rather than left as a
silent divergence between a document and the code that is supposed to implement it:

| Was | Is | Why |
|---|---|---|
| `inputs: { … }` / `outputs: { … }` | `contract: { needs: […], provides: […] }` | [D-29](DECISIONS.md) was taken after this document was written and says the contract is **data, not prose**: named and typed entries, so a tool can check that a filter's `provides` satisfies an action's `needs` before Revit is involved. A flat map of `name: type` could not carry the two sides separately |
| `revit: ">=2020"` | an explicit **list** | A range **claims every future release**. `">=2020"` asserts 2028 and everything after it, sight unseen — which is exactly what [D-05](DECISIONS.md) exists to forbid, and the same mistake `Directory.Build.props` already carries a comment about having made once (`>= 2027`) |

### Naming — the id, the capability and the folder

[docs/10 §6](10-memory-and-knowledge.md) asks for two things that pull against each other, and these
three fields are how both are had:

> *"Naming must be predictable and searchable"* … *"identity is an **ID**, never a name. Rename freely;
> identity survives."*

| Field | Shape | Rule |
|---|---|---|
| `id` | `FRG-<AREA>-<NNN>` — `FRG-SEL-001` | **Stable forever.** It carries the area and a number and **nothing else** — never the name, never the kind, never the version. Follows the agent registry's house style (`HERON-RAG-FMT-004`) in its own namespace, because a fragment is knowledge and an agent is code |
| `capability` | `SCREAMING_SNAKE_CASE`, **verb first** | `FILTER_ELEMENTS_BY_CATEGORY`, `SET_SELECTION`. A capability is a thing the system can *do*, so it reads as one. This is the searchable, renameable name |
| the folder | the capability, lower case, hyphens | `FILTER_ELEMENTS_BY_CATEGORY` → `filter-elements-by-category`. **Derived, never invented** — so a fragment can be found from its capability and cannot be quietly misfiled |

`AREA` is a fixed list in [`brain/heron_fragment.py`](../brain/heron_fragment.py) — `ELE`, `SEL`, `VIEW`,
`SHT`, `PAR`, `MEP`, `GEO`, `QA`, `DOC`. **An unlisted area is an error, never a guess**, which is
[D-05](DECISIONS.md)'s rule applied to a second table for the same reason: the moment it is open, one
fragment says `MEP` and the next says `MECH`, and no search finds both.

**Why an id may not carry the kind.** The first ids written here were `frg-filter-category-0001` and
`frg-action-setselection-0001` — readable, and wrong. A fragment that stops being a filter would carry
an id claiming it is one **forever**, and an id is the single thing that cannot be corrected without
breaking every reference into it. Kind changes; ids may not.

**Renaming a folder does not change what a fragment is, and Heron reports both facts separately.** Move
one and its identity is untouched — that is what the id is for — *and* it is now misfiled, which
`naming_problems()` says in those words. Two different questions, never confused for each other.

**`heron-status` is the fragment's lifecycle state.** There is no second `status` key. The temptation is
real — a fragment feels like it should own its own status field — and it is the two-homes-for-one-fact
failure this repository keeps catching, so it is refused here.

**A fragment implementation carries no `Heron-` header.** `impl/**/*.cs` is content the brain reads, not
Heron's own source, and its metadata is the `fragment.yaml` beside it. Putting `Heron-Status: DRAFT` in
the `.cs` next to `heron-status: DRAFT` in the YAML guarantees that a promotion updates one and not the
other. [`tools/check-metadata.py`](../tools/check-metadata.py) skips `brain/fragments/` for that reason
and says so where it does it.

### Reports and generated documents

```html
<meta name="heron-agent" content="HERON-RPT-RND-002">
<meta name="heron-status" content="PRODUCTION">
<meta name="heron-source-scope" content="PROJECT">
<meta name="heron-generated" content="2026-08-27T19:20:00Z">
```

**[NOTE]** `heron-source-scope` on a report is not decoration — it is what the
[Report Redaction Agent](28-agent-registry.md) reads to decide whether the report may leave the machine
([Golden Rule 12](14-golden-rules.md)). Metadata carrying a **policy decision** is the point.

---

## 3. What the checker enforces

`tools/check-metadata.py` — the working prototype of `HERON-STD-MET-014`:

| Check | Failure means |
|---|---|
| Every source file has all five fields | Someone added a file without declaring what it is |
| Every `Heron-Agent` id exists in the registry | Code claims an agent that was never specified |
| Every `Heron-Status` is a valid lifecycle stage | Two vocabularies creeping back in ([24](24-trust-model.md)) |
| Every `Heron-Layer` is one of the six | Layering drift |
| Every registry agent at or below the current step has an implementation | The registry is describing work that does not exist |

The last one is the valuable one, and it runs in reverse: it audits **the registry against the code**,
not just the code against the registry.

---

## 4. Deliberately small

**[NOTE]** Five fields, not twenty. Metadata standards fail by being too heavy — people stop filling
them in honestly, and a field everyone copy-pastes without thinking is worse than no field, because it
looks like signal.

The test for adding a sixth field: **would a script fail the build over it?** If not, it belongs in prose,
not in a header.

Three fields were considered and rejected: `Heron-Owner` (git already knows), `Heron-Reviewed-By`
(the PR knows), `Heron-Description` (the code should say).

---

## 5. Relationship to BIM standards

**[NOTE]** This is Heron's metadata about **itself**. It is a different thing from the BIM standards
Heron checks **models** against — naming conventions, shared parameters, ISO 19650 information
requirements ([Standards & BIM QA](28-agent-registry.md)).

They share a principle and nothing else: *an object without identity cannot be governed.* Do not merge
them. Heron's internal metadata is for a script; a BIM standard is for a delivery, and it exports to
[IDS and Model Checker checksets](10-memory-and-knowledge.md) rather than to header comments.
