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

If a session ever tells you something that disagrees with `python tools/check-gaps.py`, **believe the
tool.** It is computed from disk every time; this file is typed by hand.

**If something looks wrong while you are working — a number that feels off, a job that says it did
something you cannot see, anything odd — go to [§4a](#4a-something-looks-wrong-mid-job--start-here).** It is
indexed by what you actually see rather than by what the cause turns out to be, and it says what to do
when the thing you hit is on no list at all.

---

## WHERE THIS STANDS RIGHT NOW — read this, then §9a or §9

**360 fragments. 197 `PROVEN`. 163 below — past half, as of 2026-09-11.** D-28's executor is built, fragments run against a real
model, and since 2026-09-09 they can also CHANGE one. That is the thing every earlier handover was
waiting for.
**Proving is live and these two numbers move hourly — run
`grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c` rather than trusting the
line above.**
**On 2026-09-07 every recorded proof was re-run and all thirteen held, and the executor was put in
front of all 135 DRAFT READ fragments** — see [the verification pass](#2026-09-07--the-verification-pass-13-proofs-re-run-135-fragments-through-the-real-compiler).

| | |
|---|---|
| Fragments | **360** — every fragment-shaped job in the earlier library, five cross-project transfers from PART 5, `CREATE_GLOBAL_PARAMETER` (2026-09-07), `FIND_DATES_IN_VIEWS`, built AND proved 2026-09-08, and **ten added 2026-09-08 for the review's N01–N09 plus the read they depend on** — see the entry below. All ten are `DRAFT` and NONE has met a model |
| Proven | **197 of 360** as of 2026-09-11 (was 198; `export-views-to-fbx` was put BACK to `DRAFT` by the owner on 2026-09-11 — it is `risk: PUBLISH` and was proved through `validate`, which does not apply the risk gate `fragment` and `prove` both apply. Its proof block was kept; only the status claim was withdrawn. Before that 196; PR #108 promoted `import-parameter-values` and `export-views-to-fbx` — the first found by WRITING the CSV it reads rather than waiting for the model to hold one. Before that 188; PR #104 promoted eight found by SWEEPING fifty-one fragments in two bulk passes instead of probing one at a time — including `sum-by-group`, which with `group-and-count` completes the pair the `keep-chain` fix was built for. Before that 186; PR #99 promoted `flip-elements` and `dimension-wall-openings`, both blocked on Snowdon because its architecture is in a link and both proved on `Project1`'s hand-drawn walls and door — the third and fourth times a triage row named the model that would work. Before that 185; PR #96 promoted `set-mep-slope`, proved on `Project1` after Snowdon refused all 22 of its ducts — the second time a triage row named the model that would work and was right. Before that 180; PR #92 promoted five proved on `Snowdon-scratch` — four re-runs whose earlier records were same-day and therefore stale, plus `check-flow-direction`, which `Project1` could not feed because its ducts carry no system. Before that 178; PR #88 promoted the last two proved on `Project1`. Before that 175; PR #86 promoted three proved on the same model, one built by hand for the purpose — including `find-dead-ends`, which §3b had set aside three times on Snowdon and which needed only a duct run with a loose end. Before that, 167; PR #79 promoted eight — the first WRITE fragments this repository has promoted) — 142 at the start of the day's second proving track, **+18 from it**, and `set-view-section-box` back to `DRAFT` when its implementation changed after its proof was signed. Each carries a recorded proof with a negative case and a staleness fingerprint (D-30). Moving hourly — derive it, do not read it here |
| Compile gate | green, Revit 2020–2027 |
| Other gates | metadata, docs, gaps, agent-count, **structure** — all green. **`check-licence` added 2026-09-09 and it EXITS 1 on a finding**, unlike the other reports; 370 units, all clean today ([D-66](DECISIONS.md)). `check-revit-gate` and `check-reachable` are reports and exit 0, so their findings are questions and two of them are now worklists. The `structure` red at `83fd7e8` was `read-space-loads` naming a vendor namespace in `brain/`; **fixed 2026-09-08**, and note the checker greps the file text, so a COMMENT mentioning it fails too |
| Tests | **41 suites when this was measured, on 2026-09-10, of which 36 passed in a plain Linux container — derive the count now with `ls tests/test_*.py | wc -l`, because it moves whenever anybody adds a file. Three failures are the MACHINE, with two causes not one** — `test_mcp_serves` and `test_served_claims` need the MCP SDK, `test_bridge_roundtrip` needs a built .NET test host. On a machine with both, those three should pass. **`test_graph` and `test_reachable` were the two that were NOT the machine, and both were fixed on 2026-09-12** — each carried a fixture describing a repository that had moved on, and neither test's claim changed (see the PART record below). **Do not fix a test by editing it until it passes**; `test_embed` and `test_retrieve` were re-based against the model backend in PART 5, not edited until green. **The count was also Linux-specific and nobody knew**: `test_context` failed on the owner's Windows checkout on one check, comparing a path against a hardcoded `/` — so Windows saw one fewer pass than every document here promised. Fixed 2026-09-12 at the comparison; **unproved on Windows**, which is what [NEEDS-CHECKING](NEEDS-CHECKING.md) **A14** is for. Derive the number, never read it here |
| Register | **71 rows, 19 closed, 52 left** — PART 6 added Group J, the eight that would prove the executor's inputs. Group A is FINISHED. **Only `R1b` does not need Revit** |
| Add-in | **THAT CLAIM WAS WRONG AND IS CORRECTED. Rebuilt and redeployed 2026-09-10, to Revit 2020, 2024 AND 2027**, verified at binary level. The binary Revit had loaded was dated 2026-09-08 23:06 while `RevitFragment.cs` was written 2026-09-09 23:14 - so D-67's caller-value widening and the rollback check had NEVER reached the machine ([FRAGMENT-ISSUES](FRAGMENT-ISSUES.md) rows 8 and 12). Deploy ONE release at a time. Rebuild it after ANY change under `revit/` — and check the framework first: `check-compile.py` builds 2020–2027 into one folder and the newest wins, so a run of it leaves .NET 10 binaries that Revit 2024 refuses with *"Revit cannot run the external application"*. `deploy-addin.ps1` now guards this rather than trusting the operator |
| Agents | **71 of 250 have code**, 4 host-provided by D-01, 175 left — `python tools/agent-count.py`. The 71st is `HERON-RAG-CTX-007`, the Context Manager, on 2026-09-09. Phase 0/1's agent list is COMPLETE |
| MCP tools | **14** — `heron_gaps`, `heron_compatibility` and `heron_diagnose` added 2026-09-07/08; **`heron_context` added 2026-09-09** (the Context Manager, [32 §4.1](32-master-architecture-reconciliation.md)). Derive it: `grep -c '^@server.tool()' mcp/server/heron_mcp_server.py` |
| Open questions | **52 answered, 1 open, nothing gating any phase.** `Q-51` — what guards retrieval-into-context on the day Heron indexes text it did not write — stays open **on purpose**, with [`tests/test_carried_sources.py`](../tests/test_carried_sources.py) watching for the day it becomes real. Derived by `python tools/check-docs.py`, never read from a sentence |
| Tools | **22** in `tools/`, and **14** MCP tools. Derive both rather than trusting a line |
| Bindable inputs | **CLOSED 2026-09-09.** PART 6 bound what the selection and the previous fragment could give; the caller's half — a category, a name, a distance — arrives as text now and is resolved inside Revit (D-54). It was the largest unlock left: **287 of 360 fragments** declare such a need, 675 needs between them. **Widened again 2026-09-09**: an element TYPE by name, nine narrower classes (`WallType`, `Phase`, `FilterElement` and the rest), and **a point in millimetres** ([D-67](DECISIONS.md)) — which took the arrangeable library from 6 to 40. What is still refused, most-wanted first: `ElementId`, `Element` as a specific instance, `OverrideGraphicSettings`. Derive it with `python tools/generate-jobs.py` |
| Branches | **`main` only, and it is the only branch that exists.** **Sixteen PRs were merged on 2026-09-09** (#44–#61) and every branch behind them is deleted — the role-declaration stack, the silence-illegal fixes, the job generator, and the proving track. **Start from `main`**; nothing is parked outside it. The sha is not written here - `git log --oneline -1 origin/main` - because it moved twice while this row was being read |

**QUEUED FOR THE PC, AND THE OWNER HAS SEEN THE LIST.** **Five items**, all of them needing Revit
or `dotnet`, all of them CHECKED by a gate that runs without either: **62 link contracts**
([D-59](DECISIONS.md)), **59 dropped-counts** ([D-64](DECISIONS.md)), the **preview selection** in
`RevitWrite.cs` ([D-60](DECISIONS.md)), the **workflow id across the seam**
([D-61](DECISIONS.md)/[D-62](DECISIONS.md)) which is small and unlocks three things at once, and
the **project knowledge scope**, which the Codex review added. The lists and the order are in
[the open-questions entry](#handover--2026-09-09-the-open-questions-track-44--52-answered-and-the-rest-needs-the-pc).

**Nothing on that list is blocked on a decision.** Every one is a change somebody can sit down
and make, against a rule that is already written and already checked. That is the difference
between this list and every earlier one in this file.

**EVERY REGISTER ROW THAT DID NOT NEED REVIT IS NOW CLOSED.** `A4`, `A6`, `A7`, `A8` and `A9` all fell on
2026-09-06 on the owner's own PC — rows parked for months on *"a machine"*, closed in one afternoon by
somebody sitting at one. Nothing is waiting on a compiler, a network, a Windows box or an MCP host any
more. **44 of the 45 remaining rows need Revit open with a real model**, and the forty-fifth needs a
conversation.

**REVIT BEING OPEN IS NOT ENOUGH, AND THIS FILE USED TO IMPLY IT WAS.** Every earlier entry said the
proving pass "needs the PC". True and nowhere near sufficient: until 2026-09-06 **nothing could execute
a fragment at all**, and the server said so in its own code. The PC was never the blocker. The executor
was. It is built now, so the sentence is finally true.

### What 2026-09-06 did, in order — the four parts are BELOW THIS, and not in this order

The sections that follow are the record, and the file lists them **PART 3, PART 4, PART 2, PART 1**
because each was written on top of the last. Read them by their part number, not by where they sit.

| Part | What it was | Result |
|---|---|---|
| **1** | Verified the nine "missing" claims and built them | 329 → 339, as TEN fragments — one was two jobs |
| **2** | Refutation pass on the 317 "already covered" claims, which nobody had ever tested | five more found; the arithmetic closed at 330 for the first time |
| **3** | Built four of those five | 339 → 343. **The fifth was my own wrong claim** — a schedule calculated column is impossible on every release, and the compile gate killed it in one run |
| **4** | Built D-28's executor and ran the first proving pass | **13 `PROVEN`** |
| **5** | A SECOND TRACK, in parallel and in another worktree: the decision read-back, five register rows closed on the PC, and D-47 part B | 343 → 348, and **Group A finished** |

### PART 5 in one screen — it is NOT in the sections below, it is here

**It ran in a different worktree at the same time as PARTS 1-4**, which is why the two do not interleave.
Three things came out of it and each is recorded where it belongs rather than only here.

**1. The oldest twenty-six decisions were read back, and had never been.** `R1` covered `D-23` to `D-43`
on 2026-08-29; `D-00` to `D-22` and `D-44` to `D-46` had never been put to the owner at all — the
earliest answers, given before any code existed. All twenty-six confirmed, none reversed. **It found five
real defects**, which is the only reason it was worth doing:

| Found | |
|---|---|
| **One malformed `fragment.yaml` took down all 343 fragments** | `load()` promised `ValueError`; `yaml.YAMLError` is not one. Measured, not read — one good, one broken, one good returned *nothing*. **The owner named this shape before the code was looked at** ([D-48](DECISIONS.md)) |
| **The same bug in the SKILL loader, by a different route** | A *directory* named `x.yaml` reached `io.open` because the loader filtered on the extension and never asked whether the entry was a file. Every skill lost |
| **`D-04` still said the scripting runtime was open** | `D-28` closed it ten days earlier. A sub-decision closed in a NEW entry leaves the old entry's text saying otherwise |
| **Part 2 required a stop control that no longer existed** | `D-46` removed the button. Resolved by the owner in the same conversation: the Heron button calls `bridge.Stop()`, which is **stronger** than the flag it replaced |
| **Rule 16 promised an undo Revit cannot give** | Fixed — see below |

**2. Five register rows closed, and one of them broke another.** `A4`, `A6`, `A7`, `A8`, `A9`. **`A8`
failed first and that is why it was worth running**: `heron_capabilities` never replied, and a real Claude
Code tool call sat on it for **thirty minutes**. The stack — taken with `faulthandler`, not reasoned about
— was `import model2vec → import numpy → loading numpy's native extension`, **on the asyncio event
loop**. An import costing 1.0 s in a fresh process, still running at 40 s there.

> **CLOSING `A7` IS WHAT BROKE `A8`.** Until `model2vec` was installed that import failed instantly and
> Heron degraded to `lexical`, so the handler always answered. **Two register rows, each correct alone,
> and the failure lived only in their combination.** This file's register is deliberately a list of
> independent rows worked down in order, and **nothing in it can express *these two are fine apart and
> broken together***. No numbering fixes that; only re-running earlier rows after a later one changes the
> machine. First time it has bitten. [D-49](DECISIONS.md), and
> [`tests/test_mcp_stdio.py`](../tests/test_mcp_stdio.py) is the check made permanent — a real subprocess
> over real stdio, with a **deadline** on every reply, because a test that waits forever cannot tell a
> slow answer from no answer.

**3. D-47 part B — the cross-project transfers a modeller actually asks for.** Five built, one existing
one completed, all `DRAFT` and none has met a model:

| Transfer | The quiet failure it guards |
|---|---|
| **View filters** `FRG-VIEW-097` | arrives **matching nothing** — a rule points at a parameter this project lacks |
| **Line styles** `FRG-ELE-052` | arrives **drawing solid** — the pattern is a separate element |
| **Project parameters** `FRG-PAR-020` | a non-shared one **cannot be carried at all**, and is named rather than dropped |
| **Materials** `FRG-ELE-053` | arrives **rendering flat** — no appearance asset, and it shades correctly, so it looks right everywhere except a rendered view |
| **Object styles** `FRG-ELE-054` | the one that **OVERWRITES** — so it reports the diff, not the inventory |
| **View templates** *(existing fragment, completed)* | arrives **controlling less** — its filters are separate elements and were never counted |

**Every one of those failures is invisible in the list that names it.** The filter is there. The style is
there. The material is there. You find out on an issued drawing, or in a realistic view, or when a
category quietly loses a parameter and every value in it. **Not one of them reports those as success** —
they go to `weakened` or `skipped`, named, with the reason.

**They compose, in an order, and say so at the point of failure:** materials before object styles; view
filters before view templates.

### The thing PART 5 kept re-learning, three times in one day

**A test can encode a truth about a system that then moves under it, and the failure looks identical to a
regression.** It happened three times on 2026-09-06 and the third one was mine:

| | What moved | What the test said |
|---|---|---|
| `test_retrieve` | `A7` switched the backend to a trained model | *"the top 5 are effectively TIED"* — they stopped being tied, which is the improvement |
| `test_embed` | same | *"put them on the screen → the selection fragment"* — the model reads *"put ON"* as PLACING, which is a fair reading |
| `test_mcp_stdio` | **D-28's executor landed in the other worktree hours later** | *"it still says it CANNOT run them"* — Heron can now run a read-only fragment, so my own assertion was stale before it was a day old |

**The rule that came out of it, and it is now applied in all three:** re-base the assertion on what is
true, keep the old behaviour asserted where the old conditions still hold, and **write down why it
moved** — never edit it until green. Two of the three carried their own prediction (*"A7 is what should
break this check"*); the third did not, and was caught only because the suite was re-run after merging
`main` rather than assumed still green.

**Re-run the suite after merging, not before.** That is the cheap habit this cost nothing to learn and
would have cost a green-looking branch otherwise.

**`D-47` itself was corrected in the doing.** It claimed part B was *"mostly written"* and cited three
fragments; not one of them crosses two projects, and opening them was all it took to find out. It also
asked for a *"second binding"* that is not how this works — the destination is the bound document and the
source is found by title among the open ones, which is the pattern
`TRANSFER_VIEWS_BETWEEN_DOCUMENTS` already established.

**And Rule 16 now says what Revit can deliver.** It promised *one user action, one undo* without
qualification, and `D-47` had just allowed a job to cross projects. Every `Transaction` and
`TransactionGroup` constructor takes exactly one `Document` — read by reflection out of the shipped 2020
and 2024 assemblies. So the rule reads **per document**, and a cross-model job must say so **before** it
starts. What was *not* measured is stated too: 2027's assembly is .NET 10 and could not be
reflection-loaded, so the exhaustive absence of a two-document overload is proven on two releases, not
eight.

---

Not in the parts below, because they were fixes rather than batches: the phantom-session bug
(`revit_health` reported six connected Revits when zero existed), the target-document input, and the
served-claims fix (three MCP tools were telling every caller that nothing runs and everything is DRAFT,
hours after both stopped being true).

### What a fresh session should do next, in order

1. **Finish the four half-proved fragments.** `READ_SELECTION`, `REPORT_PHASES` and
   `REPORT_DESIGN_OPTIONS` are **done (2026-09-07)** — the owner made the last two conditions himself
   while the session ran. Each of the rest needs ONE specific thing that does not exist in any open
   model, and none can be faked. **Making one is a minute in Revit and the model need not even be saved
   — the fragment reads the open document, not the file.** This is the cheapest real progress available:

   | Make this, in the model already open | Finishes |
   |---|---|
   | **one global parameter** — Manage ▸ Global Parameters ▸ New | `REPORT_GLOBAL_PARAMETERS` — `globalCount` 0 → 1. **`CREATE_GLOBAL_PARAMETER` exists now and CANNOT do this for you** — it writes, and no write path runs a fragment yet |
   | a group, placed, then **delete the placed instance** and keep the definition | `FIND_UNUSED_GROUP_TYPES` — `unusedGroupTypes` 0 → 1 |
   | a face **painted** in a material used nowhere else | `FIND_UNUSED_MATERIALS` — **the paint-only case, which must NOT be reported unused.** Snowdon's count must go 56 → 55, not 56 → 56 |
   | a workshared model with a **CLOSED workset** | strengthens `LIST_WORKSETS` — still the one case never seen |
   | Revit's own **Purge Unused** list, read by eye | `FIND_UNUSED_FAMILIES` — it has both halves already and needs only a second route |

   **Each row above is a PREDICTION, not a hope.** The number it names is what must change; if it does
   not, that is the finding.

2. ~~**Extend the executor to fragments that take inputs.**~~ **HALF DONE, 2026-09-07 — see PART 6.**
   The host now binds every need a fragment's contract declares, from the same contract the compile gate
   reads. **What it can fill: the on-screen selection, and what the previous fragment in the batch left
   behind** — D-29's filter-feeding-action, running. That takes the fragments whose inputs the host can
   supply from **20 to 70**. **What is still missing is the caller's half**: a category, a name to match,
   a distance. **278 fragments want one of those and there is no route for them**, which is now the
   largest single unlock left, and it is bigger than this one was. **PART 6 has now met a model** — six of Group J's
   eight passed on 2026-09-07, `J2` included. `J7` and `J8` are still open.

   **The other track had already removed the first risk from this job, and the two agree.** All 135
   DRAFT READ fragments were put through the real executor the same day, and **every name any of them
   failed on is a name its own contract declares** — which is exactly the assumption PART 6 binds
   against. There was no hidden contract mismatch to find first, and there was none.

3. **The write path.** `run_fragment_read` opens no transaction on purpose, so Revit itself refuses any
   change. Running a fragment that WRITES is a separate operation that does not exist, and it belongs
   beside `move_elements` at `Modify` with a preview the user accepted.

### How to run a fragment today

```bash
# one lease, many fragments - see the trap below
HERON_CLIENT_ID=my-session python mcp/client/heron_bridge_client.py prove list-levels list-grids

# aim at a model that is open but NOT the one on screen
HERON_CLIENT_ID=my-session python mcp/client/heron_bridge_client.py prove --in "Project1" list-levels
```

Twenty fragments need nothing but `doc` and can be run this way today. `python tools/check-gaps.py`
counts what is left; believe it over this file.

### Traps that cost real time on 2026-09-06 — do not rediscover these

- **A CLI command orphans the Revit lease for five minutes.** `CLIENT_ID` is minted per process, which
  is right for the long-lived MCP server and wrong for a command line. **Always set `HERON_CLIENT_ID`**,
  and use `prove` rather than one `fragment` call at a time.
- **`git checkout` in the shared folder moves the tree under every other session.** It happened three
  times in one day: files vanished mid-build, a commit landed on somebody else's branch, and a forced
  reset destroyed another session's uncommitted edit. **[§9b](#9b-three-sessions-at-once--the-protocol-that-stops-them-colliding)
  says use a worktree. Use one.** The collisions stopped the moment that was done.
- **`git worktree remove --force` discards uncommitted work.** It ate five lines of a client change that
  had not been committed yet. Commit before tidying up, not after.
- **The add-in DLL is locked while Revit runs.** Any change under `revit/` needs Revit CLOSED, then
  `tools/deploy-addin.ps1 -RevitVersion 2024`, then Revit restarted and **Heron pressed** — the bridge
  never connects on its own.
- **`Application.Documents` contains the LINKS.** On Snowdon that is six extra documents that look like
  projects to choose from.

> ### `test_embed` and `test_retrieve` — DONE in PART 5, and done the way this warning asked
>
> **This block used to say they were failing and must not be edited until green. Both now pass, and
> nothing was edited until green.** The warning asked for them to be *"re-based against the model backend
> with the reasoning written down, which belongs with the A7 work"* — A7 was closed in PART 5 and that is
> what happened there.
>
> Each assertion is now **conditional on the backend**, so a machine with no `model2vec` still gets the
> old behaviour and is still told the truth about it. And the improvement is **asserted rather than
> described**: *"show me every duct in the model"* returns **four duct fragments** where n-grams returned
> none, and the two sentences with and without *"show me"* now agree on part of the answer where they
> previously shared nothing.
>
> **One of those assertions was written wrong first**, and it is recorded rather than smoothed over: it
> claimed the two sentences return identical shortlists, which holds in the full library and not in that
> test's smaller fixture. Measured in one place and asserted in another. The corrected one says what is
> true there, with the reason.

---

## HANDOVER — 2026-09-12 (the SECOND SITTING of the improvement-gate track): a Windows-only failure nobody knew about, the sources re-read, and the housekeeping ledger retired

**Start here if you are picking this up.** The gate work below is merged. This sitting did three
things on top of it, all at the owner's direction, and each one found something.

### 1. The suite total was Linux-specific, and every document promised the wrong number

`tests/test_context.py` compared a source path against a hardcoded `/`. The value comes from
`heron_fragment.repo_relative()`, which is `os.path.relpath` and spells the path the way the **machine**
does — so the check passed here and failed on the owner's Windows PC, reported as
`brain\fragments\...`. **That machine saw 38 of 41 where this file and the `heron-ship` skill both
promised 39**, and that skill's only job is telling a later session which failures are theirs.

Fixed at the comparison, **not** inside `repo_relative()` — that value is written into the store's
`fragments.folder` column, and rewriting a persisted value is a far larger change than the defect
deserves. Normalising at the consumer is what this repository already does twice, in
`heron_fragment.fingerprint()` and `tests/test_carried_sources.py`.

**It could not be failed first on Linux**, so `change-evidence.py` ruled it `NO CHANGE MEASURED`, which
is the correct verdict and not a passing one. **[NEEDS-CHECKING](NEEDS-CHECKING.md) A14 is the one run
that closes it** — on the PC, `python tests/test_context.py` should exit 0 with no fourth failure.

### 2. Every external source opened again, and one of my own claims did not survive

The brief's rule is to re-open each source and read the **implementation files**, not the READMEs. Four
of the five rows in [34 §2.15–2.19](34-patterns-adapted.md) hold, one verified verbatim. **§2.17 did
not**: it said their check *"drives the lifecycle start to stop"*, and nothing in that repository does
— checked across the packaging script, the release gate, the prerelease tests, catalogue validation and
the convergence script. Corrected to what they actually do. **The Heron side is unaffected** —
`check-package.py` reading `Heron.addin` is still right; only the description of the source was wrong.

**A near-miss worth keeping.** §2.19 was almost written up as a second correction, because its three
severities were not in the folder the other two rows live in. Widening the search found them at once,
spelled exactly as recorded. *Absent from the folder I looked in* is not *absent*, and **a correction
needs its own evidence exactly like the thing it corrects.**

### 3. The independent review happened, and the housekeeping ledger retired

[Golden Rule 7](14-golden-rules.md) wants one agent to create and another to validate. A different
session reviewed the 2026-09-10 housekeeping work by checking its claims **against the code** rather
than reading them. It holds up: *"no executable code was modified"* is true and precisely worded, the
deleted duplicate really was byte-identical, the Codex path bug was real, and **all 151 repository
paths named in the seven entry documents resolve.** One defect found — `tools/check-dependencies.py`
had no section in `tools/README.md` — and it was **later drift, not that work's**. Documented.

**The reviewer's own false finding is recorded too**, because the method is the point: the path check
first reported 95 of 151 broken, and every one was the checker resolving a document-relative path
against the repository root. The documents were right and the tool was wrong — the same shape as
`test_graph` and `test_reachable`, and the third time in two days.

**So the housekeeping execution record was retired**, which is what work notes do. Its last open item
moved to a permanent register first: **the cold read is now [NEEDS-CHECKING](NEEDS-CHECKING.md) R3.**

### What is owed, and by whom

| | |
|---|---|
| **A14** | On the PC: `python tests/test_context.py` exits 0, and no fourth failure in the suite |
| **A12 · A13** | The add-in loads on each release; an upgrade and a rollback work |
| **R3** | **The cold read — and it needs a stranger.** Nobody who has worked in this repository can close it, which is why two sessions in a row have declined to |
| **Golden Rule 7, for the gate itself** | `check-change.py` has still only ever been run by the sessions that built it. `work-notes/plans/improvement-gate-execution-record.md` stays until somebody else points it at their own change |

**Measured here, on Linux, this sitting:** all gates exit 0 — `check-docs`, `check-metadata`,
`check-structure`, `check-package`, `check-licence`, `check-dependencies`. Derive the suite board
rather than reading a number here; the three that do not pass need the MCP SDK and a built .NET test
host. **`code=$?` on its own line** — `$?` after a pipe is the pipe's exit code, and that mistake
produced two optimistic numbers in this branch before `change-evidence.py` caught them.

---

## HANDOVER — 2026-09-12 (the IMPROVEMENT-GATE track): a change now has to say what it is for, and two red suites went green

**Nothing here has been near Revit, and no fragment status moved.** Full ledger, every phase note and
every measurement: [`work-notes/plans/improvement-gate-execution-record.md`](work-notes/plans/improvement-gate-execution-record.md).

**What exists now that did not before.** Four things, and each answers a question nothing in this
repository was asking:

| | |
|---|---|
| [`tools/check-change.py`](../tools/check-change.py) | Does this change do only what it said it would? Compares the diff against a declared `intent` / `area` / `risk`, using the **layering table** rather than word overlap — so `brain/` is *supporting* work for an `mcp/` change and `revit/` is not |
| [`tools/change-evidence.py`](../tools/change-evidence.py) | Is it better than before? The same measurements twice, compared as sets, ruling `KEEP` · `REVERT` · **`NO CHANGE MEASURED`**. It cannot change anything, which is what makes the loop safe to point at a prompt or a fragment description |
| [`tools/check-package.py`](../tools/check-package.py) | Would the delivered thing install? **Nothing in this repository read `Heron.addin`** — the first file Revit opens. Now a fourth gate that must pass |
| [`mcp/server/heron_runtime.py`](../mcp/server/heron_runtime.py) | Why can Heron *not* do this? Six verdicts where `heron_capability.resolve()` had one `None` for five different reasons. **Wired to nothing yet, on purpose** — [PROPOSALS F1](PROPOSALS.md) |

Plus the **Python half of the layering table**, enforced for the first time since Step 1 in
[`check-structure.py`](../tools/check-structure.py) — its own source had admitted for a fortnight that
two thirds of the repository went unchecked.

**Two suites that were red on every machine are green, and both were the same defect.** `test_graph.py`
and `test_reachable.py` each carried a fixture describing a repository that had moved on: one matched
two exact adjacent lines and `role:` arrived between them, so it broke **none** of the 50 providers it
meant to break; the other excused a function that had acquired a legitimate caller. **Neither test was
edited until it passed** — both claims are unchanged. `.github/workflows/gates.yml` and the
[`heron-ship`](../.claude/skills/heron-ship/SKILL.md) skill were corrected in the same change, because
that workflow errors when a known failure starts passing and asks for exactly this.

**What is owed.** The gate has only ever been run by the session that built it, which is
[Golden Rule 7](14-golden-rules.md) unsatisfied. Two new rows in
[NEEDS-CHECKING](NEEDS-CHECKING.md) — **A12** and **A13** — are the delivery questions no script can
answer: whether the add-in loads, and whether an upgrade and a rollback work. Three defects found on the
way are in [PROPOSALS Part F](PROPOSALS.md) rather than fixed.

**Next:** use it on the next real task. `python tools/check-change.py --intent "..." --area <part>
--risk low`, then `tools/change-evidence.py capture` before and after.

---

## HANDOVER — 2026-09-10 (the HOUSEKEEPING track): three READMEs were wrong by more than a hundred, and the write path was documented as not existing

**Documentation only. No executable code was modified** — `git diff --diff-filter=M` over `.py`, `.cs`,
`.csproj`, `.props` and `.ps1` across the whole batch returns nothing. Its full ledger was a work note and was
**retired on 2026-09-12** once its last open item moved to a permanent register — the cold read is now
[NEEDS-CHECKING](NEEDS-CHECKING.md) **R3**, and the Windows-only suite failure it found is **A14**. The
sections below are what survived it; the ledger itself is in git history, which is recovery evidence
rather than something to go looking in.

**The three findings worth knowing about:**

1. **The write path was documented as not existing.** The root `README.md` and `brain/README.md` both
   said running a fragment that WRITES *"does not exist"*. It does — `run_fragment_write`, registered
   `MODIFY`, dispatched in two files, governed by [D-55](DECISIONS.md) — and **55 `MODIFY` fragments
   carry a recorded proof**, so it has met a real model. Corrected in both.
2. **Three READMEs carried a fragment count wrong by more than a hundred.** `brain/README.md` said
   *52 `PROVEN`, 308 `DRAFT`*; the truth was **167 and 193**. The root `README.md` and `docs/README.md`
   said 159/201. Every one of them already carried a sentence admitting the number goes stale —
   **writing that sentence is not enough**, and each now names the deriving command instead.
3. **The skills lived in three places and one of them never existed.** `.claude/skills/` and
   `.agents/skills/` held the same six skills, five byte-identical, while `.codex/agents/*.toml` told
   Codex to read `.Codex/skills/` — **a directory that has never existed**, so Codex was broken
   independently of either. On the owner's decision the tree is now one: `.claude/skills/`, matching
   [D-01](DECISIONS.md), with `.codex` repointed at it and the `.agents` copy deleted after
   file-by-file verification that nothing unique was lost.

| | |
|---|---|
| **Created** | `AGENTS.md` · [`docs/PROJECT-MAP.md`](PROJECT-MAP.md) · [`docs/work-notes/README.md`](work-notes/README.md) · [`tests/README.md`](../tests/README.md) |
| **Deleted** | `.agents/skills/` — 7 files, a duplicate |
| **Moved** | **Nothing.** Four candidate moves were assessed and all rejected with reasons. This file stays put because saved continuation prompts point at this exact path from outside the repository; it is **labelled** as the operational entry point instead |
| **Links** | **4 broken → 0.** All four were wrong filenames; each right target was found by reading content |
| **Also corrected** | The four specification parts disagreed about how many parts exist (1 of 2, 2 of 2, 3 of 3) · `CONTRIBUTING.md` said Phase 2 had never loaded into Revit · the `heron-ship` skill said *"35 of 38 pass"* when there are 41 suites, so its own *"a fourth failure is yours"* rule would have misled an agent |
| **Checks passing** | `check-docs` (0 broken links), `check-metadata`, `check-structure`, `check-licence`, `check-routing`, `check-intrusion`, `agent-count`, `heron_fragment`, `git diff --check`. `check-package`. `check-gaps` exits 1 on a plain container — its exit code follows the UNFINISHED list, whose only entry is `test_served_claims.py`, failing for want of the MCP SDK. Every fragment below PROVEN is in the *waiting* bucket. Run it for the counts; do not read one here |
| **Could not run** | The three compile gates and `test_bridge_roundtrip` — **no .NET SDK.** `test_mcp_serves` and `test_served_claims` — **no MCP SDK.** None counted as a pass |
| **Needs real Revit** | Everything in [NEEDS-CHECKING.md](NEEDS-CHECKING.md). Unchanged by this run |

**Two things this run could not establish, and does not claim.** The drive-letter condition — repository
on one drive, `TEMP` on another — cannot be exercised where they share a filesystem, so the
`check-licence.py` repair is **still not re-proved** and wants one run on the owner's machine. And the
cold-read walkthrough is not claimed: the session that wrote the entry documents cannot cold-read them.

**Left alone deliberately:** `test_graph` (3 checks) and `test_reachable` (1 check) fail and are
pre-existing and unrelated — recorded, not fixed. So are the dated snapshots inside `DECISIONS.md`,
`FRAGMENT-ISSUES.md` and this file's own session records; those are labelled history.

---

## HANDOVER — 2026-09-09 (the CALLER-INPUTS track): 6 → 40 arrangeable, and a job file nobody types

**The through-line is one question: what can a person hand a fragment?** Four changes answer it, and the
fifth is a rebase that had to be untangled before any of them could land.

### The measure that matters, taken the same way twice

`tools/generate-jobs.py` reports how many DRAFT fragments with no run record can be arranged as a job.
Run before and after each change against the **identical** library, it is the only honest number here:

| | Arrangeable |
|---|---|
| start of the track | **6** |
| after `Element` resolves by name | 6 (+2 `FamilySymbol`, which came free) |
| after eight contracts narrowed | 8 |
| after a point can be typed in | **40** |

**The units decision did four fifths of it**, and everything before it was groundwork that looked bigger
than it was. Say that plainly to whoever reads this: narrowing eight contracts unblocked almost nothing
on its own, because six of the eight were also waiting on `IList<XYZ>`.

### 1. The job file is generated now — `tools/generate-jobs.py`

`batch-prove.py` takes a hand-written job list, and **six input names were mistyped on 2026-09-09
alone** — `widthMm` for `width`, `sortByFields` for `sortFieldNames`. A mistyped name does not look like
a typo coming back: the fragment refuses, or binds nothing and reports zero, and both read exactly like
a broken fragment. This is [FRAGMENT-ISSUES 3h.4](FRAGMENT-ISSUES.md), fourth of the four.

Everything it emits is read out of a file named beside it: DRAFT-and-no-run-record from the library and
`brain/proof-drafts/runs/`, `write: true` from the fragment's `risk:` against what `run_fragment_write`
carries in the registry, the setup chain from whether anything a *fragment* provides is needed, and
**the input names spelled from `contract.needs`** — which is the point of the tool.

**What it refuses to derive is the more important half.** The category and the view are left blank and
marked `FILL IN`. A wrong category produces a confident meaningless result — eleven times in one batch.
So does `expect:`; the candidate names are offered as a comment and never chosen. **It removes the
errors a person makes while typing, not the judgement a person has to make.**

**Nothing is dropped silently.** Every candidate is either a job or a marked one with its reason: a risk
Heron does not reach (`PUBLISH`/`ADMIN`, refused by the client on the doorstep), a shape D-54 cannot
receive, a value the setup chain does not leave behind, two needs bound to one chain value, or **nothing
to vary between the legs** — which makes it [D-53](DECISIONS.md) tracking work rather than batch work.

**There is no generated job file committed, on purpose.** `brain/proof-drafts/runs/` is gitignored, so
*"never been in front of a model"* is a **local** fact: on a fresh clone every fragment looks untried,
and on the proving machine the list shrinks after every batch. A committed snapshot is stale by the
afternoon. **Generate it on the machine that will run it, and generate it fresh.**

### 2, 3, 4. Three type rules, and one of them was a decision nobody had made

| Landed | Rule |
|---|---|
| `Element` | resolves to an element **TYPE** by name, `Basic Wall: Generic - 200mm`. An **instance** is refused and always will be: `Element.Name` on one returns its TYPE's name, so searching instances would match every element of that type — a missing rule turned into a wrong answer |
| nine narrower classes | `WallType`, `FloorType`, `CeilingType`, `FilledRegionType`, `HostObjAttributes`, `MEPCurveType`, `FamilySymbol`, `Phase`, `FilterElement` |
| `XYZ`, `IList<XYZ>` | **[D-67](DECISIONS.md)** — three numbers in **MILLIMETRES**; several points separated by semicolons |

**D-67 was not a preference, it was already decided and unwritten.** `HeronUnits` converts nothing but
millimetres to feet; 59 caller values are named `...Mm`; D3 is written *"200 mm. Not 200 feet"*. And
`array-elements-radial` takes `centreXMm`/`centreYMm` while `place-detail-item` takes `atXMm`/`atYMm` —
**those are points, split into millimetre scalars because there was no way to send one.** The workaround
named the unit years before anybody wrote the decision.

Two things about it were checked rather than assumed, and both could have gone the other way:

- **A direction has no unit**, so dividing it by 304.8 looked wrong. It is harmless — scaling all three
  components alike does not move a vector — but **all six consumers were read** before relying on that.
  Four call `Normalize()`; `check-obstructions` hands it to `ReferenceIntersector.FindNearest`;
  `place-family-on-face` uses it as a facing vector. **None uses the magnitude.**
- **Two separators, not one.** `"0,0,0,1000,0,0"` is two points only if you already know they come in
  threes, and a list one number short silently becomes a different, valid-looking list.

Each ordinate is bounded by `HeronUnits.MaxMillimetres` (100 km) and a number past it is **refused
rather than converted** — that is a value that arrived in the wrong unit.

### The eight contracts, and the two that would have been narrowed wrongly

Each type was read out of the fragment's **own body**, never guessed from the need's name:

| Fragment | Was `Element` | Is | Because |
|---|---|---|---|
| `create-hvac-zone` | `phase` | `Phase` | the body opened with `var asPhase = phase as Phase` — declaring the base class, then casting back |
| `create-floor` | `floorType` | `FloorType` | the 2020 overload is reached by reflection as `NewFloor(CurveArray, FloorType, Level, bool)` |
| `apply-view-filter` | `filter` | **`FilterElement`** | its own comment: a rule filter and a selection filter **share a base class** |
| `create-from-room-boundaries` | `hostType` | **`HostObjAttributes`** | branches on `hostType is CeilingType` / `is FloorType` |
| `create-electrical-run` | `runType` | **`MEPCurveType`** | builds a cable tray **or** a conduit from the same value |

`ParameterFilterElement` was the obvious reading of `filter` and it is the **wrong** one.
**Narrower than `Element` is the point; narrower than the fragment can use is a regression dressed as
precision.**

The classes are named with `typeof(...)`, not looked up by string, so one missing on a release is a
build failure rather than a refusal in front of a model. `ElementsNamed` tests the class with
`IsInstanceOfType` rather than `OfClass`, because four of them are **abstract** and which abstract
classes `ElementClassFilter` accepts is a runtime question that cannot be settled at compile time on
eight releases.

### 5. The rebase that had three stale commits in it

`fix/make-silence-illegal` was eleven commits ahead of main. #50 had merged three of them **in rewritten
form** — three `role: accounting` hunks and the inside-out-margin guard removed before merge — so the
branch's copies were stale. **Because the content differed the patch-ids differed, and
`git log --cherry-pick` recognised none of the eleven as upstream**: a plain rebase would have replayed
all three and put the margin guard back. Eight cherry-picked, three dropped, merged as #55.

**Two resolutions in it were judgement, and a reviewer should know which:**

- The `FRAGMENT-ISSUES` conflict was **not two rows about different things.** Main's paragraph concluded
  sheets and views are unreachable; the branch's restated every one of its facts and then said that
  conclusion *"was wrong, and it was wrong for an hour"*. Keeping both would leave the document
  asserting something the next paragraph calls wrong, so **the branch side was taken.**
- `731cda4` carried a real `enclosed = 0;` guard for `set-view-section-box`. **Main's side was taken**,
  and that was right for a reason found later: the guard lived **inside the margin guard**, which the
  owner had had removed. See below — it must not be re-added.

### WHAT IS NOT FINISHED

**`IList<XYZ>` was the biggest blocker and is gone; the next one is `ElementId`.** Of the fragments
still marked unarrangeable, the shapes they wait on, most-wanted first: `ElementId` (24 in the MODIFY
set alone — the 2024 32-to-64-bit change), `Element` as a specific instance (14 — needs the selection,
not text), `OverrideGraphicSettings`, `Color`, `Material`, `View3D`. **Generate the list rather than
trusting this sentence:** `python tools/generate-jobs.py`.

**The `enclosed = 0;` guard — and the question it turned into. Read this before touching
`set-view-section-box`.**

This entry first said the guard must NOT be re-added, and the reasoning was right when written: it lives
inside the **inside-out-margin guard**, that guard was removed before #50 merged, and without it the
line is dead — `enclosed++` is reached only after a non-null bounding box, and reaching it is what sets
`any = true`, so `!any` already implies `enclosed == 0`.

**Both came back the same evening in `40915d6`, and both were removed again on 2026-09-09 when the
owner confirmed the removal still stands.** `40915d6` restored them without saying so: its message
carefully documents its choices for every `fragment.yaml`, for `heron_validate.py` and for
`FRAGMENT-ISSUES.md`, and **says nothing about this `.cs` file** — a conflict resolved per-side rather
than a decision taken. `impl/any/fragment.cs` is now byte-identical to `531f47e` again, and the
`fragment.yaml` beside it keeps the role declarations that merge brought, which were wanted.

**THE SAME MERGE ALSO BROKE THE FRAGMENT COMPILE GATE, and it was red on `main` for hours.**
`set-schedule-filters` came out of it declaring `refused` **twice** — two byte-identical `provides`
entries — and the generated code then failed on all eight releases with *"a local variable named
`__provides_refused` is already defined"*. Removed with the guard.

**The shape of that is worth more than the fix.** A merge resolved per-side duplicated an entry, and
nothing above the compiler could see it — the same failure that let a duplicate `## D-56` through
`check-docs.py` the same day. **After a merge that touched contracts, run
`python tools/check-fragments-compile.py` before trusting the tree**, and read a merge's message for
what it does NOT mention as carefully as for what it does.

**That last path is silent, and that IS a real finding.** If `IsSectionBoxActive` comes back false the
fragment reports `applied false`, `enclosed 22` and **adds nothing to `refusalReasons`** — a count on a
run that changed nothing, with no reason given. That is §3h.1's exact shape in the fragment §3h.1 was
written around. **It has been read from the code and never seen happen**, so it is not written into
FRAGMENT-ISSUES, whose value is that every row was observed. Someone with Revit open should try to make
it happen before it is recorded as fact.

**Three fragments declare `IList<Element>` as a caller value** — `check-room-mep-completeness`,
`connect-air-terminals`, `propose-mep-openings`. They want instances, and a comma-separated list of type
names is not what they are asking for, so the list form was deliberately **not** added alongside the
singular.

**`IList<IList<XYZ>>` stays refused** — one need in the library, `pointPairs`. It wants a third
separator, and that is a decision for when a second fragment wants one.

### Two things about the tooling that cost time here

**`check-docs.py` could not see a duplicate decision number — FIXED the same day, and it now fails the
run.** D-67 was first written as **D-56, which already existed**, and the checker passed on a file with
two `## D-56` headings. The cause was one character of intent: every registry was read with
`set(re.findall(...))`, and a set is precisely the thing that makes a duplicate invisible. The same line
was written three times, so **rules, decisions and questions are all checked now**, not just the case
that was caught.

**It fails the run where a broken link only prints, and that asymmetry is deliberate.** A dead link
announces itself the moment somebody clicks it; a duplicate id is silent and makes every reference to
that number ambiguous — both entries look correct in isolation. Verified by breaking it on purpose once
per registry, and the unmodified repository still exits 0.

**What is still owed there: `check-docs.py` gates the build and has no test at all**, unlike every other
tool in `tools/` that concludes. That gap is older than this session and closing it moves the derived
test count, so it wants its own commit.

**A stale local `main` makes a verification gate lie.** The rebase brief's gate said
`git diff main...HEAD`, and the local `main` ref was 175 commits behind `origin/main`. The gate printed
a large diff that meant nothing. **Use `origin/main` in any gate written for somebody else to run.**

---

## HANDOVER — 2026-09-09 (the REVIEW track): five defects in the branch, three proofs, and the model is still dirty

**Read "WHAT IS NOT FINISHED" below before starting anything.** Something is left in your Revit that a
fresh session cannot see and cannot fix.

### What this session was asked to do, and did

Review `fix/make-silence-illegal` before it went further. Five defects found, all fixed, all on main:

| | |
|---|---|
| **The stale-proof guard let the same day through** | `implementation_changed_after` compared `last > proof_date` — both DATES. `set-view-section-box` was proved and edited on the same day, so the guard answered *"the code did not move"* and `restamp` offered to re-bless a genuinely stale proof, refusing nothing. Now `>=`: a proof records a date, not a time, so same-day ordering is not in the data, and this file's own rule is that unknown counts as YES |
| **A refusal that still reported a count** | `set-view-section-box`'s inside-out-margin guard refused without clearing `enclosed`, so a run that changed nothing came back `enclosed 22, applied false` — the exact defect the branch existed to remove |
| **`measure-run-quantities` matched asymmetrically** | area by substring, length against a closed word list, so `surface area` read and `total length` was REFUSED. Both sides now match BY WORD — and by word matters: `diameter` contains "meter" and `volume` contains "m", so the obvious symmetric fix silently measures the wrong thing |
| **A hint narrower than its own search** | `edit-text-values` looks up the instance parameter then falls back to the TYPE, but its refusal listed instance parameters only — so it could print a list omitting the very name that would have worked |
| **Three vs four in the docs** | FRAGMENT-ISSUES §3i's *"Three more"* heading over a four-row table |

Then `set-view-section-box` went **PROVEN → DRAFT** on the owner's instruction, because fixing the §3f
doubt changed its implementation and a proof is evidence about the bytes it was taken against. The
`proof:` block was KEPT — `can_promote` refuses to carry a stale proof forward, so keeping it costs
nothing and re-proving starts from a written arrangement rather than a blank page.

### Three fragments proved, and the arrangement is the transferable part

`tools/jobs/refusal-paths.yaml`, against `Snowdon-scratch_ajmal.al` (9,628 elements, Revit 2024,
session 32940). All three PASS. Signed `by: Ajmal PS`; **all three are still `DRAFT`**, because
`accept` records a proof and refuses to promote — *"promoting it is a separate, deliberate act, and it
is yours"*.

**Every negative is a VALUE on the SAME selection.** A negative made by pointing at a different
category proves the old thing — that an empty set gives an empty answer — and never once enters the
code that was written:

```
measure-run-quantities   "total length" -> quantities 22    "diameter"       -> quantities 0, REFUSED
set-view-section-box     marginMm 500   -> enclosed 22      marginMm -999999 -> enclosed 0, REFUSED
edit-text-values         mode=prefix    -> changed 17       mode=transform   -> handed 0, REFUSED
```

**`set-view-section-box` finally has §3f's stronger negative.** Both legs report `view3D View3D` and
`viewRefused false` — a good 3D view either way — so the fragment reached the empty answer by its own
work rather than because Revit forbids a section box on a plan. That doubt is settled by evidence
rather than argument.

**Rule 2 nearly cost a leg.** `edit-text-values` needed a field already holding text, and duct
`Comments` probed `blank 22, values 0` — that positive would have come back `changed 0` and read as a
defect. Moved to the 17 sheets via `list-sheets` + `set-selection`, where `Sheet Name` probed
`values 17, blank 0`. **Probe before trusting a positive.**

### What it cost, and the finding that came out of it

Proving `edit-text-values` renamed all 17 sheets and **the rollback did not hold**. Third incident
(§1c) — and this one breaks the theory: **seventeen sheets and a rename.** Not large, no cascade, no
dialog, nothing created.

> **The element count could not see it.** 9,628 before and after. Both earlier incidents were caught by
> counting; a rename creates nothing. After a write proof, re-read the thing that was written.

Reading `RevitFragment.cs` then gave the first concrete mechanism for why three incidents have no
explanation. `SafeRollBack` was `void`:

```csharp
try { if (group.GetStatus() == TransactionStatus.Started) group.RollBack(); }
catch { }
```

The inner transaction is COMMITTED first; the undo depends entirely on this group rollback landing
afterwards. Two silent exits — a status that is not `Started` **skips** the call, a throw is
**swallowed** — and `void` meant neither reached a caller. `WithVerdict` then said *"NOTHING WAS KEPT"*
because `apply` was false, which is the request and not the result. **A failed rollback and a clean one
came back byte-identical.**

Fixed: `SafeRollBack` returns Revit's status after the attempt, `WithVerdict` says **THE ROLLBACK DID
NOT REPORT SUCCESS** when it does not, the reply carries `rolledBack`, and the client writes only what
the reply says. §5 row 10 rewritten. `RevitWrite.SafeRollBack` carries the same defect on its error
paths and was deliberately NOT changed on the day the model got damaged — named in row 10 so it reads
as a decision.

### WHAT IS NOT FINISHED — read this before starting anything

**1. THE MODEL IS STILL DIRTY. DO NOT SAVE IT.** `Snowdon-scratch_ajmal.al` in session 32940 has all
17 sheets renamed `HERON Cover Sheet`, `HERON Learn about this project`, and so on. Sheet NUMBERS are
intact. The disk file was not written. **Close without saving** and the names come back.

**2. Revit has had a dialog open for hours.** Every request since has answered *"Revit is busy… a
dialog may be open"*. Nothing model-side can run until somebody answers it at the keyboard. If it is
the save prompt, the answer is **Don't Save**.

**3. The re-prove NEVER RAN.** `set-view-section-box` and `edit-text-values` carry proofs whose text
says *"run inside a transaction and ROLLED BACK, so the model was left exactly as it was"*. For
`edit-text-values` that is FALSE and was proved false the same hour. The client no longer writes that
sentence, but **these two proof blocks were signed before the fix** and still hold it.
`measure-run-quantities` is clean — it is READ-only and never had one.

**4. The add-in fix is NOT DEPLOYED.** It compiles on 2020–2027. It needs a rebuild and a Revit
restart, which is the same restart that clears the dialog. **Do that first, then re-prove the two** —
their new proofs will carry a checked sentence instead of a disclaimer.

**5. `write.enabled` is still TRUE.** Correct for the proving run, wrong to leave. `revit_health` says
so on every call.

**6. [PR #62](https://github.com/Ajmalpshaik/Heron-AI/pull/62) is OPEN and unmerged.** Mergeable,
clean, no CI configured. This session was blocked from merging it by a permission rule, so it is a
click that is owed.

### Two things that cost this session time, so they do not cost the next one

**One `HERON_CLIENT_ID` per person.** The lease names a CHAT, not a person. Calling an MCP `revit_*`
tool claims it for the MCP process; a command line with its own id is then refused as *"in use by
another chat"* — by yourself. It reads exactly like a fragment failing. Pick one id and drive
everything from it.

**A deleted branch nearly took a commit with it.** `fix/make-silence-illegal` was merged as PR #61 and
deleted at both ends while this session still had an unpushed commit on it. The commit survived only
as an unreferenced object — nothing pointed at it, and a `git gc` would have taken it. It was rescued
onto `fix/rollback-reports-its-outcome`. **Check for unpushed work before deleting a branch in a shared
tree.**

### To carry this on

```bash
python tools/batch-prove.py tools/jobs/refusal-paths.yaml --client-id heron-review-proof
python brain/heron_validate.py review set-view-section-box
```

The job file is written and dry-runs clean. Drop the `measure-run-quantities` job — its proof is
already signed and honest.
## HANDOVER — 2026-09-09 (the ROLES track): 989 provides declared, and nothing reads a name any more

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

## HANDOVER — 2026-09-10 (the PROVING track, day four): 0 proved, 5 defects found, and the add-in was a day stale

**Nothing was promoted, and that is the honest outcome rather than a bad night.** Two drafts are
written and wait for a signature; five defects were found, four of them recorded for the first time;
and the add-in Revit had loaded turned out to predate the change everybody assumed was live.

**Read [`docs/FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md) §5 first — rows 8, 11, 12, 13 and 14.** Every
one was observed in front of a model, with what was passed and what came back.

### Two drafts wait for a person, and only a person can sign them

Both against **Project1**, a scratch model, Revit 2024 session 28596.

| | Positive | Negative |
|---|---|---|
| `create-wall` | `created 4` — *"4 wall(s) built on 'Level 1' at 3048 mm high"* | two identical points: `created 0`, `tooShort 1` |
| `create-grids` | `created 6`, named A, B, C, positions `[0, 20, 40]` | no spacings: `created 0`, and it SPOKE — *"no spacings given in either direction - nothing to set out"* |

```
python brain/heron_validate.py accept create-wall  --by "Ajmal PS"
python brain/heron_validate.py accept create-grids --by "Ajmal PS"
```

**Both carry a sentence no earlier proof could:** *"Revit reported the transaction group ROLLED
BACK."* That is the check from §5 row 10 answering for the first time instead of the client repeating
its own flag. It was confirmed independently by reading the model back — 0 walls in Project1
afterwards — rather than by trusting the line.

**The arithmetic is the point, not the counts.** Five points gave FOUR walls because a closed run has
four segments; two spacings gave THREE grids because spacings are gaps, not positions. A fragment
that returned "some" of each would have passed a weaker reading.

### THE METHOD THAT CHANGED, AND IT CAME FROM THE OWNER

**Build the content on purpose in a scratch model, instead of hunting for it in a real one.** Rule 2
says ask for what the model has, and every earlier session answered it by searching Snowdon for
content that might not be there — `read-space-loads` (all 17 spaces `noLoad`), `check-flow-direction`
(no bad joint exists), `check-surface-fit` (the floors are a link, so nothing is ever hit). A model
built for the proof has the content by construction, and the negative can be arranged honestly.

**It also removes the objection that kept MODIFY fragments out of every batch so far.** §1c's rollback
has failed three times; in a throwaway project a rollback that does not hold costs nothing. That is
why the first two write proofs in this file's history were taken tonight rather than a week ago.

### The add-in Revit was running was a DAY OLD — §5 row 12

`select-in-region` and `check-surface-fit` refused with *"A point cannot be typed in yet."*
**That sentence exists nowhere in the source.** Deployed binary **2026-09-08 23:06**;
`RevitFragment.cs` last written **2026-09-09 23:14**.

**The row above claiming the add-in was "rebuilt and redeployed to Revit 2024 on 2026-09-09" was
wrong.** D-67 — the widening that "took the arrangeable library from 6 to 40" — had never reached the
machine, and neither had §5 row 10's rollback check. Two things everyone believed were live were not.

**Now deployed and verified at binary level on 2020, 2024 AND 2027** (all three are installed; only
2024 had been kept up). Deploy one release at a time: the build output folder is shared and the
newest wins, which is the 2026-09-08 failure. Never run `check-compile.py` before a deploy.

### Three more defects, each found by a fragment refusing in front of a model

**Row 11 — the setup chain's values are wiped before the fragment under test runs.** `validate` runs
the setup chain, then sends `chain: "reset"` for the fragment itself
(`heron_bridge_client.py:1377` → `RevitFragment.cs:232`), discarding what the setup just left. **A
setup chain can hand over only what lives in Revit's OWN state — the selection — never a value.**
`group-and-count` and `sum-by-group` both refused with `needs_unbound: 'values' was never supplied`
while the setup reported success. **The obvious fix is wrong:** the chain OUTRANKS the selection
(`RevitFragment.cs:917`), so dropping the reset would make `set-selection` decorative inside every
existing arrangement. It has to be opt-in per job.

**Row 13 — an `Element` caller value binds a TYPE where all twelve fragments mean an INSTANCE.**
`OneElement` is `OneOfClass(doc, typeof(ElementType), ...)` and returns the type **with no problem
set**, so nothing refuses. `select-by-host` compares against a type's id that nothing is hosted on;
`filter-elements-by-type` calls `GetTypeId()` on a type that has none. **`generate-jobs.py` lists all
twelve as arrangeable** because it checks the name RESOLVES, never that the resolution FITS — so of
its 39, **27 are genuinely testable.**

**Row 14 — `select-in-region` converts millimetres to feet a second time.** D-67 put the conversion in
`OnePoint`, so an `XYZ` arrives already in feet; the fragment divides again. A box half a kilometre
either side of origin became about **10.7 feet** across and returned `elements 0`. **It is the only
one** — all 29 XYZ-taking fragments were read line by line, and the rest convert a SCALAR
(`spacingMm`, `gapMm`) or use `1.0/304.8` as a one-millimetre tolerance. `create-grid` carries the
rule in a comment: *"adding one 'to match the other fragments' would place every grid 304.8 times too
far out."*

### Row 8 had no named victims. It has fifteen, and it is now FIXED

`create-grid`, `create-floor` and `create-ceiling` all returned `POSITIVE UNREADABLE  created
ElementId cannot be read as a quantity`. **29 fragments declare a bare `ElementId` result and 15
DRAFT ones have no other countable result at all** — `create-sheet`, `create-schedule`,
`create-plan-view`, `create-text-note`, `create-filled-region`, `duplicate-type`,
`place-mep-fitting` and the rest.

**And it was the WHOLE fix, which was not obvious.** The batch also said *"the negative did not come
back empty either"*, which looked like a second defect — an undeclared `findings` role, §3h.2. It is
not: `findings` is already in `NOTE_KEYS`. The record showed the real cause — `create-ceiling`'s
NEGATIVE reported `created ElementId` while its own findings said *"A ceiling needs at least three
boundary points and 2 were given"*. **It created nothing; the id was `InvalidElementId`; the renderer
gave it the same word as a real one.** One line repairs both legs of all three.

`Describe` now returns `"(null)"` for `InvalidElementId` and `"1 item(s) [id N]"` otherwise — no
`IntegerValue` (deprecated at 2024) and no constructor (int to long at 2024), so no version `#if`.
Compiles clean on 2020, 2024 and 2027, and **is deployed**.

### What tomorrow starts with

1. **Sign the two drafts**, above. They are the first write proofs in this file.
2. **Re-run the creators.** `tools/jobs/creators-project1.yaml` is written, dry-run clean and correct
   on units; `create-grid`, `create-floor` and `create-ceiling` should now pass on the deployed fix.
   Then the other twelve ElementId-blocked DRAFT fragments.
3. **Put walls in Project1 to test the transforms.** `move-elements`, `copy-elements`,
   `rotate-elements`, `mirror-elements`, `array-elements`, `snap-to-grid` all need something to act
   on, and the proving batch rolls back so it leaves nothing. It needs `--write --apply`, which is a
   deliberate act by a person.
4. **Row 11's opt-in chain flag**, which unblocks every producer-to-consumer pair.
5. **Row 13's refusal** — `OneElement` should decline for the instance-meaning names rather than
   bind a type.

### Three things that will bite whoever picks this up

**`write.enabled` is `false` in `%APPDATA%\Heron\config\heron.config`, and that is the right
default.** Every MODIFY fragment refuses with `write_disabled` until it is `true`. It is read fresh on
every call, so nothing needs restarting either way. It was turned on for tonight's batch and **turned
back off**.

**`write: true` is per job in a job file and batch-prove DOES NOT derive it from the fragment's
`risk:`.** It defaults to false, and a MODIFY fragment sent down the READ path is refused politely and
reads like a fragment declining — §5 row 6, which cost a whole commit once.

**UNITS, and the two are not the same.** `XYZ` and `IList<XYZ>` are MILLIMETRES, converted by
`OnePoint` in the resolver. A bare `double` is INTERNAL FEET, raw — every creator header says
*"Lengths are internal FEET."* **The name carries it: a `...Mm` suffix means millimetres, a plain name
means feet.** So `points` are mm and `height` is feet in the same fragment, and `height: 10` built a
wall 3048 mm tall.

---

## HANDOVER — 2026-09-09 (the PROVING track, day three): 142 → 159, and four sessions in one tree

**Eighteen fragments proved against `Snowdon-scratch_ajmal.al` in Revit 2024, and one un-proved.**
`set-view-section-box` went back to `DRAFT` when its implementation changed after its proof was signed
— which is the staleness guard working, not a regression.

Three other sessions ran alongside this one, on the prompts in the previous entry. **Sixteen PRs merged
on 2026-09-09**, #44 to #61. Everything is on `main` and no branch survives.

### What was proved, and the arrangement that did it

| | Positive | Negative |
|---|---|---|
| `set-element-level` | `moved 1` | `moved 0`, `refused 9` |
| `array-elements-radial` | `count=2` → `created 22` | `count=1` → `created 0` |
| `renumber-sequential` | `renumbered 22` | `renumbered 0` |
| `dimension-mep-runs` | `dimensionedCount 22` | `0` |
| `dimension-family-instances` | `dimensionedCount 10` | `0` |
| `edit-revision` | `changed 4` | `changed 0` |
| `duplicate-sheets` | `newSheetIds 17` | `0`, all 17 collisions named |
| `select-view-templates` | 18 templates | `0` |
| `manage-sheet-sets` | `memberCount 17` | `0` |
| `remove-view-template` | `detached 1` | `0` |
| `compare-elements` | `differing 29` | `0` |
| `check-vertical-clearance` | `tooClose 18` | `0` |
| `check-insulation-clearance` | `violations 46971` | `0` |
| `check-equipment-connectors` | `mismatched 22` | `0` |
| `set-selection` | tracking (D-53) | 22, 9, 10, 10, 307 across five inputs |
| `report-geometry-complexity` | tracking | 2024, 0, 1320, 3920, 28244 |
| `report-parameter-inventory` | tracking | 93, 22, 86, 71, 93 |
| `find-untagged-elements` | tracking | 14, 9, 10, 10, 307 |

**`set-selection` is the one that mattered.** It runs in every setup chain in every job file, so every
proof taken this week was standing on a fragment that was itself `DRAFT`.

**The arrangement that works for defect-finders**, learned by getting `check-vertical-clearance` wrong
four times: **a rich selection AND a demanding threshold for the positive; DUCT TAGS for the negative.**
A value-driven negative is stronger where it works, but it only empties the results that *depend* on
the value — `clashing 5` survived every gap tried, because a physical overlap is an overlap whatever
clearance you ask for. Only a selection with no geometry empties them all at once.

### THE THING WORTH READING: three capabilities were proved and invisible

Three separate times in one day, a batch stalled on something the library already had:

| What was thought missing | What actually existed |
|---|---|
| *"Sheets and views cannot be selected"* | **`list-sheets` is PROVEN**, needs only the document, and provides `elements` — which `set-selection` consumes. `list-sheets` → `set-selection` proved **five fragments in an hour**. Same for levels, grids, revisions, links |
| *"`views` needs an element list"* | `views (IList<View>)` **resolves from a single name** |
| *"The library cannot select more narrowly than a category"* | **Fifteen PROVEN selectors** are narrower — `select-by-family`, `select-by-connection-status`, `select-types`, `select-by-parameter-value`, `select-by-workset`, and more. **Every job file written that day used `select-by-category-name` and nothing else** |

Each was one edit away from being filed as a gap in `FRAGMENT-ISSUES.md`. **The library is further
ahead than the job files are**, and that is a far stronger argument for §3h.4 — generating job files
from contracts — than the six mistyped input names it was first written about.

### Two defects found in the proving harness itself

**`batch-prove` reports verdicts about runs that never happened — OPEN.** Six lease refusals came back
as six `POSITIVE EMPTY` verdicts, judged against an earlier session's record with a *different
arrangement*. Heron behaved perfectly — it refused, said nothing was sent, and declined to write a
record *"about a model that will not name itself"*. The runner judged the leftover file. **A date check
cannot catch this**; both records say `2026-09-09`. The session id in the `model` line is what separates
them, and `validate` already stamps it.

**A tracking runner that read the wrong fragment's output.** `prove` runs a chain and prints one
provides block per fragment; a search for `elements` found the *selector's* copy, producing five rows
that tracked the input perfectly because they **were** the input. `find-untagged-elements` was promoted
on that evidence and had to be **reverted and re-proved**. Its real answer is 14 untagged of 22, not 22.
Two guards now: find the block by slug first, and refuse to write rows unless one agrees with what
`validate` recorded for that fragment alone.

> **Rows that match the input exactly are the thing to distrust.** A real describer's answer *differs*
> from what it was given.

### One person, one `HERON_CLIENT_ID`

Four ids were in use for one afternoon — one by hand, two in scripts, one the runner picks itself. The
lease identifies a **chat**, so that is four chats competing for one Revit, and **every refusal reads
exactly like a fragment failing.** It caused an eleven-job wipe-out that was misread as an arrangement
fault, and the six stale verdicts above.

### Still open, and what tomorrow starts with

1. **`batch-prove`'s stale-record hole**, above. Small fix, and it protects every future batch.
2. **The rollback failed on seventeen renamed sheets** and counting could not see it — §1c, third
   incident. Two proof blocks carry the sentence *"rolled back, so the model was left exactly as it
   was"*, which is **false for `edit-text-values`** and **unverified for `set-view-section-box`**. The
   sentence comes from a flag in the client, never from anything Revit returns (§5 row 10).
3. **The `LIST_*` gap** — five fragments blocked purely for want of a name nothing can supply: a workset
   id, a template-free view, a legend name, a section mark, a material name. `list-worksets` reports
   names but not ids.
4. **Then keep proving.** 193 `DRAFT` remain, as of 2026-09-10 — derive it, do not read it here. Read §3i first — it now holds the arrangements that work.

**Revit was left busy with a dialog open at the end of the session.** If `heron_bridge_client.py count`
answers *"Revit is busy"*, look at the Revit window before anything else.

---

## HANDOVER — 2026-09-09 (the open-questions track): 44 → 52 answered, and the rest needs the PC

**Nine questions were open at the start of this track and one is open now.** `Q-51` stays open on
purpose. Everything else became [D-59](DECISIONS.md) to [D-66](DECISIONS.md).

**The owner answered two himself and handed the other six over** with *"this you can decide and when
you decide do the best only."* So six of the eight are my decisions, and every one of them is written
up with what it rejected and why — read the decision, not this summary, before changing any of them.

### WHAT IS LEFT, AND ALL OF IT NEEDS A WINDOWS PC WITH REVIT AND `dotnet`

**This container has no `dotnet` and no Revit.** Every rule below is CHECKED by a gate that runs here;
none of the edits was guessed. **Writing 62 collector rewrites nobody could compile would have been the
opposite of production-ready**, and that is the reason the lists are lists.

**The owner said the link work waits for the PC. It is item 1 and it is the biggest.**

| # | What | How many | Where the list is |
|---|---|---|---|
| **1** | **The link contracts** ([D-59](DECISIONS.md)). Each reading fragment gains `includeLinks` in `needs` (`source: request`) and `linksSearched` in `provides`, and its collector spans `RevitLinkInstance` when asked. **Absent means host only**, so nothing already proved changes | **62** | `python tools/check-revit-gate.py --list links` |
| **2** | **The dropped-counts** ([D-64](DECISIONS.md)). Each declares a field in `provides` naming what it dropped — the rule is that ONE EXISTS, not that a particular word does. The marker rides **only on the empty answer**, capped | **59** | `python tools/check-revit-gate.py --list reporting` |
| **3** | **The preview selection** ([D-60](DECISIONS.md)) in [`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs). Select `preview.Ids` and `preview.Skipped`, **500 per set** as a setting's default, save the modeller's own selection first and put it back — except on accept | one file | D-60 |
| **4** | **The workflow id across the seam** ([D-61](DECISIONS.md), [D-62](DECISIONS.md)). The add-in mints one per request; the brain never sees one | one seam | below |
| **5** | **The project knowledge scope.** `heron_context` now carries the pinned project's NAME, but `_Open` still always opens the GLOBAL store, so `knowledge scope: global` on every packet and project-specific knowledge can never take part. Changing it touches every brain tool, so it wants a real project store | one class | `_Open` in [`heron_brain.py`](../mcp/server/heron_brain.py) |

**Item 4 is small and it unlocks three things at once**, which is why it is worth doing early rather
than last:

- the utterance cache **fills** — `remember()` already refuses anything but a completed run, and the
  only reason nothing calls it is that no run result reaches the brain
- [`measure-routes.py`](../tools/measure-routes.py) gets its **LIVE** half instead of only the
  structural one
- [`brain/heron_audit.py`](../brain/heron_audit.py) can finally claim `HERON-MCP-LOG-010`, whose row
  says *keyed by Workflow ID* and is the only reason its header still says `Heron-Agent: none`

### HOW THIS TRACK ENDED, AND WHAT A FRESH SESSION SHOULD DO FIRST

**PR #44 was merged on 2026-09-09** (`fd1df12`) after a Codex review. There is no open branch. **Start
from `main`.**

**The first five minutes of the next session, in order:**

1. `python tools/check-gaps.py` — it is computed from disk and this file is typed by hand. **Believe
   the tool.**
2. `python tools/check-docs.py` — it derives every count claimed in prose and fails on drift. It caught
   four stale counts during this track, one of them written in the same commit that broke it.
3. Read the five queued items below. **Not one of them is blocked on a decision** — every rule they need
   is written and already checked by a gate.

**What this track did NOT touch, so nothing here changes the proving work:** no fragment status moved,
`D-30` is untouched, no Golden Rule changed, `write.enabled` is as it was, and nothing built here has
been near Revit. The PROVING track entries below are unaffected and still supersede this one on method.

### AN AUTOMATED REVIEW FOUND NINE THINGS AND ALL NINE WERE REAL

PR #44 was reviewed by Codex on 2026-09-09. **Nine findings, nine confirmed against the code, all
fixed.** Worth reading as a class rather than a list, because seven of the nine are the same mistake:
**a thing declared and then not finished** — and every one of them passed all three gates and the whole
test suite.

| | What it was |
|---|---|
| **The worst one** | **`.claude/skills/heron-guard/bin/heron_guard.py` was not in the repository at all.** `.gitignore`'s `bin/` rule — there for .NET build output — swallowed it. The SKILL.md was committed, the executable it advertises was not, so on any fresh clone the hook would have failed before making a permission decision. **It passed here because the file was on disk.** `.gitignore` now re-includes `.claude/skills/*/bin/*.py`, and the directory has to be re-included before the file or git will not look inside it |
| **A crash nobody hit** | `measure-graph.py` returned a bare `[]` where the caller unpacks `ids, known`. It never fired in the recorded run — but `--revit 2019` empties the library at the version wall, so **the one setting most worth measuring was the one that raised `ValueError`** |
| **A measurement that would have lied** | The same tool's answer key was not filtered by `--revit`, so a release-specific re-run would score a fragment's correct absence as a miss. **The recorded Q-52 numbers are unaffected** — they were run with no `--revit` and therefore no wall — but a re-run would not have been |
| **A hole in a brand-new gate** | `check-licence.py` counted a source-less **skill** as clean, from a restriction left over from when `.claude/skills` was still scanned. All ten `brain/skills/*.yaml` declared no source, so an imported skill with no licence would have passed the checker whose whole promise is that unmarked and clean are different findings. The ten now declare `source: OFFICIAL` |
| **Two of four unwired** | `heron_audit.context()` and a catalogue recorder were written and never called, so `heron_context` and `heron_capabilities` left no trace. *"The brain side of the trail"* was half the brain |
| **Correct refusals counted as failures** | Every `ok: false` went out with **no error code**, and `heron_gaps.analyse()` files those under `(none)` in `unclassified`. That is `heron_gaps.py`'s own founding mistake repeated. Three codes now exist and classify as correct refusals |
| **A claimed metric that was a different number** | `measure-routes.py`'s LIVE section counted **cache rows** — inventory, not route frequency — while D-62 existed to make the live share readable. It reads the trail now |
| **The packet was quietly less true than it could be** | Every context packet said `project: none named` while the add-in had known the pinned document's name since the first `count_elements` |
| **The generator lost its imports exactly when it needed them** | `heron_context` refused the API surface along with the neighbour when nothing matched — but `_api_surface()` takes no arguments and reads the executor's import list. **The novel request, which most needs telling what it may assume, was the one told nothing** |

**One half is deliberately not fixed and is queued instead.** The same finding asked for the project
**knowledge scope** to be passed as well as the name. `_Open` always opens the GLOBAL store, so that
changes how *every* brain tool resolves knowledge rather than one — and it wants a real project store to
test against. **Item 5 on the PC list.**

**The lesson, and it is not "run a reviewer".** Every one of the nine passed `check-docs`,
`check-metadata`, `check-structure` and every test suite that runs in this container. **A gate
checks what somebody thought to check**, and seven of these were written by the same person who
wrote the gate an hour earlier.

### Two things that must be re-checked on the PC before item 1 is called done

**Nested links.** [D-59](DECISIONS.md) does not say whether `linksSearched` counts a link inside a
link. It is a real Revit case and **the first implementation answers it against a real federated
model**, not from here. Write the answer back into D-59 when you have it.

**Item 2 moves house when it is finished.** The rule lives in
[`check-revit-gate.py`](../tools/check-revit-gate.py) today because putting it in
[`heron_fragment.validate()`](../brain/heron_fragment.py) now would make 59 fragments invalid and fail
every gate in the repository on a library that is not broken. **Moving it into `validate()` is how the
worklist is declared finished** — do that, do not just close the list.

### What is new in the tree, and what each is for

| | |
|---|---|
| [`tools/check-licence.py`](../tools/check-licence.py) | **Exits 1 on a finding**, unlike the other reports. Reads the FILES, not the landing page ([D-66](DECISIONS.md)). 370 units, all clean today |
| [`tests/test_licence_check.py`](../tests/test_licence_check.py) | Proves that checker **fires** — a clean run on a clean library proves nothing |
| [`brain/heron_audit.py`](../brain/heron_audit.py) | The brain's half of the trail ([D-62](DECISIONS.md)). Writes `audit-brain-YYYYMM.jsonl` beside the add-in's file; `heron_gaps.read()` already merged by `at`, so **no C# and no reader changed**. Never writes the user's sentence |
| [`tests/test_carried_sources.py`](../tests/test_carried_sources.py) | The `Q-51` tripwire. Fails the day Heron first carries text it did not write |
| [`tools/measure-graph.py`](../tools/measure-graph.py) | The `Q-52` measurement — six settings, six losses |
| [`.claude/skills/heron-guard/`](../.claude/skills/heron-guard/) | A PreToolUse hook. Deny-tier, fails closed, `HERON_GUARD=off` to disable |
| [`.claude/skills/heron-ship/`](../.claude/skills/heron-ship/) | What to run before pushing, and which failures are the machine rather than the change |

### The three test failures are the machine, and they have TWO causes not one

`tests/test_mcp_serves.py` and `tests/test_served_claims.py` need the **MCP SDK**.
`tests/test_bridge_roundtrip.py` needs a **built .NET test host**. **37 of 40 pass here**, and on a
machine with both, all 40 should. Do not "fix" them by editing the tests.

Six checks also need tools this container has not got: `check-compile`, `check-fragments-compile` and
`check-api-surface` need `dotnet`; `check-routing` and `check-intrusion` need `HERON_KNOWLEDGE` set.

### One measurement worth carrying, because the slow version looked fine

`remember()` first called `FRAG.load_all()` — reading all 360 fragment files to use one — putting
**1,522 ms** on a path a modeller waits on. The store already knew the folder. It is **5.5 ms** now.
**Nothing about the slow version looked wrong**: `load_all()` is what `index()` calls, it was already
imported, and the cost only appears if somebody times it. Time the thing on the waiting path.

### Still needing one line from the owner

**§10.15 of [33](33-external-repository-research.md), "Claude CEO"** — the repository was never
identified. A link, or drop the row. It is the only entry in that document that names nothing.

---

## HANDOVER — 2026-09-09, the second sitting: 142 → 159, and three capabilities that already existed

**READ THIS FIRST IF THE SCRATCH MODEL IS STILL OPEN.** `Snowdon-scratch_ajmal.al` has been written to
all day and **at least one rollback did not hold** (see below). **Close it without saving.** Nothing in
this entry is worth a damaged model, and every proof recorded here was taken on a rolled-back write, so
none of them needs the file kept.

**Revit was left with a dialog open or a command running** — every request answered *"Revit is busy"*
for the last half hour of the session, and before that a fifth chat held the lease. Both are worth
clearing before the next session starts.

### What moved

| | |
|---|---|
| Proved | **142 → 159.** Eighteen signed, one (`set-view-section-box`) correctly demoted by a parallel session when its code changed under its proof |
| Job files | **14** in `tools/jobs/`, each carrying the reasoning for its arrangement in comments |
| Branches | **`main` only.** Everything merged: PRs #48, #52, #55, #56, #58, #59, #61. Nothing parked |
| Gates | check-docs, check-metadata, check-structure, bridge-roundtrip — all green on `main` |

### The thing worth carrying forward, said once

**Three separate capabilities existed, were already `PROVEN`, and were invisible — all in one day.**

1. **The `list-*` fragments as a setup chain.** `list-sheets` → `set-selection` arranges all 17 sheets
   in one step. Five fragments proved on that route within an hour of finding it. `list-levels`,
   `list-grids`, `list-revisions`, `list-linked-models` do the same for their kinds.
2. **`views (IList<View>)` resolves from a single name.** D-54 fills a list from one value.
3. **Fifteen selectors narrower than a category** — `select-by-family`, `select-by-connection-status`,
   `select-types`, `select-by-parameter-value` and twelve more. **Every job file written that day used
   `select-by-category-name` and nothing else**, and four fragments refused because they were handed a
   whole category when they needed one family or exactly two runs.

Each time the fragment was fine, the library was ahead of the job files, and the cost was hours. **This
is the strongest argument for §3h.4 — generating the job file from the contracts — and much stronger
than the mistyped field names it was first written about.** A generator reading `contract.needs` would
have offered `select-by-family` to a fragment that renames a family.

### Two defects found in the tooling, both about evidence

**`batch-prove` reports verdicts on runs that never happened.** Six jobs came back `POSITIVE EMPTY`
against records from an *earlier Revit session* describing a *different arrangement*. Heron behaved
correctly throughout — it refused the lease and declined to write a record about *"a model that will
not name itself"* — and the runner judged the file already on disk. A date check does not catch it;
both records say `2026-09-09`. The session id in the `model` line does, and `validate` already stamps
it. **OPEN.**

**A tracking harness read the wrong fragment's block.** `prove` prints one provides block per chained
fragment, and `elements` is declared by the selector as well as by the fragment under test. Reading the
selector's copy produced five rows that tracked the input perfectly — because they *were* the input.
`find-untagged-elements` was promoted on that evidence and had to be reverted and re-proved. Fixed with
two guards, and the second is the one that matters: **refuse to write rows unless one agrees with what
`validate` recorded for that fragment alone.**

> Rows that match the input exactly are the thing to distrust. A real describer's answer *differs* from
> what it was given — 22 ducts, 14 untagged.

### ONE PERSON, ONE `HERON_CLIENT_ID`

The lease identifies a **chat**, not a person. Four ids were in use for one afternoon — one by hand,
two by scripts, one the runner picks itself — and to Heron that is four chats competing for one Revit.
**Every refusal reads exactly like a fragment failing.** It cost two batches before it was understood,
and it explains an eleven-job wipe-out earlier the same day that was written up as an arrangement
fault.

### The rollback is not reliable, and size is not the boundary

§1c said small writes roll back correctly. **That is now known to be false.** A parallel session renamed
17 sheets inside a transaction with no `apply`; the rollback did not hold and `list-sheets` afterwards
still read `M000 HERON Cover Sheet`. Seventeen sheets, one rename, no cascade, no dialog.

**Every write proof signed today carries the sentence** *"run inside a transaction and ROLLED BACK, so
the model was left exactly as it was"* — and that sentence comes from the `writing` flag in the bridge
client, **never from anything Revit returns**. It is not evidence. §5 row 10 is the standing record.

### Where the proving goes next

1. **Name what already exists.** Nothing to build — the three capabilities above need to reach whoever
   writes the next job file. Cheapest item on the list and it outranks the rest.
2. **`LIST_*` fragments**, §3d. Five fragments could not be arranged for want of a name nothing can
   supply: a workset **id** (`list-worksets` gives names only — one field closes it), a view carrying
   no template, a legend name, a route to a section mark, a material name.
3. **A resolver for named Revit objects** — `FamilySymbol`, `Material`. Half the never-run MODIFY
   fragments wait on it.
4. **A way to bind two sets of elements**, which no job file can express today.

**Making silence illegal (§3h.1) still comes first.** Everything above makes proving faster; that one
makes Heron honest — and twelve fragments still answer `0` where `duplicate-sheets`, `edit-revision`
and `remove-parameter-value` say plainly what went wrong. The best line written by any fragment all
day was a refusal:

> *"0 cleared, 22 already empty. An empty field and a zero are different."*

---

## HANDOVER — 2026-09-09 (the PROVING track, day two): 52 → 142, and Heron can now change a model

**Ninety fragments proved in one day, against `Snowdon Towers Sample HVAC` in Revit 2024 on the owner's
PC.** Two things were built to make that possible and both are bigger than the day's count: fragments
can now be handed **the caller's half of their inputs**, and fragments that CHANGE a model can now
**run at all**. Before this morning, **287 of the 360** could not be given so much as a category name,
and the **184** carrying `risk: MODIFY` could not execute a single line.

**If you are here to prove more, read [the method](#the-method-that-made-it-fast--this-is-the-part-that-transfers)
and [`docs/FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md) first.** The issues file is the sit-down list — 107
rows of what failed, what is missing, what should be split and what should be edited. It is not a bug
list; it is the work queue.

### What was built

| | What it does | Why it mattered today |
|---|---|---|
| **Caller values** (D-54) | `--set categoryName=Ducts` crosses the bridge **as text** and is resolved into a `View`, `Level`, `Category` or `BuiltInCategory` **inside Revit**, where the document is | **287 fragments** declare a need the selection cannot fill — 675 such needs in all. It refuses `XYZ` and `ElementId` **by name**, with a reason, rather than half-resolving them |
| **The write engine** (D-55) | `run_fragment_write` — a second operation, `MODIFY` in the registry, wrapping the run in a `TransactionGroup` that is **assimilated only on `apply=true`** and rolled back otherwise | **184 fragments carry `risk: MODIFY`** and not one had a path to execute. A preview is now the run itself, undone — nothing is simulated, so nothing can lie |
| **Risk by name** (Golden Rule 19) | The caller says which fragment; **the registry says what it costs.** `--write` on a `READ` fragment is refused | A caller that could declare its own risk could declare `MODIFY` work to be `READ` |
| **`tools/batch-prove.py`** | Runs 10–20 fragments per pass and hands back only the failures | One-at-a-time was the real bottleneck, and the failures only make sense **read together** |
| **`tests/test_caller_values.py`** | 21 cases over the resolver, **no Revit needed** | The resolver is the piece most likely to rot silently |

### The method that made it fast — this is the part that transfers

Four things, in the order they were learned. All four are written up properly in
[`docs/FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md), under *How to arrange a case*.

1. **Never select by hand. Chain the fragments.** The owner asked *"why do I need to select — you have
   fragments for selecting, use that"*, and he was right. `select-by-category-name` → `set-selection`
   as a `--setup` chain arranges every case, repeatably, and it is faster than clicking.
2. **Small selections.** Also the owner's: *"try with a small number of ducts, like 2 or 3."*
   `FloorPlan: M1` has **22 ducts**; `FloorPlan: L3` has **307**. Two proofs that had been failing on
   time alone passed immediately on the small view. A slow case does not prove more than a fast one.
3. **Get the positive and the negative the right way round** — this was got **WRONG FIVE TIMES** in one
   day, which is why it is now rule #2 in the arranging guide. The POSITIVE is the case where the
   fragment has something to find. The NEGATIVE is where it must come back empty. Writing them the
   other way produces a run that passes and means nothing.
4. **Read the positive, not just the negative.** `heron_validate` judges whether the negative came back
   empty. A fragment that does **nothing at all** satisfies that trivially. Every silent failure found
   today was caught by reading the positive by hand, so `batch-prove.py` now checks **both halves**.

### The two model wipes — read this before you run a write

**The model was destroyed twice today, and both times it was the choice of test data, not the code.**
`transfer-views` over 60 elements, and `delete-elements` over the 5,636 elements under `Levels`. In
both cases **the rollback did not fully undo the write**, and the model was recovered only by closing
it **without saving**.

> **The boundary is not known.** Small writes roll back cleanly — dozens were proved today. Something
> between 22 elements and 5,636 does not. Nobody has found where, and **guessing at it is worse than
> saying it is unknown**, which is why §1c of the issues file says so in those words and
> `delete-elements` is marked BLOCKED rather than untested.

Until it is understood: **write against small selections, on a scrap model, and never save.**

### Ten defects, and where they came from

| Found in | What it was |
|---|---|
| The chain | A fragment's own output was overwriting the value handed to it, **matched by NAME**. Fixed by identity — `ReferenceEquals` against what was handed in. It hit 4 filter fragments, not "every filter fragment" as first claimed |
| `validate` | Was sending **writes down the read path**. A lost patch. It first read as *"intermittent worksharing behaviour"* and was written up as such, and only `prove` — which has no write path — failing identically exposed it |
| The batch runner | Passed a fragment whose only real result was **0 in both legs**, because an unreadable value counted as work. *"I cannot read this"* and *"this is a result"* must not collapse |
| The batch runner | Counted **accounting fields** as results — `check-flow-direction`, `remove-parameter-value`, `set-mep-slope`. `role: result` vs `role: accounting` exists for exactly this, and 40 fragments still do not declare it |
| The batch runner | Re-proved **15 of 16 already-`PROVEN`** fragments in its first run, because the names came off a capability list and nothing filtered by status |
| `Describe` | Renders a **valid and an invalid `ElementId` identically**. Two proofs are blocked on this and cannot be arranged around it |
| Schedules | The `Schedule Graphics` door was found — a schedule can be selected without clicking it — but **eight schedule fragments never got the fix** and fail silently |
| `--setup` | **Cannot run a step that writes.** So any case needing a written setup cannot be arranged at all |
| The deploy script | Deployed **.NET 10 binaries into Revit 2024**, which refuses them with *"Revit cannot run the external application"*. `check-compile.py` builds 2020–2027 into one folder and the newest wins. Now guarded in `deploy-addin.ps1` rather than left to memory |
| The CLI | Every invocation was a **new chat** taking a five-minute lease, so the second call was always refused. `HERON_CLIENT_ID` fixes it, set once |

**The activity banner was NOT one of them.** A whole morning was lost to `switch-active-project`
appearing to hang; a fix was guessed at, did not help, and **was reverted**. Another session then
proved the cause was the banner's own threading. *The fragment is innocent* — and reverting the guess
is the reason that could be established at all.

### What tomorrow starts with

1. **The sit-down list.** [`docs/FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md), and specifically §3h — the
   four improvements, already ranked. Do **§3h.1 first**: make silence illegal. A fragment that returns
   nothing and reports success is the failure mode that has cost the most time, twice over.
2. **The eight schedule fragments** that never got the `ScheduleSheetInstance` fix.
3. **The rollback boundary.** Find where it breaks, on a scrap model, deliberately — it is the only
   thing standing between the write engine and a real project.
4. **Then keep proving.** 219 `DRAFT` remain, and roughly 100 of them cannot run for reasons §6 of the
   issues file lists by cause.

---


## HANDOVER — 2026-09-09 (the REFUSAL-PROOFS track): silence made illegal, and eight of it proved

**To carry this on, say:** *"Read the REFUSAL-PROOFS track entry in HANDOVER.md."*

§3h.1 of [`FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md) asked for one thing: **a fragment handed something
it cannot use must REFUSE and say why, never report `0`.** Twelve fragments were changed (PR #50),
eight have been run against a model with both legs (PR #67), and §3j holds the numbers.

### What is DONE

| | |
|---|---|
| **12 fragments refuse** | `color-by-parameter`, `measure-run-quantities`, `edit-text-values`, the seven schedule ones, `add-revision-cloud`, `set-view-section-box` |
| **8 proved, both legs** | signed by Ajmal PS and promoted — the library is **167 proven, 193 to go** |
| **9 new `provides`** | seven `refused`, two `refusalReasons` — §3h.1 was wrong that every fragment already had one |
| **Model unharmed** | 9,628 elements before and after, unsaved-changes marker cleared |

**The seven schedule fragments now resolve a `ScheduleSheetInstance`**, so §3e's door is open for all of
them, not just the two READ ones. `add-schedule-fields` answered `handed 3, schedulesSeen 3` and handed
back 256 real field names.

### What is NOT done, in the order it is worth doing

**1. THE EIGHT ARE DONE — signed and promoted.** Ajmal PS accepted all eight on 2026-09-09, so each
carries a `proof:` block with his name, the model and session, both cases verbatim and a fingerprint;
`heron-status` was then moved to `PROVEN` as the separate act `accept` reserves for a person. **The
library went 159 → 167 proven, 201 → 193 to go.**

Each proof records the gap *no second route was run* — D-30's *"where one exists"* clause, undecided
rather than failed. Anyone re-reading these should know that is what the signature stood over.

The drafts are gone: `accept` consumes them as it records, so each fact has one home rather than two.
The commands, for the next batch:

```bash
python brain/heron_validate.py review <fragment>
python brain/heron_validate.py accept <fragment> --by "Ajmal PS"
# accept does NOT promote - it says so on every run. Afterwards:
sed -i 's/^heron-status: DRAFT$/heron-status: PROVEN/' brain/fragments/<fragment>/fragment.yaml
```

**2. `edit-text-values` HAS NEVER RUN — not one leg.** `Text Notes` is not selectable on the sheet
`Notes, Symbols & Schedules`, so the seven notes §3c found live in some other view and **nothing in the
library lists which**. §3d's `LIST_*` gap is the only thing between it and a proof; the refusal path is
written and compiles. **Its draft says `NOT ESTABLISHED` for both legs and must not be accepted.**

**3. Two were skipped by decision, not failure.** `export-schedule-to-csv` is `risk: PUBLISH` and the
client refuses to send above Modify (§5 defect 9). `add-revision-cloud` wants `revisionId` as an
`ElementId`, which D-54 does not resolve — the same `ElementId` wall the caller-inputs track names as
its next blocker.

**4. `set-view-section-box`'s proof is stale and still says `PROVEN`.** It leaves a `refusalReasons`
output it did not have when fingerprint `49c749930e4bed33` was taken. Nothing it recorded is
contradicted, and §3f's stronger negative — a 3D view whose selection has nothing to enclose — is now
worth running, because it finally reports something different from the weak one.

### THE MARGIN GUARD HAS COME BACK ONCE. IT MUST NOT COME BACK AGAIN.

The owner had the inside-out-margin guard removed from `set-view-section-box` before #50 merged. The
CALLER-INPUTS track dropped it correctly in #55 and wrote *"it must not be re-added"* in this file —
and then **a later merge (#61) put it back anyway**, carrying a real `enclosed = 0;` fix inside it.
Removed again here.

**The lesson is not "be careful".** It is that a commit dropped from one branch survives on every other
branch that already had it, and git will not flag it: the rewritten copies have different patch-ids, so
`--cherry-pick` reports nothing and a merge reads it as new work. **Check by content, not by history** —
`git diff main...HEAD | grep` for the thing that was supposed to be gone.

**AND THE SAME MERGE LEFT MAIN NOT COMPILING.** `set-schedule-filters` ended up declaring `refused`
**twice** — both branches added the same entry and the merge kept both — so
`tools/check-fragments-compile.py` failed on all eight releases with
`CS0128: __provides_refused is already defined`. Found by running the gate while closing this session,
not by anything that watches. Fixed here, and the whole library was swept: **that was the only one.**

> A merge can produce a fragment that no longer compiles without either side being wrong. **Run the
> compile gate after a merge, not only after an edit.** `tools/check-*.py` all passed while this was
> broken — none of them reads a contract for duplicate names.

### Three things about ARRANGING a proof, each learned by getting it wrong

**1. `open-view` cannot be a setup step.** Setup runs every step down `run_fragment_read`, and
`open-view` is `risk: EXECUTE` — step 0 is refused and **every leg of every job fails**, which reads as
a broken arrangement. Adding it made a batch of seven *worse* than leaving it out. Run it as its own
`validate` first; give the negative a view name that does not exist and the positive's view stays up.

**2. Both legs must select in ONE view, or the arrangement depends on what is on screen.** Selection is
view-scoped. A positive on a sheet and a negative in `FloorPlan: M1` works only until an earlier job
leaves the wrong view open — then six jobs fail at setup with no hint that the *view* is what moved.
Find the contrast inside one view: `Schedule Graphics` against `Title Blocks` on the same sheet.

**3. One Revit, one session — and it never says so.** §3i already records a stale lease reporting
`Could not identify the active model`. Two more shapes of the same collision, both met here:

- **`ping` succeeds while every real request times out.** The bridge answers on its own thread; the
  Revit API thread is what is busy. `doctor` showing `ping ok` does **not** mean Revit is available.
- **The lease countdown goes UP.** Each refused attempt counts as activity and renews the very lease it
  is waiting on. Stop touching it and wait, or stop the other session.

**And the MCP tools and the command line hold DIFFERENT leases.** One `revit_use_this_model` from a
chat takes the lease under that chat's id; the CLI pins its own and is refused; `release` cannot hand
back a lease it does not own; and the MCP server's id is a fresh uuid per process, so it cannot be
matched. **Pick one and stay on it for the whole run.**

### Two facts about the machinery worth not rediscovering

- **`write.enabled` is a file, and it is `false` at rest.** `%APPDATA%\Heron\config\heron.config`. It
  takes effect immediately — nothing to restart — and every MODIFY proof needs it `true`. Set it back
  afterwards; Heron's own health check says so.
- **Proof drafts are gitignored on purpose** (`.gitignore:153-154`). They cannot be committed and should
  not be. `accept` writes the `proof:` block into the fragment and deletes the draft, and it leaves
  `heron-status` alone — promoting is a separate, deliberate act.

### Where the work is

| | |
|---|---|
| PR #50 | the twelve refusals |
| PR #67 | §3j — the eight proofs, and the three arrangement findings |
| PR #69 | the close-out: the duplicate `refused` that stopped main compiling, and the margin guard removed a second time |
| `tools/jobs/reprove-refusals.yaml` | the schedule arrangement, on the sheet |
| `tools/jobs/measure-only.yaml` | the duct arrangement, in `FloorPlan: M1` — two files because of finding 2 |

---

## HANDOVER — 2026-09-08 (the fragment-review track): ten new fragments, none proved

**READ THIS FIRST IF YOU ARE SITTING DOWN AT THE PC.** Ten fragments were added today and **not one of
them has been near a real model.** They compile on all eight releases, which proves only that the API
surface agrees. Everything below is what to test, and roughly in what order.

### What was added, and what each one needs from you

| Capability | Fragment | Risk | The one thing to check |
|---|---|---|---|
| `REPORT_OPEN_DOCUMENTS` | `report-open-documents` | READ | Two projects open with the SAME title. Both must be listed with their paths, and the collision said out loud |
| `SWITCH_ACTIVE_PROJECT` | `switch-active-project` | EXECUTE | It only REQUESTS the switch. Revit performs it after the operation ends, so check the tab bar afterwards - and never chain a write onto it assuming it landed |
| `OPEN_VIEW` | `open-view` | EXECUTE | Ask it for a VIEW TEMPLATE by name. It must refuse, not fail obscurely |
| `CLOSE_VIEW_TABS` | `close-view-tabs` | EXECUTE | Ask it to close EVERY open tab. It must keep one - the ACTIVE one - because closing the last view closes the project |
| `SELECT_BY_CONNECTOR_SIZE` | `select-by-connector-size` | READ | An exact 230 × 230 selection, then hover a match and read the size off the connector. This is the fragment most exposed to a units error |
| `PLACE_FAMILY_ON_FACE` | `place-family-on-face` | MODIFY | Place on a ceiling, then MOVE THE CEILING. If the instances stay behind they were never hosted, whatever it reported |
| `CREATE_FLEX_DUCT` | `create-flex-duct` | MODIFY | A route over the length limit must create NOTHING. Then TRACE_CONNECTIVITY: the diffuser must still read as OPEN, because this makes flex and does not join it |
| `CREATE_ELECTRICAL_CIRCUIT` | `create-electrical-circuit` | MODIFY | **Run it TWICE on the same devices.** What Revit does then is the one thing that was deliberately not guessed at |
| `PROPOSE_MEP_OPENINGS` | `propose-mep-openings` | READ | A duct crossing a wall at 45°. The through-thickness must read LONGER than the wall - that is what the solid intersection buys over a bounding box |
| `DISCONNECT_CONNECTORS` | `disconnect-connectors` | MODIFY | Count the elements before and after. The count must be IDENTICAL - it disconnects and deletes nothing |

### What else changed today

- **`HANDOVER.md` and `NEEDS-CHECKING.md` moved into `docs/`.** The root now holds entry-point files
  only. Every link was repointed, including the one place that READS the register rather than linking
  it (`tools/check-gaps.py`).
- **Two real defects fixed in existing fragments.** `check-model-standards` treated a pattern with no
  wildcard as a PREFIX, so the rule `Supply Diffuser` accepted `Supply Diffuser OLD` - every exact rule
  in every project was silently a prefix rule. `import-parameter-values` read the file line by line
  before parsing it as CSV, so an Alt+Enter cell became three rows.
- **Stale documentation corrected.** `brain/README.md` claimed thirty-two fragments all `DRAFT` and
  that the executor was not built. `CONTRIBUTING.md` said there was no implementation yet.

### The thing worth carrying forward

**Four mistakes were made today and reading the code caught none of them.** The compiler refused
`ElectricalSystem.Create` with the wrong collection type; `heron_graph.py --orphans` found a fragment
nothing could ever reach; and a deliberate second pass over the finished diff found one fragment
answering an empty list to a question nobody asked, and another doing two hundred times the geometry
work it needed. The tools earned their keep, and so did re-reading the diff after believing it was
done.

---

## HANDOVER — the session that ran 2026-09-07 into 2026-09-08 (the PROVING track)

**16 `PROVEN` at the start of it, 52 at the end.** Everything below was done with Revit 2024 open on the
owner's PC, mostly on `Snowdon Towers Sample HVAC`. **The method is the reusable part and it is at the
bottom of this entry — read that first if you are here to prove more fragments.**

### What was proved, and the one thing that made it fast

| | |
|---|---|
| The activity banner (D-50) | **B6, B7, B8, B9, B10, B11, B13 all pass.** It had never been seen on a screen; it has now, in every colour |
| The write path | **Was structurally broken. Fixed, and the first write Heron has ever made ran through it** |
| 36 fragments | Proved to *run* against a real model — most for the first time |
| 36 more | **Proved outright**, 16 → 52 |
| One new fragment | `FIND_DATES_IN_VIEWS` — written and proved the same night |

### THE FIVE BUGS, because each cost real time and each is the kind that returns

1. **The write path could never have worked.** `revit_apply_move` returned *"Missing or wrong token"*.
   One JSON key, `token`, meant the SESSION token to `BridgeServer.Dispatch` (line 349, checked first)
   and the APPROVAL token to `RevitWrite` (line 103). `body.update(op_args)` let the second overwrite
   the first, so every apply was refused as unauthenticated. **No value satisfied both.** The approval
   is now `approvalToken`, and `op_args` refuses `op`/`token`/`client` outright rather than silently
   winning. Nothing but a live Revit could have found it: both sides compiled, both read a string
   called `token`.
2. **`looks_empty` could never return True.** The executor renders everything as text, so a count of
   zero arrives as the STRING `"0"` — one character, therefore "not empty". **Every negative case was
   flagged, whatever it returned.** A warning that always fires is furniture.
3. **`read-space-loads` threw on the only selection it exists for**, with Revit's own `Not Computed!`.
   Its `noLoad` handler already said *"the space is unbounded and has no volume to load"* and could
   never be reached — the property read threw first. **A guard placed after the thing it guards
   against.**
4. **Both schedule fragments refused the only object a person can select.** They demanded a
   `ViewSchedule`; clicking a schedule on a sheet gives a `ScheduleSheetInstance`. A view cannot be
   selected as an element, so they were correct in isolation and **unusable in practice**.
5. **A vendor namespace inside `brain/`**, from fix 3. `check-structure` refuses it and was right to.
   It greps the file TEXT, so a **comment** naming it fails too.

### Three decisions, and one of them replaced the guessing for good

- **[D-51](DECISIONS.md)** — a negative case is judged by its COUNTS, not by whether the fragment
  went silent. `findings` is prose; 134 of the 350 provide one.
- **[D-52](DECISIONS.md)** — a count of what was TURNED DOWN is not a count of what was FOUND.
  `noSystem: 16` means sixteen were examined and none had one, which is **stronger** evidence than
  silence. Amended the same evening to cover work counters (`scanned`, `jointsChecked`).
- **[D-53](DECISIONS.md)** — a fragment that CANNOT come back empty is proved by **tracking**:
  `count-elements` returned 28, 16, 4, 12, 37 against selections of 28, 16, 4, 12, 37. Eleven fragments
  went through on that.

**FOUR RULE CHANGES IN ONE EVENING, ALL IN THE SAME DIRECTION, AND THAT WAS SAID OUT LOUD AT THE TIME.**
The fifth was refused. What replaced it is the fix that matters: **`role: result | accounting` on a
`provides` entry**, so a fragment DECLARES which outputs are findings and which count what it was
handed. It landed optional, with the naming patterns kept as fallback so that 349 fragments did not
have to be edited at once.

**They were all edited on 2026-09-09, and it is REQUIRED now.** All 1,192 provides declare a role,
read off what each fragment is FOR rather than what its output is called - and that reading
disagreed with the patterns **166 times, in both directions**. `select-unenclosed-rooms` declares
`unplaced` and `unenclosed`, the two faults it exists to find, and `REJECT_NAMES` swallowed both;
`set-mep-slope.inGroup` is bookkeeping no pattern could see, and a non-zero one would have banked a
proof for a run that moved nothing. So `REJECT_PREFIX`, `REJECT_NAMES` and `WORK_COUNTER` were
deleted rather than kept as a fallback that is wrong one time in seven, and `check_contract` now
refuses a provide with no role. `findings` is the one exemption. **Nothing reads a name any more.**

### The method — this is the part to reuse

**TWO CONTRASTING SELECTIONS, NEVER SELECT-THEN-CLEAR.** Clearing the selection does not give an empty
answer: `elements` becomes unbound and the executor refuses, which proves nothing. Select ducts (the
positive for MEP fragments, the negative for room fragments), then spaces (the reverse). **Five
selections — ducts, spaces, walls, sheets, equipment — carried most of the library.** Sheets are the
best negative in it: no length, no volume, no level, no routing.

**THE ARRANGEMENT IS THE WORK; RUNNING TAKES SECONDS.** On a model whose content is LINKED — which
Snowdon Towers is — the thing must be **drawn in the host**, because the executor skips linked
documents by design. Five proofs needed something built: host walls, a ceiling, a curtain wall, a Join
Geometry join (walls merely touching do NOT count), and a reference section (a plain section selects
the *view*, not the marker).

**THREE KINDS OF FRAGMENT, THREE PROOF SHAPES:**

| Kind | Proof | Cost |
|---|---|---|
| Reporters | Two selections, one with the thing and one without | Cheap |
| Structure reporters — `count-elements` | Can never be empty; **tracking** (D-53) | Cheap |
| **Defect-finders — 34 unproven** | The fault must be BUILT on purpose | Slow, one per fragment |

**A DEFECT-FINDER CANNOT BE PROVED ON A CLEAN MODEL.** `check-flow-direction` was rejected four times
before equipment showed `jointsChecked: 57` — it works, there is simply no fault in Snowdon Towers to
find. Its positive needs two connectors both set to `Out`, built in the Family Editor.

**A THROWAWAY FRAGMENT IS THE FASTEST DIAGNOSTIC HERE.** The executor runs any read-only C# handed to
it, so *"what does Revit actually see"* is answerable in about a minute without building anything. It
is what found the date sitting in a text note as `09-09-2026`.

### What is left, honestly

- **Every proof says `second_route: NOT ESTABLISHED`.** All 52. D-30 asks for one *"where one exists"*
  and nobody has settled whether one exists for any of them. **That is the largest open weakness.**
- **`read-space-loads` is fixed and UNPROVEN.** Every space in the model returns `noLoad`; a positive
  needs Areas and Volumes on with the analysis run, or a Design Heating Load typed on one space.
- **`read-schedule-contents` is fixed and UNPROVEN** — it needs `maxRows`, a caller's value.
- **278 of 350 still want a caller's half** — a category, a name, a distance. Unchanged, and still the
  largest unlock.
- **The add-in is STALE** and was before this session; nothing here rebuilt it.

## 2026-09-06 — Revit now SHOWS what Heron is doing to it

**The owner's words:** *"there is no visual identification showing if the cloud or the AI is talking to
Revit. I cannot understand what is happening visually from the Revit side."*

He was right, and the gap was structural. Revit showed exactly one thing about Heron — the ribbon
button's connected picture — and that says a **pipe is open**. Nothing about whether anything is
happening right now, nothing about what, nothing about how it ended. **A refusal was invisible from
Revit**: the chat got a sentence and the screen showed nothing at all.

`HERON-REVIT-UI-022` had been reserved for this since the registry was written, deferred with the note
*"waits for Step 6, when something is finally slow enough to need it."* Step 6 shipped and the executor
arrived, so it is built now — [`HeronActivityBanner.cs`](../revit/Heron.Revit.Addin/HeronActivityBanner.cs),
raised and lowered by [`RevitDispatcher`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), decided in
[D-50](DECISIONS.md).

**Three things decide the whole design, and the first is not obvious:**

1. **The banner goes up BEFORE the work is handed to Revit.** Revit draws on the thread it works on, so
   the moment a job starts nothing can be painted. Any design that shows a banner *because a job turned
   out to be slow* can only decide that on the very thread the job already took. There is no
   delayed-appearance option — it is raise-it-first or nothing.
2. **It comes down when Revit truly finishes, not when the caller gives up.** A client that timed out at
   `still_running` has stopped waiting; Revit has not stopped working, and the screen follows the model.
   Two threads can both think a job is theirs to end, so one interlocked flag on the job decides it.
3. **It says READING or CHANGING**, blue or amber, taken from `HeronOperationRegistry` **by operation
   name** (Golden Rule 19). "Something is happening" is worth little to somebody whose real question is
   whether his model is being touched. Nothing on the wire can make a write wear the reading colour.

It also holds the outcome for about 1.4 s afterwards with how long the job took — twelve seconds of
freeze reads as a hang, the same twelve seconds labelled **12 s** reads as a duration.

**IT COMPILES — all eight releases, 2020 through 2027, every project, 0 warnings** (`B5`, closed
2026-09-07). **It has still never appeared on a screen.** `B6`–`B13` in
[NEEDS-CHECKING.md](NEEDS-CHECKING.md) all need Revit open, and `B8` is the one that matters most: if
a write shows the blue READING card instead of amber CHANGING, stop and fix it before using Heron on
real work.

**AND THE COMPILER WAS HERE ALL ALONG.** This was written believing the container had none, because
`dot.net`'s install script is blocked by the egress proxy. **Ubuntu packages it:**
`apt-get install dotnet-sdk-8.0 dotnet-sdk-10.0`, about a minute, and `check-compile.py` runs. The
**10.0** package carries the WindowsDesktop targets and is what builds 2025–2027; 8.0 alone stops at
2024. `pip install --break-system-packages mcp` works too, which takes the suite from 20 passing to
23 of 24. [docs/30](30-compiling-away-from-windows.md) had already established all of this and it
was re-learned from scratch — **a blocked download is not an absent toolchain.** The one test still
failing, `test_fragment_store`, fails identically on the base commit in a clean worktree and is the
fragment library's business, not this branch's.

One new setting, `ui.activityBanner`, declared in both halves of the config and **on by default** —
the only default in that table that is. The rest protect the model by staying off; this one protects
the person by staying on.

---

## 2026-09-07 — the verification pass: 13 proofs re-run, 135 fragments through the real compiler

**Nothing was built. Everything already claimed was checked, and it held.** Revit 2024.3 was open on
the owner's PC with `Snowdon Towers Sample HVAC` (9,628 elements) and, later in the session,
`Project1 work_ajmal.al` (3,410, a workshared local) — both open at once by the end.

| | |
|---|---|
| Recorded proofs re-run | **13 of 13 reproduce** |
| Fingerprints | **13 of 13 FRESH** — no proof has gone stale |
| DRAFT READ fragments put through the executor | **135**. 7 ran, 115 stopped on their own declared inputs, 13 cascaded from one. **0 no-replies, 0 unexplained failures** |
| Contract `provides` checked against what is actually left behind | **20 fragments, 0 broken** |
| Promoted | **`READ_SELECTION`** — 13 → **14** |

### Every recorded number reproduced, and one looked like a regression until its own proof was read

Each of the 13 was re-run and compared against the numbers written in its `proof:` block. Twelve
matched to the digit — 11 levels at the same three negative elevations, 15 grids, 17 sheets with M002
still `(0 views) - EMPTY`, 6 links, 8 external references with 1 unresolved, 145 warnings over 87
elements in 5 kinds, 13 filters against 30 views with 9 unused, 7 unplaced views against 42 placed,
8 views without a template of which 3 are issued.

**`LIST_WORKSETS` came back `workshared false, worksetCount 0` against a proof that says `workshared
true, worksetCount 2`** — and that is not a regression. Its `model:` field says the positive case was
taken on `Project1 work_ajmal.al` and the negative on Snowdon, so Snowdon returning the negative is the
proof reproducing. **Reading the whole proof, not just its positive case, is what stopped a correct
result being filed as a fault** — and when the workshared local came back in front later the same
session, `workshared true, worksetCount 2, closedCount 0` returned exactly as recorded.

**All three of PART 4's cross-checks hold on both models.** On Snowdon: `LIST_SHEETS` 17 and
`LIST_REVISIONS` walking sheets its own way to 17; `REPORT_VIEW_FILTERS` 9 unused and
`FIND_UNUSED_DEFINITIONS` reaching 9 as 8 + 1; `LIST_LINKED_MODELS` 6 and `REPORT_EXTERNAL_REFERENCES`
8, the difference being the two settings references. On `Project1 work_ajmal.al`: 0 and 0, 6 and 6 + 0,
0 links against 2 references — the same two. **The caveat is worth stating rather than counting this
twice: that local was made FROM `Project1`, so it is the same project wearing worksharing, and its
agreement with `Project1`'s recorded negatives (10 filters, 18 views, 6 unused; 2 references; 2 levels;
the template's Revision 1 on no sheets) is reproduction, not a third independent model.**

### The executor was put in front of all 135 DRAFT READ fragments, and the contracts are sound

The compile gate wraps every fragment in ONE assembly with every declared need in scope. The executor
compiles each into its OWN assembly with only `doc`, `uidoc` and `app` supplied — the arrangement that
hid PART 4's first-line bug. So all 135 were sent through it.

**Every single name any of them failed on is a name its own contract declares.** Not one fragment
references a variable its `contract.needs` never promised. That is the READ half of D-29's contract
verified against a compiler rather than by reading, and it means **wiring inputs is now the only work
left there** — there is no hidden mismatch to find first.

> ### THE TRIAGE RULE WAS WRONG FIRST, AND IT REPORTED 13 DEFECTS THAT ARE NOT DEFECTS
>
> The rule was: *a `CS0103` naming a declared need is expected; any other error code is a real defect.*
> It flagged 13. **All 13 are cascades from the missing input, and the rule cannot tell the difference.**
>
> With `services` unsupplied, `foreach (var service in services)` leaves `service` untyped, so `box` is
> untyped, so `box.Max` no longer resolves to `BoundingBoxXYZ.Max` — and the compiler falls back to the
> only `Max` it can see, reporting **`CS0119: 'Queryable.Max<TSource>' is a method`**. Same for
> `comparing.Count` becoming `CS0428`, and `+` on two "method groups" becoming `CS0019`.
>
> **A missing identifier in C# does not stay a `CS0103`.** It propagates into unrelated error codes
> several lines away, on lines that are perfectly correct. Anyone re-running this check will get the
> same 13; they are `audit-mep-openings`, `check-ceiling-coordination`, `check-equipment-clearance`,
> `check-sleeve-size`, `check-surface-fit`, `check-valve-accessibility`, `check-vertical-clearance`,
> `compare-elements`, `probe-around-elements`, `read-ceiling-grid`, `read-room-geometry`,
> `report-coverage` and `report-location`. **Settled by opening the source at the reported line, which
> is the only thing that settles it.**

### What a fragment PROMISES and what it LEAVES BEHIND were compared for the first time

`contract.provides` is a promise; the C# is a separate file. Nothing had ever checked them against each
other. Over the 20 fragments that run with no inputs: **not one fails to leave behind something it
promises.** Nineteen of twenty leave behind MORE — internal working variables reach the caller, and two
of them are delegates (`harvest`, an `Action`; `readable`, a `Func`) that serialize to a type name and
tell the caller nothing. Harmless to D-29's wiring, which matches by name, and worth knowing before
anybody treats the returned key set as the contract.

### `READ_SELECTION` is proven, and its off-screen answer is a finding rather than a pass

Selected 1,053 duct curves through `select_by_category` — a `FilteredElementCollector` query over the
model — then read them back through `uidoc.Selection`: **1,053, with `vanished` 0**. Two mechanisms,
one number. On `Project1 work_ajmal.al`, which has no ducts, both said 0. With nothing selected, both
models returned a clean zero rather than falling back to everything, to the active view, or to the
previous run's 1,053.

**What it does NOT establish is written into the proof rather than left out.** Aimed with `--in` at
Snowdon while the other model was in front, it returned 0 with `active view: (none)` — and whether
Snowdon's selection was genuinely cleared when it lost the screen, or whether a `UIDocument` built for
an off-screen document cannot see a selection at all, **was not established.** The caller cannot tell
those apart from the bare 0 either. **That is the same defect shape `LIST_WORKSETS` names in its own
negative case:** an empty answer and an unanswerable question must not read alike.

### THE MODEL CHANGED UNDER A RUNNING JOB, AND ONLY THE PRINTED TITLE CAUGHT IT

A job started against Snowdon finished against `Project1 work_ajmal.al`, because the owner switched
Revit while it ran. **Nothing failed and no number looked wrong** — the run simply described a different
building. It was caught because the script prints the active document before it does anything, which is
PART 4's scar (*"the answer always names the document"*) doing its job a day later. **Any script that
runs against Revit must print the model it read, every time, even when it is only checking.**

### The six that are still half a proof, and exactly what each one needs

Both open models have **no design options, no global parameters, no closed workset, no painted face,
and exactly the two default phases**. Every one of these ran clean and returned a correct, well-worded
negative; none can be promoted on a negative alone.

| Fragment | Snowdon | Project1 work | Still needs |
|---|---|---|---|
| ~~`REPORT_PHASES`~~ | 2 (Existing, New Construction) | the same 2 | ~~a third phase~~ **— DONE the same day, see below** |
| ~~`REPORT_DESIGN_OPTIONS`~~ | `optionCount 0` | `optionCount 0` | ~~one design option~~ **— DONE the same day, see below** |
| `REPORT_GLOBAL_PARAMETERS` | `globalCount 0`, `allowed true` | the same | **one global parameter** |
| `FIND_UNUSED_GROUP_TYPES` | `0 unused out of 2 walked` | `0 out of 0` | **a group definition with nothing placed** |
| `FIND_UNUSED_MATERIALS` | 56 unused of 460 types | 69 of 566 | **a face painted in a material used nowhere else — which must NOT be reported unused.** That is the defect the fragment exists to guard, and it is still unseen |
| `FIND_UNUSED_FAMILIES` | 94 families, 64 wholly unused | 116, 107 unused | both halves are there; it needs a **second route** — Revit's own Purge Unused list, read by eye |
| `LIST_WORKSETS` *(already PROVEN)* | not workshared | 2 worksets, both open | **a CLOSED workset** — still the one case never seen |

**None of these can be faked and none needs code.** They are minutes of work in Revit on a model that
is already open, and each one closes a fragment.

### Later the same day — the owner made the conditions, and two more fragments are PROVEN

**He created three phases and one design option in `Project1 work_ajmal.al` while the session ran.**
Both predictions in the table above came true, and both fragments are now `PROVEN` — **14 → 16.**

| | Before he touched it | After | Second route, by a DIFFERENT Revit filter |
|---|---|---|---|
| `REPORT_PHASES` | 2 — Existing, New Construction | **5** — Existing, **Phase 1, Phase 2, Phase 3**, New Construction | fragment walks `doc.Phases` (a `PhaseArray`); `OfClass(typeof(Phase))` found the same 5 names |
| `REPORT_DESIGN_OPTIONS` | `optionCount 0` | **1** — `Option Set 1 / Option 1 <primary> [primary] - 0 element(s)` | fragment collects `OfCategory(OST_DesignOptions)`; `OfClass(typeof(DesignOption))` found the same 1 |

**The phase ORDER is what makes that a proof rather than a count.** The three new phases came back
sitting BETWEEN the two the template ships with — which is where inserting after Existing puts them.
A phase list with the right names in the wrong order is wrong, and only the order shows it.

**Both negatives were measured BEFORE he changed anything, on the same build.** That is what ties each
number to a known cause rather than to a reading taken once.

### THREE "CREATE" CAPABILITIES WERE ASKED FOR. ONLY ONE OF THEM CAN EXIST.

The owner asked for fragments that create a phase, a design option and a global parameter. Reflection
against the assemblies **Revit 2024 has actually loaded** — read, not looked up:

| | What the shipped assembly says | Verdict |
|---|---|---|
| **Global parameter** | `GlobalParameter Create(Document, String, ForgeTypeId)` | **buildable — and built** |
| **Phase** | no public static `Create`/`New`, **no public constructor**, `Document.Phases` is `CanWrite=False`, and the only `Document` member mentioning phases is `get_Phases` | **IMPOSSIBLE** |
| **Design option** | no `Create`, no constructor, no `Document` method mentions one — and **`DesignOptionSet` is not a type at all** | **IMPOSSIBLE** |

**Both refusals were already written in this library and are now confirmed on the shipped assembly.**
`REPORT_PHASES`' own routing table says *"create a new phase → NOTHING HERE CAN. Revit exposes no
create call for a phase on any release"*, and `REPORT_DESIGN_OPTIONS` says the same about making an
option active. **A fragment that names its own boundary is the best gap detector in this repository** —
that is now true in both directions: it found the one gap worth building AND it correctly refused the
two that cannot be.

### `CREATE_GLOBAL_PARAMETER` is built — 348 → 349 — and the library named this gap itself

`SET_GLOBAL_PARAMETER`'s routing table has said *"make a global parameter → NOT HERE"* since
2026-09-06 **and routed it to nothing.** Measured on a rebuilt store, every one of those sentences was
being answered by `REPORT_GLOBAL_PARAMETERS` — a question fragment fielding a request to create.

**THE VERSION RANGE WAS MEASURED, NOT ASSUMED, AND THE TWO FAILURES ARE DIFFERENT FAILURES** — the same
shape `EXPORT_SHEETS_TO_PDF` records. All eight releases were declared and the compile gate was run:

    Revit 2020   CS0246 - `ForgeTypeId` does not exist at all
    Revit 2021   CS1503 - `ForgeTypeId` EXISTS, and `Create` still takes the old `ParameterType`

**The 2021 result is the trap, and it is the one a reflection check gets wrong.** Asking whether
`ForgeTypeId` is present answers YES for 2021 and the code still will not compile, because a type being
available says nothing about which overload a call carries. **Presence is not usability** — which is
exactly the limit `check-api-surface.py` states about itself. It declares 2022–2027; all eight releases
are green.

**IT CANNOT BE RUN, AND THAT IS NOT A GAP IN THE FRAGMENT.** `run_fragment_read` opens no transaction,
so Revit refuses any change; running a fragment that WRITES is the operation that does not exist yet.
So this one is `DRAFT`, compiled on six releases, and **cannot reach `PROVEN` until the write path is
built.** It also means it cannot be used to create the global parameter that would finish
`REPORT_GLOBAL_PARAMETERS` — that still needs Manage ▸ Global Parameters ▸ New, by hand.

### A ROUTING REGRESSION WAS INTRODUCED, AND `check-routing.py` DID NOT FIND IT

The checker reported one crossing on the new fragment — `"change the global parameter"`. **That was a
false positive**, exactly as the tool's own preamble warns: it measures one half of the fusion, and
`find` resolves a declared phrasing by identity before any ranking runs. Put through `find`, the host's
real entry point, it answered `SET_GLOBAL_PARAMETER` correctly.

**The real defect was in sentences NOBODY had declared, which is where no checker was looking:**

| Sentence | 348 fragments | 349, first attempt | Why it matters |
|---|---|---|---|
| `"update the global parameter value"` | `SET` | **`CREATE`** | a plain regression |
| `"change the global param"` | `REPORT` | **`CREATE`** | worse than its rank — **a harmless read replaced by an ADMIN write.** `LIST_LEVELS` records the same trade the right way round |

**Fixed by strengthening the fragment that should win, never by weakening the new one** — both are now
declared utterances of `SET_GLOBAL_PARAMETER` and resolve by identity. Two of the new fragment's own
utterances were withdrawn with the reason written down: *"set up a global parameter for this project"*
borrowed **SET's own verb**, and *"I need a global parameter"* carries no verb at all, so it was decided
by the noun — the noun trap, now recorded five times.

**One landing is left and declared rather than hidden:** the bare noun `"global parameter"`, with no
verb, moved from `REPORT` to `CREATE`. It is genuinely ambiguous, the risk direction is the wrong one,
and it is not fixed by declaring a sentence nobody says. What bounds it is written in the fragment:
resolving is not running, `CREATE` is `ADMIN` so the permission gate stands in front of it, and no write
path exists to run it at all.

> ### THE RETRIEVAL STORE DOES NOT DROP A FRAGMENT THAT HAS BEEN DELETED, AND THIS INVALIDATES ANY BEFORE-AND-AFTER TAKEN WITHOUT REBUILDING IT
>
> **The whole fragment directory was moved off disk and the store still answered
> `CREATE_GLOBAL_PARAMETER`.** Indexing is content-hashed and **additive**: it re-reads what is there
> and nothing removes an entry whose file is gone. `_Open()` reports the store as indexed, and it is —
> just not of the library as it now stands.
>
> **Every routing measurement in this file taken by editing and re-running is only as good as the store
> underneath it.** The first "after" numbers in this session were wrong for exactly this reason and were
> thrown away. The twenty-third session's note that *"the routing checker's store went stale and
> reported a good fragment as unrankable"* is the same fault from the other side.
>
> **The fix is one line, and Golden Rule 11 says it is safe** — the stores are DERIVED, so deleting one
> is a documented recovery action rather than damage:
>
> ```bash
> rm "$APPDATA/Heron/knowledge/global.db"     # then any find() rebuilds it
> ```
>
> **Do this before AND after any routing comparison.** A baseline measured on a store that still holds
> the thing you are measuring the absence of is not a baseline.

> ### THE EXECUTOR'S READ-ONLY GUARANTEE IS A GUARANTEE ABOUT THE EXECUTOR AND A PROMISE ABOUT THE SOURCE, AND `RevitFragment.cs` SAYS IT IS NEITHER
>
> Its own comment reads: *"READ ONLY, AND THAT IS A STRUCTURAL GUARANTEE RATHER THAN A PROMISE. Nothing
> here opens a transaction. Revit refuses every model change made outside one, so a fragment run through
> this path CANNOT alter the model - the enforcement is Revit's, **not a check of ours that could be
> forgotten**."*
>
> **The first half is true and the conclusion does not follow.** Nothing in `RevitFragment` opens a
> transaction — but the C# it compiles arrives **in the request**, and nothing reads it before it runs.
> A fragment whose own source opened a `Transaction` would write, and Revit would allow it, because from
> Revit's side there is now a transaction.
>
> **THIS IS NOT A GUESS, AND IT WAS NOT TESTED BY WRITING TO A MODEL.** Established by reading, because
> the only way to demonstrate it is to change somebody's model on purpose:
>
> - `run_fragment_read` is declared `HeronRisk.Analyze` in
>   [`platform/Heron.Core/HeronOperationRegistry.cs`](../platform/Heron.Core/HeronOperationRegistry.cs)
> - the emergency stop only blocks `risk >= HeronRisk.Modify`, so **the stop does not cover it**
> - `HeronPermissions.Explain(risk)` at Analyze returns nothing, so **`write.enabled = false` does not
>   cover it either**
>
> So a fragment that broke Golden Rule 16 would write with writing switched OFF, with the stop pressed,
> with no preview, no audit entry and no single-undo grouping — through the operation named `..._read`.
>
> **AND THE SOURCE IS NOT CHECKED TO BE A FRAGMENT AT ALL.** `name` is a label; `source` is whatever the
> client sends. Five arbitrary probe scripts were run through this path on 2026-09-07 — reflection over
> the shipped assemblies, and the second-route counts behind two of the proofs above. None of them is a
> fragment in the library. That is exactly why the executor is useful, and it is also why the source
> cannot be assumed to obey Rule 16.
>
> **What actually holds today**: every fragment in the library obeys Rule 16, and the client sends only
> what it read off disk. That is authorship discipline and a well-behaved client — a promise, kept. It
> is not what the comment claims.
>
> **This is a decision for the owner, not a bug to quietly patch**, because every fix trades something:
> declare the op at `Modify` so the stop and the permission gate cover it (and lose read-only proving
> while writing is off); scan the source before compiling (the "check of ours that could be forgotten"
> the comment was written to avoid); or leave the behaviour and correct the comment. **The one thing not
> to do is leave the comment saying a promise is a guarantee.**

> ### `test_graph.py` REWRITES REAL FRAGMENT FILES ON DISK, SO TWO TEST RUNS MUST NEVER OVERLAP — AND WHEN THEY DO IT LOOKS EXACTLY LIKE A BROKEN GRAPH DERIVER
>
> It failed twice on 2026-09-07 with four checks down, the first of them the one that matters:
>
>     with the real contracts, the filter feeds the action:
>     and the filter is reported as an orphan nothing can consume
>     and the action as one nothing can feed
>
> **Nothing was wrong with the graph.** `test_graph.py` proves the deriver reads the files rather than
> remembering, and the only way to prove that is to BREAK a real contract on disk and put it back — so
> `break_providers()` writes a modified `fragment.yaml` over the real one and `restore_providers()`
> writes the original back. **While it is running, the library on disk is deliberately wrong.**
>
> Both failures were caused by running a second thing at the same time: a concurrent `check-routing.py`,
> and later a second test sweep. The other process read the library mid-break and reported the break as
> its own finding. **Run alone, it passed three times out of three.**
>
> **What saved the library is that `restore_providers()` sits in a `finally`** — after a collision
> `load_all()` still returned 349 fragments with 0 problems and `git status` showed no stray fragment
> edits. A run KILLED rather than failed has no such protection: it leaves real fragments broken on
> disk, and the next session would find a library that does not load.
>
> **The rules that follow, and they cost nothing:**
>
> - **never run two test sweeps at once**, and do not run `check-gaps.py` beside anything — it runs the
>   test suite itself, which is how one of these two collisions happened
> - **a graph or contract failure straight after a concurrent run is suspect before it is believed** —
>   re-run it alone before recording it as a regression
> - if a run is interrupted, `git status` on `brain/fragments/` says immediately whether anything was
>   left broken

---

> **PART 6 RAN ON THE OTHER TRACK, IN PARALLEL WITH THE SECTION ABOVE, AND THE TWO ARE NOT IN
> CONFLICT.** Its table says *Proven: still 13* and *Fragments: 348*, and both were true where it was
> written — the promotions and the new fragment landed on this track at the same time. **The count as
> merged is 349 fragments and 16 `PROVEN`.** The two tracks agree on the thing that matters: PART 6
> binds each fragment's declared `needs`, and the pass above confirmed against a real compiler that
> **every name a fragment fails on is a name its contract declares** — which is the assumption PART 6
> rests on.

**2026-09-07, PART 6 — THE GATE AND THE EXECUTOR HAD DRIFTED IN A SECOND PLACE, AND NOTHING WAS
LOOKING AT IT. 196 fragments compiled green against a scope the machine could not reproduce.**

| | |
|---|---|
| Fragments | 348, unchanged — nothing was built |
| Compile, 2020–2027 | green, all four projects |
| Tests | all pass, plus two new ones |
| Bindable inputs | **20 → 70** |
| Proven | **still 13.** Nothing here has met a model |

**`tools/check-gaps.py` said there was nothing left to do here, and it was wrong in a way worth
recording.** Its exact words: *"UNFINISHED - nothing. Everything buildable here is built."* Meanwhile
items 2 and 3 of this file's own next-steps list were pure code needing no Revit. **The tool computes
UNFINISHED from the register and the build order, and the executor's next two slices are in neither** —
so the file this handover names as the authority reports an empty queue while the roadmap beside it lists
the biggest unlock left. Believe the tool over prose about *counts*; do not read its silence as *nothing
to build*.

### The defect, which is the same shape as one this repo already wrote a test for

`tools/check-fragments-compile.py` wraps every fragment in a method **whose parameters are its declared
`needs`** — all of them, whatever they are called. `RevitFragment.cs` supplied **three names**: `doc`,
`uidoc`, `app`. So:

    196 fragments declare a HOST-sourced need the executor had no way to supply
    348 compile green
     20 could actually run

and **nothing connected those numbers to each other.** This is precisely the drift
`tests/test_fragment_imports.py` exists to catch for *namespaces* — its own comment calls it *"a green
gate and a failure at the PC"* — one field over, in the open, with no test on it.

### What changed

**The host binds every declared need, and generates a typed local for each.** The prologue is the compile
gate's method signature written out, from the same `contract.needs`. The two sides now agree **by
construction** rather than by two lists somebody keeps level — which is the fix the imports test could
never have, because namespaces have no contract to be generated from.

**Where a value comes from, in order, and it says which on the answer:**

| | |
|---|---|
| **the chain** | what the previous fragment in the batch left under that name — D-29's design, finally running |
| **the selection** | `uidoc.Selection`, and **only when it is unambiguous** |
| neither | **it refuses, and names what was missing** |

**THE REFUSAL IS THE POINT, NOT THE FALLBACK.** An action handed zero elements reports *"0 changed"*,
which reads as *there was nothing to do* rather than *nobody was asked*. Every fragment in this library
was written to keep those apart; the host must not be the thing that collapses them. Same reasoning kills
the tempting shortcut: **`unjoin-geometry` needs `first` and `second`, and one selection cannot say which
is which**, so binding both to the same set is refused rather than run.

**Four traps closed while writing it, each of which would have failed at the PC:**

- **`Report()` echoed the host's own inputs back as `provides`.** It skipped exactly `doc`, `uidoc`
  and `app` — correct while those were all the host could bind. The moment it could bind `elements`, the
  list handed IN came back OUT as something the fragment produced: **a filter that did nothing would have
  looked identical to one that worked.**
- **The carry-over is static, so it outlived its batch.** A fragment run an hour later would bind
  elements collected by something nobody remembers running, and report them as *"from the previous
  fragment"* — right about the words, wrong about the run. The client now says `chain: reset` on the
  first fragment of every batch.
- **Elements are carried as ids, not as `Element`s**, and re-read from the document at bind time. An
  `Element` is a handle that goes stale on a regenerate or an undo and throws on its next property read.
- **The prologue shifts every compile-error line number.** The error now says by how many, because the
  alternative is somebody reading line 14 of a nine-line fragment and concluding the compiler is wrong.

### Two new tests, and one of them is the guard that was missing

- **`tests/test_fragment_needs.py`** — the `needs` half of `test_fragment_imports.py`. Every declared need
  must be writable into generated code: a C# **keyword** (`params`, `object`, `event`, `base` are all
  words a Revit contract would reach for) reads perfectly in a contract and cannot be a variable name.
- **`tests/test_fragment_needs_reader.py`** — the client is **stdlib only** by design, so it cannot use
  PyYAML to read the contract it must now send. It has a hand-rolled reader that **refuses rather than
  returning a short list**, because a needs list quietly missing an entry fails at the PC as a compile
  error inside generated code, blamed on a fragment that is correct. This test reads all 348 both ways:
  **348 agree, 0 refused, 0 disagree.**

### Two corrections, recorded rather than smoothed over

**The first number I gave was wrong.** I said the unlock took runnable from 20 to **74**, counting
fragments whose only extra need was `elements` or `view`. **The real figure is 70**: some of those needs
are `source: request`, which is the caller's value and not the model's, and I had counted them as though
the host could fill them. Measured one way, asserted another — the same mistake PART 5 recorded about
`test_embed`, three weeks apart.

**And I nearly reported a defect that was mine.** `check-compile.py` printed `FAILED 2020…2027` and the
shell reported exit 0, which looks exactly like a gate that fails green. It is not: **a pipeline's exit
code is the last command's**, and I had piped it through `tail`. The tool returns 1 correctly. Checked
before it was written down, which is the only reason it is a footnote instead of a wrong entry in this
file.

### The other track proved READ_SELECTION the same day, and it lands exactly on this

`READ_SELECTION` went `PROVEN` on 2026-09-07 in the verification worktree — 1,053 duct curves read back
as 1,053, and an honest 0 with nothing selected, on two models, agreeing with `select_by_category`
reached from the other side. **Its `second_route` note names the ambiguity this binding had to decide,
and neither track knew the other was on it:**

> Aimed with `--in` at Snowdon while another model was in front, it returned **0** with
> `active view: (none)`. Whether Snowdon's selection was genuinely cleared when it lost the screen, or
> whether a `UIDocument` built for an off-screen document **cannot see a selection at all**, was NOT
> established — and the two are indistinguishable from the bare 0 the caller gets.

**The binding refuses that case rather than resolving it**, which is the only honest move while it stays
unestablished: the selection is a candidate **only when the document being read is the one on screen**.
An off-screen target gets `needs_unbound`, naming what was missing — never a silent empty list.

**And it settles a distinction worth keeping.** `READ_SELECTION` reporting **0** is correct and is the
whole point of that fragment. An *action* being handed **0** and reporting *"0 changed"* is not. **Same
zero, two opposite correct behaviours, decided entirely by which side of the binding it sits on** — which
is why the refusal lives in the host and not in the fragments.

### IT RAN. Six of Group J's eight, the same day

**This section used to say none of it had run and the add-in was not even deployed. Both stopped being
true within the hour.** Revit was closed, the add-in deployed from `main` at `9dfdf07`, and the checks
run against `Project1 work_ajmal.al` (3,435 elements, view `1 - Mech`) in Revit 2024, session 20704.

| | |
|---|---|
| **J1** | five ducts selected → `<- elements from the selection (5)`, `count 5`. **The first fragment needing an input ever to run here** |
| **J2** | nothing selected → **refused**, naming `elements`, exit code 1. Not `count 0` |
| **J3** | `list-levels` then `count-elements` → `<- elements from list-levels (2)` **while five ducts were selected**. The chain wins over the selection and says so |
| **J4** | `unjoin-geometry` → refused, naming `first` AND `second`, because one selection cannot say which is which |
| **J5** | `filter-elements-by-category` → refused, naming `category` and `levelId` with types |
| **J6** | a fresh batch after J3 → `from the selection (5)`, **not** the two carried levels. `chain: reset` fired |

**`J2` is the one that mattered and it held.** The message is the design in one line: *"Running anyway
would report 0 results, which reads as 'there was nothing to find' rather than 'nobody was asked'."*

**Two rows were NOT run and are still open.** `J7` needs a second model open and is the one
`READ_SELECTION`'s own proof flagged as unresolved. `J8` would mean deliberately breaking a library
fragment, so **the compile-error line-offset wording has never been seen**.

**And this proves nothing about any fragment.** `COUNT_ELEMENTS` returned 5 for 5 and 2 for 2 and is
still `DRAFT`: no second route, and J2's negative is the HOST refusing before the fragment ran, not the
fragment handling an empty set. D-30 is untouched by all of it — 335 fragments still need proofs.

### The lease has no handover, and it cost twenty minutes

The first `prove` was refused: *"This Revit is in use by another chat."* The other track held the lease
and it only frees on a **five-minute idle timeout** or the **Heron button**.

**The earlier project had the opposite rule and said so plainly.** `AJ AI Brain`'s
`knowledge/revit-sessions-and-chats.md`, written 2026-08-27 from the owner's own questions:

> **"They fight.** Whichever speaks last takes the Revit over and cuts the other one off... **It is not a
> queue — a second chat does not wait its turn, it pushes the first one out.**"

Heron kept the transport half — the newest connection still takes the pipe — and deliberately put the
lease on top, because **the old project could not write and Heron can**: chopping a read mid-way is
harmless, chopping a duct move is not. That trade is right and it is recorded in `BridgeServer.cs`.

**What was traded away and never replaced is the convenience.** There is no *"I have moved to this chat,
hand it over"*: `HeronLease.Release` only works for the holder, and `Clear` only runs when the ribbon
button is pressed. So switching chats means waiting five minutes or reaching for the ribbon. **That is a
missing piece rather than a bug, and it is small.**


---

**2026-09-07, PART 7 — THE FIRST AGENT IS BUILDABLE NOW, AND THE AUDIT LOG HAS BEEN
ANSWERING A QUESTION NOBODY ASKED IT.**

This part is a survey and a decision, not a batch. Nothing was built. What it establishes is **which
agent to build first and why**, measured off disk rather than read off the roadmap.

### Agents: 12 named, 0 running, and the word means two things

**`docs/08-agent-catalog.md` names twelve agents. `docs/28-agent-registry.md` holds 250 ids, and 61 of
those are claimed by real code** (`check-gaps.py`, whose anchored pattern is the number to believe — a
loose grep says 78 and counts prose).

**Those 61 are OWNERSHIP LABELS, not software.** Every source file carries `Heron-Agent:` saying which
agent would own it. **There is no agents folder, no orchestrator, no agent runtime** — the search was
run rather than assumed: `mcp/server/heron_workflow.py` is the only thing of that shape, and the rest of
the matches are five documents *about* agents and two tools that *count* them.

> **`Failure Analysis Agent` has a TEST and no implementation.** `tests/test_failure_analysis.py` exists;
> nothing under `mcp/`, `brain/` or `platform/` implements it. A test passing against an absent component
> is worth knowing about before somebody reads the green suite as coverage.

### THE AUDIT LOG IS NOT EMPTY, AND THIS SESSION SAID IT WAS

**Corrected within one exchange, and recorded because the wrong version was already spoken.** The claim
was *"there is barely any usage yet, so a Capability Gap report would report on nothing."* Then it was
looked at:

| `%APPDATA%\Heron\audit` | |
|---|---|
| entries | **331** (10 in August, 321 in September) |
| `run_fragment_read` | **285** |
| `count_elements` | 40 · `select_by_category` 6 |
| **succeeded / failed** | **196 / 135** |

**One hundred and thirty-five recorded failures that nobody has ever read.** Most are from this day's
proving runs across both tracks. That is not "no usage" — it is a corpus, and it makes two of the twelve
agents buildable that were dismissed an hour earlier on a guess.

**The general shape, which this repository keeps meeting:** a claim about a derived store, made from
memory rather than from the store. The fix is the same every time — open it.

### What can be built now, and what cannot

**Five of the twelve, all read-only, none able to touch a model.**

| | Agent | What makes it possible NOW |
|---|---|---|
| **1** | **Fragment Validation** ⭐ | PART 6's executor. It could not have existed last week |
| **2** | **Failure Analysis** | 135 recorded failures, unread. Named, tested, unimplemented |
| **3** | **Capability Gap** | 331 audit entries. [ROADMAP](ROADMAP.md) says explicitly to build it *"much earlier than this phase"* |
| **4** | **Regression Testing** | `tests/test_golden.py` already detects STALE proofs; some are stale now |
| **5** | **Intent** | retrieval by meaning already works underneath it |

**The other seven cannot.** `Fix` needs Heron generating code (Phase 5). `Fragment Merge` / `Split` /
`Evolution` need trust scores and months of per-fragment history. `Fragment Performance` needs timing
nobody collects. `Agent Retirement` has **no agents to retire**. `Communication / Persona` is real and
worth nothing today.

### The recommendation, and the line it must not cross

**Build ONE: Fragment Validation.** It is the only one aimed at what is actually stopping this project —
**333 DRAFT against 16 PROVEN, proved by hand, one at a time.** Five half-built agents and 333 unproven
fragments is the failure mode to avoid.

> **IT MUST NEVER SET `heron-status: PROVEN` ITSELF.** It gathers evidence and DRAFTS; a person
> confirms. [D-30](DECISIONS.md) exists because an unproven claim quietly ages into a believed one,
> and an agent stamping 333 fragments is the fastest machine ever built for doing exactly that. **The
> analogy that fixes it: clash detection finds the clashes and lists them; the engineer decides which are
> real and the engineer signs the drawing. The machine never signs.**

**This is deliberately out of the roadmap's order** — agents are Phase 3+, and this is Phase 2. The
justification is that it is read-only and it attacks the measured bottleneck. Recorded as a departure
rather than slipped in.

`docs/work-notes/plans/PROMPT-fragment-validation-agent.md` is the brief, written so another session can start from it
without asking anything.

### A git trap that cost nothing this time and would eventually cost a day

**`git push origin <branch>` from a worktree checked out on a DIFFERENT branch pushes that named branch,
not your HEAD.** It succeeds, prints a normal result, and this session reported *"committed and pushed"*
about a commit that was still only on the machine. Two commits sat unpushed for hours —
`e1e7fef` (the Group J proofs) and `716c3f6` (the per-chat chain and the hand-back) — while `main` moved
underneath.

**Found by checking rather than by being bitten.** The check is one line and belongs after any push that
matters:

```bash
git branch -r --contains <sha>     # empty means it is NOT on the remote, whatever push said
```

**"Pushed" is a claim about a named branch. It is not a claim about your work.**

---

**2026-09-06, PART 3 — FOUR MORE BUILT, THE FIFTH REFUTED BY TRYING TO BUILD IT.
The library is 339 → 343, and every fragment-shaped job found in the earlier library is now built.**

| | |
|---|---|
| Fragments | **343**, all `DRAFT` |
| Compile, 2020–2027 | green |
| Routing, table claims not reached | **4** — the baseline, unchanged |
| Gates, tests | all green |

**The four built:** `ARRAY_ELEMENTS_RADIAL`, `SET_GLOBAL_PARAMETER`, `ZOOM_TO_ELEMENTS`,
`SELECT_SCOPE_BOXES`.

**THE FIFTH WAS MY OWN WRONG CLAIM, AND THE COMPILE GATE KILLED IT IN ONE RUN.** The schedule
CALCULATED VALUE column is **impossible on every release**, not a version-gated gap. The claim came
from the source library's own note - *"added to the public API in a later Revit release (2022+) as
`ScheduleField.SetFormula`"* - which says in the very next clause that it is untested: *"that's the
shape to try ... and re-verify against that version"*. **That hedge was read as a version note
instead of as the guess it was.** Probing all eight releases:

    ScheduleField.SetFormula                   does not exist, 2020 through 2027
    ScheduleField.Formula                      no such property
    ScheduleDefinition.AddCalculatedField      no such method
    ScheduleDefinition.AddCalculatedParameter  no such method

`AddField(ScheduleFieldType.Formula)` **does** compile everywhere, which is the trap: the field can be
added and nothing can give it a formula. The shipped assembly agrees - every `Calculated*` member on
the schedule side is a **Get**. **A calculated column can be read and cannot be authored.**

> **The mirror of a lesson already in this file.** *"Impossible on version X"* written without naming
> the version becomes a lie the day another Revit is installed. **A POSSIBILITY recorded without a
> version, on a call nobody ran, is equally a lie** - and worse, because it sends somebody off to
> build something that cannot exist. Four probe lines cost less than the fragment's purpose would
> have.

**Two routing defects were caught in this batch, and one of them was mine twice over.** The noun trap
took *"the driving dimension has changed"* straight to `CREATE_DIMENSION` - a global DRIVES a
dimension, and naming the dimension hands the sentence to the dimension cluster. **Rewriting that row
then wrapped it across two lines, which is the OTHER defect fixed earlier the same day**: the checker
reads the continuation marker as part of the sentence, so the claim can never match. A claimed
sentence goes on ONE line, and it must be a sentence the fragment actually declares - a table row
saying *"show these again"* against a declared *"show them again"* is one word from useless.

**Final count on the earlier library:** 312 covered + 5 impossible + 13 missing, **all 13 built**.
330 accounted for.

**Nothing is proven.** All 343 are `DRAFT`. Eight green releases is the API surface agreeing, not
evidence any fragment does the right thing (D-30, D-45). **That, and nothing else, is what is left.**

---

**2026-09-06, PART 4 — THE PROVING PASS RAN. TWELVE FRAGMENTS ARE `PROVEN`, THE FIRST
IN THIS PROJECT'S LIFE — AND THE THING THAT WAS BLOCKING IT WAS NEVER REVIT.**

**D-28's executor is built.** A fragment's C# is compiled by Roslyn inside Revit's own process, against
the assemblies Revit has actually loaded, and what it leaves behind comes back by name. It opens NO
transaction, so Revit itself refuses any model change — the guarantee is Revit's, not a check of ours.

| | |
|---|---|
| Models | `Snowdon Towers Sample HVAC` (9,628 elements) **and** `Project1` (3,410, default template), both open at once, Revit 2024.3 |
| Fragments run | **20** |
| Failed | **0** |
| Promoted to `PROVEN` | **12** |
| Still below `PROVEN` | **331** |

**REVIT WAS NEVER THE BLOCKER, AND THIS FILE SAID SO IN THE SERVER'S OWN CODE:** *"there is no
executor: the bridge speaks a fixed set of operations and none of them compiles anything."* Every
session that wrote *"the proving pass needs the PC"* was half right. The PC was necessary and nowhere
near sufficient.

**ONE MODEL WITH CONTENT AND ONE WITHOUT IS WHAT MADE IT A PROOF.** The pair gives the positive and the
negative case in one sitting, on one build, through one executor. Having both open AT ONCE proved
something neither could alone: every fragment returned only the ACTIVE document's content. Reading
across documents is exactly how these could have been quietly wrong.

**THREE CROSS-CHECKS FELL OUT OF RUNNING THE SET TOGETHER** — they were not arranged, and they are the
third leg D-30 asks for:

- `LIST_SHEETS` says 17 sheets; `LIST_REVISIONS` walks sheets its own way and also says 17
- `REPORT_VIEW_FILTERS` says 9 unused; `FIND_UNUSED_DEFINITIONS` reaches 9 from the other side
- `LIST_LINKED_MODELS` finds 6 links; `REPORT_EXTERNAL_REFERENCES` finds 8 references — the difference
  is exactly the two settings references, on both models

**THIS MORNING'S FIX FIRED ON A REAL MODEL.** Project1 holds a view template no view follows, which a
view family type names as the default for new views — `templatesKeptForViewTypeDefault: 1`. Before the
correction that template reads as unused, and deleting it changes what every new view of that kind is
created with, silently. Snowdon reports `0`. Both halves observed.

**EIGHT WERE NOT PROMOTED, ALL FOR THE SAME REASON: HALF THE PAIR WAS NEVER SEEN.** Neither model is
workshared, neither has design options or global parameters, nothing was selected in either, and both
have exactly the two default phases. `FIND_UNUSED_MATERIALS` needs the case that matters put in front
of it — **a material used ONLY as paint, which must NOT be reported unused**. That needs a painted
face, and there wasn't one.

> **WHAT TO OPEN NEXT TIME, so the other eight can finish:** a **workshared** model with a **closed
> workset**, one carrying **design options** and **global parameters**, and one with **a face painted in
> a material used nowhere else**. Each of those closes a specific fragment, and none of them can be
> faked.

**FOUR DEFECTS WERE FOUND BY RUNNING IT, AND NONE COULD HAVE BEEN FOUND BY READING IT:**

- **Every fragment died on its first line.** The globals type was nested inside an internal class;
  scripts compile into their OWN assembly, so `doc` was unreachable. The add-in compiled clean and all
  343 fragments compiled clean — because the gate's wrapper puts them in the SAME assembly, the one
  arrangement where the bug cannot appear. **Exactly the hole D-28 warned the compile gate could not
  cover.**
- **The add-in deployed without the code it needed to run.** Roslyn was referenced, restored and
  compiled against, and not one of its assemblies reached the output folder — only its twelve localized
  RESOURCE folders, which looks like a working deploy from a distance. It would have been an add-in that
  vanished at `OnStartup`. `CopyLocalLockFileAssemblies` took the output from 3 DLLs to 15.
- **Two commands in a row are refused.** `CLIENT_ID` is minted per process, which is right for the
  long-lived MCP server and wrong for a command line — every command orphaned a lease for five minutes.
  Fixed with `prove`: one process, one lease, many fragments.
- **The answer did not name the model, and that nearly forged a proof.** The first fragment to run
  reported two levels called *Level 1* and *Level 2* at 0 and 4000 mm — which is a plausible sample
  building AND exactly what an empty template contains. Later, with both models open, a run intended for
  `Project1` came back reading `Snowdon Towers` because the window was open but not ACTIVE. **Without the
  title on the line those 15 grids would have been recorded as the "returns 0 on an empty model"
  negative case.** The earlier library's own scar says it: *"The answer always names the document."*

**§9b CAUGHT THIS SESSION THREE TIMES, IN THREE DIFFERENT SHARED THINGS** — the working tree (a
`checkout` in the shared folder deleted this session's files from disk mid-build), the lease, and the
retrieval store. All three are named in that section. **The work moved to its own worktree and the
collisions stopped.**

> **`test_embed` and `test_retrieve` fail, and it is not this work.** `NEEDS-CHECKING` row **A7** was
> closed the same day: `model2vec` installed, backend switched from `lexical` to `model`, 343 fragments
> re-embedded. Both tests encode rankings measured against the n-gram backend, and `test_retrieve`'s own
> failure message predicted it — *"A7 is what should break this check."* They need re-basing against the
> model backend WITH the reasoning written down, which belongs with the A7 work. They were not quietly
> edited to go green.

---

**2026-09-06, PART 2 — THE REFUTATION PASS ON THE COVERED SIDE IS DONE, AND IT FOUND
FIVE MORE.** The blind spot the section below warned about is closed. All 321 remaining source files
were put to the opposite test from the original audit: *name the Heron capability that does this job,
or the claim fails.*

| | Claimed | Held | Refuted |
|---|---|---|---|
| Already covered | 317 | **312** | **5** |
| Impossible via the API | 4 | **4** | 0 |

**The arithmetic closes now:** 312 + 4 + 14 missing (9 built + 5 new) = **330**. It did not before.

**The five raised — four were real and are now BUILT; the fifth was wrong and is corrected in the
section above:**

| The job | Why nothing here does it |
|---|---|
| **Radial array** | `ARRAY_ELEMENTS` takes a direction, a spacing and a count. It is linear and only linear. A radial array sweeps copies around a centre point through an angle |
| **Set a global parameter's value** | `REPORT_GLOBAL_PARAMETERS` says *"deliberately not here — a MODIFY job"*, and nothing picked it up. A global is written through `GlobalParameter.SetValue`, not a parameter on an element, so `WRITE_ELEMENT_PARAMETERS` cannot reach it |
| ~~**A schedule CALCULATED VALUE column**~~ | **WRONG — refuted the same day by trying to build it. It is impossible on every release.** See the section above |
| **Navigate the view to elements** | `UIDocument.ShowElements`. Nothing calls it. `SET_SELECTION` selects without moving the view — and **`SHOW_ELEMENTS` is a name trap: it means UNHIDE**, so *"show me these"* reaches the wrong fragment |
| **Scope boxes as an element set** | `READ_SCOPE_BOX_EXTENT` finds ONE by name; nothing enumerates them or hands them back as elements to rename or delete. **The same gap as `SELECT_VIEW_TEMPLATES`**, found and built the same day |

**The four "impossible" verdicts all held**, and the source library had proved each itself: `Document.Phases`
is read-only with no creation method anywhere in the API; `PHASE_NAME` is read-only; there is no Scope
Box creation call, which also kills resize-by-recreate.

**THE LESSON IS CHEAPER THAN THE PASS THAT FOUND IT. Four of the five sit inside a fragment that
covers the neighbouring case and says so in its own purpose** — *"not a formula column"*,
*"deliberately not here"*, *"linear along a direction"*. **A fragment that names its own boundary is
the best gap detector in this repository**, and reading those sentences is far cheaper than reading
somebody else's library. The fifth came from a name collision: two different jobs both called *show
elements*.

**One method was tried and thrown away rather than reported as reassurance.** Matching source file
names against fragment vocabulary does not work — six of the nine *already-known* gaps score
0.67–1.00 against unrelated fragments. A tool that cannot find the gaps you already know cannot be
trusted to find the ones you do not.

---

**2026-09-06, PART 1 — THE NINE ARE BUILT, AS TEN FRAGMENTS. The library is 329 →
339.** Every one compiles on Revit 2020 through 2027, the ten gates are green, the tests pass, and
routing is back to its exact baseline. **Nothing is proven** — all 339 are `DRAFT` and the compile
gate is the API surface agreeing, not evidence any of them does the right thing (D-30, D-45).

| | Baseline (329) | Now (339) |
|---|---|---|
| Compile, 2020–2027 | green | green |
| Routing, by words #1 | 81% | 81% |
| Routing, by nearness #1 | 55% | 54% |
| Table claims not reached | **4** | **4** |
| Sentences two fragments both want | 351 of 1875 (18.7%) | 362 of 1941 (18.7%) |

**The nine became ten because one was two jobs.** `action-purge-unused` covered materials AND group
definitions, which share no mechanism and no sentence — `FIND_UNUSED_MATERIALS` and
`FIND_UNUSED_GROUP_TYPES`. Folding them together would have needed a mode string, and a mode string
is the shape that hands a whole list to a delete on a typo.

**Building them found a defect in a fragment already here.** `FIND_UNUSED_DEFINITIONS` walked views
only, so a view template used solely as a **view family type's default** — the template new views of
that kind start with — was reported unused. Nothing points at it, and deleting it changes what every
new plan is created with, silently. Caught only because `SELECT_VIEW_TEMPLATES` asked the same
question a third way and the two answers disagreed. Fixed in both, with the negative case written
into `tests/cases.yaml`.

**Three routing defects were fixed, and two of them were already here.** Every crossing this batch
introduced was answered, and the count came back to the baseline 4 rather than merely "not much
worse":

- **`"why will this material not purge"` had been unreachable for ever, and nobody could see it.**
  `REPLACE_MATERIAL` wrapped that sentence across two lines of its routing table, so the string the
  checker extracted carried the comment marker — `why will this material not
#    purge` — which no
  utterance can ever match. It only surfaced because `FIND_UNUSED_MATERIALS` started winning the
  sentence. **A claimed sentence goes on ONE line.**
- **A QUESTION was ranking to a fragment that WRITES.** `REPORT_VIEW_TEMPLATE_CONTROL` declared
  *"which view template is controlling this view"* and its table claimed the shorter *"which template
  is on this view"* — which resolved to `REMOVE_VIEW_TEMPLATE`. That is the worst shape there is, and
  the tool's green *"no question is answered by something that writes"* does not cover it: that line
  only checks DECLARED utterances, which resolve by identity before any ranking runs.
- **The noun was doing the routing again — fourth session running.** `AUTO_SIZE_PIPE` declared
  *"auto size the chilled water pipes"* and took *"show me the chilled water"* off
  `SELECT_BY_MEP_SYSTEM`: a sentence that selects and changes nothing, answered by a fragment that
  resizes pipework. Fixed by removing the service name from every utterance and leaving it in the
  purpose, which is indexed far more weakly. **Every utterance is led by the verb.**

**NINE WAS A FLOOR, NOT A CEILING, AND THAT IS STILL TRUE.** The audit that found them handed each of
the ten *missing* claims to a refuter. **The 317 *already covered* claims were never put to one.**
That check was built to stop a duplicate being written — the cheap failure. A false "covered" is the
expensive one, because it ends the search. Two things were tried on 2026-09-06 and only one worked:

- **Matching source file names against fragment vocabulary does not work.** Six of the nine KNOWN
  gaps score 0.67–1.00 against unrelated fragments; `create-dimension.cs` matched
  `REPORT_GLOBAL_PARAMETERS` at 1.00. A tool that cannot find the gaps already known cannot find new
  ones, so it was thrown away rather than reported as reassurance.
- **About fifteen of the most suspicious files were read by hand and no new gap was found** — the two
  likeliest were covered well (`SELECT_TOUCHING`, `SELECT_IN_REGION`). Fifteen of 317 is a sample.

**So the next session's honest choice is: put the covered claims to a refuter the way the missing
ones were, or stop saying anything about what is left.**

> **DONE the same day, and it found five more.** See the section at the top of this file. The
> paragraph below this one was right to distrust the number: nine was not all there was, and it took
> the opposite test to show it.

**One number was wrong in two documents and is now fixed.** The audit table read 316 covered + 4
impossible + 9 missing = **329, against 330 read**. The refuted claim was struck off the missing
column and never added to the covered one. It is one file and changes no conclusion — which is
exactly why it survived being printed twice. **A total nobody adds up is not a check.**

> ### §9b was not followed and it cost something. Read this before working in the shared folder.
>
> This session ran in `D:\Ajmal\Aj Programs\Heron Ai` alongside another one, without worktrees.
> [§9b](#9b-three-sessions-at-once--the-protocol-that-stops-them-colliding) says not to. It went
> wrong twice, in both directions:
>
> 1. **The branch moved out from under this session mid-work.** It started on
>    `claude/fragments-the-nine` off `main` at `3435d8d`. By commit time the other session had run
>    `git checkout` in the same folder, so `git commit` put this whole 37-file batch on top of
>    `docs/b2-confirmed-in-revit` — somebody else's branch. `git checkout` is per WORKING TREE, not
>    per chat, and nothing warns you. It was moved off with a worktree afterwards.
> 2. **Undoing that discarded an uncommitted edit belonging to the other session.** `git status`
>    read clean; the branch was reset back to what it had already pushed; and in the seconds
>    between, that session wrote `NEEDS-CHECKING.md` again. The reset took it. It was never staged,
>    so it is not in the object database and `git fsck` has nothing — the only dangling blobs were
>    this session's own fragment drafts. **Everything committed survived** (B2, B2a, B2b and B3 are
>    all recorded DONE in `5150842`); what was lost was one increment on top, and B3a is still
>    showing open, so that is the likely content.
>
> **The lesson is not "check status first" — that was done, and it was clean.** A working tree
> shared with a live session has no safe moment: it can be written between the check and the
> command. There is no careful way to run a destructive git command in a folder somebody else is
> working in. The answer is the one §9b already gives, and this is the second commit to record it:
> **one worktree per session, set up before either starts.**

---

**Updated 2026-09-06 (later session) — "EXHAUSTED" WAS WRONG. The earlier library still holds NINE
fragment-shaped jobs, found by reading all 330 eligible files rather than sampling them.** The owner
put `AJ-AI-Brain` on this machine, so for the first time the claim below could be checked instead of
believed. Twelve readers took one source folder each and classified every file; every "missing" claim
was then handed to a second agent told to REFUTE it. One of the ten was refuted. Nine were not.

| Read | Already covered | Impossible via the API | **Genuinely missing** |
|---|---|---|---|
| **330** | 316 | 4 | **9** |

**The nine, with the folder they came from — ALL BUILT 2026-09-06, as ten fragments. See the section
above.** The claims in this table were verified independently against their nearest existing
neighbour before anything was written: five of the nine had a close one, and all five turned out to
be genuinely distinct.

| Source | The job |
|---|---|
| `reporting/action-plan-shortest-route` | Compute the cheapest way to connect a loose set of points and report the ORDER plus total run length — tree (MST), chain (2-opt) or per-room. **Nothing in the 329 decides an order**, which is why `CREATE_ELECTRICAL_RUN` and `RENUMBER_SEQUENTIAL` both still need a human to supply the sequence they consume |
| `sheets-views/action-set-view-properties` | Set a VIEW's **Phase and Phase Filter** in bulk. `SET_VIEW_PROPERTIES` was re-authored narrower — scale, detail level, visual style only — and `SET_ELEMENT_PHASE` moves elements, not views |
| `structural-changes/action-auto-size-pipe` | Size pipes from the flow they already carry — bore = flow / target velocity, snapped UP to a real size |
| `structural-changes/action-batch-upgrade-revit-files` | Upgrade a FOLDER of `.rfa` / `.rft` / `.rte` to the running Revit, as background documents |
| `structural-changes/action-purge-unused` | Report unused materials (including paint-only use) and GroupTypes with no placed instance |
| `qa-checks/action-check-open-pipe-ends` | Cap open pipe ends via `PlumbingUtils.PlaceCapOnOpenEnds` |
| `creators/create-dimension` | One dimension string across Grids and/or Levels — the setting-out dimension |
| `filters/filter-by-types` | Return the TYPE elements themselves (`FamilySymbol`, `DuctType`, `WallType`) as an actionable set |
| `filters/filter-by-view-templates` | Return View Templates themselves as an actionable set, narrowed by name and by usage |

**Why the earlier count said zero.** The 2026-09-05 estimate of "roughly 60 worth adding" was made by
sampling and by reading folder names. Fifty-five were then built, and the arithmetic — 60 minus 55 —
was allowed to stand in for a recount. **Nobody read the remaining files.** The three the last batch
"could not find an honest candidate" for were real; there were nine, and they are listed above. The
denominators it quoted were right: 398 `.cs` files, 68 never fragment work (44 `recipes/` + 12
`context/` + 8 `commands/` + 3 `examples/` + 1 `lib/`), 330 eligible — all confirmed by count.

**Four routing defects were found and fixed the same day, in the count/room/fitting cluster.** Ranks
are the correct fragment's own position, before → after: *"count the fittings per space"* 6 → 1,
*"how many fittings are in each room"* 2 → 1, *"count these by hvac zone"* 2 → 1, *"how many fittings
in here"* **not in the top 25** → 1. Two were the worst shape there is — `PLACE_ROOMS` ranked third
for a counting question, and `CREATE_HVAC_ZONE` won one outright, beating `COUNT_BY_SPATIAL_CONTAINER`
**on that fragment's own declared utterance**.

**The noun was doing the routing, not the verb.** "fittings" appears in three of
`MEASURE_FITTING_AREA`'s utterances, so any sentence carrying it was pulled into the ductwork cluster.
Fixed by adding only. **Two attempts had to be undone**, both failing the same way: text added to one
fragment carrying the other's words (*"per room"*, *"in this room"*) stole its sentences. A fragment
must be strengthened on its OWN ground. That is the third session this trap has caught.

**Numbers in this file that were wrong, corrected from the rows:**

- The stale branch **is now deleted**. The note below saying the container got a `403` still stood
  weeks later; it was removed from GitHub on 2026-09-06 and `origin` now carries only `main`.
- `NEEDS-CHECKING.md` says *59 items, 6 done, 53 left*. The rows say **60, 6 done, 54 left** — the
  drift this file warns about, for the fourth time.
- **The build order stops at Step 14, and 180 fragments claim Steps 15, 16 and 17.** Those steps are
  named nowhere in [27](27-build-order.md). The library build happened without being written into
  the plan it was supposedly following.
- **Agents: 250 registered, 82 implemented, and every agent that was DUE is built.** 49 carry a build
  step, all in Steps 1–6; 4 of those are host-provided and the other 45 have source files, so **nothing
  scheduled is outstanding**. A further **37 are built that carry no step at all** — the registry's
  `Step` column was never extended past Step 6, so 201 agents have no scheduled position and "are we on
  plan" cannot be answered for them.

  > **This line first said *61 implemented, 16 due but missing*, and that was wrong — caught within the
  > hour by checking one of the sixteen.** `HERON-MCP-HLT-005` and `HERON-MCP-VER-007` are both named in
  > `mcp/server/heron_mcp_server.py`; `HERON-MCP-CMP-008` is in `heron_session.py`. **The pattern was
  > capturing only the FIRST id on a header line, and headers here list several separated by commas.**
  > That is the same failure `NEEDS-CHECKING.md` records three times over — *prove the pattern can see
  > what you know is there* — arrived at from the opposite direction: not a count that missed a group,
  > but a count that missed the second and third entry in every row it did match.

**A test could poison itself, and did.** `tests/test_scope_store.py` plants a malformed fragment inside
the real `brain/fragments/` tree. It cleaned up in a `finally` but built the directory OUTSIDE the
`try`, so an interrupted run left `zz-broken-temp` behind and every later run died on `FileExistsError`
before reaching the cleanup. One interrupt poisoned it permanently — inside the very directory this
file tells you to trust for the count. Reproduced, then fixed.

**Still true, and unchanged by any of the above: nothing is proven.** All 329 are `DRAFT`. The ten
gates are green and 18/18 tests pass, which is the API surface agreeing and not evidence that any
fragment does the right thing (D-30, D-45).

---

**Updated 2026-09-06 — the library went from 274 fragments to 329, and the earlier library is now
EXHAUSTED of fragment-shaped work.** Fifty-five new fragments in five batches, plus three upgrades:
`SET_SCHEDULE_APPEARANCE` v2 (hide a column), `READ_ROOM_GEOMETRY` v2 (holes separated from the outline)
and `SPLIT_MEP_RUN` v2 (it cut a run and left both halves open — it now rejoins them and reports
`halvesJoined` against `halvesLeftOpen`).

> **MERGED TO `main` — pull request #17, branch `claude/heron-ai-fragment-balance-dxk0l6`.** That branch
> is spent: a merged pull request cannot carry new work. **A fresh session starts a NEW branch from the
> latest `main`**, never more commits on that one. `git fetch origin main` FIRST, before any work.
>
> **DONE 2026-09-06 — that branch is deleted and `origin` carries only `main`.** The note here said the
> container's git proxy answered `403` to a ref deletion and asked for it to be done on GitHub; it was
> still sitting there weeks later, because a request parked in a document is not a task anybody owns.

**The count above is the one to distrust first.** This line said 218 while `main` already carried 274,
because peer sessions added fragments and never came back to this file. Run
`ls brain/fragments | wc -l` and believe that, not this paragraph.

**What is left in the earlier library is not fragments.** Every remaining source file is one of three
things: a RECIPE (a multi-step job that belongs in a skill, composed from these fragments), IMPOSSIBLE
through the API (creating or renaming a phase, creating or resizing a scope box), or ALREADY COVERED
here. The last batch was asked for sixteen and delivered fifteen for exactly that reason — there was no
honest sixteenth candidate, and padding it would have meant a duplicate.

**Nothing in those fifty-five is proven.** All are `DRAFT`; the compile gate is green on Revit 2020–2027
for all 329, which is the API surface agreeing and not evidence that any of them does the right thing
(D-30, D-45). Each carries its positive case, its negative case and a second route in `tests/cases.yaml`,
waiting on one concentrated pass with Revit open.

**Updated 2026-09-05 — the library went from 210 fragments to 218, the REPORTING block, and four of the
eight replaced an answer that was a WRITE.** That section is directly below.

> **Both 2026-09-04 and 2026-09-05 batches are MERGED TO `main` — pull request #8, branch
> `claude/heron-ai-fragments-wa93gf`.** That branch is spent: a merged pull request cannot carry new
> work. **A fresh session starts a NEW branch from the latest `main`**, never more commits on that one.
> `git fetch origin main` FIRST, before any work — peer sessions push to `main` too, and finding out at
> push time is how a whole session's documentation edits become merge conflicts.

**Updated 2026-09-04 — the library went from 202 fragments to 210, all eight in the SCHEDULE cluster,
where every sentence was reaching the fragment that adds a column and two were reaching fragments that
change the model.** That section is directly below.

**Updated 2026-08-31, during the ninth working session — the library went from 32 fragments to 63, and
four things turned up on the way that matter more than the count.** The section for it is below the
eighth.

**Updated 2026-08-30, during the eighth working session — the one that went looking for work the
checker could not see, and found the front door telling a new reader things that stopped being true
weeks ago.** The seventh re-authored the fragment library; the eighth is below.

**Updated 2026-08-30, during the seventh working session — the one re-authoring the owner's earlier
fragment library into this one.** The sixth wired the brain to the host, which was the last thing here
that could be *built* without a machine; the seventh is doing the thing that can still be done without
one — **writing fragments**, from 7 to **32** so far, each studied and rewritten rather than copied
([D-44](DECISIONS.md): none of them inherits the earlier library's proven status). For whoever picks
this up next: a fresh Claude session, a person, or the owner on his phone.

---

## HANDOVER — the session that ran 2026-09-07 into 2026-09-08 (the platform track)

**What happened, in one line: six agents were built, and every one of them found something that was
already true and that nobody could see.**

**It ran beside the fragment-proving track and neither touched the other's files.** That track took
`PROVEN` from 16 to 50; this one did not touch a fragment's status. Staging was by explicit path all
session, and the two tracks' commits interleave cleanly in the log.

| | Start | End |
|---|---|---|
| Agents with code | 64 | **70** of 250 |
| MCP tools served | 10 | **13** |
| `tools/*.py` | 12 | **14** |
| `tests/test_*.py` | 25 | **30** |
| Agents rejected after measuring | — | **7** |

### The six, and what each one found

| Agent | Found |
|---|---|
| `AHR-GAP-001` Capability Gap | 131 compile failures on 09-06 and none on 09-07. The fix had landed and nothing recorded that it had |
| `FRG-MTX-009` Compatibility Matrix | Everything compiles on all eight releases, and **every proof Heron holds stands on Revit 2024 alone** |
| `OPS-DIA-005` Self-Diagnostics | The two above, in the one place a person would look. Four commands became one |
| `DOC-FRG-004` Fragment Catalogue | 349 fragments had no readable index. "What have we got" had been answered five times that day with throwaway Python |
| `WSP-BAK-010` Backup | **Nothing in the repository backed up anything.** The audit trail is not in git and is the only record of what Heron has done |
| `WSP-RST-011` Restore | Shipped in the same commit, because docs/21 s8 calls an untested restore path a belief. `drill` does the round trip into scratch |

`tools/agent-count.py` was built first, to answer the question that started the session, and its own
first run is the reason the next paragraph exists.

### Four things that were wrong and are now not

**Phase 0/1 was never four agents short.** A build-state summary said it was and recommended building
the Orchestrator. `HERON-ORC-MAIN/INT/PER/SUM` are **host-provided by [D-01](DECISIONS.md)** and
`check-metadata.py` had said so all along. The register carries three states now — BUILT, HOST, LEFT —
because collapsing HOST into LEFT produces a to-do list with four items nobody will ever do.
**45 built, 4 host-provided, 0 outstanding.**

**The audit trail could not name a fragment.** 564 runs all logged as `run_fragment_read` and not one
said which. The dispatcher records it now, read from the REQUEST because a run that fails to compile
has no answer to name itself in. Durations were strings, so `"9"` sorted above `"6620"`; they are
numbers now, and both shapes are read for ever because the trail is never pruned.
**Not yet seen — the add-in carrying that change has not been deployed.**

**2,332 declared test cases had never been read by anything.** Every fragment carries
`tests/cases.yaml` with `positive`, `negative` and `second_route`; a grep for the filename returned
nothing. **Twelve did not parse, and three of those twelve belonged to fragments already at `PROVEN`.**
All twelve repaired without changing a word — the token stream was compared per file before writing.
`heron_fragment.validate()` reads them now.

**The run plan was issuing advice that had already been disproved.** `negative_case_plan` said "run it
with NOTHING selected" for every selection-fed fragment — the instruction that produced 36 refusals and
zero proofs on 09-07. NEEDS-CHECKING recorded the lesson the same day; the function issuing it was never
touched and repeated it for a week. It now says *select something containing none of what it reports*,
and shows each fragment's own declared case. **Eighteen fragments still declare "an empty element list";
`plan --all` flags every one, and none is blocked — all eighteen keep a workable case.**

### Seven agents rejected, each after measuring rather than guessing

| Rejected | Because |
|---|---|
| Duplicate Detection | 23 "duplicates" are the `transfer-*-between-documents` family being a family |
| Citation / Source | All 349 declare `source: OFFICIAL` — one value in the column it exists to distinguish |
| Naming Validation | **0** violations across 349, including capability-matches-slug |
| Keyword / synonyms | Routing is 81% first-hit, 96% top-three by words. The weak half is semantic, which synonyms do not fix |
| API Change Intelligence | Fragment API coverage is already answered by compilation. Silent behavioural change cannot be read out of assemblies |
| Fragment Performance | Needs the fragment names the dispatcher now records. **Real work, blocked on deployment** |
| Skill Research | Built it, ran it: 14 findings, **1 real**. The resolver never says "I do not know", so any sentence fragment resolves to something. Deleted |

**The pattern is the deliverable.** Rejecting on measurement took longer than building, and it is why
the six that shipped each found something.

### Read this before trusting a green result here

**`tools/check-structure.py` is FAILING on `main` as of `83fd7e8`**, and it is not from this track.
`brain/fragments/read-space-loads/impl/any/fragment.cs` writes
`catch (Autodesk.Revit.Exceptions.ApplicationException)` — the only one of 349 fragments to name the
namespace — and the layering rule forbids `Autodesk.Revit` outside `revit/`. The qualification looks
deliberate, since `ApplicationException` exists in `System` too. **It is a real tension between a
fragment needing to disambiguate and a rule written for Heron's own layers, and it is a decision for
whoever wrote it rather than something to quietly rule around.**

Everything else is green: 30 tests, `check-docs`, `check-metadata`, `agent-count`, and all eight
releases compile with 0 warnings.

### The three things waiting on Revit

1. **The trail recording which fragment ran.** Needs the add-in rebuilt and deployed. Until then
   `heron_gaps` can count failures and not attribute them.
2. **The corrected negative-case advice used on a real proof run.** The plan is right now; nobody has
   followed it yet.
3. **`heron_diagnose` and `heron_gaps` reading a trail that has fragment names in it.** Both are built
   and both are reading a trail that predates the change.

### Two habits worth keeping from this session

**A checker that has never refused anything is a claim about the checker.** Every gate built here was
fired against deliberately broken input before it was believed — `agent-count`'s five, the cases gate's
five, the backup's damaged-copy refusal. Two of them were wrong the first time and only the firing
showed it.

**A green result minutes old is not necessarily still green.** `test_validate_agent` passed
individually and failed in the full suite twenty minutes later, because the other track proved
`count-elements` in between and a test was asserting it was still unproven. With two sessions in one
tree, run the suite last.

---

## HANDOVER — the session that ran 2026-09-07 (the verification track)

**What happened, in one line: nothing was taken on trust. Every proof already recorded was re-run and
all thirteen held, three more fragments earned one, and four things that cost real time are now written
down so nobody pays for them twice.**

**It ran beside PART 6, and neither track knew about the other until the merge.** PART 6 built the
executor's inputs; this track checked what was already claimed. They agree where it counts, and the
merge commit says how.

| | Start | End |
|---|---|---|
| Fragments | 348 | **349** |
| `PROVEN` | 13 | **16** |
| Recorded proofs re-run | — | **13 of 13 hold, 13 of 13 fingerprints fresh** |
| DRAFT READ fragments through the real executor | — | **135, zero unexplained failures** |

### The three that were proven, and why they could not have been before

`READ_SELECTION`, then `REPORT_PHASES` and `REPORT_DESIGN_OPTIONS` — **the last two only because the
owner made the conditions himself while the session ran.** Neither model in this repository's history
had a third phase or a design option in it, so a fragment that ignored the model and returned the
default pair would have passed every check ever run against it. **That is the whole reason D-30 asks
for a negative case**, and it is the first time the rule has visibly earned its keep on the same day it
was applied.

### One fragment was built, and two that were asked for cannot exist

`CREATE_GLOBAL_PARAMETER` — **the gap was named by this library itself.** `SET_GLOBAL_PARAMETER`'s
routing table had said *"make a global parameter → NOT HERE"* and pointed at nothing since 2026-09-06.

Creating a **phase** and creating a **design option** were asked for in the same breath and **neither is
possible on any release** — no create method, no constructor, `Document.Phases` read-only, and
`DesignOptionSet` is not a type at all. Both refusals were already written in the two report fragments'
own routing tables; reflection against the shipped assembly confirmed them rather than discovering them.
**A fragment that names its own boundary is this repository's best gap detector, and it works in both
directions: it found the one worth building and refused the two that cannot be.**

### Four things that cost time, now written down

| | |
|---|---|
| **The retrieval store never drops a deleted fragment** | The whole directory was moved off disk and the store still answered from it. Indexing is additive. **Every before-and-after taken without deleting `global.db` first is worthless** — the first set taken this session was |
| **`test_graph.py` rewrites real fragment files** | Two runs overlapped and it failed in a way indistinguishable from a broken graph deriver. Never run two sweeps at once, and never run `check-gaps.py` beside anything — it runs the suite itself |
| **The executor's read-only guarantee is a promise, not a guarantee** | `run_fragment_read` is declared `Analyze`, so neither the emergency stop nor `write.enabled` covers it, and the C# arrives in the request unread. A fragment breaking Rule 16 would write with writing switched off. **Re-verified after the PART 6 merge and it still stands word for word** |
| **A missing C# identifier does not stay a `CS0103`** | It cascades into unrelated error codes on lines that are correct. A triage rule written on that assumption reported 13 defects that are not defects |

### What the next session should do, in order

1. **The four half-proved fragments**, listed with their predictions at the top of this file. One needs
   a global parameter created by hand — **`CREATE_GLOBAL_PARAMETER` cannot do it**, because no write
   path runs a fragment yet.
2. **Group J in `NEEDS-CHECKING.md`** — the eight rows that would prove PART 6's input binding. None of
   it has met a model.
3. **The caller's half of the inputs.** 278 fragments want a category, a name or a distance and there
   is no route for one. Now the largest single unlock.

**A warning worth repeating because it was nearly paid twice:** `LIST_WORKSETS` came back looking like a
regression, and was not — its proof records the positive case on a *different model* from the negative.
**Read the whole proof before believing a re-run disagrees with it.**

---

## HANDOVER — the session that ran 2026-09-05

**What happened, in one line: the library went from 210 fragments to 218 — the REPORTING block — and
four of the eight replaced an answer that was a WRITE.**

**The selection rule found the worst shape yet.** Asking `heron_brain.lookup` the owner's own sentences,
four questions were being answered by fragments that change the model:

| The sentence | Where it went | What that fragment does |
|---|---|---|
| *"what is the wall build up"* | `CREATE_WALL` | builds a wall |
| *"what panels and mullions are in this curtain wall"* | `CREATE_WALL` | builds a wall |
| *"what rooms are on either side of this door"* | `PLACE_ROOMS` | creates rooms |
| *"what is the pressure drop in this duct run"* | `SET_MEP_SLOPE` | changes the slope |

**Reading the neighbours stopped a duplicate outright.** `REPORT_NESTED_FAMILIES` was in the batch until
`GROUP_BY_ASSEMBLY` was read — which is that fragment already, near word for word (*"an AHU with a nested
fan, coil and filter is FOUR family instances… there is one unit on site"*). The routing was still wrong,
so the fix was **three utterances on the existing fragment, not a ninth file.** A lookup shows what is
missing; only reading the candidates shows what already exists under a different name.

**The metadata gate caught a ninth shape, and it is the one that matches itself end to end.**
`Definition.ParameterGroup` exists on 2020 and is gone by 2027; its replacement `GetGroupTypeId` has
never existed on 2020. **Neither spelling works at both ends**, so `REPORT_PARAMETER_INVENTORY` reports
no parameter group at all and says why in its own summary. *A member having a replacement does not make
it portable — check the replacement against the OLD release too.* The same pass corrected two names
before they were written: `MEPSection` is in `Autodesk.Revit.DB.Mechanical` **for pipe systems as well as
duct ones**, and `RoutingPreferenceRule` has `GetCriterion(int)`, not a `GetCriteria()` collection.
Neither guess exists on any release.

**Three routing defects were found and fixed, two of them older than this batch:**

- **A fragment contradicting itself.** `READ_ROOM_GEOMETRY`'s routing comment said *"how big is this
  room → here"* while the table in its own purpose, ten lines above, said `MEASURE_ROOM_DIMENSIONS`.
  Two comments in one file disagreeing is invisible until something reads both.
- **A question answered by a write.** *"What is the Mark on these"* resolved to
  `WRITE_ELEMENT_PARAMETERS`. Declared on `READ_ELEMENT_PARAMETERS` so identity wins.
- **A new fragment stealing a room sentence.** `REPORT_AREAS` was winning *"what is the room area"* on
  rank while `READ_ROOM_GEOMETRY` merely claimed it in a comment. An area and a room are different
  elements — that fragment's own routing table says so — so the sentence was declared where it belongs.

`check-routing`'s claimed-but-not-reached list went from **8 to 4**, and the four left are all older.

### What the next session should do, in order

1. **`python tools/check-gaps.py` first.** Computed from disk; it wins over every sentence here.
2. **On any machine with the .NET SDK: `python tools/check-fragments-compile.py`.** **Eighty** fragments
   now owe a real compile — `A9`, still the largest thing needing no Revit.
3. **With Revit open: start proving.** All 218 are `DRAFT`.
4. **To carry the library build on:** [§9a](#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start).

**How much of the owner's earlier library is left, measured rather than guessed (2026-09-05).** Of its
398 scripts, **68 are not fragments at all** — 44 `recipes/` become skills, 12 `context/` are the host's
job ([D-46](DECISIONS.md)), 8 `commands/` are native Revit commands, 4 are examples and the prelude.
That leaves **330 fragment-eligible**, of which roughly **60 are still worth adding**. It is not a
subtraction: the library is re-authored, several sources fold into one fragment, and some Heron fragments
have no source at all. The biggest remaining blocks are **tag and dimension placement** (~10: auto-arrange
tags, centre room tags, stack tags, L-shape leader, spot elevations), **filters** (~8: by parameter value,
by host, by sub-component), **CAD and model admin** (~8), **revisions** (~5: edit, delete, remove from
sheet — only create/list/cloud exist) and **exports** (~3: view image, FBX, families).

**Measured gaps left open on purpose, with the sentences that found them:**

| The sentence | Where it goes today |
|---|---|
| *"save the view as an image"* | `CREATE_SELECTION_FILTER` |
| *"which title block is on each sheet"* / *"change the title block"* | `CREATE_SHEETS` — a **write** for a question |
| *"why is this element not matching my view filter"* | `DIAGNOSE_VISIBILITY` — about view visibility, not filter rules |
| *"count the fittings per space"* | `CHECK_EQUIPMENT_CLEARANCE` |
| *"where is this element located"* | `FIND_VIEWS_SHOWING_ELEMENT` |

---

## HANDOVER — the session that ran 2026-09-04

**What happened, in one line: the library went from 202 fragments to 210, and every one of the eight was
built because a real sentence was measurably reaching a fragment that WRITES.**

**The selection rule did the work again, and this time it found a cluster rather than eight singles.**
Asking `heron_brain.lookup` the owner's own sentences against the library as it stood, every question
about a schedule was being answered by `ADD_SCHEDULE_FIELDS` — the fragment that adds a column. It had
become the sink for the whole subject because it was the only schedule *editor* in the library. Two of
the misroutes were not near misses at all:

| The sentence | Where it went | What that would have done |
|---|---|---|
| *"remove a column from the schedule"* | `ADD_SCHEDULE_FIELDS` | put the column back |
| *"filter the schedule to level 2 only"* | `SET_ELEMENT_LEVEL` | **moved the ducts to another level** |

The second is this project's whole reason for existing, found in its own library: a request to hide rows
in a table, resolving to a fragment that changes the building, and the schedule looking right afterwards.

**Reading the neighbours paid again, and differently.** `FIND_SCHEDULES` already named *"adding columns,
exporting to CSV, placing on a sheet"* in its own purpose as the things it hands schedules to — **two of
the three had never been built.** `PLACE_VIEW_ON_SHEET` was refusing schedules correctly, naming *"a
schedule that needs a different call"*, with nothing to hand them to. The library had been describing
this batch for weeks. `CREATE_SCHEDULE` carried a routing row reading *"nothing in the library exports
anything yet"*, true when written and left standing after five export fragments landed — **a routing row
that ages into a lie points a reader at nothing**, and it is the cheapest kind of drift to miss because
nothing about it looks wrong.

**Two capabilities were found to be impossible, and are recorded rather than left to be re-derived:**

- **A calculated (formula) column cannot be created.** `ScheduleField` carries no formula member on any
  release 2020 → 2027. The field type exists in the enum; the expression cannot be written.
- **A schedule column's UNIT cannot be set in one implementation.** It is `UnitType` on 2020 and
  `GetSpecTypeId` from 2022 — the first gone by 2027, the second never on 2020. Every fragment here is
  one implementation for all eight releases, so this is not a fragment.

Both are routing rows naming Revit's own route, in both fragments a reader could land on.

**The metadata gate caught an eighth shape, and it is one an existence check structurally cannot reach.**
`ScheduleField.HorizontalAlignment` looks like it takes `HorizontalAlignmentStyle`. Both that enum and
`ScheduleHorizontalAlignment` exist on every release — so **every name check passes on both and exactly
one compiles.** It was settled by decoding the property signature's type token out of the metadata blob:
`ScheduleHorizontalAlignment`, on 2020 and on 2027. Where two plausible types both exist, presence proves
nothing.

**And the probe itself was wrong first.** It reported `ScheduleFilterType` missing on all three releases
when it is present on all of them — it was splitting a bare type name into a type plus a member. *A
checker that finds nothing is evidence about the checker; one that finds something FALSE is the same
lesson with the sign flipped, and it is more convincing because it looks like a result.*

**One thing believed blocked was not.** `test_mcp_serves` was recorded here as blocked on the MCP SDK.
`pip install mcp` ran it, and it passed — 18 suites, **17 pass, 1 blocked**. That is the fifth time
something filed under *needs a machine* needed somebody trying it. The .NET SDK genuinely is blocked:
the proxy answers `403` to the CONNECT for `builds.dotnet.microsoft.com` and names the denial.

### What the next session should do, in order

1. **`python tools/check-gaps.py` first.** Computed from disk; it wins over every sentence here.
2. **On any machine with the .NET SDK: `python tools/check-fragments-compile.py`.** **Eighty**
   fragments now owe a real compile. This is `A9` and it is still the largest thing needing no Revit.
3. **With Revit open: start proving.** All 218 are `DRAFT`. [D-30](DECISIONS.md) means a proof needs
   a case that comes back EMPTY, not just one that works.
4. **To carry the library build on:** [§9a](#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start).
   Run the lookup sweep first. Un-mined source areas left are the rest of `actions/reporting/`,
   `actions/color-graphics/`, `actions/visibility/` and most of `filters/`.

**Measured gaps left open on purpose, with the sentences that found them** — each one is a real misroute,
none was guessed at:

| The sentence | Where it goes today |
|---|---|
| *"save the view as an image"* | `CREATE_SELECTION_FILTER` |
| *"which title block is on each sheet"* / *"change the title block"* | `CREATE_SHEETS` — a **write** for a question |
| *"what is the bounding box of this duct"* | `FILTER_ELEMENTS_IN_ROOM` |
| *"what parameters can I filter on"* | `REPORT_GLOBAL_PARAMETERS` — global parameters are a different thing |
| *"which families are nested inside this one"* | `CHECK_FAMILY_STANDARDS` |
| *"what is the pressure drop in this duct run"* | `SET_MEP_SLOPE` — a **write** for a question |
| *"report the routing preferences of this pipe type"* | `CREATE_MEP_SYSTEM_TYPE` |

**One observation for whoever reads `check-intrusion` next:** two of this batch's fragments
(`READ_SCHEDULE_CONTENTS`, `EXPORT_SCHEDULE_TO_CSV`) went straight into its top twelve. That is the
generic-phrasing effect the tool already documents — *"read me what is in the schedule"* shares words
with half the library — and the tool's own guidance holds: **those are real sentences and none of them
may be taken away to buy a number.** Left as they are, deliberately.

---

## HANDOVER — the session that ran 2026-09-02 into 2026-09-03

**What happened, in one line: the library went from 138 fragments to 202, in eight batches of eight, and
was merged to `main`.**

**The method, which is the part worth keeping.** Every batch started the same way: ask
`heron_brain.lookup` the owner's own sentences against the library as it then stood, and write down which
ones came back wrong. That is the whole selection rule — **nothing was built because it seemed useful; it
was built because a real sentence was measurably going somewhere else.** Then read the top three to five
candidates' `purpose` before writing anything, verify every API member against the Revit reference
assemblies for 2020, 2024 and 2027, write the three files, measure routing again, check every new
collision through `lookup`, add the cross-reference tables BOTH ways, run every checker, commit.

**Reading the neighbours before building is what earned the most.** It stopped seven fragments from being
written that already existed under another name — cable tray and conduit are one fragment, spaces are
`PLACE_ROOMS`, clearing a category override is `SET_CATEGORY_GRAPHICS` with an empty settings object, and
so on. The batch that skipped that step built a duplicate and had to withdraw it the same hour. **A
lookup can show what is missing; only reading the candidates shows what already exists under a different
name.**

**The metadata substitute for the compile gate paid for itself seven times**, and the shapes it caught
are worth knowing because a compiler on the PC will not teach them again:

| # | Shape | Example |
|---|---|---|
| 1–3 | A member the OLD release never had | `Ceiling.Create` is absent on 2020 |
| 4 | A member the NEW release removed | `GlobalParameter.IsValidDataType` is gone by 2024 |
| 5 | A whole CAPABILITY removed | Revit 2027 has no HVAC zone creation at all |
| 6 | Reflection that still names a missing TYPE | the delete-workset lookup named a 2020-absent type |
| 7 | An OVERLOAD whose ARITY changed | the filter rule takes 3 arguments on 2020, 2 by 2027 |

**Five impossibilities are now recorded rather than re-derived** — a scope box cannot be created, a design
option cannot be made active, the first legend cannot be created, a wall cannot be split, and a phase
cannot be created. Each is a routing row that says so and names the Revit route instead.

### What the next session should do, in order

1. **`python tools/check-gaps.py` first.** It is computed from disk and wins over every sentence here.
2. **On any machine with the .NET SDK: `python tools/check-fragments-compile.py`.** Sixty-four fragments
   owe a real compile. This is `A9`, and it is the largest thing that needs no Revit.
3. **With Revit open: start proving.** Every one of the 202 is `DRAFT`. [D-30](DECISIONS.md) means a
   proof needs a case that comes back EMPTY, not just one that works.
4. **To carry the library build on instead:** [§9a](#9a-continuing-the-library-build--the-recipe-so-another-session-can-just-start),
   and run the lookup sweep first — the un-mined source areas left are `actions/sheets-views/`,
   `actions/reporting/` and the rest of `filters/`.

**One gap measured and deliberately left:** *"What changed between this model and the old one"* still
routes to `SET_ELEMENT_WORKSET`. `COMPARE_ELEMENTS` compares elements inside one document and cannot be
it. A real model-to-model compare needs a second document opened or linked, and that API deserves
checking properly rather than guessing.

---

## WHERE THIS STANDS, 2026-09-05 — 218 fragments, and eighty have never seen a compiler

The library is still growing away from the PC. What is left to *prove* still needs a machine this
container does not have — and as of the twentieth session there is one more thing on that list that
needs only the **.NET SDK**, not Revit and not Windows.

| | |
|---|---|
| Fragments | **218**, every one `DRAFT`. 138 compiled on all eight Revit releases; **the newest 80 have not been compiled at all** — see `A9` |
| Skills | 10, none naming a fragment |
| Tools the host sees | 10, served by a real MCP SDK |
| Test suites | 18 — **17 pass, 1 blocked**: `test_bridge_roundtrip` needs the .NET SDK. `test_mcp_serves` was listed as blocked here until 2026-09-04 and was not: `pip install mcp` in the container ran it, and it passed. Measured, not remembered |
| Checkers | 8, all green |
| `check-gaps` | **0 unfinished, 55 waiting** |

**Nothing is unfinished. Nothing is proven.** Those are different sentences and both are true: every
buildable thing is built, and not one fragment has touched a real model.

> **The compile gate is the first thing to run on a machine that has the SDK** — `python
> tools/check-fragments-compile.py`. It is now `A9` in the register. Eighty fragments were written
> on 2026-09-02 to 2026-09-05 in containers where the SDK could not be installed, because
> the download host is refused by those networks' policy — re-checked on 2026-09-04, and the proxy
> *names* the denial rather than timing out, so it is policy and not a slow link. Every API member they
> use was checked against the real Revit reference assemblies for 2020, 2024 and 2027 instead, which is
> not the same thing and is not a substitute — though it has now caught **nine** real errors before
> they were compiled, across six distinct shapes. `A9` lists them.

### The one thing that matters next, and it needs the PC

`D3` and the fragment library. **Every fragment is `DRAFT` and stays there until it is run against a
real model with a case that comes back EMPTY** ([D-30](DECISIONS.md)) — `python
tools/check-gaps.py` counts them, and no number is typed here for the reason the rest of this file
keeps re-learning. That is the owner's job and
it is the largest remaining piece of work in the project. Everything else waiting is smaller:

- **`A7`** — the trained embedding backend has never run: the weights host is unreachable from here.
  It has moved from the least urgent item to **the most urgent one**, because the built-in n-gram
  backend has measurably saturated at this library size (see the seventeenth session below).
- **`A4`, `A6`** — need Windows.
- **`A8`** — needs the MCP SDK installed on the PC; everything else about it is done.
- **`R1b`** — a conversation.

### Two numbers to distrust until a model has been in front of them

- **`READ_SPACE_LOADS`'s load unit.** Airflow is exact arithmetic and provable on paper; the HVAC power
  unit is *assumed* to be BTU/s. If that is wrong every load is out by a constant factor — each figure
  looks plausible, comparisons between spaces still work, and only somebody sizing real equipment finds
  out. It is the fragment's first negative test case.
- **`CREATE_DIMENSION`'s reference technique.** If Revit does not surface a referenceable centreline the
  way it is assumed to, the fragment draws nothing and says so — a designed failure, but a failure.

---

## The first five minutes of the next session

**Run this before reading anything else. It is computed from disk, so it wins over every sentence
below:**

```bash
python tools/check-gaps.py
```

It sweeps the build order against what is actually on disk, runs every test and every checker, checks
every agent id against the registry, walks the fragment library, the capability registry and the
dependency graph, and reads the register. Then it sorts everything into **UNFINISHED** and **WAITING**,
and its exit code follows only the first.

**As of the twentieth session, 2026-09-02, it reports 0 unfinished and 56 waiting.** Everything waiting
needs a machine, a dependency or a conversation: **48 a real Revit**, plus the unproven fragments as one
further item, **Windows** (`A4`, `A6`, `A8`), **1 the .NET SDK** (`A9` — the eight fragments no compiler
has read), **1 a network that can reach the weights host** (`A7`), **1 the owner** (`R1b`), and **1 an
optional dependency this machine does not have** — `test_mcp_serves.py` reporting honestly that it was
skipped, rather than being counted as a pass.

> Those figures were **read off the tool, not carried forward**, and the sentence they replace shows why
> that matters: it said *55 waiting* and then listed parts summing to 54, and it still counted **3 owner
> items** after `R1` and `R2` had been struck off in the same session. A total and its own breakdown
> disagreeing is the cheapest possible drift to catch and it survived anyway. Re-derive rather than
> edit the digits.

**`A8` is mostly done, and doing it found the worst defect in this repository's history — read this
one first.** It was written down as *needing Windows*. It did not: the blocker was a missing **pip
package**, and the MCP SDK is pure Python. Installing it took ten minutes and produced two things.

**A real SDK now serves all ten tools** — names, descriptions and argument schemas — and the three brain
tools answer through its own dispatch with both refusals intact. That is the half `test_brain_reachable.py`
could only ever read *as text*, and it is now [`tests/test_mcp_serves.py`](../tests/test_mcp_serves.py).

**And installing it revealed that Heron's MCP server would not start at all on a fresh machine.**
`pip install --user mcp` — the exact line [`tools/HeronRevit.ps1`](../tools/HeronRevit.ps1) hands the user —
now resolves to SDK **2.x**, which **deleted `mcp.server.fastmcp`**: `FastMCP` was renamed `MCPServer`.
The server's import was written against 1.x, so it raised `ImportError` before registering a single tool.
**Every Heron tool absent from the host, on any machine installing today, with no Revit and no Windows
involved.** Nothing in the repository could see it, because nothing here had ever imported the SDK — the
entire suite reads that file as text, and text cannot fail an import.

The fix is the import and nothing else: the class is looked up newest-first, because 2.x's `MCPServer`
takes the same `@server.tool()` decorator and the same `run()`. **Proven on both SDK majors installed
side by side**, and validated the way this repository requires — the old line was put back and watched
to fail. `heron_version` now reports which SDK is serving, since *"Heron stopped working"* and *"the SDK
moved underneath it"* look identical from the user's side.

**What genuinely remains of `A8` needs the PC:** a real host over stdio — that Claude Code connects,
renders the docstrings and picks a tool from them. In-process dispatch is a strong signal ahead of that,
not a substitute for it.

**If the tool and this file ever disagree, believe the tool.** It is computed from disk; this file is
typed. That is not a hypothetical — for most of 2026-08-29 this tool reported `UNFINISHED - nothing`
while the entire Phase 2 brain sat unreachable, because it checked that each step's module and test
existed and never asked whether anything called them. The check that catches it was added the same day.

### Then, in order

| | What | Where it happens |
|---|---|---|
| **1** | ~~`R1`~~ **DONE 2026-08-29 — all 21 read back, all 21 confirmed, nothing moved.** What is left of the review is **`R1b`**: show him the trust model working with his own fragments in it, because [D-14](DECISIONS.md) stays *Proposed* until he has seen it | Needs a screen |
| **2** | **`B1` to `B4`** — open Revit 2020, look for the **Heron AI** tab, press **Heron**, then `ping` and `count` | Needs Revit |
| **3** | **`C3`** — with `write.enabled` still **false**, ask for a move and watch it be **refused, by name**. Prove the gate before testing the write, or a passing move proves nothing | Needs Revit |
| **4** | **`D1`–`D3`** — *"move the ducts up 200 mm"*, say yes, then **MEASURE ONE**. The single most important line in the whole register | Needs Revit |
| **5** | **`D5`** — **one** Ctrl+Z puts it all back. Two means Golden Rule 16 is broken | Needs Revit |

Everything else in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) follows in the order written there. It is in
dependency order on purpose: nothing in group D can be attempted before group A passes.

### And the ones that need no Revit at all

- **`A7`** — the trained embedding backend has **never run**, and 2026-08-31 sharpened *why* on a second
  and different container. `pip install model2vec` **works** — PyPI is reachable and the package is
  fine. What fails is `StaticModel.from_pretrained("minishlab/potion-base-8M")`, with
  `ProxyError: 403 Forbidden`. **So the precondition is not "a working network", it is reaching
  huggingface.co**, and the register row now names the host instead of saying "a model host" — a
  sentence anyone with a working network would reasonably read as already satisfied. Until it runs,
  search finds words and not meaning, and [`tests/test_embed.py`](../tests/test_embed.py) says so in
  measured numbers. **One thing it did prove**: with the package actually installed, the fallback ran
  against a *failed download* rather than a missing import — a branch that had never once executed —
  and it degraded to `lexical` and said so on its first line.
- **`A4` and `A6`** — both need Windows but not Revit. `A4` is the Windows named pipe itself; `A6` is
  thirty seconds confirming the SDK probe reads Windows correctly.
- **`A8` was on this list and nobody knew it**, filed under *needs Windows* when what it needed was
  `pip install mcp`. That is the fourth time something here turned out to be waiting on somebody trying
  it rather than on a machine. **The count is now four, and the standing advice stands: assume the
  fifth is out there.** The one that cost the most was believing an environment-specific wall was a
  property of the project.

### The one thing that was buildable here — now built

**The brain was wired to nothing, and now it is wired.** Eight modules, seven fragments and ten skills
were on disk, tested and passing, with **no MCP tool reaching any of them.** The host talks to Heron only
through the tools in [`mcp/server/heron_tools.py`](../mcp/server/heron_tools.py), and every one of them went
straight to the bridge. That left Phase 2's third definition-of-done clause open for **no external reason
at all**, which is why it was worth doing on a machine with no Revit.

**What was added**, all of it read-only and none of it touching a model:

| | |
|---|---|
| [`mcp/server/heron_brain.py`](../mcp/server/heron_brain.py) | The one seam between the MCP side and `brain/`. Opens the store, rebuilds it if the machine is fresh, indexes if stale, and hands back rows. **It holds no knowledge of its own** |
| `heron_capabilities` | What Heron knows how to do: ten jobs, which have every part provided, and the seven capabilities nothing provides |
| `heron_resolve` | Who can do one capability, at what risk, on which releases — **asked for by capability, never by fragment id** |
| `heron_lookup` | The user's own sentence resolved to a **capability**, with the provider underneath as evidence rather than as the answer |
| [`tests/test_brain_reachable.py`](../tests/test_brain_reachable.py) | Step 12's acceptance test re-run through the seam: add a better provider and the call site is the same line; delete the original and it still answers |

**Two things it deliberately does not do, and every answer says both out loud:**

- **Resolving is not running.** A fragment carries C# in `impl/`, the bridge speaks a fixed set of
  operations, and none of them compiles one — [D-28](DECISIONS.md)'s in-process Roslyn is unbuilt.
  So the host can now learn *what would do the job* and still cannot have it done. A tool that let that
  be inferred would be worse than no tool, because a plan built on it fails at the last step.
- **Nothing underneath is proven.** Every skill and every fragment is still `DRAFT`.

**The version filter reaches the host as a wall, not a preference.** The release comes from the bound
Revit session, read **without ever asking and without claiming a lease** — looking must never be the act
of claiming, which is the lesson `revit_health` learned about the lease one commit after building it.
With no Revit connected the tools still answer and say the filter did not run.

**The original finding came from running `tools/check-metadata.py` and following what it said**, hours
after `check-gaps.py` had reported everything clean. `check-gaps` now has a check for it — and that check
is an **import** check, so treat its green accordingly: it can see that the seam exists, not that a host
ever called through it. That is `A8`.

### The library, and the question that was put to the owner

**This section used to say seven capabilities were wanted by the skills and provided by nothing, and to
ask before building them.** It was right to ask. He answered, and the answer changed the shape of the
work: build the fragments now, re-authored from his earlier library, and check every one of them in Revit
later — *"checking in revit we will do after because that is a big work... mark as not verified and when
pc came we will check."*

**So the library is 146 fragments and every skill has every capability provided.** The seven were written
on 2026-08-29; twenty-five more followed on 2026-08-30, and seven more on 2026-08-31 — those last chosen
by asking the brain the owner's own sentences and reading what came back, rather than by working through
the earlier library in order. `python brain/heron_skill.py` shows no gaps.

**And his second instruction is the one that must not be quietly undone.** None of them inherits the
earlier library's proven status, however well proven it is there:

> *"Even in the aj ai proven fragment dont mark in heron this is proven because we will check each and
> everyone again in heron ai so mark it as a not proven in heron."*

That is [D-44](DECISIONS.md), and it is **enforced rather than remembered** —
[`brain/heron_fragment.py`](../brain/heron_fragment.py) refuses a status above `DRAFT` whose proof does not
match the implementation in front of it. The gate had existed and nothing stood on it: a fragment
declaring `PROVEN` on another model's proof passed every check in this repository, which was measured by
writing one.

**What that buys, and what it does not.** All one hundred and two fragments compile on all eight releases and
none of them will fail at the PC for a reason a compiler could have found — which on 2026-08-31 stopped
being a figure of speech, when the gate caught a tag accessor that Revit 2027 has removed. Not one has met a model. The debt did
not go away — it got **counted**, which is the whole point of `check-gaps` keeping *unfinished* and
*waiting* in two lists that must never be one.

---

## The twenty-seventh session, 2026-09-03 — eight more, and the API check found a third shape of version break

**What it did:** took the library from **194 to 202**. Same container, same limitation. `A9` now names
**sixty-four**.

| | |
|---|---|
| `CREATE_VIEW_FILTERS_BY_VALUE` | *"Make a filter for every different system type"* was answering `CREATE_VIEW_FILTER`, which makes ONE. **Deferred twice as too close to `COLOR_BY_PARAMETER` and built on the third asking**, because reading both purposes settled it: that one writes per-element OVERRIDES and an element drawn tomorrow gets nothing; this writes real FILTERS that re-evaluate forever. Investigate with one, set a standard with the other. **The category set comes from the elements, never guessed** — a filter naming a category that lacks the parameter is rejected by Revit *entirely*, so being helpful about it destroys the whole run |
| `REMOVE_VIEW_FILTER` | *"Take that filter off the view"* was answering `COPY_VIEW_FILTERS`. **Off one view and deleted from the project are completely different in reach**, so deleting is a separate flag and the fragment reports how many OTHER views use it first — a filter on one view is a leftover, one on twenty is somebody's standard |
| `REPORT_CATEGORY_VISIBILITY` | *"Which categories are turned off in this view"* was answering `SET_CATEGORY_VISIBILITY` — the fragment that turns things off. **It deliberately does NOT scope to the view, which is the opposite decision from `REPORT_CATEGORY_OVERRIDES` built two sessions ago**: a hidden category's elements do not appear in a view-scoped collector at all, so scoping would hide exactly what it exists to find. The two look alike and the difference is not a style choice |
| `SET_CROP_BOX_SETTINGS` | *"The tags are printing outside the crop"* had no answer. **The annotation crop is the half nothing else covers and the one that spoils sheets** — the model crop trims geometry while tags and dimensions keep printing outside it. Each flag is optional and empty means LEAVE ALONE, because a batch that forces three settings is how a drawing set loses its crop boundaries overnight |
| `CHECK_SURFACE_FIT` | *"Is the equipment sitting flat on the floor"* was answering `SNAP_TO_GRID` — which moves things. **A single centre ray is right in the middle of a surface and lies at the edges, and edges are where the mistakes are.** Five sample points, four named verdicts: STRADDLING, OVERHANGING, UNEVEN, SLOPED. Its real use is deciding which elements are safe to move automatically |
| `CREATE_MEP_SYSTEM_TYPE` | *"Make a duct system type"* was answering `READ_MEP_SYSTEM`. **There is no create call on any release** — one is made by duplicating — so the fragment's real job is the parent: the new type inherits its classification and **that cannot be changed afterwards**. Copy a Return to make a Supply and it behaves as a Return forever while reading correctly on every drawing |
| `REMOVE_PARAMETER_VALUE` | *"Clear the value out of this parameter"* was answering `COPY_PARAMETER_VALUE`. **`WRITE_ELEMENT_PARAMETERS` cannot do this, and the reason is its own best decision** — it takes the value as TEXT on purpose, and an empty string clears a text field but not a length, a number or an element reference. A blank and a zero are different things in a schedule |
| `ASSIGN_LOCATION_DATA` | *"Put the room name on all the equipment"* was answering `FIND_DUPLICATE_ELEMENTS`. Not the parameter writer, because **the value is not given — it is worked out per element**. The probe point is nudged to the room's own mid-height, and without that a ceiling diffuser tests false from its own position: the obvious version leaves every air terminal blank, which is exactly the set somebody built the register for |

**The API check earned its keep a seventh time, and on a third distinct shape of version break.** The
first three were members a release does not ship. The fourth was a member present on the OLD end and gone
by the new. The fifth was a whole capability removed. The sixth was reflection that still named a missing
TYPE in its own lookup. **This one is an OVERLOAD whose ARITY changed**:
`ParameterFilterRuleFactory.CreateEqualsRule` for a text value is `(ElementId, string, bool)` on Revit
2020 and `(ElementId, string)` by 2027 — the case-sensitivity argument was dropped. Both releases HAVE
the member; the member's name gives nothing away; **each spelling compiles on exactly one end and breaks
the other**, and only reading the two signatures byte for byte shows it. `CREATE_VIEW_FILTERS_BY_VALUE`
chooses the overload at run time by argument count. **A member being present at both ends does not mean
the call is.**

**Four sentences were already answered and are recorded rather than rebuilt.** *"Put the categories back
to normal in this view"* → `SET_CATEGORY_GRAPHICS` with an EMPTY settings object, which its own purpose
already names as the reason there is no separate clear. *"Which pipes are still open at the end"* →
`FIND_DEAD_ENDS`. *"Make a named set I can pick again later"* → `CREATE_SELECTION_FILTER`. *"Create
spaces in every enclosed area"* → `PLACE_ROOMS`, which places rooms **or** spaces. All four were declared
as utterances so the routing now matches the decision.

**A fifth impossibility joins the list:** **a phase cannot be CREATED from the API on any release** —
`Document.Phases` is a read-only collection and no factory offers one, checked at both ends. That is now
a routing row on `REPORT_PHASES`, alongside the scope box, the design-option activation, the first legend
and splitting a wall.

**And a small thing worth the sentence.** A routing row I wrote this session claimed *"which ones have no
value"* for `DESCRIBE_BLANK_PARAMETERS` — a sentence `READ_ELEMENT_PARAMETERS` already declares. The
checker caught it, and the fix was to correct **my table** to a sentence that fragment really declares,
not to move the sentence. A routing table is a claim about the index, and the index is the authority.

---

## The twenty-sixth session, 2026-09-03 — eight more, and two checks caught the session's own mistakes

**What it did:** took the library from **186 to 194**. Same container, same limitation. `A9` now names
**fifty-six**. Four of the eight close a mis-route where a QUESTION was being answered by a fragment that
WRITES — the shape this library treats as the worst.

| | |
|---|---|
| `CHECK_FAMILY_STANDARDS` | *"Check the families are named to our standard"* was answering `RENAME_FAMILY` — a write, to a question. **It audits TYPES, not instances**: one badly built type placed two hundred times is ONE thing to fix, and an instance report buries everything else in it. **Connectors are read through a placed instance**, because a type does not expose them — so a type placed nowhere is NOT CHECKED rather than passed, and that distinction is what separates an audit from a guess |
| `CHECK_CEILING_COORDINATION` | *"Are the light fittings sitting properly in the ceiling tiles"* was answering `SNAP_TO_GRID` — which MOVES things. **The fault it catches is invisible in plan**: a diffuser 40 mm above the ceiling and one exactly in it look identical from above. NO CEILING ABOVE is a finding, not an error — an open soffit is fine |
| `CHECK_EQUIPMENT_CONNECTORS` | *"Are the equipment connectors the right size for the duct"* was answering `CHECK_FLOW_DIRECTION`. **The unconnected spigots are usually the bigger finding** — no clash test and no connectivity walk flags them, because the services that ARE connected trace perfectly. **Size comparison is shape-aware**: round reports a radius and rectangular reports width and height, and comparing them as one number is how this kind of check produces confident nonsense |
| `CHECK_FIXTURE_CONNECTIVITY` | *"Are all the sinks and toilets connected to the drainage"* was answering `MEASURE_MEP_SLOPE`. **It reports PER SERVICE**, which is the only actionable form — *"nine WCs have no vent"*, not *"twelve fixtures have an unconnected connector"*. And it is not `FIND_DEAD_ENDS` from the other end: **a fixture nobody piped produces no open pipe end at all**, so that fragment structurally cannot see it |
| `REPORT_CATEGORY_OVERRIDES` | *"What category overrides are on this view"* was answering `READ_GRAPHIC_OVERRIDES` — **whose own purpose says in writing that it reads the per-element override only**. A question answered by a fragment that structurally cannot answer it, coming back empty and reading as *"nothing is overridden"*. **The visibility flag is not the signal**: a pattern's own "is visible" reads TRUE with nothing set, so a check built on it reports the whole model as overridden |
| `REPORT_DESIGN_OPTIONS` | *"Which design option am I working in"* was answering `REPORT_SPACE_AIRFLOW`. A design option changes what *"all the ducts"* means — a count that disagrees with the screen, a short schedule and an element that will not delete all have this one cause. **The active option cannot be changed from code on any release**, checked at both ends, and `Element.DesignOption` is read-only too |
| `RENAME_WORKSET` | *"Rename this workset"* was answering `SET_VIEW_WORKSET_VISIBILITY` — which changes a drawing. **A workset is not an element**; it lives in the workset table, which `RENAME_ELEMENTS` cannot reach. Deleting one is absent from 2020 and present from 2024 — read at both ends, not inherited |
| `CREATE_GRIDS` | *"Make a grid series across the building"* was answering `SNAP_TO_GRID`. Bay dimensions are the input, which is how a drawing states a grid. **They are GAPS, not positions** — four of them make FIVE grids, and both readings look sensible to somebody checking the code |

**The API check earned its keep a sixth time, and this time on code this session had already written.**
`RENAME_WORKSET` reaches `DeleteWorkset` **by name** because that call is absent from Revit 2020 — and
the lookup then named `DeleteWorksetSettings` in its argument list, **a type that does not exist on 2020
either**. The reflection was protecting the build from one missing member while breaking it on another,
on the very line meant to protect it. Nothing in the fragment's own logic would have shown this; the
metadata read printed `TYPE NOT FOUND` and it was fixed before it was ever compiled. **Reaching a call by
name is not enough — every TYPE named in the lookup has to exist on the oldest release too.**

**And `test_embed.py` caught the other one, in a way worth copying.** `CREATE_GRIDS` took
`READ_CEILING_GRID`'s own declared words: *"what is the ceiling grid spacing"* ranked the new fragment
first. The test's failure message **names the thief** rather than just failing, so the diagnosis was one
line long. The fix is the interesting part: the repository's rule is that weakening an utterance somebody
actually says, to buy back a rank, is never the answer — so nothing on `READ_CEILING_GRID` was touched.
What changed was **my own prose**: an invented utterance in developer-speak, and a `purpose` that said
*"spacings"* five times because the gaps-not-positions point was repeated. The keyword index reads the
purpose as well as the utterances, which is what made repetition a ranking act rather than a stylistic
one. Both were tightened; every point the fragment made survives.

**Three sentences were already answered and are recorded rather than rebuilt.** *"Turn the whole category
to halftone"* → `SET_CATEGORY_GRAPHICS`, which takes a prepared settings object and therefore already does
halftone and transparency. *"Put the elements back that I hid"* → `SHOW_ELEMENTS`. *"Which filters are set
up but never used"* → `REPORT_VIEW_FILTERS`, whose purpose already names the unused row as the one worth
finding. And *"is there room for the insulation on these pipes"* → `CHECK_MINIMUM_CLEARANCE`, which folded
insulation in as a flag on purpose rather than keeping it as a second fragment.

**A gap this batch measured and deliberately left:** *"What changed between this model and the old one"*
still answers `SET_ELEMENT_WORKSET`. `COMPARE_ELEMENTS` compares elements inside one document and cannot
be it. A real model-to-model compare needs a second document opened or linked, and designing that
honestly needs the open-document API checked properly rather than guessed at — it belongs to a batch that
can give it the room.

---

## The twenty-fifth session, 2026-09-02 — eight more, six of them MEP coordination

**What it did:** took the library from **178 to 186**. Same container, same limitation. `A9` now names
**forty-eight**. This batch went deliberately at the owner's own trade: six of the eight are the checks a
BIM modeller runs before a coordination issue.

| | |
|---|---|
| `READ_GRAPHIC_OVERRIDES` | *"Make this pipe look the same as that one"* was answering `CREATE_PIPE`. **This is the gap the twenty-fourth session measured and left on purpose**, and closing it took one small read. `OVERRIDE_GRAPHICS_IN_VIEW` takes settings ALREADY BUILT — nothing could produce them from an element that already looked right, so matching had no answer at all. An element with NO override still returns the empty settings object, because applying that CLEARS another element's override, which is a real use |
| `CHECK_SLEEVE_SIZE` | *"Are the sleeves big enough"* was answering `MEASURE_MEP_SLOPE`. The service is found by GEOMETRY — a sleeve family records nothing about what goes through it. **A sleeve with nothing through it is the finding**, not a blank row: either an orphan, or the run moved and the hole did not. Required size is arithmetic with every term explicit, and the insulation is read from the real element because a 50 mm jacket turns a comfortable sleeve into a tight one |
| `CHECK_EQUIPMENT_CLEARANCE` | *"Is there enough space in front of the panel"* was answering `MEASURE_MEP_SLOPE` too. **The zone is DIRECTIONAL and that is the whole point** — an AHU needs 1500 mm in front and 200 mm behind, and a sphere either passes real obstructions or fails on the wall the unit is meant to stand against. Built on the family's own facing direction, which is REPORTED per unit, because a family authored facing the wrong way makes this confidently wrong and nothing else would say so |
| `CHECK_VALVE_ACCESSIBILITY` | *"Can we reach the valve to operate it"* was answering `LIST_GRIDS`. **Three questions, answered separately, because each has a different fix**: room, an access panel where it sits above a ceiling, and reach height. Above a ceiling is NOT a fault — most valves are — so it is a list for the architect rather than a failure. A check that cries wolf about every valve gets switched off by lunchtime |
| `CHECK_VERTICAL_CLEARANCE` | *"How much room between the duct and the pipe above it"* was answering `MEASURE_ELEMENT_LENGTHS`. **Not `CHECK_MINIMUM_CLEARANCE` with a smaller number**: two services 200 mm apart diagonally have 200 mm of straight-line clearance and may have 40 mm of vertical room, which is what a hanger and a flange need. It says which one is ON TOP, so drainage above the duct it must cross under reads as a fact |
| `AUDIT_MEP_OPENINGS` | *"Which of my openings are wrong now the ducts moved"* was answering `READ_ELEMENT_OWNERSHIP`. **The trap it is built around**: a cut void is a Revit `Opening` with NO SOLID, a placed sleeve is a family instance that has one, and the obvious way to gather openings returns mostly the first — so an audit written for the second reports "no geometry" for all of them, with no crash and no error, and audits nothing |
| `CREATE_ROOM_ELEVATIONS` | *"Make elevations inside each room"* was answering `FILTER_ELEMENTS_IN_ROOM`. A marker with four slots, not a section — nothing about a section makes the four-arrow symbol a drawing set expects. **The slots are NOT rotated to face the walls**, so in a rotated room the views look at corners, and that is said in the report because it is invisible until somebody lays out the sheet |
| `CREATE_HVAC_ZONE` | *"Create an HVAC zone"* was answering `PLACE_ROOMS`. See below — this one is a first for the library |

**The first fragment here whose capability a Revit release REMOVES.** Read off 2027's own reference
assembly rather than taken from a note: the creation factory has **no zone method left**, and
`Zone.AddSpaces` and `RemoveSpaces` are **gone from the `Zone` class** — while `Zone` itself, `Zone.Spaces`
and `Zone.Area` remain, so the type still existing proves nothing. Both vanished calls are therefore
reached BY NAME, which keeps one source compiling on all eight releases, working on 2020 through 2026, and
**reporting a plain reason on 2027** instead of failing. Naming either directly would break the 2027
build, and a try/catch does not help — it never gets to run.

**Three sentences turned out to be already answered, and reading the neighbours is what showed it.**
*"Draw a cable tray here"* and *"draw the conduit"* → `CREATE_ELECTRICAL_RUN`, which already covers both
and says in its own purpose why they are one fragment. *"Place spaces in all the rooms"* → `PLACE_ROOMS`,
which already places rooms **or** spaces. *"Save these as a named selection set"* → `CREATE_SELECTION_FILTER`.
All three were routing correctly at #1 already; the batch just confirmed it rather than building beside them.
**Two more were dropped for being too close to something that exists**: a filter-per-value fragment sits
too near `COLOR_BY_PARAMETER` to be worth the crowding, and it waits for evidence rather than a guess.

**The orphan check earned its keep again, and the fix was better than last time's.** `CREATE_HVAC_ZONE`
named its input `spaces`, which nothing provides. Last batch the same report was answered by marking the
input `source: request`; here the honest answer was different — `FILTER_ELEMENTS_BY_CATEGORY` **does**
provide it, under the library's own name `elements`. Renaming the need made the composition real instead
of declaring it unfeedable. **Worth remembering: an orphan report is a question about the NAME first and
about the source second.**

**And a mis-route the batch found in passing, in a fragment it did not write.** *"What is too close to
what"* was answering `FIND_NEAREST_ELEMENTS` — while `CHECK_MINIMUM_CLEARANCE`'s own purpose says, in
capitals, that it is NOT that fragment with a threshold. The routing table said one thing and the index
had never seen the sentence. Declared, and it now routes by identity.

---

## The twenty-fourth session, 2026-09-02 — eight more, and two capabilities that already existed

**What it did:** took the library from **170 to 178**. Same container, same limitation. `A9` now names
**forty**.

| | |
|---|---|
| `COPY_VIEW_FILTERS` | *"copy the view filters from this view to the other views"* was answering `REPORT_VIEW_FILTERS` — a read, to a request to change twelve drawings. **Adding a filter is not copying it**: `AddFilter` carries EMPTY overrides, the look is a second object and the on/off state a third. Copy one of the three and twelve views hold the right filters and look identical, which reads as the job not having run |
| `REMAP_LINE_STYLES` | *"change all the old line styles to the new ones"* was answering `CREATE_FLOOR`. **Lines live where a selection cannot reach** — inside sketches, and inside filled region borders, which are not curve elements at all. And Revit gives a setter for a region's border style and **no getter**, so there is no honest way to change only the offending ones; the fragment says which of the three choices that leaves rather than pretending |
| `SET_VIEW_WORKSET_VISIBILITY` | *"turn off this workset in this view"* was answering `SET_CATEGORY_VISIBILITY` — a category is what a thing IS, a workset is who OWNS it. **A workset has three states in a view, not two.** Turning the wanted one on without pushing the rest to Hidden leaves them on Use Global Setting, which is usually visible: the view shows everything and the call looks like a no-op |
| `CREATE_SHEET_LIST` | *"make a sheet list schedule"* was answering `FIND_SCHEDULES`. **A different API call, not an option on the schedule maker** — sheets are not model elements, so the category route cannot produce a drawing index at all. A field the list cannot carry is named with the real ones, because a silently short index prints on the cover and nobody reading it can tell a column was asked for |
| `REPORT_GLOBAL_PARAMETERS` | *"show me the global parameters"* was answering `READ_MODEL_WARNINGS`. **A global is not a project parameter**: one value for the whole model that drives geometry, against a column with one value each. The interesting part is which DIMENSIONS it drives — that is the only answer to *"why did that wall shift when I changed a number"*. And a document that CANNOT hold them is a different answer from one that has none, which is why that is asked first |
| `CREATE_LINE` | *"draw a detail line here"* was answering `CREATE_DRAFTING_VIEW`. **Detail or model is the decision, not a detail** — one lives in a single view, the other turns up on every drawing — so there is no default and the caller says which. The short-curve limit is read from the application rather than typed, because it belongs to the installation |
| `ASSIGN_SCOPE_BOX_TO_VIEW` | *"set the scope box on these views"* was answering `SET_VIEW_SECTION_BOX`. Pairs with the impossibility already recorded here: **a scope box cannot be CREATED from code on any release** — but assigning one that exists is a parameter write, and that is where the repeated work is. A model with none says so, and says why |
| `SET_VIEW_CROP_TO_SHAPE` | *"crop the view to follow the room shape"* was answering `SET_VIEW_CROP`, which sets a BOX and can only ever be a rectangle. Revit demands three things of the loop and **gives the same unhelpful error for all three**; the curves are chained here rather than trusted in arrival order, and the worst gap bridged is reported so a shape that never closed does not pass as one that did |

**Two sentences were answered by a routing row instead of a fragment, and that is the batch's real
finding.** *"Grey out the linked model"* was routing to `COPY_FROM_LINK`. The instinct was to build a
link-override fragment — and reading `OVERRIDE_GRAPHICS_IN_VIEW` first showed it already does the job:
**a link is ONE element in the host document**, so a single override on it covers everything inside. It
needed the sentence declared, not a new fragment. *"Make a new duct system type"* is the same shape:
`DUPLICATE_TYPE` is how a system type is made in Revit.

**What that is worth remembering as:** the twenty-third session built a duplicate because it read the
brain's #1 answer and stopped. This one read the top five for every sentence **and then opened the
purpose of each near neighbour**, and that second step is what caught these two — the ranking had them
at #1 and #2 both times. Reading the list is not the same as reading the fragments.

**A gap this batch measured and deliberately did not fill:** *"make this pipe look the same as that
one"* still answers `CREATE_PIPE`. `OVERRIDE_GRAPHICS_IN_VIEW` takes the settings already built, and
nothing in the library READS an element's existing overrides — `View.GetElementOverrides` exists on all
three releases checked. That is one small fragment, and it belongs to the next batch rather than to a
sentence declared on something that half-answers it.

**The checker caught two orphans in this batch's own work.** `COPY_VIEW_FILTERS` and
`ASSIGN_SCOPE_BOX_TO_VIEW` both named a list of views as a need without marking it `source: request`, so
`check-gaps` correctly reported two actions nothing could feed. Worth noting because both were written
by hand from a template that had it right — the error was mine and the tool found it in seconds.

**And the API check earned its keep a fourth time, in a new shape.** `GlobalParameter.IsValidDataType`
is present on 2020 and **gone by 2024**. The three it caught before were members missing from the OLD
end; this one is missing from the NEW end, which is the reverse and is worse, because a guard written
against the release in front of you looks careful and turns every newer build red.

---

## The twenty-third session, 2026-09-02 — eight more, and one written then withdrawn

**What it did:** took the library from **162 to 170**. Same container, same limitation. `A9` now names
**thirty-two**.

| | |
|---|---|
| `CREATE_WALL` | *"make a wall here"* was answering `SPLIT_MEP_RUN`. A run of points like the duct and the pipe; project elevation again; **nothing is joined**, and the height is unconnected |
| `CREATE_MATERIAL` | *"make a material"* was answering `READ_ELEMENT_MATERIAL`. **Shading colour only** — no appearance asset — so it schedules and colours correctly and renders as flat plastic, which is said rather than discovered |
| `REPORT_VIEW_FILTERS` | *"report what filters are on this view"* was answering `APPLY_VIEW_FILTER` — **a write, to a question about a drawing**, the worst shape of mis-route here. A filter on NO view is the row worth finding. Every probe sits in its own guard, because the project browser comes back from the same collector and throws |
| `SET_DATUM_BUBBLES` | *"the grid bubbles are on the wrong side"* was answering `SNAP_TO_GRID` — which MOVES elements. **Flip is not one call**: read both ends, hide one, show the other, and the two ambiguous cases are decided rather than silently skipped. Bubbles are per view; extents are not, and confusing them is how one fix becomes twenty |
| `CREATE_FILLED_REGION` | *"make a filled region here"* was answering `CREATE_CALLOUT`. **It is annotation**: in one view, in no schedule, not in the model — right for a markup, and a hole in the coordination model for anybody who thinks they modelled something |
| `CREATE_KEY_SCHEDULE` | *"make a key schedule"* was answering `CREATE_SCHEDULE`. A table of DEFINITIONS, not a list of things. Revit is ASKED whether the category allows one, because the create call throws rather than explaining |
| `FIND_OVERLAPPING_LINES` | *"find the overlapping lines"* was answering `FIND_OVERLAPPING_TAGS`. **"Are these two lines the same" is not a direct comparison** — normalise the direction into one half-plane, round both parts of the key, then compare as 1-D intervals. Skip the first step and the check "finds nothing" on a drawing full of duplicates |
| `COPY_FROM_LINK` | *"take the elements out of the link"* was answering `RELOAD_LINKS`. **The link's transform is the whole job** — without it everything lands somewhere plausible, which is worse than obviously wrong. And the copies stop tracking the link, which is right for grids and a trap for anything still changing |

### One fragment was written and then withdrawn the same hour

**`ADD_NAME_AFFIX` was built, validated, and deleted** — because `EDIT_TEXT_VALUES` already does prefix,
suffix and replace, on any parameter *or* on the element's own name. Its purpose even records that the
earlier library carried a rename fragment and a prefix/suffix fragment with the same four
transformations, and that this one deliberately merged them. **The duplicate re-created exactly the
duplication a previous session had removed on purpose.**

> **How it happened, because the mechanism matters more than the fragment.** The brain was asked
> *"add a prefix to all the names"* and answered `ADD_SCHEDULE_FIELDS`, `EDIT_TEXT_VALUES`,
> `RENAME_ELEMENTS`. **`EDIT_TEXT_VALUES` was at #2 and was not read.** This repository's own standing
> rule is *read the top three to five, never just #1* — written for exactly this, and the wrong answer
> at #1 made the list look like a gap when the answer was one line below it.
>
> **A lookup can show what is missing. It cannot show what already exists under a different name** —
> only reading the candidates does that.

And it took the sentence with it while it existed: declaring an utterance HANDS that sentence to the
new fragment by identity, ahead of whatever answered it before. Withdrawn, *"add a prefix to all the
names"* returns to `EDIT_TEXT_VALUES`, where it belongs. A row there now records the whole episode.

### The routing checker's store went stale, and reported a good fragment as unrankable

After the withdrawal, `check-routing.py` reported the replacement fragment as `#None` on **its own
declared utterances** — not ranked anywhere. Nothing was wrong with it: the store still held the
deleted fragment and had not indexed the new one.

> **Delete a fragment, then rebuild the index from scratch before believing any routing number.** A
> session that trusted that output would have rewritten a fragment that was already correct — the same
> shape as the collision list misleading two sessions in a row, and worth the same warning.

### The routing, measured against the 162 that were there before

| | before | after |
|---|---|---|
| Utterances | 946 | 986 |
| Claimed in a routing table and not reached | 3 | **3** — the same three |
| Shortlist collisions | 148 (15.6%) | 152 (15.4%) |

The rate went slightly DOWN this time, having risen last session. Every new fragment's sentences
resolve correctly through `heron_brain.lookup`. Two stale claims were fixed on the way: one of my own
wrapped across two lines *again* — the third time that mistake has appeared, and it is now the first
thing to check in a new table — and `REPLACE_MATERIAL`'s table claimed *"the client changed the spec"*
while declaring *"the client changed the material"*, which only broke once the ranking shifted.

Reciprocal rows went into twenty-three counterpart fragments.

---

## The twenty-second session, 2026-09-02 — eight more, and two capabilities that cannot be built

**What it did:** took the library from **154 to 162**, same container, same limitation. `A9` now names
**twenty-four** fragments no compiler has read.

Chosen the same way: the owner's sentences through `heron_brain.lookup`, wrong answers first.

| | |
|---|---|
| `AUTO_SIZE_MEP` | *"auto size the ducts from the flow"* was answering `MEASURE_MEP_VELOCITY` — a read. Sizes from the flow Revit already has, **always rounding UP**: rounding to the nearer size pushes velocity above the design figure, which is the direction that causes noise and rework. **The unit assumption is printed before anything is written** — the raw internal value beside Revit's own display string, so a wrong constant shows itself on the first run rather than through a mis-sized system |
| `CONNECT_AIR_TERMINALS` | *"connect the air terminals to the duct"* was answering `REPORT_CONNECTOR_LOADS`. This is the **TAP**, cut into the duct's side — not `CONNECT_OPEN_ENDS`, which butt-joins two ends already at one point. Nearest is measured to the terminal's **connector**, which on a ceiling diffuser is on top of it; measuring from the diffuser body picks the wrong duct where two run side by side. **The call returns a bool, and false is not an exception** |
| `HIGHLIGHT_VS_REST` | *"grey everything except the ducts"* was answering `SET_CATEGORY_GRAPHICS`. **This is the owner's own grayout**, and *"do the grayout"* now lands on it. Insulation and lining travel with their host **both ways** — his standing rule: colour a duct and you colour its insulation, or you cannot see it |
| `APPLY_VIEW_FILTER` | *"apply this filter to the view"* was answering `APPLY_VIEW_TEMPLATE` — a far bigger change than the one asked for. The missing link after `CREATE_VIEW_FILTER`: without it a filter exists in the project and does nothing to any drawing. **Projection and cut are both the caller's job**, and a half-built override is warned about, because Revit derives neither from the other |
| `CREATE_FLOOR` | *"make a floor here"* was answering `CREATE_PLAN_VIEW`. Same version split as the ceiling and confirmed the same way: **`Floor.Create` is absent on 2020 and `Document.Create.NewFloor` is gone by 2024**, so both are looked up at run time. Project elevation again, not elevation |
| `RENAME_FAMILY` | *"rename the family"* was answering `LOAD_FAMILY` — a write of the wrong kind. It renames the family EVERYWHERE, and says how many types and instances went with it. **The .rfa on disk keeps its own name**, so reloading brings the old one back — worth knowing before somebody renames the same family twice |
| `CREATE_LEGEND_VIEW` | *"make a legend"* was answering `CREATE_SCHEDULE`. Duplicating is the only route: **Revit exposes no legend creation method on any release**, so a project with none cannot get its first from a script, and this says so plainly rather than failing obscurely |
| `CHECK_ROOM_MEP_COMPLETENESS` | *"which rooms have no mep in them"* was answering `PLACE_ROOMS`. Checks rooms against a rule and reports what is short. **The total found per category is printed first** — a category with zero anywhere means the devices are in a linked model, and without that line every room reads as missing them |

### Two capabilities cannot be built, and both refusals are the finding

- **`SET_DESIGN_OPTION`.** An element only lands in a design option while that option is ACTIVE, and
  **no option can be made active from a script** — checked against 2020 AND 2027, where the only member
  anywhere is the read-only `GetActiveDesignOptionId`. There is no setter on `Document`, on
  `DesignOption`, or anywhere else. A row in `SET_ELEMENT_WORKSET`'s table says so.
- **`PURGE_UNUSED`.** This one is *possible* and is deliberately not built. `FIND_UNUSED_FAMILIES`
  reports and deletes nothing, which the eighteenth session decided on purpose: purging is hard to
  reverse, Revit has the command, and doing it to a shared model from here would be the most damaging
  thing in the library. **Building it now would quietly undo that decision**, so the row in that
  fragment's table records why instead.

> That is three impossibilities established at both ends of the range in two sessions — scope box,
> design option, and the legend's first instance. **The pattern is worth the sentence: check the far
> end before writing "cannot".** The `CREATE_CEILING` lesson exists because somebody did not.

### A third false absence, and this one was in the checking tool

The metadata reader matches a type by its SHORT name and returns the first hit — and for several types
that first hit is an **internal marshalling struct**, not the real class. `OverrideGraphicSettings` reads
as having **no constructors at all** that way; it has two, including the copy constructor this session
needed. Match the full name.

That is now three ways the same tool can report a member that exists as missing: a base-type member, an
inherited one, and a short-name collision. All three are in `A9` where the method is described.

### The routing, measured against the 154 that were there before

| | before | after |
|---|---|---|
| Utterances | 906 | 946 |
| Claimed in a routing table and not reached | 3 | **3** — the same three |
| Shortlist collisions | 129 (14.2%) | 148 (15.6%) |

**The collision rate rose more than in the previous two batches**, which is what adding into the two
most crowded areas — views and MEP — does. Every one of the twenty new collisions was checked through
`heron_brain.lookup` and resolves correctly; the keyword route is not what the host calls. One new
unreached claim appeared and was fixed: `HIGHLIGHT_VS_REST`'s table claimed *"bring these forward"*,
which `OVERRIDE_GRAPHICS_IN_VIEW` already owns as *"bring the services forward"* — the table row was
changed rather than the sentence declared, because declaring it would have been a real fight over a
sentence another fragment answers correctly.

Reciprocal rows went into twenty-one counterpart fragments.

---

## The twenty-first session, 2026-09-02 — eight more, and the metadata check earned its keep

**What it did:** took the library from **146 to 154**, in the same container and with the same
limitation - no .NET SDK, so **these eight have not been compiled either**. `A9` now names sixteen.

The batch was chosen the same way as the last one: the owner's sentences put through
`heron_brain.lookup`, and the wrong answers written down first.

| | |
|---|---|
| `CREATE_CEILING` | *"create a ceiling in this room"* was answering `MEASURE_CEILING_HEIGHT` - a read, to a request to build. **The earlier library had recorded this job as IMPOSSIBLE and it was not**: `Ceiling.Create` arrived in 2022, the note left the version off, and it read as "ceilings cannot be made" while two of three installed Revits could. Reached by name at run time, so one source spans the range and 2020 gets an answer rather than an error |
| `CREATE_ELECTRICAL_RUN` | *"draw a cable tray"* was answering `DIMENSION_MEP_RUNS` - a write, of dimensions. **Tray and conduit are ONE fragment**: separate classes, the same creation shape, and two near-identical fragments in a crowded area cost more than they buy. Which one is wanted is asked for, as `PLACE_ROOMS` asks about rooms and spaces |
| `JOIN_GEOMETRY` | *"join these walls together"* was answering `PLACE_MEP_FITTING`, and after last session also `CONNECT_OPEN_ENDS` - two MEP writes for a question about walls. Both would have found nothing to do and said so, which is the quiet kind of wrong |
| `CREATE_WORKSET` | *"add a new workset"* was answering `LIST_WORKSETS`. **ADMIN risk**: a workset is project structure the whole team works inside. A workset is also **not an element** - no id - so what comes back is names |
| `REPLACE_MATERIAL` | *"change the material on these"* was answering `READ_ELEMENT_MATERIAL`. A material hides in the compound-structure LAYERS and in material PARAMETERS, and a swap that misses one leaves the old material half in use, where it refuses to purge and nobody can see why. **The layers that come back are COPIES** - editing them in place changes nothing |
| `RELOAD_LINKS` | *"reload the links"* was answering `LIST_LINKED_MODELS`. It reloads or unloads and **never removes** - removing deletes what is hosted on the link. Revit's own result code is printed raw, because which code comes back for an already-current link is still not established |
| `CREATE_SELECTION_FILTER` | *"make a selection set"* was answering `SET_SELECTION` - a highlight gone at the next click. A saved set survives the model closing; a view filter is a RULE. Three different things, one sentence |
| `SET_VIEW_UNDERLAY` | *"set the underlay on this plan"* was answering `CREATE_TEXT_NOTE`. Base and top are set TOGETHER - writing the base alone leaves the old top and shows a range nobody asked for, which reads as a corrupted view |

### One capability was refused, and the refusal is the finding

`CREATE_SCOPE_BOX` was in the batch and was dropped. **Revit exposes no way to create a scope box on
any supported release** — checked against the shipped assemblies for **2020 AND 2027**, where the only
matches for the word are print and export flags and a few parameter ids. The earlier library had
established this on 2020 alone; this confirms it at the far end, which is what the `CREATE_CEILING`
lesson demands of every "impossible".

So there is no fragment. The sentence *"put a scope box round this area"* is answered instead by a row
in `SET_VIEW_SECTION_BOX`'s routing table saying plainly that nothing can, and that one drawn by hand
works with everything else here. **A fragment whose only behaviour is to refuse would be a capability
that does nothing**, and the library counts capabilities.

### The metadata check caught three things a compiler would have

It is not a compiler and it is not a substitute for one, and it still paid for itself twice more this
session:

- **`VariesAcrossGroups` is on `InternalDefinition`, not on `Definition`.** The obvious line —
  `parameter.Definition.VariesAcrossGroups` — compiles on **no release at all**. Caught before it was
  written, and the definition is cast first.
- **`Ceiling.Create` is absent from the 2020 assembly and present on 2024.** That confirmed the run-time
  lookup was necessary rather than defensive.

> **And a warning about the tool itself: walk the BASE types.** `Space` declares neither `Number` nor
> `Area`; `HostObjAttributes` does not declare `FamilyName`. Both inherit them. A member check that
> stops at the declared type reports a false absence, and a session that trusts it will rewrite working
> code to avoid a member that was there all along.

### An `mcp` name collision that cost twenty minutes, and does not affect the repo

Last session installed the MCP SDK to run `test_mcp_serves.py`. **The pip package is called `mcp` and so
is this repository's own top-level directory** — and `mcp/` has no `__init__.py`, so it is a namespace
package, which a regular installed package **outbeats regardless of `sys.path` order**. After that,
`from mcp.server import heron_brain` reaches the SDK and fails.

**The repository is not affected**: it imports `heron_brain` as a top-level module after putting
`mcp/server` on the path, which is immune. What broke was an ad-hoc call written the natural way. The
SDK was uninstalled — it was broken in this container anyway, its `cryptography` bindings panicking on
import — and `test_mcp_serves.py` is back to its honest skip.

> To ask the brain a question from the repo root: `sys.path.insert(0, "mcp/server")` then
> `import heron_brain`. Not `from mcp.server import ...`.

### The routing, measured against the 146 that were there before

| | before | after |
|---|---|---|
| Utterances | 860 | 906 |
| Claimed in a routing table and not reached | 3 | **3** — the same three, all near-synonym pairs |
| Shortlist collisions | 122 (14.2%) | 129 (14.2%) |

**No new unreached claims this time**, because every quoted sentence was kept on one line and matched to
a declared utterance — the two mistakes of the previous batch. Reciprocal rows went into twenty
counterpart fragments, and all six new collisions resolve correctly through `heron_brain.lookup`,
checked one at a time.

**One row written last session had already gone stale and was corrected**: `PLACE_MEP_FITTING`'s table
said *"join these walls together"* was covered by nothing in the library. True when it was written, and
false one session later. A cross-reference that names an absence dates the moment the absence is filled.

---

## The twentieth session, 2026-09-02 — eight fragments, and the first batch no compiler has read

**What it did:** took the library from **138 to 146**, and it is the first batch in this repository's
history that has **never been compiled**. That is written at the top of this file and in the register as
`A9`, because a session that quietly skipped the gate and said nothing would be indistinguishable from
one that ran it.

**Why it could not run.** There is no .NET SDK in this container and none can be installed: the SDK
download host answers **403 to the CONNECT itself** — the network's policy, not a transient failure.
Checked, not assumed: `curl "$HTTPS_PROXY/__agentproxy/status"` names the host and the refusal.

**What was done instead, and exactly what it is worth.** nuget.org IS reachable from here, so the Revit
API reference assemblies for **2020, 2024 and 2027** were downloaded and read directly — every type and
every member each new fragment calls was looked up in the assemblies' own metadata, with the argument
COUNT where there were overloads.

That answers *"does this member exist in this release"*, which is the failure this repository has
actually had — `Document.CreationGUID` compiled on 2024 and does not exist on 2020, and it had survived
several readings. **It answers nothing about argument TYPES and nothing about syntax.** A metadata read
is not a compile and must not be recorded as one.

> It did earn its keep twice. `ExternalDefinitionCreationOptions.Type` is **present on 2020 and gone by
> 2024**, which is what makes `ADD_PROJECT_PARAMETER` a reflection job rather than a written-down call;
> and `Space` carries neither `Number` nor `Area` of its own — both come from `SpatialElement`, so a
> member check that does not walk the base type reports a false absence. Worth knowing before trusting
> any tool of that shape.

### The eight

| | |
|---|---|
| `CREATE_PIPE` | The plumbing twin of `CREATE_DUCT`, and deliberately the SAME SHAPE - a run of points, one segment per pair. The earlier library drew one pipe between two points and drew duct as a run; one job in two shapes is how a composition that works for duct fails for pipe. **It sets no size**: Revit snaps a diameter to the nearest its type allows and returns TRUE while doing it - 77 mm asked for, 80 mm delivered - so that check stays in `SET_MEP_SIZE`, in one place |
| `SET_MEP_SLOPE` | Puts a fall on a drainage run by moving one end. **A riser is never sloped, and the test is geometric rather than a millimetre threshold**: the earlier library used a 300 mm minimum run, which a 2 m drop with 10 mm of horizontal run walks straight past - it would be re-drawn 10 mm long and the pipe destroyed. A run that rises more than it runs is a riser. It also **reads** Revit's Slope parameter rather than writing it: a parameter written to disagree with the geometry is a lie every schedule then repeats |
| `CONNECT_OPEN_ENDS` | Joins open connectors already at one point - the "touching but Revit says not connected" cleanup. Four tests and all four must pass, and the one that matters is **facing**: two pipes crossing at the same height pass distance, domain and size, and are not a joint. **Nothing moves**, so a wide gap tolerance buys a model that REPORTS connected while the hole is still there |
| `FIND_DUPLICATE_VALUES` | Two doors marked D-101. Duplicate DATA, where `FIND_DUPLICATE_ELEMENTS` is duplicate GEOMETRY - the two share the word and nothing else. **A blank is not a duplicate**, and "180 of these have no mark" is reported as its own finding, as is "these do not carry that parameter at all" - folding those together lets a question nobody asked come back as a clean bill of health |
| `FIND_OVERLAPPING_TAGS` | Annotation printing on top of annotation, measured in **paper millimetres** - the same two tags are clear at 1:50 and merged at 1:200. **It projects onto the view's own right and up directions**, where the earlier library compared model X against model Y: right in a plan, and in a section it reports nearly every pair, because a view looking along X has all its annotation at one X |
| `COLOR_BY_PARAMETER` | A colour per value - colour by system, by level, by type. **No palette**: a list of six colours gives the seventh system the first system's colour, which is the defect the hue stepping was written to fix. **And the start hue is fixed, not random** - the earlier library re-rolled it each run "for variety", which makes today's drawing incomparable with the one issued last week |
| `ADD_PROJECT_PARAMETER` | Creates a shared parameter and binds it. **ADMIN, not MODIFY**: a binding changes the project's data structure for everyone, and un-binding discards every value anybody typed. **The shared parameter file path is asked for, never invented** - the earlier library fell back to a temp folder, which produces a shared parameter nobody can find again. **And an existing binding is extended, not replaced**: ReInsert with today's categories alone strips the parameter off every category it had |
| `DUPLICATE_TYPE` | *"Make a new duct type at 300 wide"* - which resolved to `CREATE_3D_VIEW` before this. **More than one source type is a refusal, not a loop**: Revit needs type names unique, so one literal name over five types succeeds on the first and quietly does not on the rest - a batch that reports success and half happened |

### One was not written, on purpose

`PLACE_SPACES` was in the plan and was dropped after reading `PLACE_ROOMS`, which already places spaces —
it takes a `placeKind` and calls `NewSpaces2`. A second fragment for it would have been a near-duplicate
in the most crowded kind of area, and the cost of that is not tidiness: it is retrieval order, which is
not ours to choose.

### The gaps were found by asking the brain, not by working down a list

Before writing anything, twelve of the owner's own sentences were put through `heron_brain.lookup` and
the answers read. That is what chose the batch, and some of the answers were worth the exercise on their
own:

| The sentence | What came back | |
|---|---|---|
| *"make a new duct type at 300 wide"* | `CREATE_3D_VIEW` | |
| *"the tags are on top of each other"* | `ALIGN_MEP_ELEVATION` | a WRITE, on MEP |
| *"add a project parameter"* | `COPY_PARAMETER_VALUE` | a different write |
| *"create a ceiling in this room"* | `MEASURE_CEILING_HEIGHT` | a read answering a create |
| *"add a new workset"* | `LIST_WORKSETS` | |
| *"change the material on these"* | `READ_ELEMENT_MATERIAL` | |
| *"draw a cable tray"* | `DIMENSION_MEP_RUNS` | |
| *"reload the links"* | `LIST_LINKED_MODELS` | |
| *"put a scope box round this area"* | `SET_VIEW_SECTION_BOX` | |

**The last six are still true and are the obvious next batch** — `CREATE_CEILING`, `CREATE_WORKSET`,
`REPLACE_MATERIAL`, `CREATE_CABLE_TRAY`, `RELOAD_LINKS`, `CREATE_SCOPE_BOX`. `JOIN_GEOMETRY` belongs on
that list too: *"join these walls together"* now lands on `CONNECT_OPEN_ENDS`, which joins MEP
connectors and cannot do it, so `PLACE_MEP_FITTING`'s table says so out loud until something can.

> **All of that was done by the twenty-first session, below, except one**: `CREATE_SCOPE_BOX` cannot be
> built at all — Revit exposes no way to create one on any supported release. Read that section before
> reaching for it again.

### The routing was measured against a baseline, not just run

The new fragments were moved aside and `check-routing.py` run on the 138 that were there before, so
every number below is a difference rather than an impression.

| | before | after |
|---|---|---|
| Utterances | 802 | 860 |
| Claimed in a routing table and NOT reached | 5 | **3** |
| Shortlist collisions | 107 (13.3%) | 122 (14.2%) |

**Every one of the new fragments' own sentences resolves correctly through `heron_brain.lookup`** —
checked one at a time, because the collision list measures the keyword route alone and the eighteenth
session recorded two consecutive sessions being misled by exactly that.

**The unreached-claim count went DOWN while eight fragments were added**, and that is where the real
work was:

- Three of the new fragments claimed a sentence in a table and declared it nowhere. `"add a drain line"`
  was resolving to **`CREATE_GRID`**. A routing table is a comment; comments are not indexed.
- Two of them wrapped a quoted sentence across two lines, and the checker read the fragment. **Keep each
  sentence on one line** — nothing says so anywhere else, so it is said here.
- The reciprocal tables went into **eighteen** counterpart fragments, per the rule that a cross-reference
  written one way only routes whoever lands on the newer file. Seven sentences those tables claimed were
  then declared as utterances, which is the same defect the seventeenth session named and it reappears
  every time a table is written.
- **Two of the five pre-existing claims were not harmless and were fixed**: `"is the model healthy"` was
  being served by `EXPORT_MODEL_TO_NWC` — a PUBLISH answering a read — and `"move these to level 3"` by
  `CREATE_LEVELS`, which makes levels. The other three are near-synonym pairs where both sides do the
  same kind of work, and they are left alone rather than decided blind.

### Two checkers earned their place again

- **`check-structure.py` refused `ADD_PROJECT_PARAMETER`**: its reflection named
  `Autodesk.Revit.DB.SpecTypeId` in a string, and the adapter boundary says that name belongs inside
  `revit/`. The fix is better code, not an exemption — the namespace is taken from `typeof(Document)`,
  so it follows the API instead of being written down beside it.
- **`tools/README.md` was claiming "all 32 fragments compile on all 8 releases"** while the library was
  past four times that. The line is gone rather than corrected: a count typed into prose goes stale the
  day after it is true, and a stale green is believed. The tool's own output is the count.

---

## The nineteenth session, 2026-09-02 — the test that had been blocked for four sessions

**What it did:** no new fragments. It closed the one thing this container had been carrying as
permanently blocked, and it turned out not to be blocked at all.

`tests/test_bridge_roundtrip.py` had reported *not found* for four sessions running, and was written up
each time as "the test host targets `net8.0` and only the .NET 10 runtime is installed here". True, and
the wrong conclusion: **the framework was hardcoded in the test.**

```python
_POSIX_DIR = os.path.join(HOST_PROJECT, "bin", "x64", "Debug-net8.0")
```

That was true of the machine the file was written on and false of the next one. The project **built**
and the host **refused to start** — *"You must install or update .NET"* — which reads like a missing
build rather than a missing runtime, so four sessions in a row recorded it as an environment limit.

It now asks `dotnet --list-runtimes` and builds for the newest `Microsoft.NETCore.App` the machine can
actually run, falling back to the old value when the question cannot be answered. **All 32 checks pass**
— framing, the JSON parser, the token, newest-connection-wins, the whole lease, and the toggle cycle.

> **A pinned framework in a test meant to run on whatever machine is in front of it is a
> machine-specific assumption written down as a constant.** The register row `A4` had even recorded the
> workaround — *"`-p:HeronTfm=net10.0` runs it"* — so the knowledge existed and only a human applying it
> by hand could use it. Knowing a workaround and not automating it is how something stays broken while
> being fully documented.

**The register said Windows and meant it for less than it looked.** `A4` is now down to the one thing a
Linux run genuinely cannot touch: the Windows named pipe itself — its naming, its security descriptor
and the `CreateNewInstance` flag. Everything else on that row is proven here.

**17 of 18 suites now pass**; the one that does not is `test_mcp_serves.py` reporting honestly that it
was skipped for a missing optional dependency, which `check-gaps` reads as WAITING rather than as a
pass.

---

## The eighteenth session, 2026-09-02 — eight fragments, and the check that finally reads its own label

**What it did:** took the library from **94 to 102**. All 102 compile on all eight releases; all 102 are
`DRAFT`. **Every signature was read off the reference assemblies BEFORE writing** — the lesson from last
session — and the batch compiled clean on the first run, which the previous two did not.

| | |
|---|---|
| `CREATE_REVISION` | **The input `ADD_REVISION_CLOUD` could not get.** That fragment needs a `revisionId` and refuses to invent one; nothing could produce one, so the whole revision job stopped at step one. It does **not** issue the revision: `Issued` is a one-way gate that locks the description and refuses any new cloud, so creating one already issued makes a revision nobody can cloud |
| `LIST_REVISIONS` | *"Which sheets go out with this issue"* — the print list. **A revision on NO sheets is the row worth finding**: either not clouded yet, or clouded on a view that is on no sheet, and the second is invisible in Revit until the drawing goes out unmarked |
| `SET_ELEMENT_PHASE` | *"Set the phase"* was answering `SET_ELEMENT_WORKSET` — **a write, to the wrong property, that reports success.** Created and demolished are asked for separately: a phase called "Existing" says nothing about which was meant, and putting an element ON the existing phase versus DEMOLISHING it there are opposite instructions |
| `READ_SPACE_LOADS` | Heating, cooling and airflow per Space, each figure saying whether Revit **calculated** it or somebody typed it — where they disagree it is either a decision or a stale analysis. A zero is reported as **NO LOAD**, never as `0 W`, because a number-shaped non-answer in front of somebody sizing a chiller is worse than none |
| `CREATE_DRAFTING_VIEW` | Shows no model and never changes — every standard detail in a set is one. The scale is required: a drafting view is empty and has nothing to take one from |
| `CREATE_CALLOUT` | Two things at once — a boundary on the parent and a linked enlarged view. The callout's type is taken from **what the parent is**, because a callout of a section must be a section |
| `FIND_VIEWS_WITHOUT_TEMPLATE` | **On a sheet or not is the whole difference.** A working view needs no template; the same view on a sheet is being *issued*. The working ones are counted, not listed — a flat list is hundreds of rows nobody reads |
| `FIND_UNUSED_FAMILIES` | *"Purge unused"*, answered as a **report**. It deletes nothing: purging is hard to reverse, Revit has the command, and doing it to a shared model from here would be the most damaging thing in this library |

### The units in `READ_SPACE_LOADS` are not all the same kind of certain

Worth reading before that fragment is trusted:

- **Airflow is exact.** Revit stores ft³/s; 1 ft = 0.3048 m *by definition*, so 1 ft³ = 28.316846592 L
  exactly. Same class as D-20's 304.8, provable on paper, needs no model.
- **Load is NOT.** The internal HVAC power unit is *assumed* to be BTU/s and converted at 1055.05585262 W.
  Reflection over an assembly shows a property type, never a unit.

> **If that assumption is wrong every load is out by a constant factor** — which is the most dangerous
> shape a unit error takes: each number looks plausible, every comparison between spaces still works, and
> only somebody sizing a real chiller finds out. **First thing to check against a model**, and it is
> written into the test cases as the first negative rather than asserted as settled.

### The claims check caught its own author, twice, within minutes

The audit added last session — *a routing table is a comment, and comments are not indexed* — fired on
this batch immediately:

- `"which phase is this on"` was claimed by `READ_ELEMENT_PHASE` and served by **`SET_ELEMENT_PHASE`**,
  the new write. The fragment declared *"**what** phase is this on"*. **A one-word difference put a read
  question on a fragment that changes the model.**
- Fixing that shifted the weights and took `"what is this"` and `"what fall is on this drain"` off
  `DESCRIBE_ELEMENTS` and `MEASURE_MEP_SLOPE` — both claimed in tables, neither declared. Stable after
  two rounds.

### The collision list misled two consecutive sessions, and now says so

`check-routing.py` prints **SENTENCES TWO FRAGMENTS BOTH WANT** from `SEARCH.keywords` — **one half of
the fusion, and not what the host calls.** The risk section right above it says loudly that it measures
through `find`; this one said nothing and read like a defect list.

Nine "new" collisions appeared this batch. **All nine resolve correctly through `find`** — identity
catches every one. The previous session edited fragments to chase several of the same kind before
checking; this session nearly did it again.

> The section now carries its own label and the one-line way to check:
> `heron_brain.lookup("the sentence")`. **A diagnostic that does not say which stage it measures will be
> read as a verdict.**

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **102** below `PROVEN`.

---

## The seventeenth session, 2026-09-01 — eight fragments, and a comment nobody could hear

**What it did:** took the library from **86 to 94**. All 94 compile on all eight releases; all 94 are
`DRAFT`. The batch closed a hole the previous one opened, and then found a defect class running through
half the library.

| | |
|---|---|
| `CREATE_PLAN_VIEW` | **Closes the hole `CREATE_LEVEL` opened.** A level made through the API has no views — Revit's *tool* makes them, the level does not — so *"add 4 more floors"* means levels **and** plans, and only the first half existed |
| `LIST_GRIDS` | **Grid names cannot be sorted as text.** 1 to 12 sorts to 1, 10, 11, 12, 2, 3 — somebody looking for grid 3 finds it sixth. Split into letters and trailing digits, compared as a number. Every row says which way the grid runs, because the name never does and both conventions exist in one building |
| `CREATE_GRID` | A grid is a **vertical plane**, so its defining line must be level — checked here, where the message can say why, rather than surfacing as an exception naming a curve |
| `PLACE_ROOMS` | Rooms **or** spaces, in one pass per level. MEP loads run on Spaces and the two are indistinguishable to look at. An unbounded result is the useful half: the gap in the walls is also why the area next door came out enormous |
| `LOAD_FAMILY` | **Will not overwrite, and that is a limit of the fragment format itself.** Revit needs an `IFamilyLoadOptions` object to decide about differing parameter values; that needs a **class**, and a fragment is a body of statements. The behaviour lands on the safe side by accident and is kept on purpose |
| `CREATE_VIEW_FILTER` | A filter **matches**; an override **remembers**. Work modelled next week comes out the right colour with nobody touching the view — on a reissued drawing that is the whole job |
| `EXPORT_VIEWS_TO_DWG` | The first fragment that writes **outside the model**, so it cannot be undone. Files that would be overwritten are reported, and the run still exports — refusing everything over one collision means somebody empties the folder and re-runs |
| `EXPORT_MODEL_TO_IFC` | The schema version is required, never defaulted: hand a recipient expecting 2x3 an IFC 4 and it **opens**, shows geometry, and reads as a bad model rather than a wrong format |

### The compile gate caught two API guesses, and reflection caught a third thing it could not

Two fragments failed on **all eight releases** — not a version break, just wrong signatures written from
memory. Both were settled by reflecting over the reference assemblies rather than guessing again:

- `ExportDWGSettings.GetByName` does not exist. It is **`FindByName`**.
- `NewSpaces2(Level)` does not exist. It is **`NewSpaces2(Level, Phase, View)`**, while `NewRooms2(Level)`
  needs neither. Identical on 2020 and 2026, so **not a version difference — a real asymmetry between
  the two elements.** A space belongs to a phase and is placed from a view; a room does not.

**And then the thing a green build cannot catch.** `CREATE_VIEW_FILTER` needs a compile symbol, and its
note said the old `CreateEqualsRule` overload was *"gone after 2022"*. Reflection over all eight
assemblies says it survived to **2025 and was removed in 2026**, with the replacement arriving in 2023 —
a three-release overlap. **The code was right, so all eight compiled green and nothing complained.**

> **A compile gate checks the call. It does not read the sentence beside it.** Every version claim in
> this repository is a prose claim, and prose is exactly what the gate cannot see. Read the assemblies.

### A routing table is a comment, and comments are not indexed

`OVERRIDE_GRAPHICS_IN_VIEW` claimed *"make these red"* in its routing table and never declared it as an
utterance — so the sentence resolved to `GROUP_ELEMENTS`. `heron_search` indexes **semantic-identity,
the utterances, the capability, the domain and the purpose**, and nothing else. A table saying `-> here`
records a decision retrieval cannot act on.

Audited across all 94: **69 such claims, 23 of them reaching the wrong fragment.** Among them
*"zoom to these"* building an **MEP fitting**, *"write that up"* reaching a bulk parameter **write**, and
*"place a diffuser"* reaching the duplicate finder. All 23 fixed; two of the 69 turned out to be **one
sentence claimed by two tables**, which is two comments disagreeing and invisible to every other check
here. Both resolved by deciding an owner.

`check-routing.py` now audits this every run — measured through `find`, and **proven by planting an
unreachable claim and watching it fire.** 159 claims, 0 wrong.

### `heron_scope.py --rebuild` did not do what its own help said

It promised *"re-index every scope from disk"* and rebuilt **only the fragments table** — leaving the
identity table and the embeddings holding whatever the last run put there. Measured: after adding an
utterance and running only that command, the new sentence resolved to a **different fragment**,
confidently, by the hybrid route, while the identity route that should have matched it exactly had never
heard of it.

It went unseen because every tool that reads the index calls `SEARCH.index` itself first — `check-routing`
does, the brain does. **The only person who could hit it was somebody rebuilding by hand and then asking
a question, which is exactly what the command is for and exactly what its help told them to do.** Fixed
in the command, not in `rebuild()`, which stays cheap for callers that only want the metadata.

### One fragment was not written, on purpose

*"What is the area of this"* was on the list of gaps. It is not one: `MEASURE_ELEMENT_VOLUME` already
provides `areaM2` from the same solids. A second fragment would compute the same number from the same
input and could one day disagree, with nothing to say which was right. **The gap was in the words** —
two utterances were added instead. Same finding as *"what is the elevation of this"* two sessions ago.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **94** below `PROVEN`.

---

## The sixteenth session, 2026-09-01 — eight fragments, and the search backend hit its ceiling

**What it did:** took the library from **78 to 86**. All 86 compile on all eight releases; all 86 are
`DRAFT`. Two of the eight close a hole where a request to CHANGE the model was being answered by a
read-only report.

| | |
|---|---|
| `CREATE_SHEET` | The missing link in producing a set: `CREATE_SECTION_VIEW` makes the drawing, this makes the sheet, `PLACE_VIEW_ON_SHEET` joins them, `LIST_SHEETS` reads it back. **The number is checked for a clash BEFORE anything is created** — Revit refuses a duplicate by throwing, and a throw part-way through leaves an unnumbered sheet in the register that nobody finds until an issue |
| `CREATE_TEXT_NOTE` | View-only annotation, and the refusals name the fix rather than saying "it did not work". Guarded on 3D views, schedules and templates — three different reasons, three different remedies |
| `LIST_LEVELS` | **Two elevations, and the obvious one is not the one on the drawing.** `Elevation` is measured from the internal origin; `ProjectElevation` is what the level head reads. They agree until somebody moves the base point — so most projects, which is exactly why the difference goes unnoticed until the one where it matters. Both reported when they differ. Sorted by HEIGHT, because "Level 10" sorts before "Level 2" as text |
| `CREATE_LEVEL` | Takes the height in the drawing's terms and converts, by **measuring the base-point shift off an existing level** rather than assuming it is zero. And it says out loud that **a level created through the API has no plan view** — Revit's *tool* makes those, the level does not, and the modeller who looks in the Project Browser and sees nothing concludes it failed when it did not |
| `LIST_WORKSETS` | **A closed workset is why a count comes back low, and nothing else says so.** Elements on one are not loaded: no collector returns them, no schedule counts them. Provides no `elements` on purpose — a `Workset` is not an `Element` and an empty list to look composable is a lie the graph would act on |
| `FIND_DUPLICATE_ELEMENTS` | *"Find duplicate elements"* was answering `RENAME_ELEMENTS`. **One of each cluster is kept and is NOT returned** — that is what makes the result safe to hand to `DELETE_ELEMENTS`. Same type AND same place both required: two different types at one point is a design clash, and filing it here puts a coordination problem in a list people bulk-delete from |
| `PLACE_MEP_FITTING` | *"Join these two"* was answering `CREATE_DUCT` and *"connect these two ducts"* was answering `TRACE_CONNECTIVITY` — **a read-only report answering an instruction to build something.** The fitting is chosen from the geometry (in line + same size → union, in line + different → transition, at an angle → elbow, three → tee, four → cross), which is what Revit does itself. **Nothing is moved to close a gap**: ends that are apart are refused with the distance |
| `CREATE_SCHEDULE` | A schedule VIEW that goes on a sheet, as distinct from `GROUP_AND_COUNT`, which puts numbers in the reply. A field the category does not have is **reported**, not skipped — a silently short schedule prints with a column missing and nobody reading it can tell one was wanted |

### Quoting another fragment's sentence in `purpose` hands it that sentence

The first draft of `LIST_WORKSETS` explained itself by quoting what other fragments answer — *"how many
ducts are there"*, *"is the model clean"*, *"a clash check comes back clean"*. **`purpose` is indexed.**
Those quotes took **four** sentences off the fragments that own them, including `READ_MODEL_WARNINGS`'s
own declared *"is the model clean"* and `SET_ELEMENT_WORKSET`'s *"they are on the wrong workset"* — a
request to change the model, answered by a read. Moving the examples into a `#` comment fixed all four
at once.

> **Illustrate a fragment with the sentences it OWNS.** Another fragment's example goes in a comment,
> where a reader sees it and retrieval does not. The same edit fixed `CREATE_SHEET`, whose purpose named
> `PLACE_VIEW_ON_SHEET` and thereby took *"add the view to the drawing sheet"* off it.

### Two utterances were retired, and the reason is not a rank

`CREATE_TEXT_NOTE` declared *"write this on the drawing"* and *"type this on the plan"*. Both lost — the
first to `WRITE_ELEMENT_PARAMETERS`, which is a **MODIFY**, so an annotation request was reaching a bulk
parameter write. The cause is that **write** and **type** are both Revit nouns, and a sentence whose
strongest word means something else in this domain will keep losing to the fragment that owns that
meaning. No rewording of the losing file changes it. The phrasings now avoid both words and the
ambiguity is recorded in the fragment; if somebody genuinely says one of them it belongs in a glossary
as a site-word mapping, not as an utterance that quietly loses.

### The built-in search backend has saturated, and a test was rewritten to say so

`tests/test_retrieve.py` failed. Its section 1 asserted that *"show me every duct in the model"* returns
at least two of four named claimants; at 86 fragments exactly **one** does, and the shortlist is
warnings, selection, sheets, findings and levels — **nothing about ducts.**

Measured rather than guessed: drop two words and the answer is right.

| query | top 3 |
|---|---|
| `show me every duct in the model` | QA-004, SEL-001, SHT-001 — none about ducts |
| `every duct in the model` | MEP-010, MEP-006, MEP-003 |

So the shortlist is decided by **"show me"**, not by **"duct"**. That phrase now opens an utterance in
half the library (*"show me the levels"*, *"show me the sheets"*, *"show me the warnings"* — each of them
exactly what a modeller says), while the one discriminating word carries almost no weight. The top five
span **0.0017**, well under one rank of fusion: nothing is being ranked at all. **n-gram similarity
separated 28 fragments and does not separate 86.**

**No utterance was weakened to make it green**, and the rewritten check says so in the file. What it
asserts now is what is true and stable — that the discriminating word still works when the saturating
phrase is out of the way — which is precisely the gap **`A7`** would close. A7 is the trained embedding
backend that has never run because the weights host is unreachable from here. It has been the least
urgent blocker on the register for weeks. **It is now the most urgent one**, and this is the first
measurement that says why.

> A test assertion was rewritten this session. That is worth flagging rather than burying: it is exactly
> the move that turns a real regression into a green tick. The defence is that the file had done it once
> before at 28 fragments, for the same reason, and recorded it; that the new assertion measures
> something *harder* rather than something looser; and that the old claim is left in place above the new
> one with the numbers that falsified it.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **86** below `PROVEN`.

---

## The fifteenth session, 2026-09-01 — eight fragments, and an agent nobody owned

**What it did:** took the library from **70 to 78**. All 78 compile on all eight releases; all 78 are
`DRAFT`. The batch is weighted to drawing production, because that is where the holes were: five of the
eight were sentences answered by a fragment that does something else entirely.

| | |
|---|---|
| `READ_SELECTION` | **The other half of the library.** Every other filter starts from a set that can be DESCRIBED — a category, a type, a room, an id. This one starts from what a human already pointed at, so *"move these up 200"* finally has a *"these"*. Before it, *"what did I select"* resolved to `SET_SELECTION` — a question answered by the fragment that **overwrites the thing being asked about** |
| `CREATE_SECTION_VIEW` | *"Create a section through this"* was answering `SET_VIEW_SECTION_BOX`. Same word, opposite jobs: one makes a drawing that can go on a sheet, the other hides part of a view that already exists. Cuts **across** a run, not along it — the cross-section, which is what that sentence means |
| `LIST_SHEETS` | First fragment in the `SHT` area. A sheet **view template** is dropped (it comes back from the same collector carrying a number and a name, and listing it puts a row in the register that can never be issued); a **placeholder** sheet is listed *and* flagged, because hiding it loses a row the register deliberately contains |
| `CREATE_DIMENSION` | The registry calls *"create dimensions"* a founding example. Dimensions **centre to centre between parallel MEP runs** and refuses everything else. A reference only exists if it was asked for: without `ComputeReferences` **and** `IncludeNonVisibleObjects`, every element yields a null reference, nothing is drawn, and **no error is raised** |
| `ADD_REVISION_CLOUD` | **One cloud per element, not one around the set.** A cloud spanning scattered elements states that everything between them changed, on a drawing somebody signs. Three tight clouds where one loose one would have done is untidy and true; between untidy and false this takes untidy |
| `READ_MODEL_WARNINGS` | *"How many warnings"* was answering `COUNT_ELEMENTS` — a number about something else. Grouped by message and **worst first**, so an Error cannot hide under 600 identical warnings. It provides `elements`, so *"show me what is causing them"* composes into `ISOLATE_ELEMENTS` |
| `LIST_LINKED_MODELS` | **One row per FILE, not per placement** — counting instances reports four links as nine. Loaded is established by asking for the document, not by reading the stored status: the same discipline that produced `TRACE_CONNECTIVITY`, where the flag described intent and the geometry described reality |
| `SPLIT_MEP_RUN` | Named for what it **can** do. Revit exposes a break for ducts and for pipes and nothing else, so calling it `SPLIT_ELEMENT` would promise a wall split no release can perform. Cable tray and conduit are `MEPCurve`s too and go to `notMepCurve`, not to `refused`, which would suggest it was attempted |

### Six fragments named an agent that does not exist, and both checkers said clean

`create-duct`, `measure-mep-slope`, `measure-mep-velocity`, `read-mep-system`, `set-mep-insulation` and
`set-mep-size` all claimed **`HERON-REVIT-MEP-012`**. That id is not in the registry and never was — the
MEP owner is `HERON-REVIT-SYS-030`, and `-012` is the Family Agent. All six now point at the real one.

**The reason it survived is the interesting half, and it is a gap between two checks that each look
complete.** [`tools/check-metadata.py`](../tools/check-metadata.py) *does* audit agent ids against the
registry, and *deliberately* skips `brain/fragments` — its own comment gives the reason, and the reason
is good: a fragment's metadata standard is its `fragment.yaml`, and adding a `Heron-` header beside it
would give two places to update and one that goes stale. So fragments sat outside the only check that
looks, and [`brain/heron_fragment.py`](../brain/heron_fragment.py) — the one that *does* read them — was
not looking. Neither was wrong on its own. The gap was between them.

`heron_fragment.py` now parses the registry (it does not copy it — adding an agent there is enough) and
refuses a fragment naming an id that is not in it. **Proven by putting the bad id back and watching the
validator fail on that fragment**, not by trusting that it would.

> **A checker's exclusion is a claim that something ELSE covers that ground.** When you write one, say
> which tool takes the part you are skipping — and when you skip a thing nothing else reads, that is not
> an exclusion, it is a hole with a comment over it.

### The routing was measured against a baseline, not just run

Adding eight fragments to a 70-fragment corpus changes what every OTHER fragment ranks against, so
`check-routing.py` was run on `HEAD` first and the two collision lists diffed. Four collisions were
genuinely introduced and all four are fixed:

- *"cut the pipe here"* went to `CREATE_SECTION_VIEW` — because its prose said *"a duct, a pipe, a
  wall"*, and *"cut"* is in both jobs. The enumeration went.
- *"draw the duct run"* went to `SPLIT_MEP_RUN` — its purpose used **three forms of the word "draw"** to
  explain which end the distance is measured from. Reworded.
- *"is this on any drawing"* went to `CREATE_SECTION_VIEW` — introduced by the *fix* to the first one,
  which is why it was re-measured after each edit rather than once at the end.
- *"the ones I have selected"* moved to `READ_SELECTION`, and that one is a **correction, not a
  regression**: `FILTER_ELEMENTS_BY_ID` resolves ids somebody already has and cannot ask Revit what is
  highlighted. It was never that fragment's sentence to answer.

**What was NOT fixed, and is recorded rather than smoothed over.** Five pre-existing collisions got
worse — the loser slipped a rank or two — and one flipped its winner (*"number the sprinklers along the
branch"*, now `ARRAY_ELEMENTS` over `RENUMBER_SEQUENTIAL`) between two fragments this session did not
touch. That is corpus growth changing the word weighting, and chasing it means editing other fragments'
honest prose on a guess. It is the price of a bigger library and it should be watched, not papered over.

> **Run `check-routing.py` on `HEAD` before your batch and diff the two lists.** Run only after, and
> every pre-existing collision looks like yours — 8 lines that were really 4, and half the "new" ones
> were old ones a rank lower.

### One composition claim was wrong, and `composable()` agreed with it

`CREATE_SECTION_VIEW`'s purpose said `PLACE_VIEW_ON_SHEET` could take the new view straight from it, and
`composable()` returned **true**. It was true for the wrong reason: `PLACE_VIEW_ON_SHEET` declares
`views` as `source: request`, so it has **no fragment-sourced needs at all** and every producer composes
with it vacuously. Nothing flows across; the host carries the view into the next call. The purpose now
says so.

**What none of this is.** Not one fragment has met a model. `check-gaps` reports **0 unfinished, 55
waiting**, and counts **78** below `PROVEN`.

---

## The fourteenth session, 2026-09-01 — four fragments, two filters, and one that was already there

**What it did:** took the library from **66 to 70**. All 70 compile on all eight releases; all 70 are
`DRAFT`. Six candidates went in; **one turned out not to need building at all**, which is the part worth
reading.

| | |
|---|---|
| `FILTER_ELEMENTS_IN_ROOM` | **A room is a VOLUME, not a rectangle.** An L-shaped room's bounding box covers ground outside it, so a box test puts the neighbour's diffuser in your schedule — quietly, and it looks right until somebody counts on site. Uses Revit's own point-in-room test, and handles **Space as well as Room**, because an MEP model carries Spaces where the architectural one carries Rooms |
| `FILTER_ELEMENTS_BY_TYPE` | *Select All Instances*, as a request. **Whole model or this view is an input, not a default** — the two differ by an order of magnitude, and choosing silently is how a change meant for one floor reaches nine |
| `READ_ELEMENT_MATERIAL` | Every material with its volume, not the first — a wall has every layer of its build-up. A zero-volume finish is still **named**, because dropping those loses exactly what a finishes schedule is about |
| `COPY_PARAMETER_VALUE` | Compares storage types **before writing anything**, and a mismatch stops the whole batch. It copies what is **stored**, never what is displayed: a length shown as "2400 mm" is stored in feet, and copying the display into text writes a unit-suffixed string no later calculation can use |

### The one that did not need building

*"What is the elevation of this"* was answering `READ_ROOM_GEOMETRY` and looked like a missing
capability. **It was not.** `READ_ELEMENT_LEVEL` already returns the level and the offset, and those two
*are* the elevation. The gap was in the **words**, not in the library — so two utterances were added and
no fragment was written.

> A second fragment computing the same number from the same inputs is a duplicate that can one day
> disagree with the first, and nothing would say which was right. **Check whether the library already
> composes the answer before adding to it** — the same check `count them by level` passes, since
> `READ_ELEMENT_LEVEL` into `GROUP_AND_COUNT` is that job already.

### The compile harness gained a namespace, and that is a promise to unbuilt work

`FILTER_ELEMENTS_IN_ROOM` needs `Room.IsPointInRoom`, which lives in `Autodesk.Revit.DB.Architecture` —
not among the namespaces the harness supplied. The alternative was hand-rolling point-in-polygon over the
boundary segments, **which is precisely how an L-shaped room gets answered wrongly**, and that is the case
the fragment exists to get right.

> **The `USINGS` list in [`tools/check-fragments-compile.py`](../tools/check-fragments-compile.py) is a
> contract with unbuilt work.** It declares what a fragment may assume is in scope, so
> [D-28](DECISIONS.md)'s Roslyn executor must supply the same set. A namespace added here and not
> there compiles green and fails at the PC — the second obligation this library has placed on that
> executor, after the `REVIT20xx` compile symbols.

**What none of this is.** Not one has met a model. `check-gaps` counts **70** fragments below `PROVEN`.

---

## The thirteenth session, 2026-09-01 — seven more, and the question that costs an hour

**What it did:** took the library from **60 to 66**, plus one. All 66 compile on all eight releases; all
66 are `DRAFT`.

| | |
|---|---|
| `DIAGNOSE_VISIBILITY` | ***"Why can I not see my ducts"* — the most expensive question in Revit**, because there are at least eight unrelated answers and no one place to look. **It asks the ground truth first**: a collector scoped to the view returns what the view actually contains, so everything after is explaining a fact rather than predicting one. **It reports EVERY cause, not the first** — two at once is routine, and fixing one leaves it still invisible. And it **names what it did not read** rather than inventing the likeliest, because an invented cause reads exactly like a found one |
| `SET_VIEW_RANGE` | The commonest of those causes. **Reports the range before changing it, always** — setting one blind is how a coordinated drawing starts showing the storey above, and the old numbers are the way back |
| `SET_VIEW_SCALE` | Changing scale changes every tag and dimension's apparent size. A template usually **owns** scale, so a batch that refuses is pointing at the template, not the views |
| `FIND_VIEWS_SHOWING_ELEMENT` | **Ask before deleting.** An element on four issued sheets is a different decision from one on none, and Revit will not warn. Genuinely expensive — one collector pass per view, because Revit keeps no reverse index — and it says so rather than pretending otherwise |
| `ARRAY_ELEMENTS` | **The direction is normalised**, so a vector handed in between two points 12 m apart does not silently multiply the pitch twelvefold. Count includes the original, as Revit's own array does. Plain copies, not a linked Array element — which changes what every later edit does |
| `SET_ELEMENT_LEVEL` | **The one with the trap, and the trap is the point.** Height is an OFFSET from the level, so changing the level and leaving the offset makes a duct at 2800 above Level 1 into one 2800 above Level 2 — three metres higher, silently, found when somebody sections through it. The offset is recomputed and **the absolute height verified afterwards** |

### `DIAGNOSE_VISIBILITY` and `FIND_VIEWS_SHOWING_ELEMENT` are the same read asked two ways

One view, many causes — or one element, many views. Worth knowing together, because the second is what
turns *"can I delete this"* into an answer.

### An invented agent id, caught before the tool caught it

`ARRAY_ELEMENTS` was first written claiming `HERON-REVIT-GEO-007`, **which does not exist** — there is no
GEO agent in the registry, and every other geometry fragment claims `HERON-REVIT-ELE-010`. It was found
by checking rather than by assuming, which is the third invented agent id this repository has seen and
the first not to reach a checker.

**And `SET_VIEW_CROP`'s guard from the last session has a sibling worth noting**: two of this batch's
fragments (`SET_VIEW_RANGE`, `SET_VIEW_SCALE`) take an `apply` flag so the same fragment answers *"what
is it"* and *"change it"*. That is deliberate — the read is what makes the write reversible, and
splitting them would let somebody set a value without ever recording what it was.

**What none of this is.** Not one has met a model. `check-gaps` counts **66** fragments below `PROVEN`.

---

## The twelfth session, 2026-09-01 — seven more, weighted to the MEP job

**What it did:** took the library from **53 to 60**, chosen the same way. All 60 compile on all eight
releases; all 60 are `DRAFT`. The batch leans deliberately towards the work the owner actually does —
three MEP capabilities and three drawing-production ones.

| | |
|---|---|
| `MEASURE_MEP_VELOCITY` | *"Is this duct too small for the flow"* — which returned `CREATE_DUCT`. **Computes velocity from flow and area AND reads Revit's own, then reports both**: `RBS_VELOCITY` is derived from the size and flow last calculated, so a duct resized afterwards keeps reporting the old figure and looks fine on a schedule. Two numbers disagreeing is the finding |
| `SET_MEP_INSULATION` | Ducts and pipes in one selection, because a modeller does not sort a run by discipline first. **Existing insulation is replaced, not added to** — Revit carries two layers happily and the run then occupies a size nobody drew. **A clash check run before insulating is a clash check of the wrong model** |
| `READ_MEP_SYSTEM` | The system **name** and the system **type** are different things and "system" means both: two ducts can share *Supply Air* and be on separate systems, which is exactly what somebody is checking. Read as what the model **says** — `TRACE_CONNECTIVITY` is what goes and looks |
| `CREATE_3D_VIEW` | The name is set **after** the view exists, so a duplicate name leaves a usable view rather than an exception and an orphan. `named` is a separate output for that reason |
| `SET_VIEW_CROP` | Crop, section box and isolate are all asked for as *"just show me this bit"* and do different things. **Only the crop changes what prints** — on a view already on a sheet, whatever falls outside is gone from the issued drawing and no reviewer can tell it was ever there |
| `PLACE_VIEW_ON_SHEET` | Asks `CanAddViewToSheet` **before** attempting. A view can be on only one sheet, which is the cause of most refusals here, and the fix is to duplicate it — a different fragment |
| `MEASURE_ELEMENT_VOLUME` | Surface area is the MEP number: lagging and painting are bought by the square metre. Measured from the **solids**, walking nested geometry — reading only the top level finds nothing for most equipment. **Insulation is not included**, so pricing lagging means measuring the insulation elements |

### What the checkers caught this time

**`check-structure` caught a house-rule break the compiler was happy with.** The insulation fragment
used fully-qualified `Autodesk.Revit...` type names, which crosses the adapter boundary
([docs/16 §4](16-version-support-strategy.md)) — every other fragment relies on the harness's own
`using` lines. It compiled perfectly on all eight releases; only the structure rule saw it.

> **And then the fix's own comment failed the same check**, because the rule matches on TEXT and the
> sentence explaining it spelled the namespace out. Reworded rather than teaching the checker to parse
> comments: a blunt rule that catches its own explanation costs one rewording, and a comment-aware
> parser costs something that can be wrong. Same family as `check-gaps` reporting its own regex as two
> undeclared agents.

**One error was caught by re-reading before compiling, not by a tool.** `SET_VIEW_CROP` first guarded
with `!view.CropBoxActive && !view.CanBePrinted`, which refuses on the wrong condition entirely and
would have compiled green. The rule it wanted was: a template or a view that cannot be printed.

**Retrieval, measured on 317 declared sentences**: words **92% first, 99% in the top three**; nearness
**67% and 84%**. Contested: **25 of 317**, all judgements. The risk-direction section is empty for the
reason it now states itself.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **60** fragments
below `PROVEN`.

---

## The eleventh session, 2026-08-31 — seven more, and a retraction

**What it did:** took the library from **46 to 53**, and **withdrew a finding the previous session
reported.** All 53 compile on all eight releases; all 53 are `DRAFT`.

### The retraction, first, because it was told to the owner as fact

The tenth session added a check for *a question answered by something that writes*, said it **found six
and that four were pre-existing library defects**, and named them. **All six were false.** Nothing in the
library was wrong. The check was.

**How it was wrong, in two stages, and the second is the instructive one.**

| | |
|---|---|
| **First** | It measured `taken` — the **keyword ranking**. Its own printed claim was *"a caller acting on the top hit"*, and no caller uses the keyword route alone |
| **Then** | Corrected to `retrieve` — the fused stage. **Still wrong, and far less obviously.** The host calls `find`, which tries the **identity** and cache short circuits *first*. `retrieve` is fusion alone |
| **Why that hid it** | `check-routing` asks every fragment **its own declared utterances** — and those are exactly the sentences `find` answers by identity, *before any ranking runs*. So the check was ranking sentences the host never ranks |

Verified directly: all six resolve to their own fragment on route `identity`.

> **The rule this leaves.** A check that makes a claim about **consequence** must call the same entry
> point the system calls — not the stage that looks like it. A per-route diagnostic may report any route
> it likes; it may not describe one route's ranking as what the system does. The section above it has
> always said which route it measures, and was right to.

**The check is kept, now calling `find`, and it prints its own limit rather than a bare green.** It is
empty **by construction** while the identity route holds, and its emptiness says the identity route
works — not that no question can reach a writer. **The real risk surface is paraphrase**, which no
fragment declares and this corpus therefore does not contain. What it still catches: a declared question
that stops matching by identity, or two fragments declaring one sentence where the survivor writes.

**One thing from that session survives on its own merits** — `TAG_ELEMENTS` really was carrying a
composition sentence that belonged at the step it starts from, and moving it was right for reasons that
have nothing to do with the faulty check.

### The seven

| | |
|---|---|
| `FIND_CLASHES` | Element-against-element, **solids not bounding boxes** — a box around a diagonal duct overlaps everything in the rectangle it spans, which is how a clash report grows to hundreds of rows nobody reads. **Not `CHECK_OBSTRUCTIONS`**, which casts a ray from a *point* before anything is modelled; the two are asked for in the same words |
| `GROUP_ELEMENTS` | Reads the member count back **from the group**, not from the input. Creating a group creates the condition that makes `MOVE_ELEMENTS` report blocked |
| `UNGROUP_ELEMENTS` | Hands the released members back as `elements`, so *ungroup then move* composes. Grouped, pinned and owned all present as *"it will not move"* and have three different fixes |
| `READ_ELEMENT_PHASE` | **Both** phases. Created-in-Existing and created-in-Existing-**and-demolished** read identically until the second is asked for, and they are opposite instructions on site |
| `SET_ELEMENT_WORKSET` | `worksetId` is an **int**, deliberately — a `WorksetId` is not an `ElementId` and was untouched by 2024's 64-bit change |
| `APPLY_VIEW_TEMPLATE` | A template **overrules** hand-applied graphics rather than deleting them, silently. That is the conflict the grayout job has to route around |
| `DUPLICATE_VIEW` | The option has **no default**: `AsDependent` is not a copy in the sense anybody means, and picking it by accident produces a view that mysteriously refuses changes later |

**No new routing problems.** The seven introduced none, and the risk-direction section is empty for the
right reason. Contested sentences: **21 of 282**, all judgements.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **53** fragments
below `PROVEN`.

---

## The tenth session, 2026-08-31 — seven more, and a new check that found four old defects

**What it did:** took the library from **39 to 46**, chosen the same way — the brain asked the owner's
own sentences, and the answers read. All 46 compile on all eight releases; all 46 are `DRAFT`.

The holes it closed were answered wrongly rather than not at all: *"change the name of these"* returned
`SET_MEP_SIZE`, *"who owns this"* returned `TRACE_CONNECTIVITY`, *"what is this element"* returned
`MEASURE_ELEMENT_LENGTHS`, and **deleting was missing entirely**.

| | |
|---|---|
| `DELETE_ELEMENTS` | The most destructive thing in the library. **Reports the set Revit actually removed, never the input count** — a wall takes its doors and windows, a duct takes its fittings, and `alsoWent` is the difference between a tidy-up and an accident. Run inside a rolled-back transaction, the same call is a deletion **preview** |
| `RENAME_ELEMENTS` | Find-and-replace inside the name, because *"change SUP to SUPPLY"* is the shape the job takes. An element whose name does not contain the text is `notMatched`, never counted |
| `CHANGE_ELEMENT_TYPE` | Reads the type before **and after**: `ChangeTypeId` sometimes returns quietly having done nothing, the same silent no-op as the move that moved nothing |
| `DESCRIBE_ELEMENTS` | *"What is this"* — category, family, type. Read from the **type**, not the instance name, and the id is formatted rather than read as a number, which is what keeps it clear of 2024's 64-bit `ElementId` |
| `READ_ELEMENT_OWNERSHIP` | Which elements another user holds, **before** a batch stops at the eleventh with ten already changed. It is `E8` in the register, and it never checks anything out — looking must not be the act of claiming |
| `READ_ELEMENT_LEVEL` | The level **and the offset**, because the level alone misleads: a duct on Level 1 with a 3800 mm offset sits above Level 2's floor and correctly reports Level 1 |
| `SET_VIEW_SECTION_BOX` | Box the clash and look at it. Sets the box **and switches it on** — setting without activating leaves the view identical, which reads as the call having done nothing |

### The new check, and why it is not just another collision report

`check-routing` reported eighteen contested sentences and could not tell the harmless ones from the
dangerous ones. Two of the new collisions were the same shape: **a question answered by a fragment that
writes to the model.** *"What category is this"* landed on the fragment that overrides category
graphics; *"check the tagging on this drawing"* landed on the one that places tags.

> **That is different in kind from a collision between two reads.** A caller acting on the top hit does
> not get a slightly worse answer to its question — it **changes the model in reply to one**. Risk is
> already declared on every fragment, so the check costs a lookup.

**It reported six on its first run and ALL SIX WERE FALSE. Corrected 2026-08-31 — see the eleventh
session below.** They were an artefact of the check measuring the keyword ranking of sentences the host
never ranks at all. Do not act on the four "pre-existing library defects" this paragraph used to name:
*"list every duct in the model"*, *"these ones"*, *"the ceiling height in here"* and *"follow the pipe"*
are each served correctly, by their own fragment, through the identity route.

**One of the six was a genuine over-claim and the fix stands on its own merits.** `TAG_ELEMENTS` carried the
utterance *"tag the ones that are not tagged yet"* — which is find-**then**-tag, a composition, the layer
Steps 10 and 11 already established no fragment can win. Held on the writing fragment, it dragged
`FIND_UNTAGGED_ELEMENTS`' own read sentence across with it. Moved to the step the composition **starts
at**, which hands `elements` straight to the tagger. Six became five.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **46** fragments
below `PROVEN`, and compiling proves the API surface agrees and nothing about whether a duct is deleted,
renamed or measured correctly.

---

## The ninth session, 2026-08-31 — seven fragments, and three of them were found by tools

**What it did:** took the library from **32 to 39**, chosen by evidence rather than by picking from the
owner's earlier library in order. All 39 compile on all eight releases; all 39 are `DRAFT`
([D-44](DECISIONS.md)).

**How the seven were chosen.** The brain was asked the sentences the owner actually says, and the
answers were read. Some were not near misses but opposites: *"how many metres of pipe"* returned
`CREATE_DUCT`, *"renumber the doors"* returned `SNAP_TO_GRID`, and **nothing could undo a hide** — Heron
could take things off a view with `HIDE_ELEMENTS` and `ISOLATE_ELEMENTS` and had no way to put them back,
so every hide was a one-way door. Those holes picked the batch.

| | |
|---|---|
| `SHOW_ELEMENTS` | Closes the one-way door. **Undoing a hide is not the mirror of doing one**: a permanent hide is undone element by element, a temporary one cannot be — Revit's only exit is leaving the mode, which brings back everything. So the temporary path ignores its own element list and says `temporaryModeCleared` rather than a count that would imply a precision it has not got |
| `SET_PIN_STATE` | The other half of the move defect this repository already found: Revit's move returns normally and moves nothing for a pinned element. Reports what **changed**, counting already-pinned separately |
| `MEASURE_ELEMENT_LENGTHS` | The takeoff question. Anything without a length is **named, never counted as zero** — a fitting scored 0 mm gives a total that looks complete and is short |
| `RENUMBER_SEQUENTIAL` | Does **not** decide the order — that is the caller's. Checks collisions across the whole batch **before writing anything**, because a half-renumbered corridor has two schemes in it and no record of where it stopped |
| `MEASURE_MEP_SLOPE` | Computed from the endpoints, not read from the slope parameter — a pipe whose parameter says 1% and whose ends are level is flat. Being a ratio, it is the one measurement in the library that **cannot carry a unit error** |
| `TAG_ELEMENTS` | One tag per element, skipping the already-tagged: a second pass otherwise stacks a duplicate exactly on top of the first, invisible until somebody drags one |
| `FIND_UNTAGGED_ELEMENTS` | **Written because the dependency graph said it had to exist** — see below |

### Three defects the tools found, and only one of them was in the C#

**1. The compile gate caught a version break that no documentation would have.** `find-untagged-elements`
was written against `IndependentTag.TaggedElementId`, which compiled clean on 2020 and 2022 and **failed
on 2027 — the property is removed there.** Its replacement, `GetTaggedLocalElementIds()`, **arrived in
2022**. The overlap is 2022–2026 and **no single accessor spans the supported releases**, so the
implementation splits on a compile symbol. This is the owner's own named problem — *"it errors on a newer
Revit"* — caught before the machine instead of mid-job.

> **It puts a requirement on unbuilt work.** This is the first fragment in the library to need a
> `REVIT20xx` compile symbol, so [D-28](DECISIONS.md)'s in-process Roslyn executor **must define the
> same symbols MSBuild does**. A host defining none takes the `#else` branch and breaks on 2020 and 2021
> only — which is the release the owner actually runs.

**2. The dependency graph refused a fragment that compiled perfectly.** `TAG_ELEMENTS` took an
`alreadyTagged` list and **nothing on disk could produce one**, so `test_graph.py` reported an action
nothing can feed: a composition that cannot be assembled however well each half compiles. The easy fix
was to mark that input as coming from the request, which would have made the graph quiet and the problem
permanent — nobody can type a list of element ids. So the missing piece was written instead, and it
answers a real question of its own: *"which ducts have not been tagged"*.

**3. `check-routing` caught the new fragment stealing another's sentence — and the first fix made it
worse.** `RENUMBER_SEQUENTIAL` was answering *"set the room number"*, which is
`WRITE_ELEMENT_PARAMETERS`' own declared utterance and a plain single write. The fix written first added
a disclaimer to `purpose` quoting that sentence — **and `purpose` is indexed**
([`heron_search.py`](../brain/heron_search.py) puts it in the searchable text), so the fragment was handed
one more copy of the words it was losing on and did not move.

> **The general rule, and it applies to every routing table in the library: a disclaimer written into an
> indexed field makes the fragment claim the sentence it is disclaiming.** Routing tables belong in
> comments, where a human reads them and the index does not. Contested sentences went 16 → 15 of 205 once
> the quote was removed from `purpose`.

**And a test fixture that had expired twice was made to derive itself.** `test_graph.py` hardcoded the
providers of `elements` — one until 2026-08-29, two until this session, and each time a correct library
failed the test and the fix was to type one more path. It now finds them from disk. Its first version
matched the same two lines under `needs:` as well, which broke the composition from the consumer's side
and passed for the wrong reason; it is scoped to the `provides:` block.

**What none of this is.** Not one of the seven has met a model. They are `DRAFT`, `check-gaps` counts
**39** fragments below `PROVEN`, and compiling proves the API surface agrees and nothing whatever about
whether a duct is measured in millimetres or feet.

---

## The eighth session, 2026-08-31 — the server that would not have started

**What it did:** installed the MCP SDK for the first time in this project's life, which took `A8` from
*needs Windows* to *mostly done* and, in the same ten minutes, found that **Heron's MCP server would not
start at all on a machine installing today.**

**The defect, and why nothing here could see it.** `pip install --user mcp` — the line
[`tools/HeronRevit.ps1`](../tools/HeronRevit.ps1) hands the user, unpinned — resolved to 1.x when the server
was written and resolves to **2.x** now. 2.x **deleted `mcp.server.fastmcp`**: `FastMCP` was renamed
`MCPServer`. The import was written against 1.x, so the server raised `ImportError` before registering a
single tool — **every Heron tool absent from the host**, with no Revit and no Windows anywhere in the
failure. Every test in this repository reads that file **as text**, and text cannot fail an import. The
one technique nobody had used was the obvious one: install the dependency and start the thing.

| | |
|---|---|
| **The fix** | The import, and nothing else. The class is looked up **newest first**, because 2.x's `MCPServer` takes the same `@server.tool()` decorator and the same `run()` — measured, not assumed, with both SDKs installed side by side. This is [D-05](DECISIONS.md)'s rule about Revit releases applied to a Python dependency: an unlisted version must fail **loudly**, so the last `except` re-raises naming the install line rather than leaving an `ImportError` about a module the user never typed |
| **The check that would have caught it** | [`tests/test_mcp_serves.py`](../tests/test_mcp_serves.py) — the SDK's own registry against the source text, every description, every argument schema, and the three brain tools called through the SDK's own dispatch. **Validated by putting the defect back** and watching it fail under 2.x, which is the standard this repository already holds `check-api-surface.py` to |
| **`heron_version` now reports the SDK** | *"Heron stopped working"* and *"the SDK moved underneath it"* are indistinguishable from the user's side, and this tool's stated job is what to say when something is wrong |

**A skipped suite is now WAITING, not `ok`.** The SDK is an optional dependency, so on a machine without
it that test cannot run — and `check-gaps.py` sees **only exit codes**, since it sends stdout to
`DEVNULL`. A suite that skipped would have been reported `ok`. So a skip exits **3**, and `check-gaps`
reads it as WAITING: the same distinction the whole tool is built on, extended to the one place it could
not reach. This is the *"a killed run prints a header with nothing under it, and a blank reads as a
pass"* failure, caught before it happened rather than after.

**Two register rows were wrong in a way that would have failed a correct Heron**, and both were found by
running what they asked for rather than by reading them:

- **`A8`'s PASS text was stale.** It required *ten jobs, **four** with every part provided, and the seven
  missing capabilities named*. The truth is **10, 10 and none** — the row was written on 2026-08-29
  *before* the seven capabilities were written later that same day, and never revisited. As worded, it
  would have failed a Heron that was working correctly.
- **`A8` also required `heron_lookup` to answer `FILTER_ELEMENTS_BY_CATEGORY`.** It answers
  `SET_SELECTION`. That is **the assertion Steps 10 and 11 already retired**, copied into the register
  and left behind when they retired it: *"select all the ducts"* is filter-**then**-select, a composition,
  and a composition is what a **skill** names. It has been retired here too, **with its reasoning
  written down**, because deleting it quietly would have looked identical and taught nobody anything.

**`A7` is still blocked, and the row now names the right wall.** `pip install model2vec` **succeeds** —
PyPI is reachable. `StaticModel.from_pretrained("minishlab/potion-base-8M")` fails with
`ProxyError: 403 Forbidden`. The precondition was written as *"a machine that can reach a model host"*,
which anyone with a working network would read as already satisfied; it now says **huggingface.co**. And
the attempt proved one thing for free: with the package installed, the fallback ran against a **failed
download** instead of a missing import — a branch that had never once executed — and degraded to
`lexical`, saying so on its first line.

> **The lesson, and it is the same one four times now.** *"It needs Windows"* was a guess that got
> written down as a fact and inherited. It needed `pip install mcp`. The count of things believed to be
> waiting on a machine that were waiting on somebody trying them is now **four** — the .NET SDK, the
> WindowsDesktop targets, the bridge round trip, and this. **Assume the fifth exists.**

---

## The seventh session, 2026-08-30 — the library, and two bugs it found

**What it did:** took the fragment library from 7 to **32**, re-authored from the owner's earlier Brain
under [D-25](DECISIONS.md) — studied and rewritten, never copied, split where a 243-line original
was really two jobs. All 32 compile on all eight releases. All 32 are `DRAFT` ([D-44](DECISIONS.md)).

**Two real bugs came out of the studying, and they are the same bug wearing different clothes.** Both are
this project's defining failure: **reporting the number that was ASKED FOR as the number that HAPPENED.**

| | |
|---|---|
| **The move path** | Revit's move call returns normally and moves nothing for a pinned element **or a group member**. Heron skipped pinned ones — but *a duct in a group is not pinned*, so it passed the filter and the wrong count reached the answer **and the audit log**. `RevitWrite` now probes positions before and after: **moved / partly / blocked / unverified**. `E10` is the check |
| **The parameter path** | Revit accepts a size, returns **TRUE**, throws nothing, then **snaps** it to the nearest size the type carries — ask a pipe for 77 and it comes out 80. `WRITE_ELEMENT_PARAMETERS` trusted that return value. Its own proof case asked a *human* to check the stored number matched, which the code had no way of knowing |

**The parameter fix is worth reading before writing anything that sets a value.** It needs no string
parsing and no unit handling, because it uses the snap's own timing: *straight after the set the
parameter still reports what was asked for, and the snap happens at REGENERATION.* So read the internal
double, regenerate once for the batch, read it again, compare. Two numbers from the same API in the same
units. `SET_MEP_SIZE` carries the same check for sizes reached by `BuiltInParameter` — deliberately a
second home, because a name lookup for *"Width"* finds nothing on a French install.

**A new tool, and it earned itself on its first run.** Adding a fragment can make an **existing one
unfindable**, silently, and nothing here would have noticed. It had already happened twice.
[`tools/check-routing.py`](../tools/check-routing.py) asks every fragment its own declared words back to the
search. It found sixteen contested sentences; **three were real errors** — a filter claiming two of
`TRACE_CONNECTIVITY`'s sentences, and an override fragment claiming the grayout **skill's**. Fixing those
took the words route to **100% in the top three**. The other thirteen are genuine English ambiguities and
were left alone.

> **It never fails a build, on purpose.** A collision is a judgement, not a defect. A checker that failed
> here would teach people to weaken their own utterances to buy back a rank — and that is the one
> response ruled out, because taking *"show me just these"* off the isolate fragment would make the
> isolate unfindable in order to protect a number.

**One decision is waiting for the owner at the PC.** `SET_CATEGORY_GRAPHICS` made the grayout skill
wrong: greying every wall one at a time gives the right drawing today and a wrong one tomorrow, because a
wall drawn afterwards keeps its normal graphics and nobody finds out until it prints. The skill now greys
the background **by category**. Whether he wants that, or wants it selection-scoped so he can grey some
walls and not others, is a modelling preference the API does not settle — it is written into
[`brain/skills/mep-grayout.yaml`](../brain/skills/mep-grayout.yaml) as a decision for him, not a default
someone chose quietly.

**And four tools were found answering when they should have declined** — retrieval printing `nothing
matched` against an **empty store** (indistinguishable from a real miss), searching unknown flags as
text, a structure checker failing purely on **timing** if a compile was running, and a test asserting a
fact about the corpus while its comment claimed it tested the backend. None changed a recorded number.
All four would have corrupted a later one.

---

> **If you read only one thing:** three walls fell on one day, none of which needed a Windows machine.
>
> **The C# compiles.** Revit 2020 through **2027**, every project, zero warnings — and the Revit-free bridge
> host *runs*, all 32 checks passing including the whole lease. It caught two real defects on its first
> run, both of which reading had already missed twice ([docs/30](30-compiling-away-from-windows.md)).
>
> **Every open question is answered — 41 of 41**, and **24 decisions** were taken (D-20 to D-43). Nothing
> gates any phase. Several were settled by *looking* rather than deciding: at a system already doing the
> job, and at Heron's own code, where Q-13's answer had been running since Step 1.
>
> **The Constitution is accepted and binding** — all 30 Articles, after Ajmal asked for every one to be
> read out rather than tapping yes. Reading it aloud found three stale statements inside it.
>
> **PHASE 2 STARTED, 2026-08-28.** The owner has **no Revit for about a week** and said so plainly:
> *"checking in Revit is not possible within 1 week, so keep the checking process as a document and
> start Phase 2 — we need to finish that."* [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is now a **record
> rather than a gate**, and [27 — Build Order](27-build-order.md) carries **Steps 7 to 14**, written
> that day because Phase 2 had never been broken into steps. Seven of the eight need no Revit; only
> Step 14's proof does.
>
> **Step 7 is built and proven-as-far-as-it-can-be**: the fragment's shape on disk, and the validator
> that refuses a proof with no negative case. `python tests/test_fragment_store.py`. The library grew to
> **7** fragments over Steps 9–14; every one is `DRAFT` and **none has met a model** — which is what
> DRAFT means, and why they get no register row.
>
> **Step 8 is built too**: one knowledge store per scope, as one file each. A cross-scope query is
> impossible to *write* — the API takes one scope and `ATTACH` is refused by name, which is the one
> loophole in "one file per scope". Project knowledge with no project identified is **refused, never
> defaulted**, because a wrong guess writes one client's knowledge into another's file.
> `python tests/test_scope_store.py`.
>
> **Step 9 is built**: finding a fragment by exact words, on three routes that name themselves —
> `identity` (one lookup, no search), `cache` (this wording was resolved before), `keywords` (FTS5).
> **Only a `PROVEN` fragment may run off an exact match without asking**, and two fragments claiming one
> sentence is a *miss* rather than a coin toss. It found a real gap in Step 7 on its first run: fragments
> had no `utterances`, so `OST_DuctCurves` matched nothing — [09 §2](09-skills-and-fragments.md) had
> asked for them and Step 7 had not built them. Now required.
> `python tests/test_search.py`.
>
> **Step 10 is built, and it is the one to read the caveats on.** Embeddings are local, offline and need
> nothing installed — but the built-in backend is **character n-grams, not meaning**, and the tests say
> so in measured numbers rather than in prose: synonyms score **zero or less** (`diffuser`/`grille`
> −0.136), while plurals, word endings and word order all work. The trained backend that *would*
> understand synonyms **has never run**: `huggingface.co` is refused by this container's network, so no
> weights could be fetched. `A7` in the register is that run, and it needs **no Revit and no Windows** —
> any machine with a working network will do.
>
> It also recorded its own successor's acceptance test: on *"show me every duct"* the keyword route
> ranked the right fragment first and the vector route ranked it second, so **Step 11 must fuse them.**
> **That assertion has since been retired, and the reasoning is below** — it held at two fragments and
> stopped at seven, because the sentence is a composition and a composition is what a *skill* names.
> `python tests/test_embed.py`.
>
> **Step 11 is built, and it passed that test at the time — read how, and read what happened to the
> test.** The version filter runs **first, as a wall**: a fragment declared for 2021 is not returned for 2025, proven by making it the best possible
> textual match and watching it stay absent. The two routes are then **fused** rather than chosen
> between, weighted by which embedding backend is running, because Step 10 measured the built-in one as
> not-meaning and an equal vote would over-trust it.
>
> Two of its own claims were wrong and the tests caught both. The quality nudge was **eight times larger
> than one rank of fusion** — it could have jumped a `PROVEN` fragment eight places over better matches,
> which is precisely what its docstring said it could not do. And *"both routes agree"* turns out to mean
> **nothing** while the library is smaller than the retrieval pool: the nearness route ranks every
> eligible fragment, so everything agrees, including a question about cats. The answer now says so in
> its own note. `python tests/test_retrieve.py`.
>
> **Step 12 is built** — the capability registry, and it passes its own acceptance test: a second
> provider is added and **the call site is the same line of code**, then the first is deleted and the
> same line still answers. A capability nobody provides **is** the gap, so there is no second list to
> keep in step.
>
> Its one real design decision: **almost everything is derived rather than stored**, which is
> [D-40](DECISIONS.md) applied. Risk in particular — it already has two homes (the tool registry
> and each fragment) and a third declaration would guarantee that one day two disagree and nobody knows
> which is true. So a capability's risk is the **highest among its providers**, computed — and providers
> that disagree about it are **reported as a defect**, because a thing that reads and a thing that
> modifies are not two implementations of one capability. A declared value would have hidden exactly
> that. Platform support is an **intersection** for the same reason: a union would claim 2027 because
> one provider manages it, then hand back one that does not. `python tests/test_capability.py`.
>
> **PHASE 2 IS BUILT IN FULL — all eight steps, 7 to 14.** Step 13 is the dependency graph, where every
> edge but one is computed on demand and the deriver was **shown catching a break** before its clean
> answers were believed. Step 14 is ten skills, each naming **capabilities and never fragments**.
>
> **`python tools/check-gaps.py` is the one command to run first now.** It sweeps everything — the build
> order against disk, every test, every checker, every agent id, the library, the registry, the graph,
> the register — and sorts it into **UNFINISHED** and **WAITING**, with the exit code following only the
> first. It now reports **nothing unfinished** and **55 waiting**: 47 need a real Revit, 3 need Windows,
> 1 needs a reachable network, 3 need a conversation with the owner. **It reported nothing unfinished
> until 2026-08-29 for the wrong reason**, when the reachability check was added; that earlier green was
> real about the build order and wrong about the repository. This one was earned by wiring the brain up —
> but read it knowing what the new check asks: whether the seam **exists**, not whether a host has ever
> called through it. That second question is `A8`.
>
> **It found real defects on its first runs, including two in itself.** A scanner that scans itself finds
> itself — it reported its own regex as two undeclared agents. It also caught a second invented agent id
> in as many steps. And growing the library from 2 fragments to 7 exposed a **query bug that was
> invisible at two**: every word was prefix-matched, so `in*`, `me*` and `the*` outvoted the one word in
> the sentence that carried meaning.
>
> **One assertion was retired rather than repaired, and the reasoning matters.** Steps 10 and 11 both
> asserted that *"show me every duct"* must rank the duct **filter** first. That held at two fragments
> and stopped at seven — and the honest reading is not that retrieval got worse, it is that the
> assertion asked the wrong layer. That sentence is filter-**then**-select: a composition, which is what
> a **skill** names. **Next is proof, and it needs the machine.**
>
> **What that did NOT change:** `write.enabled` is still `false` and every parked item is still unproven.
> `R1` — read the day's decisions back — **did not happen before Phase 2 began**, contrary to the
> instruction that asked for it; that was overridden by the owner, which is his to do. **It happened on
> 2026-08-29 instead, and all twenty-one were confirmed with nothing moved** — so the cost of doing it
> late was, on this occasion, nothing measurable. **`A1`, `A2`, `A3` and `A5` are done** — the whole of
> Group A except the Windows named pipe in `A4`, the probe check in `A6`, and `A8`.
>
> Before writing any code, run `python tools/check-compile.py`. It takes minutes, it now covers **all
> eight releases**, and it is no longer somebody else's job. On a fresh Linux box it needs
> `apt-get install -y dotnet-sdk-10.0` first — the .NET 8 package builds only 2020–2024.

> ## ⚠️ READ THIS FIRST — you are probably on a machine with no Revit
>
> The owner is continuing this work **from mobile**, in Claude Code. **There is no Revit there**, and
> there is no Windows, so **nothing in this repository that touches Revit can be tested.**
>
> That does not mean stop. It means **be honest about which half you are in**, and mark every piece of
> Revit-side work as untested until it has met a real Revit on the owner's own machine.
>
> **What still works away from Revit:** [§5](#5-what-you-can-and-cannot-do-without-revit).
> **What must be re-tested on return:** [§6](#6-the-return-to-the-machine-checklist) — keep it up to date.

Then read [docs/README.md](README.md) for the map and
[docs/27-build-order.md](27-build-order.md) for what to build.

---

## 1. Where the project stands, in one paragraph

**Phase 0 is complete.** Steps 1 to 5 of 6 are built and **proven in real Revit 2020 and 2024**: the
bridge, the thread hop onto Revit's own thread, a working MCP server inside Claude Code, *"select all
ducts"* with an audit trail, and one-chat-one-Revit binding that fails closed. The repository is
**private**.

**Step 6 — the first write — is BUILT, COMPILED, AND STILL UNPROVEN, and the gap between those words is
the whole story of this section.** It was written on a phone, on a machine with no Revit, no Windows and
no .NET SDK. **A compiler has now read every line of it** — Revit 2020, 2021, 2022, 2023 and 2024, zero
warnings — which it had not on 2026-08-27, and which cost one 2020-only defect to find out. The chat half
is tested and passing. But it has still **never loaded into Revit and has never moved anything**. Do not
read "Step 6 compiles" as "Step 6 works": compiling proves the API surface agrees, and says nothing
whatever about whether a duct moves 200 millimetres or 200 feet.

**Heron can no longer be read-only by construction, so it is read-only by default instead.** That is a
real weakening and it was made deliberately: until 2026-08-27 there was no transaction code in the
repository at all, and the guarantee needed no trust. Now there is, and the guarantee rests on
`write.enabled` defaulting to `false` in `HeronPermissions`. **Leave it false until the write path has
been through a real Revit.** [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is how that happens.

**Four things arrived after the write path, because auditing found them missing rather than anybody
remembering them.** The **lease** ([D-22](DECISIONS.md)) — Step 5 deferred it to *"Phase 1 with
writes"* and Step 6 is that write; a second chat is now refused instead of cutting the first off mid-job.
The **Failure Analysis Agent**, which never blind-retries and fails closed. The **tool registry**, so risk
is declared in a table rather than as a literal buried in the write path. And **configuration and health**,
which between them exposed two silent bugs — `write.enabled` could never have been switched on, and the
two halves disagreed about how long to wait.

**All 21 Golden Rules are now official** ([Q-19](OPEN-QUESTIONS.md), accepted 2026-08-28) — including
the four Step 6 was built to obey. They were accepted *before* Step 6 is proven, deliberately: a rule that
only binds once the code passes is not a rule the code was ever held to.

**PHASE 1 IS BUILT IN FULL — all eleven items, not just Step 6's seven.** The last three were found by
auditing Phase 1 against its own list rather than against the build order, and none of them had been
flagged anywhere:

| | |
|---|---|
| **The audit says WHICH elements** | It logged `moved: 4`. A count cannot answer the question anybody asks after something goes wrong — *which* ducts — and that is the entire point of an append-only record. It now carries every moved element's `UniqueId`, the document identity and the undo entry name, under one Workflow ID |
| **The Golden Test Library** | 17 cases: the 7 things Phase 0 proved, and 10 that have never met Revit. Each records the files its proof rested on, so `python tests/test_golden.py` reports the proofs that have gone **stale**. It currently reports all seven as stale, which is exactly true |
| **The Workflow Engine** | Checkpoints and resume. Built to spec and covered by its own suite — and **nothing calls it yet**, deliberately. See the untested table in [§3](#3-what-is-proven-and-what-is-only-built) |

**Phase 1's own definition of done is not met, and cannot be met here:** *"a wrong instruction can be
reversed with one Ctrl+Z, and a failed operation leaves the model untouched."* Both are written, both are
covered by reasoning, and **neither has been witnessed.**

**PHASE 2 IS BUILT — all eight steps, 7 to 14, on 2026-08-28 and 2026-08-29.** It was unblocked by the
twenty-four decisions taken on 2026-08-28 (D-20 to D-43), which closed **every open question in the
project, 41 of 41**, and then it was built. What is on disk: eight Python modules under `brain/`, seven
fragments, ten skills, **nine** new test suites, and [`tools/check-gaps.py`](../tools/check-gaps.py) — the
sweep that looks for what is missing rather than waiting to be told. The ninth suite is
[`tests/test_brain_reachable.py`](../tests/test_brain_reachable.py), and it arrived last with the seam that
made any of the other eight reachable from a conversation.

**Phase 2's own definition of done is NOT met, and only two thirds of the reason is the missing Revit.**
The [build order](27-build-order.md) states it as three clauses: *ten real skills work, none
hard-coded; a re-authored capability carries its own proof; and the Orchestrator resolves through
capabilities rather than agent names.* The first two need a model — all ten skills and all seven
fragments are `DRAFT`, and [D-30](DECISIONS.md) promotes on a proof containing a negative case.

> ### The third clause was not blocked by anything — and is now done
>
> **RESOLVED 2026-08-29, later the same day.** [`mcp/server/heron_brain.py`](../mcp/server/heron_brain.py)
> is the seam and three read-only tools stand on it — `heron_capabilities`, `heron_resolve`,
> `heron_lookup` — each asking for a **capability** and never for a fragment. `check-gaps.py` now reports
> **nothing unfinished**. What follows is the finding as it stood, kept because the *way* it was missed
> matters more than the fix, and because two limits it names are still true: **resolving is not running**
> (there is no executor, [D-28](DECISIONS.md) is unbuilt), and the tools have **never been served by
> a real `FastMCP`** — that is `A8` in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md).
>
> **The brain is wired to nothing.** Found on 2026-08-29 by running `tools/check-metadata.py` and
> following what it said. The Orchestrator is **the host's job, not Heron's** — `HERON-ORC-MAIN-001` is
> listed as host-provided under [D-01](DECISIONS.md) and
> [docs/02 §7](02-architecture-overview.md), which is correct and deliberate. But the host reaches
> Heron **only through MCP tools**, and the seven tools in
> [`mcp/server/heron_tools.py`](../mcp/server/heron_tools.py) are all `revit_*` — every one goes straight
> to the bridge. **Nothing outside `brain/` and `tests/` imports the brain at all:**
>
> ```bash
> grep -rn "heron_capability\|heron_retrieve\|heron_skill\|heron_fragment" --include=*.py . \
>   | grep -v "^./brain/\|^./tests/"      # one hit, and it is a comment
> ```
>
> So eight modules, seven fragments and ten skills are **built, tested, and unreachable from the
> conversation.** The host cannot resolve a request through a capability because it has no tool that
> asks. That is Phase 2's third clause, and it needs **no Revit, no Windows and no network** — it is the
> one genuinely buildable thing left in this repository.
>
> **`check-gaps.py` did not catch this**, which is worth more than the finding. It checked that each
> step's module and test were on disk, which they were, and never asked whether anything *calls* them.
> A check has since been added so the tool stops reporting a green it has not earned — but the lesson
> generalises: **a checker built from a build order can only ever ask whether the build order was
> followed.**

| | |
|---|---|
| **D-23 / D-24** | The knowledge store is **SQLite, one file per scope**, decided on the per-user no-admin install constraint rather than on retrieval quality. Embeddings are **local** — now on the re-indexing argument, since D-26 narrowed the confidentiality one |
| **D-25** | The existing libraries are **studied and re-authored, never imported.** Ajmal's, and it deleted a planned import pipeline rather than choosing between libraries |
| **D-26** | **The model file is never uploaded.** Refined three times in one day, each time looser: the line is the `.rvt`/`.rfa`, not the information. Project names, counts, sizes and reasoning travel like any conversation |
| **D-27** | **There are no personas.** One voice; the *shape* of the answer follows the shape of the request. Settled by reading two assistants already doing this job daily, both of which switch nothing |
| **D-28** | Generated code is **Roslyn C#, in process** — not pyRevit, whose Routes server would put HTTP inside an add-in that has no network code at all. Closes Q-37 as not applicable |
| **D-29** | A fragment is a **composable piece**, not a whole answer: filter + action, with a recipe as a named third kind |
| **D-30** | A fragment is promoted by **one recorded proof with a negative case**, not by a count of runs — because the defect that matters is one that *succeeds while doing nothing*, and that passes a thousand runs |
| **D-31** | Product / data / derived were **already separated** since Step 1, and the updater half was verified rather than assumed |

**And the rest of the day closed every remaining question.** `D-32` to `D-43`, in the same conversation:

| | |
|---|---|
| **D-32** | **v1 must be able to change the model**, and reading is what gets used first. Recorded as read-only and **reversed within the hour** when the same question was asked again |
| **D-33** | **Heron never assumes an input. It asks — and it asks once.** There is no confidence threshold, because a number invented today is a number tuned tomorrow |
| **D-34** | Heron's wording is **English**; understanding the user is the host's job, not Heron's |
| **D-35** | A shared fragment may carry code, and an **unapproved one is refused, not warned about** — a warning hands the decision to whoever is least able to judge it |
| **D-36** | **No warranty**, and it was already in place twice. `DISCLAIMER.md` makes four promises that are **still unproven** |
| **D-37** | The name is **Heron AI** — *chosen, not cleared*. No trademark search has been done |
| **D-38** | **GitHub now**, App Store possible, nothing built for it. Autodesk's requirements have **not been read**, and were not written from memory |
| **D-39** | Shadow mode is approved on an **analysed disagreement**, never a count of agreements |
| **D-40** | The dependency graph is SQLite, and **an edge is derived before it is stored** |
| **D-41** | **Single-user now**; company knowledge is a git repo and the admin is the reviewer — D-35 at a smaller radius |
| **D-42** | The **public install command is deferred**; `setup.ps1` is what is proven |
| **D-43** | **The Constitution is accepted — all 30 Articles, binding** |

**`R1` is done — 2026-08-29, and it covered D-23 to D-43 rather than the five it asked for.** All
twenty-one were read back and confirmed; the three carrying real consequence (D-33's boundary, D-26,
D-32) were put to him one at a time and none moved. **`R1b` still needs a screen**, since
[D-14](DECISIONS.md) stays *Proposed* until he has seen the trust model working. They are Accepted and are being built on; the review confirms each still says what he meant
and fills in detail left out. See the block at the top of [DECISIONS.md](DECISIONS.md).

**Two were reversed within hours of being recorded**, D-26 three times and D-32 once. Neither was a
mistake — each was a first answer sharpened once its consequence was visible, which is the whole argument
for that read-back.

---

## 2. What exists

```
revit/          Part 1 — loads into Revit.exe. C#. Changing it needs a Revit RESTART
  Heron.Revit.Addin/   ribbon toggle, icons, the ExternalEvent dispatcher, the operations
  Heron.Bridge/        named-pipe server, per-session token. No Revit reference
mcp/            Part 2 — the bridge to the AI host. Python, outside Revit
  client/              discovery, retries, doctor. Dependency-free on purpose
  server/              the MCP server and the session binding
brain/          Part 3 — knowledge. BUILT IN PHASE 2, and all of it runs without Revit
  heron_fragment.py    the fragment's shape on disk, and the validator that enforces it
  heron_scope.py       one SQLite file per scope; a cross-scope query cannot be written
  heron_search.py      exact words — identity, cache, FTS5
  heron_embed.py       nearness, local and offline. Read its caveats before trusting it
  heron_retrieve.py    the two fused, behind a hard Revit-version filter
  heron_capability.py  ask for what you want done, never for who does it
  heron_graph.py       what breaks if this changes. Every edge but one is derived
  heron_skill.py       a skill names capabilities, never fragments
  fragments/           7, all DRAFT — none has met a model
  skills/              10, all DRAFT — same
platform/       Part 4 — the Kernel
  Heron.Core/          HeronPaths, HeronConfig, HeronIdentity, HeronAudit
tests/          17 suites. 16 run anywhere; test_bridge_roundtrip needs its host built first
tools/          scripts that keep the repo honest, plus setup and deploy
docs/           41 documents — specification, decisions, questions, roadmap
.claude/        skills and agents that ship with the repo
```

Each part has a `README.md` saying what belongs there and **when to fix things there**.

---

## 3. What is proven, and what is only built

**This distinction matters more than anything else in this document**, and it matters more than ever
now that the next stretch of work happens where Revit cannot be reached.

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

### Built, never met a real Revit

| | |
|---|---|
| **Revit 2027** | Builds and installs. Never launched — the owner says his 2027 does not work |
| **Naming the session in a selection answer** | Fixed after both models turned out to be called `Project1`. Needs a Claude restart to go live, then one look |
| **Anything committed from mobile after 2026-08-28** | **Assume untested** — but no longer assume unread: `python tools/check-compile.py` runs on mobile too. Add it to [§6](#6-the-return-to-the-machine-checklist) |
| **The whole of Step 6** | **Compiles** on 2020–2027 — all eight releases, 0 warnings (2026-08-28). Never loaded into Revit, and has never moved anything |
| `RevitWrite.cs` — preview, re-count, TransactionGroup, rollback | The one file that can change a model. It compiles; every **behavioural** claim in it is still unverified. Its `DocumentKey` was the one thing the compiler caught — `Document.CreationGUID` does not exist in Revit 2020 |
| `HeronUnits`, `HeronPermissions`, `HeronStop` | Kernel C#. Plain arithmetic and flags. Compiles; `HeronUnits` is exactly what `D3` exists to measure |
| The Emergency Stop ribbon button | New `PushButtonData` in a file whose ribbon currently works. **If Heron will not load after this, look here first** |
| **The lease** (`HeronLease`) | Refuses a second chat instead of cutting the first off. **Now exercised end to end against the compiled bridge** (2026-08-28) — claim, renew, refuse a second chat, never cut off the first, exempt `ping`/`info`. Not against Revit, and not over a Windows pipe: group H is what remains |
| **The audit's element list** | Every moved element's `UniqueId` now goes into the log. Never seen against a real model, and a move of several hundred elements writes a correspondingly long line |
| **The Workflow Engine** (`heron_workflow.py`) | Built to spec and covered by its own suite, but **nothing calls it yet** — and that is deliberate, not an oversight. Phase 1's only multi-stage flow is preview→apply, which the add-in already sequences better because it is the side that can re-count against the live model. Its real customer is Phase 2's 18-stage pipeline. Proven by its tests, unproven in use |
| **Bridge protocol 2** | A request now carries a `client` id. An add-in still on protocol 1 will refuse to talk — which is correct, and means the add-in MUST be rebuilt and redeployed. The handshake itself is proven: `info` reports `protocolVersion 2` |
| `revit_preview_move`, `revit_apply_move`, `revit_use_this_model` | The three new MCP tools. Never seen by a running host |

> The chat side of Step 6 **is** tested and passing — distance parsing, document pinning, single-use
> approval: `python tests/test_write_safety.py`. That test says nothing whatsoever about the
> transaction, the rollback or the move, and says so itself.

### Built in Phase 2, and none of it has met a model

| | |
|---|---|
| **The eight `brain/` modules** | Each has its own suite and each passes — the fragment store, the scope store, exact-word search, nearness, fusion, the capability registry, the graph, and skills. What they are proven to do is **behave as specified against fixtures**. Not one of them has been handed a real Revit's answer |
| **The whole fragment library, all `DRAFT`** | `DRAFT` is not a shortcut — it is [D-30](DECISIONS.md) being obeyed. A fragment is promoted by one recorded proof **containing a negative case**, and a negative case needs a model. They stay DRAFT until then, and they are why `check-gaps` carries them under *needs a real Revit*; read the count off the tool rather than from here. **Most of them at least COMPILE** — on all eight releases, 2020 to 2027 ([`tools/check-fragments-compile.py`](../tools/check-fragments-compile.py)) — which means those will not fail at the PC for a reason a compiler could have found. **The eight written on 2026-09-02 are the exception and have never been compiled at all**, because that container could not install the .NET SDK. That is `A9`, and it is the first thing to run on a machine that has one |
| **10 skills, all `DRAFT`** | Each names **capabilities and never fragments**, and each carries the words Ajmal actually says rather than the words the technique is named after. Whether any of them does what it says is unknown |
| **The trained embedding backend** | **The highest-value item that needs no Revit, and 2026-08-30 sharpened what it buys.** [`brain/retrieval-history.md`](../brain/retrieval-history.md) tracks one query across eight library sizes (7 → 32) *and* now measures a second way: every fragment's own declared words asked back to the search — 169 sentences, **words 92% first and 100% in the top three, nearness 60% and 82%**. That corrects the older headline in this file's own history: the nearness route has **not** collapsed in general. It handles **vocabulary overlap** and fails at **disambiguation**, which is why the tracked query — a sentence several fragments fairly claim — sits mid-library while a sentence naming one fragment comes back first. So `A7` should be expected to change the **contested** lookups, not every lookup. Never run — `huggingface.co` is refused by this container, and by a second one on 2026-08-31, where the package installed cleanly from PyPI and only the **weights** download was refused (`ProxyError: 403`). The backend that *is* running is character n-grams, which measurably does not do synonyms (`diffuser`/`grille` scored −0.136). Needs no Revit and no Windows |
| **The three brain MCP tools** | `heron_capabilities`, `heron_resolve`, `heron_lookup` — and the seam under them, [`mcp/server/heron_brain.py`](../mcp/server/heron_brain.py). **A real MCP SDK has now served them** (2026-08-31): all ten tools registered with their descriptions and argument schemas, and all three answering through the SDK's own dispatch with both refusals surviving the round trip — [`tests/test_mcp_serves.py`](../tests/test_mcp_serves.py). That is no longer a text read. **It is still not a host**: nothing here shows Claude Code connecting over stdio, rendering a docstring or choosing a tool from it, and that is what is left of `A8`. **Doing it found that the server would not have started at all** on a machine installing today — see the eighth session below |

### Does not exist at all

| | |
|---|---|
| ~~Any way for the host to reach the brain~~ | **BUILT 2026-08-29.** Three read-only tools on one seam. Moved to the table above, which is where an untested thing belongs |
| **Any way to RUN a fragment** | **The gap that wiring the brain up revealed rather than closed.** A fragment carries C# under `impl/`, the bridge speaks a fixed set of operations, and **none of them compiles or executes one** — [D-28](DECISIONS.md) chose Roslyn in-process and it is not built. So a request now resolves all the way to *this capability, provided by that fragment*, and then stops. Every brain tool says so on every answer, because a host that inferred otherwise would build a plan that fails at its last step |
| ~~Seven capabilities the skills ask for~~ | **WRITTEN 2026-08-29**, taking the library to 14 that day and to **32** by 2026-08-30. **All ten skills have every capability provided** — `python brain/heron_skill.py` shows no gaps. Each one compiles on all eight releases and each carries proof cases with a negative case, **and not one has met a model**: they are `DRAFT`, which is what makes them the seven newest rows of Revit-checking debt rather than seven finished things |

> Nothing here is known-broken. Several things are **untested**, which is different and more honest.

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
| You asked a **question** and the model **changed** | `python tools/check-routing.py` — the ladder-crossing list at the top. A read sentence can rank below a fragment that writes. [D-47](DECISIONS.md) |
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
   That is [D-45](DECISIONS.md): build it all out now, prove it in one concentrated pass later. A
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

```bash
python tools/check-docs.py             # links, Golden Rule / decision / question references
python tools/check-metadata.py         # the standard, registry vs code, and version agreement
python tools/check-structure.py        # layout, layering, paths, and the PowerShell ANSI trap
python tests/test_session_binding.py   # one chat one Revit, all four cases — pure Python
python tests/test_write_safety.py      # Step 6's CHAT half — distances, pinning, approval
python tests/test_failure_analysis.py  # never blind-retries, and fails closed
python tests/test_tool_registry.py     # both languages agree on what may write
python tests/test_config_and_health.py # settings agree; health means something
python tests/test_workflow.py          # stages resume, stale inputs re-run, no blind retry
python tests/test_golden.py            # which proofs still stand against the current code

# Phase 2, added 2026-08-28 / 29. All eight run anywhere, none needs Revit
python tests/test_fragment_store.py    # identity survives a rename; a proof with no negative case is refused
python tests/test_scope_store.py       # one file per scope, and a cross-scope query cannot be written
python tests/test_search.py            # exact words - identity, cache, FTS5
python tests/test_embed.py             # nearness, and the measured proof that it is NOT meaning
python tests/test_retrieve.py          # the two fused, behind a hard Revit-version filter
python tests/test_capability.py        # a provider is swapped and the call site does not change
python tests/test_graph.py             # the deriver shown catching a break before it was believed
python tests/test_skills.py            # a skill names capabilities, never fragments

# The seam, added 2026-08-29. Also needs no Revit
python tests/test_brain_reachable.py   # the host resolves through a capability, not a fragment id

# Served by a REAL SDK, added 2026-08-31. Needs `pip install --user mcp`,
# no Revit and no Windows. With no SDK it exits 3 and check-gaps reads that
# as WAITING - a skip reported as `ok` is a green nobody earned
python tests/test_mcp_serves.py        # every tool served, with its description and its arguments

# The fragments, compiled for the first time - 2026-08-29. Needs the .NET SDK,
# no Revit and no Windows. ~10 minutes for all eight releases
python tools/check-fragments-compile.py   # every fragment, every release it claims

# Did a new fragment make an OLD one unfindable? - added 2026-08-30.
# Run it after adding any fragment. It never fails a build; it reports judgements
python tools/check-routing.py             # every fragment, asked its own words back
```

> **Set `HERON_KNOWLEDGE` before anything that touches `brain/`.** On Windows the stores go under
> `%APPDATA%`; anywhere else there is no such folder and the store has nowhere to live, so the tools stop
> with a named error rather than inventing a location. `export HERON_KNOWLEDGE=~/.heron` is enough. The
> stores are **derived** — deleting them is always a safe recovery, and `python brain/heron_scope.py
> --rebuild` puts them back.

**18 suites; 16 pass here with nothing installed at all, 514 individual `ok`/`PASS` lines** — derived,
not typed: `for f in tests/test_*.py; do python3 "$f"; done | grep -cE '^\s*(ok|PASS)\b'`. The other two
each want one thing first, and **neither is a Revit**: `test_bridge_roundtrip.py` needs its host built
(below), and `test_mcp_serves.py` needs `pip install --user mcp`, which takes it to **531**.

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
python tests/test_bridge_roundtrip.py          # 30 checks, including the whole lease
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
- Every claim in [§3](#3-what-is-proven-and-what-is-only-built) marked *proven against a real Revit*.

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

Twenty-two are in [docs/DECISIONS.md](DECISIONS.md). These are the load-bearing ones:

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

**Every question is answered — 41 of 41 — and nothing blocks any phase.** What is left is **three review
item at the PC** (`R1b` — `R1` and `R2` were closed on 2026-08-29) **and two choices Phase 2 created by
finishing, both of which have since been made.** The copyright
line was the last outstanding confirmation and it was given on 2026-08-28.

| | |
|---|---|
| ~~`R1` — read the day's decisions back~~ | **DONE 2026-08-29.** All twenty-one read back, all confirmed. It happened in conversation rather than at the PC, and **after** Phase 2 was built rather than before — both recorded rather than smoothed over. What that cost turned out to be **nothing measurable**: the two that had been reversed within hours did not move a fourth time, and no missing detail surfaced. `R2` went with it — D-26 and D-32 were the two put to him individually |
| **`R1b` — show him the trust model working** | [D-14](DECISIONS.md) stays **Proposed**. He agreed the direction and said *"show me it working at the PC first."* Use the framing that landed: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may not be called settled |
| ~~Copyright~~ | **CONFIRMED 2026-08-28 — Ajmal PS is correct.** Checked consistent in all four places it appears: the Apache appendix in `LICENSE`, `NOTICE`, `<Company>` in `Directory.Build.props`, and `README.md`. The Apache appendix is filled in rather than left as the `[name of copyright owner]` placeholder, which is the one that is usually missed |

**And two choices that did not exist until Phase 2 was built.** Neither is a question the code is stuck
on — both are the owner's to make, and both were deliberately left rather than decided quietly:

| | |
|---|---|
| ~~Write the seven missing capabilities, or wait?~~ | **DECIDED AND DONE 2026-08-29 — he asked for the no-PC work to be finished.** All seven are written and compile on all eight releases. The argument that settled it: **not writing them was itself outstanding non-Revit work.** Written, what remains is their PROOF, and proof is Revit-checking — which is exactly the only thing his standing instruction wants left. **One question came out of the work and is still open**: whether a layout that Revit refuses part of should place the rest or roll back entirely (Golden Rule 16 says roll back; a modeller may well want the 37). See `place-family-instances` |
| ~~Wire the brain to the host now, or after the Revit checks?~~ | **DECIDED AND DONE 2026-08-29 — he chose to wire it now.** Three read-only tools on one seam; `check-gaps.py` reports nothing unfinished. It cost the register one new row (`A8`, two minutes at the PC) and moved no other row, so his standing instruction that *only Revit-checking should remain outstanding* holds: what remains is 47 Revit rows, 3 Windows, 1 network, 3 conversations |

**Two things were answered by NOT answering them, and both are publication tasks rather than gaps:**
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
platforms, one shim, one set of assertions, and passes 32 checks including the whole lease.

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
through the capability registry is what the clause asks for. It is in [§1](#1-where-the-project-stands-in-one-paragraph)
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
| `context/` | 12 | see [D-46](DECISIONS.md) — most are the host's job, not fragments |
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
6. **Answer every ladder-crossing.** [D-47](DECISIONS.md) removes the option of leaving one alone:
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
| **[D-45](DECISIONS.md)** | Build it all out now, prove it against Revit later in one pass. Do **not** stop to half-prove something |
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
mostly the host's, per [D-46](DECISIONS.md). The real remaining pool is about **330** source files,
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
