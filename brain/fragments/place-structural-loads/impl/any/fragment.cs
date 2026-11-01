// NOT STANDALONE. Assumes `doc`, `elements`, `kind`, `loadCase`, `value`,
// `direction`, `at` and `points` are in scope, and leaves `placed`,
// `placedIds`, `report`, `noAnalytical`, `skipped`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Analyze > Loads: places point, line or area loads of ONE
// value, in ONE load case the project already has. HOSTED - one load on the
// analytical element of each beam, brace, column, wall or floor handed in: a
// point load at the member's start or end, a line load along the whole member,
// an area load over the whole panel. FREE - at points given in millimetres,
// on no element - only where Revit's API makes one (2020 to 2023).
//
// NEVER CHANGES OR DELETES A LOAD. Each load placed is new; one that would
// repeat a load already on that host, in that case, of that kind and force, is
// SKIPPED and named - running twice must not double a load.
//
// NEVER GUESSED (D-33). The load case is named, exactly - left out, or not
// found, refused with every case listed; one differing only in capitals is
// named, never taken. A point load on a member names its end (start or end);
// left out, refused with both ends' coordinates. The value is positive; the
// direction says which way - left out it is DOWN, project -Z, gravity, and
// the answer says so. Moments are not placed.
//
// HOSTED ONLY ON THE ANALYTICAL ELEMENT OF THE ELEMENT NAMED. 2020 to 2022 that
// is Element.GetAnalyticalModel() - an AnalyticalModelStick for point and line
// loads, an AnalyticalModelSurface for area loads; from 2023,
// AnalyticalToPhysicalAssociationManager gives the AnalyticalMember or
// AnalyticalPanel. An element with none is named in noAnalytical and gets no
// load - never a free load in its place.
//
// UNITS. kN, kN/m and kPa (kN/m2) go to Revit's internal units through
// UnitUtils.ConvertToInternalUnits - DisplayUnitType in 2020 (#if REVIT2020),
// UnitTypeId from 2021 - and every load placed is read back the same way.
// Points are millimetres / 304.8 (D-20).
//
// RELEASES (tools/api-surface --members, 2026-11-01).
//   2020-2022  PointLoad.Create(Document, AnalyticalModelStick, AnalyticalElementSelector, XYZ, XYZ, PointLoadType)
//              LineLoad.Create(Document, AnalyticalModelStick, XYZ, XYZ, LineLoadType)
//              AreaLoad.Create(Document, AnalyticalModelSurface, XYZ, AreaLoadType)
//   2023-2027  PointLoad.Create(Document, ElementId, AnalyticalElementSelector, XYZ, XYZ, PointLoadType)
//              LineLoad.Create(Document, ElementId, XYZ, XYZ, LineLoadType)
//              AreaLoad.Create(Document, ElementId, XYZ, AreaLoadType)
//   2020-2023  free: PointLoad.Create(Document, XYZ, XYZ, XYZ, PointLoadType, SketchPlane),
//              LineLoad.Create(Document, XYZ, XYZ, XYZ, XYZ, LineLoadType, SketchPlane),
//              AreaLoad.Create(Document, IList<CurveLoop>, XYZ, AreaLoadType).
//              GONE at 2024: from 2024 every Create takes a host, and RevitAPI.xml
//              documents no way to leave it unhosted - so a free load is refused.
//   2020-2027  LoadBase.LoadCaseId (get; set), OrientTo, IsOrientToPermitted,
//              HostElementId; LoadCase; LoadOrientTo.Project.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var placed = 0;
var placedIds = new List<ElementId>();
var report = "";
var noAnalytical = new List<ElementId>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> num = v => Math.Round(v, 3).ToString("0.###", invariant);
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<XYZ, string> pt = p => p == null ? "?" : "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
var lines = new List<string>();

