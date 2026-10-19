// NOT STANDALONE. Assumes `doc`, `elements`, `coverTop`, `coverBottom`,
// `coverOther`, `coverExterior` and `coverInterior` are in scope; leaves
// `changedIds`, `facesChanged`, `alreadyAsAsked`, `skipped`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// Each face by Revit's own parameter id. A value is a cover type's exact name
// or a distance in millimetres matched to the ONE cover type that gives it.
// "keep" (an input left out) leaves that face alone.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);

var changedIds = new List<ElementId>();
var facesChanged = 0;
var alreadyAsAsked = 0;
var skipped = new List<string>();
var findings = new List<string>();
var refused = "";
var lines = new List<string>();

var covers = new FilteredElementCollector(doc).OfClass(typeof(RebarCoverType)).Cast<RebarCoverType>()
    .OrderBy(c => c.CoverDistance).ToList();
var loaded = covers.Count == 0 ? "none" : string.Join(", ", covers.Select(c => "'" + c.Name + "' " + mm(c.CoverDistance) + " mm"));

// One cover type from a name or a distance; null with `refused` set.
Func<string, string, RebarCoverType> coverFrom = (text, face) =>
{
    var asked = (text ?? "").Trim();
    var named = covers.FirstOrDefault(c => string.Equals(c.Name, asked, StringComparison.Ordinal));
    if (named != null) return named;
    var number = asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase) ? asked.Substring(0, asked.Length - 2).Trim() : asked;
    double value;
    if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariant, out value) || value < 0)
    {
        refused = "No Rebar Cover Type is named '" + asked + "' (the " + face + " cover asked), and it is not a "
            + "distance in millimetres. Loaded: " + loaded + ". Nothing was changed";
        return null;
    }
    var matching = covers.Where(c => Math.Abs(c.CoverDistance * 304.8 - value) < 0.05).ToList();
    if (matching.Count == 1) return matching[0];
    if (matching.Count > 1)
        refused = matching.Count + " cover types give " + value.ToString("0.#", invariant) + " mm: "
            + string.Join(", ", matching.Select(c => "'" + c.Name + "'")) + ". Say which, by its name. Nothing was changed";
    else
        refused = "No Rebar Cover Type gives " + value.ToString("0.#", invariant) + " mm. Loaded: " + loaded
            + ". CREATE_REBAR_COVER_TYPE makes one, with a name you choose. Nothing was changed";
    return null;
};

// The faces asked: Revit's parameter id, the word, and the cover type.
var asks = new List<Tuple<BuiltInParameter, string, RebarCoverType>>();
var wanted = new[]
{
    Tuple.Create(BuiltInParameter.CLEAR_COVER_TOP, "top", coverTop),
    Tuple.Create(BuiltInParameter.CLEAR_COVER_BOTTOM, "bottom", coverBottom),
    Tuple.Create(BuiltInParameter.CLEAR_COVER_OTHER, "other faces", coverOther),
    Tuple.Create(BuiltInParameter.CLEAR_COVER_EXTERIOR, "exterior", coverExterior),
    Tuple.Create(BuiltInParameter.CLEAR_COVER_INTERIOR, "interior", coverInterior),
};
foreach (var ask in wanted)
{
    var text = (ask.Item3 ?? "").Trim();
    if (text.Length == 0 || text.Equals("keep", StringComparison.OrdinalIgnoreCase)) continue;
    var cover = coverFrom(text, ask.Item2);
    if (cover == null) break;
    asks.Add(Tuple.Create(ask.Item1, ask.Item2, cover));
}
if (refused.Length == 0 && asks.Count == 0)
    refused = "No cover was asked for - give at least one of top, bottom, other faces, exterior or interior. Nothing was changed";

if (refused.Length == 0)
{
    foreach (var host in elements)
    {
        if (host == null) continue;
        var name = (host.Category == null ? "element" : host.Category.Name) + " '" + host.Name + "' (id " + host.Id + ")";
        if (!RebarHostData.IsValidHost(host))
        {
            skipped.Add(name + ": not a rebar host - nothing set");
            continue;
        }
        var parts = new List<string>();
        var written = new List<Tuple<BuiltInParameter, ElementId>>();
        foreach (var ask in asks)
        {
            Parameter p = null;
            try { p = host.get_Parameter(ask.Item1); } catch (Exception) { }
            if (p == null || p.StorageType != StorageType.ElementId)
            {
                skipped.Add(name + ": has no " + ask.Item2 + " cover - "
                    + (host is Wall ? "a wall has exterior, interior and other faces" : "it has top, bottom and other faces"));
                continue;
            }
            if (p.IsReadOnly)
            {
                skipped.Add(name + ": its " + p.Definition.Name + " is read-only here");
                continue;
            }
            var before = doc.GetElement(p.AsElementId()) as RebarCoverType;
            if (before != null && before.Id == ask.Item3.Id)
            {
                alreadyAsAsked++;
                parts.Add(p.Definition.Name + " already '" + before.Name + "'");
                continue;
            }
            try
            {
                p.Set(ask.Item3.Id);
            }
            catch (Exception ex)
            {
                skipped.Add(name + ": Revit refused " + p.Definition.Name + " - " + ex.Message);
                continue;
            }
            written.Add(Tuple.Create(ask.Item1, ask.Item3.Id));
            parts.Add(p.Definition.Name + " " + (before == null ? "none" : "'" + before.Name + "'") + " -> '" + ask.Item3.Name + "' " + mm(ask.Item3.CoverDistance) + " mm");
        }
        doc.Regenerate();

        // READ BACK - every face written, as it reads now. Only a face that
        // reads the asked type counts; one that does not is named.
        var changedHere = false;
        foreach (var write in written)
        {
            var p = host.get_Parameter(write.Item1);
            var now = p == null ? ElementId.InvalidElementId : p.AsElementId();
            if (now == write.Item2) { facesChanged++; changedHere = true; }
            else findings.Add(name + ": " + (p == null ? write.Item1.ToString() : p.Definition.Name) + " reads back as "
                + (doc.GetElement(now) == null ? "none" : "'" + doc.GetElement(now).Name + "'") + ", not '" + doc.GetElement(write.Item2).Name + "'.");
        }
        var inHost = RebarHostData.GetRebarHostData(host).GetRebarsInHost().Count;
        if (inHost > 0) parts.Add(inHost + " rebar set(s) in it - bars tied to the cover move with it");
        if (changedHere) changedIds.Add(host.Id);
        lines.Add(name + ": " + (parts.Count == 0 ? "nothing set" : string.Join("; ", parts)));
    }
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(facesChanged + " face(s) changed and read back on " + changedIds.Count + " host(s); "
        + alreadyAsAsked + " already as asked; " + skipped.Count + " skipped.");
    findings.AddRange(skipped);
}
