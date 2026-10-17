// NOT STANDALONE. Assumes `doc` is in scope, and leaves `sheetSets`,
// `setCount`, `emptySets`, `sameSets` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THIS EXISTS (FRAGMENT-ISSUES row 5b-465, 2026-10-14). Asked "how many
// sets are there" on a Revit 2020 job model, Heron had nothing that read the
// saved View/Sheet Sets: MANAGE_SHEET_SETS makes, renames and deletes them and
// reads none back, and the words reached REPORT_VIEWPORT_TYPES. The answer was
// got by a scratch read, and this is that read made a fragment.
//
// A SAVED SET IS AN ELEMENT, AND IT IS COLLECTED LIKE ONE. Every set the
// Print and Export dialogs list by name is a ViewSheetSet in the document. The
// dialog's "<in-session>" entry is not saved in the model and is not an
// element (RevitAPI.xml names it InSessionViewSheetSet, a separate class), so
// it is never counted here - a model with no saved set answers 0, which is
// what Revit's own dialog shows once the in-session entry is set aside.
//
// A SHEET IS A VIEW, SO THE TWO ARE TOLD APART BY CLASS. A set's members come
// back as one list of views, and a sheet is a view too. A member that is a
// ViewSheet is counted and named as a sheet ("number - name"); every other
// member is a view, named with its type in the Project Browser's words.
//
// AN EMPTY SET IS FLAGGED, NOT DROPPED. A set whose views have all been
// deleted still sits in the print dialog under its name and prints nothing.
// That is the row somebody choosing a set before an issue needs to see.
//
// TWO SETS HOLDING EXACTLY THE SAME SHEETS AND VIEWS ARE FLAGGED (version 2,
// 2026-10-14 finding, FRAGMENT-ISSUES 5b-545). On a real job two sets held the
// same 8 sheets and this read named both without a word: one is usually left
// over, and an issue printed from the wrong one looks right. Members are
// compared by ElementId's own equality, as sets - the print ORDER a set keeps
// from 2023 is not read, so two sets with the same members in a different
// order are still the same. Empty sets are flagged EMPTY instead, and a set
// whose members could not be read is compared with nothing.
//
// SORTED BY NAME, as the dialog lists them; sheets inside a set by number,
// views by name. A collector's order is creation order and means nothing.
//
// ONE STRING, BECAUSE A LIST REACHES THE REPLY CUT TO THREE ENTRIES OF SIXTY
// CHARACTERS. Sets are joined with " || ", members inside a set with "; ".
// Every member is named - the whole list is the answer.

var findings = new List<string>();
var sheetSets = "";
var setCount = 0;
var emptySets = "";
var sameSets = "";

// The view type in the words the Project Browser uses. Anything not named
// here is said as Revit names it, never dropped.
Func<View, string> typeWords = view =>
{
    switch (view.ViewType)
    {
        case ViewType.FloorPlan: return "Floor Plan";
        case ViewType.CeilingPlan: return "Reflected Ceiling Plan";
        case ViewType.EngineeringPlan: return "Structural Plan";
        case ViewType.AreaPlan: return "Area Plan";
        case ViewType.Elevation: return "Elevation";
        case ViewType.Section: return "Section";
        case ViewType.Detail: return "Detail View";
        case ViewType.ThreeD: return "3D View";
        case ViewType.DraftingView: return "Drafting View";
        case ViewType.Legend: return "Legend";
        case ViewType.Schedule: return "Schedule";
        case ViewType.Rendering: return "Rendering";
        default: return view.ViewType.ToString();
    }
};

var sets = new List<ViewSheetSet>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
{
    var set = element as ViewSheetSet;
    if (set != null) sets.Add(set);
}

Func<ViewSheetSet, string> nameOf = set =>
{
    try { return set.Name ?? ""; } catch (Exception) { return ""; }
};

sets.Sort((a, b) => string.Compare(nameOf(a), nameOf(b), StringComparison.OrdinalIgnoreCase));

var rows = new List<string>();
var empties = new List<string>();
var unread = new List<string>();
int sheetsOnly = 0, viewsOnly = 0, mixed = 0;
// For the same-members check: each listed set's row, name, member ids, and
// its counts in words ("8 sheets, 0 views").
var compared = new List<Tuple<int, string, HashSet<ElementId>, string>>();

