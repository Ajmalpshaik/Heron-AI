// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `unsupported`, `checkedCount`, `report`, `skippedMembers` and
// `findings` behind.
//
// VERSION 2 (2026-11-24, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - A BEAM OR COLUMN WITH NO LOCATION TO CHECK IS COUNTED. Version 1
// passed over a beam with no location line, or a column with no base point,
// without a word, so "0 end(s) or base(s) with nothing found" could be said
// of a member never checked. `skippedMembers` carries the count every time;
// the sentence naming it is added only when it is above 0, so every other
// answer reads as version 1's. Elements of other categories are not counted -
// they are not beams or columns.
//
// READ ONLY. Opens no transaction and needs none.
//
// BEAM ENDS AND COLUMN BASES WITH NOTHING UNDER THEM - a geometric sweep, not
// an analysis. For each element handed in:
//
//   a beam or brace   each end must touch a structural column, a wall,
//                     another beam or a foundation, within toleranceMm
//   a column          its base must sit on a foundation, a floor, a beam or a
//                     wall within toleranceMm
//
// "Touch" is measured against each candidate's bounding box grown by the
// tolerance, so a support that is there but drawn a few millimetres short is
// still found and one metres away is not. A box is generous on a sloped or
// curved member, and the finding says it is a geometric check.

const double MmPerFoot = 304.8;
var unsupported = new List<string>();
var checkedCount = 0;
// VERSION 2 (D-64): beams and columns handed in with no location to check.
var skippedMembers = 0;
var lines = new List<string>();
var findings = new List<string>();
var tolerance = (toleranceMm <= 0 ? 50.0 : toleranceMm) / MmPerFoot;

var supportCategories = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_StructuralColumns),
    new ElementId(BuiltInCategory.OST_StructuralFraming),
    new ElementId(BuiltInCategory.OST_StructuralFoundation),
    new ElementId(BuiltInCategory.OST_Walls),
    new ElementId(BuiltInCategory.OST_Floors),
};
var framing = new ElementId(BuiltInCategory.OST_StructuralFraming);
var columns = new ElementId(BuiltInCategory.OST_StructuralColumns);

Func<XYZ, ElementId, bool, bool> supported = (point, self, below) =>
{
    var low = new XYZ(point.X - tolerance, point.Y - tolerance, point.Z - (below ? tolerance * 4 : tolerance));
    var high = new XYZ(point.X + tolerance, point.Y + tolerance, point.Z + tolerance);
    var filter = new BoundingBoxIntersectsFilter(new Outline(low, high));
    foreach (var candidate in new FilteredElementCollector(doc).WherePasses(filter).WhereElementIsNotElementType())
    {
        if (candidate.Id == self || candidate.Category == null) continue;
        if (!supportCategories.Contains(candidate.Category.Id)) continue;
        return true;
    }
    return false;
};

foreach (var element in elements)
{
    if (element == null || element.Category == null) continue;
    if (element.Category.Id == framing)
    {
        var curve = element.Location as LocationCurve;
        if (curve == null || curve.Curve == null) { skippedMembers++; continue; }
        checkedCount++;
        for (var end = 0; end < 2; end++)
        {
            if (!supported(curve.Curve.GetEndPoint(end), element.Id, false))
            {
                unsupported.Add("beam " + element.Id + " " + (end == 0 ? "start" : "end"));
                lines.Add("beam " + element.Id + ": nothing within " + Math.Round(tolerance * MmPerFoot) + " mm of its "
                    + (end == 0 ? "start" : "end") + ".");
            }
        }
    }
    else if (element.Category.Id == columns)
    {
        XYZ basePoint = null;
        var point = element.Location as LocationPoint;
        var curve = element.Location as LocationCurve;
        if (curve != null && curve.Curve != null)
        {
            var a = curve.Curve.GetEndPoint(0);
            var b = curve.Curve.GetEndPoint(1);
            basePoint = a.Z <= b.Z ? a : b;
        }
        else if (point != null)
        {
            var box = element.get_BoundingBox(null);
            basePoint = box == null ? point.Point : new XYZ(point.Point.X, point.Point.Y, box.Min.Z);
        }
        if (basePoint == null) { skippedMembers++; continue; }
        checkedCount++;
        if (!supported(basePoint, element.Id, true))
        {
            unsupported.Add("column " + element.Id + " base");
            lines.Add("column " + element.Id + ": nothing under its base within " + Math.Round(tolerance * MmPerFoot) + " mm.");
        }
    }
}

var report = string.Join(" | ", lines);
findings.Add(checkedCount + " beam(s) and column(s) checked; " + unsupported.Count + " end(s) or base(s) with nothing found "
    + "to bear on. A geometric check by bounding box - not a structural analysis.");
// VERSION 2 (D-64): said only when a beam or column was passed over.
if (skippedMembers > 0)
    findings.Add(skippedMembers + " beam(s) or column(s) had no location line or base point to check, so they were "
        + "NOT checked and are in neither count above.");
