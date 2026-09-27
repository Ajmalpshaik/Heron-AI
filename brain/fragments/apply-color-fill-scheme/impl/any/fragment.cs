// NOT STANDALONE. Assumes `doc`, `view`, `category` and `parameterName` are in
// scope; leaves `schemeName`, `schemeSource`, `schemeApplied`, `legendPlaced`,
// `legendsInView`, `legendAtMm`, `entries`, `summary`, `findings` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT THIS IS, AND WHAT COLOR_BY_PARAMETER IS NOT. That one paints graphic
// overrides on elements, one by one, and nothing follows a room renamed
// tomorrow. This sets the VIEW's colour fill scheme for a category - the thing
// Revit's own Color Fill Legend reads - so the plan re-colours itself as names
// change, and the legend is Revit's, not a list in a chat reply.
//
// A SCHEME IS FOUND OR COPIED, NEVER MADE FROM NOTHING, AND NEVER EDITED IF IT
// WAS ALREADY THERE. The API can only `Duplicate` a scheme. So: one that
// already colours this category by the named parameter is used exactly as it
// stands; otherwise one of the category's schemes is COPIED and only the copy
// is changed. A scheme other views use is never edited here - a scheme is
// shared by every view that shows it, and editing one to suit this view
// re-colours the others without anybody asking.
//
// THE LEGEND IS PLACED ONCE. A view that already carries a colour fill legend
// for this category keeps it and gets no second one - two legends for one
// scheme are the same information twice, and the second is the one nobody
// meant. It goes just outside the crop region's right-hand edge, level with
// its top, and where it landed is read back and reported in millimetres on
// the sheet, because that is where somebody will look for it.
//
// A FAILURE AFTER ANYTHING IS WRITTEN THROWS. A normal return keeps the
// transaction, so a refusal is only ever returned before the first write;
// once a copy exists or the scheme is set, a copy the view will not take, a
// scheme that reads back wrong or a legend Revit will not place rolls the
// whole request back. Half of this - a coloured plan with no legend - is not
// kept (Golden Rule 16).
//
// READ BACK, NOT ASSUMED. The view's scheme for the category is read again
// after it is set, and the legends in the view are counted again after one is
// placed. `schemeApplied` and `legendsInView` are those reads, not the calls
// having returned.
//
// 2022 AND LATER. `ColorFillScheme`, `ColorFillLegend` and the view's
// `SetColorFillSchemeId` are not in the 2020 or 2021 API at all - read off the
// reference assemblies, see the card's compatibility-note.

var schemeName = "";
var schemeSource = "";
var schemeApplied = false;
var legendPlaced = false;
var legendsInView = 0;
var legendAtMm = "";
var entries = new List<string>();
var findings = new List<string>();
var refused = "";
var summary = "";

var wanted = (parameterName ?? "").Trim();
var categoryName = category == null ? "" : category.Name;
ColorFillScheme scheme = null;
var parameterId = ElementId.InvalidElementId;
// The KIND of value the parameter holds, off an element that carries it, so a
// numeric row can be written in the project's units, not Revit's.
ForgeTypeId specOf = null;

// ---- what the view is, said in every answer --------------------------------
// The crop and annotation crop decide whether a legend placed outside the
// crop region can be seen at all, so they are part of every reply, refusals
// included - a refusal is often the read somebody runs first.
var cropActive = false;
var annotationCropActive = false;
var viewScale = 1;
if (view != null)
{
    try { cropActive = view.CropBoxActive; } catch (Exception) { cropActive = false; }
    try
    {
        var annotationCrop = view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);
        annotationCropActive = annotationCrop != null && annotationCrop.AsInteger() == 1;
    }
    catch (Exception) { annotationCropActive = false; }
    try { viewScale = view.Scale > 0 ? view.Scale : 1; } catch (Exception) { viewScale = 1; }
}
var viewFacts = view == null ? "no view"
    : "view '" + view.Name + "' (" + view.ViewType + ", 1:" + viewScale
      + ", crop region " + (cropActive ? "on" : "off")
      + ", annotation crop " + (annotationCropActive ? "on" : "off") + ")";

