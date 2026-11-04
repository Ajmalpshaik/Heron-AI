// NOT STANDALONE. Assumes `doc` and `settings` are in scope; leaves `before`,
// `after`, `changed`, `energyModel`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// ANALYZE > ENERGY SETTINGS, FOR THE WHOLE MODEL. `settings` is a table, one
// setting per entry, entries split by ";" or a new line, each "name=value":
//
//   building type=office; ground plane=Level 1; project phase=New Construction
//   analysis mode=rooms or spaces; export category=spaces
//   export complexity=complex with shading surfaces; include thermal properties=yes
//   building envelope=identify exterior elements; building construction class=medium
//   service type=VAV single duct; report type=detailed; use current view only=no
//   sliver space tolerance=300; analytical grid cell size=900; core offset=3600
//   divide perimeter zones=yes; target percentage glazing=40; energy model=yes
//   use heating credits=no
//
// THE NAMES ARE THE DIALOG'S, forgiven for capitals, spaces and "the". A choice
// is one of Revit's own for that setting on the release that runs - the list is
// read off Revit, so "rooms or spaces" (Revit 2022 and later) and "conceptual
// masses" (2020 to 2023) are offered exactly where Revit has them - and every
// choice is put to Revit's own Check method before anything is written. A level
// or a phase is one the model has, by name. Lengths are MILLIMETRES (any other
// unit is refused, never converted - D-33) and become feet by / 304.8 (D-20).
//
// THE GLAZING PERCENTAGE: whether Revit holds it as 40 or as 0.4 is not in the
// assemblies, so it is asked of Revit's own range check - a percent range takes
// 50, a fraction range does not - and the value is written in that form.
//
// NOT REACHED, REFUSED BY NAME: the location (SET_PROJECT_LOCATION sets it),
// the building operating schedule, HVAC system, outdoor air and the
// shade / sill / skylight targets (marked obsolete in Revit 2027), and the
// reports folder.
//
// EVERYTHING IS PARSED BEFORE ANYTHING IS WRITTEN, so a bad entry changes
// nothing. A SETTING REVIT REFUSES is caught with Revit's own reason, every
// setting already written in this call is put back to the value read before,
// and the answer is `refused` with that reason - NOTHING WAS CHANGED. If a
// put-back itself fails, the whole call throws, so the caller's transaction
// rolls back (Golden Rule 16).
//
// READ BACK. `before` and `after` are every setting this card reaches, read off
// the model before and after; a value asked for that reads back different
// throws, so nothing is kept. An ElementId is compared with ==, never read as a
// number.

var findings = new List<string>();
string refused = null;
var before = "";
var after = "";
var changed = 0;
var energyModel = false;
var culture = System.Globalization.CultureInfo.InvariantCulture;

EnergyDataSettings es = null;
if (doc.IsFamilyDocument)
    refused = "The document in front is a family. Energy Settings are a project's, so NOTHING WAS CHANGED.";
else
{
    try
    {
#if REVIT2026_OR_GREATER
        es = EnergyDataSettings.GetEnergyDataSettings(doc);
#else
        es = EnergyDataSettings.GetFromDocument(doc);
#endif
    }
    catch (Exception ex) { refused = "Revit gave no Energy Settings for " + doc.Title + " (" + ex.Message + "), so NOTHING WAS CHANGED."; }
    if (refused == null && es == null)
        refused = doc.Title + " has no Energy Settings, so NOTHING WAS CHANGED.";
}

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", culture) + " mm";
Func<object, string> yesNo = b => (bool)b ? "yes" : "no";
Func<object, string> words = e => System.Text.RegularExpressions.Regex.Replace(e.ToString(), "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ").ToLowerInvariant();
Func<string, string> key = text =>
{
    var parts = System.Text.RegularExpressions.Regex.Split((text ?? "").ToLowerInvariant(), "[^a-z0-9]+")
        .Where(w => w.Length > 0 && w != "the");
    return string.Join("", parts);
};
Func<object, string, Tuple<object, string>> ok = (v, problem) => Tuple.Create(v, problem);

