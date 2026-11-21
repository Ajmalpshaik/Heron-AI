# Decision Log

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone asking **what was decided, and why** |
> | **Authority** | The [Constitution](../HERON_CONSTITUTION.md) and [Golden Rules](14-golden-rules.md) win over this file. This file wins over every work note |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | A settled answer is **promoted here from [OPEN-QUESTIONS](OPEN-QUESTIONS.md)**. A reversal gets a **new entry** that supersedes the old — the original stays, so the reasoning is never lost |
> | **Its numbers** | Derive the highest decision: `grep -oE '^#+ *D-[0-9]+' docs/DECISIONS.md | grep -oE '[0-9]+' | sort -n | tail -1` |
> | **Where each decision lives** | Since 2026-09-23 **each decision is its own file**, `decisions/D-NN.md`, and this page is the index: the heading, the metadata lines, and a link to the full record. To add one, write it here in full as below and run `python tools/split-decisions.py --write` |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


> Every architectural decision that has been **made**, with the reasoning behind it.
> Answers from [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are promoted here once settled.
>
> This log is append-only. A decision that is later reversed gets a **new entry** that supersedes
> the old one — the original stays, so the reasoning history is never lost.
> The rule is this log's own: never destroy a working record.

---

## ✅ The read-back happened — 2026-08-29, and nothing moved

**Ajmal's instruction, 2026-08-28:** *"We will do this after we finalize one more time ... when I am at
the PC, we will do it one more time. Now we just recorded, but we will do it one more time."*

**Done on 2026-08-29, and it covered D-23 to D-43 rather than only the five.** Twenty-one decisions were
read back and twenty-one were confirmed. Three were put to him one at a time because they carried real
consequence, and all three came back unchanged:

| | |
|---|---|
| [**D-33**](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) | The boundary this log itself flagged as **never actually stated by him** — Heron never invents a Revit number and does decide its own code. **Confirmed as written**, so the decision stands rather than moves |
| [**D-26**](#d-26--the-model-file-is-never-uploaded) | The model file never leaves; names, counts, sizes and reasoning are fine. **Confirmed** after moving three times on the day it was written |
| [**D-32**](#d-32--v1-must-be-able-to-change-the-model-and-reading-is-what-gets-used-first) | v1 both reads and writes, reading first, writing off by default. **Confirmed** after being reversed once within the hour |

**It happened in conversation rather than at the PC**, which is recorded rather than smoothed over. What
*at the PC* was for — him sitting with them rather than tapping yes — did happen. What still needs a
screen is [`R1b`](NEEDS-CHECKING.md): [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes)
stays **Proposed** until he has seen the trust model working with his own fragments in it.

**And it happened AFTER Phase 2 was built, not before**, which was his own override and is weaker than
intended — a decision reviewed once the code exists gets defended rather than examined. Worth recording
what that cost: **nothing measurable.** The two that had been reversed within hours did not move a fourth
time, and no detail was found missing. That is evidence about these particular decisions, not a reason to
review late next time.

**This heading said *"Five of these get one more pass"* while the table below marked NINE** — D-32 to
D-35 were added later and the sentence was not. Nobody noticed because the sentence and the markers were
never read together. All nine are now confirmed, so the discrepancy is closed by the work rather than by
an edit.

---

## Status summary

> **Generated — do not edit this table by hand.** `python tools/generate-decision-summary.py`
> rebuilds it from the decisions below, and CI fails if it is stale. It was hand-written until
> 2026-09-12, by which time it stopped at **D-50** while the file had reached **D-70** — twenty
> decisions missing from the index of decisions, with nothing able to notice.
>
> **A status cell a person wrote is kept verbatim.** *"read back 2026-09-06"* records a
> conversation, not a fact on disk, so the generator never overwrites one — it fills in rows
> that do not exist yet, and it re-derives its OWN placeholder, which records nothing anybody
> said. A decision that states no status still reads *"status not stated"*; one that has
> since been given a status catches up.

| # | Decision | Status |
|---|---|---|
| [D-00](#d-00--documentation-first-no-implementation-yet) | Documentation first, no implementation yet | ✔ **Fulfilled** · read back 2026-09-06 |
| [D-01](#d-01--execution-host-claude-code-plugin) | Execution host: Claude Code plugin | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-02](#d-02--mcp--add-in-transport-named-pipes) | MCP ↔ add-in transport: named pipes | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-03](#d-03--mcp-tool-granularity-thick-and-specific) | MCP tool granularity: thick and specific | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-04](#d-04--generated-code-execution-hybrid) | Generated code execution: hybrid | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-09](#d-09--revit-thread-marshalling-externalevent) | Revit thread marshalling: ExternalEvent | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-05](#d-05--revit-version-support-2020-to-latest) | Revit version support: 2020 to latest | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-06](#d-06--implementation-languages-c-for-revit-python-for-brain) | Implementation languages: C# for Revit, Python for brain | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-07](#d-07--free-open-source-on-public-github) | Free open source on public GitHub | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-08](#d-08--licence-apache-20) | Licence: Apache 2.0 | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-10](#d-10--repository-stays-private-until-working-code-exists) | Repository stays private until working code exists | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-11](#d-11--adopt-master-specification-part-2-agent-operating-system) | Adopt Master Specification Part 2 (Agent Operating System) | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-12](#d-12--adopt-the-master-handover-baseline-part-3-and-its-fifteen-golden-rules) | Adopt the Master Handover Baseline (Part 3) and its fifteen Golden Rules | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-13](#d-13--adopt-additional-requirements-part-4-kernel-workflow-engine-constitution) | Adopt Additional Requirements (Part 4): Kernel, Workflow Engine, Constitution | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes) | Unify six status vocabularies into two orthogonal axes | ⏳ Proposed · ✔ re-put 2026-09-06, answer unchanged |
| [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour) | Adopt the field notes as authoritative on bridge behaviour | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope) | The session list is built live, and the Revit freeze is out of scope | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-17](#d-17--runtime-state-is-machine-local-not-roaming) | Runtime state is machine-local, not roaming | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-18](#d-18--the-transaction-agent-belongs-to-step-6-not-step-2) | The Transaction Agent belongs to Step 6, not Step 2 | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) | Writing is off by default until the write path has met a real Revit | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-20](#d-20--millimetres-to-feet-is-arithmetic-not-unitutils) | Millimetres to feet is arithmetic, not UnitUtils | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-21](#d-21--failure-analysis-is-a-table-not-a-model-call) | Failure analysis is a table, not a model call | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over) | A second chat is refused, not allowed to take over | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) | The knowledge store is SQLite, one file per scope | ✅ Accepted · ✔ read back 2026-08-29 · *sqlite-vec for meaning* superseded in part by D-194 · 2026-11-21 |
| [D-24](#d-24--embeddings-are-computed-locally-by-default) | Embeddings are computed locally by default | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) | The existing libraries are studied and re-authored, never imported | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-26](#d-26--the-model-file-is-never-uploaded) | The model file is never uploaded | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-27](#d-27--one-voice-and-the-answers-shape-follows-the-questions-shape) | One voice, and the answer's shape follows the question's shape | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) | Generated code is C#, compiled at run time, in process | ✅ Accepted |
| [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer) | A fragment is a composable piece, not a whole answer | ✅ Accepted |
| [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) | A fragment is promoted by one recorded proof, not by a count of runs | ✅ Accepted |
| [D-31](#d-31--product-data-and-derived-are-already-separated-and-the-code-is-the-record) | Product, data and derived are already separated, and the code is the record | ✅ Accepted |
| [D-32](#d-32--v1-must-be-able-to-change-the-model-and-reading-is-what-gets-used-first) | v1 must be able to change the model, and reading is what gets used first | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) | Heron never assumes an input. It asks — and it asks once | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-34](#d-34--herons-own-wording-is-english-understanding-the-user-is-not-herons-job) | Heron's own wording is English; understanding the user is not Heron's job | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-35](#d-35--a-shared-fragment-may-carry-code-and-an-unapproved-one-is-refused-not-warned-about) | A shared fragment may carry code, and an unapproved one is refused, not warned about | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-36](#d-36--no-warranty--the-standard-position-and-it-is-already-in-place-twice) | No warranty — the standard position, and it is already in place twice | ✅ Accepted |
| [D-37](#d-37--the-name-is-heron-ai-and-no-trademark-check-has-been-done) | The name is Heron AI, and no trademark check has been done | ✅ Accepted |
| [D-38](#d-38--github-now-app-store-kept-possible-and-nothing-built-for-it) | GitHub now, App Store kept possible, and nothing built for it | ✅ Accepted |
| [D-39](#d-39--shadow-mode-is-approved-on-an-analysed-disagreement-not-a-count-of-agreements) | Shadow mode is approved on an analysed disagreement, not a count of agreements | ✅ Accepted |
| [D-40](#d-40--the-dependency-graph-is-sqlite-and-an-edge-is-derived-before-it-is-stored) | The dependency graph is SQLite, and an edge is derived before it is stored | ✅ Accepted |
| [D-41](#d-41--single-user-now-company-knowledge-is-a-git-repo-and-the-admin-is-the-reviewer) | Single-user now; company knowledge is a git repo, and the admin is the reviewer | ✅ Accepted |
| [D-42](#d-42--the-public-install-command-is-not-settled-the-proven-one-is-setupps1) | The public install command is not settled; the proven one is `setup.ps1` | ✅ Accepted |
| [D-43](#d-43--the-constitution-is-accepted--all-30-articles-binding) | The Constitution is accepted — all 30 Articles, binding | ✅ Accepted |
| [D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere) | A re-authored fragment starts unproven in Heron, whatever it was elsewhere | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases) | Heron tracks the MCP SDK across major versions, the way it tracks Revit releases | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-46](#d-46--the-emergency-stop-button-is-removed-the-switch-behind-it-stays) | The Emergency Stop button is removed, the switch behind it stays | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-47](#d-47--a-job-can-cross-projects--both-repeating-it-and-copying-content--and-undo-does-not-cross-with-it) | A job can cross projects — both repeating it and copying content — and undo does not cross with it | ✅ Accepted |
| [D-48](#d-48--one-broken-part-costs-one-part-never-the-whole-library) | One broken part costs one part, never the whole library | ✅ Accepted |
| [D-49](#d-49--a-heavy-optional-import-never-happens-on-a-request-thread) | A heavy optional import never happens on a request thread | ✅ Accepted |
| [D-50](#d-50--revit-says-out-loud-what-heron-is-doing-to-it-and-whether-it-is-reading-or-changing) | Revit says out loud what Heron is doing to it, and whether it is reading or changing | ✅ Accepted |
| [D-51](#d-51--a-negative-case-is-judged-by-its-counts-not-by-whether-the-fragment-stayed-silent) | A negative case is judged by its counts, not by whether the fragment stayed silent | ✅ Accepted · 2026-09-07 |
| [D-52](#d-52--a-count-of-what-was-turned-down-is-not-a-count-of-what-was-found) | A count of what was turned down is not a count of what was found | ✅ Accepted · 2026-09-07 |
| [D-53](#d-53--a-fragment-that-cannot-come-back-empty-is-proved-by-tracking-instead) | A fragment that cannot come back empty is proved by TRACKING instead | ✅ Accepted · 2026-09-07 |
| [D-54](#d-54--the-callers-half-arrives-as-text-and-revit-is-what-turns-it-into-a-view) | The caller's half arrives as text, and Revit is what turns it into a view | ✅ Accepted · 2026-09-08 |
| [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back) | A fragment's preview is the run itself, rolled back | ✅ Accepted · 2026-09-08 · a save made on a preview superseded in part by D-171 · 2026-11-16 |
| [D-56](#d-56--the-banner-counts-in-flight-off-the-dispatcher-because-an-end-can-arrive-before-its-own-begin) | The banner counts in flight off the dispatcher, because an End can arrive before its own Begin | ✅ Accepted · 2026-09-08 |
| [D-57](#d-57--the-master-architecture-document-is-a-research-brief-not-a-fifth-part-of-the-specification) | The Master Architecture document is a research brief, not a fifth part of the specification | ✅ Accepted · 2026-09-09 |
| [D-58](#d-58--heron-measures-what-it-can-see-and-the-cost-meter-belongs-to-the-host) | Heron measures what it can see, and the cost meter belongs to the host | ✅ Accepted · 2026-09-09 |
| [D-59](#d-59--reading-spans-loaded-links-only-when-the-modeller-asks-and-the-answer-says-how-many-it-read) | Reading spans loaded links only when the modeller asks, and the answer says how many it read | ✅ Accepted · 2026-09-09 |
| [D-60](#d-60--a-preview-selects-what-it-would-change-and-what-it-would-skip-up-to-500) | A preview selects what it would change and what it would skip, up to 500 | ✅ Accepted · 2026-09-09 |
| [D-61](#d-61--only-a-run-that-came-back-may-be-cached-and-re-indexing-forgets-what-changed-underneath-it) | Only a run that came back may be cached, and re-indexing forgets what changed underneath it | ✅ Accepted · 2026-09-09 |
| [D-62](#d-62--the-brain-writes-its-own-audit-file-and-the-reader-that-already-merges-does-the-merging) | The brain writes its own audit file, and the reader that already merges does the merging | ✅ Accepted · 2026-09-09 |
| [D-63](#d-63--a-want-is-recorded-when-a-capability-is-asked-for-by-name-and-nobody-provides-it) | A want is recorded when a capability is asked for BY NAME and nobody provides it | ✅ Accepted · 2026-09-09 |
| [D-64](#d-64--a-fragment-that-goes-looking-declares-what-it-dropped-and-the-marker-rides-only-on-the-empty-answer) | A fragment that goes looking declares what it dropped, and the marker rides only on the empty answer | ✅ Accepted · 2026-09-09 |
| [D-65](#d-65--heron-keeps-the-degraded-result-rule-and-hands-routing-to-the-host) | Heron keeps the degraded-result rule and hands routing to the host | ✅ Accepted · 2026-09-09 |
| [D-66](#d-66--heron-checks-the-licence-of-what-it-ships-by-reading-the-files-not-the-landing-page) | Heron checks the licence of what it ships by reading the files, not the landing page | ✅ Accepted · 2026-09-09 |
| [D-67](#d-67--a-point-crosses-as-three-millimetre-numbers) | A point crosses as three millimetre numbers | ✅ Accepted · 2026-09-09 |
| [D-68](#d-68--a-significant-change-states-its-intent-before-it-is-made-and-is-judged-against-it-afterwards) | A significant change states its intent before it is made, and is judged against it afterwards | ✅ Accepted · 2026-09-12 |
| [D-69](#d-69--a-script-in-tools-reads-the-code-it-checks-and-that-is-not-a-layering-violation) | A script in `tools/` reads the code it checks, and that is not a layering violation | ✅ Accepted · 2026-09-12 |
| [D-70](#d-70--heron-keeps-a-usage-counter-on-the-machine-and-it-is-numbers-rather-than-a-diary) | Heron keeps a usage counter, on the machine, and it is numbers rather than a diary | ✅ Accepted · 2026-09-12 |
| [D-71](#d-71--every-length-a-caller-types-is-millimetres-and-the-fragment-converts-it) | Every length a caller types is millimetres, and the fragment converts it | ✅ Accepted · 2026-09-13 |
| [D-72](#d-72--four-values-a-caller-could-not-type-are-now-built-from-what-they-type-and-a-face-still-is-not) | Four values a caller could not type are now built from what they type, and a face still is not | • status not stated |
| [D-73](#d-73--a-table-by-name-is-two-separators-and-the-key-is-the-models-word-not-ours) | A table by name is two separators, and the key is the model's word, not ours | ✅ Accepted · 2026-11-15 · read back to the owner |
| [D-74](#d-74--a-write-is-aimed-at-the-model-it-was-told-about-not-guarded-against-the-one-in-front) | A write is AIMED at the model it was told about, not guarded against the one in front | 🕐 Proposed - **owner has not read this back** |
| [D-75](#d-75--a-development-agent-acts-on-heron-itself-not-on-the-artefact-heron-builds) | A Development agent acts on Heron itself, not on the artefact Heron builds | ✅ Accepted · 2026-09-16 |
| [D-76](#d-76--five-standards-rows-are-one-agent-with-a-subject-not-five-files) | Five Standards rows are one agent with a subject, not five files | ✅ Accepted · 2026-09-16 |
| [D-77](#d-77--two-documentation-rows-wait-for-a-tag-and-two-fold-into-the-guard) | Two Documentation rows wait for a tag, and two fold into the guard | ✅ Accepted · 2026-09-16 |
| [D-78](#d-78--the-metadata-checker-owns-metadata-validity-and-the-third-row-folds-into-it) | The metadata checker owns metadata validity, and the third row folds into it | ✅ Accepted · 2026-09-16 |
| [D-79](#d-79--a-generated-name-is-six-parts-lower-case-hyphenated-with-the-version-last) | A generated name is six parts, lower case, hyphenated, with the version last | ✅ Accepted · 2026-09-16 |
| [D-80](#d-80--five-development-rows-are-the-hosts-because-the-host-is-the-model) | Five Development rows are the host's, because the host is the model | ✅ Accepted · 2026-09-17 |
| [D-81](#d-81--carried-text-is-stamped-not-scanned) | Carried text is stamped, not scanned | ✅ Accepted · 2026-09-20 |
| [D-82](#d-82--the-cloud-line-is-drawn-by-content-type-not-by-scope) | The cloud line is drawn by content type, not by scope | ✅ Accepted · 2026-09-20 |
| [D-83](#d-83--an-ingest-may-cross-and-it-carries-the-practice-not-the-count) | An ingest may cross, and it carries the practice, not the count | ✅ Accepted · 2026-09-20 |
| [D-84](#d-84--the-sandbox-is-renamed-not-rebuilt) | The sandbox is renamed, not rebuilt | ✅ Accepted · 2026-09-20 |
| [D-85](#d-85--the-tab-says-heron-the-panel-says-ai-bridge-and-the-product-is-still-heron-ai) | The tab says Heron, the panel says AI Bridge, and the product is still Heron AI | ✅ Accepted · 2026-09-20 |
| [D-86](#d-86--a-write-may-declare-a-question-only-when-answering-it-requires-the-write) | A write may declare a question only when answering it requires the write | ✅ Accepted — **Date:** 2026-09-21 — **Answers:** [Q-57](OPEN-QUESTIONS.md) — **Evidence:** [FRAGMENT-ISSUES rows 158, 137 and 113](FRAGMENT-ISSUES.md) · 2026-09-21 |
| [D-87](#d-87--one-heron-product-is-one-revit-ribbon-tab) | One Heron product is one Revit ribbon tab | ✅ Accepted · 2026-09-21 |
| [D-88](#d-88--one-product-is-one-addin-manifest-plus-one-dll-each-with-its-own-addinid) | One product is one .addin manifest plus one DLL, each with its own AddInId | ✅ Accepted · 2026-09-21 |
| [D-89](#d-89--custom-install-is-at-tab-level-and-the-heron-tab-is-the-one-exception) | Custom Install is at tab level, and the Heron tab is the one exception | ✅ Accepted · 2026-09-21 |
| [D-90](#d-90--install-stays-per-user-and-never-asks-for-administrator-rights) | Install stays per-user and never asks for administrator rights | ✅ Accepted · 2026-09-21 |
| [D-91](#d-91--product-files-come-from-a-signed-versioned-github-release) | Product files come from a signed, versioned GitHub release | ✅ Accepted · 2026-09-21 · *signed* superseded in part by D-173 · 2026-11-16 |
| [D-92](#d-92--a-ribbon-button-is-a-front-door-onto-a-proven-fragment-not-new-logic) | A ribbon button is a front door onto a proven fragment, not new logic | ✅ Accepted · 2026-09-21 |
| [D-93](#d-93--the-product-list-is-a-manifest-read-as-data-never-a-list-written-into-the-installer) | The product list is a manifest read as data, never a list written into the installer | ✅ Accepted · 2026-09-21 |
| [D-94](#d-94--install-replaces-there-is-no-separate-upgrade-path) | Install replaces; there is no separate upgrade path | ✅ Accepted · 2026-09-21 |
| [D-95](#d-95--installing-and-showing-are-two-different-things) | Installing and showing are two different things | ✅ Accepted · 2026-09-21 |
| [D-96](#d-96--a-downloaded-heron-is-installed-by-the-installer-and-only-by-the-installer) | A downloaded Heron is installed by the installer, and only by the installer | ✅ Accepted · 2026-09-21 |
| [D-97](#d-97--the-library-is-built-out-first-and-proved-in-one-pass-later) | The library is built out first, and proved in one pass later | ✅ Accepted · 2026-08-30 |
| [D-98](#d-98--a-context-fragment-is-consumed-by-the-host-not-by-another-fragment) | A context fragment is consumed by the host, not by another fragment | ⏳ Proposed · 2026-08-31 |
| [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so) | A change asked for in a chat is kept at once, with no preview, and Article 9 says so | ✅ Accepted · 2026-09-23 · superseded by D-126 · 2026-10-17 |
| [D-100](#d-100--a-drop-in-the-score-of-the-owners-own-questions-is-reported-never-a-gate) | A drop in the score of the owner's own questions is reported, never a gate | ✅ Accepted · 2026-09-23 |
| [D-101](#d-101--risk-is-part-of-routing-not-only-part-of-permission) | Risk is part of routing, not only part of permission | ⏳ Proposed · 2026-08-31 |
| [D-102](#d-102--several-fragments-may-jointly-feed-one-and-the-orphan-check-asks-per-need) | Several fragments may jointly feed one, and the orphan check asks per need | ✅ Accepted · 2026-08-31 |
| [D-103](#d-103--a-need-may-bind-to-a-provided-name-that-is-not-its-own) | A need may bind to a provided name that is not its own | ✅ Accepted · 2026-08-31 |
| [D-104](#d-104--the-modeller-can-talk-to-the-open-chat-from-inside-revit) | The modeller can talk to the open chat from inside Revit | ↩ Superseded by D-105 the same day · 2026-09-27 |
| [D-105](#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only) | Talk is removed, and the modeller works with Heron in the chat only | ✅ Accepted · 2026-09-27 |
| [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on) | Admin and Publish each get a switch of their own, and only the owner turns them on | ✅ Accepted · 2026-09-28 |
| [D-107](#d-107--the-ifc-and-dwg-exports-are-publish-so-the-publish-switch-guards-every-export) | The IFC and DWG exports are PUBLISH, so the Publish switch guards every export | ✅ Accepted · 2026-09-28 |
| [D-108](#d-108--the-modeller-can-see-and-edit-herons-work-in-a-local-companion-page-and-the-chat-stays-the-only-place-to-talk-to-heron) | The modeller can see and edit Heron's work in a local Companion page, and the chat stays the only place to talk to Heron | ✅ Accepted · 2026-09-29 |
| [D-109](#d-109--a-companion-button-in-revit-opens-the-page-and-a-companion-switch-turns-it-off-without-touching-heron) | A Companion button in Revit opens the page, and a Companion switch turns it off without touching Heron | ✅ Accepted · 2026-09-29 |
| [D-110](#d-110--a-supply-diffuser-neck-in-an-ncrc-30-room-is-held-to-the-owners-25-ms-not-ashraes-22) | A supply diffuser neck in an NC/RC 30 room is held to the owner's 2.5 m/s, not ASHRAE's 2.2 | ✅ Accepted · 2026-10-02 |
| [D-111](#d-111--a-projects-governing-standards-are-asked-once-and-kept-for-that-project) | A project's governing standards are asked once and kept for that project | ✅ Accepted · 2026-10-02 |
| [D-112](#d-112--a-fragment-that-must-work-in-one-of-revits-edit-modes-opens-its-own-transactions-inside-herons-group) | A fragment that must work in one of Revit's edit modes opens its own transactions inside Heron's group | ⏳ Proposed · 2026-10-03 |
| [D-113](#d-113--a-projects-kept-answers-are-filed-under-the-models-own-id-and-answers-kept-under-its-templates-id-are-asked-about) | A project's kept answers are filed under the model's own id, and answers kept under its template's id are asked about | ⏳ Proposed · 2026-10-06 |
| [D-114](#d-114--a-fragment-that-reloads-a-family-is-handed-revits-load-options-by-the-add-in) | A fragment that reloads a family is handed Revit's load options by the add-in | ⏳ Proposed · 2026-10-06 |
| [D-115](#d-115--a-fragment-that-must-run-with-no-transaction-open-declares-d-112s-switch-too-and-the-first-is-open_view) | A fragment that must run with no transaction open declares D-112's switch too, and the first is OPEN_VIEW | ✅ Accepted · 2026-10-13 |
| [D-116](#d-116--a-list-of-family-forms-built-in-one-call-undoes-as-one-and-cuts-stay-a-separate-call) | A list of family forms built in one call undoes as one, and cuts stay a separate call | ✅ Accepted · 2026-10-17 |
| [D-117](#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands) | A change is kept only from a fragment whose proof still stands | ✅ Accepted · 2026-10-17 |
| [D-118](#d-118--a-tool-description-over-its-budget-stops-a-pull-request) | A tool description over its budget stops a pull request | ✅ Accepted · 2026-10-17 |
| [D-119](#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so) | A DRAFT fragment may run once on the model in front, behind a Keep or Discard prompt in Revit, and Article 11 says so | ✅ Accepted · 2026-10-17 · one word of its Article 11 text superseded in part by D-170 · 2026-11-16 |
| [D-120](#d-120--a-run-the-modeller-kept-after-checking-it-is-the-positive-case-of-a-proof-and-the-negative-case-is-still-its-own-rolled-back-run) | A run the modeller kept after checking it is the positive case of a proof, and the negative case is still its own rolled-back run | ✅ Accepted · 2026-10-17 |
| [D-121](#d-121--d-84s-cost-is-read-again-a-never-proved-fragment-runs-only-on-a-saved-model-with-nothing-unsaved) | D-84's cost is read again: a never-proved fragment runs only on a saved model with nothing unsaved | ✅ Accepted · 2026-10-17 |
| [D-122](#d-122--under-article-19-the-modeller-judges-a-drafts-one-run-and-the-agent-that-wrote-it-only-starts-it) | Under Article 19 the modeller judges a DRAFT's one run, and the agent that wrote it only starts it | ✅ Accepted · 2026-10-17 |
| [D-123](#d-123--a-picture-of-a-view-taken-for-the-chat-and-deleted-is-analyze-and-not-an-export) | A picture of a view, taken for the chat and deleted, is ANALYZE and not an export | ✅ Accepted · 2026-10-17 |
| [D-124](#d-124--selecting-or-zooming-to-elements-inside-a-linked-model-is-not-touching-it-and-asks-for-no-confirmation) | Selecting or zooming to elements inside a linked model is not touching it, and asks for no confirmation | ✅ Accepted · 2026-10-17 |
| [D-125](#d-125--a-fragment-says-what-transaction-it-needs-notransactionopen-for-one-that-must-run-with-none-and-modify-for-one-that-needs-herons) | A fragment says what transaction it needs: noTransactionOpen for one that must run with none, and MODIFY for one that needs Heron's | ✅ Accepted · 2026-10-17 |
| [D-126](#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept) | Revit asks the modeller Keep or Discard before a change from a chat is kept | ✅ Accepted · 2026-10-17 · item 5 superseded in part by D-138 · 2026-10-18 |
| [D-128](#d-128--a-request-the-host-calls-a-question-is-never-offered-a-capability-that-changes-the-model) | A request the host calls a question is never offered a capability that changes the model | ✅ Accepted · 2026-10-17 |
| [D-129](#d-129--a-fragment-that-never-ends-is-stopped-by-a-run-clock-it-carries-and-the-bridge-can-stop-heron-again) | A fragment that never ends is stopped by a run clock it carries, and the bridge can stop Heron again | ✅ Accepted · 2026-10-17 |
| [D-130](#d-130--a-stop-lasts-until-revit-restarts-and-a-restart-clears-it) | A stop lasts until Revit restarts, and a restart clears it | ✅ Accepted · 2026-10-17 |
| [D-131](#d-131--a-proof-on-another-revit-release-is-an-entry-of-proofreleases-and-a-second-route-on-a-proven-fragment-is-written-by-accept-signed) | A proof on another Revit release is an entry of proof.releases, and a second route on a PROVEN fragment is written by accept, signed | ✅ Accepted · 2026-10-17 |
| [D-132](#d-132--off-windows-the-heron-server-serves-its-library-alone-and-no-revit-tool) | Off Windows the Heron server serves its library alone, and no Revit tool | ✅ Accepted · 2026-10-17 |
| [D-133](#d-133--a-run-that-contradicts-a-proofreleases-entry-is-reported-as-one-that-contradicts-the-proof-is) | A run that contradicts a proof.releases entry is reported, as one that contradicts the proof is | ✅ Accepted · 2026-10-17 |
| [D-134](#d-134--the-re-ranker-reads-a-cards-first-declared-sentences-beside-its-identity-carried-in-the-store) | The re-ranker reads a card's first declared sentences beside its identity, carried in the store | ✅ Accepted · 2026-10-17 |
| [D-135](#d-135--herons-python-side-keeps-its-data-in-userprofileheron-and-the-companions-notes-in-the-temporary-folder-where-a-store-claudes-copy-cannot-reach-them) | Heron's Python side keeps its data in %USERPROFILE%\.heron, and the Companion's notes in the temporary folder, where a Store Claude's copy cannot reach them | ✅ Accepted · 2026-10-18 |
| [D-136](#d-136--the-companions-folder-is-the-users-temporary-folder-found-from-inside-revit-by-stepping-out-of-a-guid-named-one) | The Companion's folder is the user's temporary folder, found from inside Revit by stepping out of a GUID-named one | ✅ Accepted · 2026-10-18 |
| [D-138](#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking) | The Keep or Discard question gets a switch on the ribbon, and its box can keep the rest of one job without asking | ✅ Accepted · 2026-10-18 · item 2 superseded in part by D-141 · 2026-10-18 |
| [D-139](#d-139--the-four-parts-of-the-specification-are-corrected-in-place-where-a-fact-is-wrong-or-an-accepted-decision-settled-it-and-the-original-words-stay-beside-the-correction) | The four parts of the specification are corrected in place where a fact is wrong or an accepted decision settled it, and the original words stay beside the correction | ✅ Accepted · 2026-10-18 |
| [D-141](#d-141--one-model-is-used-by-one-chat-at-a-time-and-what-revit-keeps-for-a-chat-is-keyed-on-that-chat) | One model is used by one chat at a time, and what Revit keeps for a chat is keyed on that chat | ✅ Accepted · 2026-10-18 |
| [D-142](#d-142--anything-a-kept-change-deletes-from-the-model-is-asked-in-revit-first-in-a-box-of-its-own-whatever-ask-to-keep-says-and-never-under-a-jobs-grant) | Anything a kept change deletes from the model is asked in Revit first, in a box of its own, whatever Ask to Keep says and never under a job's grant | ✅ Accepted · 2026-10-18 |
| [D-143](#d-143--a-credential-in-a-tracked-file-fails-the-pull-request) | A credential in a tracked file fails the pull request | ✅ Accepted · 2026-10-18 |
| [D-144](#d-144--a-superseded-decision-says-so-under-its-own-heading-where-its-citations-land) | A superseded decision says so under its own heading, where its citations land | ✅ Accepted · 2026-10-19 |
| [D-145](#d-145--a-card-may-let-its-caller-answer-one-named-revit-dialog-for-one-job-and-every-other-dialog-is-still-declined) | A card may let its caller answer one named Revit dialog, for one job, and every other dialog is still declined | 🕐 Proposed - **owner has not read this back** |
| [D-146](#d-146--a-fragment-may-name-revits-appearance-asset-namespace-so-a-materials-render-appearance-can-be-edited) | A fragment may name Revit's appearance-asset namespace, so a material's render appearance can be edited | 🕐 Proposed - **owner has not read this back** |
| [D-147](#d-147--the-add-in-counts-each-models-changes-so-the-companion-can-say-the-model-has-changed-since-its-read) | The add-in counts each model's changes, so the Companion can say the model has changed since its read | ✅ Accepted · 2026-11-01 |
| [D-148](#d-148--revit_read-looks-up-the-users-words-as-a-question-so-a-write-is-never-ranked-ahead-of-the-read) | revit_read looks up the user's words as a question, so a write is never ranked ahead of the read | ✅ Accepted · 2026-11-07 |
| [D-149](#d-149--every-colour-font-and-icon-heron-shows-comes-from-the-brand-kit-in-docsbrand) | Every colour, font and icon Heron shows comes from the brand kit in docs/brand | ✅ Accepted · 2026-11-07 |
| [D-150](#d-150--herons-four-switches-move-from-the-ribbon-into-a-settings-window-and-the-ribbon-keeps-heron-settings-and-companion) | Heron's four switches move from the ribbon into a Settings window, and the ribbon keeps Heron, Settings and Companion | ✅ Accepted · 2026-11-07 |
| [D-151](#d-151--a-job-the-modeller-kept-can-be-saved-as-a-named-command-and-the-heron-panel-in-revit-runs-it-through-the-same-door-a-chat-uses) | A job the modeller kept can be saved as a named command, and the Heron panel in Revit runs it through the same door a chat uses | ⏳ Proposed · 2026-11-08 |
| [D-152](#d-152--a-qa-checklist-is-saved-checks-in-the-modellers-words-each-one-read-with-a-pass-rule-run-in-one-go-from-the-chat-or-the-companion) | A QA checklist is saved checks in the modeller's words, each one read with a pass rule, run in one go from the chat or the Companion | ⏳ Proposed · 2026-11-08 |
| [D-153](#d-153--off-windows-the-library-only-server-keeps-its-index-in-a-folder-of-its-own-when-nobody-named-one) | Off Windows, the library-only server keeps its index in a folder of its own when nobody named one | 🕐 Proposed - **owner has not read this back** |
| [D-154](#d-154--an-agent-is-retired-when-nothing-calls-it-and-a-named-thing-already-does-its-job-under-the-owners-name-with-its-row-and-its-file-kept) | An agent is retired when nothing calls it and a named thing already does its job, under the owner's name, with its row and its file kept | ✅ Accepted · 2026-11-15 · read back to the owner |
| [D-155](#d-155--a-chat-can-ask-for-a-change-as-a-dry-run-the-run-itself-rolled-back-with-nothing-kept) | A chat can ask for a change as a dry run: the run itself, rolled back, with nothing kept | ✅ Accepted · 2026-11-12 |
| [D-156](#d-156--a-write-says-element-by-element-which-value-went-from-what-to-what) | A write says, element by element, which value went from what to what | ✅ Accepted · 2026-11-12 |
| [D-157](#d-157--heron-can-undo-its-own-last-change-with-revits-own-undo-only-while-that-change-is-still-on-top) | Heron can undo its own last change, with Revit's own Undo, only while that change is still on top | ✅ Accepted · 2026-11-12 |
| [D-158](#d-158--a-qa-checks-failing-ids-travel-whole-only-when-the-check-names-them) | A QA check's failing ids travel whole, only when the check names them | ✅ Accepted · 2026-11-12 |
| [D-159](#d-159--a-qa-check-carries-its-severity-clause-and-advice-and-a-run-says-what-changed-since-the-last-one-on-the-same-model) | A QA check carries its severity, clause and advice, and a run says what changed since the last one on the same model | ✅ Accepted · 2026-11-12 |
| [D-160](#d-160--a-keep-that-changed-nothing-is-rolled-back-so-no-empty-entry-is-left-in-the-undo-list) | A keep that changed nothing is rolled back, so no empty entry is left in the undo list | ✅ Accepted · 2026-11-12 |
| [D-161](#d-161--a-change-whose-answer-was-lost-can-still-be-read-by-the-requests-own-name) | A change whose answer was lost can still be read, by the request's own name | ✅ Accepted · 2026-11-12 |
| [D-162](#d-162--the-pipe-to-revit-refuses-another-computer-the-chat-checks-who-serves-it-and-the-tokens-file-is-this-users-alone) | The pipe to Revit refuses another computer, the chat checks who serves it, and the token's file is this user's alone | ✅ Accepted · 2026-11-12 |
| [D-163](#d-163--herons-recent-changes-are-docked-in-revit-with-select-zoom-and-undo-reached-through-the-one-dispatcher) | Heron's recent changes are docked in Revit, with Select, Zoom and Undo, reached through the one dispatcher | ✅ Accepted · 2026-11-12 |
| [D-164](#d-164--two-built-in-parameters-of-one-name-that-read-the-same-value-are-one-reading-not-a-name-matched-twice) | Two built-in parameters of one name that read the same value are one reading, not a name matched twice | ✅ Accepted · 2026-11-14 |
| [D-165](#d-165--a-qa-check-never-passes-on-missing-data-a-fourth-answer-no-data-and-a-coverage-line-beside-every-pass-count) | A QA check never passes on missing data: a fourth answer, NO DATA, and a coverage line beside every pass count | ✅ Accepted · 2026-11-16 |
| [D-166](#d-166--changes-admin-and-publish-are-turned-on-only-by-a-click-in-revit-held-in-its-memory-never-read-from-a-file) | Changes, Admin and Publish are turned on only by a click in Revit, held in its memory, never read from a file | • Accepted · built 2026-11-16, all three layers, not yet run in Revit |
| [D-167](#d-167--a-modeller-installs-heron-with-one-setup-file-which-puts-heron-in-place-and-hands-over-to-the-installer-engine) | A modeller installs Heron with one Setup file, which puts Heron in place and hands over to the installer engine | ✅ Accepted · 2026-11-16 |
| [D-168](#d-168--a-change-revit-kept-in-which-nothing-changed-by-every-count-says-nothing-changed-and-refused-when-the-capability-itself-said-no) | A change Revit kept in which nothing changed by every count says NOTHING CHANGED, and REFUSED when the capability itself said no | ✅ Accepted · 2026-11-16 |
| [D-169](#d-169--the-disclaimer-says-what-the-chat-says-heron-reports-which-checked-values-a-model-meets-cites-its-source-and-certifies-no-compliance) | The DISCLAIMER says what the chat says: Heron reports which checked values a model meets, cites its source, and certifies no compliance | ✅ Accepted · 2026-11-16 |
| [D-170](#d-170--the-word-sandbox-leaves-article-11-and-golden-rule-18-new-code-runs-watched-not-contained) | The word sandbox leaves Article 11 and Golden Rule 18: new code runs watched, not contained | ✅ Accepted · 2026-11-16 |
| [D-171](#d-171--heron-may-save-the-model-it-works-in-once-its-undo-entry-has-closed-only-when-the-modeller-asks-and-never-picks-a-name-itself) | Heron may save the model it works in once its Undo entry has closed, only when the modeller asks, and never picks a name itself | ✅ Accepted · 2026-11-16 |
| [D-172](#d-172--article-13-names-the-keep-boxs-job-grant-as-its-one-recorded-exception) | Article 13 names the Keep box's job grant as its one recorded exception | ✅ Accepted · 2026-11-16 |
| [D-173](#d-173--a-release-is-versioned-and-checksummed-now-and-says-plainly-it-is-unsigned-and-is-signed-before-heron-is-given-to-anyone-outside) | A release is versioned and checksummed now and says plainly it is unsigned, and is signed before Heron is given to anyone outside | ✅ Accepted · 2026-11-16 · *the notice waits for signing* superseded in part by D-190 · 2026-11-20 |
| [D-174](#d-174--a-proof-taken-on-another-heron-add-in-build-or-another-revit-build-is-said-under-the-answer-and-never-refused) | A proof taken on another Heron add-in build or another Revit build is said under the answer, and never refused | ✅ Accepted · 2026-11-16 |
| [D-175](#d-175--only-the-chat-that-made-a-heron-change-may-undo-it-the-activity-panes-undo-is-the-modellers-own) | Only the chat that made a Heron change may undo it; the Activity pane's Undo is the modeller's own | ✅ Accepted · 2026-11-16 |
| [D-176](#d-176--the-activity-panes-select-and-zoom-look-past-a-chats-model-hold-because-they-only-look) | The Activity pane's Select and Zoom look past a chat's model hold, because they only look | ✅ Accepted · 2026-11-16 |
| [D-177](#d-177--an-idea-taken-from-another-project-is-written-in-herons-own-words-and-the-project-and-its-author-are-not-named) | An idea taken from another project is written in Heron's own words, and the project and its author are not named | ✅ Accepted · 2026-11-17 |
| [D-178](#d-178--a-qa-checklist-run-can-be-written-as-an-excel-workbook-and-an-html-page-each-a-new-file) | A QA checklist run can be written as an Excel workbook and an HTML page, each a new file | ✅ Accepted · 2026-11-17 |
| [D-179](#d-179--a-project-checklist-can-extend-an-office-checklist-changing-only-severity-target-and-onoff-and-every-run-names-the-checks-it-used) | A project checklist can extend an office checklist, changing only severity, target and on/off, and every run names the checks it used | ✅ Accepted · 2026-11-17 |
| [D-180](#d-180--a-setting-hides-folder-paths-from-what-the-chat-is-told-off-unless-turned-on) | A setting hides folder paths from what the chat is told, off unless turned on | ❌ Not accepted · 2026-11-17 - dropped; Q-40 stands |
| [D-181](#d-181--checking-a-folder-of-models-opens-each-one-in-the-revit-the-modeller-connected-heron-to-one-at-a-time-and-opening-a-model-read-only-is-a-read) | Checking a folder of models opens each one in the Revit the modeller connected Heron to, one at a time, and opening a model read-only is a read | • Accepted, nothing built · 2026-11-19 |
| [D-182](#d-182--an-edited-spreadsheet-comes-back-through-a-comparison-with-the-export-outside-revit-and-only-its-changed-cells-are-sent-a-blank-cell-of-that-file-is-skipped-before-its-parameter-is-looked-for) | An edited spreadsheet comes back through a comparison with the export, outside Revit, and only its changed cells are sent; a blank cell of that file is skipped before its parameter is looked for | ✅ Accepted · 2026-11-20 |
| [D-183](#d-183--sheets-made-from-a-spreadsheet-keep-four-choices-as-built-a-blank-cell-writes-nothing-a-date-goes-in-as-iso-a-refused-value-is-listed-and-its-sheet-kept-and-1000-rows-is-the-most) | Sheets made from a spreadsheet keep four choices as built: a blank cell writes nothing, a date goes in as ISO, a refused value is listed and its sheet kept, and 1,000 rows is the most | ✅ Accepted · 2026-11-20 |
| [D-184](#d-184--before-heron-acts-on-a-parameter-by-its-name-it-checks-where-that-name-lives-in-the-model-and-asks-the-modeller-which-one-when-more-than-one-place-carries-it) | Before Heron acts on a parameter by its name, it checks where that name lives in the model, and asks the modeller which one when more than one place carries it | ✅ Accepted · 2026-11-19 |
| [D-185](#d-185--reload-latest-and-sync-with-central-stay-the-modellers-own-clicks-and-heron-builds-no-route-for-either) | Reload Latest and Sync with Central stay the modeller's own clicks, and Heron builds no route for either | ✅ Accepted · 2026-11-19 |
| [D-186](#d-186--what-heron-should-know-out-of-the-box-runs-bim-and-iso-standards-first-regional-next-and-company-last---and-no-standard-and-no-knowledge-store-ever-ships-with-heron) | What Heron should know out of the box runs BIM and ISO standards first, regional next and company last - and no standard, and no knowledge store, ever ships with Heron | ✅ Accepted · 2026-11-20 |
| [D-187](#d-187--every-new-finding-is-a-github-issue-at-once-the-register-entry-and-its-issue-are-written-in-the-same-change-linked-both-ways) | Every new finding is a GitHub issue at once: the register entry and its issue are written in the same change, linked both ways | ✅ Accepted · 2026-11-20 |
| [D-188](#d-188--a-chat-the-claude-app-resumes-after-a-restart-is-the-same-conversation-to-revit-and-takes-back-its-own-model) | A chat the Claude app resumes after a restart is the same conversation to Revit, and takes back its own model | ✅ Accepted · 2026-11-20 |
| [D-189](#d-189--a-development-session-never-pulls-into-the-main-heron-folder) | A development session never pulls into the main Heron folder | ✅ Accepted · 2026-11-20 |
| [D-190](#d-190--the-readme-stops-asking-modellers-not-to-install-before-a-release-is-signed-and-says-plainly-the-build-is-unsigned-and-what-was-checked) | The README stops asking modellers not to install before a release is signed, and says plainly the build is unsigned and what was checked | ✅ Accepted · 2026-11-20 |
| [D-191](#d-191--the-usage-counter-keeps-offered-helped-and-the-last-date-it-helped-helped-is-only-the-modellers-word-it-moves-a-clause-less-than-one-rank-and-no-question-text-is-kept) | The usage counter keeps offered, helped and the last date it helped; "helped" is only the modeller's word; it moves a clause less than one rank; and no question text is kept | ✅ Accepted · 2026-11-20 |
| [D-192](#d-192--a-job-that-works-only-outside-the-model-it-is-aimed-at-declares-nogroupopen-and-an-admin-or-publish-job-then-opens-no-transaction-group-in-that-model) | A job that works only outside the model it is aimed at declares noGroupOpen, and an Admin or Publish job then opens no transaction group in that model | ✅ Accepted · 2026-11-21 |
| [D-193](#d-193--making-sheets-keeps-three-rules-for-a-table-and-typed-numbers-alike-a-sheet-whose-number-or-name-revit-refuses-is-taken-back-a-number-asked-for-twice-makes-neither-and-1000-is-the-most-in-one-call) | Making sheets keeps three rules for a table and typed numbers alike: a sheet whose number or name Revit refuses is taken back, a number asked for twice makes neither, and 1,000 is the most in one call | ✅ Accepted · 2026-11-21 |
| [D-194](#d-194--every-stored-vector-is-compared-in-plain-python-sqlite-vec-is-not-used) | Every stored vector is compared in plain Python; sqlite-vec is not used | ✅ Accepted · 2026-11-21 |
| [D-195](#d-195--the-loads-panels-finalize-goes-to-revit-as-one-change---the-space-values-and-the-diffusers-flows-as-steps-of-one-request-in-one-undo-entry---with-every-step-gated-as-if-sent-alone) | The Loads panel's Finalize goes to Revit as one change - the Space values and the diffusers' flows as steps of one request, in one undo entry - with every step gated as if sent alone | ⏳ Proposed · 2026-11-21 |

## Format

Write a new decision here, in full, in this shape - then run `python tools/split-decisions.py --write`,
which moves its record into `decisions/D-NN.md` and leaves its heading, its metadata lines and a link here.
Decisions already split are left alone. **A number is used once**: D-45 to D-49 were each used twice before
2026-09-23, and the first decisions under them are now D-97, D-98 and D-101 to D-103.

```markdown
## D-NN — Short title

**Status:** Proposed | Accepted | Superseded by D-NN | Rejected
**Date:** YYYY-MM-DD
**Question:** Q-NN
**Affects:** which documents / components

### Context
What made this decision necessary.

### Decision
What was decided. One or two sentences, stated plainly.

### Alternatives considered
What else was on the table, and why it lost.

### Consequences
What this makes easy. What this makes hard. What it locks in.
```

---

## D-00 — Documentation first, no implementation yet

**Status:** ✔ **Fulfilled** — its condition was met · **Date:** 2026-08-27 · **Read back and closed:** 2026-09-06 · **Affects:** the whole repository

**Full record:** [`decisions/D-00.md`](decisions/D-00.md)

## D-01 — Execution host: Claude Code plugin

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-1](OPEN-QUESTIONS.md)
**Affects:** [02](02-architecture-overview.md), [04](04-heron-mcp.md), [07](07-installation-and-update.md), [ROADMAP](ROADMAP.md)

**Full record:** [`decisions/D-01.md`](decisions/D-01.md)

## D-02 — MCP ↔ add-in transport: named pipes

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-2](OPEN-QUESTIONS.md)
**Affects:** [03 §5](03-heron-revit.md), [04](04-heron-mcp.md)

**Full record:** [`decisions/D-02.md`](decisions/D-02.md)

## D-03 — MCP tool granularity: thick and specific

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-5](OPEN-QUESTIONS.md)
**Affects:** [04 §3](04-heron-mcp.md), [09](09-skills-and-fragments.md), [12](12-security-and-permissions.md)

**Full record:** [`decisions/D-03.md`](decisions/D-03.md)

## D-04 — Generated code execution: hybrid

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-7](OPEN-QUESTIONS.md)
**Affects:** [09 §10](09-skills-and-fragments.md), [13](13-testing-and-quality.md)

**Full record:** [`decisions/D-04.md`](decisions/D-04.md)

## D-09 — Revit thread marshalling: ExternalEvent

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-4](OPEN-QUESTIONS.md)
**Affects:** [03 §4](03-heron-revit.md)

**Full record:** [`decisions/D-09.md`](decisions/D-09.md)

## D-05 — Revit version support: 2020 to latest

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-3](OPEN-QUESTIONS.md)
**Affects:** [03](03-heron-revit.md), [13](13-testing-and-quality.md), [16](16-version-support-strategy.md)

**Full record:** [`decisions/D-05.md`](decisions/D-05.md)

## D-06 — Implementation languages: C# for Revit, Python for brain

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-6](OPEN-QUESTIONS.md)
**Affects:** [03](03-heron-revit.md), [04](04-heron-mcp.md), [05](05-heron-brain.md)

**Full record:** [`decisions/D-06.md`](decisions/D-06.md)

## D-07 — Free open source on public GitHub

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-21](OPEN-QUESTIONS.md), [Q-22](OPEN-QUESTIONS.md)
**Affects:** [06](06-heron-platform.md), [10](10-memory-and-knowledge.md), [12](12-security-and-permissions.md), [17](17-open-source-and-distribution.md)

**Full record:** [`decisions/D-07.md`](decisions/D-07.md)

## D-08 — Licence: Apache 2.0

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-27](OPEN-QUESTIONS.md)
**Affects:** `LICENSE`, `NOTICE`, [17 §3](17-open-source-and-distribution.md)

**Full record:** [`decisions/D-08.md`](decisions/D-08.md)

## D-10 — Repository stays private until working code exists

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-28](OPEN-QUESTIONS.md)
**Affects:** repository visibility, [17](17-open-source-and-distribution.md)

**Full record:** [`decisions/D-10.md`](decisions/D-10.md)

## D-11 — Adopt Master Specification Part 2 (Agent Operating System)

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
**Affects:** [18](18-agent-operating-system.md), [19](19-context-and-cost.md), [20](20-knowledge-trust-and-conflict.md), [21](21-resilience-and-operations.md), [22](22-users-modes-and-extensibility.md), [ROADMAP](ROADMAP.md)

**Full record:** [`decisions/D-11.md`](decisions/D-11.md)

## D-12 — Adopt the Master Handover Baseline (Part 3) and its fifteen Golden Rules

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
**Affects:** [14 — Golden Rules](14-golden-rules.md) and **every document that cross-references a rule number**

**Full record:** [`decisions/D-12.md`](decisions/D-12.md)

## D-13 — Adopt Additional Requirements (Part 4): Kernel, Workflow Engine, Constitution

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
**Affects:** [23](23-heron-kernel.md), [24](24-trust-model.md), [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md), [ROADMAP](ROADMAP.md), [19](19-context-and-cost.md)

**Full record:** [`decisions/D-13.md`](decisions/D-13.md)

## D-14 — Unify six status vocabularies into two orthogonal axes

**Status:** Proposed · **Date:** 2026-08-27 · **Re-put to the owner 2026-09-06 — same answer** · **Question:** [Q-34](OPEN-QUESTIONS.md)
**Affects:** [24](24-trust-model.md), [09](09-skills-and-fragments.md), [18](18-agent-operating-system.md), [20](20-knowledge-trust-and-conflict.md)

**Full record:** [`decisions/D-14.md`](decisions/D-14.md)

## D-15 — Adopt the field notes as authoritative on bridge behaviour

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
**Affects:** [25](25-multi-session-and-binding.md), [03](03-heron-revit.md), [04](04-heron-mcp.md), [14](14-golden-rules.md), [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md)

**Full record:** [`decisions/D-15.md`](decisions/D-15.md)

## D-16 — The session list is built live, and the Revit freeze is out of scope

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Source:** [field notes addendum](00e-field-notes-proven-bridge.md)
**Affects:** [25 2a, 6a](25-multi-session-and-binding.md), [ROADMAP](ROADMAP.md), [Q-36](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-16.md`](decisions/D-16.md)

## D-17 — Runtime state is machine-local, not roaming

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Found during:** Step 1 implementation
**Affects:** `HeronPaths`, [25 §2](25-multi-session-and-binding.md), [06 §2](06-heron-platform.md)

**Full record:** [`decisions/D-17.md`](decisions/D-17.md)

## D-18 — The Transaction Agent belongs to Step 6, not Step 2

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-28 · **Found during:** Step 2 implementation

**Full record:** [`decisions/D-18.md`](decisions/D-18.md)

## D-19 — Writing is off by default until the write path has met a real Revit

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Found during:** Step 6 implementation
**Superseded in part by:** [D-166](#d-166--changes-admin-and-publish-are-turned-on-only-by-a-click-in-revit-held-in-its-memory-never-read-from-a-file), accepted and built 2026-11-16, not yet run in Revit - Changes is no longer *"set in the user's config"*: it is held in Revit's memory, set only from Heron's Settings window, and off every time Revit starts

**Full record:** [`decisions/D-19.md`](decisions/D-19.md)

## D-20 — Millimetres to feet is arithmetic, not UnitUtils

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Found during:** Step 6 implementation

**Full record:** [`decisions/D-20.md`](decisions/D-20.md)

## D-21 — Failure analysis is a table, not a model call

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-28 · **Found during:** Step 6 implementation
**Supersedes:** the **T2** tier given to `HERON-ORC-FAIL-004` in [28](28-agent-registry.md)

**Full record:** [`decisions/D-21.md`](decisions/D-21.md)

## D-22 — A second chat is refused, not allowed to take over

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-28 · **Found during:** Step 6, auditing for gaps

**Full record:** [`decisions/D-22.md`](decisions/D-22.md)

## D-23 — The knowledge store is SQLite, one file per scope

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-10](OPEN-QUESTIONS.md)
**Superseded in part by:** [D-194](#d-194--every-stored-vector-is-compared-in-plain-python-sqlite-vec-is-not-used), accepted by the owner 2026-11-21 - *"`sqlite-vec` for meaning"*: every stored vector is compared in plain Python, and sqlite-vec is not used

**Full record:** [`decisions/D-23.md`](decisions/D-23.md)

## D-24 — Embeddings are computed locally by default

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-11](OPEN-QUESTIONS.md)
**Amended the same day by [D-26](#d-26--the-model-file-is-never-uploaded):** the cloud opt-in below is
**Superseded in part by:** [D-82](#d-82--the-cloud-line-is-drawn-by-content-type-not-by-scope), accepted 2026-09-20 - its framing, *cloud opt-in per scope*: the line is drawn by content type instead, documents may go and model data never leaves the PC

**Full record:** [`decisions/D-24.md`](decisions/D-24.md)

## D-25 — The existing libraries are studied and re-authored, never imported

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-16](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-25.md`](decisions/D-25.md)

## D-26 — The model file is never uploaded

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-12](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-26.md`](decisions/D-26.md)

## D-27 — One voice, and the answer's shape follows the question's shape

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-15](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-27.md`](decisions/D-27.md)

## D-28 — Generated code is C#, compiled at run time, in process

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-7a](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-28.md`](decisions/D-28.md)

## D-29 — A fragment is a composable piece, not a whole answer

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-8](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-29.md`](decisions/D-29.md)

## D-30 — A fragment is promoted by one recorded proof, not by a count of runs

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-9](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-30.md`](decisions/D-30.md)

## D-31 — Product, data and derived are already separated, and the code is the record

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-13](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-31.md`](decisions/D-31.md)

## D-32 — v1 must be able to change the model, and reading is what gets used first

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-20](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-32.md`](decisions/D-32.md)

## D-33 — Heron never assumes an input. It asks — and it asks once

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-33](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-33.md`](decisions/D-33.md)

## D-34 — Heron's own wording is English; understanding the user is not Heron's job

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-17](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-34.md`](decisions/D-34.md)

## D-35 — A shared fragment may carry code, and an unapproved one is refused, not warned about

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-18](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-35.md`](decisions/D-35.md)

## D-36 — No warranty — the standard position, and it is already in place twice

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-25](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-36.md`](decisions/D-36.md)

## D-37 — The name is Heron AI, and no trademark check has been done

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-24](OPEN-QUESTIONS.md)
**Superseded in part by:** [D-85](#d-85--the-tab-says-heron-the-panel-says-ai-bridge-and-the-product-is-still-heron-ai), accepted 2026-09-20 - one sentence: the name no longer *"stays in the ribbon tab"*, which reads **Heron**, with the panel **AI Bridge**; the product is still Heron AI

**Full record:** [`decisions/D-37.md`](decisions/D-37.md)

## D-38 — GitHub now, App Store kept possible, and nothing built for it

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-26](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-38.md`](decisions/D-38.md)

## D-39 — Shadow mode is approved on an analysed disagreement, not a count of agreements

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-29](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-39.md`](decisions/D-39.md)

## D-40 — The dependency graph is SQLite, and an edge is derived before it is stored

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-31](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-40.md`](decisions/D-40.md)

## D-41 — Single-user now; company knowledge is a git repo, and the admin is the reviewer

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-32](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-41.md`](decisions/D-41.md)

## D-42 — The public install command is not settled; the proven one is `setup.ps1`

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-38](OPEN-QUESTIONS.md) — **partly, and it says which part**

**Full record:** [`decisions/D-42.md`](decisions/D-42.md)

## D-43 — The Constitution is accepted — all 30 Articles, binding

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-35](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-43.md`](decisions/D-43.md)

## D-44 — A re-authored fragment starts unproven in Heron, whatever it was elsewhere

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-29 · **Extends:** [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported), [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs)

**Full record:** [`decisions/D-44.md`](decisions/D-44.md)

## D-45 — Heron tracks the MCP SDK across major versions, the way it tracks Revit releases

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-31 · **Extends:** [D-05](#d-05--revit-version-support-2020-to-latest), [D-06](#d-06--implementation-languages-c-for-revit-python-for-brain)
**Numbering:** this number was used twice. Until commit `2248cb3` (2026-08-31) D-45 was *"The library is built out first, and proved in one pass later"* — that decision is now [D-97](#d-97--the-library-is-built-out-first-and-proved-in-one-pass-later). A citation of D-45 written before 2026-08-31 means D-97.

**Full record:** [`decisions/D-45.md`](decisions/D-45.md)

## D-46 — The Emergency Stop button is removed, the switch behind it stays

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-09-06 · **Supersedes:** the ribbon placement in
**Numbering:** this number was used twice. Until commit `75c22a9` (2026-09-06) D-46 was *"A context fragment is consumed by the host, not by another fragment"* — that decision is now [D-98](#d-98--a-context-fragment-is-consumed-by-the-host-not-by-another-fragment). A citation of D-46 written before 2026-09-06 means D-98.

**Full record:** [`decisions/D-46.md`](decisions/D-46.md)

## D-47 — A job can cross projects — both repeating it and copying content — and undo does not cross with it

**Status:** Accepted · **Date:** 2026-09-06 · **Question:** [Q-41](OPEN-QUESTIONS.md)
**Affects:** [25](25-multi-session-and-binding.md), [14 — Rule 16](14-golden-rules.md), [09](09-skills-and-fragments.md), [D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope), [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over)
**Numbering:** this number was used twice. D-47 was first *"Risk is part of routing, not only part of permission"*, which the merge `1f884ae` (2026-09-02) left out of the log - that decision is now [D-101](#d-101--risk-is-part-of-routing-not-only-part-of-permission). Commit `ec17e79` (2026-09-06) gave the number to this decision, so a citation of D-47 written before `ec17e79` means D-101.

**Full record:** [`decisions/D-47.md`](decisions/D-47.md)

## D-48 — One broken part costs one part, never the whole library

**Status:** Accepted · **Date:** 2026-09-06 · **Extends:** [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases)
**Affects:** [`brain/heron_fragment.py`](../brain/heron_fragment.py), [09](09-skills-and-fragments.md), [21](21-resilience-and-operations.md)
**Numbering:** this number was used twice. D-48 was first *"Several fragments may jointly feed one, and the orphan check asks per need"*, which the merge `1f884ae` (2026-09-02) left out of the log - that decision is now [D-102](#d-102--several-fragments-may-jointly-feed-one-and-the-orphan-check-asks-per-need). Commit `ec17e79` (2026-09-06) gave the number to this decision, so a citation of D-48 written before `ec17e79` means D-102.

**Full record:** [`decisions/D-48.md`](decisions/D-48.md)

## D-49 — A heavy optional import never happens on a request thread

**Status:** Accepted · **Date:** 2026-09-06 · **Found during:** `A8`, on the owner's PC
**Affects:** [`brain/heron_embed.py`](../brain/heron_embed.py), [`mcp/server/heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [05](05-heron-brain.md), [21](21-resilience-and-operations.md)
**Numbering:** this number was used twice. D-49 was first *"A need may bind to a provided name that is not its own"*, which the merge `1f884ae` (2026-09-02) left out of the log - that decision is now [D-103](#d-103--a-need-may-bind-to-a-provided-name-that-is-not-its-own). Commit `2087022` (2026-09-06) gave the number to this decision, so a citation of D-49 written before `2087022` means D-103.

**Full record:** [`decisions/D-49.md`](decisions/D-49.md)

## D-50 — Revit says out loud what Heron is doing to it, and whether it is reading or changing

**Status:** Accepted · **Date:** 2026-09-06 · **Found during:** the owner using it
**Affects:** [`HeronActivityBanner.cs`](../revit/Heron.Revit.Addin/HeronActivityBanner.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [25 §6](25-multi-session-and-binding.md), [28](28-agent-registry.md) `HERON-REVIT-UI-022`

**Full record:** [`decisions/D-50.md`](decisions/D-50.md)

## D-51 — A negative case is judged by its counts, not by whether the fragment stayed silent

**Status:** Accepted · **Date:** 2026-09-07 · **Found during:** the owner proving his first fragment with the agent
**Affects:** [`heron_validate.py`](../brain/heron_validate.py) `looks_empty`, [D-30](DECISIONS.md), every fragment that provides `findings`

**Full record:** [`decisions/D-51.md`](decisions/D-51.md)

## D-52 — A count of what was turned down is not a count of what was found

**Status:** Accepted · **Date:** 2026-09-07 · **Found during:** proving seven fragments from two selections
**Affects:** [`heron_validate.py`](../brain/heron_validate.py) `looks_empty`, [D-30](DECISIONS.md), [D-51](DECISIONS.md)

**Full record:** [`decisions/D-52.md`](decisions/D-52.md)

## D-53 — A fragment that cannot come back empty is proved by TRACKING instead

**Status:** Accepted · **Date:** 2026-09-07 · **Found during:** the owner asking to carry on proving fragments
**Affects:** [`heron_validate.py`](../brain/heron_validate.py) `draft_from_record`, [D-30](DECISIONS.md)

**Full record:** [`decisions/D-53.md`](decisions/D-53.md)

## D-54 — The caller's half arrives as text, and Revit is what turns it into a view

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner asking to prove a lot of fragments in one day
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `BindNeeds`/`FromRequest`, [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) `pull_values`/`caller_values`, [D-28](DECISIONS.md), [D-29](DECISIONS.md)
**Superseded in part by:** [D-164](#d-164--two-built-in-parameters-of-one-name-that-read-the-same-value-are-one-reading-not-a-name-matched-twice), accepted 2026-11-14 - item 3 for a parameter name every one of whose carriers is a Revit built-in and all of which read the same: one reading, not refused

**Full record:** [`decisions/D-54.md`](decisions/D-54.md)

## D-55 — A fragment's preview is the run itself, rolled back

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner asking why he had to click something a fragment already does
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `Run(app, request, writing)`, [`HeronOperationRegistry`](../platform/Heron.Core/HeronOperationRegistry.cs), [D-19](DECISIONS.md), [Golden Rule 16](14-golden-rules.md), [Golden Rule 19](14-golden-rules.md)
**Superseded in part by:** [D-171](#d-171--heron-may-save-the-model-it-works-in-once-its-undo-entry-has-closed-only-when-the-modeller-asks-and-never-picks-a-name-itself), accepted by the owner 2026-11-16 - a save asked for with a run is made on a preview too and is not rolled back; the run's changes to the model are still rolled back, and the reply says the save was made

**Full record:** [`decisions/D-55.md`](decisions/D-55.md)

## D-56 — The banner counts in flight off the dispatcher, because an End can arrive before its own Begin

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner watching his own screen
**Affects:** [`HeronActivityBanner.cs`](../revit/Heron.Revit.Addin/HeronActivityBanner.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [D-50](#d-50--revit-says-out-loud-what-heron-is-doing-to-it-and-whether-it-is-reading-or-changing)

**Full record:** [`decisions/D-56.md`](decisions/D-56.md)

## D-57 — The Master Architecture document is a research brief, not a fifth part of the specification

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** the owner asking for the document to be studied and matched against the project
**Affects:** [`HERON_AI_MASTER_ARCHITECTURE.md`](../HERON_AI_MASTER_ARCHITECTURE.md), [32 — reconciled](32-master-architecture-reconciliation.md), [docs/README.md](README.md)

**Full record:** [`decisions/D-57.md`](decisions/D-57.md)

## D-58 — Heron measures what it can see, and the cost meter belongs to the host

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** the owner asking whether token cost could be read from the cloud, or the function removed
**Affects:** [`HERON-OPS-OBS-011`](28-agent-registry.md), [19 §7](19-context-and-cost.md), [`tools/measure-brain.py`](../tools/measure-brain.py), [D-01](DECISIONS.md)

**Full record:** [`decisions/D-58.md`](decisions/D-58.md)

## D-59 — Reading spans loaded links only when the modeller asks, and the answer says how many it read

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/check-revit-gate.py`](../tools/check-revit-gate.py) asking question 8 of the fourteen of all 360 fragments
**Affects:** 62 reading fragments, [`check-revit-gate.py`](../tools/check-revit-gate.py), [09 §4](09-skills-and-fragments.md), [D-30](DECISIONS.md), [D-52](DECISIONS.md), [D-54](DECISIONS.md)

**Full record:** [`decisions/D-59.md`](decisions/D-59.md)

## D-60 — A preview selects what it would change and what it would skip, up to 500

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** reading [`affaan-m/ECC`](https://github.com/affaan-m/ECC) at file level ([33 §5.1](33-external-repository-research.md))
**Affects:** [`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs), [`set-selection`](../brain/fragments/set-selection/), [Golden Rule 9](14-golden-rules.md), [Golden Rule 17](14-golden-rules.md), [D-55](DECISIONS.md)

**Full record:** [`decisions/D-60.md`](decisions/D-60.md)

## D-61 — Only a run that came back may be cached, and re-indexing forgets what changed underneath it

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/measure-routes.py`](../tools/measure-routes.py) parsing the tree for real callers of `heron_search.remember()` and finding one, in a test
**Affects:** [`heron_search.py`](../brain/heron_search.py), [`measure-routes.py`](../tools/measure-routes.py), [19 §5–§6](19-context-and-cost.md), [05 §4](05-heron-brain.md), [D-30](DECISIONS.md)

**Full record:** [`decisions/D-61.md`](decisions/D-61.md)

## D-62 — The brain writes its own audit file, and the reader that already merges does the merging

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/measure-routes.py`](../tools/measure-routes.py) being able to report the structural route share and never the live one
**Affects:** [`brain/heron_audit.py`](../brain/heron_audit.py), [`heron_brain.py`](../mcp/server/heron_brain.py), [`heron_gaps.py`](../brain/heron_gaps.py), [19 §7](19-context-and-cost.md), [Golden Rule 14](14-golden-rules.md)

**Full record:** [`decisions/D-62.md`](decisions/D-62.md)

## D-63 — A want is recorded when a capability is asked for BY NAME and nobody provides it

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/check-reachable.py`](../tools/check-reachable.py) finding `heron_capability.want()` called from two tests and no production code
**Affects:** [`heron_capability.py`](../brain/heron_capability.py), [`heron_brain.py`](../mcp/server/heron_brain.py), [D-40](DECISIONS.md), [06 §6](06-heron-platform.md)

**Full record:** [`decisions/D-63.md`](decisions/D-63.md)

## D-64 — A fragment that goes looking declares what it dropped, and the marker rides only on the empty answer

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/check-revit-gate.py`](../tools/check-revit-gate.py) question 14, narrowed from 143 fragments to 59
**Affects:** 59 reading fragments, [`check-revit-gate.py`](../tools/check-revit-gate.py), [D-52](DECISIONS.md), [D-54](DECISIONS.md)

**Full record:** [`decisions/D-64.md`](decisions/D-64.md)

## D-65 — Heron keeps the degraded-result rule and hands routing to the host

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [D-58](DECISIONS.md) establishing that Heron makes no model calls
**Affects:** [19 §3–§4](19-context-and-cost.md), `HERON-KRN-MAV-017`, [24](24-trust-model.md), [D-01](DECISIONS.md), [D-30](DECISIONS.md)

**Full record:** [`decisions/D-65.md`](decisions/D-65.md)

## D-66 — Heron checks the licence of what it ships by reading the files, not the landing page

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** reading [`K-Dense-AI/scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills) at file level ([33 §5.9](33-external-repository-research.md))
**Affects:** [`tools/check-licence.py`](../tools/check-licence.py), [`tests/test_licence_check.py`](../tests/test_licence_check.py), [17](17-open-source-and-distribution.md), [09](09-skills-and-fragments.md), [D-08](DECISIONS.md)

**Full record:** [`decisions/D-66.md`](decisions/D-66.md)

## D-67 — A point crosses as three millimetre numbers

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** the `create-*` family being the largest block of unprovable fragments in the library
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `OnePoint`/`ManyPoints`, [`HeronUnits`](../platform/Heron.Core/HeronUnits.cs), [`generate-jobs.py`](../tools/generate-jobs.py), [D-54](DECISIONS.md), [D3 in NEEDS-CHECKING](NEEDS-CHECKING.md)

**Full record:** [`decisions/D-67.md`](decisions/D-67.md)

## D-68 — A significant change states its intent before it is made, and is judged against it afterwards

**Status:** Accepted · **Date:** 2026-09-12 · **Found during:** building the improvement gate the

**Full record:** [`decisions/D-68.md`](decisions/D-68.md)

## D-69 — A script in `tools/` reads the code it checks, and that is not a layering violation

**Status:** Accepted · **Date:** 2026-09-12 · **Found during:** the first run of the Python half of the

**Full record:** [`decisions/D-69.md`](decisions/D-69.md)

## D-70 — Heron keeps a usage counter, on the machine, and it is numbers rather than a diary

**Status:** Accepted · **Date:** 2026-09-12 · **Found during:** Stage 8 of the RAG track, which could

**Full record:** [`decisions/D-70.md`](decisions/D-70.md)

## D-71 — Every length a caller types is millimetres, and the fragment converts it

**Status:** Accepted · **Date:** 2026-09-13 · **Decided by:** Ajmal PS, asked directly
**Found during:** the first build of his own "let Heron make what it needs to test itself" method
**Affects:** 23 fragment implementations, [D-20](#d-20--millimetres-to-feet-is-arithmetic-not-unitutils),

**Full record:** [`decisions/D-71.md`](decisions/D-71.md)

## D-72 — Four values a caller could not type are now built from what they type, and a face still is not

**Full record:** [`decisions/D-72.md`](decisions/D-72.md)

## D-73 — A table by name is two separators, and the key is the model's word, not ours

**Status:** Proposed - the owner asked for the best answer rather than picking one, so this row is

**Full record:** [`decisions/D-73.md`](decisions/D-73.md)

## D-74 — A write is AIMED at the model it was told about, not guarded against the one in front

**Status:** Proposed · **Date:** 2026-09-15 · **Asked for by:** Ajmal PS, in his own words, at the

**Full record:** [`decisions/D-74.md`](decisions/D-74.md)

## D-75 — A Development agent acts on Heron itself, not on the artefact Heron builds

**Full record:** [`decisions/D-75.md`](decisions/D-75.md)

## D-76 — Five Standards rows are one agent with a subject, not five files

**Full record:** [`decisions/D-76.md`](decisions/D-76.md)

## D-77 — Two Documentation rows wait for a tag, and two fold into the guard

**Full record:** [`decisions/D-77.md`](decisions/D-77.md)

## D-78 — The metadata checker owns metadata validity, and the third row folds into it

**Full record:** [`decisions/D-78.md`](decisions/D-78.md)

## D-79 — A generated name is six parts, lower case, hyphenated, with the version last

**Full record:** [`decisions/D-79.md`](decisions/D-79.md)

## D-80 — Five Development rows are the host's, because the host is the model

**Full record:** [`decisions/D-80.md`](decisions/D-80.md)

## D-81 — Carried text is stamped, not scanned

**Status:** Accepted · **Date:** 2026-09-20 · **Answers:** [Q-51](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-81.md`](decisions/D-81.md)

## D-82 — The cloud line is drawn by content type, not by scope

**Status:** Accepted · **Date:** 2026-09-20 · **Answers:** [Q-54](OPEN-QUESTIONS.md) ·

**Full record:** [`decisions/D-82.md`](decisions/D-82.md)

## D-83 — An ingest may cross, and it carries the practice, not the count

**Status:** Accepted · **Date:** 2026-09-20 · **Answers:** [Q-55](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-83.md`](decisions/D-83.md)

## D-84 — The sandbox is renamed, not rebuilt

**Status:** Accepted · **Date:** 2026-09-20 · **Answers:** [Q-56](OPEN-QUESTIONS.md)

**Full record:** [`decisions/D-84.md`](decisions/D-84.md)

## D-85 — The tab says Heron, the panel says AI Bridge, and the product is still Heron AI

**Status:** Accepted · **Date:** 2026-09-20 · **Supersedes one sentence of:** [D-37](#d-37--the-name-is-heron-ai-and-no-trademark-check-has-been-done)

**Full record:** [`decisions/D-85.md`](decisions/D-85.md)

## D-86 — A write may declare a question only when answering it requires the write

**Status:** Accepted — **Date:** 2026-09-21 — **Answers:** [Q-57](OPEN-QUESTIONS.md) — **Evidence:** [FRAGMENT-ISSUES rows 158, 137 and 113](FRAGMENT-ISSUES.md)

**Full record:** [`decisions/D-86.md`](decisions/D-86.md)

## D-87 — One Heron product is one Revit ribbon tab

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-20 · **Promoted from:** [S1](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-87.md`](decisions/D-87.md)

## D-88 — One product is one .addin manifest plus one DLL, each with its own AddInId

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-20 · **Promoted from:** [S2](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-88.md`](decisions/D-88.md)

## D-89 — Custom Install is at tab level, and the Heron tab is the one exception

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-20 and 2026-09-21 · **Promoted from:** [S3](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-89.md`](decisions/D-89.md)

## D-90 — Install stays per-user and never asks for administrator rights

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** [`docs/07 §5`](07-installation-and-update.md); Ajmal PS, 2026-09-20 · **Promoted from:** [S4](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-90.md`](decisions/D-90.md)

## D-91 — Product files come from a signed, versioned GitHub release

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-20; [`docs/07 §1a`](07-installation-and-update.md) · **Promoted from:** [S5](work-notes/plans/plugin-extension/00-structure.md)
**Superseded in part by:** [D-173](#d-173--a-release-is-versioned-and-checksummed-now-and-says-plainly-it-is-unsigned-and-is-signed-before-heron-is-given-to-anyone-outside), accepted by the owner 2026-11-16 - amended: *signed* is the bar and not yet the fact; until signing is built a release is versioned and checksummed and says plainly it is unsigned, and it is signed before Heron is given to anyone outside

**Full record:** [`decisions/D-91.md`](decisions/D-91.md)

## D-92 — A ribbon button is a front door onto a proven fragment, not new logic

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-20 · **Promoted from:** [S6](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-92.md`](decisions/D-92.md)

## D-93 — The product list is a manifest read as data, never a list written into the installer

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-20 (*"designed for future expansion"*); [S7](work-notes/plans/plugin-extension/00-structure.md), [S9](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-93.md`](decisions/D-93.md)

## D-94 — Install replaces; there is no separate upgrade path

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-21 · **Promoted from:** [S8](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-94.md`](decisions/D-94.md)

## D-95 — Installing and showing are two different things

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-21 · **Promoted from:** [S9](work-notes/plans/plugin-extension/00-structure.md)

**Full record:** [`decisions/D-95.md`](decisions/D-95.md)

## D-96 — A downloaded Heron is installed by the installer, and only by the installer

**Status:** Accepted · **Date:** 2026-09-21 · **Source:** Ajmal PS, 2026-09-21 · **Promoted from:** [NEEDS-CHECKING `AA9`](NEEDS-CHECKING.md)
**Superseded in part by:** [D-167](#d-167--a-modeller-installs-heron-with-one-setup-file-which-puts-heron-in-place-and-hands-over-to-the-installer-engine), accepted 2026-11-16 - which FILE a modeller starts: the Setup, `HeronSetup-<version>.exe`, which runs the same engine; `deploy-addin.ps1` as the one copy engine and `-ExecutionPolicy Bypass` are unchanged

**Full record:** [`decisions/D-96.md`](decisions/D-96.md)

## D-97 — The library is built out first, and proved in one pass later

**Status:** Accepted · **Date:** 2026-08-30 · **Revises:** [31 §4](31-studying-the-existing-libraries.md),
**Extends:** [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported), [D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere)
**Numbering:** restored 2026-09-23 under a new number, on the owner's instruction, word for word from commit `6582810`. This decision was D-45 from 2026-08-30 until commit `2248cb3` (2026-08-31) wrote a different decision under that number and this text left the log. A citation of D-45 written before 2026-08-31 means this decision.

**Full record:** [`decisions/D-97.md`](decisions/D-97.md)

## D-98 — A context fragment is consumed by the host, not by another fragment

**Status:** Proposed · **Date:** 2026-08-31 · **Revisits:** [31 §1](31-studying-the-existing-libraries.md),
**Touches:** [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer), Step 13's dependency graph
**Numbering:** restored 2026-09-23 under a new number, on the owner's instruction, word for word from commit `6582810`. This decision was D-46 from 2026-08-31 until commit `75c22a9` (2026-09-06) wrote a different decision under that number and this text left the log. A citation of D-46 written before 2026-09-06 means this decision.

**Full record:** [`decisions/D-98.md`](decisions/D-98.md)

## D-99 — A change asked for in a chat is kept at once, with no preview, and Article 9 says so

**Status:** Accepted · **Date:** 2026-09-23 · **Source:** Ajmal PS, 2026-09-23, asked as decision D1 of the earlier-brain plan
**Amends:** [Constitution Article 9](../HERON_CONSTITUTION.md) · **Breaks, on the record:** [Golden Rule 17](14-golden-rules.md) · **Keeps:** [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)
**Superseded by:** [D-126](#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept), accepted by the owner 2026-10-17 - Revit asks the modeller Keep or Discard before a change from a chat is kept, and D-99's behaviour remains only as the owner's switch for that question
**Superseded in part by:** [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on), accepted 2026-09-28 - the row *"Publish and Admin refused"* in its table, which reads *refused unless the owner's switch for that level is on*
**Superseded in part by:** [D-117](#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands), accepted 2026-10-17 - the row *"a warning line when the fragment is not `PROVEN`"*: that change is refused instead, before anything is sent

**Full record:** [`decisions/D-99.md`](decisions/D-99.md)

## D-100 — A drop in the score of the owner's own questions is reported, never a gate

**Status:** Accepted · **Date:** 2026-09-23 · **Source:** Ajmal PS, 2026-09-23, asked as decision D3 of the earlier-brain plan
**Affects:** [`tools/score-routing.py`](../tools/score-routing.py), [`tests/test_score_routing.py`](../tests/test_score_routing.py), [`.github/workflows/gates.yml`](../.github/workflows/gates.yml), which does not run it

**Full record:** [`decisions/D-100.md`](decisions/D-100.md)

## D-101 — Risk is part of routing, not only part of permission

**Status:** Proposed · **Date:** 2026-08-31 · **Touches:** [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs), the risk ladder in the Constitution, [`tools/check-routing.py`](../tools/check-routing.py)
**Numbering:** restored 2026-09-23 under a new number, on the owner's instruction, word for word from commit `5c84302`. This decision was D-47 from 2026-08-31 until the merge `1f884ae` (2026-09-02) left it out of the log, and commit `ec17e79` (2026-09-06) gave the number to a different decision. A citation of D-47 written before `ec17e79` means this decision.
**Superseded in part by:** [D-128](#d-128--a-request-the-host-calls-a-question-is-never-offered-a-capability-that-changes-the-model), accepted 2026-10-17 - item 2, *"Resolution, proposed and not built"*; item 1, the reporting, stands unchanged

**Full record:** [`decisions/D-101.md`](decisions/D-101.md)

## D-102 — Several fragments may jointly feed one, and the orphan check asks per need

**Status:** Accepted · **Date:** 2026-08-31 · **Closes:** the open question in [D-98](#d-98--a-context-fragment-is-consumed-by-the-host-not-by-another-fragment) ·
**Touches:** [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer), Step 13's graph
**Numbering:** restored 2026-09-23 under a new number, on the owner's instruction, word for word from commit `5c84302`, except that its references to the first D-46 and D-49 now say D-98 and D-103, the numbers those decisions have now. This decision was D-48 from 2026-08-31 until the merge `1f884ae` (2026-09-02) left it out of the log, and commit `ec17e79` (2026-09-06) gave the number to a different decision. A citation of D-48 written before `ec17e79` means this decision.

**Full record:** [`decisions/D-102.md`](decisions/D-102.md)

## D-103 — A need may bind to a provided name that is not its own

**Status:** Accepted · **Date:** 2026-08-31 · **Closes:** the remainder left open by [D-102](#d-102--several-fragments-may-jointly-feed-one-and-the-orphan-check-asks-per-need) ·
**Touches:** [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer), the contract validator
**Numbering:** restored 2026-09-23 under a new number, on the owner's instruction, word for word from commit `5c84302`, except that its references to the first D-48 now say D-102, the number that decision has now. This decision was D-49 from 2026-08-31 until the merge `1f884ae` (2026-09-02) left it out of the log, and commit `2087022` (2026-09-06) gave the number to a different decision. A citation of D-49 written before `2087022` means this decision.

**Full record:** [`decisions/D-103.md`](decisions/D-103.md)

## D-104 — The modeller can talk to the open chat from inside Revit

**Status:** Superseded by D-105 the same day, after its first run in Revit lost a message · **Date:** 2026-09-27 · **Source:** Ajmal PS, 2026-09-27, in conversation
**Supersedes:** one sentence of [D-01](#d-01--execution-host-claude-code-plugin) — *"nothing is being built for it"* — and nothing else in it · **Keeps:** [D-01](#d-01--execution-host-claude-code-plugin), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit), [D-34](#d-34--herons-own-wording-is-english-understanding-the-user-is-not-herons-job), [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so)
**Superseded by:** [D-105](#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only) - on the owner's instruction Talk was removed from Revit and from the code, 2026-09-27, after NEEDS-CHECKING row AS4 failed. Its code is linked below at commit `5b18541a`, the last that had it
**Affects:** [`mcp/server/heron_talk.py`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/mcp/server/heron_talk.py), [`mcp/heron-talk.cmd`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/mcp/heron-talk.cmd), [`.mcp.json`](../.mcp.json), [`revit/Heron.Revit.Addin/HeronTalk.cs`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/revit/Heron.Revit.Addin/HeronTalk.cs), [`HeronTalkMailbox.cs`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/revit/Heron.Revit.Addin/HeronTalkMailbox.cs), [`HeronTalkWindow.cs`](https://github.com/Ajmalpshaik/Heron-AI/blob/5b18541a/revit/Heron.Revit.Addin/HeronTalkWindow.cs)

**Full record:** [`decisions/D-104.md`](decisions/D-104.md)

## D-105 — Talk is removed, and the modeller works with Heron in the chat only

**Status:** Accepted · **Date:** 2026-09-27 · **Source:** Ajmal PS, 2026-09-27, in conversation, when NEEDS-CHECKING row AS4 failed on his PC
**Supersedes:** [D-104](#d-104--the-modeller-can-talk-to-the-open-chat-from-inside-revit) in full · **Restores:** the one sentence of [D-01](#d-01--execution-host-claude-code-plugin) that D-104 superseded - *"nothing is being built for it"* · **Keeps:** [D-01](#d-01--execution-host-claude-code-plugin), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)
**Affects:** removed - `mcp/server/heron_talk.py`, `mcp/heron-talk.cmd`, `HeronTalk.cs`, `HeronTalkMailbox.cs`, `HeronTalkWindow.cs`, `Resources/Talk.png`, `tests/Heron.Talk.TestHost/`, `tests/test_talk.py`, `tests/test_talk_contract.py`, `tests/test_talk_served.py`; put back as they were before #338 - [`.mcp.json`](../.mcp.json), [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs), [`HeronPaths.cs`](../platform/Heron.Core/HeronPaths.cs), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py), [`heron_dotnet.py`](../brain/heron_dotnet.py), [`gates.yml`](../.github/workflows/gates.yml)
**Superseded in part by:** [D-108](#d-108--the-modeller-can-see-and-edit-herons-work-in-a-local-companion-page-and-the-chat-stays-the-only-place-to-talk-to-heron), accepted 2026-09-29 - point 2 only as far as *"works with Heron in the chat only"*: the modeller can see and edit Heron's work in a local Companion page, and the chat stays the only place to **talk** to Heron

**Full record:** [`decisions/D-105.md`](decisions/D-105.md)

## D-106 — Admin and Publish each get a switch of their own, and only the owner turns them on

**Status:** Accepted · **Date:** 2026-09-28 · **Source:** Ajmal PS, 2026-09-28, after Heron refused ADD_PROJECT_PARAMETER when he asked for two project parameters
**Supersedes:** the rule that Publish and Admin are *"not reachable in Phase 0 or Phase 1 at all"* - it lived in `HeronPermissions.Allows()` and the client's `risk_refusal`, never in a decision - and so the row *"Publish and Admin refused"* in [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so)'s table, which now reads *refused unless the owner's switch for that level is on* · **Keeps:** [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit), [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so), [Constitution Articles 7 and 10](../HERON_CONSTITUTION.md), [Golden Rule 19](14-golden-rules.md)
**Affects:** [`HeronPermissions.cs`](../platform/Heron.Core/HeronPermissions.cs), [`HeronConfig.cs`](../platform/Heron.Core/HeronConfig.cs), [`HeronOperationRegistry.cs`](../platform/Heron.Core/HeronOperationRegistry.cs), [`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs), [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs), [`Commands.cs`](../revit/Heron.Revit.Addin/Commands.cs), [`HeronSwitchWindow.cs`](../revit/Heron.Revit.Addin/HeronSwitchWindow.cs), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py)
**Superseded in part by:** [D-166](#d-166--changes-admin-and-publish-are-turned-on-only-by-a-click-in-revit-held-in-its-memory-never-read-from-a-file), accepted and built 2026-11-16, not yet run in Revit - items 1, 2 and 5: Admin and Publish are no longer settings in `heron.config` read from that file; each is held in Revit's memory, set only from Settings, and off every time Revit starts

**Full record:** [`decisions/D-106.md`](decisions/D-106.md)

## D-107 — The IFC and DWG exports are PUBLISH, so the Publish switch guards every export

**Status:** Accepted · **Date:** 2026-09-28 · **Question:** [Q-59](OPEN-QUESTIONS.md) · **Source:** Ajmal PS, 2026-09-28, in conversation: *"make both exports PUBLISH for Q-59"*
**Changes:** the third point under *What it accepts* in [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on) - the two exports it names are now guarded by the Publish switch · **Keeps:** [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)
**Affects:** [`export-model-to-ifc/fragment.yaml`](../brain/fragments/export-model-to-ifc/fragment.yaml), [`export-views-to-dwg/fragment.yaml`](../brain/fragments/export-views-to-dwg/fragment.yaml), [`tests/test_admin_publish.py`](../tests/test_admin_publish.py)

**Full record:** [`decisions/D-107.md`](decisions/D-107.md)

## D-108 — The modeller can see and edit Heron's work in a local Companion page, and the chat stays the only place to talk to Heron

**Status:** Accepted · **Date:** 2026-09-29 · **Source:** Ajmal PS, 2026-09-29, in conversation: *"YES YOU CAN DO IT"*, answering question 1 of [40 §18](40-heron-companion.md)
**Supersedes:** point 2 of [D-105](#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only) only as far as *"works with Heron in the chat only"* - the chat stays the only place to **talk** to Heron · **Amends:** [D-09](#d-09--revit-thread-marshalling-externalevent) - a selection watch on `Idling`, throttled to every 200 ms, on every release (point 4, revised before merge) · **Keeps:** [D-01](#d-01--execution-host-claude-code-plugin), [D-02](#d-02--mcp--add-in-transport-named-pipes), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit), [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over), [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so), [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on), [Constitution Articles 7, 8, 9, 12a-12c](../HERON_CONSTITUTION.md)
**Affects:** [40 — Heron Companion](40-heron-companion.md), which holds the design. Nothing is built yet

**Full record:** [`decisions/D-108.md`](decisions/D-108.md)

## D-109 — A Companion button in Revit opens the page, and a Companion switch turns it off without touching Heron

**Status:** Accepted · **Date:** 2026-09-29 · **Source:** Ajmal PS, 2026-09-29, in conversation, after the first Companion run in Revit 2024: *"we need one more button that if I press the button in Revit it will open this companion app ... one more settings that I can turn off this connection for companion app directly ... if I turn off the companion app I can still use the Heron AI"*
**Amends:** [D-108](#d-108--the-modeller-can-see-and-edit-herons-work-in-a-local-companion-page-and-the-chat-stays-the-only-place-to-talk-to-heron) point 1 - the page is served while the Companion switch is on, not only once a chat asks for it · **Keeps:** D-108 otherwise, [D-02](#d-02--mcp--add-in-transport-named-pipes), [D-92](#d-92--a-ribbon-button-is-a-front-door-onto-a-proven-fragment-not-new-logic)
**Affects:** [`HeronCompanionCommands.cs`](../revit/Heron.Revit.Addin/HeronCompanionCommands.cs), [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs), [`HeronLiveState.cs`](../revit/Heron.Revit.Addin/HeronLiveState.cs), [`HeronConfig.cs`](../platform/Heron.Core/HeronConfig.cs), [`HeronPaths.cs`](../platform/Heron.Core/HeronPaths.cs), [`heron_config.py`](../mcp/client/heron_config.py), [`heron_companion.py`](../mcp/companion/heron_companion.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py)

**Full record:** [`decisions/D-109.md`](decisions/D-109.md)

## D-110 — A supply diffuser neck in an NC/RC 30 room is held to the owner's 2.5 m/s, not ASHRAE's 2.2

**Status:** Accepted · **Date:** 2026-10-02 · **Answers:** [F42](proposals/f42.md) item 5 · **Source:** Ajmal PS, 2026-10-02, in conversation: *"Use 2.5 m/s, ask standards once per project"*
**Keeps:** [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) - the figure is the owner's, given once, and named wherever it is used · [D-83](#d-83--an-ingest-may-cross-and-it-carries-the-practice-not-the-count)'s reservation for the first multi-user install
**Affects:** [`heron_hvac.py`](../brain/heron_hvac.py) - `diffuser_select` and the `air_terminal_guidance` table's note, [`tests/test_hvac.py`](../tests/test_hvac.py), [41 §8 and §12](41-hvac-design.md)

**Full record:** [`decisions/D-110.md`](decisions/D-110.md)

## D-111 — A project's governing standards are asked once and kept for that project

**Status:** Accepted · **Date:** 2026-10-02 · **Answers:** [F42](proposals/f42.md) item 2 · **Source:** Ajmal PS, 2026-10-02, in conversation: *"Use 2.5 m/s, ask standards once per project"*
**Keeps:** [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) - *it asks once*, scoped to something real: the project · [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) and Golden Rule 5 - one file per project, never shared sideways
**Affects:** [`heron_hvac.py`](../brain/heron_hvac.py), [`heron_designbasis.py`](../brain/heron_designbasis.py), [`heron_brain.py`](../mcp/server/heron_brain.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py) (its comment only - the risk stays READ), [`tests/test_hvac.py`](../tests/test_hvac.py), [41 §11](41-hvac-design.md)

**Full record:** [`decisions/D-111.md`](decisions/D-111.md)

## D-112 — A fragment that must work in one of Revit's edit modes opens its own transactions inside Heron's group

**Status:** Proposed · **Date:** 2026-10-03 · **Source:** the school in Project2, 2026-10-03: *"make the stair also"*, and Revit refusing a stair inside Heron's transaction, measured the same day
**Keeps:** Golden Rule 16 - one TransactionGroup, one undo, rolled back on a preview · the read path's guarantee that nothing there can open a transaction
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`heron_fragment.py`](../brain/heron_fragment.py) (`AMBIENT`), [`check-revit-gate.py`](../tools/check-revit-gate.py), `create-stairs`

**Full record:** [`decisions/D-112.md`](decisions/D-112.md)

## D-113 — A project's kept answers are filed under the model's own id, and answers kept under its template's id are asked about

**Status:** Proposed · **Date:** 2026-10-06 · **Source:** [FRAGMENT-ISSUES 5b-324](fragment-issues/section-5b-rows-301-325.md), measured on the owner's PC the same day
**Supersedes:** the KEY in [D-111](#d-111--a-projects-governing-standards-are-asked-once-and-kept-for-that-project) item 4 - a new project inherits its Project Information UniqueId from its template, so every model made from one template shared one record
**Affects:** [`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs), [`heron_write.py`](../mcp/server/heron_write.py), [`heron_earlier.py`](../brain/heron_earlier.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`heron_earlier_answers`)

**Full record:** [`decisions/D-113.md`](decisions/D-113.md)

## D-114 — A fragment that reloads a family is handed Revit's load options by the add-in

**Status:** Proposed · **Date:** 2026-10-06 · **Source:** [FRAGMENT-ISSUES 5b-347](fragment-issues/section-5b-rows-326-350.md) - a nested nut edited and saved could not be reloaded into the families that hold it
**Keeps:** [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) - a fragment never declares a type · [D-112](#d-112--a-fragment-that-must-work-in-one-of-revits-edit-modes-opens-its-own-transactions-inside-herons-group)'s ambient-need pattern · Constitution Article 7 - a family reload is confirmed for that operation
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) (`FamilyReload`, `FamilyLoadNeed`), [`heron_fragment.py`](../brain/heron_fragment.py) (`AMBIENT`), `load-family`

**Full record:** [`decisions/D-114.md`](decisions/D-114.md)

## D-115 — A fragment that must run with no transaction open declares D-112's switch too, and the first is OPEN_VIEW

**Status:** Accepted · **Date:** 2026-10-13 · **Accepted:** Ajmal PS, 2026-10-13, in conversation: *"accept D-115 and merge the PR"* · **Source:** [FRAGMENT-ISSUES 5b-456](fragment-issues/section-5b-rows-451-475.md) - OPEN_VIEW refused by Revit for every view through revit_change, a sheet included, and the cause measured on Project1, Revit 2020, the same day
**Widens:** [D-112](#d-112--a-fragment-that-must-work-in-one-of-revits-edit-modes-opens-its-own-transactions-inside-herons-group) item 1 - the switch it made for Revit's edit modes is declared for one more reason, and nothing else in D-112 changes
**Keeps:** Golden Rule 16 - one TransactionGroup, one undo, rolled back on a preview · the read path's guarantee that nothing there can open a transaction · [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) - nothing new in the add-in
**Affects:** `open-view` only. Nothing in [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) or [`heron_fragment.py`](../brain/heron_fragment.py) changes

**Full record:** [`decisions/D-115.md`](decisions/D-115.md)

## D-116 — A list of family forms built in one call undoes as one, and cuts stay a separate call

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation, answering both halves of the question: *"Yes, one undo per list"* and *"No, keep cuts separate"* · **Question:** [Q-62](OPEN-QUESTIONS.md)
**Keeps:** Golden Rule 16 and Constitution Article 8 - one call, one TransactionGroup, one undo · all or nothing - one row that fails keeps none of the list
**Affects:** [`create-family-forms`](../brain/fragments/create-family-forms/fragment.yaml) and [`combine-family-forms`](../brain/fragments/combine-family-forms/fragment.yaml), neither changed · [NEEDS-CHECKING Group DA](needs-checking/group-da.md), unchanged · [04](04-heron-mcp.md)

**Full record:** [`decisions/D-116.md`](decisions/D-116.md)

## D-117 — A change is kept only from a fragment whose proof still stands

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-116 and merge the PR"* · **Source:** the after-the-run warning in [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py)'s `revit_change`, and the [Constitution](../HERON_CONSTITUTION.md)'s Enforcement table naming no place Article 11 is enforced
**Numbering:** written and accepted as D-116; renumbered D-129 on 2026-10-17 while merging `main` - in the hours this branch was open `main` gave D-116 to D-126 to other decisions (#546, #547, #557, #554, #556, #555, #559) and open pull requests claimed D-127 and D-128, so it took the first number no branch held. The owner's words quoted above name it by its first number.
**Enforces:** [Constitution Article 11](../HERON_CONSTITUTION.md) · [Golden Rule 18](14-golden-rules.md)
**Supersedes:** one row of [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so)'s table - *"a warning line when the fragment is not `PROVEN`"*, said after the change. It is now a refusal, said instead of sending anything
**Keeps:** [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so) - no preview in front of a proven change · [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back) - the rolled-back run · [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) · [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) · [Golden Rule 19](14-golden-rules.md)
**Affects:** [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) (`proof_of`, `unproven_refusal`, `cmd_fragment`), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`revit_change`), [`HERON_CONSTITUTION.md`](../HERON_CONSTITUTION.md) (the Enforcement table), the `heron-fragment-author` agent on both hosts

**Full record:** [`decisions/D-117.md`](decisions/D-117.md)

## D-118 — A tool description over its budget stops a pull request

**Status:** Accepted · **Date:** 2026-10-17 · **Source:** Ajmal PS, 2026-10-17, in conversation, answering the question put in the pull request that added the budget: *"keep it a gate"*
**Numbering:** written and accepted as D-116; renumbered D-129 on 2026-10-17 while merging `main` - in the hours this branch was open `main` gave D-116 to D-126 to other decisions (#546, #547, #557, #554, #556, #555, #559) and open pull requests claimed D-127 and D-128, so it took the first number no branch held. The owner's words quoted above name it by its first number.
**Keeps:** [D-03](#d-03--mcp-tool-granularity-thick-and-specific) - the tool list's cost is accepted and kept down by a small core set · [D-100](#d-100--a-drop-in-the-score-of-the-owners-own-questions-is-reported-never-a-gate) - the routing score stays a report; this decides a different number
**Affects:** [`tests/test_tool_sizes.py`](../tests/test_tool_sizes.py), [`mcp/server/heron_tools.py`](../mcp/server/heron_tools.py) (`--sizes`), [`.github/workflows/gates.yml`](../.github/workflows/gates.yml), which runs every suite

**Full record:** [`decisions/D-118.md`](decisions/D-118.md)

## D-119 — A DRAFT fragment may run once on the model in front, behind a Keep or Discard prompt in Revit, and Article 11 says so

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-116 to D-119 and merge the PR"* · **Source:** the owner's brief of 2026-10-17 - make *"Heron has no tool for this job"* fast and safe, so a fragment written on the spot can be tried on the model it was written for
**Numbering:** written and accepted as D-116 to D-119 on 2026-10-17, and renumbered D-119 to D-122 the same day as the pull request merged, because [#546](https://github.com/Ajmalpshaik/Heron-AI/pull/546), [#547](https://github.com/Ajmalpshaik/Heron-AI/pull/547) and [#557](https://github.com/Ajmalpshaik/Heron-AI/pull/557) had taken D-116, D-117 and D-118 first. The owner's words above quote the numbers he was shown.
**Amends, once accepted:** [Constitution Article 11](../HERON_CONSTITUTION.md) · **Breaks, on the record:** [Golden Rule 18](14-golden-rules.md) · **Keeps:** [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit), [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back), [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so), Article 8
**Beside:** [D-117](#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands), accepted the same day - it refuses a DRAFT's kept change on `revit_change` and `fragment --write --apply`; this decision's `try-once` is the one way a DRAFT's change is kept, and it goes through neither
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`HeronConfig.cs`](../platform/Heron.Core/HeronConfig.cs), [`heron_config.py`](../mcp/client/heron_config.py), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) (`try-once`), [`heron-fragment-author`](../.claude/agents/heron-fragment-author.md)
**Superseded in part by:** [D-170](#d-170--the-word-sandbox-leaves-article-11-and-golden-rule-18-new-code-runs-watched-not-contained), accepted by the owner 2026-11-16 - one word of the Article 11 text it wrote: *"that prompt and that one group are its sandbox"* reads *"that prompt and that one group are how it is watched, and watched is not contained"*; what the exception allows does not change

**Full record:** [`decisions/D-119.md`](decisions/D-119.md)

## D-120 — A run the modeller kept after checking it is the positive case of a proof, and the negative case is still its own rolled-back run

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-116 to D-119 and merge the PR"* · **Source:** the owner's brief of 2026-10-17, with [D-119](#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so)
**Numbering:** written and accepted as D-116 to D-119 on 2026-10-17, and renumbered D-119 to D-122 the same day as the pull request merged, because [#546](https://github.com/Ajmalpshaik/Heron-AI/pull/546), [#547](https://github.com/Ajmalpshaik/Heron-AI/pull/547) and [#557](https://github.com/Ajmalpshaik/Heron-AI/pull/557) had taken D-116, D-117 and D-118 first. The owner's words above quote the numbers he was shown.
**Applies:** [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) - its bar is unchanged · **Keeps:** [D-53](#d-53--a-fragment-that-cannot-come-back-empty-is-proved-by-tracking-instead)
**Affects:** [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) (`try-once`, `validate --kept`), [`heron_validate.py`](../brain/heron_validate.py) (`draft`, `accept`)

**Full record:** [`decisions/D-120.md`](decisions/D-120.md)

## D-121 — D-84's cost is read again: a never-proved fragment runs only on a saved model with nothing unsaved

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-116 to D-119 and merge the PR"* · **Source:** the owner's brief of 2026-10-17
**Numbering:** written and accepted as D-116 to D-119 on 2026-10-17, and renumbered D-119 to D-122 the same day as the pull request merged, because [#546](https://github.com/Ajmalpshaik/Heron-AI/pull/546), [#547](https://github.com/Ajmalpshaik/Heron-AI/pull/547) and [#557](https://github.com/Ajmalpshaik/Heron-AI/pull/557) had taken D-116, D-117 and D-118 first. The owner's words above quote the numbers he was shown.
**Revisits:** [D-84](#d-84--the-sandbox-is-renamed-not-rebuilt)'s accepted cost · **Keeps:** D-84's decision - watched, not contained, no subprocess
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) - the ask mode's refusal of an unsaved model

**Full record:** [`decisions/D-121.md`](decisions/D-121.md)

## D-122 — Under Article 19 the modeller judges a DRAFT's one run, and the agent that wrote it only starts it

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-116 to D-119 and merge the PR"* · **Source:** the owner's brief of 2026-10-17 - *"Article 19 (the modeller is the validator) supports this path"*
**Numbering:** written and accepted as D-116 to D-119 on 2026-10-17, and renumbered D-119 to D-122 the same day as the pull request merged, because [#546](https://github.com/Ajmalpshaik/Heron-AI/pull/546), [#547](https://github.com/Ajmalpshaik/Heron-AI/pull/547) and [#557](https://github.com/Ajmalpshaik/Heron-AI/pull/557) had taken D-116, D-117 and D-118 first. The owner's words above quote the numbers he was shown.
**Reads:** [Constitution Article 19](../HERON_CONSTITUTION.md) · **Keeps:** Article 20 - nothing the author writes approves itself
**Affects:** [`heron-fragment-author`](../.claude/agents/heron-fragment-author.md) and its `.codex` copy

**Full record:** [`decisions/D-122.md`](decisions/D-122.md)

## D-123 — A picture of a view, taken for the chat and deleted, is ANALYZE and not an export

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"A picture is not model data, ship it"* · **Source:** the owner's brief of 2026-10-17 for `revit_view_image` - *"a read-only tool that returns a PNG of a Revit view into the chat, so the AI can check its own work"*
**Keeps:** [D-107](#d-107--the-ifc-and-dwg-exports-are-publish-so-the-publish-switch-guards-every-export) - every `export-*` stays PUBLISH, `export-view-image` included · [D-26](#d-26--the-model-file-is-never-uploaded) · [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) - Changes stays off · [Constitution Articles 8 and 10](../HERON_CONSTITUTION.md) · Golden Rule 19 - the risk is the registry's, by name · the read path's guarantee that nothing on `run_fragment_read` opens a transaction
**Asks:** whether a view's picture is *model data* under [D-82](#d-82--the-cloud-line-is-drawn-by-content-type-not-by-scope), put as [Q-64](OPEN-QUESTIONS.md) - answered: **not model data**
**Affects:** [`RevitCapture.cs`](../revit/Heron.Revit.Addin/RevitCapture.cs) (new), [`HeronOperationRegistry.cs`](../platform/Heron.Core/HeronOperationRegistry.cs), [`HeronPaths.cs`](../platform/Heron.Core/HeronPaths.cs) (`Captures`), [`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`heron_tools.py`](../mcp/server/heron_tools.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_failure.py`](../mcp/server/heron_failure.py), [NEEDS-CHECKING group ER](needs-checking/group-er.md)

**Full record:** [`decisions/D-123.md`](decisions/D-123.md)

## D-124 — Selecting or zooming to elements inside a linked model is not touching it, and asks for no confirmation

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"Q-64: selecting/zooming linked elements is not touching, no confirmation"* · **Source:** [Q-65](open-questions/tier-2.md), raised the same day by the show door's first linked fragment, `ZOOM_TO_LINKED_ELEMENTS`
**Numbering:** written and accepted as D-118, and pushed as D-119; renumbered D-124 on 2026-10-17 while merging `main`, where #557, #554 and #556 had already given D-118 to D-123 to other decisions. Its question was written as Q-64 and is [Q-65](open-questions/tier-2.md) since #556 gave Q-64 to another; the owner's words quoted above name it by that first number.
**Answers:** [Q-65](open-questions/tier-2.md)
**Keeps:** Constitution Article 7 for everything that CHANGES a link - unloading, reloading, a path type, room bounding - and its explicit confirmation for each · [D-59](#d-59--reading-spans-loaded-links-only-when-the-modeller-asks-and-the-answer-says-how-many-it-read) - reading a link
**Affects:** [`heron_tools.py`](../mcp/server/heron_tools.py) and [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`revit_show` - the link hold removed), `zoom-to-linked-elements`

**Full record:** [`decisions/D-124.md`](decisions/D-124.md)

## D-125 — A fragment says what transaction it needs: noTransactionOpen for one that must run with none, and MODIFY for one that needs Heron's

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation, on the rows this decision closes: *"do it all the work dont keep anyting not finished okkey"* · **Source:** [FRAGMENT-ISSUES 5b-498 and 5b-499](fragment-issues/section-5b-rows-401-425.md), found building the show door
**Takes:** [D-115](#d-115--a-fragment-that-must-run-with-no-transaction-open-declares-d-112s-switch-too-and-the-first-is-open_view)'s first alternative - *"a second ambient need with an honest name"* - which it left for the owner
**Keeps:** [D-112](#d-112--a-fragment-that-must-work-in-one-of-revits-edit-modes-opens-its-own-transactions-inside-herons-group) - `editScopeFailures` for a fragment that opens its own transactions, refused everywhere but the write path · the read and show doors' guarantee that Heron opens no transaction there · Golden Rule 16
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) (`NoTransactionNeed`), [`heron_fragment.py`](../brain/heron_fragment.py) (`AMBIENT`), [`check-revit-gate.py`](../tools/check-revit-gate.py), `open-view` (version 3), `isolate-elements` (risk)

**Full record:** [`decisions/D-125.md`](decisions/D-125.md)

## D-126 — Revit asks the modeller Keep or Discard before a change from a chat is kept

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-117 and mark the PR ready"* · **Source:** Ajmal PS agreed it should be added, in the task that asked for it on 2026-10-17
**Numbering:** drafted as D-116; renumbered D-117, D-118, D-123, D-124 and then D-126 on 2026-10-17 while merging `main`, where #546, #547, #557, #554, #556 and #555 had given D-116 to D-125 to other decisions. The owner's words quoted above name it by the number it had when it was accepted, D-117
**One box with D-119:** merged into [D-119](#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so)'s Keep or Discard prompt on the owner's instruction, 2026-10-17, when #554 had built that prompt in the same code first: *"Unify into one"*. Items 2, 4 and 9 below say how it was built after that, and are marked so
**Superseded in part by:** [D-138](#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking), accepted by the owner 2026-10-18 - item 5's switch is on the ribbon too, off by default, and the box can keep the rest of one job without asking
**Supersedes:** [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so) - a change asked for in a chat is no longer kept at once; Revit shows it and the modeller keeps it · **Keeps:** [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back), [D-60](#d-60--a-preview-selects-what-it-would-change-and-what-it-would-skip-up-to-500), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit), [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on), [D-112](#d-112--a-fragment-that-must-work-in-one-of-revits-edit-modes-opens-its-own-transactions-inside-herons-group), [D-119](#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so) - one prompt for both doors, Golden Rules 16 and 19, Constitution Article 7
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`HeronKeepQuestion.cs`](../revit/Heron.Revit.Addin/HeronKeepQuestion.cs), [`RevitChangedSet.cs`](../revit/Heron.Revit.Addin/RevitChangedSet.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`HeronDialogWatch.cs`](../revit/Heron.Revit.Addin/HeronDialogWatch.cs), [`HeronActivityBanner.cs`](../revit/Heron.Revit.Addin/HeronActivityBanner.cs), [`HeronChangesWindow.cs`](../revit/Heron.Revit.Addin/HeronChangesWindow.cs), [`HeronConfig.cs`](../platform/Heron.Core/HeronConfig.cs), [`HeronPermissions.cs`](../platform/Heron.Core/HeronPermissions.cs), [`heron_config.py`](../mcp/client/heron_config.py), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py), [`heron_failure.py`](../mcp/server/heron_failure.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py), [`host.chat.yaml`](../brain/instructions/host.chat.yaml), [Constitution Article 9](../HERON_CONSTITUTION.md), [Golden Rule 17](14-golden-rules.md)

**Full record:** [`decisions/D-126.md`](decisions/D-126.md)

## D-128 — A request the host calls a question is never offered a capability that changes the model

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"do it the best only"* - choosing option B of the work note `2026-10-17-plain-questions-owner-call.md` (retired once #565 merged; git history holds it at `5fa14fd8`), the one advised · **Source:** [FRAGMENT-ISSUES 5b-477 and 5b-478](fragment-issues/section-5b-rows-401-425.md) - of 120 plain questions, 32 reached a write on the trained backend and 27 on lexical
**Numbering:** written as D-116 on 2026-10-17 and renumbered D-128 the same day, before it merged: main had given D-116 to D-123 to other work while this was open, and open pull requests claim D-124 to D-127. The owner's words name no number
**Supersedes:** [D-101](#d-101--risk-is-part-of-routing-not-only-part-of-permission) item 2, *"Resolution, proposed and not built"* - its item 1, the reporting, stands unchanged
**Keeps:** [D-01](#d-01--execution-host-claude-code-plugin) - the host decides what the user meant, and Heron never classifies it · [D-65](#d-65--heron-keeps-the-degraded-result-rule-and-hands-routing-to-the-host) - routing is the host's · [D-86](#d-86--a-write-may-declare-a-question-only-when-answering-it-requires-the-write) - which questions a write may declare · Golden Rule 19 - a risk is read from the registry, never typed
**Affects:** [`heron_retrieve.py`](../brain/heron_retrieve.py) (`eligible`, `retrieve`, `find`: `ceiling`), [`heron_brain.py`](../mcp/server/heron_brain.py) (`lookup(question=...)`, `question_ceiling`), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`heron_lookup`'s `question`)

**Full record:** [`decisions/D-128.md`](decisions/D-128.md)

## D-129 — A fragment that never ends is stopped by a run clock it carries, and the bridge can stop Heron again

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"accept D-116 and mark the PR ready"* · **Source:** the owner's task of 2026-10-17: *"A fragment that loops forever must stop, roll back and say so before the dispatcher gives up"*
**Numbering:** written and accepted as D-116; renumbered D-129 on 2026-10-17 while merging `main` - in the hours this branch was open `main` gave D-116 to D-126 to other decisions (#546, #547, #557, #554, #556, #555, #559) and open pull requests claimed D-127 and D-128, so it took the first number no branch held. The owner's words quoted above name it by its first number.
**Updates:** [D-46](#d-46--the-emergency-stop-button-is-removed-the-switch-behind-it-stays)'s stated costs - four of the five are smaller now. D-46 itself is unchanged: there is still no Emergency Stop button
**Keeps:** [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) · Golden Rule 16 · [row 5b-14](fragment-issues/section-5b-rows-001-025.md)'s named allow-list outside the gate · [Q-68](open-questions/from-master-specification-part-2.md), answered as [D-130](#d-130--a-stop-lasts-until-revit-restarts-and-a-restart-clears-it)
**Affects:** [`HeronRunClock.cs`](../revit/Heron.Revit.Addin/HeronRunClock.cs) (new), [`HeronStackGuard.cs`](../revit/Heron.Revit.Addin/HeronStackGuard.cs), [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`HeronStop.cs`](../platform/Heron.Core/HeronStop.cs), [`HeronOperationRegistry.cs`](../platform/Heron.Core/HeronOperationRegistry.cs) (`stop`), [`heron_failure.py`](../mcp/server/heron_failure.py), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py)

**Full record:** [`decisions/D-129.md`](decisions/D-129.md)

## D-130 — A stop lasts until Revit restarts, and a restart clears it

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"Q-64: restart clears it, accept your recommendation"* · **Answers:** [Q-68](open-questions/from-master-specification-part-2.md)
**Numbering:** written and accepted as D-117, answering a question then numbered Q-64; renumbered D-130 and Q-68 on 2026-10-17 while merging `main`, past every number `main` or an open pull request held. The owner's words quoted above name the question by its first number.
**Amends:** [21 §4](21-resilience-and-operations.md) requirement 3 - sticky until Revit restarts, not past it
**Keeps:** [D-46](#d-46--the-emergency-stop-button-is-removed-the-switch-behind-it-stays) - no second control · [D-129](#d-129--a-fragment-that-never-ends-is-stopped-by-a-run-clock-it-carries-and-the-bridge-can-stop-heron-again) item 7 - nothing over the bridge resumes
**Affects:** [`HeronStop.cs`](../platform/Heron.Core/HeronStop.cs) (comments only), [21 §4](21-resilience-and-operations.md), [D-129](decisions/D-129.md)

**Full record:** [`decisions/D-130.md`](decisions/D-130.md)

## D-131 — A proof on another Revit release is an entry of proof.releases, and a second route on a PROVEN fragment is written by accept, signed

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation - asked to accept Q-66's recommended answer or choose another, he chose *"Accept recommended (Recommended)"* · **Source:** [Q-66](open-questions/tier-2.md), found preparing [NEEDS-CHECKING Group EQ](needs-checking/group-eq.md)
**Numbering:** written when `origin/main` ended at D-126 and open pull requests already named D-127 to D-130, so this took D-131; `tools/next-id.py` found D-131 held by this branch alone when `main` was merged in, by then ending at D-130.
**Answers:** [Q-66](open-questions/tier-2.md)
**Keeps:** [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) - the evidence a proof needs, a person's signature, and a proof going stale made visible · the rule that nothing in the proving tools writes `heron-status`
**Affects:** [`heron_fragment.py`](../brain/heron_fragment.py) (`RELEASE_REQUIRED`, `release_problems`), [`heron_validate.py`](../brain/heron_validate.py) (`draft` and `accept` with `--release` and `--second-route`), [`heron_matrix.py`](../brain/heron_matrix.py) (`proven_releases`), [`batch-prove.py`](../tools/batch-prove.py) (`--release`), [`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md), [Group EQ](needs-checking/group-eq.md)

**Full record:** [`decisions/D-131.md`](decisions/D-131.md)

## D-132 — Off Windows the Heron server serves its library alone, and no Revit tool

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"yes"*, to Q-67 put to him in plain words · **Answers:** [Q-67](open-questions/tier-2.md)
**Keeps:** [D-02](#d-02--mcp--add-in-transport-named-pipes) - the bridge is a Windows named pipe, and nothing here reaches it · [D-01](#d-01--execution-host-claude-code-plugin) · every tool on Windows, exactly as before
**Affects:** [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_tools.py`](../mcp/server/heron_tools.py) (`LIBRARY`), [`AGENTS.md`](../AGENTS.md), [38 §4](38-the-cloud-environment.md), the [`heron-session`](../.claude/skills/heron-session/SKILL.md) start-up line, [`tests/test_mcp_stdio.py`](../tests/test_mcp_stdio.py), [`tests/test_tool_registry.py`](../tests/test_tool_registry.py)

**Full record:** [`decisions/D-132.md`](decisions/D-132.md)

## D-133 — A run that contradicts a proof.releases entry is reported, as one that contradicts the proof is

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation: *"write a new decision noting D-131's gap is closed"*, after asking for the change (#601) to be merged · **Source:** the last of [D-131](#d-131--a-proof-on-another-revit-release-is-an-entry-of-proofreleases-and-a-second-route-on-a-proven-fragment-is-written-by-accept-signed)'s Consequences
**Updates:** [D-131](#d-131--a-proof-on-another-revit-release-is-an-entry-of-proofreleases-and-a-second-route-on-a-proven-fragment-is-written-by-accept-signed)'s last Consequence - *"`brain/heron_runs.py` still reads the proof's release alone through `heron_matrix.proof_release`, so a run that contradicts an entry is not yet reported there"* - which stopped being true when #601 merged on 2026-10-17. D-131 itself is unchanged
**Keeps:** [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) - a proof is a person's evidence and a failure is a machine's, and nothing here picks between them · D-131 §2 - a stale entry is recorded and not counted · D-131 §6 - an entry's release is a field, read by HERON-FRG-MTX-009, never parsed again
**Affects:** [`heron_runs.py`](../brain/heron_runs.py) (`measure`), [`HERON-FRG-PRF-002.yaml`](../brain/agents/HERON-FRG-PRF-002.yaml) (1.0.0 to 1.1.0), [`tests/test_runs.py`](../tests/test_runs.py) §8

**Full record:** [`decisions/D-133.md`](decisions/D-133.md)

## D-134 — The re-ranker reads a card's first declared sentences beside its identity, carried in the store

**Status:** Accepted · **Date:** 2026-10-17 · **Accepted:** Ajmal PS, 2026-10-17, in conversation - shown the numbers measured on `main` `e309c81` and the costs, asked *"Ship it?"*, he chose *"Yes, ship it (Recommended)"* · **Source:** [row 5b-978](fragment-issues/section-5b-rows-976-1000.md)
**Numbering:** written as D-133, which `tools/next-id.py` found free on `main` and on every branch when it was written, 2026-10-17. #615 merged its own D-133 first, so this was renumbered D-134 with `tools/renumber.py` the same day; the owner's yes was given to it as D-133. The question put to him named row 5b-918, free then; branches took ids up to 5b-977 before this was written, so the row is 5b-978.
**Keeps:** R-41 - with no re-ranker installed every answer is what it was, measured on the words-only and model-only backends · the rule in `heron_retrieve._fragment_passage` that a question never reads the library · [D-128](#d-128--a-request-the-host-calls-a-question-is-never-offered-a-capability-that-changes-the-model) · [D-86](#d-86--a-write-may-declare-a-question-only-when-answering-it-requires-the-write) - no write declares a question here, and every question a change answered is counted on every backend
**Affects:** [`heron_rerank.py`](../brain/heron_rerank.py) (`PASSAGE_UTTERANCES`, `PASSAGE_WORDS`, `passage_said`), [`heron_search.py`](../brain/heron_search.py) (`rerank_said`, `INDEX_FORMAT` 3), [`heron_retrieve.py`](../brain/heron_retrieve.py) (`_fragment_passage`, `_carry_said`), [`tests/test_rerank.py`](../tests/test_rerank.py) section 12

**Full record:** [`decisions/D-134.md`](decisions/D-134.md)

## D-135 — Heron's Python side keeps its data in %USERPROFILE%\.heron, and the Companion's notes in the temporary folder, where a Store Claude's copy cannot reach them

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** chosen by the session on Ajmal PS's word of 2026-10-17, recorded in [Q-63](open-questions/tier-2.md): *"OK you can decide this thing. Which one is the best best thing? ... Whatever good you can do it."* - shape 3 was chosen that evening and built here · **Source:** [Q-63](open-questions/tier-2.md), rows [5b-416, 5b-418, 5b-419, 5b-1066](fragment-issues/section-5b-rows-401-425.md)
**Answers:** Q-63's run-time half · **Amends:** [D-62](#d-62--the-brain-writes-its-own-audit-file-and-the-reader-that-already-merges-does-the-merging) - two folders, one reader · [D-109](#d-109--a-companion-button-in-revit-opens-the-page-and-a-companion-switch-turns-it-off-without-touching-heron) point 4's folder for the chat's note
**Keeps:** `HeronPaths.Data` for the add-in's own settings and trail · [D-17](#d-17--runtime-state-is-machine-local-not-roaming) · Golden Rule 14 - nothing in an old place is deleted
**Affects:** [`heron_paths.py`](../brain/heron_paths.py), [`heron_scope.py`](../brain/heron_scope.py), [`heron_gaps.py`](../brain/heron_gaps.py), [`heron_audit.py`](../brain/heron_audit.py), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py), [`heron_companion.py`](../mcp/companion/heron_companion.py), [`heron_brain.py`](../mcp/server/heron_brain.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron-ready.py`](../tools/heron-ready.py), [`HeronPaths.cs`](../platform/Heron.Core/HeronPaths.cs), [`tests/test_data_home.py`](../tests/test_data_home.py)

**Full record:** [`decisions/D-135.md`](decisions/D-135.md)

## D-136 — The Companion's folder is the user's temporary folder, found from inside Revit by stepping out of a GUID-named one

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** chosen by the session on Ajmal PS's word of 2026-10-18, in the task that asked for the fix: *"Pick one, say why in the PR, and if it changes what D-135 point 3 says, record it as the decision process in docs/DECISIONS.md requires"* · **Source:** [row 5b-1100](fragment-issues/section-5b-rows-1076-1100.md), met in [HF4](needs-checking/group-hf.md)
**Amends:** [D-135](#d-135--herons-python-side-keeps-its-data-in-userprofileheron-and-the-companions-notes-in-the-temporary-folder-where-a-store-claudes-copy-cannot-reach-them) point 3 - `HeronPaths.Companion` is no longer `<Path.GetTempPath()>\Heron\companion`, and the chat no longer names it through `heron_bridge_client.temp_folder()` alone. The rest of D-135 is unchanged
**Keeps:** D-135 point 3's folder as a person sees it, `%TEMP%\Heron\companion`, still DERIVED and safe to delete · [D-17](#d-17--runtime-state-is-machine-local-not-roaming) - runtime state stays machine-local · [D-109](#d-109--a-companion-button-in-revit-opens-the-page-and-a-companion-switch-turns-it-off-without-touching-heron) - what the note holds, who writes it and who reads it
**Affects:** [`HeronPaths.cs`](../platform/Heron.Core/HeronPaths.cs) (`SharedTemp`, `SharedDerived`, `Companion`), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) (`shared_temp_folder`), [`heron_companion.py`](../mcp/companion/heron_companion.py) (`companion_dir`), [`tests/test_data_home.py`](../tests/test_data_home.py) section 6, [`tests/Heron.Kernel.TestHost`](../tests/Heron.Kernel.TestHost/Program.cs) section 2

**Full record:** [`decisions/D-136.md`](decisions/D-136.md)

## D-138 — The Keep or Discard question gets a switch on the ribbon, and its box can keep the rest of one job without asking

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** Ajmal PS, 2026-10-18, in conversation, by asking for it: *"While I'm doing another thing I have to click OK, OK, OK, OK. That is not good. Make it two options OK I can turn off this window same like this admin on off OK I can turn off. If I turn on, it will come like in one ... That you can decide."* - and, the same day, after asking why the box exists: *"make the box off by default"* · **Source:** the owner's words above, of 2026-10-18, about [D-126](#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept)'s box
**Superseded in part by:** [D-141](#d-141--one-model-is-used-by-one-chat-at-a-time-and-what-revit-keeps-for-a-chat-is-keyed-on-that-chat), accepted by the owner 2026-10-18 - item 2's grant is held by the request's conversation, not by the bridge's client id, so "a different chat" in item 3 can now happen
**Supersedes in part:** [D-126](#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept) item 5 - the owner's switch is no longer reached only through `heron.config`, it is **off by default**, and the box itself can let the rest of one job be kept without asking · **Keeps:** D-126 items 1 to 4 and 6 to 10, [D-119](#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so) - a DRAFT's one run is always asked, [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit), Golden Rules 16 and 19, Constitution Articles 7 and 10
**Affects:** [`HeronKeepGrant.cs`](../revit/Heron.Revit.Addin/HeronKeepGrant.cs), [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs), [`Commands.cs`](../revit/Heron.Revit.Addin/Commands.cs), [`HeronSwitchWindow.cs`](../revit/Heron.Revit.Addin/HeronSwitchWindow.cs), [`HeronChangesWindow.cs`](../revit/Heron.Revit.Addin/HeronChangesWindow.cs), [`HeronPermissions.cs`](../platform/Heron.Core/HeronPermissions.cs), [`heron-ready.py`](../tools/heron-ready.py), [`time-family-build.py`](../tools/time-family-build.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`host.chat.yaml`](../brain/instructions/host.chat.yaml), [Constitution Article 9](../HERON_CONSTITUTION.md), [Golden Rule 17](14-golden-rules.md), [`tests/test_keep_question.py`](../tests/test_keep_question.py), [`tests/Heron.KeepQuestion.TestHost`](../tests/Heron.KeepQuestion.TestHost/Program.cs)

**Full record:** [`decisions/D-138.md`](decisions/D-138.md)

## D-139 — The four parts of the specification are corrected in place where a fact is wrong or an accepted decision settled it, and the original words stay beside the correction

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** Ajmal PS, 2026-10-18, in conversation, by asking for it: *"fix all the spec mistakes you can"* - after the mistakes found in the four parts were listed for him in [#668](https://github.com/Ajmalpshaik/Heron-AI/pull/668) · **Source:** the owner's words above, and the list in #668's description
**Amends:** the banner of [00](00-master-specification.md), [00b](00b-master-specification-agent-os.md), [00c](00c-master-handover-baseline.md) and [00d](00d-additional-requirements.md) - *"Do not edit this file to 'fix' it. Record changes as decisions instead."* - for the corrections this decision names, and only those
**Keeps:** [D-11](#d-11--adopt-master-specification-part-2-agent-operating-system), [D-12](#d-12--adopt-the-master-handover-baseline-part-3-and-its-fifteen-golden-rules), [D-13](#d-13--adopt-additional-requirements-part-4-kernel-workflow-engine-constitution) - the four parts stay adopted, and stay the owner's intent rather than a record of what is built · [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes) stays Proposed · [PROJECT-MAP §D](PROJECT-MAP.md#d-truth-hierarchy--which-source-wins) - DECISIONS.md still wins over the specification · every original word stays readable in the file
**Affects:** [00](00-master-specification.md), [00b](00b-master-specification-agent-os.md), [00c](00c-master-handover-baseline.md), [00d](00d-additional-requirements.md)

**Full record:** [`decisions/D-139.md`](decisions/D-139.md)

## D-141 — One model is used by one chat at a time, and what Revit keeps for a chat is keyed on that chat

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** Ajmal PS, 2026-10-18, in conversation, by designing it - his words, condensed in the task that asked for it: *"One chat can access multiple models. The same model cannot be accessed by multiple chats at the same time. After one finishes, then the next goes; then the first can come back."* He confirmed that chat 2 must not need to close, archive or cancel chat 1, and asked for a *"Free this model"* button on the Heron ribbon · **Source:** the architecture audit of 2026-10-18, which found every chat on his laptop sends the same id, `ajmal-pc`, so Revit cannot tell his chats apart · [row 5b-1150](FRAGMENT-ISSUES.md)
**Numbering:** written when `origin/main` ended at D-138, #669 named D-139 and another session was adding one; `tools/next-id.py` found D-140 free, and this took D-141 to leave it room.
**Supersedes in part:** [D-138](#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking) item 2 - the grant is held by the request's **conversation**, no longer by *"the bridge's client id, the one the lease is held by"*; item 3's *"a different chat"* can now happen · **Keeps:** [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over) - the lease, per Revit process and per person, exactly as it is, `client` and `.mcp.json`'s `HERON_CLIENT_ID` included · [D-09](#d-09--revit-thread-marshalling-externalevent) - Revit's main thread still runs one job at a time · [D-74](#d-74--a-write-is-aimed-at-the-model-it-was-told-about-not-guarded-against-the-one-in-front) - which model a request runs against · Golden Rule 19 · Constitution Articles 9 and 12a
**Superseded in part by:** [D-188](#d-188--a-chat-the-claude-app-resumes-after-a-restart-is-the-same-conversation-to-revit-and-takes-back-its-own-model), accepted 2026-11-20, not yet run in Revit - item 1's conversation is no longer always a uuid made once per process: the MCP server makes it from the Claude app's own name for the chat when the app gives one, so a chat resumed after a restart takes back its own model
**Affects:** [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) (`CONVERSATION_ID`, `RESERVED_KEYS`, `hand_back_models`), [`heron_config.py`](../mcp/client/heron_config.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`revit_release`), [`heron_tools.py`](../mcp/server/heron_tools.py), [`heron_failure.py`](../mcp/server/heron_failure.py) (`model_in_use`), [`batch-prove.py`](../tools/batch-prove.py), [`HeronConversation.cs`](../revit/Heron.Revit.Addin/HeronConversation.cs), [`HeronModelHold.cs`](../revit/Heron.Revit.Addin/HeronModelHold.cs), [`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs), [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`HeronKeepGrant.cs`](../revit/Heron.Revit.Addin/HeronKeepGrant.cs), [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs), [`Commands.cs`](../revit/Heron.Revit.Addin/Commands.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`HeronOperationRegistry.cs`](../platform/Heron.Core/HeronOperationRegistry.cs), [`HeronConfig.cs`](../platform/Heron.Core/HeronConfig.cs), [25 §3](25-multi-session-and-binding.md), [`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md), [`heron-fragment-author`](../.claude/agents/heron-fragment-author.md), [`tests/test_model_hold.py`](../tests/test_model_hold.py), [`tests/Heron.ModelHold.TestHost`](../tests/Heron.ModelHold.TestHost/Program.cs), [NEEDS-CHECKING Group HK](needs-checking/group-hk.md)

**Full record:** [`decisions/D-141.md`](decisions/D-141.md)

## D-142 — Anything a kept change deletes from the model is asked in Revit first, in a box of its own, whatever Ask to Keep says and never under a job's grant

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** Ajmal PS, 2026-10-18, in conversation, his words as the task that asked for this condensed them: *"When I type delete, it means delete - that is OK. But anything deleting from the model should come through the pop-up, in any situation, Ask to Keep on or off. It shows: this many elements are going to be deleted - can we delete or not? If I agree, it deletes. If I disagree, no."* · **Source:** the architecture audit of 2026-10-18, which found *"delete these"* deleting the live selection in one call with nothing in code asking - [row 5b-161](fragment-issues/section-5b-rows-151-175.md) had said so since 2026-09-23
**Numbering:** D-139 is held twice, by open pull requests #669 and #670, and one of them renames before it merges; another session is adding one more. This took D-142 to stay clear of all three, so D-140 and D-141 may stay unused
**Meets:** [Constitution Article 7](../HERON_CONSTITUTION.md) for deleting elements, in code - the explicit confirmation is asked in Revit, of the modeller, for that specific change · **Amends:** [D-138](#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking) item 5 - what is still asked every time, grant or not, gains any change Revit counts a deletion in · **Keeps:** [D-126](#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept) and D-138 otherwise, [D-119](#d-119--a-draft-fragment-may-run-once-on-the-model-in-front-behind-a-keep-or-discard-prompt-in-revit-and-article-11-says-so) - a DRAFT's one run is still asked Keep or Discard, [D-117](#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands), [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) - `write.enabled` stays off by default, Golden Rules 16 and 19
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), [`RevitFragmentDelete.cs`](../revit/Heron.Revit.Addin/RevitFragmentDelete.cs), [`HeronDeleteQuestion.cs`](../revit/Heron.Revit.Addin/HeronDeleteQuestion.cs), [`RevitChangedSet.cs`](../revit/Heron.Revit.Addin/RevitChangedSet.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [`HeronRunClock.cs`](../revit/Heron.Revit.Addin/HeronRunClock.cs), [`heron_config.py`](../mcp/client/heron_config.py), [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [`heron_run_records.py`](../mcp/server/heron_run_records.py), [`heron_failure.py`](../mcp/server/heron_failure.py), [Constitution Enforcement](../HERON_CONSTITUTION.md), [`tests/test_delete_box.py`](../tests/test_delete_box.py), [`tests/test_keep_question.py`](../tests/test_keep_question.py), [`tests/Heron.KeepQuestion.TestHost`](../tests/Heron.KeepQuestion.TestHost/Program.cs)

**Full record:** [`decisions/D-142.md`](decisions/D-142.md)

## D-143 — A credential in a tracked file fails the pull request

**Status:** Accepted · **Date:** 2026-10-18 · **Accepted:** Ajmal PS, 2026-10-18, in conversation, asked *"When Heron's new secret scanner finds a password or API key in a change, what should happen?"* and choosing *"Block it"* over *"Warn only"*
**Keeps:** [D-07](#d-07--free-open-source-on-public-github) - the repository stays public, which is the reason for this · [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) - the idea was studied in another project and re-authored here, nothing imported
**Affects:** [`check-secrets.py`](../tools/check-secrets.py), [`test_check_secrets.py`](../tests/test_check_secrets.py), [`heron_secrets.py`](../brain/heron_secrets.py) (`PATTERNS` gains `private key`, and `FAKE_PRIVATE_KEY`), [`test_secrets.py`](../tests/test_secrets.py), [`test_configuration.py`](../tests/test_configuration.py), [`gates.yml`](../.github/workflows/gates.yml), [`test_tag.py`](../tests/test_tag.py), [`AGENTS.md`](../AGENTS.md), [`CONTRIBUTING.md`](../CONTRIBUTING.md), [`heron-ship`](../.claude/skills/heron-ship/SKILL.md), [`tools/README.md`](../tools/README.md), [`check-gaps.py`](../tools/check-gaps.py), [12 §5a](12-security-and-permissions.md), [17](17-open-source-and-distribution.md)

**Full record:** [`decisions/D-143.md`](decisions/D-143.md)

## D-144 — A superseded decision says so under its own heading, where its citations land

**Status:** Accepted · **Date:** 2026-10-19 · **Accepted:** Ajmal PS, 2026-10-19, in conversation: *"accept D-144, mark ready and merge"* · **Source:** the third file-level reading of an external project, which the owner asked for on 2026-10-18 so that its useful ideas could be taken into Heron ([33 §5.1](33-external-repository-research.md#51-affaan-mecc))
**Keeps:** the log's own rule, in its preamble - append-only, a reversal is a new entry and the original stays · [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) - the idea was studied in another project and re-authored here, nothing imported
**Affects:** [`check-supersessions.py`](../tools/check-supersessions.py), [`test_check_supersessions.py`](../tests/test_check_supersessions.py), [`check-docs.py`](../tools/check-docs.py) (§13), [`tools/README.md`](../tools/README.md), and six notes in this log, under D-24, D-37, D-99, D-101 and D-105

**Full record:** [`decisions/D-144.md`](decisions/D-144.md)

## D-145 — A card may let its caller answer one named Revit dialog, for one job, and every other dialog is still declined

**Status:** Proposed · **Date:** 2026-10-19 · the owner has not read this back; it is what the 2026-10-19 task asked for, written down so it can be accepted or overturned
**Keeps:** Article 7 of the [Constitution](../HERON_CONSTITUTION.md) - Heron still answers no dialog for the owner; the caller does, in words they typed
**Affects:** [`HeronDialogNote.cs`](../revit/Heron.Revit.Addin/HeronDialogNote.cs) (`HeronDialogRule.CallerAnswer`), [`HeronDialogWatch.cs`](../revit/Heron.Revit.Addin/HeronDialogWatch.cs) (`AnswerForJob`), [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) (a need's `dialog:`), [`set-parameter-values-by-id`](../brain/fragments/set-parameter-values-by-id/fragment.yaml) version 5, [`test_dialog_note.py`](../tests/test_dialog_note.py), [row 5b-1221](fragment-issues/section-5b-rows-1201-1225.md), [Group HP](needs-checking/group-hp.md)

**Full record:** [`decisions/D-145.md`](decisions/D-145.md)

## D-146 — A fragment may name Revit's appearance-asset namespace, so a material's render appearance can be edited

**Status:** Proposed · **Date:** 2026-10-20 · the owner has not read this back; it is what the 2026-10-20 task asked for ("in the material also ... all the parts should be covered"), written down so it can be accepted or overturned
**Keeps:** [Article 21](../HERON_CONSTITUTION.md) - every fragment already in the library compiles with the namespace added, on every release it claims, 2020 to 2027 (measured 2026-10-20)
**Affects:** [`check-fragments-compile.py`](../tools/check-fragments-compile.py) (`USINGS`), [`HeronFragmentImports.cs`](../revit/Heron.Revit.Addin/HeronFragmentImports.cs), [`set-material-appearance`](../brain/fragments/set-material-appearance/fragment.yaml), [`create-starter-materials`](../brain/fragments/create-starter-materials/fragment.yaml), [`list-materials`](../brain/fragments/list-materials/fragment.yaml) version 3, [row 5b-1264](fragment-issues/section-5b-rows-1251-1275.md), [Group IA](needs-checking/group-ia.md)

**Full record:** [`decisions/D-146.md`](decisions/D-146.md)

## D-147 — The add-in counts each model's changes, so the Companion can say the model has changed since its read

**Status:** Accepted · **Date:** 2026-11-01 · **Accepted:** Ajmal PS, 2026-11-01, in conversation, answering *"Yes, add it"* to whether the add-in may watch for model changes so the Model Dashboard can say the model has changed
**Amends:** [D-09](#d-09--revit-thread-marshalling-externalevent) - a second Revit event, `DocumentChanged`, counted and nothing else
**Affects:** [`HeronLiveState.cs`](../revit/Heron.Revit.Addin/HeronLiveState.cs), [`heron_companion.py`](../mcp/companion/heron_companion.py), [40 §21.15](40-heron-companion.md)

**Full record:** [`decisions/D-147.md`](decisions/D-147.md)

## D-148 — revit_read looks up the user's words as a question, so a write is never ranked ahead of the read

**Status:** Accepted · **Date:** 2026-11-07 · **Accepted:** Ajmal PS, 2026-11-07, in conversation, answering *"yes fix item 3"* to the proposal to turn D-128's question filter on for `revit_read` · **Source:** [issue #799](https://github.com/Ajmalpshaik/Heron-AI/issues/799) item 3
**Amends:** [D-128](#d-128--a-request-the-host-calls-a-question-is-never-offered-a-capability-that-changes-the-model)'s *"Not covered"* consequence, for `revit_read` only
**Affects:** [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`_by_words`), [`tests/test_mcp_serves.py`](../tests/test_mcp_serves.py)

**Full record:** [`decisions/D-148.md`](decisions/D-148.md)

## D-149 — Every colour, font and icon Heron shows comes from the brand kit in docs/brand

**Status:** Accepted · **Date:** 2026-11-07 · **Accepted:** Ajmal PS, 2026-11-07, in conversation: *"from now on color combo all the things will be with this"*, then *"yes"* to recording it, listing the palette and adding a checker · **Source:** the designer's brand kit and status-notification design, 2026-11-07
**Affects:** [`docs/brand/README.md`](brand/README.md) (the one palette) · [`tools/check-brand.py`](../tools/check-brand.py) · [`revit-ribbon-and-windows`](../.claude/skills/revit-ribbon-and-windows/SKILL.md) · every window, icon, page, report and the installer - not the colours a fragment writes into a model

**Full record:** [`decisions/D-149.md`](decisions/D-149.md)

## D-150 — Heron's four switches move from the ribbon into a Settings window, and the ribbon keeps Heron, Settings and Companion

**Status:** Accepted · **Date:** 2026-11-07 · **Accepted:** Ajmal PS, 2026-11-07, in conversation: the switches *"inside that settings window"*, each state *"change it to button"*, hover texts *"very simple, understandable"*, then *"you can continue the work and build it"*
**Amends:** where D-106's, D-138's and D-141's buttons are drawn - not what they do
**Affects:** [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs) (`BuildRibbon`) · [`HeronSettingsWindow.cs`](../revit/Heron.Revit.Addin/HeronSettingsWindow.cs) · [`Commands.cs`](../revit/Heron.Revit.Addin/Commands.cs) (`SettingsCommand`) · the refusal sentences, which now name Settings
**Superseded in part by:** [D-166](#d-166--changes-admin-and-publish-are-turned-on-only-by-a-click-in-revit-held-in-its-memory-never-read-from-a-file), accepted and built 2026-11-16, not yet run in Revit - item 3 for the Changes, Admin and Publish rows: pressing one no longer writes `heron.config`; it sets the switch held in Revit. Ask to Keep and the Companion rows are unchanged

**Full record:** [`decisions/D-150.md`](decisions/D-150.md)

## D-151 — A job the modeller kept can be saved as a named command, and the Heron panel in Revit runs it through the same door a chat uses

**Status:** Proposed · **Date:** 2026-11-08 · **Asked:** Ajmal PS, 2026-11-08, in conversation: *"same like a companian app a docoble panel ... if i save that and if i clock it will run"*, then *"yes you can do"* - the limits (one capability per command, no PUBLISH or ADMIN) are the owner's to accept
**Keeps:** Article 8 · [D-117](decisions/D-117.md) · [D-126](decisions/D-126.md) · [D-150](decisions/D-150.md)
**Affects:** [`heron_commands.py`](../brain/heron_commands.py) · [`heron_command_run.py`](../mcp/server/heron_command_run.py) · [`HeronCommandsPane.cs`](../revit/Heron.Revit.Addin/HeronCommandsPane.cs) · [`HeronCommandStore.cs`](../revit/Heron.Revit.Addin/HeronCommandStore.cs)

**Full record:** [`decisions/D-151.md`](decisions/D-151.md)

## D-152 — A QA checklist is saved checks in the modeller's words, each one read with a pass rule, run in one go from the chat or the Companion

**Status:** Proposed · **Date:** 2026-11-08 · **Asked:** Ajmal PS, 2026-11-08, in conversation: *"QAQC Checklist. We need it ... We can add also in the companion app also."* - the limits (one read per check, a check only reads, Select only when every failing id arrived) are the owner's to accept
**Keeps:** [D-01](decisions/D-01.md) · [D-30](decisions/D-30.md) · [D-117](decisions/D-117.md) · [D-151](decisions/D-151.md)
**Affects:** [`heron_checklists.py`](../brain/heron_checklists.py) · [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`heron_checklist`) · [`heron_companion.py`](../mcp/companion/heron_companion.py) (`ChecklistPanel`)

**Full record:** [`decisions/D-152.md`](decisions/D-152.md)

## D-153 — Off Windows, the library-only server keeps its index in a folder of its own when nobody named one

**Status:** Accepted · **Date:** 2026-11-11 · **Accepted:** Ajmal PS, 2026-11-15, in conversation: *"Yes, accept it"* - his answer when it was read back to him in plain words: in a cloud chat with no folder named, Heron picks its own folder and answers; nothing changes on his laptop. Until then it read *the owner has not read this back; it is what the 2026-11-11 task asked for - Heron's library tools answering in a cloud chat nobody set up - written down so it can be accepted or overturned* · **Source:** [row 5b-1508](fragment-issues/section-5b-rows-1501-1525.md)
**Numbering:** D-153 was the id `tools/next-id.py` printed free on 2026-11-11, with `origin/main` at `d77439ad` ending at D-152 and no branch, open pull request or worktree adding one.
**Extends:** [D-132](#d-132--off-windows-the-heron-server-serves-its-library-alone-and-no-revit-tool) - it says the server serves its library off Windows, and nothing about where that server keeps its index when no folder is named
**Keeps:** [D-135](#d-135--herons-python-side-keeps-its-data-in-userprofileheron-and-the-companions-notes-in-the-temporary-folder-where-a-store-claudes-copy-cannot-reach-them) point 1 - off Windows there is still no home, and `HERON_KNOWLEDGE` and `HERON_DATA` win as they did · D-132 points 3 and 4 - on Windows nothing changes, and only when run as the server · [D-17](#d-17--runtime-state-is-machine-local-not-roaming) · docs/06 s2's product, data and derived
**Affects:** [`heron_brain.py`](../mcp/server/heron_brain.py) (`library_store_dir`, `library_store`), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (start-up), [38 §1](38-the-cloud-environment.md), [10 §1.1](10-memory-and-knowledge.md), [`tests/test_data_home.py`](../tests/test_data_home.py) section 7, [`tests/test_mcp_stdio.py`](../tests/test_mcp_stdio.py)

**Full record:** [`decisions/D-153.md`](decisions/D-153.md)

## D-154 — An agent is retired when nothing calls it and a named thing already does its job, under the owner's name, with its row and its file kept

**Status:** Accepted · **Date:** 2026-11-11 · **Asked:** Ajmal PS, 2026-11-11, in his own words: *"if an agent is not needed, retire that agent"* · **Accepted:** Ajmal PS, 2026-11-15, in conversation: *"Yes, accept it"*, when it was read back to him in plain words - and still **nothing is retired under it until he signs that agent's retirement by name** (point 2) · **Source:** [row 5b-1546](fragment-issues/section-5b-rows-1526-1550.md) · **Answers the question of:** [row 5b-83](fragment-issues/section-5b-rows-076-100.md), OPEN since 2026-09-21
**Numbering:** D-154 was the id `tools/next-id.py` printed free on 2026-11-11, with `origin/main` at `d77439ad` ending at D-152 and D-153 added by `claude/stoic-planck-folhnp` (#855).
**Keeps:** [Article 1](../HERON_CONSTITUTION.md) - deprecated, never deleted · [Golden Rule 7](14-golden-rules.md) - a machine never signs · [D-01](decisions/D-01.md) and [D-80](decisions/D-80.md) - the host's rows stay the host's · [D-76](decisions/D-76.md), [D-77](decisions/D-77.md) and [D-78](decisions/D-78.md) - a fold is still a fold, and is not a retirement
**Affects:** [`28-agent-registry.md`](28-agent-registry.md) · [`heron_retirement.py`](../brain/heron_retirement.py) · [`tools/agent-count.py`](../tools/agent-count.py), which would need a fifth state before the first retirement · every agent the table below names

**Full record:** [`decisions/D-154.md`](decisions/D-154.md)

## D-155 — A chat can ask for a change as a dry run: the run itself, rolled back, with nothing kept

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: *"do it"*
**Keeps:** [D-55](decisions/D-55.md) · [D-117](decisions/D-117.md) · [D-126](decisions/D-126.md) · [Golden Rule 19](14-golden-rules.md)
**Affects:** [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`revit_change`)

**Full record:** [`decisions/D-155.md`](decisions/D-155.md)

## D-156 — A write says, element by element, which value went from what to what

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: *"now do the before/after read-back"*
**Keeps:** [D-55](decisions/D-55.md) · [D-126](decisions/D-126.md) · [D-155](decisions/D-155.md)
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) · [`HeronReadBack.cs`](../revit/Heron.Revit.Addin/HeronReadBack.cs) · [`RevitReadBack.cs`](../revit/Heron.Revit.Addin/RevitReadBack.cs) · [`heron_run_records.py`](../mcp/server/heron_run_records.py)

**Full record:** [`decisions/D-156.md`](decisions/D-156.md)

## D-157 — Heron can undo its own last change, with Revit's own Undo, only while that change is still on top

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: *"now do the safe undo last"*
**Acts on, in part:** [proposal F29](proposals/f29.md)
**Keeps:** Article 8 · [Golden Rule 16](14-golden-rules.md) · [D-19](decisions/D-19.md) · [D-126](decisions/D-126.md)
**Affects:** [`HeronUndoGuard.cs`](../revit/Heron.Revit.Addin/HeronUndoGuard.cs) · [`RevitUndo.cs`](../revit/Heron.Revit.Addin/RevitUndo.cs) · [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`revit_undo_last`)

**Full record:** [`decisions/D-157.md`](decisions/D-157.md)

## D-158 — A QA check's failing ids travel whole, only when the check names them

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: *"5 you can do it"*
**Keeps:** [D-26](decisions/D-26.md) · [D-152](decisions/D-152.md)
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) (`WithIds`) · [`heron_checklists.py`](../brain/heron_checklists.py) (`ids_from`)

**Full record:** [`decisions/D-158.md`](decisions/D-158.md)

## D-159 — A QA check carries its severity, clause and advice, and a run says what changed since the last one on the same model

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: item 4 of the plan, *"you can do it"*
**Keeps:** [D-152](decisions/D-152.md) · [D-158](decisions/D-158.md) · [D-135](decisions/D-135.md)
**Affects:** [`heron_checklists.py`](../brain/heron_checklists.py) (`history`) · [`companion.js`](../mcp/companion/static/companion.js) (`qaResult`)

**Full record:** [`decisions/D-159.md`](decisions/D-159.md)

## D-160 — A keep that changed nothing is rolled back, so no empty entry is left in the undo list

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: item 8 of the plan
**Keeps:** [D-126](decisions/D-126.md) · [D-157](decisions/D-157.md)
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) (`Run`, `WithVerdict`)

**Full record:** [`decisions/D-160.md`](decisions/D-160.md)

## D-161 — A change whose answer was lost can still be read, by the request's own name

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: item 9 of the plan
**Keeps:** [D-129](decisions/D-129.md) · [D-46](decisions/D-46.md)
**Affects:** [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs) (`RunRecord`) · [`heron_run_records.py`](../mcp/server/heron_run_records.py) (`record_for`)

**Full record:** [`decisions/D-161.md`](decisions/D-161.md)

## D-162 — The pipe to Revit refuses another computer, the chat checks who serves it, and the token's file is this user's alone

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: item 11 of the plan
**Keeps:** [D-132](decisions/D-132.md)
**Affects:** [`BridgeServer.cs`](../revit/Heron.Bridge/BridgeServer.cs) · [`BridgeIdentity.cs`](../revit/Heron.Bridge/BridgeIdentity.cs) · [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py)

**Full record:** [`decisions/D-162.md`](decisions/D-162.md)

## D-163 — Heron's recent changes are docked in Revit, with Select, Zoom and Undo, reached through the one dispatcher

**Status:** Accepted · **Date:** 2026-11-12 · **Accepted:** Ajmal PS, 2026-11-12, in conversation: item 10 of the plan
**Superseded in part by:** [D-176](#d-176--the-activity-panes-select-and-zoom-look-past-a-chats-model-hold-because-they-only-look) - item 5's *"it looks at the model hold"*, for Select and Zoom: they look past a chat's hold, because they only look (2026-11-16)
**Keeps:** [D-09](decisions/D-09.md) · [D-150](decisions/D-150.md) · [D-141](decisions/D-141.md) · [D-157](decisions/D-157.md)
**Affects:** [`HeronActivityPane.cs`](../revit/Heron.Revit.Addin/HeronActivityPane.cs) · [`RevitShowElements.cs`](../revit/Heron.Revit.Addin/RevitShowElements.cs)

**Full record:** [`decisions/D-163.md`](decisions/D-163.md)

## D-164 — Two built-in parameters of one name that read the same value are one reading, not a name matched twice

**Status:** Accepted · **Date:** 2026-11-14 · **Accepted:** Ajmal PS, 2026-11-14, in conversation: *"Yes, allow it"* - his answer to *"May two built-in parameters with one name and the same value count as one reading?"*, asked after CI failed the first repair of row 5b-1631
**Supersedes in part:** [D-54](#d-54--the-callers-half-arrives-as-text-and-revit-is-what-turns-it-into-a-view) item 3 - *"a name that matches twice is refused, never chosen from"* - only for a parameter name every one of whose carriers is a Revit built-in and all of which read the same · **Keeps:** D-54 item 3 for every other name matched twice · [D-52](#d-52--a-count-of-what-was-turned-down-is-not-a-count-of-what-was-found) · [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs)
**Affects:** [`compare-elements`](../brain/fragments/compare-elements/impl/any/fragment.cs) (version 3, `builtInTwinsAgree`) · [`test_same_name_guard.py`](../tests/test_same_name_guard.py) (check 7) · [FRAGMENT-ISSUES row 5b-1631](fragment-issues/row-5b-1631.md)

**Full record:** [`decisions/D-164.md`](decisions/D-164.md)

## D-165 — A QA check never passes on missing data: a fourth answer, NO DATA, and a coverage line beside every pass count

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"accept D-165 as written"*
**Keeps:** [D-152](decisions/D-152.md) · [D-159](decisions/D-159.md) · [D-01](decisions/D-01.md) · [D-149](decisions/D-149.md)
**Affects:** [`heron_checklists.py`](../brain/heron_checklists.py) (`judge`, `run_coverage`) · [`companion.js`](../mcp/companion/static/companion.js) (`qaResult`)

**Full record:** [`decisions/D-165.md`](decisions/D-165.md)

## D-166 — Changes, Admin and Publish are turned on only by a click in Revit, held in its memory, never read from a file

**Status:** Accepted · built 2026-11-16, all three layers, not yet run in Revit · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"accept F45, both layers and reset at Revit start"* - his answer to F45's two questions, read as all of F45: the task that asked for it named two layers, the heron-guard hook and the OS-level one, and the reset at Revit start is the third, the one between them · **Source:** [F45](proposals/f45.md)
**Supersedes in part:** [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) - that Changes is *"set in the user's config"*: it is held in Revit instead, and off at every start · [D-106](#d-106--admin-and-publish-each-get-a-switch-of-their-own-and-only-the-owner-turns-them-on) items 1, 2 and 5 - that Admin and Publish are each *"a setting in `heron.config`"*, read from that file · [D-150](#d-150--herons-four-switches-move-from-the-ribbon-into-a-settings-window-and-the-ribbon-keeps-heron-settings-and-companion) item 3 - *"the same setting in `heron.config`"*, for those three rows · **Keeps:** [D-126](#d-126--revit-asks-the-modeller-keep-or-discard-before-a-change-from-a-chat-is-kept) and [D-138](#d-138--the-keep-or-discard-question-gets-a-switch-on-the-ribbon-and-its-box-can-keep-the-rest-of-one-job-without-asking) - Ask to Keep stays in `heron.config` · [D-117](#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands) · [D-162](#d-162--the-pipe-to-revit-refuses-another-computer-the-chat-checks-who-serves-it-and-the-tokens-file-is-this-users-alone) · [Golden Rule 19](14-golden-rules.md) · [Constitution Article 7](../HERON_CONSTITUTION.md)
**Affects:** [`HeronConfig.cs`](../platform/Heron.Core/HeronConfig.cs) · [`HeronPermissions.cs`](../platform/Heron.Core/HeronPermissions.cs) · [`Commands.cs`](../revit/Heron.Revit.Addin/Commands.cs) · [`HeronSettingsWindow.cs`](../revit/Heron.Revit.Addin/HeronSettingsWindow.cs) · [`HeronApplication.cs`](../revit/Heron.Revit.Addin/HeronApplication.cs) · [`heron_config.py`](../mcp/client/heron_config.py) · [`heron-ready.py`](../tools/heron-ready.py) · `.claude/skills/heron-guard/bin/heron_guard.py` and [its skill](../.claude/skills/heron-guard/SKILL.md) · [`.claude/settings.json`](../.claude/settings.json)

**Full record:** [`decisions/D-166.md`](decisions/D-166.md)

## D-167 — A modeller installs Heron with one Setup file, which puts Heron in place and hands over to the installer engine

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"do it the best am not sure for this and you anre my sesior programer or manager or what eve"* - his answer to *"Wrap the installer Heron already has, or grow it into the Setup?"*. **The choices below were made by the session on that word**; each is his to reverse, and none spends money
**Numbering:** this number was used twice. On the branch of pull request 871, commit `17bd217` (2026-11-16) first wrote D-167 as *"Article 13 names the Keep box's job grant as its one recorded exception"* — that decision is now [D-172](#d-172--article-13-names-the-keep-boxs-job-grant-as-its-one-recorded-exception). This D-167 is main's; the branch renumbered its own before it merged.
**Supersedes in part:** [D-96](#d-96--a-downloaded-heron-is-installed-by-the-installer-and-only-by-the-installer) - which FILE a modeller starts: the Setup, which runs the same engine D-96 named · **Keeps:** D-96 for everything else · [D-91](#d-91--product-files-come-from-a-signed-versioned-github-release) · [D-93](#d-93--the-product-list-is-a-manifest-read-as-data-never-a-list-written-into-the-installer) · [D-94](#d-94--install-replaces-there-is-no-separate-upgrade-path) · [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)
**Leaves open:** [Q-77](open-questions/tier-2.md) - what makes signing due, and the certificate
**Affects:** [`platform/Heron.Setup/`](../platform/Heron.Setup/README.md) · [`tools/build-setup.py`](../tools/build-setup.py) · `heron-install --uninstall` and `--add-to-claude` · [`tools/deploy-addin.ps1`](../tools/deploy-addin.ps1) · since the amendment of 2026-11-16, [`HeronInstaller.exe`](../platform/Heron.Installer.App/Program.cs) (`--from`, the two ticks)
**Amended 2026-11-16, on the owner's *"Yes, do that"*:** the Setup has no pages of its own - it copies Heron's folder and opens the branded Heron Installer window with `--from` its payload, and items 5 and 5a's two ticks move into that window, both still on. **Amended again the same day, on his *"Yes, build it"*:** on a developer's PC - one whose chats have left `developer-checkout.json` naming their checkout - an installed Heron steps aside: the shared store follows that checkout, the runner note and Claude Code stay the checkout's, and the window warns before replacing its add-in in Revit

**Full record:** [`decisions/D-167.md`](decisions/D-167.md)

## D-168 — A change Revit kept in which nothing changed by every count says NOTHING CHANGED, and REFUSED when the capability itself said no

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-15, in conversation, two answers each put to him alone in plain words: *"Yes, say nothing changed"* (row 5b-1657 - views renamed from a text no view had, nothing changed, the answer read KEPT) and *"Yes, say REFUSED"* (row 5b-1635 - a level name already taken, the card said no and made nothing, the answer read KEPT). Items 2 and 4 are how those two answers are carried out, written when they were built on 2026-11-16. Item 3 - a partial result stays KEPT - he decided himself on 2026-11-16: *"Yes, keep KEPT"*, his answer to *"If a tool did part of the job and refused the rest, should the answer still say KEPT?"*
**Numbering:** this number was used twice. On the branch of pull request 871, commit `17bd217` (2026-11-16) first wrote D-168 as *"A release is versioned and checksummed now and says plainly it is unsigned, and is signed before Heron is given to anyone outside"* — that decision is now [D-173](#d-173--a-release-is-versioned-and-checksummed-now-and-says-plainly-it-is-unsigned-and-is-signed-before-heron-is-given-to-anyone-outside). This D-168 is main's; the branch renumbered its own before it merged.
**Extends:** [D-160](#d-160--a-keep-that-changed-nothing-is-rolled-back-so-no-empty-entry-is-left-in-the-undo-list) - a keep Revit WITNESSED counting zero is rolled back; this is the keep Revit raised no event for · **Keeps:** the outcome table of [docs/04 §8.12](04-heron-mcp.md) and [row 5b-1159](fragment-issues/section-5b-rows-1151-1175.md) - one word per ending, never more than is known · [D-52](#d-52--a-count-of-what-was-turned-down-is-not-a-count-of-what-was-found) - what was turned down is not what was found
**Affects:** [`heron_server_core.py`](../mcp/server/heron_server_core.py) (`NOTHING_CHANGED`, `OUTCOMES`, `kept_nothing`) · [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`revit_change`) · [docs/04 §8.12](04-heron-mcp.md) · [`test_outcome_line.py`](../tests/test_outcome_line.py) · rows [5b-1635](fragment-issues/row-5b-1635.md) and [5b-1657](fragment-issues/row-5b-1657.md)

**Full record:** [`decisions/D-168.md`](decisions/D-168.md)

## D-169 — The DISCLAIMER says what the chat says: Heron reports which checked values a model meets, cites its source, and certifies no compliance

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"go with you suggetions"* - his answer, in his own words, when [Q-79](open-questions/tier-2.md) was put to him with Claude's suggestion beside it: shape 1, the DISCLAIMER takes the chat's words - Heron reports whether the checked values are met, with the source document, and certifies no compliance · **Answers:** [Q-79](OPEN-QUESTIONS.md)
**Numbering:** written from the same answers as [D-172](decisions/D-172.md) (first numbered D-167), whose Numbering line says how the numbers were taken.
**Keeps:** [D-36](decisions/D-36.md) - the DISCLAIMER is load-bearing, and changes with the code rather than after it · the instruction every chat reads, [`host.chat.yaml`](../brain/instructions/host.chat.yaml), unchanged
**Affects:** [`DISCLAIMER.md`](../DISCLAIMER.md), *On standards checking*

**Full record:** [`decisions/D-169.md`](decisions/D-169.md)

## D-170 — The word sandbox leaves Article 11 and Golden Rule 18: new code runs watched, not contained

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"go with you suggetions"* - his answer, in his own words, when proposal F23 was put to him with Claude's suggestion beside it: yes, the word *sandbox* in Constitution Article 11 and Golden Rule 18 becomes *watched, not contained* - new code runs under watch, every attempt recorded · **Answers:** [F23 of Part F](proposals/part-f.md) - the F23 on D-84's word, not [`f23.md`](proposals/f23.md), which is the other F23
**Numbering:** written from the same answers as [D-172](decisions/D-172.md) (first numbered D-167), whose Numbering line says how the numbers were taken.
**Extends:** [D-84](decisions/D-84.md) - its rename, carried into the two binding places it did not name, so its consequence *"there is now nowhere in the repository that claims otherwise"* holds for the Constitution and the Golden Rules from today
**Supersedes one sentence of:** [D-119](decisions/D-119.md) - the Article 11 text it wrote, *"that prompt and that one group are its sandbox"*, which now reads *"that prompt and that one group are how it is watched, and watched is not contained"*; what the exception allows does not change
**Keeps:** what Article 11 and Golden Rule 18 require - untested code never touches a live model, and promotion to live comes after it passes · D-84's decision - keep what exists, build no subprocess · D-119's exception, [D-117](decisions/D-117.md), [D-121](decisions/D-121.md) and [D-122](decisions/D-122.md)
**Affects:** [`HERON_CONSTITUTION.md`](../HERON_CONSTITUTION.md) - Article 11 and the Amendment note · [Golden Rule 18](14-golden-rules.md) · the note under [Part 2 §35](00b-master-specification-agent-os.md) that reads Article 11's words · not Part 4 §10, point 4

**Full record:** [`decisions/D-170.md`](decisions/D-170.md)

## D-171 — Heron may save the model it works in once its Undo entry has closed, only when the modeller asks, and never picks a name itself

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"go with you suggetions"* - his answer, in his own words, when [Q-69](open-questions/tier-2.md) was put to him with Claude's suggestion beside it: shape 2 - save after the group closes, from the add-in, only when the modeller asks, and Heron never picks a name itself · **Answers:** [Q-69](OPEN-QUESTIONS.md)
**Numbering:** written from the same answers as [D-172](decisions/D-172.md) (first numbered D-167), whose Numbering line says how the numbers were taken.
**Supersedes in part:** [D-55](decisions/D-55.md) - a save asked for with a run is made on a preview too, and is not rolled back; the run's changes to the model are rolled back as before, and the reply says the save was made · **Extends:** [D-112](decisions/D-112.md) and [D-114](decisions/D-114.md) - a third thing the add-in does on a fragment's behalf, and the first it does after Heron's group has closed
**Keeps:** [Article 8](../HERON_CONSTITUTION.md) - one request, one named `TransactionGroup`, one undo entry, and the save is not in it · [Article 10](../HERON_CONSTITUTION.md) - never on Heron's own initiative · Articles 12a and 12b and [Golden Rule 20](14-golden-rules.md) - the pin follows a new title only for this run's own Save As of the pinned model · [D-106](decisions/D-106.md) - behind the Publish switch, as `SAVE_DOCUMENT` is · [D-117](decisions/D-117.md), [D-119](decisions/D-119.md), [D-121](decisions/D-121.md) · [D-126](decisions/D-126.md)
**Affects, being built in another session from 2026-11-16 and not on this branch:** `SAVE_DOCUMENT` version 3 (FRG-DOC-033) · the add-in's write path after the group closes ([`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs)) · the chat's model pin ([`heron_write.py`](../mcp/server/heron_write.py)) · the methods that ask the modeller to save first and name Q-69

**Full record:** [`decisions/D-171.md`](decisions/D-171.md)

## D-172 — Article 13 names the Keep box's job grant as its one recorded exception

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"go with you suggetions"* - his answer, in his own words, when [Q-73](open-questions/tier-2.md) was put to him with Claude's suggestion beside it: shape 1, Article 13 names the job grant - D-138's third link - as its one recorded exception · **Answers:** [Q-73](OPEN-QUESTIONS.md)
**Numbering:** D-167 was the id `tools/next-id.py` printed free on 2026-11-16, with `origin/main` at `2d4bc84c` ending at D-164 and D-165 and D-166 held by the open pull requests #881 and #882. This work was given *D-166 onwards*; D-166 was already held, so it starts here, and the four decisions written from the same answers are D-168 to D-171. Renumbered from D-167 to D-172 on 2026-11-16 with `tools/renumber.py`, because claude/dazzling-stonebraker-9281d3 holds D-167 too.
**Amends:** [Constitution Article 13](../HERON_CONSTITUTION.md) · **Settles:** the choice [D-139](decisions/D-139.md) point 4 left open, *"Constitution Article 13 beside D-138's job grant"*
**Keeps:** [D-138](decisions/D-138.md) and [D-141](decisions/D-141.md) - the grant as built, unchanged · [D-142](decisions/D-142.md) - a deletion is never kept under it · Articles 7, 9 and 10 · [Golden Rule 9](14-golden-rules.md) - gated in code, at the boundary
**Affects:** [`HERON_CONSTITUTION.md`](../HERON_CONSTITUTION.md) - Article 13 and the Amendment note · no code

**Full record:** [`decisions/D-172.md`](decisions/D-172.md)

## D-173 — A release is versioned and checksummed now and says plainly it is unsigned, and is signed before Heron is given to anyone outside

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"go with you suggetions"* - his answer, in his own words, when [Q-77](open-questions/tier-2.md) was put to him with Claude's suggestion beside it: shape 1, a decision amends D-91 - releases are versioned and checksummed now and say plainly they are unsigned, and are signed before Heron is given to anyone outside · **Answers:** [Q-77](OPEN-QUESTIONS.md)
**Superseded in part by:** [D-190](#d-190--the-readme-stops-asking-modellers-not-to-install-before-a-release-is-signed-and-says-plainly-the-build-is-unsigned-and-what-was-checked), accepted by the owner 2026-11-20 - the README's notice no longer waits for a signed release
**Numbering:** written from the same answers as [D-172](decisions/D-172.md) (first numbered D-167), whose Numbering line says how the numbers were taken. Renumbered from D-168 to D-173 on 2026-11-16 with `tools/renumber.py`, because claude/outcome-words-2026-11-16 holds D-168 too.
**Supersedes in part:** [D-91](decisions/D-91.md) - *"Files come from a signed GitHub release"*: until signing is built a release is versioned and checksummed and says it is unsigned, and signed stays the bar, due before Heron is given to anyone outside
**Keeps:** the rest of D-91 - named by version, never a branch, a raw file URL or whatever is newest on main, a download verified before it is used, nothing downloaded executed to decide what to install · [D-93](decisions/D-93.md) · [D-96](decisions/D-96.md) · [Article 15](../HERON_CONSTITUTION.md) - only the owner publishes a release · [Article 17](../HERON_CONSTITUTION.md) - when signing is built, its key lives in the credential store
**Affects:** D-91's index entry and record · [07 §1a and §3](07-installation-and-update.md) · nothing in [`release.yml`](../.github/workflows/release.yml), which already writes *"Not signed."* into every release's notes, or in [`heron_tag.py`](../brain/heron_tag.py), whose refusal of an unsigned release stays as the bar signing must meet

**Full record:** [`decisions/D-173.md`](decisions/D-173.md)

## D-174 — A proof taken on another Heron add-in build or another Revit build is said under the answer, and never refused

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"Keep it a flag, accept Q-92 shape 1"* · **Answers:** [Q-92](open-questions/tier-3.md)
**Numbering:** this number was used twice. On the branch of pull request 871, commit `86c33cf` (2026-11-16) first wrote D-174 as *"Only the chat that made a Heron change may undo it; the Activity pane's Undo is the modeller's own"* — that decision is now [D-175](#d-175--only-the-chat-that-made-a-heron-change-may-undo-it-the-activity-panes-undo-is-the-modellers-own). This D-174 is main's; the branch renumbered its own before it merged.
**Keeps:** [D-117](#d-117--a-change-is-kept-only-from-a-fragment-whose-proof-still-stands) - a change is refused on D-30's fingerprint alone, read from the card · [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) - a proof going stale is made visible · [D-131](#d-131--a-proof-on-another-revit-release-is-an-entry-of-proofreleases-and-a-second-route-on-a-proven-fragment-is-written-by-accept-signed) - another release is its own entry
**Affects:** [`heron_fragment.py`](../brain/heron_fragment.py) (`BUILT_ON_FIELDS`, `build_state`, `proof_build_state`) · [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`_build_line`) · [`heron_brain.py`](../mcp/server/heron_brain.py) (`proof_build`) · [`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md) · [NEEDS-CHECKING Group OF](needs-checking/group-of.md)

**Full record:** [`decisions/D-174.md`](decisions/D-174.md)

## D-175 — Only the chat that made a Heron change may undo it; the Activity pane's Undo is the modeller's own

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"own only"*
**Numbering:** written as D-174; renumbered D-175 on 2026-11-16 with `tools/renumber.py`, because claude/relaxed-hawking-me9yne holds D-174 too.
**Settles:** [Q-88](open-questions/tier-2.md) · what [row 5b-1741](fragment-issues/row-5b-1741.md) asked of the pane · **Refines:** [D-157](decisions/D-157.md)
**Affects:** `revit_undo_last` and the add-in's undo guard · the Activity pane's Undo button

**Full record:** [`decisions/D-175.md`](decisions/D-175.md)

## D-176 — The Activity pane's Select and Zoom look past a chat's model hold, because they only look

**Status:** Accepted · **Date:** 2026-11-16 · **Accepted:** Ajmal PS, 2026-11-16, in conversation: *"Yes, both."*
**Supersedes in part:** [D-163](decisions/D-163.md) item 5 - *"it looks at the model hold"* - for Select and Zoom only · **Settles:** [row 5b-1825](fragment-issues/row-5b-1825.md)
**Affects:** the Activity pane's Select and Zoom

**Full record:** [`decisions/D-176.md`](decisions/D-176.md)

## D-177 — An idea taken from another project is written in Heron's own words, and the project and its author are not named

**Status:** Accepted · **Date:** 2026-11-17 · **Accepted:** Ajmal PS, 2026-11-17, in conversation: *"we are taking oly thrrtr idea not code so no need to mention there names"*
**Extends:** [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) - studied and re-authored, never imported
**Affects:** [AGENTS.md](../AGENTS.md) ("Never") · [docs/51](51-studying-another-revit-mcp.md)

**Full record:** [`decisions/D-177.md`](decisions/D-177.md)

## D-178 — A QA checklist run can be written as an Excel workbook and an HTML page, each a new file

**Status:** Accepted · **Date:** 2026-11-17 · **Accepted:** Ajmal PS, 2026-11-17, in conversation: *"you can do one by one"*
**Extends:** [D-152](#d-152--a-qa-checklist-is-saved-checks-in-the-modellers-words-each-one-read-with-a-pass-rule-run-in-one-go-from-the-chat-or-the-companion) - QA checklists
**Affects:** [`heron_checklist_report.py`](../brain/heron_checklist_report.py) · [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`heron_checklist`, `_checklist_report`) · [docs/50](50-qa-checklists.md)

**Full record:** [`decisions/D-178.md`](decisions/D-178.md)

## D-179 — A project checklist can extend an office checklist, changing only severity, target and on/off, and every run names the checks it used

**Status:** Accepted · **Date:** 2026-11-17 · **Accepted:** Ajmal PS, 2026-11-17, in conversation: *"you can do one by one"*
**Extends:** [D-152](#d-152--a-qa-checklist-is-saved-checks-in-the-modellers-words-each-one-read-with-a-pass-rule-run-in-one-go-from-the-chat-or-the-companion) - QA checklists
**Affects:** [`heron_checklists.py`](../brain/heron_checklists.py) (`save`, `_read`, `prepare_run`, `stamp`, `fingerprint`) · [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (`heron_checklist`) · [docs/50](50-qa-checklists.md)

**Full record:** [`decisions/D-179.md`](decisions/D-179.md)

## D-180 — A setting hides folder paths from what the chat is told, off unless turned on

**Status:** Not accepted - dropped, nothing kept in code · **Date:** 2026-11-17 · **Answered:** Ajmal PS, 2026-11-17, in conversation: *"3 no issue this is only location like that no issue let them go its not taking revit model it self"* · [Q-40](open-questions/from-master-specification-part-2.md) stands as it was
**Keeps:** [D-26](#d-26--the-model-file-is-never-uploaded) - the model never leaves; work answers may
**Affects:** nothing - the code was removed

**Full record:** [`decisions/D-180.md`](decisions/D-180.md)

## D-181 — Checking a folder of models opens each one in the Revit the modeller connected Heron to, one at a time, and opening a model read-only is a read

**Status:** Accepted, nothing built · **Date:** 2026-11-19 · **Accepted:** Ajmal PS, 2026-11-19, in conversation, written on [#1038](https://github.com/Ajmalpshaik/Heron-AI/issues/1038): *"in one Revit because I have to connect the Heron on that Revit ... which Revit I given to AI, in that Revit"*
**Settles:** [Q-93](open-questions/tier-3.md), both of its questions
**Keeps:** [D-129](decisions/D-129.md) - a fragment that runs too long is stopped by its run clock · [D-152](decisions/D-152.md) - a QA checklist runs on a model
**Affects:** nothing yet - [research note 51](51-studying-another-revit-mcp.md) item 7, a later build job

**Full record:** [`decisions/D-181.md`](decisions/D-181.md)

## D-182 — An edited spreadsheet comes back through a comparison with the export, outside Revit, and only its changed cells are sent; a blank cell of that file is skipped before its parameter is looked for

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-20, in conversation, written on [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925): [Q-87](open-questions/tier-2.md) confirmed as his own decision. Points 4 and 5 decided by Claude on his delegation of the same day - *"You can decide this thing. Investigate on this and decide which is the best option."* - and his word overrules them at any time
**Settles:** [Q-87](open-questions/tier-2.md) (a), (b) and (c) · rows [5b-1525](fragment-issues/section-5b-rows-1501-1525.md), [5b-1526](fragment-issues/section-5b-rows-1526-1550.md) and [5b-1528](fragment-issues/section-5b-rows-1526-1550.md)
**Keeps:** [D-117](decisions/D-117.md) - a change is kept only from a fragment whose proof still stands · [D-33](decisions/D-33.md) - asked, never assumed
**Affects:** [`heron_sheet_compare.py`](../brain/heron_sheet_compare.py) (unchanged) · [`SET_PARAMETER_VALUES_BY_ID`](../brain/fragments/set-parameter-values-by-id/fragment.yaml) version 10 · [`parameters-csv-round-trip`](../brain/skills/parameters-csv-round-trip.yaml) · [Group NJ](needs-checking/group-nj.md) NJ5, NJ14

**Full record:** [`decisions/D-182.md`](decisions/D-182.md)

## D-183 — Sheets made from a spreadsheet keep four choices as built: a blank cell writes nothing, a date goes in as ISO, a refused value is listed and its sheet kept, and 1,000 rows is the most

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-20, in conversation, written on [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925): *keep all four as built*
**Settles:** the four owner's choices of row [5b-1486](fragment-issues/section-5b-rows-1476-1500.md)
**Affects:** [`CREATE_SHEETS_FROM_TABLE`](../brain/fragments/create-sheets-from-table/fragment.yaml) (unchanged) · [`heron_sheet_list.py`](../brain/heron_sheet_list.py) (unchanged) · whatever card carries this job later

**Full record:** [`decisions/D-183.md`](decisions/D-183.md)

## D-184 — Before Heron acts on a parameter by its name, it checks where that name lives in the model, and asks the modeller which one when more than one place carries it

**Status:** Accepted · **Date:** 2026-11-19 · **Accepted:** Ajmal PS, 2026-11-19, in conversation, written on [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916): *"there is chances in a project parameter or shared parameter or project information itself ... if any doubt regarding this, it should be asked again ... which parameter should be changed ... it should be checked before acting."*
**Settles:** row [5b-1632](fragment-issues/row-5b-1632.md)
**Extends:** [D-33](decisions/D-33.md) - Heron never assumes an input; it asks, once
**Affects:** [Group NK](needs-checking/group-nk.md) NK8 (c) - no code

**Full record:** [`decisions/D-184.md`](decisions/D-184.md)

## D-185 — Reload Latest and Sync with Central stay the modeller's own clicks, and Heron builds no route for either

**Status:** Accepted · **Date:** 2026-11-19 · **Accepted:** Ajmal PS, 2026-11-19, in conversation, written on [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911): *by hand*
**Settles:** row [5b-1423](fragment-issues/section-5b-rows-1401-1425.md)
**Keeps:** Article 10 of the [Constitution](../HERON_CONSTITUTION.md) - never synchronise on Heron's own initiative · [D-125](decisions/D-125.md) - a fragment says what transaction it needs
**Affects:** nothing in code - `SYNC_WITH_CENTRAL` stays as found

**Full record:** [`decisions/D-185.md`](decisions/D-185.md)

## D-186 — What Heron should know out of the box runs BIM and ISO standards first, regional next and company last - and no standard, and no knowledge store, ever ships with Heron

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-09-11, in conversation - recorded as R-88 of [the RAG requirement note](work-notes/plans/rag/01-requirements.md), and written here as a rule on [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) (WN-19)
**Numbering:** D-186 was first written, three minutes before this decision, on the branch `claude/clever-ritchie-0sy1nr` as "A chat the Claude app resumes after a restart is the same conversation to Revit, and takes back its own model". That branch renumbered its decision D-188 with `tools/renumber.py` before either branch merged, so D-186 is this decision.
**Settles:** R-87, R-88 and R-88a of [the RAG requirement note](work-notes/plans/rag/01-requirements.md)
**Keeps:** [D-07](decisions/D-07.md) - Heron is public and open source · [D-26](decisions/D-26.md) - the model file never leaves · [17 §2](17-open-source-and-distribution.md) - code is public, knowledge is not
**Affects:** [`tools/build-release-assets.py`](../tools/build-release-assets.py) - the download refuses a store and a source document by name · [`tests/test_knowledge_rules.py`](../tests/test_knowledge_rules.py) - nothing shaped like a store is tracked

**Full record:** [`decisions/D-186.md`](decisions/D-186.md)

## D-187 — Every new finding is a GitHub issue at once: the register entry and its issue are written in the same change, linked both ways

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-20, in conversation with the coordinating session, which gave his words in summary: *from now on, every new finding must become a GitHub issue at once ... make it automatic, with an agent for issue creation*
**Extends:** the 2026-11-16 move of every open row to GitHub issues ([`fragment-issues/ISSUES.md`](fragment-issues/ISSUES.md)) - from then on, for every new entry, the day it is written
**Keeps:** [`FRAGMENT-ISSUES`](FRAGMENT-ISSUES.md), [`NEEDS-CHECKING`](NEEDS-CHECKING.md) and [`OPEN-QUESTIONS`](OPEN-QUESTIONS.md) as the permanent record · [D-30](decisions/D-30.md) - a proof is a recorded run
**Affects:** [`tools/open-issue.py`](../tools/open-issue.py) (new) · [`heron-issue-recorder`](../.claude/agents/heron-issue-recorder.md) (new agent) · the rules for adding to the three registers · `AGENTS.md`, `heron-ship`, `fragment-proving`, `skill-recorder`, `heron-fragment-author`, `heron-fragment-merger`

**Numbering:** D-187 is the id `tools/next-id.py` printed free on 2026-11-20, with `origin/main` at `f9c188f0` ending at D-185 and D-186 held by open pull request #1096 and by `claude/clever-ritchie-0sy1nr`. It was first written here as D-186 and renamed with `tools/renumber.py` before this branch was pushed.

**Full record:** [`decisions/D-187.md`](decisions/D-187.md)

## D-188 — A chat the Claude app resumes after a restart is the same conversation to Revit, and takes back its own model

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-19, in the issue he filed, [#1033](https://github.com/Ajmalpshaik/Heron-AI/issues/1033): *"a chat that resumes the same conversation (same session transcript) takes back its own lock"*
**Numbering:** written as D-186; renumbered D-188 on 2026-11-20 with `tools/renumber.py`, because claude/keen-cannon-np3ack holds D-186 too.
**Supersedes in part:** [D-141](decisions/D-141.md) item 1 - the MCP server's conversation is no longer always *"a uuid the bridge client makes once per process"*: when the host names the chat, it is made from that name · **Keeps:** D-141 items 2 to 6 - one model, one chat at a time, three quiet minutes, Free this model, a command line a conversation of its own · [D-22](decisions/D-22.md) - the lease, per person
**Affects:** [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) (`host_conversation`, `adopt_host_conversation`), [`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) (its `__main__` block), [`tests/test_model_hold.py`](../tests/test_model_hold.py) section 2b, [NEEDS-CHECKING Group OV](needs-checking/group-ov.md) OV1

**Full record:** [`decisions/D-188.md`](decisions/D-188.md)

## D-189 — A development session never pulls into the main Heron folder

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-20, in conversation, on pull request [#1107](https://github.com/Ajmalpshaik/Heron-AI/pull/1107): *"no to B1"*
**Settles:** item B1 of the field report [#1058](https://github.com/Ajmalpshaik/Heron-AI/issues/1058)
**Keeps:** [Group EU](needs-checking/group-eu.md)'s guard - a chat whose Heron code changed under it stops and asks to be reopened
**Affects:** [`AGENTS.md`](../AGENTS.md) "How to work" - no code

**Full record:** [`decisions/D-189.md`](decisions/D-189.md)

## D-190 — The README stops asking modellers not to install before a release is signed, and says plainly the build is unsigned and what was checked

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-20, in conversation with the coordinating session: *"We can remove that one, am I right? Because it is all. Most it is ready so modeler can install"*, and then, asked whether to remove it although D-173 waits for a signed release, his choice *"Yes, remove it now"*
**Supersedes in part:** [D-173](#d-173--a-release-is-versioned-and-checksummed-now-and-says-plainly-it-is-unsigned-and-is-signed-before-heron-is-given-to-anyone-outside) point 3 - *"the README's please do not install this yet taken out"* no longer waits for a signed release
**Keeps:** the rest of D-173 - a release is versioned and checksummed, says plainly that it is unsigned, and signed stays the bar (its points 1, 2 and 4) · [D-167](decisions/D-167.md) - the one-file Setup · [D-19](decisions/D-19.md) - Heron reads only until Changes is on
**Affects:** [`README.md`](../README.md) - the notice becomes a short note and an Install section · [`docs/README.md`](README.md) · [`docs/guide/using-heron.md`](guide/using-heron.md) · [`WHAT-TO-INSTALL.md`](WHAT-TO-INSTALL.md) · D-173's index entry and record

**Full record:** [`decisions/D-190.md`](decisions/D-190.md)

## D-191 — The usage counter keeps offered, helped and the last date it helped; "helped" is only the modeller's word; it moves a clause less than one rank; and no question text is kept

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-20, in conversation, answering question 1 on [#1096](https://github.com/Ajmalpshaik/Heron-AI/pull/1096): *"yes to all four on the usage counter, build it"*
**Numbering:** written as D-190; renumbered D-191 on 2026-11-20 with `tools/renumber.py`, because `main` had given D-190 to another decision first.
**Settles:** the four parts [D-70](decisions/D-70.md) left open · R-16 and R-25 of [the RAG requirement note](work-notes/plans/rag/01-requirements.md) · WN-12 of [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006)
**Extends:** [D-70](decisions/D-70.md) - Heron keeps a usage counter, on the machine, as numbers
**Affects:** [`brain/heron_usage.py`](../brain/heron_usage.py) · [`brain/heron_retrieve.py`](../brain/heron_retrieve.py) `USAGE_SPAN` · the `heron_helped` tool · [`tests/test_usage_counter.py`](../tests/test_usage_counter.py)

**Full record:** [`decisions/D-191.md`](decisions/D-191.md)

## D-192 — A job that works only outside the model it is aimed at declares noGroupOpen, and an Admin or Publish job then opens no transaction group in that model

**Status:** Accepted · **Date:** 2026-11-20 · **Accepted:** Ajmal PS, 2026-11-21, in conversation with the session that built it: *"yes to D-192, merge #1150"* · **Written by:** a cloud session overnight, for issues [#1121](https://github.com/Ajmalpshaik/Heron-AI/issues/1121) and [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941), held unmerged as Proposed until he had read it. Taking the need off the two cards still puts every run back as it was, with no C# change.
**Numbering:** D-192 is the id `tools/next-id.py` printed free on 2026-11-20, with `origin/main` at `dea262c6` ending at D-190 and D-191 held by pull request #1135.
**Answers:** [D-125](decisions/D-125.md)'s question for a job that only makes a new document (row 5b-1912) · **Keeps:** D-125, D-112, Article 8, D-30, D-155 · **Leaves open:** row 99 and row 5b-1408, the owner's
**Affects:** `RevitFragment.cs`, `HeronDialogNote.cs`, `heron_fragment.py`, `heron_failure.py`, `heron_bridge_client.py`, `check-revit-gate.py`, `create-family-document` (version 4), `export-families` (version 2)

**Full record:** [`decisions/D-192.md`](decisions/D-192.md)

## D-193 — Making sheets keeps three rules for a table and typed numbers alike: a sheet whose number or name Revit refuses is taken back, a number asked for twice makes neither, and 1,000 is the most in one call

**Status:** Accepted · **Date:** 2026-11-21 · **Accepted:** Ajmal PS, 2026-11-21, on issue [#1080](https://github.com/Ajmalpshaik/Heron-AI/issues/1080): *"merge: yes"*, and for the three rules *"remaining all the things you can choose what is the best"* - the session chose them on that word and wrote them on #1080 the same day
**Numbering:** D-193 is the id `tools/next-id.py` printed free on 2026-11-21, with `origin/main` at `148fd459` ending at D-192.
**Settles:** the three rules [row 5b-1875](fragment-issues/row-5b-1875.md) put to the owner before the merge · [row 5b-1893](fragment-issues/row-5b-1893.md) - a sheet name Revit refuses was kept in silence
**Keeps:** [D-183](decisions/D-183.md) - its four choices, unchanged, now on `CREATE_SHEETS` · [D-33](decisions/D-33.md) - which of two is meant is asked, never guessed · [D-30](decisions/D-30.md) - DRAFT until a recorded run · Article 8 - one call, one undo
**Leaves open:** the copy job of `CREATE_SHEETS` keeps version 3's rule - [row 5b-1935](fragment-issues/row-5b-1935.md), the owner's
**Affects:** [`CREATE_SHEETS`](../brain/fragments/create-sheets/fragment.yaml) version 4 (DRAFT) · [`CREATE_SHEETS_FROM_TABLE`](../brain/fragments/create-sheets-from-table/fragment.yaml) (DEPRECATED, merged in) · [`sheets-from-excel`](../brain/skills/sheets-from-excel.yaml) · [`heron_sheet_list.py`](../brain/heron_sheet_list.py) · [Group OZ](needs-checking/group-oz.md)

**Full record:** [`decisions/D-193.md`](decisions/D-193.md)

## D-194 — Every stored vector is compared in plain Python; sqlite-vec is not used

**Status:** Accepted · **Date:** 2026-11-21 · **Accepted:** Ajmal PS, 2026-11-21, in conversation with the session working issue [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957): *"5b-1171: write the decision."*
**Numbering:** D-194 is the id `tools/next-id.py` printed free on 2026-11-21, with `origin/main` at `1e5f37e9` ending at D-193.
**Supersedes in part:** [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) - its words *"`sqlite-vec` for meaning"*
**Settles:** [row 5b-1171](fragment-issues/section-5b-rows-1151-1175.md) · **Keeps:** the rest of D-23, Golden Rule 5, PROPOSALS F7
**Affects:** [`brain/heron_embed.py`](../brain/heron_embed.py) · `requirements-optional.txt`, `brain/heron_dependencies.py`, `tools/cloud-setup.sh`, `tools/heron-ready.py` - sqlite-vec taken out · D-23's index entry and record

**Full record:** [`decisions/D-194.md`](decisions/D-194.md)

## D-195 — The Loads panel's Finalize goes to Revit as one change - the Space values and the diffusers' flows as steps of one request, in one undo entry - with every step gated as if sent alone

**Status:** Proposed · **Date:** 2026-11-21 · **Written by:** a cloud session working issue [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934), for the owner to accept, change or reject. Nothing in it is built.
**Numbering:** D-195 is the id `tools/next-id.py` printed free on 2026-11-21, with `origin/main` at `1f648dd6` ending at D-193 and D-194 held by pull request #1189.
**Answers:** [row 5b-314](fragment-issues/section-5b-rows-301-325.md) - the Loads panel's Finalize leaves two undo entries, and Article 8 asks for one · **Keeps:** Article 8 - one request, one named `TransactionGroup` · [D-126](decisions/D-126.md) - one Keep or Discard · [D-142](decisions/D-142.md) - the delete box · [D-156](decisions/D-156.md) - the read-back · Article 12c - a stale row refuses the whole write
**Affects (if accepted):** `heron_mcp_server.py` `_loads_finalize` and `revit_change` · `RevitFragment.cs` (setup steps on a kept change) · `tests/test_loads_finalize.py` · [Group CC](needs-checking/group-cc.md) CC11

**Full record:** [`decisions/D-195.md`](decisions/D-195.md)
