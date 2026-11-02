// NOT STANDALONE. Assumes `doc`, `name`, `nodeType` and `upstream` are in
// scope, and leaves `added`, `connected`, `before`, `after`, `skipped`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Revit 2023's electrical analytical model - the power
// distribution system drawn as nodes before any equipment is placed. Two
// halves, either or both in one run:
//   - ADD. With `nodeType` given and no node of that name, ONE node is made:
//     power source, bus, transformer, transfer switch or equipment load. A
//     name already present, in any capitals, is skipped and named - never
//     renamed or retyped.
//   - CONNECT. With `upstream` given, the node - or an area-based load - named
//     `name` is connected to the node named `upstream`, the one feeding it.
//     Already connected to it, skipped. Already fed from ANOTHER node, refused
//     and named: Heron never disconnects - a transfer switch, which takes two
//     sources, is the one exception, and Revit's own CanConnectToUpstreamNode
//     decides it.
//
// NAMES ARE MATCHED EXACTLY (D-33). A name the model has not got is refused
// with the nodes it has, each with its kind; one differing only in capitals is
// named, never taken.
//
// NEVER DELETES, NEVER DISCONNECTS.
//
// RELEASES. ElectricalAnalyticalNode (Create(Document,
// ElectricalAnalyticalNodeType, string), NodeType, TotalLoad,
// CanConnectToUpstreamNode, ConnectToUpstreamNode, GetUpstreamNodeIds,
// GetDownstreamNodeIds) and ElectricalAnalyticalNodeType are 2023 to 2027;
// Zone.GetDomainData and AreaBasedLoadData are 2023 to 2027, but
// AreaBasedLoadData.CanConnectToUpstreamNode, ConnectToUpstreamNode and
// GetUpstreamNodeId are 2024 on (#if REVIT2024_OR_GREATER) - in 2023 an
// area-based load is named and refused (tools/api-surface --members,
// 2026-11-02).
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var added = 0;
var connected = 0;
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> num = v => Math.Round(v, 2).ToString("0.##", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
Func<string, string> given = s =>
{
    var t = (s ?? "").Trim();
    return string.Equals(t, "none", StringComparison.OrdinalIgnoreCase) ? "" : t;
};
var wantName = given(name);
var wantKind = given(nodeType).ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
var wantUp = given(upstream);

Func<ElectricalAnalyticalNodeType, string> kindWords = k =>
    k == ElectricalAnalyticalNodeType.PowerSource ? "power source"
    : k == ElectricalAnalyticalNodeType.Bus ? "bus"
    : k == ElectricalAnalyticalNodeType.Transformer ? "transformer"
    : k == ElectricalAnalyticalNodeType.TransferSwitch ? "transfer switch" : "equipment load";

var nodes = new List<ElectricalAnalyticalNode>();
var loads = new List<Zone>();
Action readModel = () =>
{
    nodes.Clear(); loads.Clear();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ElectricalAnalyticalNode)))
        if (e is ElectricalAnalyticalNode n) nodes.Add(n);
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Zone)))
    {
        var z = e as Zone;
        if (z == null) continue;
        ZoneElementDomainData d = null;
        try { d = z.GetDomainData(); } catch (Exception) { }
        if (d is AreaBasedLoadData) loads.Add(z);
    }
    nodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    loads.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
};
Func<ElementId, string> nameOf = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var e = doc.GetElement(id);
    return e == null ? "none" : e.Name;
};
Func<ElectricalAnalyticalNode, string> nodeLine = n =>
{
    var ups = n.GetUpstreamNodeIds() ?? new List<ElementId>();
    var downs = n.GetDownstreamNodeIds() ?? new List<ElementId>();
    return quoted(n.Name) + " (" + kindWords(n.NodeType) + ", id " + n.Id.ToString() + "), fed from "
        + (ups.Count == 0 ? "nothing" : string.Join(", ", ups.Select(u => quoted(nameOf(u))))) + ", feeds " + downs.Count
        + ", total load " + num(UnitUtils.ConvertFromInternalUnits(n.TotalLoad, UnitTypeId.VoltAmperes)) + " VA";
};
Func<Zone, string> loadLine = z =>
{
    var up = "not read on this release";
#if REVIT2024_OR_GREATER
    var d = z.GetDomainData() as AreaBasedLoadData;
    up = d == null ? "nothing" : quoted(nameOf(d.GetUpstreamNodeId()));
#endif
    return quoted(z.Name) + " (area-based load, id " + z.Id.ToString() + "), fed from " + up;
};
Func<List<string>> listNow = () => nodes.Select(nodeLine).Concat(loads.Select(loadLine)).ToList();
Func<string> nodeList = () => nodes.Count == 0 ? "none"
    : string.Join(", ", nodes.Select(n => quoted(n.Name) + " (" + kindWords(n.NodeType) + ")"));

