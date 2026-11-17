// NOT STANDALONE. Assumes `doc`, `linkName`, `datums`, `toleranceMm` and
// `angleToleranceDeg` are in scope; leaves `report`, `failures`, `aligned`,
// `compared`, `notChecked`, `hostOnly`, `linkOnly`, `failingIds`, `elements`,
// `linkUsed`, `refused` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none. Nothing in the link or in
// this model is moved, renamed or selected.
//
// DO THIS MODEL'S GRIDS AND LEVELS SIT WHERE THE SAME GRIDS AND LEVELS SIT IN
// ONE LINKED MODEL? A setting-out check before an issue. Each of the link's
// datums is carried into this model's space through the link placement's TOTAL
// transform - its rotation, its move and its height - and measured against
// this model's datum of the same name.
//
// WHICH LINK. `linkName` picks one placement: its full name ("Arch.rvt : 1 :
// location <Not Shared>") or a part of it ("Arch"). Left out it reads "only",
// and the one link placement this model has is used. Two placements matching,
// or none, is refused with every placement named - two right answers is not
// one, and this never picks. A link nested inside that link is not read.
//
// MATCHED BY NAME FIRST, BY POSITION SECOND. A link datum takes this model's
// datum of the same kind and the same name. One with no namesake here takes
// the FIRST unmatched datum of the same kind here that lines up with it within
// both tolerances - in drawing order for grids, bottom to top for levels - and
// the row says "name differs" and gives both names. What is left over is
// "link only" or "host only". A datum is matched once.
//
// LEVELS. Each level's ProjectElevation - the one in the same space as every
// coordinate (REPORT_LEVEL_ELEVATIONS says why) - with the link's carried
// through the transform, so the link's height offset counts. The offset is the
// link's level minus this model's, + when the link's is higher. If the
// placement is TILTED - its Z axis not straight up within 1e-9 - a level is no
// longer a height, and no level is compared: every level row is "not checked"
// with that reason. Never a guess at what the height would have been.
//
// GRIDS, STRAIGHT. In plan: the distance square to this model's grid line from
// the link grid's START point, and the angle between the two, brought into
// (-90, 90] degrees, "drawn the opposite way" when the link grid runs the
// other way along the same line - which still lines up. Aligned when the
// distance is within toleranceMm AND the angle within angleToleranceDeg. The
// angle is its own figure because the start point alone cannot see a turn:
// 0.01 degrees moves the far end of a 60 m grid 10.5 mm.
//
// GRIDS, ARCS. The centres' distance apart in plan and the two radii: aligned
// when both are within toleranceMm. An arc against a straight grid is "not
// checked". A MULTI-SEGMENT grid is "not checked" either side, with the
// reason: its segments are not compared in this version. A tilted placement
// leaves every grid "not checked" too - a tilted grid is not a vertical plane,
// so no plan position is the whole truth about it.
//
// THE COUNTS ARE FOR A QA CHECK. `failures` is the number of datums that are
// "off"; `failingIds` and `elements` are this model's datums that are off, to
// select. `compared` is how many pairs were measured, `notChecked` how many
// could not be. A refusal compares nothing and says so, so `compared` 0 is how
// a checklist tells "nothing off" from "nothing read" (D-165).
//
// MILLIMETRES BY ARITHMETIC. Internal feet times 304.8, D-20's fixed ratio -
// no units API, so the 2021 units change never reaches this. Angles in
// degrees by 180 / pi.
//
// "ALIGNED WITHIN X MM" IS THE WORDING. Whether the setting-out is right is
// the engineer's call, not this check's.

const double MmPerFoot = 304.8;
const double Vertical = 1e-9;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

var report = "";
var failures = 0;
var aligned = 0;
var compared = 0;
var notChecked = 0;
var hostOnly = 0;
var linkOnly = 0;
var failingIds = new List<ElementId>();
var elements = new List<Element>();
var linkUsed = "";
string refused = null;
var findings = new List<string>();

// ---- What was asked ---------------------------------------------------------

