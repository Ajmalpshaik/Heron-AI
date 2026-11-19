# Fragment issues - the open rows, as GitHub issues

> **Since 2026-11-16, on the owner's word, every open row of sections 5 and 5b of [the register](../FRAGMENT-ISSUES.md) is tracked as a GitHub issue** - *"make it as an issues. Not in a single one... segregate and group it... easy to close, so one by one I can close it... a single simple task."* The rows were sorted that day, each one checked against the register, the archive, NEEDS-CHECKING, the handover and the code. Rows found already finished were closed in the register and given no issue; the rest went into small issues of one kind each. **A moved row is never deleted** - its state cell begins `MOVED TO ISSUE #N`, its old state kept after `Was:` - and `python tools/open-defects.py` counts it as moved, not as finished. `python tools/archive-fragment-issues.py --write` moves its full text to [`fragment-issues-archive/`](../fragment-issues-archive/README.md), as it does for a finished row, and its line keeps the issue's link.

> **The same day, the other registers followed**, on the owner's words *"check all of the dot MD files ... if you create the issue so you can remove from that dot MD files"*: open questions and proposals in [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) to [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967), NEEDS-CHECKING groups in [#968](https://github.com/Ajmalpshaik/Heron-AI/issues/968) to [#1004](https://github.com/Ajmalpshaik/Heron-AI/issues/1004), open work-note items in [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) to [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014) (ids WN-1 to WN-78), and [#1015](https://github.com/Ajmalpshaik/Heron-AI/issues/1015) for checks found done. Each entry keeps its text where it was, with a line saying which issue tracks it, and the parts at the end of this page map every one. (#1010 is a pull request, not one of these.)

## Working an issue

Every issue has a ready prompt that points here. Its kind is in its title: **Fix:**, **Build:**, **Tidy-up:**, **Revit run:**, **PC run:** or **Your decision:**, and its prompt says which of the parts below to follow. Every kind starts the same way:

1. Read [`AGENTS.md`](../../AGENTS.md) and follow it.
2. Read the issue. A row marked **In progress** belongs to the pull request it names: leave it alone, and tick it when that pull request merges.
3. Take the other rows one at a time. Read each in full - its file is in the table below; a line that links to its *full row* has its full text in the archive, and its new state is written on that line - and check it is still real: read the file or code it names, and search the repository for its id, because a later row may have done it already. If it is already done, close it (step 5) with that evidence.

**A FIX issue** - an AI session does the work:

4. If the row's line in the issue says *Your choice*, ask the owner that question in plain words and wait. Then make the smallest fix the row asks for. Anything else found on the way is a new row (`python tools/next-id.py` gives its number), not a fix. If the C# of a PROVEN fragment changes, set it back to DRAFT, raise its version and write the Revit run it now owes ([`heron-ship`](../../.claude/skills/heron-ship/SKILL.md)). Never sign a proof.
5. Change the row's state cell to begin `FIXED <date> - <what changed, and the test or file that shows it>`, keeping everything after `Was:`. Tick the row in the issue.

**A REVIT RUN issue** - on the owner's Windows PC, with the [`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md) skill:

4. Bring the checkout up to date with `main` and run `python tools/heron-ready.py`, so Revit loads the current add-in. Read the NEEDS-CHECKING item the row names. Run the check through Heron's own tools on a **scratch copy** of a model - never a live project - with a positive case and a negative case; the owner presses Keep or Discard. Ask the owner before signing anything in his name.
5. Write what came back into the row and its NEEDS-CHECKING item - Revit release, model, date. Passed: the state begins `CLOSED <date> - run on Revit <release>, <model>`, keeping everything after `Was:`, and the row is ticked. Failed: the row keeps `MOVED TO ISSUE #N` and says what failed.

**A DECISION issue** - the owner answers in the issue's comments, one line a row:

4. Leave any row he has not answered. Write his answer into the row with the date, in his words; a rule Heron must keep is also recorded as a decision, the way [`DECISIONS.md`](../DECISIONS.md) adds one. If the answer needs a change, make the smallest one (the FIX rules above).
5. Nothing left: the state begins `ANSWERED <date> - <his answer, and what changed>`, keeping everything after `Was:`, and the row is ticked. Work left: the row keeps `MOVED TO ISSUE #N` and adds `answered <date>: <his answer>`.

**A QUESTION OR PROPOSAL issue** - [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) to [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967); the owner answers in the issue's comments, one line each:

