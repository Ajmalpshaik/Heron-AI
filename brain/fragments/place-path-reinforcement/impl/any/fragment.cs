// NOT STANDALONE. Assumes `doc`, `elements`, `path`, `bars`, `barLength`,
// `face`, `flip`, `hook` and `pathReinforcementType` are in scope; leaves
// `placed`, `barsInPath`, `readBackWrong`, `report`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// PATH REINFORCEMENT: bars laid ACROSS a path drawn on one slab or wall - an
// edge strip, the top of a wall, a band over a beam - each bar the length
// asked, at the spacing asked. The path arrives as lines in millimetres.
// Bar type, spacing, length and face are the modeller's (D-33, D-71).
//
// THE FACE is set by Revit's own parameter id (PATH_REIN_FACE_SLAB or
// PATH_REIN_FACE_WALL), whose stored number is not documented: each value is
// tried and the one Revit DISPLAYS as the face asked is kept, read back. A face
// none of them displays is left as Revit placed it and said.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var styleFloat = System.Globalization.NumberStyles.Float;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", invariant);
Func<RebarBarType, double> nominalOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarNominalDiameter;
#endif
};

var placed = new List<ElementId>();
var barsInPath = 0;
var readBackWrong = new List<string>();
var findings = new List<string>();
var refused = "";
var report = "";

var barTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
var hookTypes = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>().ToList();
var pathTypes = new FilteredElementCollector(doc).OfClass(typeof(PathReinforcementType)).Cast<PathReinforcementType>().ToList();
Func<string> barList = () => barTypes.Count == 0 ? "none" : string.Join(", ", barTypes.OrderBy(t => nominalOf(t))
    .Select(t => "'" + t.Name + "' " + mm(nominalOf(t)) + " mm"));

// A bar type by its exact name, or a diameter in millimetres matched to the ONE
// type of that nominal diameter. Null with `refused` set.
Func<string, string, RebarBarType> barTypeFrom = (text, role) =>
{
    var asked = (text ?? "").Trim();
    var named = barTypes.FirstOrDefault(t => string.Equals(t.Name, asked, StringComparison.Ordinal));
    if (named != null) return named;
    var number = asked.EndsWith("mm", StringComparison.OrdinalIgnoreCase) ? asked.Substring(0, asked.Length - 2).Trim() : asked;
    double diameter;
    if (!double.TryParse(number, styleFloat, invariant, out diameter) || diameter <= 0)
    {
        refused = "No Rebar Bar Type is named '" + asked + "' (the " + role + " asked). Loaded: " + barList()
            + ". Give one of these names, or the diameter in millimetres. If '" + asked + "' is a bar mark from the "
            + "drawings, say which type or diameter it means. Nothing was placed";
        return null;
    }
    var matching = barTypes.Where(t => Math.Abs(nominalOf(t) * 304.8 - diameter) < 0.05).ToList();
    if (matching.Count == 1) return matching[0];
    refused = matching.Count > 1
        ? matching.Count + " bar types are " + diameter.ToString("0.#", invariant) + " mm: "
            + string.Join(", ", matching.Select(t => "'" + t.Name + "'")) + ". Say which, by its name. Nothing was placed"
        : "No Rebar Bar Type of " + diameter.ToString("0.#", invariant) + " mm is loaded. Loaded: " + barList()
            + ". CREATE_REBAR_BAR_TYPE makes one from the spec's diameter and bend diameters. Nothing was placed";
    return null;
};
// "<bar type> @ <spacing>" or "none". Spacing in feet.
Func<string, string, Tuple<RebarBarType, double>> typeAndSpacing = (text, role) =>
{
    var asked = (text ?? "").Trim();
    if (asked.Equals("none", StringComparison.OrdinalIgnoreCase)) return Tuple.Create((RebarBarType)null, 0.0);
    var at = asked.LastIndexOf('@');
    double spacing;
    var spacingText = at < 0 ? "" : asked.Substring(at + 1).Trim();
    if (spacingText.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) spacingText = spacingText.Substring(0, spacingText.Length - 2).Trim();
    if (at <= 0 || !double.TryParse(spacingText, styleFloat, invariant, out spacing) || spacing <= 0)
    {
        refused = "'" + asked + "' is not a bar type and spacing. Say it as <bar type> @ <spacing in mm> - 12mm @ 200 "
            + "- or none. Nothing was placed";
        return null;
    }
    var type = barTypeFrom(asked.Substring(0, at), role);
    return type == null ? null : Tuple.Create(type, spacing / 304.8);
};

