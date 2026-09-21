# Phase 2 — what the earlier brain already learned

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — read 2026-09-21, nothing taken yet.** **Owner:** Ajmal PS.

---

## 1. What this is, and the naming rule it obeys

The owner's earlier brain repository was read on 2026-09-21 at commit `f5570b3`, on his instruction:

> *"You can read it and use it from now on, but bring that file into our own. **Do not keep the name or
> anything like that; it needs to be totally part of our own.**"*

**That is also this repository's own rule**, arrived at from the other direction —
[D-25](../../../DECISIONS.md) and [31](../../../31-studying-the-existing-libraries.md): *studied and
re-authored, never imported*, and [AGENTS.md](../../../../AGENTS.md): *never mix another project's code,
branding or wording into Heron.*

**So nothing below carries a name, a path or a phrase from there.** The source is cited once, here, for
provenance — because a lesson with no origin cannot be checked — and every rule is written in Heron's
terms.

**Its fragment library was already read on 2026-09-06** and the result is in
[31 §1](../../../31-studying-the-existing-libraries.md): 330 files read, 317 already covered, 4
impossible through the API, **9 genuinely missing**. **That work is not repeated here.** This pass looked
only at the part that had never been examined — **how it debugs itself and how it grows.**

---

## 2. The eight lessons

### B1 — "Do it and say so" is the permission model, and it answers [Q-DE-1](07-debugging-engine.md)

Its standing rule for improving itself, in its own words:

> *save it, then tell the user what was saved and why **in the same reply — don't ask permission first,
> but always say what happened**. Deleting or replacing something that already exists still needs the
> user's explicit OK.*

**That is the answer to the question [07](07-debugging-engine.md) left open** — may the engine fix a
`DRAFT` fragment without asking? The line is not *fragment versus agent*. It is:

| | |
|---|---|
| **Adding something that did not exist** | do it, and **report it in the same reply** |
| **Changing or deleting something that does** | **ask first** |

**And it lands almost exactly on [Golden Rule 13](../../../14-golden-rules.md)'s line** — propose before
changing the core — without either having been written from the other. Two systems reaching one rule
independently is worth more than one asserting it.

**What it adds that Rule 13 does not say:** *report it in the same reply*, and *never ask permission for
a pure addition*. A rule that only says "propose" produces an assistant that asks before doing anything,
which is how a helper becomes a form to fill in. → **[R-51, R-52](01-requirements.md)**

### B2 — A version checker that never opens Revit, and its FAIL list is the fixer's input

It carries a checker that finds every Revit installed on the machine and **compile-checks every fragment
against that release's real API files, without launching Revit.** Its rule: *green means safe; a FAIL
list is what gets sent to be fixed.*

**That is the front half of the debugging engine [07](07-debugging-engine.md) is planning**, already
working somewhere else — and **Heron has the same capability and does not use it that way**.
[`check-api-surface.py`](../../../../tools/check-api-surface.py) answers *does every API member this code
calls exist in every release it claims*, and
[`check-fragments-compile.py`](../../../../tools/check-fragments-compile.py) compiles every fragment
against every release. **What Heron lacks is the last step: the failure list becoming a repair job.**
→ **[07 §5 step 1](07-debugging-engine.md)**

**Its own stated limit is the one worth copying verbatim in meaning:**

> *WHAT IT CANNOT CATCH: whether a script does the RIGHT thing. A script can compile perfectly and still
> act on the wrong elements. **Passing here is a floor, not a ceiling** — still run one element first and
> check the real result.*

That is [D-30](../../../DECISIONS.md)'s argument exactly, reached independently. **A compiler proves the
API agrees; it says nothing about whether a duct moved 200 millimetres or 200 feet.**

**One thing to carry over as a caution rather than a rule:** that checker's own summary still advertises
*"about a minute"* while the measured figure recorded elsewhere in the same repository is **767 seconds**
for three releases. A stale number surviving next to the true one, in a file whose job is to be trusted —
the same failure this repository's `check-docs` gate exists to catch.

### B3 — Revit's own data describes intent, not physical reality

