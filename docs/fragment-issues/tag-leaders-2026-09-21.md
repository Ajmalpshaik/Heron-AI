# Fragment issues — Tag leaders, 2026-09-21

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Tag leaders, 2026-09-21 — five things measured against Project1

Ajmal asked for every duct in `1 - Mech` tagged with an L-shaped leader. Five
findings, all observed in front of the model rather than reasoned about.

### 1. AN ATTACHED LEADER END AND A FORCED L CANNOT BOTH BE HAD

The single most expensive thing here, and it is a hard Revit constraint rather
than a missing call. Measured three ways on the same twelve tags:

| what was tried | result |
|---|---|
| Free the end, move it onto the corner | true L — but re-attaching moved it back, **12 of 12 diagonal** |
| Leave the end attached, set the elbow | silently ignored, **12 of 12** |
| Leave it attached, set the elbow, `doc.Regenerate()` first | still ignored, **12 of 12** |

With the end attached Revit owns the leader's shape. `SetLeaderElbow` returns
normally and nothing bends — no exception, no warning. `force-tag-leader-lshape`
now trades this where it costs least: a run lying UP the sheet is never bent and
keeps its attached end; only a run bent ACROSS the sheet is left free, and each
one is named in `endsLeftFree` rather than counted quietly.

### 2. THE CORNER IS BUILT FROM BOTH ENDS, NOT FROM THE HEAD PLUS A LEG

The fragment used to put the elbow a fixed leg-length from the HEAD and level
with it. That fixes the shoulder and leaves the run back to the duct free to be
a diagonal — a short stub with a long diagonal hanging off it, which is exactly
what a modeller rejects on sight. The corner needs its across-position from one
end and its along-position from the other, so both legs are axis-true.

**And the corner is only ONE end of that leg.** Setting it alone cannot
straighten the run: the other end is wherever Revit attached the leader. Both
have to agree, which is why the leader start is moved too.

### 3. A COUNTER THAT COUNTS THE CALL RETURNING IS NOT EVIDENCE

An earlier version of `tag-elements-in-view` incremented `elbowsDrawn` the
instant `SetLeaderElbow` did not throw. It reported **15 elbows drawn** and put
**15 straight diagonals** on the drawing, and a proof passed on that number.
Same family as `IndependentTag.Create` not honouring its type argument, which
this library already reads back. **Read the value back out of Revit and compare
it; a set call that returns is not a set call that took.**

### 4. A COUNT REPORTED AND A COUNT IN THE MODEL DISAGREED

`tag-elements-in-view --apply` reported `tagged 0` on a run that demonstrably
created **12** tags — `revit_annotation` found them immediately afterwards, and
the next run's `alreadyTagged 12` agreed. Not yet explained. **Do not trust this
fragment's `tagged` on an applied run without reading the model back.**

### 5. A VIEW-SCOPED SELECTION CANNOT REACH WHAT A VIEW FILTER HIDES

Twice, at different duct counts: 14 tags created and 12 selectable; 13 created
and 11 selectable. `1 - Mech` carries three view filters, and a filter hiding a
duct hides its tag with it. The tags are real and correct — they simply never
reach a step that selects by category in that view, so the bend never sees them.
**Reconcile what a chain created against what the next step selected, every
time.** A step that silently acts on fewer elements than the one before it looks
exactly like a step that worked.

---
