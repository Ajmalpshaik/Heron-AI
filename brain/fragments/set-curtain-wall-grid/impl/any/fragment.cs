// NOT STANDALONE. Assumes `doc`, `curtainWallType` and `grid` are in scope;
// leaves `applied`, `alreadyAsAsked`, `differs`, `wallsOfType`,
// `systemsOfType`, `readBack`, `measured`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit keeps lengths in feet;
// the modeller's spacing is MILLIMETRES (D-71), turned into feet once, at the
// write, and back once, at the read.
//
// ===========================================================================
// VERSION 3 (2026-10-29): A CURTAIN SYSTEM TYPE AS WELL AS A CURTAIN WALL TYPE.
// ===========================================================================
//
// `curtainWallType` is the type's NAME, resolved here. Until version 3 the
// add-in resolved it among wall types (D-54) and a curtain system type's name
// was refused before this ran; the add-in has no binder for a curtain system
// type, so - as CREATE_CURTAIN_SYSTEM does - the name is matched here, by the
// add-in's own rule: the type name exactly, capitals included, or "Family:
// Type" / "Family : Type". It is matched among WALL types and CURTAIN SYSTEM
// types together, so a bare name both share is refused naming both, never
// chosen from (D-54 s3). Every name that resolved to one wall type in version
// 2 resolves to the same type here, unless a curtain system type now shares
// its bare name.
//
// A WALL TYPE IS VERSION 2 TO THE WORD - the same ids, keys, refusals,
// read-back, measurement and findings. Everything below that says "system" is
// the curtain system type's path, and only it.
//
// ===========================================================================
// A CURTAIN WALL TYPE CARRIES "Layout" AND "Spacing" TWICE - ONCE PER
// DIRECTION - SO NOTHING HERE LOOKS A SETTING UP BY ITS NAME.
// ===========================================================================
//
// Type Properties shows Layout and Spacing under Vertical Grid and again under
// Horizontal Grid. A lookup by the name the palette shows returns one of the
// two, and which one is not defined - which is why WRITE_ELEMENT_PARAMETERS
// refuses such a name (FRAGMENT-ISSUES 5b-203). Here each setting is reached
// by the id Revit gives it on a WALL type: SPACING_LAYOUT_VERT and _HORIZ,
// SPACING_LENGTH_VERT and _HORIZ.
//
// A CURTAIN SYSTEM TYPE'S TWO GRIDS ARE GRID 1 AND GRID 2, AND WHICH WAY EACH
// RUNS IS THE FACE'S, NOT THE TYPE'S. So its keys are "grid 1 layout", "grid 1
// spacing", "grid 2 layout", "grid 2 spacing", and a "vertical" or "horizontal"
// key on one is refused rather than guessed at. Its ids are taken to be
// SPACING_LAYOUT_1 and _2 and SPACING_LENGTH_1 and _2. THAT IS AN ASSUMPTION:
// the API reference of all eight releases lists those ids as "Layout" and
// "Spacing" - exactly as it lists the _VERT/_HORIZ and _U/_V ones - and says
// nothing of which element carries which. So a curtain system type carrying
// none of them is refused before anything is written, the ids it does carry
// are printed in `readBack` every run, and a setting written by those ids that
// does not read back as asked THROWS - nothing is kept. The proof (Group JP)
// settles it.
//
// THE LAYOUT IS A WHOLE NUMBER, AND WHICH NUMBER MEANS WHICH LAYOUT IS AN
// ASSUMPTION UNTIL A PROOF READS IT BACK. The numbers written are Revit's own
// SpacingRuleLayout values, cast from the enum rather than typed, and read off
// all eight releases' assemblies on 2026-09-24: None 0, FixedDistance 1,
// FixedNumber 2, MaximumSpacing 3, MinimumSpacing 5 - there is no 4. Nothing
// in the API reference says this parameter uses that enum. So after the write
// every layout is read back twice - its number, and the words Revit displays
// for it - and a number Revit displays as a DIFFERENT layout stops the call:
// nothing is kept. Words this does not recognise (a Revit in another language)
// cannot confirm it either way; that setting goes in `differs`, not counted.
//
// FIXED NUMBER IS REFUSED. Under it the count is "Number", which Revit shows in
// each placed WALL's Properties rather than the type's - so a type change
// cannot say how many, and every wall of the type would switch to a count
// nobody chose. For a curtain system type it is refused too: the reference
// lists SPACING_NUM_DIVISIONS_1 and _2 as "Number" and does not say whether
// the type or each curtain system holds it. Where Revit actually keeps Number,
// Justification, Angle and Offset is MEASURED on every run and printed in
// `readBack`, so the proof records it rather than this comment asserting it.
//
// U AND V ARE NOT READ AS VERTICAL AND HORIZONTAL. The API reference names a
// curtain grid's two families of lines U and V and never says which is
// vertical on a wall; for a curtain grid it calls Grid 1 "the U grid line
// pattern" and Grid 2 the V one, and still says nothing of which way either
// runs on a face. So the lines are sorted by the way each one actually runs -
// its curve, against Revit's own angle tolerance - and the set each came from
// is printed beside the count. The proof records the answer.
//
// NOTHING IS WRITTEN UNTIL THE WHOLE REQUEST IS SETTLED. Every refusal is made
// before the first write, so a refused request changes nothing. A write Revit
// will not take, or walls it cannot rebuild, throws: the executor rolls the
// whole call back, because a type half-changed across every wall that uses it
// is worse than one left alone.
//
// A DIRECTION'S LAYOUT IS WRITTEN BEFORE ITS SPACING, AND THE SPACING IS JUDGED
// UNDER THE LAYOUT IT WILL HAVE. Revit keeps Spacing READ-ONLY while the Layout
// is None or Fixed Number - Type Properties greys it out. Until 2026-10-06 this
// asked whether the spacing could be written BEFORE writing the layout that
// frees it, so "layout fixed distance; spacing 2500" on a type whose layout was
// None was refused, and the refusal blamed "another user in a shared model" on
// a model that was not shared (FRAGMENT-ISSUES 5b-328). Read-only is Revit's
// own flag; who holds an element is asked of Revit and said only when Revit
// says it.

const double MillimetresPerFoot = 304.8;

