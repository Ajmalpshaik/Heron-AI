// NOT STANDALONE. Assumes `doc`, `elements`, `barType`, `shape`, `box`,
// `through`, `spreadToward`, `spacing`, `count` and `setLength` are in scope;
// leaves `placed`, `readBackWrong`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ONE BAR OR ONE SET IN ONE HOST, two routes, as Revit's Structure > Rebar:
//   a STANDARD SHAPE named exactly, fitted into a box of two or three points
//     - Rebar.CreateFromRebarShape, then ScaleToBox;
//   a SKETCHED bar through points, straight segments in one plane
//     - Rebar.CreateFromCurves, which uses a loaded shape that fits or makes one.
// EVERY VALUE IS THE MODELLER'S (D-33). Points arrive in Revit's feet, the
// binder having read them in millimetres; spacing and set length are text in
// millimetres and become feet here, / 304.8 (D-20). The host's cover is read
// and reported, never changed.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);
Func<XYZ, string> mmPoint = p => mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z);
Func<XYZ, string> dirText = v => Math.Round(v.X, 3).ToString("0.###", invariant) + ", "
    + Math.Round(v.Y, 3).ToString("0.###", invariant) + ", " + Math.Round(v.Z, 3).ToString("0.###", invariant);
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
var readBackWrong = new List<string>();
var findings = new List<string>();
var refused = "";
var report = "";
var tiny = 1.0 / 304.8;   // one millimetre

// ---- what the model has ---------------------------------------------------------
var barTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
var shapes = new FilteredElementCollector(doc).OfClass(typeof(RebarShape)).Cast<RebarShape>().ToList();
Func<string> barList = () => barTypes.Count == 0 ? "none" : string.Join(", ", barTypes.OrderBy(t => nominalOf(t))
    .Select(t => "'" + t.Name + "' " + mm(nominalOf(t)) + " mm"));
