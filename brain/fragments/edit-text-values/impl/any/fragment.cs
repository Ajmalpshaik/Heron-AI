// NOT STANDALONE. Assumes `doc`, `elements`, `field`, `mode`, `text` and
// `replacement` are in scope, and leaves `changed`, `untouched`, `blank`,
// `readOnly`, `absent`, `unverified` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16 wants
// one user action to be one undo entry, and that belongs to the operation's
// TransactionGroup.
//
// READ, TRANSFORM, WRITE - WHICH IS WHY THIS IS NOT WRITE_ELEMENT_PARAMETERS.
//
// That one sets a LITERAL. "Put AJ- in front of every mark" cannot be done that
// way, because the value is different on every element.
//
// THE ELEMENT'S NAME IS JUST ANOTHER FIELD.
//
// `field` of "Name" edits Element.Name; anything else is a parameter. The
// earlier library had a rename fragment and a prefix/suffix fragment carrying
// the same four transformations twice - so a fix to the replace rule could
// reach one and miss the other.
//
// TEXT ONLY, ON PURPOSE.
//
// A number is REFUSED rather than edited through its display text. That round
// trip is lossy: a length displayed as 2500 is not exactly 2500 internally, and
// writing the display back rewrites a rounded value as the truth.
//
// A BLANK VALUE IS LEFT ALONE. A prefix on an empty parameter creates a value
// where the modeller had none - a different edit from the one asked for, and it
// would look like data afterwards.
//
// ===========================================================================
// AN EDIT IT CANNOT MAKE IS REFUSED. IT NEVER COMES BACK AS "NOTHING CHANGED".
// ===========================================================================
//
// Found in front of a model on 2026-09-09: `field=text` against seven text
// notes reported all seven `absent` and changed nothing. That reads as "I
// looked and there was nothing to do", and what actually happened was "I could
// not see what you gave me" - the field is called Text, and a mistyped name
// answered exactly like a correct one on a set with nothing to change.
//
// Three inputs can each make the whole request unworkable, and each is now
// checked BEFORE the first element is touched:
//
//   field   nothing to look for
//   mode    not prefix, suffix or replace - `transform` would hand every value
//           straight back and every element would land in `untouched`
//   text    prefix and suffix would add nothing, replace would search for
//           nothing
//
// Two more can only be known after LOOKING, so they are decided after the
// loop - which is safe precisely because both are paths on which nothing was
// written:
//
//   the field is on NOT ONE element handed in - a spelling mistake
//   the field is not TEXT on any of them - a number, and this fragment will
//   not edit a number through its display text
//
// Where a refusal is decided after looking, the list the unusable input filled
// is CLEARED. `absent: 7` on a run that did nothing reads as a finding, and it
// is not one - the count is in the refusal, which is where it belongs.

var changed = new List<ElementId>();
var untouched = new List<ElementId>();
var blank = new List<ElementId>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var unverified = new List<ElementId>();
string refused = null;

bool editingName = string.Equals((field ?? "").Trim(), "Name",
                                 StringComparison.OrdinalIgnoreCase);
string how = (mode ?? "").Trim().ToLowerInvariant();

// DECIDED BEFORE ANY ELEMENT IS READ, let alone written. Each of these three
// makes every element answer the same way, which is why the failure looked
// like a fragment correctly finding nothing.
if (string.IsNullOrEmpty((field ?? "").Trim()))
{
    refused = "no field was given, so there is nothing to edit. Name the parameter to change, "
        + "or 'Name' for the element's own name. NOTHING WAS CHANGED";
}
else if (how.Length == 0)
{
    refused = "no mode was given, so there is nothing to do. Use prefix, suffix or replace. "
        + "NOTHING WAS CHANGED";
}
else if (how != "prefix" && how != "suffix" && how != "replace")
{
    refused = string.Format(
        "'{0}' is not an edit this fragment makes, so NOTHING WAS CHANGED. It makes three: "
        + "prefix (put text in front), suffix (put text after) and replace (swap one piece of "
        + "text for another). Carrying on would hand every value straight back and report them "
        + "all untouched, which reads exactly like an edit that had nothing to do", mode);
}
else if (string.IsNullOrEmpty(text))
{
    refused = string.Format(
        "no text was given, so a '{0}' has nothing to work with and NOTHING WAS CHANGED. For "
        + "prefix and suffix this is the text to add; for replace it is the text to look for. "
        + "To DELETE a piece of text, give it as the text and leave the replacement empty",
        how);
}

var handed = 0;
var notText = 0;