// ---- What kind -------------------------------------------------------------
var k = (kind ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("load", "");
var isPoint = k == "point";
var isLine = k == "line" || k == "linear";
var isArea = k == "area";
var unitWord = isPoint ? "kN" : isLine ? "kN/m" : "kPa";
var ok = true;
if (!isPoint && !isLine && !isArea)
{
    refused.Add(quoted(kind) + " is not a kind of load here - point, line or area. Nothing was placed.");
    ok = false;
}

// ---- Units, the release's own way -----------------------------------------
Func<double, double> toInternal = v => v;
Func<double, double> fromInternal = v => v;
#if REVIT2020
var unit = isPoint ? DisplayUnitType.DUT_KILONEWTONS : isLine ? DisplayUnitType.DUT_KILONEWTONS_PER_METER
    : DisplayUnitType.DUT_KILONEWTONS_PER_SQUARE_METER;
toInternal = v => UnitUtils.ConvertToInternalUnits(v, unit);
fromInternal = v => UnitUtils.ConvertFromInternalUnits(v, unit);
#else
var unit = isPoint ? UnitTypeId.Kilonewtons : isLine ? UnitTypeId.KilonewtonsPerMeter
    : UnitTypeId.KilonewtonsPerSquareMeter;
toInternal = v => UnitUtils.ConvertToInternalUnits(v, unit);
fromInternal = v => UnitUtils.ConvertFromInternalUnits(v, unit);
#endif
Func<XYZ, string> force = v => v == null ? "?"
    : "(" + num(fromInternal(v.X)) + ", " + num(fromInternal(v.Y)) + ", " + num(fromInternal(v.Z)) + ") " + unitWord;

// ---- The value and the direction ------------------------------------------
if (ok && !(value > 0))
{
    refused.Add("The load value must be more than 0 " + unitWord + " - its direction says which way. Got "
        + num(value) + ". Nothing was placed.");
    ok = false;
}
var d = (direction ?? "").Trim().ToLowerInvariant().Replace(" ", "");
XYZ axis = null;
if (d.Length == 0 || d == "down" || d == "-z" || d == "gravity") axis = new XYZ(0, 0, -1);
else if (d == "up" || d == "z" || d == "+z") axis = new XYZ(0, 0, 1);
else if (d == "x" || d == "+x") axis = new XYZ(1, 0, 0);
else if (d == "-x") axis = new XYZ(-1, 0, 0);
else if (d == "y" || d == "+y") axis = new XYZ(0, 1, 0);
else if (d == "-y") axis = new XYZ(0, -1, 0);
if (ok && axis == null)
{
    refused.Add(quoted(direction) + " is not a direction here - down, up, x, -x, y or -y, along the project's axes. "
        + "Nothing was placed.");
    ok = false;
}
if (ok && d.Length == 0)
    findings.Add("No direction was given, so each load points DOWN - project -Z, gravity.");
var forceVector = ok ? axis.Multiply(toInternal(value)) : XYZ.Zero;

// ---- The load case ---------------------------------------------------------
var cases = new FilteredElementCollector(doc).OfClass(typeof(LoadCase)).Cast<LoadCase>()
    .OrderBy(c => c.Number).ThenBy(c => c.Name).ToList();
var caseList = cases.Count == 0 ? "none - CREATE_LOAD_CASE makes one" : string.Join(", ", cases.Select(c => quoted(c.Name)));
var wantCase = (loadCase ?? "").Trim();
LoadCase theCase = null;
if (ok)
{
    if (wantCase.Length == 0)
    {
        refused.Add("No load case was named, and it is never guessed. The load cases in this model: " + caseList + ".");
        ok = false;
    }
    else
    {
        theCase = cases.FirstOrDefault(c => c.Name == wantCase);
        if (theCase == null)
        {
            var near = cases.FirstOrDefault(c => string.Equals(c.Name, wantCase, StringComparison.OrdinalIgnoreCase));
            refused.Add(near != null
                ? "No load case is called exactly " + quoted(wantCase) + "; " + quoted(near.Name)
                  + " differs only in capitals and was NOT taken."
                : quoted(wantCase) + " is not a load case in this model: " + caseList + ".");
            ok = false;
        }
    }
}

// ---- Where: elements, or free points ---------------------------------------
var hosts = (elements ?? new List<Element>()).Where(e => e != null).ToList();
var freePoints = new List<XYZ>();
var pointsText = (points ?? "").Trim();
if (ok && pointsText.Length > 0)
{
    foreach (var raw in pointsText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = raw.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        double x, y, z;
        if (parts.Length == 3
            && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, invariant, out x)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, invariant, out y)
            && double.TryParse(parts[2], System.Globalization.NumberStyles.Float, invariant, out z))
            freePoints.Add(new XYZ(x / 304.8, y / 304.8, z / 304.8));
        else
        {
            refused.Add(quoted(raw.Trim()) + " is not a point - x, y, z in millimetres, points separated by ;. "
                + "Nothing was placed.");
            ok = false;
            break;
        }
    }
}
if (ok && hosts.Count == 0 && freePoints.Count == 0)
{
    refused.Add("Nothing to place the load on: select the beams, columns, walls or floors, or give points in "
        + "millimetres for a free load.");
    ok = false;
}
if (ok && hosts.Count > 0 && freePoints.Count > 0)
    findings.Add("Elements were handed in, so the loads go on them; the points given were not used.");

