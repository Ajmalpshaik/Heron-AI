// NOT STANDALONE. Assumes `doc`, `facePlane`, `system`, `sizeParameters`,
// `form`, `facing` and `at` are in scope; leaves `connector`, `sizedBy`,
// `notAFamily`, `refused` and `findings` behind.
//
// VERSION 9 (2026-11-26, row 5b-2051) - A PORT THAT WILL NOT FIT IS REFUSED,
// NOT THROWN. Seen on Revit 2020 in Group QP's check QP2: a 215 x 205 port on a
// ring whose opening is 209 x 209 came back with the right words but as
// `fragment_threw`, because the fit is measured on the connector once it is
// made and version 8 threw there. Now the making and the fit check run inside
// a SubTransaction: a port that fits is committed and goes on to its ties and
// read-back as before; one that does not is rolled back and its words - the
// same words - come back through `refused`, nothing kept. The same refusal
// covers a whole face whose port is not the face's own size. The other throws
// after the connector is made are unchanged (row 5b-2051 lists which a
// request can reach).
//
// VERSION 8 (2026-11-24, rows 5b-2031 and 5b-2032) - A DUCT SIZE OR PIPE SIZE
// PARAMETER SIZES A CONNECTOR, AND A PORT MAY BE SMALLER THAN A RING'S OPENING.
// Seen on Revit 2020 building an air handling unit family: (1) shared
// parameters of spec Duct Size ("Supply_Air_Width") and Pipe Size
// ("Condensate_Drain_Diameter") were refused as "not a length", though Revit's
// own Associate Family Parameter takes them for a duct connector's width and
// height and a pipe connector's diameter. A Duct Size parameter now sizes a
// duct connector and a Pipe Size one a pipe connector, read by the spec on
// every release - ParameterType on 2020 and 2021, SpecTypeId from 2022 (the
// `#if` below); Length still sizes any of them, and a Duct Size on a pipe
// connector is refused by name. (2) A 205 x 205 port on a flange ring whose
// opening is 209 x 209 was refused as "would not fit". A ring's opening is a
// bore, and a real flange is bored a little over its duct; the port must fit
// INSIDE it, centred (already checked), not equal it. So on a ring a port up to
// the opening's size is taken, either way round, and the clearance is said. A
// whole face (one loop) is still matched exactly: there the face IS the port -
// a neck's end - and a mismatch means the wrong parameters were given. A port
// larger than the opening is still refused. The read-back now checks the port
// against its parameters, not against the opening.
//
// VERSION 7 (2026-11-21, Ajmalpshaik/Heron-AI#1029) - ONLY A FACE THAT FACES
// OUT, AND AT THE POINT GIVEN, COUNTS. Version 6 narrowed the count only when
// form= or facing= was given; left out, it still counted every solid face on
// the plane, so the issue's two refusals came back exactly as before. Now:
// (1) A FACE THAT FACES INTO ANOTHER SOLID IS NOT A PORT. Points spread over
// the face, out to 4% from its edges, are stepped 0.5 to 1.5 mm out along its
// normal; where every one
// lands inside a solid of the family, the face is covered all over on the
// side it points - the raised face's underside sitting on the flange top -
// and it is left out, counted, and said. A face covered only in part still
// counts. (2) `at`, optional: the connector's centre, "X, Y, Z" in mm (the
// refusal's own "X 0, Y 0, Z 884 mm" is taken as written) - only
// a face whose centre (the one this card's own refusal lists) lies within
// 1 mm of it counts, so a screw top 1 m away on the same plane drops out; a
// point more than 1 mm off the plane is refused. (3) THE CONNECTOR IS NEVER
// PUT INSIDE THE BODY: where the face chosen is covered at its own centre on
// the side it points - the flange top under its raised face, a box top under
// its neck - the call is refused, naming the form that covers it, because
// the face meant is the one on top. A ring's centre is in its opening, so
// there the opening's edge, 1 mm onto the face, is looked out of instead. Left out, `at` binds "none"; form= and
// facing= are version 6's.
//
// VERSION 6 (2026-11-20, Ajmalpshaik/Heron-AI#1029) - form= AND facing= PICK
// THE FACE. Seen 2026-11-19, Revit 2020, a pump family: the discharge
// flange's raised face was refused twice - "2 solid faces lie on "Discharge
// Face" (facing up, up)", the second the top of four terminal-box lid screws
// about 1 m away at the same Z; and on the flange top "(facing up, down)", the
// second the raised face's own underside. Every solid face on the plane counts
// anywhere in the family, as version 5 counted it. Now two optional inputs
// narrow that: `form`, the unique id(s) of the form(s) whose face takes the
// connector, commas between - only those forms' own faces count, and of
// joined or cut geometry only a combination one of them is in; an id that is
// not a solid form of this family is refused by name - and `facing`, the way
// the face points (up, down, left, right, front, back, or +Z, -Z, -X, +X, -Y,
// +Y), the words this card's own answer uses. When more than one face still
// lies there, the refusal lists each with the form it is of, the way it faces
// and its centre in millimetres, and says to give form= (or facing=), so one
// more call settles it. Left out, both bind "none" and every face counts
// exactly as in version 5; version 5's rules - a form's own face before a
// joined one, a face with no reference, the size checks - are unchanged.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A NEW CONNECTOR DOES NOT TAKE ITS FACE'S SIZE - it comes out at a generic one
// foot (reported from an earlier family build, twice: a duct port and a pipe
// stub). So a duct or pipe connector is refused without the parameters that
// size it, and they are ASSOCIATED to its own width, height or diameter so the
// port follows the family through every resize.
//
// THE FACE IS FOUND BY THE PLANE IT LIES ON. Exactly one solid face may lie on
// the plane named; the connector goes at that face's centre and points out of
// it, as Revit places it.
//
// A JOINED OR CUT FORM'S FACE IS FOUND IN ITS COMBINATION (version 3, row
// 5b-410). Revit keeps such a form's faces in the GeomCombination's geometry,
// so those are searched when no form's own face lies on the plane, under the
// same exactly-one rule. Whether Revit takes a connector on that face, and what
// its reference names, is reported as Revit answers it - never assumed.
//
// THE SYSTEM NAME DECIDES DUCT, PIPE OR ELECTRICAL, matched to Revit's own enum
// names with the spaces taken out, AND to the name Revit's System
// Classification list shows where that differs - "Fire Protection Wet" for
// FireProtectWet. The three enums are read at run time, so a system a later
// release adds is offered there with no change here.
//
// A CONNECTOR THAT CANNOT BE TIED TO ITS SIZE IS NOT LEFT BEHIND AT ONE FOOT.
// Any other failure after the checks THROWS, and the host rolls the call back.
// A port that will not fit its face is the one exception since version 9: it
// is rolled back in a SubTransaction and comes back through `refused`.
//
// A FLANGE RING'S FACE IS MEASURED BY ITS OPENING (version 5, 2026-10-24, row
// 5b-1334): one inner loop holding the connector's centre is the port, and the
// connector must sit at its centre. A plane alone takes no connector through
// the API, so a ring sketched on the end plane is the way to a port on a plane.
//
// CABLE TRAY AND CONDUIT (version 4, 2026-10-13, the gap fill) have no system
// list in Revit: "Cable Tray" takes two length parameters, width then height,
// turned to the face like a rectangular duct port; "Conduit" takes one, its
// diameter.
//
// GLOBAL AND FITTING ARE OFFERED, as Revit's System Classification list offers
// them; where Revit refuses one for a face, its refusal is reported and nothing
// is kept. SET_FAMILY_CONNECTOR_ROLES changes a connector already made.

