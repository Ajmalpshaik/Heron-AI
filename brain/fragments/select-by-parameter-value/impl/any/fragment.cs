// NOT STANDALONE. Assumes `doc`, `parameterName`, `matchText`, `matchMode`,
// `includeTypeParameters`, `categories`, `includeLinks`, `exactMatch`,
// `compareValue`, `compareValueMax` and `tolerance` are in scope; leaves
// `elements`, `withoutParameter`, `ambiguous`, `findings`, `linksSearched`,
// `linkedMatches`, `notNumeric`, `matchedFamilies`, `scannedWholeModel` and
// `found` behind.
//
// VERSION 4 (2026-11-01) - SELECT_BY_FAMILY AND SELECT_BY_NUMERIC_PARAMETER
// MERGED IN.
//
//   From SELECT_BY_FAMILY: `exactMatch` true is matchMode equals; every match
//   is counted by its family in `matchedFamilies`, so a contains match that
//   caught more than was meant says so; `scannedWholeModel` says the sweep was
//   unbounded (no categories); `found` is the count. A BLANK family or type
//   name to look for matches NOTHING and is said in words, as it was there -
//   "contains ''" would otherwise select every element that has a family.
//
//   From SELECT_BY_NUMERIC_PARAMETER: the numeric modes gt, gte, lt, lte,
//   between, eq and ne compare the STORED number - a double or an integer -
//   with `compareValue` (and `compareValueMax` for between) IN REVIT'S
//   INTERNAL UNIT: a length in decimal feet, so 500 mm is handed in as
//   500 / 304.8. Converting here would be a promise no units call can keep for
//   an airflow or an angle (D-20), so the finding states the comparison in
//   internal units AND in millimetres IF the parameter is a length. eq and ne
//   are a TOLERANCE (`tolerance`, or 1/304.8 when 0 or left out), never ==.
//   A parameter held as text is `notNumeric`, never compared; one missing or
//   empty is `withoutParameter`. With no compareValue, a number typed as
//   matchText is used; neither is refused before anything is scanned.
//
// A MISSING PARAMETER IS NOT AN EMPTY STRING, AND THE DIFFERENCE ONLY SHOWS UP
// ON A NEGATIVE QUERY. Read a missing parameter as "" and "Mark does not
// contain ABC" matches every element in the model that has no Mark - thousands
// of them, all reported as hits. So an element that does not carry the
// parameter is COUNTED in withoutParameter and never matched, whatever the
// mode. The same count is what turns a mistyped parameter name into
// "0 matched, 4,812 do not carry it" instead of a plausible zero.
//
// A NAME TWO PARAMETERS SHARE IS NOT READ, AND THE ELEMENT IS NEVER MATCHED.
// A shared or project parameter can be bound beside a built-in one of the same
// name, and a curtain wall type shows its mullion settings twice. Asked by
// name, Revit then returns "the first one encountered", which its own
// reference says "is determined at random" - so a match or a miss on such an
// element would be a coin toss reported as a fact. It is counted in
// `ambiguous` instead, where the lookup would land: on the element, or on its
// type when the element has none and type parameters were asked for (D-54 s3,
// FRAGMENT-ISSUES 5b-203). Family, Type and Family and Type are not looked up
// by name, so they are never shared.
//
// THIS MATCHES TEXT. A double only becomes "500" after Revit formats it in the
// project's display units, so searching for a number here is a coin toss -
// SELECT_BY_NUMERIC_PARAMETER compares the stored value instead.
//
// FAMILY, TYPE AND "FAMILY AND TYPE" ARE UNDERSTOOD AS NAMES because that is
// what a modeller says. "Family and Type" is matched as `Family : Type`, the
// way Revit's own properties palette writes it.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements`: that list feeds the next
// fragment in a chain, and the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// A TYPE OR A REFERENCED ELEMENT IS LOOKED UP IN THE ELEMENT'S OWN DOCUMENT,
// not in `doc`. A linked element's ids belong to the link; asked of the host
// they name nothing, or something else. For a host element the two are the
// same document, so nothing the host answer measured has moved.