// The end of a member a point load goes on.
var e0 = (at ?? "").Trim().ToLowerInvariant();
AnalyticalElementSelector? end = null;
if (e0 == "start" || e0 == "base" || e0 == "bottom") end = AnalyticalElementSelector.StartOrBase;
else if (e0 == "end" || e0 == "top") end = AnalyticalElementSelector.EndOrTop;
if (ok && isPoint && hosts.Count > 0 && end == null && e0.Length > 0)
{
    refused.Add(quoted(at) + " is not an end of a member - start (or base) or end (or top). Nothing was placed.");
    ok = false;
}

// ---- Analytical element of each element handed in -------------------------
#if REVIT2020 || REVIT2021 || REVIT2022
Func<Element, AnalyticalModel> analyticalOf = e =>
{
    if (e is AnalyticalModel) return (AnalyticalModel)e;
    try { return e.GetAnalyticalModel(); } catch { return null; }
};
Func<AnalyticalModel, Curve> curveOf = am => { try { return am.GetCurve(); } catch { return null; } };
#else
AnalyticalToPhysicalAssociationManager manager = null;
try { manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc); } catch { }
Func<Element, AnalyticalElement> analyticalOf = e =>
{
    if (e is AnalyticalElement) return (AnalyticalElement)e;
    if (manager == null) return null;
    try
    {
        var aid = manager.GetAssociatedElementId(e.Id);
        return aid == null || aid == ElementId.InvalidElementId ? null : doc.GetElement(aid) as AnalyticalElement;
    }
    catch { return null; }
};
Func<AnalyticalElement, Curve> curveOf = am => { try { return am.GetCurve(); } catch { return null; } };
#endif

Func<Element, string> describe = e => (e.Category == null ? "" : e.Category.Name + " ") + quoted(e.Name) + " (" + e.Id + ")";

// A point load on a member with no end named: refused with both ends shown.
if (ok && isPoint && hosts.Count > 0 && end == null)
{
    var ends = new List<string>();
    foreach (var e in hosts)
    {
        var am = analyticalOf(e);
        var c = am == null ? null : curveOf(am);
        if (c != null) ends.Add(describe(e) + " starts at " + pt(c.GetEndPoint(0)) + " and ends at " + pt(c.GetEndPoint(1)));
    }
    refused.Add("A point load on a member needs its end - start (base) or end (top) - and it is never guessed. "
        + (ends.Count > 0 ? string.Join("; ", ends) + "." : ""));
    ok = false;
}

