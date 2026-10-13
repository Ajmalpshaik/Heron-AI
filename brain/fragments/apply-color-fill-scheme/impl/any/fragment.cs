// NOT STANDALONE. Assumes `doc`, `view` (one view or several), `category`,
// `parameterName`, `colours`, `schemeToUse`, `legend` and `clearScheme` are in
// scope; leaves `schemeName`, `schemeSource`, `schemeApplied`, `legendPlaced`,
// `legendsInView`, `legendAtMm`, `entries`, `viewsChanged`, `rowsRecoloured`,
// `summary`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// VERSION 2 - THE WHOLE VERB FAMILY ON ONE CARD. Version 1 set one view's
// scheme and placed its legend. This sets it on SEVERAL views (or on a view
// template), colours the rows - a band, or a colour per value - uses or makes
// the scheme the caller NAMES, places the legend at a point or not at all,
// and CLEARS a view's scheme. Reading a view's scheme is REPORT_COLOR_FILL_SCHEME,
// a READ, because a question must never need the Changes switch.
//
// WHAT THIS IS, AND WHAT COLOR_BY_PARAMETER IS NOT. That one paints graphic
// overrides on elements, one by one, and nothing follows a room renamed
// tomorrow. This sets the VIEW's colour fill scheme for a category - the thing
// Revit's own Color Fill Legend reads - so the plan re-colours itself as names
// change, and the legend is Revit's, not a list in a chat reply.
//
// A SCHEME IS FOUND OR COPIED, NEVER MADE FROM NOTHING. The API can only
// `Duplicate` one. And a scheme OTHER views or templates show is never
// re-coloured unless the caller NAMES it in `schemeToUse`: a scheme is shared
// by every view that shows it, so new colours for this plan would re-colour
// the others without anybody asking. Unnamed, it is copied and the copy takes
// the colours.
//
// ONE SPATIAL SCHEME PER VIEW. Revit shows at most one scheme across Rooms,
// Spaces, HVAC Zones and Areas in a view, so setting Rooms replaces a Spaces
// scheme the view had. Revit decides that, not this; it is reported by name.
//
// A FAILURE AFTER ANYTHING IS WRITTEN THROWS. Every refusal is decided before
// the first write; once a copy exists or a view is set, a failure throws and
// the whole request rolls back (Golden Rule 16). Half of this is never kept.
//
// READ BACK, NOT ASSUMED. Each view's scheme is read again after it is set or
// cleared, each recoloured row's colour after it is written, and the legends
// counted after they are placed, moved or removed.
//
// REVIT 2020 AND 2021 TAKE THE OTHER BRANCH. `ColorFillScheme` and
// `ColorFillLegend` are not in the 2020 API at all, and 2021's has no legend -
// read off the reference assemblies, see the card's compatibility-note.

var schemeName = "";
var schemeSource = "";
var schemeApplied = false;
var legendPlaced = false;
var legendsInView = 0;
var legendAtMm = "";
var entries = new List<string>();
var viewsChanged = 0;
var rowsRecoloured = 0;
var findings = new List<string>();
var refused = "";
var summary = "";

// ---- what was asked ----------------------------------------------------------
var views = new List<View>();
if (view != null)
{
    foreach (var one in view)
        if (one != null && !views.Any(w => w.Id.Equals(one.Id))) views.Add(one);
}
var wanted = (parameterName ?? "").Trim();
var namedScheme = (schemeToUse ?? "").Trim();
var categoryName = category == null ? "" : category.Name;
var clearing = clearScheme;
var viewNames = string.Join(", ", views.Select(v => "'" + v.Name + "'"));

// What each view is, said in every answer: the crop and annotation crop
// decide whether a legend outside the crop can be seen, and the template
// decides whether the view takes a scheme at all.
Func<View, string> factsOf = v =>
{
    var scale = 1;
    var crop = false;
    var annotation = false;
    try { scale = v.Scale > 0 ? v.Scale : 1; } catch (Exception) { scale = 1; }
    try { crop = v.CropBoxActive; } catch (Exception) { crop = false; }
    try
    {
        var p = v.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);
        annotation = p != null && p.AsInteger() == 1;
    }
    catch (Exception) { annotation = false; }
    var template = v.IsTemplate ? null : doc.GetElement(v.ViewTemplateId) as View;
    return (v.IsTemplate ? "view template '" : "view '") + v.Name + "' (" + v.ViewType
        + (v.IsTemplate ? "" : ", 1:" + scale + ", crop region " + (crop ? "on" : "off")
            + ", annotation crop " + (annotation ? "on" : "off")
            + (template == null ? ", no view template" : ", view template '" + template.Name + "'"))
        + ")";
};

// Why a view will not take a scheme, naming its template when it has one - a
// template that holds the colour scheme is the usual reason, and the cure is
// to name THE TEMPLATE as the view, which changes every view that follows it.
Func<View, string> whyNot = v =>
{
    var template = v.IsTemplate ? null : doc.GetElement(v.ViewTemplateId) as View;
    if (template == null)
        return " - a view template holding the colour scheme is the usual reason.";
    var following = 0;
    try
    {
        following = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
            .Count(o => !o.IsTemplate && o.ViewTemplateId.Equals(template.Id));
    }
    catch (Exception) { following = 0; }
    return " - its view template '" + template.Name + "' holds the colour scheme. Name '"
        + template.Name + "' as the view to set it there; every view that follows it changes ("
        + following + " do).";
};

