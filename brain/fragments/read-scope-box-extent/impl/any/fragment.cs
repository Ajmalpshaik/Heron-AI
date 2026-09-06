// NOT STANDALONE. Assumes `doc` and `scopeBoxName` are in scope, and leaves
// `minMm`, `maxMm`, `scopeBoxId` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THERE IS NO SCOPE BOX CLASS.
//
// A scope box is an ordinary element of the volume-of-interest category, with
// no type of its own anywhere in the API - which is why none can be created
// from code, and why its extent has to come from its bounding box rather than
// from any property that names the corners.
//
// MILLIMETRES, AGAINST THE HABIT OF THIS LIBRARY.
//
// Everything else here hands on Revit's internal feet and converts at the
// edge. The region selection this exists to feed takes its two corners in
// millimetres, and publishing the same unit under the same names is what lets
// the two compose with nothing in between. A conversion hidden in the middle
// of a composition is the kind nobody finds.
//
// AMBIGUITY IS REFUSED, NOT RESOLVED.
//
// Boxes are named for zones and phases, so a partial name is what gets typed
// and two boxes matching it is normal. Picking the first would silently answer
// about the wrong half of a building.

const double FeetToMm = 304.8;

XYZ minMm = null;
XYZ maxMm = null;
ElementId scopeBoxId = ElementId.InvalidElementId;
var findings = new List<string>();

string wanted = (scopeBoxName ?? "").Trim();

var matches = new List<Element>();
foreach (var element in new FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
             .WhereElementIsNotElementType())
{
    if (element == null) continue;
    string name = "";
    try { name = element.Name ?? ""; } catch { }

    if (wanted.Length == 0 ||
        name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
        matches.Add(element);
}

if (matches.Count == 0)
{
    findings.Add(wanted.Length == 0
        ? "This model has no scope boxes at all."
        : "No scope box matching '" + wanted + "'. Scope boxes cannot be created from code on any " +
          "release - one has to be drawn by hand first.");
}
else if (matches.Count > 1)
{
    var names = new List<string>();
    foreach (var match in matches)
    {
        try { names.Add(match.Name ?? "(unnamed)"); } catch { names.Add("(unnamed)"); }
    }
    findings.Add("'" + wanted + "' matches " + matches.Count + " scope boxes (" +
                 string.Join(", ", names.ToArray()) + "). Refused rather than picking one: they are " +
                 "usually different halves of a building.");
}
else
{
    var box = matches[0];
    scopeBoxId = box.Id;

    BoundingBoxXYZ extent = null;
    try { extent = box.get_BoundingBox(null); } catch { }

    if (extent == null)
    {
        findings.Add("Scope box '" + box.Name + "' has no readable extent.");
    }
    else
    {
        minMm = new XYZ(extent.Min.X * FeetToMm, extent.Min.Y * FeetToMm, extent.Min.Z * FeetToMm);
        maxMm = new XYZ(extent.Max.X * FeetToMm, extent.Max.Y * FeetToMm, extent.Max.Z * FeetToMm);
        findings.Add("Scope box '" + box.Name + "': " +
                     Math.Round((maxMm.X - minMm.X)) + " x " +
                     Math.Round((maxMm.Y - minMm.Y)) + " x " +
                     Math.Round((maxMm.Z - minMm.Z)) + " mm.");
    }
}