var wantText = (datums ?? "").Trim().ToLowerInvariant();
var doGrids = false;
var doLevels = false;
if (wantText == "" || wantText == "both" || wantText == "all"
    || wantText == "grids and levels" || wantText == "levels and grids")
{
    doGrids = true;
    doLevels = true;
}
else if (wantText == "grids" || wantText == "grid") doGrids = true;
else if (wantText == "levels" || wantText == "level") doLevels = true;
else refused = string.Format("'{0}' is not grids, levels or both - say which datums to check.", datums);

if (refused == null && (double.IsNaN(toleranceMm) || double.IsInfinity(toleranceMm) || toleranceMm <= 0))
    refused = string.Format("A distance tolerance of {0} mm cannot be met by anything - give one above 0 mm "
        + "(left out, it is 0.5 mm).", toleranceMm.ToString(invariant));

if (refused == null && (double.IsNaN(angleToleranceDeg) || double.IsInfinity(angleToleranceDeg)
    || angleToleranceDeg <= 0 || angleToleranceDeg >= 90))
    refused = string.Format("An angle tolerance of {0} degrees is not one this can use - give one above 0 and "
        + "below 90 (left out, it is 0.01).", angleToleranceDeg.ToString(invariant));

// ---- Which link -------------------------------------------------------------

