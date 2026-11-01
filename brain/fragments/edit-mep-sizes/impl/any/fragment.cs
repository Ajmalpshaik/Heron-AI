// NOT STANDALONE. Assumes `doc`, `sizeList`, `name`, `action`, `sizes`,
// `outsideMm`, `insideMm` and `bendRadiusMm` are in scope, and leaves `added`,
// `removed`, `before`, `after`, `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// WHAT IT CHANGES. ONE size list, the one named - Duct Settings > Round,
// Rectangular or Oval; one pipe segment's Segments and Sizes; one conduit
// standard's Size list; or Cable Tray Settings > Size - by adding or removing
// the sizes given. No other list, segment or standard is touched.
//
// MILLIMETRES IN, FEET TO REVIT. Every number arrives from a person in mm and
// is divided by 304.8 as plain arithmetic - never a units call, which is what
// changed shape at 2021 (D-20).
//
// NEVER GUESSED (D-33). A duct or cable tray size is one number. A pipe size is
// a nominal, an outside and an inside diameter, and a conduit size adds a bend
// radius - and each of those is the modeller's to give: a missing one is
// refused and named, never made up. An inside diameter typed from memory is a
// wall thickness, which is a pressure rating.
//
// A SIZE ALREADY THERE IS SKIPPED AND NAMED, never added twice. A size to
// remove that is not there is skipped and named.
//
// A SIZE IN USE IS NEVER REMOVED. Before removing, the model is searched for
// ducts and flex ducts of that shape, pipes on that segment, conduits of that
// standard, or cable trays, carrying that size; any found and the size is
// refused, with the count. Fittings are not searched. What Revit itself does
// when a size still in use is removed is not settled here - NEEDS-CHECKING.
//
// READ BACK. `added` and `removed` are counted from the list as Revit holds it
// after the change, not from the calls made (D-52).
//
// RELEASES. DuctSizeSettings.GetDuctSizeSettings, its DuctShape indexer,
// AddSize(DuctShape, MEPSize), RemoveSize(DuctShape, double); Segment.GetSizes,
// AddSize(MEPSize), RemoveSize(double); ConduitSizeSettings.GetConduitSizeSettings,
// AddSize(string, ConduitSize), RemoveSize(string, double) and its enumeration;
// CableTraySizes.GetCableTraySizes, AddSize(MEPSize), RemoveSize(MEPSize); the
// MEPSize and ConduitSize constructors; Pipe.PipeSegment; MEPCurveType.Shape -
// all the same 2020 to 2027 (tools/api-surface --members, 2026-11-01).
//
// AN ElementId IS COMPARED WITH .Equals, NEVER READ AS A NUMBER.

var added = 0;
var removed = 0;
var before = "";
var after = "";
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", invariant);
// Two sizes within a hundredth of a millimetre are the same size.
var tolerance = 0.01 / 304.8;
Func<double, double, bool> same = (a, b) => Math.Abs(a - b) < tolerance;
var proceed = true;

// ---- Which list ------------------------------------------------------------
var listWord = (sizeList ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "");
var kind = "";
if (listWord.StartsWith("round")) kind = "round";
else if (listWord.StartsWith("rect")) kind = "rectangular";
else if (listWord.StartsWith("oval")) kind = "oval";
else if (listWord.StartsWith("pipe") || listWord.StartsWith("segment")) kind = "pipe";
else if (listWord.StartsWith("conduit")) kind = "conduit";
else if (listWord.StartsWith("tray") || listWord.StartsWith("cabletray")) kind = "tray";
if (kind.Length == 0)
{
    refused.Add("\"" + (sizeList ?? "") + "\" is not a size list - round, rectangular or oval (duct), pipe (a segment), "
        + "conduit (a standard) or tray. Nothing was guessed and nothing changed.");
    proceed = false;
}

var verb = (action ?? "").Trim().ToLowerInvariant();
var adding = verb.StartsWith("add");
var removing = verb.StartsWith("remove") || verb.StartsWith("delete");
if (proceed && !adding && !removing)
{
    refused.Add("\"" + (action ?? "") + "\" is neither add nor remove. Nothing changed.");
    proceed = false;
}

