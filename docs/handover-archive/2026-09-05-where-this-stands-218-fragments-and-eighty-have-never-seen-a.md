# WHERE THIS STANDS, 2026-09-05 — 218 fragments, and eighty have never seen a compiler

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


The library is still growing away from the PC. What is left to *prove* still needs a machine this
container does not have — and as of the twentieth session there is one more thing on that list that
needs only the **.NET SDK**, not Revit and not Windows.

| | |
|---|---|
| Fragments | **218**, every one `DRAFT`. 138 compiled on all eight Revit releases; **the newest 80 have not been compiled at all** — see `A9` |
| Skills | 10, none naming a fragment |
| Tools the host sees | 10, served by a real MCP SDK |
| Test suites | 18 — **17 pass, 1 blocked**: `test_bridge_roundtrip` needs the .NET SDK. `test_mcp_serves` was listed as blocked here until 2026-09-04 and was not: `pip install mcp` in the container ran it, and it passed. Measured, not remembered |
| Checkers | 8, all green |
| `check-gaps` | **0 unfinished, 55 waiting** |

**Nothing is unfinished. Nothing is proven.** Those are different sentences and both are true: every
buildable thing is built, and not one fragment has touched a real model.

> **The compile gate is the first thing to run on a machine that has the SDK** — `python
> tools/check-fragments-compile.py`. It is now `A9` in the register. Eighty fragments were written
> on 2026-09-02 to 2026-09-05 in containers where the SDK could not be installed, because
> the download host is refused by those networks' policy — re-checked on 2026-09-04, and the proxy
> *names* the denial rather than timing out, so it is policy and not a slow link. Every API member they
> use was checked against the real Revit reference assemblies for 2020, 2024 and 2027 instead, which is
> not the same thing and is not a substitute — though it has now caught **nine** real errors before
> they were compiled, across six distinct shapes. `A9` lists them.

### The one thing that matters next, and it needs the PC

`D3` and the fragment library. **Every fragment is `DRAFT` and stays there until it is run against a
real model with a case that comes back EMPTY** ([D-30](../DECISIONS.md)) — `python
tools/check-gaps.py` counts them, and no number is typed here for the reason the rest of this file
keeps re-learning. That is the owner's job and
it is the largest remaining piece of work in the project. Everything else waiting is smaller:

- **`A7`** — the trained embedding backend has never run: the weights host is unreachable from here.
  It has moved from the least urgent item to **the most urgent one**, because the built-in n-gram
  backend has measurably saturated at this library size (see the seventeenth session below).
- **`A4`, `A6`** — need Windows.
- **`A8`** — needs the MCP SDK installed on the PC; everything else about it is done.
- **`R1b`** — a conversation.

### Two numbers to distrust until a model has been in front of them

- **`READ_SPACE_LOADS`'s load unit.** Airflow is exact arithmetic and provable on paper; the HVAC power
  unit is *assumed* to be BTU/s. If that is wrong every load is out by a constant factor — each figure
  looks plausible, comparisons between spaces still work, and only somebody sizing real equipment finds
  out. It is the fragment's first negative test case.
- **`CREATE_DIMENSION`'s reference technique.** If Revit does not surface a referenceable centreline the
  way it is assumed to, the fragment draws nothing and says so — a designed failure, but a failure.

---
