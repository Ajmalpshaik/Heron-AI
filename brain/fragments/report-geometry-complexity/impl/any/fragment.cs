// NOT STANDALONE. Assumes `doc`, `elements`, `maxTypes` and `listTop` are in
// scope; leaves `findings`, `heaviestTypeName`, `typesMeasured`,
// `typesUnmeasured` and `totalTriangles` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TRIANGLES, NOT FILE SIZE. What slows Revit down is what it has to DRAW, and
// Face.Triangulate().NumTriangles is that number directly. A sleeve with a
// modelled thread can beat an air handling unit, and nothing in its name says so.
//
// MEASURED PER TYPE, RANKED ON TYPE x INSTANCES. Geometry belongs to the type,
// so 400 instances of a 12,000-triangle family is 4.8 million triangles from ONE
// library choice. The type's own count is not what hurts; the product is.
//
// ALL THREE DETAIL LEVELS, because the GAP is the actionable part. 40 coarse to
// 40,000 fine is a family behaving. The same big number at all three is a family
// with no detail-level control, paying full price in every view.
//
// A TYPE THAT COULD NOT BE MEASURED IS NAMED, NEVER SCORED ZERO. A zero would
// sort the heaviest thing in the model to the bottom and read as good news.
//
// THE COUNT IS A COMPARISON, NOT A SCORE. Revit tessellates on demand, so these
// numbers rank families against each other and mean nothing as absolutes.

var findings = new List<string>();
var heaviestTypeName = "";
var typesMeasured = 0;
var typesUnmeasured = 0;
long totalTriangles = 0L;

var instancesPerType = new Dictionary<ElementId, int>();
var typeOrder = new List<ElementId>();
var withoutType = 0;

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    var typeId = element.GetTypeId();
    if (typeId == null || typeId == ElementId.InvalidElementId)
    {
        withoutType++;
        continue;
    }

    if (!instancesPerType.ContainsKey(typeId))
    {
        instancesPerType[typeId] = 0;
        typeOrder.Add(typeId);
    }
    instancesPerType[typeId] = instancesPerType[typeId] + 1;
}

// One Options per detail level, made once. Building them per element would be
// the single largest avoidable cost in this fragment.
var levels = new List<ViewDetailLevel>();
levels.Add(ViewDetailLevel.Coarse);
levels.Add(ViewDetailLevel.Medium);
levels.Add(ViewDetailLevel.Fine);

var levelNames = new List<string>();
levelNames.Add("coarse");
levelNames.Add("medium");
levelNames.Add("fine");

Func<Element, ViewDetailLevel, long> trianglesAt = (element, level) =>
{
    long triangles = 0L;

    var options = new Options();
    options.DetailLevel = level;
    options.ComputeReferences = false;
    options.IncludeNonVisibleObjects = false;

    var geometry = element.get_Geometry(options);
    if (geometry == null) return 0L;

    var pending = new Queue<GeometryElement>();
    pending.Enqueue(geometry);

    while (pending.Count > 0)
    {
        var current = pending.Dequeue();
        foreach (GeometryObject item in current)
        {
            if (item == null) continue;

            var nested = item as GeometryInstance;
            if (nested != null)
            {
                var inner = nested.GetInstanceGeometry();
                if (inner != null) pending.Enqueue(inner);
                continue;
            }

            var solid = item as Solid;
            if (solid == null) continue;

            foreach (Face face in solid.Faces)
            {
                if (face == null) continue;
                var mesh = face.Triangulate();
                if (mesh != null) triangles += mesh.NumTriangles;
            }
        }
    }

    return triangles;
};

var rows = new List<string>();
var rankByCost = new List<long>();
var rankByName = new List<string>();
long heaviestCost = -1L;
var cappedOut = 0;

