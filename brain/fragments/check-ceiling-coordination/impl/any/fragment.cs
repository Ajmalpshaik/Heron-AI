// NOT STANDALONE. Assumes `doc`, `devices` and `tolerance` are in scope; leaves
// `findings`, `outOfPlane` and `noCeilingAbove` behind.
//
// READ ONLY. Opens no transaction and needs none. Distances are internal FEET.
//
// THE FAULT THIS CATCHES IS INVISIBLE IN PLAN. A diffuser 40 mm above the
// ceiling and one exactly in it look identical from above. It shows up as a site
// query, or on a section nobody cut.
//
// THE CEILING IS FOUND BY CASTING A RAY UPWARD. A device placed unhosted is not
// "hosted by" the ceiling in any readable way, and a room cannot tell you its
// own ceiling. The ray needs a real View3D; no 3D view at all is REPORTED, not
// returned as zero findings.
//
// "NO CEILING ABOVE" IS A FINDING, NOT AN ERROR - an open soffit is fine.
// Counted separately, because a missing ceiling and a misplaced device look the
// same in every other check.
//
// THE RAY GOES UP FROM THE INSERTION POINT. For a family whose insertion point
// is off its own centre the ray can miss the ceiling beside it, so a large NO
// CEILING count in an area known to have one is usually that. Said in the
// report rather than left to read as a hundred real faults.

var findings = new List<string>();
var outOfPlane = new List<ElementId>();
var noCeilingAbove = new List<ElementId>();

View3D rayView = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
{
    var candidate = element as View3D;
    if (candidate != null && !candidate.IsTemplate) { rayView = candidate; break; }
}

var hostCeilings = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType().GetElementCount();

if (rayView == null)
{
    findings.Add("no 3D view in this model, so no ray could be cast and NOTHING was checked. That is "
        + "different from every device passing");
}
else
{
    if (hostCeilings == 0)
    {
        findings.Add("no ceilings in the HOST model. If the architecture is a LINK, every device below "
            + "will read as having no ceiling above it and none of that is a real finding");
    }

    var intersector = new ReferenceIntersector(
        new ElementCategoryFilter(BuiltInCategory.OST_Ceilings), FindReferenceTarget.Element, rayView);

    foreach (var device in devices)
    {
        if (device == null || !device.IsValidObject) continue;

        XYZ point = null;
        var location = device.Location as LocationPoint;
        if (location != null) point = location.Point;
        if (point == null)
        {
            var box = device.get_BoundingBox(null);
            if (box != null) point = (box.Min + box.Max) / 2.0;
        }
        if (point == null)
        {
            findings.Add(string.Format("{0} (id {1}): no readable position - not checked",
                device.Name, device.Id));
            continue;
        }

        ReferenceWithContext hit = null;
        try { hit = intersector.FindNearest(point, XYZ.BasisZ); }
        catch { }

        if (hit == null)
        {
            noCeilingAbove.Add(device.Id);
            findings.Add(string.Format("{0} (id {1}): NO CEILING above it. Fine in an open soffit; "
                + "otherwise the device is in the wrong place or the ceiling is missing",
                device.Name, device.Id));
            continue;
        }

        var gap = hit.Proximity;
        if (gap <= tolerance)
        {
            findings.Add(string.Format("{0} (id {1}): AT the ceiling, {2:0.#} mm below it",
                device.Name, device.Id, gap * 304.8));
        }
        else
        {
            outOfPlane.Add(device.Id);
            findings.Add(string.Format("{0} (id {1}): {2:0.#} mm BELOW the ceiling - past the {3:0.#} mm "
                + "allowed. Invisible in plan; it shows up on site",
                device.Name, device.Id, gap * 304.8, tolerance * 304.8));
        }
    }

    findings.Insert(0, string.Format("{0} device(s) checked against {1} host-model ceiling(s): {2} out "
        + "of plane, {3} with no ceiling above. A large NO CEILING count where you know there is one "
        + "usually means the family's insertion point sits off its own centre and the ray missed - "
        + "check one in a section before believing it",
        devices.Count, hostCeilings, outOfPlane.Count, noCeilingAbove.Count));
}
