// NOT STANDALONE. Assumes `doc`, `elements`, `fabricSheetType`, `pointMm`,
// `majorDirection` and `location` are in scope; leaves `placed`,
// `alreadyThere`, `skipped`, `sheetTypes`, `readBackWrong`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S STRUCTURE > REINFORCEMENT > FABRIC SHEET: ONE flat welded-mesh sheet
// in ONE host, at the point the modeller gives - not a Fabric Area over the
// whole host (that is PLACE_FABRIC_REINFORCEMENT). The API is the same on
// every release 2020 to 2027 (read with tools/api-surface --members on
// 2026-11-01):
//   FabricSheet.IsValidHost(host)                 - the host test Revit uses
//   FabricSheet.Create(doc, host, sheetTypeId)    - the sheet, in that host
//   sheet.PlaceInHost(host, transform)            - where it sits, and turned
//   sheet.IsSingleFabricSheetWithinHost(host, transform)
//   sheet.FabricLocation, sheet.GetSheetLocation(), sheet.GetWireCenterlines()
//
// THE SHEET TYPE IS NEVER GUESSED (D-33). A Fabric Sheet Type named exactly.
// None named, or a name not loaded: NOTHING IS PLACED and the loaded sheet
// types come back in `sheetTypes`, with their sizes.
//
// THE POINT IS THE SHEET'S CENTRE, in millimetres from the model's internal
// origin, "x,y" on a slab (the height is the slab's own) or "x,y,z" on a wall.
// What PlaceInHost's transform origin means - the sheet's corner, its centre,
// or a move from where Create left it - is NOT KNOWN from the assemblies, so
// it is not assumed: the sheet is placed with its origin at the point, its
// centre is MEASURED from its own wires, and when the centre is more than a
// millimetre off the point the sheet is placed again by the difference, then
// measured again. Millimetres become feet by / 304.8 (D-20).
//
// REFUSED, BY NAME: anything that is not a floor, a wall, a foundation slab or
// a Part; a floor or wall whose Structural box is off; one Revit does not take
// as a fabric host; a face word that is not the host's; more than one host -
// one sheet goes in one host, and which is the modeller's to say.
//
// NEVER DOUBLED: a single sheet of the same type already centred within 10 mm
// of the point in the same host is left as it was and named in `alreadyThere`.
//
// READ BACK after a regenerate: the host, the sheet type, the face, the centre
// and the major direction. Anything that reads back other than asked is named
// in `readBackWrong`; a sheet Revit says does not lie within its host is
// removed again and named in `skipped`. Ids stay ElementId, never numbers.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;

var placed = new List<ElementId>();
var alreadyThere = new List<string>();
var skipped = new List<string>();
var sheetTypes = new List<string>();
var readBackWrong = new List<string>();
var findings = new List<string>();
var lines = new List<string>();
string refused = "";

