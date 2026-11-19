// NOT STANDALONE. Assumes `doc`, `view`, `filterName`, `position` and
// `currentOrder` are in scope; leaves `moved`, `orderBefore`, `orderAfter`,
// `reAdded`, `orderVerified`, `findings` and `refused` behind.
//
// MOVES ONE FILTER UP OR DOWN A VIEW'S FILTER LIST - the order Visibility/
// Graphics > Filters shows, where the HIGHER filter wins when two set the same
// thing on the same element. Written 2026-11-19 for a request made on Revit
// 2020: a Duct Accessories filter set green, under two filters that also cover
// Duct Accessories and set black.
//
// NO RELEASE CAN SET THE ORDER. Revit 2021 and later READ it -
// `GetOrderedFilters` - and their reference says a filter added with
// `AddFilter` "is appended as the last filter in the order". So a move is made
// by taking filters off and putting them back. Each one taken off has its whole
// row read FIRST - overrides, Visibility, and from 2021 Enable Filter - written
// back, and read back again; anything that does not read back as it was stops
// the call, and the host rolls back everything it did.
//
// THE FEWEST FILTERS ARE MOVED. The asked order is the current one with the
// filter taken out and put in at its new row. The longest run at the TOP of the
// asked order whose filters already stand in that relative order stays on the
// view untouched; every filter after it is taken off and put back in the asked
// order. A move to the bottom re-adds one filter; a move to the top re-adds
// every filter that stood above it and below it.
//
// WHAT EACH RELEASE LETS IT CHECK - and it never claims more:
//   2021 to 2027  the order is Revit's own, read before and read back after,
//                 and must equal the order asked or the call stops. A typed
//                 `currentOrder` that differs from Revit's is refused as out
//                 of date - the modeller's reading, not the model, is wrong.
//   2020          the API has NO order read: `GetFilters` gives the filters
//                 in no order its 2020 reference states (from 2021 the
//                 reference says "sorted according to their ids", and 2020
//                 is ASSUMED the same until NEEDS-CHECKING OM10 measures it),
//                 never as the Filters tab shows
//                 them, so the order before is the modeller's own reading
//                 (`currentOrder`), checked to name exactly the filters on the
//                 view. Without it, only a move whose result does not depend
//                 on the order may run: a view of two filters or fewer, or a
//                 move to the bottom. The order after is WRITTEN, never read
//                 back - and that AddFilter appends at the bottom is ASSUMED
//                 on 2020 (NEEDS-CHECKING Group OM tests it). Overrides and
//                 Visibility are read back; 2020 has no Enable Filter tick.
//   NEVER: re-add in the 2020 API's id order and call that the dialog's order.
//
// THE 2021 MEMBERS ARE REACHED BY NAME. The add-in compiles fragments with no
// release symbols (FRAGMENT-ISSUES 5b-181), so `#if` would name a member Revit
// 2020 has not got. The same pattern as REPORT_VIEW_FILTERS (`getOrdered`) and
// APPLY_VIEW_FILTER (`setEnabled`, `getEnabled`).
//
// A TEMPLATE THAT OWNS THE FILTERS is refused, naming it: the order on the view
// would be reasserted by the template. The template itself may be named as the
// view, and then every view following it changes - the answer says how many.
//
// ALL CHECKING HAPPENS BEFORE THE FIRST WRITE. A refusal is RETURNED - moved
// false, both order lists EMPTY, reAdded 0 - never thrown. Only a failure in
// the middle of the write, or a read-back that differs, throws: an
// InvalidOperationException, and nothing this call did is kept.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).

var moved = false;
var orderBefore = new List<string>();
var orderAfter = new List<string>();
var reAdded = 0;
var orderVerified = "";
var findings = new List<string>();
var refused = new List<string>();

string refusal = null;

// ---- the 2021 members, by name: null on Revit 2020 ----------------------------

var getOrdered = typeof(View).GetMethod("GetOrderedFilters", Type.EmptyTypes);
var getEnabled = typeof(View).GetMethod("GetIsFilterEnabled", new[] { typeof(ElementId) });
var setEnabled = typeof(View).GetMethod("SetIsFilterEnabled", new[] { typeof(ElementId), typeof(bool) });
var release = doc.Application.VersionNumber;

