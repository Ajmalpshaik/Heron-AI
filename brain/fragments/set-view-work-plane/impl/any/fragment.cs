// NOT STANDALONE. Assumes `doc`, `view`, `plane` and `display` are in scope,
// and leaves `after`, `changed`, `before`, `report`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT IT DOES. Revit's Architecture > Work Plane > Set and Show, for ONE
// view: its work plane taken from a level, a named reference plane or a grid
// the modeller names - never guessed (D-33) - and Show Work Plane on or off.
// The view's work plane is read before and after.
//
// THE VIEW. Left out (null), the view in front. Only the kinds the Work Plane
// dialog serves are taken - floor, ceiling, structural and area plans,
// sections, elevations, detail views and 3D views - an ALLOW list, because
// ViewType names differ between releases. A sheet, a schedule, a legend, a
// drafting view and any other kind are refused by name, as is a view
// template, before anything changes. Whether Revit itself lets a drafting
// view's or a legend's plane be set is NEEDS REAL REVIT; they are refused
// here because everything in them is drawn on the view's own plane.
//
// THE PLANE, BY NAME. The whole text is tried first as the name of a level,
// a reference plane (its own Name parameter, DATUM_TEXT - an unnamed plane
// cannot be named) or a grid, so "Level 2" is the level Level 2. Only when
// nothing has that name is a leading kind - "level", "reference plane",
// "ref plane", "grid", with or without Revit's " : " - taken off and the rest
// looked for among that kind alone. None found, or two of different kinds,
// refuses and lists what there is.
//
// ALREADY THE VIEW'S WORK PLANE - when the view's work plane was taken from
// that very element (SketchPlane.GetPlaneReference, ElementId to ElementId)
// no new work plane is made and the reply says so.
//
// READ BACK. After the set, the view's work plane must be the one made here
// and must lie on the datum - parallel to it and on it within 0.01 mm - or
// the call THROWS and the host rolls it all back. Revit gives no member that
// reads whether the work plane is SHOWN, so Show and Hide are done and said,
// never claimed as read back.
//
// MILLIMETRES OUT, * 304.8 (D-20). An ElementId is never read as a number.
//
// RELEASES (tools/api-surface --members, 2026-11-02): SketchPlane.Create(
// Document, ElementId) and (Document, Reference), SketchPlane.GetPlane and
// GetPlaneReference, View.SketchPlane get and set, View.ShowActiveWorkPlane /
// HideActiveWorkPlane, ReferencePlane.GetReference / Normal / GetPlane,
// Grid.Curve / IsCurved, Level.ProjectElevation, Document.GetElement(
// Reference) / ActiveView, View.ViewType / IsTemplate and BuiltInParameter.
// DATUM_TEXT read the same 2020 through 2027.

string after = "";
var changed = 0;
string before = "";
var report = "";
string refused = null;
var findings = new List<string>();

var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", culture);
var tolerance = 0.01 / 304.8;

