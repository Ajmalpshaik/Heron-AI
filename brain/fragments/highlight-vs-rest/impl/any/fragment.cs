// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `highlight` and `grey` are
// in scope; leaves `highlighted`, `greyed`, `wrapsFollowed` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// INSULATION AND LINING TRAVEL WITH THEIR HOST, BOTH DIRECTIONS.
// ===========================================================================
//
// Highlight an insulated duct without them and the duct you asked to see is
// still wrapped in grey material - on screen it reads as half-applied. So:
//
//   picking the WRAP  pulls in the run underneath it   (HostElementId)
//   picking the RUN   pulls in its insulation AND lining
//
// GetInsulationIds and GetLiningIds THROW for an id that cannot host a wrap, so
// catching per element IS the category test. Pre-filtering by category would be
// a second list to keep in step with Revit's own idea of what can be wrapped,
// and letting one throw escape would lose the entire view.
//
// ONLY WHAT IS ALREADY IN THE VIEW IS TOUCHED. A wrap outside the view has
// nothing to override, and adding it would pad the count with elements nobody
// can see.

var highlighted = 0;
var greyed = 0;
var wrapsFollowed = 0;
var findings = new List<string>();

// Everything the view actually draws. The grayout is about the REST, so the
// whole view has to be known before anything is coloured.
var inView = new List<Element>();
foreach (var element in new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType())
    if (element != null) inView.Add(element);

var inViewIds = new HashSet<ElementId>();
foreach (var element in inView) inViewIds.Add(element.Id);

// The highlight set, plus every wrap that belongs with it.
var wanted = new HashSet<ElementId>();
foreach (var element in elements)
    if (element != null) wanted.Add(element.Id);

var toAdd = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    // Picking a WRAP pulls in what it is wrapped around.
    var wrap = element as InsulationLiningBase;
    if (wrap != null)
    {
        try
        {
            var host = wrap.HostElementId;
            if (host != ElementId.InvalidElementId && inViewIds.Contains(host)) toAdd.Add(host);
        }
        catch { }
        continue;
    }

    // Picking a RUN pulls in its insulation and its lining. Both of these throw
    // for anything that cannot be wrapped, which is exactly the test.
    try
    {
        foreach (var id in InsulationLiningBase.GetInsulationIds(doc, element.Id))
            if (inViewIds.Contains(id)) toAdd.Add(id);
    }
    catch { }

    try
    {
        foreach (var id in InsulationLiningBase.GetLiningIds(doc, element.Id))
            if (inViewIds.Contains(id)) toAdd.Add(id);
    }
    catch { }
}

foreach (var id in toAdd)
    if (wanted.Add(id)) wrapsFollowed++;

var solid = new OverrideGraphicSettings();
try
{
    var pattern = new FilteredElementCollector(doc)
        .OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>()
        .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill);
    if (pattern != null)
    {
        solid.SetSurfaceForegroundPatternId(pattern.Id);
        solid.SetSurfaceForegroundPatternVisible(true);
        solid.SetCutForegroundPatternId(pattern.Id);
        solid.SetCutForegroundPatternVisible(true);
    }
}
catch { }

Func<Color, OverrideGraphicSettings> look = colour =>
{
    var overrides = new OverrideGraphicSettings(solid);
    overrides.SetProjectionLineColor(colour);
    overrides.SetCutLineColor(colour);
    overrides.SetSurfaceForegroundPatternColor(colour);
    overrides.SetCutForegroundPatternColor(colour);
    return overrides;
};

var highlightLook = look(highlight);
var greyLook = look(grey);

foreach (var element in inView)
{
    try
    {
        var isWanted = wanted.Contains(element.Id);
        view.SetElementOverrides(element.Id, isWanted ? highlightLook : greyLook);
        if (isWanted) highlighted++;
        else greyed++;
    }
    catch
    {
        // A view that will not take an override on this element - a template
        // controlling it is the usual cause. Not counted either way.
    }
}

findings.Add(string.Format(
    "{0} element(s) brought forward, {1} greyed back, out of {2} in '{3}'. {4} insulation or lining "
    + "element(s) followed their host, which is what stops a highlighted duct still looking wrapped in "
    + "grey", highlighted, greyed, inView.Count, view.Name, wrapsFollowed));