var placements = new List<RevitLinkInstance>();
foreach (var instance in new FilteredElementCollector(doc)
    .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
{
    if (instance == null) continue;
    RevitLinkType linkType = null;
    try { linkType = doc.GetElement(instance.GetTypeId()) as RevitLinkType; }
    catch (Exception) { linkType = null; }
    if (linkType != null && linkType.IsNestedLink) continue;
    placements.Add(instance);
}

var everyName = placements.Count == 0 ? "no links at all"
    : string.Join(", ", placements.Select(p => "'" + (p.Name ?? "(unnamed)") + "'").ToArray());

RevitLinkInstance chosen = null;
var given = (linkName ?? "").Trim();
if (refused == null)
{
    if (placements.Count == 0)
        refused = "This model has no Revit links, so there is nothing to check its grids and levels against.";
    else if (given.Length == 0 || string.Equals(given, "only", StringComparison.OrdinalIgnoreCase))
    {
        if (placements.Count == 1) chosen = placements[0];
        else refused = string.Format("This model has {0} link placements - {1} - so the one to check against "
            + "has to be named.", placements.Count, everyName);
    }
    else
    {
        var exact = placements.Where(p => string.Equals((p.Name ?? "").Trim(), given,
            StringComparison.OrdinalIgnoreCase)).ToList();
        var matching = exact.Count > 0 ? exact
            : placements.Where(p => (p.Name ?? "").IndexOf(given, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        if (matching.Count == 1) chosen = matching[0];
        else if (matching.Count == 0)
            refused = string.Format("No link matches '{0}'. This model has: {1}.", given, everyName);
        else
            refused = string.Format("AMBIGUOUS: {0} link placements match '{1}' - {2}. Name one exactly rather "
                + "than have this pick.", matching.Count, given,
                string.Join(", ", matching.Select(p => "'" + (p.Name ?? "(unnamed)") + "'").ToArray()));
    }
}

Document linked = null;
Transform placed = null;
if (refused == null && chosen != null)
{
    try { linked = chosen.GetLinkDocument(); } catch (Exception) { linked = null; }
    if (linked == null)
        refused = string.Format("'{0}' is placed here but NOT LOADED, so it has no grids or levels to read. "
            + "Reload it and ask again.", chosen.Name);
    else
    {
        try { placed = chosen.GetTotalTransform(); } catch (Exception) { placed = null; }
        if (placed == null) refused = string.Format("Revit gave no placement for '{0}'.", chosen.Name);
        else linkUsed = chosen.Name ?? "";
    }
}

// ---- The datums, each side in this model's space ------------------------------
//
// One entry per datum: Item1 name, Item2 kind ("grid" or "level"), Item3 the
// element, Item4 its shape ("line", "arc", "multi", "other" or "level"), Item5
// a line's start or an arc's centre, Item6 a line's direction, Item7 an arc's
// radius or a level's height - all in this model's internal feet.

Func<Document, Transform, List<Tuple<string, string, Element, string, XYZ, XYZ, double>>> read = (d, t) =>
{
    var found = new List<Tuple<string, string, Element, string, XYZ, XYZ, double>>();
    if (doGrids)
    {
        var segments = new HashSet<ElementId>();
        foreach (var multi in new FilteredElementCollector(d).OfClass(typeof(MultiSegmentGrid))
            .Cast<MultiSegmentGrid>())
        {
            if (multi == null) continue;
            try { foreach (var id in multi.GetGridIds()) segments.Add(id); } catch (Exception) { }
            found.Add(Tuple.Create(multi.Name ?? "", "grid", (Element)multi, "multi", (XYZ)null, (XYZ)null, 0.0));
        }
        foreach (var grid in new FilteredElementCollector(d).OfClass(typeof(Grid)).Cast<Grid>())
        {
            if (grid == null || segments.Contains(grid.Id)) continue;
            var owner = ElementId.InvalidElementId;
            try { owner = MultiSegmentGrid.GetMultiSegementGridId(grid); } catch (Exception) { }
            if (owner != null && owner != ElementId.InvalidElementId) continue;

            Curve curve = null;
            try { curve = grid.Curve; } catch (Exception) { curve = null; }
            var line = curve as Line;
            var arc = curve as Arc;
            if (line != null)
                found.Add(Tuple.Create(grid.Name ?? "", "grid", (Element)grid, "line",
                    t.OfPoint(line.GetEndPoint(0)), t.OfVector(line.Direction), 0.0));
            else if (arc != null)
                found.Add(Tuple.Create(grid.Name ?? "", "grid", (Element)grid, "arc",
                    t.OfPoint(arc.Center), (XYZ)null, arc.Radius));
            else
                found.Add(Tuple.Create(grid.Name ?? "", "grid", (Element)grid, "other", (XYZ)null, (XYZ)null, 0.0));
        }
    }
    if (doLevels)
    {
        foreach (var level in new FilteredElementCollector(d).OfClass(typeof(Level)).Cast<Level>())
        {
            if (level == null) continue;
            found.Add(Tuple.Create(level.Name ?? "", "level", (Element)level, "level", (XYZ)null, (XYZ)null,
                t.OfPoint(new XYZ(0, 0, level.ProjectElevation)).Z));
        }
    }

    // Grids in the order a drawing reads them - letters as letters, trailing
    // digits as a number, so 2 comes before 10 - then levels bottom to top.
    Func<string, string> head = n =>
    {
        var cut = n.Length;
        while (cut > 0 && char.IsDigit(n[cut - 1])) cut--;
        return n.Substring(0, cut).ToUpperInvariant();
    };
    Func<string, long> tail = n =>
    {
        var cut = n.Length;
        while (cut > 0 && char.IsDigit(n[cut - 1])) cut--;
        long value;
        return n.Length - cut > 0 && n.Length - cut < 18 && long.TryParse(n.Substring(cut), out value) ? value : -1;
    };
    found.Sort((a, b) =>
    {
        if (a.Item2 != b.Item2) return a.Item2 == "grid" ? -1 : 1;
        if (a.Item2 == "level") return a.Item7.CompareTo(b.Item7);
        var byHead = string.Compare(head(a.Item1), head(b.Item1), StringComparison.Ordinal);
        if (byHead != 0) return byHead;
        var byTail = tail(a.Item1).CompareTo(tail(b.Item1));
        return byTail != 0 ? byTail : string.Compare(a.Item1, b.Item1, StringComparison.Ordinal);
    });
    return found;
};

var host = new List<Tuple<string, string, Element, string, XYZ, XYZ, double>>();
var link = new List<Tuple<string, string, Element, string, XYZ, XYZ, double>>();
var tilted = false;
var nestedInLink = 0;
if (refused == null)
{
    host = read(doc, Transform.Identity);
    link = read(linked, placed);
    var up = placed.BasisZ;
    tilted = Math.Abs(up.X) > Vertical || Math.Abs(up.Y) > Vertical || Math.Abs(up.Z - 1.0) > Vertical;
    try
    {
        nestedInLink = new FilteredElementCollector(linked).OfClass(typeof(RevitLinkInstance)).GetElementCount();
    }
    catch (Exception) { nestedInLink = 0; }
}

// ---- One comparison: status, offset in mm, angle in degrees, reason ---------

var tiltReason = "the link is placed TILTED - its Z axis is not straight up - so this cannot be compared";
Func<Tuple<string, string, Element, string, XYZ, XYZ, double>, Tuple<string, string, Element, string, XYZ, XYZ, double>,
    Tuple<string, double, double, string>> compare = (h, l) =>
{
    if (tilted) return Tuple.Create("not checked", double.NaN, double.NaN, tiltReason);

    if (h.Item2 == "level")
    {
        var rise = (l.Item7 - h.Item7) * MmPerFoot;
        return Tuple.Create(Math.Abs(rise) <= toleranceMm ? "aligned" : "off", rise, double.NaN,
            Math.Abs(rise) < 0.005 ? "" : (rise > 0 ? "the link's is higher" : "the link's is lower"));
    }

    if (h.Item4 == "multi" || l.Item4 == "multi")
        return Tuple.Create("not checked", double.NaN, double.NaN,
            "a multi-segment grid - its segments are not compared in this version");
    if (h.Item4 == "other" || l.Item4 == "other")
        return Tuple.Create("not checked", double.NaN, double.NaN, "a grid that is neither straight nor an arc");
    if (h.Item4 != l.Item4)
        return Tuple.Create("not checked", double.NaN, double.NaN, "one is an arc and the other straight");

    if (h.Item4 == "arc")
    {
        var cx = l.Item5.X - h.Item5.X;
        var cy = l.Item5.Y - h.Item5.Y;
        var centres = Math.Sqrt(cx * cx + cy * cy) * MmPerFoot;
        var radii = (l.Item7 - h.Item7) * MmPerFoot;
        var arcOk = centres <= toleranceMm && Math.Abs(radii) <= toleranceMm;
        return Tuple.Create(arcOk ? "aligned" : "off", centres, double.NaN,
            "centres " + centres.ToString("0.00", invariant) + " mm apart, radius "
            + (radii >= 0 ? "+" : "") + radii.ToString("0.00", invariant) + " mm in the link");
    }

    var ux = h.Item6.X; var uy = h.Item6.Y;
    var vx = l.Item6.X; var vy = l.Item6.Y;
    var uLength = Math.Sqrt(ux * ux + uy * uy);
    var vLength = Math.Sqrt(vx * vx + vy * vy);
    if (uLength < 1e-9 || vLength < 1e-9)
        return Tuple.Create("not checked", double.NaN, double.NaN, "a grid line with no direction in plan");
    ux /= uLength; uy /= uLength; vx /= vLength; vy /= vLength;

    var qx = l.Item5.X - h.Item5.X;
    var qy = l.Item5.Y - h.Item5.Y;
    var square = Math.Abs(ux * qy - uy * qx) * MmPerFoot;
    var angle = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy) * 180.0 / Math.PI;
    var reversed = false;
    if (angle > 90.0) { angle -= 180.0; reversed = true; }
    else if (angle <= -90.0) { angle += 180.0; reversed = true; }
    var lineOk = square <= toleranceMm && Math.Abs(angle) <= angleToleranceDeg;
    return Tuple.Create(lineOk ? "aligned" : "off", square, angle, reversed ? "drawn the opposite way" : "");
};

// ---- Match: by name, then by position -----------------------------------------

var pairOf = new int[link.Count];
var nameDiffers = new bool[link.Count];
var hostTaken = new bool[host.Count];
for (var i = 0; i < link.Count; i++) pairOf[i] = -1;

for (var i = 0; i < link.Count; i++)
{
    for (var j = 0; j < host.Count; j++)
    {
        if (hostTaken[j] || host[j].Item2 != link[i].Item2) continue;
        if (!string.Equals(host[j].Item1, link[i].Item1, StringComparison.Ordinal)) continue;
        pairOf[i] = j;
        hostTaken[j] = true;
        break;
    }
}

for (var i = 0; i < link.Count; i++)
{
    if (pairOf[i] >= 0 || tilted) continue;
    for (var j = 0; j < host.Count; j++)
    {
        if (hostTaken[j] || host[j].Item2 != link[i].Item2) continue;
        if (compare(host[j], link[i]).Item1 != "aligned") continue;
        pairOf[i] = j;
        nameDiffers[i] = true;
        hostTaken[j] = true;
        break;
    }
}

// ---- The rows -------------------------------------------------------------------

Func<double, string> mm = v => double.IsNaN(v) ? "-" : v.ToString("0.00", invariant) + " mm";
Func<double, string> deg = v => double.IsNaN(v) ? "-" : v.ToString("0.0000", invariant) + " deg";
var rows = new List<string>();
rows.Add("kind | name | status | offset | angle | reason");

for (var i = 0; i < link.Count; i++)
{
    var l = link[i];
    if (pairOf[i] < 0)
    {
        if (tilted)
        {
            notChecked++;
            rows.Add(string.Format("{0} | {1} | not checked | - | - | no {0} of that name here, and {2}",
                l.Item2, l.Item1, tiltReason));
        }
        else
        {
            linkOnly++;
            rows.Add(string.Format("{0} | {1} | link only | - | - | no {0} here has that name or lines up with it",
                l.Item2, l.Item1));
        }
        continue;
    }

    var h = host[pairOf[i]];
    var result = compare(h, l);
    var why = result.Item4;
    if (nameDiffers[i])
        why = "name differs - the link calls it '" + l.Item1 + "'" + (why.Length > 0 ? "; " + why : "");

    if (result.Item1 == "aligned") { aligned++; compared++; }
    else if (result.Item1 == "off")
    {
        failures++;
        compared++;
        failingIds.Add(h.Item3.Id);
        elements.Add(h.Item3);
    }
    else notChecked++;

    rows.Add(string.Format("{0} | {1} | {2} | {3} | {4} | {5}",
        h.Item2, h.Item1, result.Item1, mm(result.Item2), deg(result.Item3), why));
}

for (var j = 0; j < host.Count; j++)
{
    if (hostTaken[j]) continue;
    var h = host[j];
    if (tilted)
    {
        notChecked++;
        rows.Add(string.Format("{0} | {1} | not checked | - | - | no {0} of that name in the link, and {2}",
            h.Item2, h.Item1, tiltReason));
    }
    else
    {
        hostOnly++;
        rows.Add(string.Format("{0} | {1} | host only | - | - | no {0} in the link has that name or lines up with it",
            h.Item2, h.Item1));
    }
}

// ---- The answer -------------------------------------------------------------------

if (refused != null)
{
    findings.Add(refused + " NOTHING WAS COMPARED.");
}
else
{
    report = string.Join("\n", rows.ToArray());
    var what = doGrids && doLevels ? "grids and levels" : doGrids ? "grids" : "levels";
    findings.Add(string.Format("{0} checked against '{1}': {2} pair(s) compared - {3} aligned within {4} mm "
        + "and {5} degrees, {6} off; {7} not checked, {8} only in this model, {9} only in the link.",
        what, linkUsed, compared, aligned, toleranceMm.ToString(invariant),
        angleToleranceDeg.ToString(invariant), failures, notChecked, hostOnly, linkOnly));
    if (failures > 0)
        findings.Add(string.Format("Off: {0}. Their ids are in failingIds, to select.", string.Join(", ",
            elements.Select(e => "'" + (e.Name ?? "") + "'").ToArray())));
    if (tilted)
        findings.Add("The link is placed TILTED, so no grid or level was compared - every row says so.");
    findings.Add("Grids are measured in plan from the link grid's start point and by angle; levels by "
        + "their height in this model's space, the link's placement included.");
    if (nestedInLink > 0)
        findings.Add(string.Format("{0} link placement(s) nested inside '{1}' were NOT read.", nestedInLink, linkUsed));
    if (compared == 0)
        findings.Add("Nothing was compared, so no figure here says the datums line up.");
}
