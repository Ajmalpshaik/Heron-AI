<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Improvement Gate — execution record

> **Type:** Operational work note. **Not specification.**
>
> The ledger for building the Heron Improvement Gate: the Phase 0 audit, one implementation note per
> phase, and the evidence each phase actually produced. It retires the way
> [`work-notes/README.md`](../README.md) says everything here retires — durable knowledge moved into the
> permanent document that owns it, then deleted.

**The three plan notes that drove this work were deleted on 2026-09-12**, once every phase was built
and their durable half had moved: `heron-improvement-gate-master-plan.md`,
`external-repo-adoption-plan.md` and `project-perfection-and-continuous-upgrade-plan.md`. The last of
those was a condensed re-issue of [34 — Project Perfection](../../34-project-perfection-and-continuous-upgrade-plan.md),
which stays in `docs/` for its reasoning and now carries a banner saying it has been executed — a plan
whose work is done and which still reads as an instruction is the thing
[AGENTS.md](../../../AGENTS.md) warns about.

---

## 0. Baseline, measured 2026-09-11 on `claude/heron-improvement-gate-1vzts2`

Taken before anything was changed. Every number here came from a command, and the command is named so
it can be run again.

| | Result |
|---|---|
| `python tools/check-docs.py` | **exit 0** |
| `python tools/check-metadata.py` | **exit 0** — 132 source files, 250 registry agents, 77 implemented |
| `python tools/check-structure.py` | **exit 0** — 3 project references checked, 6 layering rules |
| `git diff --check` | **exit 0** |
| `python tools/check-gaps.py` | **exit 0** — nothing UNFINISHED; 163 fragments and the whole Revit register are *waiting*, which is not failing |
| every suite in `tests/` | see §0.2 |

**`check-gaps.py` exits 0 today and the [`heron-ship`](../../../.claude/skills/heron-ship/SKILL.md)
skill says it exits 1 "by design".** Both sentences were true when written. The skill's was measured on
2026-09-09 against 218 unproven fragments; proving has since moved every genuinely *unfinished* item off
that list, leaving only *waiting* ones, and the exit code follows the unfinished list alone. The skill
is corrected in this change rather than left to be believed.

### 0.1 The tool count the ship skill states

`heron-ship` §2 says **41 suites**. `ls tests/test_*.py | wc -l` says **52**. The skill's number was
measured on 2026-09-09 and the RAG stages added suites afterwards. Corrected in this change — and
replaced with the command, because that is this repository's own rule about typed counts.

### 0.2 Suites, measured on this container

Plain Linux container, no MCP SDK, no .NET SDK, no Revit. Recorded as the **set** of failures rather
than a total, for the reason `.github/workflows/gates.yml` already gives: a lump total is how a real
regression hides.

```bash
for t in tests/test_*.py; do timeout 300 python3 "$t" >/dev/null 2>&1; echo "$(basename $t) $?"; done
```

---

## 0.3 What Heron already has — the audit the master plan's Phase 0 asks for

The rule applied throughout: **do not build what a tool here already owns.**

