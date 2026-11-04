// NOT STANDALONE. Assumes `doc`, `shapeName`, `segments`, `bends`,
// `shapeStyle`, `attachment`, `startHook` and `endHook` are in scope; leaves
// `created`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A NEW REBAR SHAPE FROM ITS SEGMENTS, as Structure > Reinforcement > Rebar
// Shape's own families are drawn: a RebarShapeDefinitionBySegments, one
// segment per length, each segment's length driven by a rebar shape parameter
// A, B, C ... in order, each segment's direction fixed by the bends, a default
// radius bend at every corner, then RebarShape.Create. EVERY VALUE IS THE
// MODELLER'S (D-33): the lengths in millimetres become feet by / 304.8 (D-20);
// the bends are degrees turned at each corner, LEFT positive and RIGHT
// negative, walking the bar from its start; nothing is defaulted.
//
// THE SHAPE PARAMETERS ARE THE MODEL'S OWN. A rebar shape length must be a
// shared parameter already in the document (RevitAPI.xml, AddParameter). The
// letters the loaded shapes use are found with
// RebarShapeParameters.GetAllRebarShapeParameters and used by name; a model
// without them is refused, never given new shared parameters from a file.
//
// WHICH BEND CATEGORY REVIT MEANS IS NOT WRITTEN DOWN. RebarShapeBendAngle is
// Acute, Right or Obtuse. Read first as the angle the bar turns through
// (under 90 acute, over 90 obtuse - a 180 degree bend is a method of its own);
// if Revit's solver cannot solve the shape that way it is built once more with
// the two swapped, and the answer says which one solved (row 5b-1454).
//
// THE TWO CREATE ROUTES, ONE #if: the hook-angle form is 2020-2026 (obsolete
// at 2026, gone at 2027); 2026 on take a RebarShapeTerminationsData.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);

var created = new List<ElementId>();
var readBack = "";
var refused = "";
var findings = new List<string>();

// An optional need left out arrives as the text "none" (row 5b-1432).
Func<string, string> given = text =>
{
    var t = (text ?? "").Trim();
    return string.Equals(t, "none", StringComparison.OrdinalIgnoreCase) ? "" : t;
};

var name = (shapeName ?? "").Trim();
var segmentText = (segments ?? "").Trim();
var bendText = given(bends);
var styleText = (shapeStyle ?? "").Trim().ToLowerInvariant();
var attachText = given(attachment).ToLowerInvariant();
var startText = given(startHook).ToLowerInvariant();
var endText = given(endHook).ToLowerInvariant();

// ---- the request, read once, before anything is made -----------------------------
var lengths = new List<double>();
var turns = new List<double>();
var style = RebarStyle.Standard;
var attach = StirrupTieAttachmentType.InteriorFace;
int startAngle = 0, endAngle = 0;
bool startLeft = true, endLeft = true;

// A hook: "none", or a whole number of degrees with left or right - "135 left".
Func<string, string, Tuple<string, int, bool>> readHook = (text, which) =>
{
    if (text.Length == 0 || text == "0") return Tuple.Create("", 0, true);
    var words = text.Replace("degrees", " ").Replace("degree", " ").Replace("deg", " ").Replace("°", " ")
        .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
    int degrees;
    var hasLeft = words.Contains("left");
    var hasRight = words.Contains("right");
    var number = words.FirstOrDefault(w => w != "left" && w != "right");
    if (number == null || !int.TryParse(number, System.Globalization.NumberStyles.Integer, invariant, out degrees)
        || degrees < 0 || degrees > 180)
        return Tuple.Create("'" + text + "' is not a " + which + " hook - give a whole number of degrees, 0 to 180, "
            + "and left or right: \"135 left\". Leave it out for no hook", 0, true);
    if (degrees > 0 && hasLeft == hasRight)
        return Tuple.Create("The " + which + " hook '" + text + "' needs one turn, left or right - which way the hook "
            + "points is never chosen for you", 0, true);
    return Tuple.Create("", degrees, !hasRight);
};