// Revit's own words, not "Exception has been thrown by the target of an
// invocation" - a member reached by name wraps what Revit said.
Func<Exception, string> revitSaid = thrown => (thrown.InnerException ?? thrown).Message;

// ---- the view -----------------------------------------------------------------

ICollection<ElementId> onView = null;
var viewLabel = "";
if (view == null)
{
    refusal = "No view was given. Name the view whose filters are to be reordered, as the Project "
        + "Browser shows it - or a view template, to reorder every view following it.";
}
else
{
    viewLabel = view.IsTemplate
        ? string.Format("the view template '{0}'", view.Name)
        : string.Format("'{0}' ({1})", view.Name, view.ViewType);

    var overridesAllowed = true;
    try { overridesAllowed = view.AreGraphicsOverridesAllowed(); }
    catch (Exception) { overridesAllowed = false; }

    if (!overridesAllowed)
    {
        refusal = string.Format("{0} takes no graphic overrides, so it has no Filters tab and no filter "
            + "order to change. Nothing was changed.", viewLabel);
    }
    else
    {
        try { onView = view.GetFilters(); }
        catch (Exception thrown)
        {
            refusal = string.Format("{0} cannot carry filters - Revit says: {1}. Nothing was changed.",
                viewLabel, revitSaid(thrown));
        }
    }
}

// A VIEW TEMPLATE THAT OWNS THE FILTERS: the filters slot not among the
// parameters the TEMPLATE leaves out of its control. The list is read FROM THE
// TEMPLATE - Revit's reference, 2020 and 2027 alike, says it is the parameters
// "not marked as included when this view is used as a template", and the PROVEN
// REPORT_VIEW_TEMPLATE_CONTROL reads it the same way. Read off the view, it
// answers a question nobody asked (FRAGMENT-ISSUES 5b-1854). In a write a test
// that cannot be read is a refusal, never a pass.
if (refusal == null && !view.IsTemplate && view.ViewTemplateId != ElementId.InvalidElementId)
{
    try
    {
        var owner = doc.GetElement(view.ViewTemplateId) as View;
        if (owner == null)
            throw new InvalidOperationException("its view template could not be found");
        var leftFree = new HashSet<ElementId>(owner.GetNonControlledTemplateParameterIds());
        if (!leftFree.Contains(new ElementId(BuiltInParameter.VIS_GRAPHICS_FILTERS)))
        {
            var ownerName = owner.Name;
            refusal = string.Format("The view template '{0}' controls the filters on '{1}', so their order "
                + "is the template's - a change made on the view itself would not stick. Nothing was changed. "
                + "To change the order on every view following that template, ask again with '{0}' as the view.",
                ownerName, view.Name);
        }
    }
    catch (Exception thrown)
    {
        refusal = string.Format("Heron could not read whether the view template of '{0}' controls its filters "
            + "- Revit says: {1}. Nothing was changed: a reorder the template then reasserts would not stick.",
            view.Name, revitSaid(thrown));
    }
}

// ELEMENT OWNERSHIP ON A WORKSHARED MODEL (Article 12): a view borrowed by
// somebody else cannot be changed, and that is said, not thrown. A status that
// cannot be READ is let through, unlike the template test above: if the view
// is borrowed after all, Revit refuses the write itself, the call throws, and
// the host rolls everything back - nothing is left half done either way.
if (refusal == null && doc.IsWorkshared)
{
    try
    {
        string borrower;
        var status = WorksharingUtils.GetCheckoutStatus(doc, view.Id, out borrower);
        if (status == CheckoutStatus.OwnedByOtherUser)
        {
            refusal = string.Format("{0} is borrowed by {1}, so its filters cannot be reordered from here. "
                + "Nothing was changed - ask them to relinquish it, or reload latest once they have.",
                viewLabel, string.IsNullOrEmpty(borrower) ? "another user" : borrower);
        }
    }
    catch (Exception) { }
}

// ---- the filters on it, by name -----------------------------------------------

var nameOf = new Dictionary<ElementId, string>();
var listedById = new List<ElementId>();
if (refusal == null)
{
    foreach (var onId in onView)
    {
        var onFilter = doc.GetElement(onId) as FilterElement;
        nameOf[onId] = onFilter == null ? "(a filter Revit could not name)" : onFilter.Name;
        listedById.Add(onId);
    }
}