| The plan wants | Heron today | Verdict |
|---|---|---|
| Layer ownership check | `check-structure.py` — parts, C# `ProjectReference` layering, the `Autodesk.Revit` adapter boundary, the path-manager rule | **Partial.** Its own source says the Python side "has never been checked here at all" |
| Metadata / artefact identity | `check-metadata.py` + [29](../../29-metadata-standard.md) — five fields, and a reverse audit of registry against code | **Have.** Do not add a second metadata system |
| Evaluation cases beside capabilities | `check-routing.py` (does a fragment still win its own utterances) · `check-intrusion.py` (who turns up in shortlists they have no claim on) | **Have**, for fragments. Deterministic and model-free |
| Golden regression library | `tests/golden/cases.py` | **Have** |
| Unfinished vs waiting | `check-gaps.py` — four buckets, exit code follows the unfinished one | **Have.** This is the offline-vs-real-Revit matrix at item level |
| Revit-version compile matrix | `check-compile.py`, `check-api-surface.py`, `check-fragments-compile.py`, `Directory.Build.props` | **Have.** Needs `dotnet` |
| Licence / provenance | `check-licence.py` | **Have**, for shipped knowledge |
| Dead code | `check-reachable.py` | **Have** |
| Real-Revit proof discipline | `batch-prove.py`, `generate-jobs.py`, [`fragment-proving`](../../../.claude/skills/fragment-proving/SKILL.md), [D-30](../../DECISIONS.md) | **Have**, and stronger than anything studied outside |
| Baseline comparison in CI | `.github/workflows/gates.yml` compares the **set** of failing suites against a known-failure list | **Have**, for CI. Not reusable by a person mid-task |
| Component health | `heron_health.assess()` — HEALTHY/WARNING/DEGRADED/FAILED, every fact passed in as an argument | **Have.** The pattern the runtime snapshot should copy |
| Risk declared per operation | `mcp/server/heron_tools.py` + `HeronOperationRegistry.cs`, and a test asserts the two agree | **Have** |
| **Intent vs. diff** | nothing | **Missing** |
| **Reusable before/after evidence record** | nothing outside CI | **Missing** |
| **Keep-or-revert ruling on a measured pair** | nothing | **Missing** |
| **Capability availability with a reason** | `heron_capability.resolve()` returns `None` for five different reasons | **Missing, and it is a defect** — see §6 |
| **Packaging / delivery checks** | nothing. Not one check reads `Heron.addin` | **Missing** |
| **One final status for a change** | nothing | **Missing** |

### Rejected during the audit, with the reason

- **A new registry for change records.** [18 §2](../../18-agent-operating-system.md) and the Capability
  Registry already own identity and lifecycle. A change is not a Heron artefact; it is a git commit.
- **A new metadata system for evidence.** [29 §4](../../29-metadata-standard.md)'s test — *would a
  script fail the build over this field?* — kills most of what the plan's §4 input contract asks for.
- **A multi-agent framework to run the gate.** The master plan's own §9 forbids it, and every mechanism
  here is deterministic enough not to need one.
- **A separate offline-vs-Revit matrix tool.** `check-gaps.py` already sorts items into unfinished,
  needs-real-Revit, needs-a-dependency and needs-the-owner. A second tool answering the same question
  is the duplicate-authority failure this whole initiative exists to prevent.

---

## 0.4 External research — what was opened, and what was taken

