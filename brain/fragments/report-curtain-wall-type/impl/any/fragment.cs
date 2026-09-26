// NOT STANDALONE. Assumes `doc` and `curtainWallType` are in scope; leaves
// `gridsLaidOut`, `mullionPositionsFilled`, `layoutsDisagreeing`, `settings`,
// `wallsOfType`, `refused` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ===========================================================================
// EVERY SETTING IS READ BY THE ID REVIT GIVES IT ON A WALL TYPE - NEVER BY THE
// NAME THE PALETTE SHOWS, BECAUSE EVERY ONE OF THOSE NAMES IS WORN TWICE.
// ===========================================================================
//
// Layout, Spacing, Adjust for Mullion Size, Interior Type, Border 1 Type and
// Border 2 Type each appear under Vertical and again under Horizontal. A
// lookup by name returns one of the two with no rule for which, and a read
// that could be either is not a read (FRAGMENT-ISSUES 5b-203). The ids are
// the _VERT and _HORIZ ones; the _1/_2, _U/_V and _GRID1/_GRID2 ids with the
// same names belong to curtain systems and divided surfaces.
//
// THE LAYOUT IS A WHOLE NUMBER, reported twice: by the name SpacingRuleLayout
// gives that number - None 0, FixedDistance 1, FixedNumber 2, MaximumSpacing
// 3, MinimumSpacing 5, read off all eight releases' assemblies - and as Revit
// DISPLAYS it. SET_CURTAIN_WALL_GRID writes the enum's numbers on the
// assumption that this parameter uses them; a stored number Revit displays as
// a different layout is listed in `layoutsDisagreeing`, which is this read
// showing that assumption wrong without writing anything. Words this does not
// recognise - a Revit in another language - are printed as they are and
// judged neither way.
//
// A CURTAIN SYSTEM IS NOT READ HERE. Its type is a CurtainSystemType, not a
// wall type, and the add-in resolves `curtainWallType` among WALL types only,
// so a curtain system's name is refused before this code runs.

const double MillimetresPerFoot = 304.8;

var gridsLaidOut = 0;
var mullionPositionsFilled = 0;
var layoutsDisagreeing = new List<string>();
var settings = "";
var wallsOfType = 0;
var refused = "";
var findings = new List<string>();

// The two directions, in the order every array below uses.
var directions = new[] { "vertical", "horizontal" };
var layoutIds = new[] { BuiltInParameter.SPACING_LAYOUT_VERT, BuiltInParameter.SPACING_LAYOUT_HORIZ };
var spacingIds = new[] { BuiltInParameter.SPACING_LENGTH_VERT, BuiltInParameter.SPACING_LENGTH_HORIZ };
var adjustIds = new[] { BuiltInParameter.CURTAINGRID_ADJUST_BORDER_VERT, BuiltInParameter.CURTAINGRID_ADJUST_BORDER_HORIZ };
var mullionIds = new BuiltInParameter[,]
{
    { BuiltInParameter.AUTO_MULLION_INTERIOR_VERT, BuiltInParameter.AUTO_MULLION_BORDER1_VERT,
      BuiltInParameter.AUTO_MULLION_BORDER2_VERT },
    { BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ, BuiltInParameter.AUTO_MULLION_BORDER1_HORIZ,
      BuiltInParameter.AUTO_MULLION_BORDER2_HORIZ },
};
var mullionPositions = new[] { "interior", "border 1", "border 2" };

// The layouts: what Type Properties shows, lower-cased to compare, and the
// number SpacingRuleLayout gives each - cast from the enum, never typed.
var layoutShown = new[] { "None", "Fixed Distance", "Fixed Number", "Maximum Spacing", "Minimum Spacing" };
var layoutWords = new[] { "none", "fixed distance", "fixed number", "maximum spacing", "minimum spacing" };
var layoutValues = new[]
{
    (int)SpacingRuleLayout.None,
    (int)SpacingRuleLayout.FixedDistance,
    (int)SpacingRuleLayout.FixedNumber,
    (int)SpacingRuleLayout.MaximumSpacing,
    (int)SpacingRuleLayout.MinimumSpacing,
};

// Lower case, one space between words - for comparing Revit's displayed
// layout with this file's own list, and for nothing else.
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

var label = curtainWallType == null ? "" : fullName(curtainWallType);

