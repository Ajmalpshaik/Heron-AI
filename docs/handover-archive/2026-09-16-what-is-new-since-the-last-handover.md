# Session note — What is NEW since the last handover

> **Archived session note** from 2026-09-16. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

## What is NEW since the last handover

| | |
|---|---|
| **RAG — all nine stages** | merged as `e1f61a1`. Heron can take a document in, find the clause, cite it, refuse when it does not know, keep scopes apart, stay current, and say when two documents disagree |
| **The install manifest** | merged as `7014b47`. `requirements.txt`, `requirements-optional.txt`, `tools/check-dependencies.py` |
| **New modules** | `brain/heron_ingest.py`, `heron_ground.py`, `heron_conflict.py`, `heron_rerank.py`, `heron_research.py` |
| **New checkers** | `tools/check-narrow-errors.py`, `tools/check-dependencies.py` |
| **New suites** | `tests/test_review_findings.py` — one check per review finding — and `tests/test_dependencies.py` |
| **CI exists now** | the `Gates` workflow. Five jobs, including the C# compile 2020–2027 |
| **12 MEP and family fragments** | 2026-09-16. Routing preferences, pipe segments, pipe schedules, family lookup tables, material colour, and opening a family in Revit's own window. **Built to do a job, proved in the model the job was done in** |
| **`--session <pid>`** | `fragment`, `prove` and `validate` no longer pick a Revit by lowest PID. With two live and none named they **refuse**. A proof had come back `positive ok` against the wrong model ([row 95](../FRAGMENT-ISSUES.md)) |

## Mistakes worth not repeating

1. **A test that names a LINE instead of a RULE has a half-life.** Bit three times in one pull request.
   Anchor on the behaviour, never on a quoted sentence or a line of code.
2. **Fix one half, leave the twin.** Four of round ten's seven findings were the untouched other half
   of a round-nine fix. After any fix, go looking for its pair.
3. **`no dotnet on PATH` is not `cannot be compiled here`.** That was filed as a blocker; the SDK was
   one `apt-get install` away. **Read what was measured, not what it suggests.**
4. **Do not type a number another file owns.** A size written in a README goes stale in the one place
   it had to be right. Name the command that derives it.
5. **The shared checkout `/home/user/Heron-AI` goes stale after every push from the worktree** and git
   reports it as pending changes. **Never commit those** — they are the OLD content and committing
   writes it over the new. Verify, then
   `git restore --source=HEAD --staged --worktree .`. (`git reset --hard` is blocked by auto mode.)

## Still TO DO

**Nothing here is code that can be written from this container.** Every row needs the owner, or a
machine this is not.

> **This table is a summary; the live list is `python tools/owner-queue.py`.** Several ids below
> **moved on 2026-09-12** when the RAG working note was retired — a work note is deleted at the end
> of its life, so anything still owed had to go to a register first. The new ids are in the rows.

| | who | what |
|---|---|---|
| **W-7** *(`S-4` is ANSWERED — [`00-structure.md` §8](../work-notes/plans/rag/00-structure.md): write the parser, let one real PDF decide)* | **owner** | **one real numbered document.** Every test document so far was written to be easy. This single item unblocks the most — it settles whether the chunker survives a real spec, and replaces the invented clause number W-7 has carried since the start |
| ~~**Q-C**~~ | ~~owner~~ | ✅ **ANSWERED 2026-09-12 — yes, a counter.** [D-70](../DECISIONS.md). Stage 8's trust half is **unblocked and still unbuilt** — the decision names the shape (clause id and a count, per scope, deletable, never sent) and deliberately sets **no weights**. **R-16 and R-25 are now the top buildable job.** One thing stays open inside it: whether the question TEXT is stored, which he was not asked and D-70 does not assume |
| **[Q-55](../OPEN-QUESTIONS.md)** *(was `Q-E`)* | owner | may an INGEST read another scope? Contractual under [D-33](../DECISIONS.md) — at read time the crossing is already authorised, at write time nothing has been asked of anybody |
| **R-75 · R-76** | owner | the sizes and the package list are one command away rather than on the README page. Marked PART on his wording; one line each to change if he wants them on the page |
| **A10** | a machine with `huggingface.co` | Stage 7's after-measurement. Nothing else stands between R-41 and DONE |
| **A11** | Revit | the project key across a save, a rename and a move |
| **[F7](../PROPOSALS.md)** *(was `W-10`)* | small job | `sqlite_vec` is the only optional package whose absence is **invisible** — Heron gets slower and says nothing, while the encoder, the re-ranker and the PDF reader all announce their fallback |
| **R-74 · R-77** | Windows | nothing installs itself; `setup.ps1` deploys the add-in and no Python package |
| **R-78 · R-79** | measurement | no version floor has ever been measured, and the disk total needs the packages present |

## The one line that still matters most

**Green is not proven.** D-30 — the machine gathers evidence, a person signs. Every measurement above
was taken on a Linux container with no Revit, and **no compiler and no test here can tell you whether a
duct moves 200 millimetres or 200 feet.**

## What I would do next — SUGGESTIONS, not decisions

**Nothing in this list is agreed.** It is written down because the owner asked what the ideas were, and
because a suggestion nobody wrote down gets rediscovered from scratch three sessions later. **Anything
here can be ignored without consequence** — the TO DO table above is the real obligation.

| | idea | why it is worth doing |
|---|---|---|
| **1** | **Build the usage counter** — R-16 and R-25, to the shape in [D-70](../DECISIONS.md) | It is the only item on the list that is both **unblocked and code**. Everything else needs the owner, Revit, Windows, or a network this container has not got |
| **2** | **Ask the question the counter left open** | D-70 covers a count against a clause id. **Whether the question TEXT is stored was never put to him**, and an implementer will otherwise decide it by accident |
| **3** | **Close W-10** — `sqlite_vec` degrades with nothing said | One small, self-contained batch. It is the only optional package whose absence is invisible, and "degrade silently but say so" is the rule it half-keeps |
| ~~**4**~~ | ~~**Apply his own note rule backwards**~~ | ✅ **DONE 2026-09-12.** `03-working-note.md` was 158 KB of diary and is **gone**. The durable half moved first: the measurement to [`retrieval-history.md`](../../brain/retrieval-history.md), `W-8` to [NEEDS-CHECKING](../NEEDS-CHECKING.md) `A15`, `W-10` to [PROPOSALS](../PROPOSALS.md) `F7`, and **`Q-D` and `Q-E` to [OPEN-QUESTIONS](../OPEN-QUESTIONS.md) as `Q-54` and `Q-55`** — which moved that file's count from **1 open to 3**. Two questions had been owed since 2026-09-10 while the register said one was. **That is the argument against a question ever living in a deletable file** |
| **5** | **Agree a convention for `HANDOVER.md`** | **Two sessions wrote handover sections into it within an hour today**, and only merge order stopped a conflict. A dated section per sitting, appended, never edited above — or one file per sitting — would remove the race |
| **6** | **Re-read §1–10 of this file against the code** | They are from a different phase and nothing has checked them since. **This is a guess, not a finding** — I have not read them closely enough to say they are wrong, only that nothing says they are right |

**The one I would actually start with is number 1**, and only because the owner's answer arrived. If one
real numbered document turns up first, **that beats all six** — it is the only thing that tests whether
any of this survives contact with a real specification.
