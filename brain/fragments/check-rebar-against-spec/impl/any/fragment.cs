// NOT STANDALONE. Assumes `doc`, `elements`, `bottomBars`, `topBars`,
// `sideBars`, `mainBars`, `links`, `linkHook`, `cover`, `lap`, `anchorage`
// and `maxBarLength` are in scope; leaves `meets`, `doesNotMeet`,
// `notChecked`, `report`, `notHosts`, `refused` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY SPEC VALUE IS HANDED IN (D-33). Nothing here knows a design code: a
// lap or an anchorage is a length in millimetres or a multiple of the bar's
// nominal diameter ("50d"), as the spec or the modeller gives it. A value not
// given is not checked. Every outcome is one of three - meets the value
// checked, does not meet it, or NOT CHECKED with the reason - and none of
// them says the reinforcement is adequate: that is the engineer's.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;
var tolerance = 0.5 / 304.8;   // half a millimetre: what Revit's own rounding of a length can hide
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
Func<Element, BuiltInParameter, Parameter> param = (e, bip) =>
{
    try { return e.get_Parameter(bip); } catch (Exception) { return null; }
};
Func<Element, string> label = e => (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";

var meets = 0;
var doesNotMeet = 0;
var notChecked = 0;
var notHosts = new List<string>();
var findings = new List<string>();
var refused = "";
var hostLines = new List<string>();
var failedLines = new List<string>();

// ---- the spec values, read once, before any host -------------------------------------
var barTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
Func<string> barList = () => barTypes.Count == 0 ? "none" : string.Join(", ", barTypes.OrderBy(t => nominalOf(t))
    .Select(t => "'" + t.Name + "' " + mm(nominalOf(t)) + " mm"));
Func<string, bool> notGiven = text =>
{
    var s = (text ?? "").Trim();
    return s.Length == 0 || s.Equals("not given", StringComparison.OrdinalIgnoreCase);
};
Func<string, string> stripMm = text =>
{
    var s = (text ?? "").Trim();
    return s.EndsWith("mm", StringComparison.OrdinalIgnoreCase) ? s.Substring(0, s.Length - 2).Trim() : s;
};
// A plain number, or NaN. double.TryParse takes "NaN", "Infinity" and "∞"
// as numbers; none of them is a spec value, so every parser below reads
// through this one and refuses them in words.
Func<string, double> numberOf = text =>
{
    double value;
    if (!double.TryParse((text ?? "").Trim(), styleFloat, invariant, out value) || double.IsNaN(value) || double.IsInfinity(value))
        return double.NaN;
    return value;
};

// A spec bar: a Rebar Bar Type by its exact name, or a nominal diameter in
// millimetres - any bar type of that diameter meets it. A word that is
// neither refuses the call: what "T16" means is the modeller's to say (D-34).
Func<string, string, Tuple<string, Func<RebarBarType, bool>>> specBar = (text, role) =>
{
    var asked = (text ?? "").Trim();
    var named = barTypes.FirstOrDefault(t => string.Equals(t.Name, asked, StringComparison.Ordinal));
    if (named != null)
        return Tuple.Create("'" + named.Name + "'", (Func<RebarBarType, bool>)(t => t != null && t.Id == named.Id));
    var diameter = numberOf(stripMm(asked));
    if (double.IsNaN(diameter) || diameter <= 0)
    {
        refused = "No Rebar Bar Type is named '" + asked + "' (the " + role + " in the spec). Loaded: " + barList()
            + ". Give one of these names, or the bar's diameter in millimetres - which type a spec word such as "
            + "T16 or H16 means is the modeller's to say. Nothing was checked";
        return null;
    }
    var feet = diameter / 304.8;
    return Tuple.Create(diameter.ToString("0.#", invariant) + " mm",
        (Func<RebarBarType, bool>)(t => t != null && Math.Abs(nominalOf(t) - feet) < 0.05 / 304.8));
};
// "<count> x <bar type>", or "none" when the spec says there are none.
Func<string, string, Tuple<int, string, Func<RebarBarType, bool>>> specCount = (text, role) =>
{
    var asked = (text ?? "").Trim();
    if (asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return Tuple.Create(0, "none", (Func<RebarBarType, bool>)null);
    var cut = asked.IndexOfAny(new[] { 'x', 'X', '*' });
    int count;
    if (cut <= 0 || !int.TryParse(asked.Substring(0, cut).Trim(), out count) || count < 1)
    {
        refused = "'" + asked + "' is not a bar count and type (the " + role + "). Say it as <count> x <bar type> - "
            + "4 x 16mm, or 4 x T16 when a bar type is named T16 - or none. Nothing was checked";
        return null;
    }
    var bar = specBar(asked.Substring(cut + 1), role);
    return bar == null ? null : Tuple.Create(count, bar.Item1, bar.Item2);
};
// "<bar type> @ <maximum spacing in mm>", or "none".
Func<string, Tuple<double, string, Func<RebarBarType, bool>>> specLinks = text =>
{
    var asked = (text ?? "").Trim();
    if (asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return Tuple.Create(0.0, "none", (Func<RebarBarType, bool>)null);
    var at = asked.LastIndexOf('@');
    var spacing = at <= 0 ? double.NaN : numberOf(stripMm(asked.Substring(at + 1)));
    if (double.IsNaN(spacing) || spacing <= 0)
    {
        refused = "'" + asked + "' is not a link bar type and spacing. Say it as <bar type> @ <largest spacing in mm> "
            + "- 10mm @ 150 - or none. Nothing was checked";
        return null;
    }
    var bar = specBar(asked.Substring(0, at), "links");
    return bar == null ? null : Tuple.Create(spacing / 304.8, bar.Item1, bar.Item2);
};
// A length in millimetres, or a multiple of the bar's diameter: "50d".
// Item1 is the multiple (0 when a length was given), Item2 the length in feet.
Func<string, string, Tuple<double, double>> specLength = (text, role) =>
{
    var asked = (text ?? "").Trim().ToLowerInvariant().Replace(" ", "");
    if (asked.EndsWith("d") || asked.EndsWith("ø") || asked.EndsWith("φ"))
    {
        var multiple = numberOf(asked.Substring(0, asked.Length - 1).TrimEnd('x', '*'));
        if (!double.IsNaN(multiple) && multiple > 0) return Tuple.Create(multiple, 0.0);
    }
    else
    {
        var millimetres = numberOf(stripMm(asked));
        if (!double.IsNaN(millimetres) && millimetres > 0) return Tuple.Create(0.0, millimetres / 304.8);
    }
    refused = "'" + text + "' is not a " + role + " - give it in millimetres (600) or as a multiple of the bar "
        + "diameter (40d), as the spec writes it. Nothing was checked";
    return null;
};
Func<Tuple<double, double>, string> lengthText = spec => spec.Item1 > 0
    ? spec.Item1.ToString("0.#", invariant) + "d" : mm(spec.Item2) + " mm";
// The length a "50d" asks of a bar of nominal diameter d, in feet.
Func<Tuple<double, double>, double, double> required = (spec, d) => spec.Item1 > 0 ? spec.Item1 * d : spec.Item2;

Tuple<int, string, Func<RebarBarType, bool>> specBottom = null, specTop = null, specSide = null, specMain = null;
Tuple<double, string, Func<RebarBarType, bool>> specTies = null;
Tuple<double, double> specLap = null, specAnchorage = null;
var specHookDegrees = double.NaN;
var specMaxLength = double.NaN;
var specCover = new Dictionary<BuiltInParameter, double>();
var coverAllFaces = double.NaN;

if (!notGiven(bottomBars)) specBottom = specCount(bottomBars, "bottom bars");
if (refused.Length == 0 && !notGiven(topBars)) specTop = specCount(topBars, "top bars");
if (refused.Length == 0 && !notGiven(sideBars)) specSide = specCount(sideBars, "side face bars on each face");
if (refused.Length == 0 && !notGiven(mainBars)) specMain = specCount(mainBars, "main bars");
if (refused.Length == 0 && !notGiven(links)) specTies = specLinks(links);
if (refused.Length == 0 && !notGiven(linkHook))
{
    // 135, 135 deg, 135 degree, 135 degrees, 135° - a number and nothing else.
    var hookWords = System.Text.RegularExpressions.Regex.Match((linkHook ?? "").Trim(),
        @"^(\d+(?:\.\d+)?)\s*(?:°|º|deg\.?|degrees?)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var angle = hookWords.Success ? numberOf(hookWords.Groups[1].Value) : double.NaN;
    if (double.IsNaN(angle) || angle <= 0 || angle > 180)
        refused = "'" + linkHook + "' is not a hook angle - give the links' hook in degrees, 135 or 90. Nothing was checked";
    else specHookDegrees = angle;
}
if (refused.Length == 0 && !notGiven(cover))
{
    // One number for every face, or faces named: "top 40, bottom 75, other 50".
    var text = (cover ?? "").Trim();
    var all = numberOf(stripMm(text));
    if (!double.IsNaN(all) && all >= 0) coverAllFaces = all / 304.8;
    else
    {
        var faces = new Dictionary<string, BuiltInParameter>(StringComparer.OrdinalIgnoreCase)
        {
            { "top", BuiltInParameter.CLEAR_COVER_TOP }, { "bottom", BuiltInParameter.CLEAR_COVER_BOTTOM },
            { "other", BuiltInParameter.CLEAR_COVER_OTHER }, { "sides", BuiltInParameter.CLEAR_COVER_OTHER },
            { "exterior", BuiltInParameter.CLEAR_COVER_EXTERIOR }, { "interior", BuiltInParameter.CLEAR_COVER_INTERIOR }
        };
        foreach (var piece in text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var words = piece.Trim().Split(new[] { ' ', '=', ':' }, StringSplitOptions.RemoveEmptyEntries);
            var value = words.Length == 2 ? numberOf(stripMm(words[1])) : double.NaN;
            BuiltInParameter face;
            if (words.Length != 2 || !faces.TryGetValue(words[0], out face) || double.IsNaN(value) || value < 0)
            {
                refused = "'" + text + "' is not a cover. Give one number in millimetres for every face (40), or "
                    + "name the faces: top 40, bottom 75, other 50 (exterior and interior on a wall). Nothing was checked";
                break;
            }
            specCover[face] = value / 304.8;
        }
    }
}
if (refused.Length == 0 && !notGiven(lap)) specLap = specLength(lap, "lap length");
if (refused.Length == 0 && !notGiven(anchorage)) specAnchorage = specLength(anchorage, "anchorage length");
if (refused.Length == 0 && !notGiven(maxBarLength))
{
    var text = (maxBarLength ?? "").Trim().ToLowerInvariant().Replace(" ", "");
    var inMm = text.EndsWith("mm") ? numberOf(text.Substring(0, text.Length - 2)) : double.NaN;
    var inM = !text.EndsWith("mm") && text.EndsWith("m") ? numberOf(text.Substring(0, text.Length - 1)) : double.NaN;
    var bare = numberOf(text);
    if (!double.IsNaN(inMm) && inMm > 0) specMaxLength = inMm / 304.8;
    else if (!double.IsNaN(inM) && inM > 0) specMaxLength = inM * 1000 / 304.8;
    else if (!double.IsNaN(bare) && bare >= 100) specMaxLength = bare / 304.8;
    else
        refused = "'" + maxBarLength + "' is not a bar length - say 12 m or 12000 mm; a bare number is millimetres "
            + "and one below 100 is not taken. Nothing was checked";
}
if (refused.Length == 0 && specBottom == null && specTop == null && specSide == null && specMain == null && specTies == null
    && double.IsNaN(specHookDegrees) && double.IsNaN(coverAllFaces) && specCover.Count == 0 && specLap == null
    && specAnchorage == null && double.IsNaN(specMaxLength))
    refused = "No spec value was given, so there is nothing to check against. Give the values the spec states - bar "
        + "counts and types, links, cover, lap, anchorage, the longest bar - each one confirmed by the modeller. "
        + "Nothing was checked";

// ---- reading the model ---------------------------------------------------------------
// Every solid of an element, through nested instances.
Func<Element, List<Solid>> solidsOf = element =>
{
    var found = new List<Solid>();
    Action<GeometryElement> walk = null;
    walk = geometry =>
    {
        if (geometry == null) return;
        foreach (GeometryObject item in geometry)
        {
            var solid = item as Solid;
            if (solid != null && solid.Volume > 1e-9) { found.Add(solid); continue; }
            var nested = item as GeometryInstance;
            if (nested != null) walk(nested.GetInstanceGeometry());
        }
    };
    try { walk(element.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false })); }
    catch (Exception) { }
    return found;
};
// [zmin, zmax] of the solids along a frame's Z, or null.
Func<List<Solid>, Transform, double[]> heightIn = (solids, frame) =>
{
    var inverse = frame.Inverse;
    double low = double.MaxValue, high = double.MinValue;
    foreach (var solid in solids)
        foreach (Edge edge in solid.Edges)
            foreach (var point in edge.Tessellate())
            {
                var z = inverse.OfPoint(point).Z;
                low = Math.Min(low, z);
                high = Math.Max(high, z);
            }
    return high > low ? new[] { low, high } : null;
};
// Each bar of a set that exists, as its centreline in the model. Null when
// the positions cannot be read. `plain` drops hooks and bend radii, so a
// bent bar is its straight legs.
Func<Rebar, bool, List<IList<Curve>>> positionsOf = (bar, plain) =>
{
    try
    {
        var found = new List<IList<Curve>>();
        var indexes = new List<int>();
        for (var i = 0; i < bar.NumberOfBarPositions; i++)
        {
            if (!bar.DoesBarExistAtPosition(i)) continue;
#if REVIT2020 || REVIT2021
            var curves = bar.GetCenterlineCurves(false, plain, plain, MultiplanarOption.IncludeAllMultiplanarCurves, i);
#else
            var curves = bar.GetTransformedCenterlineCurves(false, plain, plain, MultiplanarOption.IncludeAllMultiplanarCurves, i);
#endif
            if (curves == null || curves.Count == 0) continue;
            found.Add(curves);
            indexes.Add(i);
        }
#if REVIT2020 || REVIT2021
        // On 2020 and 2021 GetCenterlineCurves gives the set's FIRST bar at
        // every index (the API's own remarks), so each bar of a shape-driven
        // set is put in its place by the set's position transform, always.
        // A free-form set's bars are each their own curves. MZ2 confirms it.
        if (bar.IsRebarShapeDriven())
        {
            var accessor = bar.GetShapeDrivenAccessor();
            for (var k = 0; k < found.Count; k++)
            {
                var move = accessor.GetBarPositionTransform(indexes[k]);
                found[k] = found[k].Select(c => c.CreateTransformed(move)).ToList();
            }
        }
#else
        // GetTransformedCenterlineCurves places each bar. Should a release give
        // every position on top of the first, the position transform places
        // them; a set that cannot be placed is not read (MZ10).
        if (found.Count > 1 && found[0][0].GetEndPoint(0).IsAlmostEqualTo(found[found.Count - 1][0].GetEndPoint(0)))
        {
            if (!bar.IsRebarShapeDriven()) return null;
            var accessor = bar.GetShapeDrivenAccessor();
            for (var k = 0; k < found.Count; k++)
            {
                var move = accessor.GetBarPositionTransform(indexes[k]);
                found[k] = found[k].Select(c => c.CreateTransformed(move)).ToList();
            }
        }
#endif
        return found;
    }
    catch (Exception) { return null; }
};
// The widest centre-to-centre gap between neighbouring bars of a set that
// exist, in feet - NaN when it cannot be measured. NOT the Spacing parameter:
// under Minimum Clear Spacing that is the CLEAR gap, so a set too wide apart
// would read as meeting. A shape-driven set: the distance between the
// origins of neighbouring bars' position transforms. A free-form set: between
// the starts of neighbouring bars' centrelines. On 2027 Revit's own
// GetSpacingsBetweenBars is read too and the larger is kept.
Func<Rebar, double> widestGapOf = bar =>
{
    try
    {
        var indexes = new List<int>();
        for (var i = 0; i < bar.NumberOfBarPositions; i++) if (bar.DoesBarExistAtPosition(i)) indexes.Add(i);
        if (indexes.Count < 2) return double.NaN;
        var widest = 0.0;
        if (bar.IsRebarShapeDriven())
        {
            var accessor = bar.GetShapeDrivenAccessor();
            var previous = accessor.GetBarPositionTransform(indexes[0]).Origin;
            for (var k = 1; k < indexes.Count; k++)
            {
                var here = accessor.GetBarPositionTransform(indexes[k]).Origin;
                widest = Math.Max(widest, here.DistanceTo(previous));
                previous = here;
            }
        }
        else
        {
            var placed = positionsOf(bar, true);
            if (placed == null || placed.Count < 2) return double.NaN;
            for (var k = 1; k < placed.Count; k++)
                widest = Math.Max(widest, placed[k][0].GetEndPoint(0).DistanceTo(placed[k - 1][0].GetEndPoint(0)));
        }
#if REVIT2027_OR_GREATER
        try
        {
            var gaps = bar.GetSpacingsBetweenBars();
            if (gaps != null) foreach (var gap in gaps) if (!double.IsNaN(gap)) widest = Math.Max(widest, gap);
        }
        catch (Exception) { }
#endif
        // Bars on top of each other are not a spacing that was measured.
        return widest > 1 / 304.8 ? widest : double.NaN;
    }
    catch (Exception) { return double.NaN; }
};
// One bar's length as Revit gives it: the longest bar of a set whose bars
// vary, else Bar Length, else the set's total over its quantity.
Func<Element, int, double, double> barLengthOf = (bar, quantity, total) =>
{
    var longest = 0.0;
    foreach (var bip in new[] { BuiltInParameter.REBAR_MAX_LENGTH, BuiltInParameter.REBAR_ELEM_LENGTH })
    {
        var p = param(bar, bip);
        if (p != null && p.StorageType == StorageType.Double) longest = Math.Max(longest, p.AsDouble());
    }
    if (longest <= 0 && quantity > 0 && total > 0) longest = total / quantity;
    return longest;
};
Func<Element, int, string> hookAt = (bar, end) =>
{
    try
    {
        var id = bar is Rebar ? ((Rebar)bar).GetHookTypeId(end) : ((RebarInSystem)bar).GetHookTypeId(end);
        var hook = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id) as RebarHookType;
        return hook == null ? "none" : "'" + hook.Name + "' " + Math.Round(hook.HookAngle * 180 / Math.PI).ToString("0", invariant) + " deg";
    }
    catch (Exception) { return "not read"; }
};
// The length of a centreline outside the host's concrete.
Func<IList<Curve>, List<Solid>, double> outsideOf = (curves, solids) =>
{
    var outside = 0.0;
    var options = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside };
    foreach (var curve in curves)
    {
        var inside = 0.0;
        foreach (var solid in solids)
        {
            var cut = solid.IntersectWithCurve(curve, options);
            for (var s = 0; s < cut.SegmentCount; s++) inside += cut.GetCurveSegment(s).Length;
        }
        outside += Math.Max(0, curve.Length - inside);
    }
    return outside;
};

