# Three bugs in one PROVEN fragment, and the gate that cannot see any of them

**2026-09-17.** The model session. `set-view-crop` was marked **PROVEN** and was wrong in three
separate ways at once. Every one of them was found by the owner **looking at his screen**, and not
one of them could have been found by reading the fragment's own output.

> **Register rule.** New file on purpose — HANDOVER, DECISIONS, PROPOSALS, NEEDS-CHECKING and
> FRAGMENT-ISSUES all have other sessions writing to them today.

## What it was asked to do

Crop a floor plan around some elements with a margin. `test projject.rvt` (Revit 2024,
session 46936), view `FloorPlan: HERON ROOM TEST`.

It reported, six times running:

```
applied    true
enclosed   28
marginMm   3000
```

**All three of those are honest, and none of them is evidence.** `applied` means *the crop is
switched on*. `enclosed` counts what it **measured**, not what ended up inside. `marginMm` is the
caller's own input handed straight back — the "echo of the input" that `fragment-proving` warns
about by name. Not one can be false while the crop is wrong.

What the owner saw was an **empty view** with a single red line down one edge:

> *"in the view its not there its only one side any very less and i cand see that selected wall
> also"*

## Bug 1 — measured in view space, written as model space

```csharp
var box = element.get_BoundingBox(view);   // Min/Max are in the BOX'S OWN transform
...
region.Min = new XYZ(minX - margin, ...);  // ...used as if they were model coordinates
```

`get_BoundingBox(view)` returns a box expressed in that box's own `Transform`, and the extents of
two elements with different transforms were being merged as though both were model coordinates.
Right numbers, wrong meaning — a dimension read off a drawing and typed in as a site coordinate.

**Fixed** by asking for `get_BoundingBox(null)`, which is model space with an identity transform,
and the only space in which two elements' extents can honestly be combined.

## Bug 2 — the crop box was built with the wrong transform

```csharp
var region = new BoundingBoxXYZ();   // IDENTITY transform
view.CropBox = region;
```

A crop box is read **in its own transform**, never in model coordinates. Handing model X/Y/Z to a
fresh identity box puts the crop wherever the view's real transform sends those numbers.
`CropBoxActive` reads back `true` either way, which is why the fragment reported success.

**Fixed** by taking `view.CropBox` — keeping whatever transform the view carries — and mapping the
model box into it through `Transform.Inverse`, over **all eight corners** rather than two opposite
ones, because a rotated view turns a box into a box at an angle and two corners give its diagonal
rather than its extent.

**And the Z is now left exactly as the view had it.** On a plan view the crop box's Z is the view
DEPTH — the front and back clipping planes — not the height of anything being enclosed. Writing the
elements' own Z into it clips the view to the thickness of what was selected, which is a second,
independent way to make the very thing you asked to see disappear.

## Bug 3 — a shaped crop silently outranks the box

This is the one that wasted the most time, because the first two fixes landed and **nothing
changed on screen**.

`SET_VIEW_CROP_TO_SHAPE` had given the view a non-rectangular crop earlier in the sitting. Once a
view has a crop **shape**, Revit follows the shape and the rectangle written to `CropBox` is
ignored — silently. The crop sat at `8800 x 6800`, the outline of the room the shape had been set
to, through six runs at 500 mm and 3000 mm, while every one reported `applied true`.

**Fixed** by removing any existing crop shape before writing the box:

```csharp
var shapeManager = view.GetCropRegionShapeManager();
if (shapeManager != null && shapeManager.ShapeSet) shapeManager.RemoveCropRegionShape();
```

Removed rather than refused, because a caller asking for a rectangle with a margin has said plainly
which of the two they want.

**Verified on screen afterwards**: all 28 walls inside the crop with a margin all round, and then a
tighter crop at 500 mm around a 7-element selection — `enclosed 5, noGeometry 2`, the two
geometryless ones named rather than quietly dropped.

## The gate that cannot see any of this

`set-view-crop`'s fingerprint moved when the implementation changed —
`ae36c3214871d60d` → `9491e815a1837c26` — and **`tools/check-signatures.py` said nothing.**

`tools/check-signatures.py`, in `findings()`:

