# Project skills

Skills that ship **with this repository**, so anyone working on Heron — a contributor, or an AI
assistant helping one — gets the same house rules without being told them.

They are part of the product, not personal configuration. They are written for whoever is reading, name
no individual, and assume no knowledge of any other project.

| Skill | Covers |
|---|---|
| [revit-version-support](revit-version-support/SKILL.md) | Which Revit release needs which .NET runtime, how to build for one, and every API break from 2020 to the latest supported release |
| [revit-addin-conventions](revit-addin-conventions/SKILL.md) | Where code goes, the metadata header, the path rule, the Revit threading constraint, transactions and undo, how to word a message, and the checks to run |
| [revit-ribbon-and-windows](revit-ribbon-and-windows/SKILL.md) | Ribbon buttons, why one fails to appear, and the `ExternalEvent` pattern a modeless window must use to reach the Revit API without crashing Revit |
| [fragment-proving](fragment-proving/SKILL.md) | How to arrange a fragment's proof so the answer means something, the five mistakes that account for nearly every failed one, and how to run a batch of them |
| [heron-guard](heron-guard/SKILL.md) | **A hook, not a document.** Refuses an edit that would put the Revit vendor namespace outside `revit/` **at the moment the edit is proposed**, rather than when somebody remembers the sweep. Wired from [`../settings.json`](../settings.json), so it runs in **every** session and not only one that loaded the skill. Deny-tier, fails closed, `HERON_GUARD=off` turns it off |
| [heron-session](heron-session/SKILL.md) | **Hooks, not a document.** At every session start, one line: where the branch stands against `origin/main`, and how many fragments are PROVEN and DRAFT. Before a pull request is merged or marked ready, fetches `origin/main` and tells the AI which commits the branch does not have. **Advice only — neither ever blocks.** Wired from [`../settings.json`](../settings.json) like the guard |
| [heron-ship](heron-ship/SKILL.md) | **What to run before pushing, in what order, and which failures are the machine rather than the change** — stating a change's intent and its before/after evidence, the four gates you run and the **ten CI decides on**, the reports whose findings are questions, the checker whose exit code follows the unfinished list, and the six that need a compiler or a store this container has not got |

## What belongs here

A rule that is **specific to Heron** and would otherwise have to be rediscovered — the kind of thing
that costs someone an afternoon and then is forgotten before the next person hits it.

General Revit knowledge that any add-in author would already have does not belong here. Neither does
anything derivable by reading the code.

## Keeping them true

A skill that drifts from the code is worse than no skill, because it is believed.

> **Fix a skill the moment it is found to be wrong** - not in a follow-up task. A known-wrong skill left
> in place will be followed by the next person who reads it.

That applies to using one as much as to changing code: if guidance here turns out to be mistaken,
incomplete or stale while you are following it, correcting it is part of the task you are already doing.

Four agents in [`../agents/`](../agents) carry that work, each a working prototype of an agent already
specified in [the registry](../../docs/28-agent-registry.md):

| Agent | For | Registry |
|---|---|---|
| `skill-finder` | Finding which skill applies, and saying when none does | `HERON-RAG-SMT-005`, `HERON-RAG-RNK-006` |
| `skill-author` | Writing a new skill, once nothing existing covers it | `HERON-FRG-CRE-007` |
| `skill-maintainer` | Correcting one that has drifted from the code | `HERON-FRG-EVO-005`, `HERON-FRG-UPD-008` |
| `skill-recorder` | Capturing what a session learned, before it is lost | `HERON-LRN-EXT-003` |

The same folder also holds `heron-*.md` files, and they are **a different kind of thing**: not house
rules for whoever works on Heron, but Heron's own background agents for a modeller - the first is
`heron-model-auditor`, which audits the open model with Heron's read tools while the modeller keeps
working. They sit here only because this is where the host reads agents from.
[PROJECT-MAP](../../docs/PROJECT-MAP.md) says where they fit; `tests/test_agent_tools.py` holds what
they may call.

Finding and selecting a skill is **retrieval** - the read half, and part of RAG. Creating, updating and
recording one is the **knowledge lifecycle** - the write half, and not RAG. The vector index belongs to
the read half only, and is deliberately not built yet: with this few skills, matching on the frontmatter
description finds the right one exactly, and [Golden Rule 11](../../docs/14-golden-rules.md) keeps the
index an index - the skills themselves are always the source of truth.

Where a skill and the repository's own checks disagree, **the checks win** — they are executable and the
prose is not:

```bash
python tools/check-docs.py
python tools/check-metadata.py
python tools/check-structure.py
python tests/test_bridge_roundtrip.py
```