// THE SPACING THIS WILL SET. The bounds are for a number typed in the wrong
// unit, not a design rule: 1.5 meant as metres would put a grid line every
// 1.5 mm on every wall of the type, and 1,500,000 is no bay anybody draws.
const double SmallestSpacingMm = 100.0;
const double LargestSpacingMm = 100000.0;

// How far a spacing read back may sit from the one asked for and still be it.
const double SameSpacingMm = 0.5;

var applied = 0;
var alreadyAsAsked = 0;
var differs = new List<string>();
var wallsOfType = 0;
var systemsOfType = 0;
var readBack = "";
var measured = "";
var refused = "";
var findings = new List<string>();

// Lower case, one space between words. These are THIS file's words - the
// setting names and the layouts - so their spelling is loosened here; no name
// the model owns ever passes through it.
Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

// A type the way the Properties palette writes it - "Curtain Wall: Storefront".
Func<ElementType, string> fullName = type =>
{
    if (type == null) return "(no type)";
    string family = "";
    try { family = type.FamilyName; } catch { family = ""; }
    return string.IsNullOrEmpty(family) ? type.Name : family + ": " + type.Name;
};

// ---------------------------------------------------------------------------
// THE TYPE, BY NAME - among wall types and curtain system types, by the
// add-in's own rule (D-54): the name exactly, or "Family: Type" with or without
// a space before the colon, each half exact. Two found is refused, never
// chosen from.
// ---------------------------------------------------------------------------

var typeAsked = curtainWallType ?? "";
WallType asWall = null;
CurtainSystemType asSystem = null;
var nameProblem = "";

Func<ElementType, bool> nameFits = type =>
{
    string name, family;
    try { name = type.Name; } catch { return false; }
    if (string.Equals(name, typeAsked, StringComparison.Ordinal)) return true;
    try { family = type.FamilyName; } catch { return false; }
    if (string.IsNullOrEmpty(family) || name == null) return false;
    var colon = typeAsked.IndexOf(':');
    return colon >= 0
        && string.Equals(typeAsked.Substring(0, colon).Trim(), family, StringComparison.Ordinal)
        && string.Equals(typeAsked.Substring(colon + 1).Trim(), name, StringComparison.Ordinal);
};

if (typeAsked.Trim().Length == 0)
{
    nameProblem = "No wall type was named. Name the curtain wall type the way Revit writes it - "
        + "'Curtain Wall: Storefront' - or a curtain system type, 'Curtain System: ' and its name, "
        + "and ask again. Nothing was changed";
}
else
{
    var fits = new List<ElementType>();
    var curtainWallNames = new List<string>();
    var curtainSystemNames = new List<string>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType())
    {
        var wallType = element as WallType;
        var systemType = element as CurtainSystemType;
        if (wallType == null && systemType == null) continue;
        if (wallType != null)
        {
            try { if (wallType.Kind == WallKind.Curtain) curtainWallNames.Add(fullName(wallType)); }
            catch (Exception) { }
        }
        if (systemType != null) curtainSystemNames.Add(fullName(systemType));
        if (nameFits((ElementType)element)) fits.Add((ElementType)element);
    }

    if (fits.Count == 1)
    {
        asWall = fits[0] as WallType;
        asSystem = fits[0] as CurtainSystemType;
    }
    else if (fits.Count == 0)
    {
        Func<List<string>, string> listed = names => names.Count == 0 ? "none"
            : string.Join(", ", names.Distinct().OrderBy(n => n, StringComparer.Ordinal).Take(30).ToArray())
              + (names.Distinct().Count() > 30 ? ", and more" : "");
        nameProblem = string.Format(
            "No wall type or curtain system type called \"{0}\" in {1}. The name has to match what Revit "
            + "shows, capitals included - the Properties palette writes it \"Curtain Wall: Storefront\", and "
            + "either that or just \"Storefront\" works when the short name is unique. Its curtain wall types: "
            + "{2}. Its curtain system types: {3}. Nothing was changed",
            typeAsked, doc.Title, listed(curtainWallNames), listed(curtainSystemNames));
    }
    else
    {
        var choices = fits.Select(t => fullName(t)).Distinct().ToList();
        nameProblem = string.Format(
            "{0} types in {1} are called \"{2}\" - wall types and curtain system types are both searched - "
            + "so the name does not say which one is meant.{3} Nothing was changed",
            fits.Count, doc.Title, typeAsked,
            choices.Count > 1 ? " Say which: " + string.Join(", or ", choices.ToArray()) + "." : " Rename one.");
    }
}

var isSystem = asSystem != null;
HostObjAttributes chosen = isSystem ? (HostObjAttributes)asSystem : asWall;
var label = chosen == null ? "" : fullName(chosen);

// What one of it is called, and what reaches it - walls, or curtain systems.
var oneUser = isSystem ? "curtain system" : "wall";
var manyUsers = isSystem ? "curtain systems" : "walls";

// The two directions, in the order every array below uses: a wall type's
// vertical and horizontal grids, or a curtain system type's Grid 1 and Grid 2.
var directions = isSystem ? new[] { "grid 1", "grid 2" } : new[] { "vertical", "horizontal" };
var layoutIds = isSystem
    ? new[] { BuiltInParameter.SPACING_LAYOUT_1, BuiltInParameter.SPACING_LAYOUT_2 }
    : new[] { BuiltInParameter.SPACING_LAYOUT_VERT, BuiltInParameter.SPACING_LAYOUT_HORIZ };
var spacingIds = isSystem
    ? new[] { BuiltInParameter.SPACING_LENGTH_1, BuiltInParameter.SPACING_LENGTH_2 }
    : new[] { BuiltInParameter.SPACING_LENGTH_VERT, BuiltInParameter.SPACING_LENGTH_HORIZ };

// What the modeller types, what Type Properties shows, and the number written.
var layoutWords = new[] { "none", "fixed distance", "fixed number", "maximum spacing", "minimum spacing" };
var layoutShown = new[] { "None", "Fixed Distance", "Fixed Number", "Maximum Spacing", "Minimum Spacing" };
var layoutValues = new[]
{
    (int)SpacingRuleLayout.None,
    (int)SpacingRuleLayout.FixedDistance,
    (int)SpacingRuleLayout.FixedNumber,
    (int)SpacingRuleLayout.MaximumSpacing,
    (int)SpacingRuleLayout.MinimumSpacing,
};
var fixedNumber = Array.IndexOf(layoutValues, (int)SpacingRuleLayout.FixedNumber);