// ---- The sizes asked for, in mm --------------------------------------------
var wanted = new List<double>();
foreach (var piece in (sizes ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
{
    var raw = piece.Trim();
    if (raw.Length == 0) continue;
    var cleaned = raw.ToLowerInvariant().Replace("dn", "").Replace("mm", "").Replace("\u00f8", "").Trim();
    double value;
    if (!double.TryParse(cleaned, System.Globalization.NumberStyles.Float, invariant, out value) || value <= 0)
    {
        refused.Add("\"" + raw + "\" could not be read as a size in millimetres - left out.");
        continue;
    }
    var feet = value / 304.8;
    if (wanted.Any(w => same(w, feet))) { skipped.Add(raw + " mm was given twice - counted once"); continue; }
    wanted.Add(feet);
}
if (proceed && wanted.Count == 0)
{
    refused.Add("No size to " + (adding ? "add" : "remove") + " was given. Nothing changed.");
    proceed = false;
}

// A pipe or conduit size carries more than its nominal, given once per run.
var oneSizeOnly = adding && (kind == "pipe" || kind == "conduit");
if (proceed && oneSizeOnly && wanted.Count > 1)
{
    refused.Add("A " + (kind == "pipe" ? "pipe" : "conduit") + " size needs its own outside and inside diameter"
        + (kind == "conduit" ? " and bend radius" : "") + ", so one size is added per run - " + wanted.Count
        + " were given. Nothing changed.");
    proceed = false;
}
double outer = 0, inner = 0, bend = 0;
if (proceed && oneSizeOnly)
{
    var missing = new List<string>();
    if (outsideMm <= 0) missing.Add("outside diameter");
    if (insideMm <= 0) missing.Add("inside diameter");
    if (kind == "conduit" && bendRadiusMm <= 0) missing.Add("bend radius");
    if (missing.Count > 0)
    {
        refused.Add("The " + mm(wanted[0]) + " mm " + (kind == "pipe" ? "pipe" : "conduit") + " size needs its "
            + string.Join(", ", missing) + " in millimetres - never guessed. "
            + (kind == "pipe" ? "REPORT_PIPE_SEGMENTS shows another segment's sizes to copy from." : "")
            + " Nothing changed.");
        proceed = false;
    }
    else if (insideMm >= outsideMm)
    {
        refused.Add("An inside diameter of " + insideMm.ToString("0.##", invariant) + " mm is not smaller than the outside "
            + outsideMm.ToString("0.##", invariant) + " mm - check the two. Nothing changed.");
        proceed = false;
    }
    outer = outsideMm / 304.8;
    inner = insideMm / 304.8;
    bend = bendRadiusMm / 304.8;
}
if (proceed && !oneSizeOnly && (outsideMm > 0 || insideMm > 0 || bendRadiusMm > 0))
    findings.Add("An outside or inside diameter or a bend radius was given; only an added pipe or conduit size uses one, "
        + "so it was not used.");

var wantedName = (name ?? "").Trim();
if (proceed && (kind == "pipe" || kind == "conduit") && wantedName.Length == 0)
{
    refused.Add("Which " + (kind == "pipe" ? "pipe segment" : "conduit standard") + "? It must be named - every one is a "
        + "separate size list, and none is chosen for you. Nothing changed.");
    proceed = false;
}
if (proceed && kind != "pipe" && kind != "conduit" && wantedName.Length > 0)
    findings.Add("\"" + wantedName + "\" was given as a name; the " + kind + " size list is the only one of its kind, "
        + "so the name was not used.");

// Picks one name from a list: the exact name, else the only one containing it.
Func<List<string>, string, string> pick = (all, needle) =>
{
    var exact = all.FirstOrDefault(n => string.Equals(n, needle, StringComparison.OrdinalIgnoreCase));
    if (exact != null) return exact;
    var near = all.Where(n => n.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    return near.Count == 1 ? near[0] : null;
};
Func<List<string>, string, string> nearWords = (all, needle) =>
{
    var near = all.Where(n => n.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    var shown = near.Count > 1 ? near : all;
    return (near.Count > 1 ? near.Count + " names contain it - say which: " : "The names in this model: ")
        + (shown.Count == 0 ? "none" : "\"" + string.Join("\", \"", shown) + "\"");
};

// ---- Find the one list -----------------------------------------------------
DuctSizeSettings ductSettings = null;
var shape = DuctShape.Round;
PipeSegment segment = null;
ConduitSizeSettings conduitSettings = null;
var standardName = "";
CableTraySizes traySizes = null;
var label = "";
// The list in a sentence - "the round duct size list", "the pipe segment \"X\"".
var place = "";

if (proceed && (kind == "round" || kind == "rectangular" || kind == "oval"))
{
    shape = kind == "round" ? DuctShape.Round : kind == "rectangular" ? DuctShape.Rectangular : DuctShape.Oval;
    label = char.ToUpperInvariant(kind[0]) + kind.Substring(1) + " duct sizes";
    place = "the " + kind + " duct size list";
    try { ductSettings = DuctSizeSettings.GetDuctSizeSettings(doc); }
    catch (Exception ex) { findings.Add("Duct Settings could not be read: " + ex.Message); }
    if (ductSettings == null) { refused.Add("This model has no duct size settings. Nothing changed."); proceed = false; }
}
else if (proceed && kind == "pipe")
{
    var segments = new FilteredElementCollector(doc).OfClass(typeof(PipeSegment)).Cast<PipeSegment>().ToList();
    var segmentNames = segments.Select(s => s.Name ?? "").ToList();
    var chosen = pick(segmentNames, wantedName);
    if (chosen == null)
    {
        refused.Add("No single pipe segment is called \"" + wantedName + "\". " + nearWords(segmentNames, wantedName)
            + ". Nothing changed.");
        proceed = false;
    }
    else
    {
        segment = segments.First(s => (s.Name ?? "") == chosen);
        label = "Pipe segment \"" + chosen + "\" sizes";
        place = "the pipe segment \"" + chosen + "\"";
        if (!string.Equals(chosen, wantedName, StringComparison.OrdinalIgnoreCase))
            findings.Add("\"" + wantedName + "\" was taken as the pipe segment \"" + chosen + "\" - the only one whose name holds it.");
    }
}
else if (proceed && kind == "conduit")
{
    try { conduitSettings = ConduitSizeSettings.GetConduitSizeSettings(doc); }
    catch (Exception ex) { findings.Add("Conduit Settings could not be read: " + ex.Message); }
    if (conduitSettings == null) { refused.Add("This model has no conduit size settings. Nothing changed."); proceed = false; }
    else
    {
        var standardNames = new List<string>();
        foreach (var pair in conduitSettings) standardNames.Add(pair.Key ?? "");
        var chosen = pick(standardNames, wantedName);
        if (chosen == null)
        {
            refused.Add("No single conduit standard is called \"" + wantedName + "\". " + nearWords(standardNames, wantedName)
                + ". Nothing changed.");
            proceed = false;
        }
        else
        {
            standardName = chosen;
            label = "Conduit standard \"" + chosen + "\" sizes";
            place = "the conduit standard \"" + chosen + "\"";
            if (!string.Equals(chosen, wantedName, StringComparison.OrdinalIgnoreCase))
                findings.Add("\"" + wantedName + "\" was taken as the conduit standard \"" + chosen + "\" - the only one whose name holds it.");
        }
    }
}
else if (proceed && kind == "tray")
{
    label = "Cable tray sizes";
    place = "the cable tray size list";
    try { traySizes = CableTraySizes.GetCableTraySizes(doc); }
    catch (Exception ex) { findings.Add("Cable Tray Settings could not be read: " + ex.Message); }
    if (traySizes == null) { refused.Add("This model has no cable tray size settings. Nothing changed."); proceed = false; }
}

// The nominal sizes the named list holds right now, in Revit's feet.
Func<List<double>> current = () =>
{
    var held = new List<double>();
    if (ductSettings != null) { var list = ductSettings[shape]; if (list != null) foreach (MEPSize s in list) if (s != null) held.Add(s.NominalDiameter); }
    else if (segment != null) { foreach (var s in segment.GetSizes()) if (s != null) held.Add(s.NominalDiameter); }
    else if (conduitSettings != null)
    {
        foreach (var pair in conduitSettings)
            if ((pair.Key ?? "") == standardName && pair.Value != null)
                foreach (ConduitSize s in pair.Value) if (s != null) held.Add(s.NominalDiameter);
    }
    else if (traySizes != null) { foreach (MEPSize s in traySizes) if (s != null) held.Add(s.NominalDiameter); }
    held.Sort();
    return held;
};
Func<List<double>, string> words = held =>
    label + " (" + held.Count + "): " + (held.Count == 0 ? "none" : string.Join(", ", held.Select(mm)) + " mm");

var held0 = new List<double>();
if (proceed)
{
    try { held0 = current(); before = words(held0); }
    catch (Exception ex) { refused.Add(label + " could not be read: " + ex.Message + ". Nothing changed."); proceed = false; }
}

// ---- How many elements in the model carry a size ---------------------------
Func<Element, BuiltInParameter, double> lengthOf = (e, p) =>
{
    var parameter = e.get_Parameter(p);
    return parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double ? parameter.AsDouble() : -1;
};
Func<double, int> inUse = nominal =>
{
    var count = 0;
    if (ductSettings != null)
    {
        var wantProfile = shape == DuctShape.Round ? ConnectorProfileType.Round
            : shape == DuctShape.Rectangular ? ConnectorProfileType.Rectangular : ConnectorProfileType.Oval;
        var curves = new List<Element>();
        curves.AddRange(new FilteredElementCollector(doc).OfClass(typeof(Duct)).WhereElementIsNotElementType().ToElements());
        curves.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FlexDuct)).WhereElementIsNotElementType().ToElements());
        foreach (var e in curves)
        {
            var type = doc.GetElement(e.GetTypeId()) as MEPCurveType;
            if (type == null || type.Shape != wantProfile) continue;
            if (wantProfile == ConnectorProfileType.Round)
            {
                if (same(lengthOf(e, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM), nominal)) count++;
            }
            else if (same(lengthOf(e, BuiltInParameter.RBS_CURVE_WIDTH_PARAM), nominal)
                || same(lengthOf(e, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM), nominal)) count++;
        }
    }
    else if (segment != null)
    {
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Pipe)).WhereElementIsNotElementType().ToElements())
        {
            var pipe = e as Pipe;
            PipeSegment on = null;
            try { on = pipe == null ? null : pipe.PipeSegment; } catch (Exception) { }
            if (on == null || !on.Id.Equals(segment.Id)) continue;
            if (same(lengthOf(e, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM), nominal)) count++;
        }
    }
    else if (conduitSettings != null)
    {
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Conduit)).WhereElementIsNotElementType().ToElements())
        {
            if (!same(lengthOf(e, BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM), nominal)) continue;
            // A conduit whose standard cannot be read is counted: a size is
            // never removed on the strength of a parameter nobody could read.
            var type = doc.GetElement(e.GetTypeId());
            var p = type == null ? null : type.get_Parameter(BuiltInParameter.CONDUIT_STANDARD_TYPE_PARAM);
            var standardOf = p == null ? "" : (p.AsValueString() ?? "");
            if (standardOf.Length == 0 || string.Equals(standardOf, standardName, StringComparison.OrdinalIgnoreCase)) count++;
        }
    }
    else if (traySizes != null)
    {
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(CableTray)).WhereElementIsNotElementType().ToElements())
        {
            if (same(lengthOf(e, BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM), nominal)
                || same(lengthOf(e, BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM), nominal)) count++;
        }
    }
    return count;
};

