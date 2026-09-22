# Session note — TWENTY-THREE FRAGMENTS BUILT FOR RECORDED GAPS, AND FOUR API FACTS THAT CHANGED THE ANSWER

> **Archived session note** from 2026-09-19. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-19 — TWENTY-THREE FRAGMENTS BUILT FOR RECORDED GAPS, AND FOUR API FACTS THAT CHANGED THE ANSWER

**Merged as PR #184.** The owner read out five lists of Revit jobs - roughly 290 of them - and asked
which Heron could do. Twenty-three came back with no route at all, were recorded, and
were then built. **The note that recorded them was deleted on 2026-09-19 once it had emptied** — that
none of the twenty-three has met a model is [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) **A19**, and what
is still worth building is in [`PROPOSALS.md`](../PROPOSALS.md). **372 → 395 fragments. All twenty-three are `DRAFT` and none has met a model.**

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
  [`heron-ship`](../../.claude/skills/heron-ship/SKILL.md) §1 warns about, firing exactly as documented.
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
See [NEEDS-CHECKING](../NEEDS-CHECKING.md) **A19**.

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
a blocked CDN and are not: with the refresh it installed first time. [§30](../30-compiling-away-from-windows.md)
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
([D-59](../DECISIONS.md)), **59 dropped-counts** ([D-64](../DECISIONS.md)), the **preview selection** in
`RevitWrite.cs` ([D-60](../DECISIONS.md)), the **workflow id across the seam**
([D-61](../DECISIONS.md)/[D-62](../DECISIONS.md)) which is small and unlocks three things at once, and
the **project knowledge scope**, which the Codex review added. The lists and the order are in
[the open-questions entry](../handover-archive/2026-09-09-the-open-questions-track-44-52-answered-and-the-rest-needs-t.md).

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
   a comment here called it *"the property every version has had"*. That is [D-05](../DECISIONS.md), and it
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
[PROPOSALS.md](../PROPOSALS.md), none acted on. The ones that block building:
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
| **One malformed `fragment.yaml` took down all 343 fragments** | `load()` promised `ValueError`; `yaml.YAMLError` is not one. Measured, not read — one good, one broken, one good returned *nothing*. **The owner named this shape before the code was looked at** ([D-48](../DECISIONS.md)) |
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
> machine. First time it has bitten. [D-49](../DECISIONS.md), and
> [`tests/test_mcp_stdio.py`](../../tests/test_mcp_stdio.py) is the check made permanent — a real subprocess
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
  reset destroyed another session's uncommitted edit. **[§9b](../HANDOVER.md#9b-three-sessions-at-once--the-protocol-that-stops-them-colliding)
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
