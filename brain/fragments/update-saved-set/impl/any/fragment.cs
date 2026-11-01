// NOT STANDALONE. Assumes `doc`, `elements`, `setName` and `mode` are in
// scope, and leaves `setSize`, `changed`, `missing`, `deleted`, `refused`,
// `filterId` and `findings` behind.
//
// VERSION 2 (2026-11-01) - CREATE_SELECTION_FILTER MERGED IN, AS MODE "create".
// A new saved set of the elements handed in, under `setName`, its contents read
// back - Revit drops what it will not keep, and the count that matters is what
// the set really holds. An empty set is a legitimate thing to make. It is
// applied to NO view; deciding where it shows is APPLY_VIEW_FILTER's job.
//
// THE SAFER RULE WHERE THE TWO CARDS DIFFERED. CREATE_SELECTION_FILTER, handed
// a name that already existed, REPLACED that set's contents and said so. Here
// "create" with a name already in use is REFUSED, nothing written, and the
// refusal points at mode "replace" - the word that says the old contents go.
// A filter's name is shared with the rule-based view filters, so a view filter
// of that name is refused too, by name, rather than left to Revit's error.
//
// `filterId` - the set's id, for the one created and for an existing set
// edited. `findings` - what was done, in words.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Editing a saved set is a model
// change and belongs in the caller's undo entry with everything else.
//
// THE SET IS AN ELEMENT IN THE MODEL.
//
// Not a list held here. It outlives the session, everybody on the job sees it,
// and Revit keeps it as elements come and go - which is exactly why the
// membership is read fresh and written back rather than remembered.
//
// A SET OUTLIVES WHAT IS IN IT.
//
// Somebody deletes forty beams and the set still names them. Those ids resolve
// to nothing, and a set that quietly shrinks looks like somebody edited it.
// They are counted and reported, and they are not written back - a set kept
// clean is what makes the next read trustworthy.
//
// CREATING IS ASKED FOR BY NAME, NEVER DONE ON THE WAY.
//
// Adding to a set that does not exist is nearly always a name typed
// differently. Creating one there would leave two sets a letter apart, each
// holding half the elements, and neither obviously wrong - so "add" to a
// missing set is still refused, and the refusal names mode "create".

int setSize = 0;
int changed = 0;
int missing = 0;
bool deleted = false;
string refused = "";
var filterId = ElementId.InvalidElementId;
var findings = new List<string>();

string wanted = (setName ?? "").Trim();
string operation = (mode ?? "").Trim().ToLowerInvariant();

SelectionFilterElement set = null;
foreach (var candidate in new FilteredElementCollector(doc)
             .OfClass(typeof(SelectionFilterElement))
             .Cast<SelectionFilterElement>())
{
    if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
    {
        set = candidate;
        break;
    }
}

// A view filter - the rule-based kind - shares the name space (version 2).
FilterElement sameName = null;
if (set == null && wanted.Length > 0)
{
    foreach (var candidate in new FilteredElementCollector(doc)
                 .OfClass(typeof(FilterElement)).Cast<FilterElement>())
    {
        if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
        {
            sameName = candidate;
            break;
        }
    }
}

var known = operation == "add" || operation == "remove" || operation == "replace"
    || operation == "delete" || operation == "create";

if (wanted.Length == 0)
    refused = "No set name was given - a saved set with no name cannot be found again.";
else if (!known)
    refused = "Mode '" + mode + "' is not one of add, remove, replace, delete, create.";
else if (operation == "create" && set != null)
    refused = "A saved set called '" + set.Name + "' already exists, so NOTHING was written. "
        + "To put these in it instead of what it holds, ask again with mode \"replace\"; to add "
        + "them, mode \"add\"; or name a new set.";
else if (operation == "create" && sameName != null)
    refused = "A view filter called '" + sameName.Name + "' already exists, and a saved set "
        + "cannot share its name, so NOTHING was written. Name the set something else.";
else if (operation != "create" && set == null)
    refused = "No saved set called '" + wanted + "'. To make one, ask with mode \"create\" - "
        + "a name typed differently would otherwise leave two sets, each holding half the work.";

if (refused.Length == 0 && operation == "create")
{
    // CREATE_SELECTION_FILTER's job: a new set, its contents read back.
    var ids = new List<ElementId>();
    foreach (var element in elements) if (element != null && !ids.Contains(element.Id)) ids.Add(element.Id);
    try
    {
        set = SelectionFilterElement.Create(doc, wanted);
        set.SetElementIds(ids);
        filterId = set.Id;
        var held = set.GetElementIds();
        setSize = held == null ? 0 : held.Count;
        changed = setSize;
        findings.Add(string.Format("'{0}' was created and holds {1} element(s){2}. It is applied "
            + "to NO view - deciding where it shows is a separate job", wanted, setSize,
            setSize == ids.Count ? "" : string.Format(" - {0} were handed in, so Revit kept {1} "
                + "of them", ids.Count, setSize)));
    }
    catch (Exception ex)
    {
        refused = "Revit refused to create the set '" + wanted + "': " + ex.Message;
    }
}
else if (refused.Length == 0)
{
    filterId = set.Id;
    if (operation == "delete")
    {
        try
        {
            doc.Delete(set.Id);
            deleted = true;
        }
        catch { refused = "Revit refused to delete the set '" + wanted + "'."; }
    }
    else
    {
        // What is in there now, and how much of it is still real.
        var current = new List<ElementId>();
        try { current.AddRange(set.GetElementIds()); } catch { }

        var live = new List<ElementId>();
        foreach (var id in current)
        {
            if (doc.GetElement(id) != null) live.Add(id);
            else missing++;
        }

        var wantedIds = new List<ElementId>();
        foreach (var element in elements) if (element != null) wantedIds.Add(element.Id);

        var next = new List<ElementId>();

        if (operation == "replace")
        {
            next.AddRange(wantedIds);
            changed = wantedIds.Count;
        }
        else if (operation == "add")
        {
            next.AddRange(live);
            foreach (var id in wantedIds)
            {
                if (next.Contains(id)) continue;   // already in it: not a change
                next.Add(id);
                changed++;
            }
        }
        else
        {
            var drop = new HashSet<ElementId>(wantedIds);
            foreach (var id in live)
            {
                if (drop.Contains(id)) { changed++; continue; }
                next.Add(id);
            }
        }

        try
        {
            set.SetElementIds(next);
            // Read it back: the count the model holds, not the list just sent.
            setSize = set.GetElementIds().Count;
        }
        catch { refused = "Revit refused to write the set '" + wanted + "'."; }
    }
}

if (refused.Length > 0) findings.Add(refused);
else if (deleted) findings.Add("The saved set '" + wanted + "' was deleted - the elements in it are untouched.");
else if (operation != "create")
    findings.Add(string.Format("'{0}' ({1}): {2} changed, it now holds {3}{4}", wanted, operation,
        changed, setSize, missing > 0
            ? string.Format("; {0} id(s) in it no longer resolve and were not written back", missing)
            : ""));
