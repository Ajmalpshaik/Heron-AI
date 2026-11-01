// NOT STANDALONE. Assumes `doc`, `parameterName`, `setPrefix`, `values` and
// `updateExisting` are in scope, and leaves `done`, `created`, `updated`,
// `unchanged`, `skipped`, `refused` and `printRange` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) - for the document side of it.
//
// EVERY ROUTE IS THE PRINT MANAGER. A View/Sheet Set cannot be constructed; it
// is saved from the print manager's sheet-set setting, which throws unless the
// print range is "Selected views/sheets" first.
//
// AND THE PRINT MANAGER IS NOT TRANSACTIONAL. The print range, the current set
// and the In-Session set's members are application state an Undo does not put
// back. All three are read before anything is touched and put back in a
// finally, whatever happened, and `printRange` says what became of the range.
//
// AN EXISTING SET IS LEFT ALONE UNLESS ASKED. updateExisting false - the
// default - reports it in `unchanged`; true makes it hold exactly its value's
// sheets and views, and a set already in step is not saved again (Save throws
// on a set that did not change).

bool done = false;
string created = "";
string updated = "";
string unchanged = "";
string skipped = "";
string refused = "";
string printRange = "";

string wanted = (parameterName ?? "").Trim();
string prefix = (setPrefix ?? "");
if (string.Equals(prefix.Trim(), "none", StringComparison.OrdinalIgnoreCase)) prefix = "";
string only = (values ?? "").Trim();
if (string.Equals(only, "all", StringComparison.OrdinalIgnoreCase)) only = "";
var onlyValues = new HashSet<string>(only.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(v => v.Trim()).Where(v => v.Length > 0), StringComparer.OrdinalIgnoreCase);

Func<Parameter, string> valueOf = parameter =>
{
    if (parameter == null || !parameter.HasValue) return "";
    string text = "";
    try { text = parameter.StorageType == StorageType.String ? parameter.AsString() : parameter.AsValueString(); } catch { }
    return (text ?? "").Trim();
};

if (wanted.Length == 0) refused = "No parameter name was given.";

// Every sheet and view that carries the parameter, by value.
var byValue = new Dictionary<string, List<View>>(StringComparer.OrdinalIgnoreCase);
int carriers = 0;
if (refused.Length == 0)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var view = element as View;
        if (view == null || view.IsTemplate) continue;
        Parameter parameter = null;
        try { parameter = view.LookupParameter(wanted); } catch { }
        if (parameter == null) continue;
        carriers++;
        string value = valueOf(parameter);
        if (value.Length == 0) continue;
        if (onlyValues.Count > 0 && !onlyValues.Contains(value)) continue;
        List<View> group;
        if (!byValue.TryGetValue(value, out group)) { group = new List<View>(); byValue[value] = group; }
        group.Add(view);
    }

    if (carriers == 0)
        refused = "No sheet or view in " + (doc.Title ?? "this model") + " has a parameter called '" + wanted
            + "'. Name it exactly as the sheet's Properties show it. Nothing was changed.";
    else
    {
        var nobody = onlyValues.Where(v => !byValue.ContainsKey(v)).ToList();
        if (nobody.Count > 0)
            refused = "No sheet or view carries '" + wanted + "' = " + string.Join(", ", nobody.Select(v => "'" + v + "'"))
                + ". Nothing was changed.";
        else if (byValue.Count == 0)
            refused = "Every sheet and view with '" + wanted + "' has it empty, so there is no value to make a set for. "
                + "Nothing was changed.";
    }
}

if (refused.Length == 0)
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

    PrintManager printManager = null;
    ViewSheetSetting setting = null;
    PrintRange rangeBefore = PrintRange.Select;
    bool rangeRead = false;
    bool rangeChanged = false;
    IViewSheetSet currentBefore = null;
    ViewSet inSessionBefore = null;

    var made = new List<string>();
    var brought = new List<string>();
    var left = new List<string>();
    var notMade = new List<string>();

    try
    {
        try
        {
            printManager = doc.PrintManager;
            rangeBefore = printManager.PrintRange;
            rangeRead = true;
            if (rangeBefore != PrintRange.Select)
            {
                printManager.PrintRange = PrintRange.Select;
                rangeChanged = true;
            }
            setting = printManager.ViewSheetSetting;
            currentBefore = setting.CurrentViewSheetSet;
            // The In-Session set's own members, copied, so writing through it
            // to make a new set does not leave the Print dialog holding them.
            inSessionBefore = new ViewSet();
            foreach (View member in setting.InSession.Views) if (member != null) inSessionBefore.Insert(member);
        }
        catch { refused = "The print manager would not open its sheet-set setting. Nothing was changed."; }

        if (refused.Length == 0)
        {
            var order = byValue.Keys.ToList();
            order.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var value in order)
            {
                var group = byValue[value];
                string setName = (prefix + value).Trim();
                var members = new ViewSet();
                foreach (var view in group) members.Insert(view);

                ViewSheetSet existing;
                if (!sets.TryGetValue(setName, out existing))
                {
                    try
                    {
                        setting.CurrentViewSheetSet = setting.InSession;
                        setting.CurrentViewSheetSet.Views = members;
                        if (setting.SaveAs(setName)) made.Add("'" + setName + "' (" + group.Count + ")");
                        else notMade.Add("'" + setName + "': Revit did not save it");
                    }
                    catch (Exception failure) { notMade.Add("'" + setName + "': " + failure.Message); }
                    continue;
                }

                var holds = new HashSet<ElementId>();
                try { foreach (View member in existing.Views) if (member != null) holds.Add(member.Id); }
                catch { notMade.Add("'" + existing.Name + "': its members could not be read, so it was not touched"); continue; }
                var wantIds = new HashSet<ElementId>(group.Select(v => v.Id));
                int goingIn = wantIds.Count(id => !holds.Contains(id));
                int comingOut = holds.Count(id => !wantIds.Contains(id));

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
                    if (setting.Save()) brought.Add("'" + existing.Name + "' (" + goingIn + " in, " + comingOut + " out)");
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
        }
    }
    finally
    {
        // PUT THE PRINT MANAGER BACK - the In-Session set's members, the
        // current set, then the range - whatever happened above.
        if (setting != null)
        {
            try
            {
                setting.CurrentViewSheetSet = setting.InSession;
                if (inSessionBefore != null) setting.CurrentViewSheetSet.Views = inSessionBefore;
            }
            catch { }
            try { if (currentBefore != null) setting.CurrentViewSheetSet = currentBefore; } catch { }
        }

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
                printRange = "Print range NOT put back: it was set to Selected views/sheets for this run and was "
                    + rangeWords(rangeBefore) + " before. Check File - Print and set it back there.";
            }
        }
        else if (rangeRead && rangeBefore == PrintRange.Select)
            printRange = "Print range was already Selected views/sheets and is left there.";
    }

    created = string.Join("; ", made);
    updated = string.Join("; ", brought);
    unchanged = string.Join("; ", left);
    skipped = string.Join("; ", notMade);
    done = made.Count + brought.Count > 0;
    if (refused.Length == 0 && !done && notMade.Count == 0)
        refused = "Every set for '" + wanted + "' already exists" + (updateExisting ? " and is in step" : "")
            + " - nothing was changed." + (updateExisting ? "" : " To bring existing sets back in step, ask for updateExisting.");
}