// ---- Add or remove ---------------------------------------------------------
var asked = new List<double>();
if (proceed)
{
    foreach (var size in wanted)
    {
        var there = held0.Where(h => same(h, size)).ToList();
        if (adding)
        {
            if (there.Count > 0) { skipped.Add(mm(size) + " mm is already in " + place + " - left as it is"); continue; }
            try
            {
                if (ductSettings != null) ductSettings.AddSize(shape, new MEPSize(size, size, size, true, true));
                else if (segment != null) segment.AddSize(new MEPSize(size, inner, outer, true, true));
                else if (conduitSettings != null) conduitSettings.AddSize(standardName, new ConduitSize(size, inner, outer, bend, true, true));
                else if (traySizes != null) traySizes.AddSize(new MEPSize(size, size, size, true, true));
                asked.Add(size);
            }
            catch (Exception ex) { refused.Add(mm(size) + " mm could not be added - Revit said: " + ex.Message); }
        }
        else
        {
            if (there.Count == 0) { skipped.Add(mm(size) + " mm is not in " + place + " - nothing to remove"); continue; }
            var users = 0;
            try { users = inUse(there[0]); }
            catch (Exception ex)
            {
                refused.Add(mm(size) + " mm was not removed - whether anything uses it could not be checked: " + ex.Message);
                continue;
            }
            if (users > 0)
            {
                refused.Add(mm(size) + " mm was NOT removed - " + users + " element(s) in the model carry it. Change them to "
                    + "another size first.");
                continue;
            }
            try
            {
                // The stored value is handed back, never the typed one, so a
                // rounding in the last digit cannot miss the size.
                if (ductSettings != null) ductSettings.RemoveSize(shape, there[0]);
                else if (segment != null) segment.RemoveSize(there[0]);
                else if (conduitSettings != null) conduitSettings.RemoveSize(standardName, there[0]);
                else if (traySizes != null)
                {
                    MEPSize stored = null;
                    foreach (MEPSize s in traySizes) if (s != null && same(s.NominalDiameter, there[0])) { stored = s; break; }
                    if (stored != null) traySizes.RemoveSize(stored);
                }
                asked.Add(size);
            }
            catch (Exception ex) { refused.Add(mm(size) + " mm could not be removed - Revit said: " + ex.Message); }
        }
    }
}

// ---- Read back -------------------------------------------------------------
if (proceed)
{
    try
    {
        var held1 = current();
        after = words(held1);
        foreach (var size in asked)
        {
            var nowThere = held1.Any(h => same(h, size));
            if (adding && nowThere) added++;
            else if (!adding && !nowThere) removed++;
            else findings.Add(mm(size) + " mm was " + (adding ? "added" : "removed") + " without an error but the list read back "
                + (adding ? "does not hold it" : "still holds it") + ".");
        }
    }
    catch (Exception ex) { findings.Add("The list could not be read back: " + ex.Message); }
}

findings.Insert(0, (proceed ? label + ": " : "Nothing changed: ") + added + " size(s) added, " + removed + " removed, "
    + skipped.Count + " skipped, " + refused.Count + " refused.");
if (before.Length > 0) findings.Add("Before - " + before);
if (after.Length > 0) findings.Add("After - " + after);
