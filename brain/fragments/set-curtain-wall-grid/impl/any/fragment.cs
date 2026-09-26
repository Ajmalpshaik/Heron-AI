// NOT STANDALONE. Assumes `doc`, `curtainWallType` and `grid` are in scope;
// leaves `applied`, `alreadyAsAsked`, `differs`, `wallsOfType`, `readBack`,
// `measured`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Revit keeps lengths in feet;
// the modeller's spacing is MILLIMETRES (D-71), turned into feet once, at the
// write, and back once, at the read.
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
// SPACING_LENGTH_VERT and _HORIZ. The _1/_2 and _U/_V ids with the same names
// belong to curtain systems and divided surfaces, not to a wall type.
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
// nobody chose. Where Revit actually keeps Number, Justification, Angle and
// Offset is MEASURED on every run and printed in `readBack`, so the first
// proof records it rather than this comment asserting it.
//
// U AND V ARE NOT READ AS VERTICAL AND HORIZONTAL. The API reference names a
// curtain grid's two families of lines U and V and never says which is
// vertical on a wall. So the lines on a placed wall are sorted by the way each
// one actually runs - its curve, against Revit's own angle tolerance - and the
// set each came from is printed beside the count. The proof records the answer.
//
// NOTHING IS WRITTEN UNTIL THE WHOLE REQUEST IS SETTLED. Every refusal is made
// before the first write, so a refused request changes nothing. A write Revit
// will not take, or walls it cannot rebuild, throws: the executor rolls the
// whole call back, because a type half-changed across every wall that uses it
// is worse than one left alone.

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
var readBack = "";
var measured = "";
var refused = "";
var findings = new List<string>();

// The two directions, in the order every array below uses.
var directions = new[] { "vertical", "horizontal" };
var layoutIds = new[] { BuiltInParameter.SPACING_LAYOUT_VERT, BuiltInParameter.SPACING_LAYOUT_HORIZ };
var spacingIds = new[] { BuiltInParameter.SPACING_LENGTH_VERT, BuiltInParameter.SPACING_LENGTH_HORIZ };

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

// The grid settings Revit is believed to keep on each WALL rather than on the
// type. Measured below, never relied on.
var perWallIds = new[]
{
    BuiltInParameter.SPACING_NUM_DIVISIONS_VERT, BuiltInParameter.SPACING_NUM_DIVISIONS_HORIZ,
    BuiltInParameter.SPACING_JUSTIFICATION_VERT, BuiltInParameter.SPACING_JUSTIFICATION_HORIZ,
    BuiltInParameter.CURTAINGRID_ANGLE_VERT, BuiltInParameter.CURTAINGRID_ANGLE_HORIZ,
    BuiltInParameter.CURTAINGRID_ORIGIN_VERT, BuiltInParameter.CURTAINGRID_ORIGIN_HORIZ,
};
var perWallNames = new[]
{
    "vertical Number", "horizontal Number", "vertical Justification", "horizontal Justification",
    "vertical Angle", "horizontal Angle", "vertical Offset", "horizontal Offset",
};

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

var label = curtainWallType == null ? "" : fullName(curtainWallType);

// Why a setting on the type cannot be written, or null when it can.
Func<Parameter, StorageType, string, string> cannotWrite = (parameter, storage, what) =>
    parameter == null
        ? string.Format("'{0}' has no {1} - Revit gives this type none to set", label, what)
    : parameter.StorageType != storage
        ? string.Format("'{0}' keeps its {1} in a form this does not write", label, what)
    : parameter.IsReadOnly
        ? string.Format("Revit will not let the {1} of '{0}' be changed - it may be held by "
            + "another user in a shared model", label, what)
    : null;

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

