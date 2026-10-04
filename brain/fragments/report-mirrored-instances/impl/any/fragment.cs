// NOT STANDALONE. Assumes `doc` and `categories` are in scope; leaves
// `findings`, `mirrored`, `handFlipped`, `facingFlipped` and `instancesChecked`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// MIRRORED AND FLIPPED ARE THREE DIFFERENT FACTS, AND THEY ARE KEPT APART.
// `Mirrored` is true when the instance was mirrored - its geometry is a
// reflection of the family as authored, so a left-hand door now draws as a
// right-hand one while its type still says left. `HandFlipped` and
// `FacingFlipped` are the family's own flip arrows: the family author allowed
// them, and a flipped door is NOT a mirrored door. One instance can carry any
// combination, so each flag is reported per element and none is folded into
// another.
//
// WHY IT MATTERS. A mirrored door or fixture schedules the wrong handing, and a
// facing flip leaves the door's From Room and To Room as they were
// (FRAGMENT-ISSUES row 5b-212) - so the plan looks right and the schedule does
// not, and nothing in Revit warns about either.
//
// FAMILY INSTANCES ONLY. Ducts, pipes, walls and other system elements have no
// such flags; a category given with no family instances in it is SAID, never
// shown as a reassuring zero.
//
// NO ElementId IS READ AS A NUMBER - ids are printed and carried as ids.

var findings = new List<string>();
var mirrored = new List<ElementId>();
var handFlipped = new List<ElementId>();
var facingFlipped = new List<ElementId>();
var instancesChecked = 0;

// How many element rows are written out. Every id is still carried in the three
// lists above and every count is complete; this only keeps the text readable.
var rowsShown = 200;

var categoryIds = new List<ElementId>();
var categoryNames = new List<string>();
if (categories != null)
    foreach (var category in categories)
    {
        if (category == null) continue;
        categoryIds.Add(category.Id);
        categoryNames.Add(category.Name);
    }

if (categoryIds.Count == 0)
{
    findings.Add("NOT CHECKED - no category was given, so nothing was looked at. That is not a "
        + "model with nothing mirrored");
}
else
{
    // Per category and per "Family: Type": instances, mirrored, hand flipped,
    // facing flipped. Insertion order kept so the report reads in the order
    // Revit returned things.
    var byCategory = new Dictionary<string, int[]>();
    var byType = new Dictionary<string, int[]>();
    var typeOrder = new List<string>();
    var rows = new List<string>();
    var rowsNotShown = 0;

    foreach (var name in categoryNames)
        if (!byCategory.ContainsKey(name)) byCategory[name] = new int[4];

    foreach (var element in new FilteredElementCollector(doc)
        .WherePasses(new ElementMulticategoryFilter(categoryIds))
        .WhereElementIsNotElementType()
        .OfClass(typeof(FamilyInstance)))
    {
        var instance = element as FamilyInstance;
        if (instance == null) continue;
        instancesChecked++;

        var categoryName = instance.Category == null ? "(no category)" : instance.Category.Name;
        var symbol = instance.Symbol;
        // "Family: Type" with no space before the colon - the form Heron writes
        // a type name in everywhere else.
        var typeName = symbol == null ? "(no type)"
            : (string.IsNullOrEmpty(symbol.FamilyName) ? symbol.Name
                : symbol.FamilyName + ": " + symbol.Name);

        var isMirrored = instance.Mirrored;
        var isHand = instance.HandFlipped;
        var isFacing = instance.FacingFlipped;

        int[] perCategory;
        if (!byCategory.TryGetValue(categoryName, out perCategory))
        {
            perCategory = new int[4];
            byCategory[categoryName] = perCategory;
        }
        int[] perType;
        var typeKey = categoryName + " | " + typeName;
        if (!byType.TryGetValue(typeKey, out perType))
        {
            perType = new int[4];
            byType[typeKey] = perType;
            typeOrder.Add(typeKey);
        }

        perCategory[0]++; perType[0]++;
        if (isMirrored) { perCategory[1]++; perType[1]++; mirrored.Add(instance.Id); }
        if (isHand) { perCategory[2]++; perType[2]++; handFlipped.Add(instance.Id); }
        if (isFacing) { perCategory[3]++; perType[3]++; facingFlipped.Add(instance.Id); }

        if (!isMirrored && !isHand && !isFacing) continue;

        var flags = new List<string>();
        if (isMirrored) flags.Add("MIRRORED");
        if (isHand) flags.Add("hand flipped");
        if (isFacing) flags.Add("facing flipped");

        if (rows.Count < rowsShown)
        {
            // A nested shared component turns with the family it sits in, so its
            // flags are usually its host's. Said, so it is not counted as a
            // second mistake.
            var parent = instance.SuperComponent;
            rows.Add("  id " + instance.Id + "  " + categoryName + "  '" + typeName + "'  "
                + string.Join(", ", flags.ToArray())
                + (parent != null ? "  (nested in id " + parent.Id + ")" : ""));
        }
        else rowsNotShown++;
    }

    findings.Add("BY CATEGORY - instances / MIRRORED / hand flipped / facing flipped");
    foreach (var pair in byCategory)
    {
        if (pair.Value[0] == 0)
        {
            findings.Add("  " + pair.Key + ": NO FAMILY INSTANCES - none placed, or a category "
                + "made of system elements (ducts, pipes, walls) that carry no mirror or flip flag. "
                + "Not a pass");
            continue;
        }
        findings.Add("  " + pair.Key + ": " + pair.Value[0] + " / " + pair.Value[1] + " / "
            + pair.Value[2] + " / " + pair.Value[3]);
    }

    findings.Add("");
    findings.Add("BY FAMILY: TYPE - only types with at least one mirrored or flipped instance");
    var typesListed = 0;
    foreach (var key in typeOrder)
    {
        var counts = byType[key];
        if (counts[1] + counts[2] + counts[3] == 0) continue;
        typesListed++;
        findings.Add("  " + key + ": " + counts[0] + " placed, " + counts[1] + " MIRRORED, "
            + counts[2] + " hand flipped, " + counts[3] + " facing flipped");
    }
    if (typesListed == 0) findings.Add("  none");

    findings.Add("");
    findings.Add("EACH ONE");
    if (rows.Count == 0) findings.Add("  none");
    findings.AddRange(rows);
    if (rowsNotShown > 0)
        findings.Add("  ... " + rowsNotShown + " more, not listed. Every count above is complete "
            + "and every id is in the lists handed on");

    findings.Insert(0, string.Format("{0} family instance(s) checked in {1}: {2} MIRRORED, {3} hand "
        + "flipped, {4} facing flipped. A flip uses the family's own arrows and is NOT a mirror; one "
        + "instance can be both, and each flag is listed per element. Read only - nothing was "
        + "changed. Model: {5}",
        instancesChecked, string.Join(", ", categoryNames.ToArray()), mirrored.Count,
        handFlipped.Count, facingFlipped.Count,
        string.IsNullOrEmpty(doc.Title) ? "(unsaved)" : doc.Title));
}
