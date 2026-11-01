// NOT STANDALONE. Assumes `doc` and `parameters` are in scope, and leaves
// `dashboardJson`, `elementTotal`, `sectionsNotRead` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// THE COMPANION'S MODEL DASHBOARD IN ONE READ (docs/40). What the open project
// holds, part by part - placed model elements by category, level and workset,
// the warnings grouped by what they say, views and sheets, families, group
// types, Revit links, CAD files, rooms and spaces - as one JSON string the
// brain (heron_model_dashboard.py) turns into tiles, charts and lists. Nothing
// here judges anything: every figure is a count of what the model holds.
//
// EVERY RULE HERE IS ONE THE LIBRARY ALREADY USES, so the dashboard and the
// fragment that answers one part of it agree:
//
//   placed model element   not a type, a MODEL category, not view-specific, and
//                          with an extent - COPY_ELEMENTS_BETWEEN_DOCUMENTS'
//                          "all": a material or a sketch plane is filed under a
//                          model category and has no extent. Not a Level, as
//                          REPORT_LEVEL_ELEVATIONS counts them
//   an element's level     SELECT_WITHOUT_LEVEL's four steps, in its order -
//                          a duct keeps its level ONLY on its start level
//   worksets               LIST_WORKSETS: user worksets only, by name
//   warnings               READ_MODEL_WARNINGS: grouped by the text, Error the
//                          harshest in a group, worst first then commonest; an
//                          id not in this model is left out of the ids
//   views on a sheet       FIND_UNPLACED_VIEWS: a viewport OR a schedule on a
//                          sheet; templates, sheets and Revit's browser views
//                          are not views here
//   views with no template FIND_VIEWS_WITHOUT_TEMPLATE: sheets, schedules and
//                          legends take none and are not counted
//   unused families        FIND_UNUSED_FAMILIES: no type of the family is the
//                          type of anything placed, counted in one pass
//   unused group types     FIND_UNUSED_GROUP_TYPES: nothing placed AND not
//                          attached to a placed model group
//   links                  LIST_LINKED_MODELS: one row per link FILE, loaded
//                          when a placement hands back its document
//   rooms and spaces       SELECT_UNENCLOSED_ROOMS: no area and no location is
//                          NOT PLACED; no area with a location is NOT ENCLOSED
//                          - or REDUNDANT, two in one enclosed region, which
//                          reads the same (Area 0) and is not told apart here
//
// A PART THAT COULD NOT BE READ IS NULL, NEVER ZERO. Each part is read in its
// own try: a part that throws is written as null and named in `notRead` -
// "<part>: <why>" - and the rest goes on, so one bad part never loses the
// dashboard. A zero there would be the plausible zero check-narrow-errors
// exists for: a fault reported as an empty answer that nobody doubts. The run
// clock's stop is caught with the rest and is sticky - the add-in still
// reports the run stopped (HeronRunClock).
//
// A FAMILY DOCUMENT IS NOT A PROJECT. Only the model block is read; every part
// is null and named in `notRead` as not a project.
//
// EVERY LIST OF IDS OR NAMES STOPS AT 500, and `capped` says which was cut and
// how long it was. The counts beside a list are never cut.
//
// ONE STRING, BECAUSE A LIST CROSSES AS ITS FIRST THREE ITEMS (FRAGMENT-ISSUES
// 5b-195) - REPORT_SPACE_ENVELOPE's takeoffJson and REPORT_SPRINKLER_NETWORK's
// networkJson do the same. Element ids are digit strings, never numbers: an id
// is ToString(), never IntegerValue (64-bit from 2024).
//
// THE WORK IS INSIDE ONE BLOCK ON PURPOSE. The add-in hands back every name
// left at the top of a fragment, and every entry of a dictionary left there -
// so the counting tables stay inside the block and only the four outputs are
// left behind.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES, as in
// read-element-table and report-space-envelope: the tools that write these
// files have eaten typed backslashes before.
//
// ----------------------------------------------------------------------
// VERSION 2 (2026-11-01) - HOW FULL THE NAMED PARAMETERS ARE
// ----------------------------------------------------------------------
//
// `parameters` names up to 12 parameters, comma separated - "Mark, Type Mark,
// Fire Rating". Capitals and extra spaces are forgiven. Left out, empty or
// "none" (the card's `absent: none`), nothing is counted and `fill` is null -
// version 1's answer plus that one key, so "format" stays 1: a format-1 reader
// ignores a key it does not know. More than 12 is refused for this part: `fill`
// null, a "fill:" line in notRead naming the limit, the rest read as before.
//
// For every element the dashboard counts - the placed model elements, nothing
// else - each name is in one of heron_modelqa's three states, kept apart:
//
//   populated   the instance's parameter, else its type's, holds a value: a
//               text that is not blank, a number or a Yes/No (0 and No ARE
//               values), or an element that is not "none"
//   empty       the parameter is there and holds nothing
//   absent      neither the instance nor its type has it - a family or a
//               category question, not a value to type in
//
// and a fourth, never folded into them: DOUBLED - two parameters on the
// element (or, with none on it, on its type) answer to the name. Which one is
// meant is not guessed (D-54 s3, FRAGMENT-ISSUES 5b-203): a shared "Fire
// Rating" bound beside the family's own is the case.
//
// INSTANCE FIRST, THEN THE TYPE, as READ_ELEMENT_PARAMETERS asks: the one the
// modeller edits on the element wins.
//
// MARK AND TYPE MARK BY WHAT THEY ARE, NOT BY NAME - READ_ELEMENT_TABLE's rule,
// ALL_MODEL_MARK and ALL_MODEL_TYPE_MARK. Typed "Mark" or "Type Mark", they
// are read through those two built-in parameters, so they are found in a Revit
// of any language; the name is asked only where the built-in is not there.
// Their own local names are found by name, like any other.
//
// A FAULT IN THIS PART IS fill NULL AND A "fill:" LINE, never a table of zeros;
// the rest of the dashboard is unaffected.

