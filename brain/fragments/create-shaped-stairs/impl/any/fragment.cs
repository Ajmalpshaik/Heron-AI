// NOT STANDALONE. Assumes `doc`, `editScopeFailures`, `baseLevel`, `topLevel`,
// `stairType`, `shape`, `startPoint`, `direction`, `turn`, `runWidth`,
// `landingDepth`, `radius` and `firstRunRisers`; leaves `created`, `risers`,
// `riserHeightMm`, `treadDepthMm`, `runCount`, `landingCount`,
// `landingDepthMm`, `railings` and `findings`.
//
// ===========================================================================
// THIS FRAGMENT OPENS ITS OWN TRANSACTIONS, AND ONLY BECAUSE IT HAS TO - D-112.
// ===========================================================================
//
// The pattern is CREATE_STAIRS's (FRG-ELE-076), followed exactly: a stair is
// made only inside a StairsEditScope, which Revit refuses to start while a
// transaction is open. Declaring `editScopeFailures` makes the add-in run this
// inside its TransactionGroup with NO transaction open, so the job is still one
// undo and a preview still rolls all of it back. Every transaction opened here
// takes `editScopeFailures`, so what Revit says reaches the verdict.
//
// THE SHAPES - Stair by Component, placed from a point rather than fitted to
// a stair hall (fitting a straight or U stair to a hall is CREATE_STAIRS's):
//
//   l       run 1 climbs `direction` from `startPoint` (the middle of its
//           first riser); a landing at its top; run 2 leaves the landing to
//           the `turn` side, its outer edge flush with the landing's far edge
//   u       run 1 as above; a landing across the far end; run 2 comes back
//           alongside run 1 on the `turn` side, no gap between the runs
//   spiral  one curved run round `startPoint` (the CENTRE), `radius` to the
//           run's centre line; the first riser on the `direction` side of the
//           centre; `turn` left is anticlockwise seen from above, right is
//           clockwise
//
// LENGTHS: `runWidth`, `landingDepth` and `radius` arrive in MILLIMETRES and
// become feet by / 304.8 (D-20); `startPoint` arrives in feet - the add-in
// reads a point as three numbers in millimetres and hands in an XYZ.
//
// THE RISERS ARE REVIT'S RULES, NOT A NUMBER TYPED HERE: the fewest that keep
// every riser at or under the stair type's Maximum Riser Height, the tread its
// Minimum Tread Depth. On an L or U run 1 takes the larger half unless
// `firstRunRisers` says otherwise. What Revit made - risers, tread, runs,
// landings, the landing's depth - is read back inside the edit scope, and a
// miss cancels it.
//
// THE TWO ELEVATION FRAMES, from CREATE_STAIRS (measured on Revit 2020.2,
// 5b-445): a run's path Z is in MODEL coordinates; a run's TopElevation is
// RELATIVE to the stair's base, and is set only when the path did not already
// give it. A landing's base elevation is relative to the stair's base too
// (RevitAPI.xml, 2020 to 2027). A spiral run's centre Z is model coordinates.
//
// NOT MEASURED ON ANY REVIT YET (DRAFT): the sketched landing (asked for only
// when `landingDepth` is given), whose boundary is drawn at the base level's
// elevation as GetFootprintBoundary reports one; and the spiral's included
// angle, laid out as (risers - 1) treads along its centre line, the rule
// CREATE_STAIRS measured for a straight run. The read-back decides either way.

var created = ElementId.InvalidElementId;
var risers = 0;
var riserHeightMm = 0.0;
var treadDepthMm = 0.0;
var runCount = 0;
var landingCount = 0;
var landingDepthMm = 0.0;
var railings = new List<ElementId>();
string railingNote = null;
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
var tolerance = 5.0 / 304.8;

Action<Transaction> discipline = t =>
{
    var options = t.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(editScopeFailures);
    options.SetClearAfterRollback(true);
    t.SetFailureHandlingOptions(options);
};

