// NOT STANDALONE. Assumes `doc`, `elements` and `embed` are in scope; leaves
// `typesChanged`, `alreadyThat`, `notCurtain`, `readBack`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// AUTOMATICALLY EMBED IS A TYPE'S BOX, AND WHAT IT DOES IS READ OFF THE WALLS.
// ===========================================================================
//
// A curtain wall drawn inside a basic wall cuts an opening in it when its type
// has Automatically Embed ticked (Type Properties > Construction). The box is
// the built-in `ALLOW_AUTO_EMBED`, held as 1 or 0 on the curtain wall TYPE,
// so ticking it changes every wall of that type - the answer names how many.
//
// WHETHER A WALL NOW CUTS ANOTHER IS ASKED OF THE OTHER WALL. Each basic wall
// whose box meets the curtain wall's is asked for what it holds embedded -
// `HostObject.FindInserts` with embedded walls included - before and after
// one regeneration. So the answer says, per curtain wall, which walls it cut
// before and which it cuts now. ASSUMPTION for the proof: whether ticking the
// box embeds a curtain wall ALREADY drawn, or only walls drawn after, has not
// been measured; the before-and-after says which on the model, and nothing is
// claimed beyond it.

var typesChanged = 0;
var alreadyThat = new List<string>();
var notCurtain = new List<string>();
var readBack = "";
var refused = "";
var findings = new List<string>();

Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

Func<ElementType, string> fullName = type =>
{
    if (type == null) return "(no type)";
    string family = "";
    try { family = type.FamilyName; } catch (Exception) { family = ""; }
    return string.IsNullOrEmpty(family) ? type.Name : family + ": " + type.Name;
};

// 1 ticked, 0 not.
var wanted = -1;
var said = plain(embed);
if (new[] { "yes", "true", "on", "1", "ticked", "embed" }.Contains(said)) wanted = 1;
else if (new[] { "no", "false", "off", "0", "unticked", "do not embed" }.Contains(said)) wanted = 0;
else
    refused = string.Format("'{0}' is not a setting for Automatically Embed. Say yes - the curtain wall cuts "
        + "the wall it is drawn in - or no. Nothing was changed", (embed ?? "").Trim());

var walls = new List<Wall>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    var asWall = element as Wall;
    WallKind kind = WallKind.Unknown;
    if (asWall != null)
    {
        try { kind = asWall.WallType.Kind; } catch (Exception) { kind = WallKind.Unknown; }
    }
    if (asWall != null && kind == WallKind.Curtain)
    {
        if (!walls.Any(w => w.Id.Equals(asWall.Id))) walls.Add(asWall);
        continue;
    }
    string named = "";
    try { named = element.Name; } catch (Exception) { named = ""; }
    notCurtain.Add(string.Format("'{0}' (id {1}) is {2} - left alone", named, element.Id,
        asWall == null ? "not a wall" : kind == WallKind.Basic ? "a basic wall, not a curtain wall"
            : "not a curtain wall"));
}

if (refused.Length == 0 && walls.Count == 0)
    refused = notCurtain.Count == 0
        ? "No curtain wall was given. Select the curtain wall whose type should embed, and ask again. Nothing "
            + "was changed"
        : string.Format("Nothing given is a curtain wall - {0}. Select the curtain wall, not the wall it sits "
            + "in. Nothing was changed", string.Join("; ", notCurtain.Take(3)));

// THE TYPES, each with its box - refused whole when one has none or will not
// take a value.
var types = new List<WallType>();
var boxes = new List<Parameter>();
if (refused.Length == 0)
{
    foreach (var wall in walls)
    {
        WallType type = null;
        try { type = wall.WallType; } catch (Exception) { type = null; }
        if (type == null || types.Any(t => t.Id.Equals(type.Id))) continue;
        Parameter box = null;
        try { box = type.get_Parameter(BuiltInParameter.ALLOW_AUTO_EMBED); } catch (Exception) { box = null; }
        if (box == null || box.StorageType != StorageType.Integer)
        {
            refused = string.Format("The type '{0}' has no Automatically Embed box Revit will give - nothing was "
                + "changed", fullName(type));
            break;
        }
        if (box.IsReadOnly)
        {
            refused = string.Format("Automatically Embed on '{0}' is read-only here - nothing was changed",
                fullName(type));
            break;
        }
        types.Add(type);
        boxes.Add(box);
    }
}

