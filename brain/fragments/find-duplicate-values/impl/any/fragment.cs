// NOT STANDALONE. Assumes `doc`, `elements` and `parameterName` are in scope;
// leaves `findings`, `duplicated`, `blank`, `noSuchParameter` and `ambiguous`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THREE ANSWERS, NOT ONE, AND KEEPING THEM APART IS THE POINT:
//
//   duplicated       two or more elements carrying the same value
//   blank            the parameter is there and empty
//   noSuchParameter  the element does not carry that parameter at all
//
// Folding the last two together lets "none of these HAS a Mark parameter" read
// as "no duplicate marks found", which is a clean bill of health for a question
// that was never actually asked.
//
// A BLANK IS NEVER A DUPLICATE. On a real model most elements carry no Mark, so
// grouping the empties would report the whole model as one enormous clash and
// bury the four rows that matter.
//
// VALUES ARE COMPARED AS REVIT RENDERS THEM (AsValueString), because that is
// what a schedule prints and what somebody reads off the drawing. Comparing the
// raw internal double would split two identical-looking sizes over a rounding
// difference nobody can see.
//
// A NAME TWO PARAMETERS SHARE IS NOT READ (version 2; D-54 s3, FRAGMENT-ISSUES
// 5b-203). Element.LookupParameter hands back "the first one encountered" when
// two parameters on one element share the name, and Autodesk's reference says
// that match "is determined at random" - a shared or project parameter bound
// beside a built-in one of the same name does it. A duplicate found, or missed,
// on a value read that way is a coin toss that reads like a fact. So the name
// is counted on each element first, and an element carrying it twice goes to
// `ambiguous`: never read, never compared, never called unique.

var findings = new List<string>();
var duplicated = new List<ElementId>();
var blank = new List<ElementId>();
var noSuchParameter = new List<ElementId>();
var ambiguous = new List<ElementId>();

var seen = new Dictionary<string, List<ElementId>>();

foreach (var element in elements)
{
    if (element == null) continue;

    var sameName = element.GetParameters(parameterName).Count;
    if (sameName > 1) { ambiguous.Add(element.Id); continue; }

    var parameter = element.LookupParameter(parameterName);
    if (parameter == null) { noSuchParameter.Add(element.Id); continue; }

    string value = null;
    try
    {
        if (parameter.StorageType == StorageType.String) value = parameter.AsString();
        else if (parameter.StorageType == StorageType.ElementId)
        {
            var referenced = doc.GetElement(parameter.AsElementId());
            value = referenced == null ? null : referenced.Name;
        }
        else value = parameter.AsValueString();
    }
    catch { value = null; }

    if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
    {
        blank.Add(element.Id);
        continue;
    }

    var key = value.Trim();
    if (!seen.ContainsKey(key)) seen[key] = new List<ElementId>();
    seen[key].Add(element.Id);
}

foreach (var pair in seen)
{
    if (pair.Value.Count < 2) continue;
    foreach (var id in pair.Value) duplicated.Add(id);
    findings.Add(string.Format("'{0}' is on {1} elements - {2}",
        pair.Key, pair.Value.Count, string.Join(", ", pair.Value.Select(id => id.ToString()))));
}

// The two figures that are answers in their own right, not footnotes.
if (blank.Count > 0)
    findings.Add(string.Format("{0} element(s) have '{1}' EMPTY - not counted as duplicates, and worth "
        + "its own look", blank.Count, parameterName));

if (noSuchParameter.Count > 0)
    findings.Add(string.Format("{0} element(s) do not carry a parameter called '{1}' at all - nothing "
        + "was checked on those", noSuchParameter.Count, parameterName));

if (ambiguous.Count > 0)
    findings.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called '{1}' - NOT READ and not "
        + "compared, because Revit hands back whichever of them it meets first: {2}. Rename one of the "
        + "parameters, or check those by eye",
        ambiguous.Count, parameterName, string.Join(", ", ambiguous.Select(id => id.ToString()))));

if (findings.Count == 0)
    findings.Add(string.Format("Every '{0}' in the {1} element(s) checked is unique",
        parameterName, elements.Count));
