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

The five fields plus what a fragment additionally needs ([09](09-skills-and-fragments.md)):

```yaml
heron-agent: HERON-REVIT-SEL-008
heron-step: 4
heron-status: DRAFT
heron-since: 0.1.0
heron-layer: revit

id: select-elements-by-category
capability: SELECT_ELEMENTS_BY_CATEGORY
purpose: Select every element of a given category in the pinned document.
source: OFFICIAL
revit: ">=2020"
runtime: [net472, net48, net8.0-windows, net10.0-windows]
risk: EXECUTE
inputs: { category: string, documentId: string }
outputs: { selected: int, documentName: string }
tests: tests/
```

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
