# Heron AI — Session Handover

## If you are the owner, starting your PC — say this and nothing else

> **"Read HANDOVER.md in Heron-AI and carry on."**

That is enough. This file is the memory; nothing else has to be remembered or repeated. It names the
branch, the numbers, the decisions, and what comes next. Two things worth adding **only if they apply
that day**:

- **"Revit is open"** — that unlocks the whole proving pass, which is the one thing months of work here
  have been waiting on. Without it a session will correctly keep building instead.
- **"Work only on Heron-AI and AJ-Tools"** — the standing scope. `AJ-AI-Brain` is the earlier project
  and is **read-only reference**.

If a session ever tells you something that disagrees with `python tools/check-gaps.py`, **believe the
tool.** It is computed from disk every time; this file is typed by hand.

---

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
([D-44](docs/DECISIONS.md): none of them inherits the earlier library's proven status). For whoever picks
this up next: a fresh Claude session, a person, or the owner on his phone.

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

**As of 2026-08-31 it reports 0 unfinished and 55 waiting**, over **63 fragments and 0 orphans**.
Everything waiting needs a machine or a conversation — a real Revit, a Windows box, an egress policy, or
the owner. **Read the figures off the tool; the ones in this paragraph are already a day old by the time
you read them.**

> Those figures were **read off the tool, not carried forward**, and the sentence they replace shows why
> that matters: it said *55 waiting* and then listed parts summing to 54, and it still counted **3 owner
> items** after `R1` and `R2` had been struck off in the same session. A total and its own breakdown
> disagreeing is the cheapest possible drift to catch and it survived anyway. Re-derive rather than
> edit the digits.

**`A8` is new and it is the honest half of that work.** The three MCP tools were written on a machine
with **no MCP SDK installed**, so `FastMCP` has never served them. The test underneath them passes and
then reads the server *as text* — which proves they are declared, never that they appear in a host. Two
minutes at the PC clears it.

**If the tool and this file ever disagree, believe the tool.** It is computed from disk; this file is
typed. That is not a hypothetical — for most of 2026-08-29 this tool reported `UNFINISHED - nothing`
while the entire Phase 2 brain sat unreachable, because it checked that each step's module and test
existed and never asked whether anything called them. The check that catches it was added the same day.

### Then, in order

| | What | Where it happens |
|---|---|---|
| **1** | ~~`R1`~~ **DONE 2026-08-29 — all 21 read back, all 21 confirmed, nothing moved.** What is left of the review is **`R1b`**: show him the trust model working with his own fragments in it, because [D-14](docs/DECISIONS.md) stays *Proposed* until he has seen it | Needs a screen |
| **2** | **`B1` to `B4`** — open Revit 2020, look for the **Heron AI** tab, press **Heron**, then `ping` and `count` | Needs Revit |
| **3** | **`C3`** — with `write.enabled` still **false**, ask for a move and watch it be **refused, by name**. Prove the gate before testing the write, or a passing move proves nothing | Needs Revit |
| **4** | **`D1`–`D3`** — *"move the ducts up 200 mm"*, say yes, then **MEASURE ONE**. The single most important line in the whole register | Needs Revit |
| **5** | **`D5`** — **one** Ctrl+Z puts it all back. Two means Golden Rule 16 is broken | Needs Revit |

Everything else in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) follows in the order written there. It is in
dependency order on purpose: nothing in group D can be attempted before group A passes.

### And two that need no Revit at all

- **`A7`** — the trained embedding backend has **never run**, and after 2026-08-30 the row asks for
  something narrower than it used to. `pip install --user model2vec` **works** — that half was tried
  here and succeeded, so it is no longer a variable. What fails is the **weights fetch**:
  `huggingface.co` answered **403 to CONNECT**, an egress-policy denial, in a *second* container that
  had nothing to do with the first. So `A7` needs **a machine whose network policy permits the model
  host**, not merely a machine with a network — most likely the owner's own PC. Until it runs, search
  finds words and not meaning, and [`tests/test_embed.py`](tests/test_embed.py) says so in measured
  numbers.
- **`A4` and `A6`** — both need Windows but not Revit. `A4` is the Windows named pipe itself; `A6` is
  thirty seconds confirming the SDK probe reads Windows correctly.

### The one thing that was buildable here — now built

**The brain was wired to nothing, and now it is wired.** Eight modules, seven fragments and ten skills
were on disk, tested and passing, with **no MCP tool reaching any of them.** The host talks to Heron only
through the tools in [`mcp/server/heron_tools.py`](mcp/server/heron_tools.py), and every one of them went
straight to the bridge. That left Phase 2's third definition-of-done clause open for **no external reason
at all**, which is why it was worth doing on a machine with no Revit.