// The names that ARE there, so the next question is already answered - the
// same courtesy ADD_SCHEDULE_FIELDS pays with `availableFields`. Read off the
// FIRST element only: on a mixed selection the full list is long and no more
// useful than one real example, and this is a sentence a person reads.
Func<IList<Element>, string> namesOnHand = given =>
{
    var available = new List<string>();
    foreach (var element in given)
    {
        if (element == null) continue;
        try
        {
            foreach (Parameter candidate in element.Parameters)
            {
                var definition = candidate.Definition;
                if (definition == null) continue;
                var name = definition.Name ?? "";
                if (name.Length > 0 && !available.Contains(name)) available.Add(name);
            }
        }
        catch { }
        break;
    }
    available.Sort(StringComparer.OrdinalIgnoreCase);

    if (available.Count == 0) return "The first element reports no parameters at all";
    var shown = available.Count > 12 ? available.GetRange(0, 12) : available;
    return "The first element carries: " + string.Join(", ", shown)
        + (available.Count > shown.Count
            ? string.Format(", and {0} more", available.Count - shown.Count)
            : "");
};

Func<string, string> transform = current =>
{
    if (how == "prefix") return text + current;
    if (how == "suffix") return current + text;
    if (how == "replace")
    {
        if (string.IsNullOrEmpty(text)) return current;
        return current.Replace(text, replacement ?? "");
    }
    return current;
};

foreach (var element in elements)
{
    // An unworkable request stops here rather than reporting on each element
    // in turn. Every one of them would answer identically anyway, and a list
    // of identical non-answers is what made this hard to see.
    if (refused != null) break;
    if (element == null) continue;
    handed++;

    string current = null;
    Parameter parameter = null;

    if (editingName)
    {
        try { current = element.Name; } catch { }
    }
    else
    {
        parameter = element.LookupParameter(field);
        if (parameter == null)
        {
            // The type is asked second, never first: an instance parameter of
            // the same name is the one the modeller edited.
            Element elementType = null;
            try { elementType = doc.GetElement(element.GetTypeId()); } catch { }
            if (elementType != null) parameter = elementType.LookupParameter(field);
        }

        if (parameter == null) { absent.Add(element.Id); continue; }
        // Counted apart from the other two ways into `readOnly`, and only so
        // that "was it text on ANY of them" can be answered below. A locked
        // text field is a real finding; a number is a wrong request.
        if (parameter.StorageType != StorageType.String)
        {
            notText++;
            readOnly.Add(element.Id);
            continue;
        }
        if (parameter.IsReadOnly) { readOnly.Add(element.Id); continue; }
        try { current = parameter.AsString(); } catch { }
    }

    if (string.IsNullOrEmpty(current)) { blank.Add(element.Id); continue; }

    string wanted = transform(current);
    if (wanted == current) { untouched.Add(element.Id); continue; }

    bool accepted = false;
    try
    {
        if (editingName) { element.Name = wanted; accepted = true; }
        else accepted = parameter.Set(wanted);
    }
    catch { readOnly.Add(element.Id); continue; }

    if (!accepted) { readOnly.Add(element.Id); continue; }

    // READ BACK. A count of attempts is not a count of changes - which is the
    // failure this whole project is built around.
    string now = null;
    try
    {
        if (editingName) now = element.Name;
        else now = parameter.AsString();
    }
    catch { }

    if (now == null) unverified.Add(element.Id);
    else if (now == wanted) changed.Add(element.Id);
    else unverified.Add(element.Id);
}

// AFTER THE LOOP, AND ONLY BECAUSE NOTHING WAS WRITTEN ON EITHER PATH. Both
// branches below are reached only when every element fell out at `absent` or
// at the not-text check, and both of those `continue` before a single Set.
if (refused == null && !editingName && handed > 0 && absent.Count == handed)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in carries a field called '{1}', so NOTHING WAS "
        + "CHANGED. Check the spelling - the name wanted is the parameter's own, not the "
        + "schedule heading, and it is matched exactly. {2}",
        handed, field, namesOnHand(elements));
    absent.Clear();
}
else if (refused == null && !editingName && handed > 0 && notText == handed)
{
    refused = string.Format(
        "'{0}' is not a text field on any of the {1} element(s) handed in, so NOTHING WAS "
        + "CHANGED. A number is not edited through its display text: a length shown as 2500 is "
        + "not exactly 2500 inside, and writing the display back would store the rounded "
        + "figure as the truth", field, handed);
    readOnly.Clear();
}
