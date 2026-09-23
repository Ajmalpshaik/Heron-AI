# Fragment issues — How to arrange a case, learned by getting it wrong all day

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## How to arrange a case, learned by getting it wrong all day

Not a list of problems — the working method, written down because most of today's misses were the
arrangement rather than the fragment.

**This section is the evidence; the operative version is
[`.claude/skills/fragment-proving/SKILL.md`](../../.claude/skills/fragment-proving/SKILL.md).** The two say
the same five things and are deliberately not the same document: here is what was observed, with the
values that were passed and what came back; there is what to do about it, next to the job file that
does it. Fix the skill when a rule turns out to be wrong, and add the observation here.

**PROVE ON A SMALL SELECTION.** The owner's instruction, 2026-09-09, after `set-mep-size` timed out on
307 ducts: *"a lot of items change, it will affect slow process… you can try with a small number of
ducts like 2 or 3."* Retried on the 22 ducts in `FloorPlan: M1` it sized all 22 immediately, and
`split-mep-run` passed in the same batch. **A heavy write on a big selection is not a stronger test, it
is a slower one** — and a timeout tells you nothing at all about the fragment.

`select-by-category-name --set inViewOnly="FloorPlan: M1"` gives 22 ducts. `FloorPlan: L3` gives 307.

**ASK FOR WHAT THE MODEL HAS.** Five times today the POSITIVE case was the empty one, because D-30 is
written about the negative and the positive quietly goes unarranged. `select-by-connection-status` was
asked for open ends in a model with none; `measure-mep-slope` for a minimum nothing falls below;
`report-coverage` for gaps at a radius that leaves none; `check-family-standards` for a pattern nothing
matches, which makes MORE findings not fewer.

**CHECK THE CATEGORY IS VISIBLE WHERE YOU SELECT.** Sheets do not appear in a floor plan; levels do not
appear in their own plan. Twice the setup found nothing and the answer read as a missing selection.

**MATCH THE INPUT TO THE MODEL'S OWN UNITS AND SHAPES.** `set-mep-size` was handed a width and height
for ducts that are round. Every duct here is dia 102, 152 or 203 — an imperial model.

**READ THE BINDING NOTE ON THE ANSWER.** `find-overlapping-lines` ran on a stale selection of ten
equipment items and answered anyway; only `elements from the selection (10)` on the reply gave it away.

---
