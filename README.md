<p align="center">
  <img src="docs/brand/heron-readme-banner.png" alt="Heron AI - Design Automate Deliver" width="520">
</p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-Apache%202.0-0F0F10?style=flat-square" alt="Licence: Apache 2.0"></a>
  <a href="https://github.com/Ajmalpshaik/Heron-AI/actions/workflows/gates.yml"><img src="https://github.com/Ajmalpshaik/Heron-AI/actions/workflows/gates.yml/badge.svg?branch=main" alt="Gates on main"></a>
  <a href="#status"><img src="https://img.shields.io/badge/compiles%20for%20Revit-2020%20to%202027-FD641A?style=flat-square" alt="Compiles for Revit 2020 to 2027"></a>
  <a href="#status"><img src="https://img.shields.io/badge/status-in%20development-FD641A?style=flat-square" alt="Status: in development"></a>
</p>

<p align="center">
  <a href="#what-it-is">What it is</a> ·
  <a href="#how-it-fits-together">How it fits together</a> ·
  <a href="#install">Install</a> ·
  <a href="#where-to-go-next">Where to go next</a> ·
  <a href="docs/README.md">Documentation</a> ·
  <a href="CONTRIBUTING.md">Contributing</a>
</p>

# Heron AI

> [!NOTE]
> **Modellers: Heron can be installed now.** It is an early build and it is **not signed yet**, so
> Windows warns you the first time. What has been checked and what has not is under [Install](#install).
>
> **Programmers and AI engineers, your thoughts are very welcome.** Read the code and share your
> suggestions, ideas or corrections through an Issue or a Discussion.

**A modular, agent-based AI engineering platform for BIM.**

> The user focuses on BIM. Heron AI focuses on everything behind the BIM work.

## What it is

Heron AI lets a BIM Modeler, BIM Coordinator or engineer **drive Revit by describing the work in
plain words** — *"select all ducts"*, *"which views have no template?"*, *"lay out sprinklers in these
rooms"*.

Claude Code works out what you meant. Heron finds the right tool, runs it inside Revit, checks the
result and keeps a record. You never need to know C#, the Revit API or anything else behind it.

It is **not** a chatbot, a coding assistant, or a plain MCP server.
More on what you can ask: [**Using Heron**](docs/guide/using-heron.md).

## How it fits together

```mermaid
flowchart LR
    M(["<b>You, the modeller</b><br/><i>select all ducts</i>"])
    C["<b>Claude Code</b><br/>works out what you meant"]
    H["<b>Heron</b><br/>finds the right tool<br/>checks the rules"]
    S{{"<b>Changes switch</b><br/>off until you turn it on"}}
    R[("<b>Revit</b><br/>your model")]

    M -->|plain words| C
    C -->|MCP| H
    H -->|"read · always allowed"| R
    H -->|change| S
    S -->|"on · one Undo entry"| R
    R -.->|the answer| M

    classDef person fill:#F7F5F2,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef host fill:#FFFFFF,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef heron fill:#FD641A,stroke:#0F0F10,stroke-width:2px,color:#0F0F10
    classDef guard fill:#F5A524,stroke:#0F0F10,stroke-width:1.5px,color:#0F0F10
    classDef revit fill:#1B1B1F,stroke:#FD641A,stroke-width:2px,color:#F7F5F2
    class M person
    class C host
    class H heron
    class S guard
    class R revit
```

<details>
<summary>Same thing as plain text</summary>

```text
You, the modeller   "select all ducts"
     |  plain words
Claude Code         works out what you meant
     |  MCP
Heron               finds the right tool, checks the rules
     |-- read ------------------------------------> Revit  (always allowed)
     |-- change -> Changes switch, off by default -> Revit  (one Undo entry)
Revit -> the answer comes back to you
```

</details>

**Heron reads your model freely and changes nothing until you turn the Changes switch on.** Every change
it makes is one entry in Revit's Undo list. The full picture, part by part:
[**How Heron works**](docs/guide/how-heron-works.md).

## Install

The latest release is **[v0.2.0](https://github.com/Ajmalpshaik/Heron-AI/releases/latest)**: one file,
`HeronSetup-0.2.0-unsigned.exe`. It puts Heron into every Revit on your PC, 2020 to 2027, for your own
Windows user, with **no administrator rights**.

1. **Install Python once.** In Windows Terminal: `winget install Python.Python.3.12 --scope user`
2. **Close Revit**, and close Claude too if you want Heron added to Claude Code in the same go.
3. **Double-click `HeronSetup-0.2.0-unsigned.exe`.** Windows says *"Windows protected your PC"*
   because it is not signed yet: **More info**, then **Run anyway**.
4. In the **Heron Installer** window, leave the Revit years and both ticks on (*Set up Heron's AI
   side too* and *Add Heron to Claude Code*), press **Install**, wait for the report, then **Close**.
5. **Open Revit** and press **Heron** on the Heron tab. Then open Claude, **Code**, **This computer**,
   and ask, for example, *"total duct length by size"*.

**To remove it:** Settings › Apps › Installed apps › Heron AI › Uninstall. Your own Heron settings and
history stay.

**What has been checked, and what has not** (the release notes say the same):

| | |
|---|---|
| **Run on the owner's PC, 2026-11-16** | The Setup installed Heron into Revit 2020, 2024, 2026 and 2027, its Python side said `READY`, and Uninstall took everything back out |
| **Not checked yet** | Revit loading this exact build, and the *Add Heron to Claude Code* tick on a PC that is not a Heron developer's |
| **Never run** | Revit 2021, 2022, 2023 and 2025 |
| **Not signed** | No code-signing certificate yet ([D-190](docs/DECISIONS.md)). Every file on the release page is listed with its SHA-256 in `checksums.txt`, and the Setup's own SHA-256 is in the release notes |

Heron starts **read-only**: it changes nothing until you turn **Changes** on in its Settings window.
Everything a PC needs, step by step, and what is optional: [**What to install**](docs/WHAT-TO-INSTALL.md) ·
the disk cost of each piece: [**System requirements**](docs/guide/system-requirements.md).
Something wrong? Please [open an Issue](https://github.com/Ajmalpshaik/Heron-AI/issues).

## Where to go next

| You are… | Go to | You will find |
|---|---|---|
| **a BIM modeller** | [**Using Heron**](docs/guide/using-heron.md) | What you can ask, and what keeps your model safe |
| **setting up a PC** | [**What to install**](docs/WHAT-TO-INSTALL.md) · [**System requirements**](docs/guide/system-requirements.md) | Everything a PC needs, Python first, what is optional, and what each piece costs on disk |
| **changing Heron** | [**Changing Heron**](docs/guide/changing-heron.md) | Where each kind of code lives, the rules, and the checks before a pull request |
| **checking what is proven** | [**What is proven**](docs/guide/what-is-proven.md) | Built versus proven, item by item, and the commands that count it |

**Anyone else:** the owner starts at [FOR-THE-OWNER](docs/FOR-THE-OWNER.md) · an AI agent at
[AGENTS.md](AGENTS.md) · a BIM manager at [16 — Version Support](docs/16-version-support-strategy.md)
and [12 — Security & Permissions](docs/12-security-and-permissions.md). **Every document:**
[the documentation front door](docs/README.md).

## In short

Each card is the one-line version of a page. Click through for the whole of it.

### Four parts

**Heron Revit** (the add-in, in `revit/`) · **Heron MCP** (the bridge to the AI, in `mcp/`) ·
**Heron Brain** (the library and the calculations, in `brain/`) · **Heron Platform** (install,
update, settings, in `platform/`).
→ [How Heron works — Four parts](docs/guide/how-heron-works.md#four-parts)

### One request, step by step

You type; Claude Code asks Heron which tool fits; Heron runs it inside Revit in one named
`TransactionGroup`; the change is **one** entry in Revit's Undo list.
→ [How Heron works — One request, step by step](docs/guide/how-heron-works.md#one-request-step-by-step)

### What keeps the model safe

Heron only reads until **Changes** is on ([D-19](docs/DECISIONS.md)). A change from the chat runs
only a `PROVEN` tool ([D-117](docs/DECISIONS.md)); with **Ask to Keep** on, Revit asks **Keep or
Discard** first ([D-126](docs/DECISIONS.md)); and Ctrl+Z takes the whole change back.
→ [What keeps the model safe](docs/guide/safety.md)

### Status

**Built and proven are different words here, on purpose.** The bridge — Phase 0 — is proven in Revit
2020 and 2024 ([HANDOVER §3](docs/HANDOVER.md), [field notes](docs/00e-field-notes-proven-bridge.md));
most other parts are built and still owed a real Revit. No count is typed on these pages: how many
fragments are `PROVEN`, `DRAFT` or `DEPRECATED` comes from
`grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`, and
`python tools/check-gaps.py` derives what is unfinished.
→ [What is proven](docs/guide/what-is-proven.md)

---

**Licence:** [Apache 2.0](LICENSE) · [Disclaimer](DISCLAIMER.md) · [Security](SECURITY.md) ·
**Owner:** Ajmal PS · **Domain:** BIM / Revit engineering automation