var findings = new List<string>();
var connector = "";
var sizedBy = new List<string>();
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// WHERE REVIT'S LIST SAYS SOMETHING THE ENUM DOES NOT. The enum says
// FireProtectWet; System Classification says "Fire Protection Wet", and a
// modeller types what Revit shows (row 5b-297). A name here is used only when
// the enum value exists in the release running. Pipe "Other" is left out on
// purpose: a duct has Other Air, and a bare "Other" would pick the kind.
var shownAs = new Dictionary<string, string>
{
    { "FireProtectWet", "Fire Protection Wet" },
    { "FireProtectDry", "Fire Protection Dry" },
    { "FireProtectPreaction", "Fire Protection Pre-Action" },
    { "FireProtectOther", "Fire Protection Other" },
    { "SupplyHydronic", "Hydronic Supply" },
    { "ReturnHydronic", "Hydronic Return" },
};

// "SupplyAir" read as "Supply Air", the way a modeller writes it - or, where
// Revit shows it otherwise, as Revit shows it.
Func<string, string> spaced = name =>
{
    if (shownAs.ContainsKey(name)) return shownAs[name];
    var text = new System.Text.StringBuilder();
    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
        text.Append(name[i]);
    }
    return text.ToString();
};

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;

Func<XYZ, string> pointing = d =>
{
    var axisIndex = axisOf(d);
    if (axisIndex == 2) return d.Z > 0 ? "up" : "down";
    if (axisIndex == 0) return d.X > 0 ? "right (+X)" : "left (-X)";
    if (axisIndex == 1) return d.Y > 0 ? "back (+Y)" : "front (-Y)";
    return "(" + Math.Round(d.X, 3).ToString(invariant) + ", " + Math.Round(d.Y, 3).ToString(invariant)
        + ", " + Math.Round(d.Z, 3).ToString(invariant) + ")";
};

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY. A ForgeTypeId
// names its version - "...:length-2.0.0" - and plain equality may compare it,
// so a length a template made under an older version would read as some other
// kind. NameEquals compares the name alone, from 2021; before that a kind is
// an enum and plain equality is exact.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

Func<FamilyParameter, bool> isLength = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Length", flags);
            var length = property == null ? null : property.GetValue(null);
            return sameKind(spec, length);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Length";
    }
    catch (Exception)
    {
        return false;
    }
};

// VERSION 8 (row 5b-2031): THE CONNECTOR KIND A SIZE SPEC BELONGS TO - "duct"
// for Duct Size (HVAC_DUCT_SIZE), "pipe" for Pipe Size, "" for anything else.
// Revit's Associate Family Parameter offers these for a duct connector's width
// and height and a pipe connector's diameter. ParameterType names them on 2020
// and 2021; SpecTypeId from 2022, compared by name as `sameKind` does.
Func<FamilyParameter, string> sizeSpecOf = p =>
{
    try
    {
#if REVIT2020 || REVIT2021
        var type = p.Definition.ParameterType;
        return type == ParameterType.HVACDuctSize ? "duct" : type == ParameterType.PipeSize ? "pipe" : "";
#else
        var spec = p.Definition.GetDataType();
        return sameKind(spec, SpecTypeId.DuctSize) ? "duct" : sameKind(spec, SpecTypeId.PipeSize) ? "pipe" : "";
#endif
    }
    catch (Exception)
    {
        return "";
    }
};

// EVERY SYSTEM REVIT OFFERS A FAMILY CONNECTOR: (kind, name, value). Global and
// Fitting are offered too - a valve body's two pipe connectors are Global so a
// system passes through it (docs/43 §10). Both are a duct AND a pipe system, so
// the domain comes from the words ("Global pipe") or the family's category.
var systems = new List<Tuple<string, string, object>>();
var notOffered = new[] { "UndefinedSystemType", "Undefined" };
foreach (var value in Enum.GetValues(typeof(DuctSystemType)))
    if (!notOffered.Contains(value.ToString())) systems.Add(Tuple.Create("duct", value.ToString(), value));
foreach (var value in Enum.GetValues(typeof(PipeSystemType)))
    if (!notOffered.Contains(value.ToString())) systems.Add(Tuple.Create("pipe", value.ToString(), value));
foreach (var value in Enum.GetValues(typeof(ElectricalSystemType)))
    if (!notOffered.Contains(value.ToString())) systems.Add(Tuple.Create("electrical", value.ToString(), value));
systems.Add(Tuple.Create("cable tray", "CableTray", (object)null));
systems.Add(Tuple.Create("conduit", "Conduit", (object)null));

Func<Element, string> kindOf = element =>
    element is Extrusion ? "an extrusion" : element is Revolution ? "a revolve" : element is Blend ? "a blend"
    : element is SweptBlend ? "a swept blend" : element is Sweep ? "a sweep" : element is GenericForm ? "a form"
    : element is GeomCombination ? "the joined or cut forms" : "the element";

// THE WAY ROUND A JOIN OR CUT, when Revit will not take a connector on its face
// (row 5b-410): a thin solid on that face, in no combination, takes one - a
// form's own face is looked for before a combination's.
var sideStep = "The way round it: a thin solid joined or cut with nothing - 1 mm deep, sized to the flange or "
    + "port, one face on the plane and the rest inside the body - takes the connector, because a form's own "
    + "face is used before a joined one. EXTRUDE_BETWEEN_PLANES makes one.";

