# Session note — EVERY REPLY SAYS HOW IT ENDED IN ONE WORD, PROVEN TOOLS THAT ANSWERED WRONGLY ARE REPAIRED AND DRAFT AGAIN, AND A DEFECT ROW GETS A FILE OF ITS OWN

> **Archived session note** from 2026-11-12. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-11-12 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-11-12 — EVERY REPLY SAYS HOW IT ENDED IN ONE WORD, PROVEN TOOLS THAT ANSWERED WRONGLY ARE REPAIRED AND DRAFT AGAIN, AND A DEFECT ROW GETS A FILE OF ITS OWN

Written to the rule in [section 11](../HANDOVER.md#11-handover--2026-09-12): what is NEW, a MISTAKE worth not repeating,
what is still TO DO. It covers the branch `claude/stoic-planck-folhnp` from the evening of 2026-11-11, after
[that day's note](../handover-archive/2026-11-11-a-click-in-revit-answers-the-chat-five-everyday-jobs-get-a.md)
was written, to 2026-11-12. Everything on the branch was built in a cloud container; **nothing on it has run
in Revit.** The fixes are in `git log --first-parent origin/main..` on the branch and in their rows, not here.

**NEW.**

- **A door's reply says how the call ended in one fixed word.** `revit_change`, `revit_read` and `revit_show`
  carry a second line, `outcome: WORD - what it means`, and `outcome` in the reply's structured content
  ([04 §8.12](../04-heron-mcp.md); held by [`tests/test_outcome_line.py`](../../tests/test_outcome_line.py), against a
  stand-in Revit). [Row 5b-1159](../fragment-issues/section-5b-rows-401-425.md) is half closed by it; rows 5b-1559
  to 5b-1563 are what it left, and 5b-1562 is closed.
- **PROVEN cards that gave a wrong answer were repaired in code, and each is DRAFT again until it is
  proved.** The owner's word on 2026-11-11: *"if you are finding any fragment's mistake, even though it is
  proven, you can change that one."* The reads are [Group NK](../needs-checking/group-nk.md); three that change the model are
  [Group NL](../needs-checking/group-nl.md), `CREATE_ASSEMBLY` rebuilt on two transactions. What being DRAFT costs
  the reads meanwhile is measured in [row 5b-1554](../fragment-issues/section-5b-rows-401-425.md).
- **`revit_health` says, under each connected Revit, whether that Revit will ask Keep or Discard** - read
  from the add-in itself; four methods send the chat there ([row 5b-1579](../fragment-issues/row-5b-1579.md),
  [Group NP](../needs-checking/group-np.md)). Compiled for 2020 to 2027; the add-in has to be rebuilt and deployed.
- **A new 5b defect row is a file of its own**, `docs/fragment-issues/row-5b-<n>.md`, from 5b-1585 on - two
  branches adding rows no longer share a file ([row 5b-1585](../fragment-issues/row-5b-1585.md);
  [`heron-ship` §6a](../../.claude/skills/heron-ship/SKILL.md) has the two steps).
- **The MCP server is split at its first seam:** the inventory reads are in `mcp/server/heron_inventory.py`
  on a core any later tool file can import, `heron_server_core.py`, and one list says which files the
  server's source is, `heron_tools.SERVER_FILES` (rows 5b-1542, 5b-1555, 5b-1558).
- **From the evening of 2026-11-11:** a DRAFT method for parameters out to a spreadsheet and back,
  `parameters-csv-round-trip`, with what the round trip cannot do recorded ([Group NJ](../needs-checking/group-nj.md),
  [Q-87](../open-questions/tier-2.md)); every agent in the register checked, two add-in agent proofs read STALE
  ([Group NM](../needs-checking/group-nm.md)) and a rule for retiring an agent written as
  [D-154](../decisions/D-154.md), **Proposed** - nothing was retired; `heron_lookup` says each capability's
  status, risk and door; and a worktree inside the checkout is read as another checkout by the four walks
  that took it for this one ([`brain/heron_checkout.py`](../../brain/heron_checkout.py), row 5b-1539).
- **Main merged into the branch twice on 2026-11-12,** bringing the owner's own proving: the cards of a
  family's 2D side signed on Revit 2020 and Revit 2024, and `SET_FAMILY_TYPE_VALUES` version 4 (Groups
  [ND](../needs-checking/group-nd.md), [NE](../needs-checking/group-ne.md), [NF](../needs-checking/group-nf.md),
  [NN](../needs-checking/group-nn.md); each card's `proof:` block names its model).

**MISTAKES worth not repeating.**

- **Two branches each wrote a "version 5" of one card.** The owner proved main's `REPORT_FAMILY_FORMS`; the
  merge kept the proved card whole, and the register, Group NK and its job file went on saying the branch's
  repair was in the tree ([row 5b-1578](../fragment-issues/row-5b-1578.md)). After merging main, read every card
  the branch's records name again, from the card.
- **A cost written as nothing.** Group NK first said the reads lose nothing by going back to DRAFT. A review
  measured it: they no longer run on words alone, they lose a near-tie to a PROVEN card, and the QA checklists
  cannot use them. Measure what a status change costs before writing that it costs nothing.
- **A sentence true of one door, said of all.** The first Ask to Keep line was untrue of four kinds of change -
  a preview applied by `revit_apply_move`, an export or a save, a delete, a job the modeller chose to keep. Say
  what a switch does only of the tool it governs.
- **A word nobody had worked out.** The first outcome line said `REFUSED` for a failure code with no row of
  its own, and `ROLLED BACK` for a read that threw. Where the server did not work the ending out, the word is
  `OUTCOME UNKNOWN`.
- **An id taken in review.** The row written as 5b-1564 met main's own 5b-1564 to 5b-1574 at the merge and
  became 5b-1585. Run `python tools/next-id.py` again just before a branch merges, not only when the row is
  written.

**TO DO.**

- **On the owner's PC, Revit open:** the one ordered list is in
  [HANDOVER's top section](../HANDOVER.md#where-this-stands-right-now--read-this-then-9a-or-9), under its table -
  Groups NK and NL first, because merging the branch leaves their cards DRAFT on `main` until they are run.
- **The owner's word, no Revit:** [HANDOVER §8](../HANDOVER.md#8-what-is-waiting-on-the-owner), its newest
  table - D-153 and D-154, five open questions, the PROVEN tools whose known defects are held for his yes,
  the withdrawn repair, and Article 9's wording.
- **In no register group yet:** the outcome line in a real chat on a real Revit (rows 5b-1159 and 5b-1561).
- **Rows recorded and not repaired** are `python tools/open-defects.py`'s to list, not this note's. Two came
  from writing this note: `tools/next-id.py` names every id of a branch HELD TWICE once for each worktree cut
  from that branch ([row 5b-1581](../fragment-issues/row-5b-1581.md)), and `tools/owner-queue.py` lists no
  decision still Proposed ([row 5b-1582](../fragment-issues/row-5b-1582.md)).
- **Four rows that came with main say less than their cards:** 5b-1498, 5b-1564, 5b-1565 and 5b-1575 read
  *DRAFT, proof owed* or *awaits the owner's signature*, and the cards are signed PROVEN. Both are recorded;
  the rows are the owner's to close.