Func<Element, BuiltInParameter, Parameter> param = (e, bip) =>
{
    try { return e.get_Parameter(bip); } catch (Exception) { return null; }
};

// ---- the request, read once ------------------------------------------------------------
var hosts = elements.Where(e => e != null).ToList();
Element host = null;
if (hosts.Count != 1)
    refused = "A path is drawn on ONE slab or wall, and " + hosts.Count + " element(s) were handed in. Select the one it "
        + "goes on. Nothing was placed";
else
{
    host = hosts[0];
    if (!(host is Floor) && !(host is Wall))
        refused = "'" + host.Name + "' is not a slab or a wall - path reinforcement goes only on those. Nothing was placed";
    else if (!RebarHostData.IsValidHost(host))
        refused = "'" + host.Name + "' is not a rebar host - its type is not structural, or its material is not concrete. Nothing was placed";
}
var asked = refused.Length == 0 ? typeAndSpacing(bars, "path bars") : null;
if (refused.Length == 0 && asked.Item1 == null)
    refused = "The bars were asked as none, so there is nothing to place. Nothing was placed";
var lengthText = (barLength ?? "").Trim();
if (lengthText.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) lengthText = lengthText.Substring(0, lengthText.Length - 2).Trim();
double lengthMm = 0;
if (refused.Length == 0 && (!double.TryParse(lengthText, styleFloat, invariant, out lengthMm) || lengthMm <= 0))
    refused = "'" + (barLength ?? "") + "' is not a bar length in millimetres. Nothing was placed";
var curves = path == null ? new List<Curve>() : path.ToList();
if (refused.Length == 0 && curves.Count == 0)
    refused = "No path was given. Give it as lines in millimetres - x,y,z; x,y,z | x,y,z; x,y,z. Nothing was placed";
for (var i = 1; refused.Length == 0 && i < curves.Count; i++)
    if (curves[i - 1].GetEndPoint(1).DistanceTo(curves[i].GetEndPoint(0)) > 1 / 304.8)
        refused = "Line " + (i + 1) + " of the path does not start where line " + i + " ends - a path is one run, end to end. Nothing was placed";

var faceWord = (face ?? "").Trim().ToLowerInvariant();
if (refused.Length == 0)
{
    var wallWords = new[] { "exterior", "interior" };
    var slabWords = new[] { "top", "bottom" };
    if (host is Wall ? !wallWords.Contains(faceWord) : !slabWords.Contains(faceWord))
        refused = "'" + (face ?? "") + "' is not a face of a " + (host is Wall ? "wall - say exterior or interior" : "slab - say top or bottom")
            + ". Nothing was placed";
}

RebarHookType hookType = null;
var hookText = (hook ?? "").Trim();
if (refused.Length == 0 && hookText.Length > 0 && !hookText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    hookType = hookTypes.FirstOrDefault(h => string.Equals(h.Name, hookText, StringComparison.Ordinal));
    if (hookType == null || hookType.Style != RebarStyle.Standard)
        refused = (hookType == null ? "No Rebar Hook Type is named '" + hookText + "'" : "'" + hookText + "' is a hook for stirrups and ties")
            + ". Hooks for these bars: " + string.Join(", ", hookTypes.Where(h => h.Style == RebarStyle.Standard).Select(h => "'" + h.Name + "'"))
            + " - or none. Nothing was placed";
}

PathReinforcementType pathType = null;
var pathTypeText = (pathReinforcementType ?? "").Trim();
if (refused.Length == 0)
{
    if (pathTypeText.Length == 0 || pathTypeText.Equals("only", StringComparison.OrdinalIgnoreCase))
    {
        if (pathTypes.Count == 1) pathType = pathTypes[0];
        else if (pathTypes.Count > 1)
            refused = pathTypes.Count + " Path Reinforcement Types are loaded: " + string.Join(", ", pathTypes.Select(t => "'" + t.Name + "'"))
                + ". Say which, by its name. Nothing was placed";
    }
    else
    {
        pathType = pathTypes.FirstOrDefault(t => string.Equals(t.Name, pathTypeText, StringComparison.Ordinal));
        if (pathType == null)
            refused = "No Path Reinforcement Type is named '" + pathTypeText + "'. Loaded: "
                + (pathTypes.Count == 0 ? "none" : string.Join(", ", pathTypes.Select(t => "'" + t.Name + "'"))) + ". Nothing was placed";
    }
}

