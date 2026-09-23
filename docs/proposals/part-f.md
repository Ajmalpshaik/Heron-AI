# Proposals — Part F

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part F — Opened by building the Improvement Gate, 2026-09-12

Three of these are questions about Heron's own tooling rather than about the specification. They are
here rather than in [OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md) because none of them blocks anything, and
[work-notes/README](../work-notes/README.md) puts a reviewed suggestion here.

### 🟡 F1. Should the runtime capability snapshot become an MCP tool?

[`mcp/server/heron_runtime.py`](../../mcp/server/heron_runtime.py) turns the five different reasons
`heron_capability.resolve()` returns `None` into six verdicts that mean six different things —
`NO_PROVIDER`, `UNSUPPORTED_RELEASE`, `NOT_DETECTED`, `BLOCKED_BY_TRUST`, `NEEDS_REVIT`, `AVAILABLE`.
A planner given `None` can only say *"I cannot"*; the sentences a modeller needs are *"Heron has no
fragment for that"*, *"not on Revit 2020"*, *"press the Heron button"* and *"`write.enabled` is false"*,
which are not the same answer at all.

**Nothing is wired to it, and that was deliberate.** Offering it changes the risk table in
[`heron_tools.py`](../../mcp/server/heron_tools.py), which [12 §71](../12-security-and-permissions.md) says
is where risk is declared, and the add-in holds the matching half. That is a decision with a human in
it, not a wiring job. `check-reachable.py` reports the function with this reason attached.

**If it is taken**, it is `READ` with no bridge operation — it reads Heron's own library and facts the
caller already has, and sends nothing to Revit — which puts it beside `heron_capabilities` rather than
anywhere near the write path.

### 🔵 F2. `ALL_VERSIONS` is declared twice

[`check-compile.py`](../../tools/check-compile.py) and [`check-api-surface.py`](../../tools/check-api-surface.py)
each own a copy of the supported release list. [`check-package.py`](../../tools/check-package.py) imports
the first rather than making it three, but two is already one too many for the fact that decides which
Revit releases Heron claims to support. Merging them is a change to two working gates and deserves its
own review.

### ✅ F3. The decision log's status summary stops at D-50 — CLOSED 2026-09-12, and it was twenty decisions behind

[DECISIONS.md](../DECISIONS.md)'s summary table ends at `D-50` while the log itself runs past `D-69`.
Nothing enforces the table, so it drifted quietly. Completing it needs a careful one-line title per
decision — worth doing in one pass by somebody reading them, not as a side effect of another change.

**CLOSED 2026-09-12 by generating it.** This row said the summary *"stops at D-50"*. It did — and nobody had measured the gap: the file had reached **D-70**, so **twenty decisions were missing from the index of decisions**, `D-70` among them, answered by the owner the day before.

`tools/generate-decision-summary.py` rebuilds the table from the decisions themselves and `check-docs.py` §8 fails if it drifts. **Existing status cells are kept verbatim** — *"read back 2026-09-06"* records a conversation, not a fact on disk, and regenerating it away would have destroyed the record of every read-back the owner has done.

**The gate is in `check-docs.py`, not `gates.yml`.** The `gh` token here carries `repo` but not `workflow`, so no session can push a change to that workflow file — a gate nobody can install is not a gate. Worth revisiting whenever that scope is granted.

### 🔵 F4. Two documents are numbered 34, and one of them is a plan

[`34-patterns-adapted.md`](../34-patterns-adapted.md) and
[`34-project-perfection-and-continuous-upgrade-plan.md`](../34-project-perfection-and-continuous-upgrade-plan.md)
share a number, and `check-docs.py` does not object because nothing derives the numbering. Two things
are tangled here and they should be untangled together rather than one at a time:

- **The collision.** Renumbering touches every inbound link, including from [35](../35-independent-study-notes-open-design-awesome-llm-apps-openhands.md).
- **The placement.** The second is a *plan*, and by [work-notes/README](../work-notes/README.md)'s own
  test — *will this be deleted once its work is done?* — a plan belongs in `work-notes/`. Its work **is**
  done, and it now carries a banner saying so. Whether it is deleted, moved or kept for its reasoning is
  the owner's call: it is the fullest written statement of how Heron studies an outside project, and
  that part has outlived the plan around it.

### 🟡 F5. Two suites exit 1 when a dependency is missing, and one of them is the only thing `check-gaps` calls unfinished

[`tests/README.md`](../../tests/README.md) sets the rule: **0 is a pass, 1 is a failure, 3 means the suite
could not run** and proves nothing either way. `test_mcp_serves.py` obeys it — no MCP SDK, exit 3, and
[`check-gaps.py`](../../tools/check-gaps.py) files it under *waiting*.

`test_served_claims.py` needs the same SDK and exits **1**, so `check-gaps` files it under
**UNFINISHED**, and since its exit code follows that list alone, **the whole gate exits 1 on any
machine without the MCP SDK**. That is the entire unfinished list on a plain container. The same is
true of `test_bridge_roundtrip.py` and its .NET test host, which `check-gaps` sidesteps by skipping the
suite outright — a second answer to one question.

`tests/README.md` already records the inconsistency as an observation — *"So an exit code alone does
not tell you whether a failure is yours"* — rather than as something to fix. It is worth deciding which
it is, because the cost is that the repository's clearest "is anything actually unfinished" signal reads
red for a reason that is not work.

**Not fixed here.** Changing a suite's exit code changes what CI, `check-gaps` and the ship checklist
all read, and that deserves its own change rather than being a side effect of one about something else.

### 🟡 F6. `check-dependencies.py` is documented nowhere

`tools/check-dependencies.py` and `tests/test_dependencies.py` arrived on `main` in #124, with
`requirements.txt` and `requirements-optional.txt`. The tool is good and it closes a real gap — the
only install list used to be one row of a table that said `pyyaml` while the code imported six things.

**~~But [`tools/README.md`](../../tools/README.md) has no section for it~~ — CLOSED 2026-09-12: it has one now, and this row was checked rather than assumed still true when a second tool was added to the same file.** That file's whole structure
is one section per tool. A tool nobody can find is a tool nobody runs, which is the same failure the
tool itself was written to fix one level down.

