// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `parameterName` and
// `colourBand` are in scope; leaves `coloured`, `legend`, `noValue`, `skipped`,
// `ambiguous`, `refused` and `wrapsFollowed` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// A PARAMETER NOBODY CARRIES IS REFUSED. IT IS NOT COLOURED.
// ===========================================================================
//
// Found in front of a model on 2026-09-09: a parameter name that does not
// exist coloured 22 elements anyway. `valueOf` below cannot tell "there is no
// such parameter" from "the parameter is empty" - it answers "" to both - so
// every element landed in the one "(no value)" group and the view was painted
// a single colour.
//
// That is worse than slow, it is a confident wrong answer. The drawing looks
// grouped, and it is grouped by nothing. Somebody reads a uniform colour as
// "these all share a system" when what happened is that the name was mistyped.
//
// So the parameter is counted across the whole set, and if NOT ONE element
// carries it nothing is coloured and `refused` says why - carrying the names
// that ARE there, which is the answer to the question that comes next. The
// same courtesy ADD_SCHEDULE_FIELDS pays with `availableFields`.
//
// A MIXED SELECTION IS UNCHANGED. Where SOME elements carry the parameter, the
// ones that do not still join the "(no value)" group and are still coloured -
// see the note below on why a blank is its own group. Only the case where the
// parameter is on nothing at all is a refusal, because only then is there no
// grouping to do.
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
//
// ===========================================================================
// INSULATION AND LINING TAKE THE COLOUR OF THE RUN THEY WRAP.
// ===========================================================================
//
// The owner's standing rule, and HIGHLIGHT_VS_REST has followed it since it
// was written: colour a duct and leave its insulation as it was, and the
// colour you asked for sits inside a jacket of the old one. Until 2026-09-23
// this fragment coloured the run and not the wrap. Now every run it paints
// passes its colour to its own insulation and lining, where the view shows
// them - the same colour calls, on the wrap's own settings, so the wrap keeps
// anything else it had.
//
// ONE DIRECTION ONLY: RUN TO WRAP. A wrap handed in whose run is handed in too
// takes the RUN'S colour and is not grouped on its own value - otherwise
// colouring by Type gives the insulation a different colour from its duct. A
// wrap handed in WITHOUT its run is grouped like anything else, and its run is
// not pulled in: this fragment colours what it was given.
//
// GetInsulationIds and GetLiningIds THROW for an element that cannot be
// wrapped, so catching per element IS the test, as in HIGHLIGHT_VS_REST.
//
// AN ELEMENT CARRYING THE NAME TWICE IS NOT GROUPED AND NOT COLOURED. A shared
// or project parameter can be bound beside a built-in one of the same name, and
// LookupParameter then returns one of them - "determined at random", in
// Autodesk's own reference - so its colour would say a value nobody chose. It is
// listed in `ambiguous`, apart from `skipped` because the fix is different
// (D-54 s3, FRAGMENT-ISSUES 5b-203).

var coloured = 0;
var legend = new List<string>();
var noValue = new List<ElementId>();
var skipped = new List<ElementId>();
var ambiguous = new List<ElementId>();
string refused = null;
var wrapsFollowed = 0;

// What was handed in, so a wrap whose run is also here can follow the run.
var handedIds = new HashSet<ElementId>();
foreach (var element in elements) if (element != null) handedIds.Add(element.Id);

Func<Element, bool> followsItsRun = e =>
{
    var wrap = e as InsulationLiningBase;
    if (wrap == null) return false;
    try { return handedIds.Contains(wrap.HostElementId); }
    catch { return false; }
};

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
var handed = 0;
var carrying = 0;

foreach (var element in elements)
{
    if (element == null) continue;

    // A wrap whose run was handed in too takes the RUN'S colour below, and is
    // not grouped on its own value. See the header.
    if (followsItsRun(element)) continue;
    handed++;

    var twice = false;
    try { twice = element.GetParameters(parameterName).Count > 1; }
    catch { twice = false; }
    if (twice) { ambiguous.Add(element.Id); continue; }

    // ASKED SEPARATELY FROM THE VALUE, and that separation is the whole fix.
    // `valueOf` answers "" for a parameter that is absent and for one that is
    // there and empty; only this call tells them apart.
    var present = false;
    try { present = element.LookupParameter(parameterName) != null; }
    catch { present = false; }
    if (present) carrying++;

    var value = valueOf(element).Trim();
    if (value.Length == 0)
    {
        noValue.Add(element.Id);
        value = "(no value)";
    }
    if (!grouped.ContainsKey(value)) grouped[value] = new List<Element>();
    grouped[value].Add(element);
}