// A choice among an enum's names on THIS release, matched without spaces or
// capitals, with an optional word the dialog leaves off ("loose" for
// LooseConstruction). `skip` are Revit's markers that are not choices.
Func<Type, string[], string[], Func<string, Tuple<object, string>>> enumParser = (t, suffixes, skip) => v =>
{
    var k = key(v);
    foreach (var name in Enum.GetNames(t))
    {
        if (skip.Contains(name)) continue;
        var nk = key(name);
        if (nk == k || suffixes.Any(s => nk == k + s)) return ok(Enum.Parse(t, name), null);
    }
    var offered = string.Join(", ", Enum.GetNames(t).Where(n => !skip.Contains(n)).Select(n => words(Enum.Parse(t, n))));
    return ok(null, "\"" + v + "\" is not one of Revit's choices: " + offered);
};
// The same parse, then Revit's own Check method for that setting.
Func<Func<string, Tuple<object, string>>, Func<object, bool>, string, Func<string, Tuple<object, string>>> checkedBy = (parse, check, label) => v =>
{
    var r = parse(v);
    if (r.Item1 == null) return r;
    bool valid;
    try { valid = check(r.Item1); } catch (Exception) { valid = false; }
    return valid ? r : ok(null, "Revit says " + label + " \"" + v + "\" is not valid");
};
Func<string, Tuple<object, string>> parseYes = v =>
{
    var t = (v ?? "").Trim().ToLowerInvariant();
    if (t == "yes" || t == "on" || t == "true" || t == "1" || t == "ticked") return ok(true, null);
    if (t == "no" || t == "off" || t == "false" || t == "0" || t == "unticked") return ok(false, null);
    return ok(null, "\"" + v + "\" is not yes or no");
};
Func<string, double?> number = v =>
{
    double n;
    if (!double.TryParse((v ?? "").Trim(), System.Globalization.NumberStyles.Float, culture, out n)) return null;
    return n;
};
Func<string, Tuple<object, string>> parseMm = v =>
{
    var t = (v ?? "").Trim().ToLowerInvariant().Replace(" ", "");
    if (t.EndsWith("millimetres") || t.EndsWith("millimeters")) t = t.Substring(0, t.Length - 11);
    else if (t.EndsWith("mm")) t = t.Substring(0, t.Length - 2);
    var n = number(t);
    if (n == null || n.Value < 0) return ok(null, "\"" + v + "\" is not a length in millimetres");
    return ok(n.Value / 304.8, null);
};
Func<ElementId, string> nameOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var e = doc.GetElement(id);
    return e == null ? "none" : "\"" + e.Name + "\"";
};
var roomsId = new ElementId(BuiltInCategory.OST_Rooms);
var spacesId = new ElementId(BuiltInCategory.OST_MEPSpaces);
Func<object, string> categoryWord = v =>
{
    var id = v as ElementId;
    if (id == null || id == ElementId.InvalidElementId) return "none";
    if (id == roomsId) return "rooms";
    if (id == spacesId) return "spaces";
    return "another category";
};

// THE GLAZING PERCENTAGE'S FORM, asked of Revit (see the head of this file).
var glazingAsPercent = false;
try { glazingAsPercent = EnergyDataSettings.CheckRangeOfPercentageGlazing(50.0); } catch (Exception) { glazingAsPercent = false; }
Func<object, string> percent = v => (glazingAsPercent ? (double)v : (double)v * 100.0).ToString("0.##", culture) + " %";

// ---- the settings this card reaches -------------------------------------------
// label, other names, read, write, parse, show.
var entries = new List<Tuple<string, string[], Func<object>, Action<object>, Func<string, Tuple<object, string>>, Func<object, string>>>();
Action<string, string[], Func<object>, Action<object>, Func<string, Tuple<object, string>>, Func<object, string>> add =
    (label, aliases, get, set, parse, shown) => entries.Add(Tuple.Create(label, aliases, get, set, parse, shown));