**What was added**, all of it read-only and none of it touching a model:

| | |
|---|---|
| [`mcp/server/heron_brain.py`](mcp/server/heron_brain.py) | The one seam between the MCP side and `brain/`. Opens the store, rebuilds it if the machine is fresh, indexes if stale, and hands back rows. **It holds no knowledge of its own** |
| `heron_capabilities` | What Heron knows how to do: ten jobs, which have every part provided, and the seven capabilities nothing provides |
| `heron_resolve` | Who can do one capability, at what risk, on which releases — **asked for by capability, never by fragment id** |
| `heron_lookup` | The user's own sentence resolved to a **capability**, with the provider underneath as evidence rather than as the answer |
| [`tests/test_brain_reachable.py`](tests/test_brain_reachable.py) | Step 12's acceptance test re-run through the seam: add a better provider and the call site is the same line; delete the original and it still answers |

**Two things it deliberately does not do, and every answer says both out loud:**

- **Resolving is not running.** A fragment carries C# in `impl/`, the bridge speaks a fixed set of
  operations, and none of them compiles one — [D-28](docs/DECISIONS.md)'s in-process Roslyn is unbuilt.
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

**So the library is 32 fragments and every skill has every capability provided.** The seven were written
on 2026-08-29; twenty-five more followed. `python brain/heron_skill.py` shows no gaps.

**And his second instruction is the one that must not be quietly undone.** None of them inherits the
earlier library's proven status, however well proven it is there:

> *"Even in the aj ai proven fragment dont mark in heron this is proven because we will check each and
> everyone again in heron ai so mark it as a not proven in heron."*

That is [D-44](docs/DECISIONS.md), and it is **enforced rather than remembered** —
[`brain/heron_fragment.py`](brain/heron_fragment.py) refuses a status above `DRAFT` whose proof does not
match the implementation in front of it. The gate had existed and nothing stood on it: a fragment
declaring `PROVEN` on another model's proof passed every check in this repository, which was measured by
writing one.

**What that buys, and what it does not.** Thirty-two fragments compile on all eight releases and none of
them will fail at the PC for a reason a compiler could have found. Not one has met a model. The debt did
not go away — it got **counted**, which is the whole point of `check-gaps` keeping *unfinished* and
*waiting* in two lists that must never be one.

---

## The ninth session, 2026-08-31 — the library, built out; and four things it found on the way

**What it did:** carried out the owner's decision recorded as [D-45](docs/DECISIONS.md) — *build the
whole library now, prove it against a real Revit later in one concentrated pass*. **32 fragments → 63**,
every one compiling on all eight releases, every one carrying a proof spec with a negative case, none
of them proven against a model. That last clause is the deal D-45 struck, not an oversight.

His instruction, in his own words, is the one to keep: *"don't copy-paste from the old library. Check,
study, and split if you want to split — or whatever you want to do, do it as per our project
specification."* Every fragment here was re-authored against that, and several were **split** because
one source file was doing two jobs.

### What was built

| Block | Fragments |
|---|---|
| Sheets & views | `FIND_SCHEDULES`, `ADD_SCHEDULE_FIELDS`, `FIND_VIEWS_SHOWING_ELEMENT` |
| Reporting | `REPORT_LEVEL_ELEVATIONS`, `MEASURE_ROOM_DIMENSIONS`, `MEASURE_CEILING_HEIGHT`, `COMPARE_ELEMENTS`, `REPORT_ELEMENT_DEPENDENCIES`, `REPORT_MATERIAL_TAKEOFF`, `REPORT_EXTERNAL_REFERENCES`, `REPORT_ELEMENT_OWNERSHIP`, `READ_MODEL_FILE_INFO` |
| MEP | `MEASURE_RUN_QUANTITIES`, `REPORT_CONNECTOR_LOADS`, `REPORT_SPACE_AIRFLOW` |
| General | `SUM_BY_GROUP`, `GROUP_BY_ASSEMBLY`, `FIND_NEAREST_ELEMENTS` |

`READ_ELEMENT_PARAMETERS` went to **v2** and `MEASURE_ROOM_DIMENSIONS` to v2 as well. The first is
worth knowing about: it asked `LookupParameter` and nothing else, so it answered **`absent`** for a
duct's Level and for every parameter held on the *type*. Across a mixed set that read as *"most of this
model has no level"* — not a thing that can be true.

### Four findings, and they matter more than the count

