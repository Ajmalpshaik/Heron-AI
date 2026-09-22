# Session note — A SIGNATURE THE PROOF TOOL COULD NOT WRITE WAS RECORDED AS SIGNED

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A SIGNATURE THE PROOF TOOL COULD NOT WRITE WAS RECORDED AS SIGNED

**[Row 5b-139](../FRAGMENT-ISSUES.md), FIXED.** `tools/prove-agent.py` read end to end — 934 lines, **and
no suite loaded or ran it.**

**The measurement changed once every tool was named by a suite.** The new question is which tools a
suite *executes* rather than mentions — a call site is not an execution, and a path in a docstring is
neither. Four never-read tools are named only in prose and are not run by CI either:
`prove-agent` (934), `check-skill-routing` (770), `check-risk-crossings` (440), `api-changes` (208).
This is the costliest of them: it is the only route out of DRAFT for the 26 add-in agents.

> **A correction worth keeping.** `generate-agent-map.py` looked unheld and is not — CI runs it in
> *"Re-run the generators and diff"* (`gates.yml:295`), which builds the path from a loop variable, so a
> substring search of the workflow misses it. It also has a **recorded reason** for having no suite, in
> `tests/test_catalog.py`: *"A generator that only draws needs no test. One that concludes does."*

#### What was wrong

`as_yaml`'s `quoted()` escapes the apostrophe single-quoted YAML needs **and nothing else**, and
`read_yaml` **silently skips any line it does not recognise**. Measured on a draft written by the tool
itself:

```
accept HERON-REVIT-LVL-027 --by "Ajmal<newline>PS"
  ->  printed "signed by Ajmal / PS", exit 0
  ->  wrote  by: 'Ajmal      (a bare quote, not a name)
  ->  DELETED THE DRAFT
```

The draft is the evidence and `accept` removes it on the way out. **A signature cannot simply be given
again** — it was given against two live Revit sessions with two particular models open.

**And the second line can be a key.** `--by "Ajmal PS<newline>heron-status: PROVEN"` put **`PROVEN'`**
into the proof's own status — three lines below where `cmd_accept` deliberately sets `DRAFT` and prints
*"Promoting it is a separate, deliberate act, and it is yours."* Nothing automated acts on that field
today, and that is part of the finding: **nothing but this tool reads `brain/agent-proofs/`**, measured
across the repository. The files are read by people, and linked from `NEEDS-CHECKING.md` as the
evidence for a signed agent.

#### Traced and not raised — the obvious guess is wrong

A **colon** in the name is sound here: `--by "Ajmal PS, BIM Lead: Heron"` round-trips exactly. That is
where [row 5b-122](../FRAGMENT-ISSUES.md) found `resign-machine-proofs.py` broken. **This writer got the
apostrophe right and the line break wrong** — a different hole in the same wall.

#### The fix

`read_yaml` splits into `parse_yaml(lines)` + `read_yaml(path)`, and an `unwritable(body)` renders the
body and **reads it straight back in memory, before anything is written or deleted**, refusing by name
any field that would not survive. Both writers go through it. `cmd_accept` also **reads the file back
off disk and confirms the signature before removing the draft** — which covers what the in-memory guard
cannot: a short write, a full disk, an encoding the platform would not take.

#### Two teeth proofs left the suite green, and that was the suite's fault

It never called `write_draft` with a bad value, and the read-back's failure path was unreachable while
the in-memory guard held. Both have a case now.

| Break | Red |
|---|---|
| **the module exactly as found** | **6** |
| the in-memory guard alone | 2 |
| the read-back alone | 2 |
| `write_draft`'s guard | 2 |
| the `revit/` write refusal | 1 |
| the fingerprint's line-ending normalisation | 1 |
| `elements` back in the envelope unconditionally | 1 |

#### Seven things measured and found RIGHT

Recorded so nobody re-reads them: the refusal to write anywhere under `revit/`, which makes *"it never
promotes an agent"* a property of the code's reach rather than its intentions; the fingerprint
normalising path separators and line endings, after hashing raw bytes made all sixteen proven fragments
read STALE in a Linux container; `compare` treating the document's own name as the input rather than
the answer; `elements` skipped as a scalar total but read as the answer inside a list; `parse_args`
refusing a pair with no `=`; the empty `--by` refusal; and `vary` refusing fewer than three inputs —
the same bar `brain/heron_validate.py` enforces for a fragment.

**Recorded, not fixed:** `cmd_vary` writes its `tracking:` rows as **Python dict reprs inside
single-quoted YAML strings**. Measured — it round-trips and a person can read it, but it is a Python
repr in a proof, and changing it would change the draft format rather than close a hole.

**The suite never opens a bridge.** `track`, `vary` and `sessions` need two live Revit sessions with
different models — row `T1` and its neighbours in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md).
