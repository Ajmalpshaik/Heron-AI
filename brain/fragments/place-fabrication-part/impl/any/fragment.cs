// NOT STANDALONE. Assumes `doc`, `uidoc`, `service`, `button`, `point`,
// `level`, `size` and `rotation` are in scope; leaves `placed`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S MEP FABRICATION PARTS PALETTE, ONE BUTTON: FabricationPart.Create(
// Document, FabricationServiceButton, int condition, ElementId levelId), or
// Create(Document, FabricationServiceButton, double width, double depth,
// ElementId levelId), which picks the button's condition for that size -
// both present and unchanged 2020 to 2027 (tools/api-surface --members,
// 2026-11-02). RevitAPI.xml: a hanger button is refused by Create ("use
// CreateHanger"), so one is pointed at adding hangers to straights before
// Revit is asked.
//
// WHERE IT GOES. Revit makes the part on the level; it is then moved so its
// Origin sits on the point - x and y in plan, z above the level - and turned
// about the vertical through the point. FabricationPart.AlignPartByInsertion
// Point would do this in one call, but its justification enum lives in
// Revit's Fabrication namespace, which the executor does not import, so the
// part is moved by ElementTransformUtils instead and its Origin read back.
//
// NEVER GUESSED. The service by its whole name or abbreviation among the
// LOADED services; the button by its exact name, capitals forgiven, on that
// service's palettes (the first not excluded when two share a name, and
// which palette is said); the level by its name or the active plan view's
// own. Each one not found places nothing and lists what there is.
//
// NO FABRICATION CONFIGURATION: refused in Revit words - Systems > Fabrication
// > MEP Fabrication Settings - before anything is touched. The part is placed
// alone and connected to nothing.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER. Millimetres become
// feet by / 304.8 and lengths are said in mm (D-20).

var placed = new List<ElementId>();
var findings = new List<string>();
string report = "";

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
Func<string, string> asked = t =>
{
    var s = (t ?? "").Trim();
    return string.Equals(s, "none", StringComparison.OrdinalIgnoreCase) ? "" : s;
};
var wantService = asked(service);
var wantButton = asked(button);
var wantLevel = asked(level);
var wantSize = asked(size);

var refused = "";

// ---- the size asked -----------------------------------------------------------------------
double width = 0, depth = 0;
if (wantSize.Length > 0)
{
    var text = wantSize.ToLowerInvariant().Replace("mm", " ").Replace("Ø", " ").Replace("ø", " ")
        .Replace("dia", " ").Replace("×", "x").Replace("*", "x").Trim();
    var halves = text.Split('x');
    double a, b;
    if (halves.Length == 1 && double.TryParse(halves[0].Trim(), System.Globalization.NumberStyles.Float, invariant, out a) && a > 0)
    { width = a / 304.8; depth = a / 304.8; }
    else if (halves.Length == 2
          && double.TryParse(halves[0].Trim(), System.Globalization.NumberStyles.Float, invariant, out a) && a > 0
          && double.TryParse(halves[1].Trim(), System.Globalization.NumberStyles.Float, invariant, out b) && b > 0)
    { width = a / 304.8; depth = b / 304.8; }
    else
        refused = "The size " + quoted(wantSize) + " is not one this can read - give width x depth in mm (\"400x300\") "
                + "or one diameter in mm (\"150\"). Nothing was placed";
}

// ---- the level ----------------------------------------------------------------------------
Level onLevel = null;
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
var levelNames = levels.Count == 0 ? "none" : string.Join(", ", levels.Select(l => quoted(l.Name)).ToArray());
if (refused.Length == 0)
{
    if (wantLevel.Length > 0)
    {
        onLevel = levels.FirstOrDefault(l => string.Equals((l.Name ?? "").Trim(), wantLevel, StringComparison.OrdinalIgnoreCase));
        if (onLevel == null)
            refused = "No level is called " + quoted(wantLevel) + ". The levels are: " + levelNames + ". Nothing was placed";
    }
    else
    {
        var plan = uidoc == null ? null : uidoc.ActiveView as ViewPlan;
        onLevel = plan == null ? null : plan.GenLevel;
        if (onLevel == null)
            refused = "No level was named and the active view is not a plan with a level. The levels are: " + levelNames
                    + ". Name one. Nothing was placed";
    }
}