var dashboardJson = "";
var findings = "";
var elementTotal = 0;
var sectionsNotRead = 0;

{
    var most = 500;
    var backslash = ((char)92).ToString();
    var quote = ((char)34).ToString();
    var invariant = System.Globalization.CultureInfo.InvariantCulture;

    Func<string, string> esc = text =>
    {
        if (text == null) return "null";
        var sb = new System.Text.StringBuilder(quote);
        foreach (var c in text)
        {
            if (c == (char)92) sb.Append(backslash + backslash);
            else if (c == (char)34) sb.Append(backslash + quote);
            else if (c < (char)32) sb.Append(backslash + "u" + ((int)c).ToString("x4"));
            else sb.Append(c);
        }
        return sb.Append(quote).ToString();
    };
    Func<int, string> whole = n => n.ToString(invariant);
    Func<double, string> num = d =>
        (double.IsNaN(d) || double.IsInfinity(d)) ? "null" : Math.Round(d, 1).ToString("R", invariant);
    Func<bool, string> flag = b => b ? "true" : "false";
    Func<IEnumerable<string>, string> textList = items => "[" + string.Join(",", items.Select(esc).ToArray()) + "]";
    Func<Exception, string> why = ex => ex.GetType().Name + ": " + (ex.Message ?? "").Trim();

    var notRead = new List<string>();
    var capped = new List<string>();
    var lines = new List<string>();

    // The first `most`, and a note naming the list and its length when cut.
    Func<List<string>, string, List<string>> cap = (items, where) =>
    {
        if (items.Count <= most) return items;
        capped.Add(where + ": " + whole(items.Count) + " in the model, the first " + whole(most) + " are listed");
        return items.GetRange(0, most);
    };

    // ---- the model ---------------------------------------------------------

    string title = null;
    bool isFamily = false, isWorkshared = false;
    try { title = doc.Title; } catch (Exception) { title = null; }
    try { isFamily = doc.IsFamilyDocument; } catch (Exception) { isFamily = false; }
    try { isWorkshared = doc.IsWorkshared; } catch (Exception) { isWorkshared = false; }

    var modelJson = "{" + esc("title") + ":" + esc(title)
        + "," + esc("isFamily") + ":" + flag(isFamily)
        + "," + esc("isWorkshared") + ":" + flag(isWorkshared) + "}";
    lines.Add("Model '" + (title ?? "") + "' - " + (isFamily ? "a family document" : "a project")
        + (isWorkshared ? ", workshared" : ""));

    // Every part, "null" until it has been read.
    string elementTotalJson = "null", categoriesJson = "null", levelsJson = "null", noLevelJson = "null",
           worksetsJson = "null", warningsJson = "null", viewsJson = "null", sheetsJson = "null",
           familiesJson = "null", groupsJson = "null", linksJson = "null", cadJson = "null",
           roomsJson = "null", spacesJson = "null", fillJson = "null";

    // ---- version 2: the parameter names asked for ---------------------------
    //
    // A name is compared with capitals and runs of spaces forgiven. "none", or
    // nothing at all, is no name - the card's `absent: none`.
    Func<string, string> nameKey = text =>
        string.Join(" ", (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    var fillMost = 12;
    var fillNames = new List<string>();      // as typed, outer spaces trimmed
    var fillKeys = new List<string>();       // compared form, same order
    string fillRefused = null;
    if (nameKey(parameters) != "" && nameKey(parameters) != "none")
    {
        foreach (var piece in (parameters ?? "").Split(','))
        {
            var typed = piece.Trim();
            var key = nameKey(typed);
            if (key.Length == 0 || fillKeys.Contains(key)) continue;
            fillNames.Add(typed);
            fillKeys.Add(key);
        }
        if (fillKeys.Count > fillMost)
            fillRefused = whole(fillKeys.Count) + " parameter names were given and at most " + whole(fillMost)
                + " are read - nothing was counted for them; ask again with " + whole(fillMost) + " or fewer";
    }
    var fillAsked = fillKeys.Count > 0 && fillRefused == null;
    if (fillRefused != null)
    {
        notRead.Add("fill: " + fillRefused);
        lines.Add("Fill: REFUSED - " + fillRefused);
    }

    if (isFamily)
    {
        // The names notRead starts with are the parts' own: "elements" for
        // elementTotal, "levels" for levels and noLevel.
        foreach (var part in new[] { "elements", "categories", "levels", "worksets", "warnings", "views",
                                     "sheets", "families", "groups", "links", "cad", "rooms", "spaces" })
            notRead.Add(part + ": not a project - this is a family document, and the dashboard reads projects only");
        if (fillAsked)
            notRead.Add("fill: not a project - this is a family document, and the dashboard reads projects only");
        lines.Add("Nothing else was read: the dashboard reads projects only.");
    }
    else
    {
        // ---- one pass over every element that is not a type ------------------
        //
        // Feeds elementTotal, categories, levels, noLevel, the worksets'
        // counts, and which types are in use for the families.

        var usedTypes = new HashSet<ElementId>();
        var categoryName = new Dictionary<ElementId, string>();
        var categoryCount = new Dictionary<ElementId, int>();
        var categoryTypes = new Dictionary<ElementId, HashSet<ElementId>>();
        var levelOf = new Dictionary<ElementId, Level>();
        var onLevel = new Dictionary<ElementId, int>();
        // KEYED BY THE WorksetId ITSELF, as COPY_ELEMENTS keys its worksets -
        // no number is read off an id (tests/test_api_changes.py
        // reads IntegerValue as the member 2026 removed from ElementId).
        var onWorkset = new Dictionary<WorksetId, int>();
        var noLevel = 0;
        var unreadable = 0;
        string firstUnreadable = null;
        string scanFailed = null;

        // ---- version 2: what each named parameter holds, per element ---------
        //
        // States: 0 populated, 1 empty, 2 absent, 3 doubled - see the header.
        var fillCounts = new Dictionary<ElementId, int[,]>();       // category -> [name, state]
        var fillEmptyIds = new Dictionary<ElementId, List<string>[]>(); // category -> per name, first `most`
        string fillFailed = null;
        var wantedKeys = new HashSet<string>(fillKeys);
        var builtIns = new BuiltInParameter?[fillKeys.Count];
        for (var i = 0; i < fillKeys.Count; i++)
        {
            if (fillKeys[i] == "mark") builtIns[i] = BuiltInParameter.ALL_MODEL_MARK;
            else if (fillKeys[i] == "type mark") builtIns[i] = BuiltInParameter.ALL_MODEL_TYPE_MARK;
        }
        // An element's parameters that answer to an asked name, by compared
        // name - each parameter once, by its id.
        Func<Element, Dictionary<string, List<Parameter>>> matchesOn = holder =>
        {
            var found = new Dictionary<string, List<Parameter>>();
            var seenParameters = new HashSet<string>();
            foreach (Parameter p in holder.Parameters)
            {
                if (p == null || p.Definition == null) continue;
                var key = nameKey(p.Definition.Name);
                if (!wantedKeys.Contains(key)) continue;
                if (!seenParameters.Add(p.Id.ToString())) continue;
                List<Parameter> list;
                if (!found.TryGetValue(key, out list)) { list = new List<Parameter>(); found[key] = list; }
                list.Add(p);
            }
            return found;
        };
        // heron_modelqa's _filled: nothing in it is empty; 0 and No are values.
        Func<Parameter, int> stateOf = p =>
        {
            if (!p.HasValue) return 1;
            if (p.StorageType == StorageType.String) return string.IsNullOrWhiteSpace(p.AsString()) ? 1 : 0;
            if (p.StorageType == StorageType.Integer || p.StorageType == StorageType.Double) return 0;
            if (p.StorageType == StorageType.ElementId)
            {
                // An element parameter set to none holds the invalid id.
                var held = p.AsElementId();
                return held == null || held == ElementId.InvalidElementId ? 1 : 0;
            }
            return 1;
        };
        var typeMatches = new Dictionary<ElementId, Dictionary<string, List<Parameter>>>();
        var typeHolder = new Dictionary<ElementId, Element>();
        Func<Element, ElementId, int[]> readFill = (element, typeId) =>
        {
            Element type = null;
            Dictionary<string, List<Parameter>> onType = null;
            if (typeId != null && typeId != ElementId.InvalidElementId)
            {
                if (!typeHolder.ContainsKey(typeId))
                {
                    var found = doc.GetElement(typeId);
                    typeHolder[typeId] = found;
                    typeMatches[typeId] = found == null ? null : matchesOn(found);
                }
                type = typeHolder[typeId];
                onType = typeMatches[typeId];
            }
            // The element's own parameters are walked only when a name is left
            // that no built-in answered - asking for Mark alone walks none.
            Dictionary<string, List<Parameter>> onElement = null;
            var states = new int[fillKeys.Count];
            for (var i = 0; i < fillKeys.Count; i++)
            {
                Parameter chosen = null;
                if (builtIns[i].HasValue)
                {
                    chosen = element.get_Parameter(builtIns[i].Value);
                    if (chosen == null && type != null) chosen = type.get_Parameter(builtIns[i].Value);
                }
                if (chosen != null) { states[i] = stateOf(chosen); continue; }
                if (onElement == null) onElement = matchesOn(element);
                List<Parameter> list;
                if (!onElement.TryGetValue(fillKeys[i], out list) && (onType == null || !onType.TryGetValue(fillKeys[i], out list)))
                    states[i] = 2;
                else
                    states[i] = list.Count > 1 ? 3 : stateOf(list[0]);
            }
            return states;
        };

        try
        {
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
            {
                var level = element as Level;
                if (level == null) continue;
                levelOf[level.Id] = level;
                onLevel[level.Id] = 0;
            }

            foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
            {
                if (element == null) continue;
                try
                {
                    // Every placed thing's type is in use - a tag, a wall, a
                    // duct - whatever its category (FIND_UNUSED_FAMILIES).
                    var typeId = element.GetTypeId();
                    var hasType = typeId != null && typeId != ElementId.InvalidElementId;
                    if (hasType) usedTypes.Add(typeId);

                    if (element is Level || element.ViewSpecific) continue;
                    var category = element.Category;
                    if (category == null || category.CategoryType != CategoryType.Model) continue;
                    if (element.get_BoundingBox(null) == null) continue;

                    // EVERYTHING IS READ BEFORE ANYTHING IS COUNTED, so an
                    // element that throws half-way leaves no half a count.
                    var categoryId = category.Id;
                    var categoryLabel = category.Name;

                    // SELECT_WITHOUT_LEVEL's four steps, in its order.
                    var at = ElementId.InvalidElementId;
                    var wall = element as Wall;
                    if (wall != null) at = wall.LevelId;
                    if (at == ElementId.InvalidElementId) at = element.LevelId;
                    if (at == ElementId.InvalidElementId)
                    {
                        var parameter = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                        if (parameter != null && parameter.HasValue) at = parameter.AsElementId();
                    }
                    if (at == ElementId.InvalidElementId)
                    {
                        var parameter = element.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                        if (parameter != null && parameter.HasValue) at = parameter.AsElementId();
                    }

                    WorksetId worksetKey = null;
                    if (isWorkshared) worksetKey = element.WorksetId;

                    // Version 2. A fault here stops the fill part only - the
                    // element is still counted everywhere else.
                    int[] fillStates = null;
                    if (fillAsked && fillFailed == null)
                    {
                        try { fillStates = readFill(element, hasType ? typeId : null); }
                        catch (Exception ex) { fillFailed = why(ex); fillStates = null; }
                    }

                    elementTotal++;
                    if (!categoryCount.ContainsKey(categoryId))
                    {
                        categoryCount[categoryId] = 0;
                        categoryName[categoryId] = categoryLabel;
                        categoryTypes[categoryId] = new HashSet<ElementId>();
                    }
                    categoryCount[categoryId] = categoryCount[categoryId] + 1;
                    if (hasType) categoryTypes[categoryId].Add(typeId);

                    int count;
                    if (at != null && at != ElementId.InvalidElementId && onLevel.TryGetValue(at, out count))
                        onLevel[at] = count + 1;
                    else
                        noLevel++;

                    if (worksetKey != null)
                        onWorkset[worksetKey] = (onWorkset.ContainsKey(worksetKey) ? onWorkset[worksetKey] : 0) + 1;

                    if (fillStates != null)
                    {
                        int[,] tally;
                        if (!fillCounts.TryGetValue(categoryId, out tally))
                        {
                            tally = new int[fillKeys.Count, 4];
                            fillCounts[categoryId] = tally;
                            var lists = new List<string>[fillKeys.Count];
                            for (var j = 0; j < fillKeys.Count; j++) lists[j] = new List<string>();
                            fillEmptyIds[categoryId] = lists;
                        }
                        for (var j = 0; j < fillKeys.Count; j++)
                        {
                            tally[j, fillStates[j]] = tally[j, fillStates[j]] + 1;
                            // Kept to the cap; the count beside it is never cut.
                            if (fillStates[j] == 1 && fillEmptyIds[categoryId][j].Count < most)
                                fillEmptyIds[categoryId][j].Add(element.Id.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ONE ELEMENT THAT WILL NOT ANSWER IS COUNTED, NEVER
                    // DROPPED: the totals are short by it, and notRead says so.
                    unreadable++;
                    if (firstUnreadable == null) firstUnreadable = why(ex);
                }
            }
        }
        catch (Exception ex)
        {
            scanFailed = why(ex);
        }

        if (scanFailed != null)
        {
            // A pass that stopped part-way leaves a count of part of the model.
            // It is not handed on as if it were the whole: 0, with the reason.
            elementTotal = 0;
            notRead.Add("elements: the model's elements could not be read - " + scanFailed);
            notRead.Add("categories: the model's elements could not be read - " + scanFailed);
            notRead.Add("levels: the model's elements could not be read - " + scanFailed);
            if (isWorkshared) notRead.Add("worksets: the model's elements could not be read - " + scanFailed);
            lines.Add("Elements: NOT READ - " + scanFailed);
        }
        else
        {
            if (unreadable > 0)
                notRead.Add("elements: " + whole(unreadable) + " element(s) could not be read and are in no count, "
                    + "so elementTotal, categories, levels, worksets" + (fillAsked ? " and fill" : "")
                    + " are short by up to that many - first: " + firstUnreadable);
            elementTotalJson = whole(elementTotal);
            lines.Add("Elements: " + whole(elementTotal) + " placed model element(s) in "
                + whole(categoryCount.Count) + " categor" + (categoryCount.Count == 1 ? "y" : "ies"));

            // "types" is how many types the counted elements of the category
            // USE - not how many are loaded; the families part has those.
            try
            {
                var rows = categoryCount.Keys
                    .OrderByDescending(id => categoryCount[id])
                    .ThenBy(id => categoryName[id] ?? "", StringComparer.OrdinalIgnoreCase)
                    .Select(id => "{" + esc("name") + ":" + esc(categoryName[id])
                        + "," + esc("instances") + ":" + whole(categoryCount[id])
                        + "," + esc("types") + ":" + whole(categoryTypes[id].Count) + "}")
                    .ToList();
                categoriesJson = "[" + string.Join(",", rows.ToArray()) + "]";
            }
            catch (Exception ex)
            {
                notRead.Add("categories: " + why(ex));
            }

            // Bottom to top by where the level really is (its project
            // elevation); the height written is the one its Properties show,
            // measured from its own Elevation Base. Every level is listed,
            // with nothing on it or not.
            try
            {
                var levelRows = levelOf.Values
                    .OrderBy(l => l.ProjectElevation)
                    .ThenBy(l => l.Name ?? "", StringComparer.OrdinalIgnoreCase)
                    .Select(l => "{" + esc("name") + ":" + esc(l.Name)
                        + "," + esc("elevationMm") + ":" + num(l.Elevation * 304.8)
                        + "," + esc("instances") + ":" + whole(onLevel[l.Id]) + "}")
                    .ToList();
                levelsJson = "[" + string.Join(",", levelRows.ToArray()) + "]";
                noLevelJson = whole(noLevel);
                lines.Add("Levels: " + whole(levelOf.Count) + " level(s); " + whole(noLevel) + " placed model element(s) on no level");
            }
            catch (Exception ex)
            {
                notRead.Add("levels: " + why(ex));
                lines.Add("Levels: NOT READ - " + why(ex));
            }
        }

        // ---- version 2: the fill part -----------------------------------------
        //
        // One row per category with an element counted and per name, the
        // categories by name, the names in the order typed.

        if (fillAsked)
        {
            if (scanFailed != null)
            {
                notRead.Add("fill: the model's elements could not be read - " + scanFailed);
                lines.Add("Fill: NOT READ - " + scanFailed);
            }
            else if (fillFailed != null)
            {
                notRead.Add("fill: the parameters could not be read - " + fillFailed);
                lines.Add("Fill: NOT READ - " + fillFailed);
            }
            else
            {
                try
                {
                    var rows = new List<string>();
                    var cut = 0;
                    var totals = new int[fillKeys.Count, 4];
                    var order = fillCounts.Keys
                        .OrderBy(id => categoryName.ContainsKey(id) ? categoryName[id] ?? "" : "", StringComparer.OrdinalIgnoreCase)
                        .ThenBy(id => id.ToString(), StringComparer.Ordinal)
                        .ToList();
                    foreach (var categoryId in order)
                    {
                        var tally = fillCounts[categoryId];
                        for (var j = 0; j < fillKeys.Count; j++)
                        {
                            for (var s = 0; s < 4; s++) totals[j, s] = totals[j, s] + tally[j, s];
                            if (tally[j, 1] > most) cut++;
                            rows.Add("{" + esc("category") + ":" + esc(categoryName[categoryId])
                                + "," + esc("parameter") + ":" + esc(fillNames[j])
                                + "," + esc("populated") + ":" + whole(tally[j, 0])
                                + "," + esc("empty") + ":" + whole(tally[j, 1])
                                + "," + esc("absent") + ":" + whole(tally[j, 2])
                                + "," + esc("doubled") + ":" + whole(tally[j, 3])
                                + "," + esc("emptyIds") + ":" + textList(fillEmptyIds[categoryId][j]) + "}");
                        }
                    }
                    if (cut > 0)
                        capped.Add("fill.rows.emptyIds: " + whole(cut) + " row(s) have more than " + whole(most)
                            + " empty elements, and the first " + whole(most) + " of each are listed");
                    fillJson = "{" + esc("parameters") + ":" + textList(fillNames)
                        + "," + esc("rows") + ":[" + string.Join(",", rows.ToArray()) + "]}";
                    var said = new List<string>();
                    for (var j = 0; j < fillKeys.Count; j++)
                        said.Add(fillNames[j] + " " + whole(totals[j, 0]) + " populated, " + whole(totals[j, 1]) + " empty, "
                            + whole(totals[j, 2]) + " absent" + (totals[j, 3] > 0 ? ", " + whole(totals[j, 3]) + " doubled" : ""));
                    lines.Add("Fill: " + string.Join("; ", said.ToArray()) + " - over " + whole(elementTotal)
                        + " placed model element(s)");
                }
                catch (Exception ex)
                {
                    notRead.Add("fill: " + why(ex));
                    lines.Add("Fill: NOT READ - " + why(ex));
                }
            }
        }

        // ---- worksets (LIST_WORKSETS) ----------------------------------------

        if (!isWorkshared)
        {
            worksetsJson = "[]";
            lines.Add("Worksets: not workshared");
        }
        else if (scanFailed == null)
        {
            try
            {
                var worksets = new List<Workset>();
                foreach (var workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
                    if (workset != null) worksets.Add(workset);
                worksets.Sort(delegate (Workset a, Workset b)
                {
                    return string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase);
                });
                var closed = 0;
                var rows = new List<string>();
                foreach (var workset in worksets)
                {
                    var open = workset.IsOpen;
                    if (!open) closed++;
                    var key = workset.Id;
                    // ON A CLOSED WORKSET NOTHING IS LOADED, so its count is 0
                    // because nothing on it could be counted - `open` says so.
                    rows.Add("{" + esc("name") + ":" + esc(workset.Name)
                        + "," + esc("open") + ":" + flag(open)
                        + "," + esc("instances") + ":" + whole(onWorkset.ContainsKey(key) ? onWorkset[key] : 0) + "}");
                }
                worksetsJson = "[" + string.Join(",", rows.ToArray()) + "]";
                lines.Add("Worksets: " + whole(worksets.Count) + " user workset(s), " + whole(closed) + " closed"
                    + (closed > 0 ? " - nothing on a closed workset is loaded or counted" : ""));
            }
            catch (Exception ex)
            {
                notRead.Add("worksets: " + why(ex));
                lines.Add("Worksets: NOT READ - " + why(ex));
            }
        }

        // ---- warnings (READ_MODEL_WARNINGS) ----------------------------------

        try
        {
            var warnings = doc.GetWarnings();
            var total = warnings == null ? 0 : warnings.Count;
            var errors = 0;
            var notHere = 0;
            var count = new Dictionary<string, int>();
            var isError = new Dictionary<string, bool>();
            var order = new List<string>();
            var ids = new Dictionary<string, List<string>>();
            var seen = new Dictionary<string, HashSet<ElementId>>();
            if (warnings != null)
            {
                foreach (var warning in warnings)
                {
                    if (warning == null) continue;
                    var text = warning.GetDescriptionText();
                    if (string.IsNullOrEmpty(text)) text = "(the warning carries no description)";
                    var error = warning.GetSeverity() == FailureSeverity.Error;
                    if (error) errors++;
                    if (!count.ContainsKey(text))
                    {
                        count[text] = 0;
                        isError[text] = false;
                        order.Add(text);
                        ids[text] = new List<string>();
                        seen[text] = new HashSet<ElementId>();
                    }
                    count[text] = count[text] + 1;
                    if (error) isError[text] = true;
                    var failing = warning.GetFailingElements();
                    if (failing == null) continue;
                    foreach (var id in failing)
                    {
                        if (id == null || id == ElementId.InvalidElementId) continue;
                        if (!seen[text].Add(id)) continue;
                        // An id from a link's id space, or one emptied by a
                        // delete, is not this model's - left out of the ids,
                        // never handed to Select by ID (READ_MODEL_WARNINGS).
                        if (doc.GetElement(id) == null) { notHere++; continue; }
                        ids[text].Add(id.ToString());
                    }
                }
            }
            // Worst first, then commonest; OrderBy keeps first-seen order on a tie.
            var sorted = order.OrderByDescending(t => isError[t]).ThenByDescending(t => count[t]).ToList();
            var cutIds = 0;
            var rows = new List<string>();
            foreach (var text in sorted)
            {
                var list = ids[text];
                if (list.Count > most) { cutIds++; list = list.GetRange(0, most); }
                rows.Add("{" + esc("text") + ":" + esc(text)
                    + "," + esc("severity") + ":" + esc(isError[text] ? "error" : "warning")
                    + "," + esc("count") + ":" + whole(count[text])
                    + "," + esc("ids") + ":" + textList(list) + "}");
            }
            if (cutIds > 0)
                capped.Add("warnings.groups.ids: " + whole(cutIds) + " kind(s) name more than " + whole(most)
                    + " elements, and the first " + whole(most) + " of each are listed");
            rows = cap(rows, "warnings.groups");
            warningsJson = "{" + esc("total") + ":" + whole(total)
                + "," + esc("errors") + ":" + whole(errors)
                + "," + esc("groups") + ":[" + string.Join(",", rows.ToArray()) + "]}";
            lines.Add("Warnings: " + whole(total) + " (" + whole(errors) + " error(s)) in " + whole(order.Count) + " kind(s)"
                + (notHere > 0 ? "; " + whole(notHere) + " failing element id(s) are not in this model and are left out of the ids" : ""));
        }
        catch (Exception ex)
        {
            notRead.Add("warnings: " + why(ex));
            lines.Add("Warnings: NOT READ - " + why(ex));
        }

        // ---- what is on a sheet: views (FIND_UNPLACED_VIEWS) and sheets ------

        var placedViews = new HashSet<ElementId>();
        var sheetsWithSomething = new HashSet<ElementId>();
        string placementFailed = null;
        try
        {
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
            {
                var viewport = element as Viewport;
                if (viewport == null) continue;
                placedViews.Add(viewport.ViewId);
                sheetsWithSomething.Add(viewport.SheetId);
            }
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)))
            {
                var instance = element as ScheduleSheetInstance;
                if (instance == null) continue;
                placedViews.Add(instance.ScheduleId);
                // A title block's own revision schedule is on every sheet that
                // has the title block - it does not make a sheet not empty.
                if (!instance.IsTitleblockRevisionSchedule) sheetsWithSomething.Add(instance.OwnerViewId);
            }
        }
        catch (Exception ex)
        {
            placementFailed = why(ex);
        }

        if (placementFailed != null)
        {
            notRead.Add("views: what is on the sheets could not be read - " + placementFailed);
            lines.Add("Views: NOT READ - " + placementFailed);
        }
        else
        {
            try
            {
                int total = 0, templates = 0, onSheets = 0, withoutTemplate = 0;
                var byType = new Dictionary<string, int>();
                var notOnSheets = new List<string>();
                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
                {
                    var view = element as View;
                    if (view == null) continue;
                    if (view.IsTemplate) { templates++; continue; }
                    var type = view.ViewType;
                    if (type == ViewType.ProjectBrowser || type == ViewType.SystemBrowser ||
                        type == ViewType.DrawingSheet || type == ViewType.Internal ||
                        type == ViewType.Undefined) continue;
                    // A title block's revision schedule is no view in the
                    // Project Browser, and would only inflate the schedules.
                    var schedule = view as ViewSchedule;
                    if (schedule != null && schedule.IsTitleblockRevisionSchedule) continue;

                    total++;
                    var kind = type.ToString();
                    byType[kind] = (byType.ContainsKey(kind) ? byType[kind] : 0) + 1;
                    if (placedViews.Contains(view.Id)) onSheets++;
                    else notOnSheets.Add(view.Id.ToString());
                    if (schedule == null && type != ViewType.Legend && !(view is ViewSheet)
                        && view.ViewTemplateId == ElementId.InvalidElementId)
                        withoutTemplate++;
                }
                var typeRows = byType.Keys.OrderBy(k => k, StringComparer.Ordinal)
                    .Select(k => esc(k) + ":" + whole(byType[k])).ToArray();
                viewsJson = "{" + esc("total") + ":" + whole(total)
                    + "," + esc("templates") + ":" + whole(templates)
                    + "," + esc("onSheets") + ":" + whole(onSheets)
                    + "," + esc("notOnSheets") + ":" + whole(notOnSheets.Count)
                    + "," + esc("withoutTemplate") + ":" + whole(withoutTemplate)
                    + "," + esc("byType") + ":{" + string.Join(",", typeRows) + "}"
                    + "," + esc("notOnSheetsIds") + ":" + textList(cap(notOnSheets, "views.notOnSheetsIds")) + "}";
                lines.Add("Views: " + whole(total) + " (and " + whole(templates) + " template(s)); " + whole(onSheets)
                    + " on a sheet, " + whole(notOnSheets.Count) + " on no sheet; " + whole(withoutTemplate)
                    + " with no view template");
            }
            catch (Exception ex)
            {
                notRead.Add("views: " + why(ex));
                lines.Add("Views: NOT READ - " + why(ex));
            }
        }

        if (placementFailed != null)
        {
            notRead.Add("sheets: what is on the sheets could not be read - " + placementFailed);
            lines.Add("Sheets: NOT READ - " + placementFailed);
        }
        else
        {
            try
            {
                int total = 0, placeholders = 0;
                var empty = new List<string>();
                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
                {
                    var sheet = element as ViewSheet;
                    if (sheet == null) continue;
                    total++;
                    // A placeholder holds no views by definition - counted as
                    // a placeholder, never as an empty sheet.
                    if (sheet.IsPlaceholder) { placeholders++; continue; }
                    // EMPTY IS NO VIEWPORT AND NO SCHEDULE. A cover sheet with
                    // only text on it counts as empty: text is not a view.
                    if (!sheetsWithSomething.Contains(sheet.Id)) empty.Add(sheet.Id.ToString());
                }
                sheetsJson = "{" + esc("total") + ":" + whole(total)
                    + "," + esc("empty") + ":" + whole(empty.Count)
                    + "," + esc("placeholders") + ":" + whole(placeholders)
                    + "," + esc("emptyIds") + ":" + textList(cap(empty, "sheets.emptyIds")) + "}";
                lines.Add("Sheets: " + whole(total) + ", " + whole(empty.Count) + " with no view or schedule on them, "
                    + whole(placeholders) + " placeholder(s)");
            }
            catch (Exception ex)
            {
                notRead.Add("sheets: " + why(ex));
                lines.Add("Sheets: NOT READ - " + why(ex));
            }
        }

        // ---- families (FIND_UNUSED_FAMILIES) ---------------------------------

        if (scanFailed != null)
        {
            notRead.Add("families: which types are placed could not be read - " + scanFailed);
            lines.Add("Families: NOT READ - " + scanFailed);
        }
        else
        {
            try
            {
                int loaded = 0, inPlace = 0, types = 0;
                var unused = new List<string>();
                foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Family)))
                {
                    var family = element as Family;
                    if (family == null) continue;
                    if (family.IsInPlace) inPlace++; else loaded++;
                    var symbolIds = family.GetFamilySymbolIds();
                    if (symbolIds == null || symbolIds.Count == 0) continue;
                    types += symbolIds.Count;
                    // A FAMILY PLACED ONLY INSIDE ANOTHER FAMILY reads unused
                    // here, as in FIND_UNUSED_FAMILIES - it has no instance of
                    // its own in the project, and Purge Unused knows better.
                    if (!symbolIds.Any(usedTypes.Contains)) unused.Add(family.Name ?? "");
                }
                unused.Sort(StringComparer.OrdinalIgnoreCase);
                familiesJson = "{" + esc("loaded") + ":" + whole(loaded)
                    + "," + esc("inPlace") + ":" + whole(inPlace)
                    + "," + esc("unused") + ":" + whole(unused.Count)
                    + "," + esc("types") + ":" + whole(types)
                    + "," + esc("unusedNames") + ":" + textList(cap(unused, "families.unusedNames")) + "}";
                lines.Add("Families: " + whole(loaded) + " loaded and " + whole(inPlace) + " in-place, " + whole(types)
                    + " type(s); " + whole(unused.Count) + " with nothing placed");
            }
            catch (Exception ex)
            {
                notRead.Add("families: " + why(ex));
                lines.Add("Families: NOT READ - " + why(ex));
            }
        }

        // ---- group types (FIND_UNUSED_GROUP_TYPES) ---------------------------

        try
        {
            // A detail group attached to a placed model group is in use, with
            // no placement of its own.
            var offered = new HashSet<ElementId>();
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Group)))
            {
                var group = element as Group;
                if (group == null) continue;
                try
                {
                    var attached = group.GetAvailableAttachedDetailGroupTypeIds();
                    if (attached != null) foreach (var id in attached) offered.Add(id);
                }
                catch (Exception)
                {
                    // Left alone rather than assumed to offer nothing - that is
                    // the direction that calls a detail group in use unused.
                }
            }
            var modelGroups = new ElementId(BuiltInCategory.OST_IOSModelGroups);
            var detailGroups = new ElementId(BuiltInCategory.OST_IOSDetailGroups);
            var attachedGroups = new ElementId(BuiltInCategory.OST_IOSAttachedDetailGroups);
            int modelTypes = 0, detailTypes = 0, unusedTypes = 0, notAsked = 0;
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(GroupType)))
            {
                var groupType = element as GroupType;
                if (groupType == null) continue;
                var category = groupType.Category;
                if (category != null && category.Id == modelGroups) modelTypes++;
                else if (category != null && (category.Id == detailGroups || category.Id == attachedGroups)) detailTypes++;
                var placed = 0;
                try
                {
                    var groups = groupType.Groups;
                    if (groups != null) placed = groups.Size;
                }
                catch (Exception)
                {
                    // Unreadable is not zero - not counted as unused.
                    notAsked++;
                    continue;
                }
                if (placed == 0 && !offered.Contains(groupType.Id)) unusedTypes++;
            }
            groupsJson = "{" + esc("modelTypes") + ":" + whole(modelTypes)
                + "," + esc("detailTypes") + ":" + whole(detailTypes)
                + "," + esc("unusedTypes") + ":" + whole(unusedTypes) + "}";
            lines.Add("Groups: " + whole(modelTypes) + " model and " + whole(detailTypes) + " detail group type(s); "
                + whole(unusedTypes) + " with nothing placed"
                + (notAsked > 0 ? "; " + whole(notAsked) + " would not say what is placed and are not counted as unused" : ""));
        }
        catch (Exception ex)
        {
            notRead.Add("groups: " + why(ex));
            lines.Add("Groups: NOT READ - " + why(ex));
        }

        // ---- Revit links (LIST_LINKED_MODELS) --------------------------------

        try
        {
            var placements = new Dictionary<ElementId, int>();
            var loaded = new Dictionary<ElementId, bool>();
            var pinned = new Dictionary<ElementId, bool>();
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
            {
                var instance = element as RevitLinkInstance;
                if (instance == null) continue;
                var typeId = instance.GetTypeId();
                if (typeId == null || typeId == ElementId.InvalidElementId) continue;
                if (!placements.ContainsKey(typeId))
                {
                    placements[typeId] = 0;
                    loaded[typeId] = false;
                    pinned[typeId] = true;
                }
                placements[typeId] = placements[typeId] + 1;
                // LOADED IS ASKED OF THE DOCUMENT, never read off a status;
                // any placement handing one back means the file is there.
                Document linked = null;
                try { linked = instance.GetLinkDocument(); } catch (Exception) { linked = null; }
                if (linked != null) loaded[typeId] = true;
                // PINNED ONLY WHEN EVERY PLACEMENT IS: one loose copy is the
                // one that gets dragged.
                if (!instance.Pinned) pinned[typeId] = false;
            }
            var nameOf = new Dictionary<ElementId, string>();
            foreach (var typeId in placements.Keys)
            {
                var linkType = doc.GetElement(typeId);
                nameOf[typeId] = linkType == null ? "(the link type is no longer in this model)" : linkType.Name;
            }
            var rows = placements.Keys
                .OrderBy(id => nameOf[id] ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(id => "{" + esc("name") + ":" + esc(nameOf[id])
                    + "," + esc("loaded") + ":" + flag(loaded[id])
                    + "," + esc("pinned") + ":" + flag(pinned[id])
                    + "," + esc("instances") + ":" + whole(placements[id]) + "}")
                .ToList();
            linksJson = "[" + string.Join(",", rows.ToArray()) + "]";
            lines.Add("Links: " + whole(placements.Count) + " Revit link(s), "
                + whole(loaded.Values.Count(v => !v)) + " not loaded, "
                + whole(pinned.Values.Count(v => !v)) + " with a placement not pinned");
        }
        catch (Exception ex)
        {
            notRead.Add("links: " + why(ex));
            lines.Add("Links: NOT READ - " + why(ex));
        }

        // ---- CAD files ---------------------------------------------------------

        try
        {
            int total = 0, linked = 0, imported = 0;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)))
            {
                var import = element as ImportInstance;
                if (import == null) continue;
                total++;
                if (import.IsLinked) linked++; else imported++;
                // The type's name is the file's; the category carries it too.
                string name = null;
                var cadType = doc.GetElement(import.GetTypeId());
                if (cadType != null) name = cadType.Name;
                if (string.IsNullOrEmpty(name) && import.Category != null) name = import.Category.Name;
                if (!string.IsNullOrEmpty(name)) names.Add(name);
            }
            var nameList = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            cadJson = "{" + esc("total") + ":" + whole(total)
                + "," + esc("linked") + ":" + whole(linked)
                + "," + esc("imported") + ":" + whole(imported)
                + "," + esc("names") + ":" + textList(cap(nameList, "cad.names")) + "}";
            lines.Add("CAD: " + whole(total) + " placed (" + whole(linked) + " linked, " + whole(imported) + " imported) from "
                + whole(nameList.Count) + " file(s)");
        }
        catch (Exception ex)
        {
            notRead.Add("cad: " + why(ex));
            lines.Add("CAD: NOT READ - " + why(ex));
        }

        // ---- rooms and spaces (SELECT_UNENCLOSED_ROOMS) ------------------------

        Func<BuiltInCategory, string, string, string> spatial = (category, part, label) =>
        {
            try
            {
                int total = 0, unplacedCount = 0, notEnclosedCount = 0;
                var unplaced = new List<string>();
                var notEnclosed = new List<string>();
                foreach (var element in new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType())
                {
                    var one = element as SpatialElement;
                    if (one == null) continue;
                    total++;
                    double area = 0;
                    try { area = one.Area; } catch (Exception) { area = 0; }
                    if (area > 0) continue;
                    bool hasLocation = false;
                    try { hasLocation = one.Location != null; } catch (Exception) { hasLocation = false; }
                    if (hasLocation) { notEnclosedCount++; notEnclosed.Add(one.Id.ToString()); }
                    else { unplacedCount++; unplaced.Add(one.Id.ToString()); }
                }
                lines.Add(label + ": " + whole(total) + " - " + whole(unplacedCount) + " not placed, "
                    + whole(notEnclosedCount) + " placed with no area (not enclosed, or redundant)");
                return "{" + esc("total") + ":" + whole(total)
                    + "," + esc("placed") + ":" + whole(total - unplacedCount)
                    + "," + esc("unplaced") + ":" + whole(unplacedCount)
                    + "," + esc("notEnclosed") + ":" + whole(notEnclosedCount)
                    + "," + esc("unplacedIds") + ":" + textList(cap(unplaced, part + ".unplacedIds"))
                    + "," + esc("notEnclosedIds") + ":" + textList(cap(notEnclosed, part + ".notEnclosedIds")) + "}";
            }
            catch (Exception ex)
            {
                notRead.Add(part + ": " + why(ex));
                lines.Add(label + ": NOT READ - " + why(ex));
                return "null";
            }
        };
        roomsJson = spatial(BuiltInCategory.OST_Rooms, "rooms", "Rooms");
        spacesJson = spatial(BuiltInCategory.OST_MEPSpaces, "spaces", "Spaces");
    }

    sectionsNotRead = notRead.Count;
    if (notRead.Count > 0) lines.Add("Not read: " + string.Join("; ", notRead.ToArray()));
    if (capped.Count > 0) lines.Add("Lists cut at " + whole(most) + ": " + string.Join("; ", capped.ToArray()));
    findings = string.Join("\n", lines.ToArray());

    dashboardJson = "{" + esc("format") + ":1"
        + "," + esc("model") + ":" + modelJson
        + "," + esc("elementTotal") + ":" + elementTotalJson
        + "," + esc("categories") + ":" + categoriesJson
        + "," + esc("levels") + ":" + levelsJson
        + "," + esc("noLevel") + ":" + noLevelJson
        + "," + esc("worksets") + ":" + worksetsJson
        + "," + esc("warnings") + ":" + warningsJson
        + "," + esc("views") + ":" + viewsJson
        + "," + esc("sheets") + ":" + sheetsJson
        + "," + esc("families") + ":" + familiesJson
        + "," + esc("groups") + ":" + groupsJson
        + "," + esc("links") + ":" + linksJson
        + "," + esc("cad") + ":" + cadJson
        + "," + esc("rooms") + ":" + roomsJson
        + "," + esc("spaces") + ":" + spacesJson
        + "," + esc("fill") + ":" + fillJson
        + "," + esc("notRead") + ":" + textList(notRead)
        + "," + esc("capped") + ":" + textList(capped) + "}";
}
