// NOT STANDALONE. Assumes `doc`, `curves` and `referenceLines` are in scope;
// leaves `curveIds`, `drawnCurves`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// LINES AND SPLINES THROUGH POINTS - in an adaptive component, a pattern-based
// panel or a mass, the Family Editor's Spline Through Points, or a line picked
// between two points. A line drawn THROUGH the points is hosted by them, so it
// moves when they move - which is what makes a panel follow the cell it is
// placed in, or an adaptive beam follow its two clicks. A form built on these
// lines follows in turn.
//
// THE POINTS ARE NAMED BY THEIR PLACEMENT NUMBER - "1,2" - or by their unique
// id, commas between, one curve per group, `|` between groups: "1,2 | 2,3 |
// 3,4 | 4,1" is the four edges of a panel. Two points make a line, three or
// more a spline through all of them in the order given.
//
// REFERENCE LINES OR MODEL LINES. A reference line builds geometry and is not
// seen in a project; a model line is - the choice is the caller's, never a
// default.
//
// ALL OR NOTHING, READ BACK: each curve's points are read again in order; one
// that does not run through the points asked fails the call, and the host rolls
// the whole call back.

var findings = new List<string>();
var curveIds = "";
var drawnCurves = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);

var problems = new List<string>();
// Each curve asked for: its points, and how it was asked for.
var groups = new List<Tuple<List<ReferencePoint>, string>>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "Lines through points are drawn inside an adaptive or pattern-based family - open it first.";
}
else
{
    var all = new List<ReferencePoint>();
    try { all = new FilteredElementCollector(doc).OfClass(typeof(ReferencePoint)).Cast<ReferencePoint>().ToList(); }
    catch (Exception) { }
    // Every placement point by its number.
    var byNumber = new Dictionary<int, ReferencePoint>();
    foreach (var p in all)
    {
        try
        {
            if (AdaptiveComponentFamilyUtils.IsAdaptivePlacementPoint(doc, p.Id))
                byNumber[AdaptiveComponentFamilyUtils.GetPlacementNumber(doc, p.Id)] = p;
        }
        catch (Exception) { }
    }
    if (all.Count == 0)
        problems.Add("This family has no points to draw through - PLACE_ADAPTIVE_POINTS first, or start from a "
            + "pattern-based template, which brings its own.");

    var texts = (curves ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    if (texts.Count == 0) problems.Add("No curves were given - \"1,2 | 2,3 | 3,4 | 4,1\", points by placement number "
        + "or id, commas between, `|` between curves.");
    foreach (var text in texts)
    {
        var tokens = text.Split(new[] { ',', ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var group = new List<ReferencePoint>();
        foreach (var token in tokens)
        {
            int n;
            ReferencePoint found = null;
            if (int.TryParse(token, System.Globalization.NumberStyles.Integer, invariant, out n))
            {
                if (!byNumber.TryGetValue(n, out found))
                {
                    problems.Add("No placement point is numbered " + n + ". This family's: "
                        + (byNumber.Count == 0 ? "none" : string.Join(", ", byNumber.Keys.OrderBy(k => k))) + ".");
                    continue;
                }
            }
            else
            {
                found = doc.GetElement(token) as ReferencePoint;
                if (found == null)
                {
                    problems.Add("\"" + token + "\" is neither a placement number nor the id of a point in this family.");
                    continue;
                }
            }
            if (group.Count > 0 && group[group.Count - 1].Id == found.Id)
            {
                problems.Add("\"" + text + "\" names the same point twice in a row.");
                continue;
            }
            group.Add(found);
        }
        if (tokens.Count < 2) problems.Add("\"" + text + "\" names " + tokens.Count + " point(s) - a curve needs two or more.");
        else groups.Add(Tuple.Create(group, text));
    }

    if (problems.Count > 0) refused = "Nothing was drawn. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// DRAW, THEN READ EVERY CURVE BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var made = new List<Tuple<CurveByPoints, List<ReferencePoint>, string>>();
    foreach (var g in groups)
    {
        var array = new ReferencePointArray();
        foreach (var p in g.Item1) array.Append(p);
        CurveByPoints curve;
        try { curve = doc.FamilyCreate.NewCurveByPoints(array); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not draw a curve through \"" + g.Item2 + "\": " + ex.Message
                + " The call failed, and Heron rolls the whole call back.");
        }
        if (curve == null)
            throw new InvalidOperationException("Revit drew no curve through \"" + g.Item2 + "\". The call failed, and "
                + "Heron rolls the whole call back.");
        try { curve.IsReferenceLine = referenceLines; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not make the curve through \"" + g.Item2 + "\" a "
                + (referenceLines ? "reference" : "model") + " line: " + ex.Message + " The call failed, and Heron rolls "
                + "the whole call back.");
        }
        made.Add(Tuple.Create(curve, g.Item1, g.Item2));
    }

    doc.Regenerate();

    var ids = new List<string>();
    var rows = new List<string>();
    foreach (var m in made)
    {
        var read = m.Item1.GetPoints();
        var through = new List<ElementId>();
        if (read != null) for (var i = 0; i < read.Size; i++) through.Add(read.get_Item(i).Id);
        var forward = through.Count == m.Item2.Count && through.Select((id, i) => id == m.Item2[i].Id).All(x => x);
        var backward = through.Count == m.Item2.Count
            && through.Select((id, i) => id == m.Item2[m.Item2.Count - 1 - i].Id).All(x => x);
        if (!forward && !backward)
            throw new InvalidOperationException("The curve drawn through \"" + m.Item3 + "\" reads through " + through.Count
                + " point(s), not the ones asked. The call failed, and Heron rolls the whole call back.");
        if (m.Item1.IsReferenceLine != referenceLines)
            throw new InvalidOperationException("The curve through \"" + m.Item3 + "\" reads as a "
                + (m.Item1.IsReferenceLine ? "reference" : "model") + " line, not as asked. The call failed, and Heron "
                + "rolls the whole call back.");
        var length = 0.0;
        try { length = m.Item1.GeometryCurve.Length; } catch (Exception) { }
        ids.Add(m.Item1.UniqueId);
        rows.Add((m.Item2.Count == 2 ? "line" : "spline") + " " + m.Item1.UniqueId + " through " + m.Item3 + ", "
            + mm(length) + " mm");
    }

    curveIds = string.Join(",", ids);
    drawnCurves = string.Join("  ||  ", rows);
    findings.Add(made.Count + " " + (referenceLines ? "reference" : "model") + " curve(s) drawn through the points "
        + "and read back. Each is hosted by its points and moves with them.");
    findings.Add("A surface or a solid on these curves is CREATE_CONCEPTUAL_FORM, named by these ids.");
}

if (refused != null) findings.Add(refused);
