// NOT STANDALONE. Assumes `doc`, `elements`, `couplerType`, `barEnd` and
// `between` are in scope; leaves `placed`, `alreadyCoupled`, `skipped`,
// `couplerTypes`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it. Each coupler is made inside a SubTransaction of its own
// within it, so a coupler Revit refuses leaves nothing behind and the rest of
// the batch stands.
//
// REVIT'S STRUCTURE > REINFORCEMENT > REBAR COUPLER, both of its placements:
// "Coupler at bar end" caps one end of a bar (or of every bar in a rebar set),
// and "Coupler between bars" joins two bars that meet end to end. The API is
// one call, the same on every release 2020 to 2027 (read with
// tools/api-surface --members on 2026-11-01):
//   RebarCoupler.Create(doc, couplerTypeId, first, second, out RebarCouplerError)
// with RebarReinforcementData.Create(rebarId, end) for each bar end - end 0 is
// the bar's Start and 1 its End - and `second` null to cap a single end.
//
// THE COUPLER TYPE IS NEVER GUESSED. It is a type of the Structural Rebar
// Couplers category (OST_Coupler), named by the modeller exactly - its type
// name, or "Family: Type" where two families share a type name. None named,
// one not loaded, or a name two families share: NOTHING IS MADE and the loaded
// coupler types are listed in `couplerTypes`.
//
// WHICH END. `barEnd` is "both" (left out), "start" or "end" - Revit's own
// Start and End of the bar - or "top" or "bottom", read from the bar's own
// centre line: the end that is higher (or lower). A bar whose two ends are at
// the same height has no top; it is named and left, never given one.
// "between" ignores `barEnd`: the ends joined are the ends where two selected
// bars meet, and an end two other bars meet is named and left, never paired by
// a rule Heron chose.
//
// SKIPPED, NEVER DOUBLED: a bar end that already has a coupler (Rebar.
// GetCouplerId) is left as it was and named in `alreadyCoupled`; a pair where
// either end already has one likewise.
//
// REVIT'S REFUSALS IN REVIT'S WORDS. RebarCouplerError has eight values on
// every release; each is said per bar end - a hook on that end, an end
// treatment that does not suit the coupler, bars that do not touch, are not in
// line or not parallel, or rebar sets laid out differently.
//
// STRUCTURAL SETTINGS > REBAR SETTINGS. When "Include end treatments in Rebar
// Shape definition" is on (ReinforcementSettings.RebarShapeDefinesEndTreatments),
// Revit may refuse a coupler or change a bar's shape to carry it. Heron does
// not change a project setting: it is read, named in the findings, and named
// again beside every coupler Revit refused.
//
// READ BACK: after a regenerate each bar end asked is read again, and only an
// end whose GetCouplerId is the coupler just made counts. Ids are carried and
// compared as ElementId, never read as numbers.

var placed = new List<ElementId>();
var alreadyCoupled = new List<string>();
var skipped = new List<string>();
var couplerTypes = new List<string>();
var findings = new List<string>();
var lines = new List<string>();
string refused = "";

// Two bar ends this close count as meeting, for "between". Revit itself
// decides whether they TOUCH (BarsNotTouching); this only pairs them.
const double MeetFeet = 10.0 / 304.8;
const double LevelFeet = 1.0 / 304.8;

Func<Element, string> name = one =>
{
    if (one == null) return "(nothing)";
    var category = one.Category != null ? one.Category.Name : "Element";
    string label = null;
    try { label = one.Name; } catch { label = null; }
    return category + " " + one.Id + (string.IsNullOrEmpty(label) ? "" : " \"" + label + "\"");
};
Func<int, string> endWord = e => e == 0 ? "Start" : "End";

