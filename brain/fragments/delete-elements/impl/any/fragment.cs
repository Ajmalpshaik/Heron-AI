// NOT STANDALONE. Assumes `doc`, `elements` and `expectedCount` are in scope;
// leaves `deleted`, `askedFor`, `alsoWent`, `refused`, `refusedWhy`,
// `attachedBefore`, `detachedWalls`, `wallsNote`, `deletedByCategory`,
// `deletedWhat` and `countChecked` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE RETURNED SET IS THE ANSWER, NOT THE INPUT COUNT. This is the whole
// fragment.
//
// Revit deletes dependents without asking: a wall takes its doors and windows,
// a duct takes its fittings and insulation, a level can take everything on it.
// `Document.Delete` hands back the ids that ACTUALLY went, and that set is
// routinely larger than the one passed in. Reporting "deleted 4" from the input
// list would be the defining failure of this project - the number ASKED FOR
// reported as the number that HAPPENED - except here it under-reports
// destruction, which is the worst direction for it to be wrong in.
//
// `alsoWent` IS THE NUMBER SOMEBODY NEEDS TO SEE. Four ducts asked for and
// nineteen elements gone is not a detail; it is the difference between a tidy-up
// and an accident, and it is invisible unless it is counted here.
//
// A DELETION THAT REVIT REFUSES DOES NOT ABANDON THE BATCH. Some elements
// cannot be deleted - a pinned element, a member of an unmodifiable group, an
// element owned by another user. Each id is submitted so a refusal is recorded
// and stepped over. Slower than one bulk call and worth it: a batch that throws
// at the eleventh of two hundred has already deleted ten and reports nothing.
//
// VERSION 2 (2026-11-16, row 5b-1674) - A WALL ATTACHED TO WHAT GOES IS NAMED.
//
//   Deleting a roof, a floor or a ceiling DETACHES every wall attached to it.
//   The wall is not deleted, so it is in nothing Document.Delete returns, and
//   its top drops back to its own height without a word - Revit's only sign
//   is "Walls 19 modified" (issue #852). On Revit 2020 to 2024 Heron cannot
//   attach a wall again, so the modeller has to know before keeping it.
//
//   BEFORE THE DELETE: every wall whose top or base is attached - the wall's
//   own read-only "top is attached" / "base is attached", on every release -
//   and whose plan extent overlaps a roof, floor or ceiling being deleted is
//   named in `attachedBefore`. That is a likely list, not a certain one: a
//   wall can be attached to something else above the same plan.
//
//   AFTER IT: the model is rebuilt and each of those walls read again. One
//   attached before and not after is in `detachedWalls` - measured, not
//   inferred - and `wallsNote` says plainly what that means and, on 2020 to
//   2024, that Heron cannot attach them again. A wall that went with the
//   delete is in `deleted`, not here. When nothing attached is near what is
//   deleted, nothing is rebuilt and nothing here changes how a delete runs.
//
//   A REBUILD REVIT CANNOT FINISH IS THROWN, never swallowed: Revit's
//   reference says the model must not be read after it, so the whole call is
//   rolled back and nothing is deleted.
//
// VERSION 3 (2026-11-23, row 5b-1996) - WHAT WENT IS NAMED, READ BEFORE IT WENT.
//
//   Version 2 said HOW MANY more went (`alsoWent`) and gave their ids, and
//   nothing else. Measured on Revit 2020, family documents: one family type
//   asked for, 2 deleted, and nobody could say afterwards what id 9021 was;
//   21 forms asked for, 241 deleted, and that every extra was a form's own
//   sketch, sketch plane and sketch lines could only be worked out by hand
//   from id ranges. A deleted element cannot be read once it has gone, so the
//   run record, too, held only `{"change": "deleted", "id": "9021"}`.
//
//   SO EACH DELETE IS FIRST TRIED AND ROLLED BACK. For every element asked
//   for, a SubTransaction deletes it - Revit's own answer to "what would go"
//   - and rolls back; every id that delete returned is read while it is still
//   in the model (category, name, kind); then the real delete runs. Nothing is
//   guessed from a dependents list: the trial IS the delete, undone.
//
//   `deletedWhat` names every deleted id - "category | name | why it went" -
//   and, being a dictionary, travels whole into the reply and the run record.
//   `deletedByCategory` is the sentence: what was asked for and what also
//   went, counted by category. A FORM's own sketch, sketch plane and the
//   lines on that plane that went with it are counted as "their sketches", so
//   241 reads as "21 form(s) + their sketches".
//
//   NEVER A CATEGORY IT COULD NOT READ. An id the trial did not return, or
//   that Revit gave no element for, is "NOT READ" and counted so. A trial
//   Revit refuses to roll back is THROWN, never swallowed - the whole call is
//   rolled back and nothing is deleted. A trial that cannot START (no open
//   transaction to nest in) leaves only that element's ids NOT READ; the
//   delete itself runs exactly as it did in version 2.
//
// VERSION 4 (2026-11-26, row 5b-1990) - A COUNT THE YES NAMED IS CHECKED
// BEFORE ANYTHING GOES.
//
//   Measured on Revit 2020, a real job: the method's yes named 36 elements,
//   Revit's delete took 62, and the method set `deleted` against what it had
//   named only AFTER the delete - the owner kept it, but the yes had covered
//   less than went. With `expectedCount` given, the whole request is first
//   deleted inside ONE SubTransaction, in the same order as the real loop,
//   and rolled back: that trial's count is Revit's own. A different count
//   deletes NOTHING - the call is thrown with both numbers, Revit's list by
//   category and every id beyond those asked for, so the modeller is asked
//   again with what really goes. The same count: `countChecked` says so and
//   the delete runs exactly as version 3. Left out (-1), nothing is tried
//   twice and nothing here changes how a delete runs.
//
// VERSION 5 (2026-11-26, row 5b-2060) - THE COUNT STOP IS A REFUSAL, NOT A
// THROWN ERROR.
//
//   Measured on Revit 2020, 78 rooms that Revit's delete takes as 81:
//   expectedCount 80 and 0 stopped with the right words and nothing deleted,
//   but THROWN, so the caller got `fragment_threw` - and a thrown negative
//   cannot sign a proof. Now both stops - a count that differs, and a trial
//   that cannot start - put the same words in `refusedWhy`, every id asked for
//   in `refused` (none of them went), and delete NOTHING: the delete loop
//   below is handed no ids, so `deleted` stays empty. A trial Revit cannot
//   put back, and a rebuild it cannot finish, are still THROWN: those are
//   failures, never a refusal, and the whole call is rolled back.

