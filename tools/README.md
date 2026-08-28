# tools

Three small scripts that keep the documentation honest. Plain Python 3, no dependencies.
Run them from the repository root.

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
NuGet. This is `A2` and `A3` of [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) in one command instead of
one version at a time.

**It does not need Windows and it does not need Revit** — which is the point, because until
2026-08-28 not one `.cs` file here had been through a compiler at all. The first run found a real
2020-only error. [docs/30](../docs/30-compiling-away-from-windows.md) is the whole story.

Revit 2025+ needs the Windows Desktop SDK and is reported **SKIPPED** off Windows, never as passed.
A pass means the API surface agrees; it is **not** evidence that anything behaves correctly.

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
