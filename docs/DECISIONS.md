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
> This mirrors Golden Rule 4: never destroy a working record.

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
| [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) | The knowledge store is SQLite, one file per scope | ✅ Accepted · ✔ read back 2026-08-29 |
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
| [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back) | A fragment's preview is the run itself, rolled back | ✅ Accepted · 2026-09-08 |
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
| [D-73](#d-73--a-table-by-name-is-two-separators-and-the-key-is-the-models-word-not-ours) | A table by name is two separators, and the key is the model's word, not ours | 🕐 Proposed - **owner has not read this back** |
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
| [D-91](#d-91--product-files-come-from-a-signed-versioned-github-release) | Product files come from a signed, versioned GitHub release | ✅ Accepted · 2026-09-21 |
| [D-92](#d-92--a-ribbon-button-is-a-front-door-onto-a-proven-fragment-not-new-logic) | A ribbon button is a front door onto a proven fragment, not new logic | ✅ Accepted · 2026-09-21 |
| [D-93](#d-93--the-product-list-is-a-manifest-read-as-data-never-a-list-written-into-the-installer) | The product list is a manifest read as data, never a list written into the installer | ✅ Accepted · 2026-09-21 |
| [D-94](#d-94--install-replaces-there-is-no-separate-upgrade-path) | Install replaces; there is no separate upgrade path | ✅ Accepted · 2026-09-21 |
| [D-95](#d-95--installing-and-showing-are-two-different-things) | Installing and showing are two different things | ✅ Accepted · 2026-09-21 |
| [D-96](#d-96--a-downloaded-heron-is-installed-by-the-installer-and-only-by-the-installer) | A downloaded Heron is installed by the installer, and only by the installer | ✅ Accepted · 2026-09-21 |
| [D-97](#d-97--the-library-is-built-out-first-and-proved-in-one-pass-later) | The library is built out first, and proved in one pass later | ✅ Accepted · 2026-08-30 |
| [D-98](#d-98--a-context-fragment-is-consumed-by-the-host-not-by-another-fragment) | A context fragment is consumed by the host, not by another fragment | ⏳ Proposed · 2026-08-31 |
| [D-99](#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so) | A change asked for in a chat is kept at once, with no preview, and Article 9 says so | ✅ Accepted · 2026-09-23 |

## Format

Write a new decision here, in full, in this shape - then run `python tools/split-decisions.py --write`,
which moves its record into `decisions/D-NN.md` and leaves its heading, its metadata lines and a link here.
Decisions already split are left alone. **A number is used once**: D-45 and D-46 were each used twice before
2026-09-23, and the first decisions under them are now D-97 and D-98.

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

**Full record:** [`decisions/D-23.md`](decisions/D-23.md)

## D-24 — Embeddings are computed locally by default

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-11](OPEN-QUESTIONS.md)
**Amended the same day by [D-26](#d-26--the-model-file-is-never-uploaded):** the cloud opt-in below is

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

**Full record:** [`decisions/D-47.md`](decisions/D-47.md)

## D-48 — One broken part costs one part, never the whole library

**Status:** Accepted · **Date:** 2026-09-06 · **Extends:** [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases)
**Affects:** [`brain/heron_fragment.py`](../brain/heron_fragment.py), [09](09-skills-and-fragments.md), [21](21-resilience-and-operations.md)

**Full record:** [`decisions/D-48.md`](decisions/D-48.md)

## D-49 — A heavy optional import never happens on a request thread

**Status:** Accepted · **Date:** 2026-09-06 · **Found during:** `A8`, on the owner's PC
**Affects:** [`brain/heron_embed.py`](../brain/heron_embed.py), [`mcp/server/heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [05](05-heron-brain.md), [21](21-resilience-and-operations.md)

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

**Full record:** [`decisions/D-54.md`](decisions/D-54.md)

## D-55 — A fragment's preview is the run itself, rolled back

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner asking why he had to click something a fragment already does
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `Run(app, request, writing)`, [`HeronOperationRegistry`](../platform/Heron.Core/HeronOperationRegistry.cs), [D-19](DECISIONS.md), [Golden Rule 16](14-golden-rules.md), [Golden Rule 19](14-golden-rules.md)

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

**Full record:** [`decisions/D-99.md`](decisions/D-99.md)
