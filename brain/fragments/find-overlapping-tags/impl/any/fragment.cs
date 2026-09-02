// NOT STANDALONE. Assumes `doc`, `view` and `minOverlapMm` are in scope; leaves
// `findings`, `overlapping`, `pairs` and `noBox` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ===========================================================================
// THE UNIT IS PAPER MILLIMETRES, AND IT IS THE ONLY UNIT THIS QUESTION HAS.
// ===========================================================================
//
// Two tags are clear at 1:50 and merged at 1:200. So every figure is model feet
// divided by the view's own scale - what comes out of the plotter, not what the
// model happens to look like.
//
// THE BOXES ARE PROJECTED ONTO THE VIEW'S OWN PAPER AXES. Comparing model X
// against model Y is right in a plan and wrong in every section: a view looking
// along X has all its annotation at one X, so every box overlaps every other
// and the report is worthless exactly where a section is busiest. Each box's
// eight corners are projected onto the view's right and up directions, which is
// the same test in a plan and the correct one everywhere else.
//
// A BOUNDING BOX IS BIGGER THAN THE INK, so this over-reports slightly. Two
// boxes can overlap while the characters do not touch. The opposite error costs
// a reissued drawing; this one costs thirty seconds of looking.
//
// DIMENSIONS ARE NOT COLLECTED. A dimension's box is the whole string, witness
// lines and all, so it appears to overlap nearly everything inside its own
// extent. Included, it produces hundreds of rows that are all the same
// non-finding.
//
// ROOM, SPACE AND AREA TAGS ARE COLLECTED BY CATEGORY, not by class. Which of
// them is an IndependentTag subclass has moved between releases; a category
// sweep is the same on all of them.

var findings = new List<string>();
var overlapping = new List<ElementId>();
var pairs = 0;
var noBox = 0;

var mmPerFoot = 304.8;

if (view.IsTemplate)
{
    findings.Add("That is a view template - it holds no annotation of its own");
}
else if (view.Scale <= 0)
{
    // A perspective or an unscaled view has no paper size, so "how many mm
    // apart on the sheet" has no answer. Saying so is the answer.
    findings.Add(string.Format("View '{0}' has no print scale, so nothing here can be measured in paper "
        + "millimetres", view.Name));
}
else
{
    var scale = (double)view.Scale;
    var right = view.RightDirection;
    var up = view.UpDirection;

    var annotation = new List<Element>();
    var kinds = new List<string>();

    Action<IEnumerable<Element>, string> take = (found, kind) =>
    {
        foreach (var e in found)
        {
            if (e == null) continue;
            if (annotation.Any(a => a.Id == e.Id)) continue;
            annotation.Add(e);
            kinds.Add(kind);
        }
    };

    take(new FilteredElementCollector(doc, view.Id).OfClass(typeof(IndependentTag)).ToList(), "tag");
    take(new FilteredElementCollector(doc, view.Id).OfClass(typeof(TextNote)).ToList(), "text");

    var byCategory = new[]
    {
        BuiltInCategory.OST_GenericAnnotation,
        BuiltInCategory.OST_KeynoteTags,
        BuiltInCategory.OST_RoomTags,
        BuiltInCategory.OST_MEPSpaceTags,
        BuiltInCategory.OST_AreaTags,
    };
    foreach (var category in byCategory)
    {
        try
        {
            take(new FilteredElementCollector(doc, view.Id)
                .OfCategory(category).WhereElementIsNotElementType().ToList(), "tag");
        }
        catch { }
    }

    // Each box reduced to a paper rectangle: min and max along the view's own
    // right and up directions.
    var uMin = new List<double>();
    var uMax = new List<double>();
    var vMin = new List<double>();
    var vMax = new List<double>();
    var kept = new List<Element>();
    var keptKind = new List<string>();

    for (var i = 0; i < annotation.Count; i++)
    {
        BoundingBoxXYZ box = null;
        try { box = annotation[i].get_BoundingBox(view); }
        catch { box = null; }
        if (box == null) { noBox++; continue; }

        var lo = box.Min;
        var hi = box.Max;
        double u0 = 0, u1 = 0, v0 = 0, v1 = 0;
        var first = true;

        for (var c = 0; c < 8; c++)
        {
            var corner = new XYZ(
                (c & 1) == 0 ? lo.X : hi.X,
                (c & 2) == 0 ? lo.Y : hi.Y,
                (c & 4) == 0 ? lo.Z : hi.Z);
            var u = corner.DotProduct(right);
            var v = corner.DotProduct(up);
            if (first) { u0 = u1 = u; v0 = v1 = v; first = false; }
            else
            {
                if (u < u0) u0 = u;
                if (u > u1) u1 = u;
                if (v < v0) v0 = v;
                if (v > v1) v1 = v;
            }
        }

        uMin.Add(u0); uMax.Add(u1); vMin.Add(v0); vMax.Add(v1);
        kept.Add(annotation[i]);
        keptKind.Add(kinds[i]);
    }

    Func<Element, string> label = e =>
    {
        var note = e as TextNote;
        if (note != null)
        {
            var text = (note.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length > 40 ? text.Substring(0, 40) + "..." : text;
        }
        var type = doc.GetElement(e.GetTypeId());
        return type != null ? type.Name : (e.Name ?? "");
    };

    var flagged = new HashSet<ElementId>();

    for (var i = 0; i < kept.Count; i++)
    {
        for (var j = i + 1; j < kept.Count; j++)
        {
            var sharedU = Math.Min(uMax[i], uMax[j]) - Math.Max(uMin[i], uMin[j]);
            if (sharedU <= 0) continue;
            var sharedV = Math.Min(vMax[i], vMax[j]) - Math.Max(vMin[i], vMin[j]);
            if (sharedV <= 0) continue;

            // The short side of the shared rectangle, on paper. Two boxes
            // sharing an edge share nothing; two sharing half their area are
            // unreadable, and this number is what tells them apart.
            var shortSide = Math.Min(sharedU, sharedV) * mmPerFoot / scale;
            if (shortSide < minOverlapMm) continue;

            pairs++;
            flagged.Add(kept[i].Id);
            flagged.Add(kept[j].Id);
            findings.Add(string.Format("{0} '{1}' over {2} '{3}'  - {4:0.0} mm on paper",
                keptKind[i], label(kept[i]), keptKind[j], label(kept[j]), shortSide));
        }
    }

    foreach (var id in flagged) overlapping.Add(id);

    findings.Insert(0, string.Format(
        "View '{0}' at 1:{1:0} - {2} annotation element(s) compared, {3} clash(es). Dimensions are not "
        + "compared, and a box is bigger than the ink, so check the top few on the sheet",
        view.Name, scale, kept.Count, pairs));

    if (noBox > 0)
        findings.Add(string.Format("{0} annotation element(s) reported no bounding box in this view and "
            + "were not checked", noBox));
}
