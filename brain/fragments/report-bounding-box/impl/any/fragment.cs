// NOT STANDALONE. Assumes `doc`, `elements` and `maxRows` are in scope; leaves
// `findings`, `measured`, `noBoundingBox` and `combinedSizeMm` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE BOX IS ALIGNED TO PROJECT X AND Y. It is NOT the element's own size. For
// anything square to the project the two agree; for anything at an angle the box
// is bigger than the object, in a shape the object does not have. That is why
// "how big is this room" belongs to MEASURE_ROOM_DIMENSIONS and not here.
//
// `null` IS PASSED AS THE VIEW ON PURPOSE - that asks for the MODEL box. Passing
// a view gives the box as cropped by that view, which is not the element's
// extent and would change answer depending on which view was open.
//
// THE COMBINED BOX IS COMPUTED FROM EVERY ELEMENT, NOT ONLY THE LISTED ONES.
// The row list is capped for readability; the combined extent is the answer to
// "will it fit", and capping that would silently answer about a subset.
//
// AN ELEMENT WITH NO BOX IS REPORTED, NOT SKIPPED. Skipping makes the combined
// box look like it covers everything that was asked about.

var findings = new List<string>();
var measured = 0;
var noBoundingBox = new List<string>();
var combinedSizeMm = "";

var cap = maxRows > 0 ? maxRows : 50;

double minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;
var haveCombined = false;
var listed = 0;

foreach (var element in elements)
{
    if (element == null) continue;

    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); }
    catch (Exception) { box = null; }

    if (box == null)
    {
        noBoundingBox.Add(string.Format("{0} (id {1}) - no bounding box; a view-specific, annotation "
            + "or empty element", element.Name, element.Id));
        continue;
    }

    measured++;

    // Combined first, so it covers EVERY element and not just the listed ones.
    if (!haveCombined)
    {
        minX = box.Min.X; minY = box.Min.Y; minZ = box.Min.Z;
        maxX = box.Max.X; maxY = box.Max.Y; maxZ = box.Max.Z;
        haveCombined = true;
    }
    else
    {
        minX = Math.Min(minX, box.Min.X); minY = Math.Min(minY, box.Min.Y);
        minZ = Math.Min(minZ, box.Min.Z);
        maxX = Math.Max(maxX, box.Max.X); maxY = Math.Max(maxY, box.Max.Y);
        maxZ = Math.Max(maxZ, box.Max.Z);
    }

    if (listed < cap)
    {
        findings.Add(string.Format("{0} (id {1}): {2:0} x {3:0} x {4:0} mm, lowest corner at "
            + "({5:0}, {6:0}, {7:0}) mm",
            element.Name, element.Id,
            (box.Max.X - box.Min.X) * 304.8,
            (box.Max.Y - box.Min.Y) * 304.8,
            (box.Max.Z - box.Min.Z) * 304.8,
            box.Min.X * 304.8, box.Min.Y * 304.8, box.Min.Z * 304.8));
        listed++;
    }
}

if (measured > listed)
{
    findings.Add(string.Format("... {0} more measured but not listed - the COMBINED box below still "
        + "covers all {1}", measured - listed, measured));
}

if (haveCombined)
{
    combinedSizeMm = string.Format("{0:0} x {1:0} x {2:0} mm",
        (maxX - minX) * 304.8, (maxY - minY) * 304.8, (maxZ - minZ) * 304.8);

    findings.Add(string.Format("COMBINED across {0} element(s): {1}, from ({2:0}, {3:0}, {4:0}) to "
        + "({5:0}, {6:0}, {7:0}) mm. Aligned to project X and Y, so anything at an angle reads "
        + "larger than it is",
        measured, combinedSizeMm,
        minX * 304.8, minY * 304.8, minZ * 304.8,
        maxX * 304.8, maxY * 304.8, maxZ * 304.8));
}
else
{
    findings.Add("Nothing handed in has a bounding box, so there is no combined extent to report");
}

if (noBoundingBox.Count > 0)
{
    findings.Add(string.Format("{0} element(s) have no bounding box and are NOT in the combined "
        + "figure above", noBoundingBox.Count));
}

findings.Insert(0, string.Format("{0} of {1} element(s) measured", measured, elements.Count));
