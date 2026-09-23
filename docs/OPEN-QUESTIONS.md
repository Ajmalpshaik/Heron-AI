# Open Questions

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone about to guess at something the owner has not decided |
> | **Authority** | An answer here is **promoted into [DECISIONS](DECISIONS.md)** and that entry becomes the authority |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | **A question must never live only in `work-notes/`** — that folder is deleted at the end of its life. On 2026-09-12 two questions were found doing exactly that while this file said one was open |
> | **Its numbers** | `python tools/check-docs.py` derives answered-vs-open and **fails** if the Progress line disagrees |
> | **Where each section lives** | Since 2026-09-23 **each tier is its own file** in [`open-questions/`](open-questions/tier-1.md) — [Tier 1's](open-questions/tier-1.md), for one — and this page keeps the rules, the Progress line and, where each tier was, its heading and a line naming its file. **A new question goes at the end of its tier's file**, and it is still in this register: `check-docs.py` counts it and `owner-queue.py` lists it. The whole register, read as one text: `python tools/register-text.py docs/OPEN-QUESTIONS.md` |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


> Everything that must be decided before Heron AI can be built, gathered from reviewing the
> [Master Specification](00-master-specification.md).
> Answer inline under each question. Once answered, the answer is promoted into [DECISIONS.md](DECISIONS.md).
>
> **Priority:** 🔴 blocks all work · 🟠 blocks a major area · 🟡 needed soon · 🔵 can wait

**Progress: 57 answered · 1 open · nothing blocking any phase**

**The count moved 1 → 3 on 2026-09-12 without anybody asking anything new.** `Q-54` and `Q-55` were
raised on 2026-09-10 and 2026-09-11 and had been living in a work note — `Q-D` and `Q-E` in
`docs/work-notes/plans/rag/03-working-note.md` — which is a file whose lifecycle ends in deletion. They
were carried here when it was retired. **Two questions were owed the whole time and this register did
not know**, which is the argument for a question never living anywhere but this file.

**This line is checked, not trusted.** `python tools/check-docs.py` derives both numbers from the
questions themselves and fails if they disagree with this sentence. It said *14 answered · 26 open* until
2026-08-28, when the real figures were 20 and 20 — six questions had been answered and the sentence stayed
still, which is the exact failure the tooling in [`tools/`](../tools/README.md) exists to prevent.

**Nothing gates Phase 2 any more.** The last four — `Q-7a`, `Q-8`, `Q-9`, `Q-13` — were answered on
2026-08-28 as [D-28](DECISIONS.md) to [D-31](DECISIONS.md), and answering `Q-7a` closed `Q-37` with it.

Three of the four were settled by looking rather than deciding: at a system already doing the job
(`Q-7a`, `Q-8`, `Q-9`) and at Heron's own code, where `Q-13`'s answer had been running since Step 1.
**Ask what already exists before designing** — it worked four times out of five today.

`Q-20` closed the same day and it reshaped the plan rather than confirming it: **v1 is read-only**
([D-32](DECISIONS.md)). Asked which job he wanted first, Ajmal chose *answering questions about the
model*, and *reading first, writing soon after*. The write groups of the register now gate **v1.1**
instead of standing between him and something usable.

**`Q-41` is the forty-second question, and the first one the OWNER asked** — raised and answered on
2026-09-06, during the decision read-back rather than by any specification. He confirmed
[D-16](DECISIONS.md) as written and then asked for something it does not allow: a job done in one
project, repeated in another. Answered **both** readings, promoted as [D-47](DECISIONS.md), and it
blocks no phase. **The count moved 41→42 in one message**, which is worth noticing: this file's counts
have only ever grown by a document being adopted, and a question from him lands exactly the same way.

`Q-35` closed on 2026-08-28: Ajmal asked for all 30 Articles to be **read out**, and
accepted them after reading ([D-43](DECISIONS.md)). The Constitution is binding.

**One answer is agreed but not signed off**, and no count can show that — see `Q-34` below.

**`Q-34` counts as answered here but is not closed.** Ajmal agreed the direction on 2026-08-28 and asked
to see it working at the PC first, so [D-14](DECISIONS.md) stays **Proposed** and the confirmation is
`R1b` in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md). The count above cannot express *agreed but not
signed off*; this sentence is where that lives.

Everything else was answered on 2026-08-28. The last four — `Q-29`, `Q-31`, `Q-32`, `Q-38` — are
[D-39](DECISIONS.md) to [D-42](DECISIONS.md); `Q-38` is answered *as far as it honestly can be*, with the
public install command deferred to publication because the question's own warning says an install command
that does not work is worse than none.

**Q-12 and Q-40 were both answered on 2026-08-28.** Local only, every project
([D-26](DECISIONS.md)) — and the line falls at **the model, not the answer**: a count or a size list is
the job and may travel; the model, or a dump amounting to one, never does. That turned Q-40 from a
question about redacting words into a rule about tools, which is cheaper to build and easier to explain
to a client.

*(Five new questions — Q-29 to Q-33 — come from [Master Specification Part 2](00b-master-specification-agent-os.md).
None of them block Phase 0 either; they shape Phases 2–5.)*

---

## Tier 1 — Blocking

**Its own file:** [`open-questions/tier-1.md`](open-questions/tier-1.md)

## Tier 2 — Blocks a major area

**Its own file:** [`open-questions/tier-2.md`](open-questions/tier-2.md)

## Tier 3 — Needed soon

**Its own file:** [`open-questions/tier-3.md`](open-questions/tier-3.md)

## From Master Specification Part 2

**Its own file:** [`open-questions/from-master-specification-part-2.md`](open-questions/from-master-specification-part-2.md)

## Tier 4 — Strategic

**Its own file:** [`open-questions/tier-4.md`](open-questions/tier-4.md)

## Answered

**Its own file:** [`open-questions/answered.md`](open-questions/answered.md)
