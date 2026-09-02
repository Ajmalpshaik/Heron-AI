// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `parameterName` and
// `colourBand` are in scope; leaves `coloured`, `legend`, `noValue` and
// `skipped` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// HUES ARE STEPPED EVENLY. THERE IS NO PALETTE, AND THAT IS THE FIX.
// ===========================================================================
//
// A list of colours repeats: with six in the list the seventh system gets the
// first system's colour, and two different runs on one drawing then read as
// one. Stepping the hue by 360/groups guarantees every group is visually apart
// from every other however many there are - which no fixed list can promise.
//
// AND THE START HUE IS FIXED, NOT RANDOM. The same request gives the same
// colours every time. A drawing whose colours change on every run cannot be
// compared against the one issued last week, and whoever reads it has to learn
// the legend again. Groups are sorted by value first, so the mapping does not
// depend on what order the elements arrived in either.
//
// THE LEGEND IS PART OF THE ANSWER. A coloured drawing with no statement of
// which colour is which system is a picture rather than information, and this
// is the only thing that knows.
//
// A BLANK IS ITS OWN GROUP. Left plain in a coloured view, an element with
// nothing in the parameter reads as "not part of this" rather than "nobody
// filled it in", so it is coloured with the rest and named in `noValue`.

var coloured = 0;
var legend = new List<string>();
var noValue = new List<ElementId>();
var skipped = new List<ElementId>();

// The value as Revit renders it - what a schedule would print. An ElementId
// parameter is resolved to the referenced element's name, because "Level: 428"
// is not a group anybody can read.
Func<Element, string> valueOf = e =>
{
    Parameter parameter = null;
    try { parameter = e.LookupParameter(parameterName); }
    catch { parameter = null; }
    if (parameter == null || !parameter.HasValue) return "";
    try
    {
        if (parameter.StorageType == StorageType.String) return parameter.AsString() ?? "";
        if (parameter.StorageType == StorageType.ElementId)
        {
            var referenced = doc.GetElement(parameter.AsElementId());
            return referenced == null ? "" : (referenced.Name ?? "");
        }
        return parameter.AsValueString() ?? "";
    }
    catch { return ""; }
};

// Hue to RGB, written out rather than called: no drawing library is guaranteed
// in the context a fragment runs in. h is degrees, s and v are 0 to 1.
Func<double, double, double, Color> fromHue = (h, s, v) =>
{
    h = h % 360.0;
    if (h < 0) h += 360.0;
    var c = v * s;
    var x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
    var m = v - c;
    double r, g, b;
    if (h < 60) { r = c; g = x; b = 0; }
    else if (h < 120) { r = x; g = c; b = 0; }
    else if (h < 180) { r = 0; g = c; b = x; }
    else if (h < 240) { r = 0; g = x; b = c; }
    else if (h < 300) { r = x; g = 0; b = c; }
    else { r = c; g = 0; b = x; }
    return new Color(
        (byte)Math.Round((r + m) * 255.0),
        (byte)Math.Round((g + m) * 255.0),
        (byte)Math.Round((b + m) * 255.0));
};

var band = (colourBand ?? "vivid").Trim().ToLower();
var saturation = band == "pastel" ? 0.35 : (band == "neon" ? 1.0 : 0.65);
var brightness = band == "pastel" ? 0.95 : (band == "neon" ? 1.0 : 0.85);

// A solid fill is what makes the surface read as a colour rather than as a
// tinted line. Without one only the lines are coloured, and that is REPORTED
// rather than passed over - a drawing that came out fainter than expected
// should say why.
FillPatternElement solid = null;
try
{
    solid = new FilteredElementCollector(doc)
        .OfClass(typeof(FillPatternElement))
        .Cast<FillPatternElement>()
        .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill);
}
catch { solid = null; }

var grouped = new Dictionary<string, List<Element>>();
foreach (var element in elements)
{
    if (element == null) continue;
    var value = valueOf(element).Trim();
    if (value.Length == 0)
    {
        noValue.Add(element.Id);
        value = "(no value)";
    }
    if (!grouped.ContainsKey(value)) grouped[value] = new List<Element>();
    grouped[value].Add(element);
}

// Sorted, so the mapping does not depend on the order elements arrived in.
var names = grouped.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
var step = names.Count > 0 ? 360.0 / names.Count : 0.0;

for (var i = 0; i < names.Count; i++)
{
    var colour = fromHue(step * i, saturation, brightness);
    var painted = 0;

    foreach (var element in grouped[names[i]])
    {
        try
        {
            var overrides = view.GetElementOverrides(element.Id);
            overrides.SetProjectionLineColor(colour);
            overrides.SetCutLineColor(colour);
            if (solid != null)
            {
                overrides.SetSurfaceForegroundPatternColor(colour);
                overrides.SetSurfaceForegroundPatternId(solid.Id);
                overrides.SetSurfaceForegroundPatternVisible(true);
                overrides.SetCutForegroundPatternColor(colour);
                overrides.SetCutForegroundPatternId(solid.Id);
                overrides.SetCutForegroundPatternVisible(true);
            }
            view.SetElementOverrides(element.Id, overrides);
            painted++;
            coloured++;
        }
        catch
        {
            // A view that will not take an override on this element - a
            // template-controlled view is the usual cause. Named, never
            // counted as coloured.
            skipped.Add(element.Id);
        }
    }

    legend.Add(string.Format("{0}  - RGB {1},{2},{3}  ({4} element(s))",
        names[i], colour.Red, colour.Green, colour.Blue, painted));
}

if (solid == null)
    legend.Add("NOTE: this project has no solid fill pattern, so only the LINES are coloured - the "
        + "surfaces are left as they were, and the drawing will read fainter than expected");

if (noValue.Count > 0)
    legend.Add(string.Format("{0} element(s) have nothing in '{1}' - they are the '(no value)' group, "
        + "coloured on purpose so they are not mistaken for elements outside the set",
        noValue.Count, parameterName));
