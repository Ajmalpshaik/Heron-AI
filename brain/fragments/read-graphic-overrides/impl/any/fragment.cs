// NOT STANDALONE. Assumes `doc`, `view` and `elements` are in scope; leaves
// `overrides`, `findings`, `withOverride` and `overrideList` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THIS IS THE HALF THAT WAS MISSING. OVERRIDE_GRAPHICS_IN_VIEW takes settings
// ALREADY BUILT, and nothing could produce them from an element that already
// looked right. Read here, hand the object there, and matching works without
// either half knowing what colour anything is.
//
// AN ELEMENT WITH NO OVERRIDE STILL RETURNS A SETTINGS OBJECT - the empty one.
// Applying that to something else CLEARS its override, which is a real use, so
// it is handed back rather than treated as a failure. Which elements carried
// one is REPORTED, because "matched to something plain" and "the call did
// nothing" look identical afterwards.
//
// THE GETTERS ARE THE HALF THAT DID NOT MOVE. The setters were renamed between
// releases; SurfaceForegroundPatternColor and friends read the same on 2020 and
// on 2027, checked against both reference assemblies.
//
// PER-ELEMENT ONLY. A category or filter override in the same view is a
// different setting and is not read here - which is why an element can look
// grey and come back with nothing on it.
//
// THE FILL IS READ TOO (version 2, FRAGMENT-ISSUES 5b-154) - the surface
// pattern, and the cut pattern and its colour, the way REPORT_CATEGORY_
// OVERRIDES reads them since row 173. Version 1 read the line colours, the
// surface colour, transparency, halftone and line weight and no pattern at
// all, so an element overridden ONLY by a fill pattern, or only by a cut
// colour, was reported "NO per-element override in this view" - the sentence
// that sends a person to look somewhere else. The `overrides` object handed on
// was always Revit's own, patterns included; only the report was blind.
//
// AND THE WHOLE ANSWER TRAVELS AS ONE STRING, `overrideList` - every element
// that carries an override, its line in full, joined; empty when none does.
// `findings` holds the same lines as a list, unchanged, because every proof
// record quotes it.

var findings = new List<string>();
var withOverride = 0;
OverrideGraphicSettings overrides = null;
var overrideList = "";
var lines = new List<string>();

if (!view.AreGraphicsOverridesAllowed())
{
    findings.Add(string.Format("view '{0}' ({1}) does not accept graphic overrides at all, so nothing "
        + "can be carrying one", view.Name, view.ViewType));
}
else
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        OverrideGraphicSettings settings = null;
        try { settings = view.GetElementOverrides(element.Id); }
        catch (Exception ex)
        {
            findings.Add(string.Format("{0} (id {1}): could not read - {2}",
                element.Name, element.Id, ex.Message));
            continue;
        }

        // The FIRST readable one becomes the thing handed on. Matching takes one
        // source; a list of them would need somebody to choose, and choosing
        // silently is how the wrong element's look gets copied.
        if (overrides == null) overrides = settings;

        var parts = new List<string>();

        var projectionColour = settings.ProjectionLineColor;
        if (projectionColour != null && projectionColour.IsValid)
        {
            parts.Add(string.Format("line RGB({0},{1},{2})",
                projectionColour.Red, projectionColour.Green, projectionColour.Blue));
        }

        var cutColour = settings.CutLineColor;
        if (cutColour != null && cutColour.IsValid)
        {
            parts.Add(string.Format("cut line RGB({0},{1},{2})",
                cutColour.Red, cutColour.Green, cutColour.Blue));
        }

        var surfaceColour = settings.SurfaceForegroundPatternColor;
        if (surfaceColour != null && surfaceColour.IsValid)
        {
            parts.Add(string.Format("surface RGB({0},{1},{2})",
                surfaceColour.Red, surfaceColour.Green, surfaceColour.Blue));
        }

        // A real pattern id is a set pattern (5b-154) - read as
        // REPORT_CATEGORY_OVERRIDES reads it.
        if (settings.SurfaceForegroundPatternId != ElementId.InvalidElementId)
        {
            var pattern = doc.GetElement(settings.SurfaceForegroundPatternId);
            parts.Add("surface pattern " + (pattern == null ? "set" : pattern.Name));
        }

        // THE CUT HALF OF THE FILL, read the same way as the surface half.
        var cutFill = settings.CutForegroundPatternColor;
        if (cutFill != null && cutFill.IsValid)
        {
            parts.Add(string.Format("cut RGB({0},{1},{2})", cutFill.Red, cutFill.Green, cutFill.Blue));
        }

        if (settings.CutForegroundPatternId != ElementId.InvalidElementId)
        {
            var cutPattern = doc.GetElement(settings.CutForegroundPatternId);
            parts.Add("cut pattern " + (cutPattern == null ? "set" : cutPattern.Name));
        }

        if (settings.Transparency > 0) parts.Add(string.Format("{0}% transparent", settings.Transparency));
        if (settings.Halftone) parts.Add("halftone");
        if (settings.ProjectionLineWeight > 0) parts.Add("line weight " + settings.ProjectionLineWeight);

        if (parts.Count > 0)
        {
            withOverride++;
            var line = string.Format("{0} (id {1}): {2}", element.Name, element.Id,
                string.Join(", ", parts));
            findings.Add(line);
            lines.Add(line);
        }
        else
        {
            findings.Add(string.Format("{0} (id {1}): NO per-element override in this view. If it still "
                + "looks different, the cause is a CATEGORY or a FILTER override, which this does not "
                + "read", element.Name, element.Id));
        }
    }

    findings.Insert(0, string.Format("{0} of {1} element(s) carry a per-element override in '{2}'",
        withOverride, elements.Count, view.Name));

    overrideList = string.Join("  ||  ", lines.ToArray());
}
