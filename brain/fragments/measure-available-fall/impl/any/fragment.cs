// NOT STANDALONE. Assumes `doc`, `elements` and `elementIds` are in scope;
// leaves `fallMm`, `runMm`, `gradientOneIn` and `findings` behind.
//
// VERSION 2 (2026-11-26, row 5b-2006, issue #1308) - THE TWO POINTS ARE THE
// TWO ELEMENTS SELECTED IN REVIT, OR TWO IDS TYPED UPSTREAM FIRST, AND TYPED
// IDS DECIDE. Version 1 had two single-element needs, `upstream` and
// `downstream`, and the add-in's only word for one (`selected`) means exactly
// one element and binds it to BOTH - so two different points could never be
// handed in. The shape is now REPORT_PARAMETER_INVENTORY's and MEASURE_DISTANCE
// version 3's: `elements` is what is selected (or handed on by a step before),
// `elementIds` is ids typed as Revit shows them, matched here as TEXT against
// each element's own Id.ToString() in one walk of the model, so no ElementId is
// built from a number (D-05). Exactly two either way; one or three is refused in
// words that say how many came and that two are needed.
//
// A SELECTION HAS NO ORDER, SO IT CANNOT SAY WHICH END IS UPSTREAM. Taking the
// first of two as upstream would report a rise or a fall by the luck of how
// Revit lists them. So from a selection the HIGHER of the two (by its lowest
// point) is taken as upstream, and the answer says so; typed ids keep the order
// typed, upstream first, and only they can report a rise.
//
// THE RUN LENGTH IS THE HORIZONTAL DISTANCE, NOT THE STRAIGHT LINE. A drain is
// laid to a gradient measured along the plan; using the sloping length
// overstates the fall available - by little on a shallow run and by enough to
// matter on a steep one.
//
// A RISE IS REPORTED AS A RISE. Water does not run uphill, so a downstream
// point above the upstream one is a finding, not a negative number somebody
// might carry into a calculation.
//
// IT SAYS WHAT IS AVAILABLE, NEVER WHAT IS REQUIRED. Whether 1 in 80 will do
// depends on pipe size, flow, and the standard being worked to. That is a
// judgement and it is not this fragment's.

const double MillimetresPerFoot = 304.8;

var fallMm = 0.0;
var runMm = 0.0;
var gradientOneIn = 0.0;
var findings = new List<string>();

// ---- which two: two ids typed upstream first, or the two selected ----

Element upstream = null;
Element downstream = null;
string notTwo = null;

Func<string, string, bool> sameWord = (a, b) =>
    string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);

var handedIn = new List<Element>();
if (elements != null)
    foreach (var element in elements) if (element != null) handedIn.Add(element);

var typedText = (elementIds ?? "").Trim();
var idsTyped = typedText.Length > 0 && !sameWord(typedText, "none")
    && !sameWord(typedText, "selected") && !sameWord(typedText, "selection");

if (idsTyped)
{
    var typed = new List<string>();
    foreach (var piece in typedText.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' },
                                          StringSplitOptions.RemoveEmptyEntries))
    {
        var part = piece.Trim();
        var digits = part.Length > 0;
        foreach (var c in part) if (c < '0' || c > '9') digits = false;
        if (!digits)
        {
            notTwo = "\"" + part + "\" is not an element id. Type the two ids as Revit shows them - "
                + "digits only, upstream first, comma separated, like \"352516, 352517\". Nothing "
                + "was measured";
            break;
        }
        typed.Add(part);
    }

    if (notTwo == null && typed.Count != 2)
    {
        notTwo = string.Format("{0} element id(s) were typed, and this measures between exactly TWO "
            + "points. Type two ids, upstream first, comma separated. Nothing was measured",
            typed.Count);
    }
    else if (notTwo == null)
    {
        // Element types too, in the one walk, and it stops once both are found.
        // The same id typed twice is one element - said below, as before.
        var found = new Element[2];
        var left = typed[0] == typed[1] ? 1 : 2;
        var everything = new LogicalOrFilter(new ElementIsElementTypeFilter(false),
                                             new ElementIsElementTypeFilter(true));
        foreach (var id in new FilteredElementCollector(doc).WherePasses(everything).ToElementIds())
        {
            var text = id.ToString();
            if (text != typed[0] && text != typed[1]) continue;
            var named = doc.GetElement(id);
            if (text == typed[0]) found[0] = named;
            if (text == typed[1]) found[1] = named;
            if (--left == 0) break;
        }

        var missing = new List<string>();
        for (var i = 0; i < 2; i++)
            if (found[i] == null && !missing.Contains(typed[i])) missing.Add(typed[i]);

        if (missing.Count > 0)
        {
            notTwo = string.Format("No element in '{0}' has the id {1}. An id belongs to one model. "
                + "Nothing was measured", doc.Title, string.Join(" or ", missing));
        }
        else
        {
            upstream = found[0];
            downstream = found[1];
        }
    }

    if (handedIn.Count > 0)
        findings.Add(string.Format("The {0} element(s) selected were NOT used - ids were typed, and "
            + "typed ids decide", handedIn.Count));
}
else if (handedIn.Count != 2)
{
    notTwo = handedIn.Count == 0
        ? "Nothing is selected and no element ids were typed, and this measures between exactly TWO "
            + "points. Select the two in Revit, or type their two ids in elementIds, upstream first"
        : string.Format("{0} element(s) are selected (or handed on by the step before), and this "
            + "measures between exactly TWO points. A selection has no order to pick two of them "
            + "by, so nothing was measured. Select just the two, or type their two ids in "
            + "elementIds, upstream first", handedIn.Count);
}
else
{
    upstream = handedIn[0];
    downstream = handedIn[1];
}