// The grid settings Revit is believed to keep on each WALL - or each curtain
// system - rather than on the type. Measured below, never relied on.
var perWallIds = isSystem
    ? new[]
    {
        BuiltInParameter.SPACING_NUM_DIVISIONS_1, BuiltInParameter.SPACING_NUM_DIVISIONS_2,
        BuiltInParameter.SPACING_JUSTIFICATION_1, BuiltInParameter.SPACING_JUSTIFICATION_2,
        BuiltInParameter.CURTAINGRID_ANGLE_1, BuiltInParameter.CURTAINGRID_ANGLE_2,
        BuiltInParameter.CURTAINGRID_ORIGIN_1, BuiltInParameter.CURTAINGRID_ORIGIN_2,
    }
    : new[]
    {
        BuiltInParameter.SPACING_NUM_DIVISIONS_VERT, BuiltInParameter.SPACING_NUM_DIVISIONS_HORIZ,
        BuiltInParameter.SPACING_JUSTIFICATION_VERT, BuiltInParameter.SPACING_JUSTIFICATION_HORIZ,
        BuiltInParameter.CURTAINGRID_ANGLE_VERT, BuiltInParameter.CURTAINGRID_ANGLE_HORIZ,
        BuiltInParameter.CURTAINGRID_ORIGIN_VERT, BuiltInParameter.CURTAINGRID_ORIGIN_HORIZ,
    };
var perWallNames = isSystem
    ? new[]
    {
        "grid 1 Number", "grid 2 Number", "grid 1 Justification", "grid 2 Justification",
        "grid 1 Angle", "grid 2 Angle", "grid 1 Offset", "grid 2 Offset",
    }
    : new[]
    {
        "vertical Number", "horizontal Number", "vertical Justification", "horizontal Justification",
        "vertical Angle", "horizontal Angle", "vertical Offset", "horizontal Offset",
    };

// WHICH GRID IDS A CURTAIN SYSTEM TYPE CARRIES - the assumption above, read
// off the type rather than trusted. Each of Revit's three families of grid
// ids, and the plain one, named carried or not.
var probeIds = new[]
{
    BuiltInParameter.SPACING_LAYOUT_1, BuiltInParameter.SPACING_LAYOUT_2,
    BuiltInParameter.SPACING_LENGTH_1, BuiltInParameter.SPACING_LENGTH_2,
    BuiltInParameter.SPACING_LAYOUT_U, BuiltInParameter.SPACING_LAYOUT_V,
    BuiltInParameter.SPACING_LAYOUT_VERT, BuiltInParameter.SPACING_LAYOUT_HORIZ,
    BuiltInParameter.SPACING_LAYOUT,
};
Func<string> idsCarried = () =>
{
    var carried = new List<string>();
    var notCarried = new List<string>();
    foreach (var id in probeIds)
    {
        Parameter found = null;
        try { found = chosen.get_Parameter(id); } catch (Exception) { found = null; }
        (found != null ? carried : notCarried).Add(id.ToString());
    }
    return string.Format("Of Revit's grid ids, '{0}' carries: {1}; not: {2}", label,
        carried.Count == 0 ? "none" : string.Join(", ", carried),
        notCarried.Count == 0 ? "none" : string.Join(", ", notCarried));
};

// WHY REVIT KEEPS A SETTING READ-ONLY, as far as Revit itself says - never a
// guess (5b-328). Whether another user holds the type is asked of Revit, and
// only in a shared model. `when` is the state it was asked in, or "".
Func<string, string, string> readOnlyBecause = (what, when) =>
{
    var holder = "";
    if (doc.IsWorkshared && chosen != null)
    {
        try
        {
            string owner;
            if (WorksharingUtils.GetCheckoutStatus(doc, chosen.Id, out owner)
                == CheckoutStatus.OwnedByOtherUser)
                holder = string.IsNullOrEmpty(owner) ? "another user" : owner;
        }
        catch (Exception) { holder = ""; }
    }
    return holder.Length > 0
        ? string.Format("Revit keeps the {1} of '{0}' read-only{2}, and in this shared model '{0}' is "
            + "held by {3} - they relinquish it, then ask again", label, what, when, holder)
        : string.Format("Revit keeps the {1} of '{0}' read-only{2} and gives no reason for it - {3}. "
            + "Look at the setting in Type Properties, where Revit greys out what it will not let "
            + "change", label, what, when, doc.IsWorkshared
                ? "nobody else holds the type in this shared model"
                : "this model is not shared, so no other user holds it");
};

// Why a setting on the type cannot be written, or null when it can. A curtain
// system type with no such setting says which grid ids it does carry.
Func<Parameter, StorageType, string, string> cannotWrite = (parameter, storage, what) =>
    parameter == null
        ? string.Format("'{0}' has no {1} - Revit gives this type none to set", label, what)
          + (isSystem ? ". " + idsCarried() : "")
    : parameter.StorageType != storage
        ? string.Format("'{0}' keeps its {1} in a form this does not write", label, what)
    : parameter.IsReadOnly
        ? readOnlyBecause(what, "")
    : null;

// Whether a layout - Revit's number for it - spaces its lines evenly by a
// distance, which is when Revit lets that direction's Spacing be written.
Func<int, bool> usesSpacing = number =>
    number == (int)SpacingRuleLayout.FixedDistance
    || number == (int)SpacingRuleLayout.MaximumSpacing
    || number == (int)SpacingRuleLayout.MinimumSpacing;

// The layout Revit HOLDS, as a number - int.MinValue when it holds none.
Func<Parameter, int> layoutNumber = parameter =>
    parameter != null && parameter.StorageType == StorageType.Integer
        ? parameter.AsInteger() : int.MinValue;

// The words Revit DISPLAYS for it, or null.
Func<Parameter, string> layoutWordsShown = parameter =>
{
    if (parameter == null) return null;
    try { return parameter.AsValueString(); } catch { return null; }
};

// The spacing Revit holds, in millimetres - NaN when it holds none.
Func<Parameter, double> spacingMm = parameter =>
    parameter != null && parameter.StorageType == StorageType.Double
        ? parameter.AsDouble() * MillimetresPerFoot : double.NaN;

