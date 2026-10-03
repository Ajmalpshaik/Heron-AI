// NOT STANDALONE. Assumes `doc`, `editScopeFailures`, `baseLevel`, `topLevel`,
// `stairType`, `shape`, `runWidth`, `area`, `entrySide`, `firstRunSide`,
// `startOffset` and `cutOpening`; leaves `created`, `risers`, `riserHeightMm`,
// `runCount`, `landingCount`, `openings` and `findings`.
//
// ===========================================================================
// THIS FRAGMENT OPENS ITS OWN TRANSACTIONS, AND ONLY BECAUSE IT HAS TO - D-112.
// ===========================================================================
//
// A stair is made only inside a StairsEditScope, and Revit refuses to start
// one while a transaction is open - measured 2026-10-03 on Project2, Revit
// 2024, inside the add-in's own transaction: "not permitted to start ... the
// document is currently modifiable". Declaring `editScopeFailures` makes the
// add-in run this inside its TransactionGroup with NO transaction open, so
// the whole job is still one undo and a preview still rolls all of it back.
// Every transaction opened here takes `editScopeFailures`, so what Revit says
// reaches the verdict exactly as it would from the add-in's own transaction.
//
// LENGTHS: `runWidth` and `startOffset` arrive in MILLIMETRES and become feet
// by / 304.8 (D-20); the corners in `area` arrive already in feet.
//
// THE PLAN IS A RECTANGLE THE STAIR MUST FIT INSIDE - the CLEAR inside of the
// stair hall, wall faces not centrelines. `entrySide` is the side the first
// riser faces: the stair climbs AWAY from it. `startOffset` is the clear floor
// between that side and the first riser - the space a door opens into, and on
// a U stair also where the top run arrives on the upper floor.
//
//   straight  one run, hugging `firstRunSide` or on the centre line
//   u         run 1 hugs `firstRunSide` and climbs away from the entry side,
//             a landing across the far end, run 2 comes back up the other side
//
// RISER HEIGHT AND TREAD DEPTH ARE THE STAIR TYPE'S: the riser count is the
// fewest that keeps every riser at or under the type's Maximum Riser Height,
// the tread is the type's Minimum Tread Depth. Nothing here invents either.
//
// ALL OR NOTHING. Everything is read back inside the edit scope - the run
// count, the total risers, the footprint inside the rectangle - and any miss
// cancels the scope, so no stair is left. With `cutOpening` the slab on the
// TOP level is cut over the stair's footprint and its volume read before and
// after; a cut that removed nothing deletes the stair again and fails the
// call. Measured 2026-10-03, rolled back: a SHAFT opening from Level 1 to
// Level 2 over Stair 1 removed nothing from either slab inside a transaction,
// while a floor opening in the Level 2 slab removed 8 m3 - so it is the
// slab's own opening that is cut.

var created = ElementId.InvalidElementId;
var risers = 0;
var riserHeightMm = 0.0;
var runCount = 0;
var landingCount = 0;
var openings = new List<ElementId>();
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

// THE SLAB'S OWN VOLUME, from its solids - the measure the 2026-10-03 probe
// used. The computed-area parameter did not move when a shaft was added.
Func<Element, double> volumeOf = e =>
{
    var sum = 0.0;
    var geometry = e.get_Geometry(new Options());
    if (geometry == null) return 0.0;
    foreach (var item in geometry)
    {
        var solid = item as Solid;
        if (solid != null) sum += solid.Volume;
    }
    return sum;
};

string refused = null;
var side = (entrySide ?? "").Trim().ToLowerInvariant();
var runSide = (firstRunSide ?? "").Trim().ToLowerInvariant();
if (runSide == "center") runSide = "centre";
var form = (shape ?? "").Trim().ToLowerInvariant().Replace("-shaped", "").Replace(" ", "");
if (form == "dogleg" || form == "ushape") form = "u";

// ---------------------------------------------------------------------------
// 1. EVERYTHING THAT CAN BE REFUSED BEFORE THE MODEL IS TOUCHED.
// ---------------------------------------------------------------------------

StairsType chosenType = null;
var typeNames = new List<string>();
foreach (StairsType candidate in new FilteredElementCollector(doc).OfClass(typeof(StairsType)))
{
    typeNames.Add(candidate.Name);
    if (candidate.Name == (stairType ?? "").Trim()) chosenType = candidate;
}

