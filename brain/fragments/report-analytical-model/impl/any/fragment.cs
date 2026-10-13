// NOT STANDALONE. Assumes `doc` is in scope; leaves `analytical`,
// `memberCount`, `panelCount`, `unassociated` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE STRUCTURAL ANALYTICAL MODEL, FOR HANDING TO ANALYSIS: every analytical
// member with its end points in millimetres and the physical element it
// belongs to, every analytical panel with its physical element, and the
// analytical elements tied to no physical one - an analytical model drawn on
// its own, or left behind when the physical element was deleted.
//
// REVIT 2023 made the analytical model separate elements with an association
// manager; that is the API read here, so the card declares 2023 on.

const double MmPerFoot = 304.8;
var lines = new List<string>();
var unassociated = new List<ElementId>();
var findings = new List<string>();
var memberCount = 0;
var panelCount = 0;

var manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc);
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(System.Globalization.CultureInfo.InvariantCulture);
Func<ElementId, string> physical = id =>
{
    ElementId other = ElementId.InvalidElementId;
    try { other = manager == null ? ElementId.InvalidElementId : manager.GetAssociatedElementId(id); } catch { }
    if (other == null || other == ElementId.InvalidElementId) { unassociated.Add(id); return "NO PHYSICAL ELEMENT"; }
    var e = doc.GetElement(other);
    return e == null ? other.ToString() : (e.Category == null ? "element" : e.Category.Name) + " " + e.Id + " (" + e.Name + ")";
};

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(AnalyticalMember)))
{
    memberCount++;
    var member = element as AnalyticalMember;
    var ends = "";
    try
    {
        var curve = member.GetCurve();
        var a = curve.GetEndPoint(0);
        var b = curve.GetEndPoint(1);
        ends = mm(a.X) + "," + mm(a.Y) + "," + mm(a.Z) + " to " + mm(b.X) + "," + mm(b.Y) + "," + mm(b.Z) + " mm";
    }
    catch { ends = "(curve not read)"; }
    lines.Add("member " + member.Id + " | " + ends + " | " + physical(member.Id));
}
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(AnalyticalPanel)))
{
    panelCount++;
    lines.Add("panel " + element.Id + " | " + physical(element.Id));
}

var analytical = string.Join("\n", lines);
findings.Add(memberCount + " analytical member(s), " + panelCount + " analytical panel(s); " + unassociated.Count
    + " tied to no physical element.");