// ---- Loads already there, so none is doubled -------------------------------
var existing = new List<LoadBase>();
if (ok)
{
    var cls = isPoint ? typeof(PointLoad) : isLine ? typeof(LineLoad) : typeof(AreaLoad);
    existing = new FilteredElementCollector(doc).OfClass(cls).Cast<LoadBase>()
        .Where(l => l.LoadCaseId == theCase.Id).ToList();
}
Func<LoadBase, XYZ> firstForce = l => l is PointLoad ? ((PointLoad)l).ForceVector
    : l is LineLoad ? ((LineLoad)l).ForceVector1 : ((AreaLoad)l).ForceVector1;
Func<ElementId, XYZ, LoadBase> repeatOf = (hostId, where) => existing.FirstOrDefault(l =>
{
    if (l.HostElementId != hostId) return false;
    var f = firstForce(l);
    if (f == null || !f.IsAlmostEqualTo(forceVector, 1e-6)) return false;
    if (where != null && l is PointLoad) return ((PointLoad)l).Point.IsAlmostEqualTo(where, 1.0 / 304.8);
    return true;
});

// ---- After a load is made: its case, its orientation, read back -----------
Action<LoadBase, string> keep = (load, onWhat) =>
{
    try { load.LoadCaseId = theCase.Id; }
    catch (Exception ex) { findings.Add("Load " + load.Id + " was placed but Revit would not put it in " + quoted(theCase.Name) + ": " + ex.Message); }
    try { if (load.IsOrientToPermitted(LoadOrientTo.Project)) load.OrientTo = LoadOrientTo.Project; } catch { }
    placed++;
    placedIds.Add(load.Id);
    string where;
    if (load is PointLoad) where = "Point load at " + pt(((PointLoad)load).Point) + ": " + force(((PointLoad)load).ForceVector);
    else if (load is LineLoad)
    {
        var l = (LineLoad)load;
        where = "Line load " + pt(l.StartPoint) + " to " + pt(l.EndPoint) + ": "
            + (l.IsUniform ? "uniform " + force(l.ForceVector1) : force(l.ForceVector1) + " to " + force(l.ForceVector2));
    }
    else where = "Area load: " + force(((AreaLoad)load).ForceVector1);
    var caseName = "";
    try { caseName = load.LoadCaseName; } catch { }
    lines.Add(load.Id + "  " + where + "; load case " + quoted(caseName) + "; " + onWhat + "; oriented to "
        + load.OrientTo + " - read back from the model");
    if (load.LoadCaseId != theCase.Id)
        findings.Add("Read back, load " + load.Id + " is not in " + quoted(theCase.Name) + " - look at its Properties.");
};

