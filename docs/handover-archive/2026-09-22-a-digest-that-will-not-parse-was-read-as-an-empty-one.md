# Session note — A DIGEST THAT WILL NOT PARSE WAS READ AS AN EMPTY ONE

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A DIGEST THAT WILL NOT PARSE WAS READ AS AN EMPTY ONE

**[Row 5b-142](../FRAGMENT-ISSUES.md), FIXED.** `tools/api-changes.py` read end to end — 208 lines, and
**the last tool in `tools/` that nothing runs**. Every tool in `tools/` has now been reached by
something.

It writes `tools/api-surface/changes.json`, the committed evidence `HERON-REVIT-ACI-034` reads to say
which members a Revit release stopped shipping. Its own comment records a failure already fixed once:

> *"A TARGETED RUN REFRESHES, IT DOES NOT REPLACE … until 2026-09-15 it wrote a digest holding that one
> transition over the committed one holding all seven. HERON-REVIT-ACI-034 then had no transition into
> 2027 — and read the absence as '2027 removed nothing', reporting every fragment clear."*

The fix keeps what it did not recompute — and answered a digest that **does not parse** with
`except ValueError: existing = {}`. **Measured** on the seven-transition shape truncated mid-file:

```
api-changes.py 2025 2026  ->  a digest holding ONE transition, exit 0, nothing said
```

Six went out silently. **The same failure through a different door**, and D-52 is the rule it breaks —
a file that cannot be read is not an empty one. A digest that parses but is a JSON *list* was worse:
`AttributeError: 'list' object has no attribute 'get'`.

**Also fixed, and it is [row 5b-133](../FRAGMENT-ISSUES.md)'s shape.** `surface_of` raises
`NO_ASSEMBLIES`, `NO_READER` and `DUMP_FAILED` with good messages and **nothing catches them**.
Measured here — no cached assemblies, no .NET SDK — `api-changes.py 2025 2026` gave a **traceback and
exit 1**, the code a real failure uses, while *"two releases are needed"* correctly exits **2**. The
file already separated an argument problem and had no third state for a machine that cannot answer.
`cannot_answer(years)` prints `NOT RUN` and returns **3**; it is asked *after* the argument question,
and works out what still needs dumping first — a release whose surface is on disk needs neither the
assemblies nor the SDK.

**Also fixed, wording.** The comment above `releases()` said *"tools/check-api-surface.py owns which
releases are supported … Bound rather than retyped so one list stays one list."* It is bound to
`heron_fragment.REVIT_VERSIONS`, and **`check-api-surface.py:63` types its own `ALL_VERSIONS`** — so
there are **three** lists, not one. All three agree today; what was wrong was a sentence telling the
next reader a binding exists where it does not.

#### The suite crashed twice on its own cases before it was right

Both were the suite's fault: it parsed the corrupt digest the refusal is supposed to **leave alone**,
and it let the tool's `AttributeError` out instead of naming it. A raise is caught and reported as the
result now, so an error reads as a failure rather than killing the run
([`heron-ship` §2a](../../.claude/skills/heron-ship/SKILL.md)).

`tests/test_api_digest.py` is the first suite pointed at this tool — `tests/test_api_changes.py` is the
suite for the **agent** that reads the digest, a different subject — and it never dumps a surface or
compares two releases.

| Break | Red |
|---|---|
| **the module exactly as found** | **5** |
| the corrupt-digest refusal alone | 4 |
| the NOT RUN guard alone | 3 |
| the guard firing where the surface is already dumped | 5 |
| the merge no longer keeping what it did not recompute | 3 |
| `members()` no longer trimming | 1 |

**And a gate caught this session in the act, for the second time.** The suite's surface fixture typed
the real Revit vendor namespace as a member name and `check-structure.py` failed the build — *"a vendor
namespace named in a COMMENT is still a boundary being discussed in the wrong file"*. What is under
test is trimming and de-duplication, and the text of the member has nothing to do with either. **The
gate was right; the test was wrong** — the same verdict as row 5b-133.

**Needs no network.** `tools/api-surface/changes.json` was never written to — checked with
`git status`.

#### Where the sweep stands

**Every tool in `tools/` is now reached by something**, counting a suite that loads it, a suite that
loads a tool which imports it, and what `gates.yml` runs with the name in a loop variable. **19 have
still never been read**, and the next measurement should be by what a defect would cost: the writers
first (`heron-backup`, `new-agent`, the `generate-*` family), then the checkers CI does not run
(`check-revit-gate`, `check-reachable`, `check-api-surface`, `check-declared-questions`), then the
proving tools (`generate-jobs` 1157, `prove-skill` 950, `batch-prove` 735, `prove-tracking` 633).
