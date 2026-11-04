// NOT STANDALONE. Assumes `doc`, `elements` and `show` are in scope, and
// leaves `connectionCount`, `typeCount`, `report`, `names`, `notConnected` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. Revit's structural connections - Steel > Connection (Structure
// > Connection on 2020): every connection element with its type, the elements
// it joins in Revit's own order (the first is the primary one - RevitAPI.xml,
// StructuralConnectionHandler.Create), its origin, its approval type and its
// code checking status; and every connection type loaded, whether it is
// generic, detailed or custom, and how many connections use it; and the
// approval types. Every list, or only the one `show` names (connections;
// types). The connections narrowed to the elements handed in: a connection
// selected is listed itself, a beam or column selected lists every connection
// that joins it.
//
// PLATES, BOLTS, WELDS AND CUTS ARE NOT READ. A detailed connection's
// fabrication parts live behind the Advance Steel API, which Heron does not
// load; Revit's API gives the connection element and its type, and that is
// what is read here. Said once in findings whenever a connection is listed.
//
// RELEASES (tools/api-surface --members, 2026-11-01). StructuralConnectionHandler
// GetConnectedElementIds, GetOrigin, ApprovalTypeId, CodeCheckingStatus,
// IsDetailed, IsCustom and SingleElementEndIndex; StructuralConnectionHandlerType
// IsGeneric, IsDetailed, IsCustom and FamilyName;
// StructuralConnectionApprovalType.GetAllStructuralConnectionApprovalTypes and
// StructuralConnectionCodeCheckingStatus are the same 2020 to 2027. Neither
// class is in RevitAPI.xml's list of classes ElementClassFilter cannot find, so
// OfClass collects both.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var connectionCount = 0;
var typeCount = 0;
var report = "";
var names = new List<string>();
var notConnected = new List<ElementId>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<XYZ, string> at = p => p == null ? "?" : "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm";

// ---- Which lists -----------------------------------------------------------
var asked = (show ?? "").Trim().ToLowerInvariant();
var every = asked.Length == 0 || asked == "all";
bool wantConnections = every, wantTypes = every;
var unknownWords = new List<string>();
if (!every)
{
    foreach (var raw in asked.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var w = raw.Trim().Replace(" ", "").Replace("-", "");
        if (w.Length == 0) continue;
        if (w == "all") { wantConnections = wantTypes = true; }
        else if (w.StartsWith("type") || w.StartsWith("connectiontype")) wantTypes = true;
        else if (w.StartsWith("connection")) wantConnections = true;
        else unknownWords.Add(raw.Trim());
    }
}
if (unknownWords.Count > 0)
    findings.Add("\"" + string.Join("\", \"", unknownWords) + "\" is not one of the lists - connections or "
        + "types. Nothing was guessed for it. Plates, bolts, welds and cuts of a detailed connection are not "
        + "read by Revit's API at all - they need the Advance Steel API.");

Func<ElementId, string> describe = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "nothing";
    var e = doc.GetElement(id);
    if (e == null) return "element " + id + " (not in this model)";
    string cat = "", name = "";
    try { cat = e.Category == null ? "" : e.Category.Name + " "; } catch (Exception) { }
    try { name = e.Name ?? ""; } catch (Exception) { }
    return cat + "\"" + name + "\" (" + id + ")";
};

Func<Element, string> typeLabel = t =>
{
    if (t == null) return "no type";
    string name = "", family = "";
    try { name = t.Name ?? ""; } catch (Exception) { }
    try { family = ((ElementType)t).FamilyName ?? ""; } catch (Exception) { }
    return family.Length > 0 ? family + ": " + name : name;
};

// ---- Everything Revit holds ------------------------------------------------
var connections = new FilteredElementCollector(doc).OfClass(typeof(StructuralConnectionHandler))
    .Cast<StructuralConnectionHandler>().ToList();
var types = new FilteredElementCollector(doc).OfClass(typeof(StructuralConnectionHandlerType))
    .Cast<StructuralConnectionHandlerType>()
    .OrderBy(t => typeLabel(t), StringComparer.OrdinalIgnoreCase).ToList();
typeCount = types.Count;
foreach (var t in types)
{
    string name = "";
    try { name = t.Name ?? ""; } catch (Exception) { }
    names.Add("\"" + name + "\"");
}

var joined = new Dictionary<ElementId, IList<ElementId>>();
foreach (var c in connections)
{
    IList<ElementId> ids = null;
    try { ids = c.GetConnectedElementIds(); } catch (Exception) { ids = null; }
    joined[c.Id] = ids ?? new List<ElementId>();
}

