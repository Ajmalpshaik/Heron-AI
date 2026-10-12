// NOT STANDALONE. Assumes `doc` is in scope; leaves `viewportTypes`,
// `typeCount`, `unused` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY VIEWPORT TYPE - "Title w Line", "No Title" and the rest - with what
// its title shows and how many viewports on sheets use it. The type settings
// are read as Revit prints them: the title family, title shown or not, the
// extension line shown or not, the box shown or not.
//
// A TYPE NOTHING USES IS LISTED APART. Two near-identical title types where
// one is unused is the usual reason a set of sheets looks inconsistent.
//
// Viewport types are found by their family name, "Viewport", so no
// BuiltInCategory member is relied on.

var lines = new List<string>();
var unused = new List<string>();
var findings = new List<string>();

var useCount = new Dictionary<ElementId, int>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
{
    var typeId = element.GetTypeId();
    int n;
    useCount[typeId] = useCount.TryGetValue(typeId, out n) ? n + 1 : 1;
}

Func<Element, BuiltInParameter, string> shown = (e, which) =>
{
    try
    {
        var p = e.get_Parameter(which);
        if (p == null || !p.HasValue) return "";
        if (p.StorageType == StorageType.ElementId)
        {
            var target = doc.GetElement(p.AsElementId());
            return target == null ? "(none)" : target.Name;
        }
        return p.AsValueString() ?? "";
    }
    catch { return ""; }
};

var typeCount = 0;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ElementType)))
{
    var type = element as ElementType;
    if (type == null || !string.Equals(type.FamilyName, "Viewport", StringComparison.OrdinalIgnoreCase)) continue;
    typeCount++;
    int uses;
    useCount.TryGetValue(type.Id, out uses);
    lines.Add(type.Name + " | title " + shown(type, BuiltInParameter.VIEWPORT_ATTR_LABEL_TAG)
        + " | show title " + shown(type, BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL)
        + " | extension line " + shown(type, BuiltInParameter.VIEWPORT_ATTR_SHOW_EXTENSION_LINE)
        + " | box " + shown(type, BuiltInParameter.VIEWPORT_ATTR_SHOW_BOX)
        + " | used by " + uses + " viewport(s)");
    if (uses == 0) unused.Add(type.Name);
}

var viewportTypes = string.Join("\n", lines);
findings.Add(typeCount + " viewport type(s); " + unused.Count + " used by no viewport"
    + (unused.Count == 0 ? "." : ": " + string.Join(", ", unused) + "."));
