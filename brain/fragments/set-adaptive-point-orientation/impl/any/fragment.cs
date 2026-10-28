// NOT STANDALONE. Assumes `doc`, `orientation` and `points` are in scope;
// leaves `changed`, `changedPoints`, `alreadySet`, `pointOrientations`,
// `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE ORIENTATION OF AN ADAPTIVE COMPONENT'S PLACEMENT POINTS - the
// Orientation row a placement point shows in Properties in the Family Editor.
// It decides which way each point's own planes turn when the family is placed:
// with the instance, with the face or line the point lands on, with that face
// and the click order (Host and Loop System - what a panel placed on a surface
// usually needs), or with the project.
//
// WHICH POINTS: "all", or nothing said, is every placement point; "1,2,4" or
// "1-4" names them by placement number - the numbers Revit shows beside them.
//
// PLACEMENT POINTS ONLY. Revit's reference (RevitAPI.xml, read 2026-10-28)
// says GetPointOrientationType and SetPointOrientationType take an Adaptive
// Placement Point and throw for any other point. A shape handle point is
// listed in the answer and never written.
//
// ONLY IN AN ADAPTIVE COMPONENT open in the Family Editor - read before
// anything is written, and a project or any other family refused by name.
//
// A POINT ALREADY AS ASKED IS COUNTED, NOT REWRITTEN. Every adaptive point is
// read again after the write with GetPointOrientationType - the value Revit
// holds, never the value written. A point asked for that does not read as
// asked, or a point NOT asked for that reads differently from before, fails
// the call, and the host rolls the whole call back.

var findings = new List<string>();
var changed = 0;
var alreadySet = 0;
var changedPoints = "";
var pointOrientations = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// THE SIX CHOICES, in the words Revit's Properties shows for them
// (docs/43-building-revit-families.md, SOURCED; the exact display is a
// NEEDS-CHECKING JK row). Each is named from the enum, never typed as a number.
var shown = new Dictionary<AdaptivePointOrientationType, string>
{
    { AdaptivePointOrientationType.ToInstance, "Instance (xyz)" },
    { AdaptivePointOrientationType.ToInstanceZthenHost, "Instance (z) then Host (xy)" },
    { AdaptivePointOrientationType.ToHost, "Host (xyz)" },
    { AdaptivePointOrientationType.ToHostAndLoopSystem, "Host and Loop System (xyz)" },
    { AdaptivePointOrientationType.ToGlobalXYZ, "Global (xyz)" },
    { AdaptivePointOrientationType.ToGlobalZthenHost, "Global (z) then Host (xy)" },
};
// What each does to a placed copy - re-worded from the enum's own remarks.
var meaning = new Dictionary<AdaptivePointOrientationType, string>
{
    { AdaptivePointOrientationType.ToInstance, "with the placed copy's own axes - Revit's default" },
    { AdaptivePointOrientationType.ToInstanceZthenHost, "with the placed copy's Z, and X and Y from the face or line each point lands on" },
    { AdaptivePointOrientationType.ToHost, "with the face or line each point lands on" },
    { AdaptivePointOrientationType.ToHostAndLoopSystem, "with the face or line each point lands on, and Z flips when the points are clicked the other way round - clockwise instead of anticlockwise" },
    { AdaptivePointOrientationType.ToGlobalXYZ, "with the project's own axes" },
    { AdaptivePointOrientationType.ToGlobalZthenHost, "with the project's Z, and X and Y from the face or line each point lands on" },
};
Func<AdaptivePointOrientationType, string> words = o =>
    shown.ContainsKey(o) ? shown[o] : o.ToString() + " (a value Heron has no Properties words for)";

// The modeller's words, squashed to letters and digits: "Host and Loop System
// (xyz)", "host and loop system", "ToHostAndLoopSystem" all read the same.
var accepted = new Dictionary<string, AdaptivePointOrientationType>
{
    { "instance", AdaptivePointOrientationType.ToInstance },
    { "instancexyz", AdaptivePointOrientationType.ToInstance },
    { "default", AdaptivePointOrientationType.ToInstance },
    { "instancezthenhost", AdaptivePointOrientationType.ToInstanceZthenHost },
    { "instancezthenhostxy", AdaptivePointOrientationType.ToInstanceZthenHost },
    { "host", AdaptivePointOrientationType.ToHost },
    { "hostxyz", AdaptivePointOrientationType.ToHost },
    { "hostandloopsystem", AdaptivePointOrientationType.ToHostAndLoopSystem },
    { "hostandloopsystemxyz", AdaptivePointOrientationType.ToHostAndLoopSystem },
    { "hostandloop", AdaptivePointOrientationType.ToHostAndLoopSystem },
    { "hostloopsystem", AdaptivePointOrientationType.ToHostAndLoopSystem },
    { "loopsystem", AdaptivePointOrientationType.ToHostAndLoopSystem },
    { "global", AdaptivePointOrientationType.ToGlobalXYZ },
    { "globalxyz", AdaptivePointOrientationType.ToGlobalXYZ },
    { "globalzthenhost", AdaptivePointOrientationType.ToGlobalZthenHost },
    { "globalzthenhostxy", AdaptivePointOrientationType.ToGlobalZthenHost },
};