The [`heron-ship`](../../.claude/skills/heron-ship/SKILL.md) skill now names it — added here, because this
change rewrote that checklist and leaving a fifth checker out of a list claiming to be complete makes
the checklist wrong. **The `tools/README.md` section is left to whoever wrote the tool**: describing
somebody else's checker from the outside is how a README comes to say something almost true.

It is also **not in `.github/workflows/gates.yml`**. That may be deliberate — it reports on the
*machine*, not on the change, and CI's machine is not anybody's — but it is worth deciding rather than
leaving unstated.

### 🟡 F7. `sqlite_vec` is the one optional package that degrades with NOTHING SAID

**Carried in from `docs/work-notes/plans/rag/03-working-note.md` as `W-10` when that note was retired,
2026-09-12.** It was the only record of it.

The rule this repository keeps is **degrade silently but SAY SO**, and three of four places keep it:
`heron_embed` says it for the encoder, `heron_rerank` for the re-ranker, `heron_ingest` for the PDF
reader. **`_try_vec_extension` in [`brain/heron_embed.py`](../../brain/heron_embed.py) says it for
nothing** — it returns `False` on `ImportError` and the caller falls back to comparing vectors in
Python. No `backend()` line, no report line, no error carries it.

**So a person cannot tell the fast path from the slow one**, and the failure mode is the bad one: Heron
gets slower and stays that way, and nobody knows there is anything to install. That is the same shape
as `W-5`, where the install list said `pyyaml` and the code imported six things — a user ends up on the
weaker path **permanently**, because nothing ever told them there was a better one.

**Found 2026-09-11 while closing R-73, by checking the other fallbacks rather than assuming the one in
front of me was the only gap.** Recorded rather than fixed then because it was a second module in a
batch that had no business growing; recorded rather than fixed **now** because it is not what retiring
a note is for. It is a small, self-contained batch of its own.

### 🔵 F8. F5's closing claim is now false, and the reason is worth more than the fix

