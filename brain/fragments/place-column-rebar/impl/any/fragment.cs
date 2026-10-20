// NOT STANDALONE. Assumes `doc`, `elements`, `mainBars`, `barsPerFace`, `ties`,
// `tieHook`, `cover` and `extendTop` are in scope; leaves `placed`,
// `columnsReinforced`, `skipped`, `readBackWrong`, `report`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// EVERY VALUE IS THE MODELLER'S (D-33), every length millimetres (D-71). The
// bars are placed from the column's OWN solid, read in its own axes, never
// from a family parameter whose name might be b, h, Width or anything else.
//
// TWO CREATION ROUTES. Rebar.CreateFromCurves taking hook types and
// RebarHookOrientation is 2020-2026 and gone at 2027; 2026 on takes a
// BarTerminationsData. `makeBar` is the one place that knows.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);
Func<RebarBarType, double> nominalOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarNominalDiameter;
#endif
};
Func<RebarBarType, double> modelOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarModelDiameter;
#endif
};

var placed = new List<ElementId>();
var columnsReinforced = new List<ElementId>();
var skipped = new List<string>();
var readBackWrong = new List<string>();
var findings = new List<string>();
var refused = "";
var lines = new List<string>();

// ---- what the model has ---------------------------------------------------------
var barTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
var hookTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>().ToList();
var shapeCount = new FilteredElementCollector(doc).OfClass(typeof(RebarShape)).GetElementCount();
Func<string> barList = () => barTypes.Count == 0 ? "none" : string.Join(", ", barTypes.OrderBy(t => nominalOf(t))
    .Select(t => "'" + t.Name + "' " + mm(nominalOf(t)) + " mm"));

