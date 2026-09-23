# Needs checking — Group Y

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group Y — what tracking proved, and the two walls it hit, 2026-09-20

**The first fragment proved by TRACKING rather than by an empty case**, on
`test projject`, Revit 2024, session 36216.

**`check-minimum-clearance` — PROVEN.** `defaultClearance` varied across five
values, `tooClose` following it every time:

```
      1 mm  ->   4        6000 mm  ->  10
   1500 mm  ->   6       20000 mm  ->  18
   3000 mm  ->   6
```

Rising with the clearance, which is what the physical question demands. A
fragment ignoring its input, measuring nothing, or answering about a set it was
not given could not produce that. Signed and promoted — **329 PROVEN, 66 DRAFT.**

### Wall 1 — the tracked field must be one the REPLY renders in full

**`find-nearest-elements` ran perfectly and cannot be proved this way.** Three
metrics - `centre`, `gap`, `manhattan` - are three genuinely different
calculations, and all three came back **`8 entry(ies)`**. The DISTANCES differ;
the reply truncates a dictionary to its entry count, so nothing that varies is
visible, and `heron_validate` would refuse the set with *"every tracking row
came back 8 entry(ies)"* - correctly, because from where it stands they did.

**So the field has to be one whose NUMBER moves, not one whose contents move.**
`tooClose` worked because the COUNT changed; `distancesMm` failed because only
the values inside did. That is the same truncation that made `create-roof`
report *"base at 40..."* ([row 138](../FRAGMENT-ISSUES.md)) and it is a property of
the reply, not of tracking.

**The repair, for whoever wants this fragment:** have it leave a short STRING
that carries the answer - the way `create-roof` now leaves `measuredMm` - and
track that. A contract change, small, and it makes the fragment more honest for
a reader too.

### Wall 2 — a fragment needing ONE element AND a set cannot be arranged

**`trace-connectivity` blocks two skills and is still blocked.** It needs
`start` (one particular `Element`) and `elements` (the candidate set), and the
only way to name one element is `selected`, which means the whole Revit
selection. So the chain must leave **eight** for `elements` and **exactly one**
for `start`, from one selection, and there is no way to say that.

`setup-set` does not help: it gives the CHAIN its own values, and this needs two
different selections in one arrangement.

**It is the same gap the skills session hit from the other side** - `elementIds
(IList<ElementId>)` cannot be typed in, so `filter-elements-by-id` cannot pick
out one element either. One rule in `OneIdNamed` for an id named by its Revit
element id would unblock both, and it is the rule `RevitFragment` deliberately
refuses to guess at. **That is the next thing worth building**, and it is add-in
C# - a rebuild and a Revit restart.


---
