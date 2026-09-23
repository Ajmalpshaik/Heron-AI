# Needs checking — Group X

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group X — the SKILLS, and the one thing only the PC can answer — 2026-09-19

**THIS WAS WRITTEN AS GROUP V, THEN W, AND IS X. THE REASON IS THE ROW BELOW IT.** A brief asked
for a "Group V" on 2026-09-19 when none existed — the thirteen it was after are
**[Group I2](group-i.md#i2--the-model-has-not-got-it-13)** — so this session took the free letter. Another
session took the same free letter the same afternoon for a larger sort, merged first, and keeps it.
Same hazard as the row numbers, one namespace along.

**Ten skills have been DRAFT since Step 14 and nothing could say why.**
[`tools/prove-skill.py`](../../tools/prove-skill.py) now asks, and measures every half that a machine
with no Revit can measure. It reports **BLOCKED 2, CROSSING 1, NOT UNDERSTOOD 6, UNDERSTOOD 1** —
and **it cannot print PROVEN**, because a skill is proved when a model has answered.
[`tests/test_skill_proving.py`](../../tests/test_skill_proving.py) pins that by reading the verdict
function's own returns.

**Everything below is what is left, and every row of it needs the PC.**

| | What to run, and what it would settle |
|---|---|
| **X1** | **The ten job files.** `tools/jobs/skills/<skill>-model-half.yaml`, one per skill, in `tools/jobs/example.yaml`'s shape, steps in an order that composes and `keep-chain: true` exactly where a need can only come down the chain. `python tools/batch-prove.py <file> --dry-run` first. **Read each header before running it** — it says how many of its own steps `batch-prove` will refuse as `ALREADY`, which for seven of the ten is all of them |
| **X2** | **And that refusal is the finding, not a fault — [row 141](../FRAGMENT-ISSUES.md).** `batch-prove` refuses **32 of the 37 steps**, correctly: each capability's own model half is already evidenced. What is NOT evidenced is the **composition**, and nothing in this repository can run one. `prove` in `mcp/client/heron_bridge_client.py` does run a chain — first fragment resets it, the rest continue, one lease — but calls `run_fragment_read` only, so the four MODIFY skills have no chain runner at all, and it judges nothing: no negative case, no draft, no signature. **Chain, write, judgement — no one runner has all three.** Each job file carries the `prove` command line for its own chain, which is as far as today's tooling reaches |
| **X3** | **Two skills need a hand on the mouse before their batch runs.** `check-connectivity` and `trace-system` both go through `trace-connectivity`, whose `start` is one PARTICULAR element. Select it in Revit and type `selected` in the blank — the add-in refuses if none or several are selected, so the batch has to be arranged around that one pick |
| **X4** | **`trace-system` cannot run its own plan at all — [row 142](../FRAGMENT-ISSUES.md).** It declares `REPORT_FINDINGS`, and neither `filter-elements-by-id` nor `trace-connectivity` provides the `findings` that fragment needs. Two plausible repairs, both the owner's, neither guessed at. **No Revit needed to decide it; a Revit needed to prove whichever is chosen** |
| **X5** | **The routing half is a RECORDING and will go stale.** `tools/jobs/skills/routing-2026-09-19.json` is what `prove-skill.py --routing-from` and the skill catalogue both read. Re-take it with `python tools/prove-skill.py --routing-to tools/jobs/skills/routing-<date>.json` — about twenty-five minutes for forty-three sentences — and **compare the index fingerprint before comparing any count** ([row 116](../FRAGMENT-ISSUES.md)). It needs no Revit, only time. **SINCE 2026-09-19 IT NO LONGER GOES STALE IN SILENCE** ([row 152](../FRAGMENT-ISSUES.md)): `ROUTING.words_moved()` compares the recording's PHRASES against the skills' own utterances - which needs no store, so it holds in CI - and a skill whose words have moved is `OUT OF DATE` on the page and in the tool, never `UNDERSTOOD`. **That does not re-take it**: what the check buys is that nobody reads a stale count as a clean one. The fingerprint still answers a different question - WHICH LIBRARY was asked. **RE-TAKEN 2026-09-19** as `routing-2026-09-19b.json`, and the comparison is worth keeping: the two stores' md5 **differ** and **not one of the 43 answers moved**, so the fingerprint is a poor test of whether a recording is still true ([row 152](../FRAGMENT-ISSUES.md)). The new one also carries what the RETRIEVER said about each answer ([row 157](../FRAGMENT-ISSUES.md)) - **24 of 43 sentences carry a complaint, 12 of them among the 24 that reach** |

### What is NOT waiting on the PC, and is waiting on a person

Three findings are one-line edits in `brain/fragments/`, which the session that found them was told
not to touch. They need no Revit at all — only whoever owns those files.

- **[Row 146](../FRAGMENT-ISSUES.md)** — seven sentences with an owner named per sentence. The repair
  is [row 116](../FRAGMENT-ISSUES.md)'s *declaring, not demoting*: one line added to one `utterances:`
  block, after which identity fires before ranking runs.
- **[Row 134](../FRAGMENT-ISSUES.md)** — *"change the insulation thickness"* is the dangerous one.
  `set-compound-layer-width` edits a wall, floor, roof or ceiling **TYPE**; `set-mep-insulation`
  edits what was handed in. A modeller means the pipe.
- **[Row 140](../FRAGMENT-ISSUES.md)** — four declarations in three skills name a capability that
  outranks the skill's own risk. **The obvious repair is the one that must not be taken**: raising
  the skill's risk removes it from `check-skill-routing.py`'s `ASKS_A_QUESTION`, and its crossings
  stop being reported.

**And four of [row 146](../FRAGMENT-ISSUES.md)'s crossings cannot be repaired by anybody's edit**,
because nothing in the library reads the thing: an MEP element's size, which workset an element is
on, an insulation thickness, and a view's scale. Those are fragments somebody has to write, and then
a Revit to prove them.

> **THE NUMBER COLLIDED TWICE MORE WHILE THIS WAS BEING WRITTEN, AND THEN THE LETTER DID.** This
> session's row was 147 against [PR #200](https://github.com/Ajmalpshaik/Heron-AI/pull/200), then 148
> against [PR #198](https://github.com/Ajmalpshaik/Heron-AI/pull/198), and is **150**; #198's own
> [row 149](../FRAGMENT-ISSUES.md) counts the same hazard four times in one day from the other side.
> Then this group, written as V, met a larger Group V from #198 - and its Group W as well, so it is X.
> [U4](group-u.md#group-u--a-review-found-six-things-in-group-ts-own-work-and-all-six-held) records it happening
> to row 118. **Nothing sees an in-flight branch**, so no local tool can prevent it, and announcing a
> number is only half a fix — it does not help when two sessions announce at the same hour. What
> actually held every time: **keep both, renumber the later one, and check for duplicates
> afterwards** — no row and no group has been lost yet.
**BUILT 2026-09-20, AND IT WAS SMALLER THAN THIS GROUP EXPECTED.** The judging
half already existed and nothing had ever produced one - see
[row 156](../FRAGMENT-ISSUES.md). `validate --vary NAME=a,b,c --vary-field FIELD`
is the running half, demonstrated end to end on `test projject` against
`filter-elements-by-category`: `Ducts 8`, `Pipes 2`, `Air Terminals 0`,
`Walls 28`, and `heron_validate draft` wrote a proper D-53 negative case from it.

**THE THREE FRAGMENTS ARE STILL NOT PROVED**, and that is the honest state: the
machinery exists, and each of them still needs an arrangement, a model and a
signature. What has changed is that they are now *provable*, which they were
not. Whoever takes them needs an input each answer genuinely depends on:

| fragment | what to vary | why that one |
|---|---|---|
| `trace-connectivity` | the `start` element | `reached` is seeded with it, so nothing else moves the answer - `elements` only feeds the GEOMETRIC route |
| `report-findings` | `whatWasChecked` / the checked set | its result is prose, so the tracked field has to be the sentence, and the sentence must differ per input |
| `describe-blank-parameters` | `parameterName` | its record already shows the shape - `blank 22, absent 0` against `blank 0, absent 22` - which is a tracking set nobody could record |