if (curtainWallType == null)
{
    refused = "No wall type was named. Name the curtain wall type the way Revit writes it - "
        + "'Curtain Wall: Storefront' - and ask again. Nothing was changed";
}
else if (curtainWallType.Kind != WallKind.Curtain)
{
    var kind = curtainWallType.Kind == WallKind.Basic ? "a basic wall type"
        : curtainWallType.Kind == WallKind.Stacked ? "a stacked wall type"
        : "a wall type Revit does not class as a curtain wall";
    refused = string.Format(
        "'{0}' is {1}, not a curtain wall type - it has no grid to lay out. Name a curtain wall "
        + "type, 'Curtain Wall: Storefront' for one. Nothing was changed", label, kind);
}
else if (grid == null || grid.Count == 0)
{
    refused = "Nothing was named to change. Name each setting and what it becomes - "
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
        if (key == "vertical layout") { direction = 0; isLayout = true; }
        else if (key == "horizontal layout") { direction = 1; isLayout = true; }
        else if (key == "vertical spacing") { direction = 0; isLayout = false; }
        else if (key == "horizontal spacing") { direction = 1; isLayout = false; }
        else
        {
            refused = string.Format(
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
                refused = string.Format(
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
    // written at all.
    for (var d = 0; d < 2 && refused.Length == 0; d++)
    {
        layoutParameter[d] = curtainWallType.get_Parameter(layoutIds[d]);
        spacingParameter[d] = curtainWallType.get_Parameter(spacingIds[d]);

        if (askedLayout[d] >= 0)
        {
            var why = cannotWrite(layoutParameter[d], StorageType.Integer, directions[d] + " Layout");
            if (why != null) refused = why + ". Nothing was changed";
        }
        if (refused.Length == 0 && !double.IsNaN(askedSpacingMm[d]))
        {
            var why = cannotWrite(spacingParameter[d], StorageType.Double, directions[d] + " Spacing");
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
        var spaced = willBe == (int)SpacingRuleLayout.FixedDistance
            || willBe == (int)SpacingRuleLayout.MaximumSpacing
            || willBe == (int)SpacingRuleLayout.MinimumSpacing;

        if (!double.IsNaN(askedSpacingMm[d]) && !spaced)
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
    // THE REACH, COUNTED BEFORE ANYTHING IS WRITTEN. Every wall of this type
    // takes the change - it goes first in the answer, not after it.
    var walls = new List<Wall>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        var wall = element as Wall;
        if (wall == null) continue;
        ElementId typeId = null;
        try { typeId = wall.GetTypeId(); } catch { continue; }
        if (typeId != null && typeId.Equals(curtainWallType.Id)) walls.Add(wall);
    }
    wallsOfType = walls.Count;

    // ONE WALL TO MEASURE: the first whose grid can be read. Which one does not
    // matter - the type is the same on all of them - and it is named by id.
    Wall sample = null;
    foreach (var wall in walls)
    {
        CurtainGrid cells = null;
        try { cells = wall.CurtainGrid; } catch { cells = null; }
        if (cells != null) { sample = wall; break; }
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

    // A WALL'S GRID LINES BY THE WAY EACH ONE RUNS, and by the set Revit
    // returned it in: [way 0-2, or 3 when unreadable, set 0 = U or 1 = V].
    Func<Wall, int[,]> countLines = wall =>
    {
        var counts = new int[4, 2];
        CurtainGrid cells = null;
        try { cells = wall.CurtainGrid; } catch { cells = null; }
        if (cells == null) return counts;
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
        return counts;
    };

    Func<int[,], int, string> oneWay = (counts, way) =>
        counts[way, 0] + counts[way, 1] == 0
            ? "0"
            : string.Format("{0} (Revit's U set {1}, V set {2})",
                counts[way, 0] + counts[way, 1], counts[way, 0], counts[way, 1]);

    // A WALL'S MULLIONS, by the way each runs and by type - what gets ordered.
    Func<Wall, string> mullionsOn = wall =>
    {
        CurtainGrid cells = null;
        try { cells = wall.CurtainGrid; } catch { cells = null; }
        if (cells == null) return "not readable";
        ICollection<ElementId> ids = null;
        try { ids = cells.GetMullionIds(); } catch { return "not readable"; }
        var tally = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
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
        return tally.Count == 0 ? "none" : string.Join(", ", tally.Select(p => p.Value + " x " + p.Key));
    };

    Func<Wall, string> lengthOf = wall =>
    {
        var at = wall.Location as LocationCurve;
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
        if (curtainWallType.get_Parameter(perWallIds[i]) != null) onType.Add(perWallNames[i]);
        if (sample != null && sample.get_Parameter(perWallIds[i]) != null) onWall.Add(perWallNames[i]);
    }
    var carriers = string.Format(
        "Number, Justification, Angle and Offset, both ways, measured - on the TYPE: {0}; {1}",
        onType.Count == 0 ? "none of them" : string.Join(", ", onType),
        sample == null
            ? "no wall of the type is placed to measure"
            : string.Format("on wall {0}: {1}", sample.Id,
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
    var linesBefore = sample == null ? null : countLines(sample);
    var mullionsBefore = sample == null ? "" : mullionsOn(sample);

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
                "Revit could not rebuild the walls of '{0}' with this grid - {1}. Nothing this call "
                + "did is kept", label, notRebuilt.Message));
        }
    }

    // READ BACK, off the type afresh - what Revit holds, never what was asked.
    var described = new List<string>();
    var held = new List<string>();
    for (var d = 0; d < 2; d++)
    {
        var layoutNow = curtainWallType.get_Parameter(layoutIds[d]);
        var spacingNow = curtainWallType.get_Parameter(spacingIds[d]);

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
    readBack = string.Join(" | ", described) + " | " + carriers;

    // WHAT IT MADE, MEASURED ON A WALL - read off Revit, never worked out from
    // the spacing.
    if (sample == null)
    {
        measured = "No wall of this type is placed in the model, so nothing was measured. The next "
            + "wall drawn with it shows this grid";
    }
    else
    {
        var linesAfter = countLines(sample);
        var unreadable = linesAfter[3, 0] + linesAfter[3, 1];
        measured = string.Format(
            "wall {0}, {1}: vertical grid lines {2} before, {3} now; horizontal {4} before, {5} now; "
            + "at an angle {6} before, {7} now{8}. Mullions before: {9}. Now: {10}",
            sample.Id, lengthOf(sample),
            oneWay(linesBefore, 0), oneWay(linesAfter, 0),
            oneWay(linesBefore, 1), oneWay(linesAfter, 1),
            linesBefore[2, 0] + linesBefore[2, 1], linesAfter[2, 0] + linesAfter[2, 1],
            unreadable == 0 ? "" : string.Format("; {0} line(s) whose direction could not be read", unreadable),
            mullionsBefore, mullionsOn(sample));
    }

    // THE REACH FIRST, then what Revit holds.
    if (applied == 0 && differs.Count == 0)
    {
        findings.Add(string.Format(
            "Nothing on '{0}' needed changing - every setting asked for was already so. It is used "
            + "by {1} wall(s) in this model", label, wallsOfType));
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
