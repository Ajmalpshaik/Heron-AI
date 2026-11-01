// NOT STANDALONE. Assumes `doc`, `elements`, `angleDegrees` and `along` are in
// scope, and leaves `changed`, `changedIds`, `spanDegrees`, `alreadyThere`,
// `notFloors`, `notStructural`, `notChanged`, `report`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, and
// the caller owns it; nothing here opens or commits one.
//
// THE SPAN DIRECTION OF A STRUCTURAL FLOOR - Floor.SpanDirectionAngle, radians,
// get and set, present and unchanged 2020 to 2027 (tools/api-surface --members,
// 2026-11-01). DEGREES IN, RADIANS TO THE API.
//
// A NON-STRUCTURAL FLOOR HAS NONE. Revit's API documentation, every release:
// "An exception will be thrown if the floor is non structural." So the
// Structural box is read first and a floor with it clear is named and left; a
// floor whose angle still cannot be read is named the same way with Revit's own
// words. Ticking Structural is never done here as a side effect.
//
// A DIRECTION GIVEN BY AN ELEMENT IS READ OFF THAT ELEMENT, NEVER GUESSED.
// "grid NAME", "plane NAME" or "line ID" - a bare name tried as a grid, then a
// reference plane. Its line is flattened onto the plan; a curved grid, an arc,
// a multi-segment grid, a line that runs straight up, or a name that means two
// reference planes is refused by name and nothing is changed.
//
// `angleDegrees` alone is measured anticlockwise from the model's X axis; with
// `along` it is measured anticlockwise from that element's direction.
//
// A SPAN IS AN AXIS: 90 and 270 degrees are the same span. A floor already on
// it is left alone, and the read-back compares the axis, not the arrow.
//
// READ BACK: after a regeneration each floor's angle is read again, and the
// floor's own Span Direction parameter is reported beside it. One that
// did not take is set back to what it was and named.

const double DegreesPerRadian = 180.0 / Math.PI;
const double SameAxisRadians = 0.01 / DegreesPerRadian;    // a hundredth of a degree

var changed = 0;
var changedIds = new List<ElementId>();
double spanDegrees = 0;
var alreadyThere = new List<ElementId>();
var notFloors = new List<ElementId>();
var notStructural = new List<ElementId>();
var notChanged = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refused = "";

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;

// "none" is what the card binds a value left out to.
Func<string, string> given = text =>
{
    var said = (text ?? "").Trim();
    return said.Equals("none", StringComparison.OrdinalIgnoreCase) ? "" : said;
};

// Degrees to one decimal, the way a modeller reads them.
Func<double, string> degreesText = radians =>
    Math.Round(radians * DegreesPerRadian, 2).ToString("0.##", culture) + " deg";

// Two angles are the same SPAN when they differ by a multiple of 180 degrees.
Func<double, double, bool> sameAxis = (a, b) =>
{
    var gap = Math.Abs(a - b) % Math.PI;
    return Math.Min(gap, Math.PI - gap) < SameAxisRadians;
};

// The element named by id, of one kind - by the id's text, never as a number.
Func<string, Type, Element> byId = (text, kind) =>
{
    var said = (text ?? "").Trim();
    if (said.Length == 0) return null;
    foreach (var e in new FilteredElementCollector(doc).OfClass(kind).WhereElementIsNotElementType())
        if (e.Id.ToString() == said) return e;
    return null;
};

// ---- the angle asked for --------------------------------------------------

