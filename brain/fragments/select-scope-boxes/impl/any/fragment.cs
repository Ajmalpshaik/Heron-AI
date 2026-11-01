// NOT STANDALONE. Assumes `doc`, `nameContains` and `extent` are in scope;
// leaves `elements`, `matchedNames`, `scanned`, `found`, `minMm`, `maxMm`,
// `scopeBoxId` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// FOUND BY CATEGORY, NOT BY CLASS. A scope box has no dedicated API class - it
// is an ordinary element in the Volume of Interest category. Looking for a
// ScopeBox type finds nothing at all, which is the most likely wrong turn here.
//
// VERSION 2 (2026-11-01) - READ_SCOPE_BOX_EXTENT MERGED IN, AND WHAT IT HAD
// THAT THIS LACKED:
//
//   ONE BOX'S CORNERS, `minMm` AND `maxMm`, under the names SELECT_IN_REGION
//   takes, and its id in `scopeBoxId`. Read from the box's bounding box - there
//   is no property naming a scope box's corners - and published in
//   MILLIMETRES, against the habit of this library, because the region
//   selection they feed takes millimetres at its own edge: the same unit
//   under the same names is what lets the two compose with nothing in
//   between. The size goes in `findings`. Corners are published only when
//   exactly ONE box matches; with no readable extent, the id and no corners -
//   never corners at the origin, which would select the wrong part of the
//   model.
//
//   AMBIGUITY IS REFUSED WHEN AN EXTENT IS ASKED. `extent` true is "how big is
//   it, where is it": two or more boxes matching the name are refused - no
//   corners, no id, and NO ELEMENTS - and `findings` names them. Boxes are
//   named for zones and phases, so a partial name matching two is normal, and
//   picking the first answers about the wrong half of a building. Left out,
//   the list of every match is this card's answer as it always was, and the
//   corners come only with a single match.
//
//   None matching is said in `findings`, and a model with no scope boxes at
//   all is said differently from a name that matched none - with the reminder
//   that a scope box cannot be created from code on any release.

const double FeetToMm = 304.8;

var elements = new List<Element>();
var matchedNames = new List<string>();
var scanned = 0;
XYZ minMm = null;
XYZ maxMm = null;
ElementId scopeBoxId = ElementId.InvalidElementId;
var findings = new List<string>();

var needle = string.IsNullOrEmpty(nameContains) ? null : nameContains;

foreach (var candidate in new FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
             .WhereElementIsNotElementType())
{
    if (candidate == null) continue;

    scanned++;

    string name = "";
    try { name = candidate.Name ?? ""; } catch { }

    if (needle != null)
    {
        if (string.IsNullOrEmpty(name)) continue;
        if (name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
    }

    elements.Add(candidate);
    matchedNames.Add(string.IsNullOrEmpty(name) ? "(no name)" : name);
}

if (elements.Count == 0)
{
    findings.Add(scanned == 0
        ? "This model has no scope boxes at all. Scope boxes cannot be created from code on any "
          + "release - one has to be drawn by hand first."
        : "No scope box matching '" + nameContains + "' among the " + scanned + " in this model. "
          + "Scope boxes cannot be created from code on any release - one has to be drawn by hand first.");
}
else if (elements.Count > 1)
{
    if (extent)
    {
        // REFUSED, NOT RESOLVED - and the list withheld too, so nothing after
        // this can act on a box nobody chose.
        findings.Add("'" + (nameContains ?? "") + "' matches " + elements.Count + " scope boxes ("
            + string.Join(", ", matchedNames.ToArray()) + "). Refused rather than picking one: they "
            + "are usually different halves of a building. Name one of them.");
        elements.Clear();
        matchedNames.Clear();
    }
    else
    {
        findings.Add(elements.Count + " scope boxes match - corners are given for one box only; "
            + "name one to read its extent.");
    }
}
else
{
    var box = elements[0];
    scopeBoxId = box.Id;

    BoundingBoxXYZ bounds = null;
    try { bounds = box.get_BoundingBox(null); } catch { }

    if (bounds == null)
    {
        findings.Add("Scope box '" + matchedNames[0] + "' has no readable extent.");
    }
    else
    {
        minMm = new XYZ(bounds.Min.X * FeetToMm, bounds.Min.Y * FeetToMm, bounds.Min.Z * FeetToMm);
        maxMm = new XYZ(bounds.Max.X * FeetToMm, bounds.Max.Y * FeetToMm, bounds.Max.Z * FeetToMm);
        findings.Add("Scope box '" + matchedNames[0] + "': " +
                     Math.Round(maxMm.X - minMm.X) + " x " +
                     Math.Round(maxMm.Y - minMm.Y) + " x " +
                     Math.Round(maxMm.Z - minMm.Z) + " mm.");
    }
}

var found = elements.Count;