> *element names, tags, `Connector.IsConnected` — **both have been proven wrong in real sessions**. When
> the obvious answer doesn't hold up, find the technique that gets the real answer: geometry, a second
> property, walking the model.*

**Heron half has this.** `CHECK_FIXTURE_CONNECTIVITY`, `FIND_SYSTEM_ISLANDS` and `TRACE_CONNECTIVITY`
exist precisely because a connector can lie. What is **not** written down anywhere in Heron is the
general rule: *a property that reports intent is not evidence of fact, and a second route is what turns
one into the other.* → **[Q-B-1](#5-open-questions)**

### B4 — Fresh reads, never recall

> *The user edits and undoes things in Revit between messages. Re-query before acting on "known" state;
> read back after changing anything. **Never trust your own earlier tool-call result in this same
> conversation as still-current truth.***

**Heron has this only as a symptom, not as a rule.** The
[`fragment-proving`](../../../../.claude/skills/fragment-proving/SKILL.md) skill lists *"an answer given
on a stale selection"* as one of the five mistakes that account for nearly every failed proof — which is
this lesson, discovered from the wrong end. → **[Q-B-1](#5-open-questions)**

### B5 — Every number is a per-request input, never a default

> *Clearances, flows, heights, margins — confirm fresh and restate before calculating; **never reuse a
> past session's value just because it worked before**. The user speaks in mm; the API is feet — convert
> explicitly both ways.*

**Heron has the units half and not the defaults half.**
[`HeronUnits.cs`](../../../../platform/Heron.Core/HeronUnits.cs) is the single place that converts, and
[D-83](../../../DECISIONS.md) already says a remembered practice is offered **by name**, as a question —
*"in Project A you used 700mm, do you want the same here?"* — rather than applied. **That decision is
this lesson**, and it is stronger than the original because it keeps the memory while refusing the
default.

### B6 — Write down the user's words, not only the conclusion

> *When they name something in their own phrasing — a term, an abbreviation, a dictated near-miss — record
> it **in the same turn, without waiting for it to cause confusion first**. Their sentence is what a
> future session has to route from, and the measured weak spot of the search layer is exactly the site
> vocabulary that appears in no file.*

**Heron does this per skill and nowhere centrally.** Every skill carries `utterances` — *"the words
Ajmal actually says, not the words the technique is named after"* — and
[15](../../../15-glossary.md) is a glossary of Heron's own terms, not of his. **The measured claim is the
part worth taking seriously:** retrieval is weakest exactly on the vocabulary that exists in no file.
→ **[Q-B-2](#5-open-questions)**

### B7 — Files are the only portable memory

> *An assistant's own local memory is a cache, not the record. Anything worth remembering must ALSO be
> written into files, because moving to another machine means copying this folder only.*

**Heron already builds this in**, and more strictly: `HeronPaths` separates **product** from **data**
from **derived** in code, `IsSafeToDelete` refuses anything under data, and
[S8](00-structure.md) makes an install replace the product and never the data. **Recorded as agreement**
— it is the reason the Settings panel's choices live where they do ([S9](00-structure.md)).

### B8 — Plan, split, execute; and confirm before anything bulk or hard to reverse

> *Show a short numbered plan, run one step at a time, check each step's real result before starting the
> next. **Never one opaque script that does everything at once.** Small, easily-undone changes: just do
> them and report.*

**Heron has the second sentence as machinery**: the preview, the re-count, the `TransactionGroup`
assimilated only on `apply=true`, the permission gate ([D-55](../../../DECISIONS.md)). **The first
sentence it does not have** — nothing requires a multi-step job to be shown as steps and checked between
them. → **[Q-B-1](#5-open-questions)**

---

### B9 — The duplication it accumulated is the reason Heron exists, and Heron's answer is structural

> Owner, 2026-09-21: *"we need to check them carefully. **Don't copy-paste blindly, because some skills
> are overwritten. For the same work, we ended up with multiple skills or multiple fragments** — that was
> the mistake, and that is why we invented Heron to fix these issues."*

**This is the most important sentence in the whole read**, because it names what the re-authoring is
actually for. Taking two fragments that do one job and carrying both across would reproduce the defect
the new library was built to escape — and it would arrive looking like diligence.

**Heron's answer is not a habit. It is the shape of the library**, and three separate things enforce it:

| | |
|---|---|
| **One capability, one fragment** | `capability:` is unique across the store. Checked 2026-09-21: **no capability is claimed twice.** Two fragments doing one job cannot both be authoritative, because only one may own the name |
| **A skill names capabilities, never fragment ids** | so a fragment can be replaced, split or retired without any skill changing. There is nothing to keep in step, so nothing drifts out of step |
| **Three agents watch for it** | [`heron_duplicates.py`](../../../../brain/heron_duplicates.py) — *"nothing comparable is not nothing like it"* · [`heron_merge.py`](../../../../brain/heron_merge.py) — *"the same shape is not the same thing"* · [`check-skill-routing.py`](../../../../tools/check-skill-routing.py) — do a skill's own words actually reach its own capabilities |

**So the porting rule needs one line added to it, and [06](06-porting-method.md) now carries it:** before
a fragment comes across, ask **which capability it claims** — and if that capability already has an
owner, the question is *widen the owner* or *this is a different job and needs a different name*.
**Never both.**

**Note what the middle row costs to break.** A skill naming fragment ids instead of capabilities is how
the original ended up with skills overwriting each other: change a fragment and every skill pointing at
it is silently wrong. Heron's skills name capabilities for exactly that reason, and that rule is worth
more than any of the checkers.

---

## 3. What is deliberately NOT taken

| | Why |
|---|---|
| **Every name, path, folder and phrase** | The owner's instruction, and [31](../../../31-studying-the-existing-libraries.md). This note names nothing from there |
| Its skill-routing layout, its index files, its search layer | Heron has its own — the capability registry, the skills, the knowledge stores. **Two routing systems is one too many** |
| Its fragment library | **Already read on 2026-09-06** and classified in [31 §1](../../../31-studying-the-existing-libraries.md). Re-reading it would be work with a known answer |

---

## 4. Where each lesson goes

| | Lesson | Becomes |
|---|---|---|
| B1 | Add freely and report; change or delete asks first | **[R-51, R-52](01-requirements.md)** — and it **answers [Q-DE-1](07-debugging-engine.md)** |
| B2 | The version check's FAIL list is the fixer's input | **[07 §5](07-debugging-engine.md)** — Heron has the checker, not the loop |
| B3 | A property reports intent, not fact | [Q-B-1](#5-open-questions) |
| B4 | Fresh reads, never recall | [Q-B-1](#5-open-questions) |
| B5 | No number is ever a default | already [D-83](../../../DECISIONS.md), and stronger there |
| B6 | Record his words, in the same turn | [Q-B-2](#5-open-questions) |
| B7 | Files are the only memory | already `HeronPaths`, [S8](00-structure.md), [S9](00-structure.md) |
| B8 | Plan, split, execute | [Q-B-1](#5-open-questions) |

**Three of the eight Heron already has, and two of those it states more strictly than the original.**
That is the useful result of the read: it is mostly confirmation, and the confirmation is worth having
because it was reached independently.

---

## 5. Open questions

### Q-B-1 — Do B3, B4 and B8 become standing rules in Heron, or stay as skills?

Three working habits — *a property reports intent not fact*, *never trust your own earlier read*, *plan
then split then execute* — are true of **every** Revit task, not of one kind of task.

[The earlier brain put them in a single always-on file](#2-the-eight-lessons) rather than in any skill,
for a stated reason: *a skill only fires when something decided it was needed; a universal habit has to
apply whichever skill is running.*

**Heron's equivalent of that file is [`AGENTS.md`](../../../../AGENTS.md) and the Golden Rules.** Whether
these three belong there is the owner's call — and the argument against is real: the Golden Rules are
21 and deliberately few.

### Q-B-2 — Does Heron keep a glossary of the owner's own words?

Skills carry `utterances`; nothing collects his vocabulary in one place.
[15](../../../15-glossary.md) is Heron's terms, not his.

**The measured claim is what makes this worth asking**, not the tidiness: retrieval was weakest exactly
on the site vocabulary that appeared in no file. A word that exists nowhere cannot be searched for.

---

**Nothing has been taken. The reading is the deliverable.**
