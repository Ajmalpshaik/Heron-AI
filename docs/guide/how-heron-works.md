[Heron AI](../../README.md) › [Documentation](../README.md) › **How Heron works**

# How Heron works

Four parts, one path for every request, and two places it can run. For the code-level map —
which folder owns what — see [PROJECT-MAP](../PROJECT-MAP.md).

## Four parts

| Part | Responsibility | Folder |
|---|---|---|
| **Heron Revit** | The add-in, the ribbon, and everything touching the Revit API | [`revit/`](../../revit/README.md) |
| **Heron MCP** | The controlled bridge between the AI and Revit, and the Companion page | [`mcp/`](../../mcp/README.md) |
| **Heron Brain** | Fragments, skills, capabilities, knowledge, design calculations | [`brain/`](../../brain/README.md) |
| **Heron Platform** | Install, update, configuration, switches, packages | [`platform/`](../../platform/README.md) |

## How it fits together

```mermaid
flowchart TD
    U(["<b>BIM modeller</b><br/><i>select all ducts</i>"])
    CC["<b>Claude Code</b> — the host<br/>works out what you meant"]
    MS["<b>Heron MCP server</b> — Python<br/>the tools · finds the capability"]
    BR["<b>Heron Brain</b> — Python<br/>fragments · skills · knowledge · calculations"]
    SR["<b>Search</b> — finds the capability<br/>word match · built in, always on<br/><i>model2vec</i> — reads meaning · optional"]
    AD["<b>Heron Revit add-in</b> — C#<br/>compiles and runs the fragment<br/>a change: one named TransactionGroup"]
    RV[("<b>Revit</b><br/>2020 → latest")]
    CP["<b>Heron Companion</b><br/>a page beside Revit<br/>live view · editable tables"]
    PL["<b>Heron Platform</b><br/>install · update · config · switches"]

    U ==>|plain language| CC
    CC ==>|MCP| MS
    MS <==>|which fragment, which method| BR
    BR -.- SR
    MS ==>|named pipe · this PC only| AD
    AD ==>|ExternalEvent · Revit's main thread| RV
    RV -.->|answer · one Undo entry per change| U
    MS -.-> CP
    AD -.-> CP
    CC -.- PL
    MS -.- PL
    AD -.- PL

    classDef person fill:#F7F5F2,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef host fill:#FFFFFF,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef heron fill:#FD641A,stroke:#0F0F10,stroke-width:2px,color:#0F0F10
    classDef quiet fill:#F7F5F2,stroke:#0F0F10,stroke-width:1px,color:#0F0F10,stroke-dasharray:4 3
    classDef revit fill:#1B1B1F,stroke:#FD641A,stroke-width:2px,color:#F7F5F2
    class U person
    class CC host
    class MS,BR,AD heron
    class SR,CP,PL quiet
    class RV revit
```

<details>
<summary>Same thing as plain text</summary>

```text
BIM modeller          plain language
     |
Claude Code           host: works out what the modeller meant (D-01)
     |  MCP
Heron MCP server      Python: the tools, finds the capability  <->  Heron Brain: fragments, skills,
     |  named pipe, this PC only                                      knowledge, calculations
     |                                                                Search: word match (built in),
     |                                                                model2vec (optional)
Heron Revit add-in    C#: compiles and runs the fragment; a change runs in one named TransactionGroup
     |  ExternalEvent, Revit's main thread
Revit                 2020 -> latest

Heron Companion       a page beside Revit: live view of Heron's work, editable tables
Heron Platform        install, update, config, switches - under all of them
```

</details>

- **Solid arrows are the live request path.**
- **Dotted lines** are what comes back, the Companion page showing what Heron does, and Heron Platform
  holding the other parts up — it installs, updates and configures them rather than sitting in the
  call chain.

## One request, step by step

1. You type *"set the Mark of these doors to D-01"*.
2. Claude Code asks `heron_lookup` which capability serves it. Heron's search matches your words — by
   meaning too, when model2vec is installed.
3. It calls `revit_change` with your words. The server sends the job down the named pipe to the add-in.
4. The add-in compiles the fragment and runs it on Revit's main thread, inside one named
   `TransactionGroup`.