Func<double, string> mm = feet => (feet * 304.8).ToString("0", invariant);
Func<XYZ, string> mmPoint = q => mm(q.X) + "," + mm(q.Y) + "," + mm(q.Z);
Func<Element, string> named = e => (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

// ---- the sheet type ---------------------------------------------------------------------
var loaded = new FilteredElementCollector(doc).OfClass(typeof(FabricSheetType)).Cast<FabricSheetType>()
    .OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
foreach (var t in loaded)
    sheetTypes.Add("'" + t.Name + "' " + mm(t.OverallLength) + " x " + mm(t.OverallWidth) + " mm");
var loadedText = loaded.Count == 0
    ? "none - load or make the fabric sheet type the drawings name"
    : string.Join(", ", sheetTypes);

var sheetText = (fabricSheetType ?? "").Trim();
FabricSheetType sheetType = null;
if (sheetText.Length == 0 || sheetText.Equals("none", StringComparison.OrdinalIgnoreCase))
    refused = "No fabric sheet type was named - Heron never chooses one. Loaded: " + loadedText + ". Nothing was placed";
else
{
    sheetType = loaded.FirstOrDefault(t => string.Equals(t.Name, sheetText, StringComparison.Ordinal));
    if (sheetType == null)
        refused = "No Fabric Sheet Type is named '" + sheetText + "'. Loaded: " + loadedText + ". Nothing was placed";
}

// ---- the point, the direction and the face ----------------------------------------------
double[] at = null;
var pointParts = (pointMm ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
if (pointParts.Count == 2 || pointParts.Count == 3)
{
    var values = new double[pointParts.Count];
    var allRead = true;
    for (var i = 0; i < pointParts.Count; i++)
        allRead &= double.TryParse(pointParts[i], styleFloat, invariant, out values[i]);
    if (allRead) at = values;
}
if (refused.Length == 0 && at == null)
    refused = "'" + (pointMm ?? "") + "' is not a point - give the sheet's centre in millimetres, x,y on a slab or x,y,z on a wall. Nothing was placed";

var directionText = (majorDirection ?? "").Trim().ToLowerInvariant();
double angle = 0;
var angleRead = true;
if (directionText.Length == 0 || directionText == "x" || directionText == "horizontal") angle = 0;
else if (directionText == "y" || directionText == "vertical") angle = 90;
else angleRead = double.TryParse(directionText.Replace("degrees", "").Replace("deg", "").Trim(), styleFloat, invariant, out angle);
if (refused.Length == 0 && !angleRead)
    refused = "'" + (majorDirection ?? "") + "' is not a direction - say x, y or an angle in degrees on a slab, horizontal or vertical on a wall. Nothing was placed";

var where = (location ?? "").Trim().ToLowerInvariant();
bool? wantTop = null;
if (where == "top" || where == "exterior") wantTop = true;
else if (where == "bottom" || where == "interior") wantTop = false;
else if (refused.Length == 0 && where.Length > 0 && where != "unchanged")
    refused = "'" + (location ?? "") + "' is not a face - say top or bottom on a slab, exterior or interior on a wall. Nothing was placed";

// ---- the host ---------------------------------------------------------------------------
var hosts = new List<Element>();
var given = 0;
if (refused.Length == 0)
{
    foreach (var e in elements ?? new List<Element>())
    {
        if (e == null) continue;
        given++;
        if (!(e is Floor) && !(e is Wall) && !(e is Part))
        {
            skipped.Add(named(e) + ": not a floor, a wall, a foundation slab or a Part - a fabric sheet goes only in one of those");
            continue;
        }
        Parameter structural = null;
        if (e is Floor) structural = e.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
        else if (e is Wall) structural = e.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
        if (structural != null && structural.HasValue && structural.AsInteger() == 0)
        {
            skipped.Add(named(e) + ": not structural - its Structural box is off in Properties, and a fabric sheet goes only in a structural host");
            continue;
        }
        if (!FabricSheet.IsValidHost(e))
        {
            skipped.Add(named(e) + ": Revit does not take it as a fabric sheet host - that is a structural concrete floor, wall or foundation slab, or a Part made from a structural layer of one");
            continue;
        }
        hosts.Add(e);
    }
    if (hosts.Count == 0)
        refused = (given == 0 ? "Nothing was selected - select the slab or wall the sheet goes in"
            : "None of the " + given + " element(s) given is a host a fabric sheet can go in") + ". Nothing was placed";
    else if (hosts.Count > 1)
        refused = hosts.Count + " hosts that take a fabric sheet are selected: " + string.Join(", ", hosts.Select(named))
            + " - one sheet goes in one host, so select only the one it goes in. Nothing was placed";
}

// The sheet's centre, read off its own wires: the middle of their extents is the
// middle of the sheet, however it is turned. Its bounding box when it has none.
Func<FabricSheet, XYZ> centreOf = s =>
{
    IList<Curve> wires = null;
    try { wires = s.GetWireCenterlines(); }
    catch (Exception) { wires = null; }
    var ends = new List<XYZ>();
    if (wires != null)
        foreach (var w in wires)
        {
            if (w == null || !w.IsBound) continue;
            ends.Add(w.GetEndPoint(0));
            ends.Add(w.GetEndPoint(1));
        }
    if (ends.Count == 0)
    {
        var box = s.get_BoundingBox(null);
        return box == null ? null : box.Min.Add(box.Max).Multiply(0.5);
    }
    return new XYZ((ends.Min(q => q.X) + ends.Max(q => q.X)) / 2,
                   (ends.Min(q => q.Y) + ends.Max(q => q.Y)) / 2,
                   (ends.Min(q => q.Z) + ends.Max(q => q.Z)) / 2);
};

if (refused.Length == 0)
{
    var host = hosts[0];
    var name = named(host);
    string problem = null;

    // A Part takes its plane from the slab or wall it was cut from.
    Element planeSource = host;
    for (var depth = 0; depth < 5 && planeSource is Part; depth++)
    {
        Element from = null;
        foreach (var link in ((Part)planeSource).GetSourceElementIds())
        {
            if (link == null || link.HostElementId == ElementId.InvalidElementId) continue;
            from = doc.GetElement(link.HostElementId);
            if (from != null) break;
        }
        if (from == null) break;
        planeSource = from;
    }
    var isWall = planeSource is Wall;
    if (!isWall && !(planeSource is Floor))
        problem = "a Part whose original element is not a slab or a wall that Heron can read, so the plane of the sheet is not known";

    if (problem == null && wantTop.HasValue && isWall != (where == "exterior" || where == "interior"))
        problem = "a " + (isWall ? "wall's faces are exterior and interior" : "slab's faces are top and bottom") + ", not " + where;

    XYZ normal = null, major = null, minor = null, centreAsked = null;
    if (problem == null)
    {
        var radians = angle * Math.PI / 180;
        if (isWall)
        {
            var wall = (Wall)planeSource;
            var curve = wall.Location as LocationCurve;
            var line = curve == null ? null : curve.Curve as Line;
            if (line == null) problem = "a curved wall - a flat sheet has no one plane in it";
            else if (at.Length < 3) problem = "a wall needs the height too - give the point as x,y,z in millimetres";
            else
            {
                var along = new XYZ(line.Direction.X, line.Direction.Y, 0).Normalize();
                normal = wall.Orientation.Normalize();
                major = along.Multiply(Math.Cos(radians)).Add(XYZ.BasisZ.Multiply(Math.Sin(radians))).Normalize();
                minor = normal.CrossProduct(major).Normalize();
                var given3 = new XYZ(at[0] / 304.8, at[1] / 304.8, at[2] / 304.8);
                var off = given3.Subtract(line.Origin).DotProduct(normal);
                var allowed = wall.Width / 2 + 10 / 304.8;
                if (Math.Abs(off) > allowed)
                    problem = "the point is " + mm(Math.Abs(off)) + " mm off the wall, which is " + mm(wall.Width) + " mm thick - give a point on it";
                else centreAsked = given3.Subtract(normal.Multiply(off));
            }
        }
        else
        {
            normal = XYZ.BasisZ;
            major = new XYZ(Math.Cos(radians), Math.Sin(radians), 0);
            minor = normal.CrossProduct(major).Normalize();
            double z;
            if (at.Length == 3) z = at[2] / 304.8;
            else
            {
                var box = host.get_BoundingBox(null);
                z = box == null ? 0 : (box.Min.Z + box.Max.Z) / 2;
            }
            centreAsked = new XYZ(at[0] / 304.8, at[1] / 304.8, z);
        }
    }

    // The part of a difference that lies in the sheet's plane - its depth in the
    // host is Revit's (face and cover), not the point's.
    Func<XYZ, XYZ> inPlane = v => v.Subtract(normal.Multiply(v.DotProduct(normal)));

    if (problem == null)
    {
        foreach (var other in new FilteredElementCollector(doc).OfClass(typeof(FabricSheet)).Cast<FabricSheet>())
        {
            if (other.HostId != host.Id || other.FabricAreaOwnerId != ElementId.InvalidElementId) continue;
            if (other.GetTypeId() != sheetType.Id) continue;
            var c = centreOf(other);
            if (c == null || inPlane(c.Subtract(centreAsked)).GetLength() > 10 / 304.8) continue;
            alreadyThere.Add(name + ": already holds fabric sheet id " + other.Id + " of '" + sheetType.Name + "' centred at "
                + mmPoint(c) + " mm - a second would double it, so it was left as it was");
            problem = "";
            break;
        }
    }
    if (problem == null)
    {
        var areas = RebarHostData.GetRebarHostData(host).GetFabricAreasInHost().Count;
        if (areas > 0)
            findings.Add(name + " also holds " + areas + " fabric area(s); this sheet is in addition to them.");
    }

    if (problem == null)
    {
        var placement = Transform.CreateTranslation(centreAsked);
        placement.BasisX = major;
        placement.BasisY = minor;
        placement.BasisZ = normal;

        FabricSheet made = null;
        XYZ centre = null;
        string failure = null;
        var within = true;
        try
        {
            made = FabricSheet.Create(doc, host, sheetType.Id);
            if (made == null) throw new InvalidOperationException("Revit made no fabric sheet");
            made.PlaceInHost(host, placement);
            if (wantTop.HasValue) made.FabricLocation = wantTop.Value ? FabricLocation.TopOrExternal : FabricLocation.BottomOrInternal;
            doc.Regenerate();
            centre = centreOf(made);
            if (centre != null)
            {
                var shift = inPlane(centreAsked.Subtract(centre));
                if (shift.GetLength() > 1 / 304.8)
                {
                    placement.Origin = placement.Origin.Add(shift);
                    made.PlaceInHost(host, placement);
                    if (wantTop.HasValue) made.FabricLocation = wantTop.Value ? FabricLocation.TopOrExternal : FabricLocation.BottomOrInternal;
                    doc.Regenerate();
                    centre = centreOf(made);
                }
            }
            within = made.IsSingleFabricSheetWithinHost(host, placement);
        }
        catch (Exception ex)
        {
            failure = ex.Message;
        }
        if (failure == null && !within)
            failure = "Revit says a sheet of '" + sheetType.Name + "' (" + mm(sheetType.OverallLength) + " x " + mm(sheetType.OverallWidth)
                + " mm) centred there does not lie within it - move the point further in";
        if (failure != null)
        {
            if (made != null && made.IsValidObject)
            {
                try { doc.Delete(made.Id); }
                catch (Exception) { }
            }
            skipped.Add(name + ": " + failure + " - nothing was left in it");
        }
        else
        {
            // ---- read back ----------------------------------------------------------------------
            var onTop = made.FabricLocation == FabricLocation.TopOrExternal;
            var faceNow = onTop ? (isWall ? "exterior" : "top") : (isWall ? "interior" : "bottom");
            if (made.HostId != host.Id) readBackWrong.Add(name + ": the sheet reads back in another host");
            if (made.GetTypeId() != sheetType.Id) readBackWrong.Add(name + ": the sheet reads back another sheet type");
            if (wantTop.HasValue && onTop != wantTop.Value) readBackWrong.Add(name + ": the sheet reads back on the " + faceNow + " face");
            var centreText = centre == null ? "not read" : mmPoint(centre) + " mm";
            if (centre == null) readBackWrong.Add(name + ": the sheet's centre could not be read back");
            else
            {
                var miss = inPlane(centre.Subtract(centreAsked)).GetLength();
                if (miss > 5 / 304.8) readBackWrong.Add(name + ": the sheet's centre reads back " + mm(miss) + " mm from the point asked");
            }
            var sitsAt = made.GetSheetLocation();
            var axis = sitsAt == null ? null : inPlane(sitsAt.BasisX);
            var directionNow = "not read";
            if (axis == null || axis.IsZeroLength()) readBackWrong.Add(name + ": the sheet's direction could not be read back");
            else
            {
                axis = axis.Normalize();
                var turned = Math.Atan2(axis.DotProduct(minor), axis.DotProduct(major)) * 180 / Math.PI;
                var fromAsked = Math.Abs(turned) % 180;
                if (fromAsked > 90) fromAsked = 180 - fromAsked;
                directionNow = (angle + turned).ToString("0.#", invariant) + " deg";
                if (fromAsked > 1) readBackWrong.Add(name + ": the sheet's major direction reads back " + turned.ToString("0.#", invariant) + " deg from the one asked");
            }
            placed.Add(made.Id);
            lines.Add(name + ": fabric sheet id " + made.Id + " of '" + sheetType.Name + "' (" + mm(sheetType.OverallLength) + " x "
                + mm(sheetType.OverallWidth) + " mm), centred at " + centreText + ", major direction " + directionNow
                + (isWall ? " from horizontal" : " from the model's x axis") + ", " + faceNow + " face");
        }
    }
    else if (problem.Length > 0) skipped.Add(name + ": " + problem + " - nothing placed in it");
}

var report = string.Join(" | ", lines);
if (refused.Length > 0)
{
    findings.Add(refused + ".");
    findings.AddRange(skipped);
}
else
{
    findings.Insert(0, placed.Count + " fabric sheet(s) placed and read back; " + alreadyThere.Count + " already there; "
        + skipped.Count + " skipped" + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " read back other than asked" : "") + ".");
    findings.AddRange(alreadyThere);
    findings.AddRange(skipped);
    findings.AddRange(readBackWrong);
}
