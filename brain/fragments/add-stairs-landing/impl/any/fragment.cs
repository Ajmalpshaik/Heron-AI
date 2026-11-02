// NOT STANDALONE. Assumes `doc`, `editScopeFailures` and `elements` (the two
// stair runs selected); leaves `landings`, `stairs`, `risersBefore`,
// `risersAfter` and `findings`.
//
// ===========================================================================
// THIS FRAGMENT OPENS ITS OWN TRANSACTION, AND ONLY BECAUSE IT HAS TO - D-112.
// ===========================================================================
//
// CREATE_STAIRS's pattern (FRG-ELE-076), followed exactly: a stair's runs and
// landings change only inside a StairsEditScope, which Revit refuses to start
// while a transaction is open. Declaring `editScopeFailures` makes the add-in
// run this inside its TransactionGroup with NO transaction open - still one
// undo, and a preview still rolls it back. The one transaction opened here
// takes `editScopeFailures`, so what Revit says reaches the verdict.
//
// WHAT IT DOES: the two runs handed in - both of ONE stair by component - get
// the landing Revit's own "Landing > Pick Two Runs" makes between them
// (StairsLanding.CreateAutomaticLanding), of the landing type the stair type
// names. The stair is opened for editing with StairsEditScope.Start(stairsId),
// the landing made, and the landings counted before and after: a call that
// added none cancels the edit and says so.
//
// REFUSED, NOTHING CHANGED: anything but exactly two stair runs; two runs of
// different stairs; and two runs Revit answers it cannot join with a landing
// (StairsLanding.CanCreateAutomaticLanding) - asked BEFORE the stair is opened.
// Revit's reasons are not given by that call; the answer says what to look at.
//
// READ BACK: each new landing's depth along the lower run, its thickness and
// its height above the stair's base, in mm; the stair's riser count before and
// after, because Revit may re-lay the runs round the landing.

var landings = new List<ElementId>();
var stairs = ElementId.InvalidElementId;
var risersBefore = 0;
var risersAfter = 0;
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);

Action<Transaction> discipline = t =>
{
    var options = t.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(editScopeFailures);
    options.SetClearAfterRollback(true);
    t.SetFailureHandlingOptions(options);
};

// Revit's own words for a refusal, from the add-in's failure discipline -
// CREATE_STAIRS's reading (5b-1240). An older add-in gives none.
Func<string> revitSaid = () =>
{
    var text = editScopeFailures == null ? null : editScopeFailures.ToString();
    return text != null && text.StartsWith("Revit would not let it through", StringComparison.Ordinal)
        ? " - " + text : "";
};

string refused = null;
var given = (elements ?? new List<Element>()).Where(e => e != null).ToList();
var runs = given.OfType<StairsRun>().ToList();

if (given.Count != 2 || runs.Count != 2)
    refused = string.Format("Select the two stair runs to join - {0} element(s) were given, {1} of them "
        + "stair runs. In Revit, Tab onto a run inside the stair to select it", given.Count, runs.Count);
else if (runs[0].GetStairs().Id != runs[1].GetStairs().Id)
    refused = "The two runs belong to different stairs, and a landing joins two runs of ONE stair";

// THE LOWER RUN FIRST - the order Revit's landing goes between.
StairsRun lower = null; StairsRun upper = null;
Stairs host = null;
if (refused == null)
{
    lower = runs[0].BaseElevation <= runs[1].BaseElevation ? runs[0] : runs[1];
    upper = lower == runs[0] ? runs[1] : runs[0];
    host = lower.GetStairs();
    risersBefore = host.ActualRisersNumber;
    if (!StairsLanding.CanCreateAutomaticLanding(doc, lower.Id, upper.Id))
        refused = string.Format("Revit answers that it cannot join runs {0} and {1} with a landing. Look at "
            + "whether they already meet at a landing, and whether the upper run starts at the height the "
            + "lower one ends", lower.Id, upper.Id);
}

