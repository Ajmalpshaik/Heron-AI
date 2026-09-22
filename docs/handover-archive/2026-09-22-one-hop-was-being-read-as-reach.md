# Session note — ONE HOP WAS BEING READ AS REACH

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — ONE HOP WAS BEING READ AS REACH

**[Row 5b-126](../FRAGMENT-ISSUES.md), FIXED.** `tools/module-reach.py` read end to end — 177 lines,
never opened before. The **fifth** of the six unheld tools.

It exists to re-measure `mcp/README.md`'s own sentence: modules *"imported by nothing but their own
tests, and therefore **UNREACHABLE FROM ANY CONVERSATION**"*. It sorts every module in `brain/` into
four buckets **by who imports it**, and [row 5b-83](../FRAGMENT-ISSUES.md) carries the number it
prints.

**It counted one hop.** A module imported by a neighbour landed in the first bucket — *reached by
mcp/ or another brain module* — whether or not that neighbour was itself reached by anything.

**Measured by following the imports instead:**

| | |
|---|---|
| modules in `brain/` | **145** |
| the first bucket holds | **53** |
| reachable from `mcp/` at all | **17** |
| reached only by neighbours nothing reaches | **36** — closed loops inside `brain/` |

So the count of modules **no conversation can arrive at is 128**, not the 90 in bucket three, and the
figure a reader takes as *reached* overstates it by more than a factor of three.

### Fixed by widening what it reports, not by changing a bucket

A new `reached_from()` walks the import graph, and the report prints *reachable from mcp/ by
following imports* **beside** the four buckets, with a line naming how many of the first are closed
loops. `--list` names them too.

**Neither number stands in for the other.** The buckets say *who imports a module*; the walk says
*whether a conversation can arrive at it*. Both are worth having, and it still exits 0 whatever it
finds — it is a report.

**[Row 5b-83](../FRAGMENT-ISSUES.md) is still OPEN and still the owner's.** A pointer sentence was
added to it naming the sharper measurement and saying in terms that **the question did not move**.
That is [PROJECT-MAP §D](../PROJECT-MAP.md)'s *record both*, not an answer.

**`tests/test_module_reach.py`**: **one red** against the module as found, with every bucket check
green before and after — which is what makes this an addition rather than a correction. Both halves
shown to have teeth: removing the walk reds the line that asks for it, and making the walk stop
after one hop reds the count.

### Three things checked and found right, rather than assumed

| | |
|---|---|
| package-path imports | **None anywhere.** `from brain.heron_x import y` would be missed by the head-of-the-dotted-name reading, and no file does it |
| `platform/` and `revit/` left out of the searched roots | They hold **no Python at all**, so it costs nothing |
| `.claude` | Its one Python file imports **no** brain module, so a module reached only by a hook — which would land in the first bucket under a label naming mcp and brain — does not exist today |

All three are **shapes rather than defects**, and none is a row.

**One of the six unheld tools remains**: `measure-graph` — 394 lines, and a report.