// ---- Refusals before anything is touched -----------------------------------
ElectricalAnalyticalNodeType? kind = null;
if (wantKind.Length > 0)
{
    if (wantKind == "powersource" || wantKind == "source" || wantKind == "utility") kind = ElectricalAnalyticalNodeType.PowerSource;
    else if (wantKind == "bus" || wantKind == "busbar") kind = ElectricalAnalyticalNodeType.Bus;
    else if (wantKind == "transformer") kind = ElectricalAnalyticalNodeType.Transformer;
    else if (wantKind == "transferswitch" || wantKind == "ats") kind = ElectricalAnalyticalNodeType.TransferSwitch;
    else if (wantKind == "equipmentload" || wantKind == "load") kind = ElectricalAnalyticalNodeType.EquipmentLoad;
    else refused.Add(quoted(nodeType) + " is not a kind of node - powerSource, bus, transformer, transferSwitch or equipmentLoad. Never guessed.");
}
if (doc.IsFamilyDocument) refused.Add("This is a family - the electrical analytical model lives in a project.");
if (wantName.Length == 0) refused.Add("No node was named - it is never made up.");
if (refused.Count == 0 && kind == null && wantUp.Length == 0)
    refused.Add("Neither a kind of node to add nor a node to connect " + quoted(wantName) + " to was given - nothing to do.");
var didRead = false;
if (refused.Count == 0) { readModel(); before.AddRange(listNow()); didRead = true; }

// ---- Add -------------------------------------------------------------------
ElectricalAnalyticalNode node = null;
Zone load = null;
if (refused.Count == 0)
{
    node = nodes.FirstOrDefault(n => string.Equals(n.Name, wantName, StringComparison.Ordinal));
    if (node == null) load = loads.FirstOrDefault(z => string.Equals(z.Name, wantName, StringComparison.Ordinal));
    var near = nodes.Select(n => n.Name).Concat(loads.Select(z => z.Name))
        .FirstOrDefault(n => string.Equals(n, wantName, StringComparison.OrdinalIgnoreCase));
    if (node != null && kind != null)
        skipped.Add("A node is already named " + quoted(node.Name) + ", a " + kindWords(node.NodeType)
            + (node.NodeType == kind.Value ? "" : " not a " + kindWords(kind.Value)) + " - left as it is, nothing added.");
    else if (load != null && kind != null)
        skipped.Add(quoted(load.Name) + " is already an area-based load - no node of that name is added beside it.");
    else if (node == null && load == null && near != null)
        refused.Add("Nothing is named " + quoted(wantName) + ". " + quoted(near) + " differs only in capitals - Heron does not take one for the other, nor makes a second.");
    else if (node == null && load == null && kind == null)
        refused.Add("No node or area-based load is named " + quoted(wantName) + " - give its kind to add it. The nodes here are: " + nodeList() + ".");
    else if (node == null && load == null)
    {
        try
        {
            node = ElectricalAnalyticalNode.Create(doc, kind.Value, wantName);
            if (node == null) refused.Add("Revit made no node named " + quoted(wantName) + ".");
            else { added++; findings.Add("Added " + nodeLine(node) + "."); }
        }
        catch (Exception ex) { refused.Add("Revit would not add " + kindWords(kind.Value) + " node " + quoted(wantName) + ": " + ex.Message); }
    }
}

