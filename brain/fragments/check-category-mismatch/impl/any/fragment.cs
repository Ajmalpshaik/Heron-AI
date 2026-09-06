// NOT STANDALONE. Assumes `doc`, `nameKeywords` and `expectedCategory` are in
// scope, and leaves `elements`, `foundIn`, `correctlyCategorised` and
// `scanned` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE NAME CARRIES THE INTENT; THE CATEGORY WAS PICKED FROM A LIST.
//
// Somebody named the family for what the thing IS. The category is a choice
// made when the family was built, sometimes wrongly and sometimes because the
// right one was not available at the time. So the name is the evidence and the
// category is what is being tested against it - which is also why the keywords
// have to come from the caller: every job names its kit differently, and a
// built-in word list would be one office's vocabulary applied to everybody
// else's model.
//
// BOTH NAMES ARE READ.
//
// Family name and type name. A family named neutrally often has the meaning in
// its type name, and reading only one halves what this finds.
//
// WHERE THEY ARE IS PART OF THE ANSWER.
//
// Eleven in the wrong category is half a finding. Seven in generic models and
// four in casework says whether this was one family made wrongly or several
// unrelated mistakes - which decides whether the fix is one family or a
// morning.
//
// A WHOLE-MODEL WALK, ON PURPOSE. The elements being looked for are by
// definition not in the category anybody would filter by first.

var elements = new List<Element>();
var foundIn = new Dictionary<string, int>();
int correctlyCategorised = 0;
int scanned = 0;

var expectedId = new ElementId(expectedCategory);

Func<string, bool> saysSo = name =>
{
    if (string.IsNullOrEmpty(name) || nameKeywords == null) return false;
    foreach (var keyword in nameKeywords)
    {
        if (string.IsNullOrEmpty(keyword)) continue;
        if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
    }
    return false;
};

foreach (var element in new FilteredElementCollector(doc)
             .OfClass(typeof(FamilyInstance))
             .WhereElementIsNotElementType())
{
    var instance = element as FamilyInstance;
    if (instance == null) continue;
    scanned++;

    string familyName = null, typeName = null;
    try
    {
        if (instance.Symbol != null)
        {
            typeName = instance.Symbol.Name;
            if (instance.Symbol.Family != null) familyName = instance.Symbol.Family.Name;
        }
    }
    catch { }

    if (!saysSo(familyName) && !saysSo(typeName)) continue;

    ElementId actual = ElementId.InvalidElementId;
    string actualName = "(no category)";
    try
    {
        if (instance.Category != null)
        {
            actual = instance.Category.Id;
            actualName = instance.Category.Name ?? actualName;
        }
    }
    catch { }

    if (actual == expectedId) { correctlyCategorised++; continue; }

    elements.Add(instance);
    if (foundIn.ContainsKey(actualName)) foundIn[actualName] = foundIn[actualName] + 1;
    else foundIn[actualName] = 1;
}
