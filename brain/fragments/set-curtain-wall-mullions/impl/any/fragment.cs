// NOT STANDALONE. Assumes `doc`, `curtainWallType` and `mullions` are in scope;
// leaves `applied`, `alreadyAsAsked`, `differs`, `wallsOfType`,
// `systemsOfType`, `readBack`, `measured`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). No length is read or written.
//
// ===========================================================================
// VERSION 2 (2026-10-29): A CURTAIN SYSTEM TYPE AS WELL AS A CURTAIN WALL TYPE.
// ===========================================================================
//
// `curtainWallType` is the type's NAME, resolved here - among wall types and
// curtain system types together, by the add-in's own rule (D-54): the type
// name exactly, capitals included, or "Family: Type" / "Family : Type". Until
// version 2 the add-in resolved it among wall types and refused a curtain
// system type's name before this ran; it has no binder for a curtain system
// type, so the name is matched here, as CREATE_CURTAIN_SYSTEM matches its
// own. A bare name a wall type and a curtain system type share is refused
// naming both, never chosen from (D-54 s3).
//
// A WALL TYPE IS VERSION 1 TO THE WORD - the same ids, positions, refusals,
// read-back, measurement and findings. Everything that says "system" below is
// the curtain system type's path, and only it.
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
// the three _HORIZ.
//
// A CURTAIN SYSTEM TYPE'S ARE GRID 1 MULLIONS AND GRID 2 MULLIONS, and which
// way each grid runs is the face's, not the type's - so its positions are
// "grid 1 interior", "grid 1 border 1", "grid 1 border 2" and the three
// "grid 2", and a "vertical" or "horizontal" position on one is refused. Its
// ids are taken to be AUTO_MULLION_INTERIOR_GRID1, _BORDER1_GRID1,
// _BORDER2_GRID1 and the three _GRID2. THAT IS AN ASSUMPTION: the API
// reference of all eight releases lists them as "Interior Type", "Border 1
// Type" and "Border 2 Type" - exactly as it lists the _VERT/_HORIZ ones - and
// says nothing of which element carries which. So a curtain system type
// carrying none of them is refused before anything is written, naming the ids
// it does carry; and a position written by them that does not read back as
// asked THROWS - nothing is kept. The proof (Group JP) settles it.
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
var systemsOfType = 0;
var readBack = "";
var measured = "";
var refused = "";
var findings = new List<string>();

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

// ---------------------------------------------------------------------------
// THE TYPE, BY NAME - among wall types and curtain system types, by the
// add-in's own rule (D-54). Two found is refused, never chosen from.
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
var oneUser = isSystem ? "curtain system" : "wall";

// The six positions, in the order every array below uses: a wall type's three
// vertical then three horizontal, or a curtain system type's three of Grid 1
// then three of Grid 2.
var positionWords = isSystem
    ? new[]
    {
        "grid 1 interior", "grid 1 border 1", "grid 1 border 2",
        "grid 2 interior", "grid 2 border 1", "grid 2 border 2",
    }
    : new[]
    {
        "vertical interior", "vertical border 1", "vertical border 2",
        "horizontal interior", "horizontal border 1", "horizontal border 2",
    };
var positionIds = isSystem
    ? new[]
    {
        BuiltInParameter.AUTO_MULLION_INTERIOR_GRID1, BuiltInParameter.AUTO_MULLION_BORDER1_GRID1,
        BuiltInParameter.AUTO_MULLION_BORDER2_GRID1, BuiltInParameter.AUTO_MULLION_INTERIOR_GRID2,
        BuiltInParameter.AUTO_MULLION_BORDER1_GRID2, BuiltInParameter.AUTO_MULLION_BORDER2_GRID2,
    }
    : new[]
    {
        BuiltInParameter.AUTO_MULLION_INTERIOR_VERT, BuiltInParameter.AUTO_MULLION_BORDER1_VERT,
        BuiltInParameter.AUTO_MULLION_BORDER2_VERT, BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ,
        BuiltInParameter.AUTO_MULLION_BORDER1_HORIZ, BuiltInParameter.AUTO_MULLION_BORDER2_HORIZ,
    };

