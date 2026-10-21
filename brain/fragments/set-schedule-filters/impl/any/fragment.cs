// NOT STANDALONE. Assumes `doc`, `elements`, `filterFieldName`, `filterValue`,
// `matchType`, `filterAction`, `ruleNumber` and `filterRules` are in scope;
// leaves `filtered`, `replacedRules`, `cannotFilterBy`, `notPresent`,
// `appliedRules`, `refused`, `rulesBefore` and `rulesAfter` behind.
//
// `cannotFilterBy` AND `refused` ARE DIFFERENT SENTENCES, and separating them
// is the point of `refused`. The first is a finding ABOUT THE SCHEDULE - Revit
// will not filter on that field. The second is this fragment saying it could
// not use what it was handed, which is not a finding about anything and must
// not read as one. Section 3h.1 of docs/FRAGMENT-ISSUES.md, 2026-09-09.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// NOTHING IN THE MODEL IS TOUCHED. This changes which rows a table shows. The
// sentence it answers used to resolve to SET_ELEMENT_LEVEL, which moves ducts
// to another level - so it is worth saying plainly in the file itself.
//
// THE MATCH TYPE IS A STATED INPUT AND IS NOT GUESSED FROM THE VALUE. "Equals",
// "contains" and "begins with" produce different drawings from the same word,
// and inferring one is how a schedule silently includes Level 20 when somebody
// asked for Level 2. An unrecognised match type is REFUSED rather than
// defaulted - a wrong default here is invisible on screen.
//
// VERSION 2 - FOUR WAYS TO WRITE, ONE OF THEM VERSION 1's (row 5b-1289).
// `filterAction` left out is "replace": every rule the schedule had goes and
// the rule given takes their place, exactly as version 1 did, and what was
// there is reported before it goes. "change" alters ONE existing rule's value
// and/or match type and keeps every other rule; "add" puts a rule beside the
// existing ones; "remove" takes one rule out. `filterRules` gives several rules
// in one call. Found on 2026-10-21 on a real project: a duplicated schedule
// carried two rules - a type value and a level - and the modeller asked to
// change the value. Version 1 would have cleared both and written one, so the
// level rule would have gone with nothing on screen to say so.
//
// A RULE TO CHANGE OR REMOVE IS PICKED BY ITS FIELD, AND TWO RULES ON ONE FIELD
// ARE REFUSED WITHOUT A NUMBER. Picking the first of two is a guess, and a
// guessed rule is the wrong rows on a sheet that nobody re-reads. `ruleNumber`
// counts from 1 in the order the schedule holds its rules - the order
// REPORT_SCHEDULE_DEFINITION lists them in.
//
// A CHANGED VALUE IS WRITTEN IN THE KIND THE RULE ALREADY HOLDS. A text rule
// takes text. A rule that points at an element - a Level rule does - takes the
// NAME of another element of the same kind, looked up in the model, and is
// refused when no element or more than one has that name. A rule that holds a
// number is refused: a number goes to Revit in internal units, and which unit a
// parameter uses needs the units API - the call that breaks at Revit 2021. An
// element's name carries no unit, so the second is allowed and the third is not.
//
// NOTHING IS WRITTEN UNTIL THE WHOLE NEW SET OF RULES IS KNOWN, AND A REFUSAL
// FROM REVIT PUTS THE OLD SET BACK. Version 1 cleared the rules and then added
// one; when Revit refused the one, the schedule was left with no rules at all,
// which reads as a filter that matched everything.
//
// READ FIRST, WRITE, READ BACK. `rulesBefore` is what the schedule filtered on
// before anything was written; `rulesAfter` and `appliedRules` are a second
// read of what it filters on now. Both are ONE line each, because a list is cut
// to three entries before anyone reads it.

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

var filtered = 0;
var replacedRules = new List<string>();
var cannotFilterBy = new List<string>();
var notPresent = new List<string>();
var appliedRules = new List<string>();
var refused = new List<string>();
var rulesBefore = "";
var rulesAfter = "";

var beforeLines = new List<string>();
var afterLines = new List<string>();

