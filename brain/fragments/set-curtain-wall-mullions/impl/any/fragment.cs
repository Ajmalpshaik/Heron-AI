// NOT STANDALONE. Assumes `doc`, `curtainWallType` and `mullions` are in scope;
// leaves `applied`, `alreadyAsAsked`, `differs`, `wallsOfType`, `readBack`,
// `measured`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). No length is read or written.
//
// ===========================================================================
// SIX MULLION POSITIONS, AND EACH NAME IS WORN BY TWO OF THEM.
// ===========================================================================
//
// Type Properties shows Interior Type, Border 1 Type and Border 2 Type under
// Vertical Mullions and again under Horizontal Mullions. A lookup by the name
// the palette shows returns one of the two with no rule for which - so
// WRITE_ELEMENT_PARAMETERS refuses such a name (FRAGMENT-ISSUES 5b-203), and it
// refuses these a second time because each one holds a mullion TYPE, an
// element reference. Here each position is reached by the id Revit gives it on
// a WALL type: AUTO_MULLION_INTERIOR_VERT, _BORDER1_VERT, _BORDER2_VERT and
// the three _HORIZ. The _GRID1 and _GRID2 ids with the same names belong to
// curtain systems, not to a wall type.
//
// A NAME RESOLVES TO EXACTLY ONE MULLION TYPE IN THIS MODEL, OR THE WHOLE
// REQUEST IS REFUSED. Written the way the add-in resolves every type (D-54):
// the bare type name when no other mullion type shares it, or the family and
// the type - "Rectangular Mullion: 50 x 150mm", with or without a space before
// the colon, since Revit's own lists write it both ways. Compared exactly,
// capitals included. None found is refused WITH THE NAMES THE MODEL HAS; two
// found is refused naming both, never chosen from (D-54 s3).
//
// "none" TAKES THE MULLIONS OFF A POSITION - Revit's empty reference - and it
// is the only word that does. A position left out of the request keeps what it
// has.
//
// NOTHING IS LOADED. A mullion family that is not in the model is refused with
// the ones that are; loading a family brings its own materials and can
// overwrite the project's, which is the modeller's call and not this one's.
//
// NOTHING IS WRITTEN UNTIL THE WHOLE REQUEST IS SETTLED. Every refusal is made
// before the first write, so a refused request changes nothing. A write Revit
// will not take, or walls it cannot rebuild, throws: the executor rolls the
// whole call back.

var applied = 0;
var alreadyAsAsked = 0;
var differs = new List<string>();
var wallsOfType = 0;
var readBack = "";
var measured = "";
var refused = "";
var findings = new List<string>();

// The six positions, in the order every array below uses: the three vertical,
// then the three horizontal.
var positionWords = new[]
{
    "vertical interior", "vertical border 1", "vertical border 2",
    "horizontal interior", "horizontal border 1", "horizontal border 2",
};
var positionIds = new[]
{
    BuiltInParameter.AUTO_MULLION_INTERIOR_VERT, BuiltInParameter.AUTO_MULLION_BORDER1_VERT,
    BuiltInParameter.AUTO_MULLION_BORDER2_VERT, BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ,
    BuiltInParameter.AUTO_MULLION_BORDER1_HORIZ, BuiltInParameter.AUTO_MULLION_BORDER2_HORIZ,
};

// Lower case, one space between words. For THIS file's words - the position
// names - only; a mullion type's name is the model's and is compared exactly.
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

// What a position holds, in words: the mullion type, or none.
Func<ElementId, string> nameOf = id =>
{
    if (id == null) return "(not read)";
    if (id == ElementId.InvalidElementId) return "none";
    var type = doc.GetElement(id) as ElementType;
    return type == null ? "(a type no longer in the model)" : "'" + fullName(type) + "'";
};

