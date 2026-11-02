// NOT STANDALONE. Assumes `doc`, `action`, `schemeName`, `parts` and
// `separator` are in scope, and leaves `changed`, `report`, `before`,
// `after`, `skipped`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// WHAT IT DOES. Two things from Revit's Manage > MEP Settings > Electrical
// Settings > Circuit Naming, one at a time - `action` says which:
//   make - a new circuit naming scheme (`schemeName`) from circuit parameters
//          (`parts`, "panel; circuit number") with `separator` between each
//          two of them - "Panel-Circuit with a dash" is parts panel; circuit
//          number, separator "-".
//   use  - the project's circuit naming scheme set to one the model has.
//
// NEVER GUESSED (D-33). A scheme name not there exactly is refused with the
// model's schemes listed; one differing only in capitals is named, never
// taken. A part that is not a circuit parameter Heron can name is refused
// with the words it knows. Revit's own IsValidCombinedParameters is asked
// before anything is made.
//
// NEVER OVERWRITES, RENAMES OR DELETES. A new name already used, in any
// capitals, is skipped and named; the scheme already in use is left as it is.
//
// READ BACK. A new scheme's name and parts, and the project's scheme, are read
// back from the model; a read-back that disagrees throws, so the caller's
// transaction rolls the whole change back (Article 8).
//
// RELEASES. CircuitNamingScheme (Create(Document, string,
// IList<TableCellCombinedParameterData>), GetCombinedParameters, IsNameUnique,
// IsValidCombinedParameters) and CircuitNamingSchemeSettings
// (GetCircuitNamingSchemeSettings, CircuitNamingSchemeId,
// IsValidCircuitNamingSchemeId) are 2021 to 2027 and not in 2020 - the card
// claims 2021 on. TableCellCombinedParameterData.Create, ParamId, CategoryId,
// Prefix, Suffix and Separator, and LabelUtils.GetLabelFor(BuiltInParameter),
// are 2020 to 2027 (tools/api-surface --members, 2026-11-02).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var changed = 0;
var report = "";
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
var circuitCategory = new ElementId(BuiltInCategory.OST_ElectricalCircuit);

// ---- The circuit parameters Heron can name, and the words for them ---------
// Each built-in one by Revit's own label on this PC, plus the short words a
// modeller says. A project or shared parameter is matched by its name below.
var known = new List<Tuple<BuiltInParameter, string[]>>
{
    Tuple.Create(BuiltInParameter.RBS_ELEC_CIRCUIT_PANEL_PARAM, new[] { "panel", "panel name" }),
    Tuple.Create(BuiltInParameter.RBS_ELEC_CIRCUIT_NUMBER, new[] { "circuit", "circuit number", "number" }),
    Tuple.Create(BuiltInParameter.RBS_ELEC_CIRCUIT_NAME, new[] { "load name", "load" }),
    Tuple.Create(BuiltInParameter.RBS_ELEC_CIRCUIT_START_SLOT, new[] { "start slot", "slot" }),
};
Func<BuiltInParameter, string> label = p =>
{
    try { return LabelUtils.GetLabelFor(p); } catch (Exception) { return p.ToString(); }
};
Func<ElementId, string> paramName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "nothing";
    foreach (var k in known)
        if (new ElementId(k.Item1) == id) return label(k.Item1);
    var e = doc.GetElement(id);
    return e != null ? e.Name : "a parameter Heron does not name (id " + id + ")";
};

// ---- The model's schemes, read fresh whenever asked ------------------------
Func<List<CircuitNamingScheme>> readSchemes = () =>
{
    var found = new List<CircuitNamingScheme>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(CircuitNamingScheme)))
    {
        var s = e as CircuitNamingScheme;
        if (s != null) found.Add(s);
    }
    found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    return found;
};
Func<CircuitNamingScheme, string> partsOf = s =>
{
    var words = new List<string>();
    try
    {
        var data = s.GetCombinedParameters();
        for (var i = 0; data != null && i < data.Count; i++)
        {
            var d = data[i];
            words.Add((string.IsNullOrEmpty(d.Prefix) ? "" : quoted(d.Prefix) + " ") + paramName(d.ParamId)
                + (string.IsNullOrEmpty(d.Suffix) ? "" : " " + quoted(d.Suffix))
                + (i < data.Count - 1 || !string.IsNullOrEmpty(d.Separator) ? " " + quoted(d.Separator ?? "") : ""));
        }
    }
    catch (Exception ex) { return "parts not read: " + ex.Message; }
    return words.Count == 0 ? "no parts" : string.Join(" + ", words);
};
CircuitNamingSchemeSettings settings = null;
try { settings = CircuitNamingSchemeSettings.GetCircuitNamingSchemeSettings(doc); }
catch (Exception ex) { findings.Add("The project's circuit naming setting could not be read: " + ex.Message); }
Func<ElementId> projectScheme = () => settings == null ? ElementId.InvalidElementId : settings.CircuitNamingSchemeId;
Func<List<CircuitNamingScheme>, List<string>> listOf = list =>
{
    var inUse = projectScheme();
    return list.Select(s => quoted(s.Name) + ": " + partsOf(s) + (s.Id == inUse ? " - THE PROJECT'S" : "")).ToList();
};
var schemes = readSchemes();
Func<string> schemeNames = () => schemes.Count == 0 ? "none" : string.Join(", ", schemes.Select(s => quoted(s.Name)));
Func<string, CircuitNamingScheme> exactly = n =>
    schemes.FirstOrDefault(s => string.Equals(s.Name, n, StringComparison.Ordinal));
