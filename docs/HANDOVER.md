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
> go stale. It also says what is **safe to use today** — **144 proven `READ` fragments cannot modify a
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

If a session ever tells you something that disagrees with `python tools/check-gaps.py`, **believe the
tool.** It is computed from disk every time; this file is typed by hand.

**If something looks wrong while you are working — a number that feels off, a job that says it did
something you cannot see, anything odd — go to [§4a](#4a-something-looks-wrong-mid-job--start-here).** It is
indexed by what you actually see rather than by what the cause turns out to be, and it says what to do
when the thing you hit is on no list at all.

---

## WHERE THIS STANDS RIGHT NOW — read this, then §9a or §9

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
| Open questions | **57 answered, 0 open, nothing gating any phase.** **[Q-57](OPEN-QUESTIONS.md) closed 2026-09-21 as [D-86](DECISIONS.md)**: a write may declare a question **only when answering it REQUIRES the write**. It had asked — may a fragment or skill that WRITES declare a question? Ten fragments and five skills do ([row 158](FRAGMENT-ISSUES.md)), and some are probably right, so the rule is undecided rather than broken. **The last four closed 2026-09-20** as [D-81](DECISIONS.md) to [D-84](DECISIONS.md), and the method is the finding: asked back to the owner **one at a time, in plain words, with a worked example each**. Four at once was dismissed twice, and so was the same set translated into Malayalam — **the language was not the barrier, the batch was.** `Q-51` carried text is **stamped, not scanned**. `Q-54` **changed the shape of its own question**: the cloud line is drawn by **content type, not scope** — all documents may go, models and families never — superseding D-24's framing at his explicit request. `Q-55` went **further than the question offered**: not a count and a clause number but the **prior practice by name**. `Q-56` took the cheapest honest answer — nothing more than today, **and the word sandbox goes**, which makes the rename the deliverable. Derived by `python tools/check-docs.py`, never read from a sentence |
| Tools | **36** in `tools/` and **20** MCP tools as of 2026-09-15 — `ls tools/*.py \| wc -l` and `grep -c '^@server.tool()' mcp/server/heron_mcp_server.py`. New since the last entry: `generate-contract-reference.py` (what was built, against what it promised) and `api-changes.py` (what each Revit release stopped shipping). New MCP tools include `revit_links` and `revit_phases` |
| Bindable inputs | **CLOSED 2026-09-09.** PART 6 bound what the selection and the previous fragment could give; the caller's half — a category, a name, a distance — arrives as text now and is resolved inside Revit (D-54). It was the largest unlock left: **287 of 360 fragments** declare such a need, 675 needs between them. **Widened again 2026-09-09**: an element TYPE by name, nine narrower classes (`WallType`, `Phase`, `FilterElement` and the rest), and **a point in millimetres** ([D-67](DECISIONS.md)) — which took the arrangeable library from 6 to 40. **Widened again 2026-09-14** ([D-72](DECISIONS.md)): pairs of points (a PIPE between them), `OverrideGraphicSettings`, `ForgeTypeId`, `ParameterValue`, and the word `selected` for a LIST of element ids - six more fragments arrangeable. **What is still refused is now ONE thing and it is not a missing rule: `IList<Reference>`, a FACE.** A face is picked with a mouse and no text names one, so `place-family-on-face` needs Revit's own picking rather than a parser. Derive the rest with `python tools/generate-jobs.py` |
| Branches | **`main` only** after PR #142 merged on 2026-09-15 (211 agents, the fragment compile, the full Revit API surface). **`main` only, and it is the only branch that exists.** **Sixteen PRs were merged on 2026-09-09** (#44–#61) and every branch behind them is deleted — the role-declaration stack, the silence-illegal fixes, the job generator, and the proving track. **Start from `main`**; nothing is parked outside it. The sha is not written here - `git log --oneline -1 origin/main` - because it moved twice while this row was being read |

### 2026-09-20 — A FORMATTER THAT WAS ONLY COMPILED, AND A GATE THAT SAID "EVERY PROJECT" WHILE NAMING FIVE OF SIX

**PR #212, draft.** Two things, and the second was found by doing the first.

**A review finding was right and is fixed.** `RevitFragment.Size` had become the only thing separating
`(2 of 1128)` from the misleading `(2)` that [row 75](FRAGMENT-ISSUES.md) is about, and it shipped in
#210 with **no test**. A compiler cannot catch a regression in its wording or in the ORDER of its two
`object` arguments. The rule now lives in `revit/Heron.Revit.Addin/HeronBindingNote.cs`, which touches
no Autodesk type, so `tests/Heron.BindingNote.TestHost` links it **BY SOURCE** — one copy of the rule,
[row 96](FRAGMENT-ISSUES.md)'s lesson. **It was measured against the previous implementation**: built
against `Size` as it stood before the fix, **3 of 7 checks fail and the host exits 1**, and the four
that still pass are exactly the ones that must not move.

**THE LESSON WORTH CARRYING IS THE SECOND ONE, AND IT IS ABOUT BEING WRONG.**
[Row 161](FRAGMENT-ISSUES.md): `brain/heron_dotnet.py` listed the projects the compile gate builds
under the comment *"Every project, in dependency order"* while naming **five of six**, and
`Heron.Banner.TestHost` had never been compiled on any release. **The first reading of that was that
somebody forgot, and it was written down that way before being checked.** It was wrong. `5669e7e` left
it out **on purpose** — a `WinExe` WPF project, CI on Linux with `EnableWindowsTargeting`, never
watched go green — and said both where it belonged and what would unblock it. **The reason lived only
in the commit message**, so a held-back project and a forgotten one looked identical.

What caught it was reading the commit that added the file **before** writing the row. That is the
queue rule *"VERIFY BEFORE YOU BELIEVE"* earning its place again, and the row keeps the wrong first
reading in writing because the shape of the mistake is the point.

The condition `5669e7e` set is now met and Banner is listed: `tools/check-compile.py` on **Linux**,
the **10.0.x** SDK, `-p:EnableWindowsTargeting=true` — the same command, OS, SDK line and flag
`gates.yml` itself uses — builds **every project in `PROJECTS` on every release in `RELEASES`**, and
CI has since done the same. No totals are written here on purpose: the gate prints its own, and both
lists are meant to grow. `heron_dotnet.unlisted()` now **fails the gate** on any `.csproj` that
neither `PROJECTS` nor the named `NOT_SHIPPED` accounts for, so the next exclusion has to be readable
where the gate is.

**LEFT FOR THE NEXT SESSION — [row 162](FRAGMENT-ISSUES.md), recorded and not fixed.** **One suite
more sits in `tests/` than `check-gaps.py` ever runs**, and no total is written here on purpose —
`ls tests/test_*.py | wc -l` against the count the sweep prints is the check, and it is off by one
until this is done. `test_bridge_roundtrip.py` is the one, skipped by name, and unlike row 161 **its
reason is in the right place** — in a comment beside the skip, and correct: with no host binary the
suite `return 1`, which would read as a FAIL for a build step nobody took. The defect is that the
suite has **no exit-3 path**, so it cannot say *"I could not run"*, and a silenced suite prints
nothing at all — not `ok`, not `wait`. The repair is the one `tests/test_binding_note.py` already
uses next door, and a Codex review on PR #212 made the same point about **this paragraph**: a typed
total goes stale the moment a suite is added, while *"exactly one is skipped, and silently"* stays
true and is what the next session actually needs.

**Nothing here needed a Revit, and nothing here is evidence about one.** Compiling is the API surface
agreeing. `(2 of 1128)` has still never been seen in a real binding note, row 75 is still **OPEN**, and
D3 in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is still the line that catches a unit error.

### 2026-09-19 — TWENTY-THREE FRAGMENTS BUILT FOR RECORDED GAPS, AND FOUR API FACTS THAT CHANGED THE ANSWER

**Merged as PR #184.** The owner read out five lists of Revit jobs - roughly 290 of them - and asked
which Heron could do. Twenty-three came back with no route at all, were recorded, and
were then built. **The note that recorded them was deleted on 2026-09-19 once it had emptied** — that
none of the twenty-three has met a model is [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) **A19**, and what
is still worth building is in [`PROPOSALS.md`](PROPOSALS.md). **372 → 395 fragments. All twenty-three are `DRAFT` and none has met a model.**

**THE COMPILER WAS INSTALLED FIRST AND IT PAID FOR ITSELF IN THE FIRST HOUR.** The section below says
the compile gates are five minutes away; they are, and writing twenty-three fragments without them
would have been guesswork. It caught three defects before any reached the repository and settled four
facts the gap note had wrong or unverified:

| | |
|---|---|
| **Phase creation** | Recorded as *probably impossible, UNVERIFIED*. **Confirmed impossible.** `Phase` carries no members of any kind - no method, no static, nothing - on any release 2020 to 2027. It belongs with category reassignment as a permanent API limit, not on an unbuilt list |
| **The dimension gaps** | Recorded as four missing fragments. **Wrong KIND of gap.** Every `New...Dimension` call whose name suggests it belongs to the FAMILY EDITOR's creation object and cannot be reached from a project at all - which cost a compile failure on the first write of `create-linear-dimension`. The project routes are four different stories: linear through the base item factory and angular as a static, both from 2020; radial and arc-length only from **2025**; diameter through the radial call's `isDiameter` flag, the only route to one on any release. `check-fragments-compile.py` enforces it - 394 fragments claim 2024, 395 claim 2025 |
| **`propose-mep-openings`** | Says in its own purpose that cutting is *"a separate write that somebody approved"*. **That write did not exist anywhere.** `create-opening` is it |
| **Save and sync** | **Nothing in Heron could do either.** `Document.Save` and `SynchronizeWithCentral` appeared nowhere in the fragments OR the add-in. Heron could read ownership on a workshared model, modify it, and leave every change in a local file waiting for a hand on the keyboard. `save-document` and `sync-with-central` are `PUBLISH` risk, which Phase 0/1 refuse unconditionally - written and deliberately unreachable, which is the right order |

**TWO DEFECTS WERE FIXED BY ADDING A ROUTE RATHER THAN EDITING A PROVEN FRAGMENT**, and that is the
pattern worth repeating. `place-family-instances` hardcodes `StructuralType.NonStructural`, correct for
the air terminals and sprinklers its purpose names; the defect was that no OTHER route existed, so
every structural column and footing went in non-structural. `place-structural-family` is that route and
the proven fragment is untouched, so it needs no re-proof. Same shape for `select-by-level`, which
counts unlevelled elements as an `int` and discards them - `select-without-level` returns the set.

**WHAT THE GATES AND SUITES CAUGHT, IN THE ORDER THEY CAUGHT IT:**

- `check-structure` refused a **comment** naming the vendor namespace - the fixture-and-comment case
  [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) §1 warns about, firing exactly as documented.
- The fragment validator refused `checked` as a provided name **before a compiler could**: it is a C#
  keyword and the generated wrapper would not have built.
- `test_supply.py` refused `source: PROPOSED` on three fragments - **a word that was invented**. The
  supply trust ladder is UNKNOWN, EXPERIMENTAL, TESTED, VERIFIED, PROVEN, OFFICIAL, and `source` says
  where a fragment CAME FROM, not how mature or dangerous it is. **The four gates were green through
  three commits with that wrong value in place; only the suites caught it.** Run both.
- `check-docs` caught the derived count sentences going stale twice in one session, which is the rule
  working rather than a nuisance.

**ALL 199 SUITES PASS HERE**, which took installing rather than excusing - see the corrected row in the
table below. `check-gaps` exits 0 with **UNFINISHED empty**.

**WHAT IS LEFT, AND IT CANNOT BE DONE IN A CONTAINER.** Twenty-three fragments are unproven and say so.
A compile is not a proof: D-30 needs a named model, a positive case, a negative case and a fingerprint.
Each of the twenty-three already carries its two cases in `tests/cases.yaml`, several with a trap
written in that only a real model will settle - `move-annotation` on a dimension, `measure-perimeter`
against Revit's own room schedule, `set-datum-extent-type`'s claim that a 3D extent reaches every view.
See [NEEDS-CHECKING](NEEDS-CHECKING.md) **A19**.

**NOT filed as FRAGMENT-ISSUES rows, deliberately.** That register's own header says every row is a
fragment *"PUT IN FRONT OF A REAL MODEL"* that did not come away proved. These twenty-three have never
been run, so a row there would misreport what is known about them - they are unproven, which is not the
same as broken.

### THE COMPILER IS FIVE MINUTES AWAY, AND THAT CHANGES WHICH JOBS ARE "PC ONLY"

**Done on 2026-09-16 in a plain Linux container**, and worth doing again first thing in any session
that means to touch C#:

```bash
apt-get update && apt-get install -y dotnet-sdk-10.0
```

**`apt-get update` is the whole trick.** The install alone failed with ten `404 Not Found` fetches —
the image's index named `10.0.104` and the pool had moved to `10.0.112`. Those 404s read exactly like
a blocked CDN and are not: with the refresh it installed first time. [§30](30-compiling-away-from-windows.md)
now carries this.

What it unlocked, measured the same hour:

| | |
|---|---|
| `tools/check-compile.py` | **all four projects × 2020–2027**, clean |
| `tools/check-fragments-compile.py` | **395 fragments**, every release each one claims. 394 claim 2024 and 395 claim 2025 - `create-radial-dimension` is 2025+ only, and the gate enforces that rather than trusting the contract |
| `tests/test_dotnet.py` | was red here, **now green** |
| `tests/test_bridge_roundtrip.py` | was red here, **now green** after one `dotnet build` of the test host |
| `tests/test_mcp_serves.py`, `test_served_claims.py` | **THIS ROW SAID "still out, and leave them" AND THAT IS NO LONGER TRUE.** Measured 2026-09-19 in a plain container: `pip install --user mcp` followed by `pip install --user --upgrade cryptography cffi` clears both, exit 0. The earlier attempt backed out after installing `mcp` alone, which does panic on import - the missing half was the `cryptography` and `cffi` upgrade, which `.claude/skills/heron-ship/SKILL.md` has carried in sections 2 and 4 the whole time. **The excuse outlived the fix by days because nobody re-read the skill.** `gates.yml` still leaves the MCP SDK out on purpose and that is unchanged - this is about your container, not CI |

**The container is ephemeral, so this is a step and not a state.** The next session starts without it.

**What it does NOT unlock:** behaviour, of any kind. A compiler agrees about the API surface and says
nothing about whether a duct moves 200 millimetres or 200 feet — D3 in NEEDS-CHECKING, and D-30.

**QUEUED FOR THE PC, AND THE OWNER HAS SEEN THE LIST.** **Five items**, all of them needing Revit
or `dotnet`, all of them CHECKED by a gate that runs without either: **62 link contracts**
([D-59](DECISIONS.md)), **59 dropped-counts** ([D-64](DECISIONS.md)), the **preview selection** in
`RevitWrite.cs` ([D-60](DECISIONS.md)), the **workflow id across the seam**
([D-61](DECISIONS.md)/[D-62](DECISIONS.md)) which is small and unlocks three things at once, and
the **project knowledge scope**, which the Codex review added. The lists and the order are in
[the open-questions entry](handover-archive/2026-09-09-the-open-questions-track-44-52-answered-and-the-rest-needs-t.md).

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

### A SECOND CODEX REVIEW — 16 more findings, ONE fixed, FIFTEEN NOT LOOKED AT. Start here.

A second review ran on 2026-09-15 against the whole 100-commit PR and returned **16 findings, 11 of
them P1**. Codex then **hit its usage limit**, so no third review is coming and these will not be
re-reported by anything.

**One was fixed** because three agents depended on it. **Fifteen were not looked at.** They were
recorded and the PR merged, because the alternative was a PR that never closes while the same class of
finding keeps arriving — but that was a decision about scheduling, not a judgement that they are wrong.

**Round one was nine findings and every single one was real**, two worse than reported. Assume the same
here. **Reproduce each before changing anything** — and note that reproducing `heron_revision.revise()`
WRITES to `brain/skills/` when `into` is None, which is how a stray `count-things.yaml` appeared during
round one.

#### Fixed

**`brain/heron_security.py` — the fingerprint collided.** `_stable` str()'d every scalar and joined
collections with commas, so nothing recorded a value's TYPE and nothing escaped a comma inside one:

    {"apply": True}  ==  {"apply": "True"}
    {"x": ["a,b"]}   ==  {"x": ["a", "b"]}
    {"a": 1}         ==  {"a": "1"}

Every collision is a `MODIFY` change that could be altered after review without going stale. By the
time it was found, **`HERON-DEV-QA-016` and `HERON-FRG-UPD-006` had both bound this function**, so all
three shared the hole. Now canonical JSON with sorted keys.

#### ALL FIFTEEN LOOKED AT, REPRODUCED AND FIXED — 2026-09-16

**Every one was reproduced before anything was changed, and every one was real** — the same
result as round one, where nine of nine held. The table below is kept as the reviewer wrote it,
with what was done to each. Each fix carries a regression check in its own suite, under a
heading that names the review, so a later reader finds the argument and not just the assertion.

**Two of the fifteen were the SAME BUG IN A SECOND FILE**, exactly as the note below predicted,
and both were fixed at the root rather than at the line reported:

- `heron_contribution` repeated round one's string-attachment bug. `_binaries` already
  normalised a bare string — but `heron_contribution` and `heron_import` both concatenated
  `[name] + list(attachments)` **first**, splitting `"Tower.rvt"` into eleven characters before
  the normalisation ever got its turn. `heron_release._names()` is the one place that now
  answers "these names, as a list", and both callers use it.
- `heron_workspace` repeated the Windows-separator bug. `_normal()` is the one place that
  makes a path comparable, and the three comparisons in that file all go through it.

**Three fixes were shaped by a constraint the module already had**, found by its own suite
rather than reasoned about:

- `heron_render` may import only `hashlib`, `os` and `sys`, so the cell escaping is hand-rolled
  rather than a regex.
- `heron_prebuild` may not call `open(` — it reads nothing. Which standards owners exist is
  answered with `importlib.util.find_spec`, so the *mapping* is written down and whether a
  module *exists* is looked up.
- `heron_userskills` and `heron_qa` each had a test asserting the reported behaviour was
  deliberate. One was wrong and was changed (returning every author's private skills is the
  widest possible guess, not a neutral one); the other is genuinely a sequencing decision and
  is **half-closed on purpose** — see `heron_qa` below and PROPOSALS F35.

| file:line | the claim, in the reviewer's words |
|---|---|

| `brain/heron_qa.py:174` | **P1.** A result omitting `of` skips the freshness check, so an old or fabricated `{check, passed: true}` satisfies the final gate for an unrelated change. **Note:** this is the concession that agent DOCUMENTS deliberately — most checks here record nothing to compare against. The reviewer is right that it is a hole; closing it needs the checks to start recording fingerprints first, so it is a sequencing decision, not a one-line fix → **HALF-CLOSED, ON PURPOSE.** The MIXTURE is now refused — `RESULT_IS_UNFINGERPRINTED`: once one check in a run recorded a fingerprint, a result without one is a check that did not record rather than one that cannot, and that is where an old or fabricated result would sit. The all-unfingerprinted case still passes, because the note is right that closing it needs the checks to record first — but it is no longer only prose: `unfingerprinted` names every such check in the answer, so a caller can refuse on it. **The remaining half is PROPOSALS F35.** |
| `brain/heron_contribution.py:139` | **P1.** `attachments: "Tower.rvt"` is concatenated with the item name and iterated as characters, so no `.rvt` is found and `submit()` returns `may_submit: True` for a contribution carrying a model. **This is the same bug as the `heron_release` one fixed in round one, in a second file** — normalise before concatenating → **FIXED at the root.** `heron_release._names()` normalises a bare string, and `heron_contribution` and `heron_import` both call it instead of `list()`. |
| `brain/heron_knowledge.py:160` | **P1.** The literal scope name `project` is compared against the active project key, so a claim from the CURRENT project is always held as `ANOTHER_PROJECTS_KNOWLEDGE` unless the project is itself called "project" → **FIXED.** `scope: project` is the ladder RUNG and names no project; which project a claim belongs to is its own `project` field. A rung-scoped claim naming none is held under `NO_PROJECT_NAMED` — it cannot be SHOWN to be this one — rather than reported as belonging to a project called 'project'. The wall holds in both directions; seven arrangements are checked. |
| `brain/heron_userskills.py:147` | **P1.** With no `reader`, `who` is empty and nothing refuses, so `mine` returns **every supplied user's private skills** → **FIXED, and a test that asserted the old behaviour was changed.** An unnamed reader is `NOBODY_IS_ASKING`, not a matching one. The suite said *"guessing would be the wrong answer either way"* — returning every author's private skills is the widest possible guess, not a neutral one. |
| `brain/heron_pullrequest.py:207` | **P1.** A confirmation is validated against the mutable branch name, so the same `{by, at, head}` authorises a second call after new commits, a changed base, or a replaced title → **FIXED.** The confirmation carries `of`, HERON-DEV-SEC-009's fingerprint of title, body, head, base, draft and attachments. A changed title, base, body or attachment is `CONFIRMATION_IS_STALE`. This is round one's `heron_apply` fix — *an approval that does not name what it approved covers everything* — applied to the second place a name stood in for the content. |
| `brain/heron_workspace.py:140` | **P1.** Only `/` is compared, so on Windows `Brain\Skills` and `Brain\Skills\x.yaml` read as unrelated and two modifying agents are approved together. **Windows is where Revit runs** — same shape as the `heron_authoring` separator bug fixed in round one → **FIXED at the root.** `_normal()` reads both separators, and all three comparisons in the file use it. Mixed spellings of one path match each other. |
| `brain/heron_migration.py:149` | **P1.** Two migrations with the same `from` version: the second silently replaces the first, and the chain still reports complete → **FIXED.** Two migrations out of one version is `CHAIN_FORKS`, refused whole. Taking whichever arrived last ran one transformation, skipped the other, and reported the chain complete. |
| `brain/heron_repair.py:128` | **P1.** Two findings mapping to the same absent destination both pass `taken()`, so the plan overwrites a file despite the no-overwrite guarantee → **FIXED.** Destinations claimed by this same plan are tracked. `exists` answers about the disk as it stands and cannot know about a move this run already proposed, so the no-overwrite guarantee held against the disk and not against the plan's own second half. |
| `brain/heron_report.py:140` | **P1.** A titled report re-renders without its title, so `validate()` reports `CONTENT_DOES_NOT_MATCH_THE_DATA` for **every** legitimately titled report → **FIXED, and the root was in `heron_render`.** `render()` never returned the title, so a report could not carry its heading forward and `validate()` re-rendered untitled. Both were fixed; an edited report is still caught. |
| `brain/heron_index.py:160` | **P1.** Any non-empty `by` satisfies the acceptance gate — `ci`, `bot`, an agent id — despite the function requiring a human. The promotion and contribution gates already have the person check to bind → **FIXED.** `PRO._person` is bound, so `ci`, `bot`, `pipeline` and `script` are `ACCEPTED_BY_A_MACHINE`. The promotion and contribution gates already had the test; this is the same one, not a second copy. |
| `brain/heron_regression.py:156` | **P1.** The missing-result check ranges only over releases claimed BEFORE the change, so a fragment can add Revit 2026, supply only old results, and still return `safe: True` → **FIXED.** The missing-result check ranges over the union of before and after, so a release this change ADDS needs a result too. A new release that fails is `A_NEW_RELEASE_FAILED`, named apart from a regression — nothing regressed, because there was no previous behaviour there. |
| `brain/heron_compatibility.py:135` | **P1.** A declared release plus a runtime absent from the build matrix returns the declared releases as supported — `declared=['2025'], runtime='net6.0'` gives `revit: ['2025']` with no disagreement, while the same answer says no release uses that runtime → **FIXED.** A declaration against a runtime nobody here builds for supports no release KNOWN, and the conflict is reported as a disagreement. Both cannot be true at once, and `revit: ['2025']` beside an `unbuilt` note said they were. |
| `brain/heron_duplicates.py:156` | **P2.** A capability match alone classifies an import as `already`, though `heron_capability.resolve()` deliberately keeps several providers and picks by release → **FIXED.** A capability match is decisive only on a SHARED release, because `resolve()` keeps several providers and picks among them by release. No overlap goes to a new `complements` list. With neither side declaring releases an overlap cannot be ruled out, so it still reads as decisive. |
| `brain/heron_render.py:172` | **P2.** A cell containing `|` or a newline corrupts the Markdown table, and PDF/image conversion preserves the corruption → **FIXED.** A pipe is escaped and a line break becomes `<br>` (a space in the fixed-width schedule), so one value can no longer add a column or a row. Every touched cell is named in `escaped` — a value changed to fit the format is not the value. `csv` already quoted correctly and is untouched. |
| `brain/heron_prebuild.py:246` | **P2.** It hard-codes that the standards question is unanswered because no owner is built — **but `STD-PRJ-009`, `STD-CMP-002` and `STD-ISO-003` were built in this PR.** A stale hard-coded answer routing the host away from working functionality → **FIXED.** Which owners are built is looked up, not asserted. The answer now says the question is unanswered because **this agent asks none of them** — a different statement from there being none to ask — and names the three that are built. |

**Two of these are round-one bugs in a second file** (`heron_contribution` repeats the string-attachment
bug, `heron_workspace` repeats the Windows-separator bug). When one of these is fixed, **grep for the
same shape everywhere else** rather than fixing the one line reported.

The full review stays readable on the PR: <https://github.com/Ajmalpshaik/Heron-AI/pull/142>.

### THE CODEX REVIEW ON PR #142 — all nine reproduced and fixed

A Codex bot review landed on PR #142 on 2026-09-15 with **nine findings, five P1**. **Every one was
reproduced first and then fixed** before the merge — none was taken on trust, and none turned out to be
wrong. Two were worse than the review said.

Kept here because the *shapes* recur, and because every one of them passed 188 green suites.

#### The five P1s

| file | what was wrong |
|---|---|
| `mcp/server/heron_mcp_server.py` | **`revit_phases` was defined AFTER `if __name__ == "__main__"`.** `server.run()` never returns, so the decorator never ran and the tool was absent from `tools/list` while the registry still declared it. Nothing failed and nothing logged — the tool was just not there. `tests/test_api_docs.py` now checks every tool is defined above that block |
| `tools/api-changes.py` + `brain/heron_apichanges.py` | **The documented `api-changes.py 2025 2026` overwrote the committed digest with one transition**, and the agent read a missing transition as *"that release removed nothing"* and reported every fragment **clear**. That is D-52's plausible zero inside the agent built to prevent it. Targeted runs now refresh rather than replace, and `removed_in` returns `None` (not `[]`) for a release nobody looked at, which refuses as `NO_EVIDENCE_FOR_RELEASE` |
| `brain/heron_release.py` | **`attachments: "Tower.rvt"` iterated eleven characters**, so `_binaries` found no `.rvt` and the guard that exists to stop a Revit model leaving the office **failed open** |
| `brain/heron_modelqa.py` | **`value or ""` counted a zero offset and an unchecked Yes/No as EMPTY.** Those are among the commonest real values a Revit parameter holds, so every fill rate it ever produced was understated |

| file | what was wrong, and what it now does |
|---|---|
| `brain/heron_apply.py` | **A `PRODUCTION` approval was not bound to the code it approved.** It checked the fragment id and the verdict only, so the SAME approval accepted the implementation a person read **and any other implementation put in afterwards** — returning `applied: True`. Reproduced exactly that way. `HERON-DEV-SEC-009`'s `fingerprint` is now bound by identity: an approval for a verdict that moves code must carry `of`, and code swapped afterwards is `STALE_APPROVAL`. This is the file's own argument — *"an approval that does not name both covers everything"* — applied to the third thing an approval is about |
| `brain/heron_revision.py` | **Only the card coming IN was checked, never the one going OUT.** `{"purpose": ""}` and `{"id": ""}` were accepted on a `PRODUCTION` skill and written, and neither field is part of `breaks()`, so the production guard had no reason to stop it — replacing valid YAML with a card `HERON-SKL-VAL-004` rejects, which takes the skill out of use. Now `REVISION_INCOMPLETE`, deliberately a separate name from `INCOMPLETE`: *"the card you gave me is broken"* and *"the change you asked for would break it"* send the reader to different files |
| `brain/heron_memory.py` | **A project-scoped memory was accepted with no project**, carrying `expiry.project: null`. Nothing could archive it when that project closed and nothing keyed it to the job it came from — a riser size agreed on one tower would apply to the next. Golden Rule 5. Now `NO_PROJECT`, on the same argument as the `NO_TTL` refusal three lines above it: a project memory with no project is global memory under another name |

#### The four P2s

| file | what was wrong, and what it now does |
|---|---|
| `brain/heron_modelqa.py` | **`value or ""` counted a zero offset and an unchecked Yes/No as EMPTY.** Truthiness, on two of the commonest real values a Revit parameter holds — so every fill rate it ever produced understated the model |
| `brain/heron_authoring.py` | **A skill id escaped its directory.** `id: "../agents/ESCAPED"` joined cleanly and wrote an agent-shaped YAML beside the library; an absolute id landed anywhere. These drafts are **model-generated**, which is exactly the input a path must be checked against. Now `ID_IS_NOT_A_NAME`, with `HERON-KRN-SEC-012`'s `_inside` bound as the backstop. **Both separators are rejected**: `..\windows` is one filename on Linux and an escape on Windows, and Windows is where Revit runs |
| `brain/heron_iso.py` | **A title CONTAINING the standard's name was treated as the standard itself** — so *"Acme guide to ISO 19650 compliance"* and *"Deviations from ISO 19650"*, the two commonest titles a company document about a standard actually has, were presented as the standard speaking. That contradicts the function's own docstring one level up. It must now BEGIN with the designation, allowing an adoption prefix recognised **mechanically, not from a list**: `BS EN ISO 19650-2:2018` is upper case before the designation and is the standard; *"Acme guide to"* is not |
| `mcp/server/heron_mcp_server.py` + `tools/api-changes.py` | The two in the table above |

**Two were worse than the review said.** The `api-changes` one: the agent read a missing transition as *"that release removed nothing"* and reported every fragment **clear** — D-52's plausible zero inside the agent built to prevent it. The `heron_authoring` one: the reviewer named `..` and absolute paths; a Windows separator and a bare `..` also got through.

**None of this was caught by 188 green suites.** Each fix carries a regression check now. The review stays readable on the PR: <https://github.com/Ajmalpshaik/Heron-AI/pull/142>.

### What 2026-09-14/15 did — 196 agents to 211, and four things that were believed and are not true

**Nineteen agents were built.** Import & Migration finished (14 of 14), Standards & BIM QA reached 9 of
14, and Development reached 11 of 21. The four that matter most to a person using Heron:

- **`REVIT-LNK-015` linked models** and **`REVIT-PHS-032` phases and design options.** Both answer the
  same class of question: *"412 ducts"* is not a fact about a model. Elements inside a link are not in
  the host document at all, and a count is a fact about a model AND a phase AND a design option. Both
  read-only, both compile 2020–2027, **neither has ever run** — NEEDS-CHECKING group L.
- **`REVIT-ACI-034` API change intelligence.** `python tools/api-changes.py` reads the full public
  surface of all eight releases and says what each one removed. It earned its place immediately: asked
  which members exist before `RevitPhases.cs` was written, it stopped two lines that would not have
  compiled — there is no `DesignOptionSet` class, and `Phase.Name` is declared on `Element`.
- **`DEV-DOC-017` the contract reference.** `python tools/generate-contract-reference.py` checks every
  contract against the code that claims it. Three refusals were produced and undeclared; all three are
  fixed.

**Four things this repository believed that were not true:**

1. **The three "Linux failures" were never Linux failures.** They needed `pip install --user mcp` and
   one `dotnet build`. The suite reads 188 of 188 now.
2. **The fragments had never been through a compiler.** They have now: 360 × 8, clean.
3. **`ElementId.IntegerValue` is GONE in 2026 and 2027** — not deprecated. It compiled on 2020–2025 and
   a comment here called it *"the property every version has had"*. That is [D-05](DECISIONS.md), and it
   is why the compile runs all eight rather than one.
4. **CI requires seven checks, not four.** The ship skill's "four that must pass" is the fast subset;
   `.github/workflows/gates.yml` also runs `check-routing`, `check-intrusion` and `check-compile`.

**Three mistakes made here, kept because the shape of each one generalises:**

- **CI was broken for three commits and CI caught it, not the author.** Moving shared code into
  `brain/heron_dotnet.py` hoisted an import reaching PyYAML; the compile job installs a .NET SDK and
  nothing else. **Local runs were clean throughout.** Fixed with a lazy import and a test that runs the
  module in a subprocess with `yaml` blocked on purpose.
- **A security check reported five credentials in a change carrying one.** It compared
  `redact(text) != text` — and `redact` returns a **tuple**, so the compare was never equal.
  **It fails in the direction that looks like vigilance** (PROPOSALS F32).
- **A checker reported 135 undeclared refusals, then 12 fragments broken at Revit 2023 — all wrong.**
  Every correction was a case the code had already solved: a capability name is not a refusal, a comment
  is not a call, a string is not a call, a dead `#if` branch is not a call, a suite is not the agent.
  **A page of findings that are all wrong is worse than no page** — it teaches the reader to skip the
  table, which is where the real ones are (PROPOSALS F33).

**The rule that came out of all three: verify a finding against the source before believing your own
tool.** Every one of those checkers now re-checks its findings on every run.

**What is waiting on the owner grew from F24 to F34.** Ten new entries in
[PROPOSALS.md](PROPOSALS.md), none acted on. The ones that block building:
**F23** (four Documentation rows with no distinct source), **F27** (five Development rows already built
by files claiming no agent, and the two rows already claimed in that department disagree with each
other), **F31** (five Standards rows differing only by subject), **F15/F17** (two Naming rows).
**One sentence closes F27 and F31 together:** does a Development agent act on the artefact Heron builds,
or on Heron itself?

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

---

# 11. Handover — 2026-09-12

**Written to a rule the owner set on this date, and the rule is the first thing to carry forward:**

> **Do not keep a diary of the work.** Not the daily activity, and **not the issues that were fixed** —
> the code and the commit already say those. **Keep only three things:** something NEW that was made, a
> MISTAKE worth not repeating, and anything still **TO DO**.
>
> His reason, and he is right: *"if we keep everything by note that will be big."* This file is 440 KB
> and proves the point. **Sections 1 to 10 above are the old habit. This section is the new one.**

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
| **`--session <pid>`** | `fragment`, `prove` and `validate` no longer pick a Revit by lowest PID. With two live and none named they **refuse**. A proof had come back `positive ok` against the wrong model ([row 95](FRAGMENT-ISSUES.md)) |

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
| **W-7** *(`S-4` is ANSWERED — [`00-structure.md` §8](work-notes/plans/rag/00-structure.md): write the parser, let one real PDF decide)* | **owner** | **one real numbered document.** Every test document so far was written to be easy. This single item unblocks the most — it settles whether the chunker survives a real spec, and replaces the invented clause number W-7 has carried since the start |
| ~~**Q-C**~~ | ~~owner~~ | ✅ **ANSWERED 2026-09-12 — yes, a counter.** [D-70](DECISIONS.md). Stage 8's trust half is **unblocked and still unbuilt** — the decision names the shape (clause id and a count, per scope, deletable, never sent) and deliberately sets **no weights**. **R-16 and R-25 are now the top buildable job.** One thing stays open inside it: whether the question TEXT is stored, which he was not asked and D-70 does not assume |
| **[Q-55](OPEN-QUESTIONS.md)** *(was `Q-E`)* | owner | may an INGEST read another scope? Contractual under [D-33](DECISIONS.md) — at read time the crossing is already authorised, at write time nothing has been asked of anybody |
| **R-75 · R-76** | owner | the sizes and the package list are one command away rather than on the README page. Marked PART on his wording; one line each to change if he wants them on the page |
| **A10** | a machine with `huggingface.co` | Stage 7's after-measurement. Nothing else stands between R-41 and DONE |
| **A11** | Revit | the project key across a save, a rename and a move |
| **[F7](PROPOSALS.md)** *(was `W-10`)* | small job | `sqlite_vec` is the only optional package whose absence is **invisible** — Heron gets slower and says nothing, while the encoder, the re-ranker and the PDF reader all announce their fallback |
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
| **1** | **Build the usage counter** — R-16 and R-25, to the shape in [D-70](DECISIONS.md) | It is the only item on the list that is both **unblocked and code**. Everything else needs the owner, Revit, Windows, or a network this container has not got |
| **2** | **Ask the question the counter left open** | D-70 covers a count against a clause id. **Whether the question TEXT is stored was never put to him**, and an implementer will otherwise decide it by accident |
| **3** | **Close W-10** — `sqlite_vec` degrades with nothing said | One small, self-contained batch. It is the only optional package whose absence is invisible, and "degrade silently but say so" is the rule it half-keeps |
| ~~**4**~~ | ~~**Apply his own note rule backwards**~~ | ✅ **DONE 2026-09-12.** `03-working-note.md` was 158 KB of diary and is **gone**. The durable half moved first: the measurement to [`retrieval-history.md`](../brain/retrieval-history.md), `W-8` to [NEEDS-CHECKING](NEEDS-CHECKING.md) `A15`, `W-10` to [PROPOSALS](PROPOSALS.md) `F7`, and **`Q-D` and `Q-E` to [OPEN-QUESTIONS](OPEN-QUESTIONS.md) as `Q-54` and `Q-55`** — which moved that file's count from **1 open to 3**. Two questions had been owed since 2026-09-10 while the register said one was. **That is the argument against a question ever living in a deletable file** |
| **5** | **Agree a convention for `HANDOVER.md`** | **Two sessions wrote handover sections into it within an hour today**, and only merge order stopped a conflict. A dated section per sitting, appended, never edited above — or one file per sitting — would remove the race |
| **6** | **Re-read §1–10 of this file against the code** | They are from a different phase and nothing has checked them since. **This is a guess, not a finding** — I have not read them closely enough to say they are wrong, only that nothing says they are right |

**The one I would actually start with is number 1**, and only because the owner's answer arrived. If one
real numbered document turns up first, **that beats all six** — it is the only thing that tests whether
any of this survives contact with a real specification.

---

# 2026-09-14/15 — the machinery was the backlog

> **Appended, not edited above.** This is [suggestion 5](#what-i-would-do-next--suggestions-not-decisions)
> being taken: a dated section per sitting. **Where a sentence above this line disagrees with one below
> it, the one below is later.** Nothing above has been deleted.
>
> Its working note was deleted on 2026-09-19, its durable half having landed first.
> Durable detail: [`FRAGMENT-ISSUES.md`](FRAGMENT-ISSUES.md) rows **89–94** and
> [`DECISIONS.md`](DECISIONS.md) **D-72**.

## Where the library stands

**Derive it. Do not read it here.**

```bash
grep -h "^heron-status:" brain/fragments/*/fragment.yaml | sort | uniq -c
```

At close: **298 `PROVEN` / 62 `DRAFT`**, 360 total — up from 291. Also derivable: **131 `READ` proved,
153 `MODIFY` proved.**

Two things the count alone will not tell you:

- **Nothing is left signed-but-unpromoted.** `python tools/check-signatures.py` exits 0 on that half.
  Worth knowing anyway: `accept` writes the proof and **deliberately does not promote** — promotion is
  a separate hand edit, and that gap once left thirteen of his signatures sitting at DRAFT.
- **One signature is STALE** — `set-wall-constraints`, signed 2026-09-13 with its code changed
  underneath afterwards. That is D-30 working, not a fault. **Re-proving it is the owner's call.**

## What was proved, and the thing worth taking from it

Seven fragments. **Not one was blocked by anything in a model.** Every one was blocked by the
machinery, and each block was a different shape:

| | what was actually in the way |
|---|---|
| `set-sheet-title-block` | a counter that counted the CALL, not the change ([row 89](FRAGMENT-ISSUES.md)) |
| `create-line` | `IList<IList<XYZ>>` could not be typed at all |
| `filter-elements-by-id` | `selected` reached `OneElement` and never reached the id-list branch |
| `override-graphics-in-view`, `set-category-graphics`, `set-link-graphics` | `OverrideGraphicSettings` could not be typed |
| `apply-view-filter` | a yes/no result counted as zero, so **no arrangement could ever have passed** ([row 93](FRAGMENT-ISSUES.md)) |

**The 5-of-5 batch is the only one this project has had**, and the reason is the point: those five were
arranged *after* the blockers came out. The four rounds before it returned 1, 2, 1 and 2 from 23 jobs,
and every failure was a blocker already written down. **Read `python tools/generate-jobs.py` before
building a batch** — it names the blocker per fragment.

## Two findings that change how to work

**A fragment's `.cs` is LIVE. The add-in's is not.** `heron_bridge_client.py` reads
`brain/fragments/<name>/impl/any/fragment.cs` off disk and sends it on **every call**, so a fragment
can be fixed, re-run and re-proved **with Revit open**. Only `revit/Heron.Revit.Addin/*.cs` costs a
close-build-deploy-reopen. Two defects were found, fixed and re-proved on 2026-09-14 without closing
Revit once. **Do not ask the owner to close Revit for a fragment fix** — batch the add-in changes and
spend one close on all of them.

**The reply truncates, and it hid a bug for four days.** `RevitFragment.Describe` renders a list item
short, so a finding that opens with context arrives with its last sentence cut off. **When a refusal is
unexplained, put the API's own message FIRST and run it again.**

## Superseded above this line

- **"What is still refused, most-wanted first: `ElementId`, `Element` as a specific instance,
  `OverrideGraphicSettings`"** — four types were built on 2026-09-14 ([D-72](DECISIONS.md)):
  `IList<IList<XYZ>>` (a PIPE between pairs), `OverrideGraphicSettings`, `ForgeTypeId`,
  `ParameterValue`, plus `selected` for a **list** of element ids. **One type is still refused and it
  is not waiting on a rule: `IList<Reference>`, a FACE.** It is picked with a mouse and no text names
  one.
- **`switch-active-project` is not a fragment defect and never will be proved.** With two projects
  genuinely open, four routes were tried and all four answered with Revit's own sentence — *"Changing
  the active view is not applicable to inactive documents."* [Row 94](FRAGMENT-ISSUES.md). It sits in
  the 62 and can never move; **whether it stays counted is the owner's call.**

## Waiting on the owner

| | what | why only he can |
|---|---|---|
| **2** | select ONE element in Revit, for each of **13** fragments that need *that particular one* | a Revit selection is a person's act |
| **3** | **`--allow-publish`, yes or no** — 3 fragments are `risk: ADMIN`. `add-project-parameter` also writes a shared-parameter file to disk **which does not roll back** | proving a PUBLISH/ADMIN fragment is a decision |
| **4** | `gh auth refresh -h github.com -s workflow` in an interactive terminal | the token has no `workflow` scope; see below |
| **5** | decide whether `switch-active-project` stays in the DRAFT count | it changes a number he tracks |

`set-global-parameter` is **no longer code-blocked** — `ParameterValue` is typeable. It needs a global
to exist and both open models hold **zero**; making one is `create-global-parameter`, which is `ADMIN`.

## GitHub, and the one thing that cannot be pushed from here

`main` is current and CI is green. Branches on origin: `main`, the Dependabot branch, and whatever
another session has open — **check, do not assume.**

**`.github/workflows/` cannot be pushed from this machine.** The `gh` token is `gist, read:org, repo`
with **no `workflow` scope**, and a push carrying a workflow file **rejects the whole push**, not just
that file. Two things are parked on it:

- a local branch **`ci/run-check-signatures`** — one commit, seven lines, adding `check-signatures` to
  the Gates workflow. **CI does not run that gate today.**
- **PR #122** (Dependabot, `actions/setup-dotnet` 5→6) — clean and mergeable, refused on the same scope.

The `dependencies` label Dependabot asks for **has been created**, so that complaint is gone from
future bumps.

## Two tests are red on `main` and they are NOT from this work

```
tests/test_builder.py        KeyError: 'brain/heron_duct_sizing_reviewer.py'
tests/test_instructions.py   a duplicate id is refused by name
```

Both files were last touched by **PR #141**, merged by another session on 2026-09-14. Neither touches
anything this sitting changed. **CI is green because neither is in the workflow's suite** — which is
itself worth knowing. Written down here rather than left to be blamed on the nearest commit.

## One number that lied for weeks

`tools/owner-queue.py` printed *"163 DRAFT fragments need a model"* **two lines above its own
instruction to derive the number**. The real figure was 63. It counts them itself now. **A tool that
tells you to check a number it has just got wrong teaches the reader to trust neither.**

---

# Sitting of 2026-09-15 — the phantom modification, and why it was not one

**Branch:** `claude/quirky-engelbart-19a941` (worktree). **Tree left clean apart from this file and
the `A17` row added to [NEEDS-CHECKING.md](NEEDS-CHECKING.md).** No test was changed, because nothing
was shown to need changing.

## What was asked

A brief reported that running the whole Python suite left
`brain/fragments/filter-elements-by-type/fragment.yaml` modified in git, byte-identical apart from
line endings — HEAD LF, working copy CRLF — and asked for the test that does it to be found and fixed
at source. It explicitly forbade closing it with a `.gitattributes` rule, on the grounds that the
file is not the problem, the test writing to it is. **That instruction was right and was followed;
no `.gitattributes` was added.**

## What was found — the reported cause is disproved

**A pure LF-versus-CRLF difference cannot make a file show as modified on this machine.**
`core.autocrlf=true` is set in the **system** gitconfig (`Git/etc/gitconfig`, the Git-for-Windows
installer default — not something this repository chose), and **no `.gitattributes` is tracked
anywhere in the repo**. Git therefore normalises CRLF to LF on the way in, and the two spellings are
the same object to it.

Measured on an **untouched tree, with no test run**:

| | bytes | CR | LF | CRLF | `\r\r\n` |
|---|---|---|---|---|---|
| HEAD blob | 4282 | 0 | 102 | 0 | 0 |
| working copy on disk | 4384 | 102 | 102 | 102 | 0 |

…and `git status` **clean**. So the state the brief describes as *"the file afterwards"* is the
**permanent resting state of every checkout on this machine** — equally true before the suite and
after it, and on a fresh clone. `cmp` reporting DIFFERENT while `diff <(tr -d '\r' ...)` reports
nothing is **evidence of Windows, not evidence that a test wrote to the file.** The brief's own
verification step cannot distinguish a dirtied file from a clean one, which is why it looked like a
finding.

What *would* genuinely dirty it was established by pushing each variant through git's own clean
filter (`git hash-object --path`):

    lf     -> 0462ad5d...  == HEAD blob   clean
    crlf   -> 0462ad5d...  == HEAD blob   clean
    mixed  -> 0462ad5d...  == HEAD blob   clean
    crcrlf -> 0a877af4...  != HEAD blob   DIRTY

Only **double-CR** (`\r\r\n`) survives normalisation as a difference, and that needs a
**CR-preserving read paired with a translating write**. A static sweep finds no such pair on this
path: there are **no `newline=''` reads anywhere in the repository**; the only two `newline=''`
*writes* (`tools/resign-machine-proofs.py:100`, `tools/generate-decision-summary.py:159`) are the
safe non-translating kind; and the **only** writer of a real `fragment.yaml` in the entire codebase
is `heron_validate.accept` / `restamp`, which reads and writes both in text mode and so round-trips
CRLF unchanged. `tools/batch-prove.py` never writes one at all, and every `fragment.yaml` write in
`tests/test_validate_agent.py` goes into a `tempfile.mkdtemp()` workspace.

## What is NOT settled — read this before quoting the above

The run that would actually answer *"does any test write to that file"* — **every test alone,
fingerprinting the fragment after each** — **was killed at roughly 10 minutes of 99 tests** when the
sitting ended. It had reported **no change to any byte up to that point, and the tree was clean**,
but it never reached `DONE`. **So "no test writes to that file" is UNPROVEN and must not be repeated
as proven.** It is filed as **`A17`** in [NEEDS-CHECKING.md](NEEDS-CHECKING.md) with the command and
what a pass looks like. To resume:

    git checkout -- .; foreach ($f in Get-ChildItem tests\test_*.py) { python $f.FullName *> $null }; git status --short

**That is PowerShell, and it has to be.** This section first carried the bash form
(`for t in tests/test_*.py; do ...; done`). PowerShell 5.1 rejects it at parse time — `&&` is not a
valid statement separator there, `<` is reserved, and `/dev/null` is not a path — so **nothing runs
at all, including the `git checkout -- .` at the front.** It produces a wall of red `ParserError`
and changes nothing, which is safe but reads like a disaster. The repository already had this
lesson: **`A14` states its loop in Windows form for the same reason.**

Pass is `git status --short` **empty**. A **fail is worth more than a pass** — it names the test, and
that test is then the real defect the brief was reaching for.

## Two things found on the way, neither of them the reported fault

| | where | what |
|---|---|---|
| **1** | [`tests/test_scope_store.py:174`](../tests/test_scope_store.py) | Genuinely **writes into the live fragment library** — it creates `brain/fragments/zz-broken-temp/` with a deliberately malformed `fragment.yaml`, to prove a malformed fragment is skipped rather than indexed. It is a **new folder**, removed in a `finally`, and it never touches an existing fragment, so it is **not** the cause of anything reported here. But *"a test should not write into `brain/fragments/`, which is library source and not scratch"* is the brief's own principle, and this is the one place it bends. The author already met the interrupt case — there is a comment explaining why the folder is created **inside** the `try` with `exist_ok`, because an interrupted run once poisoned the test for good. **Worth a decision, not an emergency** |
| **2** | [`tests/test_embed.py:111`](../tests/test_embed.py) | Calls `os.utime()` on the **real** `brain/fragments/set-selection/fragment.yaml` to prove that touching a file without changing it embeds nothing — which is the right thing to prove, and the content is never altered. Its side effect is that it **invalidates git's stat cache** for a library file and forces a re-hash on the next `git status`. Harmless, but it is the most likely reason a `git status` oddity was noticed around this area at all |

## The honest summary

**The brief's worry was sound and its evidence was not.** A phantom modification on a clean tree
really would train a reader to ignore `git status`, and that is worth chasing. But on this machine
the specific check used — CRLF on disk against LF in HEAD — is **always** true and proves nothing,
so it cannot be the thing that showed the file as modified. Either something else did, or the file
was never modified. **`A17` is what decides which, and it has not been run to completion.**

## A17 RAN TO COMPLETION, LATER THE SAME DAY — and it passes

**The section above says the run was killed unfinished. It was, and then it was run again properly.
That paragraph stays as written; this supersedes it.**

**On the owner's PC, 2026-09-15, 18:10:34 to 18:25:27, exit 0. All 99 tests, and the tree was clean
after every one of them.** The run checked `git status --short` after each individual test rather
than only at the end, so this is **99 observations, not one** — the changed-file set never once
differed from the empty baseline. Final `git status --short` **empty**.

The fragment the brief named is **byte-identical to before the suite**:

| | sha256 | bytes | CRLF | `\r\r\n` |
|---|---|---|---|---|
| before the run | `b2599e4878a6f72d` | 4384 | 102 | 0 |
| after 99 tests | `b2599e4878a6f72d` | 4384 | 102 | 0 |

**So the reported phantom modification does not exist on current HEAD**, and the cause the brief
named was already disproved on its own terms — `core.autocrlf=true` makes a CRLF-versus-LF
difference invisible to git on this machine, so the check that raised the alarm **could never have
detected a write in the first place**.

**Nothing was fixed, because nothing was broken.** No test was changed. **No `.gitattributes` was
added** — the brief was right to forbid it, and it would have hidden a real defect had one existed.

### What this still does not say

One run, in file-name order, on one machine. It **cannot** speak for a test that writes only under a
different ordering, a different Revit, or a failing path. And two tests do touch the live library
without dirtying it — `tests/test_scope_store.py:174` creates and removes
`brain/fragments/zz-broken-temp/`, and `tests/test_embed.py:111` `utime`s a real fragment. **Neither
is the reported fault.** The first is still worth a decision: *a test should not write into
`brain/fragments/`, which is library source and not scratch* is the brief's own principle, and it is
the one place it bends.

### The lesson worth keeping

**The brief's worry was sound and its evidence was not.** A phantom modification on a clean tree
really would train a reader to ignore `git status`, and that was worth chasing. But the specific
check used — CRLF on disk against LF in HEAD — is **permanently true on this machine**, before and
after anything, so it proved nothing. **Verify what git would actually store**
(`git hash-object --path <path> <file>` against `git rev-parse HEAD:<path>`), never `cmp` against
`git show`.

## The test that wrote into the library, and the two failures found on the way out

### The fix

`tests/test_scope_store.py` proved that a malformed fragment is skipped rather than indexed, and it
proved it by writing `zz-broken-temp/` **straight into `brain/fragments/`** — library source — and
deleting it afterwards. An interrupted run left it there. An earlier fix had moved the `makedirs`
inside the `try` with `exist_ok` so the NEXT run could clear it, which made the mess **survivable
rather than stopped**.

It now builds a temp library and points `heron_fragment.FRAGMENTS_DIR` at it for the duration,
restored in the `finally`. `rebuild()` calls `load_all()` with no root and `load_all` resolves
`root or FRAGMENTS_DIR` at **call** time, so one module global redirects it. Same proof, and it
cannot outlive the run. Commit `7b85557`.

### What the fix exposed, which is worth more than the tidy-up

The good fragment is **built, not copied**, and the reason is a defect the old test could not see.

Copying was tried first. It does not work: a PROVEN fragment's proof is fingerprinted against its own
bytes and path, so a copy out of `brain/fragments/` fails validation and the temp library indexes
**nothing**. And the test still said `ok` — because `after == before` never asked whether either
count was *real*. **A library indexing zero satisfied it: `0 == 0`.** A `before == 1` check now sits
beside it, and it is what caught this.

Proved both directions: with the malformed fragment the count stays 1; adding a **valid** second
fragment moves it 1 → 2, so the comparison genuinely observes additions and is not vacuous.

### THE SUITE IS NOT GREEN, AND A17 COULD NEVER HAVE TOLD YOU

**Measured 2026-09-15, 19:29 to 19:43 — 99 suites, 3 failing:**

| suite | why | whose |
|---|---|---|
| `test_bridge_roundtrip` | needs a built .NET test host — **expected**, recorded in §Tests above | the machine |
| `test_builder` | *"one file is planned, and it is not a test"*, then `KeyError: 'brain/heron_duct_sizing_reviewer.py'` | **`45a6746`** |
| `test_instructions` | *"a duplicate id is refused by name"* and *"both files are named, so neither is the silent loser"* — its own 16 evaluation assertions still pass | **`45a6746`** |

`test_builder` and `test_instructions` are **real assertion failures needing no special machine**, and
both files plus their `brain/` modules were last touched by `45a6746` (*"The seams first, then the
departments they made cheap"*, #141). **They are NOT fixed, deliberately** — the owner's instruction
was to note them, and quietly patching another session's failing test buries the problem rather than
closing it. Filed as **`A18`** in [NEEDS-CHECKING.md](NEEDS-CHECKING.md).

**How this went unseen is the part to keep.** `A17`'s command runs every suite and checks
`git status`, and **throws every exit code away** (`python $f.FullName *> $null`, no `$LASTEXITCODE`
test). It answers *does the suite dirty the tree* and **cannot see a failing test at all** — so the
earlier "99 of 99 clean" in this file is true and says **nothing whatever** about pass or fail. Those
are two different questions and one command cannot answer both. `tools/check-gaps.py` does report
unfinished suites, but its exit code follows the UNFINISHED list, so it returns 1 on a healthy tree
and a reader learns to ignore it.

**THE SENTENCE ABOVE IS WRONG AND IT STAYS ONLY AS THE RECORD OF A WRONG BELIEF.** It said
`check-gaps.py` could not be trusted to show a failing test. Measured 2026-09-15:
`tools/check-gaps.py:131-181` **runs every suite, reads every return code, prints `FAIL <name>`**,
appends it to UNFINISHED, handles a `SKIPPED=3` sentinel and a 300-second hang bound, and skips
only `test_bridge_roundtrip` deliberately. It would have named both failures in plain sight. The
instrument existed and worked. **Nobody had run it on this PC** - which is precisely what row `K1`
of [NEEDS-CHECKING.md](NEEDS-CHECKING.md) asks for, and it was still open. The failure was
procedural, not instrumental, and that distinction is the whole lesson: before building a second
tool, check whether the first one was ever switched on.

### One mistake of mine, recorded because it nearly cost the fix

The A17 script opened with `git checkout -- .`. It was re-run while the test fix was still
uncommitted, and **discarded it**. Nothing else was lost — every commit was intact — and the work was
redone. The script now **refuses on a dirty tree** instead of forcing one: starting clean is a
precondition to check, not a state to impose. Clearing up after it also left a stale `index.lock`
(0 bytes, one minute old, no git process, this worktree only), removed after checking those four
things rather than on sight.


## The gate nobody ran, the host nobody built, and two failures nobody had seen

**2026-09-15, on the owner's PC, after #142 landed and the tree went from 99 suites to 188.**

### `test_bridge_roundtrip` is not "the machine" any more - it PASSES

It has been recorded for weeks as *needs a built .NET test host*, and counted as the one honest
machine-dependent failure. **The owner closed Revit and said to build it.** One command -
`dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024` - exit 0, and the suite then ran
**31 assertions and passed every one**, ending *"Step 1 PASSED - the bridge works, without Revit."*
Ping, info, the lease across three chats, preemption, malformed JSON, a bad token, and the bridge
toggled off and back on. **Nothing was broken. The host had simply never been built here**, and the
absence of a build had been carried in the register as though it were a property of the machine.

### `check-signatures` is now in CI, without touching a workflow file

`tools/check-signatures.py` has existed since 2026-09-13 and **nothing ran it** - the defect its own
commit message names: *"A gate nobody runs is the same as no gate."* The commit that wires it
properly sits on `ci/run-check-signatures`, rebased onto current main and **unpushable**: the token
carries `repo` but not `workflow`, and GitHub refuses
*"to allow an OAuth App to create or update workflow `.github/workflows/gates.yml`"*.

So it rides inside `tools/check-docs.py` as **section 9**, exactly as section 8 already does for the
generated decision table and for exactly the same recorded reason. `check-docs` runs in *"The gates
that must pass"*, so the signature gate now runs on every pull request. Verified: `check-docs.py`
exits **0** before and after, and section 9 reports *"Signatures in the library: 299"* with the one
STALE signature that D-30 is supposed to surface. **It is not a documentation check and the comment
says so** - if anyone ever edits the workflow by hand, give it its own step and delete the section.

### Two failures that were in nobody's list

A partial `check-gaps` sweep - stopped deliberately at 14 of 188 so the fixes could be applied
without corrupting a measurement mid-flight - named two things `A18` never knew about:

| suite | what |
|---|---|
| `test_agents.py` | **HUNG** - still running after the 300-second bound, gave up. It is 225 lines and imports only `io`, `os`, `sys` and `heron_agents`; nothing in it reads stdin. Undiagnosed |
| `test_authoring.py` | **FAILS** in 0.3s. A real assertion failure, not a machine |

**Both arrived with #142**, which added 89 suites written on Linux - the same provenance as the two
fixed above, and the prediction written down before the run was that more Windows-only failures
would be found. **A theory that did not survive checking, recorded so it is not re-run:**
`tools/api-surface/.assemblies/` really is 264 MB of Revit DLLs and `agent-count.py` really does not
skip that folder, but it filters by extension before opening anything, so the DLLs are never read
and that is **not** the cause of the hang.


### The two above were BOTH fixed, and one of my own notes above is WRONG

**`test_agents.py` does not hang.** The row above says HUNG and it stays as written, because being
wrong about it in the same hour is the finding. On a quiet machine it **passes in 279.3 seconds**
against check-gaps' 300-second bound - **twenty seconds of margin, 7%**. The first run died because
a build and a second sweep were loading the machine at the same time. So it is not broken and it is
not healthy either: **a suite that passes with 7% margin will fail at random forever**, and every
time it does, somebody will chase a defect that is not there. It needs making faster, not fixing.
Nothing here diagnoses why 225 lines that import four modules take four and a half minutes.

**`test_authoring.py` was the SAME BUG A THIRD TIME.** `brain/heron_authoring.py:109` -
`os.path.relpath(path, ROOT)`, unguarded, inside `_where()`, which exists to word a refusal. A draft
written to a temp folder put the path on `C:` with the repository on `D:`, and the crash replaced
the message it was building. The module **already imported `heron_fragment as FRAG`** and simply did
not use it here.

**And a fourth copy was found before it could bite.** `brain/heron_tag.py:95` held a **byte-identical
copy** of that helper - same docstring, same missing guard - reached through a caller-supplied
`workflow` path. Both are guarded now. Four instances of one defect in one day is no longer a bug,
it is a pattern: **`os.path.relpath` against `ROOT` is unsafe anywhere the path can come from a
caller, a test, or another drive**, and `heron_fragment.repo_relative` is the repository's one
answer. The remaining call sites were checked and left alone deliberately - they take module-level
constants (`WORKFLOW`, `TEMPLATE`, `AGENTS`, `PROPS`, `EVIDENCE`) that are under `ROOT` by
construction, and filing them would put false positives in the register.

**`test_mcp_stdio.py` was a stale claim, and the test said so itself.** Its own comment set the
standard - *"re-base it on what is true, with the reason written down, never edit it until green"* -
and then **the very assertion that comment was defending went stale in the same way.** It asserted
the reply still contains *"no way to reach Revit"*, calling it *"the limit that has NOT moved"*.
It moved: **#144, "Changes ON now changes something"**. `_cannot_run()` now says a writing fragment
reaches Revit through `revit_change`, which KEEPS what it did, while the ribbon switch is on. The
old sentence survives **only in a comment**, so the check could never have passed again. Re-based on
the durable claim instead - **writing is GATED, and Heron says so** - asserting both that the reply
names the switch and that it names the refusal, so deleting the permission wording still fails it.
A Heron that claimed it could write freely is the danger worth a test.


### `test_agents.py`: 279 seconds to 5.5, and it was one missing argument

The row above says it needs making faster and does not say why. It is one line, and the mechanism is
worth keeping because nothing about it looked like a performance bug.

`heron_agents.record()` line 174 reads `deals = contracts() if deals is None else deals`, and
`contracts()` re-reads every contract from disk - **0.6 seconds a call**. `tests/test_agents.py`
built two list comprehensions over all **250** agents calling `record(a, agents, claims, host)`
**without `deals`**, plus one more per spanning agent. Five hundred calls, 0.6s each. **That is the
279 seconds**, and the value it kept rebuilding was already sitting in `deals` at the top of
`main()`, computed once at line 64.

| | |
|---|---|
| before | **279.3s** of check-gaps' 300s bound - 7% margin |
| after | **5.5s** |
| assertions | **43, unchanged** - `deals` is the identical value `record()` built for itself |

**What this cost before it was found.** It HUNG - exceeded the bound and was killed - on **two of
three** runs, passing only on the one where nothing else was using the machine. Each hang cost five
minutes and reported a failure that did not exist, and `A18`'s whole lesson is what a report that
cries wolf does to the person reading it. The first of those hangs was recorded in this file as an
undiagnosed defect in a suite that had nothing wrong with it.

**The shape to remember:** a function that lazily rebuilds an expensive input when an argument is
omitted is invisible at one call site and quadratic at two hundred and fifty. `records()` - plural -
was never slow, because it builds `deals` once and passes it down. The singular call in a loop is
the trap.


---

# Sitting of 2026-09-16 - the notes were the backlog

**Asked for:** pull GitHub down so local and remote are identical, read the handover and say what the
balance is, read the issue registers, **and fix the notes themselves where a note was already done
and still said otherwise.** That last half is what this section is mostly about.

**Nothing was proved against Revit here.** No fragment was run, no add-in was built, no count moved
because work was done. Every number below is derived, and every correction below is a correction to a
MARK, never to a result.

## The repository

`main` was **9 commits behind** `origin/main` and fast-forwarded clean to `749b580`. Working tree was
clean before and after. **Local and GitHub are now identical.**

**Five local branches survive, and they are two different things:**

| branch | what |
|---|---|
| `claude/friendly-hypatia-196de4`, `claude/gifted-burnell-61c3c3`, `claude/nifty-mendel-ce45f7` | **Merged - their code is on `main`** (#147, #148, #149). What is unique to them is **stale README prose** - *"it is still private"*, *"298 PROVEN"*. Each also owns a live **worktree** under `.claude/worktrees/`, so another session may be standing in one. **Do not delete them without checking that.** |
| `ci/run-check-signatures`, `ci/wire-three-checkers` | **Real work, unpushable.** Both touch `.github/workflows/gates.yml` and the `gh` token has no `workflow` scope |

**`ci/wire-three-checkers` was documented NOWHERE** until this line. The section above records only
`ci/run-check-signatures` and says *"two things are parked on it"* - it is three, and the second
branch is the bigger one: **32 lines wiring `check-licence`, `check-narrow-errors` and
`check-fragments-compile`**, none of which CI runs today. All three exit **0** on the tree right now,
so the branch is ready and waiting on one command in an interactive terminal:

```bash
gh auth refresh -h github.com -s workflow
```

## The balance, derived rather than read

| | | derive it with |
|---|---|---|
| Fragments | **310 `PROVEN` / 62 `DRAFT`**, 372 total | `grep -h "^heron-status:" brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| Waiting on the owner | **140 items** - 25 decisions, 80 needing Revit, 4 needing the PC only, 2 needing a network, 29 to be read | `python tools/owner-queue.py` |
| Heron's own open defects | **29**, ids printed | `python tools/open-defects.py` |
| Stale signatures | **1** - `set-wall-constraints` | `python tools/check-signatures.py` |
| Gates | `check-docs`, `check-metadata`, `check-structure` all **exit 0** | |

**The section above says `298 PROVEN / 62 DRAFT, 360 total`.** That was true at its close and is left
standing. Twelve fragments have been added since and all twelve arrived PROVEN.

## Four notes that said OPEN and were not

Each was verified in the code or by running it, not inferred from a commit message.

| | said | is |
|---|---|---|
| **[FRAGMENT-ISSUES row 37](FRAGMENT-ISSUES.md)** | *"OPEN, and it needs an add-in rebuild"* | **Withdrawn by row 46 on 2026-09-13.** A Revision's `Element.Name` really is `"Seq. N - Description"`; no rebuild was ever needed |
| **row 44** | *"wired into `gates.yml`"* | **Never true.** That commit is the unpushable one. It runs because it **rides inside `check-docs.py` as section 9** |
| **row 96** | *"OPEN"* | **Closed 2026-09-15 by the session the row itself names.** The closing sentence was appended and the first word left alone |
| **row 97** | *"OPEN. A separate session is on it"* | **That session landed as #147.** `DocumentPin._common` - *"This is the whole fix"* |

**Row 44 is the one worth keeping.** A note claiming a gate runs in CI, when it does not run there at
all, is worse than no note: it is the exact false comfort `check-signatures` was written to prevent,
told about `check-signatures`.

## Section 5's heading said "six still open" and it was twenty-nine

Ninety-four rows were appended under it. **No append was careless** - each session wrote an honest row
and left the heading to somebody else, which is what makes this recurrent rather than sloppy. It is
the prose-total drift [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) has recorded against itself **seven
times**, in the one file that had not yet caught it.

The heading carries no number now. `tools/open-defects.py` prints the ids, and **says in its own
output what it cannot see**: a row whose state still reads OPEN after a LATER row closed it. All four
above are that shape, and no pattern finds them - the closure is written in a different row, in prose.

## A strike that a person could read and a tool could not

**`E11` to `E15` were all RUN on 2026-09-15. `owner-queue.py` kept printing all five back to the owner
for eight days**, beside the genuinely open `E16`-`E18`.

The session that ran them struck the **`Do this` cell** and left the **ID** unstruck. A reader sees a
crossed-out instruction and correctly reads *done*. The tool matches
`^\|\s*(~~)?\*\*([A-Z]\d+[a-z]?)\*\*` and only ever looks at the ID.

**Two marks for one fact, kept in two places, updated in one.** The prose drifts in this repository
were all caught by running the count; this one was invisible to the count, because the count was the
thing that was wrong. Four had PASSED (`E11`, `E13`, `E14`, `E15`); `E12` FAILED and is closed rather
than passed, superseded by `E16`. **If you strike a row, strike the ID.**

## The E numbers in NEEDS-CHECKING's prose were all one too low

[`the 2026-09-16 sitting`](handover-archive/2026-09-16-e11-e14-renumbered-mid-flight.md) warned about exactly this - *"If you remember E11
failed, that row is E12 now"* - and **the warning did not reach the file it was warning about.** The
heading `### E11 FAILED` sat directly beneath a table row reading `E11 ... PASSED`. Corrected to `E12`,
with the old numbers named rather than quietly swapped.

## Two things that are now better than the notes say

- **`test_builder.py` and `test_instructions.py` both exit 0.** The section above records them as
  *"two tests are red on `main`"*. Re-run here: both pass. Whatever fixed them is not recorded, and
  that is worth knowing - **a red test that goes green unattributed is a test nobody is reading.**
- **`check-licence`, `check-narrow-errors` and `check-signatures` all exit 0**, so the parked CI
  branches are not hiding a failure.

## The one thing that is WORSE than the notes say

**The deployed add-in is older than `main` on all three Revit releases.**

| Revit | deployed | `main`'s add-in source |
|---|---|---|
| 2020 | 2026-09-15 22:56 | **2026-09-16 13:24** (`58a613d`) |
| 2024 | 2026-09-16 02:29 | |
| 2027 | 2026-09-15 22:56 | |

So every row that ends *"needs a deploy"* - [row 10](FRAGMENT-ISSUES.md), [row 13](FRAGMENT-ISSUES.md)
- is still blocked, and **`E16`-`E18` cannot be attempted until it is rebuilt.** Build and deploy one
release at a time; the output folder is shared, and what is in it last wins.

## What this sitting did NOT do

- **Nothing was run against Revit.** The 80 Revit rows are untouched.
- **No branch was deleted.** Three are merged and safe to remove, and each owns a live worktree, so
  that is the owner's call with a session possibly standing in one.
- **The remaining 29 open defects were read, not fixed.** Row 8 is still the biggest single unblock:
  a bare `ElementId` result cannot be read as a quantity, and **15 DRAFT fragments have no other
  countable result at all.**


## The deploy happened, ninety minutes after the section above said it had not

**2026-09-16 18:04-18:06, on the owner's PC.** He closed Revit and ran all three. Verified by
reading the deployed binaries rather than by trusting the build exiting 0:

| Revit | framework in the deployed DLL | deployed | size |
|---|---|---|---|
| 2020 | `.NETFramework,Version=v4.7.2` | 18:06 | 156,672 |
| 2024 | `.NETFramework,Version=v4.8` | 18:04 | 156,672 |
| 2027 | `.NETCoreApp,Version=v10.0` | 18:04 | 157,184 |

**Each release got its OWN framework**, which is the thing the shared `bin` folder makes easy to get
wrong and impossible to see: a 2027 build sitting in a 2024 folder loads nothing and Revit does not
say why. `Heron.Core.dll` and `Heron.Bridge.dll` carry the same timestamp in all three folders, 2027
has its `.deps.json`, and the add-in grew from ~128 KB to ~156 KB - a real rebuild, not a re-copy.

**So `E16`-`E18` are unblocked, and the two rows that end *"needs a deploy"* have their precondition
met** - [row 10](FRAGMENT-ISSUES.md) and [row 13](FRAGMENT-ISSUES.md). **Neither is proved by this.**
A deployed fix is a fix that can now be run; D-30 wants it run.

**The table above this one went stale in ninety minutes**, which is the whole subject of this sitting
arriving on schedule. It is left standing and corrected here rather than edited, because the interval
is the finding: **the fastest-moving fact in this repository is the state of the machine in front of
him, and it is the one every register records as prose.**

### The command in that table was wrong, and the shell said so

It was given with `&&` between the build and the deploy. **Windows PowerShell 5.1 has no `&&`** -
*"The token '&&' is not a valid statement separator in this version."* Nothing ran; it did not parse.
The separator is `;`, and *only if the last one worked* is `if ($?) { ... }`:

```powershell
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2024; if ($?) { powershell -File tools\deploy-addin.ps1 -RevitVersion 2024 }
```

**`deploy-addin.ps1` refuses while Revit is running** (`Get-RevitBlockReason`), so the build half can
be done with Revit open and only the copy needs it closed.


## Then he asked whether the OTHER pages had been done, and three indexes had drifted the same way

Not the registers - **the pages that say what exists.** Each was a typed list of files, and each had
been appended to by sessions that added the file and not the row.

| page | said | was |
|---|---|---|
| [`work-notes/README.md`](work-notes/README.md) §*What is here now* | **11** notes | **15** on disk. Missing: `mep-session-2026-09-16`, `proving-session-2026-09-15`, `agent-build-order-2026-09-13`, `next-steps-2026-09-12` |
| [`tools/README.md`](../tools/README.md) | every tool | **three** were absent - `open-defects.py`, `new-agent.py`, `resign-machine-proofs.py` |
| [`FOR-THE-OWNER.md`](FOR-THE-OWNER.md) §2 | *"The five buckets"* | **six**, and `owner-queue.py` defines six |

**`FOR-THE-OWNER.md` is the sharpest of the three.** Its §6 is titled *why this page holds no list*,
and it carried a count that had drifted - not of items, which that section forbids, but of the buckets
the items fall into. **The rule was obeyed one level up from where it was needed.**

All three now carry the command that derives them, and **all three commands were run before being
written down.** Two were wrong the first time and the repository's own rule caught both:

- the work-notes check missed `FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` and
  `PROMPT-fragment-validation-agent.md`, because `[a-z0-9/.-]+` **cannot see a capital letter**.
- the tools check reported **twenty false extras**, because `grep -oE '[a-z-]+\.py'` splits
  `heron_architect.py` at the underscore and hands back `architect.py`, a file that does not exist.

**Prove the pattern can see what you know is there.** Written in this repository since the beginning,
broken twice in one hour by the person writing the commands to enforce it, and caught both times by
running them instead of reading them.

`tools/open-defects.py`, `new-agent.py` and `resign-machine-proofs.py` are now documented. The first
is new; **the other two had been undocumented since they were written** - the quiet version of the
defect `check-signatures.py`'s own commit message names: *a gate nobody runs is the same as no gate.*


---

# Sitting of 2026-09-16, third - what reading somebody else's code found in ours

**Asked to look at an unrelated open-source WPF app** (a 2D drawing tool with a Roslyn scripting host
and an MCP bridge - the same three problems Heron has) **and say what was worth taking.**

**Almost nothing was worth taking, and that is the result rather than a disappointment.** Its geometry
library is 2D and Revit's is 3D and authoritative; its pipe-with-an-ACL transport is what
`BridgeServer` already does; its compile cache is what `RevitFragment` already does, keyed the same
way. **What it was worth was as a mirror.** Three things wrong with Heron came out of reading it, and
none of them would have been found by reading Heron.

## The one that could have killed Revit

`RunScript`'s comment says *"a fragment that throws is a finding, not a crash."* True of every
exception but one. **`StackOverflowException` cannot be caught** - the runtime fails fast, no handler
runs, and the process it ends is Revit with the user's unsaved model in it.

**Zero of the 372 fragments recurse today**, so this was a door standing open rather than a fire. But
a fragment's `.cs` is live - read off disk and sent on every call - so the door is one edit wide.

### The idea was taken. The code was NOT, and that is the whole lesson

The usual form of this rewriter guards named members and **deliberately skips lambdas**, for a stated
and perfectly good reason: recursion flowing only through anonymous functions is not a real shape in
ordinary C#.

**Measured here: 0 of 372 fragments declare a method or a local function.** A fragment is a script -
top-level statements and `Func<>` lambdas. So the received rule, applied unexamined, **would have
shipped a guard that compiles, passes review, and protects nothing at all.** Guarding block-bodied
lambdas instead covers **91**.

That is [the standing rule](../HERON_CONSTITUTION.md) earning its keep in the most literal way
available: *study the thinking, never copy the code.* Copying would have produced a green gate over an
open hole - which is the failure this whole repository is organised against.

**What is still unguarded, deliberately:** an expression-bodied lambda. Rewriting one can turn `Func`
into `Expression` and silently pick a different overload. **A guard that occasionally misses is worth
more than one that changes what working code means.**

### It is proved over the library, and that proves less than it sounds

`tests/Heron.StackGuard.TestHost` runs the rewriter over **every fragment in the library**: line
counts unchanged (so error line numbers still point at `fragment.cs`), no new diagnostic introduced,
and every lambda-carrying fragment guarded. On 2026-09-16 that was **372 fragments and 836 checks**;
on 2026-09-21 it is **395 fragments, 882 checks and 91 carrying a guard**, which is the host counting
rather than this page claiming. It links `HeronStackGuard.cs` **by source** rather than
referencing the add-in, because that file touches no Autodesk type - the same mocked-boundary trick
`Heron.Bridge.TestHost` uses, and it keeps **one** copy of the rewriter. Two halves written to check
each other is how [row 96](FRAGMENT-ISSUES.md) happened.

**AND FOR FIVE DAYS NOTHING RAN IT.** The host was added to `heron_dotnet.PROJECTS` so that it
COMPILES on all eight releases - this page calls that "the 'a gate nobody runs' shape caught before
it could set" - and the half it caught was the compiling half. No suite built it and no suite
executed it, so 882 checks sat outside every count of *every test passes*, and the paragraph above
went on describing a library of 372. `tests/test_stack_guard.py` is the missing runner, written in
the same shape as `test_kernel.py`: it works out the newest framework an installed SDK can build and
an installed runtime can run, builds the host with `HeronTfm` overridden to a plain `net10.0`
(`RevitVersion=2024` maps to `net48`, which needs Mono), runs it, and **exits 3 where there is no
.NET - which is not a pass**. See [row 170](FRAGMENT-ISSUES.md).

**None of that says the catch works.** No test here can tell you what the CLR does when the stack
actually runs out. That is **[J9](NEEDS-CHECKING.md)**, and it needs Revit.

## The one that quietly loses your settings

`HeronConfig.Save` and `BridgeIdentity` both wrote a sibling `.tmp` and then

```csharp
if (File.Exists(target)) File.Delete(target);
File.Move(tmp, target);
```

which reads as careful and is **worse than a plain overwrite**. A plain write leaves a truncated file;
this leaves **no file**, through a window made wider by the extra syscall inside it.

**In `HeronConfig` it does not corrupt the settings, it erases them, with no message.** `Load` answers
a missing file with `Defaults` and swallows `IOException` without a word, and the next `Save` writes
those defaults back. Every step is individually reasonable. The file whose own header reads *"Owned by
you. Never overwritten by an update"* is the one that disappears.

Fixed with `HeronAtomicWrite`: `File.Replace` when the target exists, a plain rename when it does not.
**`File.Replace` and not `File.Move(tmp, target, overwrite: true)`** - the overwrite argument arrived
in .NET Core 3.0 and `Heron.Core` is built for net472 and net48 as well.

## The one that is recorded and NOT fixed

Five fragments narrow through Revit's own spatial index. **Four do not** -
`check-minimum-clearance`, `check-vertical-clearance`, `check-insulation-clearance` and
`find-nearest-elements` nest two loops with no pruning. [Row 106](FRAGMENT-ISSUES.md).

**No claim is made that it is slow, because nothing has timed it.** `check-minimum-clearance` already
reports `pairsChecked`, so one run answers it. And the fix is not a swap - a spatial filter finds what
*intersects*, clearance is about what does *not touch yet*, so the outline has to be grown first. It
also edits fragment files, so **four fingerprints change and four proofs need re-signing.** That bill
is why it is a row rather than a commit.

## What this cost, and what it did not

| | |
|---|---|
| Proofs gone stale | **none.** Both fixes are add-in C#; the fingerprint hashes the fragment file on disk, and no fragment file was touched |
| Gates | `check-compile` (5 projects x 8 releases), `check-docs`, `check-metadata`, `check-structure`, `check-package` - all **exit 0** |
| New project | `tests/Heron.StackGuard.TestHost`, **added to `heron_dotnet.PROJECTS`** - it compiled nowhere until it was, which is the "a gate nobody runs" shape caught before it could set |
| Still owed | **[J9](NEEDS-CHECKING.md)**, and it needs the add-in deployed |

**Worked in a separate worktree throughout.** The main tree was on another session's
`rename-phase-fragment` with two commits on it, and moving somebody else's checkout to save creating
a folder is not a trade worth making.


## J9 ran the same evening, and it PASSED

**Revit 2024, session 33812, started 20:13:52 against an add-in deployed 19:54:19.** That comparison
was made FIRST and it is the only reason the result means anything: a Revit started before the deploy
would have loaded the old add-in, died, and the guard would have been blamed for it.

A fragment recursing with no floor came back as an ordinary finding:

```
zz-j9-stack-guard-probe  [fragment_threw]
    'zz-j9-stack-guard-probe' threw while running: Insufficient stack to
    continue executing the program safely.
```

**Revit lived** - same PID, `ping` 1 ms, and a real read of 3,565 elements afterwards. The probe
fragment was deleted and the library is back to 372.

**The model was NOT blank.** `test projject.rvt`, 3,565 elements, open throughout - not the
arrangement the row asked for. It makes the result stronger and it is said out loud because a FAIL
would have taken that session with it.

**Still uncovered, deliberately:** an expression-bodied recursive lambda. Running one is expected to
end Revit, so it was not run unasked.

## Row 106 measured, and the measurement CHANGED the row

`check-minimum-clearance` was recorded as comparing every element against every element. Timed on the
same model:

| | |
|---|---|
| `pairsChecked` reported | **9,506** |
| the same figure derived independently | **9,506**, to the unit - the counter is honest |
| placed elements handed in | **3,565** |
| elements the loop actually paired | **98** - the ones with a bounding box |
| naive pairs it skipped | **12,699,719** of 12,709,225 |

**THE LOOP ALREADY PRUNES, and the row did not know that.** The n-squared is over GEOMETRY, not over
everything handed in. The concern is real and narrower than it was written.

**And this model cannot settle it.** Its largest physical category is **28 Walls**; the biggest
categories are settings - 180 Electrical Load Classification Parameter Elements, 125 Space Type
Settings, 91 Legend Components. **No ducts and no pipes at all** (the 17 `Pipe Segments` are segment
definitions). The 9,506-pair run took **1.52 s** and the 756-pair run **2.21 s** - the bigger one was
FASTER, so both are process start-up and one bridge round trip with the loop invisible inside them.

**So the row stays OPEN with what would close it: a real MEP model**, where nearly every element has
geometry and that pruning stops helping. `Snowdon Towers Sample HVAC.rvt` ships with Revit 2024 and is
already named in this library's own proofs. **Opening it was offered and declined on the day**, so the
measurement is still owed. **A fast number from a model that cannot show the effect would have been
worse than none** - it would have been quoted later to dismiss a real question.

## Three things that cost a round trip each, all of them already written down

- **A reply truncates a list to three.** The first sizing probe answered `12 item(s) [a, b, c, ...]`.
  **When the CONTENTS are the answer, return a string.**
- **`check-minimum-clearance` cannot be arranged from a selection** - *"one selection cannot say which
  is which"*, because `targets` binds from the CHAIN. **`python tools/generate-jobs.py` says exactly
  this about exactly this fragment**, and reading it first would have saved the attempt.
- **The lease refuses a second chat, and retrying renews it.** Two refusals arrived before the other
  session was stopped. The countdown moving 4 -> 3 minutes is the signal it is expiring rather than
  being renewed; the fast route is the Heron ribbon button off and on.

## Where the day ended

| | | derive it |
|---|---|---|
| Fragments | **310 `PROVEN` / 62 `DRAFT`**, 372 total | `grep -h "^heron-status:" brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| Waiting on the owner | **140** | `python tools/owner-queue.py` |
| Heron's own open defects | **30** | `python tools/open-defects.py` |
| Deployed | **2020, 2024, 2027** - each verified to carry its own framework and both fixes | |

**Two parked CI branches still need one command in an interactive terminal:**
`gh auth refresh -h github.com -s workflow`.
---

# Sitting of 2026-09-16 into 2026-09-17 - a rename that Revit refuses, and fourteen questions that answered with a write

**Started as "rename the Existing phase to Design".** Ended with a chain that can hand elements
between fragments safely, pipes and fittings Heron can actually select, and nine findings - most of
them about Heron rather than about Revit. Rows **107 to 117** in
[FRAGMENT-ISSUES](FRAGMENT-ISSUES.md).

*The banner work of the same night is deliberately not described here. It is being taken further and
a description written now would be stale before it was read.*

## Revit will not rename a phase, and that took building the thing to find out

A `RENAME_PHASE` fragment was written, compiled on 2020-2027, and run against real models on **both**
Revit 2020 and Revit 2024. Each refused identically: `Element.Name` throws *"This element does not
support assignment of a user-specified name"*, and `BuiltInParameter.PHASE_NAME` behind it reports
`IsReadOnly`. **Two doors, two releases, one answer - so it is the object, not the release.**

**Nothing short of running it could have found that.** `check-fragments-compile` passes on all eight
releases and `check-api-surface` passes on all eight, and both are *correct*: the member genuinely
exists. A member that exists and throws is exactly what `api-surface/changes.json` lists under
`cannotSee`.

**The fragment was removed.** What replaced it is a routing note in `REPORT_PHASES`, beside the one
that already said Revit exposes no CREATE call for a phase. The same fact, from the other end, and
nobody had read across. Row 107.

## `revit_change` had never once reported what a write did

It read `reply.get("answer")` - **a key nothing emits, on either side of the bridge**. The add-in
leaves `provides`, `bound`, `verdict`, `applied`, `rolledBack`; grep for `"answer"` across `mcp/`
returns one hit, the read itself. So the value was always `None` and **every write through that tool
reported only that it had run**.

Measured: `RENAME_PHASE` answered *"ran in PIPE"* while the fragment had produced `renamed: false`
and Revit's own refusal. Only reading the model back showed the difference.

**The comment directly above the bug was right** - *"on 2026-09-09 a rollback did not hold while this
side claimed it had"*. The lesson had been learned and written down; the key name was wrong. Row 111.

`tests/test_change_reporting.py` was confirmed to **fail on the old key** before being kept.

## Fourteen ordinary questions answered with a write, and the cause was not the ranking

Found by hand, then swept: **45 things a modeller says, 17 reached a capability above READ, 14 beat a
READ that was right there.** *"How many pipes are there"* resolved to `CAP_OPEN_PIPE_ENDS`. *"Count
the air terminals"* to `CONNECT_AIR_TERMINALS`. *"Which parameters are empty"* to
`REMOVE_PARAMETER_VALUE`.

**`check-routing.py` reported one, and was right about everything it was allowed to ask** - it tests
each fragment's OWN declared utterances, and none of those 45 sentences is declared by anybody.

**The cause was that they never reached the ranking's safety net.** `find()` tries `short_circuit`
first: an exactly declared sentence resolves by IDENTITY and no ranking runs. Two ways to miss it,
and the library had both - **ambiguity** (`__ambiguous__` is a deliberate miss; only **7 phrases of
2,442**, two of them dangerous) and **nobody declaring the sentence at all**.

**So the fix is declaring, not demoting.** A nudge could never have done it: the quality nudge is
deliberately smaller than one fusion rank (0.00026) and these writes won by **2.4 to 2.6**. Measured
both ways, **14 to 7**. Rows 113 and 116.

`tools/check-risk-crossings.py` makes the sweep repeatable, and **its question list is meant to
grow** whenever a session produces a new one.

**AND A THIRD SWEEP JOINED THEM 2026-09-19** — `tools/check-declared-questions.py`, [row 158](FRAGMENT-ISSUES.md). The other two ask the SEARCH; this one reads the CARDS, so it runs in CI where there is no store, and it finds what neither can see: a fragment **above the write line declaring a question in its own `utterances:` block**. Those win by `identity`, which short-circuits before ranking, so no re-ranking repairs one. **Ten today**, the starkest being `SET_VIEW_SCALE` declaring *"what scale is this view"*. It is in the `reports` job, so it runs on every pull request rather than when somebody remembers.

## A fragment can now consume what another left

[docs/36](36-remembering-between-steps.md) built. The chain reset stays exactly as it was; the caller
**opts in** by naming what it expects, and the executor checks before binding anything.

**Three quarters of it already existed** - `Chain` recorded the document, the producing fragment and
the time. What was missing was *what the producer ran with* and a caller-declared expectation.

Proved on `4355-BHVD-3D-50C10-BL001A`: **271 elements handed over**, and then, with the **same
carried values and nothing else changed**, a wrong declaration refused (`chain_inputs_differ`) and a
right one bound 143. All four refusals fired live. Row 115.

**It did real work the same hour** - `read-element-parameters` then `group-and-count`, the exact
hand-over `fragment-proving` records as failing.

## And it caught me giving the owner a confident wrong answer

Asked to isolate the pipes except the condensate drain, I said twice there was none. **Both checks
were real and both answered the wrong question**: the named systems `PCD 3`, `CDP 1` are genuinely
empty, and pipe TYPE names carry no service. The service lives on the element's **System Type** -
**105 Refrigerant Condensate, 103 Refrigerant Supply, 63 Condensate Drain** out of 271.

He was looking at them on screen. Row 114, and the rule: **"zero on the named system" is not "zero of
that system type"** - 688 MEP elements in that model sit on no named system and every one still
carries a type.

## What is still owed

| | |
|---|---|
| **Seven crossings** | ~~duct size, pipe diameter, an element's workset, what is missing its Mark, views on a sheet, insulation thickness, selecting pipes in a view~~ **MEASURED 2026-09-19, and it is not seven — [FRAGMENT-ISSUES rows 144 and 146](FRAGMENT-ISSUES.md).** Five are still live; *pipe diameter* and *what is missing its Mark* did not come back; and **three this row never named are crossings today**, one of them *"how high is this off the floor"* answered by MOVING the thing. Of the eleven live, **seven have an owner already in the library that never declared the sentence** — named per sentence in row 146 — and **four have no READ to give them to at all**: nothing reports an MEP element's size, an element's workset or an insulation thickness, and `SET_VIEW_SCALE` is the only capability in the library with SCALE in its name. **THAT LAST SENTENCE WAS TOO STRONG AND IS CORRECTED 2026-09-19** ([row 158](FRAGMENT-ISSUES.md)). Row 146 enumerated the capabilities with SIZE, WORKSET, INSULATION or SCALE **in their names** and was right that none of them reports the value; *"no READ to give the sentence to at all"* is a bigger claim and does not follow. `READ_ELEMENT_PARAMETERS` reads one named parameter off every element and **declares *"get the size parameter"*** - SIZE nowhere in its name, and a candidate owner for three of the four. **One is certainly a gap:** a view's scale is not an element parameter, and `SET_VIEW_SCALE` is still the only capability with SCALE in its name. **Try the existing READ before writing a new fragment.** **AND TEN MORE WERE FOUND THE SAME DAY, DECLARED IN WRITING** - `SET_VIEW_SCALE` has *"what scale is this view"* in its own `utterances:` block, so it wins by `identity` and no re-ranking repairs it. `python tools/check-declared-questions.py` lists all ten and runs on every pull request |
| **Five ambiguous phrases** | ~~all read-vs-read, so none is dangerous~~ **IT IS SIX AND TWO ARE WRITE-VS-WRITE — [row 134](FRAGMENT-ISSUES.md), and both were judged 2026-09-19.** *"change the insulation thickness"* belongs to `SET_MEP_INSULATION`: the other claimant edits a wall, floor, roof or ceiling **TYPE**, so every element of that type in the project changes, where `set-mep-insulation` edits what was handed in. *"duplicate with detailing"* is far smaller — both claimants are right and the phrase carries no NUMBER. **Neither was edited**; all four files are under `brain/fragments/`, and the exact one-line repairs are written into row 134. **BOTH ARE REPAIRED NOW, 2026-09-20** - the four one-line edits were applied as written, and read from disk either side of them **6 phrases claimed twice became 4**, every one of the four READ against READ. No proof was staled: the fingerprint walks `impl/` only and cannot see `utterances:`. **Live on this machine already** - the shared store reports `built_from` = the worktree that made the edit, and reading it back, *"change the insulation thickness"* now returns `FRG-MEP-007` alone. **It reverts the moment a tree without the edit indexes next**, because an utterance edit leaves the ID set unchanged and the drift guard cannot see it - [row 131](FRAGMENT-ISSUES.md) |
| ~~**Row 117**~~ **DONE, and this line was still asking for it 2026-09-19** | ~~a write that renamed nothing said *"the model was CHANGED"*~~. **[Row 117](FRAGMENT-ISSUES.md) reads FIXED**: `WithVerdict`'s `applied` branch now says *"'X' was KEPT: Revit accepted the transaction rather than rolling it back"* and sends the reader to the counts already in the same reply, with a test that fails on the old wording. It landed in [PR #198](https://github.com/Ajmalpshaik/Heron-AI/pull/198) and this table was never told. **That is [row 153](FRAGMENT-ISSUES.md)'s shape in the file the owner reads first** - a list of what is owed is a cache with no invalidation, and nothing sweeps it. Kept struck through rather than deleted, because a line that vanishes teaches nobody why it was here |
| **12 of the 24 sentences that REACH, Heron says it found nothing for** | Measured 2026-09-19 across all 43 skill utterances ([row 157](FRAGMENT-ISSUES.md)). 24 reach a capability their skill declares - and for **half of them** the retriever's own note says *a coin toss*, where neither route preferred the winner, or that the words route *ranked the library rather than selecting from it*. **Both crossings carry one too.** The plainest case is Heron's own opening example: *"select all the ducts"* lands on `SELECT_BY_CATEGORIES` correctly, from a route with no claim on the sentence, while *"show me the ones you found"* - the one a fragment DECLARES - carries no complaint at all. **That is the argument for declaring a sentence rather than ranking for it, in data**, and the work owed is declaring them. It needs no Revit |
| **`set-wall-constraints`** | still the one STALE signature. Predates this sitting and was not touched |

## Two things about the machine, not the code

**The Revit ribbon button and the Claude app restart different halves.** The ribbon reconnects the
bridge; only restarting the app reloads `mcp/` Python. Three stale `heron_mcp_server` processes ran
for hours with fixed code sitting unused on disk, which reads exactly like the fix not working.

**`.agents/skills/` came back and the `.gitignore` tripwire caught it**, exactly as its comment says
it would. Five of its six files were byte-identical to `.claude/skills/`; the sixth differed only in
paths pointing at `.Codex/`. It was deleted, and `test_change_gate.py` went green the moment it was -
that untracked folder was the only red suite on the board.

## The owner answered the last four questions, and the way they were asked is the finding

**`Q-51`, `Q-54`, `Q-55` and `Q-56` close as [D-81](DECISIONS.md) to [D-84](DECISIONS.md).** The
register, which **until 2026-09-20 said 52 answered / 4 open**, now reads **56 / 0**. The owner
queue goes **144 to 140**, decisions waiting on
him **25 to 21**, and [OPEN-QUESTIONS](OPEN-QUESTIONS.md) **drops out of the queue entirely.**

**All four were put to him at once and dismissed. The same four translated into Malayalam, at his own
request, were dismissed again. Asked ONE AT A TIME, in plain words with a worked example each, he
answered all four in minutes** - and asked for `Q-51` to be re-explained *in English with a different
example* before answering it. **The language was never the barrier. The batch was**, and that is worth
more than the four answers: this register has carried unanswered questions for months while the way of
asking went unexamined.

**Two of his answers were better than any option offered**, which is the argument for asking rather
than choosing a default on his behalf:

| | |
|---|---|
| **`Q-54`** | The question offered cloud opt-in **per scope**, as [D-24](DECISIONS.md) frames it. He drew the line by **what the thing is** instead - all documents may go, **models and families never** - and asked for the earlier position to be changed outright. [D-82](DECISIONS.md) supersedes D-24's framing, not its caution |
| **`Q-55`** | The question offered the read-time ceiling - a count and a clause number, never a clause. **He declined the ceiling**: he wants the **prior practice by name**, *"in Project A you used 700mm, do you want the same here?"* [D-83](DECISIONS.md) |

`Q-51` took shape 1 - **carried text is stamped, not scanned**; Heron attributes rather than asserts.
`Q-56` took the cheapest honest answer - **nothing more than today, and the word "sandbox" goes**,
which makes the **rename the deliverable** rather than a tidy-up.

**Two things he asked while deciding are recorded inside [D-82](DECISIONS.md)**, because the answers
bound the decision. *"More context means no hallucination - am I right?"* - half right, and
[row 114](FRAGMENT-ISSUES.md) is the counter-example from this repository. *"It will not go to GitHub -
am I right?"* - correct, and **verified against `.gitignore` rather than asserted**: the store lives
outside the repository, the databases are ignored, and the SOURCE documents are blocked by extension.

### Two suites that looked red were the sweep, not the code

`check-gaps.py` reported `test_brain_reachable.py` and `test_docs_guard.py` as **FAIL**, and **both
pass when run on their own** - the first takes 103s inside a serial sweep. **That was told to the owner
as "2 failing tests" before it was checked, and corrected in the same session.** Run a suite alone
before reporting it red.

### And `check-docs.py`'s exit code is not what `| tail` reports

`python tools/check-docs.py | tail -20; echo $?` prints the exit of **`tail`**, which is always 0. The
gate was read as green while it was **exiting 1**. Redirect to a file and read `$?` from the command
itself.

### The four counts that stayed at 52 were quotations, not drift

`check-docs.py` gates on every stated count **including the archive**, and it carries a deliberate
exemption for a figure **quoted as history** - `used to`, `it said`, `until 20XX`, `superseded`. All
four stale lines were genuine quotations **missing the marker that says so**, so they carry one now
rather than being rewritten. **The archived handover keeps its title** and gains *"superseded"*. The
marker is load-bearing by design: *"Write 'it said 14 answered' and this stays quiet."*

### The balance as it stands tonight

The owner asked for this in one table, and asked that it be given in this shape whenever he asks again.

| | Balance |
|---|---|
| **Skill runner** | **1 - does not exist.** [Row 141](FRAGMENT-ISSUES.md) - the only real build item, and the blocker for every skill proof |
| Skills proved | **0 of 10** |
| Fragments proved | **328 of 395** - 67 owed |
| Revit agents proved | **15 of 36** - 21 owed. The other ~214 never touch a model and are covered by passing suites |
| Agents to build | **2** - both Documentation, parked until the first release tag, blocking nothing |
| Open defects | **38** of 144 |
| Waiting on the owner | **140** - 21 decisions, 21 proposals, **0 questions** |
| Platform, add-in, install, rollback | **Nothing owed.** Loads on 2020, 2024 and 2027 |

> **THE TABLE ABOVE WAS TRUE WHEN WRITTEN AND FOUR ROWS HAVE MOVED SINCE, 2026-09-20 LATER THE SAME DAY.** Derived, not typed - run the commands in
> [work-notes/BALANCE-OF-WORK.md](work-notes/BALANCE-OF-WORK.md) rather than believing either version.
>
> | | Was | Is |
> |---|---|---|
> | Fragments proved | 328 of 395 | **329 of 395**, 66 owed |
> | Open defects | 38 of 144 | **33 of 163** - the register grew by 19 rows and more closed than opened |
> | Waiting on the owner | 140 | **146** |
> | Questions | 0 | **1** - `Q-57`, *may a fragment or skill that WRITES declare a question* |
> | `set-wall-constraints` STALE | the one stale signature | **none is stale.** It reads PROVEN and `check-signatures.py` says nothing is waiting; `create-roof` is HELD on purpose |
>
> **AND THE ONE THAT IS NOT A COUNT: FOURTEEN QUESTIONS ARE ANSWERED BY SOMETHING THAT WRITES**, measured twice by two independent routes and named one by one in
> [FRAGMENT-ISSUES row 116](FRAGMENT-ISSUES.md). Asking the diameter of a pipe CREATES a pipe; asking to select the pipes in a view CAPS THEIR OPEN ENDS; asking
> which workset something is on CREATES a workset. Every one of the fourteen has a READ sitting beside it that lost, so *there is no READ to give the sentence to*
> is disproved - but the right owner is obvious for some and wrong for others, so none was declared and the choice is still his.

**Safe on a live project today: 144 proven `READ` fragments** - that number read **114** in this file's
own cold-start instructions until 2026-09-20, understating by thirty what the owner was allowed to use.
Derive it, never read it: `grep -h '^heron-status:' brain/fragments/*/fragment.yaml`.