// A bar type by its exact name, or by a diameter in millimetres matched to the
// ONE type of that nominal diameter. Null with `refused` set.
Func<string, string, RebarBarType> barTypeFrom = (text, role) =>
{
    var asked = (text ?? "").Trim();
    var named = barTypes.FirstOrDefault(t => string.Equals(t.Name, asked, StringComparison.Ordinal));
    if (named != null) return named;
    var number = asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase) ? asked.Substring(0, asked.Length - 2).Trim() : asked;
    double diameter;
    if (!double.TryParse(number, styleFloat, invariant, out diameter) || diameter <= 0)
    {
        refused = "No Rebar Bar Type is named '" + asked + "' (the " + role + " asked). Loaded: " + barList()
            + ". Give one of these names, or the diameter in millimetres. If '" + asked + "' is a bar mark from the "
            + "drawings, say which type or diameter it means. Nothing was placed";
        return null;
    }
    var matching = barTypes.Where(t => Math.Abs(nominalOf(t) * 304.8 - diameter) < 0.05).ToList();
    if (matching.Count == 1) return matching[0];
    refused = matching.Count > 1
        ? matching.Count + " bar types are " + diameter.ToString("0.#", invariant) + " mm: "
            + string.Join(", ", matching.Select(t => "'" + t.Name + "'")) + ". Say which, by its name. Nothing was placed"
        : "No Rebar Bar Type of " + diameter.ToString("0.#", invariant) + " mm is loaded. Loaded: " + barList()
            + ". CREATE_REBAR_BAR_TYPE makes one from the spec's diameter and bend diameters. Nothing was placed";
    return null;
};
// "<count> x <bar type>" or "none". Count 0 with no type for "none".
Func<string, string, Tuple<int, RebarBarType>> countAndType = (text, role) =>
{
    var asked = (text ?? "").Trim();
    if (asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return Tuple.Create(0, (RebarBarType)null);
    var cut = asked.IndexOfAny(new[] { 'x', 'X', '*' });
    int count;
    if (cut <= 0 || !int.TryParse(asked.Substring(0, cut).Trim(), out count) || count < 1)
    {
        refused = "'" + asked + "' is not a bar count and type. Say it as <count> x <bar type> - 4 x 16mm, or "
            + "4 x T16 when a bar type is named T16 - or none. Nothing was placed";
        return null;
    }
    var type = barTypeFrom(asked.Substring(cut + 1), role);
    return type == null ? null : Tuple.Create(count, type);
};
// "<bar type> @ <spacing>" or "none". Spacing in feet.
Func<string, string, Tuple<RebarBarType, double>> typeAndSpacing = (text, role) =>
{
    var asked = (text ?? "").Trim();
    if (asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return Tuple.Create((RebarBarType)null, 0.0);
    var at = asked.LastIndexOf('@');
    double spacing;
    var spacingText = at < 0 ? "" : asked.Substring(at + 1).Trim();
    if (spacingText.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) spacingText = spacingText.Substring(0, spacingText.Length - 2).Trim();
    if (at <= 0 || !double.TryParse(spacingText, styleFloat, invariant, out spacing) || spacing <= 0)
    {
        refused = "'" + asked + "' is not a bar type and spacing. Say it as <bar type> @ <spacing in mm> - 8mm @ 150 "
            + "- or none. Nothing was placed";
        return null;
    }
    var type = barTypeFrom(asked.Substring(0, at), role);
    return type == null ? null : Tuple.Create(type, spacing / 304.8);
};
// A hook by its exact name, or none. Null with `refused` set when it is not.
Func<string, RebarStyle, string, RebarHookType> hookFrom = (text, style, role) =>
{
    var asked = (text ?? "").Trim();
    if (asked.Length == 0 || asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return null;
    var hook = hookTypes.FirstOrDefault(h => string.Equals(h.Name, asked, StringComparison.Ordinal));
    var fitting = hookTypes.Where(h => h.Style == style).Select(h => "'" + h.Name + "'").ToList();
    var fittingText = fitting.Count == 0 ? "none" : string.Join(", ", fitting);
    if (hook == null)
    {
        refused = "No Rebar Hook Type is named '" + asked + "' (the " + role + " asked). Hooks for these bars: "
            + fittingText + " - or none. Nothing was placed";
        return null;
    }
    if (hook.Style != style)
    {
        refused = "'" + hook.Name + "' is a hook for " + (hook.Style == RebarStyle.StirrupTie ? "stirrups and ties" : "standard bars")
            + ", and the " + role + " needs one for " + (style == RebarStyle.StirrupTie ? "stirrups and ties" : "standard bars")
            + ": " + fittingText + ". Nothing was placed";
        return null;
    }
    return hook;
};
Func<string, string, double> lengthFrom = (text, what) =>
{
    var asked = (text ?? "").Trim();
    if (asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) asked = asked.Substring(0, asked.Length - 2).Trim();
    double value;
    if (!double.TryParse(asked, styleFloat, invariant, out value) || value < 0)
    {
        refused = "'" + (text ?? "") + "' is not a " + what + " in millimetres. Nothing was placed";
        return double.NaN;
    }
    return value / 304.8;
};

// Revit's rule for a hook's side (CreateFromCurves' remarks): stand at the end
// of the bar, the bar behind you, its normal as up - Right is on your right.
Func<XYZ, XYZ, XYZ, bool> onRight = (facing, normal, wanted) => facing.CrossProduct(normal).DotProduct(wanted) > 0;
Func<Curve, bool, XYZ> tangentAt = (curve, atEnd) => curve.ComputeDerivatives(atEnd ? 1.0 : 0.0, true).BasisX.Normalize();

// THE ONE PLACE THAT KNOWS THE TWO ROUTES.
Func<RebarStyle, RebarBarType, RebarHookType, RebarHookType, Element, XYZ, IList<Curve>, bool, bool, Rebar> makeBar =
    (style, type, startHook, endHook, host, normal, curves, startRight, endRight) =>
{
#if REVIT2026_OR_GREATER
    var ends = new BarTerminationsData(doc);
    ends.HookTypeIdAtStart = startHook == null ? ElementId.InvalidElementId : startHook.Id;
    ends.HookTypeIdAtEnd = endHook == null ? ElementId.InvalidElementId : endHook.Id;
    ends.TerminationOrientationAtStart = startRight ? RebarTerminationOrientation.Right : RebarTerminationOrientation.Left;
    ends.TerminationOrientationAtEnd = endRight ? RebarTerminationOrientation.Right : RebarTerminationOrientation.Left;
    return Rebar.CreateFromCurves(doc, style, type, host, normal, curves, ends, true, true);
#else
    return Rebar.CreateFromCurves(doc, style, type, startHook, endHook, host, normal, curves,
        startRight ? RebarHookOrientation.Right : RebarHookOrientation.Left,
        endRight ? RebarHookOrientation.Right : RebarHookOrientation.Left, true, true);
#endif
};

// The extent of an element's solids along a frame's axes:
// [xmin, xmax, ymin, ymax, zmin, zmax, volume], or null with no solid.
Func<Element, Transform, double[]> extentsIn = (element, frame) =>
{
    var inverse = frame.Inverse;
    var box = new[] { double.MaxValue, double.MinValue, double.MaxValue, double.MinValue, double.MaxValue, double.MinValue, 0.0 };
    var any = false;
    Action<GeometryElement> walk = null;
    walk = geometry =>
    {
        if (geometry == null) return;
        foreach (GeometryObject item in geometry)
        {
            var solid = item as Solid;
            if (solid != null && solid.Volume > 1e-9)
            {
                box[6] += solid.Volume;
                foreach (Edge edge in solid.Edges)
                    foreach (var point in edge.Tessellate())
                    {
                        var q = inverse.OfPoint(point);
                        any = true;
                        box[0] = Math.Min(box[0], q.X); box[1] = Math.Max(box[1], q.X);
                        box[2] = Math.Min(box[2], q.Y); box[3] = Math.Max(box[3], q.Y);
                        box[4] = Math.Min(box[4], q.Z); box[5] = Math.Max(box[5], q.Z);
                    }
                continue;
            }
            var nested = item as GeometryInstance;
            if (nested != null) walk(nested.GetInstanceGeometry());
        }
    };
    walk(element.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false }));
    return any ? box : null;
};
// The radius of the largest cylinder about the frame's Z, or 0.
Func<Element, XYZ, double> cylinderRadius = (element, axis) =>
{
    var radius = 0.0;
    Action<GeometryElement> walk = null;
    walk = geometry =>
    {
        if (geometry == null) return;
        foreach (GeometryObject item in geometry)
        {
            var solid = item as Solid;
            if (solid != null && solid.Volume > 1e-9)
            {
                foreach (Face face in solid.Faces)
                {
                    var round = face as CylindricalFace;
                    if (round == null || Math.Abs(Math.Abs(round.Axis.Normalize().DotProduct(axis)) - 1) > 1e-6) continue;
                    radius = Math.Max(radius, round.get_Radius(0).GetLength());
                }
                continue;
            }
            var nested = item as GeometryInstance;
            if (nested != null) walk(nested.GetInstanceGeometry());
        }
    };
    walk(element.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false }));
    return radius;
};
Func<Element, BuiltInParameter, double> hostCover = (host, bip) =>
{
    Parameter p = null;
    try { p = host.get_Parameter(bip); } catch (Exception) { }
    var type = p == null ? null : doc.GetElement(p.AsElementId()) as RebarCoverType;
    return type == null ? double.NaN : type.CoverDistance;
};

