# Fragment issues - the open rows, as GitHub issues

> **Since 2026-11-16, on the owner's word, every open row of sections 5 and 5b of [the register](../FRAGMENT-ISSUES.md) is tracked as a GitHub issue** - *"make it as an issues. Not in a single one... segregate and group it... easy to close, so one by one I can close it... a single simple task."* The rows were sorted that day, each one checked against the register, the archive, NEEDS-CHECKING, the handover and the code. Rows found already finished were closed in the register and given no issue; the rest went into small issues of one kind each. **A moved row stays here in full** - its state cell begins `MOVED TO ISSUE #N`, its old state kept after `Was:` - so nothing was deleted, and `python tools/open-defects.py` counts it as moved, not as finished.

## Working an issue

Every issue has a ready prompt that points here. Its kind is in its title: **Fix:**, **Revit run:** or **Your decision:**. All three start the same way:

1. Read [`AGENTS.md`](../../AGENTS.md) and follow it.
2. Read the issue. A row marked **In progress** belongs to the pull request it names: leave it alone, and tick it when that pull request merges.
3. Take the other rows one at a time. Read each in full - its file is in the table below - and check it is still real: read the file or code it names, and search the repository for its id, because a later row may have done it already. If it is already done, close it (step 5) with that evidence.

**A FIX issue** - an AI session does the work:

4. If the row's line in the issue says *Your choice*, ask the owner that question in plain words and wait. Then make the smallest fix the row asks for. Anything else found on the way is a new row (`python tools/next-id.py` gives its number), not a fix. If the C# of a PROVEN fragment changes, set it back to DRAFT, raise its version and write the Revit run it now owes ([`heron-ship`](../../.claude/skills/heron-ship/SKILL.md)). Never sign a proof.
5. Change the row's state cell to begin `FIXED <date> - <what changed, and the test or file that shows it>`, keeping everything after `Was:`. Tick the row in the issue.

**A REVIT RUN issue** - on the owner's Windows PC, with the [`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md) skill:

4. Bring the checkout up to date with `main` and run `python tools/heron-ready.py`, so Revit loads the current add-in. Read the NEEDS-CHECKING item the row names. Run the check through Heron's own tools on a **scratch copy** of a model - never a live project - with a positive case and a negative case; the owner presses Keep or Discard. Ask the owner before signing anything in his name.
5. Write what came back into the row and its NEEDS-CHECKING item - Revit release, model, date. Passed: the state begins `CLOSED <date> - run on Revit <release>, <model>`, keeping everything after `Was:`, and the row is ticked. Failed: the row keeps `MOVED TO ISSUE #N` and says what failed.

**A DECISION issue** - the owner answers in the issue's comments, one line a row:

4. Leave any row he has not answered. Write his answer into the row with the date, in his words; a rule Heron must keep is also recorded as a decision, the way [`DECISIONS.md`](../DECISIONS.md) adds one. If the answer needs a change, make the smallest one (the FIX rules above).
5. Nothing left: the state begins `ANSWERED <date> - <his answer, and what changed>`, keeping everything after `Was:`, and the row is ticked. Work left: the row keeps `MOVED TO ISSUE #N` and adds `answered <date>: <his answer>`.

**Every kind ends the same way:** `python tools/check-docs.py`, `tools/check-metadata.py`, `tools/check-structure.py`, `tools/check-package.py`, `git diff --check` and the tests the change touches, all passing; a commit on a new branch with exact paths staged, never `git add -A`; a draft pull request that says `Closes #N` only when every row in the issue is ticked.

**A row written after 2026-11-16** starts `OPEN` as every row always has. To track it on GitHub, add it to the issue whose subject it shares, or open a new one, and change its state to begin `MOVED TO ISSUE #N <date>`.

## The issues

