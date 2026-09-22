# Session note — (last) — THREE LIVE-PATH BRAIN MODULES, AND THE SAME SHAPE TWICE

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (last) — THREE LIVE-PATH BRAIN MODULES, AND THE SAME SHAPE TWICE

**[Row 5b-86](../FRAGMENT-ISSUES.md). FIXED, and it is [5b-84](../FRAGMENT-ISSUES.md)'s shape one module
later.** `brain/heron_queue.py` built its `QUEUE_FULL` message from `max()` over its own items — and
with a limit of 0 or below that branch fires on the **first** `add()`, with nothing in the queue.
Measured: `Queue(limit=0).add(...)` raised **`ValueError: max() arg is an empty sequence`**, out of a
module whose docstring says twice that an item which cannot be queued is *refused with a reason* and
that an empty queue is *"reported as data rather than raised"*. A limit is a caller's statement about
its own memory — the file itself calls `DEFAULT_LIMIT` *"a default, not a law"* — so zero is a thing a
caller may say, and the answer should be a sentence. **4 checks go red** against the old module; the
genuinely-full case stays green in both, which is how the pair proves the change was narrow.

**THE SWEEP FOR THAT SHAPE IS FINISHED, AND IT IS NOT A CLASS.** An AST walk over `brain`, `mcp` and
`tools` finds **28** calls to `max()`/`min()` over one argument with no `default=`. Three are safe by
construction. A crude guard-detector flagged 16 as unguarded and **was wrong about 15 of them** — it
cannot read `X if cond else Y`, `not x or`, a dict truthiness test, an `if` clause in a comprehension
(Python evaluates it BEFORE the element), or a preceding `cited[0]` that would raise first. **All 13
it could not clear were then read one by one, and every one is guarded.**

**So `heron_queue` was the single instance**, and the reason is worth keeping: its guard was on the
LIMIT rather than on the collection, so the collection could still be empty when the message was
built. That is the shape to look for, not `max()` itself.

**[Row 5b-85](../FRAGMENT-ISSUES.md). FIXED, and no class was invented.** `brain/heron_paths.py` — the
module that tells product from data from derived, so an update cannot destroy a modeller's fragment
library. Every message in it said *"the **twenty** folders docs/06 s2 names"*, **seven times in one
file**. Measured against the document: its tree draws **19**, its table classifies **14** of them,
and the module matches on **17 words**. Twenty is none of those.

**The count is the small half.** The five docs/06 §2 draws and never classifies — **Community,
Configuration, Documentation, RAG, Tests** — all fell through to `UNKNOWN` carrying *"matches none of
the twenty folders docs/06 s2 names"*. **False for all five.** They are named; what is missing is a
class, and that gap is the document's. `classify()` now returns `unnamed` so the two kinds of
not-knowing are told apart, and **nothing was decided for the five** — that is
[Q-13](../OPEN-QUESTIONS.md)'s, and deciding it here would be [row 5b-75](../FRAGMENT-ISSUES.md) again.

**Both modules' suites had pinned the defect.** `test_paths.py` section 3, headed *"UNKNOWN is
honest"*, used four of the five as its examples of an unknown folder. Its input moved to a folder
docs/06 really does not name; **the assertion itself is unchanged**. And my own new section crashed
on an `AttributeError` against the old module instead of failing — one crash where there were
**thirteen** failures to report. `getattr` with a default now, and thirteen is what it reports.

**[Row 5b-84](../FRAGMENT-ISSUES.md). FIXED, in two files.** `brain/heron_router.py` is the first of
the **53 live-path brain modules** to be read — the ones a request actually travels through, as
opposed to the 90 in [row 5b-83](../FRAGMENT-ISSUES.md) that nothing calls.

**Two public methods on one class disagreed about the same input.** `route()` upper-cases the intent;
`candidates()`, public and beside it, did not. Measured: `route("classify")` answers, and
`candidates("classify")` raised **`KeyError: 'classify'`** — out of a module whose own docstring says
*"a refusal is data rather than an exception because ... an exception two layers down arrives there
as a stack trace"*. `candidates()` is the method a caller reaches for to SHOW the options.

**THE FIRST VERSION OF THE ROW WAS WRONG AND THE ROW SAYS SO.** It called the bare `KeyError`
unhandled. It is not — `brain/heron_availability.py`'s `resolve()` catches it **by name** and turns
it into the router's refusal. So it was **load-bearing**, and changing one side without the other
turned `tests/test_availability.py` red: green before the edit, two checks failing after.
**I had not looked for a caller before writing.** A red suite is a claim about the code and was
treated as one — the suite was not edited, the caller was.

**What survives is still real**: the case mismatch, which no caller depended on, and a bare
`KeyError` with no message as a signalling mechanism between two modules. `candidates()` now raises
`ValueError` carrying the whole sentence, which is what `register()` beside it already did, and the
one caller moved with it. **No gate covers this shape** — `check-narrow-errors` is about D-52's
plausible zero, an error swallowed and returned as empty; this is its mirror.

**Checked and standing**, in the module that enforces it: **confidentiality narrows and never
widens**. The flag only ever removes candidates, no value of it adds one, and a confidential request
with no local adapter is refused by name rather than falling back to the host — which that file says
is the one failure it exists to prevent.

**Three mistakes of my own were recorded today rather than quietly fixed** — a test that passed
loudest when its guard was deleted ([5b-79](../FRAGMENT-ISSUES.md)), a register row that turned CI red
twice over one character ([5b-81](../FRAGMENT-ISSUES.md)), and this one. They are in the register
because the next reader needs the shape, not the apology.