var elements = new List<Element>();
var withoutParameter = 0;
var ambiguous = 0;
var findings = new List<string>();
// Version 4.
var notNumeric = 0;
var matchedFamilies = new Dictionary<string, int>();
bool scannedWholeModel = categories == null || categories.Count == 0;
const double MillimetresPerFoot = 304.8;

var wantName = string.IsNullOrEmpty(parameterName) ? "" : parameterName.Trim();
var needle = matchText == null ? "" : matchText;
var mode = string.IsNullOrEmpty(matchMode) ? "contains" : matchMode.Trim().ToLowerInvariant();

// THE MODE IS CHECKED BEFORE ANYTHING IS SCANNED (FRAGMENT-ISSUES 5b-1319). A
// word this did not know fell through to "contains" with nothing said:
// matchMode=exact for "Outlet Port" answered "Outlet Port" AND "Outlet Port
// Bore", and the next call would have deleted the plane carrying a connector.
// "exact" and "is" are what a modeller says for equals, so they are read as
// it; any other word is refused here, naming the modes, and no element is read.
if (mode == "exact" || mode == "is") mode = "equals";
// VERSION 4 - SELECT_BY_NUMERIC_PARAMETER's comparisons, by its own words and
// their signs.
if (mode == ">") mode = "gt";
if (mode == ">=") mode = "gte";
if (mode == "<") mode = "lt";
if (mode == "<=") mode = "lte";
if (mode == "=" || mode == "==") mode = "eq";
if (mode == "!=" || mode == "<>") mode = "ne";
var knownModes = new List<string> { "contains", "equals", "begins", "ends", "notcontains", "notequals" };
var numericModes = new List<string> { "gt", "gte", "lt", "lte", "between", "eq", "ne" };
if (!knownModes.Contains(mode) && !numericModes.Contains(mode))
    throw new InvalidOperationException("matchMode '" + matchMode.Trim() + "' is not a way of matching "
        + "this knows, so NOTHING was scanned. Use contains, equals (or exact, is), begins, ends, "
        + "notcontains or notequals for text, or gt, gte, lt, lte, between, eq or ne for a number");
bool numeric = numericModes.Contains(mode);

// SELECT_BY_FAMILY's exactMatch is equals. Beside a mode that is neither
// equals nor contains it is two answers to one question, and refused.
if (exactMatch)
{
    if (mode == "contains" || mode == "equals") mode = "equals";
    else
        throw new InvalidOperationException("exactMatch was asked with matchMode '" + matchMode.Trim()
            + "' - two different ways of matching, so NOTHING was scanned. exactMatch is equals; "
            + "leave one of them out");
}

// THE NUMBER, IN REVIT'S INTERNAL UNIT. Checked before anything is scanned.
double lowValue = compareValue;
double typedNumber;
bool typedIsNumber = double.TryParse((matchText ?? "").Trim(), System.Globalization.NumberStyles.Float,
    System.Globalization.CultureInfo.InvariantCulture, out typedNumber);
if (numeric && double.IsNaN(lowValue) && typedIsNumber) lowValue = typedNumber;
else if (numeric && typedIsNumber && Math.Abs(typedNumber - compareValue) > 1e-12)
    throw new InvalidOperationException(string.Format("compareValue {0} and matchText '{1}' are two "
        + "different numbers, so NOTHING was scanned. Give one", compareValue, matchText.Trim()));
if (numeric && double.IsNaN(lowValue))
    throw new InvalidOperationException("matchMode '" + mode + "' compares a number and none was given, "
        + "so NOTHING was scanned. Give compareValue in Revit's internal unit - a length in feet, so "
        + "500 mm is 500/304.8 = 1.6404");
