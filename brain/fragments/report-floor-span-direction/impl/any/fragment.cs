// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves `read`,
// `spans`, `symbolIds`, `notStructural`, `notFloors`, `report` and `findings`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE SPAN DIRECTION OF A STRUCTURAL FLOOR - Floor.SpanDirectionAngle,
// radians, present and unchanged 2020 to 2027 (tools/api-surface --members,
// 2026-11-01). RADIANS FROM THE API, DEGREES IN THE ANSWER, anticlockwise from
// the model's X axis - as SET_FLOOR_SPAN_DIRECTION sets it.
//
// A NON-STRUCTURAL FLOOR HAS NONE. Revit's API documentation, every release:
// "An exception will be thrown if the floor is non structural." So the
// Structural box is read first and a floor with it clear is named; a floor
// whose angle still cannot be read is named the same way with Revit's words.
// Never an angle of 0 for a floor that has no span.
//
// A SPAN IS AN AXIS: 90 and 270 degrees are the same span. The answer gives the
// angle Revit holds and the axis it means, 0 up to 180.
//
// THE SYMBOLS - Floor.GetSpanDirectionSymbolIds, 2020 to 2027. Each is read
// as an Element (the SpanDirectionSymbol class exists only from 2025), named
// with its type and the view it is drawn in.

const double DegreesPerRadian = 180.0 / Math.PI;

var read = 0;
var spans = new Dictionary<string, double>();
var symbolIds = new List<ElementId>();
var notStructural = new List<ElementId>();
var notFloors = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
var withoutSymbol = 0;

var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<double, string> degreesText = degrees =>
    Math.Round(degrees, 2).ToString("0.##", culture) + " deg";

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

    // The floor's own model - a selection is of this model, but every lookup
    // below asks the floor's document, never assumes it.
    var owner = floor.Document ?? doc;

    string typeName = "floor";
    try { if (floor.FloorType != null) typeName = floor.FloorType.Name; } catch { }

    // STRUCTURAL FIRST - Revit keeps no span direction otherwise.
    var structural = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
    if (structural != null && structural.AsInteger() == 0)
    {
        notStructural.Add(floor.Id);
        lines.Add(floor.Id + " (" + typeName + "): not structural - Revit keeps a span direction only for a "
            + "floor with Structural ticked in its Properties, so it has none.");
        continue;
    }

    double radians;
    try { radians = floor.SpanDirectionAngle; }
    catch (Exception failure)
    {
        notStructural.Add(floor.Id);
        lines.Add(floor.Id + " (" + typeName + "): Revit has no span direction for it - " + failure.Message);
        continue;
    }

    var degrees = Math.Round(radians * DegreesPerRadian, 4);
    var axis = degrees % 180.0;
    if (axis < 0) axis += 180.0;
    if (180.0 - axis < 0.005) axis = 0;

    // The floor's Span Direction parameter, beside the API's angle - a second
    // reading, in whatever form Revit writes it.
    string shown = null;
    try
    {
        var property = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_SPAN_DIRECTION);
        if (property != null) shown = property.AsValueString();
    }
    catch { shown = null; }

    // Its span direction symbols, each with its type and the view it is in.
    var symbolText = new List<string>();
    ICollection<ElementId> symbols = null;
    try { symbols = floor.GetSpanDirectionSymbolIds(); }
    catch { symbols = null; }
    if (symbols != null)
    {
        foreach (var symbolId in symbols)
        {
            if (ReferenceEquals(symbolId, null) || symbolId == ElementId.InvalidElementId) continue;
            symbolIds.Add(symbolId);

            var symbol = owner.GetElement(symbolId);
            string kind = "span direction symbol";
            string where = "a view";
            if (symbol != null)
            {
                try
                {
                    var type = owner.GetElement(symbol.GetTypeId()) as ElementType;
                    if (type != null)
                        kind = string.IsNullOrEmpty(type.FamilyName) ? type.Name : type.FamilyName + ": " + type.Name;
                }
                catch { }
                try
                {
                    var view = owner.GetElement(symbol.OwnerViewId) as View;
                    if (view != null) where = "\"" + view.Name + "\"";
                }
                catch { }
            }
            symbolText.Add(symbolId + " (" + kind + ") in " + where);
        }
    }
    if (symbolText.Count == 0) withoutSymbol++;

    read++;
    spans[floor.Id.ToString()] = degrees;
    lines.Add(floor.Id + " (" + typeName + "): spans " + degreesText(degrees) + " from the model's X axis"
        + (Math.Abs(axis - degrees) > 0.005 ? " - the same span as " + degreesText(axis) : "")
        + (string.IsNullOrEmpty(shown) ? "" : " (Span Direction parameter: " + shown + ")")
        + (symbolText.Count == 0
            ? "; no span direction symbol shows it"
            : "; shown by " + string.Join(", ", symbolText))
        + ".");
}

// THE WHOLE ANSWER AS ONE LINE - a reply shows a list's first entries and its
// count, and this line carries every count in words.
findings.Add(read + " floor(s) have a span direction, read in degrees from the model's X axis; "
    + symbolIds.Count + " span direction symbol(s) show them, and " + withoutSymbol + " of them have none. "
    + "Not read: " + notStructural.Count + " not structural (no span direction in Revit), "
    + notFloors.Count + " not floors.");

var report = string.Join(" | ", lines);
findings.AddRange(lines);