// ---- 1. can this view be colour-filled for this category at all -----------
ICollection<ElementId> supported = new List<ElementId>();
if (view == null || view.IsTemplate)
{
    refused = "no view was given, or the one given is a view template. A colour scheme is set "
        + "on a view you can open. Nothing was changed.";
}
else if (category == null)
{
    refused = "no category was given - say which: Rooms, Spaces, HVAC Zones, Areas, Ducts or "
        + "Pipes. Nothing was changed.";
}
else if (wanted.Length == 0)
{
    refused = "no parameter was given to colour by - Name, Department, Zone. Nothing was changed.";
}
else
{
    try { supported = view.SupportedColorFillCategoryIds(); }
    catch (Exception) { supported = new List<ElementId>(); }

    if (!supported.Any(id => id.Equals(category.Id)))
    {
        var takes = new List<string>();
        foreach (var id in supported)
        {
            var c = Category.GetCategory(doc, id);
            if (c != null) takes.Add(c.Name);
        }
        takes.Sort(StringComparer.OrdinalIgnoreCase);
        refused = viewFacts + " cannot take a colour scheme for " + categoryName + ". "
            + (takes.Count == 0
                ? "It takes none at all - colour fill belongs to plans and sections."
                : "It takes: " + string.Join(", ", takes) + ".")
            + " Nothing was changed.";
    }
}

// ---- 2. the category's schemes, and the parameter by name -------------------
var schemes = new List<ColorFillScheme>();
if (refused.Length == 0)
{
    schemes = new FilteredElementCollector(doc)
        .OfClass(typeof(ColorFillScheme))
        .Cast<ColorFillScheme>()
        .Where(s => s.CategoryId.Equals(category.Id))
        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (schemes.Count == 0)
    {
        // Revit's API copies a scheme and cannot make the first one. Every
        // Revit template ships one per category it colours, so none at all
        // means the model was started without them.
        refused = "this model holds no colour scheme for " + categoryName + " to copy, and Revit "
            + "can only copy one, never make the first. Open Edit Color Scheme once for "
            + categoryName + " in any plan and Revit makes its default. Nothing was changed.";
    }
}

