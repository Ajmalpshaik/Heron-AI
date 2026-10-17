// NOT STANDALONE. Assumes `doc`, `elements`, `offset`, `toLevels`,
// `turnDegrees`, `turnCentreMm` and `toWorkset` are in scope; leaves `copies`,
// `notCopied`, `copiedTo`, `onWorksets` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `offset` is in internal FEET.
//
// WHICH WORKSET THE COPIES GO ON (version 3, 2026-10-17, FRAGMENT-ISSUES
// 5b-540). Revit puts every copy on the ACTIVE workset, whatever workset its
// original is on. Seen 2026-10-15 on a BIM 360 project in Revit 2020:
// builders-work openings copied from instances on BHVD_BW_WALL_OPENING landed
// on the active workset and had to be moved with SET_ELEMENT_WORKSET after.
//
//   toWorkset "active"  Revit's own way - version 2, line for line. It is what
//                       a call that leaves toWorkset out is read as (the
//                       card's `absent: active`), so no workset is read or
//                       written and `onWorksets` stays empty.
//   toWorkset "source"  each copy on the workset of the element it is a copy
//                       of. "same", "own" and "original" say the same.
//   any other text      every copy on the user workset of that name - case and
//                       extra spaces forgiven. A name in double quotes is
//                       always a name, for a workset called "Source".
//
// Both paths copy exactly as version 2 does. Onto a NAMED workset the copy is
// made with that workset active, as a modeller would, and the active workset
// put back after. For "source" each copy's workset parameter is given its
// original's value, the way SET_ELEMENT_WORKSET writes it. Every copy's
// workset is then read back from the model. A copy whose workset Revit will
// not let be set - a curtain panel, a mullion, something drawn in a view -
// follows its host or its view and is counted as such, never written.
//
// TWO WAYS TO COPY, CHOSEN BY `toLevels` (version 2, 2026-10-13, FRAGMENT-ISSUES
// 5b-460):
//
//   toLevels blank   copy once, by `offset` - version 1, line for line. The
//                    block below that does it is version 1's code unchanged.
//   toLevels named   Revit's Paste Aligned > Selected Levels: one copy on each
//                    level named, standing over the originals in plan, each one
//                    BELONGING to its level, and optionally turned a further
//                    `turnDegrees` per level about a plan point.
//
// WHY THERE IS NO POSITION CHECK IN THE OFFSET PATH, WHEN THE MOVE AND ROTATE
// FRAGMENTS ARE ALMOST ENTIRELY POSITION CHECKS.
//
// Those two call an API that returns NOTHING and can silently do nothing. This
// one returns the ids it created. A copy that did not happen is simply absent
// from that list, so the evidence arrives with the answer and a before-and-after
// comparison would be measuring something already known.
//
// The count is still checked rather than assumed equal: `notCopied` is the
// difference. Revit can return fewer than it was given - an element that cannot
// be duplicated is dropped without complaint - and a caller that assumed one
// copy per element would then pair up the wrong ids downstream.
//
// COPYING IS NOT MOVING, and the whole set is copied in ONE call on purpose.
// Copying elements one at a time breaks the relationships between them: a duct
// and its fitting copied separately arrive unconnected, where copying both
// together preserves the join. That is why this takes a list rather than
// looping - and why the level path still copies the whole set once per level.

var copies = new List<ElementId>();
var notCopied = 0;
var copiedTo = "";
var onWorksets = "";
string refused = null;

var source = new List<ElementId>();
foreach (var element in elements)
{
    if (element != null) source.Add(element.Id);
}

var levelsText = (toLevels ?? "").Trim();
var turnText = (turnDegrees ?? "").Trim();
var centreText = (turnCentreMm ?? "").Trim();

// A TURN IS READ THE SAME WAY ON BOTH PATHS, so "0" means no turn on either.
// A degree sign or the word typed after the number is forgiven; anything else
// that is not a number is refused, because a turn that silently read as zero
// is a tower that silently did not twist.
double turn = 0;
string turnProblem = null;
if (turnText.Length > 0)
{
    var bare = turnText.Replace("°", " ").ToLowerInvariant()
        .Replace("degrees", " ").Replace("degree", " ").Replace("deg", " ").Trim();
    if (!double.TryParse(bare, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out turn)
        || double.IsNaN(turn) || double.IsInfinity(turn))
    {
        turnProblem = "\"" + turnText + "\" is not an angle. Type the turn in degrees as a number - "
            + "2.8125, or -5 to turn clockwise - or leave it blank for no turn.";
        turn = 0;
    }
}