var handed = 0;
var schedulesSeen = 0;
var requestUnusable = false;

// "keep" is what a left-out value or match type is bound as (the card's
// `absent:`), and "none" a left-out field or rule list.
Func<string, bool> leftOut = text =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    return t == "keep" || t == "none";
};

// A match type, from the words a person uses. Unrecognised is a refusal, not a
// default - so this answers null for it.
Func<string, ScheduleFilterType?> matchFrom = said =>
{
    var w = (said ?? "").Trim().ToLowerInvariant();
    if (w == "equals" || w == "equal" || w == "is") return ScheduleFilterType.Equal;
    if (w == "notequals" || w == "not equals" || w == "is not" || w == "does not equal")
        return ScheduleFilterType.NotEqual;
    if (w == "contains") return ScheduleFilterType.Contains;
    if (w == "notcontains" || w == "does not contain") return ScheduleFilterType.NotContains;
    if (w == "beginswith" || w == "begins with" || w == "starts with")
        return ScheduleFilterType.BeginsWith;
    if (w == "notbeginswith" || w == "does not begin with") return ScheduleFilterType.NotBeginsWith;
    if (w == "endswith" || w == "ends with") return ScheduleFilterType.EndsWith;
    if (w == "notendswith" || w == "does not end with") return ScheduleFilterType.NotEndsWith;
    if (w == "hasvalue" || w == "has value" || w == "has a value" || w == "is filled")
        return ScheduleFilterType.HasValue;
    if (w == "hasnovalue" || w == "has no value" || w == "is blank")
        return ScheduleFilterType.HasNoValue;
    return null;
};

// A match type in the words a person reads - the reverse of the above, and
// every member of the enum, because a rule this fragment does not WRITE can
// still be one it has to REPORT.
Func<ScheduleFilterType, string> matchWords = type =>
{
    switch (type)
    {
        case ScheduleFilterType.Equal: return "equals";
        case ScheduleFilterType.NotEqual: return "does not equal";
        case ScheduleFilterType.GreaterThan: return "is greater than";
        case ScheduleFilterType.GreaterThanOrEqual: return "is greater than or equal to";
        case ScheduleFilterType.LessThan: return "is less than";
        case ScheduleFilterType.LessThanOrEqual: return "is less than or equal to";
        case ScheduleFilterType.Contains: return "contains";
        case ScheduleFilterType.NotContains: return "does not contain";
        case ScheduleFilterType.BeginsWith: return "begins with";
        case ScheduleFilterType.NotBeginsWith: return "does not begin with";
        case ScheduleFilterType.EndsWith: return "ends with";
        case ScheduleFilterType.NotEndsWith: return "does not end with";
        case ScheduleFilterType.HasValue: return "has a value";
        case ScheduleFilterType.HasNoValue: return "has no value";
        case ScheduleFilterType.HasParameter: return "has the parameter";
        case ScheduleFilterType.IsAssociatedWithGlobalParameter: return "is associated with a global parameter";
        case ScheduleFilterType.IsNotAssociatedWithGlobalParameter: return "is not associated with a global parameter";
        default: return type.ToString();
    }
};

Func<ScheduleFilterType, bool> takesNoValue = type =>
    type == ScheduleFilterType.HasValue || type == ScheduleFilterType.HasNoValue;

// The heading as it prints, and the field's own name beside it when somebody
// has renamed the heading - only one of them is on the drawing.
Func<ScheduleDefinition, ScheduleFieldId, string> fieldLabel = (definition, fieldId) =>
{
    ScheduleField field = null;
    try { field = definition.GetField(fieldId); } catch { field = null; }
    if (field == null) return "(a field no longer in the schedule)";
    var name = field.GetName();
    var heading = field.ColumnHeading;
    if (string.IsNullOrEmpty(heading) || string.Equals(heading, name, StringComparison.Ordinal))
        return name;
    return string.Format("{0} (field {1})", heading, name);
};