if (baseLevel == null || topLevel == null)
    refused = "Both a base level and a top level are needed";
else if (topLevel.ProjectElevation <= baseLevel.ProjectElevation + tolerance)
    refused = string.Format("'{0}' is not above '{1}' - a stair climbs from the base level to a "
        + "HIGHER top level", topLevel.Name, baseLevel.Name);
else if (chosenType == null)
    refused = string.Format("There is no stair type called '{0}' in this model. The ones loaded "
        + "are: {1}", stairType, string.Join(", ", typeNames));
else if (form != "straight" && form != "u")
    refused = string.Format("'{0}' is not a shape this makes - say straight or u", shape);
else if (area == null || area.Count < 2)
    refused = "The stair hall is two opposite corners of its clear inside, and fewer than two "
        + "points were given";
else if (side != "north" && side != "south" && side != "east" && side != "west")
    refused = string.Format("'{0}' is not a side - say north, south, east or west for the side "
        + "the first riser faces", entrySide);
else if (runWidth <= 0 || startOffset < 0)
    refused = "The run width must be more than zero and the start offset cannot be negative";
else if (runWidth / 304.8 < chosenType.MinRunWidth - tolerance)
    refused = string.Format("A {0} mm run is narrower than '{1}' allows - its Minimum Run Width "
        + "is {2} mm", runWidth, chosenType.Name, mm(chosenType.MinRunWidth));

// THE RECTANGLE, IN FEET, and which way is which. `along` is measured from the
// entry side into the hall; `across` is the other plan axis.
var xMin = 0.0; var xMax = 0.0; var yMin = 0.0; var yMax = 0.0;
if (refused == null)
{
    xMin = area.Min(p => p.X); xMax = area.Max(p => p.X);
    yMin = area.Min(p => p.Y); yMax = area.Max(p => p.Y);
}
var northSouth = side == "north" || side == "south";
var alongLength = northSouth ? yMax - yMin : xMax - xMin;
var acrossLow = northSouth ? xMin : yMin;
var acrossHigh = northSouth ? xMax : yMax;
var highName = northSouth ? "east" : "north";
var lowName = northSouth ? "west" : "south";

Func<double, double, double, XYZ> at = (along, across, z) =>
{
    if (side == "north") return new XYZ(across, yMax - along, z);
    if (side == "south") return new XYZ(across, yMin + along, z);
    if (side == "east") return new XYZ(xMax - along, across, z);
    return new XYZ(xMin + along, across, z);
};

var width = runWidth / 304.8;
var offset = startOffset / 304.8;
var height = 0.0; var tread = 0.0; var riser = 0.0;
var total = 0; var first = 0; var second = 0;
var across1 = 0.0; var across2 = 0.0;

if (refused == null)
{
    if (alongLength < tolerance || acrossHigh - acrossLow < tolerance)
        refused = "The two corners do not make a rectangle - they share an X or a Y";
    else if (runSide != highName && runSide != lowName && !(runSide == "centre" && form == "straight"))
        refused = string.Format("With the entry on the {0} side the first run hugs the {1} or the "
            + "{2} side{3} - '{4}' is not one of those", side, highName, lowName,
            form == "straight" ? ", or the centre" : "", firstRunSide);
}

if (refused == null)
{
    height = topLevel.ProjectElevation - baseLevel.ProjectElevation;
    tread = chosenType.MinTreadDepth;
    total = (int)Math.Ceiling(height / chosenType.MaxRiserHeight - 1e-9);
    riser = height / total;
    first = form == "u" ? (total + 1) / 2 : total;
    second = total - first;

    var hugHigh = runSide == highName;
    across1 = runSide == "centre" ? (acrossLow + acrossHigh) / 2
            : hugHigh ? acrossHigh - width / 2 : acrossLow + width / 2;
    across2 = hugHigh ? acrossLow + width / 2 : acrossHigh - width / 2;

    // A RUN OF r RISERS HAS r - 1 TREADS. The read-back below is what decides
    // whether Revit agreed; this is only where the run is asked to go.
    var needAlong = offset + (first - 1) * tread + (form == "u" ? width : 0.0);
    var needAcross = form == "u" ? 2 * width : width;

    if (needAcross > acrossHigh - acrossLow + tolerance)
        refused = string.Format("{0} need {1} mm across and the hall is {2} mm",
            form == "u" ? "Two runs side by side" : "The run", mm(needAcross),
            mm(acrossHigh - acrossLow));
    else if (needAlong > alongLength + tolerance)
        refused = string.Format("{0} risers of {1} mm on a {2} mm tread need {3} mm from the {4} "
            + "side and the hall is {5} mm - shorten the start offset, or use another shape",
            first, mm(riser), mm(tread), mm(needAlong), side, mm(alongLength));
}

