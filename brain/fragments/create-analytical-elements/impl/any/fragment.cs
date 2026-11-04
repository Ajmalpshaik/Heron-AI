// NOT STANDALONE. Assumes `doc`, `elements`, `kind`, `points`, `level` and
// `type` are in scope, and leaves `created`, `createdIds`, `report`, `refused`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Analyze > Analytical Model: draws ONE analytical element -
//
//   member  a line between exactly two points       AnalyticalMember.Create
//   panel   a closed outline through three or more  AnalyticalPanel.Create
//           corners, the last joined back to the first
//   link    between exactly two analytical nodes    AnalyticalLink.Create
//           selected, of a link type named
//
// - and reads it back from the model: its id, a member's ends or a panel's
// corners in millimetres, a link's ends and its two nodes.
//
// POINTS ARRIVE IN FEET. They are typed "x,y,z; x,y,z" in millimetres and the
// add-in converts them (RevitFragment.ManyPoints); they are reported back in
// millimetres (/ 304.8 the other way, D-20).
//
// NEVER GUESSED (D-33). A level is used only when named, and then every
// point's height is measured up from it; a name that is not a level is refused
// with every level's name. A link's type is named - left out, refused with
// every analytical link type's name.
//
// NEVER DELETES OR CHANGES AN ELEMENT ALREADY THERE, physical or analytical.
// The two nodes a link joins are read, never moved.
//
// RELEASES: 2023-2027, the card's claim (tools/api-surface --members,
// 2026-11-02). AnalyticalMember and AnalyticalPanel do not exist on 2020 to
// 2022, so this is never compiled or run there - Heron refuses the card on
// those releases by name.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var created = 0;
var createdIds = new List<ElementId>();
var report = "";
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<XYZ, string> pt = p => p == null ? "?" : "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> describe = e => e == null ? "?" : (e.Category == null ? "" : e.Category.Name + " ") + quoted(e.Name) + " (" + e.Id + ")";
var ok = true;

// ---- Kind ----------------------------------------------------------------------
var k = (kind ?? "").Trim().ToLowerInvariant();
if (k == "analytical member" || k == "line" || k == "stick") k = "member";
if (k == "analytical panel" || k == "surface") k = "panel";
if (k == "analytical link") k = "link";
var isMember = k == "member";
var isPanel = k == "panel";
var isLink = k == "link";
if (!isMember && !isPanel && !isLink)
{
    refused.Add(quoted(kind) + " is not a kind of analytical element Heron draws - member, panel or link. Nothing was drawn.");
    ok = false;
}

// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
var levelName = (level ?? "").Trim();
if (levelName.ToLowerInvariant() == "none") levelName = "";
var typeName = (type ?? "").Trim();
if (typeName.ToLowerInvariant() == "none") typeName = "";

var given = (points ?? new List<XYZ>()).Where(p => p != null).ToList();
var picked = (elements ?? new List<Element>()).Where(e => e != null).ToList();

// ---- Level, when named ---------------------------------------------------------------
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
    .OrderBy(l => l.ProjectElevation).ToList();
Level onLevel = null;
if (ok && levelName.Length > 0)
{
    if (isLink)
    {
        refused.Add("A link joins two analytical nodes where they are, so a level means nothing to it - leave the level out. Nothing was drawn.");
        ok = false;
    }
    else
    {
        onLevel = levels.FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
        if (onLevel == null)
        {
            refused.Add("There is no level called " + quoted(levelName) + ". The levels are: "
                + (levels.Count == 0 ? "(none)" : string.Join(", ", levels.Select(l => quoted(l.Name) + " at " + mm(l.ProjectElevation) + " mm")))
                + ". Nothing was drawn.");
            ok = false;
        }
    }
}
if (ok && typeName.Length > 0 && !isLink)
{
    refused.Add("A type is only read for a link; an analytical " + k + " is drawn with Revit's own settings, which "
        + "its Properties show afterwards. Leave the type out. Nothing was drawn.");
    ok = false;
}

var lift = onLevel == null ? 0.0 : onLevel.ProjectElevation;
var at = given.Select(p => new XYZ(p.X, p.Y, p.Z + lift)).ToList();

