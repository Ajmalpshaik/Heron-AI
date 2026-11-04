// NOT STANDALONE. Assumes `doc`, `elements`, `setName`, `mode`, `newName`,
// `parameterName`, `setPrefix`, `values` and `updateExisting` are in scope, and
// leaves `memberCount`, `done`, `notASheet`, `refused`, `changed`, `unchanged`,
// `printRange`, `created`, `updated`, `setsUnchanged` and `skipped` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) - for the document side of it.
//
// EVERY ROUTE IS THE PRINT MANAGER, AND THERE IS NO OTHER.
//
// A sheet set is an element that cannot be constructed, renamed through its
// name, or removed with a delete. Creating, renaming and deleting all go
// through the print manager's sheet-set setting. It is an odd shape for a
// model change and it is simply how Revit exposes it.
//
// THE SETTING THROWS UNLESS THE RANGE IS SET FIRST.
//
// Asking for it while the print range is anything but "selected views" raises
// an error whose wording is about the print dialog and mentions no sheet set
// at all. Every mode below makes sure the range is "selected views" first -
// setting it only when it is not already.
//
// AND THE PRINT MANAGER IS NOT TRANSACTIONAL.
//
// The range stays changed even if the surrounding transaction rolls back: it
// is application state, not document state. Version 1 set it and never put it
// back, so an Undo left Revit's Print dialog on "Selected views/sheets" (found
// 2026-10-14). SINCE VERSION 2 THE RANGE IS READ FIRST AND PUT BACK at the end,
// in a finally, whatever happened in between - and `printRange` says which.
// It is put back on the same print manager it was changed on and Apply() is
// NOT called: Apply() pushes every local print setting to the global ones for
// all documents (RevitAPI.xml), a wider change than version 1 ever made.
// Whether the dialog reads it back is a question for the Revit run
// (NEEDS-CHECKING Group FA), not a thing this file can know.
//
// ADD AND REMOVE (version 2, FRAGMENT-ISSUES 5b-466).
//
// They change what an EXISTING set holds, under its own name, where version 1
// could only delete it and make it again. The set is made current, its members
// read back, the elements given merged in or taken out, and Save() called -
// the route RevitAPI.xml names for a change to a saved set. Save() throws when
// nothing changed, so a call that would change nothing is refused before it is
// asked, and so is one that would leave the set holding nothing.
//
// DUPLICATE (version 3, FRAGMENT-ISSUES 5b-1361).
//
// A copy of an existing set under `newName`, the original left as it is. The
// set is made current and SaveAs(newName) called - the same call create ends
// with, on a current set that already holds the original's views. "copy" is
// taken as duplicate. A new name already used by a set is refused before
// anything is asked. The original's members are read from the set itself
// before the copy; afterwards the copy AND the original are found again by
// name, read again and compared by id - so a SaveAs that renamed rather than
// copied would be refused, not reported done.
//
// BY-PARAMETER, AND THE IN-SESSION RULE FOR EVERY MODE (version 4,
// 2026-11-01). SYNC_SHEET_SETS_BY_PARAMETER (sync-sheet-sets-by-parameter,
// version 1) was merged into this card on the owner's word and is DEPRECATED.
// What it had and this card lacked is here now:
//
//   - mode "by-parameter" ("by parameter" and "sync" taken as the same): every
//     non-template sheet and view carrying `parameterName`, grouped by its
//     value; one set per value, named `setPrefix` + the value, many in one
//     call; `values` (";" between them) narrows it to those values;
//   - an existing set for a value is LEFT ALONE and reported in
//     `setsUnchanged` unless `updateExisting`, and then made to hold exactly
//     its value's sheets and views - Save() on it, Revert() when Revit does not
//     save, and a set already in step not saved again;
//   - a parameter no sheet or view has is refused naming it, and so is a value
//     asked for that no sheet carries, and so is a parameter every carrier has
//     empty;
//   - a value Revit will not make a set for goes to `skipped` with Revit's
//     reason, and the others are still made;
//   - THE SAFER RULE, TAKEN FOR EVERY MODE THAT WRITES: the current set and
//     the In-Session set's members are read before anything is touched, a new
//     set is written through the In-Session set, and both are put back in the
//     finally, before the range. Versions 1 to 3 wrote create's views onto
//     WHICHEVER SET WAS CURRENT - a saved set, if the Print dialog was on one -
//     and left the Print dialog on whatever set each mode made current. If the
//     current set and the In-Session members cannot be read, nothing is
//     written: what cannot be read cannot be put back;
//   - and create, like that card, reads the bool SaveAs returns (row 5b-1361
//     recorded that version 3's create did not) and never saves over a set
//     that already has the name - that card never wrote a new set under a
//     name in use, and duplicate already refused one.
//
// Every set written is found again BY ITS NAME afterwards and its members read
// from the element, never from whichever set the print manager has current.
//
// `setName` and `newName` are optional since version 4 so by-parameter can be
// asked with neither: left out, the card binds the word "none", read here as
// no name at all - and every mode that needs one refuses as it always did.

