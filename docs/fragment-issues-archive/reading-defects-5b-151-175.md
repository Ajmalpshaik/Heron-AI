# FRAGMENT-ISSUES archive — section 5b, rows 151 to 175

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 151 to 175 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-151

*Moved from the register on 2026-11-17.*

**Defect.** **THE REPORT PRINTS AN ABSOLUTE CONTAINER PATH IN ONE HALF AND A REPO-RELATIVE ONE IN THE OTHER.** Found 2026-09-22 reading `tools/check-declared-questions.py` - 277 lines, never read before, run by CI under *The reports - a finding is a question*. **Measured:** all ten fragment rows print `/home/user/Heron-AI/brain/fragments/.../fragment.yaml`, naming a container that will not exist when the owner reads the CI summary, while the skill section two blocks below prints `brain/skills` relative. `tools/check-revit-gate.py` reads the same `frag.folder` and prints `os.path.basename` of it, so this is the only tool in the tree publishing the absolute one. **AND A SECOND FINDING THAT DOES NOT FIRE TODAY, SAID SO PLAINLY:** the *ASKS AND THEN SAYS WHAT TO DO* header counts `len(told) + len(s_told)`, but the very next line tests `if not told` and prints `none`, and the line after prints `len(told)` alone. Measured today `told` is 1 and `s_told` is 0, so the two agree by coincidence. With any skill utterance that asks and then instructs, the header would print a number above the word `none`, and `--all` could never print the skill instructions its own help promises. A shape, not a behaviour - recorded because it is three lines from the one that does fire, not because a scan found it.

**State.** **FIXED 2026-09-29.** `_shown()` prints every folder repo-relative with `/`, and the *ASKS AND THEN SAYS WHAT TO DO* block reads fragments and skills on all three of its lines. `tests/test_declared_questions.py` section 8 runs `main()` over one staged fragment and one staged skill and reads what it PRINTS - seen to FAIL on both findings before the change and pass after. **WHAT WAS TRACED AND IS SOUND, so nobody re-reads it:** it catches the case it was written for - `SET_VIEW_SCALE` with *what scale is this view* - and its three quotations of `tools/check-routing.py` matched that file on 2026-09-22.

---

### Row 5b-157

*Moved from the register on 2026-11-17.*

**Defect.** **TWO DECISION NUMBERS WERE USED TWICE, AND THE FIRST DECISION UNDER EACH LEFT THE LOG.** DECISIONS.md is append-only - a reversal gets a NEW entry - but commit `2248cb3` (2026-08-31) wrote *"Heron tracks the MCP SDK across major versions"* as **D-45**, over *"The library is built out first, and proved in one pass later"* (2026-08-30), and commit `75c22a9` (2026-09-06) wrote *"The Emergency Stop button is removed"* as **D-46**, over *"A context fragment is consumed by the host, not by another fragment"* (2026-08-31). Neither first text was anywhere in DECISIONS.md; both survive in `git show 6582810:docs/DECISIONS.md`. Documents still cite the first meanings - `docs/31` lines 52, 341, 354 and 359, HANDOVER's manual, two fragments' `purpose`, three code comments - and read today they point at the wrong rule. **Found 2026-09-22** by a reader in the AJ AI Brain hooks audit, then checked by that session and again here. `check-docs.py` checks that no decision number is DEFINED twice at once; nothing checks that a number's heading kept its words.