[F5](#f5-two-suites-exit-1-when-a-dependency-is-missing-and-one-of-them-is-the-only-thing-check-gaps-calls-unfinished)
says `test_served_claims.py` *"is the entire unfinished list on a plain container."* On the owner's
**Windows** checkout on 2026-09-12, `check-gaps.py` named **three** unfinished suites and
`test_served_claims` was **not among them** — `test_ingest`, `test_reachable` and
`test_document_retrieval` were.

**F5 is not wrong about Linux.** It is wrong about *"a plain container"* being the only machine anybody
runs this on, which is the same assumption [A14](../NEEDS-CHECKING.md) exists to test and the same one
that made the suite total Linux-specific for weeks. Two of the three are recorded on [`A14`](../NEEDS-CHECKING.md) with their causes. **The third,
`test_document_retrieval`, turned out not to be an operating-system difference at all** — it fails on
the trained backend and passes on the fallback, on one machine, and its record is the 2026-09-12
section of [`brain/retrieval-history.md`](../../brain/retrieval-history.md). **This row exists so F5 is
not read as current**, and so that the first attribution is not read as the final one: *three suites
fail on Windows* was the obvious reading and it was wrong about a third of itself.

### 🟠 F9. Every fragment claims the top of the trust ladder, and nothing reads it

**Found 2026-09-14 while building [`HERON-INS-SUP-013`](../../brain/heron_supply.py), the Supply Chain
Security Agent** — by checking what the field it was about actually does today, rather than assuming the
agent was the first thing that would need it.

Every one of the fragment manifests in [`brain/fragments/`](../../brain/fragments) carries a top-level
`source: OFFICIAL` — the **highest** level of [docs/00d §38](../00d-additional-requirements.md)'s trust
ladder, `UNKNOWN → EXPERIMENTAL → TESTED → VERIFIED → PROVEN → OFFICIAL`. Derive it rather than reading
this sentence:

```bash
grep -l '^source: OFFICIAL' brain/fragments/*/fragment.yaml | wc -l   # claiming the top
ls -d brain/fragments/*/ | wc -l                                      # fragments in total
grep -n 'EXPERIMENTAL\|VERIFIED\|OFFICIAL' brain/heron_fragment.py   # what reads it
```

The third command prints nothing. **`heron_fragment.py` requires the field and never validates its
value.** The `SOURCES` tuple it does check — `("fragment", "ambient", "request")` — belongs to
`contract.needs[].source`, a different field with the same name one level down. The one trust word the
loader does contain, `PROVEN`, is there as a [docs/24](../24-trust-model.md) lifecycle *status*, which
is a different ladder that happens to share a rung.

**So a fragment's trust level is a word the file writes about itself that nothing checks** — which is
precisely the shape [Golden Rule 19](../14-golden-rules.md) and [D-35](../DECISIONS.md) refuse. It costs
nothing today, because all 360 were written here and `OFFICIAL` is true of every one of them. It stops
costing nothing the first time a fragment arrives from somewhere else, and on that day the field will
already have looked trustworthy for months.

**[D-35](../DECISIONS.md) already called this out and it was not read as a to-do:** *"One thing must be
built now, long before community packages exist: the fragment format carries an approval record from the
first version. Retrofitting identity and provenance into a format already in use is the kind of change
that touches every file — cheap today, expensive later."* There are 360 files. The decision's own
argument is that the number only goes up.

**Not fixed here, deliberately.** What the field should mean is the owner's: whether `source` stays a
trust level and gains a validator, whether it is joined by an approval record as D-35 asks, and what a
fragment written in this repository is entitled to claim about itself are three decisions, not a patch.
`HERON-INS-SUP-013` is built to take the answer — it compares a package's claim against a register from
outside it and refuses `SELF_DECLARED_TRUST` — and needs that register to exist.

### 🟡 F10. Two files claim one agent id, and nothing has ever reported it

**Found 2026-09-15 by `HERON-WSP-REG-012` on its first run against the real repository** — not by a
fixture, and not by reading. Both of these carry `Heron-Agent: HERON-FRG-VAL-001`:

```bash
grep -l 'HERON-FRG-VAL-001' brain/*.py
head -3 brain/heron_fragment.py brain/heron_validate.py
```

| file | step | what it is |
|---|---|---|
| `brain/heron_fragment.py` | 7 | *"What a fragment IS on disk, and the validator that will not let it lie"* |
| `brain/heron_validate.py` | 17 | *"The Fragment Validation Agent. It gathers evidence for a proof. It never signs one."* |

**Nothing in the repository can see this.** [`tools/check-metadata.py`](../../tools/check-metadata.py)
checks that each claimed id EXISTS in the register, then collects them with `claimed.add(aid)` into a
**set** — so two files claiming one id collapse to one entry and the count comes out right.
`tools/agent-count.py` counts the agent as built either way. Both gates pass, and have all along.

**Multi-file agents are legitimate here** — several agents span `brain/` and `mcp/`, or a module and
its C# half, and the register row for `HERON-FRG-VAL-001` is broad enough to cover both halves:
*"Logic, API, versions, dependencies, metadata, duplication, reusability"*. So this is **not
automatically a defect**, which is exactly why it needs a person rather than a patch.

**The question is which of three things it is**, and only the owner can say:

1. **One agent, two files, correctly** — the format half and the evidence half. Then nothing changes
   except that the gate should stop being blind to the pattern.
2. **Two agents wearing one id** — the schema validator is arguably `HERON-FRG-FMT-*` work and not
   validation at all. Then one of them needs its own registry row.
3. **A rename that never finished** — the usual cause, and the one the gate's own comment warns about
   two lines further down.

**Not fixed here, deliberately.** Splitting an agent or renaming one is an ADMIN act needing a
signature — `HERON-AHR-RET-010` refuses it without one — and picking a winner would make the other
file invisible, which is the precise thing `HERON-WSP-REG-012` refuses to do.

**Worth adding whichever way it goes:** a check that reports one id claimed by more than one file.
Today that costs nothing to add and surfaces a real ambiguity; it stops costing nothing the first time
a rename half-lands and two files disagree about what they are.

#### CORRECTION, the same day: that check was written, measured, and NOT kept

The line above was the obvious next move and it is wrong. Before adding it, the repository was measured:

```bash
python - <<'EOF'
# every id claimed by more than one file OUTSIDE tests/
EOF
```

**Fourteen ids are claimed by more than one non-test file, and most of them are correct.**

| a sample | why it is fine |
|---|---|
| `HERON-REVIT-CMP-021` — three C# files | one feature spread over the files that make it |
| `HERON-SES-DIS-001` — the Python client and `BridgeIdentity.cs` | the two halves of discovery, in two languages |
| `HERON-WSP-PTH-007` — `heron_paths.py` and `HeronPaths.cs` | *where* things live and *which class* they are |
| `HERON-RAG-RNK-006`, `CTX-007`, `LIB-001` — each also on `heron_retrieve.py` | that module is **the orchestrator**: its own docstring says *"the whole lookup, in the order docs/05 §4 sets out"*, so it is the place those steps happen in order |

So a shared id is **the norm here, not a defect**, and the proposed check would have reported fourteen
things of which most need no action — noise, not a guard. It was not added.

**What that leaves.** The original observation stands for `FRG-VAL-001` specifically: two files, both
validators, and nothing says which is the agent. What does not stand is the general rule. **Nothing in
a header can distinguish a deliberate split from a stale one** — the shape is identical — so the
question for the owner is not *"why are there duplicates"* but *"should a header say which file is the
agent and which files merely implement part of it"*. That is one field, or a convention, and it is a
different decision from the one this row first asked for.

### 🟠 F11. docs/06 §2 draws nineteen folders and classifies fourteen

**Found 2026-09-15 by `HERON-WSP-CRE-002`**, which reads the tree out of that section rather than
carrying a copy — so the first thing it did was ask each folder what class it was in.

[docs/06 §2](../06-heron-platform.md) draws the workspace as nineteen folders, then immediately puts
folders into **Product / Data / Derived**. The table covers fourteen. These five are drawn and
classified by nothing:

```
RAG   Community   Configuration   Tests   Documentation
```

Derive it:

```bash
python brain/heron_folders.py            # the five are listed under "NO CLASS"
```

**The class is not a label — it is the only thing that answers three questions**, and every one of
them is now asked by an agent in this repository:

| question | who asks | what the wrong answer does |
|---|---|---|
| may a product update replace it wholesale? | `HERON-OPS-UPD-010` rule 6 | a practice's work is gone |
| may a cleanup delete it outright? | `HERON-WSP-CLN-009` | same, more quietly |
| does a backup cover it? | `HERON-WSP-BAK-010` | it is not there when needed |

**`Configuration` is the one to settle first.** [docs/21 §9](../21-resilience-and-operations.md) makes
configuration a **security boundary** — it holds the security policy, the update policy and the
company standards, and `HERON-INS-CFG-006` already splits it into machine-specific and portable
halves. If the folder reads as **product**, an update replaces a practice's security policy with the
shipped defaults, and nothing in the specification currently says it must not.

The other four have plausible answers that are still nobody's decision on record:

- **`RAG`** — [docs/07 §8](../07-installation-and-update.md) says *"the vector index is the easy case — it is derived"*, which points at **derived**. But the folder may hold more than the index.
- **`Community`** — [docs/00d §37](../00d-additional-requirements.md) says imported community components are untrusted by default. `Packages` is product; is `Community` product too, or data because the user installed it?
- **`Tests`**, **`Documentation`** — most likely **product**, and cheap to say so.

**Not fixed here.** Adding a row to the class table changes what four agents do to a folder, and
`HERON-WSP-PTH-007` is deliberately not extrapolated — the same reason D-05 refuses to guess a Revit
release. Until it is answered, `heron_paths.classify()` returns `UNKNOWN` for all five and `may()`
reads UNKNOWN as **data**, so nothing removes or overwrites them. That is the safe failure, not a fix:
it also means a backup does not cover them and a cleanup leaves rubbish behind.

---

### 🟠 F12. "Template" means two opposite things, and one of them is never allowed to leave

**Found by:** building `HERON-WSP-TPL-006`, the Template Agent, 2026-09-15.
**Status:** open. The agent takes the narrow reading and says so in its own answer.

[docs/12 §97](../12-security-and-permissions.md) is unusually firm, and the wording is the owner's own:

> The rule Ajmal actually set draws the line at **the file, not the information**: a `.rvt`, an
> `.rfa`, a family or project template is **never** uploaded.

[docs/00 §1115](../00-master-specification.md) lists, as possible marketplace packages:

> Skills, Fragments, Agent packs, BIM standards, Revit tools, **Project templates**, Company extensions.

and [docs/00d §325](../00d-additional-requirements.md) lists **company templates** the same way. A
marketplace package is, by definition, a file that leaves.

**So the specification says a project template is shippable and that a project template is never
uploaded.** Both lines are correct if the word carries two senses, and nothing written says it does:

| the word | what it is | how big | may it leave? |
|---|---|---|---|
| a Heron workspace/project template | a list of folder names a new job starts from | hundreds of bytes | the marketplace lines say yes |
| a Revit project or family template | `.rte`, `.rft` — the office's own starting file | hundreds of megabytes | docs/12 §97 says never |

In a Revit practice the second is what the word means. Anyone reading "project templates" on a
marketplace page reads it as the `.rte`.

**What the agent does until this is answered.** `HERON-WSP-TPL-006` takes the narrow reading, and
enforces it structurally rather than by remembering it: **a template names, it never carries.** A
template entry is a folder name and a reason — never a file body, never a path to one. Two separate
refusals, because they are two different mistakes:

| | |
|---|---|
| `A_REVIT_FILE_IS_NAMED` | any of `.rvt` `.rfa` `.rte` `.rft`, **by name alone**, with nothing attached. A template that names the office `.rte` is one read away from carrying it |
| `TEMPLATE_CARRIES_A_FILE` | an entry with `bytes`, `body`, `content`, `data`, `source`, `from`, `file`, `path` or `url` |

and the module itself uses no `open(`, no `shutil`, no `.read()` and no `urlopen` — so there is
nothing in a template that *could* be uploaded. The suite asserts all of that against the code.

```bash
python tests/test_templates.py           # 3. A template cannot carry
```

**Not fixed here.** Deciding it is one line in docs/00 — either *"project templates are name lists,
not `.rte` files"* or *"the marketplace may carry an `.rte` under these conditions"*. The second is a
change to the rule the owner set, so it is not one an agent may assume.

---

### 🟡 F13. A project folder and a workspace folder with the same name get different classes

**Found by:** the same build. **Status:** open, and narrower than F11.

`HERON-WSP-PTH-007`'s `classify()` tokenises a path and answers on the first **data** word it finds
anywhere in it. That is right for what it was built for and it has this consequence:

```
classify("Cache")                    ->  derived
classify("Projects/Tower A/Cache")   ->  data
```

The same leaf name, two classes, decided by what sits above it. A cleanup that clears **derived**
removes one and spares the other; a tool holding only the leaf name gets the opposite answer from one
holding the whole path — and `HERON-OPS-UPD-010` asks with a component **name**, not a path.

**What the agent does.** `HERON-WSP-TPL-006` refuses `RESERVED_WORKSPACE_NAME` for any of the
seventeen words the class table is made of, so a template cannot create a project folder whose fate
depends on who is asking. The reserved list **is** `heron_paths.FOLDERS`, read from it, not a second
copy.

**Not fixed here.** The fix is either a path-aware `classify()` that only matches at the top level,
or a documented statement that the leaf-name answer is the intended one. Both change what four
agents do to a folder, so it is the owner's call — the same reason F11 is still open.

---

### 🔴 F14. Three different projects write to one knowledge file, and the code says why that is a breach

**Found by:** building `HERON-WSP-PLC-005`, the File Placement Agent, 2026-09-15.
**Status:** open. **Measured, not read** — the numbers below come from running the code.

`brain/heron_scope.py`'s `scope_path()` turns a project key into a filename through `_safe_key()`,
which replaces every character outside `[A-Za-z0-9._-]` with a hyphen:

```bash
cd brain && HERON_KNOWLEDGE=/tmp/hk python -c "
import heron_scope as S
for k in ['Tower B', 'Tower/B', 'Tower-B']:
    print('%-10r -> %s' % (k, S.scope_path('project', k)))"
```

```
'Tower B'  -> /tmp/hk/projects/Tower-B.db
'Tower/B'  -> /tmp/hk/projects/Tower-B.db
'Tower-B'  -> /tmp/hk/projects/Tower-B.db
```

**Three projects, one file.** Two lines above the function that does it, `scope_path()`'s own
docstring says what that costs:

> Heron does not guess which project this is: guessing wrong writes one client's knowledge into
> another's file, which is a **contractual breach rather than a bug** (docs/10 §2).

It refuses to guess when the key is **missing** and then quietly collapses two keys that are
**present and different**. The second is the same harm as the first, arrived at more quietly.

**What keeps it from biting today, and what does not.** The key is *meant* to be the document's
Project Information `UniqueId` — hex and hyphens, which never collides. Nothing enforces that.
`scope_path()` accepts any string, and three shipped command lines document handing it a typed name:

| | |
|---|---|
| `brain/heron_conflict.py:12` | `--scopes company,project --project "Tower B"` — the agent's own usage line |
| `brain/heron_ingest.py:1593` | `project = _flag(argv, "--project")` |
| `brain/heron_research.py:741` | `project = _flag(argv, "--project")` |

So the safe case is the intended one and the unsafe case is the documented one.

**Not fixed here**, and the reason is not caution. Any fix moves where existing knowledge lives:

- **reject a key that is not a UniqueId** — correct, and it breaks the three command lines above and
  every store already named after a typed name;
- **hash the key instead of reducing it** — removes the collision and makes every existing
  `projects/*.db` unreachable, so it needs a migration under [docs/07 §8](../07-installation-and-update.md)
  (idempotent, versioned, reversible-or-backed-up) — which `HERON-WSP-MIG-008` can now plan;
- **keep the reduction and record the original key inside the store** — smallest change, detects a
  collision after it has happened rather than preventing it.

Picking among those is the owner's call, and it is the same shape as D-05: do not extrapolate.

**What the new agent does about it.** `HERON-WSP-PLC-005` takes the opposite rule and states it:
**a name that would have to be rewritten to be usable is refused, never cleaned up.** A project
called `Tower/B` is a refusal (`NAME_IS_NOT_A_PLACE`); `Tower B` is placed at `Projects/Tower B/…`
*as given*, space and all. The suite proves the rule by running `heron_scope` and watching the three
names arrive at one file, so the finding cannot quietly stop being true:

```bash
python tests/test_placement.py     # 3. A name is refused, never cleaned up - and here is the cost
```

---

### 🟡 F15. The Naming Agent's own name has no stated shape, and two documents disagree about its parts

**Found by:** building `HERON-NAM-VAL-002`, the Naming Validation Agent, 2026-09-15.
**Status:** open. The validator refuses that one kind of name rather than guessing it.

The department's rule is stated twice, as a goal:

> **"Naming must be predictable and searchable."** — [docs/06 §134](../06-heron-platform.md), [docs/00 §517](../00-master-specification.md)

That is what naming is *for*. It is not a convention: it gives no separator, no case, no order and no
allowed character set. The only two statements of what a generated name is **made of** disagree:

| source | the parts |
|---|---|
| [docs/28](../28-agent-registry.md), `HERON-NAM-GEN-001` | domain, capability, purpose, platform, **version** — five |
| [docs/00c §368](../00c-master-handover-baseline.md) | domain · capability · purpose · platform · version · **component type** — six |

So `HERON-NAM-GEN-001` is asked to generate a name whose shape nobody has written down, and
`HERON-NAM-VAL-002` is asked to check it against a convention that does not exist.

**Everything else in the system is fine**, and that is what makes this narrow rather than alarming.
Four kinds of name *are* stated, and the validator reads each from the file that owns it:

| kind | where the rule lives |
|---|---|
| agent id | `docs/28`'s own rows, through `heron_fragment.registry_agents()` |
| fragment id | `heron_fragment.ID_PATTERN` and `.AREAS` |
| capability | `heron_fragment.CAPABILITY_PATTERN` |
| the fragment folder | [docs/29 §130](../29-metadata-standard.md) — the capability, lower case, hyphens, **"derived, never invented"** |

Two more are **observed and stated nowhere**: every module in `brain/` is `heron_<name>.py` and every
suite in `tests/` is `test_<name>.py`, with no exceptions and nothing enforcing it. The validator
reports those as `UNLIKE_EVERY_OTHER` rather than `WRONG_SHAPE`, and puts the count in the answer,
because *"unlike all 63 of its neighbours"* and *"against a written rule"* are different claims.

**What it does about the seventh.** `generated-name` is refused as `UNSTATED_CONVENTION` — never
guessed. A guess here would silently **become** the convention, because this validator would be the
only thing enforcing one, and a convention arrived at that way is the hardest kind to change later.

**Not fixed here.** Settling it is one line saying which list of parts is right and what the name looks
like — a separator, a case, an order. That is a decision about what the product's filenames read like,
which is the owner's.

### SETTLED 2026-09-16 — [D-79](../DECISIONS.md): six parts, version last

    <domain>-<capability>-<purpose>-<platform>-<component>-v<n>
    mep-duct-insulation-check-revit-fitting-v1

**docs/00c won.** It is the owner's own handover document and its sixth part is real, so the register's
row was corrected rather than the baseline. The version moved to the END, which 00c's listing does not
do — recorded in D-79 as a change to the order rather than folded in quietly.

The rules live in [docs/29](../29-metadata-standard.md), which already owned every other name shape here.
`HERON-NAM-GEN-001` is built in `brain/heron_naming.py` beside the validator, and generates through the
validator's own rule so the department cannot produce a name its own checker rejects. **Naming &
Taxonomy is 7 of 7.**

**The half that is still impossible is declared rather than hidden.** A part may be hyphenated, so a
finished name cannot be split back into six parts — `check()` says it checked the SHAPE, in the answer,
and never claims to have checked the parts.

---

### 🟠 F16. The register asks for a synonym table and D-34 forbids one

**Found by:** building `HERON-NAM-KEY-005`, the Keyword Agent, 2026-09-15.
**Status:** open, but **the agent is built** — D-34's own consequences settle it, and the resolution is
worth confirming rather than assuming.

[docs/28](../28-agent-registry.md) gives the Keyword Agent:

> Search terms and **synonyms** — "duct", "ductwork", "supply air"

[D-34](../DECISIONS.md) says:

> **Heron builds nothing to understand language.** No phrase list, **no synonym table**, no parser for
> dictated near-misses. That belongs to the host and duplicating it there would be worse than the host's
> version and would need maintaining forever.

Read the register row on its own and it describes exactly the thing the decision refuses.

**D-34 answers it three lines further down**, in its own consequences:

> A site word that maps to a Revit word is a different problem and is not solved by translation. When
> somebody says something the model calls by another name, **that is knowledge** — it belongs in Heron's
> own knowledge store where it can be **looked up and corrected**, not in a language setting.

So the agent holds **knowledge, not language**, and the difference is four rules rather than a
distinction of wording. Each is a refusal in the built agent:

| | |
|---|---|
| it ships **no list** | the table starts empty and stays empty until somebody fills it. The suite proves this by *behaviour* — with nothing handed in, every word comes back unknown — not by searching the source for vocabulary |
| every entry names a **person and a date** | "looked up and corrected" needs somebody to correct and a date to correct from |
| an **inference is not a record** | an entry whose `by` reads derived, guessed, inferred, automatic, auto, model, suggested or expanded is refused, and a word recorded *only* that way stays **unknown** — the refusal is not a warning beside a usable answer |
| an **unknown term is a question** | [D-33](../DECISIONS.md): Heron never assumes an input — it asks, and it asks once. Nothing is expanded quietly, and there is no partial answer beside the question |

**What is still open:** the register row's wording. It reads as the forbidden thing and points at no
decision, so the next person to build from that row alone will build a synonym table. One clause in
`docs/28` — *"recorded by a person, never inferred — see D-34"* — closes it.

---

### 🟡 F17. Three agents own metadata validity, in two departments

**Found by:** the same build, while checking whether `HERON-NAM-MET-006` already existed.
**Status:** open. **`MET-006` was deliberately not built** — see below.

| agent | department | what docs/28 gives it |
|---|---|---|
| `HERON-STD-MET-014` | Standards & BIM QA | "Enforces the Heron metadata standard on everything Heron creates… also audits the registry against the code" — **built**, `tools/check-metadata.py` |
| `HERON-FRG-VAL-001` | Fragment Lifecycle | "Logic, API, versions, dependencies, **metadata**, duplication, reusability" — **built**, and claimed by two files (that is F10) |
| `HERON-NAM-MET-006` | Naming & Taxonomy | "Metadata completeness and schema validity" — **not built** |

The third row's job is a plain subset of the first two. `MET-014` already checks that every artefact
declares its agent, step, status, version and layer; `FRG-VAL-001` already checks a fragment's card
against its schema. A third agent would be a third place for the same rule, and every other finding in
this file is about what happens when one rule lives in two places.

**Not built, and that is the point.** Building it would have cost nothing and been wrong — the same
mistake as writing a new agent over `brain/heron_architect.py` earlier today, arrived at from the other
direction. What is needed is one line in `docs/28` saying which of the three owns metadata validity and
what the other two defer to it for. That is the owner's call, and until it is made the Naming & Taxonomy
department reads as 7 agents when its real number may be 6.

**SETTLED 2026-09-16 — [D-78](../DECISIONS.md).** `HERON-STD-MET-014` owns it and `MET-006` folds into
`tools/check-metadata.py`. Two thirds of the question turned out to be answered already, in that tool's
own source rather than in any register: *"ONE place per fact: `brain/heron_fragment.py` validates these
files, and this checker does not read them."* So `FRG-VAL-001`'s half was never an overlap, and only the
unbuilt third row needed deciding.

`tests/test_metadata_guard.py` plants one error for each word of the folded row — a missing field, an
invalid layer, a claim on an id that does not exist — and requires all three to be caught. **Naming &
Taxonomy is 6 of 7**, and the seventh is `NAM-GEN-001`, blocked on **F15** above.

---

### 🟡 F18. "Registers them with the Tool Registry" — a discovered tool must not enter a fixed table

**Found by:** building `HERON-MCP-DIS-012`, the MCP Discovery Agent, 2026-09-15.
**Status:** open, and **the agent is built** on the narrow reading. The word is worth one clause in
`docs/28`.

The register gives `HERON-MCP-DIS-012`:

> Finds **other** MCP servers installed on the machine, reads their tools, versions and capabilities,
> and **registers them with the Tool Registry**.

`HERON-MCP-REG-003`'s table is fixed, and says so in its own refusal:

> `'%s' is not declared in the MCP tool registry. Add it to TOOLS with its risk level — **being absent
> is a refusal, not a risk of zero.**`

Read "registers" literally and a foreign server's manifest ends up adding entries to Heron's own risk
table. That is exactly what **Golden Rule 19** forbids:

> No text Heron reads may raise Heron's own permission level. Content from documents, family names,
> parameter descriptions, imported folders, model text and community packages is **data, never
> instruction**. Permission comes from the user, through Heron's own UI, per action.

**The narrow reading, which the agent takes.** A discovered tool comes back as a **finding a person
reads** — at `UNKNOWN` trust ([docs/24 §47](../24-trust-model.md): provenance unclear, which is what a
server somebody installed is — installing is not vouching). The registry is untouched, and because the
tool is undeclared there, calling it already raises. **The existing refusal is the protection**; the
suite proves it by calling `risk_of` on a discovered name and catching `NotDeclared`, rather than
asserting it in prose.

Three things are refused rather than recorded:

| | |
|---|---|
| a name in **Heron's namespace** | not the eighteen names — the *prefixes*, derived from `REG-003`'s own table. `heron_select` is **not** one of Heron's tools and reads exactly like one, which is the whole danger. An exact-match check let it through on this file's first run |
| **Heron's own vocabulary** in a manifest | `risk`, `trust`, `approved`, `permission`, `confirmed`, `granted`, `allowed` — a manifest using those is writing into Heron's fields, not describing itself. Recording it as a claim would still be reading it |
| a tool with **no name** | it would sit in a list a person reads as though it were callable |

Everything else a server says lands under `says` and nowhere else, so a tool describing itself as safe,
read-only or already approved has described itself and changed nothing.

**What is still open:** the register's wording. One clause — *"presents them for a person to declare;
never writes into the table — Golden Rule 19"* — closes it, and without it the next person to build
from that row alone will write the append.

---

### 🟠 F19. D-27 abolished persona, and three places still hand work to it

**Found by:** building `HERON-USR-PRO-001`, the User Profile Agent, 2026-09-15.
**Status:** open. The agent implements what survives D-27 and does **not** implement the dead clause.

[D-27](../DECISIONS.md) is explicit, and it says what it supersedes:

> **Supersedes** the *"infer a default, display it, let the user pin it"* recommendation in
> [01 §4](../01-vision-and-principles.md) and **the two-persona table in [22 §2](../22-users-modes-and-extensibility.md)**.
>
> **Heron has one voice: plain, non-developer language, always.** Persona is not inferred, not displayed
> and not pinned — **it does not exist as a setting.**

[docs/22 §2](../22-users-modes-and-extensibility.md)'s own `[DECIDED 2026-08-28 — D-27]` block puts it more
bluntly, and it is worth quoting separately because it lives in the other file:

> **There is no persona.** The warning above was right and it argues further than it went: if silent
> switching reads as unreliability, the fix is not to display the guess — it is not to guess. Heron has
> **one voice**, and what varies is the **shape of the answer**, read off the **shape of the request**.

Three places still describe the thing it removed:

| where | what it still says |
|---|---|
| `docs/28`, `HERON-USR-PRO-001` | *"**Persona** reads this to choose how to speak"* |
| `docs/28`, `HERON-ORC-PER-003` | *"Communication / Persona Agent — detects role and technical level; **chooses wording**"* |
| [`docs/22 §3`](../22-users-modes-and-extensibility.md) NOTE | *"**Persona** may be inferred — it only changes wording. **Mode** must be granted."* |
| `docs/28`, `HERON-RPT-CMP-001` | *"Decides what goes in and at what depth **for this reader** — a modeller wants the 47 failures, a BIM manager wants the trend. **Judgement, so a model call**"* |

The third is the one that matters, because it is doing real work in a sentence about security. Its point
is sound and its example is gone: it contrasts a *grantable* mode with an *inferable* persona in order to
say why conflating them **"would let a user talk their way into `ADMIN`"**. With persona abolished there
is nothing inferable left to contrast with — which makes the rule **stronger**, not weaker, and leaves the
sentence explaining it broken.

**What the agent does.** It holds what survives and implements none of the dead clause:

| | |
|---|---|
| **a fact is declared** | somebody said it about themselves. A `by` reading inferred, derived, guessed, detected, assumed, estimated, observed, automatic, auto or model is refused — deciding from a conversation that a user is a beginner is a judgement they did not make and cannot see (D-33) |
| **a mode is granted** | it carries who granted it and when, and a grant whose `by` is not a person is refused. **No mode held is not User Mode** — a permission boundary that defaults to something is not a boundary |
| **nothing about how to speak** | there is no tone, level or phrasing field in anything it returns, whatever it is asked |

**A fourth was found later the same day**, building `HERON-RPT-CMP-001`: its row chooses *what goes
in* by reader, and calls that a judgement needing a model call. D-27 replaced exactly that judgement with
a five-row table keyed on the **request**, and gave the reason in its own consequences — *"nothing has to
detect who is talking. A whole class of 'why did it answer differently today' stops being possible rather
than being made visible."* The agent implements the table and calls no model.

**Not fixed here.** `HERON-ORC-PER-003` is one of the four agents [D-01](../DECISIONS.md) delegates to the
host, so retiring or renaming it is a change to the host contract, and `docs/22 §3`'s note needs rewriting
rather than deleting — the rule it protects is the reason the User & Personalization department exists at
all. Both are the owner's, and they are one paragraph apart from being settled.

---

### 🟠 F20. "Strips project identifiers" is the framing D-26 narrowed away from

**Found by:** building `HERON-RPT-RED-003`, the Report Redaction & Release Agent, 2026-09-15.
**Status:** open. The agent implements D-26 and does **not** strip.

[D-26](../DECISIONS.md) opens with a warning to its own reader, which is the reason this is worth raising
rather than quietly following:

> **This decision was refined three times on the day it was written, each time in the same direction:
> from *nothing may travel* toward *the file may not travel*. The rule below is the final one. It is
> narrower than the first two, and commits from that day quote the earlier framings — so the movement is
> recorded here rather than quietly overwritten, because a reader needs to know which version won.**

And the rule that won:

| Never leaves the machine | **Fine in the conversation** |
|---|---|
| The `.rvt` and `.rfa` files themselves | **Project names**, file names, content names |
| Family and project templates | Element data — counts, sizes, parameters |
| Any Revit binary | Engineering ideas, reasoning, and code |

`docs/28`'s row for this agent reads:

> The gate before a report can be shared or leave the machine. **Strips project identifiers**, enforces
> scope, blocks confidential-project egress.

**Project names are in D-26's right-hand column.** Stripping them implements the framing the decision
moved away from — and doing it quietly is worse than doing it wrongly, because it leaves a report that
*reads* as anonymised without anybody having decided it should be.

**What survives, and all of it is a refusal rather than a strip:**

| | |
|---|---|
| a **Revit binary** attached | `.rvt` `.rfa` `.rte` `.rft` — D-26's own left-hand column, the same rule `HERON-WSP-TPL-006` keeps on the other side of the machine |
| a **credential** in the text | article 17. Refused whole, not redacted — a report that had a key in it is one somebody should look at, not one to clean and send. The shape is named and the value never is |
| **more than one project** | D-26's third point: *"project-based knowledge must be kept segregated and separated"*. This is what "enforces scope" means once the stripping is gone, and it is the half of the row that survives whole |
| a project that has **not declared** this may leave | absence is not permission. docs/12 §85 is about contracts that prohibit egress, and a contract is something somebody signed rather than something to assume from silence |

**Not fixed here.** Rewriting the row is three words, but it is the third row this session found written
against a superseded decision — with **F16** (the Keyword Agent's "synonyms" against D-34) and **F19**
(three places still handing work to the persona D-27 abolished). Individually each is a clause; together
they suggest `docs/28` was written before several of the decisions that now govern it, and a pass over
the register against `DECISIONS.md` would find whatever else is in the same state. That pass is the
owner's call, not three more clauses.

---

### 🟡 F21. `docs/09 §94` still carries the promotion gate D-30 replaced, and still calls it open

**Found by:** building `HERON-LRN-PRO-004`, the Learning Promotion Agent, 2026-09-15.
**Status:** open. The agent follows D-30.

[docs/09 §94](../09-skills-and-fragments.md) proposes the lifecycle gates, and two of its lines are stale:

> | VALIDATED → PROVEN | **N successful real executions**, zero unexplained failures, no user corrections *(N to be set — suggest 10)* |
>
> Tracked as **[Q-9]** for the value of N and who may approve.

**Q-9 is answered.** [D-30](../DECISIONS.md) answers it by rejecting the count outright, and does so with a
defect from the real library:

> One fragment's record reads: the level chain never tried `RBS_START_LEVEL_PARAM`, so setting a level
> filter matched **zero** ducts **and reported success**.
>
> **A fragment that succeeds while doing nothing passes ten runs. It passes a thousand.** A count measures
> that nothing threw, which is not the property anybody cares about.

D-30's gate is **one recorded proof against a real model** — dated, naming the model, carrying a positive
case, a **negative** case (*"this is the one that catches succeeded and did nothing, and a proof without
it is not a proof"*), and a second route where one exists — recorded by whoever ran it, *"under their
name and the date, not a tick"*. It also answers the second half of Q-9: who may approve.

So `docs/09 §94` asks for a number D-30 deleted, and points at a question D-30 closed.

**What the agent does.** It follows D-30: a candidate arriving with a thousand successful runs and no
proof is refused, and **the count is echoed back** so nobody mistakes the refusal for not having noticed.
A proof with no negative case is refused **before** anything else the proof is missing — the others make
a proof incomplete, and that one makes it not a proof.

**Why this one is worth its own row.** F16, F18, F19 and F20 are all `docs/28` rows written against a
superseded decision. This is the same failure in a **different document** — which means the pass those
findings ask for is not only over the register. Anywhere a document says *"tracked as Q-n"* is worth
checking against `DECISIONS.md`, because that phrase is exactly what stops a reader looking further.

---

### 🟡 F22. One permission ladder, four copies in `brain/` alone

**Found by:** building `HERON-SKL-CMP-005`, which needed the ladder and refused to add a fifth.
**Status:** open. Measured, not read.

```bash
grep -rln 'RISK_LADDER\|RISK_ORDER\|"ANALYZE"' brain/*.py
```

| file | agent | how it carries it |
|---|---|---|
| `heron_capability.py` | `HERON-KRN-CAP-008` | `RISK_ORDER = (…)` |
| `heron_events.py` | `HERON-KRN-EVT-004` | `RISK_ORDER = (…)` |
| `heron_hr.py` | `HERON-AHR-HR-002` | `RISK_LADDER = (…)` |
| `heron_skill.py` | `HERON-SKL-VAL-004` | an **inline tuple** inside `validate()` |

All four are `("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH", "ADMIN")`, all equal by
value, **none the same object**. Three more modules derive from it — `heron_shadow.py` maps tier to risk,
`heron_trainer.py` maps risk to permission, `heron_validation.py` keeps `READ_ONLY_RISKS`.

`mcp/server/heron_tools.py` holds a fifth as integers (`READ = 0` … `ADMIN = 6`). **That one is
legitimate**: [D-48](../DECISIONS.md) forbids `brain` importing from `mcp`, so the bridge needs its own —
and `heron_tools.py`'s own comment already says a rule kept in two places by good intentions is a rule
that will eventually be kept in one.

**Why it matters more than it looks.** The ladder is ordered, and the order is the security property:
`MODIFY < PUBLISH < ADMIN` is what makes "propagate the highest risk upward" mean anything. Four
independent orderings are four chances for one to be edited — a level inserted, one renamed — and the
copies would still each look right on their own.

**Not fixed here**, and the reason is that the fix is a decision rather than an edit: `brain/` has no
module that *owns* permission levels. [docs/12 §71](../12-security-and-permissions.md) puts risk in the tool
registry, which is `mcp`'s side of D-48. Somebody has to say which `brain` module is the home — or that
it belongs in `platform/`, which both layers may import — and that is the owner's call.

`HERON-SKL-CMP-005` imports `heron_capability.RISK_ORDER` and adds no copy. The suite asserts it is that
module's object, by identity.

---

### 🟡 F23. D-84 took a word out of two places; it is still in the three that bind

**Found by:** reading `HERON_CONSTITUTION.md` word by word on 2026-09-21.
**Status:** open. One decision, three sentences, no code changes.

```bash
grep -n 'sandbox' HERON_CONSTITUTION.md docs/14-golden-rules.md README.md
```

[D-84](../DECISIONS.md) chose **keep what exists, build no subprocess, and take the word out**, because
`heron_sandbox` runs a new agent as ordinary Python in the supervising process: *"an agent that
cooperates is held. One that does not can call `open()`, import the bridge, reach into globals or
delete files, and nothing is in its way. That is most of the value and none of the guarantee, and the
word sandbox implies otherwise."* Its consequence line says **"there is now nowhere in the repository
that claims otherwise."**

[Row 5b-52](../FRAGMENT-ISSUES.md) took it out of the module, the register row, `DISCLAIMER.md` and
`SECURITY.md`. These three are left, and they are the ones with authority:

| where | what it says today |
|---|---|
| [Constitution](../../HERON_CONSTITUTION.md) **Article 11** | *"Generated or newly imported code runs in a **sandbox** or against a detached copy first."* |
| [Golden Rule 18](../14-golden-rules.md) | *"New code runs in a **sandbox** or against a detached copy."* |
| [`README.md`](../../README.md) **Decided** | *"Generated code — Hybrid: **scripting sandbox** while testing, compiled C# for production."* |

**The Constitution matters most of the three**, and not because it is the most read. Its own
Enforcement section says the Articles *"are assembled into an agent's instructions from this file"* —
so Article 11 is not a page somebody might see, it is text injected into a running agent, telling it a
containment exists that D-84 recorded as not built.

**Why an agent did not simply fix it.** The Constitution's Amendment section reads *"Articles are never
weakened silently, and **never by an agent**"*, and Golden Rule 18 is the owner's on the same footing.
`AGENTS.md` names the answer for this exact case: record both, name the governing decision, name the
observed evidence, name who resolves it — and leave the conflict open rather than closing it whichever
way makes the task easier.

**Nothing about the behaviour would change, and no requirement moves.** The first run really does shut
the Revit door and the production scopes, write down every attempt, and mark the run so it can never
count as evidence. Only the word naming that mechanism promises more than it does. Drafted so it is a
yes or a no rather than a writing job:

| where | proposed |
|---|---|
| **Article 11** | *"Untested code never touches a live model. Generated or newly imported code runs **watched — every door shut and every attempt recorded — or** against a detached copy first. Watched is not contained ([D-84](../DECISIONS.md))."* |
| **Golden Rule 18** | *"New code runs **watched, not contained** — or against a detached copy. Promotion to live comes after it passes."* |
| **README Decided** | *"Generated code — Hybrid: **watched first run** while testing, compiled C# for production."* |

A **no** is a real answer too, and it is recorded rather than argued with: it would mean the word stays
and D-84's consequence line is the sentence to correct instead.

---
