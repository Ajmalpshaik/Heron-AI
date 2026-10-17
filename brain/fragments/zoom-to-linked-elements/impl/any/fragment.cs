// NOT STANDALONE. Assumes `uidoc`, `linkName`, `categories` and `alsoSelect`
// are in scope; leaves `shown`, `selected`, `skipped`, `viewAfter` and
// `findings` behind.
//
// NO TRANSACTION, and it needs none. It moves the camera of the active view
// and, when asked, the selection - the session, never the model, and never the
// linked file.
//
// WHY ZOOM_TO_ELEMENTS CANNOT DO THIS. Revit's Show takes ids in THIS document,
// and a linked element's id means nothing here - or names an unrelated host
// element that shares the number. So the boxes are read off the linked
// elements, put through the link's own transform into host coordinates, and
// the active view is zoomed to the box round all of them.
//
// THE ACTIVE VIEW, AND IT DOES NOT JUMP. Show picks a view for you; this
// cannot, because Revit has no call that finds a view showing a linked
// element. A sheet, a schedule or a legend has no model to zoom in, and is
// refused by name.
//
// SELECTING INSIDE A LINK NEEDS Selection.SetReferences, WHICH ARRIVED AT
// REVIT 2023. It is looked up by reflection when it is wanted, so one source
// compiles on every release (D-05): on 2020 to 2022 the camera still moves and
// the finding says the selection could not be set. A link reference is made
// with Reference.CreateLinkReference, present 2020 to 2027.
//
// THE LINK IS FOUND BY SELECT_FROM_LINK'S RULES - a name that matches nothing
// lists the links, one that matches several is AMBIGUOUS and refused, and an
// unloaded link is said to be unloaded rather than answered with a zero.

var shown = 0;
var selected = 0;
var skipped = 0;
var viewAfter = "";
var findings = new List<string>();

var doc = uidoc.Document;
var wanted = string.IsNullOrEmpty(linkName) ? "" : linkName.Trim();

var matches = new List<RevitLinkInstance>();
var allNames = new List<string>();

