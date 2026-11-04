// NOT STANDALONE. Assumes `doc`, `category`, `fieldNames` and `scheduleName`
// are in scope, and leaves `created`, `scheduleCalled`, `addedFields`,
// `missingFields`, `fieldsAvailable`, `validCategories`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. One
// takeoff is one undo entry, and the caller owns it.
//
// REVIT'S VIEW > SCHEDULES > MATERIAL TAKEOFF. A schedule VIEW in the Project
// Browser, one row per material per element, that updates itself and goes on
// a sheet - made by ViewSchedule.CreateMaterialTakeoff(Document, ElementId),
// present and unchanged 2020 to 2027 (tools/api-surface --members,
// 2026-11-01). It is NOT REPORT_MATERIAL_TAKEOFF, which reads the same numbers
// into the reply and makes nothing, and NOT CREATE_SCHEDULE, whose
// CreateSchedule makes a schedule of elements with no material rows at all.
//
// THE CATEGORY IS THE MODELLER'S AND IS NEVER GUESSED (D-33). It is matched by
// the name Revit shows - "Walls", "Floors" - against the categories Revit
// itself says can have a takeoff (GetValidCategoriesForMaterialTakeoff), and
// nothing else. Left out, or a name not on that list: NOTHING IS MADE and the
// list comes back in `validCategories`. "Multi-Category" - Revit's own entry
// at the top of that dialog - is taken only when it is NAMED, and only if
// Revit says it is valid; it is never the answer to a category left out.
//
// FIELDS ARE MATCHED BY THE NAME REVIT SHOWS in the Fields tab, which is what
// SchedulableField.GetName(doc) returns - so the material's own columns are
// found under the names Revit gives them, read from Revit and not typed here.
// A name is matched exactly first; failing that, ignoring case, spaces and
// colons, but ONLY when exactly one field reads that way - so "material area"
// finds a field Revit calls "Material: Area" and is never a pick between two.
//
// A FIELD THAT DOES NOT EXIST IS REPORTED, NOT GUESSED AT. Every name that
// found nothing, or that Revit refused to add, comes back in `missingFields`
// with the material fields there are in `fieldsAvailable`. And a name that
// found the ELEMENT'S own field - "Area" is the wall's area - while a material
// field of that name also exists is ADDED AS ASKED and said in `findings`,
// because the two print different numbers and the modeller chose the words.
//
// THE TAKEOFF IS STILL MADE WHEN SOME FIELDS ARE MISSING - create-schedule's
// rule, a column can be added later. But when NOT ONE field could be added the
// takeoff just made is deleted again, inside this same undo, and the call
// refused: a takeoff with no columns looks finished in the Project Browser and
// shows nothing. Nothing else is ever deleted here.
//
// THE TAKEOFF IS READ BACK - that Revit made a material takeoff, of the
// category asked for - and a wrong one is deleted again and refused.

ElementId created = null;
string scheduleCalled = null;
var addedFields = new List<string>();
var missingFields = new List<string>();
var fieldsAvailable = new List<string>();
var validCategories = new List<string>();
string refused = null;
var findings = new List<string>();

// Lower case, no spaces and no colons: "Material: Area" and "material area"
// read the same, and nothing else is folded.
Func<string, string> squash = text =>
{
    var kept = new System.Text.StringBuilder();
    foreach (var c in (text ?? ""))
    {
        if (char.IsWhiteSpace(c) || c == ':') continue;
        kept.Append(char.ToLowerInvariant(c));
    }
    return kept.ToString();
};

// Every category Revit allows a material takeoff of, by the name Revit shows.
var takeoffCategories = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
ICollection<ElementId> allowedIds = null;
try { allowedIds = ViewSchedule.GetValidCategoriesForMaterialTakeoff(); }
catch (Exception failure) { findings.Add("Revit did not list the takeoff categories: " + failure.Message); }

if (allowedIds != null)
{
    foreach (var id in allowedIds)
    {
        if (id == null || id == ElementId.InvalidElementId) continue;
        Category known = null;
        try { known = Category.GetCategory(doc, id); } catch { known = null; }
        if (known == null) continue;
        string shown;
        try { shown = known.Name; } catch { continue; }
        if (string.IsNullOrEmpty(shown) || takeoffCategories.ContainsKey(shown)) continue;
        takeoffCategories[shown] = id;
    }
}

var multiAllowed = false;
try { multiAllowed = ViewSchedule.IsValidCategoryForMaterialTakeoff(ElementId.InvalidElementId); }
catch { multiAllowed = false; }