// WHICH MULLION IDS A CURTAIN SYSTEM TYPE CARRIES - the assumption above, read
// off the type rather than trusted.
var probeIds = new[]
{
    BuiltInParameter.AUTO_MULLION_INTERIOR_GRID1, BuiltInParameter.AUTO_MULLION_BORDER1_GRID1,
    BuiltInParameter.AUTO_MULLION_BORDER2_GRID1, BuiltInParameter.AUTO_MULLION_INTERIOR_GRID2,
    BuiltInParameter.AUTO_MULLION_BORDER1_GRID2, BuiltInParameter.AUTO_MULLION_BORDER2_GRID2,
    BuiltInParameter.AUTO_MULLION_INTERIOR_VERT, BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ,
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
    return string.Format("Of Revit's mullion ids, '{0}' carries: {1}; not: {2}", label,
        carried.Count == 0 ? "none" : string.Join(", ", carried),
        notCarried.Count == 0 ? "none" : string.Join(", ", notCarried));
};

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
        "'{0}' is {1}, not a curtain wall type - it has no mullions to set. Name a curtain wall "
        + "type, 'Curtain Wall: Storefront' for one. Nothing was changed", label, kind);
}
else if (mullions == null || mullions.Count == 0)
{
    refused = isSystem
        ? "Nothing was named to change. Name each position and its mullion type - "
            + "'grid 1 interior=Rectangular Mullion: 50 x 150mm; grid 2 border 2=none'. "
            + "Nothing was changed"
        : "Nothing was named to change. Name each position and its mullion type - "
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
        if (at < 0 && isSystem && (key.StartsWith("vertical") || key.StartsWith("horizontal")))
        {
            refused = string.Format(
                "'{0}' is a curtain WALL type's position, and '{1}' is a curtain system type. Its mullions "
                + "are Grid 1 Mullions and Grid 2 Mullions, and which way each grid runs is set by the face "
                + "it lies on, not by the type - so this does not call either vertical or horizontal. Name "
                + "one or more of: {2}. Nothing was changed", pair.Key, label, string.Join(", ", positionWords));
            break;
        }
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
        positionParameter[p] = chosen.get_Parameter(positionIds[p]);
        if (askedId[p] == null) continue;

        var parameter = positionParameter[p];
        if (parameter == null)
            refused = string.Format("'{0}' has no {1} mullion setting - Revit gives this type none "
                + "to set. {2}Nothing was changed", label, positionWords[p],
                isSystem ? idsCarried() + ". " : "");
        else if (parameter.StorageType != StorageType.ElementId)
            refused = string.Format("'{0}' keeps its {1} mullion in a form this does not write. "
                + "Nothing was changed", label, positionWords[p]);
        else if (parameter.IsReadOnly)
            refused = isSystem
                ? string.Format("Revit keeps the {1} mullion of '{0}' read-only and gives no reason for "
                    + "it - look at it in Type Properties, where Revit greys out what it will not let "
                    + "change. Nothing was changed", label, positionWords[p])
                : string.Format("Revit will not let the {1} mullion of '{0}' be changed - it may "
                    + "be held by another user in a shared model. Nothing was changed", label, positionWords[p]);
    }
}