1. **A question can route into a fragment that writes.** `check-routing.py` listed every contested
   sentence in one flat list. It now compares the two fragments' **risk** levels and prints the
   ladder-crossings separately. The first run found six, four invisible until that moment —
   *"follow the pipe"* was reaching `OFFSET_ELEMENTS`, which does not return a wrong answer, it
   **shifts the run sideways**. → [D-47](docs/DECISIONS.md)

2. **The graph could not express an action fed by two fragments.** [D-46](docs/DECISIONS.md) had left
   that open, on the condition that a real case decide it. A takeoff is that case. The fix made the
   orphan check **stricter**, and the stricter message immediately found a second defect in the
   fragment's own contract. → [D-48](docs/DECISIONS.md)

3. **A need may now bind to a differently-named provide.** `FIND_NEAREST_ELEMENTS` needs two sets of
   elements and only one can be called `elements`. `binds:` renames and never converts. →
   [D-49](docs/DECISIONS.md)

4. **A retrieval regression, and a hypothesis of mine that was wrong.** `test_retrieve.py` failed for a
   real reason: at 63 fragments the tracked query's shortlist holds fragments with **no claim** on it. I
   suspected my own long purposes (21–523 words, all indexed) — **measured it, and the correlation is
   0.313**, weak. The shortest purpose in the library is among the worst intruders and one of the
   longest intrudes on nothing. `tools/check-intrusion.py` now measures this repeatably. The assertion
   was **withdrawn rather than narrowed a third time**; `brain/retrieval-history.md` carries what
   happened.

### Three traps found by the gates, worth not re-learning

- **`LinkedFileStatus.NotLoaded` does not exist.** The enum has seven members and **three** mean
  *deliberately not loaded* — `Unloaded`, `LocallyUnloaded`, `InClosedWorkset`. A two-way split on
  *"is it Loaded"* puts three false alarms on a handover checklist.
- **`Room` and `Space` live in namespaces the fragment wrapper does not import.** Tell them apart by
  **category**, not by a type test.
- **`check-structure.py` fires on `Autodesk.Revit` written in a COMMENT.** It is right to: a substring
  match cannot tell comment from code. Reword the comment; do not weaken the checker.

### One fragment could not use its source's method, and that was correct

`MEASURE_CEILING_HEIGHT`'s source raises the room's Upper Limit, measures, and rolls it back — because a
room's solid stops at that limit and so intersects no ceiling on most real models. **Golden Rule 16
forbids that here:** a fragment runs inside a transaction it did not open, so a change-and-rollback
lands inside somebody else's batch and takes its undo entry with it. A read that quietly writes is worse
than a read that returns less. It tests plan overlap instead, and the cost of that choice is written
into the fragment rather than left to be discovered.

### What is left of the library

Creators (~36), structural changes (~33), QA checks (~30), filters/params/graphics (~60). **The 44
recipes become skills, not fragments.** Same method each time: study the source, state what was decided
differently and why, compile on all eight releases, write the proof cases with a negative case, run
`check-routing.py` and `check-intrusion.py`, then the full checker sweep before committing.

### Register

**`A9` added.** `check-api-surface.py` reports `Autodesk.Revit.UI.ItemData` **missing on 2020–2024**
while the add-in compiles clean on 2020. One of those is wrong and nobody has established which. It
**exits 0**, which is exactly how a real finding gets ignored for months.

---

## The eighth session, 2026-08-30 — the checker was right, and nobody was reading it

**What it set out to do:** start the remaining work. **What it found first:** by the register, there
is none — `check-gaps.py` reports **0 unfinished and 54 waiting**, and every waiting item needs a
real Revit, a Windows machine, an egress policy, or a conversation. The build order ends at Step 14
and there is no Step 15. So the session did what the sixth and seventh did before it: went looking
for **what the checker cannot see**.

### The front door was three versions stale, and one checker had been printing it all along

`docs/README.md` — the map a new session is *told to read first* — opened with this:

> *"**Step 1 of 6** — the bridge — is built and proven in real Revit 2024... Steps 2–6 have not begun:
> **no Revit API call exists anywhere in the repository.**"*

Steps 1 to 5 are proven, Step 6 compiles on eight releases, Phase 2 is built in full, and
`check-api-surface.py` verifies **103 Revit members** across 2020–2027. Every clause of that sentence
was false. `README.md` was no better: it had stood describing the Constitution as *"pending confirmation"* two days after acceptance, and Step 6 as **"Built, never compiled"** — which understates the work, and is the rarer and
more corrosive direction for a status line to be wrong in, because nobody re-checks a claim that
flatters them less than the truth.

