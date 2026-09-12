# Next steps — the two tracks, in order

**Status: ACTIVE.** Written 2026-09-12, at the owner's request, after he asked whether Heron can start
being *used* on one side while it is still being *built* on the other.

**Closure condition:** this note is deleted when every step below is either done or has moved into
[NEEDS-CHECKING](../../NEEDS-CHECKING.md), [OPEN-QUESTIONS](../../OPEN-QUESTIONS.md) or
[FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md). It is a schedule, not a register — nothing here is the
permanent home of anything.

> Where this disagrees with [DECISIONS.md](../../DECISIONS.md), the
> [Golden Rules](../../14-golden-rules.md) or the [Constitution](../../../HERON_CONSTITUTION.md),
> **those win.** For the long-term phases read [ROADMAP.md](../../ROADMAP.md); this covers the next
> stretch only.

---

## The answer to the question that prompted this

**Yes — the two tracks can run at the same time, and they do not collide.**

The reason is not scheduling, it is the risk classes the library already declares. Derive them:

```bash
for f in brain/fragments/*/fragment.yaml; do
  printf '%s %s\n' "$(grep -m1 '^heron-status:' "$f" | awk '{print $2}')" \
                   "$(grep -m1 '^risk:' "$f" | awk '{print $2}')"
done | sort | uniq -c | sort -rn
```

On 2026-09-12 that returned **114 `PROVEN READ`**. A `READ` fragment cannot modify a model — that is
enforced, not promised — so running one against a live project cannot damage it and cannot interfere
with anything being built on the other track. **That is the beta surface.**

---

## Track A — the owner uses it (beta, his machine only)

**The rule for the whole track: `READ` on real work, `MODIFY` only on a copy.**

### A1. Use the 114 proven READ fragments on real projects

No preparation needed beyond a deployed add-in. Anything that answers wrongly, or answers a question
nobody asked, goes to [FRAGMENT-ISSUES.md](../../FRAGMENT-ISSUES.md) with the model named — that file
is the queue, and a row there is worth more than a description in chat.

### A2. Prove the rollback fix — **the highest-value thing only the owner can do**

This is the oldest open wound in the repository and no container can close it.

**What happened:** three rollback failures, the worst taking a model from **9,628 placed elements to
3,966 with every floor plan gone**; the third was **seventeen renamed sheets**, so it is not about size.
**What was never true is that the file was corrupted** — the model was closed without saving and
reopened at 9,628. The damage stays in the live session.

**Why they were invisible:** `SafeRollBack` returned `void`, a group whose status is not `Started`
skips the rollback silently, and a `RollBack()` that throws is swallowed. **A failed rollback and a
clean one produced byte-identical output.**

**What was changed (2026-09-09, deployed 2026-09-10):** `SafeRollBack` now returns Revit's own status
after the attempt, `WithVerdict` says **THE ROLLBACK DID NOT REPORT SUCCESS** when it does not, and a
`rolledBack` flag rides on the reply. **None of that has been seen in front of a model.**

**The step:** on a **copy** of a rich model, run one `MODIFY` fragment through preview → apply → rollback
and read what the reply says. Record it in [NEEDS-CHECKING.md](../../NEEDS-CHECKING.md).

> **`delete-elements` stays `DRAFT` and must not be attempted.** Running it is how both model wipes
> happened, and the second hung Revit hard enough to need a forced close. In an MEP model almost
> everything is hosted on something else, so there is no safe negative case to find — the problem is
> not that the right category has not been picked yet.

### A3. One real numbered specification document

**This unblocks more than anything else on either track.** Every test document so far was written to be
easy. One real spec with real clause numbers settles whether the chunker, the citation path and the
refusal behaviour survive contact with a genuine document — and replaces the invented clause number
`W-7` has carried since the start. Blocks **S-4** and **W-7**.

### A4. Two one-click items

