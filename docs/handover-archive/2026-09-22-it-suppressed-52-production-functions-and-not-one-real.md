# Session note — IT SUPPRESSED 52 PRODUCTION FUNCTIONS AND NOT ONE REAL DISPATCH

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — IT SUPPRESSED 52 PRODUCTION FUNCTIONS AND NOT ONE REAL DISPATCH

**[Row 5b-149](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-reachable.py` read end to end — 334 lines,
the report that asks which built function no production path calls. CI runs it under *"The reports — a
finding is a question"*.

Its docstring is emphatic about precision: *"The precise test is a dict literal whose value is the
function or a getattr with a literal name. Both are structures; neither can be produced by prose."*
It then lists three heuristics that were fooled by text **about** the thing rather than the thing.

**Three findings, all measured by running `survey()` over this tree.**

**One — it stored the KEY and the lookup asks by FUNCTION NAME.** `hits()` asks `if name in dispatched`
where `name` is the function's. Thirteen dict entries in the tree have a module-level function of their
own file as the value, and **in every one the key differs**:

| where | written | key stored | function meant |
|---|---|---|---|
| `tools/prove-agent.py` | `"accept": cmd_accept` | `accept` | `cmd_accept` |
| `brain/heron_agents.py` | `"header_disagreements": disagreements` | `header_disagreements` | `disagreements` |
| `brain/heron_ingest.py` | `".md": _read_text` | `.md` | `_read_text` |

**The rule had never once suppressed the thing it was written for.**

**Two — a result dict is not a dispatch table.** Matching any `{str: Name}` swallowed the commonest
shape in `brain/` — `{"check": name, "passed": True}`, `{"package": name, "version": version}`. The
suppression set held **636** names: `count`, `report`, `review`, `version`, `read`, `evidence`, `why`,
`what`. **52 of the 400 public production function names were on it**, each permanently invisible to
this report — if `heron_evaluator.report` lost its last caller tomorrow the tool would say nothing.
That is the tool's own stated failure mode, in the tool written to catch it.

**Three — the getattr name is the second argument.** It read `node.args[-1]`, which in the
three-argument form is the **default**. **183 of the 191** getattr calls in the surveyed areas have
three arguments, so `''`, `'?'`, `'2024'` and `'.txt'` were recorded as dispatched names while the real
ones were missed.

#### Why the suite did not catch it

`tests/test_reachable.py` case 2 used `{'accept': accept}` — the one form where key and function name
are the same word, which **no dispatch table in this repository uses**. It passed on a coincidence the
real tree does not supply, which is the same shape as row 148's first draft. Extended, not duplicated:
case 2b, and claim 1 in the header corrected. **3 red against the module exactly as found.** Teeth four
ways — as found (3), the key instead of the value alone (2), `args[-1]` alone (1), and the value taken
without the own-function restriction (1).

The own-function restriction is **not** belt-and-braces: measured, taking any Name moves the leak
rather than closing it and **37** production names still get in, `want` among them — which this tool's
own comment calls Q-47's whole subject.

#### Four, in the tool's page rather than its code

`tools/README.md` repeated the same misleading example and then said, in the present tense, *"It found
**12**, of which **5** were already recorded and one — Q-47, `heron_capability.want()` — was not."*
**Both halves had gone**: Q-47 was answered on 2026-09-09 and is marked so in `OPEN-QUESTIONS.md`,
`want()` was wired up, and it is not reported at all any more — today's run is 8 hits, 2 recorded. A
second typed number, *"It went 12 hits → 4"*, stood flat as if current. **The tool carries a whole
section for this failure** — a `RECORDED` excuse whose reason has gone, D-54 applied to itself — and
its own page was the thing out of date. Corrected in place rather than recorded for later: it is the
page of the file being read, and the live counts are now the command.

#### The report itself did not move

Six unexplained hits and two recorded, the same names before and after. This is a fix to the tool's
**evidence**, not to its answer, and the register says so rather than claiming a finding it did not
produce.

#### Traced and not raised

`decorated` is also a name-only set, so a decorated function in one file would silence a same-named
function in another — measured, all 34 decorated names carry their own decorator, so it fires nowhere
today. That is a shape, not a behaviour. The single module-level `async def` in the surveyed areas is
in `tests/`, which is never reported, so `ast.AsyncFunctionDef` being invisible costs nothing yet. No
caller of any reported hit exists outside the five surveyed areas — the only Python file outside them
is the guard hook, and it names none of them.

**The rest of the file is sound and unusually careful.** Module level only, so a closure handed to a
thread and a class method are not mistaken for public surface. A bare name counted only where the file
imported it, because counting every bare name lost `want()` to local variables in three modules. Paths
normalised to `/` at the one place a path is made — on Windows every comparison was false and the tool
reported a clean run **by seeing nothing**. An unreadable file is **named**, never skipped. And a
stale-record section reports its own excuses going out of date, which is D-54 applied to itself.
