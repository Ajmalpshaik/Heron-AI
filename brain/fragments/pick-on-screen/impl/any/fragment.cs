// NOT STANDALONE. Assumes `uidoc` is in scope, and leaves `pointMm`,
// `clicked`, `cancelled`, `refused`, `viewName`, `waitedSeconds` and
// `findings` for whatever reports the result.
//
// CHANGES NOTHING IN THE MODEL AND NOTHING IN THE SELECTION. It puts Revit
// into its own pick mode, waits for the modeller to click ONE point, and
// reads where the click landed. Opens no transaction and needs none.
//
// THE ONE CALL THAT WAITS FOR A PERSON. Revit's own pick does not return
// until the modeller clicks or presses Esc. Everything else in this library
// returns as fast as Revit can answer; this one returns as fast as somebody
// can find the place and click it.
//
// ONE CLICK PER CALL, AND WHY (FRAGMENT-ISSUES row 5b-1507, Q-86). Several
// clicks need a loop, and the run clock (D-129) is asked at the head of
// every pass of one: a modeller who took longer than its budget over the
// first click would have the job stopped at the second pass and reported as
// having run out of time - "a loop that never ends is the usual cause",
// which is untrue of a person - with the point they clicked dropped. With no
// loop there is no second pass. Several points are several calls, until
// Q-86 is decided.
//
// THE LIMIT THAT IS LEFT, READ FROM THE ADD-IN AND NOT YET SEEN IN REVIT:
// the dispatcher waits revit.operationTimeoutSeconds - about a minute as
// installed - for a job it has started, then tells the chat Revit is still
// working and stops listening. A click made after that reaches nobody, and
// no answer follows. The longer wait it gives a Keep or Discard box cannot
// be asked for from a fragment. This fragment does not work round that; it
// says how long Revit waited, in `waitedSeconds`.
//
// ESC IS AN ANSWER, NOT A FAILURE. Revit reports it by throwing; here it is
// `cancelled` true. Revit throws the same thing when the modeller switches
// view or closes the model during the pick, and they cannot be told apart
// from here - the finding says so. The exception is matched by its type
// NAME: the namespace it lives in is not one a fragment is given.
//
// THE POINT IS ON THE VIEW'S WORK PLANE, NOT ON WHAT WAS CLICKED. Revit
// projects the click onto the active work plane: in a plan that is the
// level's height, whatever the cursor was over. A view with no work plane
// takes no click at all - Revit's own remarks, every release 2020 to 2027 -
// and says so by throwing; its words are handed back in `refused`. Whether
// a 3D view nobody has set one in is such a view is not known until it runs
// (Group NH, NH5). Setting a work plane is a change to the model, so it is
// not done here.
//
// SNAPPING IS REVIT'S OWN. The pick is asked for with the prompt alone, so
// it follows the snaps the modeller has switched on in Revit. Naming snap
// types would need a namespace a fragment is not given.
//
// MILLIMETRES OUT, AS D-67 WRITES A POINT: "x, y, z" - the text every point
// value in this library takes. Revit answers in feet; one foot is 304.8 mm
// exactly.
//
// A TABLE HAS NOWHERE TO CLICK. A schedule, a report or a browser in front
// is refused before Revit is asked, so pick mode is never entered in a view
// where a point means nothing. Read off the view type's NAME: one report
// type was re-spelled at Revit 2027, and a name test compiles on every
// release.

var findings = new List<string>();
var pointMm = "";
var clicked = 0;
var cancelled = false;
var refused = "";
var viewName = "";
var waitedSeconds = 0.0;

