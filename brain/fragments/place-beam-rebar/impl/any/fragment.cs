// NOT STANDALONE. Assumes `doc`, `elements`, `bottomBars`, `topBars`,
// `sideBars`, `stirrups`, `stirrupHook`, `barHook` and `cover` are in scope;
// leaves `placed`, `beamsReinforced`, `skipped`, `readBackWrong`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// EVERY VALUE IS THE MODELLER'S (D-33), every length millimetres (D-71). The
// bars are placed from the beam's OWN solid, read along its own line - so the
// length is the concrete that is there after the joins at its ends, not the
// line between the column centres.
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
var beamsReinforced = new List<ElementId>();
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
Func<Element, BuiltInParameter, double> hostCover = (host, bip) =>
{
    Parameter p = null;
    try { p = host.get_Parameter(bip); } catch (Exception) { }
    var type = p == null ? null : doc.GetElement(p.AsElementId()) as RebarCoverType;
    return type == null ? double.NaN : type.CoverDistance;
};

// ---- the request, read once, before anything is placed -------------------------------
Tuple<int, RebarBarType> bottom = null;
Tuple<int, RebarBarType> topLayer = null;
Tuple<int, RebarBarType> sides = null;
Tuple<RebarBarType, double> stirrup = null;
RebarHookType stirrupHookType = null;
RebarHookType barHookType = null;
var coverText = (cover ?? "").Trim();
var useHostCover = coverText.Length == 0 || coverText.Equals("host", StringComparison.OrdinalIgnoreCase);
var coverGiven = 0.0;
var sideText = (sideBars ?? "").Trim();

bottom = countAndType(bottomBars, "bottom bars");
if (refused.Length == 0) topLayer = countAndType(topBars, "top bars");
if (refused.Length == 0) sides = sideText.Length == 0 ? Tuple.Create(0, (RebarBarType)null) : countAndType(sideText, "side face bars");
if (refused.Length == 0) stirrup = typeAndSpacing(stirrups, "stirrups");
if (refused.Length == 0 && stirrup != null && stirrup.Item1 != null) stirrupHookType = hookFrom(stirrupHook, RebarStyle.StirrupTie, "stirrups");
if (refused.Length == 0) barHookType = hookFrom(barHook, RebarStyle.Standard, "top and bottom bars");
if (refused.Length == 0 && !useHostCover) coverGiven = lengthFrom(coverText, "cover");
if (refused.Length == 0 && bottom.Item1 == 0 && topLayer.Item1 == 0 && sides.Item1 == 0 && stirrup.Item1 == null)
    refused = "Every layer was asked as none, so there is nothing to place. Nothing was placed";
if (refused.Length == 0 && shapeCount == 0)
    refused = "No Rebar Shape is loaded in this model, and Revit makes a new bar's shape from the shapes already "
        + "there. Load the rebar shapes (a structural template has them). Nothing was placed";

var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);