// ---- the request, read once, before anything is placed -------------------------------
Tuple<int, RebarBarType> main = null;
Tuple<RebarBarType, double> tie = null;
RebarHookType tieHookType = null;
var coverText = (cover ?? "").Trim();
var useHostCover = coverText.Length == 0 || coverText.Equals("host", StringComparison.OrdinalIgnoreCase);
var coverGiven = 0.0;
var extension = 0.0;
var faceW = 2;
var faceD = 2;
var layoutText = (barsPerFace ?? "").Trim().ToLowerInvariant();

main = countAndType(mainBars, "main bars");
if (refused.Length == 0) tie = typeAndSpacing(ties, "ties");
if (refused.Length == 0 && tie != null && tie.Item1 != null) tieHookType = hookFrom(tieHook, RebarStyle.StirrupTie, "ties");
if (refused.Length == 0 && !useHostCover) coverGiven = lengthFrom(coverText, "cover");
var extendText = (extendTop ?? "").Trim();
if (refused.Length == 0 && extendText.Length > 0 && !extendText.Equals("none", StringComparison.OrdinalIgnoreCase))
    extension = lengthFrom(extendText, "top extension");
if (refused.Length == 0 && main != null && tie != null && main.Item1 == 0 && tie.Item1 == null)
    refused = "Both the main bars and the ties were asked as none, so there is nothing to place. Nothing was placed";
