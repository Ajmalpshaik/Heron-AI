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
different from a wrong answer. **Measured 2026-10-18, `check` reported every proof here STALE**; all
fifteen were run again on Revit 2020 and signed on 2026-10-25, and `check` is the only answer to
whether they still are. Which agents have a proof at all: `ls brain/agent-proofs/*.yaml` - the file name
is the agent's id in [the registry](../../docs/28-agent-registry.md).

The normalisation is not cosmetic. The fragment side learned it on 2026-09-06, when all sixteen proven
fragments reported STALE the first time they were read on a different operating system. None of them
was.

## One fingerprint is of one file

`source:` names the file a proof was signed for, and `check` reads the proof against **that file** -
not against whichever file claiming the agent sorts first today. Most agents here are one file. For
one that several files claim, `check` says so on a line of its own - which agents those are is that
command's answer, not a number typed here:

```
ONE FILE HERON-REVIT-VIE-013 - `list_views` was signed on ...; ... also claim this agent, and a change there does not show here
```

A proof whose signed file is gone, or no longer claims the agent, reads **STALE**, and its line names
the file it was signed on and what became of it - never another file read in its place.

The `ONE FILE` line is not a verdict and moves no exit code. It is there because *current* means **the file that
was signed has not changed**, and for an agent several files claim that is less than *the agent has not
changed*. A new draft is fingerprinted on the file its operation is in - the dispatcher's
`case "list_views":` hands the work to a class, and the file declaring that class is the one - and
carries the files it leaves out as a gap. Until 2026-11-11 it was the first file in name order whatever
the operation, which is how `VIE-013`'s proof of `list_views` came to stand on the picture code
([row 5b-1552](../../docs/fragment-issues/section-5b-rows-401-425.md)).

**A stale proof is run again, never edited.** On the PC, with Revit open on a saved scratch copy:
`track` for an agent that describes a model (two sessions, two different models) or `vary` for one that
takes an input, then `review`, then `accept --by` with the owner's own name -
[`../agent-proof-drafts/README.md`](../agent-proof-drafts/README.md) has each command, and
`python tools/prove-agent.py check` lists the ones that are stale today;
[Group NM](../../docs/needs-checking/group-nm.md) has the ones it listed on 2026-11-11.

**A record names its models and its sessions, and no Revit release.** *"Revit 2020"* for the
2026-10-25 run is in this page and in that pull request, not in the records themselves
([row 5b-1546](../../docs/fragment-issues/section-5b-rows-401-425.md)).

## What every proof carries

`moved` is the argument. A `list_*` agent has no negative case — it describes whatever model it is
handed — so [D-30](../../docs/DECISIONS.md)'s second leg is met by [D-53](../../docs/DECISIONS.md)
**tracking**: the answer must follow the input across two different models. An agent falling back to a
cached document would report the same numbers whatever it was given.

`gaps` is what the run could not establish. **Read it first.**