foreach (var piece in segmentText.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
{
    var p = piece.Trim();
    if (p.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) p = p.Substring(0, p.Length - 2).Trim();
    double value;
    if (!double.TryParse(p, styleFloat, invariant, out value) || double.IsNaN(value) || double.IsInfinity(value)
        || value <= 0 || value > 100000)
    {
        refused = "'" + piece + "' is not a segment length - give each segment in millimetres, more than 0: "
            + "\"300, 600, 300\". Nothing was made";
        break;
    }
    lengths.Add(value / 304.8);
}

if (refused.Length == 0 && bendText.Length > 0)
{
    foreach (var piece in bendText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var words = piece.Trim().ToLowerInvariant().Replace("°", " ").Replace("degrees", " ").Replace("deg", " ")
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var number = words.FirstOrDefault(w => w != "left" && w != "right");
        double value;
        if (number == null || !double.TryParse(number, styleFloat, invariant, out value) || double.IsNaN(value))
        {
            refused = "'" + piece.Trim() + "' is not a bend - give the degrees turned at each corner, left or "
                + "right: \"90 left, 90 left\" (or +90, -90). Nothing was made";
            break;
        }
        if (words.Contains("right")) value = -Math.Abs(value);
        else if (words.Contains("left")) value = Math.Abs(value);
        if (Math.Abs(value) <= 0 || Math.Abs(value) >= 180)
        {
            refused = "A bend of " + piece.Trim() + " degrees is not one this makes - more than 0 and less than "
                + "180. Nothing was made";
            break;
        }
        turns.Add(value);
    }
}

if (refused.Length == 0 && name.Length == 0)
    refused = "No name was given for the rebar shape, and Heron does not choose one. Nothing was made";
else if (refused.Length == 0 && lengths.Count == 0)
    refused = "No segment lengths were given - the shape is its segments, in millimetres: \"300, 600, 300\". "
        + "Nothing was made";
else if (refused.Length == 0 && lengths.Count > 26)
    refused = lengths.Count + " segments is more than the letters A to Z this names them by. Nothing was made";
else if (refused.Length == 0 && turns.Count != lengths.Count - 1)
    refused = lengths.Count + " segment(s) need " + (lengths.Count - 1) + " bend(s), one at each corner - "
        + turns.Count + " were given. Nothing was made";
else if (refused.Length == 0 && styleText != "standard" && styleText != "stirrup/tie" && styleText != "stirrup" && styleText != "tie")
    refused = "'" + (shapeStyle ?? "") + "' is not a rebar shape style. Say standard, for main bars, or "
        + "stirrup/tie, for links. Nothing was made";
else if (refused.Length == 0)
{
    style = styleText == "standard" ? RebarStyle.Standard : RebarStyle.StirrupTie;
    if (style == RebarStyle.StirrupTie)
    {
        if (attachText.StartsWith("interior", StringComparison.Ordinal)) attach = StirrupTieAttachmentType.InteriorFace;
        else if (attachText.StartsWith("exterior", StringComparison.Ordinal)) attach = StirrupTieAttachmentType.ExteriorFace;
        else
            refused = "A stirrup/tie shape needs its attachment: interior (cover is measured to the stirrup) or "
                + "exterior (cover is measured to the main bars) - it is never chosen for you. Nothing was made";
    }
    else if (attachText.Length > 0)
        findings.Add("The attachment '" + attachText + "' is for stirrup/tie shapes; Revit ignores it on a standard one.");

    if (refused.Length == 0)
    {
        var s = readHook(startText, "start");
        var e = readHook(endText, "end");
        if (s.Item1.Length > 0) refused = s.Item1 + ". Nothing was made";
        else if (e.Item1.Length > 0) refused = e.Item1 + ". Nothing was made";
        else { startAngle = s.Item2; startLeft = s.Item3; endAngle = e.Item2; endLeft = e.Item3; }
    }
}

// ---- the name, and the model's own shape parameters ------------------------------
var letterIds = new List<ElementId>();
if (refused.Length == 0)
{
    var same = new FilteredElementCollector(doc).OfClass(typeof(RebarShape)).Cast<RebarShape>()
        .FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.Ordinal));
    if (same != null)
        refused = "A Rebar Shape named '" + name + "' is already in the model (id " + same.Id + "). Nothing was made - "
            + "give another name";
}
if (refused.Length == 0)
{
    var byName = new Dictionary<string, ElementId>(StringComparer.Ordinal);
    var all = RebarShapeParameters.GetAllRebarShapeParameters(doc);
    var wanted = new HashSet<ElementId>(all);
    foreach (var id in all)
    {
        var element = doc.GetElement(id) as ParameterElement;
        if (element == null) continue;
        var definition = element.GetDefinition();
        var parameterName = definition != null ? definition.Name : element.Name;
        if (!string.IsNullOrEmpty(parameterName) && !byName.ContainsKey(parameterName)) byName[parameterName] = id;
    }
    // Where an id is not an element here, the loaded shapes' own shared parameters name it.
    foreach (RebarShape loaded in new FilteredElementCollector(doc).OfClass(typeof(RebarShape)))
        foreach (Parameter p in loaded.Parameters)
            if (p.IsShared && p.Definition != null && wanted.Contains(p.Id) && !byName.ContainsKey(p.Definition.Name))
                byName[p.Definition.Name] = p.Id;

    var missing = new List<string>();
    for (var i = 0; i < lengths.Count; i++)
    {
        var letter = ((char)('A' + i)).ToString();
        ElementId id;
        if (byName.TryGetValue(letter, out id)) letterIds.Add(id);
        else missing.Add(letter);
    }
    if (missing.Count > 0)
        refused = "This model has no rebar shape parameter " + string.Join(", ", missing) + " - a shape's lengths "
            + "are the model's own shared parameters, and Heron does not add them. "
            + (byName.Count == 0 ? "No rebar shapes with parameters are loaded: load the rebar shapes from the "
                + "structural template first"
               : "The ones loaded are: " + string.Join(", ", byName.Keys.OrderBy(k => k, StringComparer.Ordinal).Take(40)))
            + ". Nothing was made";
}