// A rule as a person reads it. An element value is shown by its NAME - a Level
// rule holds the level's id, and the id means nothing to anyone.
Func<ScheduleDefinition, ScheduleFilter, string> describeRule = (definition, rule) =>
{
    string value;
    if (rule.IsNullValue) value = "";
    else if (rule.IsStringValue) value = " '" + rule.GetStringValue() + "'";
    else if (rule.IsElementIdValue)
    {
        Element target = null;
        try { target = doc.GetElement(rule.GetElementIdValue()); } catch { target = null; }
        value = target == null ? " (an element no longer here)" : " '" + target.Name + "'";
    }
    else if (rule.IsIntegerValue) value = " " + rule.GetIntegerValue();
    else if (rule.IsDoubleValue)
        value = " " + rule.GetDoubleValue().ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " (Revit internal units, not converted)";
    else value = " (a kind of value this does not read)";

    return fieldLabel(definition, rule.FieldId) + " " + matchWords(rule.FilterType) + value;
};

Func<ScheduleDefinition, IList<ScheduleFilter>, string> describeAll = (definition, rules) =>
{
    if (rules.Count == 0) return "no filter rules";
    var parts = new List<string>();
    for (var i = 0; i < rules.Count; i++)
        parts.Add(string.Format("{0}. {1}", i + 1, describeRule(definition, rules[i])));
    return string.Join("; ", parts);
};

// A fresh copy of a rule, value and all, so the set put back after a refusal
// does not depend on what Revit did to the objects it was handed.
Func<ScheduleFilter, ScheduleFilter> copyOf = rule =>
{
    if (rule.IsStringValue) return new ScheduleFilter(rule.FieldId, rule.FilterType, rule.GetStringValue());
    if (rule.IsElementIdValue) return new ScheduleFilter(rule.FieldId, rule.FilterType, rule.GetElementIdValue());
    if (rule.IsIntegerValue) return new ScheduleFilter(rule.FieldId, rule.FilterType, rule.GetIntegerValue());
    if (rule.IsDoubleValue) return new ScheduleFilter(rule.FieldId, rule.FilterType, rule.GetDoubleValue());
    return new ScheduleFilter(rule.FieldId, rule.FilterType);
};

// A field by its name or its printed heading, ignoring case.
Func<ScheduleDefinition, string, ScheduleFieldId> fieldCalled = (definition, said) =>
{
    foreach (var fieldId in definition.GetFieldOrder())
    {
        var field = definition.GetField(fieldId);
        if (field == null) continue;
        if (string.Equals(field.GetName(), said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.ColumnHeading, said, StringComparison.OrdinalIgnoreCase))
            return fieldId;
    }
    return null;
};

// ---- What was asked, read once, before any schedule is looked at. ----------

var action = (filterAction ?? "").Trim().ToLowerInvariant();
if (action == "" || action == "replace" || action == "set" || action == "keep") action = "replace";
else if (action == "change" || action == "edit" || action == "modify" || action == "update") action = "change";
else if (action == "add" || action == "append") action = "add";
else if (action == "remove" || action == "delete") action = "remove";
else
{
    requestUnusable = true;
    refused.Add(string.Format("'{0}' is not something this does to a schedule's filters, so NOTHING "
        + "WAS CHANGED. Use replace (every rule goes and the one given takes their place - what "
        + "happens when this is left out), change (one rule's value or match type, the rest kept), "
        + "add (a rule beside the others) or remove (one rule out)", filterAction));
}

// Each rule asked for, as three words: field, match type, value. "keep" in the
// second or third means the request did not give one.
var asks = new List<string[]>();
var rulesGiven = !leftOut(filterRules) && !string.IsNullOrEmpty((filterRules ?? "").Trim());
var fieldGiven = !leftOut(filterFieldName) && !string.IsNullOrEmpty((filterFieldName ?? "").Trim());