**The question count was the one that gave the game away.** `README.md` used to say *"14 answered, 26 open."*
That is not merely stale — it is **the exact sentence `docs/OPEN-QUESTIONS.md` records as having been
caught and corrected on 2026-08-28**, when the real figures were 20 and 20. The fix was applied to the
file the checker enforces and to no other. The identical claim then sat in the README for two more
days while `check-docs.py` **printed it on every single run**, under a heading that reads
`COUNT CLAIMS (verify by hand)`.

> **Nobody verifies by hand.** The script's own comment says so, in writing, about a different file:
> *"Section 5 above prints count claims and says 'verify by hand'. Nobody does, which is how the
> Progress line came to say 14 answered - 26 open"* — a line that no longer says it. The diagnosis was correct, it was written down,
> and it was applied to one sentence in one file. **A checker that reports is not a checker.**

### So the reporting was turned into enforcement — section 7

[`tools/check-docs.py`](tools/check-docs.py) now derives the truth once and enforces it against **every
markdown file in the repository**, and it fails. Five real drifts on its first run: two counts in
`README.md`, one in `docs/README.md`, and the Constitution's status in `README.md` and
`docs/PROPOSALS.md`. All five are fixed; the checker is green.

**A sixth was found by hand, in the wreckage of fixing the fifth** — and it is the one the new check
still cannot catch. `docs/README.md` described the register as **"52 items in dependency order"**; it
holds **59** rows today. That number is not wrong so much as *dated*: it was true when Q-14 was
answered on 2026-08-28. It now reads as a description of the register rather than of a moment, so it
has been marked as the figure of that day, with a pointer to the tool that knows the current one.
**Enforcing register counts was considered and not done**, for the same reason fragment counts were
not: the sentence is legitimately historical, and a checker that cannot tell a dated figure from a
live claim would demand the wrong fix.

**Two false positives were found by running it rather than by reasoning about it**, which is the only
way either would have surfaced:

| | |
|---|---|
| `"Q-24 answered"` | A question **id**, not a count of twenty-four. Hence a lookbehind — and a `\b`, because without one the pattern re-enters the number at its second digit and reads the trailing **4** as a count of its own |
| `"Revit 2023 and Revit 2025 open at the same time"` | Two release numbers, not two open questions. So the *open* count is only ever read from a line that already carries an *answered* claim, which is the shape a progress sentence actually has |

**And one deliberate exemption, which is the interesting one.** A superseded figure **quoted as
history** is correct writing, not drift — `OPEN-QUESTIONS.md` records what its line used to say, and
`DECISIONS.md` records what the Constitution's status used to be. Both must stay. Lines carrying a
history marker (*"used to"*, *"it said"*, *"had stood"*, *"until 20xx"*) are skipped, **which makes the
marker load-bearing**: write *"it said 14 answered"* and the checker stays quiet, write *"14 answered"*
and it fails. **The marker has to sit on the same physical line as the figure**, since the check reads
lines — that cost this session two rounds, once on a sentence whose marker had wrapped onto the next
line, so wrap accordingly or the checker will tell you.

**That exemption was validated in both directions before its green was believed**, the same way the
`CreationGUID` defect was put back to test the compile gate: the history marker was stripped from
`OPEN-QUESTIONS.md` line 12 in a throwaway copy, and the checker **caught the line it had just been
silent about**. The exemption is doing work, not hiding a miss.

### `A7` was attempted, and it failed better than last time

The trained embedding backend needs a model host. **`pip install --user model2vec` succeeded here** —
PyPI is reachable, sixteen dependencies installed clean — so that half is settled and no future session
need spend a run on it. The weights are the wall: `huggingface.co:443` answered **403 to CONNECT**, which
the proxy itself records as a policy denial and its own documentation says to report rather than route
around.

**This is the second container to fail `A7`, for a second distinct reason** — 2026-08-28 was
`connect_rejected` at a different layer. That is what turned the row from *"any machine with a working
network"* into *"a machine whose egress policy permits the model host"*, which is a much rarer thing and
points squarely at the owner's own PC. Worth knowing before trying again: the `sentence_transformers`
fallback fetches from **the same host**, so it is not a second route.

### Then the C# was compiled and the bridge was actually run, in this container

**The .NET SDK is not on a fresh box, and the distro packages it** — `apt-get update` (the index is
stale and the first install 404s without it) then `apt-get install -y dotnet-sdk-10.0`, which is what
[docs/30](docs/30-compiling-away-from-windows.md) already said. From there, two things that had been
*described* here were *done*:

| | |
|---|---|
| `check-compile.py` | **All eight releases, all four projects, zero warnings** — 2020 through 2027, reproduced independently in a fresh container rather than inherited from the sentence that claimed it |
| `test_bridge_roundtrip.py` | **Built and run for the first time on this machine.** 30 checks, the whole lease among them. This is the 17th suite; with it the repository stands at **17 of 17 passing, 544 checks** |