// ---- the definition, built and solved before anything is made --------------------
var longest = 0;
for (var i = 1; i < lengths.Count; i++) if (lengths[i] > lengths[longest]) longest = i;

Func<bool, RebarShapeDefinitionBySegments> build = swap =>
{
    var definition = new RebarShapeDefinitionBySegments(doc, lengths.Count);
    for (var i = 0; i < lengths.Count; i++) definition.AddParameter(letterIds[i], lengths[i]);
    var heading = 0.0;
    for (var i = 0; i < lengths.Count; i++)
    {
        if (i > 0) heading += turns[i - 1] * Math.PI / 180.0;
        // Measured to the outside of a bend at a corner, from the free end at either end of the bar.
        definition.AddConstraintParallelToSegment(i, letterIds[i], i > 0, i < lengths.Count - 1);
        definition.SetSegmentFixedDirection(i, Math.Cos(heading), Math.Sin(heading));
    }
    for (var v = 1; v < lengths.Count; v++)
    {
        var turned = Math.Abs(turns[v - 1]);
        var category = Math.Abs(turned - 90.0) < 1e-6 ? RebarShapeBendAngle.Right
            : ((turned < 90.0) != swap ? RebarShapeBendAngle.Acute : RebarShapeBendAngle.Obtuse);
        definition.AddBendDefaultRadius(v, turns[v - 1] > 0 ? RebarShapeVertexTurn.Left : RebarShapeVertexTurn.Right,
            category);
    }
    definition.MajorSegmentIndex = longest;
    return definition;
};

RebarShapeDefinitionBySegments solved = null;
var swapped = false;
if (refused.Length == 0)
{
    var why = "";
    var hasOblique = turns.Any(t => Math.Abs(Math.Abs(t) - 90.0) >= 1e-6);
    foreach (var swap in hasOblique ? new[] { false, true } : new[] { false })
    {
        try
        {
            var definition = build(swap);
            if (!definition.Complete) { why = "the shape is not fully constrained"; continue; }
            if (!definition.CheckDefaultParameterValues(0.0, 0.0)) { why = "Revit's rebar solver cannot solve it"; continue; }
            solved = definition;
            swapped = swap;
            break;
        }
        catch (Exception ex)
        {
            why = ex.Message;
        }
    }
    if (solved == null)
        refused = "Revit would not take these segments and bends as a rebar shape: " + why + ". Nothing was made";
}

