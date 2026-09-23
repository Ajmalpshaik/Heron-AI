# Fragment issues — section 3h

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3h. FOUR THINGS THAT WOULD IMPROVE THIS, RANKED — 2026-09-09

Asked for by the owner at the end of two days of proving. Ranked by what they would have saved
**today**, not by how interesting they are.

### 1. MAKE SILENCE ILLEGAL — do this one first

A fragment handed something it cannot use reports **`0`** instead of refusing. `0 changed` and *"I could
not see what you gave me"* are then the same sentence.

| Fragment | Given | Answered |
|---|---|---|
| the eight schedule fragments in §3e | a schedule on a sheet | `added 0`, `changed 0`, `sorted 0` — no refusal |
| `measure-run-quantities` | `measure=ZZZNOTHINGHERE` | identical to a valid mode |
| `color-by-parameter` | a parameter that does not exist | coloured 22 elements anyway |
| `edit-text-values` | `field=text` | all seven notes reported `absent` |

**This cost more time today than anything else**, because every one of them looks exactly like a
fragment correctly finding nothing — which is also why none of them can be proved: both legs come back
identical.

**And on a real project it is worse than slow. It is a confident wrong answer.** A modeller who asks for
the wrong measure gets a number, not a question.

> **A fragment that cannot use its input must REFUSE and say why. It must never report zero.**

Every fragment already has a `refused` output, and 117 of them now declare it as accounting. The
machinery is there; the discipline is not.

### 2. Finish declaring the roles — **DONE 2026-09-09, and it was bigger than this**

This asked for 40. **The library held 989**, and the section could not see the other 949 because it was
counting by name shape.

| Tranche | What it was | Count |
|---|---|---|
| bookkeeping-shaped, the patterns MISS them → guessed `result` | what this item counted | 40 |
| the patterns CATCH them → guessed `accounting` | invisible to this item | 150 |
| match no pattern at all → defaulted to `result` | invisible to this item | 799 |

**All 1,201 provides now declare a role**, read off what each fragment is FOR rather than what its
output is called. The reading disagreed with the naming patterns **166 times, in both directions** —
`select-unenclosed-rooms` declares `unplaced` and `unenclosed`, the two faults it exists to find, and
`REJECT_NAMES` swallowed both; `set-mep-slope.inGroup` and `flip-elements.cannotFlip` are bookkeeping no
pattern could see, and a non-zero one of those banks a proof for a run that changed nothing.

So `REJECT_PREFIX`, `REJECT_NAMES` and `WORK_COUNTER` were **deleted** rather than kept as a fallback
that is wrong one time in seven, and `check_contract` now REFUSES a provide with no `role` — the
omission is a validation error somebody fixes instead of a silent default nobody sees. `findings` is the
one exemption (D-51). **Nothing reads a name any more.**

The original two victims are declared: `remove-parameter-value.alreadyEmpty` and
`set-mep-slope.bothEndsConnected` are `accounting`, and `bothIn`/`bothOut` on `check-flow-direction` are
`result` — which is the example this item was written around.

### 3. Ask the model once, not six times

Six throwaway probe scripts were written today asking the same kinds of question: what views exist, how
many ducts per view, what connector sizes, which categories hold anything. Every one a round trip.

One command answering *"describe this model"* — views by type, categories by count, the sizes actually
present — removes all six, and it is the same gap as the missing `LIST_*` fragments in §3d.

### 4. Generate the job file

`tools/batch-prove.py` takes a hand-written job list. Most of that list is derivable: which fragments are
still DRAFT and untried, whether `--write` is needed, the setup chain, and **the exact input names** —
six were mistyped today, `widthMm` for `width` and `sortByFields` for `sortFieldNames` among them.

It must leave the category and the view **blank rather than guessing**. A wrong category produces a
confident meaningless result, which happened eleven times in one batch.

### Why the order matters

Two, three and four make the PROVING faster. **One makes HERON honest**, and a tool that quietly gives
wrong answers is worse than a slow one.

---