if (refused.Length == 0 && doc.IsWorkshared)
{
    var held = new List<string>();
    foreach (var type in types)
    {
        try
        {
            string owner;
            if (WorksharingUtils.GetCheckoutStatus(doc, type.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
                held.Add(string.Format("'{0}' by {1}", fullName(type), string.IsNullOrEmpty(owner) ? "another user" : owner));
        }
        catch (Exception) { }
    }
    if (held.Count > 0)
        refused = string.Format("In this shared model the type is held - {0}. They relinquish it, then ask "
            + "again. Nothing was changed", string.Join("; ", held));
}

// WHICH WALLS THIS CURTAIN WALL CUTS - asked of every other wall whose box
// meets its box.
Func<Wall, List<string>> cuts = curtain =>
{
    var found = new List<string>();
    BoundingBoxXYZ box = null;
    try { box = curtain.get_BoundingBox(null); } catch (Exception) { box = null; }
    if (box == null) return null;
    var filter = new BoundingBoxIntersectsFilter(new Outline(box.Min, box.Max));
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)).WherePasses(filter))
    {
        var other = element as Wall;
        if (other == null || other.Id.Equals(curtain.Id)) continue;
        IList<ElementId> inserts = null;
        try { inserts = other.FindInserts(false, false, true, false); } catch (Exception) { inserts = null; }
        if (inserts != null && inserts.Contains(curtain.Id))
        {
            string named = "";
            try { named = other.Name; } catch (Exception) { named = ""; }
            found.Add(string.Format("'{0}' (id {1})", named, other.Id));
        }
    }
    return found;
};
Func<List<string>, string> sayCuts = list =>
    list == null ? "not readable" : list.Count == 0 ? "no wall" : string.Join(", ", list);

if (refused.Length == 0)
{
    var before = walls.Select(w => cuts(w)).ToList();
    for (var i = 0; i < types.Count; i++)
    {
        int now;
        try { now = boxes[i].AsInteger(); } catch (Exception) { now = -1; }
        if (now == wanted)
        {
            alreadyThat.Add(string.Format("'{0}' already has Automatically Embed {1}", fullName(types[i]),
                wanted == 1 ? "ticked" : "off"));
            continue;
        }
        bool took;
        try { took = boxes[i].Set(wanted); }
        catch (Exception notTaken)
        {
            throw new InvalidOperationException(string.Format("Revit would not set Automatically Embed on '{0}' - "
                + "{1}. Nothing this call did is kept", fullName(types[i]), notTaken.Message));
        }
        if (!took)
            throw new InvalidOperationException(string.Format("Revit refused Automatically Embed on '{0}'. Nothing "
                + "this call did is kept", fullName(types[i])));
    }
    try { doc.Regenerate(); }
    catch (Exception notRebuilt)
    {
        throw new InvalidOperationException(string.Format("Revit could not rebuild the walls after the change - "
            + "{0}. Nothing this call did is kept", notRebuilt.Message));
    }

    var typeLines = new List<string>();
    for (var i = 0; i < types.Count; i++)
    {
        Parameter box = null;
        try { box = types[i].get_Parameter(BuiltInParameter.ALLOW_AUTO_EMBED); } catch (Exception) { box = null; }
        int after;
        try { after = box == null ? -1 : box.AsInteger(); } catch (Exception) { after = -1; }
        if (after != wanted)
            throw new InvalidOperationException(string.Format("Automatically Embed on '{0}' reads {1} after Revit "
                + "rebuilt the walls, not what was asked. Nothing this call did is kept", fullName(types[i]),
                after < 0 ? "nothing" : after.ToString()));
        if (!alreadyThat.Any(a => a.StartsWith("'" + fullName(types[i]) + "'", StringComparison.Ordinal))) typesChanged++;
        var ofType = 0;
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
        {
            ElementId its = null;
            try { its = element.GetTypeId(); } catch (Exception) { its = null; }
            if (its != null && its.Equals(types[i].Id)) ofType++;
        }
        typeLines.Add(string.Format("'{0}' now {1} - {2} wall(s) of this type in the model, every one of them "
            + "with it", fullName(types[i]), wanted == 1 ? "ticked" : "off", ofType));
    }
    var wallLines = new List<string>();
    for (var i = 0; i < walls.Count; i++)
        wallLines.Add(string.Format("curtain wall {0} cut {1} before, {2} now", walls[i].Id, sayCuts(before[i]),
            sayCuts(cuts(walls[i]))));
    readBack = string.Format("Automatically Embed, read back off each type: {0}. What each curtain wall cuts, asked "
        + "of the walls it meets: {1}", string.Join("; ", typeLines), string.Join("; ", wallLines));
    findings.Add(string.Format("Automatically Embed {0} on {1} type(s); {2} already that way", wanted == 1
        ? "ticked" : "turned off", typesChanged, alreadyThat.Count));
    findings.Add("A type's box changes every wall of that type - see readBack for how many");
    findings.Add(string.Join("; ", wallLines));
    if (notCurtain.Count > 0)
        findings.Add(string.Format("{0} other element(s) given and left alone - see notCurtain", notCurtain.Count));
    findings.Add("Read back from Revit - see readBack");
}
else
{
    findings.Add(refused);
}
