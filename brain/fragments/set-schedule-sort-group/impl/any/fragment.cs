// NOT STANDALONE. Assumes `doc`, `elements`, `sortFieldNames`, `descending`,
// `showHeader`, `showFooter`, `showGrandTotal` and `itemized` are in scope,
// and since VERSION 2 `showFooterTitle`, `showFooterCount`, `showBlankLine`,
// `levelOptions`, `grandTotalTitle`, `showGrandTotalTitle` and
// `showGrandTotalCount`; leaves `sorted`, `replacedRules`, `cannotSortBy`,
// `notPresent`, `appliedOrder` and `refused` behind.
//
// VERSION 2 - EVERY SWITCH ON THE SORTING/GROUPING TAB, EACH LEFT OUT MEANING
// EXACTLY WHAT VERSION 1 DID. Each new input is `optional: true` with an
// `absent:` word, so a version 1 caller is bound as before:
//
//   showFooterTitle, showFooterCount   "yes" | "no" | left out ("leave"): the
//                                      footer's title and its count, on every
//                                      level that has a footer. Left out,
//                                      nothing is set
//   showBlankLine                      "yes" | "no" | left out: a blank line
//                                      after each group, every level. Left
//                                      out, NOTHING IS SET - version 1's rule,
//                                      kept (see the comment at the write)
//   levelOptions                       one entry per sort level, in the order
//                                      the fields are named, ";" between
//                                      levels, "," between words: ascending,
//                                      descending, header, no header, footer,
//                                      no footer, footer title, no footer
//                                      title, footer count, no footer count,
//                                      blank line, no blank line. A word
//                                      overrides the flat switches for that
//                                      level only; an empty entry overrides
//                                      nothing. Left out it is "none"
//   grandTotalTitle                    the grand total's title text, which
//                                      also turns the title on. Left out
//                                      ("leave"), Revit's text is kept
//   showGrandTotalTitle,               "yes" | "no" | left out: left out, both
//   showGrandTotalCount                are forced on with the grand total, as
//                                      version 1 did
//
// EVERY WORD IS READ BEFORE ANY SCHEDULE IS TOUCHED. An unknown word, two
// words that contradict, an entry for a level beyond the fields named, a
// footer title or count on a level with no footer, or a grand total title on
// a grand total being turned off, refuses the WHOLE call - nothing cleared,
// nothing set - in the same `refused` sentence version 1 uses.
//
// ASSUMPTION THE PROOF SETTLES. Revit's reference does not say what a NEW
// ScheduleSortGroupField holds for ShowFooterTitle, ShowFooterCount and
// ShowBlankLine. Left out, they are not set, and appliedOrder reads all three
// back per level - so the proof records Revit's default rather than this file
// claiming one.
//
// `cannotSortBy` AND `refused` ARE DIFFERENT SENTENCES. The first is a finding
// ABOUT THE SCHEDULE - Revit will not sort on that field. The second is this
// fragment saying it could not use what it was handed at all, which used to
// come back as `sorted 0` and read as "already in that order". Section 3h.1 of
// docs/FRAGMENT-ISSUES.md, 2026-09-09.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// SORTING AND GROUPING ARE ONE CONTROL IN REVIT. There is no separate group-by:
// a sort field with ShowHeader on IS a group, and the header is the band
// carrying the level name across the sheet. "Group by system type" and "sort by
// system type" are this same call with one flag different.
//
// THE SORT IS REPLACED, NOT APPENDED TO. Asking for "sort by level" on a
// schedule already sorted by mark means sorted by LEVEL. Appending is how a
// schedule ends up with four sort levels nobody chose - so what was there is
// REPORTED before it goes, rather than vanishing quietly.
//
// EVERY FIELD IS ASKED ABOUT BEFORE IT IS TRIED. CanSortByField is Revit's own
// answer and it covers what is otherwise an exception half way through a batch
// that has already changed the first two fields.
//
// READ FIRST, WRITE, READ BACK. `appliedOrder` is a second read of what the
// schedule now sorts by, not an echo of what was asked for.

// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE. The same fix and the
// same reason as REPORT_SCHEDULE_DEFINITION, where it was found on 2026-09-08:
// a schedule is a VIEW, a view cannot be selected as an element, opening one
// selects its ROWS, and nothing in this library provides a ViewSchedule to
// chain from. So a bare cast refuses the only input that could ever arrive.
//
// The placement carries the id of the schedule it draws, so it is resolved
// here rather than passed over.
Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;

    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;

    // Guarded like everything else: a placement whose schedule was deleted
    // under it should cost one element, not the whole run.
    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