**State.** **FIXED 2026-09-23, on the owner's choice** (*"Restore as D-97, D-98"*). The two first decisions are back, word for word from `6582810`, as **D-97** and **D-98**, each with a `Numbering` line saying which number it held until which commit; the current D-45 and D-46 carry a `Numbering` line pointing the other way, so a citation written before those dates can still be followed. Only references that meant the first decisions were repointed, each read in its context; dated session notes in `handover-archive/` were left as written, because they are history - and so was the `purpose` of two PROVEN fragments, `find-sheets` and `find-views`, because changing a proven fragment's file owes a real-Revit proof under D-30 that a citation does not justify. The Numbering line answers all of them. **The check it called worth its own row is [row 5b-170](../FRAGMENT-ISSUES.md#), 2026-09-23 - and building it found that the same merge lost D-47, D-48 and D-49 as well, [row 5b-171](../FRAGMENT-ISSUES.md#).**

---

### Row 5b-159

*Moved from the register on 2026-11-17.*

**Defect.** **A FRAGMENT WRITE'S AUDIT LINE DOES NOT SAY WHAT REVIT RAISED.** Found 2026-09-23 (package C3). The move path's own audit record has carried `warnings` since it was written. From 2026-09-23 a fragment write's REPLY carries `warnings` and `warningsDismissed` too - but a fragment run's audit line is written by [`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs) from the request and the reply's `error` and `document`, so the trail that exists to answer *"what happened to my model"* records that a fragment ran and not that Revit warned about what it did. The chat hears it; the trail does not. **Not fixed here:** the dispatcher is outside C3's files. The repair is one field: read `warnings` off the response beside `error`, for the two fragment operations.

**State.** **MOVED TO ISSUE [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired** **2026-10-09:** FIXED IN SOURCE, NOT YET SEEN in a real Revit's audit file. [`Json.cs`](../../revit/Heron.Bridge/Json.cs) has `ReadLong`, which walks the reply the way `ReadString` does and returns the top-level whole number, or null for absent, a string, a fraction, an exponent, overflow or a malformed reply. [`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs) adds `warnings` to the audit line's numbers, beside `ms`, for the four `run_fragment_*` operations; one `fragmentRun` test decides both this count and the `fragment` name. An absent count stays absent, never written as 0, because a read or a refusal says nothing about warnings. **Evidence:** `tests/test_failure_note.py`. Section 3 reads the dispatcher and the executor as text: the count is read as a number, added to the list the audit line is written from, only when the reply carried one, for fragment runs only, and the verdict writes it with `Json.Num`. Section 5 runs `Heron.FailureNote.TestHost`, which now links `Json.cs` by source and reads a reply built the way `WithVerdict` builds it, with a decoy `warnings` nested under `provides`. Seen to fail 2026-10-09: with `Json.cs` and the dispatcher as they stood, five dispatcher checks fail and the host does not build (no `ReadLong`); with only the dispatcher as it stood, the five fail and the host passes. Three narrower breaks - the count added to a list the line is not written from, a 0 for a reply that carried none, the verdict writing the count as text - each fail one check; the first version of the suite passed all three, and was tightened. `check-compile` builds every project for 2020-2027. **Still owed, so still OPEN:** a fragment write Revit warns on, run on the owner's PC, whose audit line carries `"warnings": N`, and a read whose line carries no `warnings` key.

---

### Row 5b-161

*Moved from the register on 2026-11-17.*

**Defect.** **ARTICLE 7'S "EXPLICIT CONFIRMATION FOR THAT SPECIFIC OPERATION" HAS NO CODE BEHIND IT ON `revit_change`.** Found 2026-09-23 while recording [D-99](../DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so). Article 7 asks for explicit confirmation before deleting elements, purging, bulk parameter writes, family reloads, workset changes and anything touching a linked model, and the Constitution's Enforcement table placed it at *"Revit add-in permission gate + transaction wrapper"*. That gate reads one risk per OPERATION - `run_fragment_write` is `MODIFY` in [`HeronOperationRegistry.cs`](../../platform/Heron.Core/HeronOperationRegistry.cs) - plus the Changes switch, and never the fragment. So with Changes ON, `revit_change` runs `DELETE_ELEMENTS`, `UNLOAD_LINKS`, `RELOAD_LINKS`, `RENAME_WORKSET` and `SET_ELEMENT_WORKSET` - every one `risk: MODIFY` - and nothing asks: `grep -n "TaskDialog\|onfirm"` over `RevitFragment.cs`, `RevitOperations.cs`, `RevitDispatcher.cs` and `HeronPermissions.cs` finds nothing, and the server sends straight through. **Not introduced by D-99** - the path has run this way since 2026-09-15; D-99 is what wrote it down. **What carries Article 7 now is text:** every chat's instructions include it (`host.chat`), which is guidance a model can be argued out of - the Constitution's own warning about a rule stated and not enforced. **What was NOT checked:** which of the library's `MODIFY` fragments are destructive in Article 7's sense. No field on a fragment says so, and that missing field is the likely shape of any repair - a declared `destructive:` the server reads before sending, or a question the add-in asks before it opens the transaction group.

**State.** **MOVED TO ISSUE [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - needs the owner**: should a destructive fragment through `revit_change` stop and ask, as Article 7 says, or be recorded as a second exception? The Enforcement table now says the switch is all that stands there. **Deleting elements answered 2026-10-18** by the owner's [D-142](../decisions/D-142.md) ([row 5b-1200](../fragment-issues/section-5b-rows-1176-1200.md)): any kept change Revit counts a deletion in is asked in Revit's own box, by measurement, not by a field on the card - built, not run in Revit. The rest of Article 7 - purging, bulk parameter writes, family reloads, worksets, links - stays OPEN.

---

### Row 5b-163

*Moved from the register on 2026-11-17.*

**Defect.** **TWO ROWS OF THIS TABLE CAN CARRY THE SAME NUMBER, AND NO GATE OR SUITE THAT WAS RUN NOTICES.** Found 2026-09-23 merging `main` into [#307](https://github.com/Ajmalpshaik/Heron-AI/pull/307) (package C2) after [#306](https://github.com/Ajmalpshaik/Heron-AI/pull/306) (package C3) merged: two sessions working in parallel had each appended rows **158** and **159** to this section, both taking the same next free number. Git stopped at the conflict, so they were renumbered by hand - C2's became 161 and 162. **Measured:** the other resolution - both sides kept, numbers untouched - left two rows 158 and two rows 159 here, and all ten CI gates, `tools/open-defects.py`, `test_archive_fragment_issues`, `test_owner_queue` and `test_naming` exited 0 on it. A citation such as `row 5b-158` would then name two rows. NEEDS-CHECKING has the check this table lacks: `test_owner_queue` fails when an ID is reported twice, and caught the same kind of clash between [#304](https://github.com/Ajmalpshaik/Heron-AI/pull/304) and #307 that day (C2's Group AE, now AH). **What was NOT checked:** the full suite run on that tree, and the other numbered tables in this file.

**State.** **FIXED 2026-09-29.** `tests/test_open_defects.py` check 7 is a GATE: it fails when a row number appears twice in section 5 or 5b, read as `open-defects.py` reads the register. Seen to FAIL with row 5b-189 appended a second time (`repeated: 5b-189`) and pass on the real register. **Not covered:** the other numbered tables in FRAGMENT-ISSUES, which no tool reads by number.

---

### Row 5b-164

*Moved from the register on 2026-11-17.*

**Defect.** **THE BOUNDARY GUARD RAN ONLY IN A SESSION THAT HAD LOADED ITS SKILL.** `heron-guard`'s hook was declared in the skill's own frontmatter - the shape [Q-49](../OPEN-QUESTIONS.md) chose on 2026-09-09 - and a hook declared there is registered only when the skill is invoked (Claude Code's own skills documentation). So every session that never loaded `heron-guard` could write the Revit vendor namespace outside `revit/` with nothing refusing it until somebody ran `check-structure.py`. **Proven by hand on 2026-09-22** (the earlier-brain plan's finding H2): the script refused a forbidden edit piped into it, and the same edit made through the editor in a normal session went straight through. **WHAT WAS NOT CHECKED:** whether a real Windows session refuses it now - NEEDS-CHECKING **AI2**. No other skill declares a hook in its frontmatter (`grep -l '^hooks:' .claude/skills/*/SKILL.md` finds none).

**State.** **FIXED 2026-09-23** in [PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308). Wired from `.claude/settings.json`, which every session reads; the frontmatter declares nothing, because the host runs a skill's copy of a hook separately from the settings' copy and both would run it twice. `tests/test_heron_guard.py` section 7 checks both halves and runs the exact command the settings give; it failed six checks on the old wiring. Seen live in the cloud session that built it: the guard refused a Write into `brain/` with the skill never loaded.

---

### Row 5b-165

*Moved from the register on 2026-11-17.*

**Defect.** **ON WINDOWS THE GUARD REFUSED ANY EDIT CARRYING ARABIC, BY CRASHING ON IT.** The host writes a hook's payload as UTF-8. Windows decodes a piped stdin in the ANSI code page, and cp1252 has no character for five byte values UTF-8 uses constantly - Arabic among them - so `sys.stdin.read()` raised on such a payload, and the guard's trap 2 turns a crash into a refusal. With the guard now running in every session, every edit carrying an Arabic letter such as the one encoded `D9 81` would have been refused, for a reason that has nothing to do with the boundary. Found 2026-09-23 while building earlier-brain package C1, and reproduced on Linux with `PYTHONIOENCODING=cp1252` and the payload sent as raw UTF-8 - the first version of the test sent it through `json.dumps`, which escapes every non-ASCII character and hid the case. **WHAT WAS NOT CHECKED:** a real Windows console - NEEDS-CHECKING **AI3**.

**State.** **FIXED 2026-09-23** in [PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308). The guard and both `heron-session` hooks read the payload as bytes and decode it as UTF-8 (`read_payload`). `tests/test_heron_guard.py` section 4a and the Arabic check in `tests/test_heron_session.py` each failed on the old code and pass now.

---

### Row 5b-166

*Moved from the register on 2026-11-17.*

**Defect.** **DRIFT NOTHING WAS CHECKING: TOOLS NAMED NOWHERE ON THEIR PAGE, CONTROL CHARACTERS IN THE DOCS, AND A TYPED MCP TOOL TOTAL.** The earlier-brain plan's finding H8, verified 2026-09-22 and re-measured 2026-09-23: eight files in `tools/` named nowhere in `tools/README.md`, three of them not Python, so the page's own `*.py` one-liner could not have seen them; a backspace and a form feed in archived row 5b-21, where the row meant to write their escapes, and a C1 control character in `docs/07`, where `Addins\2020` had once been read as an octal escape; `docs/33` and `docs/34` saying *"Heron has 14 MCP tools"* while `heron_tools.TOOLS` held more than twice that; and the count row `check-docs.py` kept for tools, which looked for the phrase *"N tools mentions"* and matched no sentence in the repository. **WHAT WAS NOT CHECKED:** any other wording of an MCP tool total - the check reads one form, *N MCP tools*, deliberately.

**State.** **FIXED 2026-09-23** in [PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308). `check-docs.py` section 10 names every file in `tools/` and every skill folder, section 11 reads every tracked file for control characters, double-encoded text and conflict markers, and section 7 derives the MCP total - each seen to fail on these before they were corrected, and each planted in a copy by `tests/test_docs_guard.py`. The nine rows the plan listed are in `tools/README.md`, the three characters are written back as the text they stood for, and the two documents type no MCP total.

---

### Row 5b-168

*Moved from the register on 2026-11-17.*

**Defect.** **`heron_bridge_client` RESOLVES HERON'S LOG AND BRIDGE FOLDERS TO RELATIVE PATHS ON LINUX.** [`mcp/client/heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) builds `DISCOVERY_DIR` and `LOG_DIR` from `os.environ.get("LOCALAPPDATA", "")`, and on Linux that variable is unset - measured 2026-09-23, they come back as `Heron/bridges` and `Heron/logs`, relative to whatever folder the process happens to run in. It is the Python seam for `HeronPaths.Logs`, so anything that asks it on Linux is told a path inside the working folder. Found while choosing where the development hooks keep their diary: `.claude/skills/heron-session/bin/hook_log.py` refuses a relative answer and falls back to the knowledge folder. **WHAT WAS NOT CHECKED:** whether any Linux test or the bridge round-trip depends on the relative form, or what the C# side resolves on Linux.

**State.** **FIXED 2026-09-23** in [PR #316](https://github.com/Ajmalpshaik/Heron-AI/pull/316). `local_app_data()` resolves the folder the way .NET resolves `LocalApplicationData`: `%LOCALAPPDATA%` on Windows, exactly as before; elsewhere an absolute `$XDG_DATA_HOME`, else `~/.local/share`. **THE NOT CHECKED, NOW CHECKED:** no Linux test used the relative form - `test_bridge_roundtrip` reads its pipe name from the host, not from the discovery folder - and .NET 10, asked directly, answers `~/.local/share` there, ignoring a relative `XDG_DATA_HOME` and `LOCALAPPDATA` alike. It was already on disk: the bridge test host had been leaving its discovery files in `~/.local/share/Heron/bridges`, the folder the Python side could not see. [`tests/test_bridge_paths.py`](../../tests/test_bridge_paths.py) failed 6 checks on the old code, and asks .NET again wherever the .NET 10 SDK is. **The hooks' diary moves with it:** on Linux it is Heron's log folder now, not the knowledge folder, and the three hook suites steer it with `XDG_DATA_HOME` as well as `LOCALAPPDATA` - without that they wrote into the real home folder, which is how it was found.

---

### Row 5b-169

*Moved from the register on 2026-11-17.*

**Defect.** **THREE PAGES STILL SAY HERON HAS NO HOOKS, OR DO NOT SAY WHERE THEY ARE WIRED.** [`docs/README.md`](../README.md)'s row for Q-49 says *"Heron has no hooks of any kind — no `.claude/settings.json`"* and marks the question open, though it was answered and built on 2026-09-09; [31 research](../31-research.md) says the same in the present tense in its sections on ECC and gstack, written that day; and [`docs/PROJECT-MAP.md`](../PROJECT-MAP.md)'s row for `.claude/` does not mention `settings.json`, which since 2026-09-23 wires three hooks every session runs. **WHAT WAS NOT CHECKED:** any other page describing the development hooks - `grep -rn 'settings.json' docs` is the start.

**State.** **FIXED 2026-09-23** in [PR #316](https://github.com/Ajmalpshaik/Heron-AI/pull/316). The Q-49 row in docs/README.md is closed and says where the hooks are wired; docs/33 keeps both passages as written and gains a dated correction under each; docs/PROJECT-MAP.md's `.claude/` row names `settings.json` and what it wires. **THE NOT CHECKED, NOW CHECKED:** `grep -rn 'settings.json' docs` and a search for *no hooks* found no other page saying Heron has none - OPEN-QUESTIONS Q-49 keeps its question as it was asked, with the answers under it.

---

### Row 5b-171

*Moved from the register on 2026-11-17.*

**Defect.** **THREE MORE DECISION NUMBERS WERE USED TWICE - D-47, D-48 AND D-49 - AND THEIR FIRST DECISIONS ARE STILL OUT OF THE LOG. ONE MERGE TOOK FIVE, AND [ROW 5b-157](../FRAGMENT-ISSUES.md#) RESTORED TWO.** Found 2026-09-23 while building [`tools/check-decision-titles.py`](../../tools/check-decision-titles.py) ([row 5b-170](../FRAGMENT-ISSUES.md#)), by reading the whole history of DECISIONS.md rather than the two numbers row 5b-157 named. **ONE BRANCH** wrote D-45 on 2026-08-30 (`85619eb`) and D-46 to D-49 on 2026-08-31 (`6582810`, `290edb5`, `5ff8ac0`, `469fd2e`). **MAIN** wrote a different D-45 on 2026-08-31 (`2248cb3`) without having seen them. **The merge of 2026-09-02, `1f884ae`** - *"take the merged session's version wherever both built the same thing"* - came out with exactly main's decision headings, ending at D-45, and the branch's five were gone. On 2026-09-06 three of the freed numbers went to new decisions: D-47 and D-48 by `ec17e79`, D-49 by `2087022`. The first D-47 was *"Risk is part of routing, not only part of permission"*, the first D-48 *"Several fragments may jointly feed one, and the orphan check asks per need"*, the first D-49 *"A need may bind to a provided name that is not its own"*. None of the three is anywhere in the tree today; all three are in `git show c4eea6e:docs/DECISIONS.md`. **TWO SOURCES DISAGREE, AND BOTH ARE RECORDED:** row 5b-157 says `2248cb3` wrote its D-45 *"over"* the first, and `75c22a9` its D-46 likewise, and the Numbering lines on D-45, D-46, D-97 and D-98 name those two commits. The history says `2248cb3` was on a branch that never held the first D-45, and the first D-46 had left the log in `1f884ae` four days before `75c22a9` reused its number. So the lines name the commit that REUSED each number, not the merge that LOST the decision. The dates the lines give for reading an old citation still hold, and nothing was edited. **WHAT WAS NOT CHECKED:** which documents cite D-47, D-48 or D-49 meaning the first decisions. #303 read each reference to D-45 and D-46 in its context and repointed the eleven that meant the first decisions; that reading was not done here

**State.** **FIXED 2026-09-23, on the owner's instruction** (*"fix row 5b-171"*), with the repair row 5b-157 took for D-45 and D-46. The three first decisions are back as [D-101, D-102 and D-103](../DECISIONS.md), word for word from commit `5c84302` - the branch tip the merge `1f884ae` dropped them from - each with a Numbering line naming its first number, and D-47, D-48 and D-49 each carry one quoting the title they were first written with. The check's `KNOWN` table lost its three entries, which it would otherwise have failed as no longer needed. **Two changes to the words, both named on the Numbering lines:** D-102 cites the first D-46 and D-49 as D-98 and D-103, and D-103 cites the first D-48 as D-102. Wrapped metadata lines were also joined, which renders the same, because `split-decisions.py` keeps a metadata line in the index only while it starts with `**Key:**`. **EVERY CITATION WAS READ IN ITS CONTEXT** - the half this row had left unread. Five meant the first D-47 and now say D-101: `brain/retrieval-history.md`, `tools/check-intrusion.py`, `tools/README.md` and two in `docs/HANDOVER.md`, on routing risk and ladder-crossings, each first written 2026-08-31 on the branch that held it. One meant the first D-48 and now says D-102: the docstring of `need_binds()` in `brain/heron_fragment.py`, whose own subject is the first D-49. None meant the first D-49. Left as written: three PROVEN fragments whose comments say a sentence naming two capabilities *"belongs to a SKILL (D-47)"* - written on 2026-09-06 before `ec17e79` reused the number, which D-47's Numbering line covers - and the dated session notes, which are history. Section 9 of [`tests/test_check_decision_titles.py`](../../tests/test_check_decision_titles.py) now carries all ten renumbered decisions and fails each the moment its line is taken away. Reading the citations also found [rows 5b-184 and 5b-185](../FRAGMENT-ISSUES.md#).

---

### Row 5b-172

*Moved from the register on 2026-11-17.*

**Defect.** **HEALING NAMES THE DECISION LOG AND THE NEEDS-CHECKING REGISTER BY THEIR OLD SINGLE FILES, SO ITS REFUSAL OF ONE DECISION'S FILE OR ONE GROUP'S FILE NO LONGER SAYS WHAT IT IS PROTECTING.** Found 2026-09-23 while splitting NEEDS-CHECKING.md one file per group, by searching the code for every list that names the register by path. [`brain/heron_healing.py`](../../brain/heron_healing.py) `SOURCES_OF_TRUTH` exists *"so a refusal can say what kind of thing it is protecting, rather than only that it will not touch it"*, and it matches `docs/DECISIONS.md` and `docs/NEEDS-CHECKING.md` as parts of the path. Since #303 each decision's full record is `docs/decisions/D-NN.md`, and since this split each group is `docs/needs-checking/group-x.md`; neither path contains either name, so `repair()` on one answers with the generic *"not in the derived register, and this agent does not guess"*. Measured 2026-09-23 by calling `repair()` on `docs/decisions/D-30.md` and `docs/needs-checking/group-j.md`. **The refusal itself is unchanged**: anything not in `DERIVED` is refused, which is the fail-closed half, so nothing can be regenerated over either file - only the reason is lost.

**State.** **FIXED 2026-09-23, on the owner's instruction.** `SOURCES_OF_TRUTH` names `docs/decisions/` and `docs/needs-checking/`, each in the words of the single file it came from, and `docs/needs-checking-archive/group-` as well: that folder holds the register's done checks, moved out of NEEDS-CHECKING.md by #303 with their words unchanged - the same gap, found while fixing this one. Only the archive's group files are named, because its README is the index its own tool rewrites. `repair()` on each now says what it protects and ends *"Nothing regenerates it."* Section 3 of [`tests/test_healing.py`](../../tests/test_healing.py) asks all three for that sentence and for a word their path cannot supply - the generic refusal repeats the path, so a check for *decision* alone passes on `docs/decisions/` unfixed - and was run red against the unfixed module first.

---

### Row 5b-174

*Moved from the register on 2026-11-17.*

**Defect.** **`AUTO_SIZE_PIPE` READ ITS SIZE BACK BEFORE THE REGENERATION THAT APPLIES THE SNAP, SO ITS OWN SNAPPED-SIZE REFUSAL COULD NEVER FIRE.** Found 2026-09-23 while adding the plan's row 2 count. `brain/fragments/auto-size-pipe/impl/any/fragment.cs` set the diameter and read it straight back with no `doc.Regenerate()` between. `SET_MEP_SIZE`'s header records that *"immediately after the set, the parameter still reports WHAT WAS ASKED FOR"* and that *"the snap happens at REGENERATION"*, and `AUTO_SIZE_MEP` regenerates before its read-back for that reason. So the case this fragment's own `tests/cases.yaml` declares - a segment offering 32 and 65, asked for 50, reported in `refused` - could not come back. **Not checked**: the timing is SET_MEP_SIZE's header, not something measured here.

**State.** **FIXED 2026-09-23** - one regeneration per pipe before the read-back, as AUTO_SIZE_MEP does. DRAFT and never proved, so no proof moved. NEEDS-CHECKING AJ2

---

### Row 5b-175

*Moved from the register on 2026-11-17.*

**Defect.** **THREE FRAGMENTS ANSWER "IS IT IN THE ROOM" TWO DIFFERENT WAYS, AND ONE CARD SAYS THE OPPOSITE OF ITS OWN CODE.** Found 2026-09-23 checking the plan's row 5, which the plan marked *reported*: a device in the ceiling void is outside its room's VOLUME (observed elsewhere, mostly on Revit 2020). In the files: `count-by-spatial-container` tests the element's X and Y at the container's own Z (`impl/any/fragment.cs:84-88`), so a ceiling-void diffuser counts IN the room below; `check-room-mep-completeness` calls `room.IsPointInRoom(point.Point)` at the device's own insertion point (`:94-99`) - the volume test - while its purpose says *"one in the ceiling void above the room still counts"*; `filter-elements-in-room` asks the volume test too (`:65-71`), and its purpose says an element above the ceiling *"can be genuinely inside"*. Where a room's volume stops at the ceiling, the first counts the diffuser in and the other two leave it out. **AND THE SIGNED PROOF OF `check-room-mep-completeness` NEVER REACHED THE ROOM TEST.** Its positive handed in 8 ducts as the devices; a duct has a location CURVE and the code counts only a location POINT (`:94-95`), so every room came back *"short of: Ducts (0 of 1)"* whatever the model held. **Not checked**: which answer Revit gives on a real room - that needs a model.

**State.** **MOVED TO ISSUE [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not settled.** The plan says settle each with evidence, not by picking one: all three cards carry a note pointing here, and NEEDS-CHECKING AJ5 is the arrangement. The proof is the owner's signed record and is not touched - whether to re-take it with point-based devices is the owner's call **RUN 2026-09-23 on the owner's PC - [NEEDS-CHECKING AJ5](../needs-checking/group-aj.md):** exactly as the code predicts - with the room's top at the ceiling, `count-by-spatial-container` counted the void diffuser IN and the other two OUTSIDE; with its Upper Limit at the level above, all three inside. Which meaning a room should have is the owner's; asked once, not answered.

---