| | |
|---|---|
| Merge [PR #122](https://github.com/Ajmalpshaik/Heron-AI/pull/122) | Rebased, all five gates green. The `gh` token here has `repo` but not `workflow` scope, so no session can merge a PR that edits `.github/workflows/`. `gh auth refresh -h github.com -s workflow` also fixes it permanently |
| Create the `dependencies` label | Every Dependabot PR opens with a complaint that the label `.github/dependabot.yml` asks for does not exist |

---

## Track B — the build continues (no Revit needed)

Ordered by *what is actually buildable*, not by what is most wanted.

### B1. The usage counter — R-16 and R-25

**The only item on the list that is both unblocked and code.** [D-70](../../DECISIONS.md) settles the
shape: a clause id and a count, per scope, deletable, never sent. The decision deliberately sets **no
weights**.

**Ask this before writing it:** *does the counter store the question TEXT, or only the count against a
clause id?* D-70 does not say, and it was never put to the owner. An implementer who is not asked will
decide it by accident, and it is the half with the privacy consequence.

### B2. Close W-10 — `sqlite_vec` degrades silently

The only optional package whose absence is **invisible**: Heron gets slower and says nothing. The
repository's own rule is *degrade, but say so*, and this half-keeps it. Self-contained.

### B3. Keep proving the 163 DRAFT fragments

The recipe is [HANDOVER §9a](../../HANDOVER.md). Batch proving is
`tools/batch-prove.py`; the method is in the `fragment-proving` skill. **The machine gathers, a person
signs** — [D-30](../../DECISIONS.md) — so this track produces *drafts*, and Track A signs them.

### B4. Keep building agents

`python tools/agent-count.py` is the live count and the worklist.

---

## Track C — finish the documentation clean-up started today

### C1. Done on 2026-09-12

| | |
|---|---|
| `HANDOVER.md` cut from **5,931 lines to ~1,376** | Forty-four finished session notes moved to [`handover-archive/`](../../handover-archive/README.md), one file per sitting, with an index |
| The two-session race removed | A sitting now writes its **own file** and adds one index row. Two sessions wrote into `HANDOVER.md` within an hour on 2026-09-12 and only merge order stopped a conflict |
| Sections 1–3 replaced | They described Phase 0 — *"the repository is private"*, *"fragments/ 7, all DRAFT"*, *"tests/ 17 suites"*, *"any way to RUN a fragment — not built"*. All four were false. Replaced with the commands that derive the answer; section 3's witnessed-proof table was kept because nothing on disk can re-derive it |

### C2. ✅ DONE 2026-09-12 — sections 4 to 10 audited against the code

**Seven sections read, each claim checked against the code or the disk rather than against other
prose.** The prediction was that they would age better than §1–§3 because they are operational. **That
held: five of seven were clean.**

| | Verdict |
|---|---|
| **§4** The things that will bite you | **Clean.** Four code claims verified — `heron_fragment` really does refuse C# reserved words, `RevitWrite.cs` really does report `moved`/`partly`/`blocked`/`unverified`, `test_session_binding.py` exists, `E10` is in the register |
| **§4a** Something looks wrong mid-job | **Clean.** A symptom index and a method. No counts to go stale |
| **§5** What you can do without Revit | **STALE — the worst of the two.** Listed **18 suites and stated "18 suites… 514 `ok`/`PASS` lines"** while calling it *"derived, not typed"*. The repository holds **57**. Thirty-nine were unmentioned, including the entire RAG subsystem and every gate added since. Replaced with the commands, plus the warning that the pass set is **machine-specific** |
| **§6** Return-to-the-machine checklist | **Clean, and well designed.** It *points at* `NEEDS-CHECKING.md` instead of copying it — *"two lists of the same thing drift"* — which is the rule the rest of this file broke |
| **§7** Decisions you must not undo | **Wording fixed.** *"Twenty-two are in DECISIONS.md"* — true of the list beneath it, false of the file, which holds **71**. The Golden Rules claim (**21, all official**) is correct and gated |
| **§8** What is waiting on the owner | **BADLY STALE.** Opened *"Every question is answered — 41 of 41 — and nothing blocks any phase."* There are **53 questions, 52 answered, three open**, two open for days. **In the section whose entire job is telling the owner what he owes.** Now a pointer to [FOR-THE-OWNER.md](../../FOR-THE-OWNER.md); the three items no register carries were kept |
| **§9 / §9a / §9b** | **Clean.** Their numbers are records of specific past runs, which is legitimate. One inconsistency fixed: the same suite was given **30 checks** in §5 and **32** in §9, and neither could be settled here — both removed rather than one guessed |
| **§10** How to work on this | **Clean.** Principles, and one of them is *"Every number in the docs should be derived, not typed"* — the rule §5 and §8 were breaking four screens above it |

**The pattern across all of it:** nothing was ever wrong when written. Every stale line was written once
in a phase where it was true, and never re-derived. **The sections that survived are the ones that
point at a source instead of copying one** — §6 names the register, §4 names the code, §10 names a
principle. The two that failed had typed a number.


### C3. ✅ DONE 2026-09-12 — `plans/rag/03-working-note.md` is retired

**158 KB and 2,383 lines of diary, gone** — and the durable half moved first, which is the only order
the [lifecycle](../README.md) allows. Nothing was summarised in place.

| What it held | Where it is now |
|---|---|
| The **first retrieval measurement ever taken on the trained backend** — 2026-09-10, 360 fragments | a dated section of [`retrieval-history.md`](../../../brain/retrieval-history.md) |
| `W-8` — whether a confidence floor can be derived at all | [NEEDS-CHECKING](../../NEEDS-CHECKING.md) **A15** |
| `W-10` — `sqlite_vec` degrades with nothing said | [PROPOSALS](../../PROPOSALS.md) **F7** |
| `Q-D`, `Q-E` — two unanswered owner questions | [OPEN-QUESTIONS](../../OPEN-QUESTIONS.md) **Q-54**, **Q-55** |
| `W-9`, `W-7` | already `A10` and [`00-structure.md` §3.3](rag/00-structure.md) — nothing to move |
| `W-1`–`W-6`, `S-1`, `S-2`, `S-4`, `Q-A`–`Q-C` | closed or answered before it was retired |

**The finding worth keeping:** `Q-D` and `Q-E` had been owed since 2026-09-10 and 2026-09-11 while
`OPEN-QUESTIONS.md` said **one** question was open. Retiring the note moved that count to **three**.
A question living in a file whose lifecycle ends in deletion is a question the register does not know
about.

### C4. Still to do — two documents share the number 34

`34-patterns-adapted.md` and `34-project-perfection-and-continuous-upgrade-plan.md`. Renumbering one
touches every link that names it, so `tools/check-docs.py` is the check that it was done completely.

---

## What is waiting on the owner, in one table

Carried from [HANDOVER.md](../../HANDOVER.md) so this note can be read alone. **The handover is the
authority** — if a row here disagrees with it, it is because this note went stale.

| | who | what |
|---|---|---|
| **S-4 · W-7** | owner | one real numbered document — **unblocks the most** |
| **A2 above** | owner + Revit | the rollback fix, in front of a model |
| **Q-E** | owner | may an ingest read another scope? Contractual under D-33 |
| **B1's open half** | owner | does the usage counter store the question text? |
| **A11** | Revit | the project key across a save, a rename and a move |
| **A10** | a machine with `huggingface.co` | Stage 7's after-measurement |
| **R-74 · R-77** | Windows | nothing installs itself — `setup.ps1` deploys the add-in and no Python package |
| **R-78 · R-79** | measurement | no version floor has ever been measured |
| **R-75 · R-76** | owner | sizes and the package list are one command away rather than on the README page |

---

## The line that governs both tracks

**Green is not proven.** [D-30](../../DECISIONS.md) — the machine gathers evidence, a person signs.
Every measurement in this repository that was not taken at the owner's PC was taken on a Linux
container with no Revit, and **no compiler and no test here can tell you whether a duct moves 200
millimetres or 200 feet.**
