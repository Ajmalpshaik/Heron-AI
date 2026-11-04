// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `numbers`, `startNumber`,
// `reference`, `justify` and `displayRule` are in scope; leaves `placed`,
// `placedIds`, `alreadyNumbered`, `refused`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list. Each run is placed inside a
// SubTransaction of its own, so a run whose numbers cannot be made what was
// asked leaves nothing behind while the others stay.
//
// WHAT REVIT CALLS IT. Annotate > Tread Number places a NumberSystem on ONE
// stair run in ONE view - a plan or an elevation - and the numbers are tread or
// riser numbers by its Tag Type. Sketch-based stairs have none (RevitAPI.xml,
// NumberSystem); a run already numbered in that view is left alone and named.
//
// THE NUMBERS ARE TREADS UNLESS RISERS ARE ASKED FOR. Tag Type is stored as a
// whole number whose meaning is not on any page read here, so it is set by
// Revit's own word for it: each number is tried until Revit shows "Riser" (or
// "Tread"), and a run where none does is rolled back and named - a Revit in
// another language says so rather than placing the wrong numbers.
//
// THE START NUMBER. Revit 2027 gives the numbers a Start Number of their own;
// where the new numbers carry one that can be set, it is set there. Before 2027
// the start is the stair's own "Tread/Riser Start Number", which changes this
// stair's numbers in every view - the answer says which was set. Left out (0),
// neither is touched.
//
// A STAIR IN A MULTISTOREY STAIR is placed on the level the plan is cut on, when
// that is one of its placement levels, or on its only one; otherwise refused,
// naming the levels - which floor is never guessed.

var placed = 0;
var placedIds = new List<ElementId>();
var alreadyNumbered = new List<string>();
var refused = new List<string>();
var lines = new List<string>();
var findings = new List<string>();

Func<string, string> clean = s =>
{
    var t = (s ?? "").Trim();
    // An optional need left out with `absent: none` arrives as the text
    // "none" (row 5b-1432): read it as not given.
    if (string.Equals(t, "none", StringComparison.OrdinalIgnoreCase)) t = "";
    return new string(t.ToLowerInvariant().Where(char.IsLetter).ToArray());
};
Func<Element, string> describe = e => string.Format("{0} '{1}' (id {2})",
    e.Category != null ? e.Category.Name : "element", e.Name, e.Id);

var kind = clean(numbers);
if (kind == "" || kind == "treads" || kind == "treadnumbers") kind = "tread";
if (kind == "risers" || kind == "risernumbers") kind = "riser";
var referenceWord = clean(reference);
if (referenceWord == "") referenceWord = "center";
var justifyWord = clean(justify);
var ruleWord = clean(displayRule);

var referenceOptions = new Dictionary<string, StairsNumberSystemReferenceOption>
{
    { "center", StairsNumberSystemReferenceOption.Center },
    { "centre", StairsNumberSystemReferenceOption.Center },
    { "left", StairsNumberSystemReferenceOption.Left },
    { "right", StairsNumberSystemReferenceOption.Right },
    { "leftquarter", StairsNumberSystemReferenceOption.LeftQuarter },
    { "rightquarter", StairsNumberSystemReferenceOption.RightQuarter },
};
var justifyOptions = new Dictionary<string, NumberSystemJustifyOption>
{
    { "front", NumberSystemJustifyOption.Front },
    { "center", NumberSystemJustifyOption.Center },
    { "centre", NumberSystemJustifyOption.Center },
    { "back", NumberSystemJustifyOption.Back },
};
var ruleOptions = new Dictionary<string, NumberSystemDisplayRule>
{
    { "all", NumberSystemDisplayRule.All },
    { "odd", NumberSystemDisplayRule.Odd },
    { "even", NumberSystemDisplayRule.Even },
    { "startandend", NumberSystemDisplayRule.StartAndEnd },
};

var target = view ?? doc.ActiveView;

if (kind != "tread" && kind != "riser")
    refused.Add(string.Format("numbers was '{0}' - say tread or riser. NOTHING WAS PLACED", numbers));
else if (!referenceOptions.ContainsKey(referenceWord))
    refused.Add(string.Format("reference was '{0}' - say left, center, right, left quarter or right quarter. "
        + "NOTHING WAS PLACED", reference));
else if (justifyWord != "" && !justifyOptions.ContainsKey(justifyWord))
    refused.Add(string.Format("justify was '{0}' - say front, center or back. NOTHING WAS PLACED", justify));
else if (ruleWord != "" && !ruleOptions.ContainsKey(ruleWord))
    refused.Add(string.Format("displayRule was '{0}' - say all, odd, even or start and end. NOTHING WAS PLACED",
        displayRule));