// ---- the legend: blank, none, keep, or a point in millimetres ---------------
// A point is the model's X and Y in millimetres, as every point in this
// library is (D-71). It is laid onto the view's own plane before use.
var legendWord = (legend ?? "").Trim();
var legendMode = "default";
var legendXMm = 0.0;
var legendYMm = 0.0;
if (legendWord.Length == 0) legendMode = "default";
else if (string.Equals(legendWord, "none", StringComparison.OrdinalIgnoreCase)
      || string.Equals(legendWord, "no", StringComparison.OrdinalIgnoreCase)) legendMode = "none";
else if (string.Equals(legendWord, "keep", StringComparison.OrdinalIgnoreCase)) legendMode = "keep";
else
{
    var bits = legendWord.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(b => b.Trim()).ToList();
    double x, y;
    if (bits.Count == 2
        && double.TryParse(bits[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
        && double.TryParse(bits[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y))
    {
        legendMode = "point";
        legendXMm = x;
        legendYMm = y;
    }
    else
    {
        refused = "legend '" + legendWord + "' is not something this understands. Leave it blank to "
            + "place one beside the crop region, write none for no legend, keep to leave the legends "
            + "as they are, or give a point as x,y in millimetres - 12000,4000. Nothing was changed.";
    }
}

// ---- the colours: a band, and/or one colour per value ------------------------
// "pastel", or "Kitchen=255,200,200; Bath=#C8DCFF", or both - the band colours
// every row and the named values then take their own. The bands are
// COLOR_BY_PARAMETER's, word for word, so "pastel" means one thing in Heron.
var bandWord = "";
var askedColours = new List<KeyValuePair<string, Color>>();
foreach (var rawPart in (colours ?? "").Split(';'))
{
    if (refused.Length > 0) break;
    var part = rawPart.Trim();
    if (part.Length == 0) continue;
    var at = part.LastIndexOf('=');
    if (at < 0)
    {
        var word = part.ToLowerInvariant();
        if ((word == "vivid" || word == "pastel" || word == "neon") && bandWord.Length == 0) bandWord = word;
        else refused = "colours '" + part + "' is neither a band (vivid, pastel or neon, once) nor "
            + "value=r,g,b. Nothing was changed.";
        continue;
    }
    var valueText = part.Substring(0, at).Trim();
    var colourText = part.Substring(at + 1).Trim();
    Color parsed = null;
    if (colourText.StartsWith("#") && colourText.Length == 7)
    {
        int rgb;
        if (int.TryParse(colourText.Substring(1), System.Globalization.NumberStyles.HexNumber,
                         System.Globalization.CultureInfo.InvariantCulture, out rgb))
            parsed = new Color((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255));
    }
    else
    {
        var channels = colourText.Split(',').Select(c => c.Trim()).ToList();
        int r, g, b;
        if (channels.Count == 3 && int.TryParse(channels[0], out r) && int.TryParse(channels[1], out g)
            && int.TryParse(channels[2], out b) && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
            parsed = new Color((byte)r, (byte)g, (byte)b);
    }
    if (valueText.Length == 0 || parsed == null)
    {
        refused = "colours '" + part + "' needs a value and a colour: Kitchen=255,200,200 or "
            + "Kitchen=#FFC8C8, each channel 0 to 255. Nothing was changed.";
    }
    else if (askedColours.Any(k => string.Equals(k.Key, valueText, StringComparison.OrdinalIgnoreCase)))
    {
        refused = "colours names '" + valueText + "' twice, so it does not say which colour it gets. "
            + "Nothing was changed.";
    }
    else askedColours.Add(new KeyValuePair<string, Color>(valueText, parsed));
}
var recolouring = bandWord.Length > 0 || askedColours.Count > 0;

if (refused.Length == 0)
{
    if (views.Count == 0)
        refused = "no view was given. Name a plan, several separated by commas, or a view template. "
            + "Nothing was changed.";
    else if (category == null)
        refused = "no category was given - say which: Rooms, Spaces, HVAC Zones, Areas, Ducts or "
            + "Pipes. Nothing was changed.";
    else if (clearing && recolouring)
        refused = "clearScheme takes the scheme off the view, so colours have nothing to colour - ask "
            + "for one or the other. Nothing was changed.";
}

#if REVIT2020 || REVIT2021
// ---- REVIT 2020 AND 2021: THE API DOES NOT REACH ----------------------------
// Read off the reference assemblies on 2026-10-13: 2020 has no ColorFillScheme,
// ColorFillSchemeEntry or ColorFillLegend type and no view call that reads or
// sets a colour scheme; 2021 has the scheme types but no ColorFillLegend. So
// nothing here writes. What the model holds is still named, so the modeller
// can do it in Revit's own dialog in one pass.
if (refused.Length == 0)
{
    // EVERY SCHEME COUNTED, each name once with how many carry it - a template
    // ships "Scheme 1" for Rooms and for Spaces, and two of them are two.
    var held = new List<string>();
    var heldCount = 0;
    try
    {
        var all = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ColorFillSchema).ToElements();
        heldCount = all.Count;
        foreach (var group in all.GroupBy(e => e.Name).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            held.Add(group.Key + (group.Count() > 1 ? " (" + group.Count() + " by that name)" : ""));
    }
    catch (Exception) { }
    var version = "";
    try { version = doc.Application.VersionNumber; } catch (Exception) { version = "this release"; }
    refused = "Revit " + version + "'s API cannot " + (clearing ? "clear" : "set") + " a view's colour "
        + "scheme, change a scheme's colours or place a Color Fill Legend - those calls arrived in "
        + "Revit 2022. Do it in Revit: select " + (views.Count == 1 ? "the view" : "each view")
        + ", Properties > Color Scheme > " + categoryName + ", "
        + (clearing ? "choose (none)" : "choose or duplicate a scheme coloured by "
            + (wanted.Length > 0 ? wanted : "the parameter you want") + ", then Annotate > Color Fill Legend")
        + ". This model holds " + heldCount + " colour scheme(s)"
        + (held.Count > 0 ? ": " + string.Join(", ", held.Take(20)) : "") + ". Nothing was changed.";
}
#else
// ---- 1. every view must take the category ----------------------------------
// Decided before anything is written, so a refusal changes nothing.
Func<View, IList<ColorFillLegend>> legendsOf = v =>
    new FilteredElementCollector(doc).OfClass(typeof(ColorFillLegend)).Cast<ColorFillLegend>()
        .Where(l => l.OwnerViewId.Equals(v.Id) && l.ColorFillCategoryId.Equals(category.Id))
        .ToList();

// A ROW IS MATCHED BY ITS VALUE, as the modeller sees it - the text itself,
// the referenced element's name, the integer - or by its caption, which is
// what the legend prints. Either matching, ignoring case, is the row.
Func<ColorFillSchemeEntry, string> keyOf = row =>
{
    try
    {
        if (row.StorageType == StorageType.String) return row.GetStringValue() ?? "";
        if (row.StorageType == StorageType.ElementId)
        {
            var referenced = doc.GetElement(row.GetElementIdValue());
            return referenced == null ? (row.Caption ?? "") : referenced.Name;
        }
        if (row.StorageType == StorageType.Integer) return row.GetIntegerValue().ToString();
    }
    catch (Exception) { }
    return row.Caption ?? "";
};
Func<ColorFillSchemeEntry, string, bool> rowIs = (row, text) =>
    string.Equals(keyOf(row), text, StringComparison.OrdinalIgnoreCase)
    || string.Equals(row.Caption ?? "", text, StringComparison.OrdinalIgnoreCase);

if (refused.Length == 0)
{
    foreach (var v in views)
    {
        ICollection<ElementId> takes;
        try { takes = v.SupportedColorFillCategoryIds(); }
        catch (Exception) { takes = new List<ElementId>(); }
        if (takes.Any(id => id.Equals(category.Id))) continue;

        var names = new List<string>();
        foreach (var id in takes)
        {
            var c = Category.GetCategory(doc, id);
            if (c != null) names.Add(c.Name);
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        refused = factsOf(v) + " cannot take a colour scheme for " + categoryName + ". "
            + (names.Count == 0
                ? "It takes none at all - colour fill belongs to plans, sections and their templates."
                : "It takes: " + string.Join(", ", names) + ".")
            + " Nothing was changed.";
        break;
    }
}

// ---- 2. CLEAR: the scheme off each view, and its legends --------------------
// The scheme ELEMENT is never deleted - other views may show it. The legends
// for the category go with it, because a legend with no scheme reads "No color
// scheme assigned"; `legend` keep leaves them.
if (refused.Length == 0 && clearing)
{
    var toClear = new List<View>();
    foreach (var v in views)
    {
        var current = v.GetColorFillSchemeId(category.Id);
        var hasLegend = !v.IsTemplate && legendsOf(v).Count > 0;
        if (current.Equals(ElementId.InvalidElementId))
        {
            if (hasLegend && legendMode != "keep") toClear.Add(v);
            continue;
        }
        if (!v.CanApplyColorFillScheme(category.Id, ElementId.InvalidElementId))
        {
            refused = factsOf(v) + " will not let its colour scheme for " + categoryName
                + " be cleared" + whyNot(v) + " Nothing was changed.";
            break;
        }
        toClear.Add(v);
    }
    if (refused.Length == 0 && toClear.Count == 0)
        refused = "none of " + viewNames + " has a colour scheme" + (legendMode == "keep" ? "" : " or a "
            + "colour fill legend") + " for " + categoryName + ", so there was nothing to clear. "
            + "Nothing was changed.";

    if (refused.Length == 0)
    {
        var legendsRemoved = 0;
        foreach (var v in toClear)
        {
            var changed = false;
            var current = v.GetColorFillSchemeId(category.Id);
            if (!current.Equals(ElementId.InvalidElementId))
            {
                var was = doc.GetElement(current) == null ? "a scheme" : "'" + doc.GetElement(current).Name + "'";
                try { v.SetColorFillSchemeId(category.Id, ElementId.InvalidElementId); }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Revit would not clear the colour scheme on "
                        + factsOf(v) + ": " + ex.Message + " NOTHING WAS KEPT.");
                }
                doc.Regenerate();
                if (!v.GetColorFillSchemeId(category.Id).Equals(ElementId.InvalidElementId))
                    throw new InvalidOperationException("The colour scheme for " + categoryName + " on "
                        + factsOf(v) + " still reads back after it was cleared. NOTHING WAS KEPT.");
                findings.Add("'" + v.Name + "': " + was + " for " + categoryName
                    + " was taken off and reads back as none");
                changed = true;
            }
            if (legendMode != "keep" && !v.IsTemplate)
            {
                var gone = legendsOf(v).Select(l => l.Id).ToList();
                if (gone.Count > 0)
                {
                    doc.Delete(gone);
                    doc.Regenerate();
                    if (legendsOf(v).Count > 0)
                        throw new InvalidOperationException("The colour fill legend in " + factsOf(v)
                            + " is still there after it was removed. NOTHING WAS KEPT.");
                    legendsRemoved += gone.Count;
                    changed = true;
                }
            }
            if (changed) viewsChanged++;
        }
        schemeSource = "cleared from " + viewsChanged + " view(s); " + legendsRemoved
            + " colour fill legend(s) removed. The scheme itself is still in the model - other views may "
            + "show it";
    }
}

// ---- 3. APPLY: which scheme, decided before anything is written -------------
ColorFillScheme target = null;
ColorFillScheme copyFrom = null;
var copyName = "";
var copyTitle = "";
var parameterId = ElementId.InvalidElementId;
ForgeTypeId specOf = null;
var namedExplicitly = false;

if (refused.Length == 0 && !clearing && wanted.Length == 0 && namedScheme.Length == 0)
    refused = "say what to colour by - parameterName Name, Department, Number - or name an existing "
        + "scheme in schemeToUse. Nothing was changed.";

// FOR AREAS, THE AREA PLAN DECIDES THE AREA SCHEME. A colour scheme for Areas
// belongs to one area scheme and an area plan to one, so the plan answers it
// and the schemes are narrowed to it. Plans of two area schemes are refused.
var areaSchemeId = ElementId.InvalidElementId;
var areaSchemeName = "";
if (refused.Length == 0 && !clearing)
{
    var areaIds = new List<ElementId>();
    foreach (var v in views)
    {
        var plan = v as ViewPlan;
        if (plan == null || v.IsTemplate) continue;
        AreaScheme owner = null;
        try { owner = plan.AreaScheme; } catch (Exception) { owner = null; }
        if (owner != null && !areaIds.Any(x => x.Equals(owner.Id)))
        {
            areaIds.Add(owner.Id);
            areaSchemeName = owner.Name;
        }
    }
    if (areaIds.Count > 1)
        refused = "these area plans belong to " + areaIds.Count + " different area schemes, and a "
            + "colour scheme belongs to one - run it once per area scheme. Nothing was changed.";
    else if (areaIds.Count == 1) areaSchemeId = areaIds[0];
}

var schemes = new List<ColorFillScheme>();
if (refused.Length == 0 && !clearing)
{
    schemes = new FilteredElementCollector(doc).OfClass(typeof(ColorFillScheme)).Cast<ColorFillScheme>()
        .Where(s => s.CategoryId.Equals(category.Id)
                 && (areaSchemeId.Equals(ElementId.InvalidElementId) || s.AreaSchemeId.Equals(areaSchemeId)))
        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    if (schemes.Count == 0)
        // Revit's API copies a scheme and cannot make the first. Every Revit
        // template ships one per category it colours, so none at all means the
        // model was started without them.
        refused = "this model holds no colour scheme for " + categoryName
            + (areaSchemeName.Length > 0 ? " in the area scheme '" + areaSchemeName + "'" : "")
            + " to copy, and Revit can only copy one, never make the first. Open Edit Color Scheme "
            + "once for " + categoryName + " in any plan and Revit makes its default. Nothing was changed.";
}

// A PARAMETER'S NAME, WITHOUT READING ITS ID AS A NUMBER. The scheme lists
// what it can colour by as ids; the names come off an element of the category
// that carries them, off the project's parameter elements, and - for a
// category with nothing placed yet - off Revit's own list of built-in ones.
Element sample = null;
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

if (refused.Length == 0 && !clearing)
{
    try
    {
        sample = new FilteredElementCollector(doc).OfCategoryId(category.Id)
            .WhereElementIsNotElementType().FirstElement();
    }
    catch (Exception) { sample = null; }

    if (wanted.Length > 0)
    {
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
        }
        else if (loose.Count > 1)
        {
            // TWO PARAMETERS BY ONE NAME. Colouring by whichever came first
            // would colour by a value nobody chose (D-54 s3).
            refused = categoryName + " carries " + loose.Count + " parameters called '" + wanted
                + "', so the name does not say which to colour by. Nothing was changed.";
        }
        else
        {
            var names = offered.Select(o => o.Key).Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            refused = "a colour scheme for " + categoryName + " cannot colour by '" + wanted + "' - "
                + categoryName + " in this model do not carry it. It can colour by: "
                + string.Join(", ", names.Take(30))
                + (names.Count > 30 ? ", and " + (names.Count - 30) + " more" : "")
                + ". Nothing was changed.";
        }
    }
}

// Who else shows a scheme - every view and template outside this request.
Func<ColorFillScheme, List<string>> otherUsers = s =>
{
    var names = new List<string>();
    foreach (var other in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        if (views.Any(w => w.Id.Equals(other.Id))) continue;
        try
        {
            if (other.GetColorFillSchemeId(category.Id).Equals(s.Id))
                names.Add((other.IsTemplate ? "template '" : "'") + other.Name + "'");
        }
        catch (Exception) { }
    }
    return names;
};

Func<ColorFillScheme, bool> allTake = s =>
    views.All(v =>
    {
        try { return v.CanApplyColorFillScheme(category.Id, s.Id); }
        catch (Exception) { return false; }
    });

if (refused.Length == 0 && !clearing)
{
    var firstCurrent = views[0].GetColorFillSchemeId(category.Id);
    var baseName = categoryName + " by " + wanted;

    if (namedScheme.Length > 0)
    {
        namedExplicitly = true;
        var existing = schemes.FirstOrDefault(s => string.Equals(s.Name, namedScheme, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (parameterId.Equals(ElementId.InvalidElementId))
            {
                parameterId = existing.ParameterDefinition;
                wanted = labelOf(parameterId) ?? "its own parameter";
            }
            if (!existing.ParameterDefinition.Equals(parameterId))
                refused = "the scheme '" + existing.Name + "' colours " + categoryName + " by "
                    + (labelOf(existing.ParameterDefinition) ?? "another parameter") + ", not by " + wanted
                    + ". Give another name in schemeToUse to make a new scheme, or leave parameterName "
                    + "blank to use it as it colours. Nothing was changed.";
            else target = existing;
        }
        else if (parameterId.Equals(ElementId.InvalidElementId))
        {
            refused = "there is no colour scheme for " + categoryName + " called '" + namedScheme
                + "' - give parameterName as well, and it is made by copying one. It has: "
                + string.Join(", ", schemes.Select(s => "'" + s.Name + "'").Take(20)) + ". Nothing was changed.";
        }
        else
        {
            copyFrom = schemes.FirstOrDefault(s => s.Id.Equals(firstCurrent) && allTake(s))
                ?? schemes.FirstOrDefault(s => allTake(s)) ?? schemes[0];
            copyName = namedScheme;
            copyTitle = namedScheme;
        }
    }
    else
    {
        // BY VALUE OR BY RANGE, EITHER COUNTS, and one every view can take
        // comes first; the one the first view already shows wins, so running
        // this twice changes nothing the second time.
        var matching = schemes.Where(s => s.ParameterDefinition.Equals(parameterId))
            .OrderBy(s => allTake(s) ? 0 : 1).ToList();
        target = matching.FirstOrDefault(s => s.Id.Equals(firstCurrent)) ?? matching.FirstOrDefault();
        if (target == null)
        {
            // COPY, AND CHANGE ONLY THE COPY. The source is the scheme the
            // first view shows now, if any - its colours and title style are
            // what the modeller last chose - else the first every view takes.
            copyFrom = schemes.FirstOrDefault(s => s.Id.Equals(firstCurrent) && allTake(s))
                ?? schemes.FirstOrDefault(s => allTake(s)) ?? schemes[0];
            copyName = baseName;
            copyTitle = baseName;
        }
    }

    // NEW COLOURS ON A SCHEME OTHERS SHOW ARE PUT ON A COPY, unless the caller
    // named that scheme - naming it is asking for every view that shows it.
    if (refused.Length == 0 && target != null && recolouring && !namedExplicitly)
    {
        var users = otherUsers(target);
        if (users.Count > 0)
        {
            findings.Add("'" + target.Name + "' is also shown by " + string.Join(", ", users.Take(8))
                + (users.Count > 8 ? " and " + (users.Count - 8) + " more" : "")
                + ", so the new colours went on a copy - name it in schemeToUse to re-colour it everywhere");
            copyFrom = target;
            copyName = target.Name;
            copyTitle = "";
            target = null;
        }
    }

    if (refused.Length == 0 && copyFrom != null)
    {
        var taken = new HashSet<string>(schemes.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var stem = copyName;
        for (var n = 2; (taken.Contains(copyName) || !copyFrom.IsValidSchemeName(copyName)) && n < 50; n++)
            copyName = stem + " (" + n + ")";
        if (namedExplicitly && !string.Equals(copyName, namedScheme, StringComparison.Ordinal))
            refused = "'" + namedScheme + "' is not a name Revit will give a colour scheme. Nothing was changed.";
    }

    // ASK EVERY VIEW BEFORE COPYING ANYTHING. A view whose template holds the
    // colour scheme takes none, and a copy made first would be kept by a run
    // that then declines.
    if (refused.Length == 0)
    {
        var probe = target ?? copyFrom;
        foreach (var v in views)
        {
            bool takes;
            try { takes = v.CanApplyColorFillScheme(category.Id, probe.Id); }
            catch (Exception) { takes = false; }
            if (takes) continue;
            refused = factsOf(v) + " will not take " + (target != null ? "the scheme '" + probe.Name + "'"
                : "a colour scheme") + " for " + categoryName + whyNot(v) + " Nothing was changed.";
            break;
        }
    }

    // A COLOUR FOR A VALUE THE SCHEME HAS NO ROW FOR is added only to a scheme
    // that colours by text; any other kind is refused here, before a write.
    if (refused.Length == 0 && target != null && askedColours.Count > 0 && target.StorageType != StorageType.String)
    {
        var have = target.GetEntries().ToList();
        var missing = askedColours.Where(k => !have.Any(h => rowIs(h, k.Key)))
            .Select(k => "'" + k.Key + "'").ToList();
        if (missing.Count > 0)
            refused = "'" + target.Name + "' has no row for " + string.Join(", ", missing) + ", and it colours "
                + "by a value that is not text, so a row cannot be made for it. Its rows: "
                + string.Join(", ", have.Select(h => keyOf(h)).Take(30)) + ". Nothing was changed.";
    }
}

// ---- 4. the writes ----------------------------------------------------------
// FROM HERE A FAILURE THROWS: a normal return keeps the transaction, and with
// it whatever half was done (Golden Rule 16).
var copied = false;
if (refused.Length == 0 && !clearing && copyFrom != null)
{
    ElementId madeId = null;
    try { madeId = copyFrom.Duplicate(copyName); }
    catch (Exception ex)
    {
        // The copy is the first write, so a refusal here changes nothing.
        refused = "Revit would not copy the scheme '" + copyFrom.Name + "': " + ex.Message
            + " Nothing was changed.";
    }
    if (madeId != null)
    {
        target = doc.GetElement(madeId) as ColorFillScheme;
        if (target == null)
            throw new InvalidOperationException("Revit copied the scheme '" + copyFrom.Name
                + "' but the copy could not be found again. NOTHING WAS KEPT.");
        if (!target.ParameterDefinition.Equals(parameterId))
        {
            if (!target.IsValidParameterDefinitionId(parameterId))
                throw new InvalidOperationException("The copy of '" + copyFrom.Name + "' will not take '"
                    + wanted + "' as what it colours by. NOTHING WAS KEPT.");
            // Revit empties the rows when what a scheme colours by changes.
            target.ParameterDefinition = parameterId;
        }
        if (copyTitle.Length > 0) target.Title = copyTitle;
        copied = true;
        schemeSource = "'" + copyFrom.Name + "' was COPIED as '" + target.Name + "'"
            + " and only the copy was set" + (copyTitle.Length > 0 ? " to colour by " + wanted : " and re-coloured")
            + " - '" + copyFrom.Name + "' is unchanged";
    }
}
else if (refused.Length == 0 && !clearing && target != null)
{
    var users = otherUsers(target);
    schemeSource = "used the scheme already in the model that colours " + categoryName + " by "
        + (labelOf(target.ParameterDefinition) ?? wanted) + (recolouring ? "" : ", as it stands")
        + (users.Count > 0 ? " (" + users.Count + " other view(s) or template(s) show it"
            + (recolouring ? ", and their colours changed too, as asked by naming it" : "") + ")" : "");
}

if (refused.Length == 0 && !clearing && target != null)
{
    schemeName = target.Name;
    if (sample != null)
    {
        foreach (Parameter p in sample.Parameters)
        {
            if (!p.Id.Equals(target.ParameterDefinition) || p.Definition == null) continue;
            try { specOf = p.Definition.GetDataType(); } catch (Exception) { specOf = null; }
            break;
        }
    }

    // ---- 4a. set it on every view, and read it back -------------------------
    var spatialBefore = new Dictionary<ElementId, List<Tuple<ElementId, string, string>>>();
    foreach (var v in views)
    {
        var before = v.GetColorFillSchemeId(category.Id);
        // What else the view colour-fills now, so a spatial scheme Revit
        // replaces is named afterwards rather than vanishing unremarked.
        var others = new List<Tuple<ElementId, string, string>>();
        try
        {
            foreach (var id in v.SupportedColorFillCategoryIds())
            {
                if (id.Equals(category.Id)) continue;
                var shown = v.GetColorFillSchemeId(id);
                var otherCategory = Category.GetCategory(doc, id);
                if (!shown.Equals(ElementId.InvalidElementId) && doc.GetElement(shown) != null)
                    others.Add(Tuple.Create(id, otherCategory == null ? "another category" : otherCategory.Name,
                        doc.GetElement(shown).Name));
            }
        }
        catch (Exception) { }
        spatialBefore[v.Id] = others;
        try { v.SetColorFillSchemeId(category.Id, target.Id); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the scheme '" + target.Name + "' on "
                + factsOf(v) + ": " + ex.Message + " NOTHING WAS KEPT.");
        }
        if (!before.Equals(target.Id)) viewsChanged++;
    }
    doc.Regenerate();
    foreach (var v in views)
    {
        if (!v.GetColorFillSchemeId(category.Id).Equals(target.Id))
            throw new InvalidOperationException("The colour scheme for " + categoryName + " on " + factsOf(v)
                + " reads back as something other than '" + target.Name + "' after it was set. NOTHING WAS KEPT.");
        foreach (var was in spatialBefore[v.Id])
        {
            if (v.GetColorFillSchemeId(was.Item1).Equals(ElementId.InvalidElementId))
                findings.Add("'" + v.Name + "' showed " + was.Item2 + " in '" + was.Item3 + "'; Revit shows one "
                    + "spatial scheme per view, so " + categoryName + " replaced it");
        }
    }
    schemeApplied = true;

    // ---- 4b. a fresh copy Revit left empty gets one row per value shown -----
    var rows = target.GetEntries();
    if (rows.Count == 0 && copied
        && (target.StorageType == StorageType.String || target.StorageType == StorageType.ElementId))
    {
        // WHAT THE VIEWS SHOW, not what sits on their levels: a zone spans
        // levels and a view range reaches past its own.
        var seen = new List<string>();
        var seenIds = new List<ElementId>();
        foreach (var v in views.Where(w => !w.IsTemplate))
        {
            var shown = new List<Element>();
            try
            {
                shown = new FilteredElementCollector(doc, v.Id).OfCategoryId(category.Id)
                    .WhereElementIsNotElementType().ToList();
            }
            catch (Exception) { shown = new List<Element>(); }
            if (shown.Count == 0)
            {
                var level = v.GenLevel;
                shown = new FilteredElementCollector(doc).OfCategoryId(category.Id).WhereElementIsNotElementType()
                    .Where(e => level == null || e.LevelId.Equals(level.Id)).ToList();
            }
            foreach (var e in shown)
            {
                foreach (Parameter p in e.Parameters)
                {
                    if (!p.Id.Equals(target.ParameterDefinition)) continue;
                    if (target.StorageType == StorageType.String)
                    {
                        var s = p.AsString();
                        if (!string.IsNullOrWhiteSpace(s) && !seen.Contains(s)) seen.Add(s);
                    }
                    else
                    {
                        var id = p.AsElementId();
                        if (id != null && !id.Equals(ElementId.InvalidElementId) && !seenIds.Contains(id)) seenIds.Add(id);
                    }
                    break;
                }
            }
        }
        seen.Sort(StringComparer.OrdinalIgnoreCase);
        FillPatternElement solidFill = null;
        try
        {
            solidFill = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>()
                .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill
                                  && f.GetFillPattern().Target == FillPatternTarget.Drafting);
        }
        catch (Exception) { solidFill = null; }
        var total = seen.Count + seenIds.Count;
        var added = 0;
        for (var i = 0; i < total; i++)
        {
            var value = i < seen.Count ? seen[i] : "an element value";
            try
            {
                var entry = new ColorFillSchemeEntry(target.StorageType);
                if (i < seen.Count) entry.SetStringValue(seen[i]);
                else entry.SetElementIdValue(seenIds[i - seen.Count]);
                // Revit's own pale tints, unless colours asks otherwise below.
                entry.Color = new Color(230, 230, 230);
                if (solidFill != null) entry.FillPatternId = solidFill.Id;
                target.AddEntry(entry);
                added++;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not add the row for '" + value + "' to '"
                    + target.Name + "': " + ex.Message + " NOTHING WAS KEPT.");
            }
        }
        if (added > 0)
        {
            doc.Regenerate();
            findings.Add("Revit left the new copy with no rows, so " + added + " were added, one per value "
                + "the view(s) show" + (recolouring ? "" : ", in grey until coloured"));
        }
    }

    // ---- 4c. colours: the band, then each named value -----------------------
    if (recolouring)
    {
        FillPatternElement solid = null;
        try
        {
            solid = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>()
                .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill
                                  && f.GetFillPattern().Target == FillPatternTarget.Drafting);
        }
        catch (Exception) { solid = null; }

        // Hue to RGB, written out: no drawing library is guaranteed here.
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
            return new Color((byte)Math.Round((r + m) * 255.0), (byte)Math.Round((g + m) * 255.0),
                             (byte)Math.Round((b + m) * 255.0));
        };

        var current = target.GetEntries().ToList();
        var planned = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        if (bandWord.Length > 0 && current.Count > 0)
        {
            var saturation = bandWord == "pastel" ? 0.35 : (bandWord == "neon" ? 1.0 : 0.65);
            var brightness = bandWord == "pastel" ? 0.95 : (bandWord == "neon" ? 1.0 : 0.85);
            for (var i = 0; i < current.Count; i++)
                planned[keyOf(current[i])] = fromHue(i * 360.0 / current.Count, saturation, brightness);
        }
        foreach (var asked in askedColours)
        {
            // A named value overrides the band's colour for the same row.
            var row = current.FirstOrDefault(e => rowIs(e, asked.Key));
            planned[row == null ? asked.Key : keyOf(row)] = asked.Value;
        }

        foreach (var pair in planned)
        {
            var row = current.FirstOrDefault(e => rowIs(e, pair.Key));
            if (row == null)
            {
                if (target.StorageType != StorageType.String)
                    throw new InvalidOperationException("'" + target.Name + "' has no row for '" + pair.Key
                        + "' and colours by a value that is not text. NOTHING WAS KEPT.");
                try
                {
                    var entry = new ColorFillSchemeEntry(StorageType.String);
                    entry.SetStringValue(pair.Key);
                    entry.Color = pair.Value;
                    if (solid != null) entry.FillPatternId = solid.Id;
                    target.AddEntry(entry);
                    rowsRecoloured++;
                    findings.Add("'" + pair.Key + "' had no row, so one was added in its colour");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Revit would not add a row for '" + pair.Key + "' to '"
                        + target.Name + "': " + ex.Message + " NOTHING WAS KEPT.");
                }
                continue;
            }
            var was = row.Color;
            if (was != null && was.Red == pair.Value.Red && was.Green == pair.Value.Green && was.Blue == pair.Value.Blue)
                continue;
            row.Color = pair.Value;
            if (solid != null && (row.FillPatternId == null || row.FillPatternId.Equals(ElementId.InvalidElementId)))
                row.FillPatternId = solid.Id;
            try
            {
                if (target.CanUpdateEntry(row)) { target.UpdateEntry(row); rowsRecoloured++; }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not re-colour the row '" + pair.Key + "' in '"
                    + target.Name + "': " + ex.Message + " NOTHING WAS KEPT.");
            }
        }
        doc.Regenerate();

        // READ EACH COLOUR BACK. A row that reads back otherwise rolls it all back.
        var after = target.GetEntries().ToList();
        foreach (var pair in planned)
        {
            var row = after.FirstOrDefault(e => rowIs(e, pair.Key));
            if (row == null || row.Color == null || row.Color.Red != pair.Value.Red
                || row.Color.Green != pair.Value.Green || row.Color.Blue != pair.Value.Blue)
                throw new InvalidOperationException("The row '" + pair.Key + "' in '" + target.Name
                    + "' does not read back in the colour it was given. NOTHING WAS KEPT.");
        }
        if (bandWord.Length > 0 && current.Count == 0)
            findings.Add("the band '" + bandWord + "' had no rows to colour - the scheme has none yet");
    }

    // ---- 4d. the rows, read off the scheme ----------------------------------
    foreach (var row in target.GetEntries())
    {
        string value;
        try
        {
            value = row.StorageType == StorageType.String ? row.GetStringValue()
                : row.StorageType == StorageType.ElementId
                    ? (doc.GetElement(row.GetElementIdValue()) == null ? "(none)" : doc.GetElement(row.GetElementIdValue()).Name)
                : row.StorageType == StorageType.Integer ? row.GetIntegerValue().ToString()
                // A NUMBER IS WRITTEN IN THE PROJECT'S UNITS, as the dialog shows it.
                : specOf != null ? UnitFormatUtils.Format(doc.GetUnits(), specOf, row.GetDoubleValue(), false)
                : (!string.IsNullOrEmpty(row.Caption) ? row.Caption
                    : row.GetDoubleValue().ToString("0.###") + " (Revit internal units)");
        }
        catch (Exception) { value = row.Caption; }
        var c = row.Color;
        entries.Add((string.IsNullOrEmpty(value) ? row.Caption : value) + " - RGB " + c.Red + "," + c.Green + ","
            + c.Blue + (row.IsInUse ? "" : " (not in use)"));
    }
    if (entries.Count == 0)
        findings.Add("the scheme has no rows yet - the plan shows no colours until " + categoryName
            + " in it carry a " + wanted);

    // ---- 4e. the legend: once per view, beside the crop or at the point ------
    var placedAt = new List<string>();
    foreach (var v in views)
    {
        if (v.IsTemplate)
        {
            if (legendMode == "default" || legendMode == "point")
                findings.Add("'" + v.Name + "' is a view template, which carries no legend - place one in each "
                    + "view that follows it");
            continue;
        }
        if (legendMode == "none" || legendMode == "keep") continue;

        var box = v.CropBox;
        var scale = 1;
        try { scale = v.Scale > 0 ? v.Scale : 1; } catch (Exception) { scale = 1; }
        var paperMm = scale / 304.8;            // one millimetre on the sheet, in model feet
        // ON THE VIEW'S PLANE: the crop box's own z = 0, the plane version 1
        // placed its legends on.
        XYZ wantAt;
        if (legendMode == "point")
        {
            var local = box.Transform.Inverse.OfPoint(new XYZ(legendXMm / 304.8, legendYMm / 304.8, 0));
            wantAt = box.Transform.OfPoint(new XYZ(local.X, local.Y, 0));
        }
        else wantAt = box.Transform.OfPoint(new XYZ(box.Max.X + 10.0 * paperMm, box.Max.Y, 0));

        var already = legendsOf(v);
        ColorFillLegend placed = already.FirstOrDefault();
        if (placed != null)
        {
            if (legendMode == "point")
            {
                try { placed.Origin = wantAt; }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Revit would not move the colour fill legend in "
                        + factsOf(v) + ": " + ex.Message + " NOTHING WAS KEPT.");
                }
                findings.Add("'" + v.Name + "' already had a legend for " + categoryName + "; it was moved to the point");
            }
            else findings.Add("'" + v.Name + "' already had a legend for " + categoryName + ", so no second one was placed");
        }
        else
        {
            try { placed = ColorFillLegend.Create(doc, v.Id, category.Id, wantAt); }
            catch (Exception ex)
            {
                // THE LEGEND IS PART OF WHAT WAS ASKED FOR. A scheme set with
                // no legend is a coloured plan nobody can read.
                throw new InvalidOperationException("Revit would not place the colour fill legend in "
                    + factsOf(v) + ": " + ex.Message + " NOTHING WAS KEPT.");
            }
            if (placed == null)
                throw new InvalidOperationException("Revit placed no colour fill legend in " + factsOf(v)
                    + ". NOTHING WAS KEPT.");
            legendPlaced = true;
        }
        doc.Regenerate();

        // WHERE IT IS, READ OFF THE LEGEND.
        if (legendMode == "point")
        {
            placedAt.Add("'" + v.Name + "' at " + Math.Round(placed.Origin.X * 304.8).ToString("0") + ", "
                + Math.Round(placed.Origin.Y * 304.8).ToString("0") + " mm");
        }
        else
        {
            var local = box.Transform.Inverse.OfPoint(placed.Origin);
            placedAt.Add("'" + v.Name + "' " + Math.Round((local.X - box.Max.X) / paperMm, 1).ToString("0.#")
                + " mm right of the crop region's right edge and "
                + Math.Round((box.Max.Y - local.Y) / paperMm, 1).ToString("0.#")
                + " mm below its top edge, on the sheet at 1:" + scale);
        }
        bool cropOn = false, annotationOn = false;
        try { cropOn = v.CropBoxActive; } catch (Exception) { cropOn = false; }
        try
        {
            var p = v.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);
            annotationOn = p != null && p.AsInteger() == 1;
        }
        catch (Exception) { annotationOn = false; }
        if (cropOn && annotationOn)
            findings.Add("'" + v.Name + "': the annotation crop is ON, so a legend outside it can be cut off - "
                + "look at it, or switch Annotation Crop off");
        if (!cropOn && legendMode == "default")
            findings.Add("'" + v.Name + "': the crop region is OFF, so 'the right-hand edge' is the view's stored "
                + "crop box, which may sit far from what the plan shows");
    }
    legendAtMm = string.Join("; ", placedAt);
}

