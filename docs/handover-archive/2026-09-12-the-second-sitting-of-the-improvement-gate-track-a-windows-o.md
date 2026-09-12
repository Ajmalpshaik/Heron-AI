# HANDOVER — 2026-09-12 (the SECOND SITTING of the improvement-gate track): a Windows-only failure nobody knew about, the sources re-read, and the housekeeping ledger retired

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Start here if you are picking this up.** The gate work below is merged. This sitting did three
things on top of it, all at the owner's direction, and each one found something.

### 1. The suite total was Linux-specific, and every document promised the wrong number

`tests/test_context.py` compared a source path against a hardcoded `/`. The value comes from
`heron_fragment.repo_relative()`, which is `os.path.relpath` and spells the path the way the **machine**
does — so the check passed here and failed on the owner's Windows PC, reported as
`brain\fragments\...`. **That machine saw 38 of 41 where this file and the `heron-ship` skill both
promised 39**, and that skill's only job is telling a later session which failures are theirs.

Fixed at the comparison, **not** inside `repo_relative()` — that value is written into the store's
`fragments.folder` column, and rewriting a persisted value is a far larger change than the defect
deserves. Normalising at the consumer is what this repository already does twice, in
`heron_fragment.fingerprint()` and `tests/test_carried_sources.py`.

**It could not be failed first on Linux**, so `change-evidence.py` ruled it `NO CHANGE MEASURED`, which
is the correct verdict and not a passing one. **[NEEDS-CHECKING](../NEEDS-CHECKING.md) A14 is the one run
that closes it** — on the PC, `python tests/test_context.py` should exit 0 with no fourth failure.

### 2. Every external source opened again, and one of my own claims did not survive

The brief's rule is to re-open each source and read the **implementation files**, not the READMEs. Four
of the five rows in [34 §2.15–2.19](../34-patterns-adapted.md) hold, one verified verbatim. **§2.17 did
not**: it said their check *"drives the lifecycle start to stop"*, and nothing in that repository does
— checked across the packaging script, the release gate, the prerelease tests, catalogue validation and
the convergence script. Corrected to what they actually do. **The Heron side is unaffected** —
`check-package.py` reading `Heron.addin` is still right; only the description of the source was wrong.

**A near-miss worth keeping.** §2.19 was almost written up as a second correction, because its three
severities were not in the folder the other two rows live in. Widening the search found them at once,
spelled exactly as recorded. *Absent from the folder I looked in* is not *absent*, and **a correction
needs its own evidence exactly like the thing it corrects.**

### 3. The independent review happened, and the housekeeping ledger retired

[Golden Rule 7](../14-golden-rules.md) wants one agent to create and another to validate. A different
session reviewed the 2026-09-10 housekeeping work by checking its claims **against the code** rather
than reading them. It holds up: *"no executable code was modified"* is true and precisely worded, the
deleted duplicate really was byte-identical, the Codex path bug was real, and **all 151 repository
paths named in the seven entry documents resolve.** One defect found — `tools/check-dependencies.py`
had no section in `tools/README.md` — and it was **later drift, not that work's**. Documented.

**The reviewer's own false finding is recorded too**, because the method is the point: the path check
first reported 95 of 151 broken, and every one was the checker resolving a document-relative path
against the repository root. The documents were right and the tool was wrong — the same shape as
`test_graph` and `test_reachable`, and the third time in two days.

**So the housekeeping execution record was retired**, which is what work notes do. Its last open item
moved to a permanent register first: **the cold read is now [NEEDS-CHECKING](../NEEDS-CHECKING.md) R3.**

### What is owed, and by whom

| | |
|---|---|
| **A14** | On the PC: `python tests/test_context.py` exits 0, and no fourth failure in the suite |
| **A12 · A13** | The add-in loads on each release; an upgrade and a rollback work |
| **R3** | **The cold read — and it needs a stranger.** Nobody who has worked in this repository can close it, which is why two sessions in a row have declined to |
| **Golden Rule 7, for the gate itself** | `check-change.py` has still only ever been run by the sessions that built it. `work-notes/plans/improvement-gate-execution-record.md` stays until somebody else points it at their own change |

**Measured here, on Linux, this sitting:** all gates exit 0 — `check-docs`, `check-metadata`,
`check-structure`, `check-package`, `check-licence`, `check-dependencies`. Derive the suite board
rather than reading a number here; the three that do not pass need the MCP SDK and a built .NET test
host. **`code=$?` on its own line** — `$?` after a pipe is the pipe's exit code, and that mistake
produced two optimistic numbers in this branch before `change-evidence.py` caught them.

---
