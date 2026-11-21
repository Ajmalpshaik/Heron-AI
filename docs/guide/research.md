[Heron AI](../../README.md) › [Documentation](../README.md) › **Research**

# Research — studied, never imported

Heron is built by people who read other people's work. **[D-25](../DECISIONS.md) is the rule: studied
and re-authored, never imported.**

```mermaid
flowchart LR
    S["<b>Study</b><br/>another project"]
    I["<b>Take the idea</b><br/>never the code · D-25"]
    W["<b>Write it in Heron's words</b><br/>the project not named · D-177"]
    D["<b>Arrives as DRAFT</b><br/>whatever it was elsewhere · D-44"]
    P(["<b>Proven here</b><br/>on a named model · D-30"])

    S --> I --> W --> D --> P

    classDef host fill:#FFFFFF,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef quiet fill:#F7F5F2,stroke:#0F0F10,stroke-width:1px,color:#0F0F10,stroke-dasharray:4 3
    classDef good fill:#3ECF8E,stroke:#0F0F10,stroke-width:2px,color:#0F0F10
    class S,I,W host
    class D quiet
    class P good
```

<details>
<summary>Same thing as plain text</summary>

```text
Study another project -> take the idea, never the code (D-25)
  -> write it in Heron's own words, the project not named (D-177)
  -> it arrives as DRAFT, whatever it was elsewhere (D-44)
  -> it is proven here, on a named real model (D-30)
```

</details>

- **No code is taken.** What is taken is the *thinking*, rewritten in Heron's own shape, and every
  capability starts at `DRAFT` here whatever status it held where it was read.
- **The project or person an idea came from is not named** in anything written since
  [D-177](../decisions/D-177.md). Research notes written before it still name the projects they studied;
  taking those names out is a separate change, made when the owner asks.
- **Where a project's licence forbade reading its source, its source was not read** — that is recorded
  per row in [33](../33-external-repository-research.md), not assumed.

## The research documents

| # | Document | Covers |
|---|---|---|
| 26 | [Prior Art: Existing Revit MCP Servers](../26-prior-art-revit-mcp.md) | What already exists, what it confirms, and **the two gaps every project shares** |
| 31 | [Studying The Existing Libraries](../31-studying-the-existing-libraries.md) | **How a fragment is studied and re-authored, never imported.** What travels (the mechanism, the scar) and what cannot (the code, the words, the proof); the owner's three rules — check and edit, add, split; and why 221 verified fragments arrive here as 221 unproven ones |
| 32 | [**The Master Architecture document, reconciled**](../32-master-architecture-reconciliation.md) | **Read this before building anything from [`HERON_AI_MASTER_ARCHITECTURE.md`](../../HERON_AI_MASTER_ARCHITECTURE.md).** The audit that document demands of itself, done against this repository: nine of its platform modules already exist here, four of them stricter than it asks for. What is genuinely missing, ranked — and partly closed since: of the seven things beside the Context Manager in [19](../19-context-and-cost.md), three are now real and four remain absent on purpose, three of them placed in the host by [D-58](../DECISIONS.md). And what is rejected, with reasons, so the same proposals are not made again. [D-57](../DECISIONS.md) |
| 33 | [The External Repository Research Matrix](../33-external-repository-research.md) | **The fifteen projects that document's §10 asks about, read twice**: from their pages, then by cloning and reading every one. The file-level pass **disagreed with the page-level one in ten of the fifteen entries** (§4a), corrected five licence cells and three of this document's own claims, and changed two decisions. **Nothing is adopted as code**, and one project is **AGPLv3** against Heron's Apache 2.0 |
| 34 | [The Patterns, Adapted](../34-patterns-adapted.md) | **Not *what* those projects are — *how* they get their result, and what that is worth to a modeller.** Each pattern's heading carries its verdict — built, already held, adopted as a rule, answered as a decision, or rejected — and `grep '^### 2\.' docs/34-patterns-adapted.md` lists them. Among those built: tiered depth and the cut marker, which together take a generation packet from **5,737 characters to 372 with the request byte-identical**. The ones that waited on the owner were answered as [D-60](../DECISIONS.md), [D-81](../DECISIONS.md) and [D-66](../DECISIONS.md). The lesson three of them taught from different directions: **a rule in code beats a rule in prose** |
| 35 | [Independent Study Notes](../35-independent-study-notes-open-design-awesome-llm-apps-openhands.md) | The second pass over the three repositories the upgrade plan referenced — corrections to it, stronger evidence, and what each one is worth to Heron specifically |
| 39 | [Three Downloaded Skill Sets, Studied](../39-three-downloaded-skill-sets.md) | **Another product's Revit connector notes, an office master prompt and a studio toolkit index, read lesson by lesson against Heron's code.** Nothing copied. A roof slope, a step-by-step building-shell skill, a defect in Heron's parameter fragments, and a list of what their connector does that Heron does not — left unbuilt on purpose |
| 45 | [Five More Downloaded Skill Files, Studied](../45-five-more-downloaded-skill-files.md) | **The same folder as 39, shared again with five new files, read line by line against Heron's code.** Nothing copied. Three defects in Heron's own fragments, DRAFT fragments widened and one added, a pre-IFC-export recipe, and a list of what touches a PROVEN fragment, left for the owner |
| 48 | [Studying a Commercial Revit AI](../48-studying-bimlogiq.md) | **Read from web-search summaries of its public pages, feature by feature against Heron's cards.** Nothing copied, nothing run. Where Heron is ahead, where it is behind, what reverse engineering is allowed (black-box study, never decompiling), and one build order |
| 51 | [Studying Another Revit MCP Platform](../51-studying-another-revit-mcp.md) | **An open-source Revit MCP platform (MIT), read in full at one commit and scored against Heron area by area — its name not recorded ([D-177](../decisions/D-177.md)).** Heron leads on safety, proof per operation and library depth; the other platform leads on what surrounds the change. Five ideas Heron already has, eight to build in one order, and what not to take. Nothing copied |
| 52 | [Revit Ribbon Coverage](../52-revit-ribbon-coverage.md) | **Every Revit ribbon tab, 2020 to 2027, checked tool by tool against Heron's library on 2026-11-01** - what is covered, what is missing, what Revit's API cannot do, and what is still unsure. A picture of that day, not a build list: ask `heron_lookup` before building anything it calls missing |
| — | [The research rule](../31-studying-the-existing-libraries.md#6-studying-an-outside-project---the-research-rule) | **How Heron studies an outside project - moved 2026-11-21 into 31 §6 from the upgrade plan**, a work note retired that day (git history holds it). What the plan produced is the Improvement Gate (`check-change.py`, `change-evidence.py`, `check-package.py`) |

## What studying gave Heron

Each idea below is something in Heron that is different for having read another project's work. The
projects are not named ([D-177](../decisions/D-177.md)).

| The idea | What it became here |
|---|---|
| A way to check a generated claim against its source that **calls no model at all** — and *"the human applies, the agent never submits"* | The second had already been reached here independently, as [D-30](../DECISIONS.md) |
| A chunk of text is embedded having lost its document | For a numbered standard, its heading path answers that for free |
| Two retrieval systems, read and **not adopted** | Both need servers Heron may not have. One's reviewable chunk boundaries stayed |
| A local document parser | Still an open question rather than a dependency |
| Vector search in a single file, with no server to install | What made [D-23](../DECISIONS.md) possible |

What each studied project became, including the ones **measured and rejected** — a rejection with a
number saves the next person a week — is in [34 — The Patterns, Adapted](../34-patterns-adapted.md).

---

↑ [Documentation](../README.md) · [Home](../../README.md)