if (rulesGiven)
{
    if (fieldGiven)
    {
        requestUnusable = true;
        refused.Add("a column was named on its own AND a list of rules was given, so NOTHING WAS "
            + "CHANGED - which of the two was meant cannot be told. Give the rules in one or the "
            + "other");
    }
    foreach (var line in filterRules.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
    {
        if (string.IsNullOrEmpty(line.Trim())) continue;
        var parts = line.Split('|');
        var field = parts[0].Trim();
        var match = parts.Length > 1 && parts[1].Trim().Length > 0 ? parts[1].Trim() : "keep";
        // The value is everything after the second bar, so a value with a bar
        // in it survives.
        // A blank value keeps the rule's own when changing one, and is an empty
        // value when writing a new one.
        var value = parts.Length > 2 ? string.Join("|", parts, 2, parts.Length - 2).Trim() : "keep";
        if (value.Length == 0 && action == "change") value = "keep";
        asks.Add(new[] { field, match, value });
    }
    if (asks.Count == 0)
    {
        requestUnusable = true;
        refused.Add("the list of rules held no rule, so NOTHING WAS CHANGED. Write each as "
            + "column | match type | value, with a semicolon between rules");
    }
}
else
{
    asks.Add(new[] { fieldGiven ? filterFieldName.Trim() : "", matchType ?? "keep", filterValue ?? "keep" });
}

if (ruleNumber < 0)
{
    requestUnusable = true;
    refused.Add(string.Format("rule number {0} is not a rule - they count from 1, so NOTHING WAS "
        + "CHANGED", ruleNumber));
}
if (ruleNumber > 0 && (action == "replace" || action == "add"))
{
    requestUnusable = true;
    refused.Add(string.Format("a rule number picks a rule that is already there, to change or "
        + "remove it - it means nothing to {0}, so NOTHING WAS CHANGED", action));
}
if (ruleNumber > 0 && asks.Count > 1)
{
    requestUnusable = true;
    refused.Add("a rule number picks ONE rule, and several were given, so NOTHING WAS CHANGED. "
        + "Change or remove that one rule in a call of its own");
}

foreach (var ask in asks)
{
    if (requestUnusable) break;
    var wantsMatch = !leftOut(ask[1]);
    var match = wantsMatch ? matchFrom(ask[1]) : null;

    if (wantsMatch && match == null)
    {
        // A REFUSAL, not a finding. It was in `cannotFilterBy` until 2026-09-09,
        // where it made an unusable request look like something learnt about the
        // schedule - so a negative case could never come back empty.
        requestUnusable = true;
        refused.Add(string.Format("'{0}' is not a match type this fragment writes, so NOTHING WAS "
            + "FILTERED. Use one of: equals, does not equal, contains, does not contain, begins "
            + "with, does not begin with, ends with, does not end with, has value, has no value. "
            + "Guessing one from the value is how a schedule quietly includes Level 20 when "
            + "somebody asked for Level 2", ask[1]));
        break;
    }

    if (ask[0].Length == 0 && !(ruleNumber > 0 && (action == "change" || action == "remove")))
    {
        requestUnusable = true;
        refused.Add("no column was named to filter on, so NOTHING WAS FILTERED. Name the field or "
            + "the heading as it prints"
            + (action == "change" || action == "remove" ? ", or the rule's number" : ""));
        break;
    }

    if (action == "replace" || action == "add")
    {
        if (!wantsMatch)
        {
            requestUnusable = true;
            refused.Add(string.Format("no match type was given for the rule on '{0}', so NOTHING "
                + "WAS FILTERED - there is no rule there yet whose match type could be kept. Say "
                + "equals, contains, begins with, has value or another", ask[0]));
            break;
        }
        if (!takesNoValue(match.Value) && leftOut(ask[2]))
        {
            requestUnusable = true;
            refused.Add(string.Format("no value was given for '{0} {1}', so NOTHING WAS FILTERED",
                ask[0], matchWords(match.Value)));
            break;
        }
    }

    if (action == "change" && !wantsMatch && leftOut(ask[2]))
    {
        requestUnusable = true;
        refused.Add(string.Format("neither a new value nor a new match type was given for the rule "
            + "on '{0}', so NOTHING WAS CHANGED", ask[0].Length == 0 ? "rule " + ruleNumber : ask[0]));
        break;
    }
}

// ---- Each schedule. --------------------------------------------------------

foreach (var element in elements)
{
    if (requestUnusable) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    ScheduleDefinition definition;
    try { definition = schedule.Definition; }
    catch (Exception) { continue; }

    var before = definition.GetFilters();
    beforeLines.Add(string.Format("'{0}': {1}", schedule.Name, describeAll(definition, before)));

    // The whole new set, built before anything is written. A reason not to
    // write lands in `problem`, and the schedule is left as it was.
    var wanted = new List<ScheduleFilter>();
    foreach (var rule in before) wanted.Add(copyOf(rule));
    var touched = new List<int>();
    var gone = new List<string>();
    string problem = null;
    var problemIsAbsence = false;

    if (action == "replace") wanted.Clear();

    foreach (var ask in asks)
    {
        if (problem != null) break;

        ScheduleFieldId fieldId = null;
        if (ask[0].Length > 0)
        {
            fieldId = fieldCalled(definition, ask[0]);
            if (ReferenceEquals(fieldId, null))
            {
                problem = string.Format("'{0}' on '{1}' - a schedule cannot filter on a field it "
                    + "does not carry. Add the column first", ask[0], schedule.Name);
                problemIsAbsence = true;
                break;
            }
        }

        var wantsMatch = !leftOut(ask[1]);
        var match = wantsMatch ? matchFrom(ask[1]) : null;

        if (action == "replace" || action == "add")
        {
            var type = match.Value;
            if (!takesNoValue(type))
            {
                var canFilter = false;
                try { canFilter = definition.CanFilterByValue(fieldId); } catch { canFilter = false; }
                if (!canFilter)
                {
                    problem = string.Format("'{0}' on '{1}' - Revit does not allow filtering this "
                        + "field by a value", fieldLabel(definition, fieldId), schedule.Name);
                    break;
                }
                wanted.Add(new ScheduleFilter(fieldId, type, ask[2]));
            }
            else
            {
                var canPresence = false;
                try { canPresence = definition.CanFilterByValuePresence(fieldId); } catch { canPresence = false; }
                if (!canPresence)
                {
                    problem = string.Format("'{0}' on '{1}' - Revit does not allow a has-a-value "
                        + "or has-no-value rule on this field", fieldLabel(definition, fieldId),
                        schedule.Name);
                    break;
                }
                wanted.Add(new ScheduleFilter(fieldId, type));
            }
            touched.Add(wanted.Count - 1);
            continue;
        }

        // change and remove: find the ONE rule meant.
        var at = -1;
        if (ruleNumber > 0)
        {
            if (ruleNumber > wanted.Count)
            {
                problem = string.Format("'{0}' has {1} filter rule(s), so there is no rule {2}",
                    schedule.Name, wanted.Count, ruleNumber);
                problemIsAbsence = true;
                break;
            }
            at = ruleNumber - 1;
            if (!ReferenceEquals(fieldId, null) && wanted[at].FieldId != fieldId)
            {
                problem = string.Format("rule {0} on '{1}' is on {2}, not on '{3}' - the number "
                    + "and the column disagree, so neither was trusted", ruleNumber, schedule.Name,
                    fieldLabel(definition, wanted[at].FieldId), ask[0]);
                break;
            }
        }
        else
        {
            var onField = new List<int>();
            for (var i = 0; i < wanted.Count; i++)
                if (wanted[i].FieldId == fieldId) onField.Add(i);
            if (onField.Count == 0)
            {
                problem = string.Format("no filter rule on '{0}' uses '{1}' - it is a column there, "
                    + "but nothing filters on it. Use add to put a rule on it", schedule.Name,
                    fieldLabel(definition, fieldId));
                problemIsAbsence = true;
                break;
            }
            if (onField.Count > 1)
            {
                problem = string.Format("{0} filter rules on '{1}' use '{2}' - rules {3} - so which "
                    + "one was meant cannot be told. Say which with its rule number",
                    onField.Count, schedule.Name, fieldLabel(definition, fieldId),
                    string.Join(" and ", onField.Select(i => (i + 1).ToString())));
                break;
            }
            at = onField[0];
        }

        if (touched.Contains(at))
        {
            problem = string.Format("rule {0} on '{1}' was asked to change twice in one call",
                at + 1, schedule.Name);
            break;
        }
        touched.Add(at);

        var old = wanted[at];
        if (action == "remove")
        {
            gone.Add(string.Format("'{0}' rule {1}: {2}", schedule.Name, at + 1, describeRule(definition, old)));
            continue;
        }

        // change.
        if (old.FilterType == ScheduleFilterType.HasParameter
            || old.FilterType == ScheduleFilterType.IsAssociatedWithGlobalParameter
            || old.FilterType == ScheduleFilterType.IsNotAssociatedWithGlobalParameter)
        {
            problem = string.Format("rule {0} on '{1}' ({2}) is a kind of rule this does not "
                + "write. Change it in Revit's schedule Properties, Filter tab", at + 1,
                schedule.Name, describeRule(definition, old));
            break;
        }

        var newType = wantsMatch ? match.Value : old.FilterType;
        ScheduleFilter changed;

        if (takesNoValue(newType))
        {
            var canPresence = false;
            try { canPresence = definition.CanFilterByValuePresence(old.FieldId); } catch { canPresence = false; }
            if (!canPresence)
            {
                problem = string.Format("'{0}' on '{1}' - Revit does not allow a has-a-value or "
                    + "has-no-value rule on this field", fieldLabel(definition, old.FieldId), schedule.Name);
                break;
            }
            changed = new ScheduleFilter(old.FieldId, newType);
        }
        else if (leftOut(ask[2]))
        {
            // The value kept, whatever its kind - only the match type moves.
            if (old.IsNullValue)
            {
                problem = string.Format("rule {0} on '{1}' ({2}) holds no value to keep, and "
                    + "'{3}' needs one - give the value", at + 1, schedule.Name,
                    describeRule(definition, old), matchWords(newType));
                break;
            }
            changed = copyOf(old);
            changed.FilterType = newType;
        }
        else if (old.IsElementIdValue)
        {
            // An element by NAME, of the kind the rule points at now.
            Element current = null;
            try { current = doc.GetElement(old.GetElementIdValue()); } catch { current = null; }
            if (current == null)
            {
                problem = string.Format("rule {0} on '{1}' points at an element no longer in the "
                    + "model, so what kind of element '{2}' should be cannot be told. Set it in "
                    + "Revit's schedule Properties, Filter tab", at + 1, schedule.Name, ask[2]);
                break;
            }
            var kind = current.GetType();
            var named = new List<Element>();
            var names = new List<string>();
            try
            {
                foreach (var candidate in new FilteredElementCollector(doc).OfClass(kind))
                {
                    var label = candidate.Name ?? "";
                    names.Add(label);
                    if (string.Equals(label.Trim(), ask[2].Trim(), StringComparison.OrdinalIgnoreCase))
                        named.Add(candidate);
                }
            }
            catch (Exception)
            {
                problem = string.Format("rule {0} on '{1}' points at a {2}, and that kind of element "
                    + "cannot be looked up by name here. Set it in Revit's schedule Properties, "
                    + "Filter tab", at + 1, schedule.Name, kind.Name);
                break;
            }
            if (named.Count != 1)
            {
                names.Sort(StringComparer.OrdinalIgnoreCase);
                problem = string.Format("rule {0} on '{1}' points at a {2}, and {3} called '{4}'. "
                    + "Those in the model: {5}", at + 1, schedule.Name, kind.Name,
                    named.Count == 0 ? "no " + kind.Name + " is" : named.Count + " of them are",
                    ask[2], string.Join(", ", names.Distinct().Take(20))
                        + (names.Distinct().Count() > 20 ? ", ..." : ""));
                problemIsAbsence = named.Count == 0;
                break;
            }
            changed = new ScheduleFilter(old.FieldId, newType, named[0].Id);
        }
        else if (old.IsIntegerValue || old.IsDoubleValue)
        {
            problem = string.Format("rule {0} on '{1}' ({2}) holds a number, and a number goes to "
                + "Revit in its internal units, which needs the units API this library does not "
                + "use. Change it in Revit's schedule Properties, Filter tab", at + 1,
                schedule.Name, describeRule(definition, old));
            break;
        }
        else
        {
            var canFilter = false;
            try { canFilter = definition.CanFilterByValue(old.FieldId); } catch { canFilter = false; }
            if (!canFilter)
            {
                problem = string.Format("'{0}' on '{1}' - Revit does not allow filtering this field "
                    + "by a value", fieldLabel(definition, old.FieldId), schedule.Name);
                break;
            }
            changed = new ScheduleFilter(old.FieldId, newType, ask[2]);
        }

        gone.Add(string.Format("'{0}' rule {1} was: {2}", schedule.Name, at + 1, describeRule(definition, old)));
        wanted[at] = changed;
    }

    if (problem != null)
    {
        if (problemIsAbsence) notPresent.Add(problem);
        else cannotFilterBy.Add(problem);
        continue;
    }

    if (action == "remove")
    {
        touched.Sort();
        for (var i = touched.Count - 1; i >= 0; i--) wanted.RemoveAt(touched[i]);
    }

    if (action == "replace")
    {
        for (var i = 0; i < before.Count; i++)
            gone.Add(string.Format("'{0}' rule {1}: {2}", schedule.Name, i + 1, describeRule(definition, before[i])));
    }

    // The old set, kept apart, so a refusal can put it back.
    var restore = new List<ScheduleFilter>();
    foreach (var rule in before) restore.Add(copyOf(rule));

    try
    {
        if (wanted.Count == 0) definition.ClearFilters();
        else definition.SetFilters(wanted);
    }
    catch (Exception ex)
    {
        var putBack = false;
        try
        {
            if (restore.Count == 0) definition.ClearFilters();
            else definition.SetFilters(restore);
            putBack = definition.GetFilterCount() == restore.Count;
        }
        catch (Exception) { putBack = false; }

        cannotFilterBy.Add(string.Format("'{0}' - Revit refused the rules: {1}. {2}", schedule.Name,
            ex.Message, putBack
                ? "The rules it had were put back."
                : "THE RULES IT HAD COULD NOT BE PUT BACK - open its Filter tab before trusting it"));
        continue;
    }

    // Read back. What is reported is what a second read found.
    var after = definition.GetFilters();
    var same = after.Count == wanted.Count;
    for (var i = 0; same && i < after.Count; i++)
        same = after[i].FieldId == wanted[i].FieldId
            && after[i].FilterType == wanted[i].FilterType
            && describeRule(definition, after[i]) == describeRule(definition, wanted[i]);

    // A read back that differs is still a schedule that CHANGED, so what it
    // holds now goes into `rulesAfter` as well as into the reason.
    if (!same)
    {
        afterLines.Add(string.Format("'{0}': {1}", schedule.Name, describeAll(definition, after)));
        cannotFilterBy.Add(string.Format("'{0}' - {1} rule(s) were written and a read back finds "
            + "something else: {2}", schedule.Name, wanted.Count, describeAll(definition, after)));
        continue;
    }

    filtered++;
    replacedRules.AddRange(gone);
    afterLines.Add(string.Format("'{0}': {1}", schedule.Name, describeAll(definition, after)));

    if (action == "remove")
        appliedRules.Add(string.Format("'{0}': {1} rule(s) removed, {2} left", schedule.Name,
            touched.Count, after.Count));
    else
        foreach (var i in touched)
            appliedRules.Add(string.Format("'{0}' rule {1}: {2}", schedule.Name, i + 1,
                describeRule(definition, after[i])));
}

rulesBefore = string.Join("  ||  ", beforeLines);
rulesAfter = string.Join("  ||  ", afterLines);

// Decided after the loop, and safe there: an element that is not a schedule
// `continue`s before anything is written.
if (!requestUnusable && handed > 0 && schedulesSeen == 0)
{
    refused.Add(string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS FILTERED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed));
}
