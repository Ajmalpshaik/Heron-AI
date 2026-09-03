// NOT STANDALONE. Assumes `doc`, `fromStyleName`, `toStyleName`,
// `includeSketchLines` and `filledRegionMode` are in scope; leaves `moved`,
// `placedFound`, `sketchFound`, `regionsSet` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// LINES LIVE IN PLACES A SELECTION CANNOT REACH, and that is why a style
// refuses to purge after somebody "moved everything":
//   1. model and detail lines placed in views - the ones a rubber band finds
//   2. lines inside SKETCHES - a floor boundary, a ceiling edge, a stair riser.
//      Owned by a sketch, selectable in no view, and the usual culprit
//   3. filled region borders - not curve elements at all, see below
//   4. lines inside a loaded FAMILY - unreachable from this document
// Both 1 and 2 are collected here, counted apart so the difference is visible.
//
// REVIT WILL NOT SAY WHICH REGIONS USE THE OLD STYLE. There is a setter for a
// region's border style and no getter, on every release. So `filledRegionMode`
// offers only what is honest: "report" counts them, "all" sets every region,
// "skip" ignores them. "all" is blunt and is the only route that lets the old
// style purge.
//
// THE COLLECTOR IS DELIBERATELY NOT FILTERED BY VIEW. Sketch lines belong to no
// view, and filtering by one is exactly how they get missed.

var moved = 0;
var placedFound = 0;
var sketchFound = 0;
var regionsSet = 0;
var refused = new List<string>();

var linesCategory = Category.GetCategory(doc, BuiltInCategory.OST_Lines);
var sketchCategory = Category.GetCategory(doc, BuiltInCategory.OST_SketchLines);

var styles = new List<GraphicsStyle>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)))
{
    var style = element as GraphicsStyle;
    if (style == null) continue;
    try
    {
        var category = style.GraphicsStyleCategory;
        if (category == null || category.Parent == null || linesCategory == null) continue;
        if (category.Parent.Id != linesCategory.Id) continue;
    }
    catch { continue; }
    styles.Add(style);
}

GraphicsStyle fromStyle = null;
GraphicsStyle toStyle = null;
foreach (var style in styles)
{
    if (style.Name == fromStyleName) fromStyle = style;
    if (style.Name == toStyleName) toStyle = style;
}

if (fromStyle == null || toStyle == null)
{
    var names = new List<string>();
    foreach (var style in styles) names.Add(style.Name);
    names.Sort();
    refused.Add(string.Format("line style '{0}' not found. This project has: {1}",
        fromStyle == null ? fromStyleName : toStyleName, string.Join(", ", names)));
}
else if (fromStyle.Id == toStyle.Id)
{
    refused.Add(string.Format("'{0}' is both the from and the to style - nothing to move", fromStyleName));
}
else
{
    var placed = new List<CurveElement>();
    var sketch = new List<CurveElement>();

    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(CurveElement)))
    {
        var curve = element as CurveElement;
        if (curve == null) continue;

        GraphicsStyle style = null;
        try { style = curve.LineStyle as GraphicsStyle; }
        catch { continue; }
        if (style == null || style.Id != fromStyle.Id) continue;

        var isSketch = false;
        try
        {
            var category = curve.Category;
            if (category != null && sketchCategory != null && category.Id == sketchCategory.Id) isSketch = true;
            else if (curve.OwnerViewId == ElementId.InvalidElementId && !(curve is ModelCurve)) isSketch = true;
        }
        catch { }

        if (isSketch) sketch.Add(curve); else placed.Add(curve);
    }

    placedFound = placed.Count;
    sketchFound = sketch.Count;

    var toMove = new List<CurveElement>(placed);
    if (includeSketchLines) toMove.AddRange(sketch);

    foreach (var curve in toMove)
    {
        try { curve.LineStyle = toStyle; moved++; }
        catch (Exception ex)
        {
            refused.Add(string.Format("line {0}: {1}", curve.Id, ex.Message));
        }
    }

    if (!includeSketchLines && sketch.Count > 0)
    {
        refused.Add(string.Format("{0} line(s) inside sketches were NOT changed - they are almost "
            + "certainly why the old style will not purge", sketch.Count));
    }

    // Filled region borders: a different API, and a one-way one. See the header.
    if (filledRegionMode == "all" || filledRegionMode == "report")
    {
        var regions = new List<FilledRegion>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FilledRegion)))
        {
            var region = element as FilledRegion;
            if (region != null) regions.Add(region);
        }

        if (filledRegionMode == "report")
        {
            if (regions.Count > 0)
            {
                refused.Add(string.Format("{0} filled region(s) NOT looked at. Revit gives no way to "
                    + "read a region's border style, so there is no way to change only the offending "
                    + "ones - set filledRegionMode to \"all\" if the old style must purge", regions.Count));
            }
        }
        else
        {
            ICollection<ElementId> valid = null;
            try { valid = FilledRegion.GetValidLineStyleIdsForFilledRegion(doc); }
            catch { }

            if (valid != null && !valid.Contains(toStyle.Id))
            {
                refused.Add(string.Format("'{0}' is not a valid border style for a filled region - "
                    + "{1} region(s) left alone", toStyle.Name, regions.Count));
            }
            else
            {
                foreach (var region in regions)
                {
                    try { region.SetLineStyleId(toStyle.Id); regionsSet++; moved++; }
                    catch (Exception ex)
                    {
                        refused.Add(string.Format("region {0}: {1}", region.Id, ex.Message));
                    }
                }
            }
        }
    }

    if (moved == 0 && placedFound == 0 && sketchFound == 0)
    {
        refused.Add(string.Format("no line in THIS document uses '{0}'. If it still will not purge it "
            + "is referenced inside a loaded family, which this cannot reach, or set as a subcategory "
            + "default in Object Styles", fromStyle.Name));
    }
}
