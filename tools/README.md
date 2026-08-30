# tools

**Nine scripts that keep this repository honest.** Plain Python 3; the two that compile also need the
.NET SDK, and `check-fragments-compile.py` needs PyYAML. Run them from the repository root.

It said *"three small scripts that keep the documentation honest"* until 2026-08-29, which had been
wrong on both halves for a while: there are nine, and four of them check **code** rather than prose.
Counting them is the same discipline the rest of this file is about.

They exist because this repository already got its own numbers wrong twice — the agent registry
asserted **166 agents while its own departments summed to 196**, and a build-step figure said 20 where
the rows said 42. Both were caught by adding up columns by hand, which is not a process.

These are the working prototype of the **Documentation Validation Agent**
(`HERON-DOC-VAL-009`) and the **Agent Documentation Agent** (`HERON-DOC-AGT-002`) — see
[docs/28](../docs/28-agent-registry.md). When those agents exist, this is roughly what they do.

---

## `check-docs.py` — link and cross-reference integrity

```bash
python tools/check-docs.py
```

Verifies that:

- every relative markdown link resolves to a file that exists
- every `Golden Rule N` reference points to a rule defined in [docs/14](../docs/14-golden-rules.md)
- every `D-NN` reference points to a decision defined in [docs/DECISIONS.md](../docs/DECISIONS.md)
- every `Q-NN` reference points to a question defined in [docs/OPEN-QUESTIONS.md](../docs/OPEN-QUESTIONS.md)

Run it after any edit that moves or renames a document. It is how the Golden Rule renumbering
(ten rules to fifteen, [D-12](../docs/DECISIONS.md)) was verified across 31 files.

---

## `recount-agent-registry.py` — counts derived, never asserted

```bash
python tools/recount-agent-registry.py
```

Reads the agent rows in [docs/28](../docs/28-agent-registry.md) and **rewrites every stated count from
what it finds**: each department heading, the summary table, the header totals, and the figures in the
prose.

> A number in that document is never typed by hand. If it disagrees with the rows, the rows win.

Run it after adding, removing or re-tiering any agent.

---

## `check-structure.py` — the layout, and the layering

```bash
python tools/check-structure.py
```

Two questions that used to be answered by hand:

1. **Is every file in the part it belongs to?** Top-level folders mirror the four product parts,
   so *"where do I fix the Revit thing"* has one answer.
2. **Does any part depend on something it must not?** `platform` depends on nothing · `revit`
   and `brain` never touch each other · **`Autodesk.Revit` appears only inside `revit/`**.

The second matters more. A layering rule written only in a document gets broken quietly; a
layering rule in a script gets broken loudly, once, and then fixed.

The working prototype of `HERON-WSP-VAL-003` and `HERON-AHR-MON-011`.

---

## `check-metadata.py` — the standard, enforced

```bash
python tools/check-metadata.py
```

Enforces [docs/29](../docs/29-metadata-standard.md) — every source file declares its agent,
build step, lifecycle status, version and layer.

Then it does the thing that makes the standard worth having, and audits **in both directions**:
code claiming an agent that is not in the registry, *and* registry agents due by now that no file
implements. The second half is an honest to-do list rather than an error.

The working prototype of `HERON-STD-MET-014`.

---

## `check-compile.py` — does the C# actually build

```bash
python tools/check-compile.py                 # every version it can reach
python tools/check-compile.py 2020 2024       # just those two
```

Builds all four projects against every Revit version, using the Revit API reference assemblies from
NuGet. This is `A2`, `A3` and `A5` of [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) in one command instead
of one version at a time.

**It does not need Windows and it does not need Revit** — which is the point, because until
2026-08-28 not one `.cs` file here had been through a compiler at all. The first run found a real
2020-only error. [docs/30](../docs/30-compiling-away-from-windows.md) is the whole story.

**All eight releases, 2020 through 2027, compile off Windows** — but only with an SDK that carries the
WindowsDesktop MSBuild targets, because the add-in uses WPF for its ribbon icons. On Ubuntu that is
`dotnet-sdk-10.0`; the `dotnet-sdk-8.0` package omits them and stops at 2024. The script **probes for
those targets** rather than for an operating system, and when they are missing it skips the affected
releases and names the package to install. A skip is still never reported as a pass.

A pass means the API surface agrees; it is **not** evidence that anything behaves correctly.

---

## `check-fragments-compile.py` — does the *fragment* C# build

```bash
python tools/check-fragments-compile.py             # every release each claims
python tools/check-fragments-compile.py 2020 2024   # just those two
python tools/check-fragments-compile.py --keep      # leave the build tree to read
```

`check-compile.py` builds the four **projects**. Until 2026-08-29 **nothing built the fragments** — and
a fragment is C# that will one day be handed to Roslyn and run inside Revit ([D-28](../docs/DECISIONS.md)).
The part of the library that does the work was the one part no compiler had read.

