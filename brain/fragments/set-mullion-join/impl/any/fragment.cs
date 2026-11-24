// NOT STANDALONE. Assumes `doc`, `elements` and `join` are in scope; leaves
// `changed`, `unchanged`, `notMullion`, `readBack`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit keeps lengths in feet;
// every length read back is turned into millimetres once, to be said.
//
// ===========================================================================
// REVIT'S MAKE CONTINUOUS AND BREAK AT JOIN, TYPED.
// ===========================================================================
//
// Where two mullions cross, one runs through and the other stops at its face.
// `Mullion.JoinMullion()` is Make Continuous - this mullion runs through the
// crossing - and `Mullion.BreakMullion()` is Break at Join - it stops there.
//
// REVIT SAYS NOTHING BACK, SO THE MULLION IS MEASURED. Neither call returns
// anything, and a mullion has no "is continuous" to read. So each mullion's
// length ALONG ITS OWN RUN is measured off its solid, before and after one
// regeneration: made continuous it grows by the crossing mullions' width,
// broken it shrinks. One that did not move the way asked is in `unchanged`,
// with both lengths, and never counted - it may already have been that way,
// it may cross nothing, or the wall type's Join Condition may decide it.
// ASSUMPTION for the proof: that the length of a mullion's solid along its run
// tells continuous from broken; a run on a model settles it.

const double MillimetresPerFoot = 304.8;

// A length change smaller than this is no change.
const double ChangedMm = 1.0;

// The most mullions one call takes.
const int MostMullions = 500;

var changed = 0;
var unchanged = new List<string>();
var notMullion = new List<string>();
var readBack = "";
var refused = "";
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

Func<double, string> sayMm = value => value.ToString("0.#", invariant);

Func<Element, string> nameOf = element =>
{
    string named = "";
    try { named = element.Name; } catch (Exception) { named = ""; }
    return string.Format("'{0}' (id {1})", named, element.Id);
};

// 1 continuous, 0 break.
var wanted = -1;
var said = plain(join);
if (said == "continuous" || said == "make continuous" || said == "join") wanted = 1;
else if (said == "break" || said == "break at join") wanted = 0;
else
    refused = string.Format("'{0}' is not a join this sets. Say continuous - the mullion runs through the "
        + "crossing, Revit's Make Continuous - or break - it stops at the crossing, Break at Join. Nothing "
        + "was changed", (join ?? "").Trim());

var mullions = new List<Mullion>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    var asMullion = element as Mullion;
    if (asMullion != null)
    {
        if (!mullions.Any(m => m.Id.Equals(asMullion.Id))) mullions.Add(asMullion);
        continue;
    }
    string category = "";
    try { category = element.Category == null ? "" : element.Category.Name; } catch (Exception) { category = ""; }
    notMullion.Add(string.Format("{0}{1} is not a mullion - left alone", nameOf(element),
        category.Length == 0 ? "" : " (" + category + ")"));
}

if (refused.Length == 0 && mullions.Count == 0)
    refused = notMullion.Count == 0
        ? "No mullion was given. Select the mullions to make continuous or break, and ask again. Nothing was "
            + "changed"
        : string.Format("Nothing given is a mullion - {0}. Select the mullions themselves - Tab to one in "
            + "Revit. Nothing was changed", string.Join("; ", notMullion.Take(3)));
if (refused.Length == 0 && mullions.Count > MostMullions)
    refused = string.Format("{0} mullions is more than this sets in one go - {1} at most. Nothing was changed",
        mullions.Count, MostMullions);