// ----------------------------------------------------------------------
// VERSION 3 - THE WORKSET, read once here and used by both paths below.
// `worksetMode` stays null for "active" or blank, and then nothing below it
// runs: no workset is read, none is written, `onWorksets` stays empty.
//
// NO WORKSET IS EVER READ AS A NUMBER. A workset is a WorksetId, compared
// with ==; the integer ELEM_PARTITION_PARAM is only ever written with the
// integer the ORIGINAL's own ELEM_PARTITION_PARAM holds - the value
// SET_ELEMENT_WORKSET reads and writes. So "source" needs no number, and a
// NAMED workset is reached the way a modeller reaches it in Revit: it is made
// the active workset for the copy, and the active workset put back after.
// ----------------------------------------------------------------------
var worksetText = (toWorkset ?? "").Trim();
string worksetMode = null;              // null, "source" or "named"
string worksetProblem = null;
var worksetNamed = WorksetId.InvalidWorksetId;
var worksetNames = new Dictionary<WorksetId, string>();
{
    var quoted = worksetText.Length >= 2 && worksetText.StartsWith("\"") && worksetText.EndsWith("\"");
    var word = string.Join(" ", worksetText.ToLowerInvariant()
        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    if (word.EndsWith(" workset")) word = word.Substring(0, word.Length - " workset".Length);
    if (word.StartsWith("the ")) word = word.Substring(4);
    if (word.StartsWith("its ")) word = word.Substring(4);

    if (!quoted && (word.Length == 0 || word == "active"))
    {
        // VERSION 2: Revit's own - every copy on the active workset.
    }
    else if (!doc.IsWorkshared)
    {
        // A VALUE THAT WOULD BE IGNORED IS REFUSED INSTEAD - the same rule as
        // the turn. With no worksets the copies would be made and the workset
        // asked for silently never applied.
        worksetProblem = "This model is not workshared, so there is no workset to put the copies on - leave the "
            + "workset out and they are copied as before.";
    }
    else
    {
        var userWorksets = new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets();
        foreach (var workset in userWorksets) worksetNames[workset.Id] = workset.Name;

        if (!quoted && (word == "source" || word == "same" || word == "own" || word == "original"))
        {
            worksetMode = "source";
        }
        else
        {
            var wanted = quoted ? worksetText.Substring(1, worksetText.Length - 2) : worksetText;
            var key = string.Join(" ", wanted.ToLowerInvariant()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            var match = userWorksets.FirstOrDefault(w => string.Join(" ", w.Name.ToLowerInvariant()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) == key);
            if (match != null && !match.IsOpen)
            {
                // A CLOSED workset cannot be the active one, and copies put on
                // it would vanish from every view - so it is said, not done.
                worksetProblem = "\"" + match.Name + "\" is closed in this session - open it (Collaborate > "
                    + "Worksets) and ask again, or the copies would sit where Revit does not show them.";
            }
            else if (match != null)
            {
                worksetNamed = match.Id;
                worksetMode = "named";
            }
            else
            {
                var known = worksetNames.Values.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                worksetProblem = "This model has no user workset named \"" + wanted + "\". Its user worksets: "
                    + (known.Count == 0 ? "none" : string.Join(", ", known.Take(40)))
                    + (known.Count > 40 ? " and " + (known.Count - 40) + " more" : "")
                    + ". Or type source for each copy on its own original's workset.";
            }
        }
    }
}

// THE ACTIVE WORKSET WHEN THE CALL STARTED - named in the answer, and put
// back after every copy onto a named workset.
var worksetActive = worksetMode != null ? doc.GetWorksetTable().GetActiveWorksetId() : WorksetId.InvalidWorksetId;

Func<WorksetId, string> worksetName = id =>
{
    string name;
    if (worksetNames.TryGetValue(id, out name)) return name;
    var other = id == WorksetId.InvalidWorksetId ? null : doc.GetWorksetTable().GetWorkset(id);
    return other != null ? other.Name : "no workset";
};

// A COPY ONTO A NAMED WORKSET IS MADE WITH THAT WORKSET ACTIVE - Revit puts
// every copy, and everything that rides with it, on the active workset. Put
// back whatever happens to the copy. Any other mode copies exactly as before.
Func<Func<ICollection<ElementId>>, ICollection<ElementId>> copyOnWorkset = copy =>
{
    if (worksetMode != "named" || worksetNamed == worksetActive) return copy();
    var table = doc.GetWorksetTable();
    table.SetActiveWorksetId(worksetNamed);
    try { return copy(); }
    finally { table.SetActiveWorksetId(worksetActive); }
};

// "source": THE COPY'S WORKSET, WRITTEN - for every copy in `made`, while each
// copy still stands `dx, dy` in plan from its original. Read back for all of
// them at the end (onWorksets).
var worksetWanted = new Dictionary<ElementId, WorksetId>();
var worksetFollowers = new List<string>();
var worksetUnmatched = new List<string>();
List<Element> worksetPool = null;

Func<Element, string> worksetCategory = element =>
    element.Category != null ? element.Category.Name : element.GetType().Name;

// WHERE AN ELEMENT IS IN PLAN - its point, both ends of its line, or its box -
// to tell two copies of one type apart when their originals sit on different
// worksets. Plan only: a copy by an upward vector can leave a wall where it
// was (measured 2026-10-14, see ONTO LEVELS below), and onto levels each copy
// is a storey up.
Func<Element, List<XYZ>> worksetMarks = element =>
{
    var marks = new List<XYZ>();
    var at = element.Location as LocationPoint;
    var along = element.Location as LocationCurve;
    if (at != null) marks.Add(at.Point);
    else if (along != null && along.Curve != null)
    {
        marks.Add(along.Curve.GetEndPoint(0));
        marks.Add(along.Curve.GetEndPoint(1));
    }
    else
    {
        var box = element.get_BoundingBox(null);
        if (box != null) { marks.Add(box.Min); marks.Add(box.Max); }
    }
    return marks;
};

Action<ICollection<ElementId>, double, double> putOnWorksets = (made, dx, dy) =>
{
    if (worksetMode == null || made == null) return;
    doc.Regenerate();

    if (worksetMode == "named")
    {
        // Made on it already (copyOnWorkset). Only the read-back is owed.
        foreach (var id in made) worksetWanted[id] = worksetNamed;
        return;
    }

    // EVERY ELEMENT A COPY CAN BE A COPY OF - each one given, and what Revit
    // copies with it: a door in a given wall, a curtain panel, a nested part.
    // Read once, before anything is written. Only one whose own workset
    // parameter holds a value can lend it to its copy.
    if (worksetPool == null)
    {
        worksetPool = new List<Element>();
        var seen = new HashSet<ElementId>();
        foreach (var element in elements)
        {
            if (element == null) continue;
            var family = new List<ElementId> { element.Id };
            family.AddRange(element.GetDependentElements(new ElementIsElementTypeFilter(true)));
            foreach (var id in family)
            {
                if (!seen.Add(id)) continue;
                var member = doc.GetElement(id);
                if (member == null || !worksetNames.ContainsKey(doc.GetWorksetId(id))) continue;
                var held = member.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                if (held != null && held.StorageType == StorageType.Integer && held.HasValue) worksetPool.Add(member);
            }
        }
    }

    const double samePlan = 2.0 / 304.8;
    foreach (var id in made)
    {
        var copy = doc.GetElement(id);
        if (copy == null) continue;
        var parameter = copy.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
        if (parameter == null || parameter.StorageType != StorageType.Integer || parameter.IsReadOnly)
        {
            // FOLLOWS ITS HOST OR ITS VIEW - Revit decides its workset.
            worksetFollowers.Add(worksetCategory(copy));
            continue;
        }

        // ITS ORIGINAL. When every original is on one workset - the usual
        // case, and the BHVD_BW_WALL_OPENING one - any of them answers for
        // every copy. Otherwise its original is found: the same category and
        // type, and where that is not enough to name one workset, the same
        // place in plan less the step. A copy of a kind no original is (a
        // sketch plane, a grid line) is left where Revit made it and counted;
        // two originals on different worksets that it could be a copy of are
        // never guessed between.
        var candidates = worksetPool;
        if (worksetPool.Select(o => doc.GetWorksetId(o.Id)).Distinct().Count() > 1)
        {
            candidates = worksetPool.Where(o => o.GetTypeId() == copy.GetTypeId()
                && worksetCategory(o) == worksetCategory(copy)).ToList();
            if (candidates.Count == 0)
            {
                worksetUnmatched.Add(worksetCategory(copy));
                continue;
            }
            if (candidates.Select(o => doc.GetWorksetId(o.Id)).Distinct().Count() > 1)
            {
                var marks = worksetMarks(copy);
                candidates = candidates.Where(o =>
                {
                    var was = worksetMarks(o);
                    if (marks.Count == 0 || was.Count != marks.Count) return false;
                    for (var i = 0; i < marks.Count; i++)
                    {
                        if (Math.Abs(marks[i].X - dx - was[i].X) > samePlan
                            || Math.Abs(marks[i].Y - dy - was[i].Y) > samePlan) return false;
                    }
                    return true;
                }).ToList();
            }
        }
        var on = candidates.Select(o => doc.GetWorksetId(o.Id)).Distinct().ToList();
        if (on.Count != 1)
            throw new InvalidOperationException("A copy of a " + worksetCategory(copy) + " could not be told "
                + "from its original" + (on.Count > 1 ? "s on different worksets" : "") + ", so its workset "
                + "would be a guess. Nothing was kept - name one workset in toWorkset, or copy each "
                + "workset's elements on their own.");

        var original = candidates.First(o => doc.GetWorksetId(o.Id) == on[0]);
        var value = original.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).AsInteger();
        if (parameter.AsInteger() != value && !parameter.Set(value))
            throw new InvalidOperationException("Revit would not put a copy of a " + worksetCategory(copy)
                + " on workset \"" + worksetName(on[0]) + "\". Nothing was kept.");
        worksetWanted[id] = on[0];
    }
};

// READ BACK - every copy's workset as the model now holds it, never the
// value written: Set returning true is not evidence it stored (the reason
// SET_ELEMENT_WORKSET reads back too). A copy whose workset can be set that
// does not hold the one asked for takes the whole call back.
Action readBackWorksets = () =>
{
    if (worksetMode == null || copies.Count == 0) return;
    var held = new Dictionary<string, int>();
    var right = 0;
    var settable = 0;
    var total = 0;
    foreach (var id in copies)
    {
        var copy = doc.GetElement(id);
        if (copy == null) continue;
        total++;
        var on = doc.GetWorksetId(id);
        var name = worksetName(on);
        held[name] = held.ContainsKey(name) ? held[name] + 1 : 1;

        WorksetId wanted;
        if (!worksetWanted.TryGetValue(id, out wanted)) continue;
        var parameter = copy.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
        if (parameter == null || parameter.StorageType != StorageType.Integer || parameter.IsReadOnly)
        {
            // Named: made on the workset, but Revit decides this one's.
            if (worksetMode == "named") worksetFollowers.Add(worksetCategory(copy));
            continue;
        }
        settable++;
        if (on != wanted)
            throw new InvalidOperationException("A copy of a " + worksetCategory(copy) + " reads workset \"" + name
                + "\" after it was put on \"" + worksetName(wanted) + "\". Nothing was kept.");
        right++;
    }

    var lines = new List<string>();
    lines.Add(worksetMode == "named"
        ? "Every copy asked onto workset \"" + worksetName(worksetNamed) + "\" - made with it the active workset, and \""
          + worksetName(worksetActive) + "\" the active workset again after."
        : "Each copy asked onto its own original's workset; Revit makes copies on the active workset, \""
          + worksetName(worksetActive) + "\".");
    lines.Add("Read back, all " + total + " copies: "
        + string.Join(", ", held.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => p.Key + " " + p.Value)) + ".");
    lines.Add(right + " of " + settable + " copies whose workset can be set are on the workset asked for"
        + (worksetFollowers.Count > 0
            ? "; " + worksetFollowers.Count + " follow their host or view and were not set ("
              + string.Join(", ", worksetFollowers.Distinct()) + ")"
            : "")
        + (worksetUnmatched.Count > 0
            ? "; " + worksetUnmatched.Count + " are of a kind none of the originals is and stay where Revit made "
              + "them (" + string.Join(", ", worksetUnmatched.Distinct()) + ")"
            : "")
        + ".");
    onWorksets = string.Join("\n", lines);
};