var angleText = given(angleDegrees);
var alongText = given(along);
double? askedDegrees = null;
if (angleText.Length > 0)
{
    var bare = angleText.Replace("°", "").Trim();
    foreach (var unit in new[] { "degrees", "degree", "deg" })
        if (bare.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
        { bare = bare.Substring(0, bare.Length - unit.Length).Trim(); break; }
    double parsed;
    if (double.TryParse(bare, numberStyle, culture, out parsed) && !double.IsNaN(parsed) && !double.IsInfinity(parsed))
        askedDegrees = parsed;
    else
        refused = "\"" + angleText + "\" is not an angle in degrees - say it as a number, \"90\" or \"45.5\". "
                + "Nothing was changed.";
}
if (refused.Length == 0 && angleText.Length == 0 && alongText.Length == 0)
    refused = "No angle and no grid, reference plane or line was given, and a span direction is never "
            + "guessed. Say an angle in degrees, or what it runs along - \"grid 1\". Nothing was changed.";

// ---- the direction of the element it runs along -------------------------------

double baseRadians = 0;
var baseName = "the model's X axis";
if (refused.Length == 0 && alongText.Length > 0)
{
    var space = alongText.IndexOf(' ');
    var word = (space < 0 ? alongText : alongText.Substring(0, space)).ToLowerInvariant();
    var rest = space < 0 ? "" : alongText.Substring(space + 1).Trim();
    if (word == "reference" && rest.StartsWith("plane ", StringComparison.OrdinalIgnoreCase))
    {
        word = "plane";
        rest = rest.Substring(6).Trim();
    }
    if (word != "grid" && word != "plane" && word != "line")
    {
        word = "";                              // a bare name: a grid, then a plane
        rest = alongText;
    }

    XYZ direction = null;
    string problem = null;

    if (word == "grid" || word == "")
    {
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().ToList();
        var grid = grids.FirstOrDefault(g => string.Equals(g.Name, rest, StringComparison.Ordinal));
        if (grid == null)
        {
            var loose = grids.Where(g => string.Equals(g.Name, rest, StringComparison.OrdinalIgnoreCase)).ToList();
            if (loose.Count == 1) grid = loose[0];
        }
        if (grid != null)
        {
            var straight = grid.IsCurved ? null : grid.Curve as Line;
            if (straight == null) problem = "Grid " + grid.Name + " is curved, and an arc runs no one way. Name a straight grid, or give the angle.";
            else { direction = straight.Direction; baseName = "grid " + grid.Name; }
        }
        else
        {
            var multi = new FilteredElementCollector(doc).OfClass(typeof(MultiSegmentGrid))
                .FirstOrDefault(m => string.Equals(m.Name, rest, StringComparison.OrdinalIgnoreCase));
            if (multi != null)
                problem = "Grid " + multi.Name + " is a multi-segment grid, which runs several ways. Name one straight grid, or give the angle.";
            else if (word == "grid")
                problem = "No grid is called \"" + rest + "\" - the name has to match the grid's head.";
        }
    }

    if (problem == null && direction == null && (word == "plane" || word == ""))
    {
        var planes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
            .Where(p => !string.IsNullOrEmpty(p.Name) && string.Equals(p.Name, rest, StringComparison.Ordinal)).ToList();
        if (planes.Count > 1)
            problem = planes.Count + " reference planes are called \"" + rest + "\", so the name does not say which. Name a grid or a line instead.";
        else if (planes.Count == 1)
        {
            direction = planes[0].FreeEnd - planes[0].BubbleEnd;
            baseName = "reference plane " + planes[0].Name;
        }
        else if (word == "plane")
            problem = "No reference plane is called \"" + rest + "\".";
        else
            problem = "No grid or reference plane is called \"" + rest + "\". Say grid NAME, plane NAME or line ID.";
    }

    if (problem == null && direction == null && word == "line")
    {
        var element = byId(rest, typeof(CurveElement)) as CurveElement;
        var straight = element == null ? null : element.GeometryCurve as Line;
        if (element == null) problem = "No model or detail line has the id \"" + rest + "\".";
        else if (straight == null) problem = "Line " + rest + " is not straight, and a curve runs no one way.";
        else { direction = straight.Direction; baseName = "line " + rest; }
    }

    if (problem == null && direction != null)
    {
        var flat = new XYZ(direction.X, direction.Y, 0);
        if (flat.GetLength() < 1e-9)
            problem = baseName + " runs straight up, so seen in a plan it has no direction.";
        else
            baseRadians = Math.Atan2(flat.Y, flat.X);
    }
    if (problem == null && direction == null)
        problem = "\"" + alongText + "\" is not something a span can run along. Say grid NAME, plane NAME or line ID.";

    if (problem != null) refused = problem + " Nothing was changed.";
}

// The span asked for, as an axis between 0 and 180 degrees.
double targetRadians = 0;
if (refused.Length == 0)
{
    targetRadians = baseRadians + (askedDegrees ?? 0) / DegreesPerRadian;
    targetRadians = targetRadians % Math.PI;
    if (targetRadians < 0) targetRadians += Math.PI;
    if (Math.PI - targetRadians < SameAxisRadians) targetRadians = 0;
    spanDegrees = Math.Round(targetRadians * DegreesPerRadian, 4);
}

// ---- each floor --------------------------------------------------------------

if (refused.Length == 0)
{
    var seen = new HashSet<ElementId>();
    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null || !seen.Add(element.Id)) continue;

        var floor = element as Floor;
        if (floor == null)
        {
            notFloors.Add(element.Id);
            continue;
        }

        bool inThisModel = false;
        try { inThisModel = floor.Document != null && floor.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notChanged.Add(floor.Id);
            lines.Add(floor.Id + ": of another model - left alone.");
            continue;
        }

        string typeName = "floor";
        try { if (floor.FloorType != null) typeName = floor.FloorType.Name; } catch { }

        // STRUCTURAL FIRST - Revit keeps no span direction otherwise.
        var structural = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
        if (structural != null && structural.AsInteger() == 0)
        {
            notStructural.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): not structural - Revit keeps a span direction only for a "
                + "floor with Structural ticked in its Properties. Left alone.");
            continue;
        }

        double before;
        try { before = floor.SpanDirectionAngle; }
        catch (Exception failure)
        {
            notStructural.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): Revit has no span direction for it - " + failure.Message + " Left alone.");
            continue;
        }

        if (sameAxis(before, targetRadians))
        {
            alreadyThere.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): already spans " + degreesText(before) + ".");
            continue;
        }

        double after = before;
        try
        {
            floor.SpanDirectionAngle = targetRadians;
            doc.Regenerate();
            after = floor.SpanDirectionAngle;
        }
        catch (Exception failure)
        {
            notChanged.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): Revit refused - " + failure.Message);
            continue;
        }

        if (!sameAxis(after, targetRadians))
        {
            // It did not take as asked - put back as it was, inside the same undo.
            try { floor.SpanDirectionAngle = before; doc.Regenerate(); } catch { }
            notChanged.Add(floor.Id);
            lines.Add(floor.Id + " (" + typeName + "): read back " + degreesText(after) + " instead of "
                + degreesText(targetRadians) + " - put back to " + degreesText(before) + ".");
            continue;
        }

        // The floor's Span Direction parameter, beside the API's own angle - a
        // second reading, in whatever form Revit writes it.
        string shown = null;
        try
        {
            var property = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_SPAN_DIRECTION);
            if (property != null) shown = property.AsValueString();
        }
        catch { shown = null; }

        int symbols = 0;
        try { symbols = floor.GetSpanDirectionSymbolIds().Count; } catch { symbols = 0; }

        changed++;
        changedIds.Add(floor.Id);
        lines.Add(floor.Id + " (" + typeName + "): span " + degreesText(before) + " -> " + degreesText(after)
            + (string.IsNullOrEmpty(shown) ? "" : " (Span Direction parameter: " + shown + ")")
            + (symbols > 0 ? "; its " + symbols + " span direction symbol(s) turn with it" : "") + ".");
    }

    // THE WHOLE ANSWER AS ONE LINE - a reply shows a list's first entries and
    // its count, and this line carries every count in words.
    findings.Add(changed + " floor(s) set to span " + spanDegrees.ToString("0.##", culture) + " deg from the model's X axis"
        + (alongText.Length > 0 ? " (along " + baseName + (askedDegrees.HasValue ? ", turned " + askedDegrees.Value.ToString("0.##", culture) + " deg" : "") + ")" : "")
        + " and read back. Not changed: " + alreadyThere.Count + " already spanning that way, " + notFloors.Count
        + " not floors, " + notStructural.Count + " not structural, " + notChanged.Count + " refused, read back "
        + "otherwise or of another model.");
}
else
{
    lines.Add(refused);
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