var askedId = new ElementId[6];
var askedName = new string[6];
var positionParameter = new Parameter[6];

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
        "'{0}' is {1}, not a curtain wall type - it has no mullions to set. Name a curtain wall "
        + "type, 'Curtain Wall: Storefront' for one. Nothing was changed", label, kind);
}
else if (mullions == null || mullions.Count == 0)
{
    refused = "Nothing was named to change. Name each position and its mullion type - "
        + "'vertical interior=Rectangular Mullion: 50 x 150mm; horizontal border 2=none'. "
        + "Nothing was changed";
}
else
{
    // EVERY MULLION TYPE IN THE MODEL, walked as element types and tested as
    // .NET types - the add-in's own way of finding a type by class.
    var known = new List<MullionType>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType())
    {
        var candidate = element as MullionType;
        if (candidate != null) known.Add(candidate);
    }

    var spellings = known.Select(t => fullName(t)).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();

    // THE REQUEST, READ WHOLE BEFORE ANYTHING IS TOUCHED.
    var seen = new List<string>();
    foreach (var pair in mullions)
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

        var at = Array.IndexOf(positionWords, key);
        if (at < 0)
        {
            refused = string.Format(
                "'{0}' is not a mullion position. Name one or more of: {1}. Nothing was changed",
                pair.Key, string.Join(", ", positionWords));
            break;
        }

        if (value.Length == 0)
        {
            refused = string.Format(
                "'{0}' was given no mullion type. Name one, or say none to take the mullions off "
                + "that position. Nothing was changed", positionWords[at]);
            break;
        }

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
        {
            askedId[at] = ElementId.InvalidElementId;
            askedName[at] = "none";
            continue;
        }

        var hits = new List<MullionType>();
        foreach (var candidate in known)
        {
            string name = "", family = "";
            try { name = candidate.Name; family = candidate.FamilyName; } catch { continue; }
            if (string.Equals(name, value, StringComparison.Ordinal)
                || (!string.IsNullOrEmpty(family)
                    && (string.Equals(family + ": " + name, value, StringComparison.Ordinal)
                        || string.Equals(family + " : " + name, value, StringComparison.Ordinal))))
                hits.Add(candidate);
        }

        if (hits.Count == 0)
        {
            refused = known.Count == 0
                ? string.Format(
                    "This model has no mullion types at all, so '{0}' cannot be put on {1}. Load a "
                    + "mullion family first - Insert > Load Family; Heron does not load families. "
                    + "Nothing was changed", value, positionWords[at])
                : string.Format(
                    "No mullion type called '{0}' in this model - capitals count. The mullion types it "
                    + "has are: {1}. A mullion family that is not in the model has to be loaded first "
                    + "- Insert > Load Family; Heron does not load families. Nothing was changed",
                    value, string.Join(", ", spellings));
            break;
        }

        if (hits.Count > 1)
        {
            refused = string.Format(
                "{0} mullion types are called '{1}', so the name does not say which is meant. Say "
                + "which: {2}. Nothing was changed", hits.Count, value,
                string.Join(", or ", hits.Select(t => fullName(t)).Distinct()));
            break;
        }

        askedId[at] = hits[0].Id;
        askedName[at] = "'" + fullName(hits[0]) + "'";
    }

    // THE TYPE'S OWN POSITIONS, BY ID, and whether each asked-for one can be
    // written at all.
    for (var p = 0; p < 6 && refused.Length == 0; p++)
    {
        positionParameter[p] = curtainWallType.get_Parameter(positionIds[p]);
        if (askedId[p] == null) continue;

        var parameter = positionParameter[p];
        if (parameter == null)
            refused = string.Format("'{0}' has no {1} mullion setting - Revit gives this type none "
                + "to set. Nothing was changed", label, positionWords[p]);
        else if (parameter.StorageType != StorageType.ElementId)
            refused = string.Format("'{0}' keeps its {1} mullion in a form this does not write. "
                + "Nothing was changed", label, positionWords[p]);
        else if (parameter.IsReadOnly)
            refused = string.Format("Revit will not let the {1} mullion of '{0}' be changed - it may "
                + "be held by another user in a shared model. Nothing was changed", label, positionWords[p]);
    }
}