foreach (var set in sets)
{
    var name = nameOf(set);
    if (name.Length == 0) name = "(a set Revit gave no name)";

    var sheets = new List<ViewSheet>();
    var views = new List<View>();
    var membersRead = true;
    try
    {
        var members = set.Views;
        if (members != null)
        {
            foreach (var member in members)
            {
                var sheet = member as ViewSheet;
                if (sheet != null) { sheets.Add(sheet); continue; }
                var view = member as View;
                if (view != null) views.Add(view);
            }
        }
    }
    catch (Exception)
    {
        membersRead = false;
    }

    if (!membersRead)
    {
        unread.Add(name);
        rows.Add(name + " - its sheets and views could not be read");
        continue;
    }

    // Sheet numbers are text, not numbers - "M-101", "A2.03" - so they are
    // compared as text, the way LIST_SHEETS orders the drawing list.
    sheets.Sort((a, b) => string.Compare(a.SheetNumber ?? "", b.SheetNumber ?? "",
                                         StringComparison.OrdinalIgnoreCase));
    views.Sort((a, b) => string.Compare(a.Name ?? "", b.Name ?? "",
                                        StringComparison.OrdinalIgnoreCase));

    if (sheets.Count == 0 && views.Count == 0)
    {
        empties.Add(name);
        rows.Add(name + " - EMPTY: no sheet and no view");
        continue;
    }

    if (views.Count == 0) sheetsOnly++;
    else if (sheets.Count == 0) viewsOnly++;
    else mixed++;

    var ids = new HashSet<ElementId>();
    foreach (var sheet in sheets) ids.Add(sheet.Id);
    foreach (var view in views) ids.Add(view.Id);

    var row = name + " - " + sheets.Count + " sheet" + (sheets.Count == 1 ? "" : "s")
        + ", " + views.Count + " view" + (views.Count == 1 ? "" : "s");
    compared.Add(Tuple.Create(rows.Count, name, ids, row.Substring(name.Length + 3)));

    if (sheets.Count > 0)
        row += " - sheets: " + string.Join("; ",
            sheets.Select(s => (s.SheetNumber ?? "") + " - " + (s.Name ?? "")).ToArray());
    if (views.Count > 0)
        row += " - views: " + string.Join("; ",
            views.Select(v => (v.Name ?? "") + " (" + typeWords(v) + ")").ToArray());
    rows.Add(row);
}

// Groups of sets holding exactly the same members, in name order. A set
// joins the first group whose members are its members; one group is one row
// of `sameSets`.
var groups = new List<List<Tuple<int, string, HashSet<ElementId>, string>>>();
foreach (var entry in compared)
{
    List<Tuple<int, string, HashSet<ElementId>, string>> home = null;
    foreach (var group in groups)
    {
        var first = group[0].Item3;
        if (first.Count == entry.Item3.Count && first.SetEquals(entry.Item3)) { home = group; break; }
    }
    if (home == null) groups.Add(new List<Tuple<int, string, HashSet<ElementId>, string>> { entry });
    else home.Add(entry);
}

var sameRows = new List<string>();
foreach (var group in groups)
{
    if (group.Count < 2) continue;
    var names = group.Select(g => g.Item2).ToList();
    sameRows.Add(string.Join(" = ", names.ToArray()));
    for (int i = 0; i < group.Count; i++)
    {
        var others = names.Where((n, j) => j != i).ToArray();
        // The flag goes straight after the set's name, where EMPTY goes.
        rows[group[i].Item1] = group[i].Item2 + " - SAME sheets and views as " + string.Join(", ", others)
            + rows[group[i].Item1].Substring(group[i].Item2.Length);
    }
}

setCount = sets.Count;
sheetSets = string.Join(" || ", rows.ToArray());
emptySets = string.Join(", ", empties.ToArray());
sameSets = string.Join("; ", sameRows.ToArray());

if (setCount == 0)
{
    findings.Add("No View/Sheet Set is saved in " + doc.Title + ". The print and export dialogs "
        + "offer only the in-session set, which is not saved in the model. MANAGE_SHEET_SETS "
        + "saves one from a list of sheets or views.");
}
else
{
    findings.Add(setCount + " View/Sheet Set" + (setCount == 1 ? "" : "s") + " saved in "
        + doc.Title + ": " + sheetsOnly + " of sheets only, " + viewsOnly + " of views only, "
        + mixed + " of both, " + empties.Count + " EMPTY.");
    if (empties.Count > 0)
        findings.Add("Holding nothing - listed in the print dialog and printing nothing: "
            + emptySets + ".");
    foreach (var group in groups)
    {
        if (group.Count < 2) continue;
        findings.Add("Holding exactly the same sheets and views, member for member ("
            + group[0].Item4 + " each): " + string.Join(", ", group.Select(g => g.Item2).ToArray())
            + ". Printing any of them prints the same drawings.");
    }
    if (unread.Count > 0)
        findings.Add("Revit would not hand back the members of: " + string.Join(", ", unread.ToArray())
            + ". They are counted as sets and their contents are not claimed.");
}