if (refused.Length == 0)
{
    // A PARAMETER'S NAME, WITHOUT READING ITS ID AS A NUMBER. The scheme lists
    // what it can colour by as ids; the names come off an element of the
    // category that carries them, and off the project's own parameter
    // elements for anything shared or added.
    Element sample = null;
    try
    {
        sample = new FilteredElementCollector(doc)
            .OfCategoryId(category.Id)
            .WhereElementIsNotElementType()
            .FirstElement();
    }
    catch (Exception) { sample = null; }

    // A CATEGORY WITH NOTHING PLACED YET STILL HAS ITS BUILT-IN PARAMETERS.
    // Revit's own list of them is walked once, only if an id is left unnamed,
    // and each is turned into an id to COMPARE - so a plan can be set up
    // before its rooms are drawn, and no id is read as a number.
    Dictionary<ElementId, string> builtIns = null;
    Func<ElementId, string> labelOf = id =>
    {
        if (sample != null)
        {
            foreach (Parameter p in sample.Parameters)
                if (p.Id.Equals(id) && p.Definition != null) return p.Definition.Name;
        }
        var asElement = doc.GetElement(id) as ParameterElement;
        if (asElement != null && asElement.GetDefinition() != null)
            return asElement.GetDefinition().Name;
        if (builtIns == null)
        {
            builtIns = new Dictionary<ElementId, string>();
            foreach (var typeId in ParameterUtils.GetAllBuiltInParameters())
            {
                try
                {
                    var asId = new ElementId(ParameterUtils.GetBuiltInParameter(typeId));
                    if (!builtIns.ContainsKey(asId))
                        builtIns[asId] = LabelUtils.GetLabelForBuiltInParameter(typeId);
                }
                catch (Exception) { }
            }
        }
        string named;
        return builtIns.TryGetValue(id, out named) ? named : null;
    };


    var offered = new List<KeyValuePair<string, ElementId>>();
    foreach (var id in schemes[0].GetSupportedParameterIds())
    {
        var label = labelOf(id);
        if (label != null) offered.Add(new KeyValuePair<string, ElementId>(label, id));
    }

    var exact = offered.Where(o => string.Equals(o.Key, wanted, StringComparison.Ordinal)).ToList();
    var loose = exact.Count > 0 ? exact
        : offered.Where(o => string.Equals(o.Key, wanted, StringComparison.OrdinalIgnoreCase)).ToList();

    if (loose.Count == 1)
    {
        parameterId = loose[0].Value;
        wanted = loose[0].Key;
        if (sample != null)
        {
            foreach (Parameter p in sample.Parameters)
            {
                if (!p.Id.Equals(parameterId) || p.Definition == null) continue;
                try { specOf = p.Definition.GetDataType(); } catch (Exception) { specOf = null; }
                break;
            }
        }
    }
    else if (loose.Count > 1)
    {
        // TWO PARAMETERS BY ONE NAME. A shared or project parameter can sit
        // beside a built-in one of the same name; colouring by whichever came
        // first would colour by a value nobody chose (D-54 s3).
        refused = categoryName + " carries " + loose.Count + " parameters called '" + wanted
            + "', so the name does not say which to colour by. Nothing was changed.";
    }
    else
    {
        var names = offered.Select(o => o.Key).Distinct().OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        refused = "a colour scheme for " + categoryName + " cannot colour by '" + wanted + "'. "
            + "It can colour by: " + string.Join(", ", names.Take(30))
            + (names.Count > 30 ? ", and " + (names.Count - 30) + " more" : "")
            + ". Nothing was changed.";
    }
}