var problems = new List<string>();
AdaptivePointOrientationType? wanted = null;
// Every placement point by its number, and every shape handle point.
var placement = new List<Tuple<int, ReferencePoint>>();
var handles = new List<ReferencePoint>();
var targets = new List<Tuple<int, ReferencePoint>>();
// What each placement point read BEFORE the write, by unique id.
var before = new Dictionary<string, AdaptivePointOrientationType>();

// ---------------------------------------------------------------------------
// READ THE WORDS
// ---------------------------------------------------------------------------

// "orientation to host" and "orient to the host" lose their leading words, one
// at a time, until what is left is a choice or nothing more comes off.
var said = squash(orientation);
var stripping = true;
while (stripping && !accepted.ContainsKey(said))
{
    stripping = false;
    foreach (var lead in new[] { "orientation", "orient", "to", "the" })
        if (said.Length > lead.Length && said.StartsWith(lead, StringComparison.Ordinal))
        {
            said = said.Substring(lead.Length);
            stripping = true;
            break;
        }
}
if (!accepted.ContainsKey(said) && said.EndsWith("orientation", StringComparison.Ordinal))
    said = said.Substring(0, said.Length - "orientation".Length);
if (accepted.ContainsKey(said)) wanted = accepted[said];
else
    problems.Add("\"" + (orientation ?? "") + "\" is not one of the Orientations Revit offers an adaptive point: "
        + string.Join(", ", shown.Values) + ".");

// Which points: numbers and ranges, or "all". Words that only say "the points"
// are ignored; anything else is refused rather than guessed at.
var numbersAsked = new List<int>();
var expanded = System.Text.RegularExpressions.Regex.Replace(points ?? "", @"(\d+)\s*(?:-|to)\s*(\d+)", m =>
{
    int from, to;
    if (!int.TryParse(m.Groups[1].Value, out from) || !int.TryParse(m.Groups[2].Value, out to) || to < from || to - from > 200)
        return m.Value + " ?";
    return string.Join(",", Enumerable.Range(from, to - from + 1).Select(n => n.ToString(invariant)));
});
var filler = new HashSet<string> { "all", "every", "each", "the", "of", "them", "placement", "adaptive", "point",
    "points", "number", "numbers", "and", "only" };
var tokens = System.Text.RegularExpressions.Regex.Split(expanded.ToLowerInvariant(), @"[^a-z0-9?]+")
    .Where(t => t.Length > 0).ToList();
var strayWords = new List<string>();
foreach (var token in tokens)
{
    int n;
    if (int.TryParse(token, out n)) { if (!numbersAsked.Contains(n)) numbersAsked.Add(n); }
    else if (!filler.Contains(token)) strayWords.Add(token);
}
if (strayWords.Contains("handle") || strayWords.Contains("handles") || strayWords.Contains("shape"))
    problems.Add("A shape handle point takes no Orientation through Revit's API - its reference sets one on placement "
        + "points only. Name placement points by number, or \"all\".");
else if (strayWords.Count > 0)
    problems.Add("\"" + (points ?? "") + "\" does not say which points - \"all\", or placement numbers such as "
        + "\"1,2,3,4\" or \"1-4\".");