**Getting there cost two failures, and neither was a defect** — both are written up under §5 with the
corrected commands. One was the shared `bin/x64/Debug` folder that `check-compile.py` fills first; the
other was that the SDK this file tells you to install carries **no .NET 8 runtime**, so the net8.0 host
cannot start.

**That second one produced a real, small fix.** The test reported it as *"host never reported a pipe
name"* — the symptom of every possible cause, including a genuinely broken bridge. It now keeps what the
host actually said and names the runtime case outright, with both remedies. Validated in both
directions: it still passes with the runtime present, and it prints the new diagnosis with it absent.
That is the same class as the four tools the seventh session caught **answering when they should have
declined**.

### The checkers were then audited for the same defect this session started with

If a checker that prints without failing let five drifts survive, the obvious question is how many other
checkers do that. **Answer: none.** Every tool in `tools/` either fails on what it prints, or is a
generator, or is `check-routing.py`, which never fails **on purpose and says so**. `check-metadata.py`
goes further and guards the case that worries this repository most — a check that has quietly stopped
guarding anything — by *raising a problem* when the claim it verifies has been reworded away, rather
than passing an empty pass.

**So the defect was real and isolated, and the audit is recorded as a negative result rather than
inflated into findings.** One thing did change: section 5 of `check-docs.py` no longer calls itself
*"verify by hand"*, because that heading is what the section 7 story is about — it now says which
section actually enforces.

### What was deliberately NOT done

- **No fragments were added.** [31 §4](docs/31-studying-the-existing-libraries.md) is explicit that this
  is *"not a race to a number"* and that after Phase 2 the library grows **when a real job needs
  something**. Thirty-two DRAFT fragments and no model to prove them on is not improved by making it
  sixty.
- **No executor.** [D-28](docs/DECISIONS.md)'s in-process Roslyn is what *"resolving is not running"*
  refers to, and it is **Phase 5** on the roadmap. Three phases early is not initiative.
- **No fragment or skill counts were written into the front-door documents.** They would be two more
  unenforced numbers, in the very files this session had to correct for carrying unenforced numbers.
  `check-gaps.py` prints them, computed. Enforcing them in prose was considered and rejected for a
  concrete reason: [31 §1](docs/31-studying-the-existing-libraries.md) legitimately states the *other*
  library's **398**, and a naive count check would call that drift.

---

## The seventh session, 2026-08-30 — the library, and two bugs it found

