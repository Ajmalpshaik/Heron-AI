// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves
// `attachedToRoof`, `attachedTargetUnread`, `unread`, `wallsRead`, `report`
// and `findings` behind. Reads only - no transaction, nothing changed.
//
// WHICH WALLS ARE ATTACHED TO A ROOF - Attach Top/Base, read back. The
// selection says what is asked: the roofs selected are the roofs asked about
// (none selected, any roof counts), and the walls selected are the walls read
// (none selected, every wall in the model is read). Anything else selected is
// named and left out.
//
// TWO WAYS TO READ IT, BY RELEASE, AND ONLY ONE SAYS WHAT A WALL IS ATTACHED
// TO. From Revit 2025, Wall.GetAttachmentIds names the element each end is
// attached to, so a wall is said to be attached to THIS roof only when that
// roof is the one named. Before 2025 there is no such member (RevitAPI.xml,
// 2020 to 2024): the wall's read-only "top is attached" and "base is
// attached" values say only THAT an end is attached - to a roof, a floor or a
// ceiling, Revit does not say. Those walls go in `attachedTargetUnread`, never
// in `attachedToRoof`: a guess at the target would be a made-up answer.
//
// A FAILED READ IS NEVER A "NO" (D-52). A wall whose attachments could not be
// read goes in `unread` with the reason, and is never counted as not attached.

var invariant = System.Globalization.CultureInfo.InvariantCulture;

var attachedToRoof = new List<ElementId>();
var attachedTargetUnread = new List<ElementId>();
var unread = new List<ElementId>();
var wallsRead = 0;
var findings = new List<string>();
var lines = new List<string>();

var notAttached = 0;
var attachedElsewhere = 0;

Func<Element, bool> isRoof = e => e != null
    && (e is RoofBase
        || (e.Category != null && e.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Roofs))));
Func<Element, string> nameOf = e =>
{
    if (e == null) return "an element not found in this model";
    var category = e.Category == null ? "element" : e.Category.Name;
    var typeName = doc.GetElement(e.GetTypeId()) is ElementType t ? t.Name : null;
    return typeName == null
        ? string.Format(invariant, "{0} {1}", category, e.Id)
        : string.Format(invariant, "{0} {1} \"{2}\"", category, e.Id, typeName);
};

var asked = elements ?? new List<Element>();
var fromSelection = asked.Count > 0;
var roofsAsked = new HashSet<ElementId>();
var walls = new List<Wall>();
foreach (var element in asked)
{
    if (element == null) continue;
    if (element is Wall selectedWall) walls.Add(selectedWall);
    else if (isRoof(element)) roofsAsked.Add(element.Id);
    else findings.Add(string.Format(invariant, "{0} is not a wall or a roof, so it was left out", nameOf(element)));
}
var wallsSelected = walls.Count > 0;
var roofText = roofsAsked.Count == 0
    ? "a roof"
    : roofsAsked.Count == 1 ? "the roof selected" : "one of the " + roofsAsked.Count + " roofs selected";

var nothingToAsk = fromSelection && walls.Count == 0 && roofsAsked.Count == 0;
if (!nothingToAsk && !wallsSelected)
{
    walls.AddRange(new FilteredElementCollector(doc).OfClass(typeof(Wall))
        .WhereElementIsNotElementType().Cast<Wall>());
}