foreach (var instance in new FilteredElementCollector(doc)
    .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
{
    allNames.Add(instance.Name ?? "(unnamed)");
    if (wanted.Length == 0) continue;
    if ((instance.Name ?? "").IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
        matches.Add(instance);
}

var everyLink = allNames.Count == 0 ? "no links at all" : string.Join(", ", allNames.ToArray());

RevitLinkInstance link = null;
Document linked = null;

if (wanted.Length == 0)
{
    findings.Add("No link was named, so nothing was zoomed to. This model has: " + everyLink);
}
else if (matches.Count == 0)
{
    findings.Add("No link matches '" + wanted + "', so nothing was zoomed to. This model has: "
        + everyLink);
}
else if (matches.Count > 1)
{
    var named = new List<string>();
    foreach (var instance in matches) named.Add("'" + (instance.Name ?? "(unnamed)") + "'");
    findings.Add("AMBIGUOUS: " + matches.Count + " links match '" + wanted + "' - "
        + string.Join(", ", named.ToArray()) + ". Name one exactly; zooming to the first "
        + "would be a coin toss reported as an answer");
}
else
{
    linked = matches[0].GetLinkDocument();
    if (linked == null)
    {
        findings.Add("'" + matches[0].Name + "' is present but NOT LOADED, so there is nothing "
            + "in it to zoom to. Nothing was zoomed to");
    }
    else
    {
        link = matches[0];
    }
}

if (link != null && (categories == null || categories.Count == 0))
{
    // EVERY CATEGORY OF A LINK IS ITS LEVELS, GRIDS AND VIEWS TOO, and a box
    // round those is the whole site. A zoom to that is a zoom to nothing.
    findings.Add("No category was named, so nothing was zoomed to. Name what to show - "
        + "Structural Framing, Ducts, Walls - because a box round everything in a link "
        + "includes its levels and grids, which is the whole site");
    link = null;
}

View view = null;
UIView screen = null;

if (link != null)
{
    view = uidoc.ActiveView;

    // A VIEW WITH A MODEL IN IT. A plan, a section or an elevation, or a 3D
    // view - the only kinds a linked element can be drawn in at all.
    if (view == null || !(view is ViewPlan || view is ViewSection || view is View3D))
    {
        findings.Add("The active view" + (view == null ? "" : " \"" + view.Name + "\"")
            + " has no model to zoom in - a sheet, a schedule, a legend or a drafting view. "
            + "Open a plan, section or 3D view that shows the link and ask again. Nothing "
            + "was zoomed to");
        link = null;
    }
    else
    {
        foreach (var open in uidoc.GetOpenUIViews())
        {
            if (open != null && open.ViewId == view.Id) { screen = open; break; }
        }

        if (screen == null)
        {
            findings.Add("\"" + view.Name + "\" is not open in a window Revit can zoom, so "
                + "nothing was zoomed to");
            link = null;
        }
    }
}

if (link != null)
{
    var place = link.GetTotalTransform();

    var minX = double.MaxValue; var minY = double.MaxValue; var minZ = double.MaxValue;
    var maxX = double.MinValue; var maxY = double.MinValue; var maxZ = double.MinValue;

    var references = new List<Reference>();
    var unreferenced = 0;

    var collector = new FilteredElementCollector(linked)
        .WhereElementIsNotElementType()
        .WherePasses(new ElementMulticategoryFilter(categories));

    foreach (var element in collector)
    {
        if (element == null) continue;

        // A BOX IN THE LINK'S OWN COORDINATES. An element with no geometry has
        // none, and is counted rather than dropped in silence (D-52).
        var box = element.get_BoundingBox(null);
        if (box == null || box.Min == null || box.Max == null) { skipped++; continue; }

        // ALL EIGHT CORNERS, through the box's own transform and then the
        // link's. Two corners are not enough once the link is rotated: the
        // box's far corner in the link is not the far corner in the host.
        var own = box.Transform ?? Transform.Identity;
        foreach (var x in new[] { box.Min.X, box.Max.X })
        foreach (var y in new[] { box.Min.Y, box.Max.Y })
        foreach (var z in new[] { box.Min.Z, box.Max.Z })
        {
            var p = place.OfPoint(own.OfPoint(new XYZ(x, y, z)));
            if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
            if (p.Z < minZ) minZ = p.Z; if (p.Z > maxZ) maxZ = p.Z;
        }

        shown++;

        if (alsoSelect)
        {
            Reference inLink = null;
            try { inLink = new Reference(element).CreateLinkReference(link); }
            catch (Exception) { inLink = null; }
            if (inLink != null) references.Add(inLink); else unreferenced++;
        }
    }

    if (shown == 0)
    {
        findings.Add("'" + link.Name + "' has nothing of those categories with a box to zoom to"
            + (skipped > 0 ? " - " + skipped + " element(s) had no geometry" : "")
            + ". Nothing was zoomed to. SELECT_FROM_LINK says what is in it");
    }
    else
    {
        // A LITTLE ROOM ROUND THE BOX, IN REVIT'S INTERNAL FEET: one foot, or
        // a twentieth of the widest side, whichever is more. A single element
        // is a box with no depth in a plan, and a zero-height rectangle is
        // not one Revit can centre on.
        var widest = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
        var room = Math.Max(1.0, widest / 20.0);

        screen.ZoomAndCenterRectangle(new XYZ(minX - room, minY - room, minZ - room),
                                      new XYZ(maxX + room, maxY + room, maxZ + room));
        viewAfter = view.Name;

        findings.Add("Zoomed \"" + viewAfter + "\" to " + shown + " element(s) inside linked model '"
            + link.Name + "'" + (alsoSelect ? "" : ", without changing the selection")
            + ". Revit moves the camera; whether each one is DRAWN there is the view's - its crop, "
            + "its view range or section box, and its visibility settings for the link");

        if (alsoSelect)
        {
            // LOOKED UP WHEN WANTED. Absent before Revit 2023.
            var setReferences = uidoc.Selection.GetType().GetMethod("SetReferences",
                new[] { typeof(IList<Reference>) });

            if (setReferences == null)
            {
                findings.Add("The selection was NOT changed: this release of Revit cannot select "
                    + "inside a link from code (Selection.SetReferences arrived at Revit 2023). "
                    + "Tab onto them in the view to select them by hand");
            }
            else if (references.Count > 0)
            {
                setReferences.Invoke(uidoc.Selection, new object[] { references });
                selected = references.Count;
                findings.Add("Selected " + selected + " of them, as linked elements - the "
                    + "selection that was there before is replaced");
            }

            if (unreferenced > 0)
                findings.Add(unreferenced + " element(s) could not be referenced through the link "
                    + "and were zoomed to but not selected");
        }
    }

    if (skipped > 0 && shown > 0)
        findings.Add(skipped + " element(s) of those categories have no geometry to box, and were "
            + "skipped");

    findings.Add("THESE BELONG TO THE LINKED DOCUMENT. Nothing in it or in this model was changed, "
        + "and nothing here can change a link");
}
