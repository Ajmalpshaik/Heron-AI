// NOT STANDALONE. Assumes `doc`, `view` and `controls` are in scope; leaves
// `controlIds`, `placed`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// FLIP CONTROLS - the small arrows a placed door, window or piece of equipment
// shows when selected, which turn or mirror it in one click. The
// Family Editor's Control tool. A family has none until they are added, and a
// modeller then flips it by mirroring, which moves it.
//
// FOUR SHAPES, AS THE CONTROL TOOL NAMES THEM: single horizontal, double
// horizontal, single vertical, double vertical. What each does to a placed copy
// is Autodesk's remark on Control, 2020 to 2027: a SINGLE arrow, horizontal or
// vertical, ROTATES the copy 180 degrees; a DOUBLE arrow MIRRORS it, a double
// horizontal one horizontally and a double vertical one vertically. Each is
// placed at a point given in millimetres in the plan's own X,Y.
//
// A FLOOR PLAN OR A CEILING PLAN, AND NO OTHER VIEW - NewControl's own words for
// its view. Where a floor plan and a ceiling plan share the name the floor plan
// is taken; whether a template's "Ref. Level" is both is what BT1's view list
// shows.
//
// READ BACK, ALL OR NOTHING: every control's shape, view and position are read
// again; one that does not read as asked fails the call, and the host rolls the
// whole call back.

var findings = new List<string>();
var controlIds = "";
var placed = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> plain = value => Math.Round(value, 1).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<XYZ, int> axisOf = n =>
    n == null ? -1 : Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var inPlane = new[] { new[] { 1, 2 }, new[] { 0, 2 }, new[] { 0, 1 } };
var letters = new[] { "X", "Y", "Z" };

Func<string, double?> number = text =>
{
    double value;
    if (!double.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, invariant, out value))
        return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

// The shapes by every name a modeller or Revit gives them.
var shapesByName = new Dictionary<string, ControlShape>
{
    { "singlehorizontal", ControlShape.HorizontalArrow }, { "horizontal", ControlShape.HorizontalArrow },
    { "horizontalarrow", ControlShape.HorizontalArrow }, { "leftright", ControlShape.HorizontalArrow },
    { "doublehorizontal", ControlShape.DoubleHorizontalArrow }, { "doublehorizontalarrow", ControlShape.DoubleHorizontalArrow },
    { "doubleleftright", ControlShape.DoubleHorizontalArrow },
    { "singlevertical", ControlShape.VerticalArrow }, { "vertical", ControlShape.VerticalArrow },
    { "verticalarrow", ControlShape.VerticalArrow }, { "frontback", ControlShape.VerticalArrow },
    { "doublevertical", ControlShape.DoubleVerticalArrow }, { "doubleverticalarrow", ControlShape.DoubleVerticalArrow },
    { "doublefrontback", ControlShape.DoubleVerticalArrow },
};
Func<ControlShape, string> shapeWords = s => s == ControlShape.HorizontalArrow ? "single horizontal"
    : s == ControlShape.DoubleHorizontalArrow ? "double horizontal" : s == ControlShape.VerticalArrow ? "single vertical"
    : "double vertical";