var sorted = 0;
var replacedRules = new List<string>();
var cannotSortBy = new List<string>();
var notPresent = new List<string>();
var appliedOrder = new List<string>();
string refused = null;

var handed = 0;
var schedulesSeen = 0;

var askedFor = 0;
if (sortFieldNames != null)
{
    foreach (var wantedName in sortFieldNames)
    {
        if (!string.IsNullOrEmpty(wantedName)) askedFor++;
    }
}

if (askedFor == 0)
{
    refused = "no column was named to sort or group by, so NOTHING WAS SORTED. Name at least "
        + "one field - clearing the sort instead would be a change nobody asked for";
}

// ---- VERSION 2: every new word, read before any schedule is touched -------
Func<string, string> tidy = text => string.Join(" ", (text ?? "").Trim().ToLowerInvariant()
    .Split(new[] { ' ', (char)9 }, StringSplitOptions.RemoveEmptyEntries));
var problems = new List<string>();

// A yes/no switch: null is "leave" - the card's `absent: leave` - and
// "leave" said aloud is the same.
Func<string, string, bool?> yesNo = (label, text) =>
{
    var t = tidy(text);
    if (t.Length == 0 || t == "leave") return null;
    if (t == "yes" || t == "on" || t == "true" || t == "show") return true;
    if (t == "no" || t == "off" || t == "false" || t == "hide") return false;
    problems.Add(string.Format("{0} takes yes or no, not '{1}'", label, (text ?? "").Trim()));
    return null;
};
var wantFooterTitle = yesNo("showFooterTitle", showFooterTitle);
var wantFooterCount = yesNo("showFooterCount", showFooterCount);
var wantBlankLine = yesNo("showBlankLine", showBlankLine);
var wantGrandTitleShown = yesNo("showGrandTotalTitle", showGrandTotalTitle);
var wantGrandCount = yesNo("showGrandTotalCount", showGrandTotalCount);

// The grand total's title text. Left out it is "leave" (`absent: leave`).
var titleText = (grandTotalTitle ?? "").Trim();
if (tidy(titleText) == "leave") titleText = "";

// One settings record per sort level, in the order the fields are named.
// Each slot is null until a word in that level's entry sets it.
var levelDescending = new List<bool?>();
var levelHeader = new List<bool?>();
var levelFooter = new List<bool?>();
var levelFooterTitle = new List<bool?>();
var levelFooterCount = new List<bool?>();
var levelBlankLine = new List<bool?>();
for (var level = 0; level < askedFor; level++)
{
    levelDescending.Add(null); levelHeader.Add(null); levelFooter.Add(null);
    levelFooterTitle.Add(null); levelFooterCount.Add(null); levelBlankLine.Add(null);
}

var levelsText = (levelOptions ?? "").Trim();
if (tidy(levelsText) == "none") levelsText = "";
var accepted = "ascending, descending, header, no header, footer, no footer, footer title, "
    + "no footer title, footer count, no footer count, blank line, no blank line";
if (levelsText.Length > 0)
{
    var entries = levelsText.Split(';');
    // A trailing ";" is not a level.
    var count = entries.Length;
    while (count > 0 && tidy(entries[count - 1]).Length == 0) count--;
    if (count > askedFor)
    {
        problems.Add(string.Format("levelOptions has {0} levels but {1} sort field(s) were named - "
            + "one entry per field, in order, with ';' between", count, askedFor));
    }
    else
    {
        for (var level = 0; level < count; level++)
        {
            foreach (var raw in entries[level].Split(','))
            {
                var word = tidy(raw);
                if (word.Length == 0) continue;
                List<bool?> slot = null;
                bool value = true;
                switch (word)
                {
                    case "ascending": case "asc": slot = levelDescending; value = false; break;
                    case "descending": case "desc": slot = levelDescending; value = true; break;
                    case "header": slot = levelHeader; value = true; break;
                    case "no header": slot = levelHeader; value = false; break;
                    case "footer": slot = levelFooter; value = true; break;
                    case "no footer": slot = levelFooter; value = false; break;
                    case "footer title": slot = levelFooterTitle; value = true; break;
                    case "no footer title": slot = levelFooterTitle; value = false; break;
                    case "footer count": slot = levelFooterCount; value = true; break;
                    case "no footer count": slot = levelFooterCount; value = false; break;
                    case "blank line": slot = levelBlankLine; value = true; break;
                    case "no blank line": slot = levelBlankLine; value = false; break;
                }
                if (slot == null)
                {
                    problems.Add(string.Format("'{0}' in level {1} of levelOptions is not a sort-level "
                        + "setting - the words are: {2}", raw.Trim(), level + 1, accepted));
                    continue;
                }
                if (slot[level].HasValue && slot[level].Value != value)
                {
                    problems.Add(string.Format("level {0} of levelOptions says '{1}' and its opposite - "
                        + "say one", level + 1, word.StartsWith("no ") ? word.Substring(3) : word));
                    continue;
                }
                slot[level] = value;
            }
        }
    }
}