5. With **Ask to Keep** on, Revit shows the change and asks **Keep or Discard**; Discard, Esc or closing
   the box rolls it back ([D-126](../DECISIONS.md)). With it off — the default — the change is kept at
   once ([D-138](../DECISIONS.md)).
6. Either way the change is **one** entry in Revit's Undo list, named *"Heron: …"*. Ctrl+Z takes it back.

What decides whether step 3 may run at all — the switches and the proof — is on
[What keeps the model safe](safety.md).

## What runs where

```mermaid
flowchart LR
    subgraph PC ["Windows PC with Revit"]
        direction TB
        C1["<b>Claude Code</b>"] --> H1["<b>Heron</b><br/>every tool"]
        H1 -->|"named pipe · this PC only"| A1["<b>Heron add-in</b><br/>inside Revit"]
        A1 --> R1[("<b>Revit</b>")]
    end
    subgraph CL ["Cloud session · not Windows"]
        direction TB
        C2["<b>Claude Code</b>"] --> H2["<b>Heron</b><br/>library tools only<br/>no revit_ tool"]
    end

    classDef host fill:#FFFFFF,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef heron fill:#FD641A,stroke:#0F0F10,stroke-width:2px,color:#0F0F10
    classDef quiet fill:#F7F5F2,stroke:#0F0F10,stroke-width:1px,color:#0F0F10,stroke-dasharray:4 3
    classDef revit fill:#1B1B1F,stroke:#FD641A,stroke-width:2px,color:#F7F5F2
    class C1,C2 host
    class H1,A1 heron
    class H2 quiet
    class R1 revit
    style PC fill:#F7F5F2,stroke:#0F0F10,color:#0F0F10
    style CL fill:#F7F5F2,stroke:#0F0F10,color:#0F0F10,stroke-dasharray:4 3
```

<details>
<summary>Same thing as plain text</summary>

```text
Windows PC with Revit    Claude Code -> Heron (every tool) -> named pipe, this PC only
                         -> Heron add-in inside Revit -> Revit
Cloud session            Claude Code -> Heron (library tools only, no revit_ tool)
```

</details>

- **The pipe never leaves the PC.** The add-in is a named-pipe server, local-only by construction
  ([D-02](../DECISIONS.md)).
- **Off Windows there is no Revit to reach**, so the server serves only its library tools and no
  `revit_` tool ([D-132](../DECISIONS.md)). Everything that needs a model runs on the modeller's
  Windows PC.
- Where a cloud session is set up, and what it runs: [38 — The Cloud Environment](../38-the-cloud-environment.md).

## How Heron finds the right tool

**Search is how the brain finds the capability for your words.**

| Level | What it does | Installed |
|---|---|---|
| **Word match** | Matches letters and words | Built in, always runs |
| **model2vec** | Reads meaning — *"stop the air going the wrong way"* can reach the flow-direction check without sharing a word with it | Optional |
Without model2vec Heron uses the word match, and **every answer says which search ran**. Measured on
2026-11-26 at 753 fragments, neither was clearly better on the owner's questions, so model2vec stays
optional. A third level, a **re-ranker** that gave the top picks a second, slower read, was removed on
2026-11-26 ([D-201](../decisions/D-201.md)): no clear gain, and the largest download Heron had.
[WHAT-TO-INSTALL §2](../WHAT-TO-INSTALL.md#2-optional--the-trained-search-model) has the figures and how
to install or remove model2vec.

## Go deeper

| Document | Covers |
|---|---|
| [PROJECT-MAP §A](../PROJECT-MAP.md#a-how-a-request-travels) | The same path, folder by folder |
| [02 — Architecture Overview](../02-architecture-overview.md) | Layers, four parts, agent tiers |
| [03 — Heron Revit](../03-heron-revit.md) · [04 — Heron MCP](../04-heron-mcp.md) · [05 — Heron Brain](../05-heron-brain.md) · [06 — Heron Platform](../06-heron-platform.md) | One part each |
| [25 — Multi-Session & Document Binding](../25-multi-session-and-binding.md) | One chat, one Revit, and the pinned document |

---

↑ [Documentation](../README.md) · [Home](../../README.md)