// ---- 3. use one that already colours by it, or copy one -------------------
var viewsUsing = 0;
var copied = false;
if (refused.Length == 0)
{
    var current = view.GetColorFillSchemeId(category.Id);

    // BY VALUE OR BY RANGE, EITHER ONE COUNTS. The request names a parameter
    // and nothing else, so a range scheme on it is a match. Turning ranges
    // away would copy one on every run, because a copy of a range scheme is
    // itself by range.
    var matching = schemes.Where(s => s.ParameterDefinition.Equals(parameterId)).ToList();

    // The one this view already uses wins, so running this twice changes
    // nothing the second time.
    scheme = matching.FirstOrDefault(s => s.Id.Equals(current)) ?? matching.FirstOrDefault();

    if (scheme != null)
    {
        foreach (var other in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
        {
            if (other.IsTemplate || other.Id.Equals(view.Id)) continue;
            try { if (other.GetColorFillSchemeId(category.Id).Equals(scheme.Id)) viewsUsing++; }
            catch (Exception) { }
        }
        schemeSource = "used the scheme already in the model that colours " + categoryName
            + " by " + wanted + ", as it stands - nothing in it was edited"
            + (viewsUsing > 0 ? " (" + viewsUsing + " other view(s) use it too)" : "");
    }
    else
    {
        // COPY, AND CHANGE ONLY THE COPY. The source is the scheme this view
        // shows now if it has one - its colours and title style are what the
        // modeller last chose - else the first by name.
        var source = schemes.FirstOrDefault(s => s.Id.Equals(current)) ?? schemes[0];

        // ASK THE VIEW BEFORE COPYING ANYTHING. A view whose colour scheme a
        // template holds takes no scheme at all, and a copy made first would
        // be kept by a run that then declines.
        if (!view.CanApplyColorFillScheme(category.Id, source.Id))
        {
            refused = viewFacts + " will not take a colour scheme for " + categoryName
                + " - a view template holding the colour scheme is the usual reason. "
                + "Nothing was changed.";
        }
        else
        {
            var baseName = categoryName + " by " + wanted;
            var newName = baseName;
            var taken = new HashSet<string>(schemes.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
            for (var n = 2; (taken.Contains(newName) || !source.IsValidSchemeName(newName)) && n < 50; n++)
                newName = baseName + " (" + n + ")";

            ElementId madeId = null;
            try { madeId = source.Duplicate(newName); }
            catch (Exception ex)
            {
                refused = "Revit would not copy the scheme '" + source.Name + "': " + ex.Message
                    + " Nothing was changed.";
            }

            // FROM HERE A COPY EXISTS, SO A FAILURE THROWS. A normal return
            // keeps the transaction, and with it a copy nobody asked for; a
            // throw rolls the whole request back (Golden Rule 16).
            if (madeId != null)
            {
                scheme = doc.GetElement(madeId) as ColorFillScheme;
                if (scheme == null)
                    throw new InvalidOperationException("Revit copied the scheme '" + source.Name
                        + "' but the copy could not be found again. NOTHING WAS KEPT.");
                if (!scheme.IsValidParameterDefinitionId(parameterId))
                    throw new InvalidOperationException("The copy of '" + source.Name
                        + "' will not take '" + wanted + "' as what it colours by. NOTHING WAS KEPT.");

                scheme.ParameterDefinition = parameterId;
                scheme.Title = baseName;
                copied = true;
                schemeSource = "no scheme coloured " + categoryName + " by " + wanted
                    + ", so '" + source.Name + "' was COPIED as '" + newName
                    + "' and only the copy was set to colour by " + wanted
                    + " - '" + source.Name + "' is unchanged";
            }
        }
    }
}

// ---- 4. set it on the view, and read it back --------------------------------
// Only a copy made above has been written so far. A refusal here, with no
// copy, leaves the model as it was; with a copy, it throws so the copy goes.
if (refused.Length == 0)
{
    schemeName = scheme.Name;
    var before = view.GetColorFillSchemeId(category.Id);
    var wasName = before.Equals(ElementId.InvalidElementId) ? "none"
        : (doc.GetElement(before) == null ? "none" : "'" + doc.GetElement(before).Name + "'");

    string declined = null;
    if (!view.CanApplyColorFillScheme(category.Id, scheme.Id))
    {
        declined = viewFacts + " will not take the scheme '" + scheme.Name + "' for "
            + categoryName + " - a view template holding the colour scheme is the usual reason.";
    }
    else
    {
        try { view.SetColorFillSchemeId(category.Id, scheme.Id); }
        catch (Exception ex) { declined = "Revit refused the scheme on " + viewFacts + ": " + ex.Message; }
    }

    if (declined == null)
    {
        doc.Regenerate();
        schemeApplied = view.GetColorFillSchemeId(category.Id).Equals(scheme.Id);
        if (!schemeApplied)
            throw new InvalidOperationException("The colour scheme for " + categoryName + " on "
                + viewFacts + " reads back as something other than '" + scheme.Name
                + "' after it was set. NOTHING WAS KEPT.");
        findings.Add("the view's colour scheme for " + categoryName + " was " + wasName
            + " and reads back as '" + scheme.Name + "'");
    }
    else if (copied)
    {
        throw new InvalidOperationException(declined + " NOTHING WAS KEPT - the copied scheme '"
            + scheme.Name + "' was rolled back with the rest.");
    }
    else
    {
        refused = declined + " Nothing was changed.";
    }
}

// ---- 5. the rows, and filling a fresh copy if Revit left it empty -----------
// Revit adds a row for every value a view shows when a by-value scheme is
// drawn. That is observed behaviour, not a promise, so the rows are counted
// and, ON A COPY MADE HERE ONLY, filled if they came back empty. A scheme that
// was already in the model is never written to by this fragment.
if (refused.Length == 0 && schemeApplied)
{
    var rows = scheme.GetEntries();
    if (rows.Count == 0 && copied
        && (scheme.StorageType == StorageType.String || scheme.StorageType == StorageType.ElementId))
    {
        FillPatternElement solid = null;
        try
        {
            solid = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill
                                  && f.GetFillPattern().Target == FillPatternTarget.Drafting);
        }
        catch (Exception) { solid = null; }

        var level = view.GenLevel;
        var seen = new List<string>();
        var seenIds = new List<ElementId>();
        foreach (var e in new FilteredElementCollector(doc).OfCategoryId(category.Id)
                              .WhereElementIsNotElementType())
        {
            if (level != null && !e.LevelId.Equals(level.Id)) continue;
            foreach (Parameter p in e.Parameters)
            {
                if (!p.Id.Equals(parameterId)) continue;
                if (scheme.StorageType == StorageType.String)
                {
                    var s = p.AsString();
                    if (!string.IsNullOrWhiteSpace(s) && !seen.Contains(s)) seen.Add(s);
                }
                else
                {
                    var v = p.AsElementId();
                    if (v != null && !v.Equals(ElementId.InvalidElementId) && !seenIds.Contains(v))
                        seenIds.Add(v);
                }
                break;
            }
        }
        seen.Sort(StringComparer.OrdinalIgnoreCase);

        // Hues stepped evenly and from a fixed start, so the same request
        // gives the same colours every time and no two rows share one.
        var total = seen.Count + seenIds.Count;
        Func<int, Color> hue = i =>
        {
            var h = total == 0 ? 0.0 : (double)i / total * 6.0;
            var x = 1.0 - Math.Abs(h % 2.0 - 1.0);
            double r = 0, g = 0, b = 0;
            if (h < 1) { r = 1; g = x; } else if (h < 2) { r = x; g = 1; }
            else if (h < 3) { g = 1; b = x; } else if (h < 4) { g = x; b = 1; }
            else if (h < 5) { r = x; b = 1; } else { r = 1; b = x; }
            Func<double, byte> tone = c => (byte)Math.Round(255 * (0.35 + 0.55 * c));
            return new Color(tone(r), tone(g), tone(b));
        };

        var added = 0;
        for (var i = 0; i < total; i++)
        {
            try
            {
                var entry = new ColorFillSchemeEntry(scheme.StorageType);
                if (i < seen.Count) entry.SetStringValue(seen[i]);
                else entry.SetElementIdValue(seenIds[i - seen.Count]);
                entry.Color = hue(i);
                if (solid != null) entry.FillPatternId = solid.Id;
                scheme.AddEntry(entry);
                added++;
            }
            catch (Exception) { }
        }
        if (added > 0)
        {
            doc.Regenerate();
            findings.Add("Revit left the new copy with no rows, so " + added + " were added, one per "
                + "value on this level");
        }
        rows = scheme.GetEntries();
    }

    foreach (var row in rows)
    {
        string value;
        try
        {
            value = row.StorageType == StorageType.String ? row.GetStringValue()
                : row.StorageType == StorageType.ElementId
                    ? (doc.GetElement(row.GetElementIdValue()) == null ? "(none)"
                        : doc.GetElement(row.GetElementIdValue()).Name)
                : row.StorageType == StorageType.Integer ? row.GetIntegerValue().ToString()
                // A NUMBER IS WRITTEN IN THE PROJECT'S UNITS, as the dialog shows
                // it. Revit stores it in its own internal units, so without the
                // parameter's kind the raw figure is said to be exactly that.
                : specOf != null
                    ? UnitFormatUtils.Format(doc.GetUnits(), specOf, row.GetDoubleValue(), false)
                    : (!string.IsNullOrEmpty(row.Caption) ? row.Caption
                        : row.GetDoubleValue().ToString("0.###") + " (Revit internal units)");
        }
        catch (Exception) { value = row.Caption; }
        var c = row.Color;
        entries.Add((string.IsNullOrEmpty(value) ? row.Caption : value)
            + " - RGB " + c.Red + "," + c.Green + "," + c.Blue
            + (row.IsInUse ? "" : " (not in use)"));
    }
    if (entries.Count == 0)
        findings.Add("the scheme has no rows yet - the plan will show no colours until "
            + categoryName + " in it carry a " + wanted);
}

// ---- 6. the legend: once, just right of the crop region, near its top ------
Func<IList<ColorFillLegend>> legendsHere = () =>
    new FilteredElementCollector(doc).OfClass(typeof(ColorFillLegend))
        .Cast<ColorFillLegend>()
        .Where(l => l.OwnerViewId.Equals(view.Id))
        .ToList();

if (refused.Length == 0 && schemeApplied)
{
    var box = view.CropBox;
    var paperMm = viewScale / 304.8;            // one millimetre on the sheet, in model feet
    const double gapMm = 10.0;                  // clear of the crop edge, on the sheet

    var already = legendsHere().Where(l => l.ColorFillCategoryId.Equals(category.Id)).ToList();
    ColorFillLegend legend = already.FirstOrDefault();

    if (legend != null)
    {
        findings.Add("the view already had a colour fill legend for " + categoryName
            + ", so no second one was placed");
    }
    else
    {
        var at = box.Transform.OfPoint(new XYZ(box.Max.X + gapMm * paperMm, box.Max.Y, 0));
        try
        {
            legend = ColorFillLegend.Create(doc, view.Id, category.Id, at);
            legendPlaced = legend != null;
        }
        catch (Exception ex)
        {
            // THE LEGEND IS HALF OF WHAT WAS ASKED FOR. A scheme set with no
            // legend is a coloured plan nobody can read, so the whole request
            // rolls back rather than keeping half of it.
            throw new InvalidOperationException("Revit would not place the colour fill legend in "
                + viewFacts + ": " + ex.Message + " NOTHING WAS KEPT - the colour scheme was "
                + "rolled back with it, so the view is as it was.");
        }
        if (legend == null)
            throw new InvalidOperationException("Revit placed no colour fill legend in " + viewFacts
                + ". NOTHING WAS KEPT - the colour scheme was rolled back with it.");
        doc.Regenerate();
    }

    if (legend != null)
    {
        // WHERE IT IS, READ OFF THE LEGEND, measured from the crop region's
        // top-right corner and turned into millimetres on the sheet.
        var local = box.Transform.Inverse.OfPoint(legend.Origin);
        var rightMm = (local.X - box.Max.X) / paperMm;
        var belowMm = (box.Max.Y - local.Y) / paperMm;
        legendAtMm = Math.Round(rightMm, 1).ToString("0.#") + " mm right of the crop region's "
            + "right edge and " + Math.Round(belowMm, 1).ToString("0.#")
            + " mm below its top edge, on the sheet at 1:" + viewScale;
    }

    if (cropActive && annotationCropActive && legend != null)
        findings.Add("the annotation crop is ON, so a legend outside it can be cut off in the "
            + "view and on the sheet - look at it, or switch Annotation Crop off");
    if (!cropActive)
        findings.Add("the crop region is OFF, so 'the right-hand edge' is the view's stored crop "
            + "box, which may sit far from what the plan shows");
}

if (view != null) legendsInView = legendsHere().Count;

// ---- one sentence the reply cannot truncate --------------------------------
summary = refused.Length > 0
    ? (refused.StartsWith(viewFacts) ? "NOT DONE: " : "NOT DONE on " + viewFacts + ": ") + refused
    : viewFacts + ": " + categoryName + " coloured by " + wanted + " with '" + schemeName + "' ("
      + schemeSource + "); read back " + (schemeApplied ? "SET" : "NOT SET") + "; "
      + entries.Count + " row(s): " + string.Join("; ", entries.Take(20))
      + (entries.Count > 20 ? "; and " + (entries.Count - 20) + " more" : "")
      + ". Legend " + (legendPlaced ? "PLACED" : "not placed") + "; colour fill legends in the "
      + "view now: " + legendsInView + (legendAtMm.Length > 0 ? " - at " + legendAtMm : "")
      + (findings.Count > 0 ? ". Notes: " + string.Join("; ", findings) : "") + ".";