if (levelsText.Length == 0)
{
    if (turnProblem != null)
    {
        refused = turnProblem + " Nothing was copied.";
    }
    else if (Math.Abs(turn) > 1e-12 || centreText.Length > 0)
    {
        // A VALUE THAT WOULD BE IGNORED IS REFUSED INSTEAD. A turn is only ever
        // applied per level; reading one here and copying by the offset anyway
        // would be a fragment that succeeded while not doing what it was told.
        refused = "A turn is applied only when copying onto levels - name them in toLevels. To turn "
            + "elements where they are, ROTATE_ELEMENTS does it; to copy them round a point, "
            + "ARRAY_ELEMENTS_RADIAL. Nothing was copied.";
    }
    else if (worksetProblem != null)
    {
        refused = worksetProblem + " Nothing was copied.";
    }
    else if (source.Count > 0)
    {
        // VERSION 1, UNCHANGED - but for a named workset, made active for it.
        var made = copyOnWorkset(() => ElementTransformUtils.CopyElements(doc, source, offset));
        if (made != null) copies.AddRange(made);

        // Fewer back than went in. Not an error and not silence either - the
        // caller needs to know the lists do not line up before it starts pairing
        // originals with copies.
        notCopied = source.Count - copies.Count;
        if (notCopied < 0) notCopied = 0;

        // VERSION 3 - nothing at all unless a workset was asked for.
        putOnWorksets(copies, offset != null ? offset.X : 0, offset != null ? offset.Y : 0);
        readBackWorksets();
    }
}
else
{
    // ------------------------------------------------------------------
    // ONTO LEVELS.
    //
    // THE METHOD, AND WHY EACH STEP IS THERE - one pass per level, in the
    // order the levels were named:
    //
    //  1. Copy the whole set once, the way Revit's own Paste Aligned to
    //     Selected Levels does it: from a floor plan on the originals' level
    //     to a floor plan on the target - one ElementTransformUtils call, so
    //     joins and hosted elements come along exactly as in the offset path.
    //     A level with no floor plan gets a temporary one, deleted as soon as
    //     the copy is made, inside the same undo entry. A kilometre to the
    //     side as well, because a copy made against its original joins to it
    //     and Revit refuses to keep that join once the copy is turned ("Can't
    //     keep elements joined").
    //     WHY PLAN TO PLAN, ALL MEASURED 2026-10-14 ON REVIT 2020, rolled
    //     back: copied by a +4000 mm vector, four walls stayed at 0..4000 on
    //     Level 1 while the slab rose; and a curtain wall moved onto Level 2
    //     by its Base Constraint keeps all six panels on Level 1, read-only.
    //     Plan to plan, the walls came out on Level 3 + 0 with their top tie
    //     moved from Level 2 to Level 4, the slab on Level 3 + 0 and all six
    //     panels on Level 3 - with walls and floors hidden in the source plan,
    //     and into a plan made for the copy and deleted after it.
    //  2. Match every copy to its original - same category, same type, same
    //     outline in plan less the step aside.
    //  3. Make sure each copy is ON its level, from its ORIGINAL's values:
    //     the level parameter the level, and every height the original's own
    //     one storey up, so the offset is the original's. A wall tied to the
    //     level above is tied to the level the same height above its new
    //     base; with none there it is left unconnected at the same height.
    //     Only a value that differs is written - Revit's copy has normally
    //     set them all already, and a level written again for nothing is
    //     what strands a curtain wall's panels on the old one.
    //  4. Move the copies back over the originals and turn them about a
    //     vertical line through the centre, by the turn times the level's place
    //     in the list. Only elements standing on their own are moved; a door,
    //     a panel or a mullion follows its host.
    //  5. Read every copy back after Revit has regenerated, against its own
    //     original: its level, its box exactly the rise above the original's
    //     to half a millimetre, its offset, and its plan point where the turn
    //     puts it. A copy that fails the level or the height takes the WHOLE
    //     call back with it (the exceptions below), because a tower with one
    //     storey left on the ground floor's level is worse than no tower.
    //
    // WHAT IS REFUSED BEFORE ANYTHING IS COPIED - each one says "nothing was
    // copied" in as many words: a level name the model does not have (the
    // negative case), the same level or two levels at one height named twice,
    // a level the originals already stand on, a non-zero offset (each copy
    // stands over the original; a shift as well is MOVE_ELEMENTS after), an
    // element with no level Heron knows how to change, and a hosted element
    // given without its host.
    // ------------------------------------------------------------------

    const double mmPerFoot = 304.8;
    const double sameHeight = 1.0 / mmPerFoot;     // two levels nearer than 1 mm are one height
    const double stayedPut = 0.5 / mmPerFoot;      // a copy that moved more than 0.5 mm moved

    var problems = new List<string>();
    if (turnProblem != null) problems.Add(turnProblem);
    if (worksetProblem != null) problems.Add(worksetProblem);

    Func<string, string> squeeze = text => string.Join(" ",
        (text ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
        .OrderBy(level => level.Elevation).ThenBy(level => level.Name).ToList();

    Func<string, Level> levelNamed = name =>
    {
        var key = squeeze(name);
        return allLevels.FirstOrDefault(level => squeeze(level.Name) == key);
    };

    Func<double, Level> levelAt = elevation =>
        allLevels.FirstOrDefault(level => Math.Abs(level.Elevation - elevation) < sameHeight);

    Func<double, string> mm = feet => Math.Round(feet * mmPerFoot, 1)
        .ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    // THE TARGETS, IN THE ORDER GIVEN - the order decides the turn. "A..B" is
    // every level from A to B by height, both included, which is how a tower's
    // thirty-one storeys are named without typing thirty-one names. A level
    // whose own name contains ".." is found by that name first.
    var targets = new List<Level>();
    var unknown = new List<string>();
    foreach (var raw in levelsText.Split(','))
    {
        var item = raw.Trim();
        if (item.Length == 0) continue;

        var exact = levelNamed(item);
        if (exact != null) { targets.Add(exact); continue; }

        var dots = item.IndexOf("..", StringComparison.Ordinal);
        if (dots < 0) { unknown.Add(item); continue; }

        var fromName = item.Substring(0, dots).Trim();
        var toName = item.Substring(dots + 2).Trim();
        var from = levelNamed(fromName);
        var to = levelNamed(toName);
        if (from == null) unknown.Add(fromName);
        if (to == null) unknown.Add(toName);
        if (from == null || to == null) continue;

        var low = Math.Min(from.Elevation, to.Elevation) - sameHeight;
        var high = Math.Max(from.Elevation, to.Elevation) + sameHeight;
        var run = allLevels.Where(level => level.Elevation >= low && level.Elevation <= high).ToList();
        if (from.Elevation > to.Elevation) run.Reverse();
        targets.AddRange(run);
    }

    if (unknown.Count > 0)
    {
        problems.Add("This model has no level named " + string.Join(", ", unknown.Select(n => "\"" + n + "\""))
            + ". Its levels, lowest first: " + string.Join(", ", allLevels.Take(40).Select(l => l.Name))
            + (allLevels.Count > 40 ? " and " + (allLevels.Count - 40) + " more" : "") + ".");
    }
    else if (targets.Count == 0)
    {
        problems.Add("toLevels named no level.");
    }

    for (var i = 0; i < targets.Count; i++)
    {
        for (var j = 0; j < i; j++)
        {
            if (targets[i].Id == targets[j].Id)
                problems.Add(targets[i].Name + " is named twice - it would get two copies, one on top of the other.");
            else if (Math.Abs(targets[i].Elevation - targets[j].Elevation) < sameHeight)
                problems.Add(targets[j].Name + " and " + targets[i].Name + " are at the same height - the copies "
                    + "on them would stand one inside the other.");
        }
    }

    if (offset != null && offset.GetLength() > 1e-9)
    {
        problems.Add("With levels named, each copy stands over its original in plan, so the offset must be "
            + "0,0,0. To shift them as well, copy them onto the levels first and MOVE_ELEMENTS after.");
    }

    // THE TURN CENTRE, in millimetres - "x, y" or "x, y, z", the height ignored.
    XYZ centre = null;
    if (centreText.Length > 0)
    {
        var parts = centreText.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var numbers = new List<double>();
        foreach (var part in parts)
        {
            double value;
            if (double.TryParse(part, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out value)
                && !double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) <= 100000000.0)
                numbers.Add(value);
            else
                numbers.Clear();
            if (numbers.Count == 0) break;
        }
        if (numbers.Count == 2 || numbers.Count == 3)
            centre = new XYZ(numbers[0] / mmPerFoot, numbers[1] / mmPerFoot, 0);
        else
            problems.Add("\"" + centreText + "\" is not a plan point. Type it as two numbers in MILLIMETRES - "
                + "\"26000, 20000\" - or leave it blank to turn about the middle of what is copied.");
    }

    // WHICH PARAMETER HOLDS AN ELEMENT'S LEVEL, AND WHICH ITS HEIGHT ABOVE IT.
    // [0] the level, [1] the level its top is tied to, [2] that top's offset,
    // [3..] every offset measured from [0]. INVALID where a kind has none.
    // The pairs are the ones SET_ELEMENT_LEVEL and SET_WALL_CONSTRAINTS
    // already change in proven runs, and one row per kind the brief names.
    Func<Element, BuiltInParameter, Parameter> writable = (element, which) =>
    {
        var parameter = element.get_Parameter(which);
        return parameter != null && !parameter.IsReadOnly ? parameter : null;
    };

    Func<Element, BuiltInParameter[]> ruleFor = element =>
    {
        var none = BuiltInParameter.INVALID;
        if (element is Wall)
            return new[] { BuiltInParameter.WALL_BASE_CONSTRAINT, BuiltInParameter.WALL_HEIGHT_TYPE,
                           BuiltInParameter.WALL_TOP_OFFSET, BuiltInParameter.WALL_BASE_OFFSET };
        if (element is MEPCurve)
            return new[] { BuiltInParameter.RBS_START_LEVEL_PARAM, none, none, BuiltInParameter.RBS_OFFSET_PARAM };
        if (element is Floor)
            return new[] { BuiltInParameter.LEVEL_PARAM, none, none, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM };
        if (element is Ceiling)
            return new[] { BuiltInParameter.LEVEL_PARAM, none, none, BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM };
        if (element is FootPrintRoof)
            return new[] { BuiltInParameter.ROOF_BASE_LEVEL_PARAM, none, none, BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM };
        if (element is ExtrusionRoof)
            return new[] { BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM, none, none,
                           BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM };
        if (element is FamilyInstance)
        {
            // A column: a base and a top, each on a level.
            if (writable(element, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM) != null)
                return new[] { BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM,
                               BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM,
                               BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM };
            // A beam or a brace: a reference level and an offset at each end.
            if (writable(element, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM) != null)
                return new[] { BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM, none, none,
                               BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION,
                               BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION };
            // Everything else placed on a level: equipment, furniture, fixtures.
            if (writable(element, BuiltInParameter.FAMILY_LEVEL_PARAM) != null)
            {
                if (writable(element, BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM) != null)
                    return new[] { BuiltInParameter.FAMILY_LEVEL_PARAM, none, none,
                                   BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM };
                if (writable(element, BuiltInParameter.INSTANCE_ELEVATION_PARAM) != null)
                    return new[] { BuiltInParameter.FAMILY_LEVEL_PARAM, none, none,
                                   BuiltInParameter.INSTANCE_ELEVATION_PARAM };
            }
        }
        return null;
    };

    Func<Element, BuiltInParameter, Level> levelIn = (element, which) =>
    {
        if (which == BuiltInParameter.INVALID) return null;
        var parameter = element.get_Parameter(which);
        if (parameter == null || parameter.StorageType != StorageType.ElementId) return null;
        return doc.GetElement(parameter.AsElementId()) as Level;
    };

    Func<Element, BuiltInParameter, double> valueOf = (element, which) =>
    {
        var parameter = element.get_Parameter(which);
        return parameter != null && parameter.HasValue && parameter.StorageType == StorageType.Double
            ? parameter.AsDouble() : 0;
    };

    // RIDES WITH SOMETHING ELSE: a door in its wall, a panel or a mullion in
    // its curtain wall, a nested family in its parent. Copying the host brings
    // it; turning or re-levelling it on its own would pull it off the host.
    // A level-based family's Host IS its level, which is not a host here.
    Func<Element, Element> hostOf = element =>
    {
        var instance = element as FamilyInstance;
        if (instance == null) return null;
        if (instance.SuperComponent != null) return instance.SuperComponent;
        var host = instance.Host;
        return host is Level ? null : host;
    };
    Func<Element, bool> rides = element =>
        element is Mullion || element is Panel || hostOf(element) != null;

    Func<Element, string> categoryOf = element =>
        element.Category != null ? element.Category.Name : element.GetType().Name;

    var given = new HashSet<ElementId>(source);
    var standing = new List<Element>();
    var withoutHost = new List<string>();
    var noLevel = new List<string>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (rides(element))
        {
            var host = hostOf(element);
            if (host == null || given.Contains(host.Id)) continue;
            withoutHost.Add(categoryOf(element));
            continue;
        }
        var rule = ruleFor(element);
        if (rule == null || writable(element, rule[0]) == null || levelIn(element, rule[0]) == null)
        {
            noLevel.Add(categoryOf(element));
            continue;
        }
        standing.Add(element);
    }

    if (withoutHost.Count > 0)
        problems.Add(withoutHost.Count + " element(s) sit in a host that was not given with them ("
            + string.Join(", ", withoutHost.Distinct()) + ") - give the walls or hosts too, and they come along.");
    if (noLevel.Count > 0)
        problems.Add(noLevel.Count + " element(s) have no level Heron knows how to change ("
            + string.Join(", ", noLevel.Distinct()) + "). This copies walls, curtain walls, floors, roofs, "
            + "ceilings, ducts, pipes and family instances placed on a level.");

    // THE ORIGINALS' LEVEL - the lowest one any of them stands on. Every target
    // is measured from it. Elements on a higher level keep their place above it.
    var originalLevels = new HashSet<ElementId>();
    Level reference = null;
    foreach (var element in standing)
    {
        var level = levelIn(element, ruleFor(element)[0]);
        originalLevels.Add(level.Id);
        if (reference == null || level.Elevation < reference.Elevation) reference = level;
    }
    foreach (var target in targets)
    {
        if (originalLevels.Contains(target.Id))
            problems.Add(target.Name + " is a level the originals already stand on - a copy there would sit on "
                + "top of them.");
    }

    if (problems.Count > 0)
    {
        refused = string.Join(" ", problems.Distinct()) + " Nothing was copied.";
    }
    else if (source.Count > 0 && standing.Count > 0)
    {
        // THE ORIGINALS' BOX IN PLAN, and each one's own box: the turn centre
        // when none was given, how far aside the copies are made, and how each
        // copy is matched back to its original.
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        var boxes = new Dictionary<ElementId, BoundingBoxXYZ>();
        foreach (var element in standing)
        {
            var box = element.get_BoundingBox(null);
            if (box == null)
                throw new InvalidOperationException("Revit gives a " + categoryOf(element) + " in the set no extent to "
                    + "measure, so its copies could not be checked. Nothing was copied.");
            boxes[element.Id] = box;
            minX = Math.Min(minX, box.Min.X); minY = Math.Min(minY, box.Min.Y);
            maxX = Math.Max(maxX, box.Max.X); maxY = Math.Max(maxY, box.Max.Y);
        }
        if (centre == null) centre = new XYZ((minX + maxX) / 2, (minY + maxY) / 2, 0);
        var axis = Line.CreateBound(centre, centre + XYZ.BasisZ);

        // ASIDE: a kilometre east of the originals' box, so no copy ever stands
        // on, in or against an original - see step 1.
        var aside = (maxX - minX) + 1000000.0 / mmPerFoot;
        const double samePlace = 2.0 / mmPerFoot;

        // A PLAN POINT FOR EVERY ORIGINAL THAT HAS ONE - a wall's start, a
        // duct's start, a family's insertion point. Turned by each level's
        // angle, it is where its copy must be found. Floors, roofs and ceilings
        // have no single point and are checked by their level and height alone.
        Func<Element, XYZ> pointOf = element =>
        {
            var at = element.Location as LocationPoint;
            if (at != null) return at.Point;
            var along = element.Location as LocationCurve;
            if (along != null && along.Curve != null) return along.Curve.GetEndPoint(0);
            return null;
        };

        // WRITTEN ONLY WHEN IT DIFFERS - see step 3.
        Func<Element, BuiltInParameter, ElementId, bool> putLevel = (element, which, id) =>
        {
            var held = element.get_Parameter(which);
            if (held != null && held.StorageType == StorageType.ElementId && held.AsElementId() == id) return true;
            var parameter = writable(element, which);
            return parameter != null && parameter.Set(id);
        };
        Func<Element, BuiltInParameter, double, bool> putLength = (element, which, value) =>
        {
            var held = element.get_Parameter(which);
            if (held != null && held.StorageType == StorageType.Double && held.HasValue
                && Math.Abs(held.AsDouble() - value) < 1e-9) return true;
            var parameter = writable(element, which);
            return parameter != null && parameter.Set(value);
        };

        // A FLOOR PLAN ON A LEVEL - the model's own when it has one, else one
        // made for the copy and deleted straight after it (step 1).
        var floorPlanType = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>().FirstOrDefault(t => t.ViewFamily == ViewFamily.FloorPlan);
        var temporaryPlans = new List<ElementId>();
        Func<Level, ViewPlan> planOn = level =>
        {
            var own = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                .FirstOrDefault(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan
                    && v.GenLevel != null && v.GenLevel.Id == level.Id);
            if (own != null) return own;
            if (floorPlanType == null)
                throw new InvalidOperationException("This model has no floor plan type to make a plan on " + level.Name
                    + " with, and Revit copies onto a level from one plan to another. Nothing was copied.");
            var plan = ViewPlan.Create(doc, floorPlanType.Id, level.Id);
            temporaryPlans.Add(plan.Id);
            return plan;
        };
        Action dropTemporaryPlans = () =>
        {
            foreach (var id in temporaryPlans) if (doc.GetElement(id) != null) doc.Delete(id);
            temporaryPlans.Clear();
        };
        var fromPlan = planOn(reference);

        var lines = new List<string>();
        lines.Add("From " + reference.Name + " onto " + targets.Count + " level(s)"
            + (Math.Abs(turn) > 1e-12
                ? ", each turned " + turn.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)
                  + " deg more than the one before, anticlockwise, about (" + mm(centre.X) + ", " + mm(centre.Y) + ") mm"
                : ", not turned")
            + ". " + standing.Count + " element(s) of " + source.Count + " given stand on a level; the rest "
            + "ride with them.");

        var unconnectedTops = 0;
        var columnTops = 0;

        for (var place = 0; place < targets.Count; place++)
        {
            var target = targets[place];
            var rise = target.Elevation - reference.Elevation;
            var degrees = turn * (place + 1);
            var radians = degrees * Math.PI / 180.0;

            // 1. COPY, PLAN TO PLAN AND ASIDE - see the method above.
            ICollection<ElementId> made;
            try
            {
                var toPlan = planOn(target);
                var toPlanId = toPlan.Id;
                made = copyOnWorkset(() => ElementTransformUtils.CopyElements(fromPlan, source, toPlan,
                    Transform.CreateTranslation(new XYZ(aside, 0, 0)), new CopyPasteOptions()));
                if (temporaryPlans.Contains(toPlanId))
                {
                    // Read before the delete: a deleted view's own Id cannot be.
                    temporaryPlans.Remove(toPlanId);
                    doc.Delete(toPlanId);
                }
            }
            catch (Exception failure)
            {
                throw new InvalidOperationException("Revit would not copy them onto " + target.Name + ": "
                    + failure.Message + " Nothing was kept - the copies on the levels before it were taken back too.");
            }
            if (made == null || made.Count == 0)
                throw new InvalidOperationException("Revit made no copies on " + target.Name
                    + ". Nothing was kept - the copies on the levels before it were taken back too.");

            copies.AddRange(made);
            notCopied += Math.Max(0, source.Count - made.Count);
            doc.Regenerate();

            // THE COPIES THAT STAND ON A LEVEL - one per original that does.
            // Whatever else Revit made rides with them: a door, a panel, a
            // curtain grid line, a slab edge. Fewer than the originals means
            // Revit dropped one, and a storey with a slab missing is stopped
            // here rather than reported as a storey.
            var own = made.Select(id => doc.GetElement(id))
                .Where(e => e != null && !rides(e) && ruleFor(e) != null).ToList();
            if (own.Count != standing.Count)
                throw new InvalidOperationException("Revit made " + own.Count + " copies on " + target.Name + " of the "
                    + standing.Count + " element(s) that stand on a level. Nothing was kept - the copies on the "
                    + "levels before it were taken back too.");

            // 2. EACH COPY MATCHED TO ITS ORIGINAL: the same category, the same
            // type, the same outline in plan less the step aside. Two alike in
            // plan are told apart by height, lowest with lowest.
            var unmatched = new List<Element>(standing);
            var pairs = new List<Tuple<Element, Element, Level>>();
            foreach (var copy in own.OrderBy(e => e.get_BoundingBox(null) == null ? 0 : e.get_BoundingBox(null).Min.Z))
            {
                var box = copy.get_BoundingBox(null);
                var match = box == null ? null : unmatched
                    .Where(o => o.GetTypeId() == copy.GetTypeId() && categoryOf(o) == categoryOf(copy)
                        && Math.Abs(box.Min.X - aside - boxes[o.Id].Min.X) < samePlace
                        && Math.Abs(box.Max.X - aside - boxes[o.Id].Max.X) < samePlace
                        && Math.Abs(box.Min.Y - boxes[o.Id].Min.Y) < samePlace
                        && Math.Abs(box.Max.Y - boxes[o.Id].Max.Y) < samePlace)
                    .OrderBy(o => boxes[o.Id].Min.Z).FirstOrDefault();
                if (match == null)
                    throw new InvalidOperationException("A copy of a " + categoryOf(copy) + " on " + target.Name
                        + " came out a different shape from every original. Nothing was kept.");
                unmatched.Remove(match);

                // The level a copy belongs on: as far above its original's level
                // as the target is above the originals'. For everything on the
                // originals' level that is the target itself.
                var from = levelIn(match, ruleFor(match)[0]);
                var wanted = from.Id == reference.Id ? target : (levelAt(from.Elevation + rise) ?? target);
                pairs.Add(Tuple.Create(copy, match, wanted));
            }

            // 3. ONTO THE LEVEL, AT THE ORIGINAL'S OFFSET. Every height is the
            // original's own, one rise up; the level parameter is the wanted
            // level and the offset is what is left. Going up, the top is set
            // before the base, so the wall is never asked to end below where it
            // starts; going down, the other way round.
            foreach (var pair in pairs)
            {
                var copy = pair.Item1;
                var original = pair.Item2;
                var wanted = pair.Item3;
                var rule = ruleFor(original);
                var from = levelIn(original, rule[0]);

                var heights = new double[rule.Length - 3];
                for (var i = 3; i < rule.Length; i++) heights[i - 3] = from.Elevation + valueOf(original, rule[i]) + rise;

                var topFrom = levelIn(original, rule[1]);
                var topHeight = topFrom != null ? topFrom.Elevation + valueOf(original, rule[2]) + rise : 0;
                var topWanted = topFrom != null ? levelAt(topFrom.Elevation + rise) : null;

                Func<bool> putBase = () =>
                {
                    var done = putLevel(copy, rule[0], wanted.Id);
                    for (var i = 3; i < rule.Length && done; i++)
                        done = putLength(copy, rule[i], heights[i - 3] - wanted.Elevation);
                    return done;
                };
                Func<bool> putTop = () =>
                {
                    if (topFrom == null)
                    {
                        // An unconnected wall: the copy keeps the original's height.
                        if (!(copy is Wall)) return true;
                        var tie = writable(copy, BuiltInParameter.WALL_HEIGHT_TYPE);
                        if (tie != null && tie.AsElementId() != ElementId.InvalidElementId
                            && !tie.Set(ElementId.InvalidElementId)) return false;
                        return putLength(copy, BuiltInParameter.WALL_USER_HEIGHT_PARAM,
                            valueOf(original, BuiltInParameter.WALL_USER_HEIGHT_PARAM));
                    }
                    if (topWanted != null)
                        return putLevel(copy, rule[1], topWanted.Id)
                            && putLength(copy, rule[2], topHeight - topWanted.Elevation);
                    if (copy is Wall)
                    {
                        // No level that far above the new base - the top of the
                        // building. The wall keeps its height, unconnected.
                        unconnectedTops++;
                        return putLevel(copy, BuiltInParameter.WALL_HEIGHT_TYPE, ElementId.InvalidElementId)
                            && putLength(copy, BuiltInParameter.WALL_USER_HEIGHT_PARAM, topHeight - heights[0]);
                    }
                    // A column's top must be on a level: its own base level,
                    // raised by the column's height.
                    columnTops++;
                    return putLevel(copy, rule[1], wanted.Id) && putLength(copy, rule[2], topHeight - wanted.Elevation);
                };

                var written = rise >= 0 ? putTop() && putBase() : putBase() && putTop();
                if (!written)
                    throw new InvalidOperationException("Revit would not put a copy of a " + categoryOf(copy)
                        + " on " + wanted.Name + ". Nothing was kept.");
            }

            // 3a. VERSION 3 - ONTO THE WORKSET ASKED FOR, while every copy
            // still stands a kilometre aside of its original in plan. Nothing
            // at all unless a workset was asked for.
            putOnWorksets(made, aside, 0);

            // 4. BACK OVER THE ORIGINALS, AND TURNED. What rides follows its host.
            var ownIds = own.Select(e => e.Id).ToList();
            try
            {
                ElementTransformUtils.MoveElements(doc, ownIds, new XYZ(-aside, 0, 0));
                if (Math.Abs(radians) > 1e-12) ElementTransformUtils.RotateElements(doc, ownIds, axis, radians);
            }
            catch (Exception failure)
            {
                throw new InvalidOperationException("Revit would not bring the copies on " + target.Name
                    + " into place: " + failure.Message + " Nothing was kept.");
            }
            doc.Regenerate();

            // 5. READ BACK, each copy against its own original: its level, its
            // height one rise above the original's, its offset, its plan point.
            var onLevel = 0;
            var atOffset = 0;
            var withPoint = 0;
            var placed = 0;
            foreach (var pair in pairs)
            {
                var copy = pair.Item1;
                var original = pair.Item2;
                var rule = ruleFor(original);

                var level = levelIn(copy, rule[0]);
                if (level == null || level.Id != pair.Item3.Id)
                    throw new InvalidOperationException("A copy of a " + categoryOf(copy) + " did not take " + pair.Item3.Name
                        + " as its level. Nothing was kept.");
                if (level.Id == target.Id) onLevel++;

                var box = copy.get_BoundingBox(null);
                var was = boxes[original.Id];
                var off = box == null ? double.MaxValue
                    : Math.Max(Math.Abs(box.Min.Z - (was.Min.Z + rise)), Math.Abs(box.Max.Z - (was.Max.Z + rise)));
                if (off > stayedPut)
                    throw new InvalidOperationException("A copy of a " + categoryOf(copy) + " on " + level.Name + " is "
                        + (box == null ? "not measurable" : mm(off) + " mm away from " + mm(rise) + " mm above its original")
                        + ". Nothing was kept.");

                if (Math.Abs(valueOf(copy, rule[3]) - valueOf(original, rule[3])) < stayedPut) atOffset++;

                var p = pointOf(original);
                var q = pointOf(copy);
                if (p == null || q == null) continue;
                withPoint++;
                var dx = p.X - centre.X;
                var dy = p.Y - centre.Y;
                var x = centre.X + dx * Math.Cos(radians) - dy * Math.Sin(radians);
                var y = centre.Y + dx * Math.Sin(radians) + dy * Math.Cos(radians);
                if (Math.Abs(q.X - x) < samePlace && Math.Abs(q.Y - y) < samePlace) placed++;
            }

            // AND WHAT RIDES ON THEM: a curtain wall's panels and mullions, a
            // door in its wall. Each should read the level its host now has -
            // the thing a re-levelled curtain wall gets wrong (step 1).
            var parts = 0;
            var partsOff = 0;
            foreach (var pair in pairs)
            {
                var hostLevel = levelIn(pair.Item1, ruleFor(pair.Item2)[0]);
                var wall = pair.Item1 as Wall;
                var riding = new List<ElementId>();
                if (wall != null && wall.CurtainGrid != null)
                {
                    riding.AddRange(wall.CurtainGrid.GetPanelIds());
                    riding.AddRange(wall.CurtainGrid.GetMullionIds());
                }
                riding.AddRange(made.Where(id =>
                {
                    var e = doc.GetElement(id);
                    var host = e == null ? null : hostOf(e);
                    return host != null && host.Id == pair.Item1.Id;
                }));
                foreach (var id in riding.Distinct())
                {
                    var part = doc.GetElement(id);
                    if (part == null || part.LevelId == ElementId.InvalidElementId) continue;
                    parts++;
                    if (part.LevelId != hostLevel.Id) partsOff++;
                }
            }

            lines.Add(target.Name + " (" + (rise >= 0 ? "+" : "") + mm(rise) + " mm): " + made.Count + " copied; "
                + onLevel + " of " + own.Count + " belong to " + target.Name + ", " + atOffset
                + " at their original's offset, each exactly " + mm(rise) + " mm above its original; " + placed + " of " + withPoint
                + " plan point(s) where "
                + (Math.Abs(radians) > 1e-12
                    ? "a turn of " + degrees.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " deg"
                    : "no turn")
                + " puts them"
                + (parts > 0
                    ? (partsOff == 0
                        ? "; all " + parts + " panel(s), mullion(s) and hosted part(s) on their host's level"
                        : "; " + partsOff + " of " + parts + " panel(s), mullion(s) and hosted part(s) still read ANOTHER level than their host")
                    : "")
                + ".");
        }
        dropTemporaryPlans();
        readBackWorksets();

        if (unconnectedTops > 0)
            lines.Add(unconnectedTops + " wall copy(ies) had no level as far above their new base as their "
                + "original's top was tied to, so each keeps its height with its top unconnected.");
        if (columnTops > 0)
            lines.Add(columnTops + " column copy(ies) had no level for their top, so each top is tied to its own "
                + "base level, raised by the column's height.");

        copiedTo = string.Join("\n", lines);
    }
}