// ONE BLOCK, so the view and the working text are gone when it ends. What a
// fragment leaves at the top is kept for the next fragment by name, and a
// view is a handle that goes stale.
{
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    var kind = "";
    var planeName = "";

    View inFront = null;
    try { inFront = uidoc.ActiveView; } catch (Exception) { inFront = null; }

    if (inFront == null)
    {
        // Revit answers null for a model that is not the one in front.
        refused = "This model is not the one in front in Revit, so there is no view to click in. "
            + "Bring it to the front and ask again. Nothing was asked in Revit.";
    }
    else
    {
        try { viewName = inFront.Name ?? ""; } catch (Exception) { viewName = ""; }
        try { kind = inFront.ViewType.ToString(); } catch (Exception) { kind = ""; }

        if (kind.IndexOf("Schedule", StringComparison.Ordinal) >= 0
            || kind.IndexOf("Report", StringComparison.Ordinal) >= 0
            || kind.IndexOf("Browser", StringComparison.Ordinal) >= 0
            || kind == "Internal" || kind == "Undefined")
        {
            refused = "\"" + viewName + "\" is a " + kind + " - a table or a list, with nowhere to "
                + "click a point. Bring a plan, section, elevation or 3D view to the front and ask "
                + "again. Nothing was asked in Revit.";
        }
    }

    if (refused.Length == 0)
    {
        // Read BEFORE the pick, so the refusal below can say whether a
        // missing work plane is the reason rather than guess at it.
        var planeMissing = false;
        try
        {
            var plane = inFront.SketchPlane;
            if (plane == null) planeMissing = true;
            else planeName = plane.Name ?? "";
        }
        catch (Exception) { planeMissing = false; planeName = ""; }

        var watch = System.Diagnostics.Stopwatch.StartNew();
        XYZ point = null;
        try
        {
            point = uidoc.Selection.PickPoint("Heron: click the point - Esc to cancel");
        }
        catch (Exception ex)
        {
            if (ex.GetType().Name == "OperationCanceledException")
            {
                cancelled = true;
            }
            else
            {
                refused = "Revit would not take a click in \"" + viewName + "\" (" + kind + "): "
                    + ex.Message;

                // SAID ONLY WHEN IT IS WHAT WAS READ: this view showed no
                // work plane before the pick. Otherwise Revit's words stand
                // alone - advice about a cause nobody saw is a guess.
                if (planeMissing)
                {
                    refused += " This view has no work plane set, and Revit takes a click only on "
                        + "one - set one in Revit (Work Plane > Set) or bring a plan view to the "
                        + "front, and ask again.";
                }
            }
        }
        watch.Stop();
        waitedSeconds = Math.Round(watch.Elapsed.TotalSeconds, 1);

        if (point != null)
        {
            // The + 0.0 turns a rounded minus zero into zero, which the older
            // and newer .NET releases would otherwise write differently.
            pointMm = (Math.Round(point.X * 304.8, 3) + 0.0).ToString("0.###", culture) + ", "
                + (Math.Round(point.Y * 304.8, 3) + 0.0).ToString("0.###", culture) + ", "
                + (Math.Round(point.Z * 304.8, 3) + 0.0).ToString("0.###", culture);
            clicked = 1;

            findings.Add("One point clicked in \"" + viewName + "\" (" + kind + "), in millimetres "
                + "from the model's internal origin. Revit waited "
                + waitedSeconds.ToString("0.#", culture) + " second(s) for the modeller.");

            findings.Add("The point lies on this view's work plane"
                + (planeName.Length == 0 ? "" : " (\"" + planeName + "\")")
                + ", not on whatever the cursor was over - in a plan its Z is the plane's height.");

            if (kind == "DrawingSheet" || kind == "DraftingView" || kind == "Legend")
            {
                findings.Add("\"" + viewName + "\" is a " + kind + ": this is a position on that "
                    + "drawing, not a place in the building.");
            }
        }
        else if (cancelled)
        {
            findings.Add("The modeller cancelled the pick in \"" + viewName + "\" after "
                + waitedSeconds.ToString("0.#", culture) + " second(s) - Esc, or the view was "
                + "switched or the model closed while Revit waited; Revit reports all three the "
                + "same way. No point was clicked and nothing in the model was changed.");
        }
        else if (refused.Length == 0)
        {
            refused = "Revit came back from the click with no point.";
        }
    }

    if (refused.Length > 0) findings.Add(refused);
}
