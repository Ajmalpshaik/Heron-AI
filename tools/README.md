# tools

**The scripts that keep this repository honest.** Plain Python 3; the two that compile also need the
.NET SDK, and `check-fragments-compile.py` and `check-routing.py` need PyYAML. Run them from the
repository root.

It said *"three small scripts that keep the documentation honest"* until 2026-08-29, then **"ten"**
until 2026-09-07. The second one was *correct on the day it was written* — 2026-08-30, in the commit
that added `check-routing.py` — and went stale **the following day**, when `check-intrusion.py` landed
and its own section was added to this file without the number at the top being touched. It then sat
wrong for a week, in the paragraph that boasts about counting.

**So the number is not typed here any more.** It is the one fact in this file that a reader can derive
in a second:

```bash
ls tools/*.py | wc -l
```

Counting them is the same discipline the rest of this file is about, and it took three goes to apply it
to this sentence.

They exist because this repository already got its own numbers wrong twice — the agent registry
asserted **166 agents while its own departments summed to 196**, and a build-step figure said 20 where
the rows said 42. Both were caught by adding up columns by hand, which is not a process.

These are the working prototype of the **Documentation Validation Agent**
(`HERON-DOC-VAL-009`) and the **Agent Documentation Agent** (`HERON-DOC-AGT-002`) — see
[docs/28](../docs/28-agent-registry.md). When those agents exist, this is roughly what they do.

---

## `check-docs.py` — link and cross-reference integrity

```bash
python tools/check-docs.py
```

Verifies that:

- every relative markdown link resolves to a file that exists
- every `Golden Rule N` reference points to a rule defined in [docs/14](../docs/14-golden-rules.md)
- every `D-NN` reference points to a decision defined in [docs/DECISIONS.md](../docs/DECISIONS.md)
- every `Q-NN` reference points to a question defined in [docs/OPEN-QUESTIONS.md](../docs/OPEN-QUESTIONS.md)
- **no rule, decision or question number is defined TWICE** — added 2026-09-09, and it **fails the run**
- **every file in `tools/` and every skill folder in `.claude/skills/` is named in its README** — one
  direction only, read from the disk so a new tool is caught before it is committed — section 10, added
  2026-09-23, **fails the run**
- **text hygiene over every file git tracks**: no control character other than tab, CR and LF, no
  double-encoded text, no merge-conflict marker — section 11, added 2026-09-23, **fails the run**, except
  a hit inside a PROVEN fragment's `impl/`, which is reported as **waiting** because fixing it makes the
  proof stale ([D-30](../docs/DECISIONS.md)). With no git to ask it says **NOT RUN**, never a pass
- **no sentence states an MCP tool total other than `len(heron_tools.TOOLS)`** — in section 7, added
  2026-09-23, **fails the run**. Another project's count (*"their 314 MCP tools"*) and a quotation are
  left alone; the better sentence types no number and points at `python mcp/server/heron_tools.py`

Run it after any edit that moves or renames a document. It is how the Golden Rule renumbering
(ten rules to fifteen, [D-12](../docs/DECISIONS.md)) was verified across 31 files.

**An id defined twice is worse than one never defined, and this script could not see one.** Every
registry was read with `set(re.findall(...))`, and a set is precisely the thing that makes a duplicate
invisible: two `## D-56` headings collapse to one entry, *"REFERENCED BUT NOT DEFINED"* stays empty, and
the file is reported clean. **D-67 was first written as D-56, which already existed, and this checker
passed on it** — the duplicate was found by eye, which is the reading it exists to make unnecessary.

**A duplicate id was the first thing here to fail the run.** This paragraph went on to say it was the
only one, and that a broken link did not, until 2026-09-23 - a broken link has failed the run since
[D-77](../docs/DECISIONS.md) on 2026-09-16, and so has everything added since. Why a duplicate came first
still holds: a dead link announces itself the moment somebody clicks it. A duplicate id is silent, and it
makes every reference to that number ambiguous — `[D-56](../docs/DECISIONS.md)` now points at two
different decisions and nothing can say which was meant, not the anchor, not the reader, not this
script. Both entries look correct in isolation.

Verified by breaking it on purpose, once per registry: a second `## D-56`, a second `### 3.` rule and a
second `### Q-51` each name themselves and exit 1, and the unmodified repository exits 0.

### The three checks added on 2026-09-23, and what each found on its first run

Each was run against the repository as it stood before anything was corrected, and each failed on
something real:

