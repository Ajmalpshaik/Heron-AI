# Heron AI — Session Handover

> **What this file is:** the **operational current-work entry point** — where the last session stopped,
> what is proven rather than merely built, and what to do next. It is a work note, not specification.
> Where it disagrees with the [Constitution](../HERON_CONSTITUTION.md), the
> [Golden Rules](14-golden-rules.md) or [DECISIONS.md](DECISIONS.md), **those win.**
>
> It belongs with [`work-notes/`](work-notes/README.md) by role, and **stays here by decision** —
> it is the documented cold start and too many things point at this exact path to move it.

## If you are the owner, starting your PC — say this and nothing else

> **"Read HANDOVER.md in Heron-AI and carry on."**

That is enough. This file is the memory; nothing else has to be remembered or repeated. It names the
branch, the numbers, the decisions, and what comes next. Two things worth adding **only if they apply
that day**:

> **If you are the owner and you want only what is waiting on YOU, read
> [`FOR-THE-OWNER.md`](FOR-THE-OWNER.md) and run `python tools/owner-queue.py`.** The page is the
> structure; the tool is the list, derived from the registers at the moment you ask so that it cannot
> go stale. It also says what is **safe to use today** — **143 proven `READ` fragments cannot modify a
> model**, so they are safe on a live project now.
>
> **For the next stretch as an ordered plan**, open
> [`work-notes/plans/next-steps-2026-09-12.md`](work-notes/plans/next-steps-2026-09-12.md) — the track
> you can run on your own machine and the track that continues here without Revit. It is a schedule and
> it will be deleted when it is spent; **this file stays the authority.**

