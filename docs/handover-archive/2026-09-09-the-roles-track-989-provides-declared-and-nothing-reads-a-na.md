# HANDOVER — 2026-09-09 (the ROLES track): 989 provides declared, and nothing reads a name any more

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**`FRAGMENT-ISSUES` §3h.2 asked for 40. The library held 989.** The section could not see the other 949
because it was counting by name shape, and two of the three tranches are invisible to any regex.

Every `provides` in the library now DECLARES whether it is an ANSWER or BOOKKEEPING. The three naming
patterns that used to guess it are deleted, and `check_contract` refuses a provide that says nothing.
**Five PRs, #46, #49, #51, #54 and #56, all on `main`.**

### The three tranches, and why the count kept growing

| | What it was | Count | How it was found |
|---|---|---|---|
| **#46** | bookkeeping-SHAPED, patterns MISS them → guessed `result` | **40** | `both*`, `without*`, `outside*` — what §3h.2 counted |
| **#49** | patterns CATCH them → guessed `accounting` | **150** | `^(no\|not\|un)[A-Z]`, `scanned\|checked`, five hand-listed names |
| **#51** | match NO pattern at all → defaulted to `result` | **799** | no query could derive it — 357 purposes, read one at a time |

**The first two came off a regex. The third came off reading.** That is the whole reason the number
was 40 and not 989: a shape can only find names that have a shape.

### What the reading found that the patterns could not — 166 disagreements, in BOTH directions

**An ANSWER read as bookkeeping** — the finding is silently dropped and never counted:

| Fragment | Provides | Its own words |
|---|---|---|
| `select-unenclosed-rooms` | `unplaced`, `unenclosed` | the TWO FAULTS it exists to find, both swallowed by `REJECT_NAMES` |
| `report-room-space-data` | `unplaced`, `unenclosed` | *"only one of them is a modelling fault"* — which is why they are split |
| `report-space-airflow` | `noDesignFigure` | the semantic identity NAMES it; without it a space nobody designed reads as balanced |
| `check-fixture-connectivity` | `noConnectors` | *"A FIXTURE WITH NO CONNECTORS AT ALL IS A DIFFERENT DEFECT"* |
| `check-ceiling-coordination` | `noCeilingAbove` | the first of the two questions it asks |
| `list-linked-models` | `notLoaded` | one of the three things it reports |

**BOOKKEEPING read as an answer** — the dangerous direction, because `batch-prove` reads a non-zero one
as evidence the run did something:

| Fragment | Provides | Why it would have banked a false proof |
|---|---|---|
| `set-mep-slope` | `inGroup` | *"counting that call as success is how a report says sloped 12 over a model where nothing moved"* |
| `flip-elements` | `cannotFlip` | non-zero while `flipped` is 0 |
| `move-elements`, `rotate-elements`, `snap-to-grid` | `blocked` | Revit's move and rotate return normally and do nothing on a group member |
| `array-elements-radial` | `stepDegrees` | worked out BEFORE anything is made — non-zero when `count=1` creates nothing, which is its own negative case |
| `distribute-along-run` | `leftoverMm` | the WHOLE run length when nothing was placed |
| `place-accessory-on-run` | `leftSevered` | runs cut in two where the accessory then failed — damage, not work |

### Then the patterns went, and `role` became REQUIRED — #54

`REJECT_PREFIX`, `REJECT_NAMES`, `WORK_COUNTER` and `_is_accounting` are **deleted** from
`heron_validate.py`, with their two call sites in `batch-prove.py`. `heron_validate.py` had asked for
this in its own words — *"THE REAL FIX IS IN THE CONTRACT, NOT HERE"* — and the record of what they
were, and of the 166 disagreements, is kept where they used to sit.

**The requirement had to come with the deletion, not after.** With nothing left to guess, a provide with
no role falls to `result`, and a bookkeeping count read as a result is exactly what banks a proof for a
run that changed nothing. So `check_contract` refuses one. `findings` is the single exemption — it is
prose, D-51, and `NOTE_KEYS` still carries it.

**It is already working.** Day three's proving added a provide after this landed, and it arrived
declared. 1,346 provides, 1,202 declared, **0 undeclared**.

### And a guard that had a hole — #56

`test_fragment_store` was failing on `main`. `set-view-section-box` was `PROVEN` against code that had
changed: the proof was taken, then the SAME DAY the commit that split `viewRefused` in two rewrote the
very negative case the proof recorded. **The fix is `DRAFT`, not a re-stamp** — the proof block is kept
as the record of why.

`implementation_changed_after` compared `git log --date=short` with `>`. **A proof carries a date and no
time**, so a commit made hours after its own proof compared EQUAL and read as *"did not move"* —
and `restamp --apply` would have made a stale proof look fresh, which its own docstring says is the one
thing it exists to prevent. `>` is now `>=`: same day counts as after. This library proves and fixes
within a single day as a matter of routine, so same-day is the COMMON case here, not the edge.

The cost is stated in the code: a genuine platform-skew re-stamp done on the day of the last commit is
refused too. That is the safe direction, and the remedy is the honest one — set `DRAFT` and re-prove.

### A false alarm that cost this session twice — and the one-line cause

`test_fragment_store` failed, passed, then failed again depending on **where the checkout was**, and it
was read as a real defect twice before being pinned down. It is not one.

`fingerprint()` joins ROOT onto a path that `repo_relative()` may have built with `..` segments — which
is what happens when the store's own tests read a fragment from a temp folder outside the repo. The
join keeps every segment, so the string can pass **260 characters** on a deep checkout and the open
fails with `FileNotFoundError` **on a file that is plainly there**.

Measured: same commit, same bytes, a worktree named `m2` passed and one named `mainonly-long-name`
failed. `os.path.normpath` now collapses the `..` before the open. It cannot change which file is
named, and it does not touch the hash — the digest is taken over the repo-relative path, not the joined
one, and `restamp` confirms every recorded fingerprint still matches.

**The lesson is the diagnosis, not the line.** A failure that moves when you change directory is about
the environment, not the code, and the way to tell is to run the SAME commit from two paths.

### What is NOT claimed

**This does not make a negative case honest.** A fragment can still declare a finding as `accounting`
and slip through. The difference is that it is now a sentence somebody wrote in the contract and can be
read back, rather than an accident of what the field was called.

**And 647 of the 1,202 are a bare `role: result` with no comment.** That is deliberate — the name and
the type already say it, and 647 sentences saying so would bury the 555 that carry a reason. But it does
mean the `result` half was read once, quickly, and is worth a second look if a proof ever reads oddly.

### What comes next, in order

1. **`set-view-section-box` needs re-proving** — it is the one that went back to `DRAFT`. The commit
   that broke it names the arrangement: a **3D view whose selection has nothing to enclose**, not a plan
   view. The old negative used a plan view, which cannot carry a section box at all, so the empty answer
   was guaranteed by the view type rather than by anything the fragment worked out.
2. **`FRAGMENT-ISSUES` §3h.1 — make silence illegal** — is the item that ranked ABOVE this one and is
   still the biggest. A fragment handed something it cannot use reports `0` instead of refusing, so
   *"0 changed"* and *"I could not see what you gave me"* are the same sentence. Twelve fragments were
   fixed on 2026-09-09; the discipline is not general yet.
3. **The `result` declarations are the weaker half** — see above.

---