if (refused.Length == 0)
{
    if (pathType == null)
    {
        pathType = doc.GetElement(PathReinforcementType.CreateDefaultPathReinforcementType(doc)) as PathReinforcementType;
        findings.Add("No Path Reinforcement Type was loaded, so Revit's default one was made"
            + (pathType == null ? "" : ": '" + pathType.Name + "'") + ".");
    }
    PathReinforcement made = null;
    string failure = null;
    var faceShown = "";
    try
    {
        var hookId = hookType == null ? ElementId.InvalidElementId : hookType.Id;
        made = PathReinforcement.Create(doc, host, curves, flip, pathType.Id, asked.Item1.Id, hookId, hookId);
        if (made == null) throw new InvalidOperationException("Revit made no path reinforcement");
        var spacing = param(made, BuiltInParameter.PATH_REIN_SPACING);
        var length = param(made, BuiltInParameter.PATH_REIN_LENGTH_1);
        var alternating = param(made, BuiltInParameter.PATH_REIN_ALTERNATING);
        if (spacing == null || length == null) throw new InvalidOperationException("its spacing or bar length cannot be set");
        spacing.Set(asked.Item2);
        length.Set(lengthMm / 304.8);
        if (alternating != null && !alternating.IsReadOnly) alternating.Set(0);
        var faceParameter = param(made, host is Wall ? BuiltInParameter.PATH_REIN_FACE_WALL : BuiltInParameter.PATH_REIN_FACE_SLAB);
        if (faceParameter != null && !faceParameter.IsReadOnly && faceParameter.StorageType == StorageType.Integer)
        {
            var kept = false;
            foreach (var value in new[] { 0, 1 })
            {
                faceParameter.Set(value);
                var shown = (faceParameter.AsValueString() ?? "").ToLowerInvariant();
                if (shown.Contains(faceWord)) { kept = true; break; }
            }
            if (!kept) findings.Add("No value of the path's Face reads '" + faceWord + "' in this Revit, so the face is as Revit set it.");
        }
        doc.Regenerate();
        faceShown = faceParameter == null ? "?" : faceParameter.AsValueString();
    }
    catch (Exception ex)
    {
        failure = ex.Message;
    }
    if (failure != null)
    {
        if (made != null && made.IsValidObject)
        {
            try { doc.Delete(made.Id); } catch (Exception) { }
        }
        refused = "Revit refused the path reinforcement: " + failure + ". It was removed again";
    }
    else
    {
        // ---- read back --------------------------------------------------------------------
        var typeNow = param(made, BuiltInParameter.PATH_REIN_TYPE_1);
        var spacingNow = param(made, BuiltInParameter.PATH_REIN_SPACING);
        var lengthNow = param(made, BuiltInParameter.PATH_REIN_LENGTH_1);
        var countNow = param(made, BuiltInParameter.PATH_REIN_NUMBER_OF_BARS);
        barsInPath = countNow == null ? 0 : countNow.AsInteger();
        placed.Add(made.Id);
        var typeName = typeNow == null || doc.GetElement(typeNow.AsElementId()) == null ? "?" : doc.GetElement(typeNow.AsElementId()).Name;
        report = "path reinforcement id " + made.Id + " on '" + host.Name + "' (id " + host.Id + "): " + barsInPath + " x '" + typeName
            + "' @ " + (spacingNow == null ? "?" : mm(spacingNow.AsDouble())) + " mm, each " + (lengthNow == null ? "?" : mm(lengthNow.AsDouble()))
            + " mm long, face " + faceShown + (flip ? ", flipped" : "") + (hookType == null ? "" : ", hooks '" + hookType.Name + "'")
            + ", path " + mm(curves.Sum(c => c.Length)) + " mm long";
        if (typeNow == null || typeNow.AsElementId() != asked.Item1.Id) readBackWrong.Add("the bar type reads back as '" + typeName + "'");
        if (spacingNow == null || Math.Abs(spacingNow.AsDouble() - asked.Item2) > 0.05 / 304.8) readBackWrong.Add("the spacing reads back other than " + mm(asked.Item2) + " mm");
        if (lengthNow == null || Math.Abs(lengthNow.AsDouble() * 304.8 - lengthMm) > 0.05) readBackWrong.Add("the bar length reads back other than " + lengthMm.ToString("0.#", invariant) + " mm");
        if (!(faceShown ?? "").ToLowerInvariant().Contains(faceWord)) readBackWrong.Add("the face reads back as " + faceShown + ", not " + faceWord);
        if (barsInPath == 0) readBackWrong.Add("the path holds no bars");
    }
}

if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add("Placed path reinforcement and read it back: " + report + ".");
    findings.AddRange(readBackWrong.Select(w => "Read back wrong: " + w + "."));
}