var problems = new List<string>();
View target = null;
// Each control asked for: its shape and the two coordinates in millimetres.
var asked = new List<Tuple<ControlShape, double, double>>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "Flip controls are added inside the family - open it first.";
}
else
{
    var said = (view ?? "").Trim();
    var plans = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.CeilingPlan)).ToList();
    var found = plans.Where(v => string.Equals(v.Name, said, StringComparison.OrdinalIgnoreCase)).ToList();
    // The floor plan, where a floor plan and a ceiling plan share the name.
    if (found.Count > 1 && found.Count(v => v.ViewType == ViewType.FloorPlan) == 1)
        found = found.Where(v => v.ViewType == ViewType.FloorPlan).ToList();
    if (said.Length == 0 || found.Count != 1)
        problems.Add((said.Length == 0 ? "No view was named." : "No single floor or ceiling plan is called \"" + said + "\".")
            + " Revit puts a control in a floor plan or a ceiling plan only. This family's plans: "
            + (plans.Count == 0 ? "none" : string.Join(", ", plans.Select(v => v.Name + " (" + v.ViewType + ")").Take(12))) + ".");
    else if (axisOf(found[0].ViewDirection) < 0)
        problems.Add("The view \"" + said + "\" looks at an angle; a control is placed in model coordinates.");
    else target = found[0];

    var entries = (controls ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (entries.Count == 0)
        problems.Add("No controls were given - \"single horizontal 0,-400 | single vertical 400,0\", the shape and "
            + "then where, in millimetres.");
    foreach (var entry in entries)
    {
        // "0, -400" reads as "0,-400".
        var words = System.Text.RegularExpressions.Regex.Replace(entry, @"\s*,\s*", ",")
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var point = words.Count == 0 ? null : words[words.Count - 1].Split(',');
        var name = squash(string.Join(" ", words.Take(Math.Max(0, words.Count - 1))));
        ControlShape shape;
        if (!shapesByName.TryGetValue(name, out shape))
        {
            problems.Add("\"" + entry + "\" does not start with a control shape - single horizontal, double "
                + "horizontal, single vertical or double vertical.");
            continue;
        }
        var first = point != null && point.Length == 2 ? number(point[0]) : null;
        var second = point != null && point.Length == 2 ? number(point[1]) : null;
        if (!first.HasValue || !second.HasValue)
        {
            problems.Add("\"" + entry + "\" does not end with where it goes, \"X,Y\" in millimetres.");
            continue;
        }
        asked.Add(Tuple.Create(shape, first.Value, second.Value));
    }

    if (problems.Count > 0) refused = "Nothing was added. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// PLACE, THEN READ EVERY CONTROL BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var axis = axisOf(target.ViewDirection);
    var at = along(target.Origin, axis);
    var u = inPlane[axis][0];
    var v = inPlane[axis][1];
    var made = new List<Tuple<Control, ControlShape, XYZ>>();
    foreach (var a in asked)
    {
        var xyz = new double[3];
        xyz[axis] = at;
        xyz[u] = a.Item2 / 304.8;
        xyz[v] = a.Item3 / 304.8;
        var where = new XYZ(xyz[0], xyz[1], xyz[2]);
        Control control;
        try { control = doc.FamilyCreate.NewControl(a.Item1, target, where); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused a " + shapeWords(a.Item1) + " control at " + plain(a.Item2)
                + "," + plain(a.Item3) + " in \"" + target.Name + "\": " + ex.Message + " The call failed, and Heron "
                + "rolls the whole call back.");
        }
        if (control == null)
            throw new InvalidOperationException("Revit made no control at " + plain(a.Item2) + "," + plain(a.Item3)
                + ". The call failed, and Heron rolls the whole call back.");
        made.Add(Tuple.Create(control, a.Item1, where));
    }

    doc.Regenerate();

    var ids = new List<string>();
    var rows = new List<string>();
    foreach (var m in made)
    {
        var origin = m.Item1.Origin;
        var inView = m.Item1.View;
        var off = origin == null ? double.MaxValue
            : Math.Sqrt(Math.Pow(along(origin, u) - along(m.Item3, u), 2) + Math.Pow(along(origin, v) - along(m.Item3, v), 2));
        if (m.Item1.Shape != m.Item2 || inView == null || inView.Id != target.Id || off > halfMillimetre)
            throw new InvalidOperationException("A control reads " + shapeWords(m.Item1.Shape) + " in "
                + (inView == null ? "no view" : "\"" + inView.Name + "\"") + ", not the " + shapeWords(m.Item2)
                + " placed in \"" + target.Name + "\" where it was asked. The call failed, and Heron rolls the whole "
                + "call back.");
        ids.Add(m.Item1.UniqueId);
        rows.Add(shapeWords(m.Item2) + " at " + letters[u] + " " + plain(along(origin, u) * 304.8) + ", " + letters[v] + " "
            + plain(along(origin, v) * 304.8) + " mm");
    }
    controlIds = string.Join(",", ids);
    placed = made.Count + " flip control(s) in \"" + target.Name + "\": " + string.Join("; ", rows) + " - read back.";
    findings.Add(placed);
    findings.Add("What each control does to a placed copy is Revit's: Autodesk's remark on Control says a single "
        + "arrow rotates it 180 degrees and a double one mirrors it, and NEEDS-CHECKING BT3 records what a placed copy "
        + "does. Copies already placed in a project take the controls when the family is loaded there again.");
}

if (refused != null) findings.Add(refused);
