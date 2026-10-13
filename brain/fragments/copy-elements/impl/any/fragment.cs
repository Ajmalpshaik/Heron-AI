// NOT STANDALONE. Assumes `doc`, `elements`, `offset`, `toLevels`,
// `turnDegrees` and `turnCentreMm` are in scope; leaves `copies`, `notCopied`,
// `copiedTo` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `offset` is in internal FEET.
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
    else if (source.Count > 0)
    {
        // VERSION 1, UNCHANGED.
        var made = ElementTransformUtils.CopyElements(doc, source, offset);
        if (made != null) copies.AddRange(made);

        // Fewer back than went in. Not an error and not silence either - the
        // caller needs to know the lists do not line up before it starts pairing
        // originals with copies.
        notCopied = source.Count - copies.Count;
        if (notCopied < 0) notCopied = 0;
    }
}
else
{
    // ------------------------------------------------------------------
    // ONTO LEVELS.
    //
    // THE METHOD, AND WHY EACH STEP IS THERE:
    //
    //  1. Copy the whole set straight up by the height from the originals'
    //     level to the target - one ElementTransformUtils call, so joins and
    //     hosted elements come along exactly as in the offset path. Revit
    //     treats that like moving them up in an elevation: each copy stays on
    //     the ORIGINAL's level with its offset raised by the height. That is
    //     the state this fragment exists to not leave behind.
    //  2. Turn the copies about a vertical line through the centre, by the
    //     turn times the level's place in the list - the first level named
    //     one turn, the second two. Only elements standing on their own are
    //     turned; a door, a panel or a mullion follows its wall.
    //  3. Put each copy ON its level: the level parameter set to it and the
    //     offset worked out again so the copy does not move - absolute height
    //     read first, offset written as that height less the new level's.
    //     So the copy's offset is the original's offset, by arithmetic rather
    //     than by trust. A wall tied to the level above is tied to the level
    //     the same height above its new base; with none there it becomes
    //     unconnected at the same height.
    //  4. Read every copy back after Revit has regenerated: its level must be
    //     the one written and its bounding box must not have moved up or down
    //     by more than half a millimetre. A copy that fails either takes the
    //     WHOLE call back with it (the exception below), because a tower with
    //     one storey left floating on the ground floor's level is worse than
    //     no tower.
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
        // THE CENTRE, when none was given: the middle of the originals' box in
        // plan. Worked out before anything is copied, so every level turns about
        // the same point.
        if (centre == null)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var element in standing)
            {
                var box = element.get_BoundingBox(null);
                if (box == null) continue;
                minX = Math.Min(minX, box.Min.X); minY = Math.Min(minY, box.Min.Y);
                maxX = Math.Max(maxX, box.Max.X); maxY = Math.Max(maxY, box.Max.Y);
            }
            centre = minX <= maxX ? new XYZ((minX + maxX) / 2, (minY + maxY) / 2, 0) : XYZ.Zero;
        }
        var axis = Line.CreateBound(centre, centre + XYZ.BasisZ);

        // A PLAN POINT FOR EVERY ORIGINAL THAT HAS ONE - a wall's start, a
        // duct's start, a family's insertion point. Turned by each level's
        // angle, these are where the copies must be found. Floors, roofs and
        // ceilings have no single point and are checked by their level alone.
        Func<Element, XYZ> pointOf = element =>
        {
            var at = element.Location as LocationPoint;
            if (at != null) return at.Point;
            var along = element.Location as LocationCurve;
            if (along != null && along.Curve != null) return along.Curve.GetEndPoint(0);
            return null;
        };
        Func<double, double, string> key = (x, y) =>
            Math.Round(x * mmPerFoot).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + ","
            + Math.Round(y * mmPerFoot).ToString("0", System.Globalization.CultureInfo.InvariantCulture);

        var originalPoints = standing.Select(pointOf).Where(p => p != null).ToList();
        var originalOffsets = standing
            .Select(e => categoryOf(e) + "@" + mm(valueOf(e, ruleFor(e)[3])))
            .OrderBy(s => s, StringComparer.Ordinal).ToList();

        var lines = new List<string>();
        lines.Add("From " + reference.Name + " onto " + targets.Count + " level(s)"
            + (Math.Abs(turn) > 1e-12
                ? ", each turned " + turn.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)
                  + " deg more than the one before, anticlockwise, about (" + mm(centre.X) + ", " + mm(centre.Y) + ") mm"
                : ", not turned")
            + ". " + standing.Count + " element(s) of " + source.Count + " given stand on a level; the rest "
            + "ride with them.");

        var unconnectedTops = 0;
        var keptTops = 0;

        for (var place = 0; place < targets.Count; place++)
        {
            var target = targets[place];
            var rise = target.Elevation - reference.Elevation;
            var degrees = turn * (place + 1);
            var radians = degrees * Math.PI / 180.0;

            // 1. COPY, straight up.
            ICollection<ElementId> made;
            try
            {
                made = ElementTransformUtils.CopyElements(doc, source, new XYZ(0, 0, rise));
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

            // THE COPIES THAT STAND ON A LEVEL - one per original that does.
            // Whatever else Revit made rides with them: a door, a panel, a
            // curtain grid line, a slab edge. Fewer than the originals means
            // Revit dropped one, and a storey with a slab missing is stopped
            // here rather than reported as a storey.
            var arrived = made.Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
            var own = arrived.Where(e => !rides(e) && ruleFor(e) != null).ToList();
            if (own.Count != standing.Count)
                throw new InvalidOperationException("Revit made " + own.Count + " copies on " + target.Name + " of the "
                    + standing.Count + " element(s) that stand on a level. Nothing was kept - the copies on the "
                    + "levels before it were taken back too.");

            // 2. TURN, the ones standing on their own; what rides follows.
            if (Math.Abs(radians) > 1e-12 && own.Count > 0)
            {
                try
                {
                    ElementTransformUtils.RotateElements(doc, own.Select(e => e.Id).ToList(), axis, radians);
                }
                catch (Exception failure)
                {
                    throw new InvalidOperationException("Revit would not turn the copies on " + target.Name + ": "
                        + failure.Message + " Nothing was kept.");
                }
            }
            doc.Regenerate();

            // 3. ONTO THE LEVEL, without moving. Every value read before any is
            // written, so nothing written can change what is read.
            var watched = new List<Tuple<Element, ElementId, double, double>>();
            foreach (var copy in own)
            {
                var rule = ruleFor(copy);
                var now = rule == null ? null : levelIn(copy, rule[0]);
                if (now == null)
                    throw new InvalidOperationException("A copy of a " + categoryOf(copy) + " on " + target.Name
                        + " has no level Heron can change, though its original had. Nothing was kept.");

                // The level a copy belongs on: the one as far above its own
                // level as the target is above the originals'. For everything
                // on the originals' level that is the target itself.
                var wanted = originalLevels.Contains(now.Id) ? levelAt(now.Elevation + rise) : null;
                if (wanted == null) wanted = target;

                var heights = new double[rule.Length - 3];
                for (var i = 3; i < rule.Length; i++) heights[i - 3] = now.Elevation + valueOf(copy, rule[i]);

                var topNow = levelIn(copy, rule[1]);
                var topHeight = topNow != null ? topNow.Elevation + valueOf(copy, rule[2]) : 0;

                var box = copy.get_BoundingBox(null);
                watched.Add(Tuple.Create(copy, wanted.Id,
                    box != null ? box.Min.Z : double.NaN, box != null ? box.Max.Z : double.NaN));

                var written = copy.get_Parameter(rule[0]).Set(wanted.Id);
                for (var i = 3; i < rule.Length && written; i++)
                {
                    var offsetParameter = writable(copy, rule[i]);
                    written = offsetParameter != null && offsetParameter.Set(heights[i - 3] - wanted.Elevation);
                }

                if (written && topNow != null)
                {
                    var topWanted = levelAt(topNow.Elevation + rise);
                    if (topWanted != null)
                    {
                        var topParameter = writable(copy, rule[1]);
                        written = topParameter != null && topParameter.Set(topWanted.Id);
                        var topOffset = written ? writable(copy, rule[2]) : null;
                        written = written && topOffset != null && topOffset.Set(topHeight - topWanted.Elevation);
                    }
                    else if (copy is Wall)
                    {
                        // No level that far above the new base - the top of the
                        // building. The wall keeps its height, unconnected.
                        var tie = writable(copy, BuiltInParameter.WALL_HEIGHT_TYPE);
                        written = tie != null && tie.Set(ElementId.InvalidElementId);
                        var height = written ? writable(copy, BuiltInParameter.WALL_USER_HEIGHT_PARAM) : null;
                        written = written && height != null && height.Set(topHeight - heights[0]);
                        unconnectedTops++;
                    }
                    else
                    {
                        // A column's top must be on a level. Left on the one it
                        // has, at the height it has - the copy is still whole.
                        keptTops++;
                    }
                }

                if (!written)
                    throw new InvalidOperationException("Revit would not put a copy of a " + categoryOf(copy)
                        + " on " + wanted.Name + ". Nothing was kept.");
            }
            doc.Regenerate();

            // 4. READ BACK. Its level, and that it did not move.
            var onLevel = 0;
            foreach (var check in watched)
            {
                var copy = check.Item1;
                var rule = ruleFor(copy);
                var level = rule == null ? null : levelIn(copy, rule[0]);
                if (level == null || level.Id != check.Item2)
                    throw new InvalidOperationException("A copy of a " + categoryOf(copy) + " did not take the level "
                        + "it was given on " + target.Name + ". Nothing was kept.");
                var box = copy.get_BoundingBox(null);
                if (box != null && !double.IsNaN(check.Item3)
                    && (Math.Abs(box.Min.Z - check.Item3) > stayedPut || Math.Abs(box.Max.Z - check.Item4) > stayedPut))
                    throw new InvalidOperationException("A copy of a " + categoryOf(copy) + " moved "
                        + mm(Math.Max(Math.Abs(box.Min.Z - check.Item3), Math.Abs(box.Max.Z - check.Item4)))
                        + " mm up or down when it was put on " + level.Name + ". Nothing was kept.");
                if (level.Id == target.Id) onLevel++;
            }

            // The originals' offsets, kind by kind, against the copies'.
            var copyOffsets = own
                .Select(e => categoryOf(e) + "@" + mm(valueOf(e, ruleFor(e)[3])))
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
            var sameOffsets = copyOffsets.SequenceEqual(originalOffsets);

            // Where the turn should have put each point, against where they are.
            var expected = new List<string>();
            foreach (var p in originalPoints)
            {
                var dx = p.X - centre.X;
                var dy = p.Y - centre.Y;
                expected.Add(key(centre.X + dx * Math.Cos(radians) - dy * Math.Sin(radians),
                                 centre.Y + dx * Math.Sin(radians) + dy * Math.Cos(radians)));
            }
            var found = own.Select(pointOf).Where(p => p != null).Select(p => key(p.X, p.Y)).ToList();
            var placed = 0;
            foreach (var point in expected)
            {
                if (found.Remove(point)) placed++;
            }

            lines.Add(target.Name + " (" + (rise >= 0 ? "+" : "") + mm(rise) + " mm): " + made.Count + " copied; "
                + onLevel + " of " + own.Count + " now belong to " + target.Name
                + (sameOffsets ? " at their originals' offsets" : " - their offsets DIFFER from the originals'")
                + "; " + placed + " of " + expected.Count + " plan point(s) where "
                + (Math.Abs(radians) > 1e-12
                    ? "a turn of " + degrees.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " deg"
                    : "no turn")
                + " puts them.");
        }

        if (unconnectedTops > 0)
            lines.Add(unconnectedTops + " wall copy(ies) had no level as far above their new base as their top "
                + "was tied to, so each keeps its height with its top unconnected.");
        if (keptTops > 0)
            lines.Add(keptTops + " column copy(ies) had no level for their top, so each top stays on the level "
                + "it had, at the same height.");

        copiedTo = string.Join("\n", lines);
    }
}
