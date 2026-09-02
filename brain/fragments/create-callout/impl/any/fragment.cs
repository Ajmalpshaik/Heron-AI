// NOT STANDALONE. Assumes `doc`, `parentView`, `corner`, `opposite`,
// `viewScale` and `viewName` are in scope; leaves `created` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// NO UNIT CONVERSION. The two points arrive as XYZ, already in Revit's feet.
//
// THE CALLOUT TYPE MATCHES THE PARENT, and that is not a detail. A callout of a
// plan is a plan; a callout of a section is a section. Handing CreateCallout a
// view family type that does not match the parent is rejected, so the type is
// taken from what the parent IS rather than from a fixed choice.
//
// A CALLOUT ON A 3D VIEW OR A SCHEDULE IS NOT A THING. Refused with the reason,
// because the alternative is an exception naming a view family type, which
// tells a modeller nothing about what to do instead.
//
// THE TWO POINTS ARE OPPOSITE CORNERS, and a degenerate rectangle gives a
// callout with no area - which Revit will make, and which is invisible on the
// drawing and impossible to select.

ElementId created = null;
string refused = null;

var name = (viewName ?? "").Trim();

if (parentView == null || parentView.IsTemplate)
{
    refused = "a callout is drawn on a real view - a template is not one";
}
else if (parentView is View3D || parentView is ViewSchedule || parentView is ViewSheet)
{
    refused = "a callout goes on a plan, a section or an elevation. A 3D view, a "
            + "schedule and a sheet have nothing to enlarge";
}
else if (corner == null || opposite == null)
{
    refused = "a callout needs two opposite corners";
}
else if (Math.Abs(corner.X - opposite.X) < 1e-6 || Math.Abs(corner.Y - opposite.Y) < 1e-6)
{
    // Revit makes it, and it is invisible on the drawing and cannot be picked.
    refused = "the two corners give a rectangle with no width or no height - the "
            + "callout would be invisible and unselectable";
}
else if (viewScale <= 0)
{
    refused = "give the scale. A callout at the parent's scale shows nothing new, and "
            + "enlarging is the whole point of one";
}
else
{
    // The callout's type has to match what the parent IS - a callout of a plan
    // is a plan. Taking the parent's own type is what guarantees that.
    var parentType = doc.GetElement(parentView.GetTypeId()) as ViewFamilyType;

    if (parentType == null)
    {
        refused = "the parent view has no view family type to match, so there is nothing "
                + "to make a callout of it from";
    }
    else
    {
        var view = ViewSection.CreateCallout(doc, parentView.Id, parentType.Id,
                                             corner, opposite);

        if (view == null)
        {
            refused = "Revit declined to create the callout";
        }
        else
        {
            view.Scale = viewScale;
            if (name.Length > 0) view.Name = name;
            created = view.Id;
        }
    }
}