if (refused.Length == 0 && layoutText.Length > 0 && layoutText != "corners")
{
    var parts = layoutText.Split('x');
    if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), out faceW) || !int.TryParse(parts[1].Trim(), out faceD) || faceW < 2 || faceD < 2)
        refused = "'" + barsPerFace + "' is not a bar layout. Say corners, or bars on each face along the width x along "
            + "the depth with the corners counted on both - 3x3 is 8 bars. Nothing was placed";
}
if (refused.Length == 0 && shapeCount == 0)
    refused = "No Rebar Shape is loaded in this model, and Revit makes a new bar's shape from the shapes already "
        + "there. Load the rebar shapes (a structural template has them). Nothing was placed";

var columnCategory = new ElementId(BuiltInCategory.OST_StructuralColumns);

if (refused.Length == 0)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var name = (element.Category == null ? "element" : element.Category.Name) + " '" + element.Name + "' (id " + element.Id + ")";
        var column = element as FamilyInstance;
        if (column == null || column.Category == null || column.Category.Id != columnCategory)
        {
            skipped.Add(name + ": not a structural column - beams take PLACE_BEAM_REBAR, footings PLACE_FOOTING_REBAR, "
                + "slabs and walls PLACE_AREA_REINFORCEMENT");
            continue;
        }
        if (!RebarHostData.IsValidHost(column))
        {
            skipped.Add(name + ": not a rebar host - a steel column, or one whose material is not concrete");
            continue;
        }
        var existing = RebarHostData.GetRebarHostData(column).GetRebarsInHost().Count;
        if (existing > 0)
        {
            skipped.Add(name + ": already holds " + existing + " rebar set(s) - run again it would double them. "
                + "REPORT_HOST_REBAR reads them; remove them first if they are to be replaced");
            continue;
        }
        var point = column.Location as LocationPoint;
        var placedFrame = column.GetTransform();
        if (point == null || Math.Abs(placedFrame.BasisZ.Normalize().Z) < 0.9999)
        {
            skipped.Add(name + ": slanted or not plumb - only vertical columns are reinforced here");
            continue;
        }

        // The column's own axes: its X turned level, Z up.
        var zAxis = XYZ.BasisZ;
        var xAxis = new XYZ(placedFrame.BasisX.X, placedFrame.BasisX.Y, 0).Normalize();
        var yAxis = zAxis.CrossProduct(xAxis).Normalize();
        var frame = Transform.Identity;
        frame.Origin = point.Point;
        frame.BasisX = xAxis;
        frame.BasisY = yAxis;
        frame.BasisZ = zAxis;
        var box = extentsIn(column, frame);
        if (box == null)
        {
            skipped.Add(name + ": has no solid geometry to reinforce");
            continue;
        }
        double xmin = box[0], xmax = box[1], ymin = box[2], ymax = box[3], zmin = box[4], zmax = box[5];
        var width = xmax - xmin;
        var depth = ymax - ymin;
        var height = zmax - zmin;
        var radius = cylinderRadius(column, zAxis);
        var round = radius > 0 && Math.Abs(2 * radius - Math.Min(width, depth)) < 2 / 304.8;
        var full = round ? Math.PI * radius * radius * height : width * depth * height;
        if (full <= 0 || box[6] < 0.97 * full)
        {
            skipped.Add(name + ": its section is not a plain rectangle or circle (" + mm(width) + " x " + mm(depth)
                + " mm with a solid " + Math.Round(100 * box[6] / Math.Max(full, 1e-9)) + "% of that box) - not reinforced here");
            continue;
        }

        var side = useHostCover ? hostCover(column, BuiltInParameter.CLEAR_COVER_OTHER) : coverGiven;
        var top = useHostCover ? hostCover(column, BuiltInParameter.CLEAR_COVER_TOP) : coverGiven;
        var bottom = useHostCover ? hostCover(column, BuiltInParameter.CLEAR_COVER_BOTTOM) : coverGiven;
        if (double.IsNaN(side) || double.IsNaN(top) || double.IsNaN(bottom))
        {
            skipped.Add(name + ": has no rebar cover set on every face - give the cover, or SET_REBAR_COVER first");
            continue;
        }

        var mainType = main.Item2;
        var tieType = tie.Item1;
        var dm = mainType == null ? 0 : modelOf(mainType);
        var dt = tieType == null ? 0 : modelOf(tieType);
        var cx = (xmin + xmax) / 2;
        var cy = (ymin + ymax) / 2;
        Func<double, double, double, XYZ> at = (x, y, z) => frame.OfPoint(new XYZ(x, y, z));

        // ---- will it fit -------------------------------------------------------------
        string noRoom = null;
        var z0 = zmin + bottom;
        var z1 = zmax - top + extension;
        if (main.Item1 > 0)
        {
            if (z1 - z0 <= dm) noRoom = "the covers leave no length for the main bars";
            else if (round)
            {
                var ring = radius - side - dt - dm / 2;
                if (ring <= 0) noRoom = "the cover and the ties leave no room for the main bars";
                else if (2 * ring * Math.Sin(Math.PI / main.Item1) <= dm)
                    noRoom = main.Item1 + " bars round a " + mm(2 * ring) + " mm circle would touch";
            }
            else
            {
                if (faceW == 2 && faceD == 2 && main.Item1 != 4)
                    noRoom = main.Item1 + " bars on a rectangular column need a layout - say barsPerFace as bars on "
                        + "each face along the width x along the depth, 3x3 for 8";
                else if (2 * faceW + 2 * (faceD - 2) != main.Item1)
                    noRoom = "a " + faceW + "x" + faceD + " layout is " + (2 * faceW + 2 * (faceD - 2)) + " bars, not " + main.Item1;
                else
                {
                    var spanW = width - 2 * (side + dt) - dm;
                    var spanD = depth - 2 * (side + dt) - dm;
                    if (spanW <= 0 || spanD <= 0) noRoom = "the cover and the ties leave no room for the main bars";
                    else if (spanW / (faceW - 1) <= dm || spanD / (faceD - 1) <= dm)
                        noRoom = "bars on a " + mm(width) + " x " + mm(depth) + " mm section at " + faceW + "x" + faceD
                            + " would be " + mm(Math.Min(spanW / (faceW - 1), spanD / (faceD - 1))) + " mm apart, closer than their "
                            + mm(dm) + " mm diameter";
                }
            }
        }
        if (noRoom == null && tieType != null)
        {
            var inner = round ? 2 * (radius - side - dt / 2) : Math.Min(width, depth) - 2 * side - dt;
            if (inner <= 2 * dt) noRoom = "the cover leaves no room for the ties";
            else if (zmax - top - zmin - bottom - dt <= 0) noRoom = "the covers leave no height for the ties";
        }
        if (noRoom != null)
        {
            skipped.Add(name + ": " + noRoom + " - nothing placed in it");
            continue;
        }

        // ---- place, all or nothing for this column ------------------------------------
        var made = new List<Tuple<Rebar, string, int>>();   // set, what, quantity asked
        string failure = null;
        try
        {
            if (main.Item1 > 0)
            {
                if (round)
                {
                    var ring = radius - side - dt - dm / 2;
                    for (var k = 0; k < main.Item1; k++)
                    {
                        var angleK = 2 * Math.PI * k / main.Item1;
                        var x = cx + ring * Math.Cos(angleK);
                        var y = cy + ring * Math.Sin(angleK);
                        var bar = makeBar(RebarStyle.Standard, mainType, null, null, column,
                            frame.OfVector(new XYZ(-Math.Sin(angleK), Math.Cos(angleK), 0)),
                            new List<Curve> { Line.CreateBound(at(x, y, z0), at(x, y, z1)) }, false, false);
                        if (bar == null) throw new InvalidOperationException("Revit made no main bar");
                        made.Add(Tuple.Create(bar, "main bar", 1));
                    }
                }
                else
                {
                    var e = side + dt + dm / 2;
                    double xlo = xmin + e, xhi = xmax - e, ylo = ymin + e, yhi = ymax - e;
                    // The two faces along the width: faceW bars each, corners included.
                    foreach (var y in new[] { ylo, yhi })
                    {
                        var bar = makeBar(RebarStyle.Standard, mainType, null, null, column, xAxis,
                            new List<Curve> { Line.CreateBound(at(xlo, y, z0), at(xlo, y, z1)) }, false, false);
                        if (bar == null) throw new InvalidOperationException("Revit made no main bar");
                        bar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(faceW, xhi - xlo, true, true, true);
                        made.Add(Tuple.Create(bar, "main bars along the width", faceW));
                    }
                    // The two faces along the depth: the bars between the corners.
                    if (faceD > 2)
                    {
                        var step = (yhi - ylo) / (faceD - 1);
                        foreach (var x in new[] { xlo, xhi })
                        {
                            var bar = makeBar(RebarStyle.Standard, mainType, null, null, column, yAxis,
                                new List<Curve> { Line.CreateBound(at(x, ylo + step, z0), at(x, ylo + step, z1)) }, false, false);
                            if (bar == null) throw new InvalidOperationException("Revit made no main bar");
                            if (faceD - 2 > 1) bar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(faceD - 2, step * (faceD - 3), true, true, true);
                            made.Add(Tuple.Create(bar, "main bars along the depth", faceD - 2));
                        }
                    }
                }
            }

            if (tieType != null)
            {
                var zt0 = zmin + bottom + dt / 2;
                var zt1 = zmax - top - dt / 2;
                var loop = new List<Curve>();
                if (round)
                {
                    var r = radius - side - dt / 2;
                    var centre = at(cx, cy, zt0);
                    loop.Add(Arc.Create(centre, r, 0, Math.PI, xAxis, yAxis));
                    loop.Add(Arc.Create(centre, r, Math.PI, 2 * Math.PI, xAxis, yAxis));
                    if (!Rebar.ContainsValidArcRadiiForStyleAndBarType(loop, RebarStyle.StirrupTie, tieType))
                        throw new InvalidOperationException("a " + mm(2 * r) + " mm hoop is tighter than the tie bar type's bend");
                }
                else
                {
                    var t = side + dt / 2;
                    var corners = new[] { at(xmin + t, ymin + t, zt0), at(xmax - t, ymin + t, zt0), at(xmax - t, ymax - t, zt0), at(xmin + t, ymax - t, zt0) };
                    for (var i = 0; i < 4; i++) loop.Add(Line.CreateBound(corners[i], corners[(i + 1) % 4]));
                }
                // Anticlockwise seen from above, so a hook into the core is to the
                // left of the bar's travel: Z x tangent.
                var startTangent = tangentAt(loop[0], false);
                var endTangent = tangentAt(loop[loop.Count - 1], true);
                var startRight = onRight(startTangent.Negate(), zAxis, zAxis.CrossProduct(startTangent));
                var endRight = onRight(endTangent, zAxis, zAxis.CrossProduct(endTangent));
                var tieSet = makeBar(RebarStyle.StirrupTie, tieType, tieHookType, tieHookType, column, zAxis, loop, startRight, endRight);
                if (tieSet == null) throw new InvalidOperationException("Revit made no tie");
                // Maximum Spacing: the fewest equal gaps no wider than asked, so
                // ceil(length / spacing) gaps and one tie more than that.
                var tieLength = zt1 - zt0;
                var tiesExpected = 1;
                if (tieLength > 0.01)
                {
                    tieSet.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(tie.Item2, tieLength, true, true, true);
                    tiesExpected = (int)Math.Ceiling(tieLength / tie.Item2 - 1e-9) + 1;
                }
                made.Add(Tuple.Create(tieSet, "ties", tiesExpected));
            }
            doc.Regenerate();
        }
        catch (Exception ex)
        {
            failure = ex.Message;
        }

        if (failure != null)
        {
            foreach (var item in made)
                if (item.Item1 != null && item.Item1.IsValidObject)
                {
                    try { doc.Delete(item.Item1.Id); } catch (Exception) { }
                }
            skipped.Add(name + ": Revit refused a bar - " + failure + " - every bar made in it was removed again");
            continue;
        }

        // ---- read back -------------------------------------------------------------------
        var setLines = new List<string>();
        var mainRead = 0;
        foreach (var item in made)
        {
            var bar = item.Item1;
            if (bar == null || !bar.IsValidObject)
            {
                readBackWrong.Add(name + ": a set of " + item.Item2 + " is gone after the regenerate");
                continue;
            }
            placed.Add(bar.Id);
            if (bar.GetHostId() != column.Id) readBackWrong.Add(name + ": rebar " + bar.Id + " reads another host");
            var quantity = bar.Quantity;
            var text = item.Item2 + " " + quantity + " x '" + doc.GetElement(bar.GetTypeId()).Name + "'";
            if (item.Item2 == "ties")
            {
                text += " at " + mm(bar.LayoutRule == RebarLayoutRule.Single ? 0 : bar.MaxSpacing) + " mm max";
                if (quantity != item.Item3) readBackWrong.Add(name + ": " + quantity + " ties read back where " + item.Item3 + " fit the height at " + mm(tie.Item2) + " mm");
            }
            else
            {
                mainRead += quantity;
                if (quantity != item.Item3) readBackWrong.Add(name + ": " + quantity + " " + item.Item2 + " read back where " + item.Item3 + " were asked");
            }
            setLines.Add(text + " (id " + bar.Id + ")");
        }
        if (main.Item1 > 0 && mainRead != main.Item1)
            readBackWrong.Add(name + ": " + mainRead + " main bars read back where " + main.Item1 + " were asked");
        columnsReinforced.Add(column.Id);
        lines.Add(name + ": " + (round ? "round " + mm(2 * radius) : mm(width) + " x " + mm(depth)) + " mm, " + mm(height)
            + " mm high; cover sides " + mm(side) + ", top " + mm(top) + ", bottom " + mm(bottom) + " mm"
            + (useHostCover ? " (the column's own)" : " (as given)") + (extension > 0 ? "; main bars run " + mm(extension) + " mm past the top" : "")
            + "; " + string.Join(", ", setLines));
    }
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(placed.Count + " rebar set(s) placed and read back in " + columnsReinforced.Count + " column(s); "
        + skipped.Count + " column(s) skipped" + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " read back other than asked" : "") + ".");
    if (tieHookType == null && tie != null && tie.Item1 != null) findings.Add("The ties were placed with no hooks, as asked.");
    findings.AddRange(skipped);
    findings.AddRange(readBackWrong);
}
