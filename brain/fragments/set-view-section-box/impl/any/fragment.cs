// NOT STANDALONE. Assumes `view`, `elements` and `marginMm` are in scope;
// leaves `applied`, `enclosed`, `noGeometry`, `viewRefused` and
// `refusalReasons` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ONLY A 3D VIEW HAS A SECTION BOX. Refused by name on anything else rather
// than returning quietly - the user is watching for the view to change, and a
// silent no-op reads as a broken tool.
//
// TWO OPPOSITE THINGS USED TO SET `viewRefused`, AND NEITHER SAID WHICH.
//
// It was set when the VIEW could not carry a section box - a plan, a section, a
// template - and again when the view was a perfectly good 3D view and NOTHING
// HANDED IN HAD ANY GEOMETRY. The fixes are opposite: open a 3D view, or hand
// over something measurable. A caller told `viewRefused true` and nothing else
// goes looking at the view when the selection is what is empty.
//
// THIS IS ALSO THE DOUBT RECORDED IN SECTION 3f, and it could not be settled
// while the two shared a word. The proof of 2026-09-09 used a plan view as its
// negative, and the owner's objection was that a plan view CANNOT have a
// section box, so the empty answer is guaranteed by the view type rather than
// by anything this fragment worked out. The stronger negative he asked for - a
// 3D view whose selection has nothing to enclose - reported `viewRefused true`
// as well, so it was indistinguishable from the weak one. It is not any more:
// the strong negative now leaves `viewRefused` FALSE and says in words that
// the view was fine and the selection was not.
//
// `viewRefused` MEANS EXACTLY ITS NAME. `refusalReasons` carries the words,
// and the empty selection is its own sentence. Section 3h.1 of
// docs/FRAGMENT-ISSUES.md: a fragment that cannot use its input must refuse
// AND SAY WHY.
//
// THE BOX IS BUILT FROM MODEL-COORDINATE BOUNDING BOXES. `get_BoundingBox(null)`
// asks for the element's extent in model space rather than as cropped by some
// view, which is what a section box is set in. Passing a view here would ask
// "how big is it on that drawing" and box the wrong volume.
//
// AN ELEMENT WITH NO BOUNDING BOX IS NAMED, NOT TREATED AS THE ORIGIN. A box
// silently stretched to 0,0,0 encloses the whole building and looks like the
// tool ignored the request - which is what would happen if a null were folded
// into the min/max without being noticed.
//
// MILLIMETRES TO FEET IS ARITHMETIC (D-20): 1 ft = 304.8 mm exactly. Nothing
// for 2021's unit rewrite to reach.

var applied = false;
var enclosed = 0;
var noGeometry = new List<ElementId>();
var refusalReasons = new List<string>();
var viewRefused = false;

const double MillimetresPerFoot = 304.8;

// The view's own name, so the sentence names the drawing the caller is looking
// at rather than making them work out which view they passed.
var viewName = "the view given";
try { if (view != null) viewName = "'" + view.Name + "'"; }
catch { viewName = "the view given"; }

var view3D = view as View3D;

if (view == null)
{
    viewRefused = true;
    refusalReasons.Add("no view was given, and a section box belongs to one 3D view");
}
else if (view3D == null)
{
    var kind = "not a 3D view";
    try { kind = "a " + view.ViewType + " view"; }
    catch { kind = "not a 3D view"; }

    viewRefused = true;
    refusalReasons.Add(viewName + " is " + kind + ", and ONLY A 3D VIEW HAS A SECTION BOX. "
        + "Nothing was changed. To crop a plan or a section instead, that is a crop region - "
        + "SET_VIEW_CROP; to show only these elements wherever they are, ISOLATE_ELEMENTS");
}
else if (view3D.IsTemplate)
{
    viewRefused = true;
    refusalReasons.Add(viewName + " is a view TEMPLATE, not a drawing anybody looks at. "
        + "Nothing was changed - box the view the template drives");
}
else
{
    double minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;
    var any = false;
    var handed = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;
        handed++;

        var box = element.get_BoundingBox(null);
        if (box == null)
        {
            noGeometry.Add(element.Id);
            continue;
        }

        if (!any)
        {
            minX = box.Min.X; minY = box.Min.Y; minZ = box.Min.Z;
            maxX = box.Max.X; maxY = box.Max.Y; maxZ = box.Max.Z;
            any = true;
        }
        else
        {
            if (box.Min.X < minX) minX = box.Min.X;
            if (box.Min.Y < minY) minY = box.Min.Y;
            if (box.Min.Z < minZ) minZ = box.Min.Z;
            if (box.Max.X > maxX) maxX = box.Max.X;
            if (box.Max.Y > maxY) maxY = box.Max.Y;
            if (box.Max.Z > maxZ) maxZ = box.Max.Z;
        }

        enclosed++;
    }

    if (!any)
    {
        // Nothing measurable. The existing section box is LEFT ALONE rather
        // than set to a degenerate volume - a zero-sized box shows an empty
        // view, which reads as "everything was deleted".
        //
        // AND `viewRefused` IS NOT SET. There is nothing wrong with this view;
        // it is a 3D view and it would have taken a box. Saying the view
        // refused would send somebody to change a view that was never the
        // problem. See the header.
        refusalReasons.Add(handed == 0
            ? "nothing was handed in to enclose, so NOTHING WAS CHANGED. " + viewName
              + " is a 3D view and would have taken a section box - the selection is what "
              + "was empty"
            : string.Format(
                "none of the {0} element(s) handed in has any geometry to enclose, so NOTHING "
                + "WAS CHANGED and the existing section box is left as it was. {1} is a 3D "
                + "view and would have taken a box - a box stretched to nothing would cut the "
                + "view to an empty screen. The element(s) are named in noGeometry",
                handed, viewName));
    }
    else
    {
        var margin = marginMm / MillimetresPerFoot;

        var box = new BoundingBoxXYZ();
        box.Min = new XYZ(minX - margin, minY - margin, minZ - margin);
        box.Max = new XYZ(maxX + margin, maxY + margin, maxZ + margin);

        view3D.SetSectionBox(box);

        // Setting the box does not switch it on. A view whose section box is
        // inactive looks exactly as it did, which would read as the call having
        // done nothing at all.
        view3D.IsSectionBoxActive = true;

        applied = view3D.IsSectionBoxActive;
    }
}