**What it did:** took the fragment library from 7 to **32**, re-authored from the owner's earlier Brain
under [D-25](docs/DECISIONS.md) — studied and rewritten, never copied, split where a 243-line original
was really two jobs. All 32 compile on all eight releases. All 32 are `DRAFT` ([D-44](docs/DECISIONS.md)).

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
[`tools/check-routing.py`](tools/check-routing.py) asks every fragment its own declared words back to the
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
[`brain/skills/mep-grayout.yaml`](brain/skills/mep-grayout.yaml) as a decision for him, not a default
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
> run, both of which reading had already missed twice ([docs/30](docs/30-compiling-away-from-windows.md)).
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
> rather than a gate**, and [27 — Build Order](docs/27-build-order.md) carries **Steps 7 to 14**, written
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
> had no `utterances`, so `OST_DuctCurves` matched nothing — [09 §2](docs/09-skills-and-fragments.md) had
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
> [D-40](docs/DECISIONS.md) applied. Risk in particular — it already has two homes (the tool registry
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

Then read [docs/README.md](docs/README.md) for the map and
[docs/27-build-order.md](docs/27-build-order.md) for what to build.

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
remembering them.** The **lease** ([D-22](docs/DECISIONS.md)) — Step 5 deferred it to *"Phase 1 with
writes"* and Step 6 is that write; a second chat is now refused instead of cutting the first off mid-job.
The **Failure Analysis Agent**, which never blind-retries and fails closed. The **tool registry**, so risk
is declared in a table rather than as a literal buried in the write path. And **configuration and health**,
which between them exposed two silent bugs — `write.enabled` could never have been switched on, and the
two halves disagreed about how long to wait.

**All 21 Golden Rules are now official** ([Q-19](docs/OPEN-QUESTIONS.md), accepted 2026-08-28) — including
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
fragments, ten skills, **nine** new test suites, and [`tools/check-gaps.py`](tools/check-gaps.py) — the
sweep that looks for what is missing rather than waiting to be told. The ninth suite is
[`tests/test_brain_reachable.py`](tests/test_brain_reachable.py), and it arrived last with the seam that
made any of the other eight reachable from a conversation.

**Phase 2's own definition of done is NOT met, and only two thirds of the reason is the missing Revit.**
The [build order](docs/27-build-order.md) states it as three clauses: *ten real skills work, none
hard-coded; a re-authored capability carries its own proof; and the Orchestrator resolves through
capabilities rather than agent names.* The first two need a model — all ten skills and all seven
fragments are `DRAFT`, and [D-30](docs/DECISIONS.md) promotes on a proof containing a negative case.

> ### The third clause was not blocked by anything — and is now done
>
> **RESOLVED 2026-08-29, later the same day.** [`mcp/server/heron_brain.py`](mcp/server/heron_brain.py)
> is the seam and three read-only tools stand on it — `heron_capabilities`, `heron_resolve`,
> `heron_lookup` — each asking for a **capability** and never for a fragment. `check-gaps.py` now reports
> **nothing unfinished**. What follows is the finding as it stood, kept because the *way* it was missed
> matters more than the fix, and because two limits it names are still true: **resolving is not running**
> (there is no executor, [D-28](docs/DECISIONS.md) is unbuilt), and the tools have **never been served by
> a real `FastMCP`** — that is `A8` in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md).
>
> **The brain is wired to nothing.** Found on 2026-08-29 by running `tools/check-metadata.py` and
> following what it said. The Orchestrator is **the host's job, not Heron's** — `HERON-ORC-MAIN-001` is
> listed as host-provided under [D-01](docs/DECISIONS.md) and
> [docs/02 §7](docs/02-architecture-overview.md), which is correct and deliberate. But the host reaches
> Heron **only through MCP tools**, and the seven tools in
> [`mcp/server/heron_tools.py`](mcp/server/heron_tools.py) are all `revit_*` — every one goes straight
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
[D-14](docs/DECISIONS.md) stays *Proposed* until he has seen the trust model working. They are Accepted and are being built on; the review confirms each still says what he meant
and fills in detail left out. See the block at the top of [DECISIONS.md](docs/DECISIONS.md).

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
| Newest connection wins — **the pipe only, since Step 6** | *"A newer connection took the session"*, older one dropped. Still true of the transport; a lease now decides who may actually send anything ([D-22](docs/DECISIONS.md)). **The lease itself is unproven** |
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
| **32 fragments, all `DRAFT`** | `DRAFT` is not a shortcut — it is [D-30](docs/DECISIONS.md) being obeyed. A fragment is promoted by one recorded proof **containing a negative case**, and a negative case needs a model. They stay DRAFT until then, and they are the reason `check-gaps` lists **thirty-two** items under *needs a real Revit*. **Since 2026-08-29 all of them at least COMPILE** — on all eight releases, 2020 to 2027 ([`tools/check-fragments-compile.py`](tools/check-fragments-compile.py)). That is not behaviour, but it does mean none of them will fail at the PC for a reason a compiler could have found |
| **10 skills, all `DRAFT`** | Each names **capabilities and never fragments**, and each carries the words Ajmal actually says rather than the words the technique is named after. Whether any of them does what it says is unknown |
| **The trained embedding backend** | **The highest-value item that needs no Revit, and 2026-08-30 sharpened what it buys.** [`brain/retrieval-history.md`](brain/retrieval-history.md) tracks one query across eight library sizes (7 → 32) *and* now measures a second way: every fragment's own declared words asked back to the search — 169 sentences, **words 92% first and 100% in the top three, nearness 60% and 82%**. That corrects the older headline in this file's own history: the nearness route has **not** collapsed in general. It handles **vocabulary overlap** and fails at **disambiguation**, which is why the tracked query — a sentence several fragments fairly claim — sits mid-library while a sentence naming one fragment comes back first. So `A7` should be expected to change the **contested** lookups, not every lookup. Never run — `huggingface.co` is refused by this container. The backend that *is* running is character n-grams, which measurably does not do synonyms (`diffuser`/`grille` scored −0.136). Needs no Revit and no Windows |
| **The three brain MCP tools** | `heron_capabilities`, `heron_resolve`, `heron_lookup` — and the seam under them, [`mcp/server/heron_brain.py`](mcp/server/heron_brain.py). The resolution beneath them is tested and passes; **`FastMCP` has never served them**, because the machine they were written on has no MCP SDK. `tests/test_brain_reachable.py` reads the server as *text* to confirm they are declared, which is the same technique `test_tool_registry.py` uses on the C# and has the same limit. `A8`, and it needs Windows rather than Revit |

### Does not exist at all

