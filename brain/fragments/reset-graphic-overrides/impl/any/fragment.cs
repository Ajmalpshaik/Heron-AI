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
        // THERE IS NO "HAS ANY OVERRIDE" QUESTION IN THE API, so this asks the
        // properties that a grayout or a colour job actually sets. It is a
        // heuristic and it is named as one: an override consisting only of
        // something not listed here would read as nothing. That is a wrong
        // COUNT, never a wrong action - the clear below runs either way.
        //
        // The first draft of this line called `IsEmpty`, which does not exist.
        // It compiled in nobody's head and failed on all eight releases at once,
        // which is what tools/check-fragments-compile.py is for.
        var existing = view.GetElementOverrides(element.Id);
        var hadSomething = existing != null &&
                           (existing.Halftone ||
                            existing.Transparency > 0 ||
                            existing.ProjectionLineColor.IsValid ||
                            existing.CutLineColor.IsValid ||
                            existing.ProjectionLineWeight > 0 ||
                            existing.CutLineWeight > 0);

        view.SetElementOverrides(element.Id, empty);

        if (hadSomething) cleared++;
        else hadNothing++;
    }
}