if (refused.Length == 0)
{
    // THE REACH, COUNTED BEFORE ANYTHING IS WRITTEN.
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

    // ONE WALL TO MEASURE: the first whose grid can be read, named by id.
    Wall sample = null;
    foreach (var wall in walls)
    {
        CurtainGrid cells = null;
        try { cells = wall.CurtainGrid; } catch { cells = null; }
        if (cells != null) { sample = wall; break; }
    }

    // WHICH WAY A CURVE RUNS: 0 vertical, 1 horizontal, 2 at an angle, -1 not
    // readable - against Revit's own angle tolerance. Never by U and V, which
    // the API reference does not tie to vertical or horizontal on a wall.
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

    // A WALL'S GRID LINES, vertical and horizontal by the way each runs - the
    // lines interior mullions sit on. [0] vertical, [1] horizontal.
    Func<Wall, int[]> linesOn = wall =>
    {
        var counts = new int[2];
        CurtainGrid cells = null;
        try { cells = wall.CurtainGrid; } catch { cells = null; }
        if (cells == null) return counts;
        var all = new List<ElementId>();
        try { all.AddRange(cells.GetUGridLineIds()); all.AddRange(cells.GetVGridLineIds()); }
        catch { return counts; }
        foreach (var id in all)
        {
            var line = doc.GetElement(id) as CurtainGridLine;
            Curve curve = null;
            try { curve = line == null ? null : line.FullCurve; } catch { curve = null; }
            var way = runs(curve);
            if (way == 0 || way == 1) counts[way]++;
        }
        return counts;
    };

    Func<Wall, string> lengthOf = wall =>
    {
        var at = wall.Location as LocationCurve;
        return at == null || at.Curve == null
            ? "its length not read"
            : string.Format("{0:F0} mm long", at.Curve.Length * 304.8);
    };

    // WHAT REVIT HOLDS BEFORE.
    var before = new ElementId[6];
    for (var p = 0; p < 6; p++)
    {
        var parameter = positionParameter[p];
        before[p] = parameter != null && parameter.StorageType == StorageType.ElementId
            ? parameter.AsElementId() : null;
    }
    var mullionsBefore = sample == null ? "" : mullionsOn(sample);

    // THE WRITE. Only what was asked, and only where it differs.
    var wrote = new bool[6];
    for (var p = 0; p < 6; p++)
    {
        if (askedId[p] == null) continue;
        if (before[p] != null && before[p].Equals(askedId[p])) continue;

        bool took;
        try
        {
            took = positionParameter[p].Set(askedId[p]);
        }
        catch (Exception notTaken)
        {
            throw new InvalidOperationException(string.Format(
                "Revit would not put {0} on the {1} mullions of '{2}' - {3}. Nothing this call did "
                + "is kept", askedName[p], positionWords[p], label, notTaken.Message));
        }
        if (!took)
            throw new InvalidOperationException(string.Format(
                "Revit refused {0} on the {1} mullions of '{2}'. Nothing this call did is kept",
                askedName[p], positionWords[p], label));
        wrote[p] = true;
    }

    // THE REBUILD IS NOT SWALLOWED - see Regenerate's own reference: a failure
    // leaves a model that must not be read again.
    if (wrote.Any(w => w))
    {
        try
        {
            doc.Regenerate();
        }
        catch (Exception notRebuilt)
        {
            throw new InvalidOperationException(string.Format(
                "Revit could not rebuild the walls of '{0}' with these mullions - {1}. Nothing this "
                + "call did is kept", label, notRebuilt.Message));
        }
    }

    // READ BACK, off the type afresh - what Revit holds, never what was asked.
    var described = new List<string>();
    var held = new List<string>();
    for (var p = 0; p < 6; p++)
    {
        var parameter = curtainWallType.get_Parameter(positionIds[p]);
        var now = parameter != null && parameter.StorageType == StorageType.ElementId
            ? parameter.AsElementId() : null;

        described.Add(string.Format("{0}: {1}{2}", positionWords[p], nameOf(now),
            askedId[p] == null
                ? " - not asked, left as it was"
                : string.Format(", was {0} - asked for {1}", nameOf(before[p]), askedName[p])));

        if (askedId[p] == null) continue;
        if (now != null && now.Equals(askedId[p]))
        {
            if (wrote[p]) applied++; else alreadyAsAsked++;
            held.Add(positionWords[p] + " " + askedName[p]);
        }
        else
        {
            differs.Add(string.Format("{0}: asked for {1}, and Revit holds {2}",
                positionWords[p], askedName[p], nameOf(now)));
        }
    }
    readBack = string.Join(" | ", described);

    // WHAT IT MADE, MEASURED ON A WALL.
    var bare = new List<string>();
    if (sample == null)
    {
        measured = "No wall of this type is placed in the model, so nothing was measured. The next "
            + "wall drawn with it shows these mullions";
    }
    else
    {
        var lines = linesOn(sample);
        measured = string.Format(
            "wall {0}, {1}, with {2} vertical and {3} horizontal grid line(s). Mullions before: {4}. "
            + "Now: {5}", sample.Id, lengthOf(sample), lines[0], lines[1], mullionsBefore,
            mullionsOn(sample));

        // AN INTERIOR TYPE ON A DIRECTION WITH NO LINES SHOWS NOWHERE. Not a
        // failure - the type holds it - but the modeller should not have to
        // work out why nothing appeared.
        if (askedId[0] != null && askedId[0] != ElementId.InvalidElementId && lines[0] == 0)
            bare.Add("vertical");
        if (askedId[3] != null && askedId[3] != ElementId.InvalidElementId && lines[1] == 0)
            bare.Add("horizontal");
    }

    // THE REACH FIRST, then what Revit holds.
    if (applied == 0 && differs.Count == 0)
    {
        findings.Add(string.Format(
            "Nothing on '{0}' needed changing - every position asked for already held it. It is "
            + "used by {1} wall(s) in this model", label, wallsOfType));
    }
    else
    {
        findings.Add(wallsOfType == 0
            ? string.Format("'{0}' is used by no wall in this model, so nothing placed changed. The "
                + "next wall drawn with it takes these mullions", label)
            : string.Format("'{0}' is used by {1} wall{2} in this model - {3} with the type. For "
                + "one wall alone, DUPLICATE_TYPE first and put that wall on the copy",
                label, wallsOfType, wallsOfType == 1 ? "" : "s",
                wallsOfType == 1 ? "it changed" : "every one of them changed"));
    }
    findings.Add(differs.Count == 0
        ? string.Format("Read back from Revit: {0}. {1} changed, {2} already so. See readBack and "
            + "measured", held.Count == 0 ? "nothing asked" : string.Join(", ", held),
            applied, alreadyAsAsked)
        : string.Format("{0} position(s) are NOT what was asked - see differs. {1} changed as asked",
            differs.Count, applied));
    foreach (var way in bare)
    {
        findings.Add(string.Format(
            "The {0} grid puts no lines on wall {1}, so its interior mullion type shows on none of "
            + "it - the type holds it all the same. SET_CURTAIN_WALL_GRID lays the lines out",
            way, sample.Id));
    }
}
else
{
    findings.Add(refused);
}
