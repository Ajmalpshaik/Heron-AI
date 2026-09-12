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

### C2. Still to do — audit sections 4 to 10 of HANDOVER.md

**Sections 1 to 3 were checked and were wrong, so the same doubt now applies to the rest and the
question is no longer hypothetical.** They are more operational — traps, a checklist, the library
recipe, the multi-session protocol — so they should have aged better. That is a reason to expect fewer
findings, not a reason to skip the read.

Check each claim against the code, not against another document. Anything that cannot be checked
without Revit or Windows goes to [NEEDS-CHECKING.md](../../NEEDS-CHECKING.md) rather than being
deleted or believed.

### C3. Still to do — retire `plans/rag/03-working-note.md`

**158 KB and 2,383 lines of diary** — the shape the owner objected to on 2026-09-12, and the same shape
the housekeeping ledger was retired for on the same day. Apply the
[lifecycle](../README.md): move the durable part into the register that owns each topic
— decisions to [DECISIONS.md](../../DECISIONS.md), unanswered items to
[OPEN-QUESTIONS.md](../../OPEN-QUESTIONS.md), unproven claims to
[NEEDS-CHECKING.md](../../NEEDS-CHECKING.md) — then delete the note.

**Do not delete it before that**, and do not summarise it in place. A note is finished when the durable
part of it lives somewhere permanent and the note is gone.

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
