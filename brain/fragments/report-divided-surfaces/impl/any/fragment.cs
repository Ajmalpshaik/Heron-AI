// NOT STANDALONE. Assumes `doc` and `withNodes` are in scope; leaves
// `divided`, `surfaceCount`, `pathCount`, `missingNodes` and `findings`
// behind.
//
// VERSION 4 (2026-11-21, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - WHAT THE NODE LISTS LEAVE OUT IS COUNTED. With withNodes true,
// `missingNodes` is how many grid nodes gave Revit no point, over every
// surface, among the nodes tried before each list stopped at 50 - each
// surface's line already said its own count, and now the whole read declares
// it. And a list that stops at 50 says so on its line - "the
// list stops at 50: N more node(s) not listed" - where version 3 stopped
// without a word. A line with 50 nodes or fewer reads as version 3's.
//
// READ ONLY - opens no transaction.
//
// EVERY DIVIDED SURFACE AND DIVIDED PATH in the document in front - a mass
// or adaptive family, or a project holding masses - with how each is laid
// out:
//
//   a surface   its host, the U and V grids (layout, number or distance,
//               justification, rotation, offset), the gridline counts, the
//               border tile setting, and the surface's own settings - all
//               grid rotation, component rotation, component flip and
//               mirror, the U and V indents - and what cuts it: each level,
//               reference plane or other element Revit lists as intersecting
//   a path      its node count, layout, distance or fixed number, length,
//               whether it closes, and how distance is measured
//
// THE SURFACE'S OWN SETTINGS, read since this card's version 2 (2026-10-29),
// are worded as SET_DIVIDED_SURFACE_LAYOUT's readBack words them - "all grid
// rotation 45 degrees", "component flip yes", "cut by level Level 2" - so what
// that card changed reads back here in the same words. A setting Revit will not read is
// said so, with Revit's reason, and never shown as "no" or "none" (D-52).
//
// VERSION 3 (2026-11-21, row 5b-1908): EACH RULE IN ITS OWN WORDS, AND
// REVIT'S GRIDLINE COUNTS UNDER REVIT'S OWN NAMES - "U rule FixedNumber 20,
// Center, V rule FixedNumber 4, Center, Revit counts 7 U-gridlines and 23
// V-gridlines". Version 2 printed each count in brackets beside its
// same-letter rule ("U FixedNumber 20, Center (7 gridlines)"); row 5b-303
// measured on a Revit 2024 mass that each count moved with the OTHER rule, so
// that pairing could tell the modeller the other direction's lines. Which rule
// makes which gridlines is NEEDS-CHECKING BV21's to settle by eye; a finding
// says so - the same words as DIVIDE_FAMILY_SURFACE version 2.
//
// withNodes true adds where the nodes are, in millimetres - the first 50 of
// each - which is what an adaptive component is placed on. A node's place
// comes from the point Revit's geometry gives for it; a node with none is
// counted, not guessed.

const double MmPerFoot = 304.8;
var divided = "";
var findings = new List<string>();
var surfaceCount = 0;
var pathCount = 0;
// VERSION 4 (D-64): grid nodes that gave no point, over every surface read.
var missingNodes = 0;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<XYZ, string> mm = p => "(" + Math.Round(p.X * MmPerFoot) + ", " + Math.Round(p.Y * MmPerFoot) + ", " + Math.Round(p.Z * MmPerFoot) + ")";

// Rounded, then + 0.0, so a hair below zero never prints as "-0" (net8 and
// net10 do) and looks like a rotation that is there.
Func<double, string> degreesText = radians =>
    (Math.Round(radians * 180.0 / Math.PI, 2) + 0.0).ToString(invariant) + " degrees";
Func<bool, string> yesNo = flag => flag ? "yes" : "no";
// Revit's members, beside the modeller's degrees - cast from the enum, never
// typed as its number.
var componentRotationShown = new Dictionary<ComponentRotation, string>
{
    { ComponentRotation.Angle0, "0 degrees" }, { ComponentRotation.Angle90, "90 degrees" },
    { ComponentRotation.Angle180, "180 degrees" }, { ComponentRotation.Angle270, "270 degrees" },
};
Func<ComponentRotation, string> sayComponentRotation = rotation =>
    componentRotationShown.ContainsKey(rotation) ? componentRotationShown[rotation] : rotation.ToString();
// What cuts a surface, named the way a modeller finds it: a level by its name,
// a reference plane by its name (or its id when it has none), anything else by
// its category and id. An id is printed as text, never read as a number.
Func<Element, string> elementWords = e =>
    e is Level ? "level " + e.Name
    : e is ReferencePlane ? (string.IsNullOrEmpty(e.Name) ? "unnamed reference plane " + e.Id : "reference plane " + e.Name)
    : (e.Category != null ? e.Category.Name + " " : "element ") + e.Id;
Func<ElementId, string> cutWords = id =>
{
    var e = doc.GetElement(id);
    return e == null ? "element " + id : elementWords(e);
};
// One setting read, or why Revit would not give it - a failed read is never a
// "no", a 0 or a "none".
Func<Func<string>, string> held = get =>
{
    try { return get(); }
    catch (Exception ex) { return "not read (" + ex.Message + ")"; }
};
Func<SpacingRule, string> rule = r =>
{
    var text = r.Layout.ToString();
    try
    {
        if (r.Layout == SpacingRuleLayout.FixedNumber) text += " " + r.Number;
        else if (r.Layout != SpacingRuleLayout.None) text += " " + Math.Round(r.Distance * MmPerFoot) + " mm";
        if (r.Layout != SpacingRuleLayout.None) text += ", " + r.Justification;
        if (Math.Abs(r.GridlinesRotation) > 1e-9) text += ", rotated " + Math.Round(r.GridlinesRotation * 180 / Math.PI, 1) + " deg";
        if (Math.Abs(r.Offset) > 1e-9) text += ", offset " + Math.Round(r.Offset * MmPerFoot) + " mm";
    }
    catch { }
    return text;
};

