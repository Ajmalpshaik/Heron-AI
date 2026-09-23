# Needs checking — Group U

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group U — a review found six things in Group T's own work, and all six held

Codex reviewed [PR #189](https://github.com/Ajmalpshaik/Heron-AI/pull/189) on 2026-09-19 and left six
comments. **Every one was checked against the code and every one was right.** Four are fixed below;
two are open and one of those is the reason two signatures were withdrawn the same day.

Recorded here rather than only in the PR because a review comment disappears into a merged branch and
a register does not.

| ID | What it found | State |
|---|---|---|
| ~~**U1**~~ | `elements` sat in the unconditional envelope skip, so a list by that name was never traversed - and… — [full row](../needs-checking-archive/group-u.md#row-u1) | **FIXED.** |
| ~~**U2**~~ | `track --arg` made the arguments part of the test and the draft recorded only `operation`, so… — [full row](../needs-checking-archive/group-u.md#row-u2) | **FIXED.** |
| ~~**U3**~~ | Four register sections still read Never run for agents that had been tracked and signed hours… — [full row](../needs-checking-archive/group-u.md#row-u3) | **FIXED.** |
| ~~**U4**~~ | Row 118 was claimed twice - by this branch and by PR #191 the same day — [full row](../needs-checking-archive/group-u.md#row-u4) | **FIXED** |
| ~~**U5**~~ | **`PAR-011` and `SEL-008` DO have an empty case, and their signed proofs said no input could produce one.** `category=Pipes` returned `elements 0, distinctParameters 0` on session 20472 - measured in the very session that signed them. So D-53 tracking is not the right substitute: D-30's real negative leg is possible and owed | **FIXED the same day.** Both were withdrawn, then re-proved properly with the new `vary` mode and re-signed. `read_parameters` across `Ducts / Pipes / Air Terminals` on session 36908 returned **8 elements and 90 parameters / 2 and 99 / 0 and 0** - three inputs, three distinct answers, and **`Air Terminals` is a real empty case**. D-30's negative leg is now RUN and recorded rather than substituted, and the review was right that it was one argument away the whole time |
| **U6** | **Nine agent proofs were signed on TWO models. The repository's own gate refuses two for a fragment**, in these words: *"the tracking set has only 2 row(s). D-53 asks for the answer to follow the input across SEVERAL different inputs; two cannot show that"* ([`brain/heron_validate.py`](../../brain/heron_validate.py)) | **HALF FIXED, and the other half is bigger than it looked.** `prove-agent.py vary` now runs one agent across several values of ONE ARGUMENT on ONE model and **refuses fewer than three**, the same number the fragment gate enforces. That settles every argument-driven agent and needs no second Revit. It cannot settle an agent that reads the WHOLE MODEL - for those a third input is a third model. **THIRTEEN proofs are in that position, not the seven first counted:** the six signed before 2026-09-19 used the same two sessions, 36860 and 71340. Each now carries the caveat in its own `gaps`, recorded rather than withdrawn - the evidence that the answer moved is real, and it is thinner than this library's own standard |

**U6 is the one that matters most and it is not a bug in anybody's code.** It is one decision, D-53,
implemented twice at different strengths, and nothing compares the two. The fragment path enforces
three rows in code; the agent path never had a threshold at all because the tool's whole shape is
two-models-at-once.

**A third input need not be a third model.** For an argument-driven agent it can be a third
**argument** - `category=Ducts`, `Duct Fittings`, `Pipes` - which is closer to what D-53 asks
(*the answer follows the input*) than a third file would be. That is a change to `track`, not a
request for another Revit.
