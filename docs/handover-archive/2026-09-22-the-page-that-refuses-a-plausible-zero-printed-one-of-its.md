# Session note — THE PAGE THAT REFUSES A PLAUSIBLE ZERO PRINTED ONE OF ITS OWN

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE PAGE THAT REFUSES A PLAUSIBLE ZERO PRINTED ONE OF ITS OWN

**[Row 5b-137](../FRAGMENT-ISSUES.md), FIXED.** `tools/balance-of-work.py` read end to end — 417 lines,
and **one of only two tools in `tools/` that no suite names at all.** That was the measurement that
picked it: every file in `tests/` searched for every tool's name, and only this and `setup.ps1` came
back with nothing.

It is the page a person opens to decide what to do next, and its own helper states the rule the whole
file runs on:

> *A figure a tool did not give back is not a zero.*

**`statuses()` tallies only the statuses it FINDS**, and rows 1 and 2 read that tally with a bare
`.get("DRAFT")`. Measured on a library written for the purpose, every fragment `PROVEN`:

```
row 1  Fragments that have never met a model  ->  **not derived** of **3**
```

**The day the fragment library is finished is the day this page stops being able to say so** — and its
own footer tells the reader, in terms, that *not derived* does **not** mean zero.

**THIS IS THE THIRD TIME THE SAME COLLAPSE HAS HAPPENED IN THIS ONE FILE**, and the first two are
commented beside the rows they hit: row 4, where an empty list of unsigned agent proofs was falsy on a
board where every draft had in fact been signed, and row 9, where a clean signature board read as an
unchecked one. **Both were found on the day their count first reached zero.** A `counted(tally, key)`
now answers the derived zero when something was read and `None` when nothing was, and rows 1 and 2 go
through it in both the page and the console.

**Also fixed:** `live_work_notes()` had no missing-file guard while `register_rows()` and
`proposals_open()` beside it both do — a deleted or renamed `docs/work-notes/README.md` answered `[]`,
which the page printed as *"lists no live note"*. It answers `None` now and the page says *not derived*.

**NOT A LIVE FAILURE TODAY, AND THAT IS PART OF THE FINDING.** 68 fragments and 10 skills are DRAFT,
the index is present, and **every figure on the page reads the same before the fix and after it** —
68 of 396, 10 of 10, 0 of 250 agents, 44 of 306 defects, 153 waiting on the owner.

#### The first teeth proof found the SUITE too loose, not the tool sound

`tests/test_balance_of_work.py` is the first suite this tool has ever had: **5 red** against the module
as found. But the break that makes an *unread* library report zero left **every check green** — because
each row prints `count of total`, and asserting *"not derived" appears somewhere on the line* is
satisfied by the **total** half while the count half is wrong. The suite reads the count cell on its
own now. Teeth proved four ways:

| Break | Red |
|---|---|
| the derived zero dropped | 4 |
| the *nothing was read* guard removed | 3 |
| the missing-index guard removed | 1 |
| the page listing itself as outstanding work | 1 |

#### Traced and not raised

`grab`'s `TOTAL ids, all sections` read **cannot** walk onto the following line the way `q_ids` once
did, because `open-defects.py` prints `(none)` rather than an empty tail. Checked by reading that
tool's own `out()` line, not by pattern.

**Recorded, not fixed:** `--write` is the only flag and an unknown one is ignored in silence — the
shape of rows 5b-112, 5b-127, 5b-129 and 5b-132 — but here the page's own generated stamp is what
tells a reader it was not rewritten.

#### What is left in `tools/`

**23 of 50 tools have still never been opened**, and `setup.ps1` (209) is now the only one of them that
no suite names — it is PowerShell, and **there is no `pwsh` on this container**, so it can be read but
not run. Largest first, the rest are `generate-jobs` (1157), `prove-skill` (950), `prove-agent` (934),
`check-skill-routing` (770), `batch-prove` (735), `generate-contract-reference` (718),
`generate-skill-catalog` (660), `check-revit-gate` (651), `prove-tracking` (633), `heron-backup` (454),
`check-risk-crossings` (440), `measure-brain` (435), `new-agent` (410), `generate-agent-map` (400),
`measure-routes` (365), `generate-fragment-catalog` (359), `generate-api-docs` (338),
`check-reachable` (334), `check-declared-questions` (277), `owner-queue` (231), `check-api-surface`
(223) and `api-changes` (208).