| | found | corrected |
|---|---|---|
| **10. named in its README** | eight files in `tools/` named nowhere on this page, three of them not Python - which is why the one-liner further down, `*.py` only, could never have seen them | a row each, in [the section below](#nine-tools-this-page-did-not-describe-until-2026-09-23), with a ninth for `balance-of-work.py`, which was named only in passing |
| **11. text hygiene** | a backspace and a form feed in one archived defect row, where the row meant to write `\u0008` and `\u000C`; and a C1 control character in [docs/07](../docs/07-installation-and-update.md), where `Addins\2020` had once been read as an octal escape and lost two characters | each written back as the text it stood for |
| **7. the MCP tool total** | [33](../docs/33-external-repository-research.md) saying *"Heron has 14 MCP tools"* twice and [34](../docs/34-patterns-adapted.md) once, while `heron_tools.TOOLS` held more than twice that. The row this list used to carry for the count could not fire - it counted `tools/*.py` against a phrase no sentence uses | the sentences type no number now, and point at the command that lists the tools |

**And it is faster for them, not slower.** The plan that asked for these checks measured this script at
30-37 s on a busy PC and asked for it to be timed before and after. Five runs each, on a quiet Linux
container, 2026-09-23:

| | median | range |
|---|---|---|
| before | **7.3 s** | 7.0 - 7.4 |
| with the three checks | 7.6 s | 7.3 - 7.8 |
| and asking each line the history question **once** | **4.3 s** | 4.2 - 4.4 |

Section 11 reads every tracked text file once, about 21 MB, and cost about 0.3 s. The profile showed where
the rest had always gone: every count check walks every line of every markdown file and asks the
history-word pattern first, so the same line was asked up to eight times - most of the run was that one
regular expression. It is remembered per line now (`is_history`), and the sentence split is kept per file
for the four checks that read it. **The output is byte for byte the same** - compared on the clean tree
and again with thirteen planted drifts.

---

## `recount-agent-registry.py` — counts derived, never asserted

```bash
python tools/recount-agent-registry.py
```

Reads the agent rows in [docs/28](../docs/28-agent-registry.md) and **rewrites every stated count from
what it finds**: each department heading, the summary table, the header totals, and the figures in the
prose.

> A number in that document is never typed by hand. If it disagrees with the rows, the rows win.

Run it after adding, removing or re-tiering any agent.

---

## `agent-count.py` — how much of the 250 exists

```bash
python tools/agent-count.py
```

The register: every department with **TOTAL · BUILT · HOST · DEFER · LEFT · T1 LEFT**, the tier split, and
where Phase 0/1 actually stands. `recount-agent-registry.py` keeps the registry honest about *itself*;
`check-metadata.py` audits it file by file. Neither answers *what proportion of each department is
built*, which is the question asked before deciding what to build next, and which was being answered by
adding up columns by hand.

**Four states, not two — and that is the point.** An agent is BUILT, LEFT, **HOST** (delegated to
Claude Code on purpose by [D-01](../docs/DECISIONS.md)) or **DEFERRED** (a decision says it cannot be
built yet, and names itself). Collapsing HOST into LEFT produces a to-do list
with four items that will never be done, and on 2026-09-07 it did exactly that — a build-state summary
read *"Phase 0/1 is four agents short"* and recommended building the **Orchestrator**, which
[docs/02 §7](../docs/02-architecture-overview.md) settles as the host's. Phase 0/1's agent list is
complete: **45 built, 4 host-provided, 0 outstanding.**

**DEFERRED was the same mistake one step along, and it was live until 2026-09-21.** `HERON-DOC-REL-005`
needs releases to write notes about and there are none; `HERON-DOC-CHG-008` needs two versions to write a
change log between and 683 files say `Heron-Since: 0.1.0`. [D-77](../docs/DECISIONS.md) settled both on
2026-09-16 and this tool went on printing **2 left** — so [the balance-of-work page](../docs/work-notes/BALANCE-OF-WORK.md),
which is what a person reads to know what remains, carried two items nobody could do. A number that
cannot reach zero stops being read. The state is **derived from the registry row** — the word `DEFERRED`
and a decision id in the same sentence — and a row that says DEFERRED without naming a decision stays
**LEFT**, because *"later"* with nobody's name on it is how a to-do list becomes a wish.

`HOST_PROVIDED` is **imported from `check-metadata.py`**, not repeated here. Two copies of that list is
the drift this folder exists to prevent.

**It fails when the register does not reconcile**, and all five gates were verified by breaking the
inputs on purpose before the tool was believed:

| Gate | Fires when |
|---|---|
| heading vs rows | a department heading's stated count disagrees with the rows beneath it |
| totals vs rows | the `**Totals:**` line disagrees with the rows |
| stale exemption | `HOST_PROVIDED` names an agent the registry no longer has |
| delegated *and* built | a file claims an agent the host provides — the decision was reversed, or the exemption is stale |
| ghost claim | a file claims an agent id that is not in the registry |
| **was built, now is not** | an agent id claimed in the **last commit** is claimed by nothing in the working tree |

The fourth is the one nothing else asks, and it is the one that would have caught the error above.

**The sixth compares against git rather than against the register, because the register could not see
what happened on 2026-09-15.** A new agent was written straight over `brain/heron_architect.py` — 272
lines holding `HERON-AHR-ARC-003`, with its 201-line suite at `tests/test_architect.py`. Nothing
complained. The register reconciled perfectly either way: the id that vanished and the id that arrived
**cancelled out in the total**, so the only symptom was a number that did not move, and an overwritten
file is unrecoverable outside git. The check now names the id and the files that held it.

It looks one way only. An id this tree has and the last commit did not is *building*, and is not
reported. And no git is not a finding — an installed Heron is not a checkout, so the tool says plainly
that it had no second opinion rather than treating that as a pass. `tests/test_agent_count.py` runs the
comparison with one id taken out and watches it fire, because a guard nobody has seen fire is a comment.

---

## `check-structure.py` — the layout, and the layering

```bash
python tools/check-structure.py
```

Two questions that used to be answered by hand:

1. **Is every file in the part it belongs to?** Top-level folders mirror the four product parts,
   so *"where do I fix the Revit thing"* has one answer.
2. **Does any part depend on something it must not?** `platform` depends on nothing · `revit`
   and `brain` never touch each other · **`Autodesk.Revit` appears only inside `revit/`**.

The second matters more. A layering rule written only in a document gets broken quietly; a
layering rule in a script gets broken loudly, once, and then fixed.

The working prototype of `HERON-WSP-VAL-003` and `HERON-AHR-MON-011`.

---

## `check-metadata.py` — the standard, enforced

```bash
python tools/check-metadata.py
```

Enforces [docs/29](../docs/29-metadata-standard.md) — every source file declares its agent,
build step, lifecycle status, version and layer.

Then it does the thing that makes the standard worth having, and audits **in both directions**:
code claiming an agent that is not in the registry, *and* registry agents due by now that no file
implements. The second half is an honest to-do list rather than an error.

The working prototype of `HERON-STD-MET-014`.

---

## `check-compile.py` — does the C# actually build

```bash
python tools/check-compile.py                 # every version it can reach
python tools/check-compile.py 2020 2024       # just those two
```

Builds all four projects against every Revit version, using the Revit API reference assemblies from
NuGet. This is `A2`, `A3` and `A5` of [`NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) in one command instead
of one version at a time.

**It does not need Windows and it does not need Revit** — which is the point, because until
2026-08-28 not one `.cs` file here had been through a compiler at all. The first run found a real
2020-only error. [docs/30](../docs/30-compiling-away-from-windows.md) is the whole story.

**All eight releases, 2020 through 2027, compile off Windows** — but only with an SDK that carries the
WindowsDesktop MSBuild targets, because the add-in uses WPF for its ribbon icons. On Ubuntu that is
`dotnet-sdk-10.0`; the `dotnet-sdk-8.0` package omits them and stops at 2024. The script **probes for
those targets** rather than for an operating system, and when they are missing it skips the affected
releases and names the package to install. A skip is still never reported as a pass.

A pass means the API surface agrees; it is **not** evidence that anything behaves correctly.

---

## `check-fragments-compile.py` — does the *fragment* C# build

```bash
python tools/check-fragments-compile.py             # every release each claims
python tools/check-fragments-compile.py 2020 2024   # just those two
python tools/check-fragments-compile.py --keep      # leave the build tree to read
```

`check-compile.py` builds the four **projects**. Until 2026-08-29 **nothing built the fragments** — and
a fragment is C# that will one day be handed to Roslyn and run inside Revit ([D-28](../docs/DECISIONS.md)).
The part of the library that does the work was the one part no compiler had read.

A fragment is a **snippet**, not a file: it assumes names are in scope and leaves names behind
([D-29](../docs/DECISIONS.md)). So each one is wrapped in a method **whose parameters are its declared
`needs`**, generated from the contract rather than guessed.

**It checks the contract, not only the syntax.** After the snippet, every name the contract `provides`
is assigned to a local of the declared type — so a fragment that promises `IList<Element> elements` and
leaves something else, or leaves nothing of that name, **fails**. That half is worth more than the
syntax: a snippet that compiles while breaking its promise is the one that composes into something
broken later, far from here.

**Its first run found a real defect**, which is the same story `check-compile.py` has: `FRG-QA-001`
declared a value called `checked` — a **reserved C# keyword** — and its own code read `if (checked == 0)`.
It could never have compiled, and it had passed every other check, because nothing had ever asked
whether a contract name could be a variable. [`brain/heron_fragment.py`](../brain/heron_fragment.py)
now refuses that whole class instantly, so the compiler is the authority and the validator is the fast
half that stops the mistake being made.

Needs the .NET SDK (`dotnet-sdk-10.0` on Ubuntu) and no Revit and no Windows. Errors point at the
fragment's own file and line, not at the generated wrapper.

**How many fragments last passed it is not written here on purpose.** This line said *"all 32 fragments
compile on all 8 releases"* until 2026-09-02, by which point the library was well past four times that -
a number typed into prose goes stale the day after it is true, and a stale green is worse than no
claim, because it is believed. **Run the tool; its own output is the count.** What each session records
instead is in [`../docs/HANDOVER.md`](../docs/HANDOVER.md): which releases were compiled, when, and by what.

Same limit as every compiler: it says nothing about **behaviour**. That is [D-30](../docs/DECISIONS.md)'s
proof with a negative case, and it needs a real model.

---

## `check-routing.py` — did a new fragment make an old one unfindable

```bash
python tools/check-routing.py
```

Asks **every fragment's own declared utterances** back to the search, and checks the fragment that
claimed the sentence comes back first.

It exists because adding a fragment can make an existing one unreachable, silently, and nothing else
here would notice. That happened twice while the library was being written, and neither was visible at
the time: `isolate-elements` declared *"show me just these"*, and later `create-duct` declared *"duct"*
phrasings — each time the duct **filter** slid down the tracked query in
[`brain/retrieval-history.md`](../brain/retrieval-history.md), and each time the new fragment was
perfectly entitled to its own words.

**Read a pass as a lower bound.** A fragment's phrasing shares vocabulary with its own indexed text, so
passing is close to circular. A **failure** is real information: a request phrased that way now lands
somewhere else.

**It never fails a build, on purpose.** A collision is a judgement, not a defect — the other fragment may
genuinely be the better answer, or the sentence may name a **composition**, which no fragment can win
and a skill should claim. A tool that failed here would teach people to weaken utterances to buy a rank,
and that is the one response ruled out: taking *"show me just these"* away from the isolate fragment
would make the isolate unfindable in order to protect a measurement.

Its first run over 169 sentences found sixteen contested ones. **Three were genuine errors** — a filter
claiming two of `TRACE_CONNECTIVITY`'s sentences, and an override fragment claiming the grayout
**skill**'s — and correcting those took the words route to **100% in the top three**. The other thirteen
were left alone and written down.

**Since 2026-08-31 it separates one class of contest from the rest.** Every contested sentence used to
print in one flat list, which is right for two reports arguing over *"show me the sizes"* and wrong for
the case underneath: a sentence claimed by a **READ** fragment and answered by one that **writes**.
There the failure is not a wrong table, it is *the model changed on a question*. Those print above the
flat list with both risk levels named. The first run found **six, four of them invisible until that
moment** — *"follow the pipe"* was reaching `OFFSET_ELEMENTS`, which does not return a wrong route, it
shifts the run sideways. See [D-101](../docs/DECISIONS.md).

---

## `check-intrusion.py` — who turns up in shortlists they have no claim on

```bash
python tools/check-intrusion.py
python tools/check-intrusion.py --top 20
```

The question `check-routing.py` **cannot** ask. That one asks whether each fragment still wins its own
sentences — a per-fragment question that says nothing about the shortlist a user actually sees. A
fragment can win every sentence it declares and still appear in the top five for forty sentences
belonging to other people, and five slots holding three plausible answers and two irrelevant ones is a
worse answer than three.

**An intrusion is not a defect.** A shortlist is meant to hold more than one candidate, and two
fragments can fairly answer one sentence. It exits 0 — a gate here would be a gate on how ordinary
somebody's phrasing is.

It was written to test a specific suspicion, and **the suspicion was wrong**. Purposes here run 21 to
523 words and the whole purpose is indexed, so length looked like the cause. It is not: the correlation
is weak, `set-selection` has the shortest purpose in the library and one of the highest intrusion
counts, and `group-by-assembly` has one of the longest and intrudes on nothing. **Shortening prose would
not have fixed it**, which is worth having measured before spending a day on it. What the top of the
list shares is generic phrasing — *"show me…"*, *"which … are in this model"* — every one a real
sentence nobody may take away. The finding is about the retrieval layer, and **A7** is what would
separate them. Numbers in
[`brain/retrieval-history.md`](../brain/retrieval-history.md); run the tool rather than quoting them.

---

## `score-routing.py` — the owner's own questions, against the answers he confirmed

```bash
python tools/score-routing.py            # score, and say what moved since the last comparable run
python tools/score-routing.py --record   # ...and append the row to brain/retrieval-history.md
python tools/score-routing.py --list     # the answer key; asks nothing
```

The two tools above ask sentences written **for** the library. This one asks the 79 questions the
owner really typed on real work, kept in
[`tests/data/owner-questions.yaml`](../tests/data/owner-questions.yaml). Each question carries the
capability, skill or tool that should answer it, chosen from the capability list before the search had
seen the question, and confirmed by him. It asks each one through `heron_brain.lookup`, the function
`heron_lookup` calls. It takes about three minutes, because every lookup opens and checks the store.

**It keeps four places apart:** first, in the top three, found but low (4th–5th of the shortlist
`heron_lookup` shows) and not found. **An exact declared phrase is counted separately**, because it tests
a declaration rather than the search. **Every question that lands on a write is listed** — a question
handed a change when the right answer changes nothing ([D-86](../docs/DECISIONS.md)), a change asked for
and a different change given, and a gap that landed on a change. A skill row can only be met by one of
the steps the skill declares, since skills are not indexed, so skill rows are kept out of the headline.

**Each recorded run is one row** in [`brain/retrieval-history.md`](../brain/retrieval-history.md). The
row is stamped with the date, the fragment count, the backend and a fingerprint of the answer key, and
carries one character per question. The next run names **which** questions moved, not only that a total
did. Two runs are compared only when the answer key, the backend and the Revit filter match; the library
may have grown between them, because a new fragment taking one of his questions is the drop it exists
to catch.

**It exits 0 whatever it finds**, like every routing check here, and exit 2 means nothing was scored.
**A drop is reported and never stops a pull request** — the owner decided so on 2026-09-23
([D-100](../docs/DECISIONS.md)), and `exit_code()` in the tool names the decision.
**Nothing it reports is answered by rewording a question, changing an answer to match the search, or
adding or weakening an utterance** — [row 113](../docs/FRAGMENT-ISSUES.md)'s forbidden move. It keeps its
79 lookups out of the owner's audit trail by pointing `HERON_AUDIT` at a throwaway folder for the run.
[`tests/test_score_routing.py`](../tests/test_score_routing.py) plants a drop, in the arithmetic and in a
private store through the real search, and proves both are caught.

---

## `check-api-surface.py` — the releases the compiler cannot reach

```bash
python tools/check-api-surface.py                 # 2020 through 2027
python tools/check-api-surface.py 2025 2026       # just those
```

Reads the **compiled** add-in's reference tables — exactly the Revit types and members the code calls,
not what a regex over the source can find — and checks each one exists in that release's shipped
reference assemblies.

It was written when `check-compile.py` stopped at 2024 off Windows, leaving the newest three releases
with nothing checking them on the machines this project is actually worked on. **That is no longer
true** — `A5` established that all eight compile off Windows given `dotnet-sdk-10.0`, and the section
above says so — so this tool is now a second opinion rather than the only one for 2025-2027. **Matching
is by name**, so a changed signature passes here and would fail a real compile: it supplements the
compile gate, never replaces it.

**Validate it before trusting a clean result.** Put `doc.CreationGUID` back into
`RevitWrite.DocumentKey()`, build for 2024, and run it against 2020 — it must report the missing
member. A checker that finds nothing is evidence about the checker until it has caught something.

---

## `api-surface --members` — what one call looks like on every release

```bash
python tools/check-api-surface.py            # once, to fetch every release into the cache
dotnet run --project tools/api-surface -- --members WorksharingUtils
dotnet run --project tools/api-surface -- --members ElementId.IntegerValue
dotnet run --project tools/api-surface -- --members Duct --inherited
```

The two tools above read Revit's API **by name**, and both say what that cannot see: a member whose
parameters, return type or setter changed, and an `[Obsolete]` mark, which is an attribute and not a
name. This reads exactly those, for one type at a time — every public member as each cached release
declares it, with the releases that have it **exactly** as written. A member re-signed between two
releases is two lines, each with its own years; an obsolete one carries its message and the years it
was marked. `ElementId.IntegerValue` reads as present 2020–2025 and marked obsolete 2024–2025 — the
two years of warning a name-only reader cannot see.

Its own file is [`api-surface/Members.cs`](api-surface/Members.cs). It reads the cache
`check-api-surface.py` fills — no Revit, no network — and names every release it read, so one that was
never fetched is visibly absent rather than read as having nothing. A named indexed property such as
`Element.BoundingBox[View]` is printed with how C# calls it (`get_BoundingBox(...)`), because it is not
an indexer. A signature naming a type this machine cannot resolve — WPF's, off Windows, for the ribbon
images in `RevitAPIUI` — is still listed, with the assembly it needs, never dropped.

**Exit codes:** 0 found · 1 no such type in any release read (it prints the names it nearly
matched) · 2 no name given, or a short name that means more than one type (it names each) · **3 NOT
RUN** — nothing cached to read, which is never an answer about Revit. Proved against libraries
[`tests/test_api_members.py`](../tests/test_api_members.py) builds for itself, so CI can check it
without Autodesk's assemblies.

---

## `check-licence.py` — what licence is on the knowledge Heron ships

```bash
python tools/check-licence.py            # every fragment and skill
python tools/check-licence.py --all      # including the ones that are fine
```

Heron plans community packages, and a package the user redistributes carries whatever licence its
source had. This reads the files rather than the landing page — [Q-53](../docs/OPEN-QUESTIONS.md),
answered as [D-66](../docs/DECISIONS.md), after a skill library whose README said *"MIT, use freely"*
turned out to ship four skills marked all rights reserved.

**Exits 1 on a finding, 0 when there is nothing to say.** A finding is a question for a person; the
tool does not decide whether redistribution is lawful and **automated coverage is not legal
clearance**.

One portability note, because it cost a session once: `inspect()` must not assume the path it is given
shares a drive with the repository. Its `repo_relative()` falls back to the absolute path when there is
no relative form, because `os.path.relpath` raises `ValueError` across drives on Windows, and the test
builds its fixture in `tempfile.mkdtemp()`. Repository on `D:`, `TEMP` on `C:`, and the gate dies.
**The rule is [`brain/heron_relpath.py`](../brain/heron_relpath.py)'s, asked rather than copied.** Until
2026-09-22 this tool repeated it on purpose, because it then lived in `heron_fragment`, and importing
that costs PyYAML - and this is the tool you run on a bare machine to read the licence of something
*before* trusting it enough to install anything for it. `heron_relpath` imports nothing but `os`, and
[`tests/test_relpath.py`](../tests/test_relpath.py) runs this tool with PyYAML blocked
([row 5b-152](../docs/FRAGMENT-ISSUES.md)).

## `heron-backup.py` — the user's own data, and getting it back

```bash
python tools/heron-backup.py backup            # take one
python tools/heron-backup.py drill             # the round trip, into scratch
python tools/heron-backup.py list | verify
python tools/heron-backup.py restore <name> --confirm
```

`%APPDATA%\Heron` holds four files and **a grep for `backup` across every `.py`, `.cs` and `.ps1` in
this repository returned nothing.** The audit trail is Heron's only record of what it has ever done,
[`HeronAudit`](../platform/Heron.Core/HeronAudit.cs) never prunes it, and it is not in git — so losing
it loses the whole history with no way back. The config holds `write.enabled`, which
[docs/12 §9](../docs/12-security-and-permissions.md) calls a security boundary.

**It excludes rather than includes.** [docs/21 §8](../docs/21-resilience-and-operations.md) says not to
back up the derived index, so `knowledge/*.db` is skipped with that reason recorded in the manifest.
Everything else under the data folder is copied — including files nobody has thought of yet. An
include-list is a decision taken today about tomorrow's files: the day somebody drops a personal
fragment library in there, an include-list silently misses it and nobody finds out until they need it.

**`drill` is the point, not a nicety.** docs/21 §8: *"Restore must be tested, not merely implemented.
An untested restore path is not a restore path — it is a belief."* The drill backs up, restores into a
scratch folder, and compares content hashes both ways, touching nothing real. It is why the Restore
Agent shipped in the same commit as the Backup Agent.

Restore refuses without `--confirm`, refuses a backup that fails verification, and **takes a safety copy
of whatever it is about to overwrite** — the thing a restore destroys is the only record of the machine
a second ago.

**What it does not protect against, plainly:** the default copy sits under the data folder it protects.
That survives an uninstall, an update, and a deleted audit folder. It does not survive losing the disk.
Pass `--to` a path on another drive for that.

The working prototype of `HERON-WSP-BAK-010` and `HERON-WSP-RST-011`.

---

## `generate-fragment-catalog.py` — the library, readable

```bash
python tools/generate-fragment-catalog.py
HERON_CATALOG_OUT=somewhere.html python tools/generate-fragment-catalog.py
```

Every fragment on one searchable page: what it is, what it needs, what it leaves behind, what it is
allowed to touch, the phrases somebody would say to reach it, its declared cases, and its proof if it
has one. Filter by status, risk, domain, or whether a proof exists.

**It answers a question that was being answered with throwaway Python.** `heron_capabilities` says what
Heron can DO, at the level of jobs — the right answer to that question and not to this one. Nothing
showed the library itself. Over one session *"what have we got"* was answered five times by writing a
script over the yaml files, which is a missing page rather than five scripts.

**It carries one judgement, and that is why it has a test** ([`tests/test_catalog.py`](../tests/test_catalog.py))
where [`generate-agent-map.py`](generate-agent-map.py) has none. It decides whether a fragment's declared
negative case can actually be run — and got that wrong on its first run, reporting **0** stranded cases
across a library holding **18**. A generator that only draws needs no test; one that concludes does.

The working prototype of `HERON-DOC-FRG-004`.

---

## `generate-skill-catalog.py` — what a person can ask for

```bash
python tools/generate-skill-catalog.py
HERON_SKILL_CATALOG_OUT=somewhere.html python tools/generate-skill-catalog.py
```

Every skill on one searchable page: the words somebody actually says to reach it, its domain, its risk,
what must be true before it runs, and what it stands on. The parallel to the fragment catalogue one
layer up.

**A skill is only as proven as the weakest fragment under it**, and that is the one thing a list of
names cannot show. Every skill in the library sits at `DRAFT`. Some rest entirely on `PROVEN`
fragments and are waiting for nothing but somebody to look; others rest on something weaker and
cannot move until those do — **the tool prints how many of each, and this page deliberately does
not**, because it said *six and four* for long enough to be wrong. In a list of names they are
identical. So each skill carries an **effective status** — the lowest rung on
[docs/09](../docs/09-skills-and-fragments.md)'s ladder among the fragments serving it — beside the
status its own card declares. A card can say anything; the chain underneath is the fact.

**AND THE CHAIN IS ONLY HALF THE FACT.** A chain of `PROVEN` fragments says nothing about whether
the skill's own sentences REACH it, and the two read identically on a card. So each skill also
carries its **words**: how many of its own utterances land on a capability it declares, every
sentence marked with where it actually went, and a loud tag when a question lands on something that
CHANGES THE MODEL. **Neither half is a proof** — the footer says so — and the subtitle labels them
`CHAIN:` and `WORDS:` so neither reads as the whole.

That half is a **recording, never re-measured here**: one `heron_brain.lookup` per utterance is about
twenty-five minutes, which is not a page render. It reads the newest `tools/jobs/skills/routing-*.json`
that `prove-skill.py --routing-to` wrote. **A missing recording
says `NOT MEASURED`**, which is a different sentence from *no crossings*, and **one whose sentences
have moved since it was taken says `OUT OF DATE`** — compared against the PHRASES, which are in git,
and never against the store, which every worktree writes
([row 152](../docs/FRAGMENT-ISSUES.md)).

**A card also shows what the RETRIEVER said about its own answer** ([row 157](../docs/FRAGMENT-ISSUES.md)).
`heron_retrieve` has always written *a coin toss* when neither route preferred the winner, and *the
words route ranked the library rather than selecting from it* when that route had no claim on the
sentence — and every consumer used to drop it. A sentence that reaches the right capability **and**
carries one of those is the right answer found by luck, and the page used to report it as a plain
success. **The two complaints are not the same strength**, so the card says *the retriever
complained*, never that the answer is wrong, and a complaint does not take a sentence out of the
reach count — whether it spends it is a reader's call.

**Reachability is reported per Revit release, never overall.** A skill declaring 2020–2027 whose
fragments cover 2024 and 2025 works on two releases and claims eight; one overall figure is exactly
what hides that. Same rule as `HERON-SKL-PRF-006`, same reason.

**It concludes, so it has a test** ([`tests/test_skill_catalog.py`](../tests/test_skill_catalog.py)):
the effective status, the per-release reachability, whether a recording still covers the skill's
sentences, and what the retriever said about each answer are all judgements that can be wrong while
the page still renders perfectly. **Nothing in that suite edits `brain/skills`** — `collect()` takes
the recording folder so the whole path runs against a recording written for the test, which is
[row 155](../docs/FRAGMENT-ISSUES.md)'s lesson about a suite that rewrites the library it is checking.

The working prototype of `HERON-DOC-SKL-003`.

---

## `generate-api-docs.py` — every MCP tool, from its own signature

```bash
python tools/generate-api-docs.py
HERON_API_DOCS_OUT=somewhere.html python tools/generate-api-docs.py
```

Every tool Heron serves over MCP: what you send, what type, whether it has a default, what it returns,
its declared risk, the bridge operation it calls, and whether it can change the model.

**A Heron tool's schema is not a JSON file anywhere** — it is the decorated function's signature, which
the MCP SDK turns into one at run time. So that is what this reads, and it **parses rather than
imports**: importing `heron_mcp_server` needs the MCP SDK installed and defines eighteen tools as a side
effect of asking what they are. A documentation tool that only works where the server already runs is
useless exactly where documentation is wanted — a reviewer's laptop, CI, a checkout with no
dependencies. `ast` needs nothing.

**The conclusion it carries: a parameter nothing explains.** A docstring can describe a tool beautifully
and never mention its arguments; a caller then reads `depth: int = 0` off the schema and guesses. Every
parameter is checked against its own tool's docstring on a word boundary — `full` is not explained by
"fully" — and the unexplained ones are named. **Two were unexplained on its first run** against eighteen
tools that all have docstrings: `revit_preview_move`'s `category` and `heron_research`'s `request`. Both
are now documented, and the run is clean.

Risk and bridge operation are **asked of** [`heron_tools.py`](../mcp/server/heron_tools.py) rather than
copied, and a tool it cannot classify is reported rather than dropped — in both directions.

It concludes, so it has a test ([`tests/test_api_docs.py`](../tests/test_api_docs.py)).

The working prototype of `HERON-DOC-API-001`.

---

## `api-changes.py` — what each Revit release stopped shipping

```bash
python tools/api-changes.py                 all eight, in order
python tools/api-changes.py 2025 2026       just that transition
```

Produces `tools/api-surface/changes.json`, which
[`HERON-REVIT-ACI-034`](../brain/heron_apichanges.py) reads. It is the tool and not the agent, because
it needs the network, a .NET SDK and 264 MB of reference assemblies, and an agent that only answers on a
machine with all three answers nowhere useful. Same split as `HERON-DEV-NET-006` and `check-compile.py`.

It dumps every public type and member each release ships — via a new `--dump` mode on
[`api-surface/Program.cs`](api-surface/Program.cs) — and diffs adjacent releases. **Removals are kept in
full; additions are counted.** A member that disappeared breaks code that calls it; a member that
appeared breaks nothing, and listing 18,000 of them buries the ones that matter.

| transition | removed | added |
|---|---|---|
| 2020 → 2021 | 627 | 2,816 |
| 2021 → 2022 | 906 | 8,126 |
| 2022 → 2023 | 828 | 1,402 |
| 2023 → 2024 | 282 | 1,364 |
| 2024 → 2025 | 665 | 1,751 |
| 2025 → 2026 | **239** | 1,415 |
| 2026 → 2027 | 647 | 1,780 |

`ElementId.IntegerValue` is in that 239. It was written into this repository with a comment calling it
*"the property every version has had"*, by someone who had checked five releases and extrapolated to
eight. **That is D-05, and this is the tool that would have said so.**

**The surfaces are gitignored and the digest is committed.** A release's full surface is ~3 MB and there
are eight; what changed between them is a few hundred lines. The surfaces are the working, the digest is
the result, and the result is what survives a fresh checkout with no network.

**What it cannot see, and the register asks for it.** docs/28 wants *"silent behavioural changes"*.
Reading two assemblies finds a member that is gone. It does not find one still there that returns a
different unit, or now throws where it returned null, or whose meaning changed — and it does not find a
**deprecation**, because `[Obsolete]` is an attribute and this reads names. A member marked in 2024 and
deleted in 2026 appears at 2025 → 2026 and nowhere earlier, two years after the warning existed.
`api-surface --members` (above) reads that attribute, and the signature, for one type at a time.

---

## `generate-contract-reference.py` — what was built, and what it promised

```bash
python tools/generate-contract-reference.py
HERON_CONTRACT_REFERENCE_OUT=somewhere.html python tools/generate-contract-reference.py
```

Every agent contract in [`brain/agents/`](../brain/agents) — its inputs and outputs with their types
and descriptions, the refusals it declares, the tools it allows — beside the metadata header of every
file that claims that agent: which file, which layer, what status, and which suite proves it.

**Not a sixth agent map.** The four generators above document what Heron *offers*, and each reads a
register: docs/28, the MCP signatures, the fragment library, the skill library. This one is in the
**Development** department and documents what was *built*, from the source nothing else reads — the
contracts. docs/28 is the plan, a contract is the promise, a header is the claim. This page is the
third, and the only one that can be checked against the other two.

**The conclusion it carries: a promise the code does not keep.** Each contract declares the refusals a
caller may have to handle. Every agent's own suite checks its own module names its own declared
failures — and that had never been run across all of them at once. It fails two ways:

- **declared, never produced** — a caller writes a branch for something that never happens;
- **produced, never declared** — a caller handling every declared failure still meets an unhandled
  one, which is the direction that breaks at run time.

**Four were undeclared on its first run** across 121 contracts, and every declared refusal was
reachable.

**A refusal is recognised by position, not by shape.** The first version read every `SCREAMING_SNAKE`
string in a file and reported **135** undeclared refusals — capability names, stated shapes, module
constants, and examples an agent quotes to say it does *not* do that. A page of findings that are all
wrong is worse than no page: it teaches the reader to skip the table, which is where the real ones are.
So a refusal is read where this repository puts one — the value under a `refused` key, or the word
before the colon in a `raise` — and both come off the parse tree, so a name quoted in a docstring
explaining why a refusal is *not* raised is not counted as raising it. A single word counts:
`INCOMPLETE`, `MODIFIED` and `PINNED` are three real refusals, and requiring a second word accused four
agents of breaking a promise they keep.

**The two directions use different evidence, deliberately.** To say a promise is broken you must be sure
the module cannot produce that refusal *at all* — so the agents it calls count too, because
`HERON-IMP-FEX-004` declares `NOT_A_FOLDER`, never writes it, and hands back `HERON-IMP-FIL-002`'s
answer unaltered. That is composition, not a broken promise. To say a refusal is undeclared you must be
sure it *is* one, so only the positional forms count. Wider to excuse, narrower to accuse.

**A suite is not the agent.** `tests/` files carry the same `Heron-Agent` header — that is how a suite
says what it proves — so they are shown, and never read for what the agent does. Counting one reported
`HERON-IMP-CLS-003` as producing `NOT_A_FOLDER`, a name that appears only in its suite's fixtures.

Everything else wrong with a contract — a missing description, a type that is not one of the eight, a
version that is not semver — is **asked of**
[`heron_contract.validate`](../brain/heron_contract.py) rather than judged again here.

A tool-layer agent and a C# one carry no contract, and that is the established shape of this repository
rather than a gap — 86 of them appear as **built without a contract**, not as unfinished. An agent
claimed by *nothing but its own suite* is shown too, flagged: `HERON-RAG-RIX-011` and
`HERON-RAG-DUP-012` are built and proved, and no file that implements them says so.

**Its count must equal [`agent-count.py`](agent-count.py)'s, and the suite checks that every run**, because
two bugs in reading a header got past everything else. A header line may claim **several** agents, comma
separated — 31 files do — and reading one whole turned the list into a single agent that exists nowhere
while every real one in it looked unclaimed. And a **C# header starts with `//`, not `#`**: the first
expression required a `#`, so it matched no `.cs` file at all and the whole `revit/` layer was invisible,
26 agents of it. Neither raised an error. The page simply described a smaller repository than the one it
was standing in.

It concludes, so it has a test ([`tests/test_contract_reference.py`](../tests/test_contract_reference.py)).

The working prototype of `HERON-DEV-DOC-017`.

---

## `generate-agent-map.py` — the visual map

```bash
python tools/generate-agent-map.py
```

Builds a self-contained, filterable HTML page of every agent, grouped by department and layer, from the
same registry. Writes `agent-map.html` in the working directory; set `HERON_MAP_OUT` to change that.

Filter by tier, filter to the build steps, search by name, ID or description. Colour encodes **cost** —
T1 quiet, T3 loud — so the expensive agents are visible at a glance.

Because it is generated, the map cannot drift from the registry. Regenerate rather than edit.

---

## Why these are committed

They are small, they have no dependencies, and they encode three rules the project already learned the
hard way:

1. **A stated count is a claim; a derived count is a fact.**
2. **A cross-reference that is not checked is a cross-reference that is broken.**
3. **A generated artefact cannot lie about its source.**
4. **Code no compiler has read is a draft, whatever the documentation calls it.**

---

## `check-signatures.py` — is anybody's signature sitting unused

```bash
python tools/check-signatures.py           # findings only
python tools/check-signatures.py --all     # including what is fine
```

A signature is the scarcest input this library has: **only a person may sign a proof**, and `accept`
deliberately does not promote. That gap is what this catches — a fragment the owner has already
signed, still sitting at DRAFT, so the next proving round offers it to be proved AGAIN. Thirteen had
piled up by 2026-09-13, which is the complaint that caused the tool to be written.

Exit 1 when a signature is being wasted, 0 otherwise. A **STALE** signature — signed, then the code
changed under it — is reported and is *not* a failure: that is [D-30](../docs/DECISIONS.md) working,
and the fragment must be proved again.

**It runs in CI, but not as its own step.** It lives inside [`check-docs.py`](check-docs.py) **§9**,
for exactly the reason §8 records below: this repository's `gh` token carries `repo` but not
`workflow`, so no session can push a step into `gates.yml` — **a gate nobody can install is not a
gate**. A commit that adds the step properly sits on the branch `ci/run-check-signatures` and cannot
be pushed. If that workflow is ever edited by hand, move it beside the others and delete §9.

**It existed for two days with nothing running it, and had no entry on this page.** The tool written
because a gate was missing was itself the one tool nobody had written down — found 2026-09-15 by
counting the `check-*` files against the sections here: seventeen exist, sixteen were documented.

## `check-gaps.py` — what is unfinished, and what is only waiting

```bash
python tools/check-gaps.py
```

One sweep over everything built — the build order against what is on disk, every test run, every
checker, every agent id the code claims against the registry, the fragment library, the capability
registry, the graph, **whether the brain is reachable from the host at all**, and the register — sorted
into **two lists that must never be one**:

| | |
|---|---|
| **UNFINISHED** | somebody could do this here, today. **The exit code follows this list only** |
| **WAITING** | needs a real Revit, or Windows, or a reachable network. Not work anybody can do here |

**Why the split is the point.** This repository's recurring failure is not bugs — it is an unproven
claim quietly ageing into a believed one. *"Nobody finished this"* and *"nobody has a Revit"* look
identical in one list, and a reader taught to skim past the second stops seeing the first.

**It found two bugs in itself on its first run**: it scans files for the `Heron-Agent:` field, its own
source contains that pattern, and it duly reported its own regex as two undeclared agents. A scanner
that scans itself finds itself. It now skips its own file and anchors the match to a comment line.

**And on 2026-08-29 it was found reporting a green it had not earned.** Every section asked whether a
thing *exists*; none asked whether anything *calls* it. So it passed the whole of Phase 2 while all
eight `brain/` modules, seven fragments and ten skills sat unreachable — imported by nothing but their
own tests, and therefore unusable from a conversation. The `THE BRAIN` section was added for exactly
that, and was validated in both directions before its result was believed: with an import present it
reports the call site, with none it reports the gap. **The general lesson is worth more than the fix —
a checker written from a build order can only ever ask whether the build order was followed.**

---

## `batch-prove.py` — many fragments proved in one pass, and neither half taken on trust

```bash
python tools/batch-prove.py tools/jobs/example.yaml --dry-run   # read it, run nothing
python tools/batch-prove.py my-jobs.yaml                        # run it against Revit
python tools/batch-prove.py my-jobs.yaml --only set-mep-size
```

Proving fragments one at a time costs a round trip each. On 2026-09-09 a throwaway script proved
fourteen in one pass, then lived in a temp folder and was retyped twice. This is that script, with the
two holes it had **closed rather than noted** — the working half of `HERON-DEV-RVT-013`, whose judging
half is [`brain/heron_validate.py`](../brain/heron_validate.py).

It takes a job file, runs `validate` per job, drafts what came back, judges it, and reports
one verdict per fragment - `PASS`, `POSITIVE EMPTY`,
`NEG NOT EMPTY`, `TIMEOUT` and the rest, each printed with what it means and what to do about it.

**It never accepts and never promotes**, and that is checked rather than promised: it reads every
fragment's `heron-status` before the batch and again after, and stops if one moved. Accepting a draft
is [`heron_validate.py accept`](../brain/heron_validate.py) and it takes a person's name, because that
name is the signature.

**Hole 1 — it re-proved finished work.** The first batch was 15 of 16 fragments already `PROVEN`,
because the names were picked off a capability list rather than filtered by status, and it cheerfully
reported six passes for it. A fragment at `PROVEN` or `PRODUCTION` is now refused, reported `ALREADY`,
and never sent to Revit. There is deliberately **no flag to override that** — re-proving after the code
changes is a staleness question, and [`tests/test_golden.py`](../tests/test_golden.py) already answers
it by recomputing fingerprints.

**Hole 2 — it passed fragments that had done nothing.** `heron_validate` judges whether the NEGATIVE
came back empty, which is [D-30](../docs/DECISIONS.md)'s leg and which a fragment that does nothing
satisfies without trying. So the POSITIVE is judged too, by one rule: **a declared result has to have
moved off zero.** `read-graphic-overrides` leaves an `OverrideGraphicSettings` **object**, and reading
that as *"not a count, therefore non-zero, therefore it did something"* passed a fragment whose only
real result was `0` in both legs. **Unreadable is not evidence of work** — the rule `_as_count` already
follows for the negative, applied to the positive.

`looks_empty` is **imported** from the validation agent, not copied. The runner and the drafter have to
agree about what empty means, and two copies of that judgement is exactly the drift this folder exists
to prevent. `generate-fragment-catalog.py` keeps its own regex on purpose and the difference is the
point: a page that *draws* may; a tool that *concludes* may not.

**The arrangement is not the tool's job, and the arrangement is where every failure came from** — a
selection of 307 where 22 would do, a positive case the model cannot fill, a category invisible in the
view chosen, a width given for a round duct. That is a brief rather than code:
[`.claude/skills/fragment-proving/SKILL.md`](../.claude/skills/fragment-proving/SKILL.md), with a job
file to start from in [`jobs/example.yaml`](jobs/example.yaml).

It concludes, so it has a test — [`tests/test_batch_prove.py`](../tests/test_batch_prove.py), which
runs both judgements against the records that fooled the original script, and dry-runs **every** fragment
the library currently holds at `PROVEN` to check each one is refused. That list is derived from the
library rather than typed, so it cannot go stale.

**The exit code follows the batch, not the fragments.** Fourteen failures is the output — the thing it
was run to find out — and exiting non-zero on them would make a successful proving run
indistinguishable from a broken one. What earns a non-zero code is a job file that cannot be run.

---

## `generate-jobs.py` — the derivable half of a job file, typed by something that cannot misspell

```bash
python tools/generate-jobs.py                                   # to the screen
python tools/generate-jobs.py --out tools/jobs/next.yaml
python tools/generate-jobs.py --risk MODIFY --limit 10 --out tools/jobs/modify.yaml
python tools/batch-prove.py tools/jobs/next.yaml --dry-run      # after filling the blanks in
```

`batch-prove.py` takes a **hand-written** job list, and the hand-written part is where the mistakes
are. **Six input names were mistyped on 2026-09-09 alone** — `widthMm` for `width`, `sortByFields` for
`sortFieldNames` — and a mistyped input name does not look like a typo when it comes back: the fragment
refuses, or binds nothing and reports zero, and both read exactly like a fragment that is broken. This
is [FRAGMENT-ISSUES 3h.4](../docs/FRAGMENT-ISSUES.md), fourth of the four things asked for at the end of
two days of proving.

Everything it emits is read out of a file, and the file is named in a comment beside it:

| Derived | From |
|---|---|
| which fragments to attempt | `heron-status: DRAFT` **and** no run record in `brain/proof-drafts/runs/` — never proved, and never yet in front of a model |
| `write: true` | the fragment's own `risk:`, against the risk `run_fragment_write` carries in [`HeronOperationRegistry.cs`](../platform/Heron.Core/HeronOperationRegistry.cs), ordered by the `HeronRisk` enum |
| the setup chain | whether the fragment needs anything a *fragment* provides. If it does not, it gets `setup: []` rather than a selection it never asked for |
| **the exact input names** | `contract.needs`, spelled out with the type beside each one. **This is the point of the tool** |
| what cannot be run at all | a need whose shape Revit has no way to receive is **marked with the reason**, not emitted as a job that would refuse the moment it reached the model |

**`write: true` is not a label this tool applies.** [Golden Rule 19](../docs/14-golden-rules.md) says the
risk of an operation is looked up **by name** in the registry and never supplied by a caller — and this
tool is a caller. So it reads two declarations and puts them side by side: what the fragment says it
carries, and what the registry says `run_fragment_write` carries. *"MODIFY needs the write path"* is
never typed here; it is read out, and if the registry is ever rearranged so the write executor is no
longer above the read one, **nothing is generated at all** and the refusal says why. A job file that
sends a write down the read path is the defect that cost half a morning on 2026-09-09, and it arrived
silently.

**What it refuses to derive is the more important half.** The category and the view are left **blank**,
marked `FILL IN`, with a comment saying whose job they are. They are judgement — which category this
model has, which view holds a small number of them — and a wrong category produces a *confident
meaningless result*, which happened **eleven times in one batch**. So does `expect:`, so does every
value, and so does the negative case: what is emitted is the **name**, correctly spelled, and the
names to choose an `expect:` from as a comment. It removes the errors a person makes while **typing**;
it does not remove the judgement a person has to make, and pretending otherwise would make it worse
than the hand-written file.

**Nothing is dropped silently.** A run against the library today emits 6 jobs and marks 108 fragments
that cannot be arranged — waiting on a risk Heron does not reach yet, on a shape
[D-54](../docs/DECISIONS.md) has no rule for, on a value the setup chain does not leave behind, or
having nothing to vary between the two legs at all, which makes them
[D-53](../docs/DECISIONS.md) tracking work rather than batch work. Those numbers move as fragments are
proved, which is why **there is no generated job file committed here**: a snapshot of a moving library
is stale by the afternoon, and this repository has already been bitten twice by a number typed into a
file that then stopped being true. Regenerate it; it costs a second.

It concludes, so it has a test — [`tests/test_generate_jobs.py`](../tests/test_generate_jobs.py). The
one that matters most reads the accepted types **out of** `RevitFragment.FromRequest` in the add-in and
fails when this tool's transcription of them disagrees, in either direction. The transcription exists
because the authority is C# and nothing in Python can call it; the test is what makes it a copy that is
**checked** rather than one that is remembered.

---

## `prove-skill.py` — is a SKILL proved, and the half that needs a Revit written out

```bash
python tools/prove-skill.py                      # both halves, all ten skills
python tools/prove-skill.py --plan-only          # the disk half, in seconds
python tools/prove-skill.py --skill count-elements
python tools/prove-skill.py --jobs tools/jobs/skills
```

The skill-level twin of `batch-prove.py`. A skill is proved when **both** halves hold: every utterance
reaches a capability the skill declares, and those capabilities, run in order on a real model, do what
the skill says — with a negative case ([D-30](../docs/DECISIONS.md)).
[`check-skill-routing.py`](check-skill-routing.py) measures the first and says so in its own last line:
*"Understanding is half a proof; the other half is a model."* This is the sentence after that one.

**It never prints PROVEN, and `tests/test_skill_proving.py` pins that.** No model is opened here, so the
furthest it reaches is `UNDERSTOOD` — half held, half owed. The verdict `CROSSING` outranks every other,
because a question answered by a write is not in better shape for having a tidy plan.

Between the two halves sits a third thing, read from disk and therefore stable between runs: whether
every declared capability has a provider, whether that provider is **PROVEN** (a skill cannot be proved
above what it rests on), whether the provider's risk **outranks the skill's own declared risk**
([row 140](../docs/FRAGMENT-ISSUES.md)), and whether the capabilities can be ordered so each one binds.
`--plan-only` is that half alone; the routing half asks the live store 43 times and takes about
twenty-five minutes, which is why `--routing-to` / `--routing-from` exist — a recording is printed back
with the index fingerprint it was taken against, and labelled a recording.

`receivable()` and `classify()` are **imported** from `generate-jobs.py` and `check-skill-routing.py`
rather than copied, for the reason `batch-prove.py` gives about `looks_empty`: two copies of a judgement
do not stay in step.

The emitted job file is `tools/jobs/example.yaml`'s shape, one per skill, with the steps in an order
that composes and `keep-chain: true` exactly where a need can only come down the chain. **Read its
header before running it** — it says how many of its own steps `batch-prove` will refuse as `ALREADY`,
which for seven of the ten skills is all of them ([row 141](../docs/FRAGMENT-ISSUES.md)).

---

## `measure-brain.py` — how long the brain takes, stage by stage

```bash
python tools/measure-brain.py
python tools/measure-brain.py --revit 2024 --requests 40
```

**The Revit side has been measured since Step 4 and the brain has never been measured at all.**
`HeronAudit` writes a duration per request and [`heron_gaps.py`](../brain/heron_gaps.py) reports median
and worst milliseconds per fragment and per operation. A grep for `perf_counter` or `monotonic` over
`heron_search.py`, `heron_embed.py` and `heron_retrieve.py` returns **nothing**, and none of them writes
to the trail. [docs/32 §4.2](../docs/32-master-architecture-reconciliation.md) is where that gap is
recorded.

**It is the half where the only real latency disaster has happened.** [D-49](../docs/DECISIONS.md):
closing register row `A7` installed a trained embedding backend, its import ran on the asyncio event
loop, and a Claude Code tool call sat on `heron_capabilities` for **thirty minutes**. An import costing
1.0 s in a fresh process, still running at 40 s there. It was found with `faulthandler`, by somebody who
noticed a hang.

So the import is timed **first and on its own** — and as **two lines**, which it was not until it was
re-read. `import heron_embed` is the module and is cheap everywhere. **`backend()` is where the trained
encoder actually loads** (`_load_model()`), and that is the 1.0 s that became 40 and then thirty
minutes. The first version timed only the module and called it the D-49 measurement; the cost happened
on the next line, **untimed**. Invisible on a machine without `model2vec` — which is not the machine
that matters. The report says which of the two cases it is looking at.

**Three things it refuses to do:**

| | |
|---|---|
| **It does not claim `HERON-OPS-OBS-011`** | The registry defines the Observability Agent as *"latency, token usage, model calls per request, cost per request"*. [D-01](../docs/DECISIONS.md) put every model call in the host and `heron_embed` runs a local model — no tokens, no account, no cost ([D-24](../docs/DECISIONS.md), [D-26](../docs/DECISIONS.md)). Three of those four fields are things Heron cannot see from here, so the header says `Heron-Agent: none` rather than letting `check-metadata.py` report the agent BUILT with three quarters of its job impossible. **The registry row wants correcting**, and that is the owner's call |
| **It is not a gate and exits 0** | A timing is not a pass or a fail, and there is no agreed budget to breach — [docs/19 §2](../docs/19-context-and-cost.md) proposes one and nothing implements it. A threshold taken from the first run would make whatever machine ran it the standard |
| **It will not report over the wrong library** | Same rule as `check-routing.py` and for the reason that tool learned the hard way: one store at `%APPDATA%\Heron\knowledge` serves every checkout on the machine, so a **count** can match while the store holds another session's fragments. It compares the **ids** |

**The first run already found something worth saying out loud.** `find()` came back in about a
millisecond and `retrieve()` took seven — because the questions are the fragments' **own declared
utterances**, so all twelve took Step 9's identity short circuit. That is the best case by construction
and the tool says so, with the count, under the table. A request phrased in somebody else's words costs
what `retrieve()` costs.

Every run prints the machine, the Python, the embedding backend and the fragment count, because a
number from a Linux container and a number from the owner's PC are not comparable and a table that did
not say which is which would be used as though they were.

---

## `measure-routes.py` — how often Heron answers without thinking

```bash
python tools/measure-routes.py
```

**The metric [D-58](../docs/DECISIONS.md) put in the registry**, replacing one Heron cannot see.
`HERON-OPS-OBS-011` asked for *model calls per request*; [D-01](../docs/DECISIONS.md) puts every model
call in the host, so Heron cannot count them. It can count how often **no model was needed at all** —
which is what [docs/19 §5](../docs/19-context-and-cost.md) actually cares about:

> Steps 1 and 2 must be tried **before** any model is invoked, structurally — not as an optimisation
> added later.

*Model calls per request* was a proxy for that rule holding. **The share answered by the identity or
cache route measures it directly**, from Heron's own side of the wire, with nothing to configure.

**Two numbers that must never be added together.** STRUCTURAL asks the library its own declared
phrasings and reports the routes — a **ceiling**, because real requests are phrased worse than the
phrasings a fragment writes for itself. LIVE reports what the utterance cache actually holds, which is
the only half that says anything about what people type.

**A short circuit that lands on a different fragment is named, not counted as a saving.** Route 1
answering confidently and wrongly costs more than a search;
[`check-routing.py`](#check-routingpy--did-a-new-fragment-make-an-old-one-unfindable) is the tool that
says which.

### What its first run found — and why it is parsed rather than grepped

**`remember()`, the only function that writes the utterance cache, is called from
[`tests/test_search.py`](../tests/test_search.py) and from no production code.** Not `ask()`, not
`find()`, not any MCP tool. So route 2 can never fire for a real user: the cache is built, tested,
indexed and permanently empty — the one [docs/19 §6](../docs/19-context-and-cost.md) calls *"the one
that pays for itself faster than any of the others"*.

**The first version of that check grepped for the text `remember(` and matched this tool's own
docstring**, which describes the problem — then printed *"the cache fills with use"*, the exact opposite
of the truth, in the one place the tool exists to be right about. It parses with `ast` now, and
[`tests/test_measure_routes.py`](../tests/test_measure_routes.py) holds that case along with prose in a
comment, `remembers()`, the definition itself, `__pycache__`, and a file that will not parse — which is
**reported** rather than skipped, because *"no production caller"* must never be an artefact of a file
nobody could read.

**Where `remember()` should be called from is [`Q-43`](../docs/OPEN-QUESTIONS.md), not a patch.** Caching
whatever the keyword route ranked first makes a **guess permanent**: the next identical wording returns
by route 2 and never searches at all.

Not a gate; exits 0. There is no agreed target to miss, and setting one from a first run would make
today's library the standard.

---

## `check-revit-gate.py` — the fourteen questions, run as a list

```bash
python tools/check-revit-gate.py                    # the whole library
python tools/check-revit-gate.py FRG-ELE-001        # one fragment, all fourteen
python tools/check-revit-gate.py --list units       # the names behind a count
```

**The questions are already this project's rules.** They are spread across [docs/03](../docs/03-heron-revit.md),
the [fragment-proving skill](../.claude/skills/fragment-proving/SKILL.md), [D-51](../docs/DECISIONS.md),
[D-53](../docs/DECISIONS.md) and [FRAGMENT-ISSUES.md](../docs/FRAGMENT-ISSUES.md), and **nothing ran
them as a list** ([docs/32 §4.3](../docs/32-master-architecture-reconciliation.md)). The proving skill
names five mistakes that account for nearly every failed proof; five of them are five of these fourteen,
which is the evidence that asking them in order pays.

**Four verdicts, and they describe evidence rather than lifecycle** — [docs/24](../docs/24-trust-model.md)
collapsed six status vocabularies into two axes and this adds no third: `ANSWERED` (read from declared
data or the compile record) · `BY DESIGN` (the architecture answers it for every fragment, and the
reason is named) · `LOOK` (a person should look, with why) · `NEEDS A RUN` (only a real model can say,
and which tool asks it).

### What it found, and what it proved it cannot do

**One real defect in 360 fragments.** `create-from-room-boundaries` took `heightAboveLevel` and never
said what the number meant. It is `Set()` straight into `CEILING_HEIGHTABOVELEVEL_PARAM`, which takes
Revit's internal **feet** and accepts a millimetre figure silently — so a caller who read *"how far
above the room's level"* and passed `2700` would get a ceiling 2,700 feet up and no error. That is
`D3`'s failure shape exactly. Fixed in the same commit; the check now reports zero.

**Question 3 was crying wolf on 42 fragments and now raises none.** It first asked *"does the contract
declare a document"* — and every one of the 42 that declares none was correct: `apply-view-template`
takes `views` and a `templateId` and needs no document at all. The question it asks now is *"does the
**code** use one the contract does **not** declare"*, which raises zero today and would still catch a
real undeclared need. Two refinements were needed to get there, and both were false positives that
looked exactly like findings:

| | |
|---|---|
| a header **comment** | almost every fragment says *"Assumes `doc` … are in scope"*, so a check reading comments finds `doc` everywhere and means nothing |
| a **local** | `zoom-to-elements` declares `uidoc` and writes `var doc = uidoc.Document;` — correct code, and reported as an undeclared need it would have sent somebody to edit a working contract |

**Question 7 went 114 → 6 → 1**, and the last cut came from opening all six. Five collect something
**project-level** — a fill pattern, a parameter filter, a family symbol, a view — which does not live in
a view at all, so a view-scoped collector would return **nothing**. Raising them asked somebody to make
a change that would break the fragment. The one that remains collects *instances* and is a genuine
judgement call.

**Question 7 went 114 → 6.** A whole-model collector is usually the job, so raising all 114 was raising
the shape of the library. What is an **inconsistency** rather than a design is a fragment handed a
`view` that never scopes to it — 6 of them, and even those are sometimes right, which is why they are a
LOOK. The 114 has not been thrown away: it is said in the answer, because it is what makes a **proof**
slow — `set-mep-size` timed out on 307 ducts and sized 22 immediately in a smaller view.

**All three of these tools load fragments through
[`heron_fragment.load_all()`](../brain/heron_fragment.py)**, not their own `yaml.safe_load`. Each parsed
the library itself at first, with `except Exception: continue` — so a malformed `fragment.yaml` vanished
from a report that counts fragments and nothing said so. [D-48](../docs/DECISIONS.md) settled that one
broken part costs one part **and is named**; `load_all()` returns its problems and each tool prints them
above everything else.

**Question 14 went 143 → 59, and the rule came from the library rather than from reasoning.** A
fragment that **writes** names what it refused **172** times out of 202; one that **reads** does it 45
times out of 158. **85% against 28%** — the norm exists and is not uniform, and reading is where both
the silence and the plausible zero live. So it asks the shape [D-52](../docs/DECISIONS.md) is actually
about: a fragment that **goes looking** and can **drop** something on the way. `filter-elements-by-type`
returns `found: 0` when its exemplar has no type and nothing separates that from *"there are none of
this type"* — which is exactly why `FILTER_ELEMENTS_BY_CATEGORY` reports `unresolvedLevel`. One
fragment already solved this; 59 have not. [`Q-46`](../docs/OPEN-QUESTIONS.md).

**Question 12 went 7 → 0.** It asked *does it guard against null* and raised seven fragments that
touch nothing nullable — `count-elements` counts a list it was handed, `set-selection` selects one,
`group-and-count` groups one. It asks now whether the code dereferences something **Revit can hand back
as null** — `GetElement`, `get_Parameter`, `LookupParameter`, a cast with `as`, `.Level` — without
checking, and names which one.

**Question 8 went 310 → 107 → 62, and the last cut is an API fact rather than a judgement.** It first
asked every fragment about links and raised 310 of 360 — the shape of the library, not a finding; a
fragment that sets a view's scale has no link question to get wrong. Narrowed to fragments that
**collect**, it raised 107. Narrowed again to those that **read**, 62 — because a **linked element
belongs to another document and cannot be changed through the host**, so a writer collecting the host
only is not under-reaching the way a reader is, and 45 fragments were on a list they could do nothing
about.

**Those 62 are the one finding on the whole list that is about what a modeller sees.** In federated MEP
work — the normal case — a fragment that collects only the host returns a **confident smaller number**
and nothing in the answer says a link was skipped. That is [`Q-48`](../docs/OPEN-QUESTIONS.md), and it is
a design question with three genuinely different answers, not a defect with a fix.

**It cannot decide whether a fragment writes, and the attempt is recorded because the failure is
instructive:**

| Attempt | Result |
|---|---|
| search for `.Create(` | flagged three `READ` fragments — **all three wrong.** `CurveLoop.Create`, `Line.CreateBound` and `GeometryCreationUtilities.CreateExtrusionGeometry` build geometry in **memory**. In the Revit API *"Create"* is not a write signal |
| narrow to calls taking `doc` | found **zero** mislabelled fragments, and missed **76** `MODIFY` ones — Revit writes through typed methods on typed objects: `view.HideElements(ids)`, `view.Scale = 2`, `param.Set(v)` |

**The write surface is the API, and no word list is the API.** The real answer already exists and is
better than any text search: `RevitFragment.Run` opens **no transaction** for a read, so Revit itself
refuses the change — enforced by the host rather than asserted by a checker. The tool says so instead of
competing with it, and question 13 answers differently for a reader and a writer.

**Not a gate; exits 0.** A finding is a question for a person, and a tool that failed a build over
*"this collector has no view"* would teach people to write worse collectors to buy a green tick. None of
it is a proof — [D-30](../docs/DECISIONS.md) needs a real model, a negative case and a fingerprint.

A count of 100+ is printed as a count, not a list: 114 whole-document collectors is the shape of the
library, and a tool that dumps 143 rows teaches people to scroll past it. `--list` names them when
somebody actually wants them.

---

## `check-narrow-errors.py` — a broken database reported as an empty one

```bash
python tools/check-narrow-errors.py
```

**[D-52](../docs/DECISIONS.md)'s plausible zero, in the one shape it keeps arriving in.**
`except sqlite3.OperationalError: return []` is written for the honest case — the table has not
been created yet — and swallows every other case with it. A **locked** database, a **malformed**
file, a **missing column**, a schema older than the code: each one comes back as *"no document is
indexed in this scope — put one in"*, which is a sentence nobody doubts, about a store that is full
and broken.

**It is a tool because four reviews found it four times, in four files, all on 2026-09-11** —
`heron_retrieve.documents()`, then `heron_graph`, then `heron_search.index_chunks`, then
`heron_retrieve.find_documents`. Each was a copy of a line already corrected somewhere else. A defect
that arrives one file at a time is not a defect, it is a shape, and a shape is something a command
can look for.

It asks only that a handler **distinguishes the normal case from a fault** before swallowing —
a check on the message and a `raise` — not any particular wording. Exits 1 on a finding.

---

## `check-reachable.py` — built, tested, and called by nothing but a test

```bash
python tools/check-reachable.py
python tools/check-reachable.py --all     # including what is already explained
```

**Twice on 2026-09-09 the same defect was found by hand, hours apart.**
`heron_search.remember()` writes the utterance cache that
[docs/19 §5](../docs/19-context-and-cost.md) makes step 1 of the whole pipeline, and it is called from
one test and nowhere else, so the cache can never fill ([`Q-43`](../docs/OPEN-QUESTIONS.md)). Then
`heron_context.assemble()` was built the same day and reachable only from a command line, until it was
put on the MCP seam. Neither is a bug — both are complete, tested code no production path touches, which
[`heron_brain.py`](../mcp/server/heron_brain.py)'s own docstring already names: *complete, tested, and
invisible to any conversation is not what "built" was meant to mean.* **Nothing was looking for the
shape.**

**A hit is a candidate, not a defect**, and that has its own flag. Some are deliberate and written down
— the Workflow Engine most clearly, where `HANDOVER.md` says *"nothing calls it yet, and that is
deliberate"* because its customer is a later phase. Those are separated so the top of the report is only
what nobody has explained.

**How many, and which, is derived and not typed here** — `python tools/check-reachable.py --all`.
When the check was first written on 2026-09-09 it found 12, of which 5 were already recorded and one —
[`Q-47`](../docs/OPEN-QUESTIONS.md), `heron_capability.want()` — was not. **That sentence stood in the
present tense until 2026-09-22 and both halves had gone**: Q-47 was answered on 2026-09-09, `want()`
was wired up, and it is not reported at all any more. The tool has a section for exactly this failure
in its own `RECORDED` list; its README did not.

### Why it parses instead of searching, learned three times in one night

A CLI subcommand is reached by name, not by a `foo()` in the source, so a naive check calls every one of
them dead. The precise test is a **dict literal whose value is the function** or a **`getattr` with a
literal name** — structures, which prose cannot produce.

**What goes in is the FUNCTION, never the key, and the `getattr` name is the SECOND argument.** Both
were wrong until 2026-09-22 ([row 5b-149](../docs/FRAGMENT-ISSUES.md)). Every real table in this
repository is shaped `"accept": cmd_accept` — key and function are never the same word — so storing the
key suppressed nothing, while matching any `{str: Name}` swallowed the result dict and hid 52 of the
400 public production function names.

That precision was arrived at by getting it wrong three times, and all three are the same failure:

| | |
|---|---|
| `check-revit-gate.py` | compared a **space-stripped haystack** against a needle that still had spaces, and reported a confident **0** where the answer is 114 |
| `measure-routes.py` | grepped for the text `remember(` and matched **its own docstring**, which describes the problem — then printed the opposite conclusion in the one line it exists to be right about |
| this tool, first version | treated any string literal `"remember"` as dispatch. `measure-routes.py` contains one, in the `ast` comparison that finds callers of `remember`. **The tool written to find the problem made the problem invisible to the next tool** |

Three heuristics, three times fooled by text *about* the thing rather than the thing.

**On 2026-09-09 it went 12 hits → 4, and the cuts were its own false positives.** It reported a **nested closure**
(`heron_bridge_client.reader`, handed to `threading.Thread`) and a **class method**
(`heron_health.worst`) as *"called by NOTHING AT ALL"* — neither is a module's public surface, and a
method reached through an instance cannot be attributed by name at all. It now looks at module-level
definitions only. It also counts a **qualified reference** as a use: `SEARCH.remember` handed to
something else is a use, not a call.

**One of those fixes broke it in the permissive direction and the difference matters.** Counting *every*
bare name as a use lost `want()` — [`Q-47`](../docs/OPEN-QUESTIONS.md)'s whole subject — to **local
variables called `want`** in three unrelated modules. A bare name counts only where the file imported
it. A check that is wrong permissively reports nothing, which is the worse direction.

**An excuse that no longer applies is reported as stale.** A `RECORDED` entry that is no longer a hit
means something now calls it, and the excuse has outlived its reason — [D-54](../docs/DECISIONS.md)'s
lesson applied to this tool's own record. Without it, `remember()` would go on being excused for ever
after somebody wired it up.

**What it cannot see:** a function reached through `globals()`, a registry built at run time, a plugin
loader, or a name assembled from parts. Absent from the source is not the same as unreachable, and it
says so. Not a gate; exits 0.

---

## `measure-graph.py` — does the graph help retrieval? It does not

`Q-52`, run rather than argued. [33 §5.16](../docs/33-external-repository-research.md) records
`gbrain` reporting **+31.4 points P@5** from a graph retrieval stream. Heron's graph is a different
object — `composes_into` / `composes_from`, derived from the contracts — so the direction was evidence
and the magnitude was nothing. **This is Heron's own number.**

```bash
python tools/measure-graph.py           # one setting
python tools/measure-graph.py --sweep   # six, which is the point
```

**The answer key is the library itself.** Every fragment declares a `semantic-identity` — one sentence
that should return it. 360 questions whose right answer is known because **nobody wrote it to make
retrieval look good**: it has been the matching text since Step 7.

**It is also easy**, so three degraded shapes are measured beside the exact one — the first word
dropped, only words of four or more characters, the first half of the sentence. **Those are where a
graph could earn its place**, because they are the cases where the right answer is not already first.

### What it found

| shape | P@1 today | with the graph | |
|---|---|---|---|
| `exact` | **95.6%** | 94.4% | −1.1 |
| `no-first` | **93.1%** | 92.2% | −0.8 |
| `content` | **90.4%** | 89.0% | −1.4 |
| `half` | **70.8%** | 69.2% | −1.7 |

**Six settings, six losses** — weights 0.05 / 0.10 / 0.30 against 1, 3 and 5 seeds. The gentlest costs
1.1 points of P@1, the strongest 14, and **P@5 never improves at any of them.**

**The reason is one line of the output:**

> neighbours per fragment: **median 50, worst 230**, none at all for 68 of them.

**A dense graph is not a retrieval signal.** A page mentions three people; a fragment providing
`IList<Element>` composes with most of the library. *"The neighbours of the best hit"* is a large slice
of the library added as competitors.

### Two things it does on purpose

**It writes its prediction down before the run** — *a gain must come from the degraded shapes, and a
loss will show first in P@1 on the exact one* — so the result cannot be read as whatever was hoped for.
**A tool that can only report good news is not a measurement.**

**It computes the neighbour map once.** The first run called `composes_into()` per seed per query and
took **275 seconds**; the map takes **fifteen**. That is what made a six-setting sweep affordable, and a
sweep is the difference between *"the graph lost"* and *"the graph lost at every setting tried"*.

**Not a gate; exits 0.** It measures whether the fragment whose own sentence was typed comes back
first — a proxy chosen because it is honest and available, not because it is the question.
[D-30](../docs/DECISIONS.md) needs a real model, and nothing here has met one.

---

## `check-change.py` — does the change do only what it said it would

```bash
python tools/check-change.py --intent "one line" --area brain --risk low
python tools/check-change.py --intent-file docs/work-notes/fixes/a-note.md --base main
python tools/check-change.py --intent "..." --area tools --risk medium --evidence after.json --json
```

**Every other gate here asks about the repository. This one asks about the diff.** A change that
compiles, passes and quietly rewrites three unrelated subsystems is exactly the shape nothing else
notices, and it is the shape an AI-written change arrives in most often.

### Three fields, and the reason it is not ten

`intent`, `area`, `risk`. The plan this came from listed ten. [29 §4](../docs/29-metadata-standard.md)
sets the test for a new field — *would a script fail the build over it?* — and only these three survive
it. **Everything else the plan wanted is derived from the diff instead**: whether this needs a real
Revit, which releases it touches, whether a public contract moved, whether delivery is affected. A
field a script cannot act on belongs in prose, and a derived fact beats a declared one.

**An intent that cannot be used stops the run** — exit 2, `BLOCKED`, nothing judged. An area that is
not a part of this repository is a typo or a misunderstanding, and a confident report built on either
is worse than no report.

### Why the comparison is structural rather than lexical

The obvious way to check a diff against a one-line intent is to count words shared between the sentence
and the path. It is cheap, and it calls `heron_retrieve.py` unrelated to *"fix retrieval"*.

Heron does not have to guess. [`check-structure.py`](check-structure.py) already holds the table of who
may depend on whom, enforced on every push, so this **imports that table** and asks a structural
question instead:

| | |
|---|---|
| `required` | the file is in a part the change declared |
| `supporting` | the file is in a part a declared part **may depend on** |
| `tests` · `documentation` · `build/config` | counted, never questioned |
| `unrelated` | neither — and this is the only class that raises |

**The `supporting` row is the one that earns its keep.** An intent declaring `mcp` and a diff touching
`brain/` is supporting work, because `mcp` may depend on `brain`. The same intent touching `revit/` is
not, because it may not — and that is precisely the change a person should look at.

`build/config` is tested **before** part membership, deliberately: a change to how Heron is built or
installed is the one class that must never hide inside another, because it is what reaches a modeller's
machine.

### Signals decide which gates the change owes

A signal is a fact about the diff, not a verdict, and each names the paths that raised it so the claim
can be checked in one look. Touching a permission file owes a human review **whatever risk was
declared** — the diff outranks the label. Touching a fragment owes a run against a real model, because
[D-30](../docs/DECISIONS.md) is not satisfied by a compile.

**It sets the homework and does not mark it.** Which gates ran is read out of an evidence record; this
tool runs none of them. A tool that did both would one day mark its own.

### The exit codes

| | |
|---|---|
| 0 | `PASS` |
| 1 | `SPLIT` · `REVISE` · `REVERT` |
| 2 | `BLOCKED` — the intent is unusable, so nothing was judged |
| 3 | `NEEDS REAL REVIT PROOF` — the same code [`tests/README.md`](../tests/README.md) uses for *could not run here*, and for the same reason: it must never read as a pass |

**A change with no evidence record is `REVISE`, never `PASS`.** That is the whole point of the tool
having two halves.

**A gate that was already failing does not block** — but only when the evidence record was compared
against a measured baseline, which is what `compared_to` in the record means. With no before-measurement
the same record blocks, because nothing can tell a pre-existing failure from a new one and the tool says
the cautious thing rather than the convenient one. It exists for a gate that was **already** red for
a reason nobody here caused — a broken install, a gate red on the base branch too. **The example this
paragraph used to give was the one case it is not needed for**: it said *"two suites here exit 1 for
want of the MCP SDK, so the tests gate reads FAIL on a plain container"*, and measured on 2026-09-21
with the SDK made unimportable, both exit **3** and the tests gate reads **PASS**
([row 5b-60](../docs/FRAGMENT-ISSUES.md)). The mechanism is the one
[`gates.yml`](../.github/workflows/gates.yml) uses: compare a **set** against a named list rather than
counting anything.

---

## `change-evidence.py` — before and after, measured the same way twice

```bash
python tools/change-evidence.py capture --out before.json --tests all
python tools/change-evidence.py capture --out after.json --tests all --against before.json
python tools/change-evidence.py compare before.json after.json
```

**"Better" is a comparison**, and until this existed there was nothing a person could run mid-task to
hold the two states side by side. CI had half of it — `gates.yml` compares a **set** against a named
list rather than counting anything — and that half is the good half, taken deliberately: a total hides
a regression that arrives the same day something else is fixed. (That list names the suites which
**cannot run** there, since [row 5b-49](../docs/FRAGMENT-ISSUES.md).)

It records only what a command derives — gate exits, the suite map, and the counts this repository has
been wrong about in prose. **Not a dump of the tree.** A record big enough to hide a change in is a
record nobody reads.

### Three rulings, and the third is the one usually missing

| | |
|---|---|
| `REVERT` | something that passed before does not pass now |
| `KEEP` | something that failed before passes now, and nothing broke |
| `NO CHANGE MEASURED` | neither — **not** *fine*, and **not** *better* |

One fix and one break is `REVERT`: a fix does not pay for a regression.

**Exit 3 is neither side of the comparison.** A suite that could not run before and passes now means
somebody installed something, not that the code improved, and it is reported as *not comparable* rather
than counted. Two unknowns are not a match.

### A claim and a measurement are different kinds of fact

`--stated check-compile=PASS:ran on the PC` records a gate that ran somewhere this container is not —
a .NET SDK, a Windows machine, a Revit. It is stored marked **stated**, never **derived**, and printed
that way. A record that let an assertion sit beside a measurement in identical type is a record in
which the assertion eventually gets believed as one.

### It cannot change anything, and that is the safety property

The improvement loop this serves is *measure → change one thing → measure again → keep or revert*. This
tool owns the two measurements and the ruling. **It owns no mutation**, so no prompt, no fragment
description, no routing hint and no line of code can be rewritten by anything in it — which is what
keeps a measuring tool from quietly becoming an editor.

---

## `check-package.py` — would the thing we deliver actually install

```bash
python tools/check-package.py
```

**Nothing else in this repository reads `Heron.addin`.** Rename the entry class and every test still
passes, all eight releases still compile, and Revit says *"cannot run the external application Heron
AI"* with nothing to say why. That failure reaches a modeller and reaches nobody else.

It asks the delivery questions that can be asked honestly on a machine with no Windows, no Revit and no
compiler — among them the four that are fatal and otherwise invisible:

| | |
|---|---|
| a `FullClassName` naming no class, or one that is not an `IExternalApplication` | Revit refuses the add-in |
| an `<Assembly>` the project does not build | the same |
| a `<ManifestSettings>` element | **crashes Revit 2025 and older**, and one manifest is deployed to all eight releases |
| a manifest the deploy rewrite no longer matches | `String.Replace` does not fail when it matches nothing — it installs pointing at the wrong path |

It also holds the promises: **per-user install on every release**, one script owning the install path,
and Autodesk's assemblies never redistributed.

### The release table is evaluated, not searched

Its first run raised Revit 2022 and 2023 as missing from `Directory.Build.props`. They are not: one row
covers `>= 2021 AND <= 2024`, and the check was looking for the literal year. That is the crying-wolf
failure `check-revit-gate.py` records at length above — a finding that sends somebody to add a row that is already there. **The conditions are
evaluated now**, and one this cannot parse is reported rather than assumed true.

### What it deliberately cannot say

Whether Heron installs. Whether an upgrade keeps a user's settings. Whether a rollback recovers.
Whether Revit finds the manifest. Each needs Windows and a Revit, and **the tool prints them as still
owed on every run** — because a green run here is not an install and must never be reported as one.

---

## `check-dependencies.py` — which packages Heron has, and what each one buys

```bash
python tools/check-dependencies.py
```

**Added by another branch on 2026-09-11 and missing from this file until 2026-09-12**, when an
independent review of the housekeeping work counted the sections against `ls tools/*.py`. The paragraph
at the top of this file is about a number going stale; this is the same failure with the number removed
— a tool nobody could find from the one document that lists them.

**Exits 1 only when a REQUIRED package is missing.** A missing optional one exits 0, because that is
what optional means and a checker that failed on it would be arguing with R-42's silent degradation.

It reads [`requirements.txt`](../requirements.txt) and
[`requirements-optional.txt`](../requirements-optional.txt) — **the same two files `pip` reads**, not a
second copy. That is the whole design: a list cannot go stale if nothing types it twice.

It exists because of one question the owner asked on 2026-09-10 — *does a new person installing from
GitHub get this automatically?* — and the answer was no, twice. `setup.ps1` installs no Python package
at all, and the only list anywhere said `pyyaml` while the code imports six things. Somebody following
the instructions exactly got the weaker retrieval backend and was told nothing. **Silent degradation is
correct; silent degradation plus an install list nobody can follow is how a person stays on the weaker
backend believing they are on the better one.**

**It does not install anything**, and it does not check that an installed version is the right one.
Saying so is better than a tool that half-installs.

## `owner-queue.py` — what is waiting on the OWNER, derived rather than typed

```bash
python tools/owner-queue.py
```

Reads [`OPEN-QUESTIONS.md`](../docs/OPEN-QUESTIONS.md), [`NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md)
and [`PROPOSALS.md`](../docs/PROPOSALS.md) **as they are right now** and groups everything waiting on
the owner by **what he has to have in front of him** — a decision, a numbered document, Revit open, the
PC, or a network that can reach a model host. [`docs/FOR-THE-OWNER.md`](../docs/FOR-THE-OWNER.md) is the
structure; this is the content.

**It exists because every typed version of this list has gone stale.** Three of them on 2026-09-12
alone: `OPEN-QUESTIONS.md` said *"1 open"* while three were, `NEEDS-CHECKING.md` said *"two rows have
been added"* while it was six, and `HANDOVER.md` §1–§3 described a repository of 7 `DRAFT` fragments and
17 suites against 360, 197 proven, and 57. None was wrong when written. Each was written once and never
re-derived.

**It decides nothing.** A row it cannot classify prints under **UNCLASSIFIED** rather than being
dropped — a queue that silently loses an item is worse than no queue — and each line is a *summary*,
with the register it names remaining the authority.

**It always exits 0.** A list of work waiting on a person is not a build failure, so it is a report and
never a gate.

**The open-question rule is copied from `check-docs.py` deliberately**, so the two cannot disagree about
what "open" means: no `✅` in the heading, and no real text after an `**Answer:**` marker.

**One thing it has that most tools here do not:** it reconfigures stdout to UTF-8 and degrades to
character replacement if it cannot. The registers are full of em dashes, the owner runs this on Windows
where the console is cp1252, and the first run printed `server receives ?` — which is `test_ingest`'s
failure (`A14`) reproduced inside the tool written to report it.

## `generate-decision-summary.py` — the decision index, rebuilt from the decisions

```bash
python tools/generate-decision-summary.py            # rewrite in place
python tools/generate-decision-summary.py --check    # exit 1 if stale
```

Rebuilds the **Status summary** table at the top of [`DECISIONS.md`](../docs/DECISIONS.md) from the
`## D-NN — Title` headings and each decision's own `**Status:**` line.

**It exists because that table was hand-written and stopped at `D-50` while the file reached `D-70`** —
**twenty decisions missing from the index of decisions**, including `D-70`, which the owner had answered
the day before. Nobody removed them; the table simply stopped being updated and nothing could notice.
[PROPOSALS](../docs/PROPOSALS.md) `F3` had recorded it as *"stops at D-50"* without measuring how far
behind it had fallen.

**A status cell that already exists is kept VERBATIM.** *"✔ read back 2026-09-06"* records a
conversation, not a fact on disk — it is not derivable and must never be regenerated away. The tool only
builds rows that do not exist yet. **That is the difference between generating a file and overwriting
one**, and a version that discarded those dates would have looked like tidying.

**The gate is inside [`check-docs.py`](check-docs.py) §8, not in `gates.yml`** where the other three
generators are diffed. The reason is worth stating: this repository's `gh` token carries `repo` but not
`workflow`, so no session can push a change to that workflow file — **a gate nobody can install is not a
gate**. `check-docs.py` already runs inside *The gates that must pass*, so this rides in with it. Moving
it beside the other generators would be tidier and would catch exactly the same thing.

**§9 now rides in the same way, for the same reason** — `check-signatures.py`, added 2026-09-15. Two riders is the point at which this stops being a neat trick and starts being a queue: if the workflow file ever becomes editable, both should move out and this note should go with them.

**Proved by breaking it**: deleting the `D-70` row makes `check-docs.py` exit 1 naming `D-70`, and
restoring it returns to 0.


---

## Three tools that were in this folder and not on this page

**Found 2026-09-16 by diffing the folder against the page**, which nobody had done. The check is one
line and it is worth keeping:

```bash
# every tool on disk that this page never names
for f in tools/*.py; do
  n=$(basename "$f")
  grep -q "$n" tools/README.md || echo "UNDOCUMENTED: $n"
done
```

**It checks one direction on purpose.** The first version of this command diffed both ways and
reported twenty false extras - `grep -oE '[a-z-]+\.py'` splits `heron_architect.py` at the
underscore and hands back `architect.py`, a file that does not exist. The direction that matters
is *on disk and unnamed here*; a name on this page with no file behind it is what `check-docs.py`
already catches. **Prove the pattern can see what you know is there** - this repository's own
rule, and the first version of this very command broke it.

**It runs by itself now.** Since 2026-09-23 `check-docs.py` section 10 does this for every file in
`tools/` - not only `*.py`, which is how three of nine went unseen - and for every skill folder in
`.claude/skills/`, and it fails the run.

**A tool nobody can find is a tool nobody runs**, and this repository has already paid for that once:
`check-signatures.py` existed for two days with nothing calling it, and its own commit message named
the defect - *"a gate nobody runs is the same as no gate."* Being undocumented is the quieter version
of the same thing.

## Nine tools this page did not describe, until 2026-09-23

**Found on 2026-09-22 by a reader comparing this folder with this page, and made a check the next day**
(`check-docs.py` section 10). Eight were named nowhere here; `balance-of-work.py` was named once, in
passing, in another tool's section. Each tool's own header says more - this is where to start.

| Tool | What it is for |
|---|---|
| [`HeronRevit.ps1`](HeronRevit.ps1) | Dot-sourced by the install scripts rather than run: which Revit releases are installed, which are open, and where each one's add-ins go - asked **per release**. **It never closes Revit**: an open Revit is named and the install stops, because an open model almost certainly holds unsaved work |
| [`balance-of-work.py`](balance-of-work.py) | What is left to do, in one place - read back from the tools that each know part of it (`check-gaps`, `owner-queue`, `open-defects`, `agent-count`), every figure beside the command that derives it. `--write` regenerates the note. A report: always exits 0 |
| [`build-release-assets.py`](build-release-assets.py) | Builds every product for every Revit release, in **Release** configuration only, and lays the results out as a GitHub release's assets - one zip per product per release, named from the manifest. **Nothing in it names a product**: the list is read from `platform/heron-products.json` |
| [`check-products.py`](check-products.py) | **One of the gates CI decides on.** The product manifest nothing compiles: a duplicate `addInId` (a load failure, not a warning), a `partOf` typo (a tick vanishes from the installer with no error), and a Revit release the repository does not build |
| [`cloud-setup.sh`](cloud-setup.sh) | Not run from a checkout: **pasted into a Claude Code cloud environment's setup box**. Installs the .NET 10 SDK, `python-is-python3` and the Python packages, gives the session somewhere to keep knowledge, and warms the store and the NuGet cache. It must exit 0, so its last lines name whatever did not arrive; [`test_cloud_setup.py`](../tests/test_cloud_setup.py) runs it in a sandbox against the refusals of 2026-09-22 (row 5b-167). [38](../docs/38-the-cloud-environment.md) says what goes in the other boxes |
| [`deploy-addin.ps1`](deploy-addin.ps1) | Copies the built add-in and its manifest into the current user's Revit add-ins folder - **no administrator rights** - and refuses an assembly built for another release's runtime. `-Remove` uninstalls, `-Rollback` puts back the install it last replaced. Revit must be closed |
| [`module-reach.py`](module-reach.py) | Who imports each module in `brain/`, in four buckets, and which ones a conversation can actually reach by following imports from `mcp/`. **It decides nothing**: a report, exit 0 |
| [`prove-agent.py`](prove-agent.py) | The proof path for an **add-in agent**, which the fragment tools cannot prove: sessions, a tracking run, a draft, and a person's acceptance **by name**. The machine never signs, and the tool has no write path into `revit/` |
| [`prove-tracking.py`](prove-tracking.py) | [D-53](../docs/DECISIONS.md) tracking for a **fragment**: runs it across several values of one input and shows the declared result following the input - the rows `heron_validate.py` has always judged and nothing had ever produced. `--dry-run` first |

## `check-declared-questions.py` — does a WRITE claim a QUESTION in writing?

```bash
python tools/check-declared-questions.py
python tools/check-declared-questions.py --all    # also the instructions
```

Always exits 0. It reports; it does not gate.

**Neither routing sweep can see this one.**
`check-routing.py` asks each
fragment's own utterances back to the search and separates *a sentence a READ claims, answered by
something that WRITES*. `check-risk-crossings.py`
asks sentences **nobody** declares, because the first can only test what is declared.

A write that DECLARES a question falls between them. Ask the search and the declaring fragment
**wins, by `identity`**, which short-circuits before any ranking runs — so `check-routing` sees a
fragment answering its own sentence and calls it correct; and the sentence *is* declared, so it is
not one `check-risk-crossings` was written to try. **No ranking change repairs one.**

Found while checking whether four crossings had a READ to give them to
([row 146](../docs/FRAGMENT-ISSUES.md)): *"what scale is this view"* resolves to `SET_VIEW_SCALE`,
a **MODIFY**, by `identity` — because that fragment declares the phrase in its own `utterances:`
block.

**It reads the FILES and never the store**, so it holds in CI where there is none, and two runs
disagree only if somebody edited a fragment. The write line is read from `HeronOperationRegistry.cs`
through `generate-jobs.write_threshold()` — Golden Rule 19, never typed — and a risk `HeronRisk`
does not name is **reported rather than assumed safe**.

**An imperative is not a question**, and the two that cost false findings the first time this was run
by hand are pinned in the suite: *"do the grayout"* opens with a word a careless pattern reads as an
auxiliary, and *"which elbow this type inserts, change it"* asks and then says what to do. The second
is listed separately, never counted — the same separation `check-risk-crossings.py` makes.

**It never suggests deleting an utterance to tidy the report.** [Row 113](../docs/FRAGMENT-ISSUES.md)'s
forbidden move is weakening a declaration to buy a number, and the mirror of it is deleting a sentence
a modeller really says so a sweep comes back clean. The repair is declaring the sentence on the READ
that should own it — and where no READ exists, the finding is a **capability gap**, which is a
different and larger thing.

**And it reads the SKILLS through the same rule**, adapted in four lines rather than copied — a
skill declares a risk and a list of utterances just as a fragment does. That is exactly where
[row 137](../docs/FRAGMENT-ISSUES.md) said the blind spot was: a crossing compares a sentence's
reach against the **skill's own** declared risk, so a question inside a MODIFY skill never
registers as one. This asks nothing about reach. **Five today**, and **some of them are probably
right** — *"how many sprinklers do I need"* may genuinely belong to a layout skill, because
answering it IS the layout. The tool says so in its own output rather than asking for a deletion.

**It concludes, so it has a test** ([`tests/test_declared_questions.py`](../tests/test_declared_questions.py)),
and every sentence in it is a real declared utterance rather than an invented one — an invented
sentence would only prove the pattern matches itself.

---

## `open-defects.py` - how many of Heron's own defects are still open

```bash
python tools/open-defects.py            # the open ones, by id
python tools/open-defects.py --all      # every row, with its state
```

Reads sections **5 and 5b** of [`docs/FRAGMENT-ISSUES.md`](../docs/FRAGMENT-ISSUES.md) and prints the
**ids** of every row whose Status begins with *open*. Always exits 0 - it reports, it does not gate.

**The two sections are counted apart, and that is the point.** Section 5 is what **proving against a
model** found; **5b** is what **reading the repository file by file** found. They are re-tested
differently - a proving defect by running the fragment again, a reading defect by reading the file
again - and [`review-ledger.py`](#review-ledgerpy---which-files-have-been-read-word-by-word) knows
when the second is due. 5b's ids print as `5b-3` rather than `3`, because a bare number would now be
ambiguous and a defect nobody can find is the failure this register exists to prevent.

It exists because that section was headed *"six still open"* from the day it was written until
2026-09-16, and by then ninety-four rows had been appended and twenty-nine were open. **No append was
careless**: each session wrote an honest row and left the heading to somebody else. That is the
prose-total drift [`NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) records against itself seven times.

**It prints the ids rather than the total on purpose**, and says in its own output what it cannot see:
a row whose Status still reads OPEN after a LATER row closed it. Four were that shape on 2026-09-16
(rows 37, 44, 96, 97) and no pattern finds them - the closure is written in a different row, in prose.
**This narrows the pile you have to read. It does not replace reading it.**

**And it now asks about a row that argues with itself** - a Status that begins with *open* and carries a **dated** `FIXED` or `CLOSED` claim further down the same cell. Row 127 was that shape for an evening: the fix was appended under the sentence saying OPEN rather than replacing it, so the row was fixed while every count and every list went on calling it open. The test is deliberately narrow - a dated claim in capitals, inside one sentence, not the word *fixed* in passing - because eleven open rows mention something else being fixed or closed and **every one of them is genuinely open**. It asks; it never re-counts, because which sentence is the state is a reader's judgement and a tool that guessed would start closing rows. [`tests/test_open_defects.py`](../tests/test_open_defects.py) pins both halves.

---

## `archive-fragment-issues.py` - move finished defect rows out of the live register

```bash
python tools/archive-fragment-issues.py            # what would move - writes nothing
python tools/archive-fragment-issues.py --write    # move it
```

Moves the **finished** rows of sections **5 and 5b** of [`docs/FRAGMENT-ISSUES.md`](../docs/FRAGMENT-ISSUES.md)
into [`docs/fragment-issues-archive/`](../docs/fragment-issues-archive/README.md), and leaves each one a single
line in the register: **the same number**, a one-line title, the opening of its state, and a link to its full
text. Exits **0** when the plan is clean or was written, **1** when a safety check refused and nothing was
written, **2** when the register could not be read at all.

It exists because on 2026-09-22 the register was **1,123,145 bytes** — more than a session can hold at
once — and those two sections were 83% of it. A session sent to the queue could not read the queue.
**Nobody did anything wrong**: every session closed its rows in place, which is what the register asks. What
was missing was the step that moves a finished row out of the way, and a step that has to be remembered is
the one that falls behind. [`docs/handover-archive/`](../docs/handover-archive/README.md) is the same answer for
session notes.

**When in doubt, a row stays.** A row moves only when
[`open-defects.py`](#open-defectspy---how-many-of-herons-own-defects-are-still-open) would not count it open,
its state **begins** with a closing word (FIXED, CLOSED, WITHDRAWN, NOT A DEFECT and a few more), the opening of
that state does not qualify the claim (PARTLY, MOSTLY, EXCEPT and a few more), and **nothing anywhere in the
state says something is still owed** — STILL OPEN, NOT FIXED, NOT YET, AWAITING, OWNER'S CALL, REOPENED and
the rest of the tool's list. A row here is closed in place, so what is left is usually written at the END of a
long state. The first version of this tool read only the opening, and on 2026-09-22 it would have moved
59 rows carrying those words further down — two of them saying NOT YET RUN IN REVIT.
*"Proved, awaiting signature"* stays. So does anything the rule does not recognise.

**It refuses rather than guesses, and it borrows `open-defects.py`'s own row pattern, cell splitter and section
headings** instead of keeping its own — two tools that each decided those would sooner or later disagree
about a row. Before a byte is written it runs `open-defects.py` **on the rewritten text** and requires the same
rows with the same open answer, proves every re-pointed link reaches the file it reached before, and refuses
if the archive already holds a row the register still carries in full.

**Each archive file holds one fixed band of row numbers**, so *row 107* is in `proving-defects-101-125.md` and
*row 5b-62* in `reading-defects-5b-051-075.md` without an index. The band width is part of every link the tool
writes, which is why it is a constant. [`tests/test_archive_fragment_issues.py`](../tests/test_archive_fragment_issues.py)
proves the tool on a register it builds for itself, never on the real one — which changes daily, and would
fail the suite for the tool being right.

**Run it again whenever rows close.** A moved row is recognised by its link, so a second run moves only what
has closed since the first.

**Since 2026-09-23 it reads and writes the register one file per section** - the layout
[`split-register.py`](#split-registerpy---one-file-per-section-the-registers-page-as-its-index) made, with the rows of
5 and 5b in files of 25. It still plans on the register as one text, read through `register-text.py`, then writes
each changed rows file back and refuses unless the files read back as the new register. `open-defects.py` and
`review-ledger.py` read the register through the same reader.

---

## `archive-handover.py` - one file per sitting, as the archive's own README asks

```bash
python tools/archive-handover.py            # what would move - writes nothing
python tools/archive-handover.py --write    # move it
```

Moves every session note out of [`docs/HANDOVER.md`](../docs/HANDOVER.md) into
[`docs/handover-archive/`](../docs/handover-archive/README.md), **one file per sitting**, and adds a row for each
to that folder's README, newest first. What stays is what HANDOVER.md is for: the owner's quick start, the
summary at the top of *Where this stands*, sections 1 to 10a, and a **Latest sittings** table linking the newest
notes. Exits **0**, **1** when a check refused and nothing was written, **2** when the file could not be read.

**The rule was written down twice and kept by neither.** The archive README says *"Do not write a new section
into ../HANDOVER.md"*, and section 10a records the owner's own reason: *"if we keep everything by note that will
be big."* On 2026-09-22 HANDOVER.md was 485,741 bytes, and that day alone had added 81 entries.

A **sitting** is a dated `### YYYY-MM-DD` entry and the undated entries after it, so a note's parts travel
together; below section 10a it runs to a `---` line. Words are unchanged. Every relative link is re-pointed one
folder deeper and proved; a link to a heading that moved follows it to its new file; a link from another
document into a moving heading **stops the run**, and so does a linked heading two sittings share. Lines inside a
code block are never read as headings. The tool's own table is recognised and rewritten, never archived - the
first version archived it on its second run, which the suite caught. [`tests/test_archive_handover.py`](../tests/test_archive_handover.py).

**Run it whenever notes collect in HANDOVER.md.** A note written to the archive in the first place needs nothing.

---

## `archive-needs-checking.py` - move the done checks out of NEEDS-CHECKING.md

```bash
python tools/archive-needs-checking.py            # what would move - writes nothing
python tools/archive-needs-checking.py --write    # move it
```

Moves every row of [`docs/NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) whose ID is struck through at both ends
into [`docs/needs-checking-archive/`](../docs/needs-checking-archive/), one file per group, and leaves a line of
the same shape - the struck ID, a title with a link, the opening of its result, as many cells as its table has.
A struck row whose words still say something is owed - the list in `archive-fragment-issues.py`, plus
*STILL WORTH* - stays in full.

**It proves the register's three readers see the same file afterwards** -
[`owner-queue.py`](#owner-queuepy--what-is-waiting-on-the-owner-derived-rather-than-typed), [`check-gaps.py`](#check-gapspy--what-is-unfinished-and-what-is-only-waiting)
and [`balance-of-work.py`](balance-of-work.py) are each run on the rewritten
text and must answer exactly as before - and that row B8's dated `PASSED`, which `check-docs.py` reads, survives.
[`tests/test_archive_needs_checking.py`](../tests/test_archive_needs_checking.py).

**Since 2026-09-23 it reads and writes the register one file per group** - the layout
[`split-needs-checking.py`](#split-needs-checkingpy---one-file-per-group-needs-checkingmd-as-the-index) made. It
still plans on the register as one text, read through `needs-checking-register.py`, then writes each changed
group back to its own file and refuses unless the files read back as the new register.

---

## `split-needs-checking.py` - one file per group, NEEDS-CHECKING.md as the index

```bash
python tools/split-needs-checking.py            # what would move - writes nothing
python tools/split-needs-checking.py --write    # move it
```

Gives each group of [`docs/NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) its own file in
[`docs/needs-checking/`](../docs/needs-checking/), and keeps NEEDS-CHECKING.md as the register's index: the page's
rules, and each group's heading in its place with one line naming its file. A `## Group X` section moves whole; so
does a dated section whose rows all belong to one group. A section with rows of two groups, or none, stays in full.
Exits **0**, **1** when a check refused and nothing was written, **2** when the register could not be read.

**The register is still one text, and that is what is proved.** Nothing is written unless the new files read back
as the register byte for byte; the real readers - `owner-queue.py`, `check-gaps.py` with its WAITING list,
`balance-of-work.py` and row B8 in `check-docs.py` - print exactly what they printed from the one file; every
re-pointed link reaches the file it reached before; and no other document links into a heading that would move.

**A new row goes into its group's file.** A new group written into NEEDS-CHECKING.md in full is moved by the next
run; a run with nothing new moves nothing. [`tests/test_split_needs_checking.py`](../tests/test_split_needs_checking.py)
builds its own register, never the real one.

## `needs-checking-register.py` - the register read as one text

```bash
python tools/needs-checking-register.py              # the whole register
python tools/needs-checking-register.py | grep K3    # search all of it at once
```

The reader every tool above goes through. It puts each group's file back under its heading in NEEDS-CHECKING.md,
with its links as they were written in the one file, so a tool reads exactly the text it read before the split.
Standard library only, so a reader never breaks because a writer changed. **A missing group file is not skipped**:
a heading naming a file that is not there, or a file that does not hold that heading, stops it with
`RegisterBroken` rather than returning a shorter register - *"a count that quietly omits a whole group is worse
than no count"*, as the register says of itself.

---

## `split-register.py` - one file per section, the register's page as its index

```bash
python tools/split-register.py fragment-issues            # what would move - writes nothing
python tools/split-register.py fragment-issues --write    # move it
```

The same step as [`split-needs-checking.py`](#split-needs-checkingpy---one-file-per-group-needs-checkingmd-as-the-index),
for a register whose sections are not groups. Every `## ` section of [`docs/FRAGMENT-ISSUES.md`](../docs/FRAGMENT-ISSUES.md)
moves whole to its own file in [`docs/fragment-issues/`](../docs/fragment-issues/section-1.md) - `## 1c.` to
`section-1c.md` - and leaves its heading on the page with one line naming its file. The page's own rules stay.
**Sections 5 and 5b are too big for one file each**, so they keep their own words on the page and move only their
rows, **25 to a file by row number** - row 5b-62 is in `section-5b-rows-051-075.md`, the band
[`archive-fragment-issues.py`](#archive-fragment-issuespy---move-finished-defect-rows-out-of-the-live-register)
already files it under. The page keeps one line where each table was, naming its files in order.
Exits **0**, **1** when a check refused and nothing was written, **2** when the register could not be read.

**The register is still one text, and that is what is proved.** Nothing is written unless the new files read back
as the register byte for byte; the register's readers - `open-defects.py`'s whole report, `review-ledger.py`'s rows
of 5b and `archive-fragment-issues.py`'s plan - answer exactly the same on a copy laid out the old way and on one laid
out the new way; every re-pointed link reaches the file it reached before; and no other document links into a
heading that would leave the page.

**A new row goes at the end of its section's last file**, whatever its number; the next run moves it to the file its
number belongs to. A new section written on the page is moved by the next run, and a run with nothing new moves
nothing. [`tests/test_split_register.py`](../tests/test_split_register.py) builds its own register, never the real one.

## `register-text.py` - a split register read as one text

```bash
python tools/register-text.py docs/FRAGMENT-ISSUES.md                 # the whole register
python tools/register-text.py docs/FRAGMENT-ISSUES.md | grep 5b-62    # search all of it at once
```

The reader every tool of a register split by `split-register.py` goes through: `open-defects.py`,
`review-ledger.py` and `archive-fragment-issues.py` for FRAGMENT-ISSUES. It puts each section's file back under
its heading, and each band of rows back where the table was, with the links as they were written in the one file.
Standard library only. **A missing file is not skipped** - a line naming a file that is not there, a section's
file that does not open with its heading, or a rows file with no table stops it with `RegisterBroken` rather than
returning a shorter register. A page that names no file is returned as it is, which is how a suite's own register
is read.

---

## `split-decisions.py` - one file per decision, DECISIONS.md as the index

```bash
python tools/split-decisions.py            # what would change - writes nothing
python tools/split-decisions.py --write    # change it
```

Gives every decision its own file, `docs/decisions/D-NN.md`, and keeps [`docs/DECISIONS.md`](../docs/DECISIONS.md)
as the index: each decision's heading, its metadata lines (Status, Date and the rest), and a link to its full
record. That is the standard shape of a decision log - an architecture decision record, one record per decision
- and the log already kept every other rule of it: numbered, append-only, a status on each.

**Exactly that stays, because tools read exactly that.** Every link to a decision - `DECISIONS.md#d-nn-...` - lands on a
heading, and `check-docs.py` takes the set of defined decisions from them;
[`generate-decision-summary.py`](#generate-decision-summarypy--the-decision-index-rebuilt-from-the-decisions)
reads a decision's Status and Date from the lines under its heading, so its table is rebuilt unchanged.

**To add a decision, write it into DECISIONS.md in full, as always, and run this.** It moves the new one into its
file and leaves every decision already split alone. [`tests/test_split_decisions.py`](../tests/test_split_decisions.py).

## `check-decision-titles.py` - a decision number means one decision for ever

```bash
python tools/check-decision-titles.py                        # against HEAD's history
python tools/check-decision-titles.py --published origin/main # against what main has published
```

**A gate: CI's gates job runs it, with `--published HEAD^1`.** It reads every decision heading in
[`docs/DECISIONS.md`](../docs/DECISIONS.md) and the git history of that file, and fails when a number's heading
no longer carries the title the number was first written with, when a title turns up under a number it was not
first written under, when a number leaves the log, or when a `decisions/D-NN.md` record stops opening with its
heading. `check-docs.py` checks that no number is defined twice at once; nothing checked that a number KEPT its
decision, and five did not - [rows 5b-157 and 5b-171](../docs/FRAGMENT-ISSUES.md).

**It reads history because the tree cannot show what went wrong.** The first D-45 to D-49 were not edited away:
a merge on 2026-09-02 kept one branch's log whole and dropped the other's five. A list of titles kept in the tree
passes through a merge like that exactly as the log does. Only the history still holds both sides - which is why
it walks `--full-history`: git's default walk drops the side a merge threw away, and with it the evidence.

**The one excuse lives in the decision.** A changed title passes only when the decision's own `**Numbering:**`
line quotes the title it was first written with; a moved title only when that line names the number it had.
D-45 to D-49, D-97, D-98 and D-101 to D-103 carry such lines. Titles that changed before the check existed are
named one by one in its `KNOWN` table, and an entry that stops being needed fails the run, so that list only
shrinks.

**It exits 2 in a shallow clone** - CI's default checkout, and a cloud session's - rather than pass on history
it cannot see. `git fetch --unshallow` first; the gates job fetches the whole history for it.
[`tests/test_check_decision_titles.py`](../tests/test_check_decision_titles.py) builds its own repositories, so
it runs anywhere.

---

## `review-ledger.py` - which files have been read, word by word

```bash
python tools/review-ledger.py                       # the balance
python tools/review-ledger.py --next 20             # what to read next
python tools/review-ledger.py --mark <path> clean
python tools/review-ledger.py --mark <path> issue --note 5b-3
python tools/review-ledger.py --mark <path> clean --part --note "what is left"
python tools/review-ledger.py --stale               # marks that no longer apply
python tools/review-ledger.py --history <path>      # what one file has been through
```

Reads [`docs/REVIEW-LEDGER.tsv`](../docs/REVIEW-LEDGER.tsv). **The reporting commands always exit 0** -
they report, they do not gate. **`--mark` is the exception and exits 2 when it refuses**, because a
refusal that exits 0 is indistinguishable from a mark that landed, and that cost two silent misses on
2026-09-21 ([row 5b-63](../docs/FRAGMENT-ISSUES.md)). This line read *"Always exits 0"* until that day,
and it was true when it was written - which is why it was looked for rather than waited for.

**The `check-*.py` gates check rules. None of them records that a file was READ.** So a second
session had no way to know the first had already read a file, and the only honest thing it could do
was read it again - which is the whole sweep done twice, and the second pass is indistinguishable from
the first in every report Heron prints. **No total is written here**; the tool derives one every time
it runs, because a count typed into a README is the cache-with-no-invalidation this repository keeps
paying for.

**The mark carries the file's content hash, and that is the whole design.** `open-defects.py` already
names the failure this ledger could easily have become - *a prose total is a cache with no
invalidation* - and a bare "checked" tick is exactly that: marked today, the file edited tomorrow,
and the tick still reads *checked* while describing content nobody has seen. It is **worse** than no
tick, because a session trusts it and skips the file. So a row records the git blob hash of the file
**as it was read**. When the file changes by one byte the hash stops matching and the row goes
**STALE by itself** - nobody has to remember to withdraw it, and no session has to trust that
somebody did. That was measured rather than reasoned: `tools/open-defects.py` was marked clean, then
genuinely edited in the same sitting, and the ledger reported it stale with the two hashes side by
side without being asked.

**Scope is every tracked file except `brain/**.yaml`**, which keeps its own gates and the retrieval
evals - the owner's decision, 2026-09-20.

**The sweep records; it does not repair.** A file found wrong is marked `issue` and written up in
section **5b** of [`docs/FRAGMENT-ISSUES.md`](../docs/FRAGMENT-ISSUES.md); the file itself is left
alone. `--mark ... issue` **refuses without a `--note`** carrying the 5b row, because a file marked
*issue* with nowhere to read the issue is a finding nobody can find.

**The ledger is append-only.** Rows are never rewritten or removed; the newest row for a path counts
and the ones behind it are that file's history, which `--history` prints.

**How much was read is a SEPARATE column from what was found**, because they are separate facts and
nineteen files are both at once. `--part` records that only some of a file was read; the file keeps
its verdict, stays in the `--next` queue marked `PART`, and is counted apart from the files read word
by word. **`--part` refuses without a `--note`** saying which part, for the same reason `issue` refuses
without a row: a part-read mark nobody can resume is *worse* than no mark, because it takes the file
out of the never-opened queue and puts nothing in its place.

> **This column was added on 2026-09-21, and the reason is the number it corrected.** Thirty files
> carried `PARTIAL READ and said so` in their **note** - honest prose, in a column nothing counted -
> so the headline read *142 files read* when **113** had been read word by word, and `AGENTS.md`
> sends people to that number for exactly this question. A count derived from prose is guessed, not
> derived ([row 5b-90](../docs/FRAGMENT-ISSUES.md)). **Nothing was rewritten**: each of the 29 still
> in scope got a NEW row, naming whose read it was, and `--history` keeps the chain. A row written
> before the column has six cells and means `full`, which is what it claimed at the time.

## `new-agent.py` - scaffold the next agent from its row in the register

```bash
python tools/new-agent.py HERON-OPS-QUE-002
python tools/new-agent.py HERON-OPS-QUE-002 --part mcp --step 16
```

Writes three files and nothing else: the contract in `brain/agents/<ID>.yaml`, the module with its
five-field metadata header already correct, and the test - **which fails until it is written.**

146 agents can be built on a machine with no Revit
([`agent-build-order-2026-09-13`](../docs/work-notes/plans/agent-build-order-2026-09-13.md)). By hand
that is 146 chances to mistype a layer or invent a field.

## `resign-machine-proofs.py` - replace a MACHINE's name in a proof with a person's

```bash
python tools/resign-machine-proofs.py --list
python tools/resign-machine-proofs.py --by "Ajmal PS"
```

Sixteen fragments carried a proof signed `"Claude Opus 5, at Ajmal PS's PC"` with no date. **[D-30](../docs/DECISIONS.md)
says the machine gathers evidence and a PERSON signs**, so that is not a signature - it is the thing
the rule exists to forbid, written into the field meant to prevent it.

`heron_validate.py accept` cannot fix them: it reads a draft from `brain/proof-drafts/` and none of the
sixteen has one. **It changes the one field that is wrong and leaves every other line exactly as
recorded, and it DOES NOT judge the evidence** - that was checked separately.

## `hook-report.py` - how often each hook decided something, and what it said

```bash
python tools/hook-report.py                 # the diary this machine keeps
python tools/hook-report.py --days 7        # only the last week
python tools/hook-report.py --log FILE      # another diary file
```

Every hook in [`.claude/settings.json`](../.claude/settings.json) - the
[guard](../.claude/skills/heron-guard/SKILL.md), the session line and the "has main moved?" advice of
[`heron-session`](../.claude/skills/heron-session/SKILL.md) - appends one line per decision to a diary
outside this repository, found through Heron's own path helpers and never a typed path. This counts it:
per hook, how many decisions in how many sessions, each kind of decision, **how often it spoke at all**,
and the things it said most.

**A hook that only nags is switched off, and a hook that never fires is not there** - and from inside one
session nobody can tell which. On 2026-09-22 the guard turned out to have been running only in sessions
that had loaded its skill; a diary with a guard line in every session is what "runs everywhere" looks
like when it is true.

**A report, not a gate: it exits 0 whatever it finds.** A torn line - the file is appended to by
several processes - is counted and shown, never fatal, and the one older copy kept after a rotation is
read too. No diary on this machine is an answer, with the reason, not a crash.
[`tests/test_hook_report.py`](../tests/test_hook_report.py) holds it to a diary written with known
contents.
