// NOT STANDALONE. Assumes `doc`, `view`, `categories`, `onlyUsed`, `spacingMm`
// and `labels` are in scope; leaves `placed`, `placedIds`, `alreadyShown`,
// `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and opens none (Golden Rule 16): CopyElement,
// Parameter.Set, MoveElement and TextNote.Create all need one.
//
// A SYMBOL LEGEND - one legend component per door, window or fixture type, in a
// column, each with its type's name beside it (Annotate > Component > Legend
// Component, done once per type by hand otherwise).
//
// REVIT'S API CANNOT PLACE A NEW LEGEND COMPONENT, on any release 2020 to 2027:
// no creation method exists for one. What it CAN do is copy one already in
// the legend (ElementTransformUtils.CopyElement) and set the copy's Component
// parameter (BuiltInParameter.LEGEND_COMPONENT) to another type. So the legend
// must hold ONE component placed by hand - any family - and every other type
// is a copy of it. With none there, this refuses and says how to place one.
// It never makes the legend view either: Revit's API cannot make the first
// legend (DUPLICATE_VIEWS copies one that exists).
//
// A TYPE ALREADY SHOWN IS LEFT ALONE. Every component in the legend is read
// first, and a type one already shows is counted in `alreadyShown`, never
// placed twice - so asking again places nothing new.
//
// EACH COPY IS READ BACK. The Component parameter is read after it is set; a
// copy that does not show the type asked for is deleted and named in
// `skipped`, never left as a second copy of the one placed by hand.

var findings = new List<string>();
var refused = new List<string>();
var skipped = new List<string>();
var placedIds = new List<ElementId>();
var placed = 0;
var alreadyShown = 0;

var mmPerFoot = 304.8;

if (view == null)
    refused.Add("No legend view was named.");
else if (view.ViewType != ViewType.Legend)
    refused.Add("'" + view.Name + "' is a " + view.ViewType + " view, not a legend - legend components go only in a legend view.");

// THE CATEGORIES - INVALID is the binder's "none named".
var wantedCategories = new List<BuiltInCategory>();
if (categories != null)
    foreach (var c in categories)
        if (c != BuiltInCategory.INVALID && !wantedCategories.Contains(c)) wantedCategories.Add(c);
if (wantedCategories.Count == 0)
    refused.Add("No category was named - say which, for example Doors, Windows, Plumbing Fixtures.");

if (spacingMm < 0) refused.Add("The gap between components cannot be negative (" + spacingMm + " mm asked).");

// THE ONE PLACED BY HAND, AND WHAT THE LEGEND ALREADY SHOWS.
Element seed = null;
var shown = new HashSet<string>();
var lowest = double.MaxValue;
var leftmost = double.MaxValue;
if (refused.Count == 0)
{
    foreach (var e in new FilteredElementCollector(doc, view.Id)
        .OfCategory(BuiltInCategory.OST_LegendComponents).WhereElementIsNotElementType().ToElements())
    {
        if (e == null) continue;
        if (seed == null) seed = e;
        try
        {
            var p = e.get_Parameter(BuiltInParameter.LEGEND_COMPONENT);
            if (p != null) shown.Add(p.AsElementId().ToString());
        }
        catch { }
        try
        {
            var box = e.get_BoundingBox(view);
            if (box != null)
            {
                if (box.Min.Y < lowest) lowest = box.Min.Y;
                if (box.Min.X < leftmost) leftmost = box.Min.X;
            }
        }
        catch { }
    }
    if (seed == null)
        refused.Add("Legend '" + view.Name + "' holds no legend component, and Revit's API cannot place the first one. "
            + "Place one by hand - Annotate > Component > Legend Component, any family - then ask again: every type is copied from it.");
}

