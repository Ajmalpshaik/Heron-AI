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

Run it after any edit that moves or renames a document. It is how the Golden Rule renumbering
(ten rules to fifteen, [D-12](../docs/DECISIONS.md)) was verified across 31 files.

**An id defined twice is worse than one never defined, and this script could not see one.** Every
registry was read with `set(re.findall(...))`, and a set is precisely the thing that makes a duplicate
invisible: two `## D-56` headings collapse to one entry, *"REFERENCED BUT NOT DEFINED"* stays empty, and
the file is reported clean. **D-67 was first written as D-56, which already existed, and this checker
passed on it** — the duplicate was found by eye, which is the reading it exists to make unnecessary.

**It is the only thing here that fails the run, and a broken link does not.** That asymmetry is
deliberate. A dead link announces itself the moment somebody clicks it. A duplicate id is silent, and it
makes every reference to that number ambiguous — `[D-56](../docs/DECISIONS.md)` now points at two
different decisions and nothing can say which was meant, not the anchor, not the reader, not this
script. Both entries look correct in isolation.

Verified by breaking it on purpose, once per registry: a second `## D-56`, a second `### 3.` rule and a
second `### Q-51` each name themselves and exit 1, and the unmodified repository exits 0.

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

The register: every department with **TOTAL · BUILT · HOST · LEFT · T1 LEFT**, the tier split, and
where Phase 0/1 actually stands. `recount-agent-registry.py` keeps the registry honest about *itself*;
`check-metadata.py` audits it file by file. Neither answers *what proportion of each department is
built*, which is the question asked before deciding what to build next, and which was being answered by
adding up columns by hand.

**Three states, not two — and that is the point.** An agent is BUILT, LEFT, or **HOST**: delegated to
Claude Code on purpose by [D-01](../docs/DECISIONS.md). Collapsing HOST into LEFT produces a to-do list
with four items that will never be done, and on 2026-09-07 it did exactly that — a build-state summary
read *"Phase 0/1 is four agents short"* and recommended building the **Orchestrator**, which
[docs/02 §7](../docs/02-architecture-overview.md) settles as the host's. Phase 0/1's agent list is
complete: **45 built, 4 host-provided, 0 outstanding.**

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

The fourth is the one nothing else asks, and it is the one that would have caught the error above.

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
shifts the run sideways. See [D-47](../docs/DECISIONS.md).

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
shares a drive with the repository. It uses a local `repo_relative()` that falls back to the absolute
path when there is no relative form — the same rule
[`brain/heron_fragment.py`](../brain/heron_fragment.py) owns — because
`os.path.relpath` raises `ValueError` across drives on Windows, and the test builds its fixture in
`tempfile.mkdtemp()`. Repository on `D:`, `TEMP` on `C:`, and the gate dies. Repeated rather than
imported on purpose: importing it costs PyYAML, and this is the tool you run on a bare machine to read
the licence of something *before* trusting it enough to install anything for it.

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
what nobody has explained. It found **12**, of which **5** were already recorded and one —
[`Q-47`](../docs/OPEN-QUESTIONS.md), `heron_capability.want()` — was not.

### Why it parses instead of searching, learned three times in one night

A CLI subcommand is reached by name, not by a `foo()` in the source, so a naive check calls every one of
them dead. The precise test is a **dict literal whose value is the function** (`{"accept": accept}`) or
a **`getattr` with a literal name** — structures, which prose cannot produce.

That precision was arrived at by getting it wrong three times, and all three are the same failure:

| | |
|---|---|
| `check-revit-gate.py` | compared a **space-stripped haystack** against a needle that still had spaces, and reported a confident **0** where the answer is 114 |
| `measure-routes.py` | grepped for the text `remember(` and matched **its own docstring**, which describes the problem — then printed the opposite conclusion in the one line it exists to be right about |
| this tool, first version | treated any string literal `"remember"` as dispatch. `measure-routes.py` contains one, in the `ast` comparison that finds callers of `remember`. **The tool written to find the problem made the problem invisible to the next tool** |

Three heuristics, three times fooled by text *about* the thing rather than the thing.

**It went 12 hits → 4, and the cuts were its own false positives.** It reported a **nested closure**
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
