// NOT STANDALONE. Assumes `doc`, `elements`, `kind`, `fixity` and `at` are in
// scope, and leaves `placed`, `placedIds`, `report`, `noAnalytical`,
// `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Analyze > Boundary Conditions: places ONE boundary condition
// on the analytical element of each element handed in - a POINT at a beam's,
// brace's or column's start (base) or end (top), a LINE along a beam or brace,
// or an AREA over a floor, wall or foundation - Fixed, Pinned or Roller.
//
//   point  Fixed  every translation and rotation fixed
//          Pinned translations fixed, rotations released
//          Roller Z translation fixed, everything else released
//   line   the same, over X, Y, Z and rotation about X
//   area   Fixed and Pinned fix X, Y and Z (an area support has no rotation);
//          Roller fixes Z only
//
// Whether Revit's own preset reads the same afterwards is read back from its
// BOUNDARY_PARAM_PRESET parameters and reported - not seen in Revit.
//
// NEVER CHANGES OR DELETES A BOUNDARY CONDITION. One already on that host, of
// that kind (and, for a point, at that end), is SKIPPED and named.
//
// NEVER GUESSED (D-33). The fixity is named - left out, refused. A point on a
// member names its end - left out, refused with both ends' coordinates.
//
// HOSTED ONLY ON THE ANALYTICAL ELEMENT OF THE ELEMENT NAMED. An element with
// none is named in noAnalytical and gets nothing.
//
// RELEASES - WHY THE CARD CLAIMS 2020-2022 AND 2027 ONLY (tools/api-surface
// --members and RevitAPI.xml, 2026-11-01).
//   2020-2022  Document.Create.NewPointBoundaryConditions(Reference, ...) and
//              NewLineBoundaryConditions(Reference, ...), the reference from
//              AnalyticalModel.GetReference(AnalyticalModelSelector) - "a
//              reference to a Beam's, Brace's or Column's analytical line end";
//              NewAreaBoundaryConditions(Element, ...) on "a Wall, Slab or Slab
//              Foundation".
//   2027       BoundaryConditions.CreatePointBoundaryConditions(Document,
//              ElementId, XYZ, ...), CreateLineBoundaryConditions(Document,
//              ElementId, Curve, ...) and CreateAreaBoundaryConditions(Document,
//              ElementId, IList<CurveLoop>, ...) on the AnalyticalElement; the
//              Document.Create ones are obsolete there.
//   2023-2026  only the Document.Create methods, whose documentation still asks
//              for an analytical line end's reference - and AnalyticalModel,
//              which gave it, is gone. No documented member gives an
//              AnalyticalMember's end as a Reference, so nothing is claimed
//              until a run in Revit shows one (the #else refuses).
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
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<XYZ, string> pt = p => p == null ? "?" : "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> describe = e => e == null ? "?" : (e.Category == null ? "" : e.Category.Name + " ") + quoted(e.Name) + " (" + e.Id + ")";
var lines = new List<string>();
var ok = true;

// ---- Kind, fixity, end ------------------------------------------------------
var k = (kind ?? "").Trim().ToLowerInvariant().Replace(" ", "");
var isPoint = k == "point";
var isLine = k == "line" || k == "linear";
var isArea = k == "area";
if (!isPoint && !isLine && !isArea)
{
    refused.Add(quoted(kind) + " is not a kind of boundary condition - point, line or area. Nothing was placed.");
    ok = false;
}
var f = (fixity ?? "").Trim().ToLowerInvariant();
var isFixed = f == "fixed" || f == "fix";
var isPinned = f == "pinned" || f == "pin";
var isRoller = f == "roller";
if (ok && !isFixed && !isPinned && !isRoller)
{
    refused.Add((f.Length == 0 ? "No fixity was given" : quoted(fixity) + " is not a fixity here")
        + " - Fixed, Pinned or Roller; it is never guessed. Nothing was placed.");
    ok = false;
}
var tXY = isRoller ? TranslationRotationValue.Release : TranslationRotationValue.Fixed;
var tZ = TranslationRotationValue.Fixed;
var rot = isFixed ? TranslationRotationValue.Fixed : TranslationRotationValue.Release;

var e0 = (at ?? "").Trim().ToLowerInvariant();
int? endIndex = null;
if (e0 == "start" || e0 == "base" || e0 == "bottom") endIndex = 0;
else if (e0 == "end" || e0 == "top") endIndex = 1;
if (ok && isPoint && endIndex == null && e0.Length > 0)
{
    refused.Add(quoted(at) + " is not an end of a member - start (or base) or end (or top). Nothing was placed.");
    ok = false;
}