4. First check the entry is still open: its heading does not start ✅, and it has no **Answer:** paragraph. An entry that was marked *In progress* in the issue was decided by pull request [#871](https://github.com/Ajmalpshaik/Heron-AI/pull/871), merged 2026-11-17: check the decision is in the file and tick the line, naming where it is.
5. Otherwise write his answer under the entry, dated and in his words, the way the top of [`OPEN-QUESTIONS.md`](../OPEN-QUESTIONS.md) or [`PROPOSALS.md`](../PROPOSALS.md) says - a question's answer is an **Answer:** paragraph, a proposal's heading turns ✅ - and promote it into [`DECISIONS.md`](../DECISIONS.md) where it is a rule. If the answer needs a change, make the smallest one (the FIX rules). Tick the line.

**A NEEDS-CHECKING issue** - [#968](https://github.com/Ajmalpshaik/Heron-AI/issues/968) to [#1004](https://github.com/Ajmalpshaik/Heron-AI/issues/1004), and [#1015](https://github.com/Ajmalpshaik/Heron-AI/issues/1015); on the owner's Windows PC for a run:

4. For each group, run the checks the issue lists as owed, as a REVIT RUN step 4 says (a PC run needs no Revit, only the PC). Ids listed as *already settled, not yet struck* need no run: their result is already in the group's file.
5. Write each result into the group's file the way the top of [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) says - release, model, date, what came back. A check that held has its id struck at both ends (`~~**KE1**~~`); one that failed stays, says what failed, and a wrong piece of code becomes a new row in the register. Tick the group when none of its ids is left unstruck.

**A WORK NOTE issue** - [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) to [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014); items WN-1 to WN-78, each at a line of a work note or the handover that carries *(Tracked as GitHub issue ...)*:

4. A fix or build item follows the FIX rules, a decision item the DECISION rules with the answer written where the item is, a PC-run item the REVIT RUN rules.
5. Done: replace the item's tracking note with `done <date>: <what changed>` and tick it. A note whose items are all done is retired the way [the work-notes README](../work-notes/README.md) says - its lasting part moved somewhere permanent first, never simply deleted.

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
| [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | re-sign proofs and check new tools | 10 |
| [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | routing re-checked on your PC's trained model | 5 |
| [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | how the add-in guards changes | 7 |
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
| [row 5b-427](section-5b-rows-426-450.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | The server adds the Companion folder to Python's path again on every tool call - *done in #871, merged* |
| [row 5b-428](section-5b-rows-426-450.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | Each change or read call re-reads every card file to find one capability |
| [row 5b-434](section-5b-rows-426-450.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | 'How many furniture are there' was answered by a room counter or a write |
| [row 5b-437](section-5b-rows-426-450.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | An OmniClass number Heron set and read back was blank again minutes later |
| [row 5b-444](section-5b-rows-426-450.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | A category typed by its shown name could bind a different category (Railings) |
| [row 5b-448](section-5b-rows-426-450.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | A sub-category like Rooms: Interior Fill could not be named to any category tool |
| [row 5b-455](section-5b-rows-451-475.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | The stair tool said it makes no railing, but Revit adds two with every stair |
| [row 5b-472](section-5b-rows-451-475.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | Quality checks sit under two domains and levels and grids under four |
| [row 5b-473](section-5b-rows-451-475.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | Nothing reports a sentence that two cards declare, which breaks exact matching |
| [row 5b-474](section-5b-rows-451-475.md) | [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | No read reports a link's room bounding or path type, so the questions reach writes |
| [row 5b-475](section-5b-rows-451-475.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | Nothing opens a model detached from central or with audit |
| [row 5b-476](section-5b-rows-476-500.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'Line the viewports up' reaches a read; a room-area question reached a write |
| [row 5b-477](section-5b-rows-476-500.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Plain modeller questions still reach tools that change the model (first 60) |
| [row 5b-478](section-5b-rows-476-500.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | Plain modeller questions still reach tools that change the model (second 60) |
| [row 5b-490](section-5b-rows-476-500.md) | [#909](https://github.com/Ajmalpshaik/Heron-AI/issues/909) | Revit run on your PC | 'Describe these elements' reached a write that renumbers them |
| [row 5b-496](section-5b-rows-476-500.md) | [#958](https://github.com/Ajmalpshaik/Heron-AI/issues/958) | Code fix | With the trained model installed, two routing suites fail on main and CI never sees it |
| [row 5b-513](section-5b-rows-501-525.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | Heron cannot save the model it is working in; two unsaved builds were lost |
| [row 5b-650](section-5b-rows-626-650.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | Builders work layout method is a draft with gaps and a word-matching bug |
| [row 5b-665](section-5b-rows-651-675.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Two code comments still say the duct move was never measured |
| [row 5b-666](section-5b-rows-651-675.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | 'Move the ducts up 200 mm' moves every duct in the model, not the selection |
| [row 5b-861](section-5b-rows-851-875.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Tools that make copies put them on the active workset and cannot be told otherwise |
| [row 5b-940](section-5b-rows-926-950.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | General notes update method is a draft; three edit pitfalls were recorded |
| [row 5b-977](section-5b-rows-976-1000.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | A read-level job could still commit its own transaction and keep the change |
| [row 5b-1003](section-5b-rows-1001-1025.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Opening a family by a backslash path once said there was no file |
| [row 5b-1004](section-5b-rows-1001-1025.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | A Store Claude keeps its own copy of Heron's settings, so Revit and Heron can disagree |
| [row 5b-1007](section-5b-rows-1001-1025.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Asking to read a text note's wording reaches a date reader or a write, not the note read |
| [row 5b-1060](section-5b-rows-1051-1075.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Two models from one template look like one model, so the move refuses for the wrong reason |
| [row 5b-1061](section-5b-rows-1051-1075.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | One move writes two audit lines under two workflow ids, so they cannot be joined |
| [row 5b-1062](section-5b-rows-1051-1075.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | A refused move says neither what Revit objected to nor that the move was rolled back |
| [row 5b-1063](section-5b-rows-1051-1075.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Save with close says rolled back, nothing kept - but the window really was closed |
| [row 5b-1065](section-5b-rows-1051-1075.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Installer and heron-ready disagree on package versions for a Store Claude PC |
| [row 5b-1066](section-5b-rows-1051-1075.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | Installer refusal says installs from inside Claude land in the copy - not always true |
| [row 5b-1090](section-5b-rows-1076-1100.md) | [#899](https://github.com/Ajmalpshaik/Heron-AI/issues/899) | Revit run on your PC | Picturing chosen elements always failed: the reply read a view already rolled back |
| [row 5b-1091](section-5b-rows-1076-1100.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | The run guard refused families opened for editing, so two proven tools stopped working |
| [row 5b-1092](section-5b-rows-1076-1100.md) | [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) | Revit run on your PC | A cancelled save of another model was reported as a change Heron cannot undo |
| [row 5b-1095](section-5b-rows-1076-1100.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | Twenty calls at once lose 2 to 7 answers, not the 1 or 2 doc 25 claims, and once hung |
| [row 5b-1100](section-5b-rows-1076-1100.md) | [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) | Revit run on your PC | The Companion button looked in Revit's own temp folder and said no chat was running |
| [row 5b-1110](section-5b-rows-1101-1125.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | Long proving runs fill Revit 2020's memory until jobs fail and the chat crashes |
| [row 5b-1112](section-5b-rows-1101-1125.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Setting duct justification on Revit 2020 sets nothing, and a no-change counts as set |
| [row 5b-1113](section-5b-rows-1101-1125.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Placing rooms on a level with no plan view throws instead of refusing by name |
| [row 5b-1114](section-5b-rows-1101-1125.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | batch-prove calls a tool whose one result is a single object unreadable |
| [row 5b-1115](section-5b-rows-1101-1125.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Move annotation says it moves text notes, but a text note comes back not movable |
| [row 5b-1118](section-5b-rows-1101-1125.md) | [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) | Your decision | Placing rooms on a finished level makes two extra unbounded rooms |
| [row 5b-1119](section-5b-rows-1101-1125.md) | [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | Putting a damper or valve into a lone duct or pipe on Revit 2020 always rolls back |
| [row 5b-1121](section-5b-rows-1101-1125.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Rolled-back link runs leave the linked file open, so later links read as not loaded |
| [row 5b-1122](section-5b-rows-1101-1125.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Link coordinate check puts 'not loaded - not checked' in its result, so it reads as a pass |
| [row 5b-1124](section-5b-rows-1101-1125.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Linking a Revit file makes a link that never shows its model, yet says Loaded |
| [row 5b-1125](section-5b-rows-1101-1125.md) | [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) | Your decision | Spaces made from linked rooms come back with no area but are still counted as made |
| [row 5b-1126](section-5b-rows-1126-1150.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Moving a Revit link answers moved 0 though it did move, inviting a second move |
| [row 5b-1130](section-5b-rows-1126-1150.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | HVAC and fire design contracts name refusal words the code never sends |
| [row 5b-1131](section-5b-rows-1126-1150.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | Two places choose which tool provides a capability by different rules - *#871 merged, the row is still open* |
| [row 5b-1132](section-5b-rows-1126-1150.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | The which-Revit list shows process numbers and no project name |
| [row 5b-1134](section-5b-rows-1126-1150.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Four code comments still describe Heron as it was before D-129, D-132 and D-135 |
| [row 5b-1136](section-5b-rows-1126-1150.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Group EQ's command counts a Revit 2022 proof that does not exist |
| [row 5b-1138](section-5b-rows-1126-1150.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Two credential lists disagree, and commit staging never checks staged files for secrets |
| [row 5b-1153](section-5b-rows-1151-1175.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | A proof's fingerprint covers only the code, not the card, the release or the path it ran |
| [row 5b-1155](section-5b-rows-1151-1175.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | Only the Python side enforces proven-only changes; the add-in runs any code it is sent |
| [row 5b-1156](section-5b-rows-1151-1175.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | The MCP server runs the checkout it started in and nothing says which folder or commit |
| [row 5b-1157](section-5b-rows-1151-1175.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | When the only Revit closes, Heron quietly binds to another, against Article 12a |
| [row 5b-1158](section-5b-rows-1151-1175.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | Some reads hand back lengths and areas in feet and square feet with no unit |
| [row 5b-1159](section-5b-rows-1151-1175.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Still-running answers carry no workflow id and wrongly say the run is not recorded |
| [row 5b-1161](section-5b-rows-1151-1175.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | revit_change runs reads as writes; select all ducts sits on a read that selects nothing - *#871 merged, the row is still open* |
| [row 5b-1162](section-5b-rows-1151-1175.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Values the AI passes are filed as the modeller's own word and never asked again |
| [row 5b-1163](section-5b-rows-1151-1175.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | A run record still says KEPT after the modeller undid the change |
| [row 5b-1164](section-5b-rows-1151-1175.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | Values carried between tool steps are matched to a model by title, which files can share |
| [row 5b-1166](section-5b-rows-1151-1175.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Proof validate runs of changes and publishes are resent when an answer is lost - *done in #871, merged* |
| [row 5b-1167](section-5b-rows-1151-1175.md) | [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) | Code fix | The add-in can send an answer of any size over the pipe; a lost read is still called safe |
| [row 5b-1168](section-5b-rows-1151-1175.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | Developer files load in a modeller's chat, and tool descriptions sit at their size limit |
| [row 5b-1170](section-5b-rows-1151-1175.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | check-docs passes links to emoji headings that GitHub does not resolve - *done in #871, merged* |
| [row 5b-1171](section-5b-rows-1151-1175.md) | [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) | Code fix | The vector search speed-up is never loaded, though D-23 says it is used - *#871 merged, the row is still open* |
| [row 5b-1172](section-5b-rows-1151-1175.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | The rebuild command says it restores every store, but it restores only the global one - *done in #871, merged* |
| [row 5b-1173](section-5b-rows-1151-1175.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | Two register tools file the same row under different needs - *done in #871, merged* |
| [row 5b-1174](section-5b-rows-1151-1175.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | prove-agent's own usage line for track cannot run: it leaves out a required option - *done in #871, merged* |
| [row 5b-1175](section-5b-rows-1151-1175.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Code already runs on two decisions still marked Proposed: D-112 and D-113 |
| [row 5b-1176](section-5b-rows-1176-1200.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | D-138 says two different things about asking before a publish when Ask to Keep is off |
| [row 5b-1177](section-5b-rows-1176-1200.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Three places still word Heron as it was before D-27 and the standards tool |
| [row 5b-1178](section-5b-rows-1176-1200.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The needs-checking index page still holds rows that belong in group files - *#871 merged, the row is still open* |
| [row 5b-1179](section-5b-rows-1176-1200.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | Two struck checks said the emergency stop could not be set; since D-129 it can - *done in #871, merged* |
| [row 5b-1180](section-5b-rows-1176-1200.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The owner's ask for one numbered specification document has no register row |
| [row 5b-1181](section-5b-rows-1176-1200.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The register file holds rows far outside its band of 25 |
| [row 5b-1201](section-5b-rows-1201-1225.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Placing beams at points made broken zero-length beams and reported them as placed |
| [row 5b-1220](section-5b-rows-1201-1225.md) | [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) | Code fix | The decision log's preamble cites the wrong Golden Rule for its append-only rule - *done in #871, merged* |
| [row 5b-1221](section-5b-rows-1201-1225.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Renaming a level always rolled back: Heron declined Revit's rename-views question |
| [row 5b-1242](section-5b-rows-1226-1250.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Flipping a door to face its room left its From and To Room the old way round |
| [row 5b-1261](section-5b-rows-1251-1275.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Arraying family forms on planes a parameter moves breaks the array at other sizes |
| [row 5b-1267](section-5b-rows-1251-1275.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Revit's API cannot run a render or reach some display, sky and camera settings |
| [row 5b-1292](section-5b-rows-1276-1300.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | A combined schedule column put the separator after the last part and could not be edited |
| [row 5b-1293](section-5b-rows-1276-1300.md) | [#958](https://github.com/Ajmalpshaik/Heron-AI/issues/958) | Code fix | Routing test checks fail with the trained model installed, and CI cannot see them |
| [row 5b-1324](section-5b-rows-1301-1325.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | A void Heron makes in a floor-based family cuts no floor, and no Revit API call can |
| [row 5b-1329](section-5b-rows-1326-1350.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | Column rebar tool places no ties in round concrete columns; Revit says internal error |
| [row 5b-1335](section-5b-rows-1326-1350.md) | [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902) | Revit run on your PC | Flip tool could not turn a nested part placed upside down on a reversed work plane |
| [row 5b-1340](section-5b-rows-1326-1350.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | An add-in agent cannot be promoted: editing its status line stales its own proof |
| [row 5b-1347](section-5b-rows-1326-1350.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | 'T10 at 150 both faces of these walls' reaches the wall-by-face tool, not area rebar |
| [row 5b-1351](section-5b-rows-1351-1375.md) | [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) | Code fix | Family model text tool reads letters back as a metre deep when 1 mm was asked |
| [row 5b-1355](section-5b-rows-1351-1375.md) | [#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924) | Your decision | Model dashboard counts three categories all named Center line, 43% of its total |
| [row 5b-1362](section-5b-rows-1351-1375.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | After five duplicate merges, the element ownership report v2 is still unproven |
| [row 5b-1363](section-5b-rows-1351-1375.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Leftovers of sixteen duplicate merges: questions on writes, old names in code |
| [row 5b-1364](section-5b-rows-1351-1375.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Merge gaps left: commas split filter names, no quote rule, one copy lacks has-no-value |
| [row 5b-1365](section-5b-rows-1351-1375.md) | [#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950) | Code fix | Leftovers of 49 duplicate merges: three keepers unproven, old names, questions on writes |
| [row 5b-1366](section-5b-rows-1351-1375.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | After thirty more merges, 'select walls of type' and other questions reach writes |
| [row 5b-1368](section-5b-rows-1351-1375.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | 'How much concrete is in the walls', a question, reaches an area rebar write |
| [row 5b-1373](section-5b-rows-1351-1375.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | After more merges, 'which element cuts which in this join' reaches the join write |
| [row 5b-1375](section-5b-rows-1351-1375.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' reaches a write that the show-elements tool does not name |
| [row 5b-1376](section-5b-rows-1376-1400.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Radial array was merged into the copy tool; its circular copy cases never ran in Revit |
| [row 5b-1378](section-5b-rows-1376-1400.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | 'Space these round a point' reaches a room line write, not the copy tool |
| [row 5b-1381](section-5b-rows-1376-1400.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | On Revit 2023-2026 a pinned support request reaches the attach-columns write |
| [row 5b-1382](section-5b-rows-1376-1400.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | Probe questions like 'which circuit is this light on' reach new write tools |
| [row 5b-1383](section-5b-rows-1376-1400.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Create floor sends a foundation slab type through as a plain floor, unchecked |
| [row 5b-1386](section-5b-rows-1376-1400.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | 'Is this connection approved' reaches the steel connection write |
| [row 5b-1388](section-5b-rows-1376-1400.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | On older Revit, 'make a PDF of the drawings' reaches image import, a write |
| [row 5b-1389](section-5b-rows-1376-1400.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | On Revit 2020 a circuit naming scheme request reaches the circuit edit write |
| [row 5b-1391](section-5b-rows-1376-1400.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | 'Colour code the ducts by system' and 'add a pipe legend' reach the wrong tools |
| [row 5b-1395](section-5b-rows-1376-1400.md) | [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) | Code fix | 'Select all the ducts' reached a write when the question flag was not set |
| [row 5b-1397](section-5b-rows-1376-1400.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Set MEP justification changes nothing: every duct refuses both offsets |
| [row 5b-1398](section-5b-rows-1376-1400.md) | [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) | Your decision | Moving elements by 0,0,0 reports them as moved |
| [row 5b-1399](section-5b-rows-1376-1400.md) | [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | Camera framing tool 'framed' sheets, which have no 3D extent, and made a view |
| [row 5b-1400](section-5b-rows-1376-1400.md) | [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) | Code fix | Minimum clearance check reported every pair of sheets as too close |
| [row 5b-1402](section-5b-rows-1401-1425.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | IFC export cannot run: its schema value cannot be typed in a request |
| [row 5b-1404](section-5b-rows-1401-1425.md) | [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) | Code fix | Listing materials with all tabs crashed Revit 2020 with a memory error |
| [row 5b-1405](section-5b-rows-1401-1425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved again from one wrong write to another |
| [row 5b-1406](section-5b-rows-1401-1425.md) | [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) | Code fix | A single text note id handed to a tool wanting a list crashed with a cast error |
| [row 5b-1407](section-5b-rows-1401-1425.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Export families saved nothing: Revit refused to open each family for editing |
| [row 5b-1408](section-5b-rows-1401-1425.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Reload links reloaded nothing: Revit refuses it inside Heron's transaction group |
| [row 5b-1409](section-5b-rows-1401-1425.md) | [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) | Code fix | Set MEP justification refuses every duct on Revit 2024 too |
| [row 5b-1411](section-5b-rows-1401-1425.md) | [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) | Code fix | Export families is refused on Revit 2024 too: the proof route is the cause |
| [row 5b-1413](section-5b-rows-1401-1425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | A units question about cooling load reaches the heating and cooling loads write |
| [row 5b-1417](section-5b-rows-1401-1425.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | 'Which one is cutting' reaches neither the join report nor any card claiming it |
| [row 5b-1420](section-5b-rows-1401-1425.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No tool can read or set keynote numbering; Revit's API has nothing for it |
| [row 5b-1423](section-5b-rows-1401-1425.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | No Heron route can Reload Latest or Sync: Revit refuses both inside a transaction group |
| [row 5b-1424](section-5b-rows-1401-1425.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved a third time from one wrong write to another |
| [row 5b-1426](section-5b-rows-1426-1450.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved again, and made-up line style questions reach a write |
| [row 5b-1427](section-5b-rows-1426-1450.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | List categories reads no material, so a material question gets no material |
| [row 5b-1428](section-5b-rows-1426-1450.md) | [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) | Your decision | 'Put it back' moved a fifth time from one write to another |
| [row 5b-1429](section-5b-rows-1426-1450.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | A copy between projects rolls back whole when a type name already exists here |
| [row 5b-1431](section-5b-rows-1426-1450.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | 'What combinations are in this model' reaches the load combination write |
| [row 5b-1433](section-5b-rows-1426-1450.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Four made-up structural settings questions reach the new settings writes |
| [row 5b-1435](section-5b-rows-1426-1450.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Three made-up analytical model questions reach the analytical writes |
| [row 5b-1440](section-5b-rows-1426-1450.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No tool can link an IFC file; a fragment cannot run Revit's IFC importer |
| [row 5b-1441](section-5b-rows-1426-1450.md) | [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) | Code fix | Point clouds link origin to origin only; by shared coordinates is named, not linked |
| [row 5b-1443](section-5b-rows-1426-1450.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | 'Which images need reloading' and point cloud questions reach writes |
| [row 5b-1445](section-5b-rows-1426-1450.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Beam justification numbers are assumed; which number means which is undocumented |
| [row 5b-1446](section-5b-rows-1426-1450.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Stair tread numbers pick tread or riser by trial; the 2027 start number is unknown |
| [row 5b-1447](section-5b-rows-1426-1450.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | Remove project parameter deletes or unbinds without seeing what Revit's Remove does |
| [row 5b-1448](section-5b-rows-1426-1450.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Made-up stair and beam sentences miss their tools; two stair questions reach a write |
| [row 5b-1449](section-5b-rows-1426-1450.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Mechanical settings annotation sizes assumed to be lengths; rise/drop symbols unreachable |
| [row 5b-1450](section-5b-rows-1426-1450.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Energy settings: the glazing form and what the energy model switch does are unknown |
| [row 5b-1451](section-5b-rows-1451-1475.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Create energy model does not know what a second run does to an existing one |
| [row 5b-1452](section-5b-rows-1451-1475.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | gbXML import runs inside Heron's transaction; whether Revit allows it is unseen |
| [row 5b-1453](section-5b-rows-1451-1475.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Made-up settings questions reach settings writes; two requests miss their tool |
| [row 5b-1454](section-5b-rows-1451-1475.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Rebar shape bend kind and leg lengths are assumed, not checked against Revit |
| [row 5b-1455](section-5b-rows-1451-1475.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Reinforcement symbols: the type category and plan or section behaviour are unknown |
| [row 5b-1456](section-5b-rows-1451-1475.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Sketched stairs draw one riser line per riser; whether Revit agrees is unseen |
| [row 5b-1457](section-5b-rows-1451-1475.md) | [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) | Your decision | Made-up rebar and stair questions reach the new write tools |
| [row 5b-1458](section-5b-rows-1451-1475.md) | [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) | Revit run on your PC | Offset line chain: the joins and which side the sign goes have not been seen |
| [row 5b-1459](section-5b-rows-1451-1475.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Insert views from file opens another project inside Heron's transaction, unseen |
| [row 5b-1460](section-5b-rows-1451-1475.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | Systems analysis runs in the background; what Discard does to it is unknown |
| [row 5b-1461](section-5b-rows-1451-1475.md) | [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) | Revit run on your PC | Publish Coordinates never saves the linked file; what Revit does at next save is unknown |
| [row 5b-1462](section-5b-rows-1451-1475.md) | [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) | Your decision | Questions like 'where is it now' reach a write tool when the question flag is off |
| [row 5b-1477](section-5b-rows-1476-1500.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | New tag-many-views tool overlaps two proven tag tools and is not yet run in Revit |
| [row 5b-1478](section-5b-rows-1476-1500.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | New MEP dimension-many-views tool overlaps proven dimension tools, not yet run in Revit |
| [row 5b-1480](section-5b-rows-1476-1500.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Work plane report names a plan's level in quotes, not as 'Level : Level 1' as promised |
| [row 5b-1481](section-5b-rows-1476-1500.md) | [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) | Revit run on your PC | New CAD-lines-to-ducts tool is untested in Revit; one size question routes to it |
| [row 5b-1482](section-5b-rows-1476-1500.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | Heron cannot change an existing dimension label, so window width/height stayed type-driven |
| [row 5b-1483](section-5b-rows-1476-1500.md) | [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) | Your decision | Trying a draft tool once was refused as 'in use' by the same chat's own lease |
| [row 5b-1484](section-5b-rows-1476-1500.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | Reloading a window family wanted to delete 24 elements, and why is unknown |
| [row 5b-1485](section-5b-rows-1476-1500.md) | [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) | Code fix | Family job hit draft tools (export families, open documents); windows not found in rooms |
| [row 5b-1486](section-5b-rows-1476-1500.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | New sheets-from-spreadsheet tool proven; owner choices on blanks, dates and limits left |
| [row 5b-1488](section-5b-rows-1476-1500.md) | [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) | Code fix | MEP schematic pressure-drop labels show 0.00 on every run; rounding or a wrong read |
| [row 5b-1489](section-5b-rows-1476-1500.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | New rebar-against-spec check tool is not yet run in Revit |
| [row 5b-1493](section-5b-rows-1476-1500.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Tool to lock family 2D lines to planes is proven; the by-hand second check is not run |
| [row 5b-1496](section-5b-rows-1476-1500.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Family 2D read is proven; two sentences still route to a lock tool, not re-measured |
| [row 5b-1497](section-5b-rows-1476-1500.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Linking a family's 2D Visible to a Yes/No is proven; the kept flex check is not run |
| [row 5b-1498](section-5b-rows-1476-1500.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | Labelling a family angle is proven; which plane turns on flex is still not checked |
| [row 5b-1499](section-5b-rows-1476-1500.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | A detail item cannot be placed in a family's own view; Revit 2020 refuses it |
| [row 5b-1505](section-5b-rows-1501-1525.md) | [#912](https://github.com/Ajmalpshaik/Heron-AI/issues/912) | Your decision | Tools cannot mark which outputs are refusals, so repeat 'blocked' shows as unchanged |
| [row 5b-1507](section-5b-rows-1501-1525.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | New click-in-Revit tool is not run, and a click after 60 seconds reaches nobody |
| [row 5b-1510](section-5b-rows-1501-1525.md) | [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) | Code fix | Two test suites can ping the live Revit and send it a release message |
| [row 5b-1511](section-5b-rows-1501-1525.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | heron_resolve says Heron keeps no list of missing things, but it does now |
| [row 5b-1512](section-5b-rows-1501-1525.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | 'put the duct tags above the ducts with a leader' goes to the tag-placing tool |
| [row 5b-1513](section-5b-rows-1501-1525.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | With re-ranker, five rename and place-view sentences reach the wrong tool |
| [row 5b-1514](section-5b-rows-1501-1525.md) | [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) | Your decision | Asking if units are metric or imperial reaches the tool that changes units |
| [row 5b-1515](section-5b-rows-1501-1525.md) | [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) | Code fix | Divide parts reports every part as divided even when the datum cut none |
| [row 5b-1517](section-5b-rows-1501-1525.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Without the re-ranker, eight routing guard sentences reach the wrong tool |
| [row 5b-1518](section-5b-rows-1501-1525.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Five new job methods (issue drawings, health audit, set-up...) are not run in Revit |
| [row 5b-1520](section-5b-rows-1501-1525.md) | [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) | Code fix | Two chats starting together on one store folder slow down and can hit 'database locked' |
| [row 5b-1522](section-5b-rows-1501-1525.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | First lookup after the server starts ranks badly and still claims the trained model |
| [row 5b-1523](section-5b-rows-1501-1525.md) | [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) | Code fix | When a door hands back candidates it shows their risk but not whether they are proven |
| [row 5b-1524](section-5b-rows-1501-1525.md) | [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) | Code fix | Asking to bring an edited spreadsheet back can reach the export or a family card |
| [row 5b-1525](section-5b-rows-1501-1525.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | Bringing a CSV back writes every cell unchecked, overwriting newer values in the model - *#871 merged, the row is still open* |
| [row 5b-1526](section-5b-rows-1526-1550.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | A file the export wrote can be refused whole when brought straight back |
| [row 5b-1527](section-5b-rows-1526-1550.md) | [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) | Revit run on your PC | The CSV file route of a proven parameter card has never been run in real Revit |
| [row 5b-1528](section-5b-rows-1526-1550.md) | [#925](https://github.com/Ajmalpshaik/Heron-AI/issues/925) | Your decision | The round-trip CSV has no fixed encoding, so accented or Arabic names may be written wrong - *#871 merged, the row is still open* |
| [row 5b-1529](section-5b-rows-1526-1550.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No Heron tool makes a structural floor; Create Floor always passes not-structural |
| [row 5b-1530](section-5b-rows-1526-1550.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | Insulation cannot be added to duct or pipe fittings, accessories or flex runs |
| [row 5b-1531](section-5b-rows-1526-1550.md) | [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) | Revit run on your PC | Slanted column placement exists in code but no tool offers it and it was never run |
| [row 5b-1532](section-5b-rows-1526-1550.md) | [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) | Your decision | No Heron tool makes a structural plan view; Create Plan View takes floor or ceiling only |
| [row 5b-1533](section-5b-rows-1526-1550.md) | [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) | Your decision | Constitution Article 9 still says Ask to Keep is on the ribbon; it moved to Settings |
| [row 5b-1534](section-5b-rows-1526-1550.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | Three proven change tools pick a parameter by name at random when two share the name |
| [row 5b-1540](section-5b-rows-1526-1550.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | The module-reach tool skips every bin folder, so it never sees the hooks that import code - *done in #871, merged* |
| [row 5b-1541](section-5b-rows-1526-1550.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The review-ledger tool crashes with a traceback on a machine with no git - *done in #871, merged* |
| [row 5b-1545](section-5b-rows-1526-1550.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | Nothing runs the build-matrix agent, so its baseline drifts from the tree unseen |
| [row 5b-1546](section-5b-rows-1526-1550.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | 120 built agents are called by nothing, and agent proofs name no Revit release |
| [row 5b-1548](section-5b-rows-1526-1550.md) | [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) | Your decision | Proven cards' descriptions still say DRAFT; removing the text moved routing answers |
| [row 5b-1550](section-5b-rows-1526-1550.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | The fitting-family method says routing preference sets a size window; the card does not |
| [row 5b-1551](section-5b-rows-1551-1575.md) | [#947](https://github.com/Ajmalpshaik/Heron-AI/issues/947) | Code fix | The parameter export calls a column blank on every element a missing parameter name |
| [row 5b-1552](section-5b-rows-1551-1575.md) | [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) | Your decision | An add-in agent proof watches only one of the several files that make up the agent |
| [row 5b-1554](section-5b-rows-1551-1575.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Reads put back to DRAFT answer only when named until their Group NK proofs are signed |
| [row 5b-1557](section-5b-rows-1551-1575.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | Three messages say a card with editScopeFailures works in an edit mode; it may not |
| [row 5b-1559](section-5b-rows-1551-1575.md) | [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) | Code fix | An add-in error reply cannot say whether its rollback held, so ROLLED BACK is unconfirmed |
| [row 5b-1560](section-5b-rows-1551-1575.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | The failure classifier files 'nothing came back' on a read as never ran and safe to retry |
| [row 5b-1561](section-5b-rows-1551-1575.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | Heron never flags a reply as an MCP error, and whether any should is unsettled |
| [row 5b-1563](section-5b-rows-1551-1575.md) | [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) | Code fix | Only the main doors print the one-word outcome line; other tools answer in prose only |
| [row 5b-1566](section-5b-rows-1551-1575.md) | [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) | Your decision | A new family connector still takes two calls to add and set up |
| [row 5b-1567](section-5b-rows-1551-1575.md) | [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) | Revit run on your PC | A family connector could not sit on a reference plane with no solid under it |
| [row 5b-1570](section-5b-rows-1551-1575.md) | [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) | Your decision | Sweep of project connector tools found one duplicate pair and five missing jobs |
| [row 5b-1579](row-5b-1579.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | Locking family lines removes Revit's own sketch dimensions and the delete box alarms |
| [row 5b-1582](row-5b-1582.md) | [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) | Your decision | The owner queue tool lists no decision still marked Proposed |
| [row 5b-1583](row-5b-1583.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The tool that archives struck Needs-Checking rows crashes, so nothing can be archived |
| [row 5b-1584](row-5b-1584.md) | [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) | Code fix | A method precondition with a colon is served to the chat as a dictionary, not text |
| [row 5b-1587](row-5b-1587.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The open-defects tool exits 0 even when it could not read the register at all |
| [row 5b-1589](row-5b-1589.md) | [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) | Code fix | Syncing spaces in a non-final phase looks up existing spaces and rooms in the last phase |
| [row 5b-1590](row-5b-1590.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The next-id tool reports dozens of false double holds from helper worktrees - *done in #871, merged* |
| [row 5b-1594](row-5b-1594.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | The add-in runs an export or admin job sent without apply and says nothing was kept |
| [row 5b-1611](row-5b-1611.md) | [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) | Code fix | The heron-guard hook wrongly blocks Revit code edits made inside a worktree |
| [row 5b-1612](row-5b-1612.md) | [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) | Your decision | A dry run replaces values the chat carried between steps, though nothing was kept - *fixed in code in #871, merged; its Revit run is owed in Group NT* |
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
| [row 5b-1845](row-5b-1845.md) | [#938](https://github.com/Ajmalpshaik/Heron-AI/issues/938) | Code fix | The archive re-points links to a re-banded row everywhere but in the register's own folder |
| [row 5b-1848](row-5b-1848.md) | [#1039](https://github.com/Ajmalpshaik/Heron-AI/issues/1039) | Code fix | The new grids-and-levels check ranks first for "bring the grids in from the architect" |
| [row 5b-1850](row-5b-1850.md) | [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) | Revit run on your PC | A tower's low-angle camera measured its eye from the roof; fixed in #1024, DRAFT v6, the run is owed |
| [row 5b-1700](row-5b-1700.md) | [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) | Your decision | The delete box also asks about elements Revit deletes and remakes in the same run; whether it should is the owner's |

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
| [row 5b-429](section-5b-rows-426-450.md) | CREATE_FAMILY_SWEEP is DEPRECATED, merged into CREATE_FAMILY_FORMS, whose card gives the circle, rect, polygon and corner-with-arc profiles for every row and which two coordinates each plane uses |
| [row 5b-469](section-5b-rows-451-475.md) | GROUP_BY_ASSEMBLY is DEPRECATED, merged into REPORT_NESTED_FAMILIES on 2026-11-01 (row 5b-1362), so one card does the job |
| [row 5b-1182](section-5b-rows-1176-1200.md) | all three cards are PROVEN now - set-global-parameter and report-category-overrides (proofs 2026-11-02), set-view-filter-categories (2026-10-18) - and none is on check-signatures' stale list, so revit_change keeps their changes |
| [row 5b-1262](section-5b-rows-1251-1275.md) | row 5b-1575, CLOSED 2026-11-12: SET_FAMILY_TYPE_VALUES version 4 writes an air flow and any other measured kind, PROVEN on Revit 2020 and Revit 2024, signed Ajmal PS |
| [row 5b-1268](section-5b-rows-1251-1275.md) | the three leftover phrases were asked on the owner's PC with the trained model and the re-ranker (docs/needs-checking/group-ib.md, "Row 5b-1268's three phrases on this PC") and each reached the card it wants; (3) stays a coin toss by design, as the row said |
| [row 5b-1309](section-5b-rows-1301-1325.md) | fixed by row 5b-1415: SET_RAILING_TYPE reads the primary handrail only when the type has one, and version 3 is PROVEN on 2026-11-14 ("handrail none, read back as asked") |
| [row 5b-1310](section-5b-rows-1301-1325.md) | SWITCH_ACTIVE_PROJECT is DEPRECATED, merged into ACTIVATE_DOCUMENT (PROVEN), which does not carry viewName (activate-document/fragment.yaml: "viewName NOT CARRIED ... (row 5b-1310)"); OPEN_VIEW, PROVEN, opens the view |
| [row 5b-1401](section-5b-rows-1401-1425.md) | row 5b-1412, FIXED 2026-11-02: "the three roles, in the cards' own text; row 5b-1401 with it" - networkJson is role: accounting, and REPORT_SPRINKLER_NETWORK is PROVEN on its 2026-11-02 proof |
| [row 5b-1568](section-5b-rows-1551-1575.md) | row 5b-1580 ran the probe on 2026-11-12 on Revit 2024 and recorded it as an API LIMIT - "closed, nothing to build" |
| [row 5b-1217](section-5b-rows-1201-1225.md) | The same defect as row 5b-1219, FIXED 2026-10-19: `tests/test_mcp_serves.py` reads the card's own `heron-status` instead of a typed DRAFT. Its state was left as it was because pull request #871 edited that line; #871 merged 2026-11-17, and the state now begins `CLOSED 2026-11-14`. |

## Done by pull requests merged on 2026-11-16

These rows were in progress when the issues were made, and the pull requests below merged the same day. Each is ticked in its issue; the Revit run each still owes is in its NEEDS-CHECKING group, not here.

- **[#886](https://github.com/Ajmalpshaik/Heron-AI/pull/886)** - 5b-1710, 5b-1711
- **[#887](https://github.com/Ajmalpshaik/Heron-AI/pull/887)** - 5b-1707, 5b-1730, 5b-1731, 5b-1732, 5b-1733, 5b-1736
- **[#889](https://github.com/Ajmalpshaik/Heron-AI/pull/889)** - 5b-1703, 5b-1704, 5b-1705, 5b-1706, 5b-1708
- **[#890](https://github.com/Ajmalpshaik/Heron-AI/pull/890)** - 5b-1670, 5b-1672, 5b-1673, 5b-1674
- **[#895](https://github.com/Ajmalpshaik/Heron-AI/pull/895)** - 5b-1739, 5b-1740, 5b-1821

## Left as they were, for open pull requests

None is left. On 2026-11-16 the rows below still began `OPEN` because open pull request [#871](https://github.com/Ajmalpshaik/Heron-AI/pull/871) edited each one. #871 merged on 2026-11-17, and on 2026-11-19 each row was checked again on `main`:

- **Finished, and ticked in its issue** - each state cell begins `FIXED` or `CLOSED`: 5b-427, 5b-1166, 5b-1170, 5b-1172, 5b-1173, 5b-1174, 5b-1179, 5b-1220, 5b-1540, 5b-1541, 5b-1590. Also 5b-1612, whose state begins `FIXED IN CODE`: it is ticked as the section above ticks such rows, and its Revit run is owed in [Group NT](../needs-checking/group-nt.md), NT12 to NT15.
- **Still `OPEN` on `main`, not ticked** - 5b-1131, 5b-1161, 5b-1171, 5b-1178, 5b-1525, 5b-1528. #871 repaired part of some of them; each row says what is left, and each stays in its issue.
- **5b-1672** was listed here too, but [#890](https://github.com/Ajmalpshaik/Heron-AI/pull/890) had done it; it is ticked in [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918).
- Row 5b-1217, also edited by #871, is closed (above) and has no issue.

## Questions and proposals - [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) to [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967)

Each stays in full where it is. Pull request [#871](https://github.com/Ajmalpshaik/Heron-AI/pull/871), merged 2026-11-17, recorded a decision for every one of them but F43. On 2026-11-19 each was checked on `main` - its heading starts ✅, and it has an **Answer:** paragraph or a DECIDED mark - and ticked in its issue. None got a tracking line in its file, because #871 was editing those same entries.

| Entry | Issue | File | State on 2026-11-19 |
|---|---|---|---|
| Q-69 | [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ answered 2026-11-16 by the owner; ticked |
| Q-88 | [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; read back and answered 2026-11-16 by the owner, D-175; ticked |
| Q-89 | [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| F44 | [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) | [`proposals/f44.md`](../proposals/f44.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| Q-72 | [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-73 | [#962](https://github.com/Ajmalpshaik/Heron-AI/issues/962) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ answered 2026-11-16 by the owner; ticked |
| Q-60 | [#963](https://github.com/Ajmalpshaik/Heron-AI/issues/963) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ answered 2026-11-16 by the owner; ticked |
| Q-71 | [#963](https://github.com/Ajmalpshaik/Heron-AI/issues/963) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-78 | [#963](https://github.com/Ajmalpshaik/Heron-AI/issues/963) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-86 | [#963](https://github.com/Ajmalpshaik/Heron-AI/issues/963) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| F1 | [#963](https://github.com/Ajmalpshaik/Heron-AI/issues/963) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| Q-70 | [#963](https://github.com/Ajmalpshaik/Heron-AI/issues/963) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-58 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-61 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ answered 2026-11-16 by the owner; ticked |
| Q-77 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ answered 2026-11-16 by the owner; ticked |
| Q-79 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ answered 2026-11-16 by the owner; ticked |
| Q-80 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-81 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| F6 | [#964](https://github.com/Ajmalpshaik/Heron-AI/issues/964) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| Q-74 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-75 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-76 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| F10 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F19 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F20 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F23 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ answered 2026-11-16 by the owner; ticked |
| F22 | [#965](https://github.com/Ajmalpshaik/Heron-AI/issues/965) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| Q-87 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| F11 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F12 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F13 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F14 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F4 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F2 | [#966](https://github.com/Ajmalpshaik/Heron-AI/issues/966) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| Q-82 | [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| Q-85 | [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967) | [`open-questions/tier-2.md`](../open-questions/tier-2.md) | ✅ decided 2026-11-15 on the owner's delegation; ticked |
| F7 | [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F9 | [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967) | [`proposals/part-f.md`](../proposals/part-f.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F42 | [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967) | [`proposals/f42.md`](../proposals/f42.md) | ✅ decided 2026-11-15 (owner's delegation); ticked |
| F43 | [#967](https://github.com/Ajmalpshaik/Heron-AI/issues/967) | [`proposals/f43.md`](../proposals/f43.md) | Open - 🟠; F43-1 and F43-4 decided 2026-11-15, the owner answered F43-2, 3, 5 and 6 on 2026-11-16, and still owed are F43-2's six answers and the copies F43-3 and F43-6 wait for. #967 stays open for it |
| Q-93 | [#1038](https://github.com/Ajmalpshaik/Heron-AI/issues/1038) | [`open-questions/tier-3.md`](../open-questions/tier-3.md) | Open since 2026-11-17, held for a discussion with the owner |

## NEEDS-CHECKING groups - [#968](https://github.com/Ajmalpshaik/Heron-AI/issues/968) to [#1004](https://github.com/Ajmalpshaik/Heron-AI/issues/1004)

Each group's file starts with a line naming its issue. The checks still owed are listed in the issue, group by group.

| Issue | Subject | Groups |
|---|---|---|
| [#968](https://github.com/Ajmalpshaik/Heron-AI/issues/968) | Revit run: add-in bridge and how jobs run | [A](../needs-checking/group-a.md), [EI](../needs-checking/group-ei.md), [EJ](../needs-checking/group-ej.md), [EL](../needs-checking/group-el.md), [EM](../needs-checking/group-em.md), [EZ](../needs-checking/group-ez.md), [F](../needs-checking/group-f.md), [J](../needs-checking/group-j.md), [NP](../needs-checking/group-np.md), [NW](../needs-checking/group-nw.md) |
| [#969](https://github.com/Ajmalpshaik/Heron-AI/issues/969) | Revit run: change safety, dry runs, undo and run guard | [E](../needs-checking/group-e.md), [NT](../needs-checking/group-nt.md), [NU](../needs-checking/group-nu.md), [EO](../needs-checking/group-eo.md), [EX](../needs-checking/group-ex.md), [NS](../needs-checking/group-ns.md), [EV](../needs-checking/group-ev.md), [G](../needs-checking/group-g.md), [HL](../needs-checking/group-hl.md) |
| [#970](https://github.com/Ajmalpshaik/Heron-AI/issues/970) | Revit run: Keep or Discard, safety switches, permissions and saving | [ET](../needs-checking/group-et.md), [HG](../needs-checking/group-hg.md), [OB](../needs-checking/group-ob.md), [BF](../needs-checking/group-bf.md), [C](../needs-checking/group-c.md), [AH](../needs-checking/group-ah.md), [EU](../needs-checking/group-eu.md), [MS](../needs-checking/group-ms.md), [HN](../needs-checking/group-hn.md), [EN](../needs-checking/group-en.md) |
| [#971](https://github.com/Ajmalpshaik/Heron-AI/issues/971) | Revit run: two chats, the Companion page, ribbon and windows | [H](../needs-checking/group-h.md), [HK](../needs-checking/group-hk.md), [BK](../needs-checking/group-bk.md), [JU](../needs-checking/group-ju.md), [JT](../needs-checking/group-jt.md), [B](../needs-checking/group-b.md), [Z](../needs-checking/group-z.md), [HF](../needs-checking/group-hf.md) |
| [#972](https://github.com/Ajmalpshaik/Heron-AI/issues/972) | PC run: installer, Setup and PC set-up | [AA](../needs-checking/group-aa.md), [AB](../needs-checking/group-ab.md), [AC](../needs-checking/group-ac.md), [AD](../needs-checking/group-ad.md), [AE](../needs-checking/group-ae.md), [AF](../needs-checking/group-af.md), [EP](../needs-checking/group-ep.md), [OA](../needs-checking/group-oa.md), [AK](../needs-checking/group-ak.md), [AI](../needs-checking/group-ai.md) |
| [#973](https://github.com/Ajmalpshaik/Heron-AI/issues/973) | Revit run: proving tools and every Revit release | [NI](../needs-checking/group-ni.md), [NM](../needs-checking/group-nm.md), [OF](../needs-checking/group-of.md), [U](../needs-checking/group-u.md), [IW](../needs-checking/group-iw.md), [EQ](../needs-checking/group-eq.md), [GW](../needs-checking/group-gw.md) |
| [#974](https://github.com/Ajmalpshaik/Heron-AI/issues/974) | Revit run: cards repaired from field reports, run again | [NX](../needs-checking/group-nx.md), [NK](../needs-checking/group-nk.md), [NL](../needs-checking/group-nl.md), [OC](../needs-checking/group-oc.md), [OD](../needs-checking/group-od.md), [OE](../needs-checking/group-oe.md), [OH](../needs-checking/group-oh.md), [NY](../needs-checking/group-ny.md) |
| [#975](https://github.com/Ajmalpshaik/Heron-AI/issues/975) | Revit run: agents, skills, QA checklists and owner review | [K](../needs-checking/group-k.md), [AG](../needs-checking/group-ag.md), [R](../needs-checking/group-r.md), [X](../needs-checking/group-x.md), [EC](../needs-checking/group-ec.md), [MV](../needs-checking/group-mv.md), [NV](../needs-checking/group-nv.md) |
| [#976](https://github.com/Ajmalpshaik/Heron-AI/issues/976) | Revit run: family editor - forms, links and visibility | [AN](../needs-checking/group-an.md), [BD](../needs-checking/group-bd.md), [BL](../needs-checking/group-bl.md), [BM](../needs-checking/group-bm.md), [BN](../needs-checking/group-bn.md), [BO](../needs-checking/group-bo.md), [BP](../needs-checking/group-bp.md), [BQ](../needs-checking/group-bq.md) |
| [#977](https://github.com/Ajmalpshaik/Heron-AI/issues/977) | Revit run: family editor - nested parts, settings and family kinds | [BR](../needs-checking/group-br.md), [BS](../needs-checking/group-bs.md), [BV](../needs-checking/group-bv.md), [BY](../needs-checking/group-by.md), [BZ](../needs-checking/group-bz.md), [CA](../needs-checking/group-ca.md), [CB](../needs-checking/group-cb.md), [CM](../needs-checking/group-cm.md) |
| [#978](https://github.com/Ajmalpshaik/Heron-AI/issues/978) | Revit run: family editor - freeform, text and pictures | [CU](../needs-checking/group-cu.md), [CV](../needs-checking/group-cv.md), [CW](../needs-checking/group-cw.md), [DA](../needs-checking/group-da.md), [DC](../needs-checking/group-dc.md), [EY](../needs-checking/group-ey.md), [IQ](../needs-checking/group-iq.md), [JB](../needs-checking/group-jb.md) |
| [#979](https://github.com/Ajmalpshaik/Heron-AI/issues/979) | Revit run: family editor - 2D lines, locks and labels | [NA](../needs-checking/group-na.md), [NB](../needs-checking/group-nb.md), [NC](../needs-checking/group-nc.md), [ND](../needs-checking/group-nd.md), [NE](../needs-checking/group-ne.md), [NF](../needs-checking/group-nf.md), [NN](../needs-checking/group-nn.md), [IS](../needs-checking/group-is.md) |
| [#980](https://github.com/Ajmalpshaik/Heron-AI/issues/980) | Revit run: family parameters, types and loading | [CN](../needs-checking/group-cn.md), [CI](../needs-checking/group-ci.md), [CL](../needs-checking/group-cl.md), [CO](../needs-checking/group-co.md), [CQ](../needs-checking/group-cq.md), [DB](../needs-checking/group-db.md), [CP](../needs-checking/group-cp.md), [CS](../needs-checking/group-cs.md), [DJ](../needs-checking/group-dj.md), [GC](../needs-checking/group-gc.md) |
| [#981](https://github.com/Ajmalpshaik/Heron-AI/issues/981) | Revit run: family placement and MEP families | [CH](../needs-checking/group-ch.md), [FP](../needs-checking/group-fp.md), [CR](../needs-checking/group-cr.md), [DD](../needs-checking/group-dd.md), [IR](../needs-checking/group-ir.md), [JH](../needs-checking/group-jh.md), [JN](../needs-checking/group-jn.md), [IK](../needs-checking/group-ik.md) |
| [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982) | Revit run: parameters - reading and writing values | [BB](../needs-checking/group-bb.md), [CD](../needs-checking/group-cd.md), [DI](../needs-checking/group-di.md), [DW](../needs-checking/group-dw.md), [EB](../needs-checking/group-eb.md), [EK](../needs-checking/group-ek.md), [FE](../needs-checking/group-fe.md), [HP](../needs-checking/group-hp.md) |
| [#983](https://github.com/Ajmalpshaik/Heron-AI/issues/983) | Revit run: parameters - shared parameters, CSV and project info | [HZ](../needs-checking/group-hz.md), [II](../needs-checking/group-ii.md), [IU](../needs-checking/group-iu.md), [JD](../needs-checking/group-jd.md), [JF](../needs-checking/group-jf.md), [NG](../needs-checking/group-ng.md), [NJ](../needs-checking/group-nj.md), [P](../needs-checking/group-p.md), [DE](../needs-checking/group-de.md), [DF](../needs-checking/group-df.md), [LV](../needs-checking/group-lv.md) |
| [#984](https://github.com/Ajmalpshaik/Heron-AI/issues/984) | Revit run: tags | [AY](../needs-checking/group-ay.md), [DG](../needs-checking/group-dg.md), [EG](../needs-checking/group-eg.md), [IJ](../needs-checking/group-ij.md), [KH](../needs-checking/group-kh.md), [KI](../needs-checking/group-ki.md), [MC](../needs-checking/group-mc.md), [MT](../needs-checking/group-mt.md) |
| [#985](https://github.com/Ajmalpshaik/Heron-AI/issues/985) | Revit run: text notes, annotation and dimensions | [FD](../needs-checking/group-fd.md), [HE](../needs-checking/group-he.md), [IL](../needs-checking/group-il.md), [DL](../needs-checking/group-dl.md), [KT](../needs-checking/group-kt.md), [EF](../needs-checking/group-ef.md), [EH](../needs-checking/group-eh.md), [LR](../needs-checking/group-lr.md), [MA](../needs-checking/group-ma.md), [MU](../needs-checking/group-mu.md) |
| [#986](https://github.com/Ajmalpshaik/Heron-AI/issues/986) | Revit run: schedules | [AT](../needs-checking/group-at.md), [AZ](../needs-checking/group-az.md), [FY](../needs-checking/group-fy.md), [IG](../needs-checking/group-ig.md), [IH](../needs-checking/group-ih.md), [KK](../needs-checking/group-kk.md) |
| [#987](https://github.com/Ajmalpshaik/Heron-AI/issues/987) | Revit run: views and sheets | [AU](../needs-checking/group-au.md), [AW](../needs-checking/group-aw.md), [DN](../needs-checking/group-dn.md), [DT](../needs-checking/group-dt.md), [FC](../needs-checking/group-fc.md), [FG](../needs-checking/group-fg.md), [JE](../needs-checking/group-je.md), [MR](../needs-checking/group-mr.md), [KA](../needs-checking/group-ka.md) |
| [#988](https://github.com/Ajmalpshaik/Heron-AI/issues/988) | Revit run: sheets, revisions, legends and keynotes | [CY](../needs-checking/group-cy.md), [EA](../needs-checking/group-ea.md), [MY](../needs-checking/group-my.md), [FB](../needs-checking/group-fb.md), [FA](../needs-checking/group-fa.md), [BA](../needs-checking/group-ba.md), [CZ](../needs-checking/group-cz.md), [MP](../needs-checking/group-mp.md), [NZ](../needs-checking/group-nz.md), [LZ](../needs-checking/group-lz.md) |
| [#989](https://github.com/Ajmalpshaik/Heron-AI/issues/989) | Revit run: view filters, visibility and object styles | [AX](../needs-checking/group-ax.md), [BC](../needs-checking/group-bc.md), [EE](../needs-checking/group-ee.md), [DV](../needs-checking/group-dv.md), [ED](../needs-checking/group-ed.md), [DY](../needs-checking/group-dy.md), [DZ](../needs-checking/group-dz.md), [FU](../needs-checking/group-fu.md), [MB](../needs-checking/group-mb.md) |
| [#990](https://github.com/Ajmalpshaik/Heron-AI/issues/990) | Revit run: view pictures, rendering and cameras | [ER](../needs-checking/group-er.md), [IP](../needs-checking/group-ip.md), [IV](../needs-checking/group-iv.md), [IX](../needs-checking/group-ix.md), [IB](../needs-checking/group-ib.md), [IC](../needs-checking/group-ic.md), [IF](../needs-checking/group-if.md), [IM](../needs-checking/group-im.md), [JC](../needs-checking/group-jc.md), [AV](../needs-checking/group-av.md) |
| [#991](https://github.com/Ajmalpshaik/Heron-AI/issues/991) | Revit run: linked models | [BE](../needs-checking/group-be.md), [BG](../needs-checking/group-bg.md), [BH](../needs-checking/group-bh.md), [BI](../needs-checking/group-bi.md), [BJ](../needs-checking/group-bj.md), [DO](../needs-checking/group-do.md), [L](../needs-checking/group-l.md), [MK](../needs-checking/group-mk.md), [GH](../needs-checking/group-gh.md) |
| [#992](https://github.com/Ajmalpshaik/Heron-AI/issues/992) | Revit run: images, lines, work planes and selection | [LF](../needs-checking/group-lf.md), [LG](../needs-checking/group-lg.md), [MO](../needs-checking/group-mo.md), [LM](../needs-checking/group-lm.md), [LL](../needs-checking/group-ll.md), [DH](../needs-checking/group-dh.md), [IT](../needs-checking/group-it.md), [ES](../needs-checking/group-es.md), [NH](../needs-checking/group-nh.md) |
| [#993](https://github.com/Ajmalpshaik/Heron-AI/issues/993) | Revit run: walls, floors and roofs | [DK](../needs-checking/group-dk.md), [DP](../needs-checking/group-dp.md), [DR](../needs-checking/group-dr.md), [DS](../needs-checking/group-ds.md), [DX](../needs-checking/group-dx.md), [DU](../needs-checking/group-du.md), [JG](../needs-checking/group-jg.md), [HM](../needs-checking/group-hm.md), [LY](../needs-checking/group-ly.md), [LT](../needs-checking/group-lt.md), [AP](../needs-checking/group-ap.md) |
| [#994](https://github.com/Ajmalpshaik/Heron-AI/issues/994) | Revit run: curtain walls, massing and adaptive components | [AR](../needs-checking/group-ar.md), [JI](../needs-checking/group-ji.md), [JJ](../needs-checking/group-jj.md), [JM](../needs-checking/group-jm.md), [JP](../needs-checking/group-jp.md), [JR](../needs-checking/group-jr.md), [JL](../needs-checking/group-jl.md), [JO](../needs-checking/group-jo.md), [JK](../needs-checking/group-jk.md), [JQ](../needs-checking/group-jq.md) |
| [#995](https://github.com/Ajmalpshaik/Heron-AI/issues/995) | Revit run: rooms, spaces, phases and groups | [HT](../needs-checking/group-ht.md), [HX](../needs-checking/group-hx.md), [CF](../needs-checking/group-cf.md), [AO](../needs-checking/group-ao.md), [LJ](../needs-checking/group-lj.md), [LK](../needs-checking/group-lk.md), [M](../needs-checking/group-m.md), [JS](../needs-checking/group-js.md), [CT](../needs-checking/group-ct.md), [S](../needs-checking/group-s.md) |
| [#996](https://github.com/Ajmalpshaik/Heron-AI/issues/996) | Revit run: project set-up, materials and worksharing | [CK](../needs-checking/group-ck.md), [CJ](../needs-checking/group-cj.md), [MD](../needs-checking/group-md.md), [CX](../needs-checking/group-cx.md), [IA](../needs-checking/group-ia.md), [GP](../needs-checking/group-gp.md), [KN](../needs-checking/group-kn.md), [KO](../needs-checking/group-ko.md), [LW](../needs-checking/group-lw.md), [LN](../needs-checking/group-ln.md) |
| [#997](https://github.com/Ajmalpshaik/Heron-AI/issues/997) | Revit run: structure, stairs and site | [HO](../needs-checking/group-ho.md), [JZ](../needs-checking/group-jz.md), [KB](../needs-checking/group-kb.md), [KC](../needs-checking/group-kc.md), [KD](../needs-checking/group-kd.md), [DQ](../needs-checking/group-dq.md), [BX](../needs-checking/group-bx.md), [HS](../needs-checking/group-hs.md), [LS](../needs-checking/group-ls.md), [MJ](../needs-checking/group-mj.md), [IY](../needs-checking/group-iy.md) |
| [#998](https://github.com/Ajmalpshaik/Heron-AI/issues/998) | Revit run: structural floors and loads | [KL](../needs-checking/group-kl.md), [KM](../needs-checking/group-km.md), [KP](../needs-checking/group-kp.md), [KZ](../needs-checking/group-kz.md), [LA](../needs-checking/group-la.md), [LB](../needs-checking/group-lb.md), [ML](../needs-checking/group-ml.md) |
| [#999](https://github.com/Ajmalpshaik/Heron-AI/issues/999) | Revit run: structural settings, analytical model and steel | [LC](../needs-checking/group-lc.md), [ME](../needs-checking/group-me.md), [MF](../needs-checking/group-mf.md), [MG](../needs-checking/group-mg.md), [LD](../needs-checking/group-ld.md), [LE](../needs-checking/group-le.md) |
| [#1000](https://github.com/Ajmalpshaik/Heron-AI/issues/1000) | Revit run: rebar | [HY](../needs-checking/group-hy.md), [KE](../needs-checking/group-ke.md), [KF](../needs-checking/group-kf.md), [KQ](../needs-checking/group-kq.md), [KR](../needs-checking/group-kr.md), [KS](../needs-checking/group-ks.md), [LX](../needs-checking/group-lx.md), [MN](../needs-checking/group-mn.md), [MZ](../needs-checking/group-mz.md) |
| [#1001](https://github.com/Ajmalpshaik/Heron-AI/issues/1001) | Revit run: MEP systems and routing | [AJ](../needs-checking/group-aj.md), [DM](../needs-checking/group-dm.md), [JW](../needs-checking/group-jw.md), [KG](../needs-checking/group-kg.md), [N](../needs-checking/group-n.md), [AL](../needs-checking/group-al.md), [LQ](../needs-checking/group-lq.md), [LP](../needs-checking/group-lp.md), [MX](../needs-checking/group-mx.md) |
| [#1002](https://github.com/Ajmalpshaik/Heron-AI/issues/1002) | Revit run: MEP fabrication, insulation, sizes and schematics | [LO](../needs-checking/group-lo.md), [MH](../needs-checking/group-mh.md), [JX](../needs-checking/group-jx.md), [JY](../needs-checking/group-jy.md), [KX](../needs-checking/group-kx.md), [KY](../needs-checking/group-ky.md), [AM](../needs-checking/group-am.md), [MW](../needs-checking/group-mw.md), [NR](../needs-checking/group-nr.md) |
| [#1003](https://github.com/Ajmalpshaik/Heron-AI/issues/1003) | Revit run: electrical | [MI](../needs-checking/group-mi.md), [JV](../needs-checking/group-jv.md), [LI](../needs-checking/group-li.md), [LH](../needs-checking/group-lh.md), [KU](../needs-checking/group-ku.md), [KV](../needs-checking/group-kv.md), [KW](../needs-checking/group-kw.md) |
| [#1004](https://github.com/Ajmalpshaik/Heron-AI/issues/1004) | Revit run: HVAC, fire protection and energy | [AQ](../needs-checking/group-aq.md), [BT](../needs-checking/group-bt.md), [BU](../needs-checking/group-bu.md), [CG](../needs-checking/group-cg.md), [CE](../needs-checking/group-ce.md), [CC](../needs-checking/group-cc.md), [LU](../needs-checking/group-lu.md), [MM](../needs-checking/group-mm.md) |
| [#1015](https://github.com/Ajmalpshaik/Heron-AI/issues/1015) | Tidy-up: strike checks already done | [IZ](../needs-checking/group-iz.md), [NO](../needs-checking/group-no.md), [NQ](../needs-checking/group-nq.md) |
| [#1036](https://github.com/Ajmalpshaik/Heron-AI/issues/1036) | Revit run: grids and levels against a link, the new QA reports and office checklists | [OJ](../needs-checking/group-oj.md) |

## Work-note items - [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) to [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014)

Each item's line in its note ends with *(Tracked as GitHub issue ...)* and its WN id, so searching for the id finds it.

| Item | Issue | File | What |
|---|---|---|---|
| WN-1 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](../work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | C01: room floor on Revit 2020-2021 still slabs over a shaft instead of refusing |
| WN-2 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](../work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | C02: duct sizing prints 'UNIT CHECK ... STOP' then writes sizes anyway |
| WN-3 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](../work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | C07: room geometry flattens every hole into one list; keep each hole as its own loop |
| WN-4 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](../work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | C08: connector report gives text only; add structured connector measurements |
| WN-5 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/earlier-brain/02-work-packages.md`](../work-notes/plans/earlier-brain/02-work-packages.md) | C8.2: 'changed in central?' column in the element-ownership reports |
| WN-6 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/earlier-brain/02-work-packages.md`](../work-notes/plans/earlier-brain/02-work-packages.md) | C8.3: placeholder sheets fragment; create-sheet-list card wrongly says CREATE_SHEETS makes them |
| WN-7 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`HANDOVER.md`](../HANDOVER.md) | Dropped-count marker (D-64) missing on reading tools that filter candidates |
| WN-8 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`HANDOVER.md`](../HANDOVER.md) | Brain always opens the GLOBAL store, so a project knowledge store never takes part |
| WN-9 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/investigations/2026-11-15-revit-mcp-landscape.md`](../work-notes/investigations/2026-11-15-revit-mcp-landscape.md) | Make the Delete box count what Revit really deletes (dependants included) |
| WN-10 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/investigations/2026-11-15-revit-mcp-landscape.md`](../work-notes/investigations/2026-11-15-revit-mcp-landscape.md) | Tie each approval to the exact request and the model's state: one use, a few minutes |
| WN-11 | [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) | [`work-notes/plans/facade-capability.md`](../work-notes/plans/facade-capability.md) | Facade NOT YET: remove a grid line or segment, move a mullion, Automatically Embed, flatness |
| WN-12 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-16/R-25: build the usage counter D-70 decided, then use it in ranking |
| WN-13 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-30: build the cloud-embedding opt-in, split by content type (Q-54) |
| WN-14 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-43: context packet should also carry confidence and what is missing (citation already done) |
| WN-15 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-73/R-77: every fallback says so; installs named, sized and announced first |
| WN-16 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-78: check that an installed package is the right version, not only present |
| WN-17 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-79: a system requirements section with the total disk cost |
| WN-18 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-85 to R-87: record each document's licence, warn before sharing, never ship a store |
| WN-19 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-88/R-88a: out-of-box knowledge order, and no standard ever bundled, as written rules |
| WN-20 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-89 to R-92: tell a modeller in plain words where knowledge goes and that files are safe |
| WN-21 | [#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-93: BIM standards as a first-class document kind |
| WN-22 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/README.md`](../work-notes/README.md) | Retire next-steps note: every open step already has a register home |
| WN-23 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/README.md`](../work-notes/README.md) | Retire the-sweep-2026-09-13 map: its backlog table is overtaken *(no tracking line yet: #871 edited the lines beside it, and merged 2026-11-17)* |
| WN-24 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/README.md`](../work-notes/README.md) | Retire project-review-2026-10-08: what waves 3-6 owe is in register rows |
| WN-25 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/README.md`](../work-notes/README.md) | Give the four FINAL loads and sprinkler plans a retirement condition, or retire them |
| WN-26 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/README.md`](../work-notes/README.md) | Bring stale work-notes README rows up to date (building-loads, family-2d) |
| WN-27 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/investigations/rag-engineering-practices-2026-09-10.md`](../work-notes/investigations/rag-engineering-practices-2026-09-10.md) | Record the built fabrication check (heron_ground) as BUILT in docs/34 |
| WN-28 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/investigations/rag-state-of-the-art-2026-09-10.md`](../work-notes/investigations/rag-state-of-the-art-2026-09-10.md) | Fold the second RAG reading into docs/34, then retire it |
| WN-29 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/investigations/2026-11-01-revit-ribbon-coverage.md`](../work-notes/investigations/2026-11-01-revit-ribbon-coverage.md) | Move the ribbon note's 'API cannot', 'UNSURE' and tab tables somewhere permanent, then retire |
| WN-30 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`work-notes/investigations/2026-11-15-revit-mcp-landscape.md`](../work-notes/investigations/2026-11-15-revit-mcp-landscape.md) | File the landscape's untaken ideas (6, 9, 11, 13-15 and the small ones) before retiring it |
| WN-31 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`HANDOVER.md`](../HANDOVER.md) | Read specification parts 00 to 00d line by line against the tree |
| WN-32 | [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007) | [`HANDOVER.md`](../HANDOVER.md) | Bring HANDOVER's numbered PC list up to date: most items ran on 2026-11-13 and 14 |
| WN-33 | [#1008](https://github.com/Ajmalpshaik/Heron-AI/issues/1008) | [`work-notes/README.md`](../work-notes/README.md) | Retire the fragment-validation-agent prompt: it looks finished |
| WN-34 | [#1008](https://github.com/Ajmalpshaik/Heron-AI/issues/1008) | [`work-notes/README.md`](../work-notes/README.md) | Retire agent-build-order note: every block built |
| WN-35 | [#1008](https://github.com/Ajmalpshaik/Heron-AI/issues/1008) | [`work-notes/plans/facade-capability.md`](../work-notes/plans/facade-capability.md) | Retire the facade note: rule on JI-JL, re-point the method, cards and groups |
| WN-36 | [#1008](https://github.com/Ajmalpshaik/Heron-AI/issues/1008) | [`work-notes/investigations/2026-11-15-revit-mcp-landscape.md`](../work-notes/investigations/2026-11-15-revit-mcp-landscape.md) | Owner reads the Revit MCP landscape note, then it is retired |
| WN-37 | [#1008](https://github.com/Ajmalpshaik/Heron-AI/issues/1008) | [`work-notes/plans/improvement-gate-execution-record.md`](../work-notes/plans/improvement-gate-execution-record.md) | Improvement Gate used by someone who did not build it (Golden Rule 7); then retire |
| WN-38 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/plans/next-steps-2026-09-12.md`](../work-notes/plans/next-steps-2026-09-12.md) | Give Heron one real numbered standard (a QCS section PDF) to test the parser |
| WN-39 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/plans/next-steps-2026-09-12.md`](../work-notes/plans/next-steps-2026-09-12.md) | Does the usage counter store the question text, or only a count per clause? |
| WN-40 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/plans/rag/00-structure.md`](../work-notes/plans/rag/00-structure.md) | Write the six RAG structure choices as a numbered decision |
| WN-41 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-75/R-76: is a pointer to the manifest enough, or must the README list sizes and uses? |
| WN-42 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/investigations/rag-engineering-practices-2026-09-10.md`](../work-notes/investigations/rag-engineering-practices-2026-09-10.md) | Owner's call: a capped query-rewrite loop, and who loops (brain or host) |
| WN-43 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/investigations/rag-engineering-practices-2026-09-10.md`](../work-notes/investigations/rag-engineering-practices-2026-09-10.md) | Owner's call: should document chunks overlap? |
| WN-44 | [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) | [`work-notes/investigations/rag-engineering-practices-2026-09-10.md`](../work-notes/investigations/rag-engineering-practices-2026-09-10.md) | Owner's call: time each retrieval stage, not only the whole call |
| WN-45 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/01-requirements.md`](../work-notes/plans/plugin-extension/01-requirements.md) | R-52 says the installer asks where to keep Heron; D-167's Setup uses a fixed folder |
| WN-46 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/02-implementation.md`](../work-notes/plans/plugin-extension/02-implementation.md) | Stage 9 Settings panel: hide and show Heron's ribbon panels - not started |
| WN-47 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/03-open-questions.md`](../work-notes/plans/plugin-extension/03-open-questions.md) | Q-PE-1: what goes in 'Heron Tools' - content still open |
| WN-48 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/03-open-questions.md`](../work-notes/plans/plugin-extension/03-open-questions.md) | Q-PE-3: what panels go in Heron Doc? |
| WN-49 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/03-open-questions.md`](../work-notes/plans/plugin-extension/03-open-questions.md) | Q-PE-4: what panels go in Heron MEP? |
| WN-50 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/03-open-questions.md`](../work-notes/plans/plugin-extension/03-open-questions.md) | Q-PE-8: do products share one version number or carry their own? |
| WN-51 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/03-open-questions.md`](../work-notes/plans/plugin-extension/03-open-questions.md) | Q-PE-9: write the .addin manifest at install time, or keep patching a shipped one? |
| WN-52 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/03-open-questions.md`](../work-notes/plans/plugin-extension/03-open-questions.md) | Q-PE-15: how the installer writes an audit line (R-27) |
| WN-53 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/05-phases.md`](../work-notes/plans/plugin-extension/05-phases.md) | Plugin plan Phase 3: the owner has not described it yet |
| WN-54 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/07-debugging-engine.md`](../work-notes/plans/plugin-extension/07-debugging-engine.md) | Q-DE-3: where does a modeller report an error from? |
| WN-55 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/plugin-extension/07-debugging-engine.md`](../work-notes/plans/plugin-extension/07-debugging-engine.md) | Debugging engine: error-report entry point, reproduce, report the diagnosis and stop |
| WN-56 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/windows-setup-2026-11-16.md`](../work-notes/plans/windows-setup-2026-11-16.md) | Sign Heron's own DLLs as well? Revit 2027 logs them Unsigned |
| WN-57 | [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) | [`work-notes/plans/windows-setup-2026-11-16.md`](../work-notes/plans/windows-setup-2026-11-16.md) | Confirm the timestamp claim in Microsoft's Authenticode documentation |
| WN-58 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`HANDOVER.md`](../HANDOVER.md) | A layout Revit refuses part of: place the rest, or roll back all of it? |
| WN-59 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](../work-notes/plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | S01-S05: the five proposed fragment splits wait for discussion |
| WN-60 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/earlier-brain/03-owner-data.md`](../work-notes/plans/earlier-brain/03-owner-data.md) | Owner's site words and grayout values are loaded nowhere since C5 was dropped |
| WN-61 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/plugin-extension/08-lessons-from-the-brain.md`](../work-notes/plans/plugin-extension/08-lessons-from-the-brain.md) | Q-B-1: make 'intent not fact', 'fresh reads' and 'plan, split, execute' standing rules? |
| WN-62 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/plugin-extension/08-lessons-from-the-brain.md`](../work-notes/plans/plugin-extension/08-lessons-from-the-brain.md) | Q-B-2: keep a glossary of the owner's own words? |
| WN-63 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/investigations/2026-11-11-family-2d-capabilities.md`](../work-notes/investigations/2026-11-11-family-2d-capabilities.md) | Decide three family-2D merge candidates (detail vs symbolic lines, two lock cards, two link cards) |
| WN-64 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md) | Scope box on grids, levels and reference planes - widens PROVEN ASSIGN_SCOPE_BOX_TO_VIEW |
| WN-65 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md) | Parts shown or hidden in a view - widens PROVEN SET_VIEW_PROPERTIES |
| WN-66 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md) | Which tool should 'purge unused' reach: FIND_UNUSED_FAMILIES or FIND_PURGEABLE_ELEMENTS? |
| WN-67 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/facade-capability.md`](../work-notes/plans/facade-capability.md) | Widen report-curtain-wall-type to read a curtain system type |
| WN-68 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/investigations/2026-11-01-revit-ribbon-coverage.md`](../work-notes/investigations/2026-11-01-revit-ribbon-coverage.md) | Research the UNSURE ribbon tools before building (precast, steel, P&ID, about 30 more) |
| WN-69 | [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012) | [`work-notes/plans/revit-sitting-2026-11-14.md`](../work-notes/plans/revit-sitting-2026-11-14.md) | May the session press Revit's Keep/Discard boxes and click in Revit (computer use)? |
| WN-70 | [#1013](https://github.com/Ajmalpshaik/Heron-AI/issues/1013) | [`work-notes/plans/companion-model-dashboard-2026-11-01.md`](../work-notes/plans/companion-model-dashboard-2026-11-01.md) | Dashboard phase 2 leftovers: duplicate marks, rule checks, zoom-to, click a bar |
| WN-71 | [#1013](https://github.com/Ajmalpshaik/Heron-AI/issues/1013) | [`work-notes/plans/companion-model-dashboard-2026-11-01.md`](../work-notes/plans/companion-model-dashboard-2026-11-01.md) | Dashboard phase 3: MEP checks, quantities, documentation, links and coordinates |
| WN-72 | [#1013](https://github.com/Ajmalpshaik/Heron-AI/issues/1013) | [`work-notes/plans/companion-model-dashboard-2026-11-01.md`](../work-notes/plans/companion-model-dashboard-2026-11-01.md) | Dashboard phase 4 leftovers: weekly trend chart, submission readiness |
| WN-73 | [#1013](https://github.com/Ajmalpshaik/Heron-AI/issues/1013) | [`work-notes/plans/companion-model-dashboard-2026-11-01.md`](../work-notes/plans/companion-model-dashboard-2026-11-01.md) | Dashboard phase 5: fix from a row, and purge unused families |
| WN-74 | [#1013](https://github.com/Ajmalpshaik/Heron-AI/issues/1013) | [`work-notes/plans/sprinkler-panel-2026-10-04.md`](../work-notes/plans/sprinkler-panel-2026-10-04.md) | Sprinkler phase 2: write flows and sizes back, draw the remote area; phases 3-4 |
| WN-75 | [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014) | [`work-notes/plans/rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md) | R-42: install the re-ranker per user, no admin, on the owner's PC |
| WN-76 | [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014) | [`work-notes/plans/earlier-brain/05-pc-proving.md`](../work-notes/plans/earlier-brain/05-pc-proving.md) | With Changes on, a chat asking to sync with central must be refused (PR #299) |
| WN-77 | [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014) | [`work-notes/plans/plugin-extension/02-implementation.md`](../work-notes/plans/plugin-extension/02-implementation.md) | Install test on a real locked-down machine, not a developer PC |
| WN-78 | [#1014](https://github.com/Ajmalpshaik/Heron-AI/issues/1014) | [`work-notes/plans/companion-model-dashboard-2026-11-01.md`](../work-notes/plans/companion-model-dashboard-2026-11-01.md) | Dashboard 3D view tools (top, front, cut height, save picture) never seen in Revit's browser |

## Found finished on 2026-11-16 - in the other registers

- **Group [MQ](../needs-checking/group-mq.md)** - the group says *"Every MQ row has now held"* (Revit 2024, 2026-11-07) and both cards read PROVEN. Its ids were struck here, and it has no issue.
- **Groups [BW](../needs-checking/group-bw.md), [IO](../needs-checking/group-io.md) and [JA](../needs-checking/group-ja.md)** - every check has run and held, and pull request [#871](https://github.com/Ajmalpshaik/Heron-AI/pull/871), merged 2026-11-17, struck every one of their ids. No issue.
- **Groups [IZ](../needs-checking/group-iz.md), [NO](../needs-checking/group-no.md) and [NQ](../needs-checking/group-nq.md)** - run and held; #871, merged 2026-11-17, struck some of their ids and [#1015](https://github.com/Ajmalpshaik/Heron-AI/issues/1015) holds the rest, to strike after it merges. NO6 there needs the owner's word: it ran on a connector Heron placed, not one placed by hand.
- **Group [IS](../needs-checking/group-is.md)** - not finished after all: its own note says whether IS0 to IS7 are what the signed proof ran is *"for the owner"*. Added to [#979](https://github.com/Ajmalpshaik/Heron-AI/issues/979).
- **Proposals F8, F16, F18 and F21** - each found finished (F5 closed; docs/28 and docs/09 carry the clauses they asked for), and pull request #871, merged 2026-11-17, closed all four - each heading starts ✅. No issue.
- **WN-14** - the note says citations are not built; they are (`brain/heron_context.py`). Its issue line asks only for the confidence and *what is missing* parts.