| | |
|---|---|
| ~~Any way for the host to reach the brain~~ | **BUILT 2026-08-29.** Three read-only tools on one seam. Moved to the table above, which is where an untested thing belongs |
| **Any way to RUN a fragment** | **The gap that wiring the brain up revealed rather than closed.** A fragment carries C# under `impl/`, the bridge speaks a fixed set of operations, and **none of them compiles or executes one** — [D-28](docs/DECISIONS.md) chose Roslyn in-process and it is not built. So a request now resolves all the way to *this capability, provided by that fragment*, and then stops. Every brain tool says so on every answer, because a host that inferred otherwise would build a plan that fails at its last step |
| ~~Seven capabilities the skills ask for~~ | **WRITTEN 2026-08-29**, taking the library to 14 that day and to **32** by 2026-08-30. **All ten skills have every capability provided** — `python brain/heron_skill.py` shows no gaps. Each one compiles on all eight releases and each carries proof cases with a negative case, **and not one has met a model**: they are `DRAFT`, which is what makes them the seven newest rows of Revit-checking debt rather than seven finished things |

> Nothing here is known-broken. Several things are **untested**, which is different and more honest.

---

## 4. The things that will bite you

Each of these cost real time. They are in the order they were learned.

1. **The Revit API can only be called from Revit's own thread, inside an API context.** An MCP server is
   a separate process and cannot call it at all. Everything marshals through one `ExternalEvent`.
   [docs/03 §4](docs/03-heron-revit.md)

2. **A named pipe needs `CreateNewInstance`, not just `ReadWrite`.** Without it only the *first*
   listener can be created and every retry fails with "access denied".

3. **The discovery file must never carry the document name.** Storing it produced the stale-name trap in
   the owner's earlier work. [docs/25 §2a](docs/25-multi-session-and-binding.md)

4. **"No reply" does not mean "dead".** Test the *process*, not the reply — and check it is still the
   right *program*, because Windows reuses process ids.

5. **An assumption is not a choice.** If one Revit was open, Heron *assumed* it. If the user answered,
   they *chose*. Conflate the two and a second Revit opening mid-chat silently sends everything to the
   first one. [docs/25](docs/25-multi-session-and-binding.md), and `tests/test_session_binding.py`.

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
    nothing" failure [D-30](docs/DECISIONS.md) exists to catch, in the one place Heron can actually
    change a model. Heron skipped **pinned** elements, which is one half of the case; a group member
    is not pinned and went straight through. `RevitWrite` now probes each position before and after
    and reports four outcomes — moved, partly, blocked, unverified — instead of the count it asked
    for. **Found by studying the owner's earlier library rather than by reading Heron's own code**,
    which had been read several times. `E10` is the check.

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

**17 suites, 544 individual `ok`/`PASS` lines, all passing** — derived, not typed:
`for f in tests/test_*.py; do python3 "$f"; done | grep -cE '^\s*(ok|PASS)\b'`. **That now includes
`test_bridge_roundtrip.py`**, which needs its host built first (below) and, on 2026-08-30, was built and
run here rather than described — 30 checks by the same count, including the whole lease.

> Those figures replace *"15 suites, 435 lines… the sixteenth takes it to 465"*, and the gap is the
> point: the counts were right when written and nothing recomputed them as the library grew. **A count
> in prose is a claim, and only `check-docs.py` section 7 enforces any of them** — it covers question
> counts and the Constitution's status, not these. Re-derive with the command above rather than trusting
> the digits in this sentence.

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
[docs/30](docs/30-compiling-away-from-windows.md)):

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
warnings. [docs/30 §2a](docs/30-compiling-away-from-windows.md) has the account and the two-directional
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
>   features. [PROPOSALS Part E](docs/PROPOSALS.md) is what that study produced.
> - **Never copy code or text out of them.** Understand the mechanism, then write it for Heron, in
>   Heron's shape, with Heron's reasoning. His words: *"study and use and make it part of our heron,
>   blindly copy paste dont do it."*
> - **Do not cite them as Heron's authority.** He stops using both once Heron is finished, so a note
>   whose argument is *"go read that other repository"* becomes worthless on that day. State the
>   reasoning here, in full, so it stands on its own.

Twenty-two are in [docs/DECISIONS.md](docs/DECISIONS.md). These are the load-bearing ones:

| | |
|---|---|
| **D-01** | Heron runs as a **Claude Code plugin** |
| **D-02** | **Named pipes**, per-PID. Local-only by construction — the add-in has *no network code at all* |
| **D-05** | **Revit 2020 → latest.** Never extrapolate the runtime table forward; an unlisted release is a build error |
| **D-06** | **C# for Revit, Python for everything outside it.** Settled again on evidence in [Q-39](docs/OPEN-QUESTIONS.md) |
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