var hosts = (elements ?? new List<Element>()).Where(e => e != null).ToList();
if (ok && hosts.Count == 0)
{
    refused.Add("No element was handed in - select the columns, beams, walls or floors to support.");
    ok = false;
}

#if REVIT2020 || REVIT2021 || REVIT2022 || REVIT2027_OR_GREATER

// ---- Analytical element of each element handed in -------------------------
#if REVIT2020 || REVIT2021 || REVIT2022
Func<Element, Element> analyticalOf = e =>
{
    if (e is AnalyticalModel) return e;
    try { return e.GetAnalyticalModel(); } catch { return null; }
};
Func<Element, bool> isMember = a => a is AnalyticalModelStick;
Func<Element, bool> isPanel = a => a is AnalyticalModelSurface;
Func<Element, Curve> curveOf = a => { try { return ((AnalyticalModel)a).GetCurve(); } catch { return null; } };
#else
AnalyticalToPhysicalAssociationManager manager = null;
try { manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc); } catch { }
Func<Element, Element> analyticalOf = e =>
{
    if (e is AnalyticalElement) return e;
    if (manager == null) return null;
    try
    {
        var aid = manager.GetAssociatedElementId(e.Id);
        return aid == null || aid == ElementId.InvalidElementId ? null : doc.GetElement(aid) as AnalyticalElement;
    }
    catch { return null; }
};
Func<Element, bool> isMember = a => a is AnalyticalMember;
Func<Element, bool> isPanel = a => a is AnalyticalPanel;
Func<Element, Curve> curveOf = a => { try { return ((AnalyticalElement)a).GetCurve(); } catch { return null; } };
#endif

// A point on a member with no end named: refused with both ends shown.
if (ok && isPoint && endIndex == null)
{
    var ends = new List<string>();
    foreach (var e in hosts)
    {
        var a = analyticalOf(e);
        var c = a == null ? null : curveOf(a);
        if (c != null) ends.Add(describe(e) + " starts at " + pt(c.GetEndPoint(0)) + " and ends at " + pt(c.GetEndPoint(1)));
    }
    refused.Add("A point boundary condition on a member needs its end - start (base) or end (top) - and it is never "
        + "guessed. " + (ends.Count > 0 ? string.Join("; ", ends) + "." : ""));
    ok = false;
}

// ---- Boundary conditions already there --------------------------------------
var wantType = isPoint ? BoundaryConditionsType.Point : isLine ? BoundaryConditionsType.Line : BoundaryConditionsType.Area;
var existing = ok
    ? new FilteredElementCollector(doc).OfClass(typeof(BoundaryConditions)).Cast<BoundaryConditions>()
        .Where(b => { try { return b.GetBoundaryConditionsType() == wantType; } catch { return false; } }).ToList()
    : new List<BoundaryConditions>();

Func<BoundaryConditions, string> readBack = b =>
{
    var presetParam = isArea ? BuiltInParameter.BOUNDARY_PARAM_PRESET_AREA
        : isLine ? BuiltInParameter.BOUNDARY_PARAM_PRESET_LINEAR : BuiltInParameter.BOUNDARY_PARAM_PRESET;
    string preset = null;
    try { var p = b.get_Parameter(presetParam); preset = p == null ? null : p.AsValueString(); } catch { }
    var where = "";
    try
    {
        if (isPoint) where = " at " + pt(b.Point);
        else if (isLine) { var c = b.GetCurve(); if (c != null) where = " from " + pt(c.GetEndPoint(0)) + " to " + pt(c.GetEndPoint(1)); }
    }
    catch { }
    return wantType + where + ", Revit shows it as " + (string.IsNullOrEmpty(preset) ? "?" : preset);
};