int memberCount = 0;
bool done = false;
var notASheet = new List<ElementId>();
string refused = "";
// Version 2. How many went in (add) or came out (remove) - 0 for every other
// mode. What was handed in and left as it was: already in the set for add,
// never in it for remove. And what happened to the print range.
int changed = 0;
var unchanged = new List<ElementId>();
string printRange = "";
// Version 4, by-parameter only: the sets made and brought back in step, each
// with what it holds; the existing sets left as they were, each saying why;
// and the values Revit would not make a set for, with Revit's reason.
string created = "";
string updated = "";
string setsUnchanged = "";
string skipped = "";

// "none" is the card's word for a name left out (version 4), so it is no name.
Func<string, string> givenName = text =>
{
    string trimmed = (text ?? "").Trim();
    return string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase) ? "" : trimmed;
};

string wanted = givenName(setName);
string operation = (mode ?? "").Trim().ToLowerInvariant();
if (operation == "copy") operation = "duplicate";
if (operation == "by parameter" || operation == "byparameter" || operation == "sync") operation = "by-parameter";

if (operation != "create" && operation != "rename" && operation != "delete"
    && operation != "add" && operation != "remove" && operation != "duplicate" && operation != "by-parameter")
    refused = "Mode '" + mode + "' is not one of create, rename, delete, add, remove, duplicate, by-parameter.";
else if (operation != "by-parameter" && wanted.Length == 0)
    refused = "No set name was given.";

// BY-PARAMETER, before the print manager is touched: every sheet and view that
// carries the parameter, by value - and the refusals that need no set at all.
string parameterWanted = givenName(parameterName);
string valuePrefix = setPrefix ?? "";
if (string.Equals(valuePrefix.Trim(), "none", StringComparison.OrdinalIgnoreCase)) valuePrefix = "";
string onlyText = (values ?? "").Trim();
if (string.Equals(onlyText, "all", StringComparison.OrdinalIgnoreCase)) onlyText = "";
var onlyValues = new HashSet<string>(onlyText.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(v => v.Trim()).Where(v => v.Length > 0), StringComparer.OrdinalIgnoreCase);
var byValue = new Dictionary<string, List<View>>(StringComparer.OrdinalIgnoreCase);

if (refused.Length == 0 && operation == "by-parameter")
{
    Func<Parameter, string> valueOf = parameter =>
    {
        if (parameter == null || !parameter.HasValue) return "";
        string text = "";
        try { text = parameter.StorageType == StorageType.String ? parameter.AsString() : parameter.AsValueString(); } catch { }
        return (text ?? "").Trim();
    };

    if (parameterWanted.Length == 0) refused = "No parameter name was given.";
    else
    {
        int carriers = 0;
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
        {
            var view = element as View;
            if (view == null || view.IsTemplate) continue;
            Parameter carried = null;
            try { carried = view.LookupParameter(parameterWanted); } catch { }
            if (carried == null) continue;
            carriers++;
            string value = valueOf(carried);
            if (value.Length == 0) continue;
            if (onlyValues.Count > 0 && !onlyValues.Contains(value)) continue;
            List<View> group;
            if (!byValue.TryGetValue(value, out group)) { group = new List<View>(); byValue[value] = group; }
            group.Add(view);
        }

        if (carriers == 0)
            refused = "No sheet or view in " + (doc.Title ?? "this model") + " has a parameter called '" + parameterWanted
                + "'. Name it exactly as the sheet's Properties show it. Nothing was changed.";
        else
        {
            var nobody = onlyValues.Where(v => !byValue.ContainsKey(v)).ToList();
            if (nobody.Count > 0)
                refused = "No sheet or view carries '" + parameterWanted + "' = "
                    + string.Join(", ", nobody.Select(v => "'" + v + "'")) + ". Nothing was changed.";
            else if (byValue.Count == 0)
                refused = "Every sheet and view with '" + parameterWanted + "' has it empty, so there is no value to make a "
                    + "set for. Nothing was changed.";
        }
    }
}