// ---- Connect ---------------------------------------------------------------
if (refused.Count == 0 && wantUp.Length > 0 && (node != null || load != null))
{
    var up = nodes.FirstOrDefault(n => string.Equals(n.Name, wantUp, StringComparison.Ordinal));
    if (up == null)
    {
        var nearUp = nodes.FirstOrDefault(n => string.Equals(n.Name, wantUp, StringComparison.OrdinalIgnoreCase));
        refused.Add("No node is named " + quoted(wantUp) + ". "
            + (nearUp != null ? quoted(nearUp.Name) + " differs only in capitals - Heron does not take one for the other. " : "")
            + "The nodes here are: " + nodeList() + ".");
    }
    else if (node != null && up.Id == node.Id)
        refused.Add("A node cannot feed itself - " + quoted(wantName) + " was named on both ends.");
    else if (node != null)
    {
        var ups = node.GetUpstreamNodeIds() ?? new List<ElementId>();
        if (ups.Any(u => u == up.Id))
            skipped.Add(quoted(node.Name) + " is already fed from " + quoted(up.Name) + " - left as it is.");
        else if (ups.Count > 0 && node.NodeType != ElectricalAnalyticalNodeType.TransferSwitch)
            refused.Add(quoted(node.Name) + " is already fed from " + string.Join(", ", ups.Select(u => quoted(nameOf(u))))
                + " - Heron never disconnects; disconnect it in Revit first if " + quoted(up.Name) + " should feed it instead.");
        else if (!node.CanConnectToUpstreamNode(up.Id))
            refused.Add("Revit says " + quoted(up.Name) + " (" + kindWords(up.NodeType) + ") cannot feed " + quoted(node.Name)
                + " (" + kindWords(node.NodeType) + ").");
        else
        {
            try
            {
                node.ConnectToUpstreamNode(up.Id);
                if ((node.GetUpstreamNodeIds() ?? new List<ElementId>()).Any(u => u == up.Id))
                {
                    connected++;
                    findings.Add("Connected " + quoted(node.Name) + " to " + quoted(up.Name) + ", which now feeds it.");
                }
                else refused.Add("Revit did not keep " + quoted(node.Name) + " fed from " + quoted(up.Name) + ".");
            }
            catch (Exception ex) { refused.Add("Revit would not connect " + quoted(node.Name) + " to " + quoted(up.Name) + ": " + ex.Message); }
        }
    }
    else
    {
#if REVIT2024_OR_GREATER
        var data = load.GetDomainData() as AreaBasedLoadData;
        var was = data == null ? ElementId.InvalidElementId : data.GetUpstreamNodeId();
        if (data == null) refused.Add(quoted(load.Name) + " could not be read as an area-based load.");
        else if (was == up.Id) skipped.Add(quoted(load.Name) + " is already fed from " + quoted(up.Name) + " - left as it is.");
        else if (was != null && was != ElementId.InvalidElementId)
            refused.Add(quoted(load.Name) + " is already fed from " + quoted(nameOf(was)) + " - Heron never disconnects; disconnect it in Revit first.");
        else if (!data.CanConnectToUpstreamNode(up.Id))
            refused.Add("Revit says " + quoted(up.Name) + " (" + kindWords(up.NodeType) + ") cannot feed area-based load " + quoted(load.Name) + ".");
        else
        {
            try
            {
                data.ConnectToUpstreamNode(up.Id);
                if (data.GetUpstreamNodeId() == up.Id)
                {
                    connected++;
                    findings.Add("Connected area-based load " + quoted(load.Name) + " to " + quoted(up.Name) + ", which now feeds it.");
                }
                else refused.Add("Revit did not keep " + quoted(load.Name) + " fed from " + quoted(up.Name) + ".");
            }
            catch (Exception ex) { refused.Add("Revit would not connect " + quoted(load.Name) + " to " + quoted(up.Name) + ": " + ex.Message); }
        }
#else
        refused.Add(quoted(load.Name) + " is an area-based load, and Revit 2023 does not let Heron connect one - connect it in Revit itself, or on 2024 or later.");
#endif
    }
}

// ---- Read back -------------------------------------------------------------
if (didRead)
{
    try { readModel(); after.AddRange(listNow()); } catch (Exception ex) { findings.Add("The analytical model could not be read back: " + ex.Message); }
}
findings.Insert(0, "Nodes added " + added + ", connections made " + connected
    + (refused.Count > 0 ? "; refused" : skipped.Count > 0 ? "; some already here, skipped" : "")
    + ". Nodes and area-based loads: " + before.Count + " before, " + after.Count + " after.");
findings.AddRange(skipped);
findings.AddRange(refused);
