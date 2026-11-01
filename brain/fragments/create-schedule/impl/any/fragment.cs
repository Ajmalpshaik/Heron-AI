// NOT STANDALONE. Assumes `doc`, `categoryId`, `fieldNames`, `scheduleName`,
// `kind`, `areaScheme` and `noteBlockFamily` are in scope; leaves `created`,
// `refused`, `missingFields` and `madeAs` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// VERSION 2 (2026-11-01, FRAGMENT-ISSUES row 5b-1363, NEEDS-CHECKING Group
// KF): the OTHER KINDS OF SCHEDULE on Revit's View > Schedules menu, by
// `kind` - a material takeoff, a note block, a revision schedule and a
// keynote legend - and an area schedule of one AREA SCHEME, by `areaScheme`.
// Left out, `kind` is "schedule", `areaScheme` and `noteBlockFamily` are
// "none", and the run is version 1 to the word: ViewSchedule.CreateSchedule
// on the category, named, its fields added.
//
// THE CATEGORY IS STILL REQUIRED, FOR EVERY KIND. The add-in has no value an
// ElementId can take when it is left out (HeronBindingNote.AbsentValue), so
// `categoryId` stays a need the caller supplies. A kind that has a category of
// its own is CHECKED against it rather than ignoring it, so a request that
// meant a revision schedule and arrived with Ducts is refused, not quietly
// turned into something else:
//
//   revision schedule   Revisions             (OST_Revisions)
//   keynote legend      Keynote Tags          (OST_KeynoteTags)
//   note block          Generic Annotations   (OST_GenericAnnotation)
//   material takeoff    any category Revit takes a takeoff of -
//                       IsValidCategoryForMaterialTakeoff, asked first
//   area schedule       Areas (OST_Areas), with `areaScheme` naming the scheme
//
// The host sends that category with that kind; the refusal names it.
//
// FIELDS ARE MATCHED BY THE NAME REVIT SHOWS, which is what GetName(doc)
// returns and what appears in the Fields tab. So "Size" finds the field a
// modeller would pick off that list. It does NOT match internal parameter
// names, and that limit is deliberate: the request comes from somebody
// describing a drawing, not from somebody reading the API. The same rule for
// every kind - a material takeoff's Material: fields are on its own list.
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
//
// `madeAs` IS A SECOND READ of the view made - its name, what Revit says it is
// (a material takeoff, a key schedule, its area scheme, its family) and how
// many columns it has - never the request echoed. What the reference does not
// say a definition reports (whether a note block's FamilyId is the family it
// was made from) is read and printed, and the proof settles it.

ElementId created = null;
string refused = null;
var missingFields = new List<string>();
var madeAs = "";

var wanted = fieldNames;
var name = (scheduleName ?? "").Trim();