if (numeric && mode == "between" && (double.IsNaN(compareValueMax) || compareValueMax <= lowValue))
    throw new InvalidOperationException(string.Format("A 'between' needs compareValueMax above the lower "
        + "bound - {0} to {1} is not a range, so NOTHING was scanned", lowValue,
        double.IsNaN(compareValueMax) ? "(none given)" : compareValueMax.ToString()));
var slack = tolerance <= 0 || double.IsNaN(tolerance) ? 1.0 / MillimetresPerFoot : tolerance;

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

bool namingFamily = string.Equals(wantName, "Family and Type", StringComparison.OrdinalIgnoreCase)
    || string.Equals(wantName, "Family", StringComparison.OrdinalIgnoreCase)
    || string.Equals(wantName, "Type", StringComparison.OrdinalIgnoreCase);

if (wantName.Length == 0)
{
    findings.Add("No parameter was named - say which parameter to match, or Family, Type or "
        + "'Family and Type'");
}
else if (namingFamily && numeric)
{
    findings.Add(string.Format("'{0}' is a name, and '{1}' compares a number - NOTHING was scanned. Use "
        + "contains or equals for a name", wantName, mode));
}
else if (namingFamily && needle.Trim().Length == 0)
{
    // SELECT_BY_FAMILY's rule: no name to look for matches nothing. "contains
    // ''" would select every element that has a family.
    findings.Add(string.Format("No {0} name was given to look for, so NOTHING was scanned - a blank would "
        + "match every element that has one", wantName.ToLowerInvariant()));
}
else
{
    Func<Element, ElementType> typeOf = element =>
    {
        var instance = element as FamilyInstance;
        if (instance != null && instance.Symbol != null) return instance.Symbol;
        return element.Document.GetElement(element.GetTypeId()) as ElementType;
    };

    Func<ElementType, string> familyNameOf = type =>
    {
        if (type == null) return null;
        var name = type.FamilyName;
        if (!string.IsNullOrEmpty(name)) return name;

        var p = type.get_Parameter(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)
             ?? type.get_Parameter(BuiltInParameter.ALL_MODEL_FAMILY_NAME);
        return p == null ? null : p.AsString();
    };

    Func<Parameter, string> textOf = p =>
    {
        if (p == null || !p.HasValue) return null;

        if (p.StorageType == StorageType.String) return p.AsString() ?? p.AsValueString();
        if (p.StorageType == StorageType.Integer) return p.AsValueString() ?? p.AsInteger().ToString();
        if (p.StorageType == StorageType.Double) return p.AsValueString();
        if (p.StorageType == StorageType.ElementId)
        {
            var id = p.AsElementId();
            if (id == ElementId.InvalidElementId) return null;
            var referenced = p.Element == null ? null : p.Element.Document.GetElement(id);
            // BY NAME, never by the id as a number.
            return referenced == null ? null : referenced.Name;
        }
        return null;
    };

    // null means "this element does not carry that parameter at all", which is
    // a different answer from "it carries it and it is empty".
    Func<Element, string> valueOf = element =>
    {
        if (string.Equals(wantName, "Family and Type", StringComparison.OrdinalIgnoreCase))
        {
            var type = typeOf(element);
            if (type == null) return null;
            var family = familyNameOf(type);
            return string.IsNullOrEmpty(family) ? type.Name : family + " : " + type.Name;
        }

        if (string.Equals(wantName, "Family", StringComparison.OrdinalIgnoreCase))
            return familyNameOf(typeOf(element));

        if (string.Equals(wantName, "Type", StringComparison.OrdinalIgnoreCase))
        {
            var type = typeOf(element);
            return type == null ? null : type.Name;
        }

        var p = element.LookupParameter(wantName);
        if (p == null && includeTypeParameters)
        {
            var type = typeOf(element);
            if (type != null) p = type.LookupParameter(wantName);
        }
        if (p == null) return null;

        return textOf(p) ?? "";   // present but empty is "", not null
    };

    // TWO PARAMETERS BY THIS NAME WHERE valueOf WOULD LOOK - see the header.
    // Asked first, so a shared name is never read at all.
    var byName = !string.Equals(wantName, "Family and Type", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(wantName, "Family", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(wantName, "Type", StringComparison.OrdinalIgnoreCase);

    Func<Element, bool> nameIsShared = element =>
    {
        if (!byName) return false;
        var onElement = element.GetParameters(wantName).Count;
        if (onElement > 1) return true;
        if (onElement > 0 || !includeTypeParameters) return false;
        var type = typeOf(element);
        return type != null && type.GetParameters(wantName).Count > 1;
    };

    // VERSION 4 - SELECT_BY_NUMERIC_PARAMETER's parameter: the same lookup as
    // valueOf, kept as the Parameter so its stored number can be read.
    Func<Element, Parameter> parameterOf = element =>
    {
        var p = element.LookupParameter(wantName);
        if (p == null && includeTypeParameters)
        {
            var type = typeOf(element);
            if (type != null) p = type.LookupParameter(wantName);
        }
        return p;
    };

    // eq IS A TOLERANCE, NEVER AN EQUALITY - two doubles that both display as
    // 500 mm are not the same double.
    Func<double, bool> passes = value =>
    {
        if (mode == "gt") return value > lowValue;
        if (mode == "gte") return value >= lowValue;
        if (mode == "lt") return value < lowValue;
        if (mode == "lte") return value <= lowValue;
        if (mode == "between") return value >= lowValue && value <= compareValueMax;
        if (mode == "ne") return Math.Abs(value - lowValue) > slack;
        return Math.Abs(value - lowValue) <= slack;
    };

    // 1 matched, 0 not, -1 missing or empty, -2 held as text.
    Func<Element, int> numberTest = element =>
    {
        var p = parameterOf(element);
        if (p == null || !p.HasValue) return -1;
        double value;
        if (p.StorageType == StorageType.Double) value = p.AsDouble();
        else if (p.StorageType == StorageType.Integer) value = p.AsInteger();
        else return -2;
        return passes(value) ? 1 : 0;
    };

    Func<string, bool> matchesNeedle = value =>
    {
        if (mode == "equals") return string.Equals(value, needle, StringComparison.OrdinalIgnoreCase);
        if (mode == "begins") return value.StartsWith(needle, StringComparison.OrdinalIgnoreCase);
        if (mode == "ends") return value.EndsWith(needle, StringComparison.OrdinalIgnoreCase);
        if (mode == "notcontains") return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0;
        if (mode == "notequals") return !string.Equals(value, needle, StringComparison.OrdinalIgnoreCase);
        return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    };

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    var scanned = 0;
    foreach (var element in collector)
    {
        scanned++;

        var shared = false;
        try { shared = nameIsShared(element); }
        catch (Exception) { shared = false; }
        if (shared)
        {
            ambiguous++;
            continue;
        }

        if (numeric)
        {
            int test;
            try { test = numberTest(element); }
            catch { test = -1; }
            if (test == -1) { withoutParameter++; continue; }
            if (test == -2) { notNumeric++; continue; }
            if (test == 1) elements.Add(element);
            continue;
        }

        string value = null;
        try { value = valueOf(element); }
        catch { value = null; }

        if (value == null)
        {
            withoutParameter++;
            continue;
        }

        if (matchesNeedle(value)) elements.Add(element);
    }

    // SELECT_BY_FAMILY's `matchedFamilies`: every match by its family, so a
    // contains that caught more than was meant shows which.
    foreach (var element in elements)
    {
        string family = null;
        try { family = familyNameOf(typeOf(element)); } catch { family = null; }
        var key = string.IsNullOrEmpty(family) ? "(no family)" : family;
        matchedFamilies[key] = matchedFamilies.ContainsKey(key) ? matchedFamilies[key] + 1 : 1;
    }

    if (numeric)
    {
        var asMm = mode == "between"
            ? string.Format("{0:0.###} to {1:0.###}", lowValue * MillimetresPerFoot,
                compareValueMax * MillimetresPerFoot)
            : string.Format("{0:0.###}", lowValue * MillimetresPerFoot);
        findings.Add(string.Format("{0} of {1} scanned element(s) have {2} {3} {4} in internal units - "
            + "which is {5} mm IF this parameter is a length, and something else if it is not. {6} do "
            + "not carry '{2}' or carry it empty and {7} hold it as text - never compared",
            elements.Count, scanned, wantName, mode,
            mode == "between" ? string.Format("{0} to {1}", lowValue, compareValueMax) : lowValue.ToString(),
            asMm, withoutParameter, notNumeric));
        if (notNumeric > 0)
            findings.Add(string.Format("{0} element(s) carry '{1}' as text rather than a number - a text "
                + "mode (contains, equals, ...) is the one that matches those", notNumeric, wantName));
        if (mode == "eq" || mode == "ne")
            findings.Add(string.Format("'{0}' was tested to a tolerance of {1:0.####} internal units "
                + "({2:0.###} mm if a length), because two doubles that display the same are not the "
                + "same double", mode, slack, slack * MillimetresPerFoot));
    }
    else
    {
        findings.Add(string.Format("{0} of {1} scanned element(s) match {2} {3} '{4}'. {5} do not carry "
            + "'{2}' at all and were never tested",
            elements.Count, scanned, wantName, mode, needle, withoutParameter));
    }
    if (scannedWholeModel)
        findings.Add("No category was given, so the WHOLE model was scanned - fine once, expensive in a "
            + "loop; name the categories when they are known");

    if (ambiguous > 0)
        findings.Add(string.Format("{0} element(s) carry TWO OR MORE parameters named '{1}' and were "
            + "NOT tested - asked by name, Revit picks one of them at random, so a match or a miss "
            + "there would be a guess. Select one and look in Properties to see both",
            ambiguous, wantName));

    if (withoutParameter == scanned && scanned > 0)
        findings.Add(string.Format("NOTHING carries '{0}'. That is a parameter name that does not "
            + "exist on these elements, not a model with no matches - check the spelling, and whether "
            + "it is a type parameter", wantName));

    if (mode == "notcontains" || mode == "notequals")
        findings.Add("This is a NEGATIVE match, so elements that do not carry the parameter are "
            + "excluded rather than counted as matches - they are in the untested figure above");

    // THE SAME TEST IN EACH LINK. Counted, never selected - see the header.
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories));

        var linkScanned = 0;
        var linkMatched = 0;
        var linkWithout = 0;
        var linkShared = 0;

        foreach (var element in linkedCollector)
        {
            linkScanned++;

            var shared = false;
            try { shared = nameIsShared(element); }
            catch (Exception) { shared = false; }
            if (shared) { linkShared++; continue; }

            if (numeric)
            {
                int test;
                try { test = numberTest(element); }
                catch (Exception) { test = -1; }
                if (test < 0) { linkWithout++; continue; }
                if (test == 1) linkMatched++;
                continue;
            }

            string value = null;
            try { value = valueOf(element); }
            catch (Exception) { value = null; }

            if (value == null) { linkWithout++; continue; }
            if (matchesNeedle(value)) linkMatched++;
        }

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1} of {2} match; {3} do not carry '{4}'{5}",
            linked.Title, linkMatched, linkScanned, linkWithout, wantName,
            linkShared > 0
                ? string.Format("; {0} carry it twice and were NOT tested", linkShared)
                : ""));
    }
}

if (includeLinks && linkDocs.Count > 0 && linksSearched == 0)
    linkBlocked = "Links NOT read: no parameter was named - see findings";

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} match(es), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");

// SELECT_BY_FAMILY's count, version 4.
int found = elements.Count;
