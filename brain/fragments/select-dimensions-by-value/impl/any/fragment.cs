// NOT STANDALONE. Assumes `doc`, `viewName`, `minMm` and `maxMm` are in
// scope; leaves `elements`, `matched`, `outside`, `notLinear` and `findings`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE DIMENSIONS WHOSE MEASURED VALUE IS IN A RANGE - every one under 1 m,
// over 5 m, or between two values - in one view or every view. What to DO
// with them is the next fragment's: OVERRIDE_GRAPHICS_IN_VIEW colours them,
// ISOLATE_ELEMENTS shows them alone, SET_SELECTION selects them. That is why
// this hands `elements` on rather than painting anything itself.
//
// THE VALUE IS REVIT'S MEASURED VALUE, not its text. A string of several
// segments is in range when ANY segment is, and every segment's value is
// counted; a single dimension uses its own value. An overridden text (a
// dimension made to read "EQ" or a typed number) does not change what it
// measures, and the measure is what this compares.
//
// ONLY LINEAR DIMENSIONS. An angular or radial dimension measures in another
// unit or a different way, and is counted apart rather than compared with
// millimetres.
//
// RANGE: minMm 0 means no lower limit; maxMm 0 means no upper limit; both 0
// refuses, because "every dimension" is not a range.

const double MmPerFoot = 304.8;
var elements = new List<Element>();
var matched = 0;
var outside = 0;
var notLinear = 0;
var findings = new List<string>();

// "Level 1" can name a floor plan AND a ceiling plan, so the kind may be
// written in front - "FloorPlan: Level 1", as the rest of Heron writes views -
// and a name that still answers to two views is refused, never guessed.
View view = null;
string viewProblem = null;
var wantedView = (viewName ?? "").Trim();
if (wantedView != "")
{
    var wantedKind = "";
    var bare = wantedView;
    var colon = wantedView.IndexOf(':');
    if (colon > 0)
    {
        wantedKind = wantedView.Substring(0, colon).Replace(" ", "").Trim();
        bare = wantedView.Substring(colon + 1).Trim();
    }
    var answering = new List<View>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var v = candidate as View;
        if (v == null || v.IsTemplate) continue;
        if (!string.Equals(v.Name, bare, StringComparison.OrdinalIgnoreCase)) continue;
        if (wantedKind != "" && !string.Equals(v.ViewType.ToString(), wantedKind, StringComparison.OrdinalIgnoreCase)) continue;
        answering.Add(v);
    }
    if (answering.Count == 1) view = answering[0];
    else if (answering.Count > 1)
        viewProblem = answering.Count + " views are named \"" + bare + "\" ("
            + string.Join(", ", answering.Select(a => a.ViewType.ToString())) + "). Write it as \""
            + answering[0].ViewType + ": " + bare + "\".";
}

var low = minMm > 0 ? minMm / MmPerFoot : double.NegativeInfinity;
var high = maxMm > 0 ? maxMm / MmPerFoot : double.PositiveInfinity;

if (wantedView != "" && view == null)
    findings.Add((viewProblem ?? "No view is named \"" + wantedView + "\".") + " Nothing was read.");
else if (minMm <= 0 && maxMm <= 0)
    findings.Add("Neither a lower nor an upper limit was given, so nothing was selected - every "
        + "dimension is not a range. Give minMm, maxMm or both.");
else if (minMm > 0 && maxMm > 0 && minMm > maxMm)
    findings.Add("The lower limit is above the upper one, so nothing can be in range.");
else
{
    var collector = view == null
        ? new FilteredElementCollector(doc)
        : new FilteredElementCollector(doc, view.Id);
    foreach (var element in collector.OfClass(typeof(Dimension)))
    {
        var dimension = element as Dimension;
        if (dimension == null || dimension is SpotDimension) continue;

        bool linear = false;
        try { linear = dimension.DimensionShape == DimensionShape.Linear; } catch { }
        if (!linear) { notLinear++; continue; }

        var values = new List<double>();
        try
        {
            if (dimension.NumberOfSegments > 1)
            {
                foreach (DimensionSegment segment in dimension.Segments)
                {
                    var v = segment.Value;
                    if (v.HasValue) values.Add(v.Value);
                }
            }
            else
            {
                var v = dimension.Value;
                if (v.HasValue) values.Add(v.Value);
            }
        }
        catch { }

        if (values.Any(v => v >= low && v <= high))
        {
            matched++;
            elements.Add(dimension);
        }
        else outside++;
    }

    findings.Add(matched + " linear dimension(s) measure "
        + (minMm > 0 && maxMm > 0 ? "between " + minMm + " and " + maxMm + " mm"
           : minMm > 0 ? "at least " + minMm + " mm" : "at most " + maxMm + " mm")
        + (view == null ? " in the model" : " in \"" + view.Name + "\"") + "; " + outside
        + " do not; " + notLinear + " angular or radial left out.");
}