// A name among the filters ON THIS VIEW: exact first, then capitals ignored.
Func<string, List<ElementId>> onViewNamed = typedName =>
{
    var wanted = (typedName ?? "").Trim();
    var exact = listedById.Where(candidate => nameOf[candidate] == wanted).ToList();
    if (exact.Count > 0) return exact;
    return listedById.Where(candidate =>
        string.Equals(nameOf[candidate], wanted, StringComparison.OrdinalIgnoreCase)).ToList();
};

Func<IList<ElementId>, string> numbered = ids =>
{
    var lines = new List<string>();
    for (var at = 0; at < ids.Count; at++) lines.Add((at + 1) + ". " + nameOf[ids[at]]);
    return string.Join(", ", lines);
};

// What IS on the view, for a refusal: in the Filters tab's order where Revit
// can give it, and otherwise said to be in no particular order.
Func<string> whatIsOnTheView = () =>
{
    if (listedById.Count == 0) return string.Format("{0} carries no filters at all.", viewLabel);
    if (getOrdered != null)
    {
        try
        {
            var shown = getOrdered.Invoke(view, null) as IEnumerable<ElementId>;
            if (shown != null)
                return string.Format("{0} carries, top to bottom: {1}.", viewLabel, numbered(shown.ToList()));
        }
        catch (Exception) { }
    }
    return string.Format("{0} carries (in no particular order - Revit {1} cannot give the Filters tab's order): {2}.",
        viewLabel, release, string.Join(", ", listedById.Select(each => "'" + nameOf[each] + "'")));
};

// ---- the filter to move -------------------------------------------------------

ElementId target = null;
var targetName = "";
if (refusal == null)
{
    var askedName = (filterName ?? "").Trim();
    var everyFilter = new FilteredElementCollector(doc).OfClass(typeof(FilterElement))
        .Cast<FilterElement>().ToList();
    var inProject = everyFilter.Where(candidate => candidate.Name == askedName).ToList();
    if (inProject.Count == 0)
        inProject = everyFilter.Where(candidate =>
            string.Equals(candidate.Name, askedName, StringComparison.OrdinalIgnoreCase)).ToList();

    if (askedName.Length == 0)
    {
        refusal = "No filter was named. Give the name of the filter to move, as the Filters tab shows it. "
            + whatIsOnTheView();
    }
    else if (inProject.Count == 0)
    {
        refusal = string.Format("No filter called '{0}' in this project. Nothing was changed. {1}",
            askedName, whatIsOnTheView());
    }
    else if (inProject.Count > 1)
    {
        refusal = string.Format("{0} filters in this project are called '{1}' when capitals are ignored ({2}). "
            + "Type the name exactly as the Filters tab shows it. Nothing was changed.", inProject.Count,
            askedName, string.Join(", ", inProject.Select(each => "'" + each.Name + "'")));
    }
    else
    {
        var found = inProject[0];
        if (!listedById.Contains(found.Id))
        {
            refusal = string.Format("The filter '{0}' is in this project but is not on {1}, so there is "
                + "nothing to move. Nothing was changed. {2} To put it on the view, use APPLY_VIEW_FILTER - "
                + "it adds a filter at the bottom of the list.", found.Name, viewLabel, whatIsOnTheView());
        }
        else
        {
            target = found.Id;
            targetName = found.Name;
        }
    }
}

// ---- where to -----------------------------------------------------------------

var askedRow = 0;          // 1 = the top; meaningful when neither of the next two is set
var toBottom = false;
ElementId besideId = null; // "above X" / "below X"
var besideAbove = false;
if (refusal == null)
{
    var where = (position ?? "").Trim();
    var lower = where.ToLowerInvariant();
    if (lower.StartsWith("row ")) { where = where.Substring(4).Trim(); lower = where.ToLowerInvariant(); }

    int rowNumber;
    if (lower.Length == 0 || lower == "top" || lower == "first")
    {
        askedRow = 1;
    }
    else if (lower == "bottom" || lower == "last")
    {
        toBottom = true;
    }
    else if (lower.StartsWith("above ") || lower.StartsWith("below "))
    {
        besideAbove = lower.StartsWith("above ");
        var otherName = where.Substring(6).Trim();
        var others = onViewNamed(otherName);
        if (others.Count != 1)
        {
            refusal = string.Format("'{0}' {1}. Nothing was changed. {2}", otherName,
                others.Count == 0 ? "is not a filter on this view" : "names more than one filter on this view",
                whatIsOnTheView());
        }
        else if (others[0].Equals(target))
        {
            refusal = string.Format("'{0}' cannot be moved {1} itself - name another filter on the view. "
                + "Nothing was changed.", targetName, besideAbove ? "above" : "below");
        }
        else
        {
            besideId = others[0];
        }
    }
    else if (int.TryParse(where, out rowNumber))
    {
        if (rowNumber < 1)
            refusal = string.Format("Row {0} is not a row - row 1 is the top of the Filters tab. Nothing was "
                + "changed.", rowNumber);
        else
            askedRow = rowNumber;
    }
    else
    {
        refusal = string.Format("position \"{0}\" is not one Heron can read. Leave it blank or say top for row "
            + "1; bottom for the last row; a row number counted from the top; or \"above <filter>\" / "
            + "\"below <filter>\" naming another filter on the view. Nothing was changed.", position);
    }
}