var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);
var columnCategory = new ElementId(BuiltInCategory.OST_StructuralColumns);
var coverFaces = new[]
{
    BuiltInParameter.CLEAR_COVER_TOP, BuiltInParameter.CLEAR_COVER_BOTTOM, BuiltInParameter.CLEAR_COVER_OTHER,
    BuiltInParameter.CLEAR_COVER_EXTERIOR, BuiltInParameter.CLEAR_COVER_INTERIOR
};

if (refused.Length == 0)
{
    foreach (var host in elements)
    {
        if (host == null) continue;
        var name = label(host);
        if (!RebarHostData.IsValidHost(host))
        {
            notHosts.Add(name + " is not a rebar host - Revit hosts rebar only in concrete structural columns, "
                + "framing, foundations, floors and walls, and in a structural part. Nothing was checked in it");
            continue;
        }
        var data = RebarHostData.GetRebarHostData(host);
        var kind = host.Category == null ? "other" : host.Category.Id == framingCategory ? "beam"
            : host.Category.Id == columnCategory ? "column" : "other";
        var lines = new List<string>();
        Action<string, string, string, int, string> check = (what, read, spec, outcome, why) =>
        {
            var word = outcome > 0 ? "meets" : outcome == 0 ? "DOES NOT MEET" : "NOT CHECKED";
            if (outcome > 0) meets++; else if (outcome == 0) doesNotMeet++; else notChecked++;
            var line = what + ": read " + read + "; spec " + spec + " - " + word + (string.IsNullOrEmpty(why) ? "" : " (" + why + ")");
            lines.Add(line);
            if (outcome == 0) failedLines.Add(name + " - " + line);
        };

        var rebars = data.GetRebarsInHost().ToList();
        var inSystem = new List<RebarInSystem>();
        foreach (var area in data.GetAreaReinforcementsInHost())
            foreach (var id in area.GetRebarInSystemIds()) { var bar = doc.GetElement(id) as RebarInSystem; if (bar != null) inSystem.Add(bar); }
        foreach (var path in data.GetPathReinforcementsInHost())
            foreach (var id in path.GetRebarInSystemIds()) { var bar = doc.GetElement(id) as RebarInSystem; if (bar != null) inSystem.Add(bar); }
        Func<Element, RebarBarType> typeOf = bar => doc.GetElement(bar.GetTypeId()) as RebarBarType;
        Func<Rebar, bool> isTie = bar =>
        {
            RebarShape shape = null;
            try { shape = doc.GetElement(bar.GetShapeId()) as RebarShape; } catch (Exception) { }
            return shape != null && shape.RebarStyle == RebarStyle.StirrupTie;
        };
        // Each set's bars read ONCE per host, hooks and bends dropped, and
        // shared by the position, spacing and lap checks.
        var plainCache = new Dictionary<ElementId, List<IList<Curve>>>();
        Func<Rebar, List<IList<Curve>>> plainOf = bar =>
        {
            List<IList<Curve>> got;
            if (!plainCache.TryGetValue(bar.Id, out got)) { got = positionsOf(bar, true); plainCache[bar.Id] = got; }
            return got;
        };
        var ties = rebars.Where(b => isTie(b)).ToList();
        var standard = rebars.Where(b => !isTie(b)).ToList();
        lines.Add(rebars.Count + " rebar set(s) (" + ties.Count + " stirrup/tie), " + inSystem.Count + " bar group(s) of area or path reinforcement");

        // ---- bar count and type by position: beams and columns ----------------------
        Func<List<Tuple<Rebar, IList<Curve>>>, Tuple<int, string, Func<RebarBarType, bool>>, string, string, bool> countCheck =
            (bars, spec, what, extra) =>
        {
            var byType = bars.GroupBy(b => b.Item1.GetTypeId()).Select(g =>
            {
                var t = doc.GetElement(g.Key) as RebarBarType;
                return g.Count() + " x " + (t == null ? "(no bar type)" : "'" + t.Name + "' " + mm(nominalOf(t)) + " mm");
            }).ToList();
            var read = bars.Count == 0 ? "none" : string.Join(", ", byType);
            var specText = spec.Item1 == 0 ? "none" : spec.Item1 + " x " + spec.Item2;
            if (spec.Item1 == 0)
            {
                check(what, read, specText, bars.Count == 0 ? 1 : 0, bars.Count == 0 ? extra : "the spec has none here");
                return true;
            }
            var wrongType = bars.Where(b => !spec.Item3(typeOf(b.Item1))).Count();
            var reasons = new List<string>();
            if (bars.Count != spec.Item1) reasons.Add(bars.Count + " bar(s) where the spec has " + spec.Item1);
            if (wrongType > 0) reasons.Add(wrongType + " bar(s) not " + spec.Item2);
            check(what, read, specText, reasons.Count == 0 ? 1 : 0, reasons.Count == 0 ? extra : string.Join("; ", reasons) + (string.IsNullOrEmpty(extra) ? "" : "; " + extra));
            return true;
        };
        var positionSpecs = new[] { Tuple.Create(specBottom, "bottom bars"), Tuple.Create(specTop, "top bars"), Tuple.Create(specSide, "side face bars"), Tuple.Create(specMain, "main bars") }
            .Where(s => s.Item1 != null).ToList();
        var unreadSets = new List<string>();
        var placedBars = new List<Tuple<Rebar, IList<Curve>>>();
        if (positionSpecs.Count > 0 && kind != "other")
            foreach (var bar in standard)
            {
                var at = plainOf(bar);
                if (at == null) { unreadSets.Add("id " + bar.Id); continue; }
                foreach (var curves in at) placedBars.Add(Tuple.Create(bar, curves));
            }
        var unreadNote = unreadSets.Count == 0 ? "" : "the bars of " + unreadSets.Count + " set(s) could not be placed by position: " + string.Join(", ", unreadSets.Take(5));

        if (kind == "beam" && positionSpecs.Count > 0)
        {
            // The beam's own frame: X along it, Y level across it, Z up through
            // it. A bar's place is the middle of its longest straight leg.
            var located = host.Location as LocationCurve;
            var axis = located == null ? null : located.Curve as Line;
            double[] height = null;
            Transform frame = null;
            if (axis != null && Math.Abs(axis.Direction.Normalize().Z) < 0.999)
            {
                var xAxis = axis.Direction.Normalize();
                var yAxis = XYZ.BasisZ.CrossProduct(xAxis).Normalize();
                frame = Transform.Identity;
                frame.Origin = axis.GetEndPoint(0);
                frame.BasisX = xAxis;
                frame.BasisY = yAxis;
                frame.BasisZ = xAxis.CrossProduct(yAxis).Normalize();
                height = heightIn(solidsOf(host), frame);
            }
            if (height == null)
            {
                foreach (var spec in positionSpecs)
                    check(spec.Item2, "not read", spec.Item1.Item1 == 0 ? "none" : spec.Item1.Item1 + " x " + spec.Item1.Item2, -1,
                        "the beam is not straight, or has no solid, so its top and bottom cannot be told apart here");
            }
            else
            {
                var bottom = new List<Tuple<Rebar, IList<Curve>>>();
                var top = new List<Tuple<Rebar, IList<Curve>>>();
                var side = new List<Tuple<Rebar, IList<Curve>>>();
                var inverse = frame.Inverse;
                // A set whose bars are spread up the beam's depth more than
                // across it is a side face set, every bar of it - PLACE_BEAM_REBAR
                // lays side face bars so. A set spread ACROSS the beam is a
                // layer: below half the depth bottom, above it top - so a second
                // bottom layer in a shallow beam is still bottom. Only a bar on
                // its own goes by thirds: lowest bottom, highest top, between
                // them side face.
                foreach (var set in placedBars.GroupBy(b => b.Item1.Id))
                {
                    var middles = set.Select(b => inverse.OfPoint(b.Item2.OrderByDescending(c => c.Length).First().Evaluate(0.5, true))).ToList();
                    var rise = middles.Max(m => m.Z) - middles.Min(m => m.Z);
                    var across = middles.Max(m => m.Y) - middles.Min(m => m.Y);
                    var upright = middles.Count > 1 && rise > 1 / 304.8 && rise > across;
                    var alone = middles.Count == 1;
                    var k = 0;
                    foreach (var placed in set)
                    {
                        var share = (middles[k++].Z - height[0]) / (height[1] - height[0]);
                        var zone = upright ? side
                            : alone ? (share < 1.0 / 3 ? bottom : share > 2.0 / 3 ? top : side)
                            : (share < 0.5 ? bottom : top);
                        zone.Add(placed);
                    }
                }
                foreach (var spec in positionSpecs)
                {
                    if (unreadSets.Count > 0)
                    {
                        check(spec.Item2, "not read", spec.Item1.Item1 + " x " + spec.Item1.Item2, -1, unreadNote);
                        continue;
                    }
                    if (spec.Item2 == "bottom bars") countCheck(bottom, spec.Item1, "bottom bars", "");
                    else if (spec.Item2 == "top bars") countCheck(top, spec.Item1, "top bars", "");
                    else if (spec.Item2 == "side face bars")
                    {
                        // The spec's count is on EACH face, as PLACE_BEAM_REBAR takes it.
                        var both = Tuple.Create(spec.Item1.Item1 * 2, spec.Item1.Item2, spec.Item1.Item3);
                        countCheck(side, both, "side face bars, both faces", "the spec's " + spec.Item1.Item1 + " on each face");
                    }
                    else check("main bars", "-", spec.Item1.Item1 + " x " + spec.Item1.Item2, -1,
                        "main bars are a column's; a beam's are given as bottom, top and side face bars");
                }
                // Never silent: bars between the layers that no value given checks.
                if (specSide == null && unreadSets.Count == 0 && side.Count > 0)
                    check("bars between the layers", side.Count + " bar(s)", "side face bars not given", -1,
                        side.Count + " bars between the layers not counted - give sideBars (on each face) to check them");
            }
        }
        else if (kind == "column" && positionSpecs.Count > 0)
        {
            foreach (var spec in positionSpecs)
            {
                if (spec.Item2 != "main bars")
                    check(spec.Item2, "-", spec.Item1.Item1 + " x " + spec.Item1.Item2, -1,
                        "a column's bars are given as main bars; top, bottom and side face bars are a beam's");
                else if (unreadSets.Count > 0)
                    check("main bars", "not read", spec.Item1.Item1 + " x " + spec.Item1.Item2, -1, unreadNote);
                else countCheck(placedBars, spec.Item1, "main bars", "every bar that is not a tie counts as a main bar");
            }
        }
        else if (positionSpecs.Count > 0)
        {
            foreach (var spec in positionSpecs)
                check(spec.Item2, "-", spec.Item1.Item1 == 0 ? "none" : spec.Item1.Item1 + " x " + spec.Item1.Item2, -1,
                    "bars are sorted by position in beams and columns only; REPORT_HOST_REBAR lists this host's sets and layers");
        }

        // ---- links: bar type, largest spacing, hooks ---------------------------------
        if (specTies != null || !double.IsNaN(specHookDegrees))
        {
            var readTies = ties.Count == 0 ? "none" : string.Join(", ", ties.Select(t =>
            {
                var type = typeOf(t);
                return t.Quantity + " x " + (type == null ? "(no bar type)" : "'" + type.Name + "'") + " (id " + t.Id + ")";
            }));
            if (kind == "other")
            {
                if (specTies != null) check("links", readTies, specTies.Item1 == 0 ? "none" : specTies.Item2 + " @ " + mm(specTies.Item1) + " mm max", -1,
                    "links are checked in beams and columns only");
                if (!double.IsNaN(specHookDegrees)) check("link hooks", "-", specHookDegrees.ToString("0", invariant) + " deg", -1,
                    "links are checked in beams and columns only");
            }
            else
            {
                if (specTies != null && specTies.Item1 == 0)
                    check("links", readTies, "none", ties.Count == 0 ? 1 : 0, ties.Count == 0 ? "" : "the spec has none here");
                else if (specTies != null)
                {
                    if (ties.Count == 0) check("links", "none", specTies.Item2 + " @ " + mm(specTies.Item1) + " mm max", 0, "no stirrup or tie in this host");
                    else
                    {
                        var wrongType = ties.Where(t => !specTies.Item3(typeOf(t))).Select(t => "id " + t.Id).ToList();
                        check("link bar type", readTies, specTies.Item2, wrongType.Count == 0 ? 1 : 0,
                            wrongType.Count == 0 ? "" : "not " + specTies.Item2 + ": " + string.Join(", ", wrongType.Take(5)));
                        var over = new List<string>();
                        var unread = new List<string>();
                        var widest = 0.0;
                        foreach (var t in ties)
                        {
                            if (t.LayoutRule == RebarLayoutRule.Single) { unread.Add("id " + t.Id + " (one tie on its own)"); continue; }
                            var spacing = widestGapOf(t);
                            if (double.IsNaN(spacing)) { unread.Add("id " + t.Id); continue; }
                            widest = Math.Max(widest, spacing);
                            if (spacing > specTies.Item1 + tolerance) over.Add("id " + t.Id + " at " + mm(spacing) + " mm");
                        }
                        var readSpacing = widest > 0 ? "widest centre-to-centre " + mm(widest) + " mm" : "none measured";
                        if (over.Count > 0) check("link spacing", readSpacing, mm(specTies.Item1) + " mm max", 0, "wider: " + string.Join(", ", over.Take(5)));
                        else if (unread.Count > 0) check("link spacing", readSpacing, mm(specTies.Item1) + " mm max", -1,
                            "the centre-to-centre spacing of " + string.Join(", ", unread.Take(5)) + " could not be measured - a tie on its own has none, and the gaps between separate ties are not measured");
                        else check("link spacing", readSpacing, mm(specTies.Item1) + " mm max", 1, "");
                    }
                }
                if (!double.IsNaN(specHookDegrees))
                {
                    if (ties.Count == 0) check("link hooks", "no links", specHookDegrees.ToString("0", invariant) + " deg", -1, "no stirrup or tie in this host");
                    else
                    {
                        var wrong = new List<string>();
                        var read = new List<string>();
                        foreach (var t in ties)
                        {
                            var ends = new[] { hookAt(t, 0), hookAt(t, 1) };
                            read.Add("id " + t.Id + " " + ends[0] + " / " + ends[1]);
                            for (var end = 0; end < 2; end++)
                            {
                                RebarHookType hook = null;
                                try { hook = doc.GetElement(t.GetHookTypeId(end)) as RebarHookType; } catch (Exception) { }
                                if (hook == null || Math.Abs(hook.HookAngle * 180 / Math.PI - specHookDegrees) > 0.5)
                                {
                                    wrong.Add("id " + t.Id + (end == 0 ? " start" : " end"));
                                }
                            }
                        }
                        check("link hooks", string.Join(", ", read.Take(5)), specHookDegrees.ToString("0", invariant) + " deg at both ends",
                            wrong.Count == 0 ? 1 : 0, wrong.Count == 0 ? "" : "other or none: " + string.Join(", ", wrong.Take(6)));
                    }
                }
            }
        }

        // ---- cover: the host's own Rebar Cover SETTING, face by face -------------------
        // What is compared is the cover type set on the host - what Revit places
        // new bars to and what a schedule or tag reads. How far each bar
        // actually sits from the concrete face is NOT measured here (it needs
        // each face of the solid told apart on every kind of host), and each
        // line says so; Revit's own warning for a bar inside the cover is the
        // other route.
        var notMeasured = "the bars' own distance to the face is not measured";
        if (!double.IsNaN(coverAllFaces) || specCover.Count > 0)
        {
            var anyFace = false;
            foreach (var bip in coverFaces)
            {
                var p = param(host, bip);
                var wanted = !double.IsNaN(coverAllFaces) ? coverAllFaces : specCover.ContainsKey(bip) ? specCover[bip] : double.NaN;
                if (p == null || p.StorageType != StorageType.ElementId)
                {
                    if (specCover.ContainsKey(bip))
                        check("host cover setting, " + bip.ToString().Replace("CLEAR_COVER_", "").ToLowerInvariant() + " face", "-", mm(wanted) + " mm", -1,
                            "this host has no cover parameter for that face; " + notMeasured);
                    continue;
                }
                anyFace = true;
                if (double.IsNaN(wanted)) continue;
                var coverType = doc.GetElement(p.AsElementId()) as RebarCoverType;
                if (coverType == null) { check("host cover setting, " + p.Definition.Name, "no cover type", mm(wanted) + " mm", 0, "no cover is set on this face; " + notMeasured); continue; }
                var held = coverType.CoverDistance;
                var same = Math.Abs(held - wanted) <= tolerance;
                check("host cover setting, " + p.Definition.Name, "'" + coverType.Name + "' " + mm(held) + " mm", mm(wanted) + " mm", same ? 1 : 0,
                    (same ? "" : (held < wanted ? "less" : "more") + " than the spec's; ") + notMeasured);
            }
            if (!anyFace) check("host cover setting", "-", cover, -1, "this host has no Rebar Cover parameters; " + notMeasured);
        }

        // ---- the longest bar ----------------------------------------------------------
        if (!double.IsNaN(specMaxLength))
        {
            var lengths = rebars.Select(b => Tuple.Create((Element)b, barLengthOf(b, b.Quantity, b.TotalLength)))
                .Concat(inSystem.Select(b => Tuple.Create((Element)b, barLengthOf(b, b.Quantity, 0.0)))).ToList();
            var known = lengths.Where(l => l.Item2 > 0).ToList();
            if (lengths.Count == 0) check("longest bar", "no bars", mm(specMaxLength) + " mm max", -1, "no bars in this host");
            else
            {
                var longest = known.Count == 0 ? 0 : known.Max(l => l.Item2);
                var over = known.Where(l => l.Item2 > specMaxLength + tolerance).Select(l => "id " + l.Item1.Id + " " + mm(l.Item2) + " mm").ToList();
                var unknown = lengths.Count - known.Count;
                if (over.Count > 0) check("longest bar", mm(longest) + " mm", mm(specMaxLength) + " mm max", 0, "longer: " + string.Join(", ", over.Take(5)));
                else if (unknown > 0) check("longest bar", known.Count == 0 ? "not read" : mm(longest) + " mm", mm(specMaxLength) + " mm max", -1,
                    unknown + " set(s) whose bar length Revit does not give");
                else check("longest bar", mm(longest) + " mm", mm(specMaxLength) + " mm max", 1, "");
            }
        }

        // ---- laps: straight legs of two sets, parallel, touching, ends past each other ---
        if (specLap != null)
        {
            var lapScope = "a lap is read only between straight, parallel bars of two sets in this one host that touch "
                + "and end past each other; a lap into another element, a spaced lap and a coupler are not read";
            // Every straight leg, copied ONCE into arrays: its set, start, unit
            // direction, length, model radius, bar type, and its box padded.
            var legSet = new List<int>();
            var legType = new List<RebarBarType>();
            var legLine = new List<Line>();
            var lapUnread = new List<string>();
            for (var si = 0; si < standard.Count; si++)
            {
                var at = plainOf(standard[si]);
                if (at == null) { lapUnread.Add("id " + standard[si].Id); continue; }
                var type = typeOf(standard[si]);
                if (type == null) { lapUnread.Add("id " + standard[si].Id + " (no bar type)"); continue; }
                foreach (var curves in at)
                    foreach (var curve in curves)
                    {
                        var line = curve as Line;
                        if (line != null && line.Length > 1e-6) { legSet.Add(si); legType.Add(type); legLine.Add(line); }
                    }
            }
            var unreadText = lapUnread.Count == 0 ? "" : "the bars of " + lapUnread.Count + " set(s) could not be read, so a lap in them is not known: "
                + string.Join(", ", lapUnread.Take(5)) + "; ";
            var n = legLine.Count;
            if (n > 20000)
                check("laps", n + " straight legs", lengthText(specLap), -1, "too many bars in one host to compare in one call; " + lapScope);
            else
            {
                var sx = new double[n]; var sy = new double[n]; var sz = new double[n];
                var dx = new double[n]; var dy = new double[n]; var dz = new double[n];
                var len = new double[n]; var radius = new double[n];
                var lowX = new double[n]; var highX = new double[n]; var lowY = new double[n]; var highY = new double[n];
                var lowZ = new double[n]; var highZ = new double[n];
                var widestRadius = 0.0;
                for (var i = 0; i < n; i++)
                {
                    var p0 = legLine[i].GetEndPoint(0); var p1 = legLine[i].GetEndPoint(1);
                    var u = (p1 - p0).Normalize();
                    sx[i] = p0.X; sy[i] = p0.Y; sz[i] = p0.Z; dx[i] = u.X; dy[i] = u.Y; dz[i] = u.Z;
                    len[i] = legLine[i].Length;
                    radius[i] = modelOf(legType[i]) / 2;
                    widestRadius = Math.Max(widestRadius, radius[i]);
                    lowX[i] = Math.Min(p0.X, p1.X); highX[i] = Math.Max(p0.X, p1.X);
                    lowY[i] = Math.Min(p0.Y, p1.Y); highY[i] = Math.Max(p0.Y, p1.Y);
                    lowZ[i] = Math.Min(p0.Z, p1.Z); highZ[i] = Math.Max(p0.Z, p1.Z);
                }
                var pad = 2 * widestRadius + 1 / 304.8;
                var cosLimit = Math.Cos(Math.PI / 180);
                var near = 1 / 304.8;
                var laps = new Dictionary<string, Tuple<int, double, double, string>>(StringComparer.Ordinal);
                var mixed = new List<string>();
                // SORT AND SWEEP along X: two legs whose padded boxes do not
                // meet cannot touch, so each leg is compared only with the legs
                // whose box starts before its own ends.
                var order = Enumerable.Range(0, n).OrderBy(i => lowX[i]).ToArray();
                for (var oa = 0; oa < n; oa++)
                {
                    var ia = order[oa];
                    for (var ob = oa + 1; ob < n; ob++)
                    {
                        var ib = order[ob];
                        if (lowX[ib] > highX[ia] + pad) break;
                        if (legSet[ia] == legSet[ib]) continue;
                        if (lowY[ib] > highY[ia] + pad || highY[ib] < lowY[ia] - pad || lowZ[ib] > highZ[ia] + pad || highZ[ib] < lowZ[ia] - pad) continue;
                        if (Math.Abs(dx[ia] * dx[ib] + dy[ia] * dy[ib] + dz[ia] * dz[ib]) < cosLimit) continue;
                        // B's two ends along A, and B's distance from A's line.
                        double ox = sx[ib] - sx[ia], oy = sy[ib] - sy[ia], oz = sz[ib] - sz[ia];
                        var along = ox * dx[ia] + oy * dy[ia] + oz * dz[ia];
                        double cx = ox - along * dx[ia], cy = oy - along * dy[ia], cz = oz - along * dz[ia];
                        if (Math.Sqrt(cx * cx + cy * cy + cz * cz) > radius[ia] + radius[ib] + near) continue;
                        var otherEnd = along + len[ib] * (dx[ia] * dx[ib] + dy[ia] * dy[ib] + dz[ia] * dz[ib]);
                        var s0 = Math.Min(along, otherEnd); var s1 = Math.Max(along, otherEnd);
                        var length = len[ia];
                        var passes = (s0 < -near && s1 > near && s1 < length - near) || (s0 > near && s0 < length - near && s1 > length + near);
                        if (!passes) continue;
                        var overlap = Math.Min(length, s1) - Math.Max(0, s0);
                        var ta = legType[ia]; var tb = legType[ib];
                        var da = nominalOf(ta); var db = nominalOf(tb);
                        if (specLap.Item1 > 0 && Math.Abs(da - db) > 0.05 / 304.8)
                        {
                            mixed.Add("'" + ta.Name + "' with '" + tb.Name + "' " + mm(overlap) + " mm");
                            continue;
                        }
                        var first = Math.Min(legSet[ia], legSet[ib]); var second = Math.Max(legSet[ia], legSet[ib]);
                        var key = standard[first].Id + " / " + standard[second].Id;
                        var need = required(specLap, da);
                        Tuple<int, double, double, string> held;
                        laps[key] = laps.TryGetValue(key, out held)
                            ? Tuple.Create(held.Item1 + 1, Math.Min(held.Item2, overlap), need, held.Item4)
                            : Tuple.Create(1, overlap, need, "'" + ta.Name + "'");
                    }
                }
                var specText = lengthText(specLap);
                if (laps.Count == 0 && mixed.Count == 0)
                    check("laps", "no lap found", specText, -1, unreadText + lapScope);
                else
                {
                    var shortLaps = laps.Where(l => l.Value.Item2 < l.Value.Item3 - tolerance)
                        .Select(l => "sets " + l.Key + " " + mm(l.Value.Item2) + " mm where " + mm(l.Value.Item3) + " mm is asked").ToList();
                    var read = string.Join(", ", laps.Take(5).Select(l => l.Value.Item1 + " lap(s) of " + l.Value.Item4 + " between sets " + l.Key
                        + ", shortest " + mm(l.Value.Item2) + " mm"));
                    if (specLap.Item1 > 0 && laps.Count > 0) specText += " (" + string.Join(", ", laps.Values.Select(v => mm(v.Item3) + " mm").Distinct()) + ")";
                    // A set that could not be read may hold a short lap, so the
                    // laps found never MEET while one is unread.
                    if (laps.Count > 0)
                    {
                        if (shortLaps.Count > 0) check("laps", read, specText, 0, "shorter: " + string.Join(", ", shortLaps.Take(5)) + "; " + unreadText + lapScope);
                        else if (lapUnread.Count > 0) check("laps", read, specText, -1, unreadText + lapScope);
                        else check("laps", read, specText, 1, lapScope);
                    }
                    if (mixed.Count > 0)
                        check("laps between two bar sizes", string.Join(", ", mixed.Take(5)), lengthText(specLap), -1,
                            "which bar's diameter the spec's " + lengthText(specLap) + " means is the engineer's to say - give the lap in mm to check it; " + lapScope);
                }
            }
        }

        // ---- anchorage: never passed - Revit holds no support for a bar --------------------
        if (specAnchorage != null)
        {
            var solids = solidsOf(host);
            var read = new List<string>();
            foreach (var bar in standard.Take(8))
            {
                var at = positionsOf(bar, false);
                var type = typeOf(bar);
                var outside = "not measured";
                if (at != null && at.Count > 0 && solids.Count > 0)
                {
                    try
                    {
                        var length = outsideOf(at[0], solids);
                        outside = length < 1 / 304.8 ? "both ends inside this host" : mm(length) + " mm outside this host's concrete";
                    }
                    catch (Exception) { }
                }
                read.Add("id " + bar.Id + " " + (type == null ? "" : "'" + type.Name + "' ") + "hooks " + hookAt(bar, 0) + " / " + hookAt(bar, 1) + ", " + outside
                    + (specAnchorage.Item1 > 0 && type != null ? " (" + lengthText(specAnchorage) + " = " + mm(required(specAnchorage, nominalOf(type))) + " mm)" : ""));
            }
            check("anchorage", read.Count == 0 ? "no main bars" : string.Join("; ", read) + (standard.Count > 8 ? "; and " + (standard.Count - 8) + " more set(s)" : ""),
                lengthText(specAnchorage), -1,
                "Revit holds no support for a bar and no anchorage length: how far a bar reaches into its support, past which face, "
                + "is read on the drawing and judged by the engineer - never passed here");
        }

        hostLines.Add(name + " - " + string.Join(" | ", lines));
    }
}

var report = string.Join(" || ", hostLines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(hostLines.Count + " host(s) checked, " + notHosts.Count + " not a rebar host: " + meets + " check(s) meet the value checked, "
        + doesNotMeet + " do not meet it, " + notChecked + " NOT CHECKED. Meeting a value says only that the model holds what the spec "
        + "gives; whether the reinforcement is adequate is the engineer's decision.");
    findings.AddRange(failedLines.Take(20));
    if (failedLines.Count > 20) findings.Add("and " + (failedLines.Count - 20) + " more that do not meet - every one is in `report`.");
    findings.AddRange(notHosts);
}
