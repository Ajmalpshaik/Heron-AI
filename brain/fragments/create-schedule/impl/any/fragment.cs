// NOT STANDALONE. Assumes `doc`, `categoryId`, `fieldNames`, `scheduleName`,
// `listKind`, `sortByField`, `keyParameterName`, `areaScheme` and
// `noteBlockFamily` are in scope; leaves `created`, `refused`, `missingFields`,
// `fieldsAdded`, `findings` and `madeAs` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// FIELDS ARE MATCHED BY THE NAME REVIT SHOWS, which is what GetName(doc)
// returns and what appears in the Fields tab. So "Size" finds the field a
// modeller would pick off that list. It does NOT match internal parameter
// names, and that limit is deliberate: the request comes from somebody
// describing a drawing, not from somebody reading the API.
//
// A FIELD THAT DOES NOT EXIST IS REPORTED. Ask for Size on a category with no
// Size and a silently short schedule looks finished, prints, and is missing a
// column - and the person reading it cannot tell a column was ever wanted.
// Every name that found nothing comes back in `missingFields`.
//
// THE SCHEDULE IS STILL MADE when some fields are missing, rather than the
// whole thing being refused. A schedule with three of four columns is useful
// and can have the fourth added; no schedule at all is a re-run. The refusal
// is reserved for the cases where there is nothing to make.
//
// THE SCHEDULABLE FIELD LIST IS ASKED FOR ONCE. It is derived from the category
// and every parameter bound to it, so it is not cheap, and asking per requested
// field would multiply that by the number of columns.

// VERSION 2 (2026-11-01) - CREATE_SHEET_LIST AND CREATE_KEY_SCHEDULE MERGED
// IN. `listKind` says which schedule is made, and every kind then gets the
// same name, fields, sort and read-back:
//
//   schedule (left out)  ViewSchedule.CreateSchedule - a CATEGORY of model
//                        elements, version 1 exactly
//   sheets               ViewSchedule.CreateSheetList - the drawing index.
//                        Sheets are not model elements, so the category route
//                        cannot make one; placeholder sheets are rows too
//   views                ViewSchedule.CreateViewList - Revit's View List. A
//                        view list shows every view; "only the placed ones" is
//                        SET_SCHEDULE_FILTERS on Sheet Number, has value
//   key                  ViewSchedule.CreateKeySchedule - a table of
//                        DEFINITIONS elements point at, so one choice fills
//                        several fields. Revit is ASKED first whether the
//                        category may have one (IsValidCategoryForKeySchedule)
//                        because the create call throws rather than
//                        explaining. The key parameter is named, because the
//                        default is "Key Name" on every key schedule. It
//                        creates the table and NO ROWS - an empty dropdown
//                        that looks broken and is simply new, and is said so
//
// Any other kind is REFUSED and nothing is created: guessing would make the
// wrong schedule and report it made.
//
// VERSION 3 (2026-11-01, FRAGMENT-ISSUES row 5b-1367, NEEDS-CHECKING Group
// KK) - the OTHER KINDS on Revit's View > Schedules menu, as more words of
// the same `listKind`, and an area schedule of one AREA SCHEME:
//
//   material takeoff     ViewSchedule.CreateMaterialTakeoff - a category,
//                        asked first with IsValidCategoryForMaterialTakeoff
//   note block           ViewSchedule.CreateNoteBlock - ONE Generic
//                        Annotation family, named in `noteBlockFamily` and
//                        found among GetValidFamiliesForNoteBlock
//   revision schedule    ViewSchedule.CreateRevisionSchedule - no category
//   keynote legend       ViewSchedule.CreateKeynoteLegend - no category
//   `areaScheme`         with the kind left as schedule and the category
//                        Areas: CreateSchedule(doc, Areas, scheme)
//
// A CATEGORY GIVEN WITH A KIND THAT HAS ITS OWN IS CHECKED, NOT IGNORED. A
// revision schedule is of Revisions, a keynote legend of Keynote Tags, a note
// block of Generic Annotations; a request that meant one and arrived with
// Ducts is refused naming the right category, not quietly turned into
// something else. Left out, the category is not needed for those three. (A
// sheet or view list keeps version 2's rule: a category given is said in
// findings and not used.) An area scheme with any other kind or category, and
// a note block family with any other kind, are refused.
//
// `madeAs` IS A SECOND READ of the view made - its name, its category, what
// Revit says it is (a material takeoff, a key schedule, its area scheme, its
// family) and how many columns it has - never the request echoed. Whether a
// note block's definition reports its family through FamilyId is not said by
// the reference: it is read and printed, and the proof settles it.
//
// Left out, every version 3 input reads as version 2 exactly.
//
// THE SAFER RULE IN EACH CONFLICT IS KEPT. A name already used by another
// view leaves Revit's generated name in place and says so (CREATE_SHEET_LIST)
// - version 1 let Revit's refusal out, which aborts the caller's whole
// transaction. A field the schedule cannot carry is in missingFields AND
// named in `findings` with the names it CAN carry. A sort on a field that is
// not a column is said, never silently unsorted. A schedule left with no
// column at all is said - it prints as an empty box. Fields are matched
// ignoring capitals; two fields showing one name still give ONE column.

