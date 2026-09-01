// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `revisionId` and
// `paddingMm` are in scope; leaves `created`, `refused` and `viewRefused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// ONE CLOUD PER ELEMENT, NOT ONE AROUND THE SET, and this is the decision most
// worth arguing with. A single cloud spanning scattered elements covers
// everything between them, and a revision cloud is a STATEMENT THAT THE WORK
// INSIDE IT CHANGED. Over-clouding says work was revised that was not, on a
// drawing somebody signs; under-clouding gives three tight clouds where one
// loose one would have done, which is untidy and true. Between an untidy
// drawing and a false one, this takes untidy.
//
// THE VIEW HAS TO BE ONE A CLOUD CAN LIVE IN, and the ones that are refused
// are refused for different reasons:
//
//   a 3D view      Revit's own Revision Cloud tool is unavailable there. A
//                  cloud is a 2D annotation and has no plane to sit in
//   a schedule     has no geometry at all
//   a SHEET        this is the surprising one, because clouding straight onto
//                  a sheet is normal practice. A sheet's coordinates are PAPER
//                  coordinates and the elements' bounding boxes are MODEL
//                  coordinates; mapping between them needs each viewport's own
//                  transform and scale. Drawing paper-sized rectangles from
//                  model numbers would put a cloud kilometres off the sheet,
//                  so it refuses rather than produce that
//   a template     is not a drawing anybody issues
//
// THE BOX IS ASKED FOR IN THIS VIEW, not in the model. get_BoundingBox(view)
// returns null when the element is not visible there, which is exactly the
// right refusal: a cloud around something the drawing does not show marks a
// change nobody can see.

var padding = paddingMm / 304.8;

var created = new List<ElementId>();
var refused = new List<ElementId>();
var viewRefused = false;

var revision = doc.GetElement(revisionId) as Revision;

if (view == null || view.IsTemplate || view is View3D || view is ViewSchedule
    || view is ViewSheet || revision == null)
{
    // Everything is refused together, and `viewRefused` says it was the view
    // or the revision rather than the elements. Reporting each element as
    // individually refused would send somebody looking at the elements.
    viewRefused = true;

    foreach (var element in elements)
    {
        if (element != null) refused.Add(element.Id);
    }
}
else
{
    var origin = view.Origin;
    var right = view.RightDirection;
    var up = view.UpDirection;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var box = element.get_BoundingBox(view);
        if (box == null) { refused.Add(element.Id); continue; }

        // Every corner is projected onto the view's own axes. Using the box's
        // X and Y directly would be right only for a view square to the world,
        // and a section through a duct run at 30 degrees is not.
        double minU = 0, maxU = 0, minV = 0, maxV = 0;
        bool first = true;

        for (int i = 0; i < 8; i++)
        {
            var corner = new XYZ((i & 1) == 0 ? box.Min.X : box.Max.X,
                                 (i & 2) == 0 ? box.Min.Y : box.Max.Y,
                                 (i & 4) == 0 ? box.Min.Z : box.Max.Z);
            var offset = corner - origin;

            var u = offset.DotProduct(right);
            var v = offset.DotProduct(up);

            if (first) { minU = maxU = u; minV = maxV = v; first = false; }
            else
            {
                minU = Math.Min(minU, u); maxU = Math.Max(maxU, u);
                minV = Math.Min(minV, v); maxV = Math.Max(maxV, v);
            }
        }

        minU -= padding; maxU += padding;
        minV -= padding; maxV += padding;

        // A cloud with no size is not a cloud, and Revit rejects a line shorter
        // than its own tolerance. A zero-padding request against a
        // point-shaped element is a real way to arrive here.
        if ((maxU - minU) < 1e-6 || (maxV - minV) < 1e-6) { refused.Add(element.Id); continue; }

        // Built ON THE VIEW'S PLANE, which is what makes these curves legal
        // input: the model corners are dropped onto it by keeping only their
        // components along the view's own two axes.
        var a = origin + right * minU + up * minV;
        var b = origin + right * maxU + up * minV;
        var c = origin + right * maxU + up * maxV;
        var d = origin + right * minU + up * maxV;

        // A closed loop, in order. Revit turns these into the cloud's arcs; an
        // open or out-of-order loop is rejected as a shape rather than drawn
        // wrongly, which is the one mercy in this API.
        var outline = new List<Curve>();
        outline.Add(Line.CreateBound(a, b));
        outline.Add(Line.CreateBound(b, c));
        outline.Add(Line.CreateBound(c, d));
        outline.Add(Line.CreateBound(d, a));

        var cloud = RevisionCloud.Create(doc, view, revisionId, outline);
        if (cloud == null) { refused.Add(element.Id); continue; }

        created.Add(cloud.Id);
    }
}
