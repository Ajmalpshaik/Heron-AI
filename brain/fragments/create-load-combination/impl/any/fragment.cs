// NOT STANDALONE. Assumes `doc`, `combinations`, `state`, `combinationType` and
// `usage` are in scope, and leaves `added`, `before`, `after`, `skipped`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16). Each
// combination is made inside its own SubTransaction, so one Revit refuses
// half-way leaves nothing of itself behind.
//
// WHAT IT DOES. Analyze > Load Combinations: makes one load combination for
// each entry of `combinations` - "ULS1 = 1.2 DL1 + 1.6 LL1", several separated
// by ; or a new line. Each entry is a name, "=", and terms joined by "+"; each
// term is a factor and a load case's name - "1.2DL1", "1.2 DL1", "1.2 x DL1",
// "1.2*DL1" - or a load case's name alone, factor 1. An entry with no "=" is
// named after its formula as written, and the answer says so. Nothing existing
// is changed, renamed or removed: a combination name already used, in any
// capitals, is SKIPPED and named. The load combinations are read before and
// after, each with its factors and load cases read back from the model.
//
// NEVER GUESSED (D-33). Every load case is matched by its EXACT name; one
// that differs only in capitals is named, never taken; a name no case has
// refuses that combination with the cases listed. A load case is NEVER made
// here - CREATE_LOAD_CASE does that. A term naming a load COMBINATION is
// refused: components here are load cases only. A load case twice in one
// formula is refused. A state or type left out keeps Revit's own default,
// read back and stated. A usage is matched by its exact name and never made.
//
// RELEASES (tools/api-surface --members, 2026-11-02). LoadCombination.Create
// (Document, string) and (Document, string, LoadCombinationType,
// LoadCombinationState), State and Type { get; set; }, GetComponents,
// SetComponents(IList<LoadComponent>), GetUsageIds, SetUsageIds(IList<ElementId>);
// new LoadComponent(ElementId, double), LoadCaseOrCombinationId, Factor;
// LoadUsage; LoadCombinationType Combination and Envelope; LoadCombinationState
// Serviceability and Ultimate; SubTransaction - the same 2020 to 2027.
// LoadCombinationState ServiceabilityQuasiPermanent, ServiceabilityFrequent,
// ServiceabilityRare, Accidental and Special are 2026 on (#if
// REVIT2026_OR_GREATER); asked for earlier, refused with the release named.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var added = 0;
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<double, string> num = v => Math.Round(v, 4).ToString("0.####", invariant);
Func<string, string> squash = s => (s ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
// A request need the card marks `absent: none` arrives as the text "none" when
// left out, and an empty text is a value left out too.
Func<string, bool> leftOut = s =>
{
    var t = (s ?? "").Trim();
    return t.Length == 0 || string.Equals(t, "none", StringComparison.OrdinalIgnoreCase);
};

// ---- What the project has ----------------------------------------------------
Func<List<LoadCombination>> readCombinations = () => new FilteredElementCollector(doc).OfClass(typeof(LoadCombination))
    .Cast<LoadCombination>().OrderBy(c => c.Name).ToList();
Func<ElementId, string> nameOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "?";
    var e = doc.GetElement(id);
    return e == null ? "?" : e.Name;
};
Func<LoadCombination, string> combinationLine = c =>
{
    var parts = new List<string>();
    try
    {
        foreach (var comp in c.GetComponents())
            parts.Add(num(comp.Factor) + " x " + quoted(nameOf(comp.LoadCaseOrCombinationId)));
    }
    catch { parts.Add("components unreadable"); }
    var usages = new List<string>();
    try { foreach (var id in c.GetUsageIds()) usages.Add(quoted(nameOf(id))); } catch { }
    string st = "?", ty = "?";
    try { st = c.State.ToString(); } catch { }
    try { ty = c.Type.ToString(); } catch { }
    return quoted(c.Name) + " - " + st + ", " + ty + ": " + (parts.Count == 0 ? "no components" : string.Join(" + ", parts))
        + (usages.Count == 0 ? "" : "; usage " + string.Join(", ", usages));
};

var existing = readCombinations();
foreach (var c in existing) before.Add(combinationLine(c));
var cases = new FilteredElementCollector(doc).OfClass(typeof(LoadCase)).Cast<LoadCase>()
    .OrderBy(c => c.Number).ThenBy(c => c.Name).ToList();
var caseList = cases.Count == 0 ? "none" : string.Join(", ", cases.Select(c => quoted(c.Name)));
var usagesHad = new FilteredElementCollector(doc).OfClass(typeof(LoadUsage)).Cast<LoadUsage>()
    .OrderBy(u => u.Name).ToList();
var usageList = usagesHad.Count == 0 ? "none" : string.Join(", ", usagesHad.Select(u => quoted(u.Name)));

var ok = true;