// ---- the shape --------------------------------------------------------------------
if (refused.Length == 0)
{
    RebarShape made = null;
    try
    {
#if REVIT2026_OR_GREATER
        var ends = new RebarShapeTerminationsData(doc);
        ends.HookAngleAtStart = startAngle;
        ends.HookAngleAtEnd = endAngle;
        ends.TerminationOrientationAtStart = startLeft ? RebarTerminationOrientation.Left : RebarTerminationOrientation.Right;
        ends.TerminationOrientationAtEnd = endLeft ? RebarTerminationOrientation.Left : RebarTerminationOrientation.Right;
        made = RebarShape.Create(doc, solved, null, style, attach, 0, ends);
#else
        made = RebarShape.Create(doc, solved, null, style, attach,
            startAngle, startLeft ? RebarHookOrientation.Left : RebarHookOrientation.Right,
            endAngle, endLeft ? RebarHookOrientation.Left : RebarHookOrientation.Right, 0);
#endif
        made.Name = name;
        doc.Regenerate();
        created.Add(made.Id);

        // READ BACK, from the shape Revit now holds.
        var back = made.GetRebarShapeDefinition() as RebarShapeDefinitionBySegments;
        var parts = new List<string>();
        if (back != null)
            for (var i = 0; i < lengths.Count; i++)
                parts.Add(((char)('A' + i)) + " " + (back.HasParameter(letterIds[i])
                    ? mm(back.GetParameterDefaultValue(letterIds[i])) + " mm" : "missing"));
#if REVIT2026_OR_GREATER
        var heldEnds = made.GetTerminationsData();
        var heldStart = heldEnds.HookAngleAtStart;
        var heldEnd = heldEnds.HookAngleAtEnd;
#else
        var heldStart = made.GetDefaultHookAngle(0);
        var heldEnd = made.GetDefaultHookAngle(1);
#endif
        var allowed = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>()
            .Where(t => made.GetAllowed(t)).Count();
        readBack = "'" + made.Name + "' (id " + made.Id + "), " + (made.RebarStyle == RebarStyle.StirrupTie
                ? "stirrup/tie, attached to the " + (made.StirrupTieAttachment == StirrupTieAttachmentType.ExteriorFace
                    ? "exterior" : "interior") + " face of cover"
                : "standard")
            + ", " + (back == null ? "?" : back.NumberOfSegments.ToString(invariant)) + " segment(s): "
            + string.Join(", ", parts)
            + "; bends " + (turns.Count == 0 ? "none" : string.Join(", ", turns.Select(t =>
                Math.Abs(t).ToString("0.##", invariant) + (t > 0 ? " left" : " right"))))
            + "; start hook " + (heldStart == 0 ? "none" : heldStart + " degrees")
            + ", end hook " + (heldEnd == 0 ? "none" : heldEnd + " degrees")
            + "; allowed for " + allowed + " bar type(s)";
        findings.Add("Made Rebar Shape " + readBack + ".");
        if (swapped)
            findings.Add("The bend categories solved with acute and obtuse read as the angle BETWEEN the legs, "
                + "not the angle turned - recorded for row 5b-1454.");
        if (back == null || back.NumberOfSegments != lengths.Count || parts.Any(p => p.EndsWith("missing", StringComparison.Ordinal))
            || made.RebarStyle != style || heldStart != startAngle || heldEnd != endAngle)
            findings.Add("Read back different from what was asked.");
    }
    catch (Exception ex)
    {
        refused = "Revit refused the rebar shape: " + ex.Message + ". Nothing was kept";
        if (made != null && made.IsValidObject)
        {
            try { doc.Delete(made.Id); } catch (Exception) { }
        }
        created.Clear();
        readBack = "";
    }
}

if (refused.Length > 0) findings.Add(refused + ".");