ElementId created = null;
string refused = null;
var missingFields = new List<string>();
var fieldsAdded = new List<string>();
var findings = new List<string>();

var madeAs = "";

var name = (scheduleName ?? "").Trim();

// The card's `absent:` words - "schedule" for the kind, "none" for a list, a
// sort or a key parameter left out - read here as nothing given.
Func<string, string, bool> sameText = (a, b) =>
    string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);

var wanted = new List<string>();
if (fieldNames != null)
{
    foreach (var requested in fieldNames)
    {
        if (string.IsNullOrEmpty(requested)) continue;
        var key = requested.Trim();
        if (key.Length == 0) continue;
        if (fieldNames.Count == 1 && sameText(key, "none")) continue;
        wanted.Add(key);
    }
}
var sortWanted = (sortByField ?? "").Trim();
if (sameText(sortWanted, "none")) sortWanted = "";
var keyName = (keyParameterName ?? "").Trim();
if (sameText(keyName, "none")) keyName = "";
var schemeWord = (areaScheme ?? "").Trim();
if (sameText(schemeWord, "none")) schemeWord = "";
var familyWord = (noteBlockFamily ?? "").Trim();
if (sameText(familyWord, "none")) familyWord = "";

var kindText = (listKind ?? "").Trim().ToLowerInvariant();
string kind = null;
if (kindText == "" || kindText == "schedule" || kindText == "category" || kindText == "regular"
    || kindText == "quantities" || kindText == "schedule/quantities")
    kind = "schedule";
else if (kindText == "sheets" || kindText == "sheet" || kindText == "sheet list"
    || kindText == "sheetlist" || kindText == "drawing index")
    kind = "sheets";
else if (kindText == "views" || kindText == "view" || kindText == "view list"
    || kindText == "viewlist")
    kind = "views";
else if (kindText == "key" || kindText == "key schedule" || kindText == "keys"
    || kindText == "schedule keys")
    kind = "key";
// VERSION 3.
else if (kindText == "material takeoff" || kindText == "materialtakeoff" || kindText == "takeoff"
    || kindText == "material takeoffs")
    kind = "takeoff";
else if (kindText == "note block" || kindText == "noteblock" || kindText == "note blocks")
    kind = "noteblock";
else if (kindText == "revision schedule" || kindText == "revisions" || kindText == "revision"
    || kindText == "revisionschedule")
    kind = "revisions";
else if (kindText == "keynote legend" || kindText == "keynotes" || kindText == "keynote"
    || kindText == "keynotelegend")
    kind = "keynotes";

var kindWord = kind == "sheets" ? "sheet list" : kind == "views" ? "view list"
             : kind == "key" ? "key schedule" : kind == "takeoff" ? "material takeoff"
             : kind == "noteblock" ? "note block" : kind == "revisions" ? "revision schedule"
             : kind == "keynotes" ? "keynote legend" : "schedule";

bool categoryGiven = categoryId != null && categoryId != ElementId.InvalidElementId;
string categoryName = "that category";
if (categoryGiven)
{
    try
    {
        var category = Category.GetCategory(doc, categoryId);
        if (category != null) categoryName = "'" + category.Name + "'";
    }
    catch { }
}

ViewSchedule schedule = null;
string revitSaid = null;

// VERSION 3: a kind with its own category, and what an area scheme or a note
// block family needs.
Func<BuiltInCategory, bool> isCategory = wantedCategory =>
    categoryGiven && categoryId.Equals(new ElementId(wantedCategory));
ElementId schemeId = null;
ElementId familyId = null;

