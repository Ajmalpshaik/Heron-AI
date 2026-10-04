// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `level`,
// `structuralType`, `topLevelName` and `topOffset` are in scope; leaves
// `placed`, `failed`, `tops`, `topWrong`, `topRefused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// WHY THIS IS NOT PLACE_FAMILY_INSTANCES. That one passes
// StructuralType.NonStructural as a literal, which is correct for the air
// terminals and sprinkler heads its purpose names and wrong for every column
// and footing. The structural type is an INPUT here, and that is the only
// material difference between the two files.
//
// A NON-STRUCTURAL COLUMN LOOKS IDENTICAL ON EVERY DRAWING and is counted
// differently by every schedule, so the wrong value is invisible until somebody
// prices it.
//
// VERSION 2: THE COLUMN'S TOP. Version 1 placed a column with a base level and
// nothing said about its top, so it stood at whatever height Revit gave it.
// `topLevelName` and `topOffset` (millimetres) are both BLANK for version 1's
// behaviour exactly - the top left as Revit placed it, which the reply says.
// Given, and only for structuralType "column":
//   1. THE LEVEL IS RESOLVED EXACTLY, by its whole name. No level of that
//      name, or two, and nothing is placed - never the nearest name.
//   2. A TOP AT OR BELOW THE BASE IS REFUSED BEFORE ANYTHING IS PLACED - the
//      top level's elevation, and that elevation plus the offset, against
//      the base level's.
//   3. SET on the built-ins FAMILY_TOP_LEVEL_PARAM and
//      FAMILY_TOP_LEVEL_OFFSET_PARAM, never a parameter found by name. A
//      column whose family has neither usable is removed again inside the
//      same undo and named in `topRefused`, rather than left at a height
//      nobody asked for.
//   4. READ BACK after a regenerate: the top level and offset the column now
//      carries, as an elevation in millimetres, beside what was asked in
//      `tops`, with the top of its own geometry as a second reading. More
//      than 1 mm out, or a different level, is named in `topWrong`.

var placed = new List<ElementId>();
var failed = 0;
var tops = new List<string>();
var topWrong = new List<string>();
var topRefused = new List<string>();
var findings = new List<string>();

var asked = (structuralType ?? "").Trim().ToLowerInvariant();
var wanted = StructuralType.NonStructural;
var understood = true;

if (asked == "column") wanted = StructuralType.Column;
else if (asked == "footing") wanted = StructuralType.Footing;
else if (asked == "beam") wanted = StructuralType.Beam;
else if (asked == "brace") wanted = StructuralType.Brace;
else if (asked == "nonstructural" || asked == "non-structural") wanted = StructuralType.NonStructural;
else understood = false;

// THE TOP ASKED FOR - read and judged before anything is placed.
var topName = (topLevelName ?? "").Trim();
var offsetText = (topOffset ?? "").Trim();
Level topLevel = null;
var offsetFeet = 0.0;
string topRefusal = null;

if (topName.Length == 0 && offsetText.Length > 0)
{
    topRefusal = "A top offset of " + offsetText + " mm was given with no top level, and which "
        + "level it is measured from is not guessed. Nothing was placed";
}
else if (topName.Length > 0)
{
    double offsetMm = 0;
    if (offsetText.Length > 0
        && !double.TryParse(offsetText, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out offsetMm))
    {
        topRefusal = "'" + offsetText + "' is not a top offset. Give it in millimetres - 300 or "
            + "-150 - or leave it blank for none. Nothing was placed";
    }
    else if (understood && wanted != StructuralType.Column)
    {
        topRefusal = "A top level is set only on columns, and this was asked as '"
            + structuralType + "'. Nothing was placed";
    }
    else if (level == null)
    {
        topRefusal = "No base level was given, so a top level cannot be judged against it. "
            + "Nothing was placed";
    }
    else
    {
        offsetFeet = offsetMm / 304.8;
        var matches = new List<Level>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
        {
            var candidate = element as Level;
            if (candidate != null && string.Equals(candidate.Name, topName, StringComparison.Ordinal))
                matches.Add(candidate);
        }

        if (matches.Count == 0)
            topRefusal = "No level is named '" + topName + "' exactly, so the top is not guessed. "
                + "Nothing was placed";
        else if (matches.Count > 1)
            topRefusal = matches.Count + " levels are named '" + topName + "', so which one is the "
                + "top is not guessed. Nothing was placed";
        else
        {
            topLevel = matches[0];
            if (topLevel.Id == level.Id || topLevel.ProjectElevation <= level.ProjectElevation)
            {
                topRefusal = string.Format("The top level '{0}' at {1:0} mm is not above the base "
                    + "level '{2}' at {3:0} mm. Nothing was placed", topLevel.Name,
                    topLevel.ProjectElevation * 304.8, level.Name, level.ProjectElevation * 304.8);
                topLevel = null;
            }
            else if (topLevel.ProjectElevation + offsetFeet <= level.ProjectElevation)
            {
                topRefusal = string.Format("'{0}' with a {1:0} mm offset puts the top at or below "
                    + "the base level '{2}'. Nothing was placed", topLevel.Name, offsetMm, level.Name);
                topLevel = null;
            }
        }
    }
}