// ---------------------------------------------------------------------------
// THE FAMILY, BEFORE ANYTHING IS WRITTEN
// ---------------------------------------------------------------------------

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "An adaptive point's Orientation is set inside the adaptive family - select a placed copy and Edit Family "
        + "first.";
}
else
{
    var family = doc.OwnerFamily;
    var adaptive = false;
    try { adaptive = family != null && AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(family); } catch (Exception) { }
    if (!adaptive)
    {
        var panel = false;
        try { panel = family != null && family.IsCurtainPanelFamily; } catch (Exception) { }
        problems.Insert(0, "\"" + doc.Title + "\" is not an adaptive component - Revit gives an Orientation only to the "
            + "placement points of one, started from the Generic Model Adaptive template. "
            + (panel ? "Revit reads this as a curtain panel family, not an adaptive one. " : "")
            + "REPORT_FAMILY_TEMPLATE says which kind of family this is.");
    }
    else
    {
        foreach (var p in new FilteredElementCollector(doc).OfClass(typeof(ReferencePoint)).Cast<ReferencePoint>())
        {
            bool isPlacement = false, isHandle = false;
            try { isPlacement = AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, p.Id); } catch (Exception) { }
            if (!isPlacement)
                try { isHandle = AdaptiveComponentFamilyUtils.IsAdaptiveShapeHandlePoint(doc, p.Id); } catch (Exception) { }
            if (isPlacement)
            {
                var n = -1;
                try { n = AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, p.Id); } catch (Exception) { }
                placement.Add(Tuple.Create(n, p));
            }
            else if (isHandle) handles.Add(p);
        }
        placement = placement.OrderBy(t => t.Item1).ToList();
        var have = string.Join(", ", placement.Select(t => t.Item1.ToString(invariant)));

        if (placement.Count == 0)
            problems.Add("This adaptive family has no placement points yet - PLACE_ADAPTIVE_POINTS places them, and "
                + "the Orientation belongs to each one.");
        else if (numbersAsked.Count == 0)
            targets.AddRange(placement);
        else
            foreach (var n in numbersAsked)
            {
                var hit = placement.FirstOrDefault(t => t.Item1 == n);
                if (hit == null)
                    problems.Add("There is no placement point " + n + " - this family's are " + have + ".");
                else targets.Add(hit);
            }

        foreach (var t in placement)
        {
            try { before[t.Item2.UniqueId] = AdaptiveComponentFamilyUtils.GetPointOrientationType(doc, t.Item2.Id); }
            catch (Exception ex)
            {
                problems.Add("Revit would not read placement point " + t.Item1 + "'s Orientation: " + ex.Message);
            }
        }
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// WRITE WHAT DIFFERS, THEN READ EVERY ADAPTIVE POINT BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var asked = wanted.Value;
    var askedIds = new HashSet<string>(targets.Select(t => t.Item2.UniqueId));
    var written = new List<Tuple<int, ReferencePoint>>();
    foreach (var t in targets)
    {
        if (before[t.Item2.UniqueId] == asked) { alreadySet++; continue; }
        try { AdaptiveComponentFamilyUtils.SetPointOrientationType(doc, t.Item2.Id, asked); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not set placement point " + t.Item1 + "'s Orientation to "
                + words(asked) + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        written.Add(t);
    }

    if (written.Count > 0) doc.Regenerate();

    var rows = new List<string>();
    foreach (var t in placement)
    {
        var uid = t.Item2.UniqueId;
        AdaptivePointOrientationType now;
        try { now = AdaptiveComponentFamilyUtils.GetPointOrientationType(doc, t.Item2.Id); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not read placement point " + t.Item1 + "'s Orientation back: "
                + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        if (askedIds.Contains(uid) && now != asked)
            throw new InvalidOperationException("Placement point " + t.Item1 + " reads " + words(now) + " after the write, "
                + "not the " + words(asked) + " asked. The call failed, and Heron rolls the whole call back.");
        if (!askedIds.Contains(uid) && now != before[uid])
            throw new InvalidOperationException("Placement point " + t.Item1 + " was not asked for and now reads "
                + words(now) + ", not the " + words(before[uid]) + " it read before. The call failed, and Heron rolls the "
                + "whole call back.");
        rows.Add("placement point " + t.Item1 + " " + uid + ": " + words(now));
    }
    foreach (var h in handles)
    {
        string note;
        try
        {
            note = words(AdaptiveComponentFamilyUtils.GetPointOrientationType(doc, h.Id))
                + " (Revit read one, though its reference keeps Orientation to placement points) - not written";
        }
        catch (Exception) { note = "no Orientation through Revit's API, which keeps it to placement points - not written"; }
        rows.Add("shape handle point " + h.UniqueId + ": " + note);
    }

    changed = written.Count;
    changedPoints = string.Join("  ||  ", written.Select(t => "point " + t.Item1 + ": "
        + words(before[t.Item2.UniqueId]) + " -> " + words(asked)));
    pointOrientations = string.Join("  ||  ", rows);

    if (changed > 0)
        findings.Add(changed + " placement point(s) set to " + words(asked) + " and read back"
            + (alreadySet > 0 ? "; " + alreadySet + " already read it and were left as they were" : "") + ".");
    else
        findings.Add("Every placement point asked for already reads " + words(asked) + " - nothing was changed.");
    if (handles.Count > 0)
        findings.Add(handles.Count + " shape handle point(s) left as they are - Revit's API sets an Orientation on "
            + "placement points only.");
    if (changed > 0)
        findings.Add("The Orientation shows when the family is placed: each point's planes turn "
            + (meaning.ContainsKey(asked) ? meaning[asked] : words(asked)) + ". Select a point here and Properties shows "
            + "its Orientation. Copies already placed in a project take it when the family is loaded there again "
            + "(LOAD_FAMILY); placing new ones is PLACE_ADAPTIVE_COMPONENTS.");
}

if (refused != null) findings.Add(refused);