// What each level will be, flat switch first, its own word over it.
Func<int, bool> footerAt = level => levelFooter[level] ?? showFooter;
for (var level = 0; level < askedFor && problems.Count == 0; level++)
{
    if (!footerAt(level) && (levelFooterTitle[level] == true || levelFooterCount[level] == true))
        problems.Add(string.Format("level {0} asks for a footer title or count but has no footer - "
            + "add 'footer' to that level, or turn showFooter on", level + 1));
}
var anyFooter = false;
for (var level = 0; level < askedFor; level++) if (footerAt(level)) anyFooter = true;
if (problems.Count == 0 && askedFor > 0 && !anyFooter && (wantFooterTitle == true || wantFooterCount == true))
    problems.Add("showFooterTitle or showFooterCount was asked, but no sort level has a footer - "
        + "turn showFooter on, or add 'footer' to a level in levelOptions");
if (!showGrandTotal && (titleText.Length > 0 || wantGrandTitleShown == true || wantGrandCount == true))
    problems.Add("a grand total title or count was asked while showGrandTotal is off - turn the "
        + "grand total on to give it a title or a count");
if (titleText.Length > 0 && wantGrandTitleShown == false)
    problems.Add("grandTotalTitle gives the grand total a title and showGrandTotalTitle hides it - "
        + "say one");

if (refused == null && problems.Count > 0)
{
    refused = string.Join("; ", problems) + ". NOTHING WAS SORTED - no schedule was touched";
}

Func<bool, string> onOff = flag => flag ? "on" : "off";