if (kind == null)
{
    refused = string.Format("'{0}' is not a kind of schedule this makes, so NOTHING WAS CREATED. "
        + "Leave it out for a schedule of a category, or use 'sheets' for a sheet list, 'views' "
        + "for a view list, 'key' for a key schedule, 'material takeoff', 'note block', "
        + "'revision schedule' or 'keynote legend'", listKind);
}
else if ((kind == "schedule" || kind == "key" || kind == "takeoff") && !categoryGiven)
{
    refused = kind == "key"
        ? "a key schedule belongs to a category - no category was given, so NOTHING WAS CREATED"
        : kind == "takeoff"
        ? "a material takeoff is a takeoff OF a category - no category was given, so NOTHING WAS CREATED"
        : "a schedule is a schedule OF something - no category was given";
}
else if (schemeWord.Length > 0 && (kind != "schedule" || !isCategory(BuiltInCategory.OST_Areas)))
{
    refused = string.Format("an area scheme ('{0}') belongs to an area schedule only - the category "
        + "Areas with the kind left as schedule - and this asked for {1}. NOTHING WAS CREATED",
        schemeWord, kind == "schedule" ? "a schedule of " + categoryName : "a " + kindWord);
}
else if (familyWord.Length > 0 && kind != "noteblock")
{
    refused = string.Format("a note block family ('{0}') is only for the kind note block, and this "
        + "asked for a {1}. NOTHING WAS CREATED", familyWord, kindWord);
}
else if (kind == "revisions" && categoryGiven && !isCategory(BuiltInCategory.OST_Revisions))
{
    refused = "a revision schedule is a schedule of Revisions, and the category given was "
        + categoryName + ". Send Revisions, or no category, with this kind. NOTHING WAS CREATED";
}
else if (kind == "keynotes" && categoryGiven && !isCategory(BuiltInCategory.OST_KeynoteTags))
{
    refused = "a keynote legend lists Keynote Tags, and the category given was " + categoryName
        + ". Send Keynote Tags, or no category, with this kind. NOTHING WAS CREATED";
}
else if (kind == "noteblock" && categoryGiven && !isCategory(BuiltInCategory.OST_GenericAnnotation))
{
    refused = "a note block lists one Generic Annotation family, and the category given was "
        + categoryName + ". Send Generic Annotations, or no category, and the family in "
        + "noteBlockFamily. NOTHING WAS CREATED";
}

// VERSION 3 - what the kind needs found before anything is created.
if (refused == null && schemeWord.Length > 0)
{
    var schemes = new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().ToList();
    var named = schemes.Where(s => sameText(s.Name, schemeWord)).ToList();
    if (named.Count == 1) schemeId = named[0].Id;
    else
        refused = "no area scheme is called '" + schemeWord + "'"
            + (schemes.Count == 0 ? " - this project has none"
                : ". The schemes are: " + string.Join(", ", schemes.Select(s => "'" + s.Name + "'").ToArray()))
            + ". NOTHING WAS CREATED";
}
if (refused == null && kind == "noteblock")
{
    var valid = new List<Family>();
    try
    {
        foreach (var id in ViewSchedule.GetValidFamiliesForNoteBlock(doc))
        {
            var family = doc.GetElement(id) as Family;
            if (family != null) valid.Add(family);
        }
    }
    catch (Exception) { }
    var list = valid.Count == 0 ? "this project has no Generic Annotation family a note block can list"
        : "the families a note block can list are: "
          + string.Join(", ", valid.Select(f => "'" + f.Name + "'").OrderBy(t => t).ToArray());
    if (familyWord.Length == 0)
        refused = "a note block lists ONE family - name it in noteBlockFamily; " + list + ". NOTHING WAS CREATED";
    else
    {
        var named = valid.Where(f => sameText(f.Name, familyWord)).ToList();
        if (named.Count == 1) familyId = named[0].Id;
        else refused = "'" + familyWord + "' is not a family a note block can list; " + list + ". NOTHING WAS CREATED";
    }
}
if (refused == null && kind == "takeoff")
{
    var takes = false;
    try { takes = ViewSchedule.IsValidCategoryForMaterialTakeoff(categoryId); }
    catch (Exception) { takes = false; }
    if (!takes)
        refused = "Revit does not make a material takeoff of " + categoryName + " - asked before "
            + "trying, because the create call throws rather than explaining. NOTHING WAS CREATED";
}