Func<RebarCouplerError, string> sayError = error =>
{
    switch (error)
    {
        case RebarCouplerError.IncorrectInputData:
            return "Revit did not accept this bar end for a coupler (incorrect input)";
        case RebarCouplerError.DifferentLayout:
            return "the two rebar sets are laid out differently - a coupler between sets needs the same layout, number and spacing";
        case RebarCouplerError.BarsNotTouching:
            return "the two bars do not touch end to end";
        case RebarCouplerError.IncorrectEndTreatmentHook:
            return "this bar end has a hook - a coupler goes only on an end with no hook";
        case RebarCouplerError.IncorrectEndTreatmentCoupler:
            return "this bar end's end treatment does not suit this coupler type";
        case RebarCouplerError.BarSegementsAreNotParallel:
            return "the end segments of the two bars are not parallel";
        case RebarCouplerError.BarSegmentsAreNotOnSameLine:
            return "the end segments of the two bars are not in line";
        default:
            return "Revit refused it (" + error + ")";
    }
};

// ---- the project setting that can block a coupler ----
var shapeHoldsEndTreatments = false;
try
{
    var settings = ReinforcementSettings.GetReinforcementSettings(doc);
    shapeHoldsEndTreatments = settings != null && settings.RebarShapeDefinesEndTreatments;
}
catch { shapeHoldsEndTreatments = false; }
var settingNote = shapeHoldsEndTreatments
    ? " Structural Settings > Rebar Settings has \"Include end treatments in Rebar Shape definition\" ON - with it on Revit can refuse a coupler; it is a project setting Heron does not change."
    : "";

// ---- the coupler types loaded ----
var symbols = new FilteredElementCollector(doc)
    .OfClass(typeof(FamilySymbol))
    .OfCategory(BuiltInCategory.OST_Coupler)
    .Cast<FamilySymbol>()
    .OrderBy(s => s.FamilyName + ": " + s.Name)
    .ToList();
foreach (var s in symbols) couplerTypes.Add(s.FamilyName + ": " + s.Name);

FamilySymbol chosen = null;
var asked = (couplerType ?? "").Trim();
if (symbols.Count == 0)
    refused = "No coupler type is loaded in this model - load a Structural Rebar Couplers family first. NOTHING WAS MADE.";
else if (asked == "" || asked.Equals("none", StringComparison.OrdinalIgnoreCase))
    refused = "No coupler type was named - Heron never chooses one. Loaded: " + string.Join(", ", couplerTypes)
        + ". Say which. NOTHING WAS MADE.";
else
{
    var byFull = symbols.Where(s => (s.FamilyName + ": " + s.Name) == asked
                                 || (s.FamilyName + " : " + s.Name) == asked).ToList();
    var byName = symbols.Where(s => s.Name == asked).ToList();
    if (byFull.Count == 1) chosen = byFull[0];
    else if (byName.Count == 1) chosen = byName[0];
    else if (byName.Count > 1)
        refused = "\"" + asked + "\" is a type in " + byName.Count + " coupler families ("
            + string.Join(", ", byName.Select(s => s.FamilyName + ": " + s.Name))
            + ") - say which as \"Family: Type\". NOTHING WAS MADE.";
    else
        refused = "No coupler type named \"" + asked + "\" is loaded. Loaded: " + string.Join(", ", couplerTypes)
            + ". NOTHING WAS MADE.";
}

// ---- which end ----
var which = (barEnd ?? "").Trim().ToLowerInvariant();
if (which == "" || which == "ends" || which == "both ends") which = "both";
if (refused == "" && !between
    && which != "both" && which != "start" && which != "end" && which != "top" && which != "bottom")
    refused = "\"" + barEnd + "\" is not a bar end - say start, end, both, top or bottom. NOTHING WAS MADE.";

// ---- the bars ----
var bars = new List<Rebar>();
if (refused == "")
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var bar = element as Rebar;
        if (bar == null)
        {
            skipped.Add(name(element) + ": not a rebar bar or rebar set - a coupler goes on rebar only.");
            continue;
        }
        bars.Add(bar);
    }
    if (bars.Count == 0)
        refused = "None of the " + skipped.Count + " element(s) given is a rebar bar or set. NOTHING WAS MADE.";
    else if (between && bars.Count < 2)
        refused = "Coupler between bars joins two bars - only one rebar was given. NOTHING WAS MADE.";
}