// ---------------------------------------------------------------------------
// 2. THE STAIR, inside its edit scope.
// ---------------------------------------------------------------------------

if (refused == null)
{
    using (var scope = new StairsEditScope(doc, "Heron: create stairs"))
    {
        if (!scope.IsPermitted)
        {
            refused = "Revit will not open a stair for editing at this moment - another edit "
                + "mode is active, or a transaction is open around this fragment (D-112)";
        }
        else
        {
            var stairsId = scope.Start(baseLevel.Id, topLevel.Id);

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
                    var length1 = Math.Max(first - 1, 1) * tread;

                    var run1 = StairsRun.CreateStraightRun(doc, stairsId,
                        Line.CreateBound(at(offset, across1, z), at(offset + length1, across1, z)),
                        justify);
                    run1.ActualRunWidth = width;
                    run1.TopElevation = first * riser;

                    if (form == "u")
                    {
                        var z2 = z + first * riser;
                        var length2 = Math.Max(second - 1, 1) * tread;
                        var run2 = StairsRun.CreateStraightRun(doc, stairsId,
                            Line.CreateBound(at(offset + length1, across2, z2),
                                             at(offset + length1 - length2, across2, z2)),
                            justify);
                        run2.ActualRunWidth = width;
                        run2.TopElevation = height;

                        if (!StairsLanding.CanCreateAutomaticLanding(doc, run1.Id, run2.Id))
                            refused = "Revit cannot put a landing between the two runs as they "
                                + "were laid out";
                        else
                            StairsLanding.CreateAutomaticLanding(doc, run1.Id, run2.Id);
                    }

                    doc.Regenerate();

                    // THE READ-BACK, while the scope can still be cancelled.
                    if (refused == null)
                    {
                        var runs = stairs.GetStairsRuns();
                        runCount = runs.Count;
                        landingCount = stairs.GetStairsLandings().Count;
                        risers = stairs.ActualRisersNumber;
                        riserHeightMm = Math.Round(stairs.ActualRiserHeight * 304.8, 1);

                        var perRun = new List<string>();
                        foreach (var runId in runs)
                            perRun.Add(((StairsRun)doc.GetElement(runId)).ActualRisersNumber
                                .ToString(invariant));

                        var box = stairs.get_BoundingBox(null);
                        var wantRuns = form == "u" ? 2 : 1;

                        if (runCount != wantRuns)
                            refused = string.Format("Revit made {0} run(s) where {1} were laid out",
                                runCount, wantRuns);
                        else if (form == "u" && landingCount < 1)
                            refused = "Revit made the two runs and no landing between them";
                        else if (risers != total)
                            refused = string.Format("The stair has {0} risers ({1} by run) where {2} "
                                + "reach '{3}' - it would not arrive at the top level", risers,
                                string.Join(" + ", perRun), total, topLevel.Name);
                        else if (box == null || box.Min.X < xMin - tolerance || box.Max.X > xMax + tolerance
                                 || box.Min.Y < yMin - tolerance || box.Max.Y > yMax + tolerance)
                            refused = string.Format("The stair as Revit made it runs outside the hall - "
                                + "its plan extent is X {0}..{1}, Y {2}..{3} mm",
                                box == null ? "?" : mm(box.Min.X), box == null ? "?" : mm(box.Max.X),
                                box == null ? "?" : mm(box.Min.Y), box == null ? "?" : mm(box.Max.Y));
                        else
                            findings.Add(string.Format("{0} stair of type '{1}' from '{2}' to '{3}': "
                                + "{4} risers of {5} mm ({6} by run), {7} mm treads, {8} mm runs, "
                                + "{9} landing(s). Plan extent X {10}..{11}, Y {12}..{13} mm",
                                form == "u" ? "U-shaped" : "Straight", chosenType.Name,
                                baseLevel.Name, topLevel.Name, risers, riserHeightMm,
                                string.Join(" + ", perRun), mm(stairs.ActualTreadDepth), runWidth,
                                landingCount, mm(box.Min.X), mm(box.Max.X), mm(box.Min.Y),
                                mm(box.Max.Y)));
                    }
                }
                catch (Exception ex)
                {
                    refused = string.Format("Revit refused the stair: {0}", ex.Message);
                }

                if (refused != null)
                    transaction.RollBack();
                else if (transaction.Commit() != TransactionStatus.Committed)
                    refused = "Revit would not keep the stair's runs";
            }

            if (refused == null)
            {
                scope.Commit(editScopeFailures);
                if (doc.GetElement(stairsId) as Stairs == null)
                    refused = "Revit did not keep the stair when its edit mode closed";
                else
                    created = stairsId;
            }
            else
            {
                scope.Cancel();
            }
        }
    }
}

