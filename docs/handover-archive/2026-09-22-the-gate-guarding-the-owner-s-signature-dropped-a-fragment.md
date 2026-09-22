# Session note — THE GATE GUARDING THE OWNER'S SIGNATURE DROPPED A FRAGMENT AND SAID NOTHING

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE GATE GUARDING THE OWNER'S SIGNATURE DROPPED A FRAGMENT AND SAID NOTHING

**[Row 5b-130](../FRAGMENT-ISSUES.md), FIXED. [Row 5b-131](../FRAGMENT-ISSUES.md) raised, and it is
the owner's.** `tools/check-signatures.py` read end to end — 155 lines, never opened before. The
**fourth** of the seven unread CI gates.

It guards the scarcest input this library has. It exists because **thirteen fragments the owner
had already signed were sitting at DRAFT** on 2026-09-13, so the next proving round offered them
to him to prove **again** — his own complaint, in his own words, and *the worst possible thing to
be right about*.

**`findings()` called `F.load_all()` and discarded the problems it returns.** Measured, on a
library of three built by copying real fragments, one of them a `fragment.yaml` that will not
parse:

```
  Signatures in the library: 1
  exit 0
```

The broken fragment is never named, and nothing anywhere says one could not be read. **If the one
it cannot read is the one carrying an unused signature, this gate says nothing is waiting and the
owner signs it again** — the exact failure it was built to prevent, one level up, and D-52's
plausible zero wearing this tool's own face.

The problems are printed under **COULD NOT BE READ** now, naming each. The headline reads
`Signatures in the library: 331, across 396 fragment(s) read`, so the two numbers sit together and
neither stands alone, and **the gate does not pass** while a fragment cannot be read.

**Also fixed, smaller**: its usage line documented `--all` as *"including what is fine"*, and
`--all` adds exactly one sentence and lists nothing — measured by diffing the two runs. The line
says what it does.

### The three-way split is right, and it is the good part

**UNUSED** fails the gate. **STALE** does not, because re-proving is D-30 working rather than a
fault. **HELD** does not either, because a fragment signed and meant to wait carries `proof-held:`
in its own file **with the reason**, so a reader sees why and the tool can tell a hold from a
forgotten promotion. That third answer was added after `create-roof` was reported as an oversight
on 2026-09-19 and was not one. It also asks `can_promote` rather than re-deriving the answer, so a
rule added there is honoured here for free.

### One fixture fault caught while writing the suite, and it is worth knowing

**Every copied fragment reads STALE on arrival.** `fingerprint()` hashes the file's **path** as
well as its content, and a fragment read from outside the repository comes back with `..` segments
— which `heron_fragment` states in terms, naming the store's own tests as the case. The copies are
re-stamped against themselves now, so the cases about waste are about waste; the one case that is
about an unreadable file mutates *after* the re-stamp.

### What is left, and it is the owner's

**[Row 5b-131](../FRAGMENT-ISSUES.md).** A machine's name in `by:` is counted as a signature by the
gate that counts signatures — measured with `Claude Opus 5, at Ajmal PS's PC`. D-30 says the
machine gathers evidence and a **person** signs, and `resign-machine-proofs.py` exists because
sixteen fragments carried that exact string.

**Not a live failure** — zero fragments are machine-signed today — but nothing in CI runs that
tool, so nothing would notice the first one. **The question is where the answer lives**, not what
it is: a constant in `heron_fragment`, an import sideways from a hand-run tool, or a CI step that
runs `resign-machine-proofs --list`. The suite **prints that measurement rather than asserting
it**, on purpose: asserting today's behaviour would lock in the thing the row is open about.

**Three CI gates remain unopened**: `check-products` (527), `check-fragments-compile` (400),
`check-intrusion` (213).