if (notTwo != null)
{
    findings.Add(notTwo);
}
else if (upstream.Id == downstream.Id)
{
    findings.Add("Both points are the same element, so there is no run and no fall to measure");
}
else
{
    var boxUp = upstream.get_BoundingBox(null);
    var boxDown = downstream.get_BoundingBox(null);

    if (boxUp == null || boxDown == null)
    {
        findings.Add("One of the two has no geometry to measure from, so it has no level and no "
            + "position");
    }
    else
    {
        // FROM A SELECTION THE HIGHER ONE IS UPSTREAM - a selection has no
        // order, and water runs from the higher invert to the lower. Typed ids
        // keep the order typed, so only they can report a rise.
        if (!idsTyped)
        {
            if (boxDown.Min.Z > boxUp.Min.Z)
            {
                var swap = upstream; upstream = downstream; downstream = swap;
                var swapBox = boxUp; boxUp = boxDown; boxDown = swapBox;
            }
            findings.Add(string.Format("Taken from the selection, which has no order: the higher of "
                + "the two by its lowest point, {0} ({1}), is taken as upstream and {2} ({3}) as "
                + "downstream. Type the two ids in elementIds, upstream first, to set the direction "
                + "yourself", upstream.Name, upstream.Id, downstream.Name, downstream.Id));
        }

        // The invert is what a drain is set out from, so the LOWEST point of
        // each element is the one that matters - not its centre.
        var upZ = boxUp.Min.Z;
        var downZ = boxDown.Min.Z;

        var upCentre = (boxUp.Min + boxUp.Max) / 2.0;
        var downCentre = (boxDown.Min + boxDown.Max) / 2.0;

        var dx = downCentre.X - upCentre.X;
        var dy = downCentre.Y - upCentre.Y;
        var horizontalFeet = Math.Sqrt(dx * dx + dy * dy);

        runMm = horizontalFeet * MillimetresPerFoot;
        fallMm = (upZ - downZ) * MillimetresPerFoot;

        if (runMm < 1e-6)
        {
            findings.Add(string.Format("The two points are vertically above one another, so there is "
                + "no horizontal run to fall along. The height difference is {0:0.#} mm", fallMm));
        }
        else if (fallMm > 0)
        {
            gradientOneIn = runMm / fallMm;

            findings.Add(string.Format("{0:0.#} mm of fall over {1:0} mm of horizontal run, which is "
                + "1 in {2:0.#}", fallMm, runMm, gradientOneIn));

            findings.Add("That is what is AVAILABLE. Whether it is enough depends on the pipe size, "
                + "the flow and the standard being worked to, and this fragment does not know any of "
                + "those");
        }
        else if (fallMm < 0)
        {
            findings.Add(string.Format("The downstream point is {0:0.#} mm HIGHER than the upstream "
                + "one over {1:0} mm of run. That is a rise, not a fall - water will not run this way "
                + "and no gradient is reported", -fallMm, runMm));
        }
        else
        {
            findings.Add(string.Format("The two points are at exactly the same level over {0:0} mm of "
                + "run, so there is no fall available at all", runMm));
        }

        findings.Add("Measured from the LOWEST point of each element, which is what a drain is set "
            + "out from, and along the horizontal distance between their centres - a gradient is "
            + "measured on plan, not along the slope");
    }
}