var askedLayout = new[] { -1, -1 };
var askedSpacingMm = new[] { double.NaN, double.NaN };
var layoutParameter = new Parameter[2];
var spacingParameter = new Parameter[2];

if (nameProblem.Length > 0)
{
    refused = nameProblem;
}
else if (!isSystem && asWall.Kind != WallKind.Curtain)
{
    var kind = asWall.Kind == WallKind.Basic ? "a basic wall type"
        : asWall.Kind == WallKind.Stacked ? "a stacked wall type"
        : "a wall type Revit does not class as a curtain wall";
    refused = string.Format(
        "'{0}' is {1}, not a curtain wall type - it has no grid to lay out. Name a curtain wall "
        + "type, 'Curtain Wall: Storefront' for one. Nothing was changed", label, kind);
}
else if (grid == null || grid.Count == 0)
{
    refused = isSystem
        ? "Nothing was named to change. Name each setting and what it becomes - "
            + "'grid 1 layout=fixed distance; grid 1 spacing=1500'. Nothing was changed"
        : "Nothing was named to change. Name each setting and what it becomes - "
            + "'vertical layout=fixed distance; vertical spacing=1500'. Nothing was changed";
}
else
{
    // THE REQUEST, READ WHOLE BEFORE ANYTHING IS TOUCHED.
    var seen = new List<string>();
    foreach (var pair in grid)
    {
        var key = plain(pair.Key);
        var value = (pair.Value ?? "").Trim();

        if (seen.Contains(key))
        {
            refused = string.Format(
                "'{0}' is named twice, and only one of the two could be kept. Say it once. "
                + "Nothing was changed", key);
            break;
        }
        seen.Add(key);

        int direction;
        bool isLayout;
        if (key == directions[0] + " layout") { direction = 0; isLayout = true; }
        else if (key == directions[1] + " layout") { direction = 1; isLayout = true; }
        else if (key == directions[0] + " spacing") { direction = 0; isLayout = false; }
        else if (key == directions[1] + " spacing") { direction = 1; isLayout = false; }
        else if (isSystem && (key.StartsWith("vertical") || key.StartsWith("horizontal")))
        {
            refused = string.Format(
                "'{0}' is a curtain WALL type's setting, and '{1}' is a curtain system type. Its two grids "
                + "are Grid 1 and Grid 2, and which way each one runs is set by the face it lies on, not "
                + "by the type - so this does not call either of them vertical or horizontal. Name one or "
                + "more of: grid 1 layout, grid 1 spacing, grid 2 layout, grid 2 spacing. Nothing was "
                + "changed", pair.Key, label);
            break;
        }
        else
        {
            refused = isSystem
                ? string.Format(
                    "'{0}' is not a grid setting this changes on a curtain system type. Name one or more "
                    + "of: grid 1 layout, grid 1 spacing, grid 2 layout, grid 2 spacing. Nothing was changed",
                    pair.Key)
                : string.Format(
                    "'{0}' is not a grid setting this changes. Name one or more of: vertical layout, "
                    + "vertical spacing, horizontal layout, horizontal spacing. Nothing was changed",
                    pair.Key);
            break;
        }

        if (isLayout)
        {
            var at = Array.IndexOf(layoutWords, plain(value));
            if (at < 0)
            {
                refused = string.Format(
                    "'{0}' is not a layout. The {1} layout is one of: none, fixed distance, maximum "
                    + "spacing, minimum spacing. Nothing was changed", value, directions[direction]);
                break;
            }
            if (at == fixedNumber)
            {
                refused = isSystem
                    ? string.Format(
                        "Fixed Number is not set here. Under Fixed Number the COUNT of {0} lines is "
                        + "'Number', and Revit's API reference does not say whether a curtain system type "
                        + "holds it or each curtain system does - for a curtain wall it is each placed "
                        + "wall's own. Changing the layout alone would leave every curtain system of '{1}' "
                        + "with a count nobody chose. Choose Fixed Number in Type Properties and give the "
                        + "Number by hand; Heron has no route for the count yet. Nothing was changed",
                        directions[direction], label)
                    : string.Format(
                        "Fixed Number is not set here. Under Fixed Number the COUNT of {0} grid lines is "
                        + "not a setting of the type: Revit shows it as 'Number' in each placed wall's own "
                        + "Properties, so changing the type cannot say how many - and every wall of '{1}' "
                        + "would switch to a count nobody chose. Choose Fixed Number in Type Properties "
                        + "and give each wall its Number by hand; Heron has no route for the per-wall "
                        + "count yet. Nothing was changed", directions[direction], label);
                break;
            }
            askedLayout[direction] = at;
        }
        else
        {
            double mm;
            if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out mm)
                || double.IsNaN(mm) || double.IsInfinity(mm))
            {
                refused = string.Format(
                    "'{0}' is not a spacing. Give the {1} spacing in millimetres, digits only - 1500, "
                    + "not 1500mm or 1.5 m. Nothing was changed", value, directions[direction]);
                break;
            }
            if (mm < SmallestSpacingMm || mm > LargestSpacingMm)
            {
                refused = string.Format(
                    "A {0} spacing of {1} mm is outside what this sets - {2:F0} to {3:F0} mm. The "
                    + "spacing is in MILLIMETRES, so 1.5 m is 1500. Nothing was changed",
                    directions[direction], value, SmallestSpacingMm, LargestSpacingMm);
                break;
            }
            askedSpacingMm[direction] = mm;
        }
    }

    // THE TYPE'S OWN SETTINGS, BY ID, and whether each asked-for one can be
    // written at all. A spacing's READ-ONLY flag is not asked here: it follows
    // the layout, and is judged below against the layout it will have.
    for (var d = 0; d < 2 && refused.Length == 0; d++)
    {
        layoutParameter[d] = chosen.get_Parameter(layoutIds[d]);
        spacingParameter[d] = chosen.get_Parameter(spacingIds[d]);

        if (askedLayout[d] >= 0)
        {
            var why = cannotWrite(layoutParameter[d], StorageType.Integer, directions[d] + " Layout");
            if (why != null) refused = why + ". Nothing was changed";
        }
        if (refused.Length == 0 && !double.IsNaN(askedSpacingMm[d]))
        {
            var why = spacingParameter[d] == null || spacingParameter[d].StorageType != StorageType.Double
                ? cannotWrite(spacingParameter[d], StorageType.Double, directions[d] + " Spacing")
                : null;
            if (why != null) refused = why + ". Nothing was changed";
        }
    }

    // A SPACING ONLY MEANS SOMETHING UNDER A LAYOUT THAT USES ONE. Under None
    // there are no lines, and under Fixed Number the count decides; a spacing
    // written there would change nothing, and reporting it as done would be the
    // write that did nothing reported as one that did.
    for (var d = 0; d < 2 && refused.Length == 0; d++)
    {
        var willBe = askedLayout[d] >= 0 ? layoutValues[askedLayout[d]] : layoutNumber(layoutParameter[d]);
        var spaced = usesSpacing(willBe);

        // A SPACING READ-ONLY WHILE ITS LAYOUT STAYS AS IT IS IS REFUSED NOW,
        // with Revit's reason. One whose layout this same request changes to a
        // spaced one is judged after that layout is written, in the write
        // below - before it, Revit's flag still describes the old layout.
        var layoutChanges = askedLayout[d] >= 0
            && layoutNumber(layoutParameter[d]) != layoutValues[askedLayout[d]];

        if (!double.IsNaN(askedSpacingMm[d]) && spaced && !layoutChanges
            && spacingParameter[d].IsReadOnly)
        {
            refused = readOnlyBecause(directions[d] + " Spacing", string.Format(
                " under its {0} layout of {1}", directions[d],
                layoutWordsShown(layoutParameter[d]) ?? "one that uses a spacing")) + ". Nothing was changed";
        }
        else if (!double.IsNaN(askedSpacingMm[d]) && !spaced)
        {
            var shownNow = askedLayout[d] >= 0 ? layoutShown[askedLayout[d]] : layoutWordsShown(layoutParameter[d]);
            refused = string.Format(
                "A {0} spacing does nothing while the {0} layout is {1} - there are no evenly spaced "
                + "lines to space. Say the layout as well: fixed distance, maximum spacing or minimum "
                + "spacing. Nothing was changed", directions[d],
                string.IsNullOrEmpty(shownNow) ? "not one that uses a spacing" : shownNow);
        }
        else if (askedLayout[d] >= 0 && spaced && double.IsNaN(askedSpacingMm[d]))
        {
            // THE TYPE'S OWN SPACING STAYS, as asked - but there has to be one.
            var held = spacingMm(spacingParameter[d]);
            if (double.IsNaN(held) || held <= 0)
                refused = string.Format(
                    "A {0} layout of {1} needs a spacing, and '{2}' holds none. Give one - '{0} "
                    + "spacing=1500'. Nothing was changed", directions[d], layoutShown[askedLayout[d]], label);
        }
    }
}

