# Session note — A NAME WITH A COLON IN IT DESTROYS THE PROOF IT WAS SIGNING

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A NAME WITH A COLON IN IT DESTROYS THE PROOF IT WAS SIGNING

**[Row 5b-122](../FRAGMENT-ISSUES.md), FIXED.** `tools/resign-machine-proofs.py` read end to end —
110 lines, never opened before. It is the **second** of the six unheld tools, and it was taken next
because it is the only one of the six that **writes into `brain/fragments/`** — the library
[Golden Rule 4](../14-golden-rules.md) says a record is never destroyed in.

Its `by:` and `date:` lines were built by string substitution — `"  by: %s" % args.by.strip()`.

**Measured on a fragment tree written for the purpose**, with `tool.FRAGMENTS` pointed at a temp
folder so the real library was never touched:

| what was typed | what it left behind |
|---|---|
| `--by "Ajmal: PS"` | a `fragment.yaml` that **no longer parses** |
| `--by "Ajmal #2"` | one that parses and is signed **`Ajmal`** — the rest is a YAML comment |
| `--by "   "` | `by:` **empty** — an unsigned proof, the one thing D-30 exists to prevent |
| a date with a quote in it | one that **no longer parses** |
| a name containing a newline | a key at the **top level** of the fragment |

**All five printed `signed` and exited 0.** Nobody is attacking this tool — it is run by hand, by
the one person whose name goes in — and these are the shapes an ordinary name has.

### The guard was next door the whole time

`heron_validate.py accept` writes the same field and does it with two things this had neither of:
it **refuses a blank `--by`** in one line, and it writes, **re-reads the file, and restores the
original** if anything moved. Both are here now — one step earlier, so nothing is ever
half-written:

- the name and the date go through **`yaml.safe_dump`**, so what needs quoting is quoted and a
  value spanning lines lands indented under its key rather than at the top level;
- every fragment is **built in memory and read back before any of them is written**. If the text
  would not parse, would not carry a proof block, or the name that comes back is not the name that
  was typed, **nothing is written at all** and the tool names the fragment that would have been
  damaged. A run that fails on the ninth of sixteen no longer leaves eight rewritten;
- `re.sub` takes a **lambda**, not a replacement string, because a backslash in a name is not an
  escape.

**`tests/test_resign_signature.py`** is the first suite this tool has ever had: **14 checks red
against the module as found**, all green after, and every one **fails rather than crashes**.

**Its work is already done** — `--list` against the real library answers *"No proof is signed by a
machine"*. So this guards a tool that fires rarely and writes to the one place that must not be
damaged.

**One thing measured and deliberately not fixed**, recorded in the row instead: `MACHINE` matches
its five words **anywhere** in the `by:` field, so a proof signed by a person who *also* names the
machine that gathered the evidence would be reported as machine-signed and rewritten. No such
proof exists in the library today, and inventing one to fix it would widen the change.

**Four of the six unheld tools remain**: `measure-graph`, `module-reach`,
`generate-decision-summary`, `recount-agent-registry`.
