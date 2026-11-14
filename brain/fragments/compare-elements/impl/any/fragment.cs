// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `differing`, `identicalCount`, `comparedCount`, `tooFewToCompare`,
// `ambiguous` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE SUPPRESSION IS THE FEATURE.
//
// A real element carries fifty to eighty parameters, and on two elements of the
// same type nearly all of them match. Returning all of them and leaving a
// person to find the three that differ is the work they came here to avoid.
// Only the differences come back - and `identicalCount` says how many matched,
// so "they are the same" is something this fragment SAYS rather than something
// an empty result might mean.
//
// COMPARED AS REVIT DISPLAYS THEM, NOT AS DOUBLES.
//
// Two ducts both drawn at 2500 mm can hold internal feet differing in the
// twelfth decimal place. As numbers they differ; as the text Revit shows, they
// match. Matching is the true answer - nothing in the model behaves differently
// because of that noise. The cost: a real difference finer than the project's
// display rounding cannot be seen here. That is a stated limit, not a hidden
// one.
//
// A PARAMETER PRESENT ON ONE SIDE ONLY IS A DIFFERENCE.
//
// Comparing a wall against a duct, most parameters exist on one side only.
// Skipping them for not being comparable throws away the most useful finding -
// "that one has no System Name at all" is usually what was being asked.
//
// TYPE PARAMETERS COUNT.
//
// If two elements differ only in their type, EVERY difference lives on the
// type. Comparing instance parameters alone reports them identical, which is a
// confident wrong answer to the exact question asked. Type parameters are
// gathered too and their names carry a marker so it stays clear where a
// difference has to be fixed.
//
// A NAME TWO PARAMETERS SHARE IS NOT READ (version 2; D-54 s3, FRAGMENT-ISSUES
// 5b-203). The names are gathered off each element's own parameter list, so a
// name that sits there twice - a shared or project parameter bound beside a
// built-in one, a curtain wall type's two "Interior Type" - is asked for once
// and LookupParameter hands back "the first one encountered", which Autodesk's
// reference says "is determined at random". Compared that way the row could
// read identical, or different, on a coin toss. So each name is counted on
// each element first, and a name carried twice on any of them is neither
// identical nor differing: the elements go to `ambiguous` and `findings` says
// which name, in the words every reader of row 5b-203 uses. AND IT FAILS
// CLOSED: where Revit will not say how many parameters carry the name, none
// is picked either - the row is said as not read, never looked up anyway.
//
// EXCEPT WHERE EVERY ONE OF THEM IS BUILT-IN AND THEY READ THE SAME (version
// 3, D-164, FRAGMENT-ISSUES 5b-1631). Revit gives every element two built-in
// parameters named "Category" - ELEM_CATEGORY_PARAM and ELEM_CATEGORY_PARAM_MT
// - so version 2 said "'Category' - NOT READ" on every element in every model
// (Revit 2024, 2026-11-13), while Category itself was never compared. Two
// parameters of one name that are BOTH Revit's own and give the SAME value are
// one reading, not a doubt: whichever Revit meets first, the answer is the
// same. That value is compared as any other. A shared or project parameter
// among them, or built-ins that read differently, stays NOT READ exactly as
// version 2 says it. The reading is its own step, `builtInTwinsAgree`, asked
// before the refusal and only of a name counted twice; the refusal itself
// fills in no value (tests/test_same_name_guard.py checks 5 and 7).

var differing = new Dictionary<string, IList<string>>();
int identicalCount = 0;
var ambiguous = new List<ElementId>();
var findings = new List<string>();

// Eight columns is where a side-by-side stops being readable. The rest are not
// dropped silently - comparedCount says how many were actually looked at, so
// the gap against elements.Count is visible.
var comparing = elements.Take(8).ToList();
int comparedCount = comparing.Count;
bool tooFewToCompare = comparedCount < 2;

const string Absent = "(not on this element)";
const string Empty = "(empty)";
const string TypeMarker = " [type]";

Func<Parameter, string> readValue = p =>
{
    if (p == null) return Absent;
    if (!p.HasValue) return Empty;

    string text = null;
    try { text = p.AsValueString(); } catch { }

    if (string.IsNullOrEmpty(text))
    {
        try
        {
            if (p.StorageType == StorageType.String) text = p.AsString();
            else if (p.StorageType == StorageType.Integer) text = p.AsInteger().ToString();
            else if (p.StorageType == StorageType.Double) text = p.AsDouble().ToString("F4");
            else if (p.StorageType == StorageType.ElementId)
            {
                // Resolved to a NAME, never to the id's number. That keeps this
                // clear of the 2024 64-bit ElementId change, and a name is what
                // a person reads anyway - two elements pointing at the same
                // level should not read as different because their ids differ.
                var target = doc.GetElement(p.AsElementId());
                text = target == null ? Empty : target.Name;
            }
        }
        catch { }
    }

    return string.IsNullOrEmpty(text) ? Empty : text;
};

