# Session note — THE WARNING THAT COUNTS ROWS AND LISTS NAMES, AND `heron_mcp_server.py` IS FINISHED

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE WARNING THAT COUNTS ROWS AND LISTS NAMES, AND `heron_mcp_server.py` IS FINISHED

**[Row 5b-111](../FRAGMENT-ISSUES.md), FIXED. The whole file has now been read** — 3,516 lines, the last
266 of them in this pass.

`revit_parameters` closes its answer with the one sentence in that table a modeller has to act on:
which parameter names answer to **two different parameters on a single element**, so asking by them
would hit whichever Revit returned first. **The count came from a list of ROWS and the list from the
DISTINCT names**, and those are not the same number. The add-in writes one row per name per `where`
and says so in the `reads` note beside every answer — *"the same name can appear twice, once as each,
and they are two different parameters"* — so a name ambiguous on the instance **and** on the type is
two rows and one name:

```
2 parameter name(s) answer to TWO different parameters on a single element here — Comments.
```

The second name is not missing from the list. **It never existed**, and a modeller who counts goes
looking for it. And the list is capped at five with no marker, so seven flagged names print five and
the sentence says nothing about the other two.

**THE RULE AGAINST THAT IS EIGHTEEN LINES UP, IN THE SAME FUNCTION.** The `notListed` block goes to
real trouble to name *which kind* was cut, and says why: *"a truncated answer that does not say what
it dropped is the one a reader trusts by mistake."*

**NOT an R-82 breach, and not claimed as one.** [Row 5b-100](../FRAGMENT-ISSUES.md) settled that
distinction the first time it came up — R-82 is about the **scanner's window**, not the report's
margin — and writing R-82 on this row would have been a finding manufactured out of a word. The
authority is the function's own rule.

**MEASURED BY LIFTING THE FUNCTION OUT OF THE FILE AND CALLING IT**, not by reading it. That is
`tests/test_values_crossing.py`'s technique and its recorded reason, and it is the one that matters
for anything in this file: **`import heron_mcp_server` needs the MCP SDK that `gates.yml` leaves out
on purpose**, so an importing suite exits **3**, lands in *could not run*, and never runs where it
matters — which for a regression guard is the same as not existing.

```bash
python tests/test_parameter_clash.py     # 3 red against the module as found
```

**The five checks that nothing legitimate moved were green BEFORE the fix**, which is what made it
safe to make: two genuinely different ambiguous names still read two and are both named, five names
carry no *more* because nothing was cut, and a model with no clash still gets no sentence at all.

**WHY IT SURVIVED.** Every other figure in that table adds up out loud — `withAValue` plus `noValue`
plus `blank` **is** `onElements`, and the answer says so — while this one is a count beside a list,
checkable only by counting the list, which is exactly what nobody does.

**AND THE FILE IS FINISHED.** `_parameter_coverage`, `_parameter_values`, `_clip`,
`_groups_inventory`, `_groups_in_category` and `_repo_root` were the last unread helpers. **Checked
rather than assumed** that nothing was left: `len(clashes)` was the only count-beside-a-list of its
kind in the file, and an AST walk says the module holds **no classes** and nothing at module level but
imports, five assignments, one `try`, one `if` and 47 function definitions — so there is no surface
here a read of the functions could have missed.

**One thing recorded and NOT fixed**, per the smallest-safe-change rule: a row whose `name` is absent
would raise inside `sorted(set(...))`. It would have raised before this change too — the shape is
unchanged and widening it here would have been a second edit riding on a reviewed one.
