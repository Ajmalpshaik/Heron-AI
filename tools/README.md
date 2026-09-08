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

Run it after any edit that moves or renames a document. It is how the Golden Rule renumbering
(ten rules to fifteen, [D-12](../docs/DECISIONS.md)) was verified across 31 files.

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