#if REVIT2025_OR_GREATER
// FROM 2025: what each end is attached to, by element.
var roofsReached = new HashSet<ElementId>();
foreach (var wall in walls)
{
    IList<ElementId> top;
    IList<ElementId> bottom;
    try
    {
        top = wall.GetAttachmentIds(AttachmentLocation.Top) ?? new List<ElementId>();
        bottom = wall.GetAttachmentIds(AttachmentLocation.Base) ?? new List<ElementId>();
    }
    catch (Exception notRead)
    {
        // ONE WALL THAT CANNOT BE READ DOES NOT COST THE OTHERS THEIR ANSWER.
        unread.Add(wall.Id);
        findings.Add(string.Format(invariant, "{0}: its attachments could not be read - {1}",
            nameOf(wall), notRead.Message));
        continue;
    }
    wallsRead++;

    Func<ElementId, bool> counts = id => roofsAsked.Count > 0 ? roofsAsked.Contains(id) : isRoof(doc.GetElement(id));
    var topRoofs = top.Where(counts).ToList();
    var bottomRoofs = bottom.Where(counts).ToList();
    var others = top.Concat(bottom).Where(id => !counts(id)).Distinct().ToList();

    if (topRoofs.Count > 0 || bottomRoofs.Count > 0)
    {
        attachedToRoof.Add(wall.Id);
        foreach (var id in topRoofs.Concat(bottomRoofs)) roofsReached.Add(id);
        var ends = new List<string>();
        if (topRoofs.Count > 0)
            ends.Add("top attached to " + string.Join(" and ", topRoofs.Select(id => nameOf(doc.GetElement(id)))));
        if (bottomRoofs.Count > 0)
            ends.Add("base attached to " + string.Join(" and ", bottomRoofs.Select(id => nameOf(doc.GetElement(id)))));
        lines.Add(nameOf(wall) + ": " + string.Join("; ", ends));
    }
    else
    {
        notAttached++;
        if (others.Count > 0) attachedElsewhere++;
        if (wallsSelected)
        {
            lines.Add(others.Count > 0
                ? string.Format(invariant, "{0}: not attached to {1} - attached to {2}", nameOf(wall), roofText,
                    string.Join(" and ", others.Select(id => nameOf(doc.GetElement(id)))))
                : string.Format(invariant, "{0}: not attached to anything at its top or its base", nameOf(wall)));
        }
    }
}
foreach (var roofId in roofsAsked)
{
    if (!roofsReached.Contains(roofId))
        findings.Add(string.Format(invariant, "{0}: no wall read is attached to it", nameOf(doc.GetElement(roofId))));
}
#else
// BEFORE 2025: only whether each end is attached - not to what.
foreach (var wall in walls)
{
    Parameter topValue = null;
    Parameter bottomValue = null;
    try
    {
        topValue = wall.get_Parameter(BuiltInParameter.WALL_TOP_IS_ATTACHED);
        bottomValue = wall.get_Parameter(BuiltInParameter.WALL_BOTTOM_IS_ATTACHED);
    }
    catch (Exception notRead)
    {
        unread.Add(wall.Id);
        findings.Add(string.Format(invariant, "{0}: its attachments could not be read - {1}",
            nameOf(wall), notRead.Message));
        continue;
    }
    var topRead = topValue != null && topValue.HasValue;
    var bottomRead = bottomValue != null && bottomValue.HasValue;
    var topAttached = topRead && topValue.AsInteger() == 1;
    var bottomAttached = bottomRead && bottomValue.AsInteger() == 1;

    if (topAttached || bottomAttached)
    {
        wallsRead++;
        attachedTargetUnread.Add(wall.Id);
        var ends = topAttached && bottomAttached ? "top and base are" : topAttached ? "top is" : "base is";
        lines.Add(string.Format(invariant, "{0}: its {1} attached - to what, this Revit release does not say "
            + "(a roof, a floor or a ceiling)", nameOf(wall), ends));
    }
    else if (topRead && bottomRead)
    {
        wallsRead++;
        notAttached++;
        if (wallsSelected)
            lines.Add(string.Format(invariant, "{0}: not attached to anything at its top or its base", nameOf(wall)));
    }
    else
    {
        // NEVER A "NO": an end whose value is missing is not read as detached.
        unread.Add(wall.Id);
        findings.Add(string.Format(invariant, "{0}: whether its {1} is attached could not be read - the value "
            + "is missing", nameOf(wall), !topRead && !bottomRead ? "top or base" : !topRead ? "top" : "base"));
    }
}
#endif

var report = string.Join("\n", lines);
var scope = wallsSelected
    ? "of the " + walls.Count + " wall(s) selected"
    : "of the " + walls.Count + " wall(s) in this model";
if (nothingToAsk)
{
    findings.Insert(0, "Nothing selected is a wall or a roof, so no wall was read. Select the roof (or the walls) "
        + "and ask again, or select nothing to read every wall against every roof");
}
else if (walls.Count == 0)
{
    findings.Insert(0, "This model has no wall to read, so none is attached to a roof");
}
else
{
#if REVIT2025_OR_GREATER
    findings.Insert(0, string.Format(invariant,
        "{0} {1} attached to {2} at the top or the base; {3} read and attached to no {4}{5}{6}. Nothing changed.{7}",
        attachedToRoof.Count, scope, roofText, notAttached, roofsAsked.Count == 0 ? "roof" : "roof selected",
        attachedElsewhere > 0
            ? string.Format(invariant, " ({0} of them attached to something else - a floor, a ceiling or another roof)",
                attachedElsewhere)
            : "",
        unread.Count > 0
            ? string.Format(invariant, "; {0} could not be read, each named below - not the same as not attached",
                unread.Count)
            : "",
        report.Length == 0 ? "" : " " + report.Replace("\n", " | ")));
#else
    findings.Insert(0, string.Format(invariant,
        "{0} {1} have a top or base attached - BUT BEFORE REVIT 2025 REVIT DOES NOT SAY WHAT A WALL IS ATTACHED "
        + "TO, so whether it is {2} or a floor or a ceiling cannot be read here; {3} read and attached to nothing{4}. "
        + "Nothing changed.{5}",
        attachedTargetUnread.Count, scope, roofText, notAttached,
        unread.Count > 0
            ? string.Format(invariant, "; {0} could not be read, each named below - not the same as not attached",
                unread.Count)
            : "",
        report.Length == 0 ? "" : " " + report.Replace("\n", " | ")));
    if (roofsAsked.Count > 0)
        findings.Add("The roof(s) selected could not be checked against: on this Revit release no reading says which "
            + "element a wall is attached to. Revit 2025 and later can");
#endif
}