if (refused.Length == 0)
{
    // THE REACH, COUNTED BEFORE ANYTHING IS WRITTEN.
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

    // ONE TO MEASURE: the first whose grid can be read, named by id.
    Element sample = null;
    foreach (var each in isSystem ? systems.Cast<Element>() : walls.Cast<Element>())
    {
        if (gridsOf(each).Count > 0) { sample = each; break; }
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

    // GRID LINES, vertical and horizontal by the way each runs - the lines
    // interior mullions sit on - and, for a curtain system, Revit's U and V
    // sets. [0] vertical, [1] horizontal, [2] U set, [3] V set.
    Func<List<CurtainGrid>, int[]> linesOn = cellsList =>
    {
        var counts = new int[4];
        foreach (var cells in cellsList)
        {
            ICollection<ElementId> uIds = null, vIds = null;
            try { uIds = cells.GetUGridLineIds(); vIds = cells.GetVGridLineIds(); }
            catch { continue; }
            counts[2] += uIds.Count;
            counts[3] += vIds.Count;
            var all = new List<ElementId>();
            all.AddRange(uIds);
            all.AddRange(vIds);
            foreach (var id in all)
            {
                var line = doc.GetElement(id) as CurtainGridLine;
                Curve curve = null;
                try { curve = line == null ? null : line.FullCurve; } catch { curve = null; }
                var way = runs(curve);
                if (way == 0 || way == 1) counts[way]++;
            }
        }
        return counts;
    };

    Func<Element, string> lengthOf = element =>
    {
        var at = element.Location as LocationCurve;
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
    var mullionsBefore = sample == null ? "" : mullionsOn(gridsOf(sample));

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
                "Revit could not rebuild the {0} of '{1}' with these mullions - {2}. Nothing this "
                + "call did is kept", isSystem ? "curtain systems" : "walls", label, notRebuilt.Message));
        }
    }

    // READ BACK, off the type afresh - what Revit holds, never what was asked.
    var described = new List<string>();
    var held = new List<string>();
    for (var p = 0; p < 6; p++)
    {
        var parameter = chosen.get_Parameter(positionIds[p]);
        var now = parameter != null && parameter.StorageType == StorageType.ElementId
            ? parameter.AsElementId() : null;

        described.Add(string.Format("{0}: {1}{2}", positionWords[p], nameOf(now),
            askedId[p] == null
                ? " - not asked, left as it was"
                : string.Format(", was {0} - asked for {1}", nameOf(before[p]), askedName[p])));

        if (askedId[p] == null) continue;

        // A CURTAIN SYSTEM TYPE'S IDS ARE THE ASSUMPTION (see the top). A
        // position written by them that Revit does not then hold means the id
        // is not the one Type Properties shows - nothing this call did is kept.
        if (isSystem && wrote[p] && (now == null || !now.Equals(askedId[p])))
            throw new InvalidOperationException(string.Format(
                "'{0}' was given {1} for its {2} mullions by {3} and holds {4} after the rebuild - that "
                + "id may not be the setting Type Properties shows under {5}, so nothing this call did "
                + "is kept. {6}", label, askedName[p], positionWords[p], positionIds[p], nameOf(now),
                p < 3 ? "Grid 1 Mullions" : "Grid 2 Mullions", idsCarried()));

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
    readBack = string.Join(" | ", described) + (isSystem ? " | " + idsCarried() : "");

    // WHAT IT MADE, MEASURED ON A PLACED ONE.
    var bare = new List<string>();
    if (sample == null)
    {
        measured = isSystem
            ? "No curtain system of this type is placed in the model, so nothing was measured. The next "
                + "curtain system made with it shows these mullions"
            : "No wall of this type is placed in the model, so nothing was measured. The next "
                + "wall drawn with it shows these mullions";
    }
    else if (isSystem)
    {
        var grids = gridsOf(sample);
        var lines = linesOn(grids);
        measured = string.Format(
            "curtain system {0}, on {1} face(s), with {2} vertical and {3} horizontal grid line(s) - "
            + "Revit's U set {4}, V set {5}. Mullions before: {6}. Now: {7}", sample.Id, grids.Count,
            lines[0], lines[1], lines[2], lines[3], mullionsBefore, mullionsOn(grids));

        // AN INTERIOR TYPE WITH NO LINES SHOWS NOWHERE. Which of Grid 1 and
        // Grid 2 is Revit's U set and which its V set is not known here, so
        // this is said only when the curtain system has no grid line at all.
        if (lines[2] + lines[3] == 0
            && ((askedId[0] != null && askedId[0] != ElementId.InvalidElementId)
                || (askedId[3] != null && askedId[3] != ElementId.InvalidElementId)))
            bare.Add(string.Format(
                "Curtain system {0} has no grid line on any face, so an interior mullion type shows on "
                + "none of it - the type holds it all the same. SET_CURTAIN_WALL_GRID lays the lines out, "
                + "on a curtain system type too", sample.Id));
    }
    else
    {
        var lines = linesOn(gridsOf(sample));
        measured = string.Format(
            "wall {0}, {1}, with {2} vertical and {3} horizontal grid line(s). Mullions before: {4}. "
            + "Now: {5}", sample.Id, lengthOf(sample), lines[0], lines[1], mullionsBefore,
            mullionsOn(gridsOf(sample)));

        // AN INTERIOR TYPE ON A DIRECTION WITH NO LINES SHOWS NOWHERE. Not a
        // failure - the type holds it - but the modeller should not have to
        // work out why nothing appeared.
        var ways = new List<string>();
        if (askedId[0] != null && askedId[0] != ElementId.InvalidElementId && lines[0] == 0)
            ways.Add("vertical");
        if (askedId[3] != null && askedId[3] != ElementId.InvalidElementId && lines[1] == 0)
            ways.Add("horizontal");
        foreach (var way in ways)
            bare.Add(string.Format(
                "The {0} grid puts no lines on wall {1}, so its interior mullion type shows on none of "
                + "it - the type holds it all the same. SET_CURTAIN_WALL_GRID lays the lines out",
                way, sample.Id));
    }

    // THE REACH FIRST, then what Revit holds.
    if (applied == 0 && differs.Count == 0)
    {
        findings.Add(string.Format(
            "Nothing on '{0}' needed changing - every position asked for already held it. It is "
            + "used by {1} {2}(s) in this model", label, usersOfType, oneUser));
    }
    else if (isSystem)
    {
        findings.Add(usersOfType == 0
            ? string.Format("'{0}' is used by no curtain system in this model, so nothing placed changed. "
                + "The next curtain system made with it takes these mullions", label)
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
    foreach (var line in bare) findings.Add(line);
}
else
{
    findings.Add(refused);
}