var askedFor = elements.Count;
var deleted = new List<ElementId>();
var refused = new List<ElementId>();
// VERSION 5: why the count check stopped the delete, in the modeller's words.
// Empty unless it stopped.
var refusedWhy = "";
var attachedBefore = "";
var detachedWalls = new List<ElementId>();
var wallsNote = "";

// ---- VERSION 3: WHAT WENT, READ BEFORE IT WENT -----------------------------
// deletedWhat: id -> "category | name | why it went". readBeforeDelete: one row
// per deleted id - { category, group (asked / sketch / other / not read),
// sketch part, the form it belongs to } - for the sentence at the end.
var deletedWhat = new Dictionary<string, string>();
var deletedByCategory = "";
var readBeforeDelete = new List<string[]>();

// ---- VERSION 2: THE WALLS ATTACHED TO A ROOF, FLOOR OR CEILING BEING DELETED
var askedIds = new List<ElementId>();
// VERSION 4: which of them are forms, read now - the loops below go by id and
// never read an element object after a trial delete of it was rolled back.
var askedForms = new HashSet<ElementId>();
var hostBoxes = new List<BoundingBoxXYZ>();
foreach (var element in elements)
{
    if (element == null) continue;
    askedIds.Add(element.Id);
    if (element is GenericForm) askedForms.Add(element.Id);
    if (!(element is RoofBase || element is Floor || element is Ceiling)) continue;
    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch { box = null; }
    if (box != null) hostBoxes.Add(box);
}