// A row number past the end is the bottom - said only once the call has
// moved the filter or found it already there, never beside a refusal.
string pastEndNote = null;
if (refusal == null && askedRow > 0 && askedRow >= listedById.Count)
{
    if (askedRow > listedById.Count)
        pastEndNote = string.Format("Row {0} is past the end of {1}'s {2} filter(s), so it was read as the bottom.",
            askedRow, viewLabel, listedById.Count);
    toBottom = true;
}

// ---- the order now: Revit's from 2021, the modeller's on 2020 -------------------

// The modeller's reading. Left out, the card's `absent: none` arrives as one
// item, "none"; an empty list or blank items are left out too.
var typed = (currentOrder ?? new List<string>()).Select(each => (each ?? "").Trim())
    .Where(each => each.Length > 0).ToList();
if (typed.Count == 1 && string.Equals(typed[0], "none", StringComparison.OrdinalIgnoreCase)) typed.Clear();

List<ElementId> typedIds = null;
if (refusal == null && typed.Count > 0)
{
    var resolved = new List<ElementId>();
    var notOnView = new List<string>();
    var twice = new List<string>();
    foreach (var readName in typed)
    {
        var hits = onViewNamed(readName);
        if (hits.Count != 1) { notOnView.Add("'" + readName + "'"); continue; }
        if (resolved.Contains(hits[0])) { twice.Add("'" + nameOf[hits[0]] + "'"); continue; }
        resolved.Add(hits[0]);
    }
    var missing = listedById.Where(each => !resolved.Contains(each)).Select(each => "'" + nameOf[each] + "'").ToList();

    if (notOnView.Count > 0 || twice.Count > 0 || missing.Count > 0)
    {
        var problems = new List<string>();
        if (notOnView.Count > 0) problems.Add("not on this view: " + string.Join(", ", notOnView));
        if (twice.Count > 0) problems.Add("named twice: " + string.Join(", ", twice));
        if (missing.Count > 0) problems.Add("on the view but not in the list: " + string.Join(", ", missing));
        var comma = listedById.Any(each => nameOf[each].Contains(","))
            ? " A filter whose name holds a comma cannot be typed in this list - commas separate the names."
            : "";
        refusal = string.Format("The order given as currentOrder does not name exactly the filters on {0} - {1}. "
            + "Read the Filters tab top to bottom and give every filter once.{2} Nothing was changed. {3}",
            viewLabel, string.Join("; ", problems), comma, whatIsOnTheView());
    }
    else
    {
        typedIds = resolved;
    }
}

List<ElementId> before = null;   // the order now, top to bottom, where known
var fromRevit = false;
if (refusal == null && getOrdered != null)
{
    try
    {
        var shown = getOrdered.Invoke(view, null) as IEnumerable<ElementId>;
        if (shown == null) throw new InvalidOperationException("no list came back");
        before = shown.ToList();
        fromRevit = true;
    }
    catch (Exception thrown)
    {
        refusal = string.Format("Revit would not give the order of the filters on {0} - {1}. Nothing was changed.",
            viewLabel, revitSaid(thrown));
    }

    // Revit's ordered list and its plain one must name the same filters.
    if (refusal == null && (before.Count != listedById.Count || before.Any(each => !listedById.Contains(each))))
    {
        refusal = string.Format("Revit's two lists of the filters on {0} disagree, so the order cannot be "
            + "trusted. Nothing was changed.", viewLabel);
    }

    if (refusal == null && typedIds != null && !typedIds.SequenceEqual(before))
    {
        refusal = string.Format("The order given as currentOrder is not the order Revit holds now - it may have "
            + "changed since it was read. Revit's order, top to bottom: {0}. Nothing was changed; ask again "
            + "without currentOrder, or with Revit's order.", numbered(before));
    }
}
else if (refusal == null)
{
    before = typedIds;   // Revit 2020: the modeller's reading, or none
}

