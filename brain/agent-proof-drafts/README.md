<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Agent proof drafts — evidence waiting for a person

Everything here was written by [`tools/prove-agent.py`](../../tools/prove-agent.py). **None of it is a
proof yet.**

A draft is what came back when one add-in agent was run against **two different models**: what each
answered, which numbers moved between them, and which held the same. It is arranged into the shape
[D-30](../../docs/DECISIONS.md) asks for and then it stops, because the last step is a judgement no
machine can make — *is that answer right for this building?*

`by:` is empty in every draft, deliberately. The field is required, so a draft that reached a proof
without a person's name on it would fail loudly instead of quietly counting as proven.

## Reading one

```
python tools/prove-agent.py review                        every draft
python tools/prove-agent.py review HERON-REVIT-LVL-027    one of them
```

**Read `gaps` first.** A draft with gaps is a job half done, not a proof with footnotes. Two gaps turn
up often and mean very different things:

**`NOTHING MOVED`** — every comparable number was identical across both models. That is **not** a pass.
It is exactly what an agent falling back to a cached document or the active view would produce. Either
the two models genuinely hold the same counts, or the agent is not reading the one it was given.

**`TRACKING IS THIN`** — some moved, more held the same. The ones that held are consistent with an
agent reading the model *and* with one ignoring it, so the whole argument rests on the few that moved.
No threshold is invented: the ratio is stated and the reader judges, because how much movement is
enough depends on which two models somebody chose.

## Accepting one

```
python tools/prove-agent.py accept HERON-REVIT-RM-028 --by "Ajmal PS"
```

Your name **is** the signature. This writes the proof to [`../agent-proofs/`](../agent-proofs/),
deletes the draft, and **leaves `Heron-Status` in the agent's own file exactly as it was.**

## What to do with a draft you do not believe

**Delete the file.** Nothing depends on it and rejecting one costs nothing.

**Do not sign a thin draft to clear the list.** A signature on weak evidence is worth less than no
signature, because it stops anybody looking again.