A fragment is a **snippet**, not a file: it assumes names are in scope and leaves names behind
([D-29](../docs/DECISIONS.md)). So each one is wrapped in a method **whose parameters are its declared
`needs`**, generated from the contract rather than guessed.

**It checks the contract, not only the syntax.** After the snippet, every name the contract `provides`
is assigned to a local of the declared type — so a fragment that promises `IList<Element> elements` and
leaves something else, or leaves nothing of that name, **fails**. That half is worth more than the
syntax: a snippet that compiles while breaking its promise is the one that composes into something
broken later, far from here.

**Its first run found a real defect**, which is the same story `check-compile.py` has: `FRG-QA-001`
declared a value called `checked` — a **reserved C# keyword** — and its own code read `if (checked == 0)`.
It could never have compiled, and it had passed every other check, because nothing had ever asked
whether a contract name could be a variable. [`brain/heron_fragment.py`](../brain/heron_fragment.py)
now refuses that whole class instantly, so the compiler is the authority and the validator is the fast
half that stops the mistake being made.

Needs the .NET SDK (`dotnet-sdk-10.0` on Ubuntu) and no Revit and no Windows. **All 28 fragments compile
on all 8 releases, 2020 to 2027**, verified 2026-08-29. Errors point at the fragment's own file and line,
not at the generated wrapper.

Same limit as every compiler: it says nothing about **behaviour**. That is [D-30](../docs/DECISIONS.md)'s
proof with a negative case, and it needs a real model.

---

## `check-api-surface.py` — the releases the compiler cannot reach

```bash
python tools/check-api-surface.py                 # 2020 through 2027
python tools/check-api-surface.py 2025 2026       # just those
```

Reads the **compiled** add-in's reference tables — exactly the Revit types and members the code calls,
not what a regex over the source can find — and checks each one exists in that release's shipped
reference assemblies.

It was written when `check-compile.py` stopped at 2024 off Windows, leaving the newest three releases
with nothing checking them on the machines this project is actually worked on. **That is no longer
true** — `A5` established that all eight compile off Windows given `dotnet-sdk-10.0`, and the section
above says so — so this tool is now a second opinion rather than the only one for 2025-2027. **Matching
is by name**, so a changed signature passes here and would fail a real compile: it supplements the
compile gate, never replaces it.

**Validate it before trusting a clean result.** Put `doc.CreationGUID` back into
`RevitWrite.DocumentKey()`, build for 2024, and run it against 2020 — it must report the missing
member. A checker that finds nothing is evidence about the checker until it has caught something.

---

## `generate-agent-map.py` — the visual map

```bash
python tools/generate-agent-map.py
```

Builds a self-contained, filterable HTML page of every agent, grouped by department and layer, from the
same registry. Writes `agent-map.html` in the working directory; set `HERON_MAP_OUT` to change that.

Filter by tier, filter to the build steps, search by name, ID or description. Colour encodes **cost** —
T1 quiet, T3 loud — so the expensive agents are visible at a glance.

Because it is generated, the map cannot drift from the registry. Regenerate rather than edit.

---

## Why these are committed

They are small, they have no dependencies, and they encode three rules the project already learned the
hard way:

1. **A stated count is a claim; a derived count is a fact.**
2. **A cross-reference that is not checked is a cross-reference that is broken.**
3. **A generated artefact cannot lie about its source.**
4. **Code no compiler has read is a draft, whatever the documentation calls it.**

---

## `check-gaps.py` — what is unfinished, and what is only waiting

```bash
python tools/check-gaps.py
```

One sweep over everything built — the build order against what is on disk, every test run, every
checker, every agent id the code claims against the registry, the fragment library, the capability
registry, the graph, **whether the brain is reachable from the host at all**, and the register — sorted
into **two lists that must never be one**:

| | |
|---|---|
| **UNFINISHED** | somebody could do this here, today. **The exit code follows this list only** |
| **WAITING** | needs a real Revit, or Windows, or a reachable network. Not work anybody can do here |

**Why the split is the point.** This repository's recurring failure is not bugs — it is an unproven
claim quietly ageing into a believed one. *"Nobody finished this"* and *"nobody has a Revit"* look
identical in one list, and a reader taught to skim past the second stops seeing the first.

**It found two bugs in itself on its first run**: it scans files for the `Heron-Agent:` field, its own
source contains that pattern, and it duly reported its own regex as two undeclared agents. A scanner
that scans itself finds itself. It now skips its own file and anchors the match to a comment line.

**And on 2026-08-29 it was found reporting a green it had not earned.** Every section asked whether a
thing *exists*; none asked whether anything *calls* it. So it passed the whole of Phase 2 while all
eight `brain/` modules, seven fragments and ten skills sat unreachable — imported by nothing but their
own tests, and therefore unusable from a conversation. The `THE BRAIN` section was added for exactly
that, and was validated in both directions before its result was believed: with an import present it
reports the call site, with none it reports the gap. **The general lesson is worth more than the fix —
a checker written from a build order can only ever ask whether the build order was followed.**