Func<Wall, BuiltInParameter, bool> isAttached = (wall, which) =>
{
    try
    {
        var flag = wall.get_Parameter(which);
        return flag != null && flag.HasValue && flag.StorageType == StorageType.Integer && flag.AsInteger() == 1;
    }
    catch { return false; }
};

// Each candidate: the wall, and which ends were attached before.
var candidates = new List<Tuple<Wall, bool, bool>>();
if (hostBoxes.Count > 0)
{
    foreach (Wall wall in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
    {
        if (askedIds.Contains(wall.Id)) continue;
        var topWas = isAttached(wall, BuiltInParameter.WALL_TOP_IS_ATTACHED);
        var baseWas = isAttached(wall, BuiltInParameter.WALL_BOTTOM_IS_ATTACHED);
        if (!topWas && !baseWas) continue;

        BoundingBoxXYZ wallBox = null;
        try { wallBox = wall.get_BoundingBox(null); } catch { wallBox = null; }
        if (wallBox == null) continue;

        var under = false;
        foreach (var hostBox in hostBoxes)
        {
            if (wallBox.Max.X < hostBox.Min.X || wallBox.Min.X > hostBox.Max.X) continue;
            if (wallBox.Max.Y < hostBox.Min.Y || wallBox.Min.Y > hostBox.Max.Y) continue;
            under = true;
            break;
        }
        if (!under) continue;

        candidates.Add(Tuple.Create(wall, topWas, baseWas));
        attachedBefore += (attachedBefore.Length == 0 ? "" : ", ")
            + string.Format("wall {0} ({1})", wall.Id,
                topWas && baseWas ? "top and base attached" : topWas ? "top attached" : "base attached");
    }
    if (attachedBefore.Length > 0)
        attachedBefore = "Attached, and in plan under a roof, floor or ceiling being deleted: " + attachedBefore;
}

// ---- VERSION 4: THE COUNT THE YES NAMED, CHECKED BEFORE ANYTHING GOES ------
// With `expectedCount` given, the whole request is deleted inside ONE
// SubTransaction - the same deletes, in the same order, as the loop below -
// and rolled back. What that trial returned is Revit's own count of what goes.
// A different count from the one the yes named deletes NOTHING: the call
// stops, and `refusedWhy` carries Revit's list so the question can be asked
// again. The elements are read AFTER the rollback, when they are back, and
// only by id: an element object is never touched after its delete was rolled
// back, here or in the loop below.
//
// "NO COUNT" IS A NEGATIVE NUMBER, NEVER 0. A yes can name 0 - nothing goes -
// and that 0 is checked like any other count, so it stops if Revit would take
// anything. Left out, `expectedCount` is -1 and nothing is tried twice.
//
// VERSION 5: A STOP IS A REFUSAL (row 5b-2060). Version 4 threw both stops
// below, so the caller saw `fragment_threw`. Now each one writes its words to
// `refusedWhy`, puts every id asked for in `refused`, and the delete loop is
// handed no ids - nothing is deleted. The words are version 4's own.
var countChecked = "";
if (expectedCount >= 0)
{
    var wouldGo = new List<ElementId>();
    var wouldGoSeen = new HashSet<ElementId>();
    var wouldRefuse = new List<string>();
    var wholeStarted = false;
    using (var whole = new SubTransaction(doc))
    {
        try { whole.Start(); wholeStarted = true; } catch (Exception) { wholeStarted = false; }
        if (wholeStarted)
        {
            foreach (var askedId in askedIds)
            {
                ICollection<ElementId> wouldTake = null;
                try { wouldTake = doc.Delete(askedId); } catch (Exception) { wouldTake = null; }
                // Refused, or already gone with one asked before it - said in the
                // message, never silently left out of the count.
                if (wouldTake == null || wouldTake.Count == 0)
                {
                    if (!wouldGoSeen.Contains(askedId)) wouldRefuse.Add(askedId.ToString());
                    continue;
                }
                foreach (var goingId in wouldTake)
                    if (wouldGoSeen.Add(goingId)) wouldGo.Add(goingId);
            }

            // A trial Revit cannot put back is a FAILURE, not a refusal: still
            // thrown, so the whole call is rolled back.
            try
            {
                whole.RollBack();
            }
            catch (Exception notBack)
            {
                throw new InvalidOperationException(
                    "Revit could not put back the trial delete used to check the count the yes named - "
                    + notBack.Message + ". Nothing this call did is kept");
            }
        }
    }

    if (!wholeStarted)
    {
        // A count that cannot be checked is never taken as met.
        refusedWhy = "STOPPED BEFORE THE DELETE - expectedCount " + expectedCount + " was given, and Revit "
            + "could not start the trial that counts what this delete takes, so that count cannot be "
            + "checked. Nothing has been deleted";
    }
    else if (wouldGo.Count != expectedCount)
    {
        var categories = new Dictionary<string, int>();
        var alsoIds = new List<string>();
        foreach (var goingId in wouldGo)
        {
            string category = null;
            try
            {
                var going = doc.GetElement(goingId);
                category = going == null ? "NOT READ"
                    : going.Category != null ? going.Category.Name : "(no category) " + going.GetType().Name;
            }
            catch (Exception) { category = "NOT READ"; }
            int seen;
            categories.TryGetValue(category, out seen);
            categories[category] = seen + 1;
            if (!askedIds.Contains(goingId)) alsoIds.Add(goingId.ToString());
        }
        var byCategory = string.Join(", ", categories
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key + " " + pair.Value)
            .ToArray());
        refusedWhy = string.Format(
            "STOPPED BEFORE THE DELETE - NOTHING HAS BEEN DELETED. The yes named {0} element(s); Revit's "
            + "own delete of the {1} asked for takes {2}{3}. {4} more than the {1} asked for would go{5}.{6} "
            + "Name what really goes to the modeller and ask again, with expectedCount={2}",
            expectedCount, askedIds.Count, wouldGo.Count,
            byCategory.Length > 0 ? " - by category: " + byCategory : "",
            alsoIds.Count,
            alsoIds.Count > 0 ? ": " + string.Join(", ", alsoIds.ToArray()) : "",
            wouldRefuse.Count > 0
                ? " " + wouldRefuse.Count + " asked for would NOT go - Revit refused them: "
                  + string.Join(", ", wouldRefuse.ToArray()) + "."
                : "");
    }
    else
    {
        countChecked = string.Format(
            "Revit's own trial of this delete takes {0} element(s) - the count the yes named - checked "
            + "before anything was deleted.", wouldGo.Count);
    }
}