// The two end points of a bar's centre line, hooks left out, read from the
// first bar position that exists (a set may leave its first bar out).
Func<Rebar, XYZ[]> endsOf = bar =>
{
    try
    {
        var position = 0;
        var count = Math.Max(1, bar.NumberOfBarPositions);
        while (position < count && !bar.DoesBarExistAtPosition(position)) position++;
        if (position >= count) return null;
        var curves = bar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, position);
        if (curves == null || curves.Count == 0) return null;
        return new[] { curves[0].GetEndPoint(0), curves[curves.Count - 1].GetEndPoint(1) };
    }
    catch { return null; }
};

// What was asked: the coupler made, and every bar end it should now sit on.
var made = new List<Tuple<ElementId, List<Tuple<Rebar, int>>, string>>();
var symbolReady = false;

Action<List<Tuple<Rebar, int>>, string> makeOne = (barEnds, label) =>
{
    if (!symbolReady)
    {
        if (!chosen.IsActive) { chosen.Activate(); doc.Regenerate(); }
        symbolReady = true;
    }
    var step = new SubTransaction(doc);
    try
    {
        step.Start();
        var first = RebarReinforcementData.Create(barEnds[0].Item1.Id, barEnds[0].Item2);
        var second = barEnds.Count > 1 ? RebarReinforcementData.Create(barEnds[1].Item1.Id, barEnds[1].Item2) : null;
        RebarCouplerError error;
        var coupler = RebarCoupler.Create(doc, chosen.Id, first, second, out error);
        if (coupler == null || error != RebarCouplerError.ValidationSuccessfuly)
        {
            step.RollBack();
            skipped.Add(label + ": not placed - " + sayError(error) + "." + settingNote);
            return;
        }
        step.Commit();
        made.Add(Tuple.Create(coupler.Id, barEnds, label));
    }
    catch (Exception ex)
    {
        if (step.HasStarted() && !step.HasEnded()) step.RollBack();
        skipped.Add(label + ": Revit refused it - " + ex.Message + settingNote);
    }
};

if (refused == "" && !between)
{
    foreach (var bar in bars)
    {
        var points = endsOf(bar);
        var endsAsked = new List<int>();
        if (which == "both") { endsAsked.Add(0); endsAsked.Add(1); }
        else if (which == "start") endsAsked.Add(0);
        else if (which == "end") endsAsked.Add(1);
        else
        {
            if (points == null)
            {
                skipped.Add(name(bar) + ": its centre line could not be read, so which end is the " + which + " is not known - left.");
                continue;
            }
            var rise = points[1].Z - points[0].Z;
            if (Math.Abs(rise) < LevelFeet)
            {
                skipped.Add(name(bar) + ": both ends are at the same height, so it has no " + which + " end - say start or end. Left.");
                continue;
            }
            var startIsTop = rise < 0;
            endsAsked.Add((which == "top") == startIsTop ? 0 : 1);
        }

        foreach (var e in endsAsked)
        {
            var label = name(bar) + " at its " + endWord(e) + (which == "top" || which == "bottom" ? " (" + which + ")" : "");
            ElementId existing = ElementId.InvalidElementId;
            try { existing = bar.GetCouplerId(e); } catch { existing = ElementId.InvalidElementId; }
            if (existing != null && existing != ElementId.InvalidElementId)
            {
                alreadyCoupled.Add(label + ": already has coupler " + existing + " - left as it was.");
                continue;
            }
            makeOne(new List<Tuple<Rebar, int>> { Tuple.Create(bar, e) }, label);
        }
    }
}