if (refused != null)
{
    // Refused above - nothing is created.
}
else if (kind == "key")
{
    // REVIT IS ASKED FIRST. The create call throws rather than explaining,
    // and "an exception occurred" is not an answer somebody can act on.
    var allowed = false;
    try { allowed = ViewSchedule.IsValidCategoryForKeySchedule(categoryId); }
    catch { allowed = false; }

    if (!allowed)
        refused = string.Format("Revit does not allow a key schedule on {0}. Asked before trying, "
            + "because the create call throws rather than explaining", categoryName);
    else
    {
        try { schedule = ViewSchedule.CreateKeySchedule(doc, categoryId); }
        catch (Exception failure) { revitSaid = failure.Message; }
        if (schedule == null)
            refused = string.Format("The key schedule for {0} could not be created{1}", categoryName,
                revitSaid == null ? "" : ": " + string.Join(" ", revitSaid.Split()));
    }
}
else
{
    // REVIT THROWS HERE, IT DOES NOT RETURN NULL - asked for a schedule of
    // Views, CreateSchedule raises "categoryId is not a valid category for a
    // regular schedule" (measured 2026-09-13 on `test projject`). Revit's
    // own sentence is kept and passed on, because "not every category can be
    // scheduled" does not say WHICH rule was broken and Revit's does.
    try
    {
        schedule = kind == "sheets" ? ViewSchedule.CreateSheetList(doc)
                 : kind == "views" ? ViewSchedule.CreateViewList(doc)
                 : kind == "takeoff" ? ViewSchedule.CreateMaterialTakeoff(doc, categoryId)
                 : kind == "noteblock" ? ViewSchedule.CreateNoteBlock(doc, familyId)
                 : kind == "revisions" ? ViewSchedule.CreateRevisionSchedule(doc)
                 : kind == "keynotes" ? ViewSchedule.CreateKeynoteLegend(doc)
                 : schemeId != null ? ViewSchedule.CreateSchedule(doc, categoryId, schemeId)
                 : ViewSchedule.CreateSchedule(doc, categoryId);
    }
    catch (Exception failure)
    {
        revitSaid = failure.Message;
    }

    if (schedule == null)
    {
        refused = kind == "schedule"
            ? "Revit declined to create a schedule for that category - not every category can be "
              + "scheduled"
              + (revitSaid == null ? "" : ". Revit said: " + string.Join(" ", revitSaid.Split()))
            : "could not create a " + kindWord
              + (revitSaid == null ? "" : " - " + string.Join(" ", revitSaid.Split()));
    }
    else if (categoryGiven && (kind == "sheets" || kind == "views"))
    {
        findings.Add(string.Format("A {0} lists {1}, not a category, so the category given was not "
            + "used", kindWord, kind == "sheets" ? "the sheets" : "the views"));
    }
}