// THE TYPES TO SHOW - every type of the categories named, or (onlyUsed) only
// those with an instance placed in the model.
var wantedTypes = new List<ElementType>();
if (refused.Count == 0)
{
    foreach (var category in wantedCategories)
    {
        var used = new HashSet<string>();
        if (onlyUsed)
        {
            try
            {
                foreach (var instance in new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType().ToElements())
                {
                    var typeId = instance.GetTypeId();
                    if (typeId != null && typeId != ElementId.InvalidElementId) used.Add(typeId.ToString());
                }
            }
            catch { }
        }
        var ofCategory = new List<ElementType>();
        try
        {
            foreach (var t in new FilteredElementCollector(doc).OfCategory(category).WhereElementIsElementType().ToElements())
            {
                var type = t as ElementType;
                if (type == null) continue;
                if (onlyUsed && !used.Contains(type.Id.ToString())) continue;
                ofCategory.Add(type);
            }
        }
        catch { }
        if (ofCategory.Count == 0)
            findings.Add("No " + (onlyUsed ? "placed " : "") + "type of " + category.ToString().Replace("OST_", "")
                + " in this model" + (onlyUsed ? " - say all types to show the ones not placed as well" : "") + ".");
        wantedTypes.AddRange(ofCategory
            .OrderBy(t => t.FamilyName ?? "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Name ?? "", StringComparer.OrdinalIgnoreCase));
    }
    const int most = 200;
    if (wantedTypes.Count > most)
        refused.Add(wantedTypes.Count + " types match - more than " + most + " in one legend. Name fewer categories, or leave onlyUsed on.");
}

// PLACE: one copy of the seed per type, its Component set and read back, then
// stacked in a column under everything the legend held, its name beside it.
if (refused.Count == 0)
{
    var scale = view.Scale > 0 ? (double)view.Scale : 1.0;
    var gap = spacingMm / mmPerFoot * scale;
    var cursor = lowest == double.MaxValue ? 0.0 : lowest - gap;
    var left = leftmost == double.MaxValue ? 0.0 : leftmost;
    var textType = ElementId.InvalidElementId;
    if (labels)
    {
        try { textType = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType); } catch { }
        if (textType == null || textType == ElementId.InvalidElementId)
            findings.Add("The model has no default text type, so no type names were written beside the components.");
    }

    foreach (var type in wantedTypes)
    {
        var name = string.IsNullOrEmpty(type.FamilyName) ? type.Name : type.FamilyName + ": " + type.Name;
        if (shown.Contains(type.Id.ToString())) { alreadyShown++; continue; }

        ElementId copyId = null;
        try
        {
            var copies = ElementTransformUtils.CopyElement(doc, seed.Id, XYZ.Zero);
            if (copies != null) foreach (var id in copies) { copyId = id; break; }
        }
        catch (Exception ex) { skipped.Add(name + " - Revit would not copy the legend component: " + ex.Message); continue; }
        if (copyId == null) { skipped.Add(name + " - Revit made no copy of the legend component."); continue; }

        var copy = doc.GetElement(copyId);
        var why = "";
        try
        {
            var p = copy == null ? null : copy.get_Parameter(BuiltInParameter.LEGEND_COMPONENT);
            if (p == null || p.IsReadOnly) why = "the copy has no Component to set";
            else if (!p.Set(type.Id)) why = "Revit would not show this type in a legend";
            else if (!p.AsElementId().Equals(type.Id)) why = "the copy read back showing another type";
        }
        catch (Exception ex) { why = "Revit would not show this type in a legend: " + ex.Message; }
        if (why != "")
        {
            try { doc.Delete(copyId); } catch { }
            skipped.Add(name + " - " + why + ".");
            continue;
        }

        // STACK IT: its top on the cursor, its left on the legend's left edge.
        try
        {
            doc.Regenerate();
            var box = copy.get_BoundingBox(view);
            if (box != null)
            {
                ElementTransformUtils.MoveElement(doc, copyId, new XYZ(left - box.Min.X, cursor - box.Max.Y, 0));
                doc.Regenerate();
                box = copy.get_BoundingBox(view);
            }
            if (box != null)
            {
                if (textType != null && textType != ElementId.InvalidElementId)
                {
                    try { TextNote.Create(doc, view.Id, new XYZ(box.Max.X + gap, box.Max.Y, 0), name, textType); }
                    catch (Exception ex) { findings.Add("The name beside " + name + " could not be written: " + ex.Message); }
                }
                cursor = box.Min.Y - gap;
            }
            else findings.Add(name + " was placed but reported no size in the legend, so it sits on the one placed by hand - move it there.");
        }
        catch (Exception ex) { findings.Add(name + " was placed but could not be moved into the column: " + ex.Message); }

        placed++;
        placedIds.Add(copyId);
    }

    var seedView = "";
    try { var v = seed.get_Parameter(BuiltInParameter.LEGEND_COMPONENT_VIEW); if (v != null) seedView = v.AsValueString() ?? ""; } catch { }
    findings.Insert(0, placed + " legend component(s) placed in '" + view.Name + "'" + (labels ? ", each with its type's name beside it" : "")
        + "; " + alreadyShown + " type(s) the legend already showed were left alone; " + skipped.Count + " skipped."
        + (seedView == "" ? "" : " Each copy shows the view the one placed by hand shows (" + seedView + ") - change it on any component in Properties."));
}
else
{
    findings.Insert(0, "Nothing placed - " + refused[0]);
}