// Handed back only when the category is what was refused - the list is long,
// and on a takeoff that was made it answers nothing.
var allowedShown = new List<string>(takeoffCategories.Keys);
allowedShown.Sort(StringComparer.OrdinalIgnoreCase);
if (multiAllowed) allowedShown.Insert(0, "Multi-Category");

var asked = (category ?? "").Trim();
var askedBare = asked.Trim('<', '>', ' ');
var name = (scheduleName ?? "").Trim();
if (string.Equals(name, "none", StringComparison.OrdinalIgnoreCase)) name = "";

ElementId categoryId = null;
var isMulti = false;
var categoryShown = askedBare;

if (asked.Length == 0 || string.Equals(asked, "none", StringComparison.OrdinalIgnoreCase))
{
    refused = "No category was named, and a material takeoff is a takeoff OF one category - "
            + "it is never chosen for you. Nothing was made. The categories Revit allows a "
            + "material takeoff of are in validCategories.";
    validCategories.AddRange(allowedShown);
}
else if (string.Equals(askedBare, "Multi-Category", StringComparison.OrdinalIgnoreCase)
         || string.Equals(askedBare, "Multi Category", StringComparison.OrdinalIgnoreCase))
{
    if (multiAllowed)
    {
        categoryId = ElementId.InvalidElementId;
        isMulti = true;
        categoryShown = "Multi-Category";
    }
    else
    {
        refused = "Revit does not allow a Multi-Category material takeoff in this model. "
                + "Nothing was made. The categories it allows are in validCategories.";
        validCategories.AddRange(allowedShown);
    }
}
else if (takeoffCategories.ContainsKey(askedBare))
{
    categoryId = takeoffCategories[askedBare];
    foreach (var pair in takeoffCategories)
        if (pair.Value == categoryId) { categoryShown = pair.Key; break; }
}
else
{
    refused = "\"" + asked + "\" is not a category Revit allows a material takeoff of - "
            + "only categories whose elements carry materials can have one. Nothing was "
            + "made. The categories it allows are in validCategories.";
    validCategories.AddRange(allowedShown);
}

var wanted = new List<string>();
if (refused == null && fieldNames != null)
{
    foreach (var requested in fieldNames)
    {
        var key = (requested ?? "").Trim();
        if (key.Length == 0) continue;
        if (!wanted.Contains(key)) wanted.Add(key);
    }
}

if (refused == null && wanted.Count == 0)
{
    refused = "No field was named, and the columns of a takeoff are never chosen for you. "
            + "Nothing was made.";
}

// A NAME ALREADY TAKEN IS REFUSED BEFORE ANYTHING IS MADE. Revit throws on a
// second schedule of one name, and refusing here leaves nothing to clean up.
if (refused == null && name.Length > 0)
{
    foreach (var existing in new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)))
    {
        string taken;
        try { taken = existing.Name; } catch { continue; }
        if (string.Equals(taken, name, StringComparison.OrdinalIgnoreCase))
        {
            refused = "A schedule called \"" + taken + "\" is already in the project. Nothing "
                    + "was made - name the takeoff something else.";
            break;
        }
    }
}