| Issue | Kind | Subject | Rows |
|---|---|---|---|
| [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | add-in rollback and run-guard fixes | 7 |
| [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | add-in fixes for ids, categories and carried values | 5 |
| [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Heron's folders on a Microsoft Store Claude PC | 7 |
| [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | family 2D, labels and connectors | 10 |
| [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | family-building tools and family methods | 5 |
| [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | beams, columns, stairs and rebar | 11 |
| [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | door flip, levels, offset lines and radial copy | 5 |
| [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | links, imports and coordinates | 6 |
| [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | MEP and energy tools | 7 |
| [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | parameters, rooms, schedules and tags | 10 |
| [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | re-sign proofs and check new tools | 9 |
| [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | routing re-checked on your PC's trained model | 5 |
| [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | how the add-in guards changes | 6 |
| [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | dry runs, saving and Sync | 4 |
| [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | how replies show values (cut text, units) | 5 |
| [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | agents and their contracts | 9 |
| [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | proofs, signatures and the reading ledger | 6 |
| [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | decisions and Constitution wording | 7 |
| [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) | Your decision | three NEEDS-CHECKING rows that ask the wrong thing | 3 |
| [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | moving, copying, rotating and two-set checks | 10 |
| [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | columns and roofs from the villa build | 7 |
| [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | tools Heron does not have yet | 8 |
| [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | family tools | 10 |
| [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | links and linked models | 4 |
| [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | MEP, spaces and loads | 6 |
| [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | rooms and doors | 6 |
| [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | tags, schedules and the model dashboard | 6 |
| [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | the CSV round trip and sheets from Excel | 4 |
| [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | library tidy-up (domains, status words, revisions) | 4 |
| [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | chat side (which Revit, try-once, tool sizes) | 7 |
| [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | what 'put it back' means | 5 |
| [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | structural and settings questions that reach a change | 8 |
| [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | more questions that reach a change | 9 |
| [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | lookup words on older Revit and with the re-ranker | 9 |
| [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | add-in bridge and reply reliability | 5 |
| [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | add-in binds the right model, elements and inputs | 8 |
| [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | run records, audit lines and outcome words in the add-in | 8 |
| [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | view pictures, camera framing and sheets with no geometry | 4 |
| [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | brain small fixes | 6 |
| [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | stale wording in documents and registers | 8 |
| [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | register tools that crash or miscount | 7 |
| [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | check-gaps, timeouts and flaky tests | 5 |
| [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | checks that miss things | 6 |
| [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | jobs Revit refuses inside Heron's transaction | 5 |
| [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | duct and pipe justification does nothing | 5 |
| [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | MEP accessories, circuits, zones and labels | 4 |
| [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | family build and flex | 10 |
| [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | family reports, reload and settings | 6 |
| [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | link tools that report wrong | 6 |
| [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | parameters that share a name | 7 |
| [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | floors, doors, roofs, parts and clearance | 7 |
| [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | rebar, materials and structural loads | 5 |
| [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | leftovers from the duplicate merges | 4 |
| [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | view filters, tags, spaces and IFC export input | 7 |
| [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | chat-side outcome words that are wrong | 7 |
| [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | MCP server small fixes | 7 |
| [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | plain questions that land on a change (part 1) | 6 |
| [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | plain questions that land on a change (part 2) | 6 |
| [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | requests that miss their tool | 10 |
| [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | lookup search engine | 6 |
| [#958](https://github.com/Ajmalpshaik/Heron-AI/issues/958) | Code fix | routing tests that fail with the trained model | 3 |
| [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | skills - the runner, skill words and risk | 7 |
| [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | job methods to correct | 6 |

## Row by row

Every row of sections 5 and 5b that `python tools/open-defects.py` listed as open on 2026-11-16, and the nine rows (5b-1780 to 5b-1832) that pull requests merged later that day added. A bare number is a section 5 row.

| Row | Issue | Kind | What is wrong |
|---|---|---|---|
| [row 10](section-5-rows-001-025.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | Heron could say nothing was kept even when Revit's rollback silently failed |
| [row 18](section-5-rows-001-025.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Setting duct justification offsets does nothing: Revit keeps the old value |
| [row 19](section-5-rows-001-025.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | A preview of a view rename was reported rolled back, but the views stayed renamed |
| [row 21](section-5-rows-001-025.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Export families can never run: Revit will not open a family inside Heron's transaction |
| [row 36](section-5-rows-026-050.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Sheet list name-clash warning was thought unreachable since a schedule can share a name |
| [row 41](section-5-rows-026-050.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Some tools report what they reached, not what they did, so empty-result proofs fail |
| [row 45](section-5-rows-026-050.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Duct justification offsets only accept 0 mm; any other value is silently discarded |
| [row 73](section-5-rows-051-075.md) | [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | Unloading links changes the model with no undo, even in a dry run |
| [row 75](section-5-rows-051-075.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Elements from a linked model can bind the wrong host elements in the next step |
| [row 99](section-5-rows-076-100.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Rule against tools opening a transaction blocks editing a second document like a family |
| [row 101](section-5-rows-101-125.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Optional inputs on some tools are still refused when left out |
| [row 103](section-5-rows-101-125.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Clearance check takes a typed table of rules, but the decision allowing it is unsigned |
| [row 106](section-5-rows-101-125.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Four clearance checks compare every element with every element; may be slow on big models |
| [row 108](section-5-rows-101-125.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | A one-line refusal declared as a list is cut to 60 characters and hides the reason |
| [row 116](section-5-rows-101-125.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | Some plain questions still reach a tool that changes the model instead of a read |
| [row 120](section-5-rows-101-125.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | Several tools still cut their refusal text to 60 characters; fixing them stales proofs |
| [row 128](section-5-rows-126-150.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | A column family asked to go in as a beam was placed anyway |
| [row 129](section-5-rows-126-150.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | The same two-set check runs one way and is refused the other, so hand runs mislead |
| [row 131](section-5-rows-126-150.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | Shared lookup index can come from another checkout, so a phrase may run a tool unasked |
| [row 132](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | Most skill phrases do not reach the tools the skill says it needs |
| [row 135](section-5-rows-126-150.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Two-set tools can only compare a set with itself; no way to give a second, different set |
| [row 137](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | Asking pendent or upright in sprinkler layout reaches an unrelated drawing tool |
| [row 140](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | Three skills declare a lower risk than the tools they use |
| [row 141](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | Nothing can run and prove a skill's whole plan, so no skill has ever been proved |
| [row 142](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | Trace-system skill lists a report step its own plan can never feed |
| [row 143](section-5-rows-126-150.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Register's sort of failed drafts is stale and can only be checked on the owner's PC |
| [row 144](section-5-rows-126-150.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | A wider question list found more plain questions answered by tools that change the model |
| [row 146](section-5-rows-126-150.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | Plain questions like view scale or duct size were answered by tools that change the model |
| [row 148](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | A skill plan runs the next step after a failed one and counts a stale selection |
| [row 150](section-5-rows-126-150.md) | [#959](https://github.com/Ajmalpshaik/Heron-AI/issues/959) | Code fix | Skill catalogue showed proven chains while most skills' own sentences reach other tools |
| [row 158](section-5-rows-151-175.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Tools that change the model claim plain questions, so those questions land on a write |
| [row 176](section-5-rows-176-200.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Owner's own read questions, like ping the Revit model, reach tools that change the model |
| [row 177](section-5-rows-176-200.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | A thin cap flipped sides between build and parameter link, so its connector pointed inward |
| [row 179](section-5-rows-176-200.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | No tool sets detail levels on a nested family instance, so a handwheel shows in Coarse |
| [row 180](section-5-rows-176-200.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Radius label tool refuses one parameter on two circles, forcing twin L and R parameters |
| [row 5b-83](section-5b-rows-076-100.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | Most brain modules are reached only by their own tests, never from a conversation |
| [row 5b-95](section-5b-rows-076-100.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | Six refusals the code gives are missing from their agents' contracts |
| [row 5b-103](section-5b-rows-101-125.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | The reading ledger signs cloud sessions' reads with the owner's PC name |
| [row 5b-105](section-5b-rows-101-125.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | Compatibility matrix shows zero proven on every release despite hundreds of proofs |
| [row 5b-107](section-5b-rows-101-125.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | Embedding search compares vectors from different backends whenever their lengths match |
| [row 5b-110](section-5b-rows-101-125.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | The narrow-errors gate checks one exception form, so broad handlers can swallow faults |
| [row 5b-113](section-5b-rows-101-125.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | A sub-clause citation like [7a] is treated as a measured value and reported as invented |
| [row 5b-125](section-5b-rows-101-125.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Decision D-72 has no status line, so the decision index says status not stated |
| [row 5b-131](section-5b-rows-126-150.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | Signature gate counts a machine's name in a proof's signed-by field as a signature |
| [row 5b-136](section-5b-rows-126-150.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | The metadata standard never defines what each Heron-Layer value means |
| [row 5b-145](section-5b-rows-126-150.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | Agents claimed only by a test suite are counted as built, and two tools disagree |
| [row 5b-159](section-5b-rows-151-175.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | The audit trail did not record the warnings Revit raised on a fragment change |
| [row 5b-161](section-5b-rows-151-175.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | Destructive changes like purge, reloads, worksets and links run without asking first |
| [row 5b-175](section-5b-rows-151-175.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Three room tools disagree on whether a ceiling-void device is inside the room |
| [row 5b-180](section-5b-rows-176-200.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Two level tools read level elevation and project elevation the opposite way round |
| [row 5b-182](section-5b-rows-176-200.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | Five Python files find Windows special folders themselves and the gate cannot see it |
| [row 5b-185](section-5b-rows-176-200.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Docs cite D-48 as the brain-may-not-import-mcp rule, but D-48 says nothing on layers |
| [row 5b-187](section-5b-rows-176-200.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | Air terminal connect lifted diffusers onto ducts, made no tap, joined supply to return |
| [row 5b-194](section-5b-rows-176-200.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | Chat replies cut each list line at 60 characters and show dictionaries only as a count |
| [row 5b-197](section-5b-rows-176-200.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | Atomic file save can fail on Windows when another program briefly holds the file |
| [row 5b-200](section-5b-rows-176-200.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | Re-ranker download notice understates the size and says not installed when weights lack |
| [row 5b-202](section-5b-rows-201-225.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Copy-from-link is handed the host model's ids, not the linked elements chosen |
| [row 5b-204](section-5b-rows-201-225.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Sealed fragment code comments still say lengths are feet though input is millimetres |
| [row 5b-203](section-5b-rows-201-225.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | Tools pick a random parameter when two share one name, giving wrong reads or writes |
| [row 5b-205](section-5b-rows-201-225.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | The fragment generator stamps its own agent id on every fragment it makes |
| [row 5b-211](section-5b-rows-201-225.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Placing a door type at a wall point puts it in free-standing and reports success |
| [row 5b-212](section-5b-rows-201-225.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Door report treats a flipped door as wrong, but flipping does not change From/To Room |
| [row 5b-213](section-5b-rows-201-225.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Asking which rooms are untagged goes to a tool that tags, and no read finds room tags |
| [row 5b-214](section-5b-rows-201-225.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | A request to name rooms is sent to tools that place rooms or tags, not one that names |
| [row 5b-215](section-5b-rows-201-225.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Renumbering could not zero-pad numbers and a trailing space in a prefix was lost |
| [row 5b-216](section-5b-rows-201-225.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Rooms tagged with ordinary tags; not known if they count as Revit room tags |
| [row 5b-219](section-5b-rows-201-225.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | Four design values on MEP spaces are read-only and nothing sets a space type |
| [row 5b-221](section-5b-rows-201-225.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | Element ids could not be typed into a request, only taken from the selection |
| [row 5b-222](section-5b-rows-201-225.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | A flow written by name landed but reading by the same name showed the old value |
| [row 5b-224](section-5b-rows-201-225.md) | [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | Six plain questions about zones, room tops and terminals land on tools that change |
| [row 5b-225](section-5b-rows-201-225.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | No read shows an HVAC zone's settings, and zone outdoor air is ignored unless set |
| [row 5b-226](section-5b-rows-226-250.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | A new flow-writing tool pulled flow questions away from the reads that answer them |
| [row 5b-227](section-5b-rows-226-250.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Cards mark some inputs optional but the add-in still refuses them when left out |
| [row 5b-230](section-5b-rows-226-250.md) | [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | Questions listing mullion types still reach the tool that sets mullions |
| [row 5b-233](section-5b-rows-226-250.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | The lookup answered from whichever checkout last rebuilt the shared store |
| [row 5b-235](section-5b-rows-226-250.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | Two chats on one Revit overwrote each other's carried values between steps |
| [row 5b-240](section-5b-rows-226-250.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | Tag arranging takes its gap in model millimetres while its card says paper |
| [row 5b-246](section-5b-rows-226-250.md) | [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | Undeclared revision questions reach tools that delete or change revision settings |
| [row 5b-253](section-5b-rows-251-275.md) | [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | First batch of reads that look inside linked models: built, results never recorded |
| [row 5b-256](section-5b-rows-251-275.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Second batch of linked-model reads: built, group checks never written back |
| [row 5b-257](section-5b-rows-251-275.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Third batch of linked-model reads for MEP coordination: group checks not recorded |
| [row 5b-258](section-5b-rows-251-275.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Fourth batch of link checks; the gate's link worklist misses tools fed elements |
| [row 5b-259](section-5b-rows-251-275.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Fifth batch: ray and clearance checks that now see links, never run as a group |
| [row 5b-260](section-5b-rows-251-275.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Linking a family parameter to a connector size may be refused where Revit allows it |
| [row 5b-265](section-5b-rows-251-275.md) | [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | Family-from-a-photo method written but never followed in a real chat |
| [row 5b-278](section-5b-rows-276-300.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | Workforce tests read the live agent register, so new agent wording can turn them red |
| [row 5b-280](section-5b-rows-276-300.md) | [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | New family-kind tools ran, but the seven family skills were never followed |
| [row 5b-281](section-5b-rows-276-300.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Size table card says the first column is the key; sources say it holds row names |
| [row 5b-282](section-5b-rows-276-300.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Connector said to sit at a face's centre; Autodesk says it may sit at plane origin |
| [row 5b-285](section-5b-rows-276-300.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | Family settings count a setting Revit changed by itself as asked and changed |
| [row 5b-286](section-5b-rows-276-300.md) | [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | Door, window, structural and lighting family skills written but never run |
| [row 5b-287](section-5b-rows-276-300.md) | [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | Divide surface, revolve and beam settings built and proven; their checks unrecorded |
| [row 5b-289](section-5b-rows-276-300.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | No tool drew room separation lines, so an open archway could not split a room |
| [row 5b-294](section-5b-rows-276-300.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Doors placed before rooms exist are kept unchecked and may swing the wrong way |
| [row 5b-295](section-5b-rows-276-300.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Family forms report shows a solid with a void cut in it as 0 litres |
| [row 5b-296](section-5b-rows-276-300.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Touching profile loops give only Revit's unclear internal error |
| [row 5b-297](section-5b-rows-276-300.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Connector tool refused Revit's own fire protection system names |
| [row 5b-299](section-5b-rows-276-300.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | No tool could make a stair; now it can, but the slab opening was never run |
| [row 5b-301](section-5b-rows-301-325.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Placing a nested family with its own offset is wrongly treated as a failure |
| [row 5b-302](section-5b-rows-301-325.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Linking a nested family's type to a family parameter is refused by Revit 2024 |
| [row 5b-303](section-5b-rows-301-325.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Divided surface report gives each grid the other direction's line count |
| [row 5b-304](section-5b-rows-301-325.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | An unsaved family shows an empty name in two family reports |
| [row 5b-314](section-5b-rows-301-325.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Loads panel Finalize leaves two undo entries instead of one |
| [row 5b-320](section-5b-rows-301-325.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | The trained search reads only the start of long cards, so openings decide routing |
| [row 5b-321](section-5b-rows-301-325.md) | [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | Questions about doors and windows in a wall still reach tools that change the model |
| [row 5b-331](section-5b-rows-326-350.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | Loads use Revit's layer-only U-value and round people down to whole persons |
| [row 5b-336](section-5b-rows-326-350.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Room data card says spaces get a volume without the project switch; measured not |
| [row 5b-337](section-5b-rows-326-350.md) | [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | Questions about how room area is measured still reach tools that change the model |
| [row 5b-338](section-5b-rows-326-350.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | A sprinkler above its space's top is reported as in no space |
| [row 5b-340](section-5b-rows-326-350.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Two tools describe a level's elevation in opposite ways |
| [row 5b-342](section-5b-rows-326-350.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | A parameter fetched by built-in id can exist yet not belong to the element |
| [row 5b-346](section-5b-rows-326-350.md) | [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) | Code fix | Questions about the project's site location still reach the tool that sets it |
| [row 5b-348](section-5b-rows-326-350.md) | [#958](https://github.com/Ajmalpshaik/Heron-AI/issues/958) | Code fix | A retrieval test fails on the owner's PC with the trained model, on main too |
| [row 5b-350](section-5b-rows-326-350.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | A read-only run of nested family placement left a real element in the family |
| [row 5b-356](section-5b-rows-351-375.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | A true north question went to the site setter after both setters were signed |
| [row 5b-358](section-5b-rows-351-375.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Arrayed family forms are model groups even for one form; card said only several are |
| [row 5b-361](section-5b-rows-351-375.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | A reply is lost when a Revit window opens mid-call and another connection takes the pipe |
| [row 5b-364](section-5b-rows-351-375.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Radius labels refuse two extrusions or revolves sharing one parameter |
| [row 5b-367](section-5b-rows-351-375.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Flexing a family never checks nested families, so a nested part that fails passes |
| [row 5b-378](section-5b-rows-376-400.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | First family on a new laptop took 6-13 minutes; office PC comparison never made |
| [row 5b-396](section-5b-rows-376-400.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Questions about what an element is made of still reach tools that change materials |
| [row 5b-402](section-5b-rows-401-425.md) | [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | The MCP serving test runs past check-gaps' 300 second limit in a cloud container |
| [row 5b-403](section-5b-rows-401-425.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Companion page actions guess how a Revit call ended, or call a lost answer refused |
| [row 5b-414](section-5b-rows-401-425.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | Revision numbering: a letters question reaches a write, and a harmless change is refused |
| [row 5b-415](section-5b-rows-401-425.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | Windows on the owner's laptop blocks freshly built test programs |
| [row 5b-416](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Revit's Companion button looked for chat notes where a Store Claude never writes them |
| [row 5b-418](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | On a Store Claude PC, files Heron writes under AppData land in the app's private copy |
| [row 5b-419](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Resetting a Store Claude app deleted Heron's data kept in its private copy |
| [row 5b-420](section-5b-rows-401-425.md) | [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | The heron-ready test failed 3 of 16 runs at random and kept no output to say why |
| [row 5b-394](section-5b-rows-401-425.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | A material routing note says other cards repeat its rows, but none of them does |
| [row 5b-395](section-5b-rows-401-425.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | Two routing notes send 'what is this wall made of' to different tools |
| [row 5b-423](section-5b-rows-401-425.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | On Revit 2026 and 2027 no OmniClass code can be set: Autodesk ships only a sample file |
| [row 5b-424](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'Set the keynote of this type' reaches a read, not the tool that sets type values |
| [row 5b-427](section-5b-rows-401-425.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | The server adds the Companion folder to Python's path again on every tool call - *in progress in #871* |
| [row 5b-428](section-5b-rows-401-425.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | Each change or read call re-reads every card file to find one capability |
| [row 5b-434](section-5b-rows-401-425.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | 'How many furniture are there' was answered by a room counter or a write |
| [row 5b-437](section-5b-rows-401-425.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | An OmniClass number Heron set and read back was blank again minutes later |
| [row 5b-444](section-5b-rows-401-425.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | A category typed by its shown name could bind a different category (Railings) |
| [row 5b-448](section-5b-rows-401-425.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | A sub-category like Rooms: Interior Fill could not be named to any category tool |
| [row 5b-455](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | The stair tool said it makes no railing, but Revit adds two with every stair |
| [row 5b-472](section-5b-rows-401-425.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | Quality checks sit under two domains and levels and grids under four |
| [row 5b-473](section-5b-rows-401-425.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | Nothing reports a sentence that two cards declare, which breaks exact matching |
| [row 5b-474](section-5b-rows-401-425.md) | [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | No read reports a link's room bounding or path type, so the questions reach writes |
| [row 5b-475](section-5b-rows-401-425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | Nothing opens a model detached from central or with audit |
| [row 5b-476](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'Line the viewports up' reaches a read; a room-area question reached a write |
| [row 5b-477](section-5b-rows-401-425.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Plain modeller questions still reach tools that change the model (first 60) |
| [row 5b-478](section-5b-rows-401-425.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Plain modeller questions still reach tools that change the model (second 60) |
| [row 5b-490](section-5b-rows-401-425.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | 'Describe these elements' reached a write that renumbers them |
| [row 5b-496](section-5b-rows-401-425.md) | [#958](https://github.com/Ajmalpshaik/Heron-AI/issues/958) | Code fix | With the trained model installed, two routing suites fail on main and CI never sees it |
| [row 5b-513](section-5b-rows-401-425.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | Heron cannot save the model it is working in; two unsaved builds were lost |
| [row 5b-650](section-5b-rows-401-425.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | Builders work layout method is a draft with gaps and a word-matching bug |
| [row 5b-665](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Two code comments still say the duct move was never measured |
| [row 5b-666](section-5b-rows-401-425.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | 'Move the ducts up 200 mm' moves every duct in the model, not the selection |
| [row 5b-861](section-5b-rows-401-425.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Tools that make copies put them on the active workset and cannot be told otherwise |
| [row 5b-940](section-5b-rows-401-425.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | General notes update method is a draft; three edit pitfalls were recorded |
| [row 5b-977](section-5b-rows-401-425.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | A read-level job could still commit its own transaction and keep the change |
| [row 5b-1003](section-5b-rows-401-425.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Opening a family by a backslash path once said there was no file |
| [row 5b-1004](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | A Store Claude keeps its own copy of Heron's settings, so Revit and Heron can disagree |
| [row 5b-1007](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Asking to read a text note's wording reaches a date reader or a write, not the note read |
| [row 5b-1060](section-5b-rows-401-425.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Two models from one template look like one model, so the move refuses for the wrong reason |
| [row 5b-1061](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | One move writes two audit lines under two workflow ids, so they cannot be joined |
| [row 5b-1062](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | A refused move says neither what Revit objected to nor that the move was rolled back |
| [row 5b-1063](section-5b-rows-401-425.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Save with close says rolled back, nothing kept - but the window really was closed |
| [row 5b-1065](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Installer and heron-ready disagree on package versions for a Store Claude PC |
| [row 5b-1066](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Installer refusal says installs from inside Claude land in the copy - not always true |
| [row 5b-1090](section-5b-rows-401-425.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | Picturing chosen elements always failed: the reply read a view already rolled back |
| [row 5b-1091](section-5b-rows-401-425.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | The run guard refused families opened for editing, so two proven tools stopped working |
| [row 5b-1092](section-5b-rows-401-425.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | A cancelled save of another model was reported as a change Heron cannot undo |
| [row 5b-1095](section-5b-rows-401-425.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | Twenty calls at once lose 2 to 7 answers, not the 1 or 2 doc 25 claims, and once hung |
| [row 5b-1100](section-5b-rows-401-425.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | The Companion button looked in Revit's own temp folder and said no chat was running |
| [row 5b-1110](section-5b-rows-401-425.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | Long proving runs fill Revit 2020's memory until jobs fail and the chat crashes |
| [row 5b-1112](section-5b-rows-401-425.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Setting duct justification on Revit 2020 sets nothing, and a no-change counts as set |
| [row 5b-1113](section-5b-rows-401-425.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Placing rooms on a level with no plan view throws instead of refusing by name |
| [row 5b-1114](section-5b-rows-401-425.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | batch-prove calls a tool whose one result is a single object unreadable |
| [row 5b-1115](section-5b-rows-401-425.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Move annotation says it moves text notes, but a text note comes back not movable |
| [row 5b-1118](section-5b-rows-401-425.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Placing rooms on a finished level makes two extra unbounded rooms |
| [row 5b-1119](section-5b-rows-401-425.md) | [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | Putting a damper or valve into a lone duct or pipe on Revit 2020 always rolls back |
| [row 5b-1121](section-5b-rows-401-425.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Rolled-back link runs leave the linked file open, so later links read as not loaded |
| [row 5b-1122](section-5b-rows-401-425.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Link coordinate check puts 'not loaded - not checked' in its result, so it reads as a pass |
| [row 5b-1124](section-5b-rows-401-425.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Linking a Revit file makes a link that never shows its model, yet says Loaded |
| [row 5b-1125](section-5b-rows-401-425.md) | [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | Spaces made from linked rooms come back with no area but are still counted as made |
| [row 5b-1126](section-5b-rows-401-425.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Moving a Revit link answers moved 0 though it did move, inviting a second move |
| [row 5b-1130](section-5b-rows-401-425.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | HVAC and fire design contracts name refusal words the code never sends |
| [row 5b-1131](section-5b-rows-401-425.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | Two places choose which tool provides a capability by different rules - *in progress in #871* |
| [row 5b-1132](section-5b-rows-401-425.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | The which-Revit list shows process numbers and no project name |
| [row 5b-1134](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Four code comments still describe Heron as it was before D-129, D-132 and D-135 |
| [row 5b-1136](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Group EQ's command counts a Revit 2022 proof that does not exist |
| [row 5b-1138](section-5b-rows-401-425.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Two credential lists disagree, and commit staging never checks staged files for secrets |
| [row 5b-1153](section-5b-rows-401-425.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | A proof's fingerprint covers only the code, not the card, the release or the path it ran |
| [row 5b-1155](section-5b-rows-401-425.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | Only the Python side enforces proven-only changes; the add-in runs any code it is sent |
| [row 5b-1156](section-5b-rows-401-425.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | The MCP server runs the checkout it started in and nothing says which folder or commit |
| [row 5b-1157](section-5b-rows-401-425.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | When the only Revit closes, Heron quietly binds to another, against Article 12a |
| [row 5b-1158](section-5b-rows-401-425.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | Some reads hand back lengths and areas in feet and square feet with no unit |
| [row 5b-1159](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Still-running answers carry no workflow id and wrongly say the run is not recorded |
| [row 5b-1161](section-5b-rows-401-425.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | revit_change runs reads as writes; select all ducts sits on a read that selects nothing - *in progress in #871* |
| [row 5b-1162](section-5b-rows-401-425.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Values the AI passes are filed as the modeller's own word and never asked again |
| [row 5b-1163](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | A run record still says KEPT after the modeller undid the change |
| [row 5b-1164](section-5b-rows-401-425.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Values carried between tool steps are matched to a model by title, which files can share |
| [row 5b-1166](section-5b-rows-401-425.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Proof validate runs of changes and publishes are resent when an answer is lost - *in progress in #871* |
| [row 5b-1167](section-5b-rows-401-425.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | The add-in can send an answer of any size over the pipe; a lost read is still called safe |
| [row 5b-1168](section-5b-rows-401-425.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | Developer files load in a modeller's chat, and tool descriptions sit at their size limit |
| [row 5b-1170](section-5b-rows-401-425.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | check-docs passes links to emoji headings that GitHub does not resolve - *in progress in #871* |
| [row 5b-1171](section-5b-rows-401-425.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | The vector search speed-up is never loaded, though D-23 says it is used - *in progress in #871* |
| [row 5b-1172](section-5b-rows-401-425.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | The rebuild command says it restores every store, but it restores only the global one - *in progress in #871* |
| [row 5b-1173](section-5b-rows-401-425.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | Two register tools file the same row under different needs - *in progress in #871* |
| [row 5b-1174](section-5b-rows-401-425.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | prove-agent's own usage line for track cannot run: it leaves out a required option - *in progress in #871* |
| [row 5b-1175](section-5b-rows-401-425.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Code already runs on two decisions still marked Proposed: D-112 and D-113 |
| [row 5b-1176](section-5b-rows-401-425.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | D-138 says two different things about asking before a publish when Ask to Keep is off |
| [row 5b-1177](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Three places still word Heron as it was before D-27 and the standards tool |
| [row 5b-1178](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The needs-checking index page still holds rows that belong in group files - *in progress in #871* |
| [row 5b-1179](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Two struck checks said the emergency stop could not be set; since D-129 it can - *in progress in #871* |
| [row 5b-1180](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The owner's ask for one numbered specification document has no register row |
| [row 5b-1181](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The register file holds rows far outside its band of 25 |
| [row 5b-1201](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Placing beams at points made broken zero-length beams and reported them as placed |
| [row 5b-1220](section-5b-rows-401-425.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The decision log's preamble cites the wrong Golden Rule for its append-only rule - *in progress in #871* |
| [row 5b-1221](section-5b-rows-401-425.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Renaming a level always rolled back: Heron declined Revit's rename-views question |
| [row 5b-1242](section-5b-rows-401-425.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Flipping a door to face its room left its From and To Room the old way round |
| [row 5b-1261](section-5b-rows-401-425.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Arraying family forms on planes a parameter moves breaks the array at other sizes |
| [row 5b-1267](section-5b-rows-401-425.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Revit's API cannot run a render or reach some display, sky and camera settings |
| [row 5b-1292](section-5b-rows-401-425.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | A combined schedule column put the separator after the last part and could not be edited |
| [row 5b-1293](section-5b-rows-401-425.md) | [#958](https://github.com/Ajmalpshaik/Heron-AI/issues/958) | Code fix | Routing test checks fail with the trained model installed, and CI cannot see them |
| [row 5b-1324](section-5b-rows-401-425.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | A void Heron makes in a floor-based family cuts no floor, and no Revit API call can |
| [row 5b-1329](section-5b-rows-401-425.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | Column rebar tool places no ties in round concrete columns; Revit says internal error |
| [row 5b-1335](section-5b-rows-401-425.md) | [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | Flip tool could not turn a nested part placed upside down on a reversed work plane |
| [row 5b-1340](section-5b-rows-401-425.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | An add-in agent cannot be promoted: editing its status line stales its own proof |
| [row 5b-1347](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'T10 at 150 both faces of these walls' reaches the wall-by-face tool, not area rebar |
| [row 5b-1351](section-5b-rows-401-425.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Family model text tool reads letters back as a metre deep when 1 mm was asked |
| [row 5b-1355](section-5b-rows-401-425.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | Model dashboard counts three categories all named Center line, 43% of its total |
| [row 5b-1362](section-5b-rows-401-425.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | After five duplicate merges, the element ownership report v2 is still unproven |
| [row 5b-1363](section-5b-rows-401-425.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Leftovers of sixteen duplicate merges: questions on writes, old names in code |
| [row 5b-1364](section-5b-rows-401-425.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Merge gaps left: commas split filter names, no quote rule, one copy lacks has-no-value |
| [row 5b-1365](section-5b-rows-401-425.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Leftovers of 49 duplicate merges: three keepers unproven, old names, questions on writes |
| [row 5b-1366](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | After thirty more merges, 'select walls of type' and other questions reach writes |
| [row 5b-1368](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | 'How much concrete is in the walls', a question, reaches an area rebar write |
| [row 5b-1373](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | After more merges, 'which element cuts which in this join' reaches the join write |
| [row 5b-1375](section-5b-rows-401-425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' reaches a write that the show-elements tool does not name |
| [row 5b-1376](section-5b-rows-401-425.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Radial array was merged into the copy tool; its circular copy cases never ran in Revit |
| [row 5b-1378](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | 'Space these round a point' reaches a room line write, not the copy tool |
| [row 5b-1381](section-5b-rows-401-425.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | On Revit 2023-2026 a pinned support request reaches the attach-columns write |
| [row 5b-1382](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | Probe questions like 'which circuit is this light on' reach new write tools |
| [row 5b-1383](section-5b-rows-401-425.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Create floor sends a foundation slab type through as a plain floor, unchecked |
| [row 5b-1386](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | 'Is this connection approved' reaches the steel connection write |
| [row 5b-1388](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | On older Revit, 'make a PDF of the drawings' reaches image import, a write |
| [row 5b-1389](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | On Revit 2020 a circuit naming scheme request reaches the circuit edit write |
| [row 5b-1391](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | 'Colour code the ducts by system' and 'add a pipe legend' reach the wrong tools |
| [row 5b-1395](section-5b-rows-401-425.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | 'Select all the ducts' reached a write when the question flag was not set |
| [row 5b-1397](section-5b-rows-401-425.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Set MEP justification changes nothing: every duct refuses both offsets |
| [row 5b-1398](section-5b-rows-401-425.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Moving elements by 0,0,0 reports them as moved |
| [row 5b-1399](section-5b-rows-401-425.md) | [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | Camera framing tool 'framed' sheets, which have no 3D extent, and made a view |
| [row 5b-1400](section-5b-rows-401-425.md) | [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | Minimum clearance check reported every pair of sheets as too close |
| [row 5b-1402](section-5b-rows-401-425.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | IFC export cannot run: its schema value cannot be typed in a request |
| [row 5b-1404](section-5b-rows-401-425.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | Listing materials with all tabs crashed Revit 2020 with a memory error |
| [row 5b-1405](section-5b-rows-401-425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved again from one wrong write to another |
| [row 5b-1406](section-5b-rows-401-425.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | A single text note id handed to a tool wanting a list crashed with a cast error |
| [row 5b-1407](section-5b-rows-401-425.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Export families saved nothing: Revit refused to open each family for editing |
| [row 5b-1408](section-5b-rows-401-425.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Reload links reloaded nothing: Revit refuses it inside Heron's transaction group |
| [row 5b-1409](section-5b-rows-401-425.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Set MEP justification refuses every duct on Revit 2024 too |
| [row 5b-1411](section-5b-rows-401-425.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Export families is refused on Revit 2024 too: the proof route is the cause |
| [row 5b-1413](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | A units question about cooling load reaches the heating and cooling loads write |
| [row 5b-1417](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | 'Which one is cutting' reaches neither the join report nor any card claiming it |
| [row 5b-1420](section-5b-rows-401-425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No tool can read or set keynote numbering; Revit's API has nothing for it |
| [row 5b-1423](section-5b-rows-401-425.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | No Heron route can Reload Latest or Sync: Revit refuses both inside a transaction group |
| [row 5b-1424](section-5b-rows-401-425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved a third time from one wrong write to another |
| [row 5b-1426](section-5b-rows-401-425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved again, and made-up line style questions reach a write |
| [row 5b-1427](section-5b-rows-401-425.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | List categories reads no material, so a material question gets no material |
| [row 5b-1428](section-5b-rows-401-425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved a fifth time from one write to another |
| [row 5b-1429](section-5b-rows-401-425.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | A copy between projects rolls back whole when a type name already exists here |
| [row 5b-1431](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | 'What combinations are in this model' reaches the load combination write |
| [row 5b-1433](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Four made-up structural settings questions reach the new settings writes |
| [row 5b-1435](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Three made-up analytical model questions reach the analytical writes |
| [row 5b-1440](section-5b-rows-401-425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No tool can link an IFC file; a fragment cannot run Revit's IFC importer |
| [row 5b-1441](section-5b-rows-401-425.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Point clouds link origin to origin only; by shared coordinates is named, not linked |
| [row 5b-1443](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | 'Which images need reloading' and point cloud questions reach writes |
| [row 5b-1445](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Beam justification numbers are assumed; which number means which is undocumented |
| [row 5b-1446](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Stair tread numbers pick tread or riser by trial; the 2027 start number is unknown |
| [row 5b-1447](section-5b-rows-401-425.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Remove project parameter deletes or unbinds without seeing what Revit's Remove does |
| [row 5b-1448](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Made-up stair and beam sentences miss their tools; two stair questions reach a write |
| [row 5b-1449](section-5b-rows-401-425.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Mechanical settings annotation sizes assumed to be lengths; rise/drop symbols unreachable |
| [row 5b-1450](section-5b-rows-401-425.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Energy settings: the glazing form and what the energy model switch does are unknown |
| [row 5b-1451](section-5b-rows-401-425.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Create energy model does not know what a second run does to an existing one |
| [row 5b-1452](section-5b-rows-401-425.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | gbXML import runs inside Heron's transaction; whether Revit allows it is unseen |
| [row 5b-1453](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Made-up settings questions reach settings writes; two requests miss their tool |
| [row 5b-1454](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Rebar shape bend kind and leg lengths are assumed, not checked against Revit |
| [row 5b-1455](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Reinforcement symbols: the type category and plan or section behaviour are unknown |
| [row 5b-1456](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Sketched stairs draw one riser line per riser; whether Revit agrees is unseen |
| [row 5b-1457](section-5b-rows-401-425.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Made-up rebar and stair questions reach the new write tools |
| [row 5b-1458](section-5b-rows-401-425.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Offset line chain: the joins and which side the sign goes have not been seen |
| [row 5b-1459](section-5b-rows-401-425.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Insert views from file opens another project inside Heron's transaction, unseen |
| [row 5b-1460](section-5b-rows-401-425.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Systems analysis runs in the background; what Discard does to it is unknown |
| [row 5b-1461](section-5b-rows-401-425.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Publish Coordinates never saves the linked file; what Revit does at next save is unknown |
| [row 5b-1462](section-5b-rows-401-425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | Questions like 'where is it now' reach a write tool when the question flag is off |
| [row 5b-1477](section-5b-rows-401-425.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | New tag-many-views tool overlaps two proven tag tools and is not yet run in Revit |
| [row 5b-1478](section-5b-rows-401-425.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | New MEP dimension-many-views tool overlaps proven dimension tools, not yet run in Revit |
| [row 5b-1480](section-5b-rows-401-425.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Work plane report names a plan's level in quotes, not as 'Level : Level 1' as promised |
| [row 5b-1481](section-5b-rows-401-425.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | New CAD-lines-to-ducts tool is untested in Revit; one size question routes to it |
| [row 5b-1482](section-5b-rows-401-425.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Heron cannot change an existing dimension label, so window width/height stayed type-driven |
| [row 5b-1483](section-5b-rows-401-425.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | Trying a draft tool once was refused as 'in use' by the same chat's own lease |
| [row 5b-1484](section-5b-rows-401-425.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | Reloading a window family wanted to delete 24 elements, and why is unknown |
| [row 5b-1485](section-5b-rows-401-425.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | Family job hit draft tools (export families, open documents); windows not found in rooms |
| [row 5b-1486](section-5b-rows-401-425.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | New sheets-from-spreadsheet tool proven; owner choices on blanks, dates and limits left |
| [row 5b-1488](section-5b-rows-401-425.md) | [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | MEP schematic pressure-drop labels show 0.00 on every run; rounding or a wrong read |
| [row 5b-1489](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | New rebar-against-spec check tool is not yet run in Revit |
| [row 5b-1493](section-5b-rows-401-425.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Tool to lock family 2D lines to planes is proven; the by-hand second check is not run |
| [row 5b-1496](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Family 2D read is proven; two sentences still route to a lock tool, not re-measured |
| [row 5b-1497](section-5b-rows-401-425.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Linking a family's 2D Visible to a Yes/No is proven; the kept flex check is not run |
| [row 5b-1498](section-5b-rows-401-425.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Labelling a family angle is proven; which plane turns on flex is still not checked |
| [row 5b-1499](section-5b-rows-401-425.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | A detail item cannot be placed in a family's own view; Revit 2020 refuses it |
| [row 5b-1505](section-5b-rows-401-425.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | Tools cannot mark which outputs are refusals, so repeat 'blocked' shows as unchanged |
| [row 5b-1507](section-5b-rows-401-425.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | New click-in-Revit tool is not run, and a click after 60 seconds reaches nobody |
| [row 5b-1510](section-5b-rows-401-425.md) | [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | Two test suites can ping the live Revit and send it a release message |
| [row 5b-1511](section-5b-rows-401-425.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | heron_resolve says Heron keeps no list of missing things, but it does now |
| [row 5b-1512](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | 'put the duct tags above the ducts with a leader' goes to the tag-placing tool |
| [row 5b-1513](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | With re-ranker, five rename and place-view sentences reach the wrong tool |
| [row 5b-1514](section-5b-rows-401-425.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | Asking if units are metric or imperial reaches the tool that changes units |
| [row 5b-1515](section-5b-rows-401-425.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Divide parts reports every part as divided even when the datum cut none |
| [row 5b-1517](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Without the re-ranker, eight routing guard sentences reach the wrong tool |
| [row 5b-1518](section-5b-rows-401-425.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Five new job methods (issue drawings, health audit, set-up...) are not run in Revit |
| [row 5b-1520](section-5b-rows-401-425.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Two chats starting together on one store folder slow down and can hit 'database locked' |
| [row 5b-1522](section-5b-rows-401-425.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | First lookup after the server starts ranks badly and still claims the trained model |
| [row 5b-1523](section-5b-rows-401-425.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | When a door hands back candidates it shows their risk but not whether they are proven |
| [row 5b-1524](section-5b-rows-401-425.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Asking to bring an edited spreadsheet back can reach the export or a family card |
| [row 5b-1525](section-5b-rows-401-425.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | Bringing a CSV back writes every cell unchecked, overwriting newer values in the model - *in progress in #871* |
| [row 5b-1526](section-5b-rows-401-425.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | A file the export wrote can be refused whole when brought straight back |
| [row 5b-1527](section-5b-rows-401-425.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | The CSV file route of a proven parameter card has never been run in real Revit |
| [row 5b-1528](section-5b-rows-401-425.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | The round-trip CSV has no fixed encoding, so accented or Arabic names may be written wrong - *in progress in #871* |
| [row 5b-1529](section-5b-rows-401-425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No Heron tool makes a structural floor; Create Floor always passes not-structural |
| [row 5b-1530](section-5b-rows-401-425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | Insulation cannot be added to duct or pipe fittings, accessories or flex runs |
| [row 5b-1531](section-5b-rows-401-425.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Slanted column placement exists in code but no tool offers it and it was never run |
| [row 5b-1532](section-5b-rows-401-425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No Heron tool makes a structural plan view; Create Plan View takes floor or ceiling only |
| [row 5b-1533](section-5b-rows-401-425.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Constitution Article 9 still says Ask to Keep is on the ribbon; it moved to Settings |
| [row 5b-1534](section-5b-rows-401-425.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | Three proven change tools pick a parameter by name at random when two share the name |
| [row 5b-1540](section-5b-rows-401-425.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | The module-reach tool skips every bin folder, so it never sees the hooks that import code - *in progress in #871* |
| [row 5b-1541](section-5b-rows-401-425.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The review-ledger tool crashes with a traceback on a machine with no git - *in progress in #871* |
| [row 5b-1545](section-5b-rows-401-425.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | Nothing runs the build-matrix agent, so its baseline drifts from the tree unseen |
| [row 5b-1546](section-5b-rows-401-425.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | 120 built agents are called by nothing, and agent proofs name no Revit release |
| [row 5b-1548](section-5b-rows-401-425.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | Proven cards' descriptions still say DRAFT; removing the text moved routing answers |
| [row 5b-1550](section-5b-rows-401-425.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | The fitting-family method says routing preference sets a size window; the card does not |
| [row 5b-1551](section-5b-rows-401-425.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | The parameter export calls a column blank on every element a missing parameter name |
| [row 5b-1552](section-5b-rows-401-425.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | An add-in agent proof watches only one of the several files that make up the agent |
| [row 5b-1554](section-5b-rows-401-425.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Reads put back to DRAFT answer only when named until their Group NK proofs are signed |
| [row 5b-1557](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Three messages say a card with editScopeFailures works in an edit mode; it may not |
| [row 5b-1559](section-5b-rows-401-425.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | An add-in error reply cannot say whether its rollback held, so ROLLED BACK is unconfirmed |
| [row 5b-1560](section-5b-rows-401-425.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | The failure classifier files 'nothing came back' on a read as never ran and safe to retry |
| [row 5b-1561](section-5b-rows-401-425.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Heron never flags a reply as an MCP error, and whether any should is unsettled |
| [row 5b-1563](section-5b-rows-401-425.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Only the main doors print the one-word outcome line; other tools answer in prose only |
| [row 5b-1566](section-5b-rows-401-425.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | A new family connector still takes two calls to add and set up |
| [row 5b-1567](section-5b-rows-401-425.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | A family connector could not sit on a reference plane with no solid under it |
| [row 5b-1570](section-5b-rows-401-425.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | Sweep of project connector tools found one duplicate pair and five missing jobs |
| [row 5b-1579](row-5b-1579.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | Locking family lines removes Revit's own sketch dimensions and the delete box alarms |
| [row 5b-1582](row-5b-1582.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | The owner queue tool lists no decision still marked Proposed |
| [row 5b-1583](row-5b-1583.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The tool that archives struck Needs-Checking rows crashes, so nothing can be archived |
| [row 5b-1584](row-5b-1584.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | A method precondition with a colon is served to the chat as a dictionary, not text |
| [row 5b-1587](row-5b-1587.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The open-defects tool exits 0 even when it could not read the register at all |
| [row 5b-1589](row-5b-1589.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Syncing spaces in a non-final phase looks up existing spaces and rooms in the last phase |
| [row 5b-1590](row-5b-1590.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The next-id tool reports dozens of false double holds from helper worktrees - *in progress in #871* |
| [row 5b-1594](row-5b-1594.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | The add-in runs an export or admin job sent without apply and says nothing was kept |
| [row 5b-1611](row-5b-1611.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | The heron-guard hook wrongly blocks Revit code edits made inside a worktree |
| [row 5b-1612](row-5b-1612.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | A dry run replaces values the chat carried between steps, though nothing was kept - *in progress in #871* |
| [row 5b-1626](row-5b-1626.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Two decisions were marked accepted on leave to build, not on being read back |
| [row 5b-1627](row-5b-1627.md) | [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) | Your decision | A Needs-Checking setup relies on material Keywords, which is not a parameter at all |
| [row 5b-1632](row-5b-1632.md) | [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) | Your decision | A Needs-Checking row expects views to be ambiguous when nothing carries the name twice |
| [row 5b-1634](row-5b-1634.md) | [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) | Your decision | A Needs-Checking row writes Comments into types, which only have Type Comments |
| [row 5b-1640](row-5b-1640.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | The structural loads report shows a question mark for every load case category |
| [row 5b-1642](row-5b-1642.md) | [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | check-gaps crashes on Windows when its output goes to a file, cutting its summary |
| [row 5b-1644](row-5b-1644.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | Column rebar with hooked ties is always refused by Revit 2020 |
| [row 5b-1645](row-5b-1645.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | Revit health says a Revit is free while another chat holds the open model |
| [row 5b-1646](row-5b-1646.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'Delete these two walls' looks up a wall-join tool first and the delete tool third |
| [row 5b-1648](row-5b-1648.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | The extrusion roof tool can keep no roof on Revit 2020; its level check always fails |
| [row 5b-1649](row-5b-1649.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'Apply the view template X to view Y' does not offer the apply-template tool at all |
| [row 5b-1650](row-5b-1650.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Create View Filter adds filters to a view whose template owns them, without a warning |
| [row 5b-1651](row-5b-1651.md) | [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | After a kept change, reading a plan through Heron sees the wrong ducts and tags |
| [row 5b-1652](row-5b-1652.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Tag arranging works one category at a time, so tags of different kinds stay overlapped |
| [row 5b-1653](row-5b-1653.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | The HVAC spaces method wrongly says new mechanical projects start on Areas only |
| [row 5b-1654](row-5b-1654.md) | [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | Making an HVAC zone with a taken name makes a duplicate and the answer does not say so |
| [row 5b-1655](row-5b-1655.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Syncing spaces asked for the first phase makes them in the last phase, and does not say |
| [row 5b-1670](row-5b-1670.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | The roof slope tool cannot slope only the edges a modeller picks - *done in #890, merged* |
| [row 5b-1671](row-5b-1671.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | 'Join the main roof to the wing roof' reaches a roof-making tool; Revit has no join API |
| [row 5b-1672](row-5b-1672.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | No tool shapes a wall top to a gable, so gable triangles stay open on 2020-2024 - *done in #890, merged* |
| [row 5b-1673](row-5b-1673.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | No tool sets a column's top level without moving it; the words reach an attach tool - *done in #890, merged* |
| [row 5b-1674](row-5b-1674.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | Deleting a roof detaches its walls and Heron's answer does not say so - *done in #890, merged* |
| [row 5b-1702](row-5b-1702.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | Loading a family reports that the project's Glass material changed, maybe wrongly |
| [row 5b-1703](row-5b-1703.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | A column's Top Level cannot be set by name because two parameters share that name - *done in #889, merged* |
| [row 5b-1704](row-5b-1704.md) | [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) | Your decision | Placing an architectural column with a top level is refused unless placed as structural - *done in #889, merged* |
| [row 5b-1705](row-5b-1705.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Duplicate Type works only from a placed element, not from a type itself - *done in #889, merged* |
| [row 5b-1706](row-5b-1706.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Placing families has no rotation, so each placement needs a separate rotate step - *done in #889, merged* |
| [row 5b-1707](row-5b-1707.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | A mistyped id bound to a non-model element and Move ran on it instead of refusing - *done in #887, merged* |
| [row 5b-1708](row-5b-1708.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Rotate prints a working value that reads as the opposite turn - *done in #889, merged* |
| [row 5b-1709](row-5b-1709.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | A casework family came out 45 mm short and its box reaches where nothing is drawn |
| [row 5b-1710](row-5b-1710.md) | [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | A plan picture shows the whole crop, so a house in a large plan is a small patch - *done in #886, merged* |
| [row 5b-1711](row-5b-1711.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | Every lookup warned the knowledge store did not match the checkout for a whole session - *done in #886, merged* |
| [row 5b-1730](row-5b-1730.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | A keynote legend is reported as having no columns while it has two - *done in #887, merged* |
| [row 5b-1731](row-5b-1731.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | The tag overlap check says clean when tags sit on walls, and does not say it skips walls - *done in #887, merged* |
| [row 5b-1732](row-5b-1732.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | Tag arranging never moves room tags or treats them as obstacles - *done in #887, merged* |
| [row 5b-1733](row-5b-1733.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | Where a tag's head sits cannot be read by Report Location or checked after a move - *done in #887, merged* |
| [row 5b-1736](row-5b-1736.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No tool selects just the elements a section cuts, so cut ones must be found by hand - *done in #887, merged* |
| [row 5b-1739](row-5b-1739.md) | [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | Circuit naming takes a built-in word over a same-named project parameter, silently - *done in #895, merged* |
| [row 5b-1740](row-5b-1740.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Notes written below a need's name line in a card never reach the chat - *done in #895, merged* |
| [row 5b-1749](row-5b-1749.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | Listing materials with all tabs crashed inside Revit 2024 on a fresh project's Glass |
| [row 5b-1750](row-5b-1750.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Open Family for Editing says not opened while the family is open in front |
| [row 5b-1752](row-5b-1752.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The Needs-Checking split tool cannot plan a new group because it misreads Group BV |
| [row 5b-1821](row-5b-1821.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | The run record says KEPT for a change the reply called nothing changed or refused - *done in #895, merged* |
| [row 5b-1822](row-5b-1822.md) | [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | Change evidence cannot record check-gaps where the suites run over half an hour |
| [row 5b-1780](row-5b-1780.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Rotate does not refuse a whole turn typed to seven places (6.2831853) |
| [row 5b-1800](row-5b-1800.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Move to a point counts a tag as 'positioned by a curve'; a tag moves by its head |
| [row 5b-1801](row-5b-1801.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Two proven section cards read the cut plane in two different places |
| [row 5b-1826](row-5b-1826.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Detail lines cannot name a floor plan when a ceiling plan has the same name |
| [row 5b-1827](row-5b-1827.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No tool sets a wall's Location Line, and the refusal gives no reason |
| [row 5b-1828](row-5b-1828.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Move moves an elevation marker but cannot read it back, so it reads 'unverified' |
| [row 5b-1830](row-5b-1830.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | A draft's one kept run that raises the delete box ends 'needs_unbound' and keeps nothing |
| [row 5b-1831](row-5b-1831.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Setting four columns' levels would delete 2 elements, and which two is unknown |
| [row 5b-1832](row-5b-1832.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'Attach these walls to the roof' and 'delete this roof' reach the wrong tool on 2024 |

## Found finished on 2026-11-16 - closed in the register, no issue

Each row's state was corrected that day with the evidence, and its old state kept after `Was:`.

| Row | The evidence |
|---|---|
| [row 32](section-5-rows-026-050.md) | place-rooms is PROVEN (version 3) with a negative that came back empty - created 0, refused "nothing was placed - there is no enclosed area on this level" (its fragment.yaml proof) - and place-room-at-point is DEPRECATED, superseded by PLACE_ROOMS |
| [row 174](section-5-rows-151-175.md) | the proof was taken again on 2026-10-13, signed Ajmal PS, with GetCategoryOverrides' read-back as its evidence (brain/fragments/set-category-solid-fill/fragment.yaml proof); the sentence in dispute is gone, and the card is DEPRECATED since |
| [row 5b-218](section-5b-rows-201-225.md) | IMPORT_PARAMETER_VALUES is DEPRECATED, superseded by SET_PARAMETER_VALUES_BY_ID (PROVEN), whose read-back takes Revit's own parse of a number instead of comparing the typed text |
| [row 5b-288](section-5b-rows-276-300.md) | no typed library or skill count is left in brain/README.md, CONTRIBUTING.md or docs/README.md (grep, 2026-11-16): since 2026-10-04 those counts are derived by a command (heron-ship section 6a), so the wordings the gate could not read are gone |
| [row 5b-290](section-5b-rows-276-300.md) | SET_PARAMETER_VALUES_BY_ID (PROVEN, signed 2026-10-13, row 5b-459) names rooms by their Number, so no id has to be guessed |
| [row 5b-291](section-5b-rows-276-300.md) | CREATE_FAMILY_FORMS (PROVEN 2026-11-02) builds one form or a whole list in one call, as one undo; row 5b-651 records 43 forms in 6 calls |
| [row 5b-429](section-5b-rows-401-425.md) | CREATE_FAMILY_SWEEP is DEPRECATED, merged into CREATE_FAMILY_FORMS, whose card gives the circle, rect, polygon and corner-with-arc profiles for every row and which two coordinates each plane uses |
| [row 5b-469](section-5b-rows-401-425.md) | GROUP_BY_ASSEMBLY is DEPRECATED, merged into REPORT_NESTED_FAMILIES on 2026-11-01 (row 5b-1362), so one card does the job |
| [row 5b-1182](section-5b-rows-401-425.md) | all three cards are PROVEN now - set-global-parameter and report-category-overrides (proofs 2026-11-02), set-view-filter-categories (2026-10-18) - and none is on check-signatures' stale list, so revit_change keeps their changes |
| [row 5b-1262](section-5b-rows-401-425.md) | row 5b-1575, CLOSED 2026-11-12: SET_FAMILY_TYPE_VALUES version 4 writes an air flow and any other measured kind, PROVEN on Revit 2020 and Revit 2024, signed Ajmal PS |
| [row 5b-1268](section-5b-rows-401-425.md) | the three leftover phrases were asked on the owner's PC with the trained model and the re-ranker (docs/needs-checking/group-ib.md, "Row 5b-1268's three phrases on this PC") and each reached the card it wants; (3) stays a coin toss by design, as the row said |
| [row 5b-1309](section-5b-rows-401-425.md) | fixed by row 5b-1415: SET_RAILING_TYPE reads the primary handrail only when the type has one, and version 3 is PROVEN on 2026-11-14 ("handrail none, read back as asked") |
| [row 5b-1310](section-5b-rows-401-425.md) | SWITCH_ACTIVE_PROJECT is DEPRECATED, merged into ACTIVATE_DOCUMENT (PROVEN), which does not carry viewName (activate-document/fragment.yaml: "viewName NOT CARRIED ... (row 5b-1310)"); OPEN_VIEW, PROVEN, opens the view |
| [row 5b-1401](section-5b-rows-401-425.md) | row 5b-1412, FIXED 2026-11-02: "the three roles, in the cards' own text; row 5b-1401 with it" - networkJson is role: accounting, and REPORT_SPRINKLER_NETWORK is PROVEN on its 2026-11-02 proof |
| [row 5b-1568](section-5b-rows-401-425.md) | row 5b-1580 ran the probe on 2026-11-12 on Revit 2024 and recorded it as an API LIMIT - "closed, nothing to build" |
| [row 5b-1217](section-5b-rows-401-425.md) | The same defect as row 5b-1219, FIXED 2026-10-19: `tests/test_mcp_serves.py` reads the card's own `heron-status` instead of a typed DRAFT. **Its state is left as it was** because open pull request #871 edits that line. |

## Done by pull requests merged on 2026-11-16

These rows were in progress when the issues were made, and the pull requests below merged the same day. Each is ticked in its issue; the Revit run each still owes is in its NEEDS-CHECKING group, not here.

- **[#886](https://github.com/Ajmalpshaik/Heron-AI/pull/886)** - 5b-1710, 5b-1711
- **[#887](https://github.com/Ajmalpshaik/Heron-AI/pull/887)** - 5b-1707, 5b-1730, 5b-1731, 5b-1732, 5b-1733, 5b-1736
- **[#889](https://github.com/Ajmalpshaik/Heron-AI/pull/889)** - 5b-1703, 5b-1704, 5b-1705, 5b-1706, 5b-1708
- **[#890](https://github.com/Ajmalpshaik/Heron-AI/pull/890)** - 5b-1670, 5b-1672, 5b-1673, 5b-1674
- **[#895](https://github.com/Ajmalpshaik/Heron-AI/pull/895)** - 5b-1739, 5b-1740, 5b-1821

## Left as they were, for open pull requests

These rows still begin `OPEN` here: an open pull request already edits each one, and changing the same line twice would only make a merge conflict. Each is in its issue above, marked *In progress*. (Row 5b-1217 is also edited by #871; it is finished, above, and has no issue. Row 5b-1672 is listed here too, but #890 has done it.)

- **[#871](https://github.com/Ajmalpshaik/Heron-AI/pull/871)** - 5b-427, 5b-1131, 5b-1161, 5b-1166, 5b-1170, 5b-1171, 5b-1172, 5b-1173, 5b-1174, 5b-1178, 5b-1179, 5b-1220, 5b-1525, 5b-1528, 5b-1540, 5b-1541, 5b-1590, 5b-1612, 5b-1672