Func<string, string> shapeList = asked =>
{
    var near = shapes.Where(s => asked.Length > 0 && s.Name.IndexOf(asked, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(s => "'" + s.Name + "'").OrderBy(n => n, StringComparer.Ordinal).ToList();
    if (near.Count > 0) return "Shapes whose name has '" + asked + "' in it: " + string.Join(", ", near);
    var all = shapes.Select(s => "'" + s.Name + "'").OrderBy(n => n, StringComparer.Ordinal).ToList();
    return "Loaded shapes" + (all.Count > 40 ? " (the first 40 of " + all.Count + ")" : "") + ": "
        + (all.Count == 0 ? "none" : string.Join(", ", all.Take(40)));
};
Func<string, bool> isNone = text =>
{
    var t = (text ?? "").Trim();
    return t.Length == 0 || t.Equals("none", StringComparison.OrdinalIgnoreCase);
};

// A bar type by its exact name, or by a diameter in millimetres matched to the
// ONE type of that nominal diameter. Null with `refused` set. "T16" is a bar
// type only if one is named "T16" (D-34).
Func<string, RebarBarType> barTypeFrom = text =>
{
    var asked = (text ?? "").Trim();
    if (asked.Length == 0)
    {
        refused = "No bar type was named - Heron never chooses one. Loaded: " + barList() + ". Nothing was placed";
        return null;
    }
    var named = barTypes.FirstOrDefault(t => string.Equals(t.Name, asked, StringComparison.Ordinal));
    if (named != null) return named;
    var number = asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase) ? asked.Substring(0, asked.Length - 2).Trim() : asked;
    double diameter;
    if (!double.TryParse(number, styleFloat, invariant, out diameter) || diameter <= 0)
    {
        refused = "No Rebar Bar Type is named '" + asked + "'. Loaded: " + barList()
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
// A length in millimetres to feet, or NaN with `refused` set.
Func<string, string, double> lengthFrom = (text, what) =>
{
    var asked = (text ?? "").Trim();
    if (asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) asked = asked.Substring(0, asked.Length - 2).Trim();
    double value;
    if (!double.TryParse(asked, styleFloat, invariant, out value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
    {
        refused = "'" + (text ?? "") + "' is not a " + what + " in millimetres. Nothing was placed";
        return double.NaN;
    }
    return value / 304.8;
};
// Any vector square to `d`, for a straight bar's plane when nothing else sets
// it: that plane does not move a straight bar with no hooks, and is reported.
Func<XYZ, XYZ> squareTo = d =>
{
    var c = d.CrossProduct(XYZ.BasisZ);
    if (c.GetLength() < 1e-6) c = d.CrossProduct(XYZ.BasisX);
    return c.Normalize();
};

// THE ONE PLACE THAT KNOWS THE TWO CreateFromCurves ROUTES: the hook-type form
// is 2020-2026 and gone at 2027; 2026 on takes a BarTerminationsData. A
// sketched bar here has no hooks, so both ends are none.
Func<RebarBarType, Element, XYZ, IList<Curve>, Rebar> sketchBar = (type, host, normal, curves) =>
{
#if REVIT2026_OR_GREATER
    var ends = new BarTerminationsData(doc);
    ends.HookTypeIdAtStart = ElementId.InvalidElementId;
    ends.HookTypeIdAtEnd = ElementId.InvalidElementId;
    return Rebar.CreateFromCurves(doc, RebarStyle.Standard, type, host, normal, curves, ends, true, true);
#else
    return Rebar.CreateFromCurves(doc, RebarStyle.Standard, type, null, null, host, normal, curves,
        RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
#endif
};

// ---- the request, read once, before anything is placed ---------------------------
var hosts = (elements ?? new List<Element>()).Where(e => e != null).ToList();
Element host = null;
string hostName = "";
if (hosts.Count != 1)
{
    refused = hosts.Count == 0
        ? "No host was given. Select the ONE concrete wall, floor, foundation, beam or column the bar goes in. Nothing was placed"
        : hosts.Count + " elements were given (" + string.Join(", ", hosts.Take(5).Select(e => "'" + e.Name + "' id " + e.Id))
            + (hosts.Count > 5 ? ", ..." : "") + "). A bar goes in ONE host - select just that one. Nothing was placed";
}
else
{
    host = hosts[0];
    hostName = (host.Category == null ? "element" : host.Category.Name) + " '" + host.Name + "' (id " + host.Id + ")";
    if (!RebarHostData.IsValidHost(host))
    {
        var seen = new List<string>();
        var asInstance = host as FamilyInstance;
        if (asInstance != null) seen.Add("its structural material reads " + asInstance.StructuralMaterialType);
        foreach (var bip in new[] { BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL })
        {
            Parameter flag = null;
            try { flag = host.get_Parameter(bip); } catch (Exception) { }
            if (flag != null && flag.StorageType == StorageType.Integer)
                seen.Add("Structural is " + (flag.AsInteger() == 1 ? "on" : "off"));
        }
        refused = "Revit says " + hostName + " cannot hold rebar - its rebar host check answers no"
            + (seen.Count > 0 ? " (" + string.Join("; ", seen) + ")" : "")
            + ". Revit puts rebar only in structural concrete walls, floors, foundations, beams and columns. Nothing was placed";
    }
}

RebarBarType type = null;
if (refused.Length == 0) type = barTypeFrom(barType);
if (refused.Length == 0 && shapes.Count == 0)
    refused = "No Rebar Shape is loaded in this model, and Revit draws every bar from one. Load the rebar shapes "
        + "(a structural template has them). Nothing was placed";

RebarShape shapeAsked = null;
var sketch = isNone(shape);
var boxPoints = box ?? new List<XYZ>();
var throughPoints = through ?? new List<XYZ>();
if (refused.Length == 0 && !sketch)
{
    var askedShape = shape.Trim();
    shapeAsked = shapes.FirstOrDefault(s => string.Equals(s.Name, askedShape, StringComparison.Ordinal));
    if (shapeAsked == null)
        refused = "No Rebar Shape is named '" + askedShape + "'. " + shapeList(askedShape)
            + ". Name the shape exactly as the Project Browser shows it. Nothing was placed";
    else if (throughPoints.Count > 0)
        refused = "Shape '" + shapeAsked.Name + "' was named AND points to sketch through were given. A shape is fitted "
            + "into a box (two or three points); points to sketch through are for a bar with no shape named. Give one. Nothing was placed";
    else if (boxPoints.Count < 2 || boxPoints.Count > 3)
        refused = "Shape '" + shapeAsked.Name + "' needs a box: two points (the start and end of a straight bar) or three "
            + "(the box's corner, the end of its first side, the end of its second side, at a right angle), in millimetres. "
            + boxPoints.Count + " given. Nothing was placed";
}
if (refused.Length == 0 && sketch)
{
    if (boxPoints.Count > 0)
        refused = "A box was given but no shape was named. Name the Rebar Shape to fit into it, or give the points the "
            + "bar is sketched through instead. Nothing was placed";
    else if (throughPoints.Count < 2)
        refused = "No shape was named and fewer than two points were given to sketch the bar through. Name a Rebar Shape "
            + "and its box, or give the points, in millimetres. Nothing was placed";
}

XYZ spread = null;
if (refused.Length == 0 && !isNone(spreadToward))
{
    var parts = spreadToward.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    var v = new double[3];
    var good = parts.Length == 3;
    for (var i = 0; good && i < 3; i++)
        good = double.TryParse(parts[i].Trim(), styleFloat, invariant, out v[i]) && !double.IsNaN(v[i]) && !double.IsInfinity(v[i]);
    if (good && new XYZ(v[0], v[1], v[2]).GetLength() > 1e-9) spread = new XYZ(v[0], v[1], v[2]).Normalize();
    else refused = "'" + spreadToward + "' is not a direction. Say it as three numbers, x, y, z - 1,0,0 is east. Nothing was placed";
}

// THE LAYOUT: two of spacing, count and set length, or none of them for one bar.
var layout = "single";
var spacingFeet = 0.0;
var lengthFeet = 0.0;
var positions = 1;
var hasSpacing = !isNone(spacing);
var hasCount = !isNone(count);
var hasLength = !isNone(setLength);
if (refused.Length == 0 && hasSpacing) spacingFeet = lengthFrom(spacing, "spacing");
if (refused.Length == 0 && hasLength) lengthFeet = lengthFrom(setLength, "set length");
if (refused.Length == 0 && hasCount && (!int.TryParse(count.Trim(), out positions) || positions < 1))
    refused = "'" + count + "' is not a number of bars. Nothing was placed";
if (refused.Length == 0)
{
    if (hasSpacing && hasCount && hasLength)
        refused = "Spacing, count and set length were all given, and two of them decide the third. Give two. Nothing was placed";
    else if (hasSpacing && hasLength) layout = "maximum spacing";
    else if (hasSpacing && hasCount) layout = positions == 1 ? "single" : "number with spacing";
    else if (hasCount && hasLength) layout = positions == 1 ? "single" : "fixed number";
    else if (hasSpacing)
        refused = "A spacing of " + mm(spacingFeet) + " mm was given, but not over what length or for how many bars. Say "
            + "the set length in millimetres or the number of bars. Nothing was placed";
    else if (hasLength)
        refused = "A set length was given without a spacing or a number of bars. Say one. Nothing was placed";
    else if (hasCount && positions > 1)
        refused = positions + " bars were asked without a spacing or a set length. Say one. Nothing was placed";
}
if (refused.Length == 0 && layout != "single" && spread == null)
    refused = "A set of bars spreads square to the bar's plane, to one side of the first bar, and which side was not "
        + "said. Give spreadToward as a direction, x, y, z - along the wall, for instance. Nothing was placed";

// ---- the geometry, still before anything is placed ----------------------------------
XYZ origin = null, xDir = null, yDir = null, xBox = null, yBox = null, normal = null;
var curves = new List<Curve>();
if (refused.Length == 0 && !sketch)
{
    origin = boxPoints[0];
    xBox = boxPoints[1] - origin;
    if (xBox.GetLength() < tiny) refused = "The box's first two points are the same point. Nothing was placed";
    else xDir = xBox.Normalize();
    if (refused.Length == 0 && boxPoints.Count == 3)
    {
        yBox = boxPoints[2] - origin;
        if (yBox.GetLength() < tiny) refused = "The box's first and third points are the same point. Nothing was placed";
        else if (Math.Abs(yBox.Normalize().DotProduct(xDir)) > 1e-3)
            refused = "The box's third point does not make a right angle at the first - a shape is fitted into a "
                + "rectangle. Nothing was placed";
        else yDir = yBox.Normalize();
    }
    else if (refused.Length == 0)
    {
        if (!shapeAsked.SimpleLine)
            refused = "Shape '" + shapeAsked.Name + "' is bent, and two points only say where a straight bar runs. Give a "
                + "third point - the end of the box's second side, at a right angle. Nothing was placed";
        else if (spread != null && Math.Abs(spread.DotProduct(xDir)) > 1e-3)
            refused = "spreadToward runs along the bar, and a set spreads square to it. Nothing was placed";
        else
        {
            yDir = spread != null ? spread.CrossProduct(xDir).Normalize() : squareTo(xDir).CrossProduct(xDir).Normalize();
            yBox = yDir.Multiply(modelOf(type));
        }
    }
    if (refused.Length == 0)
    {
        normal = xDir.CrossProduct(yDir).Normalize();
        if (spread != null && Math.Abs(spread.DotProduct(normal)) < 0.999)
            refused = "These box points put the bar in a plane whose square direction is " + dirText(normal) + " (or its "
                + "opposite), and a set spreads only that way - spreadToward " + dirText(spread) + " is not it. Nothing was placed";
    }
}
if (refused.Length == 0 && sketch)
{
    for (var i = 0; i + 1 < throughPoints.Count && refused.Length == 0; i++)
    {
        if (throughPoints[i].DistanceTo(throughPoints[i + 1]) < tiny)
            refused = "Points " + (i + 1) + " and " + (i + 2) + " are the same point. Nothing was placed";
        else curves.Add(Line.CreateBound(throughPoints[i], throughPoints[i + 1]));
    }
    XYZ plane = null;
    var first = throughPoints[0];
    var run = throughPoints[throughPoints.Count - 1] - first;
    if (refused.Length == 0)
    {
        var lead = (throughPoints[1] - first).Normalize();
        for (var k = 2; k < throughPoints.Count && plane == null; k++)
        {
            var c = lead.CrossProduct(throughPoints[k] - first);
            if (c.GetLength() > tiny) plane = c.Normalize();
        }
        if (plane != null && throughPoints.Any(p => Math.Abs((p - first).DotProduct(plane)) > tiny))
            refused = "The points are not in one plane, and Revit sketches a bar in one plane. Nothing was placed";
        if (refused.Length == 0)
        {
            if (spread != null)
            {
                if (plane != null && Math.Abs(spread.DotProduct(plane)) < 0.999)
                    refused = "These points lie in a plane whose square direction is " + dirText(plane) + " (or its opposite), and a "
                        + "set spreads only that way - spreadToward " + dirText(spread) + " is not it. Nothing was placed";
                else if (plane == null && Math.Abs(spread.DotProduct(run.Normalize())) > 1e-3)
                    refused = "spreadToward runs along the bar, and a set spreads square to it. Nothing was placed";
                else normal = spread;
            }
            else normal = plane ?? squareTo(run.Normalize());
        }
    }
}

// ---- place, read back, and remove again if Revit refuses any of it ----------------------
if (refused.Length == 0)
{
    Rebar bar = null;
    var expected = 1;
    try
    {
        if (sketch) bar = sketchBar(type, host, normal, curves);
        else
        {
            bar = Rebar.CreateFromRebarShape(doc, shapeAsked, type, host, origin, xDir, yDir);
            if (bar != null) bar.GetShapeDrivenAccessor().ScaleToBox(origin, xBox, yBox);
        }
        if (bar == null) throw new InvalidOperationException("Revit made no bar");
        var accessor = bar.GetShapeDrivenAccessor();
        var barNormal = accessor.Normal;
        var onNormalSide = spread == null || spread.DotProduct(barNormal) > 0;
        if (spread != null && Math.Abs(spread.DotProduct(barNormal)) < 0.999)
            throw new InvalidOperationException("the bar Revit made lies in a plane square to " + dirText(barNormal)
                + ", so it cannot spread toward " + dirText(spread));
        if (layout == "maximum spacing")
        {
            accessor.SetLayoutAsMaximumSpacing(spacingFeet, lengthFeet, onNormalSide, true, true);
            expected = (int)Math.Ceiling(lengthFeet / spacingFeet - 1e-9) + 1;
        }
        else if (layout == "number with spacing")
        {
            accessor.SetLayoutAsNumberWithSpacing(positions, spacingFeet, onNormalSide, true, true);
            expected = positions;
        }
        else if (layout == "fixed number")
        {
            accessor.SetLayoutAsFixedNumber(positions, lengthFeet, onNormalSide, true, true);
            expected = positions;
        }
        doc.Regenerate();
    }
    catch (Exception ex)
    {
        if (bar != null && bar.IsValidObject)
        {
            try { doc.Delete(bar.Id); } catch (Exception) { }
        }
        bar = null;
        refused = "Revit refused the bar in " + hostName + ": " + ex.Message + " Nothing was kept";
    }

    if (bar != null && !bar.IsValidObject)
    {
        refused = "The bar is gone after Revit regenerated " + hostName + ". Nothing was kept";
        bar = null;
    }
    if (bar != null)
    {
        placed.Add(bar.Id);
        if (bar.GetHostId() != host.Id) readBackWrong.Add("rebar " + bar.Id + " reads another host than " + hostName);
        var shapeRead = doc.GetElement(bar.GetShapeId()) as RebarShape;
        var shapeName = shapeRead == null ? "(none)" : shapeRead.Name;
        if (shapeAsked != null && bar.GetShapeId() != shapeAsked.Id)
            readBackWrong.Add("rebar " + bar.Id + " reads shape '" + shapeName + "' where '" + shapeAsked.Name + "' was asked");
        var typeRead = doc.GetElement(bar.GetTypeId());
        if (bar.GetTypeId() != type.Id)
            readBackWrong.Add("rebar " + bar.Id + " reads bar type '" + (typeRead == null ? "?" : typeRead.Name) + "' where '" + type.Name + "' was asked");
        var quantity = bar.Quantity;
        if (quantity != expected)
            readBackWrong.Add("rebar " + bar.Id + " reads " + quantity + " bar(s) where " + expected + " were expected from the "
                + layout + " layout asked");
        var spacingRead = bar.LayoutRule == RebarLayoutRule.Single ? 0.0 : bar.MaxSpacing;
        if (layout == "number with spacing" && Math.Abs(spacingRead - spacingFeet) > 0.5 / 304.8)
            readBackWrong.Add("rebar " + bar.Id + " reads spacing " + mm(spacingRead) + " mm where " + mm(spacingFeet) + " was asked");

        // THE FIRST BAR AGAINST THE HOST'S EXTENTS - Revit hosts a bar wherever it
        // is drawn, so a bar outside the concrete is named, not hidden.
        var outside = new List<string>();
        try
        {
            var centre = bar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
            var extent = host.get_BoundingBox(null);
            if (extent != null)
                foreach (var c in centre)
                    foreach (var p in new[] { c.GetEndPoint(0), c.GetEndPoint(1) })
                        if (p.X < extent.Min.X - tiny || p.Y < extent.Min.Y - tiny || p.Z < extent.Min.Z - tiny
                            || p.X > extent.Max.X + tiny || p.Y > extent.Max.Y + tiny || p.Z > extent.Max.Z + tiny)
                            outside.Add(mmPoint(p));
        }
        catch (Exception ex) { findings.Add("The bar's centre line could not be read back: " + ex.Message); }
        if (outside.Count > 0)
            readBackWrong.Add("rebar " + bar.Id + ": the first bar runs outside " + hostName + "'s extents at "
                + string.Join("; ", outside.Distinct().Take(4)) + " mm - Revit hosts it, but it is not inside the concrete");

        // THE COVER AS REVIT HOLDS IT ON THIS HOST - read, never changed.
        var covers = new List<string>();
        var faces = new[] {
            Tuple.Create(BuiltInParameter.CLEAR_COVER_TOP, "top"), Tuple.Create(BuiltInParameter.CLEAR_COVER_BOTTOM, "bottom"),
            Tuple.Create(BuiltInParameter.CLEAR_COVER_EXTERIOR, "exterior"), Tuple.Create(BuiltInParameter.CLEAR_COVER_INTERIOR, "interior"),
            Tuple.Create(BuiltInParameter.CLEAR_COVER_OTHER, "other faces") };
        foreach (var face in faces)
        {
            Parameter p = null;
            try { p = host.get_Parameter(face.Item1); } catch (Exception) { }
            var coverType = p == null ? null : doc.GetElement(p.AsElementId()) as RebarCoverType;
            if (coverType != null) covers.Add(face.Item2 + " '" + coverType.Name + "' " + mm(coverType.CoverDistance) + " mm");
        }

        report = hostName + ": rebar " + bar.Id + (sketch ? ", sketched through " + throughPoints.Count + " points" : "")
            + ", shape '" + shapeName + "', bar type '" + (typeRead == null ? "?" : typeRead.Name) + "', " + quantity + " bar(s)"
            + (bar.LayoutRule == RebarLayoutRule.Single ? " (single)" : ", " + layout + ", spacing " + mm(spacingRead) + " mm")
            + ", bar plane square to " + dirText(bar.GetShapeDrivenAccessor().Normal)
            + ", total length " + mm(bar.TotalLength) + " mm"
            + "; host cover " + (covers.Count == 0 ? "none set" : string.Join(", ", covers)) + " - not changed";
    }
}

if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add("1 rebar placed and read back in " + hostName + (readBackWrong.Count > 0 ? "; " + readBackWrong.Count + " read back other than asked" : "") + ".");
    if (sketch && spread == null && throughPoints.Count == 2)
        findings.Add("A straight bar's plane was not said; it was set square to " + dirText(normal) + ", which does not move the bar.");
    findings.AddRange(readBackWrong);
}
