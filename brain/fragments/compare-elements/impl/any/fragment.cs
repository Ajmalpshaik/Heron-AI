// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `differing`, `identicalCount`, `comparedCount` and `tooFewToCompare` behind.
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

var differing = new Dictionary<string, IList<string>>();
int identicalCount = 0;

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
                try { found = source.LookupParameter(lookFor); } catch { }
            }
            row.Add(readValue(found));
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
