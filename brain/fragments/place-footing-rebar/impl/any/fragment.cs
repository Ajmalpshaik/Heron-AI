// NOT STANDALONE. Assumes `doc`, `elements`, `bottomLength`, `bottomWidth`,
// `topLength`, `topWidth`, `outerLayer`, `barHook` and `cover` are in scope;
// leaves `placed`, `footingsReinforced`, `skipped`, `readBackWrong`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// EVERY VALUE IS THE MODELLER'S (D-33), every length millimetres (D-71). The
// bars are placed from the footing's OWN solid, read in its own axes. Which
// layer sits outermost is asked, never chosen: it moves the lever arm of both.
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
var footingsReinforced = new List<ElementId>();
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
var coverText = (cover ?? "").Trim();
var useHostCover = coverText.Length == 0 || coverText.Equals("host", StringComparison.OrdinalIgnoreCase);
var coverGiven = 0.0;
var outer = (outerLayer ?? "").Trim().ToLowerInvariant();
RebarHookType barHookType = null;

var bottomL = typeAndSpacing(bottomLength, "bottom bars along the length");
Tuple<RebarBarType, double> bottomW = null, topL = null, topW = null;
if (refused.Length == 0) bottomW = typeAndSpacing(bottomWidth, "bottom bars along the width");
if (refused.Length == 0) topL = typeAndSpacing(topLength, "top bars along the length");
if (refused.Length == 0) topW = typeAndSpacing(topWidth, "top bars along the width");
if (refused.Length == 0) barHookType = hookFrom(barHook, RebarStyle.Standard, "footing bars");
if (refused.Length == 0 && !useHostCover) coverGiven = lengthFrom(coverText, "cover");
if (refused.Length == 0 && outer != "length" && outer != "width")
    refused = "'" + (outerLayer ?? "") + "' does not say which bars sit outermost. Say length - the bars running along "
        + "the footing's length are lowest at the bottom and highest at the top - or width. Nothing was placed";
if (refused.Length == 0 && bottomL.Item1 == null && bottomW.Item1 == null && topL.Item1 == null && topW.Item1 == null)
    refused = "Every layer was asked as none, so there is nothing to place. Nothing was placed";
if (refused.Length == 0 && shapeCount == 0)
    refused = "No Rebar Shape is loaded in this model, and Revit makes a new bar's shape from the shapes already "
        + "there. Load the rebar shapes (a structural template has them). Nothing was placed";

var foundationCategory = new ElementId(BuiltInCategory.OST_StructuralFoundation);