// The landing's depth along the lower run, read off its own footprint.
Func<StairsLanding, XYZ, double> depthOf = (landing, way) =>
{
    var low = double.MaxValue; var high = double.MinValue;
    foreach (var curve in landing.GetFootprintBoundary())
        for (var end = 0; end < 2; end++)
        {
            var reach = curve.GetEndPoint(end).DotProduct(way);
            low = Math.Min(low, reach); high = Math.Max(high, reach);
        }
    return high > low ? high - low : 0.0;
};

if (refused == null)
{
    var before = new HashSet<ElementId>(host.GetStairsLandings());
    var path = lower.GetStairsPath().ToList();
    var way = path.Count == 0 ? XYZ.BasisX
        : (path[path.Count - 1].GetEndPoint(1) - path[0].GetEndPoint(0)).Normalize();
    string madeNote = null;

    using (var scope = new StairsEditScope(doc, "Heron: add a stair landing"))
    {
        if (!scope.IsPermitted)
            refused = "Revit will not open the stair for editing at this moment - another edit mode is "
                + "active, or a transaction is open around this fragment (D-112)";
        else
        {
            try
            {
                scope.Start(host.Id);
            }
            catch (Exception ex)
            {
                refused = string.Format("Revit will not open stair {0} for editing: {1}", host.Id, ex.Message);
            }

            if (refused == null)
            {
                using (var transaction = new Transaction(doc, "Heron: stair landing"))
                {
                    transaction.Start();
                    discipline(transaction);
                    try
                    {
                        StairsLanding.CreateAutomaticLanding(doc, lower.Id, upper.Id);
                        doc.Regenerate();
                        var fresh = host.GetStairsLandings().Where(id => !before.Contains(id)).ToList();
                        if (fresh.Count == 0)
                            refused = "Revit added no landing between the two runs";
                        else
                        {
                            landings.AddRange(fresh);
                            var notes = fresh.Select(id =>
                            {
                                var landing = (StairsLanding)doc.GetElement(id);
                                return string.Format("landing {0}: {1} mm deep, {2} mm thick, {3} mm above the "
                                    + "stair's base", id, mm(depthOf(landing, way)), mm(landing.Thickness),
                                    mm(landing.BaseElevation));
                            });
                            madeNote = string.Format("{0} landing(s) added to stair {1} between runs {2} and {3} - "
                                + "{4}", fresh.Count, host.Id, lower.Id, upper.Id, string.Join("; ", notes));
                        }
                    }
                    catch (Exception ex)
                    {
                        refused = string.Format("Revit refused the landing: {0}", ex.Message);
                    }

                    if (refused != null)
                        transaction.RollBack();
                    else if (transaction.Commit() != TransactionStatus.Committed)
                        refused = "Revit would not keep the landing" + revitSaid();
                }
            }

            if (refused == null)
            {
                scope.Commit(editScopeFailures);
                var after = doc.GetElement(host.Id) as Stairs;
                var kept = after == null ? 0 : after.GetStairsLandings().Count(id => landings.Contains(id));
                if (kept == 0)
                    refused = "Revit did not keep the landing when the stair's edit mode closed" + revitSaid();
                else
                {
                    stairs = host.Id;
                    risersAfter = after.ActualRisersNumber;
                    findings.Add(madeNote);
                    findings.Add(risersAfter == risersBefore
                        ? string.Format("The stair still has {0} risers", risersAfter)
                        : string.Format("The stair had {0} risers and now has {1} - Revit re-laid the runs "
                            + "round the landing", risersBefore, risersAfter));
                }
            }
            else if (scope.IsActive)
            {
                scope.Cancel();
            }
        }
    }
}

if (refused != null)
{
    findings.Add("Nothing was changed. " + refused);
    landings.Clear();
    stairs = ElementId.InvalidElementId;
    risersAfter = 0;
}
