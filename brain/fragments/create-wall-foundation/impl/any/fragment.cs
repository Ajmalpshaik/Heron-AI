// NOT STANDALONE. Assumes `doc`, `elements` and `foundationType` are in scope;
// leaves `created`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, and
// the caller owns it. Each wall runs inside a SubTransaction of that
// transaction, so a wall Revit refuses is put back on its own and the rest
// still go.
//
// REVIT'S STRUCTURE > FOUNDATION > WALL. A wall foundation is a system element
// hosted by its wall - not a family, and not placed at a point - made by
// WallFoundation.Create(doc, typeId, wallId). It follows the wall's base.
//
// THE TYPE IS NEVER GUESSED (D-33). `foundationType` names a Wall Foundation
// type by its name or as "Family: Type". Left out it arrives as "list": nothing
// is made and the model's wall foundation types are named - even when there is
// only one, because which footing goes under a wall is the modeller's decision.
//
// ONE PER WALL. Every wall foundation already in the model is read first, by
// the wall it is hosted on (WallFoundation.WallId). A wall that has one is
// skipped and named, never given a second.
//
// NOTHING IS REFUSED ON A GUESS. Whether Revit hosts a wall foundation on a
// curtain wall or a stacked wall is not known here; each wall is tried, and a
// wall Revit refuses is named with Revit's own words and its wall kind.
//
// WHAT IS REPORTED IS READ BACK. After every wall is done the model is
// regenerated once and each foundation is looked at again: still in the model,
// hosted by the wall asked, of the type asked, with a bounding box. One that
// fails any of those is named and NOT counted as made.

var created = new List<ElementId>();
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was made.
string refused = null;
var findings = new List<string>();

var lines = new List<string>();
var skipped = new List<string>();
var problems = new List<string>();
var notWalls = new List<string>();

Func<Element, string> describe = element =>
{
    if (element == null) return "an element no longer in the model";
    var category = element.Category == null ? "element" : element.Category.Name;
    var name = "";
    try { name = element.Name ?? ""; } catch (Exception) { name = ""; }
    return name.Length > 0
        ? string.Format("{0} \"{1}\" (id {2})", category, name, element.Id)
        : string.Format("{0} (id {1})", category, element.Id);
};

Func<ElementId, string> levelName = id =>
{
    var level = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id);
    if (level == null) return "no level";
    try { return "\"" + (level.Name ?? "") + "\""; } catch (Exception) { return "a level"; }
};

Func<Wall, string> kindOf = wall =>
{
    WallType type = null;
    try { type = wall.WallType; } catch (Exception) { type = null; }
    if (type == null) return "a wall of unknown kind";
    if (type.Kind == WallKind.Curtain) return "a curtain wall";
    if (type.Kind == WallKind.Stacked) return "a stacked wall";
    if (wall.IsStackedWallMember) return "part of a stacked wall";
    return "a basic wall";
};

// ---- what was asked ---------------------------------------------------------

var said = (foundationType ?? "").Trim();
var listOnly = said.Length == 0 || string.Equals(said, "list", StringComparison.OrdinalIgnoreCase);

var allTypes = new List<Element>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(WallFoundationType)))
{
    if (element != null) allTypes.Add(element);
}
var typeNames = new List<string>();
foreach (var one in allTypes)
{
    string name = "", family = "";
    try { name = one.Name ?? ""; } catch (Exception) { }
    try { family = ((ElementType)one).FamilyName ?? ""; } catch (Exception) { }
    typeNames.Add(family.Length > 0 ? "\"" + family + ": " + name + "\"" : "\"" + name + "\"");
}
var typeChoices = typeNames.Count == 0
    ? "this model has no wall foundation type - load or duplicate one in Revit first"
    : "the model's wall foundation types are " + string.Join(", ", typeNames.ToArray());

ElementId useType = null;
string askedProblem = null;

if (listOnly)
{
    askedProblem = "No wall foundation type was named, so nothing was made - " + typeChoices
        + ". Say which one goes under these walls";
}
else
{
    var matches = new List<Element>();
    foreach (var one in allTypes)
    {
        string name = "", family = "";
        try { name = one.Name ?? ""; } catch (Exception) { }
        try { family = ((ElementType)one).FamilyName ?? ""; } catch (Exception) { }
        if (string.Equals(name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + ": " + name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + " : " + name, said, StringComparison.OrdinalIgnoreCase))
            matches.Add(one);
    }
    if (matches.Count == 1) useType = matches[0].Id;
    else if (matches.Count == 0)
        askedProblem = string.Format("No wall foundation type called \"{0}\" - {1}", said, typeChoices);
    else
        askedProblem = string.Format("{0} wall foundation types are called \"{1}\" - name it as "
            + "\"Family: Type\"; {2}", matches.Count, said, typeChoices);
}

// ---- the walls that already carry one -----------------------------------------

// Every wall foundation in the model, by the wall that hosts it. Ids are
// compared as ElementIds, never as numbers.
var hostedOn = new List<ElementId>();
var hostedBy = new List<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(WallFoundation)))
{
    var existing = element as WallFoundation;
    if (existing == null) continue;
    ElementId wallId = null;
    try { wallId = existing.WallId; } catch (Exception) { wallId = null; }
    if (wallId == null || wallId == ElementId.InvalidElementId) continue;
    hostedOn.Add(wallId);
    hostedBy.Add(existing.Id);
}

Func<ElementId, ElementId> foundationUnder = wallId =>
{
    for (var i = 0; i < hostedOn.Count; i++)
        if (hostedOn[i] == wallId) return hostedBy[i];
    return null;
};