// REVIT 2020 WITH NO READING: only a move whose result does not depend on the
// unknown order may run.
var orderFree = listedById.Count <= 2 || toBottom;
if (refusal == null && before == null && !orderFree)
{
    refusal = string.Format("Revit {0} cannot tell Heron the order of the filters in Visibility/Graphics > "
        + "Filters - its list is not in the Filters tab's order - so moving '{1}' would put the other {2} filters in an order nobody "
        + "chose. Nothing was changed. Read the filters top to bottom off the Filters tab of {3} and give them "
        + "as currentOrder, separated by commas; a move to the bottom needs no reading. {4}", release,
        targetName, listedById.Count - 1, viewLabel, whatIsOnTheView());
}

// ---- the order asked, and the fewest filters to take off and put back -------------

List<ElementId> asked = null;          // the order wanted, where it is fully known
var putBack = new List<ElementId>();   // taken off and put back, in this order
if (refusal == null)
{
    if (before != null)
    {
        var rest = before.Where(each => !each.Equals(target)).ToList();
        int insertAt;
        if (besideId != null)
        {
            var besideAt = rest.IndexOf(besideId);
            insertAt = besideAbove ? besideAt : besideAt + 1;
        }
        else if (toBottom) insertAt = rest.Count;
        else insertAt = Math.Min(askedRow - 1, rest.Count);

        asked = new List<ElementId>(rest);
        asked.Insert(insertAt, target);

        if (!asked.SequenceEqual(before))
        {
            // The longest run at the top of the asked order already standing in
            // that relative order now stays where it is.
            var kept = 0;
            var lastAt = -1;
            while (kept < asked.Count)
            {
                var nowAt = before.IndexOf(asked[kept]);
                if (nowAt <= lastAt) break;
                lastAt = nowAt;
                kept++;
            }
            putBack.AddRange(asked.Skip(kept));
        }
    }
    else if (listedById.Count == 1)
    {
        asked = new List<ElementId>(listedById);   // one filter: already where it is
    }
    else if (listedById.Count == 2 && !toBottom
             && (askedRow == 1 || (besideId != null && besideAbove)))
    {
        // Two filters, order unknown, the target to the top: put the other back.
        var other = listedById.First(each => !each.Equals(target));
        asked = new List<ElementId> { target, other };
        putBack.Add(other);
    }
    else
    {
        // To the bottom, order unknown: only the target is taken off and put
        // back. With two filters the whole order is known after; with more,
        // the others keep an order Revit 2020 cannot tell.
        putBack.Add(target);
        if (listedById.Count == 2)
            asked = new List<ElementId> { listedById.First(each => !each.Equals(target)), target };
    }
}

// ---- already there: nothing written ----------------------------------------------

if (refusal == null && putBack.Count == 0)
{
    var row = asked.IndexOf(target) + 1;
    findings.Add(string.Format("'{0}' is already at row {1} of {2} on {3}. Nothing was changed.",
        targetName, row, asked.Count, viewLabel));
    if (pastEndNote != null) findings.Add(pastEndNote);
    orderBefore.AddRange(asked.Select(each => nameOf[each]));
    orderAfter.AddRange(asked.Select(each => nameOf[each]));
    orderVerified = fromRevit ? "read from Revit - nothing was written"
        : before != null ? "from the order you typed - Revit 2020 cannot read it; nothing was written"
        : "nothing to order - the only filter on the view";
}

// ---- snapshot EVERY filter that will be taken off, before the first write ---------