foreach (var element in elements)
{
    if (refused != null) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    // Resolve every requested name to a field FIRST. Nothing is cleared until
    // the whole request is known to be workable - clearing and then failing
    // leaves a schedule with no sorting at all, which is worse than refusing.
    var wantedIds = new List<ScheduleFieldId>();
    var wantedNames = new List<string>();
    var blocked = false;

    foreach (var wanted in sortFieldNames)
    {
        if (string.IsNullOrEmpty(wanted)) continue;

        ScheduleFieldId foundId = null;
        string foundName = null;
        foreach (var fieldId in definition.GetFieldOrder())
        {
            var field = definition.GetField(fieldId);
            if (field == null) continue;
            if (string.Equals(field.GetName(), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.ColumnHeading, wanted, StringComparison.OrdinalIgnoreCase))
            {
                foundId = fieldId;
                foundName = field.GetName();
                break;
            }
        }

        if (foundId == null)
        {
            notPresent.Add(string.Format("'{0}' on '{1}' - not a column in this schedule, so there "
                + "is nothing to sort by. Add it first", wanted, schedule.Name));
            blocked = true;
            continue;
        }

        var canSort = false;
        try { canSort = definition.CanSortByField(foundId); }
        catch (Exception) { canSort = false; }

        if (!canSort)
        {
            cannotSortBy.Add(string.Format("'{0}' on '{1}' - Revit does not allow sorting on this "
                + "field", foundName, schedule.Name));
            blocked = true;
            continue;
        }

        wantedIds.Add(foundId);
        wantedNames.Add(foundName);
    }

    if (blocked || wantedIds.Count == 0)
    {
        // Nothing is changed on this schedule. See the header.
        continue;
    }

    // Report what is being replaced before it goes.
    foreach (var existing in definition.GetSortGroupFields())
    {
        var field = definition.GetField(existing.FieldId);
        replacedRules.Add(string.Format("'{0}' was sorted by {1} {2}", schedule.Name,
            field == null ? "(a field no longer in the schedule)" : field.GetName(),
            existing.SortOrder == ScheduleSortOrder.Descending ? "descending" : "ascending"));
    }

    try
    {
        definition.ClearSortGroupFields();

        for (var level = 0; level < wantedIds.Count; level++)
        {
            var levelDown = levelDescending[level] ?? descending;
            var rule = new ScheduleSortGroupField(wantedIds[level],
                levelDown ? ScheduleSortOrder.Descending : ScheduleSortOrder.Ascending);
            rule.ShowHeader = levelHeader[level] ?? showHeader;
            var footerOn = footerAt(level);
            rule.ShowFooter = footerOn;
            // VERSION 2. A footer's title and count are set only on a level
            // that has a footer, and only when asked - the level's own word
            // first, then the flat switch. Left out, nothing is set.
            if (footerOn)
            {
                var title = levelFooterTitle[level] ?? wantFooterTitle;
                if (title.HasValue) rule.ShowFooterTitle = title.Value;
                var countOn = levelFooterCount[level] ?? wantFooterCount;
                if (countOn.HasValue) rule.ShowFooterCount = countOn.Value;
            }
            // ShowBlankLine is LEFT AS REVIT HAS IT unless it is ASKED FOR. A
            // blank line between groups is a separate drafting choice, and
            // tying it to the footer flag would be this fragment inventing a
            // preference and then changing the sheet to match it. Version 2
            // keeps that: left out, it is not set, exactly as version 1; only
            // an explicit showBlankLine or a level's "blank line" sets it.
            var blank = levelBlankLine[level] ?? wantBlankLine;
            if (blank.HasValue) rule.ShowBlankLine = blank.Value;
            definition.AddSortGroupField(rule);
        }

        definition.IsItemized = itemized;
        definition.ShowGrandTotal = showGrandTotal;
        if (showGrandTotal)
        {
            // Version 1 forced the title and the count on with the grand total;
            // left out, version 2 still does.
            definition.ShowGrandTotalTitle = wantGrandTitleShown ?? true;
            definition.ShowGrandTotalCount = wantGrandCount ?? true;
            if (titleText.Length > 0) definition.GrandTotalTitle = titleText;
        }
    }
    catch (Exception ex)
    {
        cannotSortBy.Add(string.Format("'{0}' - Revit refused the sort: {1}",
            schedule.Name, ex.Message));
        continue;
    }

    // Read back. What is reported is what a second read found - every switch
    // of every level, so Revit's defaults for what was left out are recorded.
    var after = definition.GetSortGroupFields();
    var readBack = new List<string>();
    foreach (var rule in after)
    {
        var field = definition.GetField(rule.FieldId);
        readBack.Add(string.Format("{0} {1}{2}, footer {3}{4}, blank line {5}",
            field == null ? "(unknown)" : field.GetName(),
            rule.SortOrder == ScheduleSortOrder.Descending ? "descending" : "ascending",
            rule.ShowHeader ? " with header (grouped)" : ", no header",
            onOff(rule.ShowFooter),
            rule.ShowFooter
                ? string.Format(" (title {0}, count {1})", onOff(rule.ShowFooterTitle), onOff(rule.ShowFooterCount))
                : "",
            onOff(rule.ShowBlankLine)));
    }

    var grandTotal = "off";
    if (definition.ShowGrandTotal)
    {
        string readTitle;
        try { readTitle = definition.GrandTotalTitle; } catch (Exception) { readTitle = "(unreadable)"; }
        grandTotal = string.Format("ON - title {0}{1}, count {2}", onOff(definition.ShowGrandTotalTitle),
            definition.ShowGrandTotalTitle ? " '" + readTitle + "'" : "", onOff(definition.ShowGrandTotalCount));
    }

    appliedOrder.Add(string.Format("'{0}': {1}; {2}; grand total {3}", schedule.Name,
        readBack.Count == 0 ? "nothing" : string.Join(", then ", readBack),
        definition.IsItemized
            ? "every element on its own row"
            : "rows collapsed - identical elements share a row",
        grandTotal));

    var titleKept = true;
    if (showGrandTotal && titleText.Length > 0)
    {
        try { titleKept = definition.GrandTotalTitle == titleText; } catch (Exception) { titleKept = false; }
    }

    if (after.Count == wantedIds.Count && titleKept) sorted++;
    else if (after.Count != wantedIds.Count)
        cannotSortBy.Add(string.Format("'{0}' - asked for {1} sort field(s), a read back finds {2}",
            schedule.Name, wantedIds.Count, after.Count));
    else
        cannotSortBy.Add(string.Format("'{0}' - asked for the grand total title '{1}', a read back "
            + "finds another", schedule.Name, titleText));
}

// Decided after the loop, and safe there: an element that is not a schedule
// `continue`s before ClearSortGroupFields is ever reached.
if (refused == null && handed > 0 && schedulesSeen == 0)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS SORTED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed);
}