if (curtainWallType == null)
{
    refused = "No wall type was named. Name the curtain wall type the way Revit writes it - "
        + "'Curtain Wall: Storefront' - and ask again";
}
else if (curtainWallType.Kind != WallKind.Curtain)
{
    var kind = curtainWallType.Kind == WallKind.Basic ? "a basic wall type"
        : curtainWallType.Kind == WallKind.Stacked ? "a stacked wall type"
        : "a wall type Revit does not class as a curtain wall";
    refused = string.Format(
        "'{0}' is {1}, not a curtain wall type - it has no curtain grid or mullions to read. "
        + "REPORT_COMPOUND_STRUCTURE reads a layered wall's build-up", label, kind);
}
else
{
    // THE REACH - the same count the two curtain writers put first, so a
    // modeller deciding whether to change the type sees what it would touch.
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        var wall = element as Wall;
        if (wall == null) continue;
        ElementId typeId = null;
        try { typeId = wall.GetTypeId(); } catch { continue; }
        if (typeId != null && typeId.Equals(curtainWallType.Id)) wallsOfType++;
    }

    var parts = new List<string>();
    for (var d = 0; d < 2; d++)
    {
        // LAYOUT - the number, the enum's name for it, and Revit's display.
        var layout = curtainWallType.get_Parameter(layoutIds[d]);
        string layoutText;
        var value = int.MinValue;
        if (layout == null || layout.StorageType != StorageType.Integer)
        {
            layoutText = "no Layout this reads on the type";
        }
        else
        {
            value = layout.AsInteger();
            string shown = null;
            try { shown = layout.AsValueString(); } catch { shown = null; }
            var valueAt = Array.IndexOf(layoutValues, value);
            var shownAt = Array.IndexOf(layoutWords, plain(shown));

            layoutText = string.Format("{0} (Revit's value {1}, shown as {2})",
                valueAt >= 0 ? layoutShown[valueAt] : "a value SpacingRuleLayout does not have",
                value, string.IsNullOrEmpty(shown) ? "nothing" : "'" + shown + "'");

            if (shownAt >= 0 && shownAt != valueAt)
                layoutsDisagreeing.Add(string.Format(
                    "{0} layout: Revit holds {1}, which SpacingRuleLayout calls {2}, and shows it as "
                    + "'{3}'", directions[d], value,
                    valueAt >= 0 ? layoutShown[valueAt] : "nothing", shown));

            if (valueAt >= 0 && value != (int)SpacingRuleLayout.None) gridsLaidOut++;
            if (value == (int)SpacingRuleLayout.FixedNumber)
                layoutText += " - the count is each wall's own Number, not read here";
        }

        // SPACING, in millimetres - and said to do nothing where it does nothing.
        var spacing = curtainWallType.get_Parameter(spacingIds[d]);
        var spaced = value == (int)SpacingRuleLayout.FixedDistance
            || value == (int)SpacingRuleLayout.MaximumSpacing
            || value == (int)SpacingRuleLayout.MinimumSpacing;
        var spacingText = spacing == null || spacing.StorageType != StorageType.Double
            ? "no Spacing this reads on the type"
            : string.Format("{0:F1} mm{1}", spacing.AsDouble() * MillimetresPerFoot,
                spaced ? "" : " (not used under this layout)");

        // ADJUST FOR MULLION SIZE - a yes/no, read as Revit displays it when it
        // is not held as a whole number.
        var adjust = curtainWallType.get_Parameter(adjustIds[d]);
        string adjustText;
        if (adjust == null)
            adjustText = "not on the type";
        else if (adjust.StorageType == StorageType.Integer)
            adjustText = adjust.AsInteger() != 0 ? "yes" : "no";
        else
        {
            string shownAdjust = null;
            try { shownAdjust = adjust.AsValueString(); } catch { shownAdjust = null; }
            adjustText = string.IsNullOrEmpty(shownAdjust) ? "(not shown)" : "'" + shownAdjust + "'";
        }

        // THE THREE MULLION POSITIONS of this direction.
        var mullionTexts = new List<string>();
        for (var m = 0; m < 3; m++)
        {
            var position = curtainWallType.get_Parameter(mullionIds[d, m]);
            string held;
            if (position == null || position.StorageType != StorageType.ElementId)
            {
                held = "not on the type";
            }
            else
            {
                var id = position.AsElementId();
                if (id == null || id == ElementId.InvalidElementId)
                {
                    held = "none";
                }
                else
                {
                    var type = doc.GetElement(id) as ElementType;
                    held = type == null ? "(a type no longer in the model)" : "'" + fullName(type) + "'";
                    mullionPositionsFilled++;
                }
            }
            mullionTexts.Add(mullionPositions[m] + " " + held);
        }

        parts.Add(string.Format(
            "{0} grid: layout {1}; spacing {2}; adjust for mullion size {3}. {0} mullions: {4}",
            directions[d], layoutText, spacingText, adjustText, string.Join(", ", mullionTexts)));
    }
    settings = string.Join(" | ", parts);

    findings.Add(string.Format(
        "'{0}' is used by {1} wall{2} in this model - a change to any of these settings changes {3}",
        label, wallsOfType, wallsOfType == 1 ? "" : "s",
        wallsOfType == 1 ? "it" : wallsOfType == 0 ? "no placed wall" : "every one of them"));
    findings.Add(string.Format(
        "{0} of 2 directions laid out with grid lines, {1} of 6 mullion positions filled. See "
        + "settings", gridsLaidOut, mullionPositionsFilled));
    if (layoutsDisagreeing.Count > 0)
        findings.Add(string.Format(
            "{0} layout(s) are shown by Revit as a different layout from the number stored - "
            + "SET_CURTAIN_WALL_GRID's table of layout numbers would be wrong here. See "
            + "layoutsDisagreeing", layoutsDisagreeing.Count));
}

if (refused.Length > 0) findings.Add(refused);