// ---- Hosted ------------------------------------------------------------------
if (ok && hosts.Count > 0)
{
    foreach (var e in hosts)
    {
        var am = analyticalOf(e);
        if (am == null)
        {
            noAnalytical.Add(e.Id);
            continue;
        }
        var onWhat = "on " + describe(e) + " through analytical " + am.Id;
        XYZ endPoint = null;
        if (isPoint && end != null)
        {
            var c = curveOf(am);
            if (c != null) endPoint = c.GetEndPoint(end == AnalyticalElementSelector.StartOrBase ? 0 : 1);
        }
        var again = repeatOf(am.Id, endPoint);
        if (again != null)
        {
            skipped.Add(describe(e) + " - load " + again.Id + " in " + quoted(theCase.Name) + " already carries "
                + force(firstForce(again)) + (endPoint != null ? " at that end" : "") + "; not placed twice.");
            continue;
        }
        LoadBase made = null;
        try
        {
#if REVIT2020 || REVIT2021 || REVIT2022
            var stick = am as AnalyticalModelStick;
            var surface = am as AnalyticalModelSurface;
            if ((isPoint || isLine) && stick == null)
                skipped.Add(describe(e) + " - its analytical element is not a beam, brace or column, so a "
                    + (isPoint ? "point" : "line") + " load cannot go along it.");
            else if (isArea && surface == null)
                skipped.Add(describe(e) + " - its analytical element is not a floor, wall or foundation panel, so an "
                    + "area load cannot go on it.");
            else if (isPoint)
                made = PointLoad.Create(doc, stick, end.Value, forceVector, XYZ.Zero, (PointLoadType)null);
            else if (isLine)
                made = LineLoad.Create(doc, stick, forceVector, XYZ.Zero, (LineLoadType)null);
            else
                made = AreaLoad.Create(doc, surface, forceVector, (AreaLoadType)null);
#else
            if ((isPoint || isLine) && !(am is AnalyticalMember))
                skipped.Add(describe(e) + " - its analytical element is not an analytical member (beam, brace or "
                    + "column), so a " + (isPoint ? "point" : "line") + " load cannot go along it.");
            else if (isArea && !(am is AnalyticalPanel))
                skipped.Add(describe(e) + " - its analytical element is not an analytical panel (floor, wall or "
                    + "foundation), so an area load cannot go on it.");
            else if (isPoint)
                made = PointLoad.Create(doc, am.Id, end.Value, forceVector, XYZ.Zero, (PointLoadType)null);
            else if (isLine)
                made = LineLoad.Create(doc, am.Id, forceVector, XYZ.Zero, (LineLoadType)null);
            else
                made = AreaLoad.Create(doc, am.Id, forceVector, (AreaLoadType)null);
#endif
        }
        catch (Exception ex)
        {
            skipped.Add(describe(e) + " - Revit would not place the load on it: " + ex.Message);
        }
        if (made != null) keep(made, onWhat);
    }
    if (noAnalytical.Count > 0)
        findings.Add(noAnalytical.Count + " element(s) have no analytical element, so no load was placed on them - "
            + "never a free load in their place: " + string.Join(", ", noAnalytical.Select(id => describe(doc.GetElement(id)))) + ".");
}

// ---- Free --------------------------------------------------------------------
else if (ok && freePoints.Count > 0)
{
#if REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
    try
    {
        if (isPoint)
        {
            foreach (var p in freePoints)
            {
                var again = repeatOf(ElementId.InvalidElementId, p);
                if (again != null) { skipped.Add("the point " + pt(p) + " - load " + again.Id + " is already there; not placed twice."); continue; }
                keep(PointLoad.Create(doc, p, forceVector, XYZ.Zero, (PointLoadType)null, (SketchPlane)null), "free - on no element");
            }
        }
        else if (isLine)
        {
            if (freePoints.Count != 2)
                refused.Add("A free line load needs exactly two points, its start and end; " + freePoints.Count + " were given.");
            else
                keep(LineLoad.Create(doc, freePoints[0], freePoints[1], forceVector, XYZ.Zero, (LineLoadType)null, (SketchPlane)null),
                    "free - on no element");
        }
        else
        {
            if (freePoints.Count < 3)
                refused.Add("A free area load needs at least three corner points; " + freePoints.Count + " were given.");
            else
            {
                var loop = new CurveLoop();
                for (var i = 0; i < freePoints.Count; i++)
                    loop.Append(Line.CreateBound(freePoints[i], freePoints[(i + 1) % freePoints.Count]));
                keep(AreaLoad.Create(doc, new List<CurveLoop> { loop }, forceVector, (AreaLoadType)null), "free - on no element");
            }
        }
    }
    catch (Exception ex)
    {
        refused.Add("Revit would not place the free load: " + ex.Message);
    }
#else
    refused.Add("A free load - on no element - cannot be placed on this Revit: from 2024 Revit's API makes loads only "
        + "on an analytical element. Select the beams, columns, walls or floors instead. Nothing was placed.");
#endif
}

report = string.Join("\n", lines);
if (placed == 0) findings.Add("No load was placed - the model's loads are as they were.");
