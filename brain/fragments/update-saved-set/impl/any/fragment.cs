// NOT STANDALONE. Assumes `doc`, `elements`, `setName` and `mode` are in
// scope, and leaves `setSize`, `changed`, `missing`, `deleted` and `refused`
// behind.
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
// CREATING IS REFUSED ON PURPOSE.
//
// Adding to a set that does not exist is nearly always a name typed
// differently. Creating one here would leave two sets a letter apart, each
// holding half the elements, and neither obviously wrong.

int setSize = 0;
int changed = 0;
int missing = 0;
bool deleted = false;
string refused = "";

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

if (wanted.Length == 0)
    refused = "No set name was given.";
else if (set == null)
    refused = "No saved set called '" + wanted + "'. Creating one is CREATE_SELECTION_FILTER - " +
              "a name typed differently would otherwise leave two sets, each holding half the work.";
else if (operation != "add" && operation != "remove" && operation != "replace" && operation != "delete")
    refused = "Mode '" + mode + "' is not one of add, remove, replace, delete.";

if (refused.Length == 0)
{
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