// ---- The elements handed in --------------------------------------------------
var selected = (elements ?? new List<Element>()).Where(e => e != null).ToList();
var narrowed = selected.Count > 0;
var wanted = new HashSet<ElementId>();
var members = 0;
foreach (var e in selected)
{
    if (e is StructuralConnectionHandler) { wanted.Add(e.Id); continue; }
    members++;
    var any = false;
    foreach (var c in connections)
        if (joined[c.Id].Any(id => id == e.Id)) { wanted.Add(c.Id); any = true; }
    if (!any) notConnected.Add(e.Id);
}
if (notConnected.Count > 0)
    findings.Add(notConnected.Count + " of the " + members + " member(s) handed in "
        + (notConnected.Count == 1 ? "is" : "are") + " joined by no connection: "
        + string.Join(", ", notConnected.Select(describe)) + ".");

var lines = new List<string>();

// ---- Connections -----------------------------------------------------------
if (wantConnections)
{
    var shown = connections.Where(c => !narrowed || wanted.Contains(c.Id))
        .OrderBy(c => typeLabel(doc.GetElement(c.GetTypeId())), StringComparer.OrdinalIgnoreCase).ToList();
    connectionCount = shown.Count;
    lines.Add("Connections (" + shown.Count + ")" + (narrowed ? " for the " + selected.Count + " element(s) handed in" : "")
        + ":" + (shown.Count == 0 ? " none" : ""));
    foreach (var c in shown)
    {
        var ids = joined[c.Id];
        var parts = new List<string>();
        for (var i = 0; i < ids.Count; i++)
            parts.Add(describe(ids[i]) + (i == 0 && ids.Count > 1 ? " (primary)" : ""));
        var where = "?";
        try { where = at(c.GetOrigin()); } catch (Exception) { }
        var approval = "none";
        try
        {
            var aid = c.ApprovalTypeId;
            if (aid != null && aid != ElementId.InvalidElementId)
            {
                var a = doc.GetElement(aid);
                approval = a == null ? "element " + aid : "\"" + a.Name + "\"";
            }
        }
        catch (Exception) { approval = "?"; }
        var status = "?";
        try
        {
            var s = c.CodeCheckingStatus;
            status = s == StructuralConnectionCodeCheckingStatus.OkChecked ? "checked OK"
                : s == StructuralConnectionCodeCheckingStatus.CheckingFailed ? "checking FAILED"
                : "not calculated";
        }
        catch (Exception) { }
        var style = new List<string>();
        try { if (c.IsDetailed()) style.Add("detailed"); } catch (Exception) { }
        try { if (c.IsCustom()) style.Add("custom"); } catch (Exception) { }
        var end = "";
        if (ids.Count == 1)
        {
            try { end = ", at the member's " + (c.SingleElementEndIndex == 0 ? "start" : "end") + " (end index "
                + c.SingleElementEndIndex + ")"; }
            catch (Exception) { }
        }
        lines.Add("  " + c.Id + "  type \"" + typeLabel(doc.GetElement(c.GetTypeId())) + "\""
            + (style.Count == 0 ? "" : " (" + string.Join(", ", style) + ")")
            + " - joins " + ids.Count + ": " + (parts.Count == 0 ? "nothing Revit reports" : string.Join(", ", parts))
            + end + "; origin " + where + "; approval " + approval + "; code checking " + status);
    }
}

// ---- Connection types --------------------------------------------------------
if (wantTypes)
{
    lines.Add("Connection types loaded (" + types.Count + "):" + (types.Count == 0
        ? " none - this model has no connection type. Steel > Connection > Connection Settings loads them"
        : ""));
    foreach (var t in types)
    {
        var kind = new List<string>();
        try { if (t.IsGeneric()) kind.Add("generic"); } catch (Exception) { }
        try { if (t.IsDetailed()) kind.Add("detailed"); } catch (Exception) { }
        try { if (t.IsCustom()) kind.Add("custom"); } catch (Exception) { }
        var used = connections.Count(c => c.GetTypeId() == t.Id);
        lines.Add("  \"" + typeLabel(t) + "\"" + (kind.Count == 0 ? "" : " - " + string.Join(", ", kind))
            + "; used by " + used + " connection(s)");
    }
    ICollection<ElementId> approvalIds = null;
    try { StructuralConnectionApprovalType.GetAllStructuralConnectionApprovalTypes(doc, out approvalIds); }
    catch (Exception) { approvalIds = null; }
    var approvals = (approvalIds ?? new List<ElementId>()).Select(id => doc.GetElement(id))
        .Where(a => a != null).Select(a => "\"" + a.Name + "\"").ToList();
    lines.Add("Approval types (" + approvals.Count + "): " + (approvals.Count == 0 ? "none" : string.Join(", ", approvals)));
}

report = string.Join("\n", lines);
if (wantConnections && connectionCount > 0)
    findings.Add("Each connection is the Revit connection element and its type. Its plates, bolts, welds and cuts "
        + "are not read - Revit's API does not reach them; the Advance Steel API does.");
if (narrowed && wantConnections && connectionCount == 0 && notConnected.Count == 0)
    findings.Add("No connection joins what was handed in.");
findings.Add("Read only - nothing in the model changed.");