else if (startNumber < 0)
    refused.Add(string.Format("startNumber was {0} - give a whole number from 1, or leave it out. NOTHING WAS PLACED",
        startNumber));
else if (target == null)
    refused.Add("No view was named and there is no active view. NOTHING WAS PLACED");
else if (target.IsTemplate || !(target.ViewType == ViewType.FloorPlan || target.ViewType == ViewType.Elevation
    || target.ViewType == ViewType.Section || target.ViewType == ViewType.EngineeringPlan
    || target.ViewType == ViewType.CeilingPlan))
    refused.Add(string.Format("'{0}' is a {1}{2} - tread numbers go in a plan, an elevation or a section. "
        + "NOTHING WAS PLACED", target.Name, target.ViewType, target.IsTemplate ? " template" : ""));
else if (elements == null || elements.Count == 0)
    refused.Add("No stair or stair run was handed in. NOTHING WAS PLACED");
else
{
    // The runs, in the order handed in: a run itself, or every run of a stair.
    var runs = new List<StairsRun>();
    var seen = new HashSet<ElementId>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        var run = element as StairsRun;
        var stair = element as Stairs;
        if (run != null)
        {
            if (seen.Add(run.Id)) runs.Add(run);
        }
        else if (stair != null)
        {
            if (!Stairs.IsByComponent(doc, stair.Id))
            {
                refused.Add(describe(stair) + ": a sketch-based stair - Revit puts tread numbers on component "
                    + "stairs only");
                continue;
            }
            foreach (var id in stair.GetStairsRuns())
            {
                var r = doc.GetElement(id) as StairsRun;
                if (r != null && seen.Add(r.Id)) runs.Add(r);
            }
        }
        else
            refused.Add(describe(element) + ": not a stair or a stair run");
    }

    // Runs already numbered in this view - Revit allows one set per run there.
    var numberedHere = new Dictionary<ElementId, ElementId>();
    foreach (var ns in new FilteredElementCollector(doc, target.Id).OfClass(typeof(NumberSystem)).Cast<NumberSystem>())
    {
        var host = ns.NumberedElementId;
        if (host != null && host.HostElementId != ElementId.InvalidElementId && !numberedHere.ContainsKey(host.HostElementId))
            numberedHere[host.HostElementId] = ns.Id;
    }

    // Revit 2027's own Start Number on the numbers, looked up by name so this
    // compiles on every release.
    BuiltInParameter ownStart;
    var hasOwnStart = Enum.TryParse("NUMBER_SYSTEM_START_NUMBER", out ownStart);
    var stairStartSet = new HashSet<ElementId>();

    foreach (var run in runs)
    {
        var stair = run.GetStairs();
        var name = describe(run) + (stair != null ? " of " + describe(stair) : "");
        if (numberedHere.ContainsKey(run.Id))
        {
            alreadyNumbered.Add(name + ": already numbered in '" + target.Name + "' (id " + numberedHere[run.Id]
                + ") - left alone");
            continue;
        }

        var sub = new SubTransaction(doc);
        sub.Start();
        try
        {
            var option = referenceOptions[referenceWord];
            NumberSystem made;
            if (stair != null && stair.MultistoryStairsId != ElementId.InvalidElementId)
            {
                var multi = doc.GetElement(stair.MultistoryStairsId) as MultistoryStairs;
                var levels = multi != null ? multi.GetStairsPlacementLevels(stair).ToList() : new List<ElementId>();
                var cut = target.GenLevel != null ? target.GenLevel.Id : ElementId.InvalidElementId;
                ElementId level = levels.Contains(cut) ? cut : levels.Count == 1 ? levels[0] : ElementId.InvalidElementId;
                if (level == ElementId.InvalidElementId)
                {
                    sub.RollBack();
                    refused.Add(name + ": part of a multistorey stair placed on "
                        + string.Join(", ", levels.Select(l => doc.GetElement(l) != null ? doc.GetElement(l).Name : l.ToString()))
                        + " - name a plan cut on one of those levels; which floor is never guessed");
                    continue;
                }
                made = NumberSystem.Create(doc, target.Id, new LinkElementId(run.Id), option, new LinkElementId(level));
            }
            else
                made = NumberSystem.Create(doc, target.Id, new LinkElementId(run.Id), run.GetNumberSystemReference(option));

            if (made == null)
            {
                sub.RollBack();
                refused.Add(name + ": Revit made no numbers - nothing was placed on it");
                continue;
            }

            // Tag Type, by Revit's own word.
            var tagType = made.get_Parameter(BuiltInParameter.NUMBER_SYSTEM_TAG_TYPE);
            Func<string> shownType = () => tagType != null ? (tagType.AsValueString() ?? "") : "";
            if (!shownType().ToLowerInvariant().Contains(kind))
            {
                var done = false;
                if (tagType != null && !tagType.IsReadOnly && tagType.StorageType == StorageType.Integer)
                {
                    var first = tagType.AsInteger();
                    for (int v = 0; v <= 3 && !done; v++)
                    {
                        if (v == first) continue;
                        try { tagType.Set(v); } catch { continue; }
                        done = shownType().ToLowerInvariant().Contains(kind);
                    }
                }
                if (!done)
                {
                    sub.RollBack();
                    refused.Add(name + ": Revit's Tag Type for the numbers offered no choice reading '" + kind
                        + "' (it read '" + shownType() + "') - nothing was placed on it rather than the wrong numbers");
                    continue;
                }
            }

            if (justifyWord != "") made.JustifyOption = justifyOptions[justifyWord];
            if (ruleWord != "") made.NumberDisplayRule = ruleOptions[ruleWord];

            var startNote = "";
            if (startNumber > 0)
            {
                var own = hasOwnStart ? made.get_Parameter(ownStart) : null;
                if (own != null && !own.IsReadOnly && own.StorageType == StorageType.Integer)
                {
                    own.Set(startNumber);
                    startNote = "; its own Start Number set to " + startNumber;
                }
                else
                {
                    var basis = stair != null ? stair.get_Parameter(BuiltInParameter.STAIRS_TRISER_NUMBER_BASE_INDEX) : null;
                    if (basis == null || basis.IsReadOnly || basis.StorageType != StorageType.Integer)
                    {
                        sub.RollBack();
                        refused.Add(name + ": the start number cannot be set - the stair's Tread/Riser Start Number "
                            + (basis == null ? "is not there" : "is greyed out") + ". Nothing was placed on it");
                        continue;
                    }
                    if (stairStartSet.Add(stair.Id) && basis.AsInteger() != startNumber)
                        basis.Set(startNumber);
                    startNote = "; the stair's Tread/Riser Start Number set to " + startNumber
                        + " - it numbers this stair in every view";
                }
            }

            sub.Commit();
            doc.Regenerate();

            // READ BACK: the numbers are there, on this run, in this view.
            var back = doc.GetElement(made.Id) as NumberSystem;
            if (back == null || back.NumberedElementId == null || back.NumberedElementId.HostElementId != run.Id
                || back.OwnerViewId != target.Id)
            {
                refused.Add(name + ": Revit made numbers but they do not read back on this run in '" + target.Name + "'");
                continue;
            }
            placed++;
            placedIds.Add(back.Id);
            var startRead = "";
            if (startNumber > 0)
            {
                var own = hasOwnStart ? back.get_Parameter(ownStart) : null;
                var basis = stair != null ? stair.get_Parameter(BuiltInParameter.STAIRS_TRISER_NUMBER_BASE_INDEX) : null;
                var read = own != null && !own.IsReadOnly ? own.AsInteger() : basis != null ? basis.AsInteger() : -1;
                startRead = read == startNumber ? ", starting at " + read : ", but the start reads " + read;
            }
            var tagBack = back.get_Parameter(BuiltInParameter.NUMBER_SYSTEM_TAG_TYPE);
            lines.Add(string.Format("{0}: {1} numbers placed in '{2}' (id {3}) - reference {4}, justify {5}, show {6}{7}{8}",
                name, tagBack != null ? (tagBack.AsValueString() ?? kind) : kind, target.Name, back.Id,
                referenceWord, back.JustifyOption, back.NumberDisplayRule, startRead, startNote));
        }
        catch (Exception ex)
        {
            if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack();
            refused.Add(name + ": Revit would not place numbers on it in '" + target.Name + "' - Revit said: " + ex.Message);
        }
    }
}

var report = string.Format("{0} run(s) numbered in '{1}'; {2} already numbered there; {3} refused.",
    placed, target != null ? target.Name : "no view", alreadyNumbered.Count, refused.Count)
    + (lines.Count > 0 ? " " + string.Join(" ", lines.Select(l => l.TrimEnd('.') + ".")) : "")
    + (alreadyNumbered.Count > 0 ? " " + string.Join(" ", alreadyNumbered.Select(l => l + ".")) : "")
    + (refused.Count > 0 ? " " + string.Join(" ", refused.Select(l => l.TrimEnd('.') + ".")) : "")
    + (placed == 0 ? " NOTHING WAS PLACED." : "");
findings.Add(report);