Re-opened at implementation time, source inspected rather than READMEs, as the now-retired
external-repository adoption note required, and as
[34's mandatory research rule](../../34-project-perfection-and-continuous-upgrade-plan.md) still does.

| Repository | Licence at the paths read | Files actually read |
|---|---|---|
| `nexu-io/open-design` | Apache-2.0 (repository `LICENSE`) | the CI scope classifier and its rule contract; the artefact lint pass; an injection-style capability registry; the installed-acceptance check in the release scripts |
| `Shubhamsaboo/awesome-llm-apps` | Apache-2.0 (repository `LICENSE`); the skill read also declares `license: Apache-2.0` in its own front matter | the scope-creep skill brief, its diff classifier, its signal reference; the deterministic trigger-eval runner |
| `OpenHands/OpenHands` | MIT | the mock-LLM end-to-end configuration and its scripted mock server |
| `OpenHands/agent-canvas` | — | **not opened.** Archived, and [35 §5.1](../../35-independent-study-notes-open-design-awesome-llm-apps-openhands.md) already records that its content moved |

**No file, name, structure or wording from any of them appears in Heron.** What was taken is listed in
[34 — Patterns adapted](../../34-patterns-adapted.md), which is where this repository already keeps
that record, and each row says what Heron does differently and why.

---

## 1. Phase notes, in the order they were built

Each phase was written down before it was implemented: current state, the exact intended change,
acceptance, risk, files, tests, and whether real Revit proof would still be owed. Condensed here to what
a later reader needs.

### Phase 1 — Change intent and scope · `tools/check-change.py`

| | |
|---|---|
| **Problem** | Every gate asks about the repository; none asks about **this change**. A diff that compiles, passes and rewrites three unrelated subsystems is invisible to all of them |
| **Change** | One tool: classify each changed file against a declared `intent` / `area` / `risk`, raise signals, name the gates the change owes, return one status |
| **Acceptance** | An unrelated file returns `SPLIT`; an unusable intent returns `BLOCKED` and judges nothing; a change with no evidence is never `PASS` |
| **Risk** | Low — a new tool, called by nothing else, changing no behaviour |
| **Tests** | `tests/test_change_gate.py`, mostly negative cases |
| **Real Revit** | Not required, and the tool is the thing that says when it *is* |

**The design decision worth keeping** is in [D-68](../../DECISIONS.md): the comparison is **structural,
not lexical**. It imports the layering table rather than counting shared words, so `supporting` becomes
a real class — a change declaring `mcp` that touches `brain/` is supporting work; the same change
touching `revit/` is not.

**A trap found by running it on its own first diff:** it reported zero files while two new ones sat
beside it. `git diff` does not show untracked files, and *"something arrived that nobody asked for"* is
the entire class this tool exists to catch. Untracked files are now listed read-only; `git add -N` was
refused because a checker that edits the index is one somebody has to undo.

### Phase 2 and Phase 4 — Evidence, and the keep/revert loop · `tools/change-evidence.py`

**One tool, not two, and that is the finding.** The plan asked for a baseline recorder (Phase 2) and an
evaluation-gated improvement loop (Phase 4). The loop *is* `capture → change one thing → capture again →
rule`, so a separate loop tool would have been a second home for one mechanism.

The hard restriction the plan puts on Phase 4 — no automatic mutation of architecture, Revit writes,
trust rules, installers or public contracts — is met **by construction rather than by policy**: the tool
cannot change anything at all. It measures, compares and rules.

`NO CHANGE MEASURED` is deliberate. It is the honest verdict for a change whose effect nothing measurable
moved, and it is the one most such tools quietly round up into a pass.

### Phase 3 — Offline harness · three suites, and the line written down

Added `tests/test_change_gate.py`, `tests/test_package_gate.py`, `tests/test_runtime_snapshot.py` — all
three offline, all three mostly negative cases.

**No new "offline vs Revit" tool was built.** `check-gaps.py` already sorts every outstanding item into
unfinished · needs-a-real-Revit · needs-a-dependency · needs-the-owner. What was missing was the *rule*
that decides which side a behaviour falls on, and that is prose, so it went into the permanent document
that owns testing: [13 §3a](../../13-testing-and-quality.md).

### Phase 5 — Architecture ownership · `tools/check-structure.py` extended

The gap was already written down **in the checker's own source**: the layering table *"only ever ran
against C# `ProjectReference`s, so the Python side has never been checked here at all"* — an admission
left standing for a fortnight while two thirds of the repository went unchecked.

The Python pass raised **34 imports** on its first run, all of them gates in `tools/` reading `brain/`,
which is what [D-48](../../DECISIONS.md) asks them to do. Resolved as [D-69](../../DECISIONS.md), and
resolved the slow way: the rule was **not** loosened to make the new check green. Verified by breaking
it on purpose — a `brain/` module importing an `mcp/` one fails the build and names both.

### Phase 6 — Runtime capability snapshot · `mcp/server/heron_runtime.py`

`heron_capability.resolve()` returns `None` for five unrelated reasons, and a planner given `None` can
only say *"I cannot"*. The five sentences a modeller needs are completely different: a gap, a version
answer, a button to press, a setting to change, or *"nothing has told me which Revit this is"*.

Six verdicts now, each meaning one thing. Built as a **pure function** — every fact arrives as an
argument — copying `heron_health.assess()`, which is the pattern that makes it testable on a machine with
no Revit, no bridge and no config.

**Nothing is wired to it yet, deliberately, and that is recorded rather than hidden.** Offering it as an
MCP tool changes the risk table in `heron_tools.py`, which is a decision with a human in it.
`check-reachable.py` reports it, with the reason.

### Phase 7 — Packaging · `tools/check-package.py`

**Nothing in this repository read `Heron.addin`.** Not one of twenty-five tools, and it is the first
file Revit opens.

Four faults it catches that every other gate stays green through, listed in
[34 §2.17](../../34-patterns-adapted.md). The `<ManifestSettings>` one is the sharpest: the element
arrived at Revit 2026 and **crashes 2025 and older**, and Heron deploys one manifest to all eight
releases.

**Its first run raised two false positives, both instructive**, and both were fixed rather than
tolerated — a checker that cries wolf teaches people to skim checker output:

| Raised | Why it was wrong |
|---|---|
| `setup.ps1` does not install under `%APPDATA%` | It **delegates** to `deploy-addin.ps1`, which is the path manager rule applied to install scripts. Raising it would have sent somebody to give it a second copy of the path — the defect, not the fix |
| `Directory.Build.props` has no row for Revit 2022 or 2023 | One row covers `>= 2021 AND <= 2024`. The check was searching for the literal year. The conditions are **evaluated** now, and one that cannot be parsed is reported rather than assumed true |

### Phase 8 — The one status

Folded into `check-change.py` rather than given a tool of its own. The plan's five statuses are all
reachable, plus `BLOCKED`, and the exit codes keep the four states this repository refuses to merge
apart: `0` PASS · `1` needs work · `2` could not judge · `3` NEEDS REAL REVIT PROOF, which is the code
[`tests/README.md`](../../../tests/README.md) already uses for *could not run here*.

---

## 2. The pilot, run twice, with measured evidence

The master plan's §12 asks for a contained first task with deterministic offline tests and a small diff.
Two were found **while building the gate**, both pre-existing, both red on every machine, and both named
in the `heron-ship` skill and `gates.yml` as known failures that were nobody's fault.

They turned out to be the same defect twice: **a fixture describing a repository that had moved on.**

| | `tests/test_reachable.py` | `tests/test_graph.py` |
|---|---|---|
| **Symptom** | 1 check red | 3 checks red |
| **Root cause** | a recorded excuse for `provide_role` being uncalled, which `tools/generate-jobs.py` has legitimately called since 2026-09-10 — found by `git log -S` | the fixture renamed a provide by matching two exact adjacent lines, and D-51/D-52's work put `role:` between them |
| **Blast radius** | one entry in `RECORDED`; nothing in production | the test's own helper; nothing in production |
| **Evidence of the cause** | the tool's own stale-record section named it | **50 of 50** providers were being missed — `break_providers()` was a no-op and the library was correct all along |
| **Fix** | remove the stale entry, say why it went | match the entry by its `name:` line, bounded to the `provides:` block |
| **Ruling** | `KEEP` — suite FAIL → PASS, nothing else moved | `KEEP` — suite FAIL → PASS, nothing else moved |

**Neither test was edited until it passed.** Both claims are unchanged and both still fail if what they
assert stops being true — `test_graph` was re-verified by confirming it finds 50 providers, that the
composition disappears when they are broken, and that every fragment file is byte-identical afterwards.
That is the same act `tests/README.md` records for `test_embed` and `test_retrieve`, and it is recorded
the same way here.

`.github/workflows/gates.yml` and the `heron-ship` skill were updated in the same change, because that
workflow **errors when a known failure starts passing** and asks for exactly this rather than a quietly
better number.

---

## 3. Defects found and recorded rather than fixed

Per [AGENTS.md](../../../AGENTS.md): a defect found while doing something else is recorded.

| | |
|---|---|
| `ALL_VERSIONS` is declared **twice** — `check-compile.py` and `check-api-surface.py` each own a copy of the supported release list | [PROPOSALS F2](../../PROPOSALS.md). `check-package.py` imports the first rather than making it three |
| `DECISIONS.md`'s status summary table **stops at D-50** while the log runs to D-69 | [PROPOSALS F3](../../PROPOSALS.md). Seventeen rows predate this work |
| `docs/34` is used **twice** — `34-patterns-adapted.md` and `34-project-perfection-and-continuous-upgrade-plan.md` | A numbering collision, tangled with a placement question. Both moved to [PROPOSALS F4](../../PROPOSALS.md) so they outlive this note |

---

## 4. What this work does NOT prove

- **Nothing here has been near Revit.** [D-30](../../DECISIONS.md) is untouched, no fragment status
  moved, and `check-package.py` prints what it cannot answer on every run.
- **The gate has never been run by anyone but its author**, which is [Golden Rule 7](../../14-golden-rules.md)
  unsatisfied: one agent creates, another validates. The independent review in §5 was performed in the
  same session as the build and is worth exactly what that is worth.
- **`heron_runtime.snapshot` is wired to nothing.** It is a provider with no consumer until somebody
  decides whether it should be an MCP tool.

---

## 5. The final review, and what it is worth

The fourteen questions, answered against the diff rather than against the plan. **Read the first line of
§4 before any of it**: this review was written in the same session as the code, which is
[Golden Rule 7](../../14-golden-rules.md) unsatisfied, and no amount of care inside one session fixes
that.

| | |
|---|---|
| **1. Is each accepted change proven better than its baseline?** | For the two pilots, yes, and measured: `test_reachable.py` and `test_graph.py` went FAIL → PASS with nothing else moving, recorded as `KEEP`. For the gate itself, no — a new tool has no baseline to be better than. What it has is negative cases proving it says no |
| **2. Did any change exceed its stated intent?** | The gate's own report is in §6. Everything outside `tools/` and `mcp/` falls in the classes that are counted rather than questioned, and the two pilot fixes were both found *by* the work rather than sought out |
| **3. Is business logic duplicated anywhere new?** | No, and it was actively avoided three times: the layering table is **imported** from `check-structure.py` rather than copied; the release list is **imported** from `check-compile.py` rather than made a third copy; and Phase 2 and Phase 4 became **one** tool because the improvement loop *is* the before/after comparison |
| **4. Are Heron's architecture boundaries still correct?** | Stricter than before. The Python half of the layering table is enforced for the first time, and `tests/test_layering.py` watches it refuse three different things. `heron_runtime.py` sits in `mcp/` because it combines brain knowledge with bridge facts, which is the only part allowed to do both |
| **5. Is Revit 2020–2027 compatibility preserved?** | **No C# changed at all.** `check-package.py` now asserts every declared release resolves to a runtime, and would refuse a `<ManifestSettings>` element that breaks 2020–2025. Compatibility is better observed, not altered |
| **6. Are trust and transaction boundaries preserved?** | Untouched. Nothing in this change reads or writes a permission, a risk level, a transaction or the write gate. `heron_runtime.py` **reports** the trust ceiling and cannot alter it, and `check-change.py` raises a `trust` signal that owes a human review whatever risk was declared |
| **7. Are negative cases covered?** | They are most of the new tests: a brain module importing an mcp one, a docstring that merely mentions an import, a file that does not parse, four fatal manifest faults, an unusable intent, an unrelated file, a fix that does not pay for a regression, and a suite that could not run being counted as neither |
| **8. Are dependencies justified?** | **Nothing was added.** Every new file is Python standard library — `ast`, `argparse`, `json`, `glob`, `subprocess`, `xml.etree`. No PyYAML, no third-party anything |
| **9. Are licences and provenance handled correctly?** | Each external path read is named with its licence in [34 §2.15–2.19](../../34-patterns-adapted.md), checked at the path rather than assumed from the repository. **No file, name, folder shape or wording was copied.** One borrowed term reached a gate name and was renamed to Heron's own words before the change was finished |
| **10. Are packaging and update paths still valid?** | Better checked than before and unchanged in behaviour. `check-package.py` passes on the current tree and **prints what it cannot answer on every run** |
| **11. Is anything being called proven without real evidence?** | The word *proven* is used nowhere about this work. The gate's own vocabulary refuses it: a clean scope report says a change stayed where it said it would, and the tool prints that sentence every run |
| **12. Is any item still dependent on REAL REVIT PROOF?** | Yes, and unchanged: everything `check-gaps.py` already lists. This work moved **no** fragment status and took **no** proof. The four delivery questions `check-package.py` cannot answer need Windows and a Revit |
| **13. Are the work notes updated honestly?** | This record carries the three defects found and not fixed, the two false positives the packaging gate raised on its first run, the two traps the change gate hit on its own diff, and §4's admission |
| **14. Are durable findings moved to their permanent owner docs?** | [D-68](../../DECISIONS.md) · [D-69](../../DECISIONS.md) · [34 §2.15–2.19](../../34-patterns-adapted.md) · [13 §3a](../../13-testing-and-quality.md) · [04 §6b](../../04-heron-mcp.md) · [PROPOSALS Part F](../../PROPOSALS.md). The three plan notes were deleted afterwards, not before |

