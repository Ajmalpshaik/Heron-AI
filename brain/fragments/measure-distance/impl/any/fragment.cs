// NOT STANDALONE. Assumes `doc`, `elements` and `elementIds` are in scope;
// leaves `straightMm`, `horizontalMm`, `verticalMm` and `findings` behind.
//
// VERSION 3 (2026-11-26, row 5b-2006, issue #1308) - THE TWO ELEMENTS ARE THE
// TWO SELECTED IN REVIT, OR TWO IDS TYPED, AND TYPED IDS DECIDE. Version 2 had
// two single-element needs, `first` and `second`, and the add-in's only word for
// one (`selected`) means exactly one element and binds it to BOTH - so two
// different elements could never be handed in. The shape is now
// REPORT_PARAMETER_INVENTORY's: `elements` is what is selected (or handed on by
// a step before), `elementIds` is ids typed as Revit shows them, matched here as
// TEXT - each element's own Id.ToString() against what was typed, in one walk of
// the model - so no ElementId is built from a number (that constructor changed
// at Revit 2024, and nothing here carries a version #if; D-05). Exactly two
// either way: one or three is refused in words that say how many came and that
// two are needed, because measuring "two of the three" would be a guess.
//
// AN ELEMENT IS NOT A POINT. A thirty-metre duct has no single position, so
// centre to centre would call two touching runs thirty metres apart. The gap is
// measured between the nearest faces of the two bounding boxes, and the report
// says which definition was used - where both are point-placed the two agree.
//
// THREE NUMBERS, BECAUSE ALL THREE GET ASKED. Straight line is the tape
// measure; horizontal is what a plan shows; vertical is the one that decides
// whether a duct clears a beam. Reporting only the first makes the reader do
// the trigonometry the question was about.
//
// MILLIMETRES OUT, by multiplying by 304.8 - internal feet are not what anybody
// asked, and naming the unit is what stops the number being read as another one.

const double MillimetresPerFoot = 304.8;

var straightMm = 0.0;
var horizontalMm = 0.0;
var verticalMm = 0.0;
var findings = new List<string>();

// ---- which two: two ids typed, or the two selected ----

Element first = null;
Element second = null;
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
                + "digits only, comma separated, like \"352516, 352517\". Nothing was measured";
            break;
        }
        typed.Add(part);
    }

    if (notTwo == null && typed.Count != 2)
    {
        notTwo = string.Format("{0} element id(s) were typed, and this measures between exactly TWO. "
            + "Type two ids, comma separated. Nothing was measured", typed.Count);
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
            first = found[0];
            second = found[1];
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
            + "elements. Select the two in Revit, or type their two ids in elementIds, comma separated"
        : string.Format("{0} element(s) are selected (or handed on by the step before), and this "
            + "measures between exactly TWO. A selection has no order to pick two of them by, so "
            + "nothing was measured. Select just the two, or type their two ids in elementIds",
            handedIn.Count);
}
else
{
    first = handedIn[0];
    second = handedIn[1];
}

if (notTwo != null)
{
    findings.Add(notTwo);
}
else if (first.Id == second.Id)
{
    findings.Add("Both sides are the same element, so the distance is zero by definition rather "
        + "than by measurement");
}
else
{
    var boxA = first.get_BoundingBox(null);
    var boxB = second.get_BoundingBox(null);

    if (boxA == null || boxB == null)
    {
        findings.Add(string.Format("{0} has no geometry to measure from. An element with no bounding "
            + "box - much annotation, and the analytical elements - has no position in space",
            boxA == null ? first.Name : second.Name));
    }
    else
    {
        // The gap along one axis is zero where the two ranges overlap, and the
        // clear distance between them where they do not.
        Func<double, double, double, double, double> gap = (lowA, highA, lowB, highB) =>
        {
            if (highA < lowB) return lowB - highA;
            if (highB < lowA) return lowA - highB;
            return 0.0;
        };

        var dx = gap(boxA.Min.X, boxA.Max.X, boxB.Min.X, boxB.Max.X);
        var dy = gap(boxA.Min.Y, boxA.Max.Y, boxB.Min.Y, boxB.Max.Y);
        var dz = gap(boxA.Min.Z, boxA.Max.Z, boxB.Min.Z, boxB.Max.Z);

        var straight = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var horizontal = Math.Sqrt(dx * dx + dy * dy);

        straightMm = straight * MillimetresPerFoot;
        horizontalMm = horizontal * MillimetresPerFoot;
        verticalMm = dz * MillimetresPerFoot;

        findings.Add(string.Format("{0:0.#} mm apart in a straight line: {1:0.#} mm horizontally and "
            + "{2:0.#} mm vertically, measured between the nearest faces of the two bounding boxes",
            straightMm, horizontalMm, verticalMm));

        if (straight < 1e-9)
            // VERSION 2 (2026-11-21, row 5b-1363 item 4): SELECT_TOUCHING was
            // retired into FIND_CLASHES on 2026-11-01, and neither ever told
            // touching from overlapping - FIND_CLASHES says so in its own
            // purpose. So the sentence names the live card and what it does.
            findings.Add("They touch or overlap, so every distance is zero. That is a real answer, "
                + "and this fragment cannot tell touching from overlapping - FIND_CLASHES tests their "
                + "real solids, though it counts touching as clashing too");

        findings.Add("A bounding box is axis-aligned and is larger than a sloped or rotated element, "
            + "so for those this gap is a LOWER bound: the real clearance is at least this and may "
            + "be more");
    }
}