// VERSION 5: STOPPED, SO NOTHING GOES. Every id asked for is in `refused` -
// none of them was deleted - and the loop below is handed none of them, so
// `deleted`, `deletedWhat` and `alsoWent` stay empty and no wall is read
// again. Not stopped: the loop runs over every id asked for, as version 4.
if (refusedWhy.Length > 0) refused.AddRange(askedIds);
var deleteNow = refusedWhy.Length > 0 ? new List<ElementId>() : askedIds;

foreach (var id in deleteNow)
{
    // VERSION 3: THE TRIAL. Revit's own delete of this element, rolled back,
    // so everything it takes can be read while it is still there.
    var readFirst = new Dictionary<string, string[]>();
    var planesGoing = new HashSet<string>();
    ICollection<ElementId> willGo = null;
    using (var trial = new SubTransaction(doc))
    {
        var trialStarted = false;
        try { trial.Start(); trialStarted = true; } catch (Exception) { trialStarted = false; }
        if (trialStarted)
        {
            try { willGo = doc.Delete(id); } catch (Exception) { willGo = null; }
            try
            {
                trial.RollBack();
            }
            catch (Exception notBack)
            {
                throw new InvalidOperationException(
                    "Revit could not put back the trial delete used to read what a delete takes - "
                    + notBack.Message + ". Nothing this call did is kept");
            }
        }
    }
    if (willGo != null)
    {
        foreach (var goingId in willGo)
        {
            Element going = null;
            try { going = doc.GetElement(goingId); } catch (Exception) { going = null; }
            if (going == null) continue;

            var kind = going.GetType().Name;
            string category = null;
            try { category = going.Category != null ? going.Category.Name : null; } catch (Exception) { category = null; }
            string name = null;
            try { name = going.Name; } catch (Exception) { name = null; }
            if (string.IsNullOrEmpty(name)) name = kind;

            // Which part of a sketch it is, if any - read, never inferred from
            // an id. A line counts as a sketch line only when its plane is one
            // that goes with this same delete (checked below, once all are read).
            var part = "";
            try
            {
                if (going is Sketch)
                {
                    part = "sketch";
                    var plane = ((Sketch)going).SketchPlane;
                    if (plane != null) planesGoing.Add(plane.Id.ToString());
                }
                else if (going is SketchPlane)
                {
                    part = "sketch plane";
                    planesGoing.Add(goingId.ToString());
                }
                else if (going is CurveElement)
                {
                    var plane = ((CurveElement)going).SketchPlane;
                    if (plane != null) part = "line on " + plane.Id;
                }
            }
            catch (Exception) { part = ""; }

            readFirst[goingId.ToString()] = new[]
            {
                string.IsNullOrEmpty(category) ? "(no category) " + kind : category, name, part
            };
        }
    }
    var isForm = askedForms.Contains(id);

    try
    {
        var went = doc.Delete(id);

        // An EMPTY return is not a success. Revit accepted the call and removed
        // nothing, which is the silent-no-op shape this repository has already
        // been bitten by on the move path. Recorded as refused, not counted.
        if (went == null || went.Count == 0)
        {
            refused.Add(id);
            continue;
        }

        foreach (var gone in went)
        {
            if (deleted.Contains(gone)) continue;
            deleted.Add(gone);

            // VERSION 3: what it was, as the trial read it.
            var key = gone.ToString();
            var wasAsked = askedIds.Contains(gone);
            string[] seen;
            if (!readFirst.TryGetValue(key, out seen))
            {
                deletedWhat[key] = "NOT READ - Revit gave nothing to read for this id before the delete | "
                    + (wasAsked ? "asked for" : "went with " + id);
                readBeforeDelete.Add(new[] { "(not read)", wasAsked ? "asked" : "not read", "", "" });
                continue;
            }

            var sketchPart = "";
            if (isForm && !wasAsked)
            {
                if (seen[2] == "sketch" || seen[2] == "sketch plane") sketchPart = seen[2];
                else if (seen[2].StartsWith("line on ") && planesGoing.Contains(seen[2].Substring(8)))
                    sketchPart = "sketch line";
            }

            var why = wasAsked ? "asked for"
                : sketchPart.Length > 0 ? sketchPart + " of form " + id
                : "went with " + id;
            deletedWhat[key] = seen[0] + " | " + seen[1] + " | " + why;
            readBeforeDelete.Add(new[]
            {
                seen[0], wasAsked ? "asked" : sketchPart.Length > 0 ? "sketch" : "other", sketchPart, id.ToString()
            });
        }
    }
    catch (Exception)
    {
        refused.Add(id);
    }
}