// ---- Place ---------------------------------------------------------------------
if (ok)
{
    foreach (var e in hosts)
    {
        var a = analyticalOf(e);
        if (a == null) { noAnalytical.Add(e.Id); continue; }
        if ((isPoint || isLine) && !isMember(a))
        {
            skipped.Add(describe(e) + " - its analytical element is not a beam, brace or column, so a "
                + (isPoint ? "point at its end" : "line along it") + " cannot go there.");
            continue;
        }
        if (isArea && !isPanel(a))
        {
            skipped.Add(describe(e) + " - its analytical element is not a floor, wall or foundation panel, so an area "
                + "boundary condition cannot go on it.");
            continue;
        }
        var curve = curveOf(a);
        XYZ endPoint = isPoint && curve != null ? curve.GetEndPoint(endIndex.Value) : null;
        var hostIds = new[] { a.Id, e.Id };
        var again = existing.FirstOrDefault(b => hostIds.Contains(b.HostElementId)
            && (!isPoint || endPoint == null || (b.Point != null && b.Point.IsAlmostEqualTo(endPoint, 1.0 / 304.8))));
        if (again != null)
        {
            skipped.Add(describe(e) + " - boundary condition " + again.Id + " (" + readBack(again) + ") is already there; "
                + "left as it is, not placed twice.");
            continue;
        }
        BoundaryConditions made = null;
        try
        {
#if REVIT2020 || REVIT2021 || REVIT2022
            var am = (AnalyticalModel)a;
            if (isPoint)
            {
                var r = am.GetReference(new AnalyticalModelSelector(endIndex == 0 ? AnalyticalCurveSelector.StartPoint : AnalyticalCurveSelector.EndPoint));
                if (r == null) skipped.Add(describe(e) + " - Revit gave no reference to that end; nothing placed.");
                else made = doc.Create.NewPointBoundaryConditions(r, tXY, 0, tXY, 0, tZ, 0, rot, 0, rot, 0, rot, 0);
            }
            else if (isLine)
            {
                var r = am.GetReference(new AnalyticalModelSelector(AnalyticalCurveSelector.WholeCurve));
                if (r == null) skipped.Add(describe(e) + " - Revit gave no reference to its analytical line; nothing placed.");
                else made = doc.Create.NewLineBoundaryConditions(r, tXY, 0, tXY, 0, tZ, 0, rot, 0);
            }
            else
                made = doc.Create.NewAreaBoundaryConditions(e is AnalyticalModel ? doc.GetElement(am.GetElementId()) : e,
                    tXY, 0, tXY, 0, tZ, 0);
#else
            if (isPoint)
            {
                if (endPoint == null) skipped.Add(describe(e) + " - its analytical member has no line to find the end of.");
                else made = BoundaryConditions.CreatePointBoundaryConditions(doc, a.Id, endPoint, tXY, 0, tXY, 0, tZ, 0, rot, 0, rot, 0, rot, 0);
            }
            else if (isLine)
            {
                if (curve == null) skipped.Add(describe(e) + " - its analytical member has no line.");
                else made = BoundaryConditions.CreateLineBoundaryConditions(doc, a.Id, curve, tXY, 0, tXY, 0, tZ, 0, rot, 0);
            }
            else
            {
                CurveLoop outline = null;
                try { outline = ((AnalyticalSurfaceBase)a).GetOuterContour(); } catch { }
                if (outline == null) skipped.Add(describe(e) + " - its analytical panel has no outline.");
                else made = BoundaryConditions.CreateAreaBoundaryConditions(doc, a.Id, new List<CurveLoop> { outline }, tXY, 0, tXY, 0, tZ, 0);
            }
#endif
        }
        catch (Exception ex)
        {
            skipped.Add(describe(e) + " - Revit would not place the boundary condition: " + ex.Message);
        }
        if (made != null)
        {
            placed++;
            placedIds.Add(made.Id);
            lines.Add(made.Id + "  " + readBack(made) + "; asked " + (isFixed ? "Fixed" : isPinned ? "Pinned" : "Roller")
                + "; on " + describe(e) + " through analytical " + a.Id + " - read back from the model");
        }
    }
    if (noAnalytical.Count > 0)
        findings.Add(noAnalytical.Count + " element(s) have no analytical element, so no boundary condition was placed "
            + "on them: " + string.Join(", ", noAnalytical.Select(id => describe(doc.GetElement(id)))) + ".");
    if (isArea && isPinned)
        findings.Add("An area boundary condition has no rotations, so Pinned fixes X, Y and Z - the same as Fixed.");
}

#else
if (ok)
{
    refused.Add("Boundary conditions cannot be placed by Heron on this Revit (2023 to 2026): its API documents no way "
        + "to reach an analytical member's end or line for them since the analytical model became separate elements. "
        + "Place them with Analyze > Boundary Conditions. Nothing was placed.");
    ok = false;
}
#endif

report = string.Join("\n", lines);
if (placed == 0) findings.Add("No boundary condition was placed - the model's supports are as they were.");
