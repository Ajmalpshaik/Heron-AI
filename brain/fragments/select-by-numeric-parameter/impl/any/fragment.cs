// NOT STANDALONE. Assumes `doc`, `parameterName`, `comparison`, `compareValue`,
// `compareValueMax`, `tolerance`, `includeTypeParameters`, `categories` and
// `includeLinks` are in scope; leaves `elements`, `withoutParameter`,
// `notNumeric`, `findings`, `linksSearched` and `linkedMatches` behind.
//
// THE VALUE ARRIVES IN REVIT'S INTERNAL UNIT, AND THAT IS A REFUSAL TO GUESS. A
// length is decimal feet, so 500 mm is handed in as 500 / 304.8. An airflow, a
// pressure, an angle and a temperature each have a different internal unit, and
// the only way to know which one this parameter uses is a units API - the one
// that changed shape at 2021, which is why D-20 keeps conversion at the edge.
// Accepting millimetres here would be a promise this code cannot keep.
//
// SO THE READ-BACK STATES BOTH: the comparison in internal units, and what that
// is in millimetres if the parameter is a length. A value handed in without
// converting shows up as an absurd millimetre figure instead of as silence.
//
// AN INTEGER PARAMETER IS COMPARED DIRECTLY, NOT SKIPPED. Testing only for
// StorageType.Double turns "poles equals 3" into a clean, wrong zero. Integers
// are counts and flags; they carry no unit and the same number is the right
// comparison.
//
// eq IS A TOLERANCE, NEVER AN EQUALITY. Two doubles that both display as 500 mm
// are not the same double, and == finds almost nothing while looking like a
// filter that ran.
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
// A TYPE IS LOOKED UP IN THE ELEMENT'S OWN DOCUMENT, not in `doc`. A linked
// element's type id belongs to the link; asked of the host it names nothing,
// or something else. For a host element the two are the same document.

var elements = new List<Element>();
var withoutParameter = 0;
var notNumeric = 0;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;

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

var wantName = string.IsNullOrEmpty(parameterName) ? "" : parameterName.Trim();
var how = string.IsNullOrEmpty(comparison) ? "eq" : comparison.Trim().ToLowerInvariant();
var slack = tolerance <= 0 ? 1.0 / MillimetresPerFoot : tolerance;

if (wantName.Length == 0)
{
    findings.Add("No parameter was named - say which numeric parameter to compare");
}
else if (how == "between" && compareValueMax <= compareValue)
{
    findings.Add(string.Format("A 'between' needs an upper bound above the lower one - {0} to {1} "
        + "is not a range", compareValue, compareValueMax));
}
else
{
    Func<Element, Parameter> parameterOf = element =>
    {
        var p = element.LookupParameter(wantName);
        if (p == null && includeTypeParameters)
        {
            var instance = element as FamilyInstance;
            ElementType type = instance != null && instance.Symbol != null
                ? instance.Symbol
                : element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (type != null) p = type.LookupParameter(wantName);
        }
        return p;
    };

    Func<double, bool> passes = value =>
    {
        if (how == "gt") return value > compareValue;
        if (how == "gte") return value >= compareValue;
        if (how == "lt") return value < compareValue;
        if (how == "lte") return value <= compareValue;
        if (how == "between") return value >= compareValue && value <= compareValueMax;
        if (how == "ne") return Math.Abs(value - compareValue) > slack;
        return Math.Abs(value - compareValue) <= slack;   // "eq", and it is a tolerance
    };

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    var scanned = 0;
    foreach (var element in collector)
    {
        scanned++;

        Parameter p = null;
        try { p = parameterOf(element); }
        catch { p = null; }

        if (p == null || !p.HasValue)
        {
            withoutParameter++;
            continue;
        }

        double value;
        if (p.StorageType == StorageType.Double) value = p.AsDouble();
        else if (p.StorageType == StorageType.Integer) value = p.AsInteger();
        else
        {
            notNumeric++;
            continue;
        }

        if (passes(value)) elements.Add(element);
    }

    var asMm = how == "between"
        ? string.Format("{0:0.###} to {1:0.###}", compareValue * MillimetresPerFoot,
            compareValueMax * MillimetresPerFoot)
        : string.Format("{0:0.###}", compareValue * MillimetresPerFoot);

    findings.Add(string.Format("{0} of {1} scanned element(s) have {2} {3} {4} in internal units - "
        + "which is {5} mm IF this parameter is a length, and something else if it is not",
        elements.Count, scanned, wantName, how,
        how == "between" ? string.Format("{0} to {1}", compareValue, compareValueMax)
                         : compareValue.ToString(),
        asMm));

    if (withoutParameter > 0)
        findings.Add(string.Format("{0} element(s) do not carry '{1}' at all, or carry it empty, and "
            + "were never compared", withoutParameter, wantName));

    if (notNumeric > 0)
        findings.Add(string.Format("{0} element(s) carry '{1}' as text rather than a number - "
            + "SELECT_BY_PARAMETER_VALUE is the one that matches those", notNumeric, wantName));

    if (withoutParameter == scanned && scanned > 0)
        findings.Add(string.Format("NOTHING carries '{0}' as a value. That is a parameter name that "
            + "does not exist on these elements, not a model with no matches", wantName));

    if (how == "eq" || how == "ne")
        findings.Add(string.Format("'{0}' was tested to a tolerance of {1:0.####} internal units "
            + "({2:0.###} mm if a length), because two doubles that display the same are not the "
            + "same double", how, slack, slack * MillimetresPerFoot));

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
        var linkNotNumeric = 0;

        foreach (var element in linkedCollector)
        {
            linkScanned++;

            Parameter p = null;
            try { p = parameterOf(element); }
            catch (Exception) { p = null; }

            if (p == null || !p.HasValue) { linkWithout++; continue; }

            double value;
            if (p.StorageType == StorageType.Double) value = p.AsDouble();
            else if (p.StorageType == StorageType.Integer) value = p.AsInteger();
            else { linkNotNumeric++; continue; }

            if (passes(value)) linkMatched++;
        }

        linksSearched++;
        linkedTotal += linkMatched;
        linkedMatches.Add(string.Format("{0}: {1} of {2} match; {3} lack '{4}', {5} hold it as text",
            linked.Title, linkMatched, linkScanned, linkWithout, wantName, linkNotNumeric));
    }
}

if (includeLinks && linkDocs.Count > 0 && linksSearched == 0)
    linkBlocked = "Links NOT read: the question was incomplete - see findings";

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