// What went beyond what was named. Never below zero: an element can be taken
// as a dependent of an earlier deletion and then be missing when its own turn
// comes, so the two counts are not guaranteed to be ordered.
var alsoWent = deleted.Count - askedFor;
if (alsoWent < 0) alsoWent = 0;

// ---- VERSION 3: THE SENTENCE - what was asked for and what also went -------
if (readBeforeDelete.Count > 0)
{
    Func<IEnumerable<string[]>, string> byCategory = rows => string.Join(", ", rows
        .GroupBy(row => row[0])
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.Key + " " + group.Count())
        .ToArray());

    var askedRows = readBeforeDelete.Where(row => row[1] == "asked").ToList();
    var sketchRows = readBeforeDelete.Where(row => row[1] == "sketch").ToList();
    var otherRows = readBeforeDelete.Where(row => row[1] == "other").ToList();
    var notRead = readBeforeDelete.Count(row => row[1] == "not read");
    var forms = sketchRows.Select(row => row[3]).Distinct().Count();

    var extras = new List<string>();
    if (sketchRows.Count > 0)
    {
        var parts = new List<string>();
        foreach (var word in new[] { "sketch", "sketch plane", "sketch line" })
        {
            var count = sketchRows.Count(row => row[2] == word);
            if (count > 0) parts.Add(count + " " + word + (word == "sketch" ? "(es)" : "(s)"));
        }
        extras.Add(string.Format("the {0} form(s)' own sketches {1} ({2})",
            forms, sketchRows.Count, string.Join(", ", parts.ToArray())));
    }
    if (otherRows.Count > 0) extras.Add(byCategory(otherRows));
    if (notRead > 0)
        extras.Add(notRead + " NOT READ - Revit gave nothing to read for them before the delete, "
            + "so what they were is not known");

    var extraCount = sketchRows.Count + otherRows.Count + notRead;
    // "N deleted = F form(s) + their sketches" only when that is the whole of
    // it: every element asked for was a form with its sketch, and nothing else went.
    var lead = forms > 0 && forms == askedRows.Count && otherRows.Count == 0 && notRead == 0
        ? string.Format("{0} deleted = {1} form(s) + their sketches", deleted.Count, forms)
        : deleted.Count + " deleted";
    deletedByCategory = lead
        + ". Asked for " + askedRows.Count + (askedRows.Count > 0 ? ": " + byCategory(askedRows) : "")
        + ". " + (extraCount > 0
            ? "Also went " + extraCount + ": " + string.Join("; ", extras.ToArray()) + "."
            : "Nothing else went.")
        + " Each was read before the delete, from Revit's own trial of it; every id is in deletedWhat.";
}