{
    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
    Func<string, string> oneLine = text => string.Join(" ", (text ?? "").Split());
    Func<ElementId, string> categoryName = id =>
    {
        try
        {
            var found = Category.GetCategory(doc, id);
            return found == null ? "that category" : "'" + found.Name + "'";
        }
        catch (Exception) { return "that category"; }
    };

    // ---- the kind, in the words of Revit's View > Schedules menu -------------
    var kindWord = squash(kind);
    string want = null;
    if (kindWord.Length == 0 || kindWord == "schedule" || kindWord == "schedulequantities"
        || kindWord == "regular" || kindWord == "regularschedule" || kindWord == "quantities")
        want = "schedule";
    else if (kindWord == "materialtakeoff" || kindWord == "materialtakeoffs" || kindWord == "takeoff")
        want = "material takeoff";
    else if (kindWord == "noteblock" || kindWord == "noteblocks")
        want = "note block";
    else if (kindWord == "revisionschedule" || kindWord == "revisions" || kindWord == "revision")
        want = "revision schedule";
    else if (kindWord == "keynotelegend" || kindWord == "keynotes" || kindWord == "keynote")
        want = "keynote legend";

    var schemeWord = (areaScheme ?? "").Trim();
    var wantScheme = schemeWord.Length > 0 && squash(schemeWord) != "none";
    var familyWord = (noteBlockFamily ?? "").Trim();
    var wantFamily = familyWord.Length > 0 && squash(familyWord) != "none";

    if (want == null)
    {
        refused = "'" + (kind ?? "").Trim() + "' is not a kind of schedule here - the kinds are schedule, "
            + "material takeoff, note block, revision schedule and keynote legend. A key schedule is "
            + "CREATE_KEY_SCHEDULE's, a sheet list or view list CREATE_SHEET_LIST's. Nothing was made";
    }
    else if (categoryId == null || categoryId == ElementId.InvalidElementId)
    {
        refused = "a schedule is a schedule OF something - no category was given";
    }
    else if (wantScheme && (want != "schedule" || !categoryId.Equals(new ElementId(BuiltInCategory.OST_Areas))))
    {
        refused = "an area scheme ('" + schemeWord + "') belongs to an area schedule only - the category "
            + "Areas with the kind left as schedule - and this asked for "
            + (want == "schedule" ? "a schedule of " + categoryName(categoryId) : "a " + want)
            + ". Nothing was made";
    }
    else if (wantFamily && want != "note block")
    {
        refused = "a note block family ('" + familyWord + "') is only for the kind note block, and this asked "
            + "for a " + want + ". Nothing was made";
    }
    else if (want == "revision schedule" && !categoryId.Equals(new ElementId(BuiltInCategory.OST_Revisions)))
    {
        refused = "a revision schedule is a schedule of Revisions, and the category given was "
            + categoryName(categoryId) + ". Send the category Revisions with this kind. Nothing was made";
    }
    else if (want == "keynote legend" && !categoryId.Equals(new ElementId(BuiltInCategory.OST_KeynoteTags)))
    {
        refused = "a keynote legend lists Keynote Tags, and the category given was " + categoryName(categoryId)
            + ". Send the category Keynote Tags with this kind. Nothing was made";
    }
    else if (want == "note block" && !categoryId.Equals(new ElementId(BuiltInCategory.OST_GenericAnnotation)))
    {
        refused = "a note block lists one Generic Annotation family, and the category given was "
            + categoryName(categoryId) + ". Send the category Generic Annotations and the family in "
            + "noteBlockFamily. Nothing was made";
    }

    // ---- what the kind needs found first ----------------------------------
    ElementId schemeId = null;
    ElementId familyId = null;
    if (refused == null && wantScheme)
    {
        var schemes = new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().ToList();
        var named = schemes.Where(s => string.Equals((s.Name ?? "").Trim(), schemeWord, StringComparison.OrdinalIgnoreCase)).ToList();
        if (named.Count == 1) schemeId = named[0].Id;
        else
            refused = "no area scheme is called '" + schemeWord + "'"
                + (schemes.Count == 0 ? " - this project has none"
                    : ". The schemes are: " + string.Join(", ", schemes.Select(s => "'" + s.Name + "'").ToArray()))
                + ". Nothing was made";
    }
    if (refused == null && want == "note block")
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
        if (!wantFamily)
            refused = "a note block lists ONE family - name it in noteBlockFamily; " + list + ". Nothing was made";
        else
        {
            var named = valid.Where(f => string.Equals((f.Name ?? "").Trim(), familyWord, StringComparison.OrdinalIgnoreCase)).ToList();
            if (named.Count == 1) familyId = named[0].Id;
            else refused = "'" + familyWord + "' is not a family a note block can list; " + list + ". Nothing was made";
        }
    }
    if (refused == null && want == "material takeoff")
    {
        var takes = false;
        try { takes = ViewSchedule.IsValidCategoryForMaterialTakeoff(categoryId); }
        catch (Exception) { takes = false; }
        if (!takes)
            refused = "Revit does not make a material takeoff of " + categoryName(categoryId)
                + " - asked before trying, because the create call throws rather than explaining. Nothing was made";
    }

    if (refused == null)
    {
        // REVIT THROWS HERE, IT DOES NOT RETURN NULL, and the `schedule == null`
        // test below could therefore never run. Asked for a schedule of Views it
        // raises ArgumentException - "categoryId is not a valid category for a
        // regular schedule" - so version 1 CRASHED at exactly the point it was
        // written to refuse politely. Measured 2026-09-13 on `test projject`:
        // categoryId Ducts created one, categoryId Views threw. Every kind's
        // create call is caught the same way.
        //
        // Revit's own sentence is kept and passed on, because "not every
        // category can be scheduled" does not say WHICH rule was broken and
        // Revit's does.
        ViewSchedule schedule = null;
        string revitSaid = null;
        try
        {
            if (want == "material takeoff") schedule = ViewSchedule.CreateMaterialTakeoff(doc, categoryId);
            else if (want == "note block") schedule = ViewSchedule.CreateNoteBlock(doc, familyId);
            else if (want == "revision schedule") schedule = ViewSchedule.CreateRevisionSchedule(doc);
            else if (want == "keynote legend") schedule = ViewSchedule.CreateKeynoteLegend(doc);
            else if (schemeId != null) schedule = ViewSchedule.CreateSchedule(doc, categoryId, schemeId);
            else schedule = ViewSchedule.CreateSchedule(doc, categoryId);
        }
        catch (Exception failure)
        {
            revitSaid = failure.Message;
        }

        if (schedule == null)
        {
            refused = (want == "schedule"
                    ? "Revit declined to create a schedule for that category - not every category can be scheduled"
                    : "Revit declined to create the " + want)
                + (revitSaid == null ? "" : ". Revit said: " + oneLine(revitSaid));
        }
        else
        {
            var definition = schedule.Definition;

            // Name first, and only if one was asked for. Revit's own default
            // ("Duct Schedule") is a real name, and writing blank over it leaves a
            // nameless row in the Project Browser.
            if (name.Length > 0) schedule.Name = name;

            // One pass, because building this list walks the category and every
            // parameter bound to it.
            var available = new Dictionary<string, SchedulableField>();

            foreach (var field in definition.GetSchedulableFields())
            {
                if (field == null) continue;

                var fieldName = field.GetName(doc);
                if (string.IsNullOrEmpty(fieldName)) continue;

                // First one wins. Two schedulable fields can share a display name -
                // an instance parameter and a type parameter of the same name - and
                // adding both gives two identical column headings over different
                // numbers, which is worse than picking one.
                if (!available.ContainsKey(fieldName)) available[fieldName] = field;
            }

            if (wanted != null)
            {
                foreach (var requested in wanted)
                {
                    if (string.IsNullOrEmpty(requested)) continue;

                    var key = requested.Trim();

                    if (!available.ContainsKey(key)) { missingFields.Add(key); continue; }

                    definition.AddField(available[key]);
                }
            }

            created = schedule.Id;

            // ---- read back: what the view made says it is ------------------
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
                var columns = 0;
                try { columns = d.GetFieldCount(); } catch (Exception) { }
                madeAs = string.Format("'{0}' - asked as a {1}; Revit reads it back as a schedule of {2}{3}, with {4} column(s)",
                    back.Name, want, categoryName(d.CategoryId),
                    says.Count == 0 ? "" : " (" + string.Join(", ", says.ToArray()) + ")", columns);
                if (want == "material takeoff" && !d.IsMaterialTakeoff)
                    madeAs += ". IT DOES NOT READ BACK AS A MATERIAL TAKEOFF";
            }
        }
    }
}