- **"Revit is open"** — and say WHICH model, because that now decides what can be proved. Until
  2026-09-06 this line said Revit was "the one thing months of work here have been waiting on", and
  that was wrong: **nothing could execute a fragment at all**. The executor is built now, so Revit
  being open is finally the thing that matters — and [the top section](#where-this-stands-right-now--read-this-then-9a-or-9)
  lists exactly which six models finish the fragments that are still half-proved.
- **"Work only on Heron-AI and AJ-Tools"** — the standing scope. `AJ-AI-Brain` is the earlier project
  and is **read-only reference**.

**To carry the PROVING on in a fresh session, say:** *"Read the PROVING track entry in HANDOVER.md and
carry on proving fragments."* There are TWO proving entries now and the newer one
(2026-09-09) supersedes the older on method: chain `select-by-category-name` into `set-selection`
rather than selecting by hand, and use SMALL views. Between them they hold everything — the three
shapes of proof, why clearing the selection proves nothing, which things have to be DRAWN in the host
because the model's content is linked, and how to arrange a case without getting the positive and the
negative backwards. **16 to 52 in one night, then 52 to 142 the next; the method is why, not the
hours.** Have Revit open with a rich model — `Snowdon Towers Sample HVAC` is the one it was worked out
on — and read [`docs/FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md) before starting, because it is the queue.

**To do the work that has been WAITING FOR YOUR PC since 2026-09-09, say:** *"Read the
open-questions entry in HANDOVER.md and do the link contracts."* Five items are queued there in order,
the biggest being **62 reading fragments that must be able to look inside linked models when you ask
them to** — which in Qatar MEP work is most of the time, because the architecture is a link, the
structure is a link, and often the MEP you are checking is a link too. Every rule is already checked by
a gate; none of the edits was guessed, because this container has no Revit and no `dotnet` and writing
62 collector rewrites nobody could compile would have been worse than leaving a list.

**To carry the ROLES work on, say:** *"Read the ROLES track entry in HANDOVER.md."* That track is
CLOSED — all 1,202 provides declare a role and nothing guesses one from a name — so read it only to
understand why a `provides` entry must say `result` or `accounting`, and what happens if a new fragment
forgets (validation refuses it).

**To carry the REFUSAL-PROOFS work on, say:** *"Read the REFUSAL-PROOFS track entry in HANDOVER.md."*
Twelve fragments now REFUSE what they cannot use instead of reporting `0`, and eight are proved with
both legs — but **the drafts are unsigned and every `heron-status` still says DRAFT**, because the
machine never signs. That entry names the three still unproved and why, and carries the three rules for
arranging a proof that cost a whole session to learn.

**To carry the library build on in a fresh session, say:** *"Read HANDOVER.md §9a in Heron-AI and carry
on building fragments."* [§9a](#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start)
holds the whole recipe — where the sources are, the eight steps per fragment, the commands, and the
rules that must not be broken. It is written so a session can start from it without asking anything.

**To carry the LINUX BALANCE work on in a fresh session, say:** *"Read the LINUX BALANCE track entry
in HANDOVER.md and carry on."* [§10a](#10a-the-linux-balance-track--what-a-session-with-no-revit-does)
holds the whole brief — the standing agreement, how to choose what to read next by measurement rather
than by folder order, and where each method rule already lives. **It exists so the owner does not have
to paste a long prompt at the start of every session.** It is the reading sweep and the defect register,
and it is the only track that needs neither a Revit nor a Windows machine.

If a session ever tells you something that disagrees with `python tools/check-gaps.py`, **believe the
tool.** It is computed from disk every time; this file is typed by hand.

**If something looks wrong while you are working — a number that feels off, a job that says it did
something you cannot see, anything odd — go to [§4a](#4a-something-looks-wrong-mid-job--start-here).** It is
indexed by what you actually see rather than by what the cause turns out to be, and it says what to do
when the thing you hit is on no list at all.

---

## WHERE THIS STANDS RIGHT NOW — read this, then §9a or §9

> ## THE INSTALLER IS BUILT AND MERGED. NOTHING OF IT HAS RUN ON A REVIT — 2026-09-22
>
> Merged as [#253](https://github.com/Ajmalpshaik/Heron-AI/pull/253). Stages 5, 6 and 7, plus the
> shape the owner asked for the same day: **one download carries the built plugin AND the brain**, both
> doors install **offline**, and the internet is used only to ask **whether a newer version exists**.
> `R-51` to `R-56`. `Q-PE-5`, `12`, `13`, `14` and `16` answered; `R-30` corrected — it had claimed
> files that were never in the repository.
>
> **AND THAT IS ALL BUILT, NOT PROVEN.** Ten gates green, 243 suites green, 49 deliberate breaks all
> seen to fail — and **not one line of it has run against a real Revit.** No Heron tab has appeared
> because of this code. No checksum has refused a real file. [`D-30`](DECISIONS.md) does not accept a
> green suite as a proof, and neither should the next session.
>
> **THE NEXT STEP IS PROVING, NOT BUILDING**, and the prompt for it is written and waiting:
> **[PROMPT D](work-notes/plans/plugin-extension/PROMPTS-remaining-work.md)** — paste it into a fresh
> session on the owner's Windows PC. It runs `AE1`–`AE6` and `AF1`–`AF5` from
> [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md), keeps PASS / FAIL / NOT RUN apart, and **stops** if either
> of the two that matter fails:
>
> | | |
> |---|---|
> | **`AE3`** | Revit closed, install, open Revit 2024. **No Heron tab means nothing else matters.** |
> | **`AF4`** | Change one character in `checksums.txt` and install. **If it installs anyway, the whole verification story is wrong** — and that story is what makes handing a folder to somebody on a USB stick safe. |
>
> **DO NOT BUILD ON TOP OF THE INSTALLER UNTIL PROMPT D HAS BEEN RUN.** Everything after it assumes a
> tab that has never been seen.
>
> **Waiting on the owner, and nobody else may decide them:**
>
> - **No release has ever been published** — zero, verified. `AC1`–`AC5` and `AF6` cannot run until
>   one exists, and publishing is his call.
> - **Stage 8, signing** — parked at his request; he said he would give an alternative.
> - **Stage 9**, and with it [`R-52`](work-notes/plans/plugin-extension/01-requirements.md)'s *"ask
>   where to keep it"*, which `heron-install` does **not** do. Recorded rather than left to be found.


> **UPDATED 2026-09-15.** The rows below were rewritten that day against derived numbers. Several had
> been stale for a week — the agent row said 71 when it was 211, the test row said 41 suites when it
> was 188. **Derive every number before trusting it**; each row names the command.
>
> **UPDATED 2026-09-16.** A working session, not a proving one: a copper refrigerant pipe type built
> in `PIPE` for a real job, **12 new fragments because the job needed them**, and 13 signed. **Its working note was deleted
> on 2026-09-19, once every durable part of it had landed elsewhere** — which is what a work note
> is for. What survives, and what to read before touching the MEP or family fragments, is
> [`FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md) rows 95–99 — of which **95 to 98 are now FIXED and only
> 99 is still open**, held as a question rather than a bug. This sentence read *"rows 95–98, two of
> them still OPEN"* until 2026-09-19, and both halves of that had drifted: derive it with
> `python tools/open-defects.py` rather than reading a range here.

**395 fragments. 316 `PROVEN`, 79 `DRAFT`, as of 2026-09-19** — and on 2026-09-14 **all 360 compiled on
all eight releases they claim, for the first time.** These two numbers moved twice while this block was
being written: derive them with
`grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`. D-28's executor is built, fragments run against a real
model, and since 2026-09-09 they can also CHANGE one. That is the thing every earlier handover was
waiting for.
**Proving is live and these two numbers move hourly — run
`grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c` rather than trusting the
line above.**
**On 2026-09-07 every recorded proof was re-run and all thirteen held, and the executor was put in
front of all 135 DRAFT READ fragments** — see [the verification pass](handover-archive/2026-09-07-the-verification-pass-13-proofs-re-run-135-fragments-through.md).

| | |
|---|---|
| Fragments | **360** — every fragment-shaped job in the earlier library, five cross-project transfers from PART 5, `CREATE_GLOBAL_PARAMETER` (2026-09-07), `FIND_DATES_IN_VIEWS`, built AND proved 2026-09-08, and **ten added 2026-09-08 for the review's N01–N09 plus the read they depend on** — see the entry below. All ten are `DRAFT` and NONE has met a model |
| Proven | **197 of 360** as of 2026-09-11 (was 198; `export-views-to-fbx` was put BACK to `DRAFT` by the owner on 2026-09-11 — it is `risk: PUBLISH` and was proved through `validate`, which does not apply the risk gate `fragment` and `prove` both apply. Its proof block was kept; only the status claim was withdrawn. Before that 196; PR #108 promoted `import-parameter-values` and `export-views-to-fbx` — the first found by WRITING the CSV it reads rather than waiting for the model to hold one. Before that 188; PR #104 promoted eight found by SWEEPING fifty-one fragments in two bulk passes instead of probing one at a time — including `sum-by-group`, which with `group-and-count` completes the pair the `keep-chain` fix was built for. Before that 186; PR #99 promoted `flip-elements` and `dimension-wall-openings`, both blocked on Snowdon because its architecture is in a link and both proved on `Project1`'s hand-drawn walls and door — the third and fourth times a triage row named the model that would work. Before that 185; PR #96 promoted `set-mep-slope`, proved on `Project1` after Snowdon refused all 22 of its ducts — the second time a triage row named the model that would work and was right. Before that 180; PR #92 promoted five proved on `Snowdon-scratch` — four re-runs whose earlier records were same-day and therefore stale, plus `check-flow-direction`, which `Project1` could not feed because its ducts carry no system. Before that 178; PR #88 promoted the last two proved on `Project1`. Before that 175; PR #86 promoted three proved on the same model, one built by hand for the purpose — including `find-dead-ends`, which §3b had set aside three times on Snowdon and which needed only a duct run with a loose end. Before that, 167; PR #79 promoted eight — the first WRITE fragments this repository has promoted) — 142 at the start of the day's second proving track, **+18 from it**, and `set-view-section-box` back to `DRAFT` when its implementation changed after its proof was signed. Each carries a recorded proof with a negative case and a staleness fingerprint (D-30). Moving hourly — derive it, do not read it here |
| Compile gate | green, Revit 2020–2027 — **and since 2026-09-14 all 360 fragments compile on all eight releases too**, which had never been run on any machine before that day |
| Contracts | **124** in `brain/agents/`. `python tools/generate-contract-reference.py` builds a page of every one against the code that claims it, and it found **three refusals the code produces that no contract declared** on its first run. All three are fixed; every declared refusal is now reachable and every refusal declared |
| Revit API | **The full public surface of all eight releases is now readable.** `python tools/api-changes.py` says what each release stopped shipping — 4,194 members across seven transitions. `ElementId.IntegerValue` is in the 239 that left at 2026, and this repository had written it down as *"the property every version has had"*. **Ask `HERON-REVIT-ACI-034` before writing a Revit member you have not compiled** |
| Other gates | metadata, docs, gaps, agent-count, **structure** — all green. **`check-licence` added 2026-09-09 and it EXITS 1 on a finding**, unlike the other reports; 370 units, all clean today ([D-66](DECISIONS.md)). `check-revit-gate` and `check-reachable` are reports and exit 0, so their findings are questions and two of them are now worklists. The `structure` red at `83fd7e8` was `read-space-loads` naming a vendor namespace in `brain/`; **fixed 2026-09-08**, and note the checker greps the file text, so a COMMENT mentioning it fails too |
| Tests | **199 suites as of 2026-09-19, and ALL 199 pass in a plain Linux container** - derive the total with `ls tests/test_*.py | wc -l`. **That took installing rather than excusing, and this row used to say the opposite.** Four were carried for weeks as needing a special machine and not one of them did: `test_dotnet` and the compile gates needed `apt-get update && apt-get install -y dotnet-sdk-10.0`; `test_bridge_roundtrip` needed one `dotnet build` of the test host, the exact line its own failure message prints; `test_mcp_serves` and `test_served_claims` needed `pip install --user mcp` **followed by `pip install --user --upgrade cryptography cffi`** - and the earlier attempt that was backed out had installed `mcp` alone, which does panic on import. The missing half was in `.claude/skills/heron-ship/SKILL.md` sections 2 and 4 the whole time. **An excuse that turns out to be false and a dependency that is genuinely hostile look identical from the outside** - the only way to tell is to try it and read what changed, and every one of these four was the first kind. **`.github/workflows/gates.yml` still leaves the MCP SDK out on purpose and its known-failure list is unchanged** - the job goes red on both a new failure AND a listed one that starts passing, so this is about your container, not CI. **Do not fix a test by editing it until it passes** - `test_embed` and `test_retrieve` were re-based against the model backend, not edited until green. **And do not pin a derived count inside a test**: `test_csharp` pinned three measured integers on 2026-09-15 and went red the same hour when one C# file was added. Check the CLAIM, not the figure. **`test_supply.py` is the one that earned its keep on 2026-09-19** - it caught an invented `source:` value that all four gates had passed through three commits. **AND THE ROW WAS PROVED RIGHT AGAIN ON 2026-09-20, BY A SESSION THAT HAD BEEN CARRYING THE EXCUSE IT WARNS ABOUT.** A cloud container ran for hours reporting *"four suites cannot pass on Linux and that is expected"* - `test_dotnet`, `test_bridge_roundtrip`, `test_mcp_serves`, `test_served_claims` - because its brief said so. **All four pass**, and the commands are the ones written above and in [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) §§2 and 4: `apt-get install -y dotnet-sdk-10.0`; `dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 -p:HeronTfm=net10.0 -p:OutputPath=bin/x64/Debug-net10.0/`, which the suite's own failure message prints verbatim; then `pip install --user mcp` followed by `pip install --user --upgrade cryptography cffi`. **The excuse outlived the fix a second time, in a session that had this row open in front of it** - which is the row's own point, and the reason it is worth reading before believing any "cannot run here" |
| Register | **71 rows, 19 closed, 52 left** — PART 6 added Group J, the eight that would prove the executor's inputs. Group A is FINISHED. **Only `R1b` does not need Revit** |
| Add-in | **THAT CLAIM WAS WRONG AND IS CORRECTED. Rebuilt and redeployed 2026-09-10, to Revit 2020, 2024 AND 2027**, verified at binary level. The binary Revit had loaded was dated 2026-09-08 23:06 while `RevitFragment.cs` was written 2026-09-09 23:14 - so D-67's caller-value widening and the rollback check had NEVER reached the machine ([FRAGMENT-ISSUES](FRAGMENT-ISSUES.md) rows 8 and 12). Deploy ONE release at a time. Rebuild it after ANY change under `revit/` — and check the framework first: `check-compile.py` builds 2020–2027 into one folder and the newest wins, so a run of it leaves .NET 10 binaries that Revit 2024 refuses with *"Revit cannot run the external application"*. `deploy-addin.ps1` now guards this rather than trusting the operator |
| Agents | **215 of 250 have code** as of 2026-09-16, 4 host-provided by D-01, **31 left** — `python tools/agent-count.py`. **Sixteen departments are complete.** Phase 0/1's agent list is COMPLETE. The 31 split cleanly and the split is the useful part: **16 wait on one sentence from the owner** — **F23** four Documentation rows, **F27** five Development rows, **F31** five Standards rows, **F15/F17** two Naming rows — and **10 Revit Engineering rows are open work**. **Four were built on 2026-09-16**: `HERON-REVIT-API-020` (reasons ABOUT the API rather than touching a model), `HERON-REVIT-SYS-030` (MEP systems), `HERON-REVIT-PAR-011` (parameters) and `HERON-REVIT-GRP-033` (groups and assemblies). **They are not blocked on a compiler** — [§30](30-compiling-away-from-windows.md) is a five-minute install and every one builds on 2020–2027 once it is done. They are blocked on a MODEL: each is `MODIFY` in the register, which that column defines as the HIGHEST permission it can require, so each is built READ-FIRST exactly as `RevitLinks` and `RevitPhases` were — and none of the five has ever run |
| MCP tools | **24** as of 2026-09-16. The five a modeller will feel are **`revit_links`** (2026-09-14), **`revit_phases`** (2026-09-15), **`revit_systems`**, **`revit_parameters`** and **`revit_groups`** (all 2026-09-16), and they answer the same class of question: **a number about a model is only a number about the part of it you asked for.** Elements inside a LINK are not in the host document; a count is a fact about a model AND a phase AND a design option; an MEP element **connected to nothing** is on no system and missing from every total; a parameter check that reads only the INSTANCE reports a confident zero on data sitting on the TYPE; and an element inside a GROUP carries your edit into every placement of that group, which Revit does not warn about. All five read-only. **`revit_groups` is the PRE-FLIGHT for the write tools** — it is how `RevitWrite`'s *"almost certainly inside a group"* stops being a guess made afterwards. **Define a new tool ABOVE `if __name__ == "__main__"`** — `server.run()` never returns, so a decorator below it never runs and the tool is simply absent while the registry still declares it. Round-one Codex finding; `tests/test_api_docs.py` checks it now. Derive the count: `grep -c '^@server.tool()' mcp/server/heron_mcp_server.py` |
| Open questions | **57 answered, 1 open, nothing gating any phase.** **[Q-58](OPEN-QUESTIONS.md) opened 2026-09-21** while building Stage 3 of the installer: `platform/README.md` rule 3 says `HeronPaths` is the only thing that builds a Heron path, and `tools/deploy-addin.ps1` builds two because a `.ps1` cannot call a C# class. Recorded rather than closed whichever way was easier. **[Q-57](OPEN-QUESTIONS.md) closed 2026-09-21 as [D-86](DECISIONS.md)**: a write may declare a question **only when answering it REQUIRES the write**. It had asked — may a fragment or skill that WRITES declare a question? Ten fragments and five skills do ([row 158](FRAGMENT-ISSUES.md)), and some are probably right, so the rule is undecided rather than broken. **The last four closed 2026-09-20** as [D-81](DECISIONS.md) to [D-84](DECISIONS.md), and the method is the finding: asked back to the owner **one at a time, in plain words, with a worked example each**. Four at once was dismissed twice, and so was the same set translated into Malayalam — **the language was not the barrier, the batch was.** `Q-51` carried text is **stamped, not scanned**. `Q-54` **changed the shape of its own question**: the cloud line is drawn by **content type, not scope** — all documents may go, models and families never — superseding D-24's framing at his explicit request. `Q-55` went **further than the question offered**: not a count and a clause number but the **prior practice by name**. `Q-56` took the cheapest honest answer — nothing more than today, **and the word sandbox goes**, which makes the rename the deliverable. Derived by `python tools/check-docs.py`, never read from a sentence |
| Tools | **36** in `tools/` and **20** MCP tools as of 2026-09-15 — `ls tools/*.py \| wc -l` and `grep -c '^@server.tool()' mcp/server/heron_mcp_server.py`. New since the last entry: `generate-contract-reference.py` (what was built, against what it promised) and `api-changes.py` (what each Revit release stopped shipping). New MCP tools include `revit_links` and `revit_phases` |
| Bindable inputs | **CLOSED 2026-09-09.** PART 6 bound what the selection and the previous fragment could give; the caller's half — a category, a name, a distance — arrives as text now and is resolved inside Revit (D-54). It was the largest unlock left: **287 of 360 fragments** declare such a need, 675 needs between them. **Widened again 2026-09-09**: an element TYPE by name, nine narrower classes (`WallType`, `Phase`, `FilterElement` and the rest), and **a point in millimetres** ([D-67](DECISIONS.md)) — which took the arrangeable library from 6 to 40. **Widened again 2026-09-14** ([D-72](DECISIONS.md)): pairs of points (a PIPE between them), `OverrideGraphicSettings`, `ForgeTypeId`, `ParameterValue`, and the word `selected` for a LIST of element ids - six more fragments arrangeable. **What is still refused is now ONE thing and it is not a missing rule: `IList<Reference>`, a FACE.** A face is picked with a mouse and no text names one, so `place-family-on-face` needs Revit's own picking rather than a parser. Derive the rest with `python tools/generate-jobs.py` |
| Branches | **`main` only** after PR #142 merged on 2026-09-15 (211 agents, the fragment compile, the full Revit API surface). **`main` only, and it is the only branch that exists.** **Sixteen PRs were merged on 2026-09-09** (#44–#61) and every branch behind them is deleted — the role-declaration stack, the silence-illegal fixes, the job generator, and the proving track. **Start from `main`**; nothing is parked outside it. The sha is not written here - `git log --oneline -1 origin/main` - because it moved twice while this row was being read |

### Latest sittings

**Every session note lives in its own file in [`handover-archive/`](handover-archive/README.md)**, one
file per sitting, newest first in that folder's README. These are the newest 12. Write a new note there,
not here — [`tools/archive-handover.py`](../tools/archive-handover.py) moves any that land here anyway.

| Date | Sitting |
|---|---|
| 2026-09-24 | [THE RE-RANKER RAN FOR THE FIRST TIME, AND IT MAKES PICKING A TOOL WORSE](handover-archive/2026-09-24-the-re-ranker-ran-for-the-first-time-and-it-makes-picking-a.md) |
| 2026-09-23 | [THE THREE ROWS C1 LEFT OPEN ARE FIXED, AND THE SETUP SCRIPT HAS TO BE PASTED AGAIN](handover-archive/2026-09-23-the-three-rows-c1-left-open-are-fixed-and-the-setup-script.md) |
| 2026-09-23 | [THE GUARD RAN ONLY WHERE ITS SKILL WAS LOADED, AND NOTHING SAID WHEN MAIN HAD MOVED](handover-archive/2026-09-23-the-guard-ran-only-where-its-skill-was-loaded-and-nothing.md) |
| 2026-09-22 | [THE CATEGORY-OVERRIDE READ-BACK COULD NOT SHOW THE FILL](handover-archive/2026-09-22-the-category-override-read-back-could-not-show-the-fill.md) |
| 2026-09-22 | [THE SYSTEMS CHECK COUNTED TERMINALS AND CALLED THEM THE SYSTEM](handover-archive/2026-09-22-the-systems-check-counted-terminals-and-called-them-the.md) |
| 2026-09-22 | [THE RELPATH CRASH: TWO MORE CALLS FIXED, AND THE RULE MOVED WHERE EVERY TOOL CAN IMPORT IT](handover-archive/2026-09-22-the-relpath-crash-two-more-calls-fixed-and-the-rule-moved.md) |
| 2026-09-22 | [FOUR HINTS TOLD A CALLER TO TYPE WHAT REVIT REFUSES, AND FOURTEEN SAID NOTHING](handover-archive/2026-09-22-four-hints-told-a-caller-to-type-what-revit-refuses-and.md) |
| 2026-09-22 | [SESSION CLOSED. THE READING SWEEP THROUGH `tools/`, AND WHERE IT STOPPED](handover-archive/2026-09-22-session-closed-the-reading-sweep-through-tools-and-where-it.md) |
| 2026-09-22 | [IT SUPPRESSED 52 PRODUCTION FUNCTIONS AND NOT ONE REAL DISPATCH](handover-archive/2026-09-22-it-suppressed-52-production-functions-and-not-one-real.md) |
| 2026-09-22 | [ELEVEN OF FOURTEEN REPORT `0`, AND THAT ZERO MEANS THREE THINGS](handover-archive/2026-09-22-eleven-of-fourteen-report-0-and-that-zero-means-three-things.md) |
| 2026-09-22 | [THE API PAGE'S ONE FINDING WAS FALSE, AND TWO DOCSTRINGS TYPED A STALE COUNT](handover-archive/2026-09-22-the-api-page-s-one-finding-was-false-and-two-docstrings.md) |
| 2026-09-22 | [THREE AGENTS ARE COUNTED AS BUILT BY A TEST SUITE'S HEADER](handover-archive/2026-09-22-three-agents-are-counted-as-built-by-a-test-suite-s-header.md) |

## The session archive — what happened before today

**Every sitting up to 2026-09-12 is recorded in [`handover-archive/`](handover-archive/README.md),
one file per sitting.** They were held in this file in full until then, which is why it reached 5,931
lines: a reader looking for what to do next had to walk past forty-four finished sessions to find it.

**You do not need to read any of them to carry on.** The durable part of each — the decisions, the
defects, the unanswered questions, the unproven claims — was already written into the registers that
own those things, and those are the files to believe:

| If you want | Read |
|---|---|
| A decision and why it was taken | [DECISIONS.md](DECISIONS.md) |
| A fragment that misbehaves, and the queue | [FRAGMENT-ISSUES.md](FRAGMENT-ISSUES.md) |
| A question nobody has answered | [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) |
| A claim that has never been checked | [NEEDS-CHECKING.md](NEEDS-CHECKING.md) |
| A reviewed idea | [PROPOSALS.md](PROPOSALS.md) |

Read an archived note when you want the **narrative** — how something was found, what was tried
first, why an approach was abandoned. That is the part a register deliberately does not carry.

**Going forward, a sitting writes its own file in that folder and adds one line to its index.**
Two sessions wrote sections into *this* file within an hour on 2026-09-12 and only merge order
stopped a conflict — two sessions writing two new files cannot collide at all.

## 1. Where the project stands

**Do not read a number off this page.** Sections 1 to 3 used to carry typed counts and on
2026-09-12 every one of them was wrong — they described Phase 0, where the library was seven
`DRAFT` fragments and nothing could execute one. They were replaced with the commands that derive
the answer, because a number typed here goes stale in the one place it had to be right.

```bash
ls brain/fragments/ | wc -l                                   # fragments in the library
grep -rh '^heron-status:' brain/fragments/*/fragment.yaml \
  | sort | uniq -c                                            # how many are PROVEN
python tools/agent-count.py                                   # agents built, and what is left
ls tests/test_*.py | wc -l                                    # suites
python tools/check-gaps.py                                    # what is unfinished vs waiting
```

**[The top of this file](#where-this-stands-right-now--read-this-then-9a-or-9) is the live
picture** and is kept current. This section exists only for the part that a command cannot tell
you: what has been witnessed at a real machine, below.

**What has changed since sections 1 to 3 were written**, so an old reading is not carried forward:
the repository is **public**; the executor exists, so a fragment can be *run* rather than merely
resolved; writes are proved, not just compiled; and the compile gate covers **2020 to 2027**, not
2020 to 2024.

### The distinction that still matters most

**Built is not proven, and compiling is neither.** [D-30](DECISIONS.md) is the rule: the machine
gathers the evidence and **a person signs it**. No compiler and no test in this repository can tell
you whether a duct moves 200 millimetres or 200 feet — which is why a fragment stays `DRAFT` until
one recorded proof **with a negative case** exists against a real model.

## 2. What exists

**The folder map lives in [PROJECT-MAP.md §B](PROJECT-MAP.md).** It was duplicated here, and the
copy went stale while the original stayed right — so this section is a pointer now rather than a
second answer. [§D of that file](PROJECT-MAP.md) also settles which source wins when two disagree.

## 3. What has been witnessed at a real Revit

**This table is the reason this section survives.** Everything in it was seen at a machine, by a
person, and no command on disk can re-derive it. Anything NOT in it should be assumed unproven no
matter how green the gates are.

### Proven against a real Revit
| | |
|---|---|
| The bridge answers `ping` | Revit 2020 and 2024 |
| One button connects **and** disconnects; the icon shows which | Toggled repeatedly, every transition logged |
| Per-session token, minted per connect | 2024's token on the 2020 pipe → `unauthorized` |
| Newest connection wins — **the pipe only, since Step 6** | *"A newer connection took the session"*, older one dropped. Still true of the transport; a lease now decides who may actually send anything ([D-22](DECISIONS.md)). **The lease itself is unproven** |
| Two Revits at once, separate pipes | `heron.2024.*` and `heron.2020.*` together |
| **The thread hop** | `5,844 elements` from 2024, `3,167` from 2020 |
| **"Revit is busy" instead of a hang** | Dialog open → clean refusal after 10s, recovers by itself |
| **The MCP server inside Claude Code** | Answered from both live Revits, no command run |
| **"Select all ducts"** | 4 found and **highlighted on screen**, confirmed by eye |
| The audit trail | Every request recorded — op, outcome, document, timing |
| **Fails closed when the chosen Revit closes** | 2024 closed with 2020 open → Heron **stopped** |
| `setup.ps1` end to end | Detects, builds, deploys all three in one run |
| Installer skips only the **open** release | 2020 and 2024 open → both skipped by name, 2027 installed |
| Each build carries the right runtime | `net472` / `net48` / `net10.0-windows`, read from the deployed DLLs |

> For what is proven **since** — the fragment library, the write path, the rollback — read the top
> of this file and [FRAGMENT-ISSUES.md](FRAGMENT-ISSUES.md). The tables that used to sit here
> listing *built, never met a real Revit* were a Phase 0/1 inventory and are gone: most of what
> they listed as unproven has since been proved, and a stale "unproven" reads as an obligation
> that nobody owes.

---

## 4. The things that will bite you

Each of these cost real time. They are in the order they were learned.

> **Looking something up while a job is going wrong? Use [§4a](#4a-something-looks-wrong-mid-job--start-here)
> instead.** This list is ordered by CAUSE, which is only searchable once you already know the answer.
> §4a enters the same knowledge from the symptom.

1. **The Revit API can only be called from Revit's own thread, inside an API context.** An MCP server is
   a separate process and cannot call it at all. Everything marshals through one `ExternalEvent`.
   [docs/03 §4](03-heron-revit.md)

2. **A named pipe needs `CreateNewInstance`, not just `ReadWrite`.** Without it only the *first*
   listener can be created and every retry fails with "access denied".

3. **The discovery file must never carry the document name.** Storing it produced the stale-name trap in
   the owner's earlier work. [docs/25 §2a](25-multi-session-and-binding.md)

4. **"No reply" does not mean "dead".** Test the *process*, not the reply — and check it is still the
   right *program*, because Windows reuses process ids.

5. **An assumption is not a choice.** If one Revit was open, Heron *assumed* it. If the user answered,
   they *chose*. Conflate the two and a second Revit opening mid-chat silently sends everything to the
   first one. [docs/25](25-multi-session-and-binding.md), and `tests/test_session_binding.py`.

6. **Naming the document is not always enough.** Two sessions can both have `Project1` open — it
   happened. Name the session too.

7. **Two waits, not one.** *"Did Revit pick it up?"* and *"having started, did it finish?"* are
   different questions. Collapsing them tells the user Revit is busy while it is actually working.

8. **Retry only what never left the machine.** A failed connect can always be retried. A **lost answer**
   cannot — Step 6 writes must pass `idempotent=False` or a move happens twice.

9. **`Platform=x64` puts build output in `bin/x64/$Configuration`.** Scripts should *find* the
   assembly, not assume the path shape.

10. **An empty array passed to a PowerShell parameter arrives as `$null`**, and `@($null)` has one
    element. This turned "no Revit open" into "one Revit open" and blocked every install.

11. **A contract name becomes a C# variable, so it has to be able to be one.** `FRG-QA-001` declared a
    value called `checked` — a reserved C# keyword — and its own code read `if (checked == 0)`. It could
    never have compiled, and it passed every check for as long as nothing compiled a fragment.
    `heron_fragment.py` now refuses reserved words and non-identifiers outright.

12. **A `#line` path must be absolute.** The fragment compile harness pointed errors back at each
    fragment's own file with a repo-relative `#line`. That built fine on `net472` and `net48` and failed
    on every `net8`/`net10` release with `CS1504 source file could not be opened` — the newer compiler
    *resolves* that path rather than only printing it, and resolves it against the project. Found by
    running all eight releases instead of the one that happened to work.

13. **Revit's move call returns normally and moves nothing, for a group member.** No exception, no
    return value, no warning. Counting *"it did not throw"* as *"it moved"* reports **"Moved 5,
    skipped 0"** for five air terminals that have not shifted a millimetre — the "succeeded and did
    nothing" failure [D-30](DECISIONS.md) exists to catch, in the one place Heron can actually
    change a model. Heron skipped **pinned** elements, which is one half of the case; a group member
    is not pinned and went straight through. `RevitWrite` now probes each position before and after
    and reports four outcomes — moved, partly, blocked, unverified — instead of the count it asked
    for. **Found by studying the owner's earlier library rather than by reading Heron's own code**,
    which had been read several times. `E10` is the check.

---

## 4a. Something looks wrong mid-job — start here

**§4 above is indexed by CAUSE. This one is indexed by what you actually SEE**, because mid-job you
have a symptom and not a diagnosis, and nobody can search a list of causes with a symptom. Same
knowledge, entered from the other end.

### The rule that comes before the table

**Write down exactly what you saw, word for word, BEFORE you touch anything.** The first instinct is to
run it again. In Revit that destroys the evidence: the model has moved on, the selection is gone, the
transaction closed. A re-run that succeeds tells you nothing about the run that did not, and now
nobody can ever look at the one that mattered.

Copy the message. Note which element, which view, which document. Then investigate.

### Symptom → check this first

| What you see | Check this first |
|---|---|
| *"Done — 5 elements"* and you are not certain | **Go and measure one, by hand, in Revit.** Revit's move returns normally and moves nothing for a group member — no exception, no warning. §4 note 13 |
| A height or level that is plausible but feels off | `REPORT_LEVEL_ELEVATIONS`. A level has **two** heights and only `ProjectElevation` is in the same space as the model's coordinates. On a survey-offset site model every height answer is wrong by exactly that offset |
| **Every** room's volume reads zero | *Area and Volume Computations* is set to areas only. `MEASURE_ROOM_DIMENSIONS` reports `volumeComputationOff`. **One** room at zero is that room's bounding, which is a different problem |
| Every space looks balanced | Which spaces have **no design figure at all**. Design zero against actual zero passes a balance check, so the rooms nobody has designed report as the ones with no problem |
| A count higher than what is standing on site | Nested families and insulation. An AHU with a nested fan, coil and filter is four instances and one unit — `GROUP_BY_ASSEMBLY` |
| A painted finish reports zero area | Paint and geometry are two separate material sets and the area call needs the same flag it was listed with — `REPORT_MATERIAL_TAKEOFF` |
| A ceiling-height answer says *"no ceiling"* for a room that plainly has one | The room's **Upper Limit**. A room's solid stops there, so a solid test intersects nothing. `MEASURE_CEILING_HEIGHT` avoids it by design |
| You asked a **question** and the model **changed** | `python tools/check-routing.py` — the ladder-crossing list at the top. A read sentence can rank below a fragment that writes. [D-101](DECISIONS.md) |
| A fragment "does not exist", or ranks nowhere | The store is **stale**, not the library broken. `check-routing.py` rebuilds when its count disagrees with disk and says so |
| It hangs, with no error | **Two waits, not one.** *"Did Revit pick it up?"* and *"having started, did it finish?"* are different questions. §4 note 7 |
| *"Access denied"* opening the bridge | `CreateNewInstance` on the pipe. §4 note 2 |
| It went to the **wrong Revit** | An assumption is not a choice, and two sessions can both have `Project1` open. §4 notes 5 and 6 |
| It compiles clean and Revit rejects it anyway | **A green check is only as wide as what the checker reads.** `check-fragments-compile.py` reads fragment implementations; anything generated elsewhere is unchecked until something reads it too |
| A checker went green and you did not fix anything | Suspect the checker, not your luck. Find the line that changed |

### The anomaly that does not look like an anomaly

This is the one worth reading twice, because **the failure this whole project is built around is
reporting the number that was ASKED FOR as the number that HAPPENED.** It arrives looking like success.

Treat these as suspicious even when nothing is obviously wrong:

- **A clean round result with zero failures** — *"Moved 5, skipped 0"*. Ask what it would have printed
  if it had moved nothing.
- **It came back faster than the work should take.**
- **A zero where you expected a small number.** Nobody double-checks a big wrong number; everybody
  believes a zero.
- **Totals that match what you asked for rather than what was found.** A report that can only agree
  with its own input is not a check.
- **Everything passing right after a change somewhere unrelated.**

The test in every case is the same one written into every fragment's proof spec: **a second route.**
Ask Revit the same question a different way — a schedule, the Properties palette, a dimension placed by
hand, a spot elevation. If the two disagree, the second route is right until proven otherwise, because
it is the one a person can see.

### It is on no list — then what

Four steps, in order, and step 1 is the one that gets skipped.

1. **Record it verbatim, before re-running.** See the rule above.
2. **Ask three questions.** *Does it happen again?* *Does a second route agree?* *Would it change what
   somebody does?* An anomaly that is not reproducible, or that changes nothing, is still worth writing
   down — but it is not worth stopping the job for.
3. **Route it to exactly one place**, and do it in the same session:

   | What it turned out to be | Where it goes |
   |---|---|
   | Needs a real Revit, or Windows, to settle | A new row in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) — the single register |
   | A Revit behaviour worth never re-learning | §4 above, numbered, in the order learned |
   | A choice that could reasonably have gone the other way | [`docs/DECISIONS.md`](DECISIONS.md), with what was rejected and why |
   | Something a tool could have caught and did not | A checker in `tools/` — that is how §4 note 11 and the risk ladder both became permanent |
   | A defect in one fragment | That fragment, plus a **negative case** in its `tests/cases.yaml` |

4. **If it needs Revit and Revit is not in front of you, it becomes a register row — not a half-fix.**
   That is [D-97](DECISIONS.md): build it all out now, prove it in one concentrated pass later. A
   speculative fix to something nobody has watched fail is a change with no evidence behind it, and it
   costs more to unpick than to write.

### Why this is worth the minute it takes

**A finding that only exists in the chat is gone when the session closes.** Every one of the thirteen
lessons in §4 cost real time, and each is there because somebody wrote it down instead of fixing it
quietly and moving on. The register is not bureaucracy — it is the only reason the next session does not
pay for the same discovery twice.

---

## 5. What you can and cannot do without Revit

### Works anywhere, including mobile

**There is no list of suites here any more, and that is the point.** This section used to name
eighteen of them and state *"18 suites; 16 pass here… 514 individual `ok`/`PASS` lines"* — while
calling that figure *"derived, not typed"*. On 2026-09-12 the repository held **57**, so the list was
missing thirty-nine, including the whole RAG subsystem and every gate added since. A reader following
it would have believed two thirds of what runs here does not exist.

**Derive them:**

```bash
ls tests/test_*.py | wc -l                        # how many suites there are
ls tests/test_*.py                                # which ones
for f in tests/test_*.py; do
  python "$f" >/dev/null 2>&1 && echo "pass  $f" || echo "FAIL  $f"
done                                              # which pass on THIS machine
```

**The last command is the one that matters, because the answer is machine-specific and has twice been
reported as though it were not.** `test_ingest` fails on Windows for an encoding reason and passes on
Linux; `test_document_retrieval` fails on the **trained** backend and passes on the fallback, so CI has
only ever seen it green because CI cannot reach `huggingface.co`. Run it where you are, and read
[`NEEDS-CHECKING`](NEEDS-CHECKING.md) `A14` before deciding a failure is yours.

> **Set `HERON_KNOWLEDGE` before anything that touches `brain/`.** On Windows the stores go under
> `%APPDATA%`; anywhere else there is no such folder and the store has nowhere to live, so the tools stop
> with a named error rather than inventing a location. `export HERON_KNOWLEDGE=~/.heron` is enough. The
> stores are **derived** — deleting them is always a safe recovery, and `python brain/heron_scope.py
> --rebuild` puts them back.

**The three checkers, and the two suites that need something installed first:**

```bash
python tools/check-docs.py             # links, rules, decisions, questions, and the generated table
python tools/check-metadata.py         # the standard, registry vs code, and version agreement
python tools/check-structure.py        # layout, layering, paths, and the PowerShell ANSI trap
```

`test_mcp_serves.py` needs `pip install --user mcp` and **exits 3 without it** — which `check-gaps`
reads as WAITING, because a skip reported as `ok` is a green nobody earned. `test_bridge_roundtrip.py`
needs its host built (below). Neither needs a Revit.

**And one command that runs all of the above and then looks for what is missing:**

```bash
python tools/check-gaps.py             # UNFINISHED vs WAITING. The exit code follows only the first
```

It is the first thing to run in a new session and the last thing to run before saying anything is
finished. **It found real defects on its first runs, including two in itself** — a scanner that scans
itself reported its own regex as two undeclared agents — and it caught a second invented agent id in as
many steps. Growing the library from 2 fragments to 7 exposed a query bug that had been **invisible at
two**: every word was prefix-matched, so `in*`, `me*` and `the*` outvoted the one word in the sentence
that carried meaning.

**And two more things that were believed to need Windows, and do not** (2026-08-28,
[docs/30](30-compiling-away-from-windows.md)):

```bash
python tools/check-compile.py                  # Revit 2020-2027, all four projects, 0 warnings

dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 -p:HeronTfm=net8.0 \
    -p:OutputPath=bin/x64/Debug-net8.0/
python tests/test_bridge_roundtrip.py          # the whole lease, end to end
```

The first needs the .NET SDK, which Linux distributions package — Microsoft's CDN is often blocked from a
container and that is the wall earlier sessions hit. The second runs because `Heron.Bridge` has no Revit
reference, so it compiles for `net8.0` and .NET implements named pipes on Unix as a socket.

> **Both extra arguments above were added on 2026-08-30, after following this section verbatim failed
> twice.** Neither failure was a defect, and neither announced itself as an environment problem:
>
> - **`-p:OutputPath` is not optional if `check-compile.py` ran first** — and this section tells you to
>   run it first. That tool builds the same project once per Revit release into the shared
>   `bin/x64/Debug`, so the POSIX host needs its own folder or the test finds the wrong artifact.
>   `test_bridge_roundtrip.py` had already documented this interaction in its own header and printed the
>   corrected command on failure; only this section was missing it.
> - **`apt-get install -y dotnet-sdk-10.0` gives you a machine that cannot run this test.** The host
>   targets `net8.0`, the .NET 10 SDK carries no 8.0 runtime, and the host then fails to start — which
>   the test reported as *"host never reported a pipe name"*, the symptom of every possible cause. Fix
>   with `apt-get install -y dotnet-runtime-8.0`, or `DOTNET_ROLL_FORWARD=Major`. **The test now names
>   this case itself** rather than blaming the transport. Also worth knowing: the first
>   `apt-get install` 404s on a stale index — run `apt-get update` first.

**What that still does not cover is the Windows named pipe itself** — its naming, its security
descriptor, and the `CreateNewInstance` flag in note 2 of [§4](#4-the-things-that-will-bite-you). `A4`
in the register means the Windows run, and a POSIX pass is a strong signal ahead of it rather than a
substitute for it.

**A third thing, added 2026-08-28** — it reads what the releases actually ship:

```bash
python tools/check-api-surface.py              # every Revit member Heron calls, on 2020 THROUGH 2027
```

It was built while 2025–2027 were being skipped by the compile gate, to leave them with something rather
than nothing. **That skip is gone** — see below — so this is no longer their only cover, and it is still
worth running: it reads the **shipped** assemblies for all eight releases, where a compile reads the
NuGet reference packages for the one release it is building. It looks up the **compiled** add-in's
reference tables — exactly what the code calls. **All 103 exist on every release from 2020 to 2027.** It
matches by name, so a changed signature would still only show up in a real compile; it supplements the
compile gate and says so on every run. **It was validated before its clean result was believed**, by
putting the `CreationGUID` defect back and watching it fail.

**And the skip itself turned out to be one more environment-specific claim, found the same day.** The
newest three releases were reported SKIPPED off Windows as *needing the Windows Desktop SDK*. Those
targets are a property of **the installed SDK package**, not of the operating system: Ubuntu's
`dotnet-sdk-10.0` ships them and its `dotnet-sdk-8.0` does not, and `check-compile.py` was passing
`-p:EnableWindowsTargeting=true` — the flag whose whole purpose is the non-Windows case — **only on
Windows**. With the .NET 10 SDK installed, **all eight releases compile here**, add-in included, 0
warnings. [docs/30 §2a](30-compiling-away-from-windows.md) has the account and the two-directional
validation that was done before the eight greens were believed.

- **All documentation, decisions, specifications and open questions.**
- **The session binding**, in full. Its test fakes the world and exercises the real logic.
- **Reading and reasoning about the C#.** Just not running it.
- **Compiling it, on every supported release** — 2020 to 2027, since the .NET 10 SDK arrived on this
  side of the fence. Two things this repository "could not do here" have now turned out to be things
  nobody had attempted; assume the third is out there and go looking before writing the sentence.

### Cannot be done without Revit — and cannot be faked

- Anything that loads into `Revit.exe`: the ribbon, the icons, the dispatcher, the operations.
- `tools/setup.ps1` and `tools/deploy-addin.ps1` — they need Revit installed, and Windows.
- `test_bridge_roundtrip.py` needs **Windows named pipes**, so it will not run on a phone either.
- Every claim in [§3](#3-what-has-been-witnessed-at-a-real-revit) marked *proven against a real Revit*.

> **If you change C# from mobile, you cannot know it works — but you can now know it BUILDS, so build
> it.** `python tools/check-compile.py` is minutes, needs nothing installed but the .NET SDK, and it
> catches the entire "worked in 2020, broke in 2024" class. Compiling is not testing: say which one you
> did in the commit message, and add the rest to the checklist below. A commit that reads as though it
> were tested is worse than one that admits it was not.

---

## 6. The return-to-the-machine checklist

**The list lives in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md), not here.**

Every item is unproven, every item has an ID (`A1`, `D3`) so it can be named in a message without
being described again, and they are in dependency order — nothing in group D can be attempted before
group A passes.

**When a check behaves oddly rather than simply passing or failing, stop and read
[§4a](#4a-something-looks-wrong-mid-job--start-here) before deciding what it meant.** This is the pass where
most anomalies will surface, because it is the first time any of this meets a real model — and the
dangerous ones arrive looking like a pass.

It is kept as ONE register rather than a copy in each file, because two lists of the same thing drift
and this repository has been bitten by that more than once. Add to it every time something is built
away from Revit; delete an item only when it has actually passed.

**The single most important line in it is `D3`:** move the ducts 200 mm, then *measure one*. That is
what catches a unit error, and a unit error is the failure that looks fine until somebody measures it
months later.

The one-command path back to a working machine:

```powershell
powershell -ExecutionPolicy Bypass -File tools\setup.ps1
```

It checks Python first, detects installed Revit versions, refuses only the ones that are **open**, and
deploys per-user with no administrator rights. Then start Revit → **Heron AI** tab → **Heron** (click to
connect, again to disconnect), and:

```bash
python mcp/client/heron_bridge_client.py ping
python mcp/client/heron_bridge_client.py count
```

Anything wrong → `python mcp/client/heron_bridge_client.py doctor` prints everything needed to diagnose it.

**For unattended testing**, set `bridge.autoConnect = true` in `%APPDATA%\Heron\config\heron.config`
and the bridge starts without a button press. Default is `false` on purpose — a Revit that was never
connected being invisible is a safety property. **Put it back afterwards.**

---

## 7. The decisions you must not quietly undo

> ### ⛔ Heron is the only codebase you write to
>
> **The owner's instruction, 2026-08-28.** There are two other repositories on this machine — an existing
> Revit add-in and a knowledge package. Both are **reference only**:
>
> - **Never commit to them, never open a pull request on them, never update or upgrade them.** Every
>   change goes to Heron.
>
>   *One exception exists and is closed:* a commit and draft PR were made to the knowledge package on
>   2026-08-28 before this rule was given. They were withdrawn, and then the owner allowed that one
>   through — *"do it for AJ AI, this time only, next time no need"* — so
>   [AJ-AI-Brain#47](https://github.com/Ajmalpshaik/AJ-AI-Brain/pull/47) is open on purpose. **It is not a
>   precedent.** If you find yourself about to add a second one, the answer is no.
> - **Read them freely** — they solve overlapping problems and their scars are worth more than their
>   features. [PROPOSALS Part E](PROPOSALS.md) is what that study produced.
> - **Never copy code or text out of them.** Understand the mechanism, then write it for Heron, in
>   Heron's shape, with Heron's reasoning. His words: *"study and use and make it part of our heron,
>   blindly copy paste dont do it."*
> - **Do not cite them as Heron's authority.** He stops using both once Heron is finished, so a note
>   whose argument is *"go read that other repository"* becomes worthless on that day. State the
>   reasoning here, in full, so it stands on its own.

**Twenty-two of them are listed below.** That is a selection, not the total — [DECISIONS.md](DECISIONS.md) holds every decision and its own generated table at the top is the index. This sentence read *"Twenty-two are in docs/DECISIONS.md"* until 2026-09-12, which was true of the list and false of the file, and the file had reached **D-70** by then.

These are the load-bearing ones:

| | |
|---|---|
| **D-01** | Heron runs as a **Claude Code plugin** |
| **D-02** | **Named pipes**, per-PID. Local-only by construction — the add-in has *no network code at all* |
| **D-05** | **Revit 2020 → latest.** Never extrapolate the runtime table forward; an unlisted release is a build error |
| **D-06** | **C# for Revit, Python for everything outside it.** Settled again on evidence in [Q-39](OPEN-QUESTIONS.md) |
| **D-09** | One `ExternalEvent`, one queue, one handler |
| **D-15** | **Where the field notes disagree with a specification, the field notes win.** Observed beats designed |
| **D-17** | Runtime state is machine-local; user data roams; the audit log stays with the data because it is evidence |
| **D-18** | The Transaction Agent belongs to **Step 6**, not Step 2 — a write path built before its rails ships without them |
| **D-19** | **Writing is off by default** until the write path has met a real Revit. Read-only stopped being structural the moment Step 6 existed; this is what replaced it |
| **D-20** | Millimetres to feet is **arithmetic, not `UnitUtils`** — exact, and nothing for Autodesk to move under it across 2020–2027 |
| **D-21** | Failure analysis is a **table, not a model call** — Heron's failures are its own bounded set of codes |
| **D-22** | A second chat is **refused, not allowed to take over**. *"Newest connection wins"* now describes the pipe only |
| **D-25** | The existing libraries are **studied and re-authored, never imported.** Ajmal's. Nothing is copied; a capability is written from scratch here and starts unproven, whatever status it held where it was read |
| **D-26** | **The model file is never uploaded** — the `.rvt`/`.rfa`, not the information. Refined three times in one day, each looser; read the final rule, not the earlier framings that same-day commits still quote |
| **D-27** | **There are no personas.** One voice; the answer's *shape* follows the request's shape |
| **D-28** | Generated code is **Roslyn C#, in process.** No Python runtime, and **no HTTP server enters the add-in** — the add-in having zero network code is a structural guarantee, not a setting |
| **D-30** | A fragment is promoted by **one proof containing a negative case**, never by a count of successful runs |
| **D-32** | **v1 must be able to change the model.** Reading is what gets used first, but a Heron that cannot change anything is a report tool, not the product |
| **D-33** | **Never assume an input — ask, and ask once.** No confidence threshold: a number invented today is tuned tomorrow, and the first tune to reduce interruptions starts it guessing |
| **D-35** | An unapproved shared fragment is **refused, not warned about**. A warning hands the decision to whoever is least able to judge it |
| **D-39** | Shadow mode is approved on an **analysed disagreement**, never a count of agreements. Agreement is weak evidence; a thing that does nothing agrees with everything |
| **D-43** | **The Constitution is binding** — all 30 Articles. Its own Amendment clause applies: never weakened silently, and never by an agent |

**Golden Rules** — **21, all official** — are in [docs/14](14-golden-rules.md). 16–21 cover undo,
preview-before-modify, sandboxing, permission escalation, document pinning and stale reads, and were
**accepted on 2026-08-28** ([Q-19](OPEN-QUESTIONS.md)) *while Step 6 remained unproven*. That order
was deliberate: a rule that only binds once the code passes is not a rule the code was ever held to.
**They are exactly what Step 6 builds.**

---

## 8. What is waiting on the owner

> **The list is [`FOR-THE-OWNER.md`](FOR-THE-OWNER.md) and `python tools/owner-queue.py`.** It is
> derived from [OPEN-QUESTIONS](OPEN-QUESTIONS.md), [NEEDS-CHECKING](NEEDS-CHECKING.md) and
> [PROPOSALS](PROPOSALS.md) at the moment you ask, and grouped by what you need in front of you —
> a decision, a numbered document, Revit open, the PC, or a network.

**This section used to carry the list, and on 2026-09-12 it opened with *"Every question is answered —
41 of 41 — and nothing blocks any phase."*** It said **53 questions, 52 answered and three open**,
and two of those three had been open for two days. Anyone reading this section would have concluded
nothing was waiting on them.

It was not wrong when written. It was written once, in a phase where 41 was the total, and never
re-derived — the same failure as §1–§3, in the section whose entire job is telling the owner what he
owes. **That is why it is a pointer now and not a table.**

### What survives from it, because no register carries it

Three items below are **history**, not queue. They record owner decisions whose reasoning would be lost
if the section were simply deleted — which is the test `work-notes/README` sets before removing
anything.

| | |
|---|---|
| **`R1b` — show him the trust model working** | [D-14](DECISIONS.md) stays **Proposed**. He agreed the direction and said *"show me it working at the PC first."* Use the framing that landed: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may **not** be called settled. Still open — it is `R1b` in the register, and the queue prints it |
| ~~`R1`, `R2` — read the decisions back~~ | **DONE 2026-08-29.** All twenty-one read back and confirmed. It happened in conversation rather than at the PC, and **after** Phase 2 was built rather than before — both recorded rather than smoothed over. What that cost was **nothing measurable**: the two reversed within hours did not move a fourth time |
| ~~Copyright~~ | **CONFIRMED 2026-08-28 — Ajmal PS is correct.** Consistent in all four places: the Apache appendix in `LICENSE`, `NOTICE`, `<Company>` in `Directory.Build.props`, and `README.md`. The appendix is filled in rather than left as the `[name of copyright owner]` placeholder, which is the one usually missed |

**And one question that came out of building, still open and owned by nobody's register:** whether a
layout that Revit refuses part of should place the rest or roll back entirely. [Golden Rule
16](14-golden-rules.md) says roll back; a modeller may well want the 37 that worked. See
`place-family-instances`.

**Two things were answered by NOT answering them**, and both are publication tasks rather than gaps:
[Q-38](OPEN-QUESTIONS.md) — the public install command — and the Autodesk App Store requirements in
[D-38](DECISIONS.md). Both need **current documentation read at the time**, and writing either from
memory is the failure this repository has already had twice. `tools\setup.ps1` is the proven route
meanwhile.

---

## 9. Next — read the decisions back, then prove what is built

> **This section is what to do WHEN REVIT IS OPEN.** If it is not, the live work is
> [§9a](#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start) — the fragment
> library, which needs no Revit and no Windows.


**Step 6 is built. Phase 2 is built. Do not build either again.** All seven items the build order asks
of Step 6 are in the repository, and so is everything an audit turned up afterwards: the lease, the
Failure Analysis Agent, the tool registry, the configuration and health agents. Steps 7 to 14 are all on
disk with a test each. 59 agent ids are claimed by code and every one exists in the registry.

**`python tools/check-gaps.py` is what says this, rather than this paragraph.** Run it first. It now
reports **nothing** unfinished — the brain was reachable from nothing on the morning of 2026-08-29 and
was wired up the same day — and everything else it lists is **waiting on a machine or on a
conversation** —
which is not the same as being finished, and the tool is careful to say so in its own closing line:
*"waiting is not failing — but a waiting item is still UNPROVEN, and no number of days spent waiting
makes `D3` any more true."*

**`R1` is no longer the first thing at the PC — it is done** (2026-08-29, all twenty-one confirmed).
What remains of the review is `R1b`: show him the trust model
working, because [D-14](DECISIONS.md) is still *Proposed*. His instruction — *"now we just recorded,
but we will do it one more time"*.

**It was meant to happen before Phase 2 was built, and it did not.** The owner overrode it — his to do —
and Phase 2 was built on those decisions instead. So the read-back is now a review of code that already
exists, which is **weaker than it was meant to be**, because a decision reviewed after the code exists
gets defended rather than examined. That is a reason to do it early, not a reason to drop it.

**The evidence for that pass is two reversals on the day itself.** D-26 moved three times, each looser.
D-32 was recorded as *"v1 is read-only"* and reversed to *"it must change things too"* when the same
question was asked again an hour later. Neither was a mistake — each was a first answer sharpened once its
consequence became visible, which is exactly what a read-back is for.

**Then the register, and a compiler is no longer what is missing.** `A1`, `A2` and `A3` are done — see
[docs/30](30-compiling-away-from-windows.md) for how, in one command:

```bash
python tools/check-compile.py     # 2020-2027, all four projects, no Windows and no Revit needed
```

**It found the thing it was built to find, on its first run.** `RevitWrite.DocumentKey()` used
`Document.CreationGUID`, which **does not exist in Revit 2020** — it compiled clean on 2024 and failed on
2020. That property was named in the register as a *likely* problem spot, by reading; reading had already
passed it twice. The fix is not a `#if`: the Project Information element's `UniqueId` is created with the
document, survives save, rename and move, and exists on every release from 2020 to 2027.

**And running the bridge found a second one, in the test rather than the code.**
`test_bridge_roundtrip.py` asserted that nothing held the lease at a point where an earlier unknown-op
probe had already claimed it — the bridge was right and the check was false. It had been written from
reading, in a file that could not run on the machine it was written on. That file now runs on both
platforms, one shim, one set of assertions, and passes the whole lease end to end.
**Two different counts for this suite sat in this file until 2026-09-12** - "30 checks" here and
"32 checks" in section 5 - and neither could be settled from this container, which has no built
host. Both are gone rather than one being guessed at: the suite prints its own total when it runs.

**What no compiler will ever do is tell you a duct moved the right distance.** `D3` is still the line
that matters most in this repository.

### The two things left that need Windows but NOT Revit

```
A4   python tests/test_bridge_roundtrip.py    the WINDOWS named pipe itself
A6   python tools/check-compile.py 2025       that the new SDK probe reads Windows correctly
```

Everything in `A4` except the Windows pipe is already passing — its naming, its security descriptor and
the `CreateNewInstance` flag are what remain, and no amount of Linux gets at them.

**`A5` was here and is done.** All eight releases compile off Windows with the .NET 10 SDK; the row that
said otherwise had assumed an operating system where the answer was a package.

**`A6` is `A5`'s own bill.** Deciding what to skip is now done by looking for the WindowsDesktop targets
under each installed SDK rather than by asking what operating system this is — and that probe has only
ever run on Linux. If it misreads Windows it would skip the three releases **on the one machine where
they already built**, so it is worth thirty seconds at the PC. Writing a check for a machine you cannot
run it on is how this file gets its rows.

### Then the rest of the register, in order

47 items need a real Revit. They are in dependency order and each says what PASS actually looks like.
The three that matter most:

| | |
|---|---|
| **C3** | With `write.enabled` still false, a move must be **refused**, naming the setting. Prove the gate before testing the write, or a passing move proves nothing |
| **D3** | Move the ducts 200 mm, then **MEASURE ONE.** The single most important line in the register — a unit error is the failure that looks fine until somebody measures it months later |
| **D5** | **One** Ctrl+Z puts it all back, as a single undo entry. Two means Golden Rule 16 is broken |

### Record what you prove, as you prove it

`tests/golden/cases.py` is the permanent record — 17 cases, each with what to do and what PASS looks
like. When you prove one, set its `status` to `PROVEN`, the date, the Revit versions, and stamp its
`fingerprint` (run `python tests/test_golden.py --stamp` to see the value; it prints and writes nothing,
because a fingerprint is a claim that a person watched it work).

That is what makes the next regression findable. All seven Phase 0 proofs currently read **STALE** —
proven against a build that no longer exists — and that is the library doing its job, not a fault.

### Three things that are not in the register

**1. The add-in must be rebuilt and redeployed.** The bridge protocol is now **2** — a request carries a
chat id, and an add-in still on protocol 1 will refuse to talk. That refusal is correct behaviour, but if
an old DLL is still deployed it will look like Heron has stopped working.

**2. Seven capabilities are wanted and unprovided**, listed at the top of this file. `check-gaps` reports
them, the register does not, and that is on purpose: a register row is a **test somebody must run**, and
these are **code somebody must decide to write**. Do not add them to `NEEDS-CHECKING.md` — it would turn
a list of things waiting on a machine into a list of things waiting on a decision, and those are the two
categories this repository has spent two sessions learning to keep apart.

**3. The brain is not reachable from the host**, so Phase 2's third done-clause is open — and unlike
the rest of the register, **nothing external is stopping it.** One MCP tool that resolves a request
through the capability registry is what the clause asks for. It is in [§1](#1-where-the-project-stands)
with the evidence, and in [§8](#8-what-is-waiting-on-the-owner) as the choice it creates.

---

## 9a. Continuing the library build — the recipe, so another session can just start

**This is the work that needs no Revit, and it is where the project actually is.** §9 above is what to
do when Revit is open. If it is not, everything below can be done from anywhere, and a fresh session
should be able to start from this section alone without asking anything.

### Say this to start

> **"Read HANDOVER.md §9a in Heron-AI and carry on building fragments."**

### Where the work comes from, and how much is left

The source is the owner's earlier library at `AJ-AI-Brain/scripts/`. **It is REFERENCE ONLY — read it,
never edit it, never commit to it.** His instruction on how to use it, in his own words:

> *"Don't copy-paste from the old library. Check, study, and split if you want to split — or whatever
> you want to do, do it as per our project specification."*

| Source folder | Files | State |
|---|---|---|
| `actions/reporting/` | 43 | **started** — most of the high-value ones are done |
| `actions/sheets-views/` | 54 | **started** — the spine is done |
| `creators/` | 36 | started — the duct and pipe spine is done |
| `actions/structural-changes/` | 33 | started — slope, connect and duplicate-type are done |
| `actions/qa-checks/` | 30 | started — duplicate values and annotation overlap are done |
| `actions/color-graphics/` | 25 | barely started — 4 done |
| `actions/parameters-naming/` | 21 | started — 2 done |
| `actions/visibility/` | 17 | barely started — 3 done |
| `actions/move-copy-rotate/` | 13 | mostly done |
| `filters/` | 51 | a few done |
| `context/` | 12 | see [D-98](DECISIONS.md) — most are the host's job, not fragments |
| `recipes/` | 44 | **these become SKILLS, not fragments** — a recipe is a whole job |

**Check what already exists before writing anything**, because the names do not match one to one — this
library is re-authored, not ported:

```bash
ls brain/fragments/                                    # by folder name
grep -h '^capability:' brain/fragments/*/fragment.yaml # by capability
```

### The recipe, per fragment

Same eight steps every time. Steps 2 and 6 are the ones that get skipped and are the reason for the
rest.

1. **Read the source file end to end**, including its comment header. That header is where the earlier
   library recorded what it had already got wrong — it is the most valuable part of the file.
2. **Decide what changes, and be able to say why.** A re-authored fragment is not a translation. Split
   one source into two if it is doing two jobs; fold two into one if they share a mechanism; drop a
   feature that belongs to the host. **Write the decision into the fragment's `purpose`**, because the
   next person will otherwise assume the difference was an accident.
3. **Write three files** under `brain/fragments/<kebab-name>/`:
   - `fragment.yaml` — metadata, contract, `purpose`, `compatibility-note`, `utterances`
   - `impl/any/fragment.cs` — the C#, **non-standalone**: it assumes its `needs` are in scope and
     leaves its `provides` behind. No `using`, no class, no method wrapper.
   - `tests/cases.yaml` — `positive`, `negative`, `second_route`. **[D-30](DECISIONS.md): a proof
     without a negative case does not count**, because the defect being guarded against is the fragment
     that succeeds while doing nothing.
4. **Compile it on all eight releases.** This is the gate that has caught every real mistake so far:
   ```bash
   python tools/check-fragments-compile.py
   ```
5. **Check routing**, and read the ladder-crossing list at the top first:
   ```bash
   python tools/check-routing.py
   python tools/check-intrusion.py     # optional; the shortlist view
   ```
6. **Answer every ladder-crossing.** [D-101](DECISIONS.md) removes the option of leaving one alone:
   say which fragment should win, or say the sentence names a composition and belongs to a skill. Where
   two fragments fairly claim one sentence, **put the same cross-reference table in BOTH** — written
   one way it only routes whoever lands on the newer file.
7. **Run the sweep**, all of it, before committing:
   ```bash
   python tools/check-metadata.py && python tools/check-structure.py      && python tools/check-docs.py && python tools/check-gaps.py
   for t in tests/test_*.py; do python "$t" >/dev/null || echo "FAILED $t"; done
   ```
8. **Commit and push to the branch below.** Say in the message what was decided differently and why —
   the commit is where the reasoning survives.

### The rules that must not be quietly broken

| Rule | What it means here |
|---|---|
| **Golden Rule 16** | A fragment **assumes an open transaction and never opens one**. A batch is one undo entry. This is why a read fragment may not change-and-roll-back to measure something — see `MEASURE_CEILING_HEIGHT` |
| **[D-44](DECISIONS.md)** | A re-authored fragment starts `DRAFT` whatever its status was in the earlier library. Nothing here inherits proven |
| **[D-97](DECISIONS.md)** | Build it all out now, prove it against Revit later in one pass. Do **not** stop to half-prove something |
| **Units** | mm → internal feet by `/ 304.8`, plain arithmetic. **Never a units API** — that is what breaks at Revit 2021. Hand values on in feet; [D-20](DECISIONS.md) keeps the conversion at the edge |
| **ElementId** | Never read one as a number. Compare `ElementId` to `ElementId` — `IntegerValue` is gone by 2026 and the type went 64-bit at 2024 |
| **Namespaces** | The wrapper imports the base DB namespace only. `Room`, `Space`, `Ceiling` and friends are **not** available as types — tell them apart by **category** |
| **No outside sources** | Never name another person's repo, tool, product, website or name — anywhere. His instruction, 2026-08-20. A re-authored technique is written in our own words as this project's own knowledge |
| **Areas** | `ELE, SEL, VIEW, SHT, PAR, MEP, GEO, QA, DOC` — a fixed list. Take the next free number in the area |
| **Risk** | `READ, ANALYZE, SUGGEST, EXECUTE, MODIFY, PUBLISH, ADMIN`. An export is **PUBLISH**, higher than MODIFY |
| **Read-backs** | Every fragment that writes reports what **happened**, never what was asked for. That is the defect this whole project exists around |

### The branch

The branch name is given per session and is not fixed here — the twentieth used
`claude/heron-ai-fragments-6d5xtp`, and the 2026-09-04/05 batches used
`claude/heron-ai-fragments-wa93gf` (merged, PR #8). Push with
`git push -u origin <the branch you were given>`, and open a draft pull request against `main`.

**`git fetch origin main` at the START of the session, not at push time.** Peer sessions push to `main`
too. A session that only checks when it is ready to push finds every documentation edit it made is
against a stale count, and each one becomes a merge conflict.

**A branch whose pull request is already MERGED is finished and cannot carry follow-up work.** Start a
new branch from the latest `main` instead — `git fetch origin main && git checkout -B <new-branch>
origin/main`. Stacking new commits on merged history is the one thing not to do here.

**Heron-AI and AJ-Tools only.** `AJ-AI-Brain` is read-only reference — **read it, never edit it, never
commit to it.** As of 2026-09-06 it is on the owner's machine at `D:\Ajmal\AJ AI Brain`, so it can be
audited directly rather than guessed at. `scripts/` holds 398 `.cs` files. **The nine that were still
fragment-shaped work were built on 2026-09-06, as ten fragments** — see the section at the top of
this file.

**The refutation pass on the covered side is now DONE** (2026-09-06) and it found **five more**,
listed at the top of this file. 312 of the 317 "covered" claims held, all 4 "impossible" verdicts
held, and the arithmetic closes at 330 for the first time. **The five are not built.** That, and the
proving pass, is what is left of this library.

### Before the C# — one step this recipe did not have

**Step 4 is the compile gate and it is not optional; when it cannot run, say so and do the next best
thing.** On a container with no .NET SDK:

```bash
# nuget.org is reachable where the SDK download host is not
curl -o api.nupkg https://api.nuget.org/v3-flatcontainer/nice3point.revit.api.revitapi/<version>/<file>.nupkg
```

Unzip it, and read `ref/*/RevitAPI.dll`'s metadata with Python's `dnfile` — every type and member the
fragment calls, on 2020 and on 2027, plus argument counts where there are overloads. **Check base types
as well**: `Space` declares neither `Number` nor `Area`; both come from `SpatialElement`, and a check
that stops at the declared type reports a false absence. This catches a member a release does not ship
and NOTHING ELSE — argument types and syntax stay unchecked, and the batch still owes a real compile.

### When something behaves oddly while doing this

[§4a](#4a-something-looks-wrong-mid-job--start-here). Do not fix past it quietly — the compile gate and
the checkers have caught four real defects during this build, and each one was worth more than the
fragment being written at the time.

---

## 9b. Three sessions at once — the protocol that stops them colliding

**This has been tried, and it went wrong.** Commit `6cdb9f7` is the record: two sessions built fragments
in parallel, and the merge found **41 capabilities built twice** and **31 ids collided** —
`FRG-VIEW-014` was `find-schedules` on one branch and `set-view-crop` on the other. A session's work was
thrown away and a cleanup commit renumbered 36 fragments. Read that commit message before deciding this
section is over-cautious.

Both failures have one cause. [§9a](#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start)
says **"take the next free number in the area"**, and two sessions starting from the same `main` both
take the same next free number. That rule is correct for one session and broken for three.

### What actually collides, and what does not

A fragment batch changes almost nothing shared. Pull request #8 touched **73 files — 71 of them new
folders under `brain/fragments/`, and exactly two shared**: `HANDOVER.md` and `NEEDS-CHECKING.md`.

**That paragraph used to end "so the whole conflict surface is two files and the id space", and it was
wrong.** Two of the three things three sessions collide on are not files in this repository at all, and
neither is fixed by partitioning ids:

- **the working directory**, which a branch does not partition — see the next section, and
- **the retrieval store** at `%APPDATA%\Heron\knowledge`, which is ONE store per machine serving every
  checkout on it. Three sessions rebuilding it in turn means any session's routing numbers may have been
  computed over another session's library. `tools/check-routing.py` compared `store.count()` against the
  fragments on disk to decide the store was stale, and all three trees held exactly 226 — so the count
  matched, no rebuild fired, and the tool reported with full confidence on the wrong library. It printed
  *"claimed in a routing table and not reached: 14"* on one run and *"7 questions answered by something
  that writes"* on another. Both were artefacts; the second is the tool's own headline safety check.
  Fixed in `d1ee3ad` — it compares the ids now — but the general shape is worth remembering: **a global
  cache is shared state between sessions even when the repository is not.**

### A branch is not a workspace — give each session its own worktree

**`git checkout` is per WORKING TREE, not per chat.** Three sessions in one folder are three sessions on
one branch, whatever each was told. This was tried: all three started in `D:\Ajmal\Aj Programs\Heron Ai`,
each ran `git checkout -B <its branch>`, and each one silently moved the branch out from under the other
two. Session A wrote a whole eight-fragment batch onto `main` by accident and had to move it into a
worktree afterwards, and every session saw the others' half-finished files appear in its own `git status`
— which is also how a session can commit another session's work by running `git add -A`.

Set up three worktrees ONCE, from the main checkout, before any session starts:

```bash
git fetch origin main
git worktree add -b claude/fragments-views    ../Heron-A origin/main   # session A
git worktree add -b claude/fragments-elements ../Heron-B origin/main   # session B
git worktree add -b claude/fragments-data     ../Heron-C origin/main   # session C
```

Each session then works **only** in its own folder and never runs `git checkout` in the shared one. The
main checkout stays on `main` and is what everybody pulls into to see the merged result.

When a session's pull request has merged, its worktree is finished. Remove it with
`git worktree remove ../Heron-A` — never `rm -rf`, which leaves git's worktree metadata pointing at a
folder that no longer exists — then delete the branch locally and on GitHub.

### The partition

Each session owns **source folders** and **id areas** no other session may touch. Distinct sources are
what stop two sessions re-authoring the same capability; distinct areas are what stop the ids colliding,
and they let §9a's "next free number" rule stand exactly as written.

| Session | Worktree | Branch | Source folders under `AJ-AI-Brain/scripts/` | Id areas | Files |
|---|---|---|---|---|---|
| **A** — views and sheets | `../Heron-A` | `claude/fragments-views` | `actions/sheets-views/`, `actions/visibility/`, `actions/color-graphics/`, `actions/sheet-dates-revisions/` | `VIEW`, `SHT` | 102 |
| **B** — elements and geometry | `../Heron-B` | `claude/fragments-elements` | `creators/`, `actions/structural-changes/`, `actions/move-copy-rotate/`, `filters/` | `ELE`, `GEO`, `MEP`, `SEL` | 133 |
| **C** — data, QA and parameters | `../Heron-C` | `claude/fragments-data` | `actions/reporting/`, `actions/qa-checks/`, `actions/parameters-naming/`, `actions/selection/` | `DOC`, `QA`, `PAR` | 94 |

**A fragment belonging in another session's area is not built.** It is listed in the pull request and
left to the session that owns that area. Reaching across is how the areas stop being disjoint.

`filters/` is the widest gap in the library: 51 source files against **three** `SEL` fragments built.

### The seven rules

1. **Work in your own worktree, and never `git checkout` in the shared one.** A branch does not
   partition a folder; the section above is what happens when three sessions share one. Check you are in
   the right place before writing anything — `git rev-parse --show-toplevel` and `git branch
   --show-current` together, not either alone.
2. **`git fetch origin main` first**, and branch off the latest `main` — not at push time. The warning at
   the top of this file exists because a session found out at push time once.
3. **Stage your own paths by name. Never `git add -A`.** In a shared tree that commits whatever the other
   sessions have left lying around; even in your own worktree it is the habit that makes the first
   mistake expensive.
4. **Do not edit `HANDOVER.md` or `NEEDS-CHECKING.md`.** They are the only two shared files, so all three
   sessions would conflict on them. The batch summary goes in the **pull request description**, and is
   folded into this file once, after all three have merged.
5. **Never invent an agent id.** Use only ids already in the registry on `main`. Eight fragments in
   `6cdb9f7` carried agent ids the merged registry did not have — all of them one branch's own numbering.
6. **Compare by capability, not by folder name.** `find-sheets` and `list-sheets` are different folders
   for the same job, and a folder-name comparison adds a second `FIND_SHEETS` beside main's
   `LIST_SHEETS`. The check is `grep -h '^capability:' brain/fragments/*/fragment.yaml`.
7. **Merge one pull request at a time**, and have the other sessions rebase after each merge. Three
   merges landing together is the situation `6cdb9f7` had to clean up.

### When another session has already fixed the thing you were about to fix

It happens, because all three run the same checkers against the same `main`. **Take their commit rather
than writing your own version of the same change** — `git cherry-pick -x <their sha>` keeps it
byte-identical, so when both pull requests land git sees the same content on both sides and there is no
conflict at all. It worked: `check-surface-fit` was broken on `main`, Session C fixed it in `024f9e3`,
Session A cherry-picked that commit to get a green gate without waiting, and the later rebase reported
*"skipped previously applied commit"* and dropped it silently. Two independent fixes to one file would
have conflicted instead.

### Say this to start a session

> **"Read HANDOVER.md §9b in Heron-AI. You are SESSION A."** — and B and C the same, one per chat.

Set the three worktrees up first, per the section above; that is the one piece of preparation the
sentence assumes. After that the table is the whole briefing: that session's row gives it its sources,
its id areas, its branch and its folder, and the rules bind all three. Nothing else has to be pasted.

### What is not fragment work

`recipes/` (44 files) become **skills**, not fragments — a recipe is a whole job. `context/` (12) is
mostly the host's, per [D-98](DECISIONS.md). The real remaining pool is about **330** source files,
not 379.

---

## 10. How to work on this

- **The owner is a BIM modeller, not a developer.** Explain in BIM terms. He delegates technical choices
  and is right more often than not about product ones — the folder restructure, the metadata standard,
  the toggle button, *"prove it yourself"* and *"study it but take none of its names"* were all his.
- **Do not hand him commands to run when you can run them yourself.** He called that out, fairly.
- **Field evidence beats specification.** The sharpest findings in this repository came from running
  things, not from any document.
- **Ask what already exists before designing anything.** On 2026-08-28 it worked four times out of five:
  three questions were settled by reading a system already doing the job, and `Q-13`'s answer had been
  running inside Heron since Step 1 while the question sat open. Look first; decide second.
- **An environment-specific block belongs in a sentence that names the environment.** *"The C# cannot be
  compiled here"* was true of one container that could not reach one download server. Written without its
  environment it became a fact about the project, and three sessions inherited it.
- **Read a document aloud before asking anybody to accept it.** Ajmal asked for all 30 Constitution
  Articles to be read out rather than tapping yes. Reading them found **three stale statements inside**,
  including eight Articles citing a *"Proposed"* Golden Rule that had been official since that morning.
  None changed what an Article required; all would have been read as current by whoever implements it.
- **Explain in the user's own materials, not in the abstract.** The trust model was explained once as
  *"lifecycle and source axes"* and he said plainly he did not follow it. Explained as a Revit family
  having **a maker** and **an approval status** — two things nobody would put on one dropdown — he agreed
  at once. The second explanation is also a better argument, which is usually the way round it goes.
- **A count cannot express a nuance, and should not be taught to.** Q-34 is *agreed but not signed off*.
  The question counter reads it as answered; the prose beside it carries the rest. The moment a counter
  needs to understand nuance it stops being a fact about the rows.
- **Say what is untested.** "It builds" is not "it works". On mobile it can now genuinely be *built* —
  which is a real rung above where this project was, and still two below *proven*.
- **Reference material is studied, never copied.** His earlier repositories are read for their
  reasoning; none of their names, dependencies or branding come across.
- **Every number in the docs should be derived, not typed.** The tooling exists; use it.
- **A library too small to be wrong proves nothing.** The retrieval query prefix-matched every word, so
  `in*`, `me*` and `the*` outvoted the one word that carried meaning. At **two** fragments it looked
  correct; at **seven** it was plainly broken. Nothing about the code changed in between. Before
  believing a component that ranks, sorts or matches, ask how many items it has ever been given.
- **Write a threshold as arithmetic, not as prose.** The quality nudge was documented as *"it can never
  outrank a better match"* and was in fact **eight times larger than one rank of fusion** — it could have
  jumped a `PROVEN` fragment eight places. The docstring was confident and wrong; the check that caught
  it was one subtraction. Any sentence of the form *"small enough that…"* is a calculation somebody has
  not done yet.
- **Retire an assertion with its reasoning, or repair it — never quietly loosen it.** *"Show me every
  duct must rank the duct filter first"* held at two fragments and failed at seven. The honest reading
  was not that retrieval got worse: that sentence is filter-**then**-select, a composition, which is
  what a **skill** names. The assertion was asking the wrong layer. Deleting it without that paragraph
  would have looked identical and taught nobody anything.
- **"Studied, not copied" fails silently unless somebody checks.** [D-25](DECISIONS.md) was being
  obeyed in intent and broken in fact — a level lookup was verbatim with one variable renamed, under a
  commit message that said *"written fresh"*. It surfaced only because the owner asked outright. The
  answer was yes, and both pieces were rewritten. **Do the side-by-side yourself before the commit**,
  not when asked; the method is in [docs/31](31-studying-the-existing-libraries.md).
- **A scanner that scans itself finds itself.** `check-gaps.py` reported its own regex as two undeclared
  agents on its first run. Funny once; worth remembering as the general shape — a tool that reads the
  repository is part of the repository.
- Commits are authored **Ajmal PS**. The repo is **private**.

---

*Phase 0 is finished and proven. Step 6 and the whole of Phase 2 are finished and proven of nothing —
built carefully, obeying rules that are now binding, tested where testing was possible, and compiled on
every release from 2020 to 2027. **Every fragment and all ten skills** sit at `DRAFT`, which is not a shortcut
but [D-30](DECISIONS.md) being obeyed: promotion needs one proof containing a negative case, and a
negative case needs a model. `python tools/check-gaps.py` is now the file that answers "what is left",
because it is computed from disk and this one is not — and where they disagree, believe the tool. It
currently says **nothing** here is unfinished and **55 are waiting: 49 on a Revit — `A9`, the uncompiled
batch, among them — plus the unproven fragments as one further item, 3 on Windows, 1 on a network that
can reach the weights host, and 1 on the owner.** Re-derive that from the tool rather than editing the
digits — a total that disagrees with its own breakdown is the cheapest drift there is to catch, and it
has survived here before.
Almost nothing in this repository is waiting on another session; it is waiting on a machine — and **four
times now**, something believed to be waiting on a machine was waiting on somebody trying it. The
newest was `A8`, filed under *needs Windows* when what it needed was `pip install mcp` — and trying it
found that Heron's MCP server would not have started at all on a machine installing today. `D3` is
still the line that matters most: move the ducts 200 mm, then measure one.*

---

## 10a. The LINUX BALANCE track — what a session with no Revit does

**This section exists so the owner does not have to paste a long prompt every time.** Saying *"Read the
LINUX BALANCE track entry in HANDOVER.md and carry on"* is the whole instruction. Everything below was
the standing agreement across the sessions of 2026-09-20 and 2026-09-21, written down rather than
retyped.

### The standing agreement

> Do only work that can be done **without Windows, without Revit, and without fragment proving.** The
> owner is a BIM modeller, not a programmer — **decide what to do next and get on with it.** Don't
> block. If something genuinely needs his PC or his decision, write it where it belongs, say so, and
> move to the next thing. When a chunk gets big: commit, push, open a **draft** PR, drive CI to green,
> merge, carry on.

**Other sessions work on this repository at the same time.** Fetch before you start and before you
push, and never touch a branch that is not yours. [§9b](#9b-three-sessions-at-once--the-protocol-that-stops-them-colliding)
is the protocol.

### Where the balance is — derive all three, they move within hours

| | command |
|---|---|
| **The reading sweep** — the main Linux work left, and where nearly every real finding has come from | `python tools/review-ledger.py` |
| **Open defects**, including the ones that need the owner rather than a Revit | `python tools/open-defects.py` — **read the ids, not the count** |
| **What is genuinely unfinished** versus merely waiting | `python tools/check-gaps.py` — and read `code=$?` on its own line |

The ledger also reports **STALE** — files read once and edited since. **Clear those first: a stale mark
is a lie about what has been read.**

### How to choose what to read next — by MEASUREMENT, not by folder order

For every module transitively reachable from `mcp/`, count the **distinct suites that reach it** against
its **public surface**, and take the thinnest-held. Reading `brain/` alphabetically finds what happens
to be first; this finds what is least held. Rows [5b-101](FRAGMENT-ISSUES.md) and
[5b-83](FRAGMENT-ISSUES.md) were both found that way, and the sessions that read in folder order found
less per hour.

### The method rules live elsewhere — read them there, they are not repeated here

| rule | where it lives |
|---|---|
| **A fix is not proved until its test has been seen to FAIL** — and it must FAIL, not CRASH | [`heron-ship` §2a](../.claude/skills/heron-ship/SKILL.md) |
| **Trace, don't grep** — a call site is not an execution; and point the tracer at the suites that REACH a module, not only those that name it | the two 2026-09-21 entries in this file, and [row 5b-101](FRAGMENT-ISSUES.md) |
| **Shape is not behaviour** — a scan over source text was wrong 28 times out of 28 on one sweep | [rows 5b-86, 5b-92, 5b-95](FRAGMENT-ISSUES.md) |
| What to run before pushing, and which failures are the machine | [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) |

**A negative result is a result.** Read a module and find it sound? Write that in this file, the way
`heron_retrieve` and the finished trace sweep are written, so nobody reads it again. **Never manufacture
a finding out of a shape to have something to show** — [row 5b-95](FRAGMENT-ISSUES.md) is what that
costs.

### One container trap that cost a session real time

**Never wait on a background job with `until ! pgrep -f NAME`.** The waiter's own command line contains
`NAME`, so `pgrep -f` matches the waiter itself and the loop never exits — four of them were still
spinning at the end of 2026-09-21, long after the jobs they watched had finished. Poll the output file
for its exit line instead.

---

# 11. Handover — 2026-09-12

**Written to a rule the owner set on this date, and the rule is the first thing to carry forward:**

> **Do not keep a diary of the work.** Not the daily activity, and **not the issues that were fixed** —
> the code and the commit already say those. **Keep only three things:** something NEW that was made, a
> MISTAKE worth not repeating, and anything still **TO DO**.
>
> His reason, and he is right: *"if we keep everything by note that will be big."* This file is 440 KB
> and proves the point. **Sections 1 to 10 above are the old habit. This section is the new one.**