// ---- Draw ------------------------------------------------------------------------------
Element made = null;
if (ok && isMember)
{
    if (at.Count != 2)
    {
        refused.Add("An analytical member is drawn between exactly two points; " + at.Count + " were given"
            + (at.Count == 0 ? "" : " (" + string.Join("; ", at.Select(pt)) + ")") + ". Type them as \"x,y,z; x,y,z\" in mm. Nothing was drawn.");
        ok = false;
    }
    else
    {
        Line line = null;
        try { line = Line.CreateBound(at[0], at[1]); }
        catch (Exception ex) { refused.Add("Revit cannot make a line from " + pt(at[0]) + " to " + pt(at[1]) + ": " + ex.Message + " Nothing was drawn."); ok = false; }
        if (ok && !AnalyticalMember.IsValidCurve(line))
        {
            refused.Add("Revit says a line from " + pt(at[0]) + " to " + pt(at[1]) + " cannot be an analytical member. Nothing was drawn.");
            ok = false;
        }
        if (ok)
        {
            try { made = AnalyticalMember.Create(doc, line); }
            catch (Exception ex) { refused.Add("Revit would not draw the analytical member: " + ex.Message + " Nothing was drawn."); ok = false; }
        }
    }
}
else if (ok && isPanel)
{
    if (at.Count < 3)
    {
        refused.Add("An analytical panel needs three or more corners; " + at.Count + " were given. Type them as "
            + "\"x,y,z; x,y,z; x,y,z\" in mm, in order round the outline. Nothing was drawn.");
        ok = false;
    }
    else
    {
        CurveLoop outline = new CurveLoop();
        try
        {
            for (var i = 0; i < at.Count; i++) outline.Append(Line.CreateBound(at[i], at[(i + 1) % at.Count]));
        }
        catch (Exception ex)
        {
            refused.Add("Revit cannot close an outline through " + string.Join("; ", at.Select(pt)) + ": " + ex.Message
                + " - two corners may be the same, or the outline crosses itself. Nothing was drawn.");
            ok = false;
        }
        if (ok)
        {
            try { made = AnalyticalPanel.Create(doc, outline); }
            catch (Exception ex)
            {
                refused.Add("Revit would not draw an analytical panel through " + string.Join("; ", at.Select(pt)) + ": "
                    + ex.Message + " - the corners must lie in one flat plane. Nothing was drawn.");
                ok = false;
            }
        }
    }
}
else if (ok && isLink)
{
    var hubs = picked.Where(e => { try { return AnalyticalLink.IsValidHub(doc, e.Id); } catch { return false; } }).ToList();
    var linkTypes = new FilteredElementCollector(doc).OfClass(typeof(AnalyticalLinkType)).Cast<AnalyticalLinkType>()
        .OrderBy(t => t.Name).ToList();
    var linkType = typeName.Length == 0 ? null
        : linkTypes.FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
    if (hubs.Count != 2 || picked.Count != 2)
    {
        refused.Add("A link joins exactly two analytical nodes, selected. " + picked.Count + " element(s) were selected"
            + (picked.Count == 0 ? "" : " (" + string.Join(", ", picked.Select(describe)) + ")") + ", " + hubs.Count
            + " of them an analytical node Revit can link. Select the two nodes and ask again. Nothing was drawn.");
        ok = false;
    }
    else if (linkType == null)
    {
        refused.Add((typeName.Length == 0 ? "No analytical link type was named" : "There is no analytical link type called " + quoted(typeName))
            + " - it is never guessed. The types are: "
            + (linkTypes.Count == 0 ? "(none)" : string.Join(", ", linkTypes.Select(t => quoted(t.Name)))) + ". Nothing was drawn.");
        ok = false;
    }
    else
    {
        try { made = AnalyticalLink.Create(doc, linkType.Id, hubs[0].Id, hubs[1].Id); }
        catch (Exception ex) { refused.Add("Revit would not link " + describe(hubs[0]) + " and " + describe(hubs[1]) + ": " + ex.Message + " Nothing was drawn."); ok = false; }
    }
}

// ---- Read it back ---------------------------------------------------------------------
if (made != null)
{
    try { doc.Regenerate(); } catch { }
    created = 1;
    createdIds.Add(made.Id);
    var line = "";
    if (made is AnalyticalMember)
    {
        Curve c = null;
        try { c = ((AnalyticalMember)made).GetCurve(); } catch { }
        line = "analytical member " + made.Id + (c == null ? ", its line not read back"
            : " from " + pt(c.GetEndPoint(0)) + " to " + pt(c.GetEndPoint(1)) + ", " + mm(c.Length) + " mm long");
    }
    else if (made is AnalyticalPanel)
    {
        var corners = new List<string>();
        try { foreach (Curve c in ((AnalyticalPanel)made).GetOuterContour()) corners.Add(pt(c.GetEndPoint(0))); } catch { }
        line = "analytical panel " + made.Id + (corners.Count == 0 ? ", its outline not read back"
            : " with " + corners.Count + " corners " + string.Join("; ", corners));
    }
    else if (made is AnalyticalLink)
    {
        var link = (AnalyticalLink)made;
        line = "analytical link " + made.Id + " from " + pt(link.Start) + " to " + pt(link.End)
            + ", nodes " + link.StartHubId + " and " + link.EndHubId + ", type " + quoted(typeName);
    }
    if (onLevel != null) line += ", heights measured from " + quoted(onLevel.Name) + " at " + mm(onLevel.ProjectElevation) + " mm";
    report = line + " - read back from the model";
    findings.Add("Drew " + line + ". It is tied to no physical element; ASSOCIATE_ANALYTICAL_ELEMENTS ties it to one.");
}
if (created == 0) findings.Add("No analytical element was drawn - the model is as it was.");