if (schedule != null)
{
    created = schedule.Id;
    var definition = schedule.Definition;

    // NAME FIRST, and only if one was asked for. Revit's own default ("Duct
    // Schedule") is a real name, and writing blank over it leaves a nameless
    // row in the Project Browser. A name already used by another view leaves
    // Revit's generated name in place - the schedule still exists and can be
    // renamed - and is said (CREATE_SHEET_LIST's rule; version 1 threw).
    if (name.Length > 0)
    {
        try { schedule.Name = name; }
        catch
        {
            findings.Add(string.Format("'{0}' is already the name of another view - the {1} was "
                + "created as '{2}' instead", name, kindWord, schedule.Name));
        }
    }

    // THE KEY PARAMETER IS NAMED HERE. Every key schedule defaults to "Key
    // Name", and three of them called that is how nobody can tell which is
    // which in a dropdown six months later.
    var keyOk = true;
    if (kind == "key" && keyName.Length > 0)
    {
        try { schedule.KeyScheduleParameterName = keyName; }
        catch { keyOk = false; }
    }

    // One pass, because building this list walks the category and every
    // parameter bound to it. First one wins: two schedulable fields can share
    // a display name - an instance parameter and a type parameter of the same
    // name - and adding both gives two identical column headings over
    // different numbers, which is worse than picking one.
    var available = new Dictionary<string, SchedulableField>(StringComparer.OrdinalIgnoreCase);
    var availableNames = new List<string>();
    foreach (var field in definition.GetSchedulableFields())
    {
        if (field == null) continue;
        string fieldName = null;
        try { fieldName = field.GetName(doc); } catch { }
        if (string.IsNullOrEmpty(fieldName)) continue;
        if (available.ContainsKey(fieldName)) continue;
        available[fieldName] = field;
        availableNames.Add(fieldName);
    }

    ScheduleField sortField = null;
    foreach (var key in wanted)
    {
        SchedulableField match;
        if (!available.TryGetValue(key, out match))
        {
            missingFields.Add(key);
            findings.Add(string.Format("'{0}' is not a field a {1} can carry. It can carry: {2}",
                key, kindWord, string.Join(", ", availableNames.ToArray())));
            continue;
        }

        try
        {
            var added = definition.AddField(match);
            fieldsAdded.Add(key);
            if (sortWanted.Length > 0 && sameText(key, sortWanted)) sortField = added;
        }
        catch (Exception ex)
        {
            missingFields.Add(key);
            findings.Add(string.Format("'{0}' could not be added - {1}", key, ex.Message));
        }
    }

    if (sortWanted.Length > 0)
    {
        if (sortField == null)
        {
            findings.Add(string.Format("cannot sort by '{0}' - it is not one of the columns. An "
                + "index in creation order is the usual reason one looks wrong", sortWanted));
        }
        else
        {
            try
            {
                var sort = new ScheduleSortGroupField(sortField.FieldId);
                sort.SortOrder = ScheduleSortOrder.Ascending;
                definition.AddSortGroupField(sort);
            }
            catch (Exception ex)
            {
                findings.Add(string.Format("could not sort by '{0}' - {1}", sortWanted, ex.Message));
            }
        }
    }

    if (kind == "key")
    {
        // READ IT BACK. Revit decides what things end up called.
        var back = doc.GetElement(created) as ViewSchedule;
        var nowCalled = back == null ? name : back.Name;
        findings.Add(string.Format(
            "Key schedule '{0}' created for {1}{2}. It has NO ROWS yet - the definitions are "
            + "what somebody types, and until then the dropdown on an element is empty",
            nowCalled, categoryName,
            keyOk ? "" : string.Format(", and the key parameter could not be named '{0}' - it "
                + "stays whatever Revit called it, which is 'Key Name' on every one of them", keyName)));
    }
    else if (fieldsAdded.Count == 0)
    {
        findings.Add(string.Format("the {0} was created with NO columns - it will print as an empty box",
            kindWord));
    }
}

// VERSION 3 - `madeAs`, a second read of what was made, for every kind.
if (created != null)
{
    var back = doc.GetElement(created) as ViewSchedule;
    if (back == null)
    {
        madeAs = "made, and could not be read back";
    }
    else
    {
        var d = back.Definition;
        var says = new List<string>();
        try { if (d.IsMaterialTakeoff) says.Add("a material takeoff"); } catch (Exception) { }
        try { if (d.IsKeySchedule) says.Add("a key schedule"); } catch (Exception) { }
        try
        {
            if (d.AreaSchemeId != null && d.AreaSchemeId != ElementId.InvalidElementId)
            {
                var scheme = doc.GetElement(d.AreaSchemeId);
                says.Add("area scheme '" + (scheme == null ? "?" : scheme.Name) + "'");
            }
        }
        catch (Exception) { }
        try
        {
            if (d.FamilyId != null && d.FamilyId != ElementId.InvalidElementId)
            {
                var family = doc.GetElement(d.FamilyId);
                says.Add("family '" + (family == null ? "?" : family.Name) + "'");
            }
        }
        catch (Exception) { }
        var backCategory = "no category";
        try
        {
            var c = Category.GetCategory(doc, d.CategoryId);
            if (c != null) backCategory = "'" + c.Name + "'";
        }
        catch (Exception) { }
        var columns = 0;
        try { columns = d.GetFieldCount(); } catch (Exception) { }
        madeAs = string.Format("'{0}' - asked as a {1}; Revit reads it back as a schedule of {2}{3}, with {4} column(s)",
            back.Name, kindWord, backCategory,
            says.Count == 0 ? "" : " (" + string.Join(", ", says.ToArray()) + ")", columns);
        var takeoffBack = false;
        try { takeoffBack = d.IsMaterialTakeoff; } catch (Exception) { }
        if (kind == "takeoff" && !takeoffBack)
            madeAs += ". IT DOES NOT READ BACK AS A MATERIAL TAKEOFF";
    }
}