foreach (var v in views)
    if (!v.IsTemplate && category != null) legendsInView += legendsOf(v).Count;
#endif

// ---- one sentence the reply cannot truncate ---------------------------------
var allFacts = string.Join("; ", views.Select(v => factsOf(v)));
if (refused.Length > 0)
    summary = "NOT DONE on " + (allFacts.Length > 0 ? allFacts : "no view") + ": " + refused;
else if (clearing)
    summary = allFacts + ": " + categoryName + " colour scheme " + schemeSource
        + "; colour fill legends for it in these views now: " + legendsInView
        + (findings.Count > 0 ? ". Notes: " + string.Join("; ", findings) : "") + ".";
else
    summary = allFacts + ": " + categoryName + " coloured by " + wanted + " with '" + schemeName + "' ("
        + schemeSource + "); read back " + (schemeApplied ? "SET on every view" : "NOT SET") + " ("
        + viewsChanged + " changed); " + rowsRecoloured + " row(s) re-coloured; " + entries.Count + " row(s): "
        + string.Join("; ", entries.Take(20)) + (entries.Count > 20 ? "; and " + (entries.Count - 20) + " more" : "")
        + ". Legend " + (legendPlaced ? "PLACED" : "not placed") + "; colour fill legends for " + categoryName
        + " in these views now: " + legendsInView + (legendAtMm.Length > 0 ? " - " + legendAtMm : "")
        + (findings.Count > 0 ? ". Notes: " + string.Join("; ", findings) : "") + ".";