Tuple<string, string, object> chosen = null;
PlanarFace face = null;
// The combination the face belongs to, when it is a joined or cut face.
GeomCombination faceIn = null;
// Faces of joined or cut forms on the same plane, when a form's own face won.
var alsoJoined = 0;
var sizes = new List<FamilyParameter>();
var faceName = (facePlane ?? "").Trim();
// VERSION 6 (issue #1029): the forms and the way the face points, when said.
// Left out, each binds "none" - version 5 exactly.
Func<string, string> unlessNone = text =>
{
    var typed = (text ?? "").Trim();
    return string.Equals(typed, "none", StringComparison.OrdinalIgnoreCase) ? "" : typed;
};
var formSaid = unlessNone(form);
var facingSaid = unlessNone(facing);
// VERSION 7: the connector's centre, when said.
var atSaid = unlessNone(at);
// Faces on the plane that form= and facing= left out, for the answer.
var facesLeftOut = 0;
// VERSION 7: faces left out because they face into another solid, and
// because their centre is not at the point at= gives.
var facesIntoBody = 0;
var facesNotAt = 0;
// Within this of at=, a face's centre is the connector's place: 1 mm.
var oneMillimetre = 1.0 / 304.8;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A connector "
        + "is added inside the family - open it first.";
}
else
{
    var fm = doc.FamilyManager;
    var problems = new List<string>();

    // THE SYSTEM.
    var said = squash(system);
    // Either the enum's own name or the name Revit shows for it.
    var systemMatches = systems.Where(s => squash(s.Item2) == said || squash(spaced(s.Item2)) == said
        || squash(s.Item2) + s.Item1 == said || s.Item1 + squash(s.Item2) == said).ToList();
    if (said.Length == 0) problems.Add("No system was named - \"Supply Air\", \"Domestic Cold Water\", \"Power Balanced\".");
    else if (systemMatches.Count == 0)
    {
        var near = systems.Where(s => squash(spaced(s.Item2)).Contains(said) || said.Contains(squash(spaced(s.Item2)))
                || squash(s.Item2).Contains(said) || said.Contains(squash(s.Item2)))
            .Select(s => spaced(s.Item2) + " (" + s.Item1 + ")").Take(8).ToList();
        problems.Add("No connector system is called \"" + system.Trim() + "\"."
            + (near.Count > 0 ? " Close: " + string.Join(", ", near) + "." : " Revit's names are used - Supply Air, Return Air, Exhaust Air, Domestic Cold Water, Domestic Hot Water, Hydronic Supply, Hydronic Return, Sanitary, Vent, Fire Protection Wet, Fire Protection Dry, Fire Protection Pre-Action, Fire Protection Other, Other Pipe, Power Balanced, Cable Tray, Conduit."));
    }
    else if (systemMatches.Count > 1)
    {
        // A NAME BOTH DOMAINS HAVE - Global, Fitting. The domain is the word
        // beside it, "Global pipe", or else the family's category: a Pipe
        // Accessories family's Global is a pipe Global.
        var category = doc.OwnerFamily == null || doc.OwnerFamily.FamilyCategory == null ? null : doc.OwnerFamily.FamilyCategory.Id;
        Func<BuiltInCategory[], bool> isOneOf = list => category != null && list.Any(b => category == new ElementId(b));
        var pipeFamily = isOneOf(new[] { BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_Sprinklers });
        var ductFamily = isOneOf(new[] { BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_DuctTerminal });
        var asked = systemMatches.Where(s => said.StartsWith(s.Item1) || said.EndsWith(s.Item1)).ToList();
        if (asked.Count == 0 && pipeFamily) asked = systemMatches.Where(s => s.Item1 == "pipe").ToList();
        if (asked.Count == 0 && ductFamily) asked = systemMatches.Where(s => s.Item1 == "duct").ToList();
        if (asked.Count == 1)
        {
            chosen = asked[0];
            findings.Add("\"" + system.Trim() + "\" is a duct and a pipe system both; a " + chosen.Item1 + " connector "
                + "was made, " + (said.StartsWith(chosen.Item1) || said.EndsWith(chosen.Item1) ? "as the words say."
                : "from the family's category."));
        }
        else
            problems.Add("\"" + system.Trim() + "\" is a " + string.Join(" and a ", systemMatches.Select(s => s.Item1))
                + " system both - say which, \"" + system.Trim() + " pipe\" or \"" + system.Trim() + " duct\". Nothing was "
                + "made rather than one being picked.");
    }
    else chosen = systemMatches[0];

    // THE SIZES.
    var sizeNames = (sizeParameters ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    foreach (var name in sizeNames)
    {
        var p = fm.get_Parameter(name);
        if (p == null) problems.Add("\"" + name + "\" is not a parameter of this family - ADD_FAMILY_PARAMETERS makes one.");
        else if (isLength(p)) sizes.Add(p);
        else
        {
            // VERSION 8: a Duct Size or Pipe Size parameter sizes its own kind.
            // With no system settled there is a problem already, so it is taken.
            var sizeKind = sizeSpecOf(p);
            if (sizeKind.Length > 0 && (chosen == null || chosen.Item1 == sizeKind)) sizes.Add(p);
            else if (sizeKind.Length > 0)
                problems.Add("\"" + name + "\" is a " + (sizeKind == "duct" ? "Duct Size" : "Pipe Size") + " parameter, "
                    + "which sizes a " + sizeKind + " connector, not a " + chosen.Item1 + " one. A Length parameter "
                    + "sizes any connector.");
            else
                problems.Add("\"" + name + "\" is not a Length, Duct Size or Pipe Size parameter, so it cannot size "
                    + "a connector.");
        }
    }

    if (chosen != null && problems.Count == 0)
    {
        if (chosen.Item1 == "duct" && sizes.Count != 1 && sizes.Count != 2)
            problems.Add("A duct connector needs its size: two Length or Duct Size parameters for a rectangular port "
                + "(width, then height) or one for a round one (the diameter). Without them it stays at "
                + "Revit's one-foot placeholder, whatever face it is on.");
        if (chosen.Item1 == "pipe" && sizes.Count != 1)
            problems.Add("A pipe connector needs ONE Length or Pipe Size parameter - its diameter. Without it it stays at "
                + "Revit's one-foot placeholder.");
        if (chosen.Item1 == "cable tray" && sizes.Count != 2)
            problems.Add("A cable tray connector needs TWO length parameters - its width, then its height. "
                + "Without them it stays at Revit's one-foot placeholder.");
        if (chosen.Item1 == "conduit" && sizes.Count != 1)
            problems.Add("A conduit connector needs ONE length parameter - its diameter. Without it it stays at "
                + "Revit's one-foot placeholder.");
        if (chosen.Item1 == "electrical" && sizes.Count != 0)
            problems.Add("An electrical connector has no size, so no size parameters are taken.");
        if (sizes.Count > 0 && fm.CurrentType == null)
            problems.Add("This family has no type yet, so its size parameters hold nothing to check the "
                + "connector against. SET_FAMILY_TYPE_VALUES makes the first type.");
    }

    // VERSION 6 (issue #1029): THE FORMS NAMED, by the unique ids the form
    // tools give back - only a SOLID form of this family; anything else is
    // refused by name.
    var onlyForms = new List<GenericForm>();
    foreach (var formToken in formSaid.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
    {
        Element formElement = null;
        try { formElement = doc.GetElement(formToken); } catch (Exception) { formElement = null; }
        var asForm = formElement as GenericForm;
        if (formElement == null)
            problems.Add("No element in this family has the id \"" + formToken + "\" given in form=.");
        else if (asForm == null)
            problems.Add("\"" + formToken + "\" given in form= is not a form - it is one of this family's "
                + (formElement.Category != null ? formElement.Category.Name : spaced(formElement.GetType().Name))
                + ". Name an extrusion, blend, revolve, sweep or swept blend by the id its tool gave back.");
        else if (!asForm.IsSolid)
            problems.Add("\"" + formToken + "\" given in form= is " + kindOf(asForm) + " that is a VOID - a connector "
                + "goes on a solid's face.");
        else if (!onlyForms.Any(o => o.Id == asForm.Id)) onlyForms.Add(asForm);
    }

    // VERSION 6: WHICH WAY THE FACE POINTS, in the words this card's answer
    // uses - up, down, right (+X), left (-X), back (+Y), front (-Y).
    XYZ wantedFacing = null;
    if (facingSaid.Length > 0)
    {
        var way = new string(facingSaid.ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c != '(' && c != ')').ToArray());
        var ways = new Dictionary<string, XYZ>
        {
            { "up", XYZ.BasisZ }, { "+z", XYZ.BasisZ }, { "up+z", XYZ.BasisZ },
            { "down", XYZ.BasisZ.Negate() }, { "-z", XYZ.BasisZ.Negate() }, { "down-z", XYZ.BasisZ.Negate() },
            { "right", XYZ.BasisX }, { "+x", XYZ.BasisX }, { "right+x", XYZ.BasisX },
            { "left", XYZ.BasisX.Negate() }, { "-x", XYZ.BasisX.Negate() }, { "left-x", XYZ.BasisX.Negate() },
            { "back", XYZ.BasisY }, { "+y", XYZ.BasisY }, { "back+y", XYZ.BasisY },
            { "front", XYZ.BasisY.Negate() }, { "-y", XYZ.BasisY.Negate() }, { "front-y", XYZ.BasisY.Negate() },
        };
        if (ways.ContainsKey(way)) wantedFacing = ways[way];
        else
            problems.Add("\"" + facingSaid + "\" is not a way a face can point - say up, down, left, right, front or "
                + "back, or +Z, -Z, -X, +X, -Y or +Y.");
    }

    // VERSION 7: THE CONNECTOR'S CENTRE, "X, Y, Z" in millimetres - the
    // numbers a refusal lists for each face.
    XYZ wantedAt = null;
    if (atSaid.Length > 0)
    {
        // The numbers, in order; around them only the words a refusal prints
        // its centre with - "X 0, Y 0, Z 884 mm" - and separators.
        var numberPattern = new System.Text.RegularExpressions.Regex(@"[-+]?[0-9]*\.?[0-9]+");
        var read = new List<double>();
        foreach (System.Text.RegularExpressions.Match found in numberPattern.Matches(atSaid))
        {
            double value;
            if (double.TryParse(found.Value, System.Globalization.NumberStyles.Float, invariant, out value)) read.Add(value);
        }
        var leftOver = numberPattern.Replace(atSaid, "").ToLowerInvariant();
        var onlyWords = leftOver.All(c => " ,;:=xyzm\t".IndexOf(c) >= 0);
        if (read.Count == 3 && onlyWords) wantedAt = new XYZ(read[0] / 304.8, read[1] / 304.8, read[2] / 304.8);
        else
            problems.Add("\"" + atSaid + "\" given in at= is not a point - give the connector's centre as three "
                + "numbers in millimetres, X, Y, Z, as a refusal lists each face's centre.");
    }

    // THE FACE, by the plane it lies on.
    if (faceName.Length == 0) problems.Add("No face was named - the reference plane or level it lies on, \"Neck Top\".");
    else
    {
        var datums = new List<Tuple<int, double, string>>();
        foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
        {
            var axisIndex = axisOf(rp.Normal);
            if (axisIndex >= 0 && string.Equals(ownName(rp), faceName, StringComparison.OrdinalIgnoreCase))
                datums.Add(Tuple.Create(axisIndex, along(rp.GetPlane().Origin, axisIndex), ownName(rp)));
        }
        foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
            if (string.Equals(level.Name, faceName, StringComparison.OrdinalIgnoreCase))
                datums.Add(Tuple.Create(2, level.Elevation, level.Name));

        if (datums.Count == 0) problems.Add("No reference plane or level is called \"" + faceName + "\".");
        else if (datums.Count > 1) problems.Add("\"" + faceName + "\" names " + datums.Count + " planes - rename one first.");
        else
        {
            var datum = datums[0];
            // VERSION 7: a point given in at= lies on the plane, or it names
            // no face on it.
            if (wantedAt != null && Math.Abs(along(wantedAt, datum.Item1) - datum.Item2) > oneMillimetre)
                problems.Add("The point given in at= is " + mm(Math.Abs(along(wantedAt, datum.Item1) - datum.Item2))
                    + " mm off \"" + datum.Item3 + "\", so it names no face lying on it - give a point on the plane.");
            Func<Face, bool> onDatum = each =>
            {
                var flat = each as PlanarFace;
                return flat != null && axisOf(flat.FaceNormal) == datum.Item1
                    && Math.Abs(along(flat.Origin, datum.Item1) - datum.Item2) < halfMillimetre;
            };
            var candidates = new List<PlanarFace>();
            // VERSION 6: the form each face is of, beside it.
            var candidateOf = new List<GenericForm>();
            var everything = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();
            var forms = everything.OfType<GenericForm>().Where(f => f.IsSolid).ToList();
            // VERSION 7: every solid of the family with the element it is of,
            // to tell a face that faces into another solid.
            // A form in a combination is left out: its own geometry reads
            // empty (row 5b-410), and if a release ever gave back its uncut
            // solid, a bore would read as solid. The combination stands for it.
            // Each body's box is worked out once, for the probes' first test.
            var bodies = new List<Tuple<Solid, Element, BoundingBoxXYZ>>();
            var joinedMembers = new HashSet<ElementId>();
            foreach (var join in everything.OfType<GeomCombination>())
            {
                try
                {
                    foreach (CombinableElement member in join.AllMembers) joinedMembers.Add(member.Id);
                }
                catch (Exception) { }
            }
            foreach (var solidForm in forms)
            {
                var geometry = solidForm.get_Geometry(new Options { ComputeReferences = true });
                if (geometry == null) continue;
                foreach (GeometryObject piece in geometry)
                {
                    var solid = piece as Solid;
                    if (solid == null) continue;
                    if (solid.Volume > 0 && !joinedMembers.Contains(solidForm.Id))
                        bodies.Add(Tuple.Create(solid, (Element)solidForm, solidForm.get_BoundingBox(null)));
                    foreach (Face each in solid.Faces)
                        if (onDatum(each) && each.Reference != null)
                        {
                            candidates.Add((PlanarFace)each);
                            candidateOf.Add(solidForm);
                        }
                }
            }

            // A FORM THAT IS JOINED OR CUT HAS NO FACES OF ITS OWN: Revit keeps
            // them in the COMBINATION's geometry (row 5b-410 - a flange cut by a
            // bore void, its face exactly on the plane, came back "no solid
            // face"). So every combination is read too, as COMBINE_FAMILY_FORMS
            // reads one, and its faces are used only when no form's own face
            // lies on the plane - a form in no join keeps winning, as it always
            // has, which is what lets a thin solid on a cut face take the
            // connector where Revit will not. Every face on the plane counts
            // here, referenced or not, so exactly one must lie there.
            var combined = new List<PlanarFace>();
            var combinedIn = new List<GeomCombination>();
            foreach (var join in everything.OfType<GeomCombination>())
            {
                var geometry = join.get_Geometry(new Options { ComputeReferences = true });
                if (geometry == null) continue;
                foreach (GeometryObject piece in geometry)
                {
                    // Only a piece holding volume, as COMBINE_FAMILY_FORMS
                    // measures one - a void's own geometry is inside out, of
                    // negative volume, and a join of voids is no solid face.
                    var solid = piece as Solid;
                    if (solid == null || solid.Volume <= 0) continue;
                    bodies.Add(Tuple.Create(solid, (Element)join, join.get_BoundingBox(null)));
                    foreach (Face each in solid.Faces)
                        if (onDatum(each))
                        {
                            combined.Add((PlanarFace)each);
                            combinedIn.Add(join);
                        }
                }
            }

            // VERSION 6 (issue #1029): EACH FACE SAID WITH ITS FORM, THE WAY IT
            // POINTS AND ITS CENTRE, so a refusal names what form= and facing=
            // choose between. A combination's face is said with the forms in it.
            Func<GeomCombination, List<GenericForm>> membersOf = combination =>
            {
                var held = new List<GenericForm>();
                try
                {
                    foreach (CombinableElement member in combination.AllMembers)
                    {
                        var asMember = member as GenericForm;
                        if (asMember != null) held.Add(asMember);
                    }
                }
                catch (Exception) { }
                return held;
            };
            // The middle of a face's outline - where its connector goes, and
            // the point at= is matched against. Null when it cannot be read.
            Func<PlanarFace, XYZ> centreAt = f =>
            {
                var outlinePoints = new List<XYZ>();
                try
                {
                    foreach (EdgeArray edgeLoop in f.EdgeLoops)
                        foreach (Edge loopEdge in edgeLoop)
                            outlinePoints.AddRange(loopEdge.Tessellate());
                }
                catch (Exception) { outlinePoints.Clear(); }
                if (outlinePoints.Count == 0) return null;
                return new XYZ((outlinePoints.Min(q => q.X) + outlinePoints.Max(q => q.X)) / 2,
                    (outlinePoints.Min(q => q.Y) + outlinePoints.Max(q => q.Y)) / 2,
                    (outlinePoints.Min(q => q.Z) + outlinePoints.Max(q => q.Z)) / 2);
            };
            Func<PlanarFace, string> centreOf = f =>
            {
                var middle = centreAt(f);
                if (middle == null) return "its centre could not be read";
                return "centred at X " + mm(middle.X) + ", Y " + mm(middle.Y) + ", Z " + mm(middle.Z) + " mm";
            };

            // VERSION 7: WHAT A POINT LOOKS INTO. A short line from 0.5 to
            // 1.5 mm out of the face along its normal - longer than Revit's
            // shortest curve - is asked of every solid of the family whose box
            // is near; the first that holds part of it is returned, else null.
            // A solid Revit will not answer for is passed over, so a face is
            // counted rather than dropped on a failed read.
            Func<XYZ, XYZ, Element> coveredBy = (point, normal) =>
            {
                Line probe;
                try { probe = Line.CreateBound(point + normal * (0.5 / 304.8), point + normal * (1.5 / 304.8)); }
                catch (Exception) { return null; }
                var inside = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside };
                var margin = 2.0 / 304.8;
                foreach (var body in bodies)
                {
                    var box = body.Item3;
                    if (box != null && (point.X < box.Min.X - margin || point.X > box.Max.X + margin
                            || point.Y < box.Min.Y - margin || point.Y > box.Max.Y + margin
                            || point.Z < box.Min.Z - margin || point.Z > box.Max.Z + margin))
                        continue;
                    try
                    {
                        var hit = body.Item1.IntersectWithCurve(probe, inside);
                        if (hit != null && hit.SegmentCount > 0) return body.Item2;
                    }
                    catch (Exception) { }
                }
                return null;
            };
            // VERSION 7: A FACE THAT FACES INTO ANOTHER SOLID ALL OVER - the
            // element it faces into, else null. Points spread over the face are
            // each looked out of; only when every one is covered is the face
            // inside the body. The points reach the edges - a 5 x 5 grid over
            // the face's own box, 4% in from each side, kept where it lies ON
            // the face - and its triangles' middles, at most twelve, add the
            // inside of a curved outline. Triangle middles alone sat a sixth of
            // the way in on a rectangle, so a box top with a neck over its
            // middle read as covered all over (found in review, 2026-11-21). A
            // face whose points cannot be read is counted, as version 6 did.
            Func<PlanarFace, Element> facesInto = f =>
            {
                var points = new List<XYZ>();
                try
                {
                    var range = f.GetBoundingBox();
                    var fractions = new[] { 0.04, 0.27, 0.5, 0.73, 0.96 };
                    foreach (var fu in fractions)
                        foreach (var fv in fractions)
                        {
                            var uv = new UV(range.Min.U + (range.Max.U - range.Min.U) * fu,
                                range.Min.V + (range.Max.V - range.Min.V) * fv);
                            if (f.IsInside(uv)) points.Add(f.Evaluate(uv));
                        }
                }
                catch (Exception) { }
                try
                {
                    var mesh = f.Triangulate();
                    var step = Math.Max(1, mesh.NumTriangles / 12);
                    var added = 0;
                    for (var t = 0; t < mesh.NumTriangles && added < 12; t += step, added++)
                    {
                        var triangle = mesh.get_Triangle(t);
                        points.Add((triangle.get_Vertex(0) + triangle.get_Vertex(1) + triangle.get_Vertex(2)) / 3);
                    }
                }
                catch (Exception) { }
                if (points.Count == 0) return null;
                Element first = null;
                foreach (var point in points)
                {
                    var over = coveredBy(point, f.FaceNormal);
                    if (over == null) return null;
                    if (first == null) first = over;
                }
                return first;
            };
            var ownInto = candidates.Select(facesInto).ToList();
            var joinedInto = combined.Select(facesInto).ToList();
            Func<Element, string> intoWords = e => e == null ? ""
                : ", covered all over on that side by " + kindOf(e) + " " + e.UniqueId + " - it faces into the body";
            Func<PlanarFace, bool> atAsked = f =>
            {
                if (wantedAt == null) return true;
                var middle = centreAt(f);
                return middle != null && middle.DistanceTo(wantedAt) <= oneMillimetre;
            };

            Func<GenericForm, string> formWords = g => kindOf(g) + " " + g.UniqueId;
            Func<int, string> ownFaceWords = i => "a face of " + formWords(candidateOf[i]) + ", facing "
                + pointing(candidates[i].FaceNormal) + ", " + centreOf(candidates[i]) + intoWords(ownInto[i]);
            Func<int, string> joinedFaceWords = i =>
            {
                // The SOLID forms in it - the ids form= takes; a void cuts it.
                var inIt = membersOf(combinedIn[i]).Where(g => g.IsSolid).ToList();
                return "a face of the joined or cut forms " + (inIt.Count == 0 ? "(none could be read)"
                        : string.Join(", ", inIt.Select(formWords))) + ", facing " + pointing(combined[i].FaceNormal)
                    + ", " + centreOf(combined[i]) + intoWords(joinedInto[i]);
            };
            var everyFace = Enumerable.Range(0, candidates.Count).Select(ownFaceWords)
                .Concat(Enumerable.Range(0, combined.Count).Select(joinedFaceWords)).ToList();
            var settleIt = " Give form= the id of the form whose face takes the connector"
                + (wantedAt == null ? ", or at= its centre as listed (X, Y, Z in mm)" : "")
                + (wantedFacing == null ? ", or facing= the way that face points (up, down, left, right, front, back)" : "")
                + " - or name a plane only one face lies on.";

            // VERSION 6: ONLY WHAT form= AND facing= SAY COUNTS. A form's own
            // face counts when it is of a form named; a combination's when the
            // combination holds one. Neither said, every face counts, as before.
            Func<PlanarFace, bool> facesAsked = f => wantedFacing == null || f.FaceNormal.DotProduct(wantedFacing) > 0.9999;
            // VERSION 7: always filtered now - a face that faces into another
            // solid is never a port, whatever was given. Each face left out is
            // counted once, under the first rule that leaves it out: form= and
            // facing= first (version 6's count), then at=, then the body.
            {
                var keptOwn = new List<int>();
                for (var i = 0; i < candidates.Count; i++)
                {
                    var asked = facesAsked(candidates[i])
                        && (onlyForms.Count == 0 || onlyForms.Any(o => o.Id == candidateOf[i].Id));
                    if (!asked) facesLeftOut++;
                    else if (!atAsked(candidates[i])) facesNotAt++;
                    else if (ownInto[i] != null) facesIntoBody++;
                    else keptOwn.Add(i);
                }
                var keptJoined = new List<int>();
                for (var i = 0; i < combined.Count; i++)
                {
                    var asked = facesAsked(combined[i]) && (onlyForms.Count == 0
                        || membersOf(combinedIn[i]).Any(m => onlyForms.Any(o => o.Id == m.Id)));
                    if (!asked) facesLeftOut++;
                    else if (!atAsked(combined[i])) facesNotAt++;
                    else if (joinedInto[i] != null) facesIntoBody++;
                    else keptJoined.Add(i);
                }
                ownInto = keptOwn.Select(i => ownInto[i]).ToList();
                joinedInto = keptJoined.Select(i => joinedInto[i]).ToList();
                candidates = keptOwn.Select(i => candidates[i]).ToList();
                candidateOf = keptOwn.Select(i => candidateOf[i]).ToList();
                combined = keptJoined.Select(i => combined[i]).ToList();
                combinedIn = keptJoined.Select(i => combinedIn[i]).ToList();
            }
            var askedWords = (onlyForms.Count == 0 ? "" : onlyForms.Count == 1 ? " of the form named" : " of the forms named")
                + (wantedFacing == null ? "" : " facing " + pointing(wantedFacing))
                + (wantedAt == null ? "" : " centred at the point given in at=")
                + (facesIntoBody == 0 ? "" : " that faces out of the body");

            if (candidates.Count == 0 && combined.Count == 0 && everyFace.Count == 0)
                problems.Add("No solid face lies on \"" + datum.Item3 + "\". Build the geometry to it first - "
                    + "EXTRUDE_BETWEEN_PLANES locks a box to named planes.");
            else if (candidates.Count == 0 && combined.Count == 0)
                problems.Add("No solid face" + askedWords + " lies on \"" + datum.Item3 + "\". What does lie on it: "
                    + string.Join("; ", everyFace) + "." + settleIt);
            else if (candidates.Count > 1)
                problems.Add(candidates.Count + " solid faces" + askedWords + " lie on \"" + datum.Item3 + "\" (facing "
                    + string.Join(", ", candidates.Select(c => pointing(c.FaceNormal))) + "), so which one "
                    + "the connector goes on is not clear: " + string.Join("; ", Enumerable.Range(0, candidates.Count)
                        .Select(ownFaceWords)) + "." + settleIt + " The top of a neck, not the plane a neck stands on.");
            else if (candidates.Count == 1)
            {
                face = candidates[0];
                alsoJoined = combined.Count;
            }
            else if (combined.Count > 1)
                problems.Add(combined.Count + " faces of joined or cut forms" + askedWords + " lie on \"" + datum.Item3
                    + "\" (facing " + string.Join(", ", combined.Select(c => pointing(c.FaceNormal))) + "), so which one "
                    + "the connector goes on is not clear: " + string.Join("; ", Enumerable.Range(0, combined.Count)
                        .Select(joinedFaceWords)) + "." + settleIt + " The outer face of a flange, not a plane two "
                    + "bodies share.");
            // A FACE REVIT GIVES NO REFERENCE TO cannot take a connector - said
            // plainly, with the way round it, never left as "no face".
            else if (combined[0].Reference == null)
                problems.Add("A face of joined or cut forms lies on \"" + datum.Item3 + "\", but Revit gives no "
                    + "reference to it, so a connector cannot be put on it. " + sideStep);
            else
            {
                face = combined[0];
                faceIn = combinedIn[0];
            }

            // VERSION 7: NEVER A CONNECTOR INSIDE THE BODY. It goes at the
            // face's centre and points out of it; where another solid covers
            // that very point on the side the face points - a flange top under
            // its raised face, a box top under its neck - the port would sit
            // inside that solid, so nothing is made and the solid is named.
            //
            // A RING'S CENTRE IS IN ITS OPENING, where nothing covers it - a
            // bored flange with a bored raised face on it (found in review,
            // 2026-11-21). So where the centre is not on the face itself, the
            // port's edge is looked out of instead: the points of the loop
            // nearest the centre, each pushed 1 mm away from it onto the face;
            // only when every one is covered is the port inside the body.
            Func<PlanarFace, Element> portCoveredBy = f =>
            {
                var middle = centreAt(f);
                if (middle == null) return null;
                var onFace = false;
                try
                {
                    var projected = f.Project(middle);
                    onFace = projected != null && f.IsInside(projected.UVPoint);
                }
                catch (Exception) { onFace = true; }
                if (onFace) return coveredBy(middle, f.FaceNormal);
                List<XYZ> nearest = null;
                var nearestDistance = double.MaxValue;
                try
                {
                    foreach (EdgeArray edgeLoop in f.EdgeLoops)
                    {
                        var loopPoints = new List<XYZ>();
                        foreach (Edge loopEdge in edgeLoop) loopPoints.AddRange(loopEdge.Tessellate());
                        if (loopPoints.Count == 0) continue;
                        var mean = loopPoints.Average(q => q.DistanceTo(middle));
                        if (mean < nearestDistance) { nearestDistance = mean; nearest = loopPoints; }
                    }
                }
                catch (Exception) { nearest = null; }
                if (nearest == null) return null;
                var step = Math.Max(1, nearest.Count / 12);
                Element first = null;
                var looked = 0;
                for (var k = 0; k < nearest.Count; k += step)
                {
                    var away = nearest[k] - middle;
                    if (away.GetLength() < 1e-9) continue;
                    var point = nearest[k] + away.Normalize() * (1.0 / 304.8);
                    try
                    {
                        var projected = f.Project(point);
                        if (projected == null || !f.IsInside(projected.UVPoint)) continue;
                    }
                    catch (Exception) { continue; }
                    looked++;
                    var over = coveredBy(point, f.FaceNormal);
                    if (over == null) return null;
                    if (first == null) first = over;
                }
                return looked == 0 ? null : first;
            };
            if (face != null)
            {
                var over = portCoveredBy(face);
                if (over != null)
                {
                    problems.Add("The connector would go at the centre of the face on \"" + datum.Item3 + "\" ("
                        + centreOf(face) + ", facing " + pointing(face.FaceNormal) + "), and " + kindOf(over) + " "
                        + over.UniqueId + " covers its port there on the side the face points - the port would sit "
                        + "inside it. The face meant is probably that one's own outer face: name the plane it lies on"
                        + (wantedAt == null && formSaid.Length == 0 ? "" : ", and give form= or at= for it") + ".");
                    face = null;
                    faceIn = null;
                }
            }
        }
    }

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems);
}