if (refused.Length == 0)
{
    // THE REACH, COUNTED BEFORE ANYTHING IS WRITTEN. Every wall - or curtain
    // system - of this type takes the change; it goes first in the answer, not
    // after it.
    var walls = new List<Wall>();
    var systems = new List<CurtainSystem>();
    if (isSystem)
    {
        foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_CurtaSystem)
                     .WhereElementIsNotElementType())
        {
            var system = element as CurtainSystem;
            if (system == null) continue;
            ElementId typeId = null;
            try { typeId = system.GetTypeId(); } catch { continue; }
            if (typeId != null && typeId.Equals(chosen.Id)) systems.Add(system);
        }
        systemsOfType = systems.Count;
    }
    else
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
        {
            var wall = element as Wall;
            if (wall == null) continue;
            ElementId typeId = null;
            try { typeId = wall.GetTypeId(); } catch { continue; }
            if (typeId != null && typeId.Equals(chosen.Id)) walls.Add(wall);
        }
        wallsOfType = walls.Count;
    }
    var usersOfType = isSystem ? systemsOfType : wallsOfType;

    // A placed element's curtain grids - a wall has one, a curtain system one
    // per face, the API reference says. Empty when none can be read.
    Func<Element, List<CurtainGrid>> gridsOf = element =>
    {
        var found = new List<CurtainGrid>();
        var wall = element as Wall;
        if (wall != null)
        {
            CurtainGrid cells = null;
            try { cells = wall.CurtainGrid; } catch { cells = null; }
            if (cells != null) found.Add(cells);
            return found;
        }
        var system = element as CurtainSystem;
        if (system == null) return found;
        CurtainGridSet all = null;
        try { all = system.CurtainGrids; } catch { all = null; }
        if (all == null) return found;
        foreach (CurtainGrid each in all) if (each != null) found.Add(each);
        return found;
    };

    // ONE TO MEASURE: the first whose grid can be read. Which one does not
    // matter - the type is the same on all of them - and it is named by id.
    Element sample = null;
    foreach (var each in isSystem ? systems.Cast<Element>() : walls.Cast<Element>())
    {
        if (gridsOf(each).Count > 0) { sample = each; break; }
    }

    // WHICH WAY A CURVE RUNS: 0 vertical, 1 horizontal, 2 at an angle, -1 not
    // readable. Revit's own angle tolerance decides "vertical" and "level".
    var steep = Math.Cos(doc.Application.AngleTolerance);
    var level = Math.Sin(doc.Application.AngleTolerance);
    Func<Curve, int> runs = curve =>
    {
        if (curve == null) return -1;
        try
        {
            var along = curve.GetEndPoint(1) - curve.GetEndPoint(0);
            var length = along.GetLength();
            if (length <= 0) return -1;
            var rise = Math.Abs(along.Z) / length;
            if (rise >= steep) return 0;
            if (rise <= level) return 1;
            return 2;
        }
        catch { return -1; }
    };

    // GRID LINES BY THE WAY EACH ONE RUNS, and by the set Revit returned it
    // in: [way 0-2, or 3 when unreadable, set 0 = U or 1 = V].
    Func<List<CurtainGrid>, int[,]> countLines = cellsList =>
    {
        var counts = new int[4, 2];
        foreach (var cells in cellsList)
        {
            for (var set = 0; set < 2; set++)
            {
                ICollection<ElementId> ids = null;
                try { ids = set == 0 ? cells.GetUGridLineIds() : cells.GetVGridLineIds(); }
                catch { ids = null; }
                if (ids == null) continue;
                foreach (var id in ids)
                {
                    var line = doc.GetElement(id) as CurtainGridLine;
                    Curve curve = null;
                    try { curve = line == null ? null : line.FullCurve; } catch { curve = null; }
                    var way = runs(curve);
                    counts[way < 0 ? 3 : way, set]++;
                }
            }
        }
        return counts;
    };

    Func<int[,], int, string> oneWay = (counts, way) =>
        counts[way, 0] + counts[way, 1] == 0
            ? "0"
            : string.Format("{0} (Revit's U set {1}, V set {2})",
                counts[way, 0] + counts[way, 1], counts[way, 0], counts[way, 1]);

    // MULLIONS, by the way each runs and by type - what gets ordered. One
    // mullion counted once, even where two faces' grids both hand it back.
    Func<List<CurtainGrid>, string> mullionsOn = cellsList =>
    {
        if (cellsList.Count == 0) return "not readable";
        var tally = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var counted = new HashSet<string>();
        foreach (var cells in cellsList)
        {
            ICollection<ElementId> ids = null;
            try { ids = cells.GetMullionIds(); } catch { return "not readable"; }
            foreach (var id in ids)
            {
                if (!counted.Add(id.ToString())) continue;
                var mullion = doc.GetElement(id) as Mullion;
                if (mullion == null) continue;
                var type = doc.GetElement(mullion.GetTypeId()) as ElementType;
                Curve along = null;
                try { along = mullion.LocationCurve; } catch { along = null; }
                var way = runs(along);
                var key = (way == 0 ? "vertical" : way == 1 ? "horizontal" : way == 2 ? "at an angle" : "unmeasured")
                    + " '" + fullName(type) + "'";
                int had;
                tally[key] = tally.TryGetValue(key, out had) ? had + 1 : 1;
            }
        }
        return tally.Count == 0 ? "none" : string.Join(", ", tally.Select(p => p.Value + " x " + p.Key));
    };

    // EACH FACE OF A CURTAIN SYSTEM, its U and V lines - which of the type's
    // Grid 1 and Grid 2 is which set is what the proof reads off this.
    Func<List<CurtainGrid>, List<int[]>> linesByFace = cellsList =>
    {
        var faces = new List<int[]>();
        foreach (var cells in cellsList)
        {
            int u = 0, v = 0;
            try { u = cells.GetUGridLineIds().Count; } catch { u = -1; }
            try { v = cells.GetVGridLineIds().Count; } catch { v = -1; }
            faces.Add(new[] { u, v });
        }
        return faces;
    };

    Func<Element, string> lengthOf = element =>
    {
        var at = element.Location as LocationCurve;
        return at == null || at.Curve == null
            ? "its length not read"
            : string.Format("{0:F0} mm long", at.Curve.Length * MillimetresPerFoot);
    };

    // WHERE REVIT KEEPS THE PER-WALL GRID SETTINGS - measured, because it
    // decides what Fixed Number could ever mean here.
    var onType = new List<string>();
    var onWall = new List<string>();
    for (var i = 0; i < perWallIds.Length; i++)
    {
        if (chosen.get_Parameter(perWallIds[i]) != null) onType.Add(perWallNames[i]);
        if (sample != null && sample.get_Parameter(perWallIds[i]) != null) onWall.Add(perWallNames[i]);
    }
    var carriers = string.Format(
        "Number, Justification, Angle and Offset, both {0}, measured - on the TYPE: {1}; {2}",
        isSystem ? "grids" : "ways",
        onType.Count == 0 ? "none of them" : string.Join(", ", onType),
        sample == null
            ? string.Format("no {0} of the type is placed to measure", oneUser)
            : string.Format("on {0} {1}: {2}", oneUser, sample.Id,
                onWall.Count == 0 ? "none of them"
                : onWall.Count == perWallIds.Length ? "all eight" : string.Join(", ", onWall)));

    // WHAT REVIT HOLDS BEFORE, so the answer can say what changed.
    var layoutBefore = new int[2];
    var layoutShownBefore = new string[2];
    var spacingBeforeMm = new double[2];
    for (var d = 0; d < 2; d++)
    {
        layoutBefore[d] = layoutNumber(layoutParameter[d]);
        layoutShownBefore[d] = layoutWordsShown(layoutParameter[d]);
        spacingBeforeMm[d] = spacingMm(spacingParameter[d]);
    }
    var linesBefore = sample == null ? null : countLines(gridsOf(sample));
    var facesBefore = sample == null ? null : linesByFace(gridsOf(sample));
    var mullionsBefore = sample == null ? "" : mullionsOn(gridsOf(sample));

    // THE WRITE. Only what was asked, and only where it differs from what Revit
    // already holds.
    var wroteLayout = new bool[2];
    var wroteSpacing = new bool[2];
    for (var d = 0; d < 2; d++)
    {
        if (askedLayout[d] >= 0 && layoutBefore[d] != layoutValues[askedLayout[d]])
        {
            bool took;
            try
            {
                took = layoutParameter[d].Set(layoutValues[askedLayout[d]]);
            }
            catch (Exception notTaken)
            {
                throw new InvalidOperationException(string.Format(
                    "Revit would not take the {0} layout {1} on '{2}' - {3}. Nothing this call did "
                    + "is kept", directions[d], layoutShown[askedLayout[d]], label, notTaken.Message));
            }
            if (!took)
                throw new InvalidOperationException(string.Format(
                    "Revit refused the {0} layout {1} on '{2}'. Nothing this call did is kept",
                    directions[d], layoutShown[askedLayout[d]], label));
            wroteLayout[d] = true;
        }

        if (!double.IsNaN(askedSpacingMm[d])
            && !(Math.Abs(spacingBeforeMm[d] - askedSpacingMm[d]) < SameSpacingMm))
        {
            // THE SPACING UNDER THE LAYOUT IT NOW HAS - asked of the type afresh.
            // A layout just written may free it only once Revit has rebuilt the
            // type, so a spacing still read-only is asked again after one
            // rebuild before it is called refused (5b-328).
            var spacingNow = chosen.get_Parameter(spacingIds[d]);
            if (wroteLayout[d] && spacingNow != null && spacingNow.IsReadOnly)
            {
                try
                {
                    doc.Regenerate();
                }
                catch (Exception notRebuilt)
                {
                    throw new InvalidOperationException(string.Format(
                        "Revit could not rebuild '{0}' with the {1} layout {2} - {3}. Nothing this call "
                        + "did is kept", label, directions[d], layoutShown[askedLayout[d]], notRebuilt.Message));
                }
                spacingNow = chosen.get_Parameter(spacingIds[d]);
            }
            if (spacingNow == null || spacingNow.IsReadOnly)
                throw new InvalidOperationException(string.Format(
                    "{0}. Nothing this call did is kept",
                    spacingNow == null
                        ? string.Format("'{0}' has no {1} Spacing - Revit gives this type none to set",
                            label, directions[d])
                        : readOnlyBecause(directions[d] + " Spacing", wroteLayout[d]
                            ? string.Format(" even with its {0} layout just set to {1}",
                                directions[d], layoutShown[askedLayout[d]])
                            : "")));
            spacingParameter[d] = spacingNow;

            bool took;
            try
            {
                // MILLIMETRES INTO FEET - the one conversion in, D-20.
                took = spacingParameter[d].Set(askedSpacingMm[d] / MillimetresPerFoot);
            }
            catch (Exception notTaken)
            {
                throw new InvalidOperationException(string.Format(
                    "Revit would not take a {0} spacing of {1:F0} mm on '{2}' - {3}. Nothing this call "
                    + "did is kept", directions[d], askedSpacingMm[d], label, notTaken.Message));
            }
            if (!took)
                throw new InvalidOperationException(string.Format(
                    "Revit refused a {0} spacing of {1:F0} mm on '{2}'. Nothing this call did is kept",
                    directions[d], askedSpacingMm[d], label));
            wroteSpacing[d] = true;
        }
    }

    // THE REBUILD IS NOT SWALLOWED. Revit's reference for Regenerate says a
    // failure there leaves a model that must not be read again, and the
    // transaction has to be abandoned - so it is thrown straight back.
    if (wroteLayout[0] || wroteLayout[1] || wroteSpacing[0] || wroteSpacing[1])
    {
        try
        {
            doc.Regenerate();
        }
        catch (Exception notRebuilt)
        {
            throw new InvalidOperationException(string.Format(
                "Revit could not rebuild the {0} of '{1}' with this grid - {2}. Nothing this call "
                + "did is kept", manyUsers, label, notRebuilt.Message));
        }
    }

    // READ BACK, off the type afresh - what Revit holds, never what was asked.
    var described = new List<string>();
    var held = new List<string>();
    for (var d = 0; d < 2; d++)
    {
        var layoutNow = chosen.get_Parameter(layoutIds[d]);
        var spacingNow = chosen.get_Parameter(spacingIds[d]);

        var nowValue = layoutNumber(layoutNow);
        var nowShown = layoutWordsShown(layoutNow);
        var valueAt = Array.IndexOf(layoutValues, nowValue);
        var shownAt = Array.IndexOf(layoutWords, plain(nowShown));

        // THE ASSUMPTION, CHECKED ON EVERY DIRECTION - written or not. A number
        // Revit shows as a different layout from the one this file maps it to
        // means the table above is wrong, and then any layout this call wrote
        // may be wrong too.
        if (shownAt >= 0 && valueAt != shownAt)
        {
            throw new InvalidOperationException(string.Format(
                "The {0} layout of '{1}' holds Revit's value {2}, which this takes to mean {3}, and "
                + "Revit shows it as '{4}'. The numbers this writes are not the ones Revit uses for "
                + "this setting, so nothing this call did is kept - the fragment's table of layouts "
                + "needs correcting before it is used again", directions[d], label, nowValue,
                valueAt >= 0 ? layoutShown[valueAt] : "no layout at all", nowShown));
        }

        Func<int, string, string> sayLayout = (number, words) =>
            number == int.MinValue ? "not carried by the type"
            : string.Format("{0} [Revit's value {1}]",
                Array.IndexOf(layoutWords, plain(words)) >= 0
                    ? layoutShown[Array.IndexOf(layoutWords, plain(words))]
                    : (string.IsNullOrEmpty(words) ? "(shown as nothing)" : "'" + words + "'"),
                number);
        Func<double, string> saySpacing = mm =>
            double.IsNaN(mm) ? "not carried by the type" : string.Format("{0:F1} mm", mm);

        var nowMm = spacingMm(spacingNow);

        described.Add(string.Format(
            "{0}: layout {1}, was {2}{3}; spacing {4}, was {5}{6}",
            directions[d],
            sayLayout(nowValue, nowShown), sayLayout(layoutBefore[d], layoutShownBefore[d]),
            askedLayout[d] >= 0 ? " - asked" : " - not asked, left as it was",
            saySpacing(nowMm), saySpacing(spacingBeforeMm[d]),
            !double.IsNaN(askedSpacingMm[d]) ? " - asked" : " - not asked, left as it was"));

        // A CURTAIN SYSTEM TYPE'S IDS ARE THE ASSUMPTION (see the top). A
        // setting written by them that Revit does not then hold as asked means
        // the ids are not the ones Type Properties shows under Grid 1 and Grid
        // 2 - so nothing this call did is kept, rather than reported as a
        // difference beside a type changed somewhere nobody asked.
        if (isSystem && wroteLayout[d] && nowValue != layoutValues[askedLayout[d]])
            throw new InvalidOperationException(string.Format(
                "'{0}' was given the {1} layout {2} by {3} and holds {4} after the rebuild - that id may "
                + "not be the setting Type Properties shows under {5}, so nothing this call did is "
                + "kept. {6}", label, directions[d], layoutShown[askedLayout[d]], layoutIds[d],
                sayLayout(nowValue, nowShown), d == 0 ? "Grid 1" : "Grid 2", idsCarried()));
        if (isSystem && wroteSpacing[d]
            && (double.IsNaN(nowMm) || !(Math.Abs(nowMm - askedSpacingMm[d]) < SameSpacingMm)))
            throw new InvalidOperationException(string.Format(
                "'{0}' was given a {1} spacing of {2:F1} mm by {3} and holds {4} after the rebuild - that "
                + "id may not be the setting Type Properties shows under {5}, so nothing this call did "
                + "is kept. {6}", label, directions[d], askedSpacingMm[d], spacingIds[d], saySpacing(nowMm),
                d == 0 ? "Grid 1" : "Grid 2", idsCarried()));

        if (askedLayout[d] >= 0)
        {
            var wanted = layoutValues[askedLayout[d]];
            if (nowValue == wanted && shownAt == askedLayout[d])
            {
                if (wroteLayout[d]) applied++; else alreadyAsAsked++;
                held.Add(string.Format("{0} layout {1}", directions[d], layoutShown[askedLayout[d]]));
            }
            else if (nowValue != wanted)
            {
                differs.Add(string.Format("{0} layout: asked for {1}, and Revit holds {2}",
                    directions[d], layoutShown[askedLayout[d]], sayLayout(nowValue, nowShown)));
            }
            else
            {
                differs.Add(string.Format(
                    "{0} layout: Revit holds the value written for {1} and shows it as {2}, which this "
                    + "cannot match to a layout name - check it in Type Properties",
                    directions[d], layoutShown[askedLayout[d]],
                    string.IsNullOrEmpty(nowShown) ? "nothing" : "'" + nowShown + "'"));
            }
        }

        if (!double.IsNaN(askedSpacingMm[d]))
        {
            if (!double.IsNaN(nowMm) && Math.Abs(nowMm - askedSpacingMm[d]) < SameSpacingMm)
            {
                if (wroteSpacing[d]) applied++; else alreadyAsAsked++;
                held.Add(string.Format("{0} spacing {1:F0} mm", directions[d], nowMm));
            }
            else
            {
                differs.Add(string.Format("{0} spacing: asked for {1:F1} mm, and Revit holds {2}",
                    directions[d], askedSpacingMm[d], saySpacing(nowMm)));
            }
        }
    }
    readBack = string.Join(" | ", described) + " | " + carriers + (isSystem ? " | " + idsCarried() : "");

    // WHAT IT MADE, MEASURED ON A PLACED ONE - read off Revit, never worked out
    // from the spacing.
    if (sample == null)
    {
        measured = isSystem
            ? "No curtain system of this type is placed in the model, so nothing was measured. The next "
                + "curtain system made with it shows this grid"
            : "No wall of this type is placed in the model, so nothing was measured. The next "
                + "wall drawn with it shows this grid";
    }
    else
    {
        var linesAfter = countLines(gridsOf(sample));
        var unreadable = linesAfter[3, 0] + linesAfter[3, 1];
        if (isSystem)
        {
            var facesAfter = linesByFace(gridsOf(sample));
            var byFace = new List<string>();
            for (var f = 0; f < facesAfter.Count && f < 8; f++)
            {
                var was = f < facesBefore.Count ? facesBefore[f] : new[] { -1, -1 };
                byFace.Add(string.Format("face {0} U {1} -> {2}, V {3} -> {4}", f + 1,
                    was[0], facesAfter[f][0], was[1], facesAfter[f][1]));
            }
            if (facesAfter.Count > 8) byFace.Add(string.Format("and {0} more face(s)", facesAfter.Count - 8));
            measured = string.Format(
                "curtain system {0}, on {1} face(s): vertical grid lines {2} before, {3} now; horizontal "
                + "{4} before, {5} now; at an angle {6} before, {7} now{8}. By face, Revit's U and V lines "
                + "before -> now: {9}. Mullions before: {10}. Now: {11}",
                sample.Id, facesAfter.Count,
                oneWay(linesBefore, 0), oneWay(linesAfter, 0),
                oneWay(linesBefore, 1), oneWay(linesAfter, 1),
                linesBefore[2, 0] + linesBefore[2, 1], linesAfter[2, 0] + linesAfter[2, 1],
                unreadable == 0 ? "" : string.Format("; {0} line(s) whose direction could not be read", unreadable),
                string.Join("; ", byFace), mullionsBefore, mullionsOn(gridsOf(sample)));
        }
        else
        {
            measured = string.Format(
                "wall {0}, {1}: vertical grid lines {2} before, {3} now; horizontal {4} before, {5} now; "
                + "at an angle {6} before, {7} now{8}. Mullions before: {9}. Now: {10}",
                sample.Id, lengthOf(sample),
                oneWay(linesBefore, 0), oneWay(linesAfter, 0),
                oneWay(linesBefore, 1), oneWay(linesAfter, 1),
                linesBefore[2, 0] + linesBefore[2, 1], linesAfter[2, 0] + linesAfter[2, 1],
                unreadable == 0 ? "" : string.Format("; {0} line(s) whose direction could not be read", unreadable),
                mullionsBefore, mullionsOn(gridsOf(sample)));
        }
    }

    // THE REACH FIRST, then what Revit holds.
    if (applied == 0 && differs.Count == 0)
    {
        findings.Add(string.Format(
            "Nothing on '{0}' needed changing - every setting asked for was already so. It is used "
            + "by {1} {2}(s) in this model", label, usersOfType, oneUser));
    }
    else if (isSystem)
    {
        findings.Add(usersOfType == 0
            ? string.Format("'{0}' is used by no curtain system in this model, so nothing placed changed. "
                + "The next curtain system made with it takes this grid", label)
            : string.Format("'{0}' is used by {1} curtain system{2} in this model - {3} with the type. "
                + "For one curtain system alone, put it on a copy of the type first - DUPLICATE_TYPE, "
                + "then CHANGE_ELEMENT_TYPE; neither has been run on a curtain system yet",
                label, usersOfType, usersOfType == 1 ? "" : "s",
                usersOfType == 1 ? "it changed" : "every one of them changed"));
    }
    else
    {
        findings.Add(wallsOfType == 0
            ? string.Format("'{0}' is used by no wall in this model, so nothing placed changed. The "
                + "next wall drawn with it takes this grid", label)
            : string.Format("'{0}' is used by {1} wall{2} in this model - {3} with the type. For "
                + "one wall alone, DUPLICATE_TYPE first and put that wall on the copy",
                label, wallsOfType, wallsOfType == 1 ? "" : "s",
                wallsOfType == 1 ? "it changed" : "every one of them changed"));
    }
    findings.Add(differs.Count == 0
        ? string.Format("Read back from Revit: {0}. {1} changed, {2} already so. See readBack and "
            + "measured", held.Count == 0 ? "nothing asked" : string.Join(", ", held),
            applied, alreadyAsAsked)
        : string.Format("{0} setting(s) are NOT what was asked - see differs. {1} changed as asked",
            differs.Count, applied));
}
else
{
    findings.Add(refused);
}