// A saved set found by its name, read from the element - never assumed to be
// the set the print manager has current.
Func<string, ViewSheetSet> findSet = lookFor =>
{
    foreach (var each in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
    {
        var hit = each as ViewSheetSet;
        if (hit == null) continue;
        string hitName = "";
        try { hitName = (hit.Name ?? "").Trim(); } catch { }
        if (string.Equals(hitName, (lookFor ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return hit;
    }
    return null;
};

if (refused.Length == 0)
{
    PrintManager printManager = null;
    ViewSheetSetting setting = null;
    // The range as it was, so it can be put back - and whether it was changed
    // at all, so a range that was never touched is not "put back".
    PrintRange rangeBefore = PrintRange.Select;
    bool rangeRead = false;
    bool rangeChanged = false;
    // Version 4. The current set and the In-Session set's own members, copied,
    // so they can be put back - writing through the In-Session set to make a
    // new set must not leave the Print dialog holding what was written.
    IViewSheetSet currentBefore = null;
    ViewSet inSessionBefore = null;
    bool sessionRead = false;
    // Whether this run wrote the In-Session set's members - only create and
    // by-parameter do. The In-Session selection is put back only then, so a
    // rename, delete, add, remove, duplicate or early refusal never rewrites it
    // (review of version 4, 2026-11-01).
    bool sessionWritten = false;
    // The set delete removed, so putting back a current set that no longer
    // exists is not reported as a failure.
    ElementId deletedId = ElementId.InvalidElementId;
    // The name the set written ends up under, for the read-back.
    string finalName = wanted;

    try
    {
        printManager = doc.PrintManager;
        rangeBefore = printManager.PrintRange;
        rangeRead = true;
        // First, or the setting itself throws with a message about the print
        // dialog that names nothing to do with sheet sets.
        if (rangeBefore != PrintRange.Select)
        {
            printManager.PrintRange = PrintRange.Select;
            rangeChanged = true;
        }
        setting = printManager.ViewSheetSetting;
        currentBefore = setting.CurrentViewSheetSet;
        inSessionBefore = new ViewSet();
        foreach (View member in setting.InSession.Views) if (member != null) inSessionBefore.Insert(member);
        sessionRead = true;
    }
    catch
    {
        refused = setting == null
            ? "The print manager would not open its sheet-set setting. Nothing was changed."
            : "Revit would not hand back the Print dialog's current set and its In-Session selection, so they could "
              + "not be put back afterwards - nothing was changed.";
    }

    try
    {
        if (setting != null && sessionRead)
        {
            if (operation == "by-parameter")
            {
                var sets = new Dictionary<string, ViewSheetSet>(StringComparer.OrdinalIgnoreCase);
                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
                {
                    var set = element as ViewSheetSet;
                    if (set == null) continue;
                    string name = "";
                    try { name = (set.Name ?? "").Trim(); } catch { }
                    if (name.Length > 0 && !sets.ContainsKey(name)) sets[name] = set;
                }

                var made = new List<string>();
                var brought = new List<string>();
                var left = new List<string>();
                var notMade = new List<string>();
                int holding = 0;

                var order = byValue.Keys.ToList();
                order.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (var value in order)
                {
                    var group = byValue[value];
                    string valueSetName = (valuePrefix + value).Trim();
                    var members = new ViewSet();
                    foreach (var view in group) members.Insert(view);

                    ViewSheetSet existing;
                    if (!sets.TryGetValue(valueSetName, out existing))
                    {
                        bool saved = false;
                        try
                        {
                            sessionWritten = true;
                            setting.CurrentViewSheetSet = setting.InSession;
                            setting.CurrentViewSheetSet.Views = members;
                            saved = setting.SaveAs(valueSetName);
                            if (!saved) notMade.Add("'" + valueSetName + "': Revit did not save it");
                        }
                        catch (Exception failure) { notMade.Add("'" + valueSetName + "': " + failure.Message); }

                        if (saved)
                        {
                            // Read back from the set itself, found by its name.
                            var madeSet = findSet(valueSetName);
                            int holds = -1;
                            try
                            {
                                if (madeSet != null)
                                {
                                    holds = 0;
                                    foreach (View member in madeSet.Views) if (member != null) holds++;
                                }
                            }
                            catch { holds = -1; }

                            if (madeSet == null)
                                notMade.Add("'" + valueSetName + "': Revit reported it saved, but no set by that name was found "
                                    + "afterwards - check File - Print - Settings");
                            else if (holds < 0)
                                made.Add("'" + valueSetName + "' (saved; Revit would not hand back what it holds)");
                            else
                            {
                                made.Add("'" + valueSetName + "' (" + holds + ")");
                                holding += holds;
                            }
                        }
                        continue;
                    }

                    var holdsIds = new HashSet<ElementId>();
                    try { foreach (View member in existing.Views) if (member != null) holdsIds.Add(member.Id); }
                    catch { notMade.Add("'" + existing.Name + "': its members could not be read, so it was not touched"); continue; }
                    var wantIds = new HashSet<ElementId>(group.Select(v => v.Id));
                    int goingIn = wantIds.Count(id => !holdsIds.Contains(id));
                    int comingOut = holdsIds.Count(id => !wantIds.Contains(id));

                    if (goingIn == 0 && comingOut == 0) { left.Add("'" + existing.Name + "' already in step"); continue; }
                    if (!updateExisting)
                    {
                        left.Add("'" + existing.Name + "' out of step (" + goingIn + " to add, " + comingOut
                            + " to take out) - left alone, updating existing sets was not asked");
                        continue;
                    }
                    try
                    {
                        setting.CurrentViewSheetSet = existing;
                        setting.CurrentViewSheetSet.Views = members;
                        if (setting.Save())
                        {
                            int holdsNow = 0;
                            try { foreach (View member in existing.Views) if (member != null) holdsNow++; }
                            catch { holdsNow = members.Size; }
                            brought.Add("'" + existing.Name + "' (" + goingIn + " in, " + comingOut + " out)");
                            holding += holdsNow;
                        }
                        else
                        {
                            notMade.Add("'" + existing.Name + "': Revit did not save the change");
                            try { setting.Revert(); } catch { }
                        }
                    }
                    catch (Exception failure)
                    {
                        notMade.Add("'" + existing.Name + "': " + failure.Message);
                        try { setting.Revert(); } catch { }
                    }
                }

                created = string.Join("; ", made);
                updated = string.Join("; ", brought);
                setsUnchanged = string.Join("; ", left);
                skipped = string.Join("; ", notMade);
                done = made.Count + brought.Count > 0;
                memberCount = holding;
                if (!done && notMade.Count == 0)
                    refused = "Every set for '" + parameterWanted + "' already exists" + (updateExisting ? " and is in step" : "")
                        + " - nothing was changed."
                        + (updateExisting ? "" : " To bring existing sets back in step, ask for updateExisting.");
                else if (!done)
                    refused = "No set for '" + parameterWanted + "' was saved - see skipped for Revit's reasons.";
            }
            else if (operation == "create")
            {
                var views = new ViewSet();
                foreach (var element in elements)
                {
                    var view = element as View;
                    if (view == null || view.IsTemplate)
                    {
                        if (element != null) notASheet.Add(element.Id);
                        continue;
                    }
                    views.Insert(view);
                    memberCount++;
                }

                if (memberCount == 0)
                    refused = "No views or sheets to put in the set.";
                else if (findSet(wanted) != null)
                    refused = "A sheet set called '" + wanted + "' already exists - nothing was saved. To change what it "
                        + "holds, use mode add or remove.";
                else
                {
                    try
                    {
                        // Version 4: through the In-Session set, never onto
                        // whichever saved set the Print dialog had current.
                        sessionWritten = true;
                        setting.CurrentViewSheetSet = setting.InSession;
                        setting.CurrentViewSheetSet.Views = views;
                        if (setting.SaveAs(wanted)) done = true;
                        else refused = "Revit did not save a set called '" + wanted + "'.";
                    }
                    catch { refused = "Revit refused to save a set called '" + wanted + "'."; }
                }
            }
            else
            {
                // Rename, delete, add and remove all work on the set made current
                // first.
                ViewSheetSet existing = null;
                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
                {
                    var candidate = element as ViewSheetSet;
                    if (candidate == null) continue;
                    string name = "";
                    try { name = candidate.Name ?? ""; } catch { }
                    if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = candidate;
                        break;
                    }
                }

                if (existing == null)
                    refused = "No sheet set called '" + wanted + "'.";
                else
                {
                    // A missing new name is refused before the Print dialog's
                    // current set is touched.
                    if (operation == "rename" && givenName(newName).Length == 0)
                        refused = "No new name was given for the rename.";
                    else if (operation == "duplicate" && givenName(newName).Length == 0)
                        refused = "No name was given for the copy of '" + wanted + "'.";

                    if (refused.Length == 0)
                    {
                        try { setting.CurrentViewSheetSet = existing; }
                        catch { refused = "Could not make '" + wanted + "' the current set."; }
                    }

                    if (refused.Length == 0 && operation == "delete")
                    {
                        ElementId goingId = existing.Id;
                        try { setting.Delete(); done = true; deletedId = goingId; }
                        catch { refused = "Revit refused to delete the set '" + wanted + "'."; }
                    }
                    else if (refused.Length == 0 && operation == "rename")
                    {
                        string renameTo = givenName(newName);
                        if (renameTo.Length == 0) refused = "No new name was given for the rename.";
                        else
                        {
                            try { setting.Rename(renameTo); done = true; finalName = renameTo; }
                            catch { refused = "Revit refused to rename '" + wanted + "'."; }
                        }
                    }
                    else if (refused.Length == 0 && operation == "duplicate")
                    {
                        string copyName = givenName(newName);
                        bool nameTaken = false;
                        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
                        {
                            string name = "";
                            try { name = element.Name ?? ""; } catch { }
                            if (string.Equals(name.Trim(), copyName, StringComparison.OrdinalIgnoreCase)) { nameTaken = true; break; }
                        }

                        // The original's members, read from the set itself before the
                        // copy is made - not from whichever set the print manager has
                        // current - so the copy is compared with what the original held.
                        var originalIds = new HashSet<ElementId>();
                        bool originalRead = true;
                        try
                        {
                            foreach (View member in existing.Views)
                                if (member != null) originalIds.Add(member.Id);
                        }
                        catch { originalRead = false; }

                        if (copyName.Length == 0)
                            refused = "No name was given for the copy of '" + wanted + "'.";
                        else if (nameTaken)
                            refused = "A sheet set called '" + copyName + "' already exists - nothing was copied.";
                        else if (!originalRead)
                            refused = "Revit would not hand back what '" + wanted + "' holds, so nothing was copied.";
                        else if (originalIds.Count == 0)
                            refused = "'" + wanted + "' holds nothing, so there is nothing to copy.";
                        else
                        {
                            try
                            {
                                if (!setting.SaveAs(copyName))
                                    refused = "Revit did not save a copy of '" + wanted + "' as '" + copyName + "'.";
                            }
                            catch { refused = "Revit refused to save a copy of '" + wanted + "' as '" + copyName + "'."; }

                            if (refused.Length == 0)
                            {
                                // The copy, found by its name and read from the element -
                                // never assumed to be the set SaveAs left current.
                                ViewSheetSet copy = null;
                                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
                                {
                                    var candidate = element as ViewSheetSet;
                                    string name = "";
                                    try { name = candidate == null ? "" : candidate.Name ?? ""; } catch { }
                                    if (candidate != null && string.Equals(name, copyName, StringComparison.Ordinal)) { copy = candidate; break; }
                                }

                                var copyIds = new HashSet<ElementId>();
                                bool copyRead = true;
                                try
                                {
                                    if (copy != null)
                                        foreach (View member in copy.Views)
                                            if (member != null) copyIds.Add(member.Id);
                                }
                                catch { copyRead = false; }

                                // And the original found again by its name and read again:
                                // a copy is only a copy while the set it came from is still
                                // there, holding what it held.
                                var originalAfter = new HashSet<ElementId>();
                                bool originalStill = false;
                                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
                                {
                                    var candidate = element as ViewSheetSet;
                                    string name = "";
                                    try { name = candidate == null ? "" : candidate.Name ?? ""; } catch { }
                                    if (candidate == null || !string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)) continue;
                                    try
                                    {
                                        foreach (View member in candidate.Views)
                                            if (member != null) originalAfter.Add(member.Id);
                                        originalStill = true;
                                    }
                                    catch { }
                                    break;
                                }

                                if (copy == null)
                                    refused = "Revit reported the copy saved, but no set called '" + copyName + "' was found "
                                        + "afterwards - check File - Print - Settings.";
                                else if (!copyRead)
                                    refused = "'" + copyName + "' was saved, but Revit would not hand back what it holds - check it "
                                        + "in File - Print - Settings.";
                                else if (!copyIds.SetEquals(originalIds))
                                    refused = "'" + copyName + "' was saved but holds " + copyIds.Count + " sheets and views where '"
                                        + wanted + "' holds " + originalIds.Count + " - check both in File - Print - Settings.";
                                else if (!originalStill || !originalAfter.SetEquals(originalIds))
                                    refused = "'" + copyName + "' was saved, but '" + wanted + "' could not be read back as it was "
                                        + "- check both in File - Print - Settings.";
                                else
                                {
                                    done = true;
                                    memberCount = copyIds.Count;
                                    finalName = copyName;
                                    // Version 3 made the copy current here; since
                                    // version 4 the current set is put back below.
                                }
                            }
                        }
                    }
                    else if (refused.Length == 0)
                    {
                        // What the set holds now, read from the set rather than
                        // assumed - and compared by id, never by name.
                        var members = new List<View>();
                        var memberIds = new HashSet<ElementId>();
                        bool membersRead = true;
                        try
                        {
                            foreach (View member in setting.CurrentViewSheetSet.Views)
                                if (member != null && memberIds.Add(member.Id)) members.Add(member);
                        }
                        catch { membersRead = false; }

                        // What was handed in, once each. A template cannot be in a
                        // set, so it goes to notASheet exactly as it does for create.
                        var given = new List<View>();
                        var givenIds = new HashSet<ElementId>();
                        foreach (var element in elements)
                        {
                            var view = element as View;
                            if (view == null || view.IsTemplate)
                            {
                                if (element != null) notASheet.Add(element.Id);
                                continue;
                            }
                            if (givenIds.Add(view.Id)) given.Add(view);
                        }

                        // Partitioned only when the set's members were read - a set
                        // that could not be read claims nothing about what is in it.
                        var views = new ViewSet();
                        if (membersRead && operation == "add")
                        {
                            foreach (var member in members) views.Insert(member);
                            foreach (var view in given)
                            {
                                if (memberIds.Contains(view.Id)) { unchanged.Add(view.Id); continue; }
                                views.Insert(view);
                                changed++;
                            }
                        }
                        else if (membersRead)
                        {
                            foreach (var view in given)
                                if (!memberIds.Contains(view.Id)) unchanged.Add(view.Id);
                            foreach (var member in members)
                            {
                                if (givenIds.Contains(member.Id)) { changed++; continue; }
                                views.Insert(member);
                            }
                        }

                        if (!membersRead)
                            refused = "Revit would not hand back what '" + wanted + "' holds, so nothing was changed.";
                        else if (given.Count == 0)
                            refused = "No views or sheets were given to " + operation
                                + (operation == "add" ? " to '" : " from '") + wanted + "'.";
                        else if (changed == 0)
                            refused = operation == "add"
                                ? "Every sheet and view given is already in '" + wanted + "' - nothing was added."
                                : "None of the sheets and views given is in '" + wanted + "' - nothing was taken out.";
                        else if (views.IsEmpty)
                            refused = "Taking these out would leave '" + wanted + "' holding nothing, so nothing was "
                                + "taken out. To remove the whole set, use mode delete.";
                        else
                        {
                            try
                            {
                                setting.CurrentViewSheetSet.Views = views;
                                if (setting.Save()) { done = true; memberCount = views.Size; }
                                else refused = "Revit did not save the change to '" + wanted + "'; nothing was changed.";
                            }
                            catch { refused = "Revit refused to save the change to '" + wanted + "'; nothing was changed."; }

                            // A change Revit did not save is not left waiting on
                            // the current set for the next print to pick up.
                            if (!done)
                            {
                                try { setting.Revert(); } catch { }
                            }
                        }

                        if (!done) changed = 0;
                    }
                }
            }

            // What the set holds NOW, read back rather than assumed from what was
            // sent - a view deleted since leaves a hole in it. Since version 4
            // read from the set FOUND BY ITS NAME, not from the current one.
            if (done && operation != "delete" && operation != "by-parameter")
            {
                try
                {
                    var written = findSet(finalName);
                    if (written != null)
                    {
                        int actual = 0;
                        foreach (View member in written.Views) if (member != null) actual++;
                        memberCount = actual;
                    }
                    else
                    {
                        // Revit said it was saved, but no set holds that name -
                        // never reported as done on Revit's word alone.
                        done = false;
                        changed = 0;
                        memberCount = 0;
                        refused = "Revit reported '" + finalName + "' saved, but no sheet set by that name can be found "
                            + "afterwards - it is not reported as done. Check File - Print - Settings.";
                    }
                }
                catch { }
            }
        }
    }
    finally
    {
        // PUT THE PRINT MANAGER BACK, whatever happened above - a refusal, a
        // throw, a rollback the caller does afterwards. Version 4: the
        // In-Session set's members, then the current set, then the range.
        string sessionNote = "";
        if (setting != null && sessionRead)
        {
            // Only when this run wrote it - otherwise it was never touched.
            bool sessionBack = !sessionWritten;
            if (sessionWritten)
            {
                try
                {
                    setting.CurrentViewSheetSet = setting.InSession;
                    setting.CurrentViewSheetSet.Views = inSessionBefore;
                    sessionBack = true;
                }
                catch { }
            }

            bool currentBack = false;
            bool currentGone = false;
            try
            {
                var wasElement = currentBefore as Element;
                currentGone = wasElement != null && !deletedId.Equals(ElementId.InvalidElementId)
                    && wasElement.Id.Equals(deletedId);
            }
            // Unreadable is not gone: try to put it back, and say so if that fails.
            catch { currentGone = false; }
            if (!currentGone)
            {
                try { if (currentBefore != null) setting.CurrentViewSheetSet = currentBefore; currentBack = true; } catch { }
            }
            else currentBack = true;

            if (!sessionBack || !currentBack)
                sessionNote = " The Print dialog's " + (!sessionBack ? "In-Session selection" : "current set")
                    + " could NOT be put back as it was - check File - Print - Settings.";
        }

        // The words are the Print dialog's own.
        Func<PrintRange, string> rangeWords = range =>
            range == PrintRange.Current ? "Current window"
            : range == PrintRange.Visible ? "Visible portion of current window"
            : range == PrintRange.Select ? "Selected views/sheets"
            : range.ToString();

        if (rangeChanged)
        {
            try
            {
                printManager.PrintRange = rangeBefore;
                printRange = "Print range put back to " + rangeWords(rangeBefore) + ", as it was before.";
            }
            catch
            {
                printRange = "Print range NOT put back: Revit refused to set it back. It was set to Selected "
                    + "views/sheets for this run and was " + rangeWords(rangeBefore) + " before. Check File - "
                    + "Print and set it back there.";
            }
        }
        else if (rangeRead && rangeBefore == PrintRange.Select)
            printRange = "Print range was already Selected views/sheets and is left there.";

        printRange += sessionNote;
    }
}