// ---- the configuration, the service and the button ----------------------------------------
var config = FabricationConfiguration.GetFabricationConfiguration(doc);
FabricationService chosen = null;
FabricationServiceButton hit = null;
var hitPalette = "";
if (refused.Length == 0 && config == null)
    refused = "This model has no fabrication configuration - choose one in Systems > Fabrication > MEP "
            + "Fabrication Settings and load a service. Nothing was placed";
else if (refused.Length == 0)
{
    var loaded = (config.GetAllLoadedServices() ?? new List<FabricationService>()).Where(s => s != null).ToList();
    var loadedNames = loaded.Count == 0 ? "none - services are loaded in MEP Fabrication Settings"
        : string.Join(", ", loaded.Select(s => quoted(s.Name)
            + (string.IsNullOrEmpty(s.Abbreviation) ? "" : " (" + s.Abbreviation + ")")).ToArray());
    Func<FabricationService, bool> same = s =>
        string.Equals((s.Name ?? "").Trim(), wantService, StringComparison.OrdinalIgnoreCase)
        || string.Equals((s.Abbreviation ?? "").Trim(), wantService, StringComparison.OrdinalIgnoreCase);
    if (wantService.Length == 0)
        refused = "No fabrication service was named. The services loaded in this project are: " + loadedNames
                + ". Name one. Nothing was placed";
    else
    {
        var matches = loaded.Where(same).ToList();
        if (matches.Count == 1)
            chosen = matches[0];
        else if (matches.Count > 1)
            refused = "More than one loaded service answers to " + quoted(wantService) + ": "
                    + string.Join(", ", matches.Select(s => quoted(s.Name)).ToArray())
                    + " - name it by its whole name. Nothing was placed";
        else
        {
            var inConfiguration = (config.GetAllServices() ?? new List<FabricationService>())
                .Where(s => s != null && same(s)).ToList();
            refused = (inConfiguration.Count > 0
                    ? quoted(inConfiguration[0].Name) + " is in the configuration but not loaded in this project - "
                      + "load it in Systems > Fabrication > MEP Fabrication Settings first"
                    : "No loaded service is called " + quoted(wantService))
                + ". The services loaded in this project are: " + loadedNames
                + ". The whole name or the abbreviation is needed. Nothing was placed";
        }
    }

    if (chosen != null)
    {
#if REVIT2020 || REVIT2021
        var paletteCount = chosen.GroupCount;
#else
        var paletteCount = chosen.PaletteCount;
#endif
        var all = new List<string>();
        var sameName = 0;
        for (var p = 0; p < paletteCount; p++)
        {
#if REVIT2020 || REVIT2021
            var paletteName = chosen.GetGroupName(p);
#else
            var paletteName = chosen.GetPaletteName(p);
#endif
            var count = chosen.GetButtonCount(p);
            var here = new List<string>();
            for (var b = 0; b < count; b++)
            {
                var candidate = chosen.GetButton(p, b);
                if (candidate == null) continue;
                here.Add(quoted(candidate.Name));
                if (wantButton.Length == 0
                    || !string.Equals((candidate.Name ?? "").Trim(), wantButton, StringComparison.OrdinalIgnoreCase)) continue;
                sameName++;
                if (hit == null || (hit.IsExcluded() && !candidate.IsExcluded()))
                { hit = candidate; hitPalette = paletteName; }
            }
            all.Add("palette " + quoted(paletteName) + ": " + (here.Count == 0 ? "no buttons" : string.Join(", ", here.ToArray())));
        }
        var listed = all.Count == 0 ? "none" : string.Join("; ", all.ToArray());
        if (wantButton.Length == 0)
            refused = "No button was named. The buttons of " + quoted(chosen.Name) + " are - " + listed + ". Nothing was placed";
        else if (hit == null)
            refused = quoted(chosen.Name) + " has no button called " + quoted(wantButton) + ". Its buttons are - " + listed
                    + ". Nothing was placed";
        else if (hit.IsAHanger)
            refused = "The button " + quoted(hit.Name) + " is a hanger - a hanger is put on a fabrication straight; ask to "
                    + "add hangers to the straights with this button. Nothing was placed";
        else if (sameName > 1)
            findings.Add(sameName + " buttons on " + quoted(chosen.Name) + " are called " + quoted(wantButton)
                       + " - the one on palette " + quoted(hitPalette) + " was used");
    }
}