string refused = null;
var form = (shape ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace(" ", "").Replace("shaped", "").Replace("shape", "");
if (form == "dogleg") form = "u";
var heading = (direction ?? "").Trim().ToLowerInvariant();
var hand = (turn ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace(" ", "");
if (hand == "clockwise") hand = "right";
if (hand == "anticlockwise" || hand == "counterclockwise") hand = "left";

// ---------------------------------------------------------------------------
// 1. EVERYTHING THAT CAN BE REFUSED BEFORE THE MODEL IS TOUCHED.
// ---------------------------------------------------------------------------

// THE STAIR TYPE: the one named, or Revit's default for new stairs - and the
// answer says which. A name not loaded is refused with the loaded ones listed.
var wantedType = (stairType ?? "").Trim();
var usedDefault = wantedType.Length == 0 || wantedType.Equals("none", StringComparison.OrdinalIgnoreCase);
StairsType chosenType = null;
var typeNames = new List<string>();
foreach (StairsType candidate in new FilteredElementCollector(doc).OfClass(typeof(StairsType)))
{
    typeNames.Add(candidate.Name);
    if (!usedDefault && candidate.Name == wantedType) chosenType = candidate;
}
if (usedDefault)
    chosenType = doc.GetElement(doc.GetDefaultElementTypeId(ElementTypeGroup.StairsType)) as StairsType;

var width = runWidth / 304.8;
var depth = landingDepth / 304.8;
var centreRadius = radius / 304.8;

if (baseLevel == null || topLevel == null)
    refused = "Both a base level and a top level are needed - name them as the model's level list does";
else if (topLevel.ProjectElevation <= baseLevel.ProjectElevation + tolerance)
    refused = string.Format("'{0}' is not above '{1}' - a stair climbs from the base level to a "
        + "HIGHER top level", topLevel.Name, baseLevel.Name);
else if (chosenType == null)
    refused = usedDefault
        ? string.Format("This model has no default stair type set, so name one. The ones loaded "
            + "are: {0}", string.Join(", ", typeNames))
        : string.Format("There is no stair type called '{0}' in this model. The ones loaded are: {1}",
            stairType, string.Join(", ", typeNames));
else if (form != "l" && form != "u" && form != "spiral")
    refused = string.Format("'{0}' is not a shape this makes - say L, U or spiral. A straight stair, "
        + "or one fitted to the walls of a stair hall, is made by asking for the stair in the stair hall",
        shape);
else if (startPoint == null)
    refused = "No point was given to place the stair from";
else if (heading != "north" && heading != "south" && heading != "east" && heading != "west")
    refused = string.Format("'{0}' is not a direction - say north, south, east or west: the way the "
        + "first run climbs, or on a spiral the side of the centre its first riser is on", direction);
else if (hand != "left" && hand != "right")
    refused = string.Format("'{0}' is not a turn - say left or right", turn);
else if (runWidth <= 0 || landingDepth < 0 || radius < 0 || firstRunRisers < 0)
    refused = "The run width must be more than zero, and the landing depth, radius and first-run risers "
        + "cannot be negative";
else if (width < chosenType.MinRunWidth - tolerance)
    refused = string.Format("A {0} mm run is narrower than '{1}' allows - its Minimum Run Width is {2} mm",
        runWidth, chosenType.Name, mm(chosenType.MinRunWidth));
else if (form == "spiral" && radius <= 0)
    refused = "A spiral stair needs its radius - to the centre line of the run, in mm. It is never guessed";
else if (form == "spiral" && centreRadius <= width / 2 + tolerance)
    refused = string.Format("A {0} mm radius to the centre of a {1} mm run puts the run's inside edge "
        + "through the centre - the radius must be more than half the run width", radius, runWidth);
else if (form == "l" && depth > 0 && depth < width - tolerance)
    refused = string.Format("On an L the landing must be at least as deep as the run is wide ({0} mm) "
        + "for the second run to leave it - {1} mm was asked", runWidth, landingDepth);

// THE PLAN DIRECTIONS. `along` climbs run 1; `side` points to the turn.
var along = heading == "north" ? XYZ.BasisY : heading == "south" ? -XYZ.BasisY
          : heading == "east" ? XYZ.BasisX : -XYZ.BasisX;
var side = hand == "left" ? new XYZ(-along.Y, along.X, 0) : new XYZ(along.Y, -along.X, 0);

var height = 0.0; var tread = 0.0; var riser = 0.0;
var total = 0; var first = 0; var second = 0;

// THE RISERS AND HOW THEY SPLIT for n risers: sets total, riser, first and
// second, and answers null or why n will not do.
Func<int, string> layoutFor = n =>
{
    total = n;
    riser = height / total;
    if (form == "spiral") { first = total; second = 0; return null; }
    first = firstRunRisers > 0 ? firstRunRisers : (total + 1) / 2;
    second = total - first;
    if (first < 2 || second < 2)
        return string.Format("{0} risers reach '{1}' and each of the two runs needs at least 2 - "
            + "run 1 was to have {2}", total, topLevel.Name, first);
    return null;
};

if (refused == null)
{
    height = topLevel.ProjectElevation - baseLevel.ProjectElevation;
    tread = chosenType.MinTreadDepth;
    refused = layoutFor((int)Math.Ceiling(height / chosenType.MaxRiserHeight - 1e-9));
}

// ---------------------------------------------------------------------------
// 2. THE STAIR, inside its edit scope.
// ---------------------------------------------------------------------------

// REVIT'S OWN WORDS FOR A REFUSAL, from the add-in's failure discipline, as
// CREATE_STAIRS reads them (5b-1240). An older add-in gives none.
Func<string> revitSaid = () =>
{
    var text = editScopeFailures == null ? null : editScopeFailures.ToString();
    return text != null && text.StartsWith("Revit would not let it through", StringComparison.Ordinal)
        ? " - " + text : "";
};

// A REFUSED STAIR LEAVES NOTHING (CREATE_STAIRS, 5b-1241): every id before,
// and whatever is new after a refusal is deleted again.
var idsBefore = new HashSet<ElementId>();
Action<HashSet<ElementId>> allIds = into =>
{
    foreach (var id in new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElementIds()) into.Add(id);
    foreach (var id in new FilteredElementCollector(doc).WhereElementIsElementType().ToElementIds()) into.Add(id);
};

// The landing's depth along run 1, read off its own footprint.
Func<StairsLanding, double> depthOf = landing =>
{
    var low = double.MaxValue; var high = double.MinValue;
    foreach (var curve in landing.GetFootprintBoundary())
        for (var end = 0; end < 2; end++)
        {
            var reach = curve.GetEndPoint(end).DotProduct(along);
            low = Math.Min(low, reach); high = Math.Max(high, reach);
        }
    return high > low ? high - low : 0.0;
};

// A rectangle on the base level: `a` along run 1 from `origin`, `c` to the turn side.
Func<XYZ, double, double, double, double, CurveLoop> rectangle = (origin, a0, a1, c0, c1) =>
{
    var corners = new[]
    {
        origin + along * a0 + side * c0, origin + along * a1 + side * c0,
        origin + along * a1 + side * c1, origin + along * a0 + side * c1,
    };
    var loop = new CurveLoop();
    for (var i = 0; i < 4; i++) loop.Append(Line.CreateBound(corners[i], corners[(i + 1) % 4]));
    return loop;
};

string madeNote = null;
string riserNote = null;
var riserRetried = false;

if (refused == null)
{
    allIds(idsBefore);

    using (var scope = new StairsEditScope(doc, "Heron: create shaped stairs"))
    {
        if (!scope.IsPermitted)
        {
            refused = "Revit will not open a stair for editing at this moment - another edit mode is "
                + "active, or a transaction is open around this fragment (D-112)";
        }
        else
        {
            var stairsId = scope.Start(baseLevel.Id, topLevel.Id);

            for (var pass = 1; pass <= 2 && refused == null; pass++)
            {
                var again = false;
                using (var transaction = new Transaction(doc, "Heron: stair runs"))
                {
                    transaction.Start();
                    discipline(transaction);
                    try
                    {
                        var stairs = (Stairs)doc.GetElement(stairsId);
                        if (stairs.GetTypeId() != chosenType.Id) stairs.ChangeTypeId(chosenType.Id);
                        stairs.DesiredRisersNumber = total;

                        var z = baseLevel.ProjectElevation;
                        var justify = StairsRunJustification.Center;
                        Action<StairsRun, double> topAt = (run, top) =>
                        {
                            if (Math.Abs(run.TopElevation - top) > tolerance) run.TopElevation = top;
                        };
                        var p0 = new XYZ(startPoint.X, startPoint.Y, z);
                        var turnDegrees = 0.0;

                        if (form == "spiral")
                        {
                            var startAngle = Math.Atan2(along.Y, along.X);
                            var included = Math.Max(total - 1, 1) * tread / centreRadius;
                            turnDegrees = included * 180.0 / Math.PI;
                            var run = StairsRun.CreateSpiralRun(doc, stairsId, p0, centreRadius, startAngle,
                                included, hand == "right", justify);
                            run.ActualRunWidth = width;
                            topAt(run, height);
                        }
                        else
                        {
                            var length1 = Math.Max(first - 1, 1) * tread;
                            var length2 = Math.Max(second - 1, 1) * tread;
                            var p1 = p0 + along * length1;
                            var z2 = z + first * riser;
                            var lift = new XYZ(0, 0, z2 - z);

                            var run1 = StairsRun.CreateStraightRun(doc, stairsId, Line.CreateBound(p0, p1), justify);
                            run1.ActualRunWidth = width;
                            topAt(run1, first * riser);

                            XYZ q0;
                            if (form == "l")
                                q0 = p1 + along * ((depth > 0 ? depth : width) - width / 2) + side * (width / 2) + lift;
                            else
                                q0 = p1 + side * width + lift;
                            var q1 = form == "l" ? q0 + side * length2 : q0 - along * length2;
                            var run2 = StairsRun.CreateStraightRun(doc, stairsId, Line.CreateBound(q0, q1), justify);
                            run2.ActualRunWidth = width;
                            topAt(run2, height);

                            if (depth > 0)
                            {
                                // THE LANDING AS DEEP AS ASKED, sketched: along run 1 from its
                                // top riser, across from run 1's outer edge to the far edge of
                                // whatever leaves it.
                                var loop = form == "l"
                                    ? rectangle(p1, 0, depth, -width / 2, width / 2)
                                    : rectangle(p1, 0, depth, -width / 2, width * 1.5);
                                StairsLanding.CreateSketchedLanding(doc, stairsId, loop, first * riser);
                            }
                            else if (!StairsLanding.CanCreateAutomaticLanding(doc, run1.Id, run2.Id))
                                refused = "Revit cannot put a landing between the two runs as they were laid out";
                            else
                                StairsLanding.CreateAutomaticLanding(doc, run1.Id, run2.Id);
                        }

                        doc.Regenerate();

                        // THE READ-BACK, while the scope can still be cancelled.
                        if (refused == null)
                        {
                            var runs = stairs.GetStairsRuns();
                            var landings = stairs.GetStairsLandings();
                            runCount = runs.Count;
                            landingCount = landings.Count;
                            risers = stairs.ActualRisersNumber;
                            riserHeightMm = Math.Round(stairs.ActualRiserHeight * 304.8, 1);
                            treadDepthMm = Math.Round(stairs.ActualTreadDepth * 304.8, 1);
                            landingDepthMm = 0.0;
                            foreach (var landingId in landings)
                                landingDepthMm = Math.Max(landingDepthMm,
                                    Math.Round(depthOf((StairsLanding)doc.GetElement(landingId)) * 304.8, 1));

                            var perRun = new List<string>();
                            foreach (var runId in runs)
                                perRun.Add(((StairsRun)doc.GetElement(runId)).ActualRisersNumber.ToString(invariant));

                            var wantRuns = form == "spiral" ? 1 : 2;
                            var box = stairs.get_BoundingBox(null);

                            if (runCount != wantRuns)
                                refused = string.Format("Revit made {0} run(s) where {1} were laid out", runCount, wantRuns);
                            else if (form != "spiral" && landingCount < 1)
                                refused = "Revit made the two runs and no landing between them";
                            else if (risers != total)
                                refused = string.Format("The stair has {0} risers ({1} by run) where {2} reach "
                                    + "'{3}' - it would not arrive at the top level", risers,
                                    string.Join(" + ", perRun), total, topLevel.Name);
                            else if (depth > 0 && Math.Abs(landingDepthMm - landingDepth) > 5.0)
                                refused = string.Format("The landing Revit made is {0} mm deep where {1} mm was asked",
                                    landingDepthMm, landingDepth);
                            else
                                madeNote = string.Format("{0} stair of type '{1}'{2} from '{3}' to '{4}': {5} risers "
                                    + "of {6} mm ({7} by run), {8} mm treads, {9} mm run width, {10} landing(s){11}{12}"
                                    + ". Plan extent X {13}..{14}, Y {15}..{16} mm",
                                    form == "l" ? "L-shaped" : form == "u" ? "U-shaped" : "Spiral",
                                    chosenType.Name, usedDefault ? " (Revit's default for new stairs)" : "",
                                    baseLevel.Name, topLevel.Name, risers, riserHeightMm,
                                    string.Join(" + ", perRun), treadDepthMm, runWidth, landingCount,
                                    form == "spiral" ? "" : string.Format(" {0} mm deep, turning {1}", landingDepthMm, hand),
                                    form == "spiral" ? string.Format(", turning {0} degrees {1} round a {2} mm radius",
                                        Math.Round(turnDegrees, 1).ToString(invariant),
                                        hand == "right" ? "clockwise" : "anticlockwise", radius) : "",
                                    box == null ? "?" : mm(box.Min.X), box == null ? "?" : mm(box.Max.X),
                                    box == null ? "?" : mm(box.Min.Y), box == null ? "?" : mm(box.Max.Y));
                        }
                    }
                    catch (Exception ex)
                    {
                        refused = string.Format("Revit refused the stair: {0}", ex.Message);
                    }

                    if (refused != null)
                        transaction.RollBack();
                    else if (transaction.Commit() != TransactionStatus.Committed)
                    {
                        // ONE RISER MORE, ONCE - CREATE_STAIRS's version 4 (5b-1239): a riser
                        // count Revit would not make on one type was made with one more.
                        var said = revitSaid();
                        var was = total;
                        var why = riserRetried ? "" : layoutFor(was + 1);
                        if (!riserRetried && why == null)
                        {
                            riserRetried = true;
                            riserNote = string.Format("Revit would not make {0} risers of {1} mm with '{2}'{3}, so "
                                + "the stair was made with {4}", was, mm(height / was), chosenType.Name,
                                said.Length > 0 ? " (" + said.Substring(3) + ")" : "", total);
                            again = true;
                        }
                        else
                        {
                            if (!riserRetried) layoutFor(was);
                            refused = "Revit would not keep the stair's runs" + said
                                + (riserRetried ? string.Format(" - with one riser more ({0}) as well", total) : "");
                        }
                    }
                }
                if (!again) break;
            }

            if (refused == null)
            {
                scope.Commit(editScopeFailures);
                if (doc.GetElement(stairsId) as Stairs == null)
                    refused = "Revit did not keep the stair when its edit mode closed" + revitSaid();
                else
                {
                    created = stairsId;
                    findings.Add(madeNote);
                    if (riserNote != null) findings.Add(riserNote);

                    // THE RAILINGS REVIT ADDED - they arrive at the Commit (CREATE_STAIRS, 5b-455).
                    railings.AddRange(((Stairs)doc.GetElement(created)).GetAssociatedRailings());
                    var railingTypes = railings
                        .Select(id => doc.GetElement(id))
                        .Select(e => e == null ? null : doc.GetElement(e.GetTypeId()))
                        .Select(t => t == null ? "(no type)" : "'" + t.Name + "'")
                        .Distinct()
                        .ToList();
                    railingNote = railings.Count == 0
                        ? "Revit added no railings with the stair"
                        : string.Format("Revit added {0} railing{1} of type{2} {3} with the stair",
                            railings.Count, railings.Count == 1 ? "" : "s",
                            railingTypes.Count == 1 ? "" : "s", string.Join(", ", railingTypes));
                    findings.Add(railingNote);
                }
            }
            else
            {
                scope.Cancel();
            }
        }
    }
}

// ---------------------------------------------------------------------------
// 3. A REFUSED STAIR LEAVES NOTHING - see idsBefore.
// ---------------------------------------------------------------------------

if (refused != null && idsBefore.Count > 0)
{
    var idsNow = new HashSet<ElementId>();
    allIds(idsNow);
    var left = idsNow.Where(id => !idsBefore.Contains(id)).ToList();
    if (left.Count > 0)
    {
        using (var sweep = new Transaction(doc, "Heron: remove what the refused stair left"))
        {
            sweep.Start();
            discipline(sweep);
            foreach (var id in left)
            {
                if (doc.GetElement(id) == null) continue;
                // One Revit will not delete is counted just below, never assumed gone.
                try { doc.Delete(id); } catch (Exception) { }
            }
            if (sweep.Commit() != TransactionStatus.Committed)
                refused += " - and Revit would not remove what the attempt left" + revitSaid();
        }
        var stillThere = new HashSet<ElementId>();
        allIds(stillThere);
        var leftBehind = stillThere.Count(id => !idsBefore.Contains(id));
        if (leftBehind > 0)
            refused += string.Format(" - {0} element(s) it made could not be removed again: look in "
                + "Revit's undo list and press Undo on this job", leftBehind);
    }
}

if (refused != null)
{
    findings.Add("Nothing was made. " + refused);
    created = ElementId.InvalidElementId;
    risers = 0; riserHeightMm = 0; treadDepthMm = 0; runCount = 0; landingCount = 0; landingDepthMm = 0;
    if (railingNote != null) findings.Remove(railingNote);
    railings.Clear();
}