// ---- The state, for every combination of this call ---------------------------
LoadCombinationState? wantState = null;
if (!leftOut(state))
{
    var w = squash(state);
    if (w == "ultimate" || w == "uls" || w == "ultimatelimitstate") wantState = LoadCombinationState.Ultimate;
    else if (w == "serviceability" || w == "sls" || w == "serviceabilitylimitstate") wantState = LoadCombinationState.Serviceability;
#if REVIT2026_OR_GREATER
    else if (w == "quasipermanent" || w == "serviceabilityquasipermanent") wantState = LoadCombinationState.ServiceabilityQuasiPermanent;
    else if (w == "frequent" || w == "serviceabilityfrequent") wantState = LoadCombinationState.ServiceabilityFrequent;
    else if (w == "rare" || w == "serviceabilityrare") wantState = LoadCombinationState.ServiceabilityRare;
    else if (w == "accidental") wantState = LoadCombinationState.Accidental;
    else if (w == "special") wantState = LoadCombinationState.Special;
    var stateWords = "Ultimate, Serviceability, Serviceability Quasi-Permanent, Serviceability Frequent, "
        + "Serviceability Rare, Accidental or Special";
#else
    var laterOnly = new[] { "quasipermanent", "serviceabilityquasipermanent", "frequent", "serviceabilityfrequent",
        "rare", "serviceabilityrare", "accidental", "special" };
    var stateWords = "Ultimate or Serviceability";
    if (wantState == null && laterOnly.Contains(w))
    {
        refused.Add("The state " + quoted(state.Trim()) + " is in Revit 2026 and later only; this Revit has "
            + stateWords + ". Nothing was made.");
        ok = false;
    }
#endif
    if (wantState == null && ok)
    {
        refused.Add(quoted(state.Trim()) + " is not a load combination state. Revit's states here: " + stateWords
            + ". Nothing was made.");
        ok = false;
    }
}

// ---- The type ----------------------------------------------------------------
LoadCombinationType? wantType = null;
if (ok && !leftOut(combinationType))
{
    var w = squash(combinationType);
    if (w == "combination" || w == "combo" || w == "linear") wantType = LoadCombinationType.Combination;
    else if (w == "envelope") wantType = LoadCombinationType.Envelope;
    else
    {
        refused.Add(quoted(combinationType.Trim()) + " is not a load combination type - Combination or Envelope. "
            + "Nothing was made.");
        ok = false;
    }
}

// ---- The usages - matched exactly, never made --------------------------------
var usageIds = new List<ElementId>();
if (ok && !leftOut(usage))
{
    foreach (var raw in usage.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var u = raw.Trim();
        if (u.Length == 0) continue;
        var hit = usagesHad.FirstOrDefault(x => x.Name == u);
        if (hit == null)
        {
            var near = usagesHad.FirstOrDefault(x => string.Equals(x.Name, u, StringComparison.OrdinalIgnoreCase));
            refused.Add(near != null
                ? "No load usage is called exactly " + quoted(u) + "; " + quoted(near.Name)
                  + " differs only in capitals and was NOT taken. Nothing was made."
                : quoted(u) + " is not a load usage this project has: " + usageList + ". A usage is never made "
                  + "here. Nothing was made.");
            ok = false;
        }
        else if (!usageIds.Contains(hit.Id)) usageIds.Add(hit.Id);
    }
}

// ---- The entries ---------------------------------------------------------------
var entries = (combinations ?? "").Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
if (ok && entries.Count == 0)
{
    refused.Add("No load combination was given - write one as a name, \"=\" and factored load cases, such as "
        + "ULS1 = 1.2 DL1 + 1.6 LL1. The load cases in this model: " + caseList + ".");
    ok = false;
}

// "1.2DL1", "1.2 DL1", "1.2 x DL1", "1.2*DL1", "-0.9 W1" - or a name alone.
var term = new System.Text.RegularExpressions.Regex(
    @"^(?<f>[+-]?(?:\d+(?:\.\d+)?|\.\d+))\s*(?:(?:\*|\u00D7)\s*|[xX]\s+)?(?<n>.+?)\s*$");
var namesTaken = new List<string>(existing.Select(c => c.Name));

