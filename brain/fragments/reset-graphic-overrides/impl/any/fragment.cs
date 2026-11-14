// NOT STANDALONE. Assumes `view` and `elements` are in scope; leaves `cleared`,
// `hadNothing` and `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// AN EMPTY OVERRIDE IS THE CLEARED STATE. There is no separate "clear" call in
// the API: writing a fresh, untouched OverrideGraphicSettings removes whatever
// was there. That reads as a trick and it is the documented way.
//
// It is also exactly why OVERRIDE_GRAPHICS_IN_VIEW replaces rather than merges -
// the same behaviour seen from the other side. Setting overrides wholesale is
// what makes clearing them possible with an empty object.
//
// THIS CLEARS THE PER-ELEMENT OVERRIDE ONLY. A CATEGORY-level override in the
// same view is a different setting and is untouched. Somebody clearing an
// element and watching it stay grey has hit that, and the answer is
// SET_CATEGORY_GRAPHICS rather than a longer version of this.
//
// `hadNothing` is separated from `cleared` because they are different answers.
// "I cleared 40" and "12 of those had nothing on them" together tell somebody
// their selection was wider than the thing they were trying to undo - and a
// single count of 40 hides that completely.

var cleared = 0;
var hadNothing = 0;
var viewRefused = false;

if (view == null || !view.AreGraphicsOverridesAllowed())
{
    viewRefused = true;
}
else
{
    var empty = new OverrideGraphicSettings();

    foreach (var element in elements)
    {
        if (element == null) continue;

        // Asked first, so the report can tell "undone" from "there was nothing
        // to undo". Both leave the element looking the same; only one of them
        // means the user's selection matched what they had changed.
        //
        // THERE IS NO "HAS ANY OVERRIDE" QUESTION IN THE API, so this asks
        // EVERY setting the Override Graphics box holds - the same list
        // READ_GRAPHIC_OVERRIDES reads since its row 5b-154 (version 2 here,
        // row 5b-1630). Version 1 asked only halftone, transparency, the two
        // line colours and the two line weights, so a wall overridden only by
        // a detail level, a line pattern or a surface pattern switched off was
        // cleared and then counted in `hadNothing` - measured 2026-11-13,
        // Revit 2024: "cleared 0" for three walls whose overrides it removed.
        // That was a wrong COUNT, never a wrong action - the clear below runs
        // either way. A pattern's Visible flag reads TRUE on an element with
        // nothing overridden, so only FALSE counts as set.
        //
        // The first draft of this line called `IsEmpty`, which does not exist.
        // It compiled in nobody's head and failed on all eight releases at once,
        // which is what tools/check-fragments-compile.py is for.
        var existing = view.GetElementOverrides(element.Id);
        Func<Color, bool> colourSet = c => c != null && c.IsValid;
        var hadSomething = existing != null &&
                           (existing.Halftone ||
                            existing.Transparency > 0 ||
                            existing.DetailLevel != ViewDetailLevel.Undefined ||
                            colourSet(existing.ProjectionLineColor) ||
                            colourSet(existing.CutLineColor) ||
                            existing.ProjectionLineWeight > 0 ||
                            existing.CutLineWeight > 0 ||
                            existing.ProjectionLinePatternId != ElementId.InvalidElementId ||
                            existing.CutLinePatternId != ElementId.InvalidElementId ||
                            colourSet(existing.SurfaceForegroundPatternColor) ||
                            existing.SurfaceForegroundPatternId != ElementId.InvalidElementId ||
                            colourSet(existing.SurfaceBackgroundPatternColor) ||
                            existing.SurfaceBackgroundPatternId != ElementId.InvalidElementId ||
                            colourSet(existing.CutForegroundPatternColor) ||
                            existing.CutForegroundPatternId != ElementId.InvalidElementId ||
                            colourSet(existing.CutBackgroundPatternColor) ||
                            existing.CutBackgroundPatternId != ElementId.InvalidElementId ||
                            !existing.IsSurfaceForegroundPatternVisible ||
                            !existing.IsSurfaceBackgroundPatternVisible ||
                            !existing.IsCutForegroundPatternVisible ||
                            !existing.IsCutBackgroundPatternVisible);

        view.SetElementOverrides(element.Id, empty);

        if (hadSomething) cleared++;
        else hadNothing++;
    }
}