if (refused == null)
{
    ViewSchedule schedule = null;
    string revitSaid = null;
    try { schedule = ViewSchedule.CreateMaterialTakeoff(doc, categoryId); }
    catch (Exception failure) { revitSaid = failure.Message; }

    if (schedule == null)
    {
        refused = "Revit declined to make a material takeoff of " + categoryShown
                + (revitSaid == null ? "" : ". Revit said: " + string.Join(" ", revitSaid.Split()))
                + ". Nothing was made.";
    }
    else
    {
        var definition = schedule.Definition;

        // READ BACK: a material takeoff, of the category asked for.
        var isTakeoff = false;
        ElementId madeOf = null;
        try { isTakeoff = definition.IsMaterialTakeoff; madeOf = definition.CategoryId; }
        catch { isTakeoff = false; }

        var rightCategory = isMulti
            ? (madeOf == null || madeOf == ElementId.InvalidElementId)
            : (madeOf != null && madeOf == categoryId);

        string nameProblem = null;
        if (isTakeoff && rightCategory && name.Length > 0)
        {
            // Only when one was asked for: Revit's own default ("Wall Material
            // Takeoff") is a real name, and blank over it is a nameless row.
            try { schedule.Name = name; }
            catch (Exception failure) { nameProblem = string.Join(" ", failure.Message.Split()); }
        }

        if (!isTakeoff || !rightCategory)
        {
            refused = "Revit made a schedule that did not read back as a material takeoff of "
                    + categoryShown + ", so it was deleted again. Nothing was kept.";
        }
        else if (nameProblem != null)
        {
            refused = "Revit would not name the takeoff \"" + name + "\" - " + nameProblem
                    + " - so it was deleted again. Nothing was kept.";
        }

        if (refused == null)
        {
            // One pass over the field list: it is built from the category and
            // every parameter bound to it, and it is not cheap.
            var byName = new Dictionary<string, SchedulableField>(StringComparer.Ordinal);
            var bySquash = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var materialNames = new List<string>();

            foreach (var field in definition.GetSchedulableFields())
            {
                if (field == null) continue;
                string fieldName;
                try { fieldName = field.GetName(doc); } catch { continue; }
                if (string.IsNullOrEmpty(fieldName)) continue;

                // First one wins - an instance and a type parameter can share a
                // display name, and two identical headings over different
                // numbers is worse than one (create-schedule's rule).
                if (byName.ContainsKey(fieldName)) continue;
                byName[fieldName] = field;

                var folded = squash(fieldName);
                if (!bySquash.ContainsKey(folded)) bySquash[folded] = new List<string>();
                bySquash[folded].Add(fieldName);

                if (field.FieldType == ScheduleFieldType.Material
                    || field.FieldType == ScheduleFieldType.MaterialQuantity)
                    materialNames.Add(fieldName);
            }

            var addedKeys = new List<string>();

            foreach (var key in wanted)
            {
                string found = null;
                if (byName.ContainsKey(key)) found = key;
                else
                {
                    foreach (var fieldName in byName.Keys)
                        if (string.Equals(fieldName, key, StringComparison.OrdinalIgnoreCase))
                        { found = found == null ? fieldName : ""; }
                    if (found == "") found = null;
                    if (found == null)
                    {
                        var folded = squash(key);
                        if (bySquash.ContainsKey(folded) && bySquash[folded].Count == 1)
                            found = bySquash[folded][0];
                    }
                }

                if (found == null) { missingFields.Add(key); continue; }

                if (addedKeys.Contains(found))
                {
                    findings.Add("\"" + key + "\" is the field \"" + found
                               + "\", already added - one column, not two.");
                    continue;
                }

                try
                {
                    definition.AddField(byName[found]);
                    addedKeys.Add(found);
                    addedFields.Add(found);
                }
                catch (Exception failure)
                {
                    missingFields.Add(key);
                    findings.Add("Revit would not add \"" + found + "\": "
                               + string.Join(" ", failure.Message.Split()));
                    continue;
                }

                // The element's own field, where a material field of that name
                // exists too - added as asked, and said.
                var field = byName[found];
                if (field.FieldType != ScheduleFieldType.Material
                    && field.FieldType != ScheduleFieldType.MaterialQuantity)
                {
                    foreach (var materialName in materialNames)
                    {
                        if (squash(materialName) == "material" + squash(found))
                        {
                            findings.Add("\"" + found + "\" was added as the element's own "
                                       + found + ", not the material's. The material's is \""
                                       + materialName + "\" - ask for it by that name to "
                                       + "have it.");
                            break;
                        }
                    }
                }
            }

            if (missingFields.Count > 0) fieldsAvailable.AddRange(materialNames);

            if (addedFields.Count == 0)
            {
                refused = "Not one of the fields asked for is one this takeoff has, so the "
                        + "takeoff was deleted again rather than left with no columns. "
                        + "Nothing was kept. The material fields it offers are in "
                        + "fieldsAvailable, and the category's own fields are offered too.";
            }
            else
            {
                created = schedule.Id;
                try { scheduleCalled = schedule.Name; } catch { scheduleCalled = name; }
                findings.Add("Material takeoff \"" + scheduleCalled + "\" of " + categoryShown
                           + " made, with " + addedFields.Count + " field(s): "
                           + string.Join(", ", addedFields)
                           + (missingFields.Count == 0 ? "." : ". NOT added: "
                               + string.Join(", ", missingFields) + "."));
            }
        }

        if (refused != null)
        {
            try { doc.Delete(schedule.Id); }
            catch (Exception failure)
            {
                findings.Add("The takeoff could not be deleted again: " + failure.Message
                           + " - Undo takes it back.");
            }
        }
    }
}