foreach (var typeId in typeOrder)
{
    if (maxTypes > 0 && typesMeasured + typesUnmeasured >= maxTypes)
    {
        cappedOut++;
        continue;
    }

    var typeElement = doc.GetElement(typeId);
    if (typeElement == null)
    {
        typesUnmeasured++;
        rows.Add(string.Format("  (a type that is no longer in the model, id {0}) - NOT MEASURED",
            typeId));
        continue;
    }

    var typeName = typeElement.Name;
    var familyName = "";
    var asElementType = typeElement as ElementType;
    if (asElementType != null)
    {
        try { familyName = asElementType.FamilyName; } catch (Exception) { }
    }
    var shownName = string.IsNullOrEmpty(familyName) ? typeName : familyName + ": " + typeName;

    // Geometry is read off the TYPE where it has any, and off a placed instance
    // otherwise. A loadable family's type carries no geometry of its own - the
    // instance does - so reading only the type would report every loadable
    // family as empty, which is the failure this fallback exists for.
    Element measured = typeElement;
    long coarse = 0L;
    long medium = 0L;
    long fine = 0L;
    var readIt = false;

    for (var attempt = 0; attempt < 2 && !readIt; attempt++)
    {
        if (attempt == 1)
        {
            Element instance = null;
            foreach (var candidate in elements)
            {
                if (candidate == null || !candidate.IsValidObject) continue;
                if (candidate.GetTypeId() == typeId) { instance = candidate; break; }
            }
            if (instance == null) break;
            measured = instance;
        }

        try
        {
            coarse = trianglesAt(measured, levels[0]);
            medium = trianglesAt(measured, levels[1]);
            fine = trianglesAt(measured, levels[2]);
            readIt = coarse + medium + fine > 0L || attempt == 1;
        }
        catch (Exception)
        {
            readIt = false;
        }
    }

    if (!readIt)
    {
        typesUnmeasured++;
        rows.Add(string.Format("  {0} - NOT MEASURED. Its geometry did not come back; that is not the "
            + "same as having none, and it is NOT scored zero", shownName));
        continue;
    }

    typesMeasured++;
    var instanceCount = instancesPerType[typeId];
    var cost = fine * instanceCount;
    totalTriangles += cost;

    if (cost > heaviestCost)
    {
        heaviestCost = cost;
        heaviestTypeName = shownName;
    }

    var behaviour = fine > 0L && coarse * 4L < fine
        ? "has detail-level control - it costs little on a working plan"
        : "SAME WEIGHT AT EVERY DETAIL LEVEL - it pays full price in every view";

    rankByCost.Add(cost);
    rankByName.Add(string.Format("  {0,-52} {1,10} {2,10} {3,10} x{4,-6} = {5,12}   {6}",
        shownName.Length > 52 ? shownName.Substring(0, 52) : shownName,
        coarse, medium, fine, instanceCount, cost, behaviour));
}

// Sort the ranking without LINQ over a parallel pair - a plain selection pass
// keeps the two lists honestly in step and is trivial at these sizes.
var ordered = new List<string>();
var taken = new List<bool>();
for (var i = 0; i < rankByName.Count; i++) taken.Add(false);
while (ordered.Count < rankByName.Count && (listTop <= 0 || ordered.Count < listTop))
{
    var best = -1;
    for (var i = 0; i < rankByName.Count; i++)
    {
        if (taken[i]) continue;
        if (best < 0 || rankByCost[i] > rankByCost[best]) best = i;
    }
    if (best < 0) break;
    taken[best] = true;
    ordered.Add(rankByName[best]);
}

if (ordered.Count > 0)
{
    findings.Add(string.Format("  {0,-52} {1,10} {2,10} {3,10} {4,-7}   {5,12}",
        "family: type", "coarse", "medium", "fine", "instances", "fine x count"));
    foreach (var row in ordered) findings.Add(row);
}
foreach (var row in rows) findings.Add(row);

if (listTop > 0 && rankByName.Count > listTop)
{
    findings.Add(string.Format("  ... {0} more measured type(s) not listed - raise listTop to see them",
        rankByName.Count - listTop));
}
if (cappedOut > 0)
{
    findings.Add(string.Format("  {0} type(s) were NOT looked at at all, because maxTypes is {1}. The "
        + "ranking above is of what was measured, not of the model", cappedOut, maxTypes));
}
if (withoutType > 0)
{
    findings.Add(string.Format("  {0} element(s) have no type - a line, a group or a view does not. "
        + "That is not a fault and they carry no family geometry to measure", withoutType));
}

if (typesMeasured == 0)
{
    findings.Insert(0, "Nothing could be measured. That is not a model with no geometry - it is a "
        + "reading that failed, and the rows below say for which types");
}
else
{
    findings.Insert(0, string.Format("{0} type(s) measured{1}, {2} instance(s) between them, {3} "
        + "triangle(s) at fine detail in total. Heaviest by far: {4}. THESE NUMBERS COMPARE FAMILIES "
        + "WITH EACH OTHER and are not a score - Revit tessellates on demand, so an absolute figure "
        + "here means nothing on its own",
        typesMeasured,
        typesUnmeasured > 0
            ? string.Format(", {0} could NOT be measured and are named below", typesUnmeasured)
            : "",
        elements.Count, totalTriangles, heaviestTypeName));
}