if (refused == null)
{
    var fm = doc.FamilyManager;
    var round = chosen.Item1 != "electrical" && sizes.Count == 1;
    ConnectorElement made = null;

    // WHAT A JOINED FACE'S REFERENCE NAMES is measured on every run, not
    // assumed: the form that made the face, or the combination itself.
    var joinedFace = "";
    if (faceIn != null)
    {
        var named = doc.GetElement(face.Reference.ElementId);
        joinedFace = "The face is one of joined or cut forms (the combination " + faceIn.UniqueId + "); Revit's "
            + "reference to it names " + (named == null ? "no element"
                : named is GeomCombination ? "the combination itself" : kindOf(named) + " " + named.UniqueId) + ".";
    }
    var refusedHere = " NOTHING from this call was kept." + (faceIn == null ? "" : " " + joinedFace + " " + sideStep);
    // A refusal after the connector was made still says what the joined face's
    // reference named - the measurement a proof of version 3 records.
    var joinedNote = faceIn == null ? "" : " " + joinedFace;

    var swapped = false;
    var faceAlongX = 0.0;
    var faceAlongY = 0.0;
    // VERSION 8: the port's own size along the same axes - the face's, or less
    // in a ring's opening.
    var portAlongX = 0.0;
    var portAlongY = 0.0;
    var faceWord = "The face on \"" + faceName + "\" is ";
    var ringOpening = false;

    // VERSION 9 (2026-11-26, row 5b-2051): THE MAKING AND THE FIT CHECK RUN IN
    // A SubTransaction. Whether the port fits its face - or a ring's opening -
    // is known only once the connector is made, because the face is measured
    // along the connector's own axes. So the connector is made inside a
    // SubTransaction; when the port will not fit, it is rolled back and the
    // words come back through `refused`, nothing kept and the call ending
    // normally - version 8 threw them, and a thrown negative is no proof. Any
    // other failure in this stretch rolls the SubTransaction back before it
    // throws, so the host never unwinds over one still open.
    var trial = new SubTransaction(doc);
    trial.Start();
    try
    {
        try
        {
            if (chosen.Item1 == "duct")
                made = ConnectorElement.CreateDuctConnector(doc, (DuctSystemType)chosen.Item3,
                    round ? ConnectorProfileType.Round : ConnectorProfileType.Rectangular, face.Reference);
            else if (chosen.Item1 == "pipe")
                made = ConnectorElement.CreatePipeConnector(doc, (PipeSystemType)chosen.Item3, face.Reference);
            else if (chosen.Item1 == "cable tray")
                made = ConnectorElement.CreateCableTrayConnector(doc, face.Reference);
            else if (chosen.Item1 == "conduit")
                made = ConnectorElement.CreateConduitConnector(doc, face.Reference);
            else
                made = ConnectorElement.CreateElectricalConnector(doc, (ElectricalSystemType)chosen.Item3, face.Reference);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the " + spaced(chosen.Item2) + " connector on the "
                + "face lying on \"" + faceName + "\": " + ex.Message + refusedHere);
        }
        if (made == null)
            throw new InvalidOperationException("Revit made no " + spaced(chosen.Item2) + " connector on the face lying "
                + "on \"" + faceName + "\", and gave no reason." + refusedHere);

        // WHICH WAY ROUND. Revit takes a connector's own width direction from the
        // FACE, not from the family's X, so "Neck Width" tied to the connector's
        // width can come out lying along the neck's depth. The face is therefore
        // measured along the connector's own axes, and the two parameters are tied
        // the way round the face is actually built - a 400 x 300 neck gets a
        // 400 x 300 port whichever way its width runs. Parameters that match the
        // face neither way round would size a port that does not fit the face it
        // sits on, and are refused.
        //
        // A RING FACE IS MEASURED BY ITS OPENING (version 5, 2026-10-24). A flange's
        // face - a ring round the duct opening - is the face a port belongs on, and
        // its outline is the flange, not the port: a 600 x 400 duct's flange ring
        // reads 640 x 440 outside. So when one inner loop of the face holds the
        // connector's centre, that loop - the opening - is what is measured, and the
        // connector must sit at its centre (measured on Revit 2020: a ring face takes
        // the connector at its centre and the connector follows the plane the ring
        // is sketched on). A face of one loop is measured whole, exactly as before.
        if ((chosen.Item1 == "duct" || chosen.Item1 == "cable tray") && !round)
        {
            var frame = made.CoordinateSystem;
            var boxes = new List<double[]>();
            foreach (EdgeArray loop in face.EdgeLoops)
            {
                var points = new List<XYZ>();
                foreach (Edge edge in loop)
                    points.AddRange(edge.Tessellate());
                var acrossX = points.Select(q => q.DotProduct(frame.BasisX)).ToList();
                var acrossY = points.Select(q => q.DotProduct(frame.BasisY)).ToList();
                boxes.Add(new[] { acrossX.Min(), acrossX.Max(), acrossY.Min(), acrossY.Max() });
            }
            var outline = boxes.OrderByDescending(b => (b[1] - b[0]) * (b[3] - b[2])).First();
            var centreX = made.Origin.DotProduct(frame.BasisX);
            var centreY = made.Origin.DotProduct(frame.BasisY);
            var openings = boxes.Where(b => b != outline && b[0] < centreX && centreX < b[1]
                && b[2] < centreY && centreY < b[3]).ToList();
            if (openings.Count > 1)
                throw new InvalidOperationException("The face on \"" + faceName + "\" has " + openings.Count + " openings "
                    + "round the connector's centre, so which one is the port is not clear. NOTHING from this call "
                    + "was kept." + joinedNote);
            var measured = outline;
            if (openings.Count == 1)
            {
                measured = openings[0];
                ringOpening = true;
                faceWord = "The opening of the ring face on \"" + faceName + "\" is ";
                var offX = (measured[0] + measured[1]) / 2 - centreX;
                var offY = (measured[2] + measured[3]) / 2 - centreY;
                if (Math.Abs(offX) > halfMillimetre || Math.Abs(offY) > halfMillimetre)
                    throw new InvalidOperationException("Revit put the connector " + mm(Math.Sqrt(offX * offX + offY * offY))
                        + " mm off the centre of the ring face's opening on \"" + faceName + "\" - a ring that is not "
                        + "even round its opening. NOTHING from this call was kept." + joinedNote);
            }
            faceAlongX = measured[1] - measured[0];
            faceAlongY = measured[3] - measured[2];

            var first = fm.CurrentType.AsDouble(sizes[0]);
            var second = fm.CurrentType.AsDouble(sizes[1]);
            var both = first.HasValue && second.HasValue;
            var asGiven = both
                && Math.Abs(first.Value - faceAlongX) <= halfMillimetre
                && Math.Abs(second.Value - faceAlongY) <= halfMillimetre;
            var turned = both
                && Math.Abs(first.Value - faceAlongY) <= halfMillimetre
                && Math.Abs(second.Value - faceAlongX) <= halfMillimetre;
            // VERSION 8 (row 5b-2032): IN A RING'S OPENING, A PORT UP TO ITS SIZE.
            // The opening is a bore - a flange is bored a little over its duct - so
            // the port must fit inside it, not equal it. A whole face is still
            // matched exactly: there the face is the port.
            var fitsAsGiven = ringOpening && both
                && first.Value <= faceAlongX + halfMillimetre && second.Value <= faceAlongY + halfMillimetre;
            var fitsTurned = ringOpening && both
                && first.Value <= faceAlongY + halfMillimetre && second.Value <= faceAlongX + halfMillimetre;

            // VERSION 9 (row 5b-2051): A PORT THAT WILL NOT FIT IS REFUSED, NOT
            // THROWN - the connector made to measure it is rolled back below.
            if (!asGiven && !turned && !fitsAsGiven && !fitsTurned)
                refused = faceWord + mm(faceAlongX) + " x "
                    + mm(faceAlongY) + " mm, and \"" + sizes[0].Definition.Name + "\" and \"" + sizes[1].Definition.Name
                    + "\" hold " + (first.HasValue ? mm(first.Value) : "nothing") + " and "
                    + (second.HasValue ? mm(second.Value) : "nothing") + " mm - a port sized by them would not fit "
                    + (ringOpening ? "inside that opening either way round" : "the face it sits on") + ". "
                    + (ringOpening ? "" : "On a whole face the port must be the face's own size. ")
                    + "NOTHING from this call was kept." + joinedNote;
            else
            {
                if (asGiven) swapped = false;
                else if (turned) swapped = true;
                else if (fitsAsGiven && fitsTurned)
                    // Both ways fit: the way round whose shape is nearer the opening's,
                    // as given on a tie (a square port in a square opening).
                    swapped = Math.Abs(first.Value - faceAlongY) + Math.Abs(second.Value - faceAlongX)
                        < Math.Abs(first.Value - faceAlongX) + Math.Abs(second.Value - faceAlongY) - halfMillimetre;
                else swapped = !fitsAsGiven;

                portAlongX = swapped ? second.Value : first.Value;
                portAlongY = swapped ? first.Value : second.Value;
                if (ringOpening && !asGiven && !turned)
                    findings.Add("The port, " + mm(portAlongX) + " x " + mm(portAlongY) + " mm from its parameters, sits "
                        + "centred in the ring face's opening of " + mm(faceAlongX) + " x " + mm(faceAlongY) + " mm - "
                        + mm((faceAlongX - portAlongX) / 2) + " and " + mm((faceAlongY - portAlongY) / 2) + " mm clear each "
                        + "side. The opening is not the port's size; the parameters are.");
            }
        }

        if (refused == null) trial.Commit();
        else trial.RollBack();
    }
    catch (Exception)
    {
        try { if (trial.HasStarted() && !trial.HasEnded()) trial.RollBack(); } catch (Exception) { }
        throw;
    }

    // The ties and the read-back run only for a port that fits (version 9).
    if (refused == null)
    {
        Action<BuiltInParameter, FamilyParameter, string> tie = (own, driver, role) =>
        {
            var ownParameter = made.get_Parameter(own);
            if (ownParameter == null || !fm.CanElementParameterBeAssociated(ownParameter))
                throw new InvalidOperationException("The connector's " + role + " cannot be tied to \""
                    + driver.Definition.Name + "\" in this Revit, so it would have stayed at one foot. NOTHING "
                    + "from this call was kept." + joinedNote);
            try
            {
                fm.AssociateElementParameterToFamilyParameter(ownParameter, driver);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not tie the connector's " + role + " to \""
                    + driver.Definition.Name + "\": " + ex.Message + " NOTHING from this call was kept." + joinedNote);
            }
            sizedBy.Add("connector " + role + " <- " + driver.Definition.Name);
        };

        if (chosen.Item1 != "electrical")
        {
            if (round) tie(BuiltInParameter.CONNECTOR_DIAMETER, sizes[0], "diameter");
            else
            {
                tie(BuiltInParameter.CONNECTOR_WIDTH, swapped ? sizes[1] : sizes[0], "width");
                tie(BuiltInParameter.CONNECTOR_HEIGHT, swapped ? sizes[0] : sizes[1], "height");
            }
        }

        doc.Regenerate();

        // READ BACK - and a connector that does not read what it was tied to, or
        // does not fit its face, is not kept at some other size: it THROWS.
        Func<Func<double>, double?> read = get => { try { return get(); } catch (Exception) { return null; } };
        var shape = "";

        if (chosen.Item1 == "electrical") shape = "no size";
        else if (round)
        {
            var diameter = read(() => made.Radius * 2);
            var wanted = fm.CurrentType.AsDouble(sizes[0]);
            shape = "round " + (diameter.HasValue ? mm(diameter.Value) : "?") + " mm";
            if (!diameter.HasValue || !wanted.HasValue || Math.Abs(diameter.Value - wanted.Value) > halfMillimetre)
                throw new InvalidOperationException("The connector's diameter reads "
                    + (diameter.HasValue ? mm(diameter.Value) + " mm" : "nothing") + " and \"" + sizes[0].Definition.Name
                    + "\" holds " + (wanted.HasValue ? mm(wanted.Value) + " mm" : "nothing")
                    + ". NOTHING from this call was kept." + joinedNote);
        }
        else
        {
            var width = read(() => made.Width);
            var height = read(() => made.Height);
            shape = "rectangular " + (width.HasValue ? mm(width.Value) : "?") + " x "
                + (height.HasValue ? mm(height.Value) : "?") + " mm";
            // VERSION 8: against the port its parameters size - the face's own size
            // on a whole face, up to the opening's in a ring.
            if (!width.HasValue || !height.HasValue
                || Math.Abs(width.Value - portAlongX) > halfMillimetre || Math.Abs(height.Value - portAlongY) > halfMillimetre)
                throw new InvalidOperationException("The connector reads " + shape + " where its parameters size a port "
                    + mm(portAlongX) + " x " + mm(portAlongY) + " mm along the same axes, on a "
                    + (ringOpening ? "ring face whose opening is " : "face ") + mm(faceAlongX) + " x " + mm(faceAlongY)
                    + " mm - it does not fit. NOTHING from this call was kept." + joinedNote);

            if (swapped)
                findings.Add("Revit runs this connector's width along the face's other side, so \""
                    + sizes[1].Definition.Name + "\" drives its width and \"" + sizes[0].Definition.Name
                    + "\" its height - which makes the port match the face as it is built.");
        }

        var origin = made.Origin;
        // Said in the result itself, a string, because a reply cuts a list to its
        // first three items and each to sixty characters.
        connector = chosen.Item1 + " connector, " + spaced(chosen.Item2) + ", " + shape + ", at X " + mm(origin.X)
            + ", Y " + mm(origin.Y) + ", Z " + mm(origin.Z) + ", pointing " + pointing(made.Direction)
            + ", on the face lying on \"" + faceName + "\""
            + (ringOpening ? ", centred in the ring face's opening and sized by its parameters" : "")
            + (faceIn != null ? ". " + joinedFace : "")
            + (alsoJoined > 0 ? ". It is a form's own face, which is used before a joined one; " + alsoJoined
                + " face(s) of joined or cut forms lie on the same plane too - check it is the face meant" : "");

        findings.Add("Made a " + connector + " - read back from the connector." + (sizedBy.Count > 0
            ? " Its size follows the family: " + string.Join("; ", sizedBy) + "."
            : ""));
        // VERSION 6: what form= and facing= left out on the same plane.
        if (facesLeftOut > 0)
            findings.Add(facesLeftOut + " other face(s) lie on \"" + faceName + "\" and were left out by "
                + (formSaid.Length > 0 && facingSaid.Length > 0 ? "form= and facing="
                    : formSaid.Length > 0 ? "form=" : "facing=") + ".");
        // VERSION 7: what at= and the body left out.
        if (facesNotAt > 0)
            findings.Add(facesNotAt + " other face(s) lie on \"" + faceName + "\" with their centre more than 1 mm "
                + "from the point given in at=, and were left out.");
        if (facesIntoBody > 0)
            findings.Add(facesIntoBody + " other face(s) lie on \"" + faceName + "\" facing into another solid all "
                + "over - inside the body, not a port - and were left out.");

        findings.Add("Flow direction, description and electrical load are Revit's defaults - set them in "
            + "Properties. FLEX_FAMILY checks the connector follows its parameters through a resize.");
    }
}

if (refused != null) findings.Add(refused);