if (refused.Length == 0)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var name = (element.Category == null ? "element" : element.Category.Name) + " '" + element.Name + "' (id " + element.Id + ")";
        var footing = element as FamilyInstance;
        if (footing == null || footing.Category == null || footing.Category.Id != foundationCategory)
        {
            skipped.Add(name + (element is Floor
                ? ": a foundation slab - it takes PLACE_AREA_REINFORCEMENT"
                : element is WallFoundation
                    ? ": a wall (strip) foundation - not reinforced by a tool yet"
                    : ": not an isolated footing - columns take PLACE_COLUMN_REBAR, beams PLACE_BEAM_REBAR"));
            continue;
        }
        if (!RebarHostData.IsValidHost(footing))
        {
            skipped.Add(name + ": not a rebar host - its material is not concrete, or it is not structural");
            continue;
        }
        var existing = RebarHostData.GetRebarHostData(footing).GetRebarsInHost().Count;
        if (existing > 0)
        {
            skipped.Add(name + ": already holds " + existing + " rebar set(s) - run again it would double them. "
                + "REPORT_HOST_REBAR reads them; remove them first if they are to be replaced");
            continue;
        }
        var placedFrame = footing.GetTransform();
        if (Math.Abs(placedFrame.BasisZ.Normalize().Z) < 0.9999)
        {
            skipped.Add(name + ": not level - not reinforced here");
            continue;
        }
        var zAxis = XYZ.BasisZ;
        var xAxis = new XYZ(placedFrame.BasisX.X, placedFrame.BasisX.Y, 0).Normalize();
        var yAxis = zAxis.CrossProduct(xAxis).Normalize();
        var frame = Transform.Identity;
        frame.Origin = placedFrame.Origin;
        frame.BasisX = xAxis;
        frame.BasisY = yAxis;
        frame.BasisZ = zAxis;
        var box = extentsIn(footing, frame);
        if (box == null)
        {
            skipped.Add(name + ": has no solid geometry to reinforce");
            continue;
        }
        double xmin = box[0], xmax = box[1], ymin = box[2], ymax = box[3], zmin = box[4], zmax = box[5];
        var sizeX = xmax - xmin;
        var sizeY = ymax - ymin;
        var thickness = zmax - zmin;
        if (box[6] < 0.9 * sizeX * sizeY * thickness)
        {
            skipped.Add(name + ": not a plain rectangular block (" + mm(sizeX) + " x " + mm(sizeY) + " x " + mm(thickness)
                + " mm, the solid " + Math.Round(100 * box[6] / Math.Max(sizeX * sizeY * thickness, 1e-9)) + "% of that box) - not reinforced here");
            continue;
        }
        // The LENGTH is the longer side; a square footing's length is its own X.
        var lengthAlongX = sizeX >= sizeY;

        var side = useHostCover ? hostCover(footing, BuiltInParameter.CLEAR_COVER_OTHER) : coverGiven;
        var top = useHostCover ? hostCover(footing, BuiltInParameter.CLEAR_COVER_TOP) : coverGiven;
        var under = useHostCover ? hostCover(footing, BuiltInParameter.CLEAR_COVER_BOTTOM) : coverGiven;
        if (double.IsNaN(side) || double.IsNaN(top) || double.IsNaN(under))
        {
            skipped.Add(name + ": has no rebar cover set on every face - give the cover, or SET_REBAR_COVER first");
            continue;
        }
        Func<double, double, double, XYZ> at = (x, y, z) => frame.OfPoint(new XYZ(x, y, z));

        // The layers, outermost first at each face: (bars, runs along X?, what).
        var bottomOuter = outer == "length" ? bottomL : bottomW;
        var bottomInner = outer == "length" ? bottomW : bottomL;
        var topOuter = outer == "length" ? topL : topW;
        var topInner = outer == "length" ? topW : topL;
        var outerAlongX = outer == "length" ? lengthAlongX : !lengthAlongX;
        var outerWord = outer == "length" ? "along the length" : "along the width";
        var innerWord = outer == "length" ? "along the width" : "along the length";
        Func<Tuple<RebarBarType, double>, double> dia = layer => layer.Item1 == null ? 0 : modelOf(layer.Item1);

        var zBottomOuter = zmin + under + dia(bottomOuter) / 2;
        var zBottomInner = zmin + under + dia(bottomOuter) + dia(bottomInner) / 2;
        var zTopOuter = zmax - top - dia(topOuter) / 2;
        var zTopInner = zmax - top - dia(topOuter) - dia(topInner) / 2;

        string noRoom = null;
        var bottomStack = under + dia(bottomOuter) + dia(bottomInner);
        var topStack = top + dia(topOuter) + dia(topInner);
        if (bottomStack + topStack >= thickness) noRoom = "the covers and the bar layers do not fit in a " + mm(thickness) + " mm thickness";
        foreach (var layer in new[] { bottomOuter, bottomInner, topOuter, topInner })
        {
            if (noRoom != null || layer.Item1 == null) continue;
            if (sizeX - 2 * side - dia(layer) <= 0 || sizeY - 2 * side - dia(layer) <= 0) noRoom = "the side cover leaves no room for the bars";
            else if (layer.Item2 <= dia(layer)) noRoom = "a spacing of " + mm(layer.Item2) + " mm is not wider than the bar";
        }
        if (noRoom != null)
        {
            skipped.Add(name + ": " + noRoom + " - nothing placed in it");
            continue;
        }

        // ---- place, all or nothing for this footing ---------------------------------
        var made = new List<Tuple<Rebar, string, int>>();
        string failure = null;
        try
        {
            // One layer: bars running along X (or Y) at height z, spaced across the
            // other way at no more than the spacing asked, hooks toward `hookTowards`.
            Action<Tuple<RebarBarType, double>, bool, double, XYZ, string> layerOf = (layer, alongX, z, hookTowards, what) =>
            {
                if (layer.Item1 == null) return;
                var d = modelOf(layer.Item1);
                XYZ from, to, normal, run;
                double arrayLength;
                if (alongX)
                {
                    var y = ymin + side + d / 2;
                    from = at(xmin + side, y, z); to = at(xmax - side, y, z);
                    normal = yAxis; run = xAxis;
                    arrayLength = (ymax - side - d / 2) - y;
                }
                else
                {
                    var x = xmin + side + d / 2;
                    from = at(x, ymin + side, z); to = at(x, ymax - side, z);
                    normal = xAxis; run = yAxis;
                    arrayLength = (xmax - side - d / 2) - x;
                }
                var towards = frame.OfVector(hookTowards);
                var bar = makeBar(RebarStyle.Standard, layer.Item1, barHookType, barHookType, footing, normal,
                    new List<Curve> { Line.CreateBound(from, to) }, onRight(run.Negate(), normal, towards), onRight(run, normal, towards));
                if (bar == null) throw new InvalidOperationException("Revit made no " + what);
                var expected = 1;
                if (arrayLength > 0.01)
                {
                    bar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(layer.Item2, arrayLength, true, true, true);
                    expected = (int)Math.Ceiling(arrayLength / layer.Item2 - 1e-9) + 1;
                }
                made.Add(Tuple.Create(bar, what, expected));
            };
            layerOf(bottomOuter, outerAlongX, bottomOuter.Item1 == null ? zmin + under + dia(bottomInner) / 2 : zBottomOuter, XYZ.BasisZ, "bottom bars " + outerWord);
            layerOf(bottomInner, !outerAlongX, bottomOuter.Item1 == null ? zmin + under + dia(bottomInner) / 2 : zBottomInner, XYZ.BasisZ, "bottom bars " + innerWord);
            layerOf(topOuter, outerAlongX, topOuter.Item1 == null ? zmax - top - dia(topInner) / 2 : zTopOuter, XYZ.BasisZ.Negate(), "top bars " + outerWord);
            layerOf(topInner, !outerAlongX, topOuter.Item1 == null ? zmax - top - dia(topInner) / 2 : zTopInner, XYZ.BasisZ.Negate(), "top bars " + innerWord);
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
            if (bar.GetHostId() != footing.Id) readBackWrong.Add(name + ": rebar " + bar.Id + " reads another host");
            var quantity = bar.Quantity;
            var text = item.Item2 + " " + quantity + " x '" + doc.GetElement(bar.GetTypeId()).Name + "'"
                + (bar.LayoutRule == RebarLayoutRule.Single ? "" : " at " + mm(bar.MaxSpacing) + " mm max");
            if (quantity != item.Item3) readBackWrong.Add(name + ": " + quantity + " " + item.Item2 + " read back where " + item.Item3 + " fit at the spacing asked");
            setLines.Add(text + " (id " + bar.Id + ")");
        }
        footingsReinforced.Add(footing.Id);
        lines.Add(name + ": " + mm(lengthAlongX ? sizeX : sizeY) + " long x " + mm(lengthAlongX ? sizeY : sizeX) + " wide x "
            + mm(thickness) + " mm; cover sides " + mm(side) + ", top " + mm(top) + ", bottom " + mm(under) + " mm"
            + (useHostCover ? " (the footing's own)" : " (as given)") + "; bars " + outerWord + " outermost"
            + (barHookType == null ? "" : "; hooked '" + barHookType.Name + "' at both ends, into the footing")
            + "; " + string.Join(", ", setLines));
    }
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(placed.Count + " rebar set(s) placed and read back in " + footingsReinforced.Count + " footing(s); "
        + skipped.Count + " skipped" + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " read back other than asked" : "") + ".");
    findings.AddRange(skipped);
    findings.AddRange(readBackWrong);
}