// D-164: THE ONE READING OF A NAME EVERY CARRIER OF WHICH IS BUILT-IN AND ALL
// OF WHICH READ THE SAME - null otherwise, and on anything Revit refuses, so
// the name is refused as D-54 item 3 says. A shared or project parameter is an
// InternalDefinition too, with BuiltInParameter INVALID, so one among them
// keeps the refusal.
Func<Element, string, string> builtInTwinsAgree = (holder, name) =>
{
    try
    {
        var carrying = holder.GetParameters(name);
        if (carrying == null || carrying.Count < 2) return null;
        foreach (var q in carrying)
        {
            var definition = q == null ? null : q.Definition as InternalDefinition;
            if (definition == null || definition.BuiltInParameter == BuiltInParameter.INVALID) return null;
        }
        var readings = carrying.Select(q => readValue(q)).Distinct(StringComparer.Ordinal).ToList();
        return readings.Count == 1 ? readings[0] : null;
    }
    catch { return null; }
};

if (!tooFewToCompare)
{
    // Every parameter name seen on ANY of them, so a parameter that only one
    // side has still gets a row. That row is the most useful one in the table.
    var names = new List<string>();
    var seen = new HashSet<string>();

    Action<Element, bool> collectNames = (element, fromType) =>
    {
        if (element == null) return;
        foreach (Parameter p in element.Parameters)
        {
            if (p == null || p.Definition == null) continue;
            string name = p.Definition.Name + (fromType ? TypeMarker : "");
            if (seen.Add(name)) names.Add(name);
        }
    };

    foreach (var element in comparing)
    {
        collectNames(element, false);
        Element elementType = null;
        try { elementType = doc.GetElement(element.GetTypeId()); } catch { }
        collectNames(elementType, true);
    }

    foreach (var name in names)
    {
        bool fromType = name.EndsWith(TypeMarker);
        string lookFor = fromType ? name.Substring(0, name.Length - TypeMarker.Length) : name;

        var row = new List<string>();
        // The elements this name sits on twice, and the ones Revit would not
        // count it on (5b-203).
        var twiceOn = new List<ElementId>();
        var uncountedOn = new List<ElementId>();
        foreach (var element in comparing)
        {
            Element source = element;
            if (fromType)
            {
                source = null;
                try { source = doc.GetElement(element.GetTypeId()); } catch { }
            }

            Parameter found = null;
            if (source != null)
            {
                var sameName = -1;
                try { sameName = source.GetParameters(lookFor).Count; } catch { }

                // BUILT-IN TWINS THAT AGREE ARE ONE READING (D-164, version 3,
                // 5b-1631) - asked only of a name counted twice, before the
                // refusal below, which is unchanged. null for anything else.
                var agreed = sameName > 1 ? builtInTwinsAgree(source, lookFor) : null;
                if (agreed != null) { row.Add(agreed); continue; }

                if (sameName > 1) { twiceOn.Add(element.Id); continue; }
                if (sameName < 0) { uncountedOn.Add(element.Id); continue; }

                try { found = source.LookupParameter(lookFor); } catch { }
            }
            row.Add(readValue(found));
        }

        if (twiceOn.Count > 0)
        {
            foreach (var id in twiceOn) if (!ambiguous.Contains(id)) ambiguous.Add(id);
            findings.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}' - NOT READ, "
                + "because Revit hands back whichever of them it meets first: {2}. '{1}' is counted neither as "
                + "identical nor as differing; compare it in Properties",
                twiceOn.Count, name, string.Join(", ", twiceOn.Select(id => id.ToString()))));
            continue;
        }

        if (uncountedOn.Count > 0)
        {
            findings.Add(string.Format("'{0}' was NOT READ on {1}: Revit would not say how many parameters "
                + "carry that name there, so none was picked. It is counted neither as identical nor as "
                + "differing", name, string.Join(", ", uncountedOn.Select(id => id.ToString()))));
            continue;
        }

        bool allSame = true;
        for (int i = 1; i < row.Count; i++)
        {
            if (!string.Equals(row[i], row[0], StringComparison.Ordinal)) { allSame = false; break; }
        }

        if (allSame) identicalCount++;
        else differing[name] = row;
    }
}