var keptLooks = new List<OverrideGraphicSettings>();
var keptVisible = new List<bool>();
var keptEnabled = new List<bool?>();
if (refusal == null && putBack.Count > 0)
{
    foreach (var snapId in putBack)
    {
        try
        {
            keptLooks.Add(new OverrideGraphicSettings(view.GetFilterOverrides(snapId)));
            keptVisible.Add(view.GetFilterVisibility(snapId));
            keptEnabled.Add(getEnabled == null ? (bool?)null
                : (bool)getEnabled.Invoke(view, new object[] { snapId }));
        }
        catch (Exception thrown)
        {
            refusal = string.Format("Heron could not read the settings of '{0}' on {1} before moving it - "
                + "Revit says: {2}. Nothing was changed: a filter whose row cannot be read cannot be put back "
                + "as it was.", nameOf[snapId], viewLabel, revitSaid(thrown));
            break;
        }
    }
}

// OverrideGraphicSettings compared by every public readable property, through
// reflection, so the comparison is the same on every release. A colour by
// whether it is set and then its RGB - reading Red on an unset colour throws -
// and everything else by Equals; an ElementId is never read as a number.
var looksProperties = typeof(OverrideGraphicSettings).GetProperties()
    .Where(prop => prop.CanRead && prop.GetIndexParameters().Length == 0 && prop.GetGetMethod() != null
        && !prop.GetGetMethod().IsStatic && prop.Name != "IsValidObject")
    .ToList();
Func<object, object, bool> sameValue = (first, second) =>
{
    if (first == null || second == null) return first == null && second == null;
    var firstColour = first as Color;
    var secondColour = second as Color;
    if (firstColour != null || secondColour != null)
    {
        if (firstColour == null || secondColour == null) return false;
        if (firstColour.IsValid != secondColour.IsValid) return false;
        if (!firstColour.IsValid) return true;
        return firstColour.Red == secondColour.Red && firstColour.Green == secondColour.Green
            && firstColour.Blue == secondColour.Blue;
    }
    return first.Equals(second);
};
Func<OverrideGraphicSettings, OverrideGraphicSettings, List<string>> looksDiffer = (wanted, got) =>
{
    var differing = new List<string>();
    foreach (var prop in looksProperties)
    {
        try
        {
            if (!sameValue(prop.GetValue(wanted, null), prop.GetValue(got, null))) differing.Add(prop.Name);
        }
        catch (Exception unread) { differing.Add(prop.Name + " (unreadable: " + revitSaid(unread) + ")"); }
    }
    return differing;
};

// ---- a view template named as the view: how many views follow it ------------------