if (refused.Length == 0)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var name = (element.Category == null ? "element" : element.Category.Name) + " '" + element.Name + "' (id " + element.Id + ")";
        var beam = element as FamilyInstance;
        if (beam == null || beam.Category == null || beam.Category.Id != framingCategory)
        {
            skipped.Add(name + ": not structural framing - columns take PLACE_COLUMN_REBAR, footings PLACE_FOOTING_REBAR, "
                + "slabs and walls PLACE_AREA_REINFORCEMENT");
            continue;
        }
        if (!RebarHostData.IsValidHost(beam))
        {
            skipped.Add(name + ": not a rebar host - a steel or timber member, or one whose material is not concrete");
            continue;
        }
        var existing = RebarHostData.GetRebarHostData(beam).GetRebarsInHost().Count;
        if (existing > 0)
        {
            skipped.Add(name + ": already holds " + existing + " rebar set(s) - run again it would double them. "
                + "REPORT_HOST_REBAR reads them; remove them first if they are to be replaced");
            continue;
        }
        var located = beam.Location as LocationCurve;
        var axisLine = located == null ? null : located.Curve as Line;
        if (axisLine == null)
        {
            skipped.Add(name + ": not a straight beam - a curved beam is not reinforced here");
            continue;
        }
        var xAxis = axisLine.Direction.Normalize();
        if (Math.Abs(xAxis.Z) > 0.999)
        {
            skipped.Add(name + ": vertical - a column takes PLACE_COLUMN_REBAR");
            continue;
        }
        Parameter rotation = null;
        try { rotation = beam.get_Parameter(BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE); } catch (Exception) { }
        if (rotation != null && rotation.StorageType == StorageType.Double && Math.Abs(rotation.AsDouble()) > 1e-6)
        {
            skipped.Add(name + ": its cross-section is rotated - not reinforced here");
            continue;
        }

        // The beam's own axes: X along it, Y level across it, Z up through it.
        var yAxis = XYZ.BasisZ.CrossProduct(xAxis).Normalize();
        var zAxis = xAxis.CrossProduct(yAxis).Normalize();
        var frame = Transform.Identity;
        frame.Origin = axisLine.GetEndPoint(0);
        frame.BasisX = xAxis;
        frame.BasisY = yAxis;
        frame.BasisZ = zAxis;
        var box = extentsIn(beam, frame);
        if (box == null)
        {
            skipped.Add(name + ": has no solid geometry to reinforce");
            continue;
        }
        double xmin = box[0], xmax = box[1], ymin = box[2], ymax = box[3], zmin = box[4], zmax = box[5];
        var length = xmax - xmin;
        var width = ymax - ymin;
        var depth = zmax - zmin;
        if (box[6] < 0.9 * length * width * depth)
        {
            skipped.Add(name + ": its section is not a plain rectangle (" + mm(width) + " x " + mm(depth)
                + " mm, the solid " + Math.Round(100 * box[6] / Math.Max(length * width * depth, 1e-9)) + "% of that box) - not reinforced here");
            continue;
        }

        var side = useHostCover ? hostCover(beam, BuiltInParameter.CLEAR_COVER_OTHER) : coverGiven;
        var top = useHostCover ? hostCover(beam, BuiltInParameter.CLEAR_COVER_TOP) : coverGiven;
        var under = useHostCover ? hostCover(beam, BuiltInParameter.CLEAR_COVER_BOTTOM) : coverGiven;
        if (double.IsNaN(side) || double.IsNaN(top) || double.IsNaN(under))
        {
            skipped.Add(name + ": has no rebar cover set on every face - give the cover, or SET_REBAR_COVER first");
            continue;
        }

        var dt = stirrup.Item1 == null ? 0 : modelOf(stirrup.Item1);
        var db = bottom.Item2 == null ? 0 : modelOf(bottom.Item2);
        var dtop = topLayer.Item2 == null ? 0 : modelOf(topLayer.Item2);
        var ds = sides.Item2 == null ? 0 : modelOf(sides.Item2);
        var zb = zmin + under + dt + db / 2;
        var zt = zmax - top - dt - dtop / 2;
        var x0 = xmin + side;
        var x1 = xmax - side;
        Func<double, double, double, XYZ> at = (x, y, z) => frame.OfPoint(new XYZ(x, y, z));

        // ---- will it fit -------------------------------------------------------------
        string noRoom = null;
        Func<Tuple<int, RebarBarType>, double, string> layerRoom = (layer, d) =>
        {
            if (layer.Item1 == 0) return null;
            var span = width - 2 * (side + dt) - d;
            if (span < 0 || (layer.Item1 > 1 && span <= 0)) return "the cover and the stirrups leave no room across the beam";
            if (layer.Item1 > 1 && span / (layer.Item1 - 1) <= d)
                return layer.Item1 + " bars across " + mm(width) + " mm would be " + mm(span / (layer.Item1 - 1)) + " mm apart, closer than their " + mm(d) + " mm diameter";
            return null;
        };
        if (x1 - x0 <= 0) noRoom = "the end covers leave no length";
        if (noRoom == null) noRoom = layerRoom(bottom, db);
        if (noRoom == null) noRoom = layerRoom(topLayer, dtop);
        var bottomTop = bottom.Item1 > 0 ? zb + db / 2 : zmin + under + dt;
        var topBottom = topLayer.Item1 > 0 ? zt - dtop / 2 : zmax - top - dt;
        if (noRoom == null && topBottom - bottomTop <= 0) noRoom = "the top and bottom bars would meet in a " + mm(depth) + " mm deep beam";
        if (noRoom == null && sides.Item1 > 0 && (topBottom - bottomTop) / (sides.Item1 + 1) <= ds)
            noRoom = sides.Item1 + " side face bars do not fit between the top and bottom bars";
        if (noRoom == null && stirrup.Item1 != null && (width - 2 * side - dt <= 2 * dt || depth - top - under - dt <= 2 * dt))
            noRoom = "the cover leaves no room for the stirrups";
        if (noRoom != null)
        {
            skipped.Add(name + ": " + noRoom + " - nothing placed in it");
            continue;
        }

        // ---- place, all or nothing for this beam --------------------------------------
        var made = new List<Tuple<Rebar, string, int>>();
        string failure = null;
        try
        {
            // A straight layer along the beam: `count` bars across it at height z,
            // hooks turned toward `hookTowards`.
            Action<int, RebarBarType, double, double, XYZ, string> layerAlong = (count, type, d, z, hookTowards, what) =>
            {
                var ylo = ymin + side + dt + d / 2;
                var yhi = ymax - side - dt - d / 2;
                var y = count == 1 ? (ymin + ymax) / 2 : ylo;
                var curve = Line.CreateBound(at(x0, y, z), at(x1, y, z));
                var hookTowardsWorld = frame.OfVector(hookTowards);
                var startRight = onRight(xAxis.Negate(), yAxis, hookTowardsWorld);
                var endRight = onRight(xAxis, yAxis, hookTowardsWorld);
                var bar = makeBar(RebarStyle.Standard, type, barHookType, barHookType, beam, yAxis, new List<Curve> { curve }, startRight, endRight);
                if (bar == null) throw new InvalidOperationException("Revit made no " + what);
                if (count > 1) bar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(count, yhi - ylo, true, true, true);
                made.Add(Tuple.Create(bar, what, count));
            };
            if (bottom.Item1 > 0) layerAlong(bottom.Item1, bottom.Item2, db, zb, XYZ.BasisZ, "bottom bars");
            if (topLayer.Item1 > 0) layerAlong(topLayer.Item1, topLayer.Item2, dtop, zt, XYZ.BasisZ.Negate(), "top bars");

            // Side face bars: `count` on each face, evenly between the layers.
            if (sides.Item1 > 0)
            {
                var step = (topBottom - bottomTop) / (sides.Item1 + 1);
                foreach (var y in new[] { ymin + side + dt + ds / 2, ymax - side - dt - ds / 2 })
                {
                    var curve = Line.CreateBound(at(x0, y, bottomTop + step), at(x1, y, bottomTop + step));
                    var bar = makeBar(RebarStyle.Standard, sides.Item2, null, null, beam, zAxis, new List<Curve> { curve }, false, false);
                    if (bar == null) throw new InvalidOperationException("Revit made no side face bar");
                    if (sides.Item1 > 1) bar.GetShapeDrivenAccessor().SetLayoutAsFixedNumber(sides.Item1, step * (sides.Item1 - 1), true, true, true);
                    made.Add(Tuple.Create(bar, "side face bars", sides.Item1));
                }
            }

            if (stirrup.Item1 != null)
            {
                var t = dt / 2;
                double ylo = ymin + side + t, yhi = ymax - side - t, zlo = zmin + under + t, zhi = zmax - top - t;
                var xs = xmin + side + t;
                // Anticlockwise seen from the beam's start, the hooks at a top
                // corner: top-left, down, across the bottom, up, back.
                var corners = new[] { at(xs, ylo, zhi), at(xs, ylo, zlo), at(xs, yhi, zlo), at(xs, yhi, zhi) };
                var loop = new List<Curve>();
                for (var i = 0; i < 4; i++) loop.Add(Line.CreateBound(corners[i], corners[(i + 1) % 4]));
                var startTangent = tangentAt(loop[0], false);
                var endTangent = tangentAt(loop[3], true);
                var startRight = onRight(startTangent.Negate(), xAxis, xAxis.CrossProduct(startTangent));
                var endRight = onRight(endTangent, xAxis, xAxis.CrossProduct(endTangent));
                var set = makeBar(RebarStyle.StirrupTie, stirrup.Item1, stirrupHookType, stirrupHookType, beam, xAxis, loop, startRight, endRight);
                if (set == null) throw new InvalidOperationException("Revit made no stirrup");
                // Maximum Spacing: the fewest equal gaps no wider than asked.
                var run = (xmax - side - t) - xs;
                var expected = 1;
                if (run > 0.01)
                {
                    set.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(stirrup.Item2, run, true, true, true);
                    expected = (int)Math.Ceiling(run / stirrup.Item2 - 1e-9) + 1;
                }
                made.Add(Tuple.Create(set, "stirrups", expected));
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
        foreach (var item in made)
        {
            var bar = item.Item1;
            if (bar == null || !bar.IsValidObject)
            {
                readBackWrong.Add(name + ": a set of " + item.Item2 + " is gone after the regenerate");
                continue;
            }
            placed.Add(bar.Id);
            if (bar.GetHostId() != beam.Id) readBackWrong.Add(name + ": rebar " + bar.Id + " reads another host");
            var quantity = bar.Quantity;
            var text = item.Item2 + " " + quantity + " x '" + doc.GetElement(bar.GetTypeId()).Name + "'";
            if (item.Item2 == "stirrups" && bar.LayoutRule != RebarLayoutRule.Single) text += " at " + mm(bar.MaxSpacing) + " mm max";
            if (quantity != item.Item3) readBackWrong.Add(name + ": " + quantity + " " + item.Item2 + " read back where " + item.Item3 + " were asked");
            setLines.Add(text + " (id " + bar.Id + ")");
        }
        beamsReinforced.Add(beam.Id);
        lines.Add(name + ": " + mm(width) + " x " + mm(depth) + " mm, " + mm(length) + " mm of concrete; cover sides and ends "
            + mm(side) + ", top " + mm(top) + ", bottom " + mm(under) + " mm" + (useHostCover ? " (the beam's own)" : " (as given)")
            + (barHookType == null ? "" : "; top and bottom bars hooked '" + barHookType.Name + "' at both ends")
            + "; " + string.Join(", ", setLines));
    }
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(placed.Count + " rebar set(s) placed and read back in " + beamsReinforced.Count + " beam(s); "
        + skipped.Count + " beam(s) skipped" + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " read back other than asked" : "") + ".");
    if (stirrupHookType == null && stirrup != null && stirrup.Item1 != null) findings.Add("The stirrups were placed with no hooks, as asked.");
    findings.AddRange(skipped);
    findings.AddRange(readBackWrong);
}