foreach (var entry in ok ? entries : new List<string>())
{
    string name, formula;
    var eq = entry.IndexOf('=');
    if (eq >= 0)
    {
        name = entry.Substring(0, eq).Trim();
        formula = entry.Substring(eq + 1).Trim();
        if (name.Length == 0)
        {
            refused.Add(quoted(entry) + " - nothing before \"=\", so no name. A name is never made up; "
                + "leave out \"=\" to name it after its formula.");
            continue;
        }
    }
    else
    {
        formula = entry;
        name = System.Text.RegularExpressions.Regex.Replace(entry, @"\s+", " ").Trim();
        findings.Add("No name was given for " + quoted(entry) + ", so it is named after its formula, as written: "
            + quoted(name) + ".");
    }

    var clash = namesTaken.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
    if (clash != null)
    {
        var held = readCombinations().FirstOrDefault(c => c.Name == clash);
        skipped.Add(quoted(name) + " - the load combination " + (held != null ? combinationLine(held) : quoted(clash))
            + " already has that name; left as it is, nothing renamed or changed.");
        continue;
    }

    // Each term: a factor and an EXACT load case name.
    var components = new List<LoadComponent>();
    var wanted = new List<Tuple<double, LoadCase>>();
    var problem = "";
    foreach (var rawTerm in formula.Split('+'))
    {
        var t = rawTerm.Trim();
        if (t.Length == 0)
        {
            problem = "an empty term - two \"+\" together, or one at an end, in " + quoted(formula);
            break;
        }
        // A factor read off the front, unless the whole term is a load case's
        // name and what follows the number is not - "2 Snow" as a case's name.
        double factor = 1.0;
        string caseName = t;
        var noFactor = true;
        var m = term.Match(t);
        var parsedName = m.Success ? m.Groups["n"].Value.Trim() : "";
        if (m.Success && (cases.Any(c => c.Name == parsedName) || !cases.Any(c => c.Name == t)))
        {
            factor = double.Parse(m.Groups["f"].Value, invariant);
            caseName = parsedName;
            noFactor = false;
        }
        var hit = cases.FirstOrDefault(c => c.Name == caseName);
        if (hit == null)
        {
            var near = cases.FirstOrDefault(c => string.Equals(c.Name, caseName, StringComparison.OrdinalIgnoreCase));
            var combo = existing.FirstOrDefault(c => c.Name == caseName);
            if (near != null)
                problem = "no load case is called exactly " + quoted(caseName) + "; " + quoted(near.Name)
                    + " differs only in capitals and was NOT taken";
            else if (combo != null)
                problem = quoted(caseName) + " is a load combination, not a load case - this builds combinations from "
                    + "load cases only";
            else
                problem = "no load case is called " + quoted(caseName) + " - a load case is never made here. The load "
                    + "cases in this model: " + caseList
                    + (t.Contains(" - ") ? ". A minus is written \"+ -0.9 W1\"" : "");
            break;
        }
        if (wanted.Any(x => x.Item2.Id == hit.Id))
        {
            problem = "the load case " + quoted(hit.Name) + " is in " + quoted(formula) + " twice - give it once, "
                + "with its whole factor";
            break;
        }
        if (noFactor)
            findings.Add(quoted(t) + " in " + quoted(name) + " has no factor written, so its factor is 1.");
        wanted.Add(Tuple.Create(factor, hit));
    }
    if (problem.Length > 0)
    {
        refused.Add(quoted(name) + " was not made: " + problem + ".");
        continue;
    }
    foreach (var w in wanted) components.Add(new LoadComponent(w.Item2.Id, w.Item1));

    // ---- Make it, all or nothing ----------------------------------------------
    LoadCombination made = null;
    var sub = new SubTransaction(doc);
    try
    {
        sub.Start();
        made = LoadCombination.Create(doc, name);
        if (wantType.HasValue) made.Type = wantType.Value;
        if (wantState.HasValue) made.State = wantState.Value;
        made.SetComponents(components);
        if (usageIds.Count > 0) made.SetUsageIds(usageIds);
        sub.Commit();
    }
    catch (Exception ex)
    {
        try { if (sub.HasStarted()) sub.RollBack(); } catch { }
        made = null;
        refused.Add("Revit would not make the load combination " + quoted(name) + ": " + ex.Message
            + " - nothing of it was kept.");
    }
    if (made == null) continue;

    added++;
    namesTaken.Add(made.Name);
    findings.Add("Made the load combination " + combinationLine(made) + ", read back from the model.");

    // Read back against what was asked.
    var back = new List<LoadComponent>();
    try { back = made.GetComponents().ToList(); } catch { }
    var matches = back.Count == wanted.Count && wanted.All(w => back.Any(b =>
        b.LoadCaseOrCombinationId == w.Item2.Id && Math.Abs(b.Factor - w.Item1) < 1e-9));
    if (!matches)
        findings.Add("Read back, " + quoted(made.Name) + "'s factors and load cases are not the ones asked for - look at it "
            + "in Analyze > Load Combinations.");
    if (!wantState.HasValue)
    {
        var said = squash(name);
        findings.Add("No state was asked for " + quoted(made.Name) + ", so Revit's own default was kept: "
            + made.State + "."
            + (said.StartsWith("sls") && made.State != LoadCombinationState.Serviceability
                ? " Its name begins SLS - say serviceability to set it."
                : said.StartsWith("uls") && made.State != LoadCombinationState.Ultimate
                    ? " Its name begins ULS - say ultimate to set it." : ""));
    }
    if (!wantType.HasValue)
        findings.Add("No type was asked for " + quoted(made.Name) + ", so Revit's own default was kept: " + made.Type + ".");
}

foreach (var c in readCombinations()) after.Add(combinationLine(c));
if (added == 0) findings.Add("Nothing was added - the load combinations are as they were.");