// DECIDED BEFORE ANYTHING IS PAINTED. Grouping reads; the loop below writes.
// Nothing above this line has changed the model, so a refusal here leaves the
// view exactly as it was found.
if (string.IsNullOrEmpty((parameterName ?? "").Trim()))
{
    refused = "no parameter name was given, so there is nothing to group by. Nothing was "
        + "coloured";
}
else if (handed == 0)
{
    refused = "nothing was handed in to colour";
}
else if (ambiguous.Count == handed)
{
    refused = string.Format(
        "every one of the {0} element(s) handed in carries TWO OR MORE parameters called '{1}' - "
        + "usually a shared or project parameter bound beside a built-in one of the same name. "
        + "Revit would pick one of them at random, so there is no one value to group by and "
        + "NOTHING WAS COLOURED", handed, parameterName);
}
else if (carrying == 0)
{
    // The names that ARE there. Taken from the first element only: on a mixed
    // selection the full list is long and no more useful than a real example,
    // and this is a message a person reads rather than a set to iterate.
    var available = new List<string>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        try
        {
            foreach (Parameter candidate in element.Parameters)
            {
                var definition = candidate.Definition;
                if (definition == null) continue;
                var name = definition.Name ?? "";
                if (name.Length > 0 && !available.Contains(name)) available.Add(name);
            }
        }
        catch { }
        break;
    }
    available.Sort(StringComparer.OrdinalIgnoreCase);

    var shown = available.Count > 12 ? available.GetRange(0, 12) : available;
    var hint = shown.Count == 0
        ? "The first element reports no parameters at all"
        : "The first element carries: " + string.Join(", ", shown)
            + (available.Count > shown.Count
                ? string.Format(", and {0} more", available.Count - shown.Count)
                : "");

    refused = string.Format(
        "not one of the {0} element(s) handed in carries a parameter called '{1}', so there is "
        + "nothing to group by and NOTHING WAS COLOURED. Colouring them all one colour would "
        + "say they share a value they do not have. Check the spelling - a parameter name is "
        + "case-sensitive here and is not the same as the schedule heading. {2}",
        handed, parameterName, hint);
}

if (refused != null)
{
    // A refusal reports nothing FOUND. These were filled while reading and
    // would otherwise read as a finding on a run that did no work.
    grouped.Clear();
    noValue.Clear();
}

// Sorted, so the mapping does not depend on the order elements arrived in.
var names = grouped.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
var step = names.Count > 0 ? 360.0 / names.Count : 0.0;

// The same colour calls for a run and for its wraps, starting from the
// element's OWN current overrides so whatever else it had is kept. False when
// the view will not take the override.
Func<ElementId, Color, bool> paint = (id, colour) =>
{
    try
    {
        var overrides = view.GetElementOverrides(id);
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
        view.SetElementOverrides(id, overrides);
        return true;
    }
    catch
    {
        return false;
    }
};

// A run's insulation and lining. Both calls THROW for an element that cannot
// be wrapped, which is the test.
Func<ElementId, List<ElementId>> wrapsOf = id =>
{
    var found = new List<ElementId>();
    try { foreach (var wrapId in InsulationLiningBase.GetInsulationIds(doc, id)) found.Add(wrapId); }
    catch { }
    try { foreach (var wrapId in InsulationLiningBase.GetLiningIds(doc, id)) found.Add(wrapId); }
    catch { }
    return found;
};

// What the view shows, asked for only once a wrap turns up. A wrap outside the
// view has nothing to override, and counting it would pad the report.
HashSet<ElementId> shownInView = null;
var wrapsPainted = new HashSet<ElementId>();

for (var i = 0; i < names.Count; i++)
{
    var colour = fromHue(step * i, saturation, brightness);
    var painted = 0;

    foreach (var element in grouped[names[i]])
    {
        if (!paint(element.Id, colour))
        {
            // A view that will not take an override on this element - a
            // template-controlled view is the usual cause. Named, never
            // counted as coloured.
            skipped.Add(element.Id);
            continue;
        }
        painted++;
        coloured++;

        foreach (var wrapId in wrapsOf(element.Id))
        {
            if (wrapsPainted.Contains(wrapId)) continue;

            // A wrap that was handed in is painted wherever it is; one found
            // through its run only where the view shows it.
            if (!handedIds.Contains(wrapId))
            {
                if (shownInView == null)
                {
                    shownInView = new HashSet<ElementId>();
                    foreach (var shown in new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType())
                        shownInView.Add(shown.Id);
                }
                if (!shownInView.Contains(wrapId)) continue;
            }

            if (paint(wrapId, colour))
            {
                wrapsPainted.Add(wrapId);
                wrapsFollowed++;
            }
        }
    }

    legend.Add(string.Format("{0}  - RGB {1},{2},{3}  ({4} element(s))",
        names[i], colour.Red, colour.Green, colour.Blue, painted));
}

// A wrap handed in to follow its run, whose run the view refused: refused
// with it, never silently dropped from the counts.
if (refused == null)
{
    foreach (var element in elements)
    {
        if (element == null || !followsItsRun(element)) continue;
        if (!wrapsPainted.Contains(element.Id) && !skipped.Contains(element.Id)) skipped.Add(element.Id);
    }
}

if (refused == null && wrapsFollowed > 0)
    legend.Add(string.Format("{0} insulation or lining element(s) took the colour of the run they wrap, "
        + "so no coloured run is left inside a jacket of another colour", wrapsFollowed));

if (refused == null && solid == null)
    legend.Add("NOTE: this project has no solid fill pattern, so only the LINES are coloured - the "
        + "surfaces are left as they were, and the drawing will read fainter than expected");

if (refused == null && noValue.Count > 0)
    legend.Add(string.Format("{0} element(s) have nothing in '{1}' - they are the '(no value)' group, "
        + "coloured on purpose so they are not mistaken for elements outside the set",
        noValue.Count, parameterName));