Func<ReferencePlane, string> ownName = rp =>
{
    var p = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = p == null ? null : p.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// The element a work plane was taken from, or null when Revit does not say.
Func<SketchPlane, Element> hostOf = sp =>
{
    try
    {
        var reference = sp.GetPlaneReference();
        if (reference == null) return null;
        var host = doc.GetElement(reference);
        return host is SketchPlane ? null : host;
    }
    catch (Exception) { return null; }
};

Func<Element, string> kindAndName = e =>
{
    if (e is Level) return "Level : " + e.Name;
    if (e is ReferencePlane)
    {
        var n = ownName((ReferencePlane)e);
        return "Reference Plane : " + (n.Length == 0 ? "(unnamed)" : n);
    }
    if (e is Grid) return "Grid : " + e.Name;
    return (e.Category == null ? "Element" : e.Category.Name) + " : " + e.Name;
};

Func<Plane, string> where = p =>
{
    var n = p.Normal;
    if (Math.Abs(Math.Abs(n.Z) - 1) < 1e-6) return "horizontal at elevation " + mm(p.Origin.Z) + " mm";
    if (Math.Abs(n.Z) < 1e-6)
        return "vertical, facing " + Math.Round(n.X, 3).ToString(culture) + "," + Math.Round(n.Y, 3).ToString(culture)
            + ", through " + mm(p.Origin.X) + "," + mm(p.Origin.Y) + " mm";
    return "sloped, facing " + Math.Round(n.X, 3).ToString(culture) + "," + Math.Round(n.Y, 3).ToString(culture) + ","
        + Math.Round(n.Z, 3).ToString(culture);
};

Func<View, string> describe = v =>
{
    SketchPlane sp = null;
    try { sp = v.SketchPlane; } catch (Exception) { }
    if (sp == null) return "no work plane";
    var host = hostOf(sp);
    var said = host != null ? kindAndName(host) : "\"" + sp.Name + "\"";
    try { said += " - " + where(sp.GetPlane()); } catch (Exception) { }
    return said;
};

var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
    .OrderBy(l => l.ProjectElevation).ToList();
var refPlanes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
    .Where(rp => ownName(rp).Length > 0).ToList();
var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().ToList();
Func<string> planeList = () =>
    " Levels: " + (levels.Count == 0 ? "none" : string.Join(", ", levels.Select(l => l.Name)))
    + ". Named reference planes: " + (refPlanes.Count == 0 ? "none"
        : string.Join(", ", refPlanes.Select(ownName).OrderBy(n => n)))
    + ". Grids: " + (grids.Count == 0 ? "none" : string.Join(", ", grids.Select(g => g.Name).OrderBy(n => n))) + ".";

// Every element named `name`, of the kinds `kinds` allows ("" every kind).
Func<string, string, List<Element>> named = (name, kinds) =>
{
    var hits = new List<Element>();
    if (kinds == "" || kinds == "level")
        hits.AddRange(levels.Where(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)));
    if (kinds == "" || kinds == "reference plane")
        hits.AddRange(refPlanes.Where(rp => string.Equals(ownName(rp), name, StringComparison.OrdinalIgnoreCase)));
    if (kinds == "" || kinds == "grid")
        hits.AddRange(grids.Where(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)));
    return hits;
};

var target = view ?? doc.ActiveView;
var said = (plane ?? "").Trim();
var show = (display ?? "").Trim().ToLowerInvariant();
Element datum = null;

if (target == null)
    refused = "No view was named and none is in front. NOTHING WAS CHANGED.";
else if (target.IsTemplate)
    refused = "\"" + target.Name + "\" is a view template, which has no work plane of its own. NOTHING WAS CHANGED.";
else
{
    var kind = target.ViewType;
    var takes = kind == ViewType.FloorPlan || kind == ViewType.CeilingPlan || kind == ViewType.EngineeringPlan
        || kind == ViewType.AreaPlan || kind == ViewType.Section || kind == ViewType.Elevation
        || kind == ViewType.Detail || kind == ViewType.ThreeD;
    if (!takes)
    {
        string why;
        if (kind == ViewType.DrawingSheet) why = "a sheet, which has no work plane - name a view placed on it";
        else if (kind == ViewType.Schedule || kind == ViewType.ColumnSchedule || kind == ViewType.PanelSchedule)
            why = "a schedule, a table with no work plane";
        else if (kind == ViewType.Legend) why = "a legend, drawn on the view's own plane";
        else if (kind == ViewType.DraftingView) why = "a drafting view, drawn on the view's own plane";
        else why = "a " + kind + " view, not one the Work Plane dialog serves";
        refused = "\"" + target.Name + "\" is " + why + ". A work plane is set in a plan, section, elevation, detail "
            + "or 3D view. NOTHING WAS CHANGED.";
    }
}

if (refused == null && show != "" && show != "show" && show != "hide")
    refused = "\"" + display + "\" is not show or hide. NOTHING WAS CHANGED.";
if (refused == null && said.Length == 0 && show == "")
    refused = "Nothing was asked - name a level, reference plane or grid for the work plane, or say show or hide. "
        + "NOTHING WAS CHANGED." + planeList();