// ---- place --------------------------------------------------------------------------------
FabricationPart made = null;
var conditionWords = "";
if (refused.Length == 0)
{
    try
    {
        if (wantSize.Length > 0)
        {
            made = FabricationPart.Create(doc, hit, width, depth, onLevel.Id);
            conditionWords = "the condition Revit picked for " + (Math.Abs(width - depth) < 1e-9 ? mm(width) : mm(width) + " x " + mm(depth)) + " mm";
        }
        else
        {
            made = FabricationPart.Create(doc, hit, 0, onLevel.Id);
            conditionWords = "its first condition" + (hit.ConditionCount > 0 ? " " + quoted(hit.GetConditionName(0)) : "")
                + (hit.ConditionCount > 1 ? " of " + hit.ConditionCount + " - give a size for another" : "");
        }
    }
    catch (Exception ex)
    {
        refused = "Revit refused to place " + quoted(hit.Name) + " from " + quoted(chosen.Name) + " - " + ex.Message
                + ". Nothing was placed";
    }
}

if (made != null)
{
    var target = new XYZ(point.X / 304.8, point.Y / 304.8, onLevel.Elevation + point.Z / 304.8);
    try
    {
        ElementTransformUtils.MoveElement(doc, made.Id, target - made.Origin);
        if (Math.Abs(rotation) > 1e-9)
            ElementTransformUtils.RotateElement(doc, made.Id, Line.CreateBound(target, target + XYZ.BasisZ), rotation * Math.PI / 180.0);
    }
    catch (Exception ex)
    {
        findings.Add("Revit made the part but would not move or turn it to the point - " + ex.Message
                   + "; it is where Revit put it");
    }
    doc.Regenerate();
    var fp = doc.GetElement(made.Id) as FabricationPart;
    if (fp == null)
        refused = "Revit returned a part that is not in the model after a regenerate. Nothing was placed";
    else
    {
        placed.Add(fp.Id);
        var at = fp.Origin;
        if (at.DistanceTo(target) > 0.5 / 304.8)
            findings.Add("the part's origin reads " + mm(at.DistanceTo(target)) + " mm from the point asked");
        report = "Placed id " + fp.Id + " " + quoted(fp.Name) + " from the button " + quoted(hit.Name)
               + (hitPalette.Length > 0 ? " (palette " + quoted(hitPalette) + ")" : "") + " on " + quoted(fp.ServiceName)
               + ", at " + conditionWords + (string.IsNullOrEmpty(fp.Size) ? "" : ", size " + fp.Size)
               + (fp.IsAStraight() ? ", straight " + mm(fp.CenterlineLength) + " mm long" : "")
               + ", on " + quoted(onLevel.Name) + " with its origin at x " + mm(at.X) + ", y " + mm(at.Y)
               + " mm and " + mm(at.Z - onLevel.Elevation) + " mm above the level"
               + (Math.Abs(rotation) > 1e-9 ? ", turned " + rotation.ToString("0.#", invariant) + " degrees" : "")
               + ". It is connected to nothing";
    }
}

if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