if (refused.Length == 0 && doc.IsWorkshared)
{
    var held = new List<string>();
    foreach (var mullion in mullions)
    {
        try
        {
            string owner;
            if (WorksharingUtils.GetCheckoutStatus(doc, mullion.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
                held.Add(string.Format("{0} by {1}", nameOf(mullion), string.IsNullOrEmpty(owner) ? "another user" : owner));
        }
        catch (Exception) { }
    }
    if (held.Count > 0)
        refused = string.Format("In this shared model {0} mullion(s) are held by others - {1}. They relinquish "
            + "them, then ask again. Nothing was changed", held.Count, string.Join("; ", held.Take(5)));
}

// THE LENGTH OF A MULLION ALONG ITS RUN, off its solid: every edge end of
// every solid, projected on the direction of its location line. Negative
// when it cannot be read.
var options = new Options();
Func<GeometryElement, XYZ, double[], int> spread = null;
spread = (geometry, way, range) =>
{
    var points = 0;
    if (geometry == null) return 0;
    foreach (var piece in geometry)
    {
        var solid = piece as Solid;
        if (solid != null)
        {
            EdgeArray edges = null;
            try { edges = solid.Edges; } catch (Exception) { edges = null; }
            if (edges == null) continue;
            foreach (Edge edge in edges)
            {
                Curve curve = null;
                try { curve = edge.AsCurve(); } catch (Exception) { curve = null; }
                if (curve == null) continue;
                for (var end = 0; end < 2; end++)
                {
                    var along = curve.GetEndPoint(end).DotProduct(way);
                    range[0] = Math.Min(range[0], along);
                    range[1] = Math.Max(range[1], along);
                    points++;
                }
            }
            continue;
        }
        var instance = piece as GeometryInstance;
        if (instance != null)
        {
            GeometryElement inside = null;
            try { inside = instance.GetInstanceGeometry(); } catch (Exception) { inside = null; }
            points += spread(inside, way, range);
        }
    }
    return points;
};
Func<Mullion, double> lengthOf = mullion =>
{
    Curve run = null;
    try { run = mullion.LocationCurve; } catch (Exception) { run = null; }
    if (run == null) return -1;
    var way = run.GetEndPoint(1) - run.GetEndPoint(0);
    if (way.GetLength() <= 1e-9) return -1;
    way = way.Normalize();
    GeometryElement geometry = null;
    try { geometry = mullion.get_Geometry(options); } catch (Exception) { geometry = null; }
    var range = new[] { double.MaxValue, double.MinValue };
    if (spread(geometry, way, range) == 0) return -1;
    return range[1] - range[0];
};

if (refused.Length == 0)
{
    var before = mullions.Select(m => lengthOf(m)).ToList();
    for (var i = 0; i < mullions.Count; i++)
    {
        try
        {
            if (wanted == 1) mullions[i].JoinMullion();
            else mullions[i].BreakMullion();
        }
        catch (Exception notTaken)
        {
            throw new InvalidOperationException(string.Format("Revit would not {0} mullion {1} - {2}. Nothing "
                + "this call did is kept", wanted == 1 ? "make continuous" : "break at its join",
                nameOf(mullions[i]), notTaken.Message));
        }
    }
    try { doc.Regenerate(); }
    catch (Exception notRebuilt)
    {
        throw new InvalidOperationException(string.Format("Revit could not rebuild the curtain walls after the "
            + "change - {0}. Nothing this call did is kept", notRebuilt.Message));
    }

    var lines = new List<string>();
    for (var i = 0; i < mullions.Count; i++)
    {
        var after = lengthOf(mullions[i]);
        var word = string.Format("{0}: {1} mm long along its run before, {2} mm now", nameOf(mullions[i]),
            before[i] < 0 ? "not readable" : sayMm(before[i] * MillimetresPerFoot),
            after < 0 ? "not readable" : sayMm(after * MillimetresPerFoot));
        lines.Add(word);
        if (before[i] < 0 || after < 0)
        {
            unchanged.Add(word + " - its length could not be read, so whether it changed is not known");
            continue;
        }
        var grew = (after - before[i]) * MillimetresPerFoot;
        if ((wanted == 1 && grew > ChangedMm) || (wanted == 0 && grew < -ChangedMm)) changed++;
        else unchanged.Add(word + (wanted == 1
            ? " - no longer than before: it may already run through, cross nothing, or the wall type's Join "
                + "Condition may decide it"
            : " - no shorter than before: it may already stop at the crossing, cross nothing, or the wall "
                + "type's Join Condition may decide it"));
    }
    readBack = string.Format("{0} for {1} mullion(s), each measured off its solid along its own run after "
        + "Revit rebuilt the wall: {2}", wanted == 1 ? "Make Continuous" : "Break at Join", mullions.Count,
        string.Join("; ", lines));
    findings.Add(string.Format("{0} of {1} mullion(s) {2}, measured", changed, mullions.Count,
        wanted == 1 ? "now run through the crossing - longer along their run" : "now stop at the crossing - "
            + "shorter along their run"));
    if (unchanged.Count > 0)
        findings.Add(string.Format("{0} did not change length the way asked - see unchanged. Revit took the "
            + "call and said nothing back", unchanged.Count));
    if (notMullion.Count > 0)
        findings.Add(string.Format("{0} other element(s) given and left alone - see notMullion", notMullion.Count));
    findings.Add("Read back from Revit - see readBack");
}
else
{
    findings.Add(refused);
}