// ---- VERSION 2: WHICH OF THOSE WALLS LOST THEIR ATTACHMENT, MEASURED ------
if (candidates.Count > 0 && deleted.Count > 0)
{
    try
    {
        doc.Regenerate();
    }
    catch (Exception notRebuilt)
    {
        throw new InvalidOperationException(
            "Revit could not rebuild the model after the delete, so the walls attached to what was "
            + "deleted could not be read again - " + notRebuilt.Message + ". Nothing this call did is kept");
    }

    var stillAttached = 0;
    foreach (var candidate in candidates)
    {
        var wall = candidate.Item1;
        if (!wall.IsValidObject || deleted.Contains(wall.Id)) continue;
        var topNow = isAttached(wall, BuiltInParameter.WALL_TOP_IS_ATTACHED);
        var baseNow = isAttached(wall, BuiltInParameter.WALL_BOTTOM_IS_ATTACHED);
        if ((candidate.Item2 && !topNow) || (candidate.Item3 && !baseNow))
            detachedWalls.Add(wall.Id);
        else
            stillAttached++;
    }

    if (detachedWalls.Count > 0)
    {
        int release;
        int.TryParse(doc.Application.VersionNumber, out release);
        wallsNote = string.Format(
            "{0} wall(s) were attached to what was deleted and are DETACHED now - each wall's top or "
            + "base goes back to its own height and level: {1}. {2}{3}",
            detachedWalls.Count,
            string.Join(", ", detachedWalls.Select(id => id.ToString()).ToArray()),
            release > 0 && release < 2025
                ? "On Revit " + release + " Heron cannot attach a wall again - Attach Top/Base by hand "
                  + "(Modify | Walls) once the new roof, floor or ceiling is in."
                : "ATTACH_WALLS can attach them to what replaces it.",
            stillAttached > 0
                ? string.Format(" {0} other wall(s) there are still attached to something else.", stillAttached)
                : "");
    }
}