var noNames = new string[0];

if (es != null)
{
    add("building type", noNames, () => es.BuildingType, v => es.BuildingType = (gbXMLBuildingType)v,
        checkedBy(enumParser(typeof(gbXMLBuildingType), noNames, new[] { "CustomizeBuildingType" }),
            v => EnergyDataSettings.CheckBuildingType((gbXMLBuildingType)v), "building type"), words);
    add("ground plane", noNames, () => es.GroundPlane, v => es.GroundPlane = (ElementId)v,
        v =>
        {
            var hits = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .Where(l => string.Equals(l.Name, (v ?? "").Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (hits.Count != 1)
                return ok(null, "\"" + v + "\" is not a level this model has; it has " + string.Join(", ",
                    new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Select(l => "\"" + l.Name + "\"").Take(15)));
            return es.CheckGroundPlane(hits[0].Id) ? ok(hits[0].Id, null) : ok(null, "Revit says level \"" + v + "\" cannot be the ground plane");
        }, v => nameOf((ElementId)v));
    add("project phase", new[] { "phase" }, () => es.ProjectPhase, v => es.ProjectPhase = (ElementId)v,
        v =>
        {
            Phase hit = null;
            var names = new List<string>();
            foreach (Phase ph in doc.Phases)
            {
                names.Add("\"" + ph.Name + "\"");
                if (string.Equals(ph.Name, (v ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) hit = ph;
            }
            if (hit == null) return ok(null, "\"" + v + "\" is not a phase this model has; it has " + string.Join(", ", names));
            return es.CheckProjectPhase(hit.Id) ? ok(hit.Id, null) : ok(null, "Revit says phase \"" + v + "\" cannot be the project phase");
        }, v => nameOf((ElementId)v));
    add("analysis mode", new[] { "mode", "energy analysis mode", "analytical mode", "energy model mode" },
        () => es.AnalysisType, v => es.AnalysisType = (AnalysisMode)v,
        checkedBy(enumParser(typeof(AnalysisMode), noNames, noNames),
            v => EnergyDataSettings.CheckAnalysisType((AnalysisMode)v), "analysis mode"), words);
    add("export category", noNames, () => es.ExportCategory, v => es.ExportCategory = (ElementId)v,
        checkedBy(v =>
        {
            var k = key(v);
            if (k == "rooms" || k == "room") return ok(roomsId, null);
            if (k == "spaces" || k == "space") return ok(spacesId, null);
            return ok(null, "\"" + v + "\" is not rooms or spaces");
        }, v => EnergyDataSettings.CheckExportCategory((ElementId)v), "export category"), categoryWord);
    add("export complexity", noNames, () => es.ExportComplexity, v => es.ExportComplexity = (gbXMLExportComplexity)v,
        checkedBy(enumParser(typeof(gbXMLExportComplexity), noNames, noNames),
            v => EnergyDataSettings.CheckExportComplexity((gbXMLExportComplexity)v), "export complexity"), words);
    add("include thermal properties", noNames, () => es.IncludeThermalProperties, v => es.IncludeThermalProperties = (bool)v, parseYes, yesNo);
    add("building envelope", new[] { "building envelope determination" },
        () => es.BuildingEnvelopeDeterminationMethod, v => es.BuildingEnvelopeDeterminationMethod = (gbXMLExportBuildingEnvelope)v,
        checkedBy(enumParser(typeof(gbXMLExportBuildingEnvelope), noNames, noNames),
            v => EnergyDataSettings.CheckBuildingEnvelope((gbXMLExportBuildingEnvelope)v), "building envelope"), words);
    add("building construction class", new[] { "construction class" },
        () => es.BuildingConstructionClass, v => es.BuildingConstructionClass = (HVACLoadConstructionClass)v,
        checkedBy(enumParser(typeof(HVACLoadConstructionClass), new[] { "construction" }, noNames),
            v => EnergyDataSettings.CheckBuildingConstructionClass((HVACLoadConstructionClass)v), "building construction class"), words);
    add("service type", new[] { "building service", "hvac service" }, () => es.ServiceType, v => es.ServiceType = (gbXMLServiceType)v,
        checkedBy(enumParser(typeof(gbXMLServiceType), noNames, new[] { "NoOfServiceTypes" }),
            v => EnergyDataSettings.CheckServiceType((gbXMLServiceType)v), "service type"), words);
    add("report type", new[] { "loads report type", "project report type" },
        () => es.ProjectReportType, v => es.ProjectReportType = (HVACLoadLoadsReportType)v,
        checkedBy(enumParser(typeof(HVACLoadLoadsReportType), new[] { "report" }, noNames),
            v => EnergyDataSettings.CheckProjectReportType((HVACLoadLoadsReportType)v), "report type"), words);
#if REVIT2024_OR_GREATER
    add("use current view only", noNames, () => es.UseCurrentViewOnly, v => es.UseCurrentViewOnly = (bool)v, parseYes, yesNo);
#endif
    add("sliver space tolerance", noNames, () => es.SliverSpaceTolerance, v => es.SliverSpaceTolerance = (double)v,
        checkedBy(parseMm, v => EnergyDataSettings.CheckRangeOfSliverSpaceTolerance((double)v), "sliver space tolerance"), v => mm((double)v));
    add("analytical grid cell size", noNames, () => es.AnalyticalGridCellSize, v => es.AnalyticalGridCellSize = (double)v, parseMm, v => mm((double)v));
    add("core offset", noNames, () => es.CoreOffset, v => es.CoreOffset = (double)v, parseMm, v => mm((double)v));
    add("divide perimeter zones", new[] { "divide perimeter" }, () => es.DividePerimeter, v => es.DividePerimeter = (bool)v, parseYes, yesNo);
    add("target percentage glazing", new[] { "percentage glazing", "glazing percentage" },
        () => es.PercentageGlazing, v => es.PercentageGlazing = (double)v,
        checkedBy(v =>
        {
            var n = number((v ?? "").Trim().TrimEnd('%').Trim());
            if (n == null || n.Value < 0 || n.Value > 100) return ok(null, "\"" + v + "\" is not a percentage from 0 to 100");
            return ok(glazingAsPercent ? n.Value : n.Value / 100.0, null);
        }, v => EnergyDataSettings.CheckRangeOfPercentageGlazing((double)v), "target percentage glazing"), percent);
    add("energy model", noNames, () => es.EnergyModel, v => es.EnergyModel = (bool)v, parseYes, yesNo);
    add("use heating credits", noNames, () => es.UseHeatingCredits, v => es.UseHeatingCredits = (bool)v, parseYes, yesNo);
}
var nameList = string.Join(", ", entries.Select(e => e.Item1));
Func<string> describe = () => "Energy Settings - " + string.Join("; ", entries.Select(e => e.Item1 + " " + e.Item6(e.Item3())));

// label, the value wanted, read, write, show, the value read before.
var plans = new List<Tuple<string, object, Func<object>, Action<object>, Func<object, string>, object>>();
var problems = new List<string>();

if (refused == null)
{
    var entryTexts = (settings ?? "").Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
    if (entryTexts.Count == 0)
        refused = "No setting was named, so NOTHING WAS CHANGED. Name each as \"name=value\", \";\" between: " + nameList + ".";
    foreach (var entry in entryTexts)
    {
        var eq = entry.IndexOf('=');
        if (eq <= 0) { problems.Add("\"" + entry + "\" is not name=value"); continue; }
        var askedName = entry.Substring(0, eq).Trim();
        var value = entry.Substring(eq + 1).Trim();
        var k = key(askedName);
        if (k.Contains("location") || k.Contains("schedule") || k.Contains("hvacsystem") || k.Contains("outdoorair")
            || k.Contains("outsideair") || k.Contains("shade") || k.Contains("sill") || k.Contains("skylight") || k.Contains("folder"))
        {
            problems.Add("\"" + askedName + "\" is in Energy Settings but not a setting this card changes"
                + (k.Contains("location") ? " - the project location is SET_PROJECT_LOCATION's" : " - change it in Analyze > Energy Settings"));
            continue;
        }
        var hit = entries.FirstOrDefault(e => key(e.Item1) == k || e.Item2.Any(a => key(a) == k));
        if (hit == null)
        {
#if !REVIT2024_OR_GREATER
            if (k == "usecurrentviewonly") { problems.Add("\"" + askedName + "\" is Revit 2024 and later"); continue; }
#endif
            problems.Add("\"" + askedName + "\" is not a setting this card reaches; it takes " + nameList);
            continue;
        }
        var parsed = hit.Item5(value);
        if (parsed.Item1 == null) { problems.Add(hit.Item1 + ": " + parsed.Item2); continue; }
        plans.Add(Tuple.Create(hit.Item1, parsed.Item1, hit.Item3, hit.Item4, hit.Item6, hit.Item3()));
    }
    if (refused == null && problems.Count > 0)
        refused = string.Join("; ", problems) + ". NOTHING WAS CHANGED.";
    if (refused == null)
    {
        var labels = plans.Select(p => p.Item1).ToList();
        var twice = labels.Where(l => labels.Count(x => x == l) > 1).Distinct().ToList();
        if (twice.Count > 0)
            refused = string.Join(", ", twice) + " is named more than once, so NOTHING WAS CHANGED - name each setting once.";
    }
}

// ---- write, and on a refusal by Revit put back what this call wrote ------------
if (refused == null)
{
    before = describe();
    var written = new List<Tuple<string, object, Func<object>, Action<object>, Func<object, string>, object>>();
    var already = new List<string>();
    foreach (var p in plans)
    {
        var now = p.Item5(p.Item3());
        if (now == p.Item5(p.Item2)) { already.Add(p.Item1 + " " + now); continue; }
        try { p.Item4(p.Item2); written.Add(p); }
        catch (Exception ex)
        {
            foreach (var w in Enumerable.Reverse(written))
            {
                try { w.Item4(w.Item6); }
                catch (Exception back)
                {
                    throw new InvalidOperationException("Revit refused " + p.Item1 + " = " + p.Item5(p.Item2) + " (" + ex.Message
                        + ") and then would not put " + w.Item1 + " back (" + back.Message + "). The whole change is "
                        + "rolled back, so NOTHING WAS CHANGED.", ex);
                }
            }
            refused = "Revit refused " + p.Item1 + " = " + p.Item5(p.Item2) + ": " + ex.Message + " NOTHING WAS CHANGED - "
                + (written.Count == 0 ? "nothing had been written yet." : "the " + written.Count + " setting(s) already written were put back.");
            written.Clear();
            break;
        }
    }
    if (refused == null)
    {
        var wrong = written.Where(p => p.Item5(p.Item3()) != p.Item5(p.Item2))
            .Select(p => p.Item1 + " reads back " + p.Item5(p.Item3()) + ", not " + p.Item5(p.Item2)).ToList();
        if (wrong.Count > 0)
            throw new InvalidOperationException("Revit did not keep what was asked: " + string.Join("; ", wrong)
                + ". The whole change is rolled back, so NOTHING WAS CHANGED.");
        changed = written.Count;
        after = describe();
        if (already.Count > 0)
            findings.Add("Already so, not counted: " + string.Join("; ", already) + ".");
        if (changed > 0)
            findings.Add("Energy Settings changed for the whole model (" + changed + " setting(s)): "
                + string.Join("; ", written.Select(p => p.Item1 + " " + p.Item5(p.Item2))) + ". The next energy model, "
                + "gbXML export and heating and cooling loads run use these. An energy model made before this change is "
                + "not rebuilt by it. Read back: " + after + ".");
        else
            findings.Add("Every setting asked for is already so, so nothing was changed: " + before + ".");
    }
}
if (es != null)
{
    try { energyModel = es.EnergyModel; } catch (Exception) { energyModel = false; }
}
if (refused != null) findings.Add(refused);
