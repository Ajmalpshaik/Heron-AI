// NOT STANDALONE. Assumes `doc`, `form`, `face`, `uGrid`, `vGrid` and
// `pattern` are in scope; leaves `surfaceId`, `divided`, `notAFamily`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// DIVIDE SURFACE - the conceptual Family Editor's tool that lays a U and a V
// grid on a face of a form, so a pattern-based panel family can fill every
// cell: a facade's panels on a mass, a canopy's glazing. DividedSurface.Create,
// then each direction's spacing rule.
//
// THE FORM IS NAMED BY ITS ID - the one CREATE_CONCEPTUAL_FORM gave back - and
// THE FACE BY WHICH WAY IT FACES: top, bottom, front, back, left or right, or
// "largest" (the default). A face that cannot be divided, or already is, is
// refused by name.
//
// EACH GRID IS A NUMBER OR A DISTANCE: "10" is Revit's Number for that
// direction, "1500 mm" a fixed distance; empty leaves Revit's default layout.
// Both are centred, unrotated, with no offset.
//
// THE PATTERN OR PANEL, optional: a tile pattern by name ("Rectangle",
// "Hexagon") or a loaded pattern-based panel family's type ("Panel : Glass"),
// chosen from the types Revit lists as valid for this surface.
//
// READ BACK, ALL OR NOTHING: the surface's host, each direction's layout,
// number or distance and its gridline count, and its type are read again; one
// that does not read as asked THROWS, and the host rolls the whole call back.

var findings = new List<string>();
var surfaceId = "";
var divided = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

var problems = new List<string>();
Element host = null;
Reference chosenFace = null;
var faceWords = "";
// Each grid asked for: (is a number, the number, the distance in feet) or null.
Tuple<bool, int, double> uAsked = null;
Tuple<bool, int, double> vAsked = null;
var patternWord = (pattern ?? "").Trim();