**Golden Rules** — **21, all official** — are in [docs/14](docs/14-golden-rules.md). 16–21 cover undo,
preview-before-modify, sandboxing, permission escalation, document pinning and stale reads, and were
**accepted on 2026-08-28** ([Q-19](docs/OPEN-QUESTIONS.md)) *while Step 6 remained unproven*. That order
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
| **`R1b` — show him the trust model working** | [D-14](docs/DECISIONS.md) stays **Proposed**. He agreed the direction and said *"show me it working at the PC first."* Use the framing that landed: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may not be called settled |
| ~~Copyright~~ | **CONFIRMED 2026-08-28 — Ajmal PS is correct.** Checked consistent in all four places it appears: the Apache appendix in `LICENSE`, `NOTICE`, `<Company>` in `Directory.Build.props`, and `README.md`. The Apache appendix is filled in rather than left as the `[name of copyright owner]` placeholder, which is the one that is usually missed |

**And two choices that did not exist until Phase 2 was built.** Neither is a question the code is stuck
on — both are the owner's to make, and both were deliberately left rather than decided quietly:

| | |
|---|---|
| ~~Write the seven missing capabilities, or wait?~~ | **DECIDED AND DONE 2026-08-29 — he asked for the no-PC work to be finished.** All seven are written and compile on all eight releases. The argument that settled it: **not writing them was itself outstanding non-Revit work.** Written, what remains is their PROOF, and proof is Revit-checking — which is exactly the only thing his standing instruction wants left. **One question came out of the work and is still open**: whether a layout that Revit refuses part of should place the rest or roll back entirely (Golden Rule 16 says roll back; a modeller may well want the 37). See `place-family-instances` |
| ~~Wire the brain to the host now, or after the Revit checks?~~ | **DECIDED AND DONE 2026-08-29 — he chose to wire it now.** Three read-only tools on one seam; `check-gaps.py` reports nothing unfinished. It cost the register one new row (`A8`, two minutes at the PC) and moved no other row, so his standing instruction that *only Revit-checking should remain outstanding* holds: what remains is 47 Revit rows, 3 Windows, 1 network, 3 conversations |

**Two things were answered by NOT answering them, and both are publication tasks rather than gaps:**
[Q-38](docs/OPEN-QUESTIONS.md) — the public install command — and the Autodesk App Store requirements in
[D-38](docs/DECISIONS.md). Both need **current documentation read at the time**, and writing either from
memory is the failure this repository has already had twice. `tools\setup.ps1` is the proven route
meanwhile.

---

## 9. Next — read the decisions back, then prove what is built

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
working, because [D-14](docs/DECISIONS.md) is still *Proposed*. His instruction — *"now we just recorded,
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
[docs/30](docs/30-compiling-away-from-windows.md) for how, in one command:

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
- **"Studied, not copied" fails silently unless somebody checks.** [D-25](docs/DECISIONS.md) was being
  obeyed in intent and broken in fact — a level lookup was verbatim with one variable renamed, under a
  commit message that said *"written fresh"*. It surfaced only because the owner asked outright. The
  answer was yes, and both pieces were rewritten. **Do the side-by-side yourself before the commit**,
  not when asked; the method is in [docs/31](docs/31-studying-the-existing-libraries.md).
- **A scanner that scans itself finds itself.** `check-gaps.py` reported its own regex as two undeclared
  agents on its first run. Funny once; worth remembering as the general shape — a tool that reads the
  repository is part of the repository.
- Commits are authored **Ajmal PS**. The repo is **private**.

---

*Phase 0 is finished and proven. Step 6 and the whole of Phase 2 are finished and proven of nothing —
built carefully, obeying rules that are now binding, tested where testing was possible, and compiled on
every release from 2020 to 2027. Seventeen fragments and skills sit at `DRAFT`, which is not a shortcut
but [D-30](docs/DECISIONS.md) being obeyed: promotion needs one proof containing a negative case, and a
negative case needs a model. `python tools/check-gaps.py` is now the file that answers "what is left",
because it is computed from disk and this one is not — and where they disagree, believe the tool. It
currently says **nothing** here is unfinished and 55 are waiting: 47 on a Revit, 3 on Windows, 1 on a
network, 3 on a conversation. The one thing that *was* unfinished — **the brain wired to nothing** — was
invisible for a day because the tool had only ever been taught to ask whether the build order was
followed; it is now wired, and the newest waiting row is the half of that work this machine could not
prove.
Almost nothing in this repository is waiting on another session; it is waiting on a machine — and three
times now, something believed to be waiting on a machine was waiting on somebody trying it. `D3` is
still the line that matters most: move the ducts 200 mm, then measure one.*