// ---------------------------------------------------------------------------
// 3. THE OPENING in the top level's slab, over the stair - only when asked.
// ---------------------------------------------------------------------------

if (refused == null && cutOpening)
{
    var footprint = doc.GetElement(created).get_BoundingBox(null);
    var slabs = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
        .Where(f => f.LevelId == topLevel.Id)
        .Where(f =>
        {
            var b = f.get_BoundingBox(null);
            return b != null && b.Min.X <= footprint.Min.X && b.Max.X >= footprint.Max.X
                && b.Min.Y <= footprint.Min.Y && b.Max.Y >= footprint.Max.Y;
        })
        .ToList();

    if (slabs.Count == 0)
    {
        findings.Add(string.Format("No floor on '{0}' spans the stair, so there was no slab to "
            + "cut an opening in", topLevel.Name));
    }
    else
    {
        using (var transaction = new Transaction(doc, "Heron: stair opening"))
        {
            transaction.Start();
            discipline(transaction);
            try
            {
                var z = topLevel.ProjectElevation;
                var corners = new[]
                {
                    new XYZ(footprint.Min.X, footprint.Min.Y, z), new XYZ(footprint.Max.X, footprint.Min.Y, z),
                    new XYZ(footprint.Max.X, footprint.Max.Y, z), new XYZ(footprint.Min.X, footprint.Max.Y, z),
                };
                foreach (var slab in slabs)
                {
                    var before = volumeOf(slab);
                    var profile = new CurveArray();
                    for (var i = 0; i < 4; i++)
                        profile.Append(Line.CreateBound(corners[i], corners[(i + 1) % 4]));
                    var opening = doc.Create.NewOpening(slab, profile, false);
                    doc.Regenerate();
                    var removed = (before - volumeOf(slab)) * 0.0283168466;
                    if (opening == null || removed <= 0.001)
                    {
                        refused = string.Format("The opening in floor {0} on '{1}' took nothing out "
                            + "of the slab", slab.Id, topLevel.Name);
                        break;
                    }
                    openings.Add(opening.Id);
                    findings.Add(string.Format("Opening cut in floor {0} on '{1}' over the stair, "
                        + "X {2}..{3}, Y {4}..{5} mm - {6} m3 of slab removed", slab.Id,
                        topLevel.Name, mm(footprint.Min.X), mm(footprint.Max.X),
                        mm(footprint.Min.Y), mm(footprint.Max.Y),
                        Math.Round(removed, 2).ToString(invariant)));
                }
            }
            catch (Exception ex)
            {
                refused = string.Format("Revit refused the opening: {0}", ex.Message);
            }

            if (refused != null)
                transaction.RollBack();
            else if (transaction.Commit() != TransactionStatus.Committed)
                refused = "Revit would not keep the opening";
        }

        // ALL OR NOTHING: a stair asked for with its opening goes again if the
        // opening could not be made.
        if (refused != null)
        {
            openings.Clear();
            using (var cleanup = new Transaction(doc, "Heron: remove the stair"))
            {
                cleanup.Start();
                discipline(cleanup);
                doc.Delete(created);
                cleanup.Commit();
            }
            created = ElementId.InvalidElementId;
            refused += " - the stair was removed again, so nothing is left";
        }
    }
}

if (refused != null)
{
    findings.Add("Nothing was made. " + refused);
    risers = 0; riserHeightMm = 0; runCount = 0; landingCount = 0;
}