if (refusal == null && putBack.Count > 0 && view.IsTemplate)
{
    var following = 0;
    foreach (var each in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
    {
        try { if (!each.IsTemplate && view.Id.Equals(each.ViewTemplateId)) following++; }
        catch (Exception) { }
    }
    var includesFilters = true;
    try
    {
        includesFilters = !new HashSet<ElementId>(view.GetNonControlledTemplateParameterIds())
            .Contains(new ElementId(BuiltInParameter.VIS_GRAPHICS_FILTERS));
    }
    catch (Exception) { }
    findings.Add(includesFilters
        ? string.Format("'{0}' is a view template: {1} view(s) follow it, and every one of them draws its filters "
            + "in this new order.", view.Name, following)
        : string.Format("'{0}' is a view template, but its V/G Overrides Filters is not included in it, so the "
            + "{1} view(s) following it keep their own filters and are not changed.", view.Name, following));
}

// ---- THE WRITE ----------------------------------------------------------------------

if (refusal == null && putBack.Count > 0)
{
    foreach (var offId in putBack)
    {
        try { view.RemoveFilter(offId); }
        catch (Exception thrown)
        {
            throw new InvalidOperationException(string.Format("Revit would not take the filter '{0}' off {1} to "
                + "move it - {2}. Nothing this call did is kept.", nameOf[offId], viewLabel, revitSaid(thrown)));
        }
    }

    for (var slot = 0; slot < putBack.Count; slot++)
    {
        var backId = putBack[slot];
        try
        {
            view.AddFilter(backId);
            view.SetFilterOverrides(backId, keptLooks[slot]);
            view.SetFilterVisibility(backId, keptVisible[slot]);
            if (setEnabled != null && keptEnabled[slot].HasValue)
                setEnabled.Invoke(view, new object[] { backId, keptEnabled[slot].Value });
        }
        catch (Exception thrown)
        {
            throw new InvalidOperationException(string.Format("Revit would not put the filter '{0}' back on {1} "
                + "with its settings - {2}. Nothing this call did is kept.", nameOf[backId], viewLabel,
                revitSaid(thrown)));
        }
    }

    // ---- READ BACK: the same filters, every row as it was, and the order --------------

    ICollection<ElementId> afterSet;
    try { afterSet = view.GetFilters(); }
    catch (Exception thrown)
    {
        throw new InvalidOperationException(string.Format("Revit would not list the filters on {0} after the "
            + "move - {1}. Nothing this call did is kept.", viewLabel, revitSaid(thrown)));
    }
    if (afterSet.Count != listedById.Count || listedById.Any(each => !afterSet.Contains(each)))
        throw new InvalidOperationException(string.Format("After the move {0} carries {1} filter(s) where it "
            + "carried {2}, or not the same ones. Nothing this call did is kept.", viewLabel, afterSet.Count,
            listedById.Count));

    for (var slot = 0; slot < putBack.Count; slot++)
    {
        var backId = putBack[slot];
        var differs = new List<string>();
        try
        {
            if (view.GetFilterVisibility(backId) != keptVisible[slot]) differs.Add("Visibility");
            if (getEnabled != null && keptEnabled[slot].HasValue
                && (bool)getEnabled.Invoke(view, new object[] { backId }) != keptEnabled[slot].Value)
                differs.Add("Enable Filter");
            differs.AddRange(looksDiffer(keptLooks[slot], view.GetFilterOverrides(backId)));
        }
        catch (Exception thrown)
        {
            throw new InvalidOperationException(string.Format("Revit would not read back the settings of '{0}' "
                + "on {1} after the move - {2}. Nothing this call did is kept.", nameOf[backId], viewLabel,
                revitSaid(thrown)));
        }
        if (differs.Count > 0)
            throw new InvalidOperationException(string.Format("'{0}' did not come back onto {1} as it was - {2} "
                + "differed after it was put back. Nothing this call did is kept.", nameOf[backId], viewLabel,
                string.Join(", ", differs)));
    }

    if (fromRevit)
    {
        List<ElementId> nowShown;
        try
        {
            var shown = getOrdered.Invoke(view, null) as IEnumerable<ElementId>;
            if (shown == null) throw new InvalidOperationException("no list came back");
            nowShown = shown.ToList();
        }
        catch (Exception thrown)
        {
            throw new InvalidOperationException(string.Format("Revit would not give the order of the filters on "
                + "{0} after the move - {1}. Nothing this call did is kept.", viewLabel, revitSaid(thrown)));
        }
        if (!nowShown.SequenceEqual(asked))
            throw new InvalidOperationException(string.Format("The filters on {0} did not end in the order asked. "
                + "Asked: {1}. Revit reads: {2}. Nothing this call did is kept.", viewLabel, numbered(asked),
                numbered(nowShown)));
        orderVerified = "read back from Revit";
        orderAfter.AddRange(nowShown.Select(each => nameOf[each]));
        if (before != null) orderBefore.AddRange(before.Select(each => nameOf[each]));
    }
    else
    {
        orderVerified = "written, not readable on Revit " + release;
        if (before != null) orderBefore.AddRange(before.Select(each => nameOf[each]));
        if (asked != null) orderAfter.AddRange(asked.Select(each => nameOf[each]));

        findings.Add(string.Format("Revit {0} cannot read the order of the filters back, so this order was "
            + "WRITTEN and could not be checked - look at Visibility/Graphics > Filters on {1}. Each filter put "
            + "back was read back with its Visibility and overrides as they were.", release, viewLabel));
        if (before != null)
            findings.Add("The order before is your own reading of the Filters tab; Revit " + release
                + " cannot confirm it. If that reading was out of date, the order now will not be the one asked.");
        else if (asked == null)
            findings.Add(string.Format("'{0}' is now the last filter; the other {1} keep the order they had, "
                + "which Revit {2} cannot tell Heron.", targetName, listedById.Count - 1, release));
        else if (listedById.Count == 2)
            findings.Add("Revit " + release + " cannot tell whether it was already there; the order was written "
                + "as asked.");
    }

    moved = true;
    reAdded = putBack.Count;
    if (pastEndNote != null) findings.Insert(0, pastEndNote);
    findings.Insert(0, string.Format("'{0}' moved to row {1} of {2} on {3}; {4} filter(s) taken off and put back "
        + "with their settings.", targetName, asked != null ? asked.IndexOf(target) + 1 : listedById.Count,
        listedById.Count, viewLabel, reAdded));
}

if (refusal != null) refused.Add(refusal);