if (!understood)
{
    findings.Add(string.Format(
        "'{0}' is not a structural type Revit knows. Say column, footing, beam, "
        + "brace or non-structural", structuralType ?? "(nothing)"));
}
else if (topRefusal != null)
{
    findings.Add(topRefusal);
}
else if (points == null || points.Count == 0)
{
    findings.Add("No points were given, so there is nothing to place");
}
else if (symbol == null)
{
    findings.Add("No family type was given");
}
else
{
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    for (var i = 0; i < points.Count; i++)
    {
        var point = points[i];
        if (point == null) { failed++; continue; }
        FamilyInstance made = null;
        try
        {
            made = doc.Create.NewFamilyInstance(point, symbol, level, wanted);
        }
        catch
        {
            made = null;
        }
        if (made == null) { failed++; continue; }

        var label = string.Format("point {0} ({1:0}, {2:0})", i + 1,
            point.X * 304.8, point.Y * 304.8);

        if (topLevel == null)
        {
            placed.Add(made.Id);
            continue;
        }

        // THE TOP - the built-ins only, never a parameter found by name.
        Parameter topParameter = null;
        Parameter offsetParameter = null;
        try
        {
            topParameter = made.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
            offsetParameter = made.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
        }
        catch { }

        if (topParameter == null || topParameter.IsReadOnly
            || topParameter.StorageType != StorageType.ElementId
            || offsetParameter == null || offsetParameter.IsReadOnly
            || offsetParameter.StorageType != StorageType.Double)
        {
            try { doc.Delete(made.Id); } catch { }
            topRefused.Add(label + " - its family has no settable Top Level and Top Offset, so it "
                + "was removed again rather than left at a top nobody asked for");
            continue;
        }

        var refusedWith = "";
        try
        {
            if (!topParameter.Set(topLevel.Id) || !offsetParameter.Set(offsetFeet))
                refusedWith = " - Revit returned false for the top level or the top offset";
            else
                doc.Regenerate();
        }
        catch (Exception failure)
        {
            refusedWith = " - Revit refused it: " + failure.Message;
        }

        // A TOP REVIT WOULD NOT TAKE IS THE SAME CASE AS NO TOP PARAMETER: the
        // column is removed again in the same undo, never kept at a top nobody
        // asked for (review of PR #410 - it used to be kept and only listed).
        if (refusedWith.Length > 0)
        {
            try { doc.Delete(made.Id); } catch { }
            topRefused.Add(label + refusedWith + ", so it was removed again rather than left at "
                + "a top nobody asked for");
            continue;
        }

        placed.Add(made.Id);

        // READ BACK - the level and offset it now carries, and its geometry.
        var askedMm = (topLevel.ProjectElevation + offsetFeet) * 304.8;
        Level readLevel = null;
        var readMm = double.NaN;
        try
        {
            var readTop = made.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
            var readOffset = made.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
            if (readTop != null) readLevel = doc.GetElement(readTop.AsElementId()) as Level;
            if (readLevel != null && readOffset != null)
                readMm = (readLevel.ProjectElevation + readOffset.AsDouble()) * 304.8;
        }
        catch { }

        var geometryTop = "";
        try
        {
            var box = made.get_BoundingBox(null);
            if (box != null) geometryTop = string.Format(", its geometry reaches {0:0} mm", box.Max.Z * 304.8);
        }
        catch { }

        var reads = double.IsNaN(readMm) ? "nothing"
            : string.Format("'{0}' + offset = {1:0} mm", readLevel.Name, readMm);
        tops.Add(string.Format("{0} - top asked '{1}' + {2:0} mm = {3:0} mm, reads {4}{5}",
            label, topLevel.Name, offsetFeet * 304.8, askedMm, reads, geometryTop));
        if (readLevel == null || readLevel.Id != topLevel.Id || double.IsNaN(readMm)
            || Math.Abs(readMm - askedMm) > 1.0)
            topWrong.Add(string.Format("{0} - top asked {1:0} mm on '{2}', reads {3}{4}",
                label, askedMm, topLevel.Name, reads, refusedWith));
    }

    findings.Add(string.Format(
        "{0} placed as {1} on '{2}', {3} point(s) could not take one",
        placed.Count, wanted, level == null ? "(no level)" : level.Name, failed));

    if (topLevel != null)
        findings.Add(string.Format(
            "Top asked for: '{0}' + {1:0} mm. Read back as asked on {2}, otherwise on {3}, "
            + "{4} removed because the family has no settable top",
            topLevel.Name, offsetFeet * 304.8, tops.Count - topWrong.Count, topWrong.Count,
            topRefused.Count));
    else
        findings.Add("No top level was asked for, so every top was left as Revit placed it");
}