Func<string, string, Tuple<bool, int, double>> gridOf = (text, letter) =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    if (t.Length == 0) return null;
    var isDistance = t.EndsWith("mm");
    var digits = isDistance ? t.Substring(0, t.Length - 2).Trim() : t;
    double value;
    if (!double.TryParse(digits, System.Globalization.NumberStyles.Float, invariant, out value)
        || double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
    {
        problems.Add("\"" + text.Trim() + "\" is not a " + letter + " grid - a number, \"10\", or a distance, \"1500 mm\".");
        return null;
    }
    if (!isDistance && (value != Math.Floor(value) || value > 1000))
    {
        problems.Add("\"" + text.Trim() + "\" is not a whole number of " + letter + " divisions from 1 to 1000; a distance "
            + "ends in mm.");
        return null;
    }
    return isDistance ? Tuple.Create(false, 0, value / 304.8) : Tuple.Create(true, (int)value, 0.0);
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A surface is divided inside a mass, adaptive or pattern-based family - open it first.";
}
else
{
    var family = doc.OwnerFamily;
    var conceptual = false;
    var adaptive = false;
    var panel = false;
    try { conceptual = family != null && family.IsConceptualMassFamily; } catch (Exception) { }
    try { adaptive = family != null && AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(family); } catch (Exception) { }
    try { panel = family != null && family.IsCurtainPanelFamily; } catch (Exception) { }
    if (!conceptual && !adaptive && !panel)
        problems.Add("This family is not a mass, an adaptive component or a pattern-based panel - Revit divides "
            + "surfaces only in those (REPORT_FAMILY_TEMPLATE says which this is).");

    host = doc.GetElement((form ?? "").Trim());
    if (host == null || !(host is Form))
        problems.Add("\"" + (form ?? "").Trim() + "\" is not the id of a form in this family - CREATE_CONCEPTUAL_FORM gives "
            + "one back.");

    uAsked = gridOf(uGrid, "U");
    vAsked = gridOf(vGrid, "V");

    // THE FACE: every face of the form that Revit says can be divided, then the
    // one facing the way asked.
    if (host is Form)
    {
        var faces = new List<Tuple<Face, XYZ>>();
        var geometry = host.get_Geometry(new Options { ComputeReferences = true });
        if (geometry != null)
            foreach (GeometryObject piece in geometry)
            {
                var body = piece as Solid;
                if (body == null) continue;
                foreach (Face f in body.Faces)
                {
                    if (f.Reference == null || !DividedSurface.CanBeDivided(doc, f.Reference)) continue;
                    var box = f.GetBoundingBox();
                    var middle = new UV((box.Min.U + box.Max.U) / 2, (box.Min.V + box.Max.V) / 2);
                    XYZ normal = null;
                    try { normal = f.ComputeNormal(middle); } catch (Exception) { }
                    faces.Add(Tuple.Create(f, normal));
                }
            }
        var said = squash(face);
        if (said.Length == 0) said = "largest";
        var ways = new Dictionary<string, XYZ>
        {
            { "top", XYZ.BasisZ }, { "bottom", XYZ.BasisZ.Negate() }, { "front", XYZ.BasisY.Negate() },
            { "back", XYZ.BasisY }, { "left", XYZ.BasisX.Negate() }, { "right", XYZ.BasisX },
        };
        List<Tuple<Face, XYZ>> fits;
        if (said == "largest") fits = faces.OrderByDescending(f => f.Item1.Area).Take(1).ToList();
        else if (ways.ContainsKey(said))
            fits = faces.Where(f => f.Item2 != null && f.Item2.DotProduct(ways[said]) > 0.7)
                .OrderByDescending(f => f.Item1.Area).Take(1).ToList();
        else
        {
            problems.Add("\"" + face + "\" is not a face - top, bottom, front, back, left, right or largest.");
            fits = new List<Tuple<Face, XYZ>>();
        }
        if (faces.Count == 0)
            problems.Add("No face of this form can be divided, by Revit's own test.");
        else if (fits.Count == 0 && problems.Count == 0)
            problems.Add("No face of this form faces " + said + " - its faces that can be divided face "
                + string.Join(", ", faces.Where(f => f.Item2 != null).Select(f => "(" + Math.Round(f.Item2.X, 2)
                    .ToString(invariant) + ", " + Math.Round(f.Item2.Y, 2).ToString(invariant) + ", "
                    + Math.Round(f.Item2.Z, 2).ToString(invariant) + ")").Distinct()) + ".");
        else if (fits.Count == 1)
        {
            chosenFace = fits[0].Item1.Reference;
            faceWords = (said == "largest" ? "its largest face" : "its " + said + " face") + ", "
                + Math.Round(fits[0].Item1.Area * 0.09290304, 3).ToString(invariant) + " m2";
            var already = DividedSurface.GetDividedSurfaceForReference(doc, chosenFace);
            if (already != null)
                problems.Add("That face is already divided - surface " + already.UniqueId + ". Its grid is changed in "
                    + "its Properties, not by dividing it again.");
        }
    }

    if (problems.Count > 0) refused = "Nothing was divided. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// DIVIDE, SET EACH GRID AND THE PATTERN, THEN READ IT ALL BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    DividedSurface surface;
    try { surface = DividedSurface.Create(doc, chosenFace); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit would not divide " + faceWords + ": " + ex.Message + " The call "
            + "failed, and Heron rolls the whole call back.");
    }
    if (surface == null)
        throw new InvalidOperationException("Revit made no divided surface. The call failed, and Heron rolls the whole "
            + "call back.");

    Action<SpacingRule, Tuple<bool, int, double>, string> lay = (rule, asked, letter) =>
    {
        if (asked == null) return;
        try
        {
            if (asked.Item1) rule.SetLayoutFixedNumber(asked.Item2, SpacingRuleJustification.Center, 0, 0);
            else rule.SetLayoutFixedDistance(asked.Item3, SpacingRuleJustification.Center, 0, 0);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not lay the " + letter + " grid out as asked: " + ex.Message
                + " The call failed, and Heron rolls the whole call back.");
        }
    };
    lay(surface.USpacingRule, uAsked, "U");
    lay(surface.VSpacingRule, vAsked, "V");

    // THE PATTERN OR PANEL, from the types Revit lists as valid for it.
    if (patternWord.Length > 0)
    {
        var valid = surface.GetValidTypes().Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
        Func<Element, string> typeName = e =>
        {
            var symbol = e as FamilySymbol;
            return symbol != null && symbol.Family != null ? symbol.Family.Name + " : " + symbol.Name : e.Name;
        };
        var wanted = valid.Where(e => squash(typeName(e)) == squash(patternWord)).ToList();
        if (wanted.Count == 0) wanted = valid.Where(e => squash(e.Name) == squash(patternWord)).ToList();
        if (wanted.Count != 1)
            throw new InvalidOperationException((wanted.Count == 0 ? "No pattern or panel called \"" + patternWord + "\""
                : "\"" + patternWord + "\" names " + wanted.Count + " types") + " is offered for this surface - Revit "
                + "offers " + string.Join(", ", valid.Select(typeName).Take(20)) + ". A panel family is loaded into this "
                + "family first (LOAD_FAMILY). The call failed, and Heron rolls the whole call back.");
        try { surface.ChangeTypeId(wanted[0].Id); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not give the surface " + typeName(wanted[0]) + ": "
                + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }

    doc.Regenerate();

    // READ BACK.
    if (surface.Host == null || surface.Host.Id != host.Id)
        throw new InvalidOperationException("The divided surface does not read as hosted by the form named. The call "
            + "failed, and Heron rolls the whole call back.");
    Func<SpacingRule, Tuple<bool, int, double>, string, int, string> readBack = (rule, asked, letter, lines) =>
    {
        if (asked != null)
        {
            var right = asked.Item1
                ? rule.Layout == SpacingRuleLayout.FixedNumber && rule.Number == asked.Item2
                : rule.Layout == SpacingRuleLayout.FixedDistance && Math.Abs(rule.Distance - asked.Item3) < 0.5 / 304.8;
            if (!right)
                throw new InvalidOperationException("The " + letter + " grid reads " + rule.Layout + ", not as asked. The "
                    + "call failed, and Heron rolls the whole call back.");
        }
        return letter + " " + (rule.Layout == SpacingRuleLayout.FixedNumber ? "number " + rule.Number
            : rule.Layout == SpacingRuleLayout.FixedDistance ? mm(rule.Distance) + " mm apart"
            : rule.Layout.ToString()) + ", " + lines + " gridline(s)";
    };
    var uRow = readBack(surface.USpacingRule, uAsked, "U", surface.NumberOfUGridlines);
    var vRow = readBack(surface.VSpacingRule, vAsked, "V", surface.NumberOfVGridlines);
    var typeNow = doc.GetElement(surface.GetTypeId());

    surfaceId = surface.UniqueId;
    divided = "Divided " + faceWords + " of form " + host.UniqueId + ": " + uRow + "; " + vRow
        + (typeNow == null ? "" : "; pattern " + (typeNow is FamilySymbol && ((FamilySymbol)typeNow).Family != null
            ? ((FamilySymbol)typeNow).Family.Name + " : " + typeNow.Name : typeNow.Name)) + " - surface " + surface.UniqueId
        + ", read back.";
    findings.Add(divided);
    findings.Add("Whether Revit's Number counts the divisions or the lines between them is NEEDS-CHECKING BT21 - the "
        + "gridline count above is what Revit made. A pattern-based panel family of the same tile pattern fills the "
        + "cells: PATTERN-BASED-FAMILY-CREATION builds one.");
}

if (refused != null) findings.Add(refused);
