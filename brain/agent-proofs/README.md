<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Agent proofs — evidence somebody signed

One file per add-in agent, keyed by its id, written by
[`tools/prove-agent.py`](../../tools/prove-agent.py) when a person accepted a draft under their own
name.

**These sit beside [`../agents/`](../agents/), which holds the CONTRACTS, and the pairing is
deliberate: the contract is the promise and the proof is the evidence.** Two files, one key, neither
pretending to be the other.

## Why they are not inside the agent

A fragment carries its `proof:` block inside its own `fragment.yaml`. An add-in agent is a `.cs` file
and cannot. So the proof lives here — and the tool that writes it **refuses any path under `revit/`**,
which is what makes *"it never promotes an agent"* a property of the code's reach rather than of its
good intentions.

## A proof is not a status

**Every `.cs` file in `revit/Heron.Revit.Addin/` still says `Heron-Status: DRAFT`, and a proof here
does not change that.** Promoting an agent is a separate, deliberate act and it belongs to the owner.
These record what was observed; they do not confer anything.

## Reading one

```
python tools/prove-agent.py check      which proofs have gone stale
```

`fingerprint` is a hash of the agent's source, normalised to `/` separators and `\n` line endings. If
the source changes, `check` reports **STALE** — an answer about code that has since moved on, which is
different from a wrong answer.

The normalisation is not cosmetic. The fragment side learned it on 2026-09-06, when all sixteen proven
fragments reported STALE the first time they were read on a different operating system. None of them
was.

## What every proof carries

`moved` is the argument. A `list_*` agent has no negative case — it describes whatever model it is
handed — so [D-30](../../docs/DECISIONS.md)'s second leg is met by [D-53](../../docs/DECISIONS.md)
**tracking**: the answer must follow the input across two different models. An agent falling back to a
cached document would report the same numbers whatever it was given.

`gaps` is what the run could not establish. **Read it first.**