```python
if (frag.status or "") != "DRAFT":
    continue
```

**The staleness check only examines fragments at DRAFT.** Its docstring says so plainly — *"Every
fragment signed but not promoted"* — so this is the tool doing what it was designed to do. But the
consequence is that **once a fragment reaches PROVEN its implementation can change without limit
and no gate ever reports the proof as stale.** That is 309 fragments whose `proof:` block can
quietly stop describing their own code.

`set-view-crop` is the live demonstration: three bug fixes, a changed fingerprint, and a proof block
still reading `by: Ajmal PS, date: 2026-09-13` — vouching for code that no longer exists.

**Demoted to DRAFT here**, because a proof that does not describe the code is not a proof. That
moved `310 → 309 PROVEN`, `62 → 63 DRAFT` and `158 → 157 MODIFY PROVEN`, and the sentences stating
those counts in three READMEs are corrected in the same change.

**`tools/` is outside this session's paths, so the gate itself is reported rather than changed.**
The fix is one line — widen the filter past DRAFT — but it will light up every proven fragment whose
code has moved since signing, and that list is somebody's deliberate morning rather than a proving
session's aside.

## What to take from it

**A result that cannot come back false is not evidence.** `applied true` survived three
simultaneous bugs across six runs. `enclosed 28` was true while 27 of the 28 were outside the crop.
`marginMm 3000` was the caller's own number.

The session ran that fragment six times, read success six times, and believed it. A person looked at
the view once.

**For anything whose output is a picture, look at the picture.** The screenshot that broke this open
took one tool call.

## Re-proved — and the proof block still cannot see the thing that was wrong

**Later the same day**, on `test projject.rvt` (Revit 2024, session 64416, 3,565 elements), against the
add-in deployed at 22:34. `set-view-crop` passes both halves and is **signed and promoted again**:

```
positive   28 walls selected in {3D}   applied true    enclosed 28   viewRefused false
negative   the same 28 against a SCHEDULE   applied false   enclosed 0   viewRefused true
```

**Read that positive line and notice that it is word for word what the broken version reported.**
`applied true, enclosed 28, marginMm 3000` is exactly the sentence that survived three simultaneous
bugs across six runs. A proof block carrying it is not evidence the fixes work, and signing one as
though it were would be repeating the original mistake in a more official place.

### The selection is made in `{3D}`, not in the view being cropped

`inViewOnly` finds what is **visible**, so selecting in the view you are about to crop makes the next
selection smaller, which crops tighter again. That feedback loop shrinks a proof one run at a time and
it is invisible in every number the fragment returns. `{3D}` is not cropped, so the wall count is the
model's rather than the crop's — 28 either way, and the same 28 in both legs.

### What was measured instead, because it can come back false

The view showed **24 of the 28 walls** before anything ran. Three crops were then applied for real —
not rolled back — and the visible count re-read after each:

| crop applied | walls visible in the plan |
|---|---|
| around all 28, margin **3000 mm** | **24** |
| around all 28, margin **20000 mm** | **24** |
| around **one** wall, margin **500 mm** | **2** |

**The third row is the proof.** Tightening the crop around a single wall collapses the view from 24 to
2 — the target and the one neighbour that overlaps it. The crop therefore lands where it is told, and
the broken version could not have done this: it put the region in the view's own transform as though it
were model coordinates, which is why the owner saw an empty view with one line down the edge.

**The first two rows are the control**, and they matter as much. A 20000 mm margin changes nothing, so
the four walls missing from the plan are **not** excluded by the crop. Had only the 3000 mm row been
run, 24 would have looked like a failure and the fix would have been backed out.

### One thing left open, and it is not this fragment

**Four walls are in the model and never appear in that floor plan**, at any crop size. The crop is ruled
out. The cause is not established here — level, view range, phase or discipline are all candidates, and
`read-element-level` returns all 28 level names but the reply truncates lists to three, so the
distribution could not be read from it.

It is written down rather than guessed at because **`inViewOnly` is the selector half the proofs in this
library lean on**. If it can under-report, that is worth somebody's morning. If it is ordinary Revit
view behaviour, that is worth one sentence saying so.