if (refused == null && said.Length > 0)
{
    var hits = named(said, "");
    if (hits.Count == 0)
    {
        var lower = said.ToLowerInvariant();
        foreach (var prefix in new[] { "reference plane", "ref plane", "level", "grid" })
        {
            // The kind must be a whole word: "level 2" is "level" + "2", "levels" is nothing.
            if (!lower.StartsWith(prefix + " ") && !lower.StartsWith(prefix + ":")) continue;
            var rest = said.Substring(prefix.Length).Trim();
            if (rest.StartsWith(":")) rest = rest.Substring(1).Trim();
            if (rest.Length == 0) continue;
            hits = named(rest, prefix == "ref plane" ? "reference plane" : prefix);
            break;
        }
    }
    if (hits.Count == 0)
        refused = "Nothing in this project named \"" + said + "\" can be a work plane - a level, a named reference "
            + "plane or a grid - and it is never guessed. NOTHING WAS CHANGED." + planeList();
    else if (hits.Count > 1)
        refused = "\"" + said + "\" names more than one: " + string.Join(", ", hits.Select(kindAndName))
            + " - say which, as \"reference plane " + said + "\" or \"level " + said + "\". NOTHING WAS CHANGED.";
    else if (hits[0] is Grid && (((Grid)hits[0]).IsCurved || !(((Grid)hits[0]).Curve is Line)))
        refused = "Grid \"" + hits[0].Name + "\" is an arc, and an arc has no flat plane to work on. NOTHING WAS CHANGED.";
    else
        datum = hits[0];
}

if (refused == null && datum == null && show == "show")
{
    SketchPlane current = null;
    try { current = target.SketchPlane; } catch (Exception) { }
    if (current == null)
        refused = "\"" + target.Name + "\" has no work plane yet, so there is none to show - name a level, reference "
            + "plane or grid to set it to. NOTHING WAS CHANGED." + planeList();
}

if (refused == null)
{
    before = describe(target);
    var rows = new List<string>();

    if (datum != null)
    {
        SketchPlane current = null;
        try { current = target.SketchPlane; } catch (Exception) { }
        var currentHost = current == null ? null : hostOf(current);
        if (currentHost != null && currentHost.Id == datum.Id)
        {
            rows.Add(kindAndName(datum) + " is already the work plane of \"" + target.Name + "\"; it was not set again");
        }
        else
        {
            SketchPlane made;
            try
            {
                made = datum is ReferencePlane
                    ? SketchPlane.Create(doc, ((ReferencePlane)datum).GetReference())
                    : SketchPlane.Create(doc, datum.Id);
                target.SketchPlane = made;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused " + kindAndName(datum) + " as the work plane of \""
                    + target.Name + "\": " + ex.Message + " NOTHING from this call was kept.");
            }

            // READ BACK - the view's work plane is the one made, and it lies on
            // the datum.
            XYZ facing, point;
            if (datum is Level)
            {
                facing = XYZ.BasisZ;
                point = new XYZ(0, 0, ((Level)datum).ProjectElevation);
            }
            else if (datum is ReferencePlane)
            {
                facing = ((ReferencePlane)datum).Normal;
                point = ((ReferencePlane)datum).GetPlane().Origin;
            }
            else
            {
                var line = (Line)((Grid)datum).Curve;
                facing = XYZ.BasisZ.CrossProduct(line.Direction).Normalize();
                point = line.GetEndPoint(0);
            }
            SketchPlane now = null;
            try { now = target.SketchPlane; } catch (Exception) { }
            var lies = false;
            if (now != null && now.Id == made.Id)
            {
                var p = now.GetPlane();
                lies = Math.Abs(Math.Abs(p.Normal.DotProduct(facing)) - 1) < 1e-6
                    && Math.Abs((p.Origin - point).DotProduct(facing)) < tolerance;
            }
            if (!lies)
                throw new InvalidOperationException("The work plane of \"" + target.Name + "\" read back as "
                    + describe(target) + ", not " + kindAndName(datum) + ". NOTHING from this call was kept.");
            changed++;
            rows.Add("work plane set to " + kindAndName(datum));
        }
    }

    if (show == "show" || show == "hide")
    {
        try
        {
            if (show == "show") target.ShowActiveWorkPlane();
            else target.HideActiveWorkPlane();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not " + show + " the work plane in \"" + target.Name
                + "\": " + ex.Message + " NOTHING from this call was kept.");
        }
        changed++;
        rows.Add("work plane " + (show == "show" ? "shown" : "hidden") + " - Revit gives no way to read whether a "
            + "work plane is shown, so this is not read back; look at the view");
    }

    after = describe(target);
    report = "\"" + target.Name + "\" (" + target.ViewType + "): " + string.Join(" || ", rows)
        + " || work plane before: " + before + " || after, read back: " + after;
    findings.Add(report);
}

if (refused != null) findings.Add(refused);