Func<string, CircuitNamingScheme> anyCapitals = n =>
    schemes.FirstOrDefault(s => string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase));

// ---- Which action ----------------------------------------------------------
var said = (action ?? "").Trim().ToLowerInvariant();
var first = said.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
string mode = null;
if (new[] { "make", "create", "new", "add" }.Contains(first)) mode = "make";
else if (new[] { "use", "set", "apply", "assign", "switch", "change" }.Contains(first)) mode = "use";
else
    refused.Add("\"" + (action ?? "") + "\" is not one of the two things this does - make (a new circuit naming scheme) "
        + "or use (set the project's circuit naming scheme). Nothing was changed.");

var wanted = (schemeName ?? "").Trim();
if (mode != null && wanted.Length == 0)
{
    refused.Add(mode == "make"
        ? "No name was given for the new circuit naming scheme - it is never made up. Nothing was changed."
        : "No circuit naming scheme was named for the project - it is never guessed. The model's schemes are: "
          + schemeNames() + ".");
    mode = null;
}

before.AddRange(listOf(schemes));

// ---- make: a new scheme from circuit parameters ----------------------------
if (mode == "make")
{
    var taken = anyCapitals(wanted);
    var words = (parts ?? "").Split(new[] { ';', ',', '+' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(w => w.Trim()).Where(w => w.Length > 0).ToList();
    var knownWords = string.Join(", ", known.Select(k => quoted(label(k.Item1)) + " (" + string.Join(", ", k.Item2) + ")"))
        + ", or a project parameter of electrical circuits by its name";

    if (taken != null || !CircuitNamingScheme.IsNameUnique(doc, wanted))
        skipped.Add("A circuit naming scheme is already named " + quoted(taken != null ? taken.Name : wanted)
            + " - left as it is, nothing made.");
    else if (words.Count == 0)
        refused.Add("No parts were given for " + quoted(wanted) + " - which circuit parameters it is made of is never "
            + "guessed. The parts Heron knows: " + knownWords + ". Nothing was made.");
    else
    {
        // Each word to a parameter: a built-in one by its label or a short word,
        // else a project parameter by its exact name.
        var parameterElements = new FilteredElementCollector(doc).OfClass(typeof(ParameterElement)).ToElements();
        var ids = new List<ElementId>();
        foreach (var w in words)
        {
            ElementId id = null;
            foreach (var k in known)
                if (string.Equals(label(k.Item1), w, StringComparison.OrdinalIgnoreCase)
                    || k.Item2.Any(a => string.Equals(a, w, StringComparison.OrdinalIgnoreCase)))
                { id = new ElementId(k.Item1); break; }
            if (id == null)
            {
                var hits = parameterElements.Where(p => string.Equals(p.Name, w, StringComparison.Ordinal)).ToList();
                if (hits.Count == 1) id = hits[0].Id;
                else if (hits.Count > 1)
                {
                    refused.Add(hits.Count + " project parameters are named " + quoted(w) + " - which one is never guessed.");
                    continue;
                }
                else
                {
                    var near = parameterElements.FirstOrDefault(p => string.Equals(p.Name, w, StringComparison.OrdinalIgnoreCase));
                    refused.Add(quoted(w) + " is not a circuit parameter Heron can name"
                        + (near != null ? " - the project parameter " + quoted(near.Name) + " differs only in capitals and is not taken" : "")
                        + ". The parts Heron knows: " + knownWords + ".");
                    continue;
                }
            }
            ids.Add(id);
        }

        if (refused.Count == 0)
        {
            var gap = separator ?? "";
            var data = new List<TableCellCombinedParameterData>();
            for (var i = 0; i < ids.Count; i++)
            {
                var d = TableCellCombinedParameterData.Create();
                d.CategoryId = circuitCategory;
                d.ParamId = ids[i];
                d.Prefix = "";
                d.Suffix = "";
                // Revit draws a part's separator AFTER it - so every part but the
                // last carries the gap (ADD_SCHEDULE_COMBINED_FIELD, 2026-10-21).
                d.Separator = i < ids.Count - 1 ? gap : "";
                data.Add(d);
            }

            if (!CircuitNamingScheme.IsValidCombinedParameters(doc, data))
                refused.Add("Revit does not accept " + string.Join(" + ", ids.Select(paramName))
                    + " as the parts of a circuit naming scheme. Nothing was made.");
            else
            {
                CircuitNamingScheme made = null;
                try { made = CircuitNamingScheme.Create(doc, wanted, data); }
                catch (Exception ex)
                {
                    refused.Add("Revit would not make a circuit naming scheme named " + quoted(wanted) + ": " + ex.Message
                        + " Nothing was made.");
                }
                if (made == null && refused.Count == 0)
                    refused.Add("Revit returned no scheme and raised no error. Nothing was made.");
                if (made != null)
                {
                    var back = made.GetCombinedParameters();
                    var same = back != null && back.Count == ids.Count
                        && string.Equals(made.Name, wanted, StringComparison.Ordinal);
                    for (var i = 0; same && i < ids.Count; i++)
                        same = back[i].ParamId == ids[i] && (back[i].Separator ?? "") == data[i].Separator;
                    if (!same)
                        throw new InvalidOperationException("The new circuit naming scheme reads back as " + quoted(made.Name)
                            + ": " + partsOf(made) + " - not what was asked. The whole change is rolled back, so nothing is kept.");
                    changed = 1;
                    findings.Add("Made the circuit naming scheme " + quoted(wanted) + ": " + partsOf(made)
                        + ". It is not the project's until it is set with use.");
                }
            }
        }
    }
}

// ---- use: the project's scheme ---------------------------------------------
if (mode == "use")
{
    var scheme = exactly(wanted);
    if (scheme == null)
    {
        var near = anyCapitals(wanted);
        refused.Add("No circuit naming scheme is named " + quoted(wanted)
            + (near != null ? " - " + quoted(near.Name) + " differs only in capitals and is not taken; name it exactly" : "")
            + ". The model's schemes are: " + schemeNames() + ".");
    }
    else if (settings == null)
        refused.Add("The project's circuit naming setting could not be read, so nothing was changed.");
    else if (settings.CircuitNamingSchemeId == scheme.Id)
        skipped.Add("The project already uses " + quoted(scheme.Name) + " - left as it is.");
    else if (!CircuitNamingSchemeSettings.IsValidCircuitNamingSchemeId(doc, scheme.Id))
        refused.Add("Revit does not accept " + quoted(scheme.Name) + " as the project's circuit naming scheme.");
    else
    {
        var was = doc.GetElement(settings.CircuitNamingSchemeId);
        try { settings.CircuitNamingSchemeId = scheme.Id; }
        catch (Exception ex)
        {
            refused.Add("Revit refused to set the project's circuit naming to " + quoted(scheme.Name) + ": " + ex.Message);
        }
        if (refused.Count == 0)
        {
            var now = CircuitNamingSchemeSettings.GetCircuitNamingSchemeSettings(doc).CircuitNamingSchemeId;
            if (now != scheme.Id)
                throw new InvalidOperationException("Revit took the project's circuit naming as " + quoted(scheme.Name)
                    + " without an error, but it reads back as " + quoted(paramName(now)) + ". The whole change is rolled "
                    + "back, so nothing is kept.");
            changed = 1;
            findings.Add("The project's circuit naming is now " + quoted(scheme.Name) + " (" + partsOf(scheme) + "), was "
                + (was == null ? "none" : quoted(was.Name)) + ". No panel's own Circuit Naming parameter was changed - a "
                + "panel set to a scheme of its own keeps it.");
        }
    }
}

schemes = readSchemes();
after.AddRange(listOf(schemes));
report = "Circuit naming schemes before (" + before.Count + "): " + (before.Count == 0 ? "none" : string.Join("; ", before))
    + " | after (" + after.Count + "): " + (after.Count == 0 ? "none" : string.Join("; ", after));

findings.Insert(0, mode == "make"
    ? (changed == 1 ? "1 circuit naming scheme made." : "No circuit naming scheme made.")
    : mode == "use"
        ? (changed == 1 ? "The project's circuit naming scheme changed." : "The project's circuit naming scheme not changed.")
        : "Nothing was changed.");
foreach (var s in skipped) findings.Add(s);
foreach (var r in refused) findings.Add(r);