if (refused == "" && between)
{
    var allEnds = new List<Tuple<Rebar, int, XYZ>>();
    foreach (var bar in bars)
    {
        var points = endsOf(bar);
        if (points == null)
        {
            skipped.Add(name(bar) + ": its centre line could not be read, so where it meets another bar is not known - left.");
            continue;
        }
        allEnds.Add(Tuple.Create(bar, 0, points[0]));
        allEnds.Add(Tuple.Create(bar, 1, points[1]));
    }

    var paired = new HashSet<string>();
    var anyMeeting = new HashSet<ElementId>();
    foreach (var one in allEnds)
    {
        var key = one.Item1.Id + "/" + one.Item2;
        if (paired.Contains(key)) continue;
        var partners = allEnds.Where(o => o.Item1.Id != one.Item1.Id && o.Item3.DistanceTo(one.Item3) <= MeetFeet).ToList();
        if (partners.Count == 0) continue;
        anyMeeting.Add(one.Item1.Id);
        var label = name(one.Item1) + " " + endWord(one.Item2);
        if (partners.Count > 1)
        {
            paired.Add(key);
            foreach (var p in partners) { paired.Add(p.Item1.Id + "/" + p.Item2); anyMeeting.Add(p.Item1.Id); }
            skipped.Add(label + ": " + partners.Count + " other selected bars meet it here ("
                + string.Join(", ", partners.Select(p => name(p.Item1))) + ") - select the two to join. Left.");
            continue;
        }
        var other = partners[0];
        var otherKey = other.Item1.Id + "/" + other.Item2;
        if (paired.Contains(otherKey)) continue;
        paired.Add(key);
        paired.Add(otherKey);
        anyMeeting.Add(other.Item1.Id);
        var pairLabel = label + " to " + name(other.Item1) + " " + endWord(other.Item2);

        ElementId hereHas = ElementId.InvalidElementId, thereHas = ElementId.InvalidElementId;
        try { hereHas = one.Item1.GetCouplerId(one.Item2); } catch { }
        try { thereHas = other.Item1.GetCouplerId(other.Item2); } catch { }
        var hereTaken = hereHas != null && hereHas != ElementId.InvalidElementId;
        var thereTaken = thereHas != null && thereHas != ElementId.InvalidElementId;
        if (hereTaken || thereTaken)
        {
            alreadyCoupled.Add(pairLabel + ": already has coupler " + (hereTaken ? hereHas : thereHas) + " - left as it was.");
            continue;
        }
        makeOne(new List<Tuple<Rebar, int>> { Tuple.Create(one.Item1, one.Item2), Tuple.Create(other.Item1, other.Item2) }, pairLabel);
    }
    foreach (var bar in bars)
        if (!anyMeeting.Contains(bar.Id) && allEnds.Any(o => o.Item1.Id == bar.Id))
            skipped.Add(name(bar) + ": no other selected bar meets it end to end - nothing joined.");
}

// ---- read back ----
if (made.Count > 0)
{
    doc.Regenerate();
    foreach (var m in made)
    {
        var good = true;
        foreach (var be in m.Item2)
        {
            ElementId now = ElementId.InvalidElementId;
            try { now = be.Item1.GetCouplerId(be.Item2); } catch { now = ElementId.InvalidElementId; }
            if (now == null || now != m.Item1) good = false;
        }
        var coupler = doc.GetElement(m.Item1) as RebarCoupler;
        if (!good || coupler == null)
        {
            skipped.Add(m.Item3 + ": a coupler was made but does not read back on " + (m.Item2.Count > 1 ? "both bar ends" : "that bar end") + " - check it.");
            continue;
        }
        var quantity = 1;
        try { quantity = coupler.GetCouplerQuantity(); } catch { quantity = 1; }
        placed.Add(m.Item1);
        lines.Add(m.Item3 + ": coupler " + m.Item1 + " \"" + chosen.FamilyName + ": " + chosen.Name + "\" placed"
            + (quantity > 1 ? " - " + quantity + " couplers, one per bar in the set" : "") + ".");
    }
}

if (refused == "")
    findings.Add(placed.Count + " coupler(s) placed and read back; " + alreadyCoupled.Count + " bar end(s) already had one; "
        + skipped.Count + " skipped, each named with why.");
if (shapeHoldsEndTreatments) findings.Add(settingNote.Trim());
lines.AddRange(alreadyCoupled);
lines.AddRange(skipped);
if (refused != "") lines.Add(refused);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