// VERSION 3 (row 5b-1911): EACH LINE IS KEPT INSIDE THIS BLOCK, closed at the
// end of the file. The add-in sends every list of text a fragment leaves at its
// top level whole beside the answer, so version 2's top-level `report` printed
// every line a third time, beside `divided` and `findings`.
{
var report = new List<string>();
foreach (DividedSurface surface in new FilteredElementCollector(doc).OfClass(typeof(DividedSurface)))
{
    surfaceCount++;
    // VERSION 3: each rule in its own words, and Revit's two gridline counts
    // under Revit's own names, beside no rule (row 5b-1908, BV21).
    var line = "divided surface " + surface.Id + " on " + (surface.Host != null ? (surface.Host.Category != null ? surface.Host.Category.Name + " " : "") + surface.Host.Id : "no host")
        + ": U rule " + rule(surface.USpacingRule) + ", V rule " + rule(surface.VSpacingRule)
        + ", Revit counts " + surface.NumberOfUGridlines + " U-gridlines and " + surface.NumberOfVGridlines
        + " V-gridlines, border tiles " + surface.BorderTile;
    var one = surface;
    line += ", all grid rotation " + held(() => degreesText(one.AllGridRotation))
        + ", component rotation " + held(() => sayComponentRotation(one.ComponentRotation))
        + ", component flip " + held(() => yesNo(one.IsComponentFlipped))
        + ", component mirror " + held(() => yesNo(one.IsComponentMirrored))
        + ", U indent " + held(() => one.UPatternIndent.ToString(invariant))
        + ", V indent " + held(() => one.VPatternIndent.ToString(invariant))
        + ", cut by " + held(() =>
        {
            var cuts = one.GetAllIntersectionElements();
            if (cuts == null) return "not read (Revit gave no list)";
            return cuts.Count == 0 ? "none" : string.Join(", ", cuts.Select(cutWords));
        });
    if (withNodes)
    {
        var places = new List<string>();
        var none = 0;
        for (var u = 0; u < surface.NumberOfUGridlines && places.Count < 50; u++)
            for (var v = 0; v < surface.NumberOfVGridlines && places.Count < 50; v++)
            {
                var node = new GridNode(u, v);
                Point point = null;
                try
                {
                    var reference = surface.GetGridNodeReference(node);
                    point = reference != null ? surface.GetGeometryObjectFromReference(reference) as Point : null;
                }
                catch { }
                if (point == null) { none++; continue; }
                places.Add(u + "," + v + " " + mm(point.Coord));
            }
        line += "; nodes " + (places.Count > 0 ? string.Join(" ", places) : "none read") + (none > 0 ? "; " + none + " node(s) gave no point" : "");
        // VERSION 4 (D-64): counted for the whole read, and the cap said.
        missingNodes += none;
        var unlisted = surface.NumberOfUGridlines * surface.NumberOfVGridlines - places.Count - none;
        if (places.Count >= 50 && unlisted > 0)
            line += "; the list stops at 50: " + unlisted + " more node(s) not listed";
    }
    report.Add(line);
}

var options = new Options();
options.ComputeReferences = true;
options.IncludeNonVisibleObjects = true;
foreach (DividedPath path in new FilteredElementCollector(doc).OfClass(typeof(DividedPath)))
{
    pathCount++;
    var line = "divided path " + path.Id + ": " + path.NumberOfPoints + " node(s), " + path.SpacingRuleLayout;
    try
    {
        if (path.SpacingRuleLayout == SpacingRuleLayout.FixedNumber) line += " " + path.FixedNumberOfPoints;
        else if (path.SpacingRuleLayout != SpacingRuleLayout.None) line += " " + Math.Round(path.Distance * MmPerFoot) + " mm";
    }
    catch { }
    line += ", length " + Math.Round(path.TotalPathLength * MmPerFoot) + " mm" + (path.IsClosedLoop ? ", closed" : ", open")
        + ", measured " + path.MeasurementType;
    if (withNodes)
    {
        var places = new List<string>();
        // VERSION 4 (D-64): every point is counted, so the cap can be said.
        var pointsRead = 0;
        try
        {
            foreach (GeometryObject item in path.get_Geometry(options))
            {
                var point = item as Point;
                if (point == null) continue;
                pointsRead++;
                if (places.Count < 50) places.Add(mm(point.Coord));
            }
        }
        catch { }
        line += "; nodes " + (places.Count > 0 ? string.Join(" ", places) : "none read from its geometry");
        if (pointsRead > places.Count)
            line += "; the list stops at 50: " + (pointsRead - places.Count) + " more node(s) not listed";
    }
    report.Add(line);
}

// Nothing found leaves `divided` empty, so the hand-on sees no answer; the
// finding says it in words.
findings.Add(surfaceCount + " divided surface(s) and " + pathCount + " divided path(s) read in \"" + doc.Title + "\".");
if (surfaceCount > 0)
    findings.Add("Which rule makes which gridlines is not settled, so Revit's two counts are given under its own names "
        + "and not beside a rule: on a test mass the U-gridline count moved with the V rule and the V-gridline count "
        + "with the U rule. NEEDS-CHECKING BV21 settles it by eye in the Family Editor.");
divided = string.Join(" | ", report);
findings.AddRange(report);
}   // VERSION 3 (row 5b-1911): the end of the block that keeps `report`.