// ---- which walls --------------------------------------------------------------

var walls = new List<Wall>();
var seen = new List<ElementId>();

if (askedProblem == null && elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (seen.Contains(element.Id)) continue;
        seen.Add(element.Id);

        var wall = element as Wall;
        if (wall == null)
        {
            notWalls.Add(describe(element) + (element is WallFoundation
                ? ": is itself a wall foundation - select the wall above it"
                : ": not a wall - a wall foundation goes only under a wall"));
            continue;
        }
        walls.Add(wall);
    }
}

// ---- one wall at a time -------------------------------------------------------

var made = new List<ElementId>();
var madeUnder = new List<Wall>();

foreach (var wall in walls)
{
    var wallName = describe(wall);
    var onLevel = levelName(wall.LevelId);

    var already = foundationUnder(wall.Id);
    if (already != null)
    {
        skipped.Add(string.Format("{0} on {1}: already has a wall foundation ({2}) - left as it "
            + "was, not doubled", wallName, onLevel, describe(doc.GetElement(already))));
        continue;
    }

    WallFoundation foundation = null;
    string failed = null;
    using (var attempt = new SubTransaction(doc))
    {
        try
        {
            attempt.Start();
            foundation = WallFoundation.Create(doc, useType, wall.Id);
            if (foundation == null)
            {
                attempt.RollBack();
                failed = string.Format("{0} on {1}, {2}: NOT made - Revit returned no foundation",
                    wallName, onLevel, kindOf(wall));
            }
            else
            {
                attempt.Commit();
            }
        }
        catch (Exception ex)
        {
            try { attempt.RollBack(); } catch (Exception) { }
            foundation = null;
            failed = string.Format("{0} on {1}, {2}: NOT made - Revit refused: {3}", wallName,
                onLevel, kindOf(wall), ex.Message);
        }
    }
    if (failed != null)
    {
        problems.Add(failed);
        continue;
    }

    made.Add(foundation.Id);
    madeUnder.Add(wall);
    // A second id in the selection naming the same wall must not get one too.
    hostedOn.Add(wall.Id);
    hostedBy.Add(foundation.Id);
}

// ---- read back ----------------------------------------------------------------

if (made.Count > 0)
{
    try { doc.Regenerate(); }
    catch (Exception ex) { findings.Add("Revit would not regenerate after the foundations were made: " + ex.Message); }
}

for (var i = 0; i < made.Count; i++)
{
    var wall = madeUnder[i];
    var wallName = describe(wall);
    var onLevel = levelName(wall.LevelId);
    var now = doc.GetElement(made[i]) as WallFoundation;

    if (now == null || !now.IsValidObject)
    {
        problems.Add(string.Format("{0} on {1}: Revit took the call but the foundation is NOT in the "
            + "model afterwards", wallName, onLevel));
        continue;
    }
    ElementId hostNow = null;
    try { hostNow = now.WallId; } catch (Exception) { hostNow = null; }
    if (hostNow == null || hostNow != wall.Id)
    {
        problems.Add(string.Format("{0} on {1}: a foundation ({2}) was made but reads back hosted by "
            + "another element - look at it in Revit", wallName, onLevel, now.Id));
        continue;
    }
    if (now.GetTypeId() != useType)
    {
        problems.Add(string.Format("{0} on {1}: a foundation ({2}) was made but reads back of another "
            + "type - look at it in Revit", wallName, onLevel, now.Id));
        continue;
    }
    BoundingBoxXYZ box = null;
    try { box = now.get_BoundingBox(null); } catch (Exception) { box = null; }
    if (box == null)
    {
        problems.Add(string.Format("{0} on {1}: a foundation ({2}) was made but has no extent in the "
            + "model - look at it in Revit", wallName, onLevel, now.Id));
        continue;
    }

    created.Add(now.Id);
    lines.Add(string.Format("{0} on {1}: wall foundation made ({2}), type {3}", wallName, onLevel,
        now.Id, "\"" + (doc.GetElement(useType) == null ? "" : doc.GetElement(useType).Name) + "\""));
}

// ---- the answer -----------------------------------------------------------------

var all = new List<string>(lines);
all.AddRange(skipped);
all.AddRange(problems);
all.AddRange(notWalls);
report = string.Join("\n", all.ToArray());

if (askedProblem == null)
    findings.Add(string.Format("{0} wall foundation(s) made, {1} wall(s) already had one, {2} wall(s) "
        + "could not take one, of {3} wall(s) selected", created.Count, skipped.Count, problems.Count,
        walls.Count));

foreach (var one in problems) findings.Add(one);
foreach (var one in notWalls) findings.Add(one);

if (created.Count > 0)
    findings.Add("Each foundation follows its wall's base and is hosted by it: moving or deleting the "
        + "wall moves or deletes its foundation. Nothing reinforces a wall foundation here yet");

if (created.Count == 0)
{
    if (askedProblem != null)
        refused = askedProblem;
    else if (elements == null || elements.Count == 0)
        refused = "Nothing was selected - select the walls to put a wall foundation under";
    else if (walls.Count == 0)
        refused = string.Format("None of the {0} selected element(s) is a wall, so no wall foundation "
            + "was made", elements.Count);
    else if (problems.Count == 0)
        refused = string.Format("All {0} selected wall(s) already have a wall foundation - nothing was "
            + "made, none was doubled", walls.Count);
    else
        refused = string.Format("No wall foundation was made: {0} wall(s) could not take one and {1} "
            + "already had one - the report names each and why", problems.Count, skipped.Count);
}
